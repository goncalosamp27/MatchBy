using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.PlayerRating;
using MatchBy.Repositories.User;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.PlayerRatings;

public class PlayerRatingService(
    IUserRepository userRepository,
    IMatchRepository matchRepository,
    IPlayerRatingRepository playerRatingRepository,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreatePlayerRatingDto> createRatingValidator,
    IValidator<UpdatePlayerRatingDto> updateRatingValidator) : IPlayerRatingService
{
    public async Task<Result<PlayerRatingDto>> GetRatingByIdAsync(string ratingId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PlayerRating? rating = await playerRatingRepository.GetByIdAsync(ratingId, dbContext, ct);
        return rating == null
            ? Result<PlayerRatingDto>.Fail($"Rating with id {ratingId} not found.")
            : Result<PlayerRatingDto>.Ok(rating.ToDto());
    }
    
    public static PaginationResponse<List<PlayerRatingDto>> MapToDtoPaginationResponse(PaginationResponse<List<PlayerRating>> ratings)
    {
        var ratingDtos = ratings.Data.Select(r => r.ToDto()).ToList();

        return new PaginationResponse<List<PlayerRatingDto>>
        {
            Data = ratingDtos,
            Page = ratings.Page,
            PageSize = ratings.PageSize,
            TotalCount = ratings.TotalCount
        };
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsForMatchAsync(
        string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if the match exists
        bool matchExists = await matchRepository.ExistsAsync(matchId, dbContext, ct);
        if (!matchExists)
        {
            return Result<PaginationResponse<List<PlayerRatingDto>>>.Fail($"Match with id {matchId} not found.");
        }
        PaginationResponse<List<PlayerRating>> ratings = await playerRatingRepository.GetRatingsForMatch(matchId, dbContext, page, pageSize, ct);
        
        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(MapToDtoPaginationResponse(ratings));
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsGivenByUserAsync(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PaginationResponse<List<PlayerRating>> ratings = await playerRatingRepository.GetRatingsGivenByUser(userId, dbContext, page, pageSize, ct);
        
        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(MapToDtoPaginationResponse(ratings));
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsReceivedByUserAsync(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PaginationResponse<List<PlayerRating>> ratings = await playerRatingRepository.GetRatingsReceivedByUser(userId, dbContext, page, pageSize, ct);
        
        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(MapToDtoPaginationResponse(ratings));
    }

    public async Task<Result<PlayerRatingDto>> CreateRatingAsync(CreatePlayerRatingDto createDto,
        CancellationToken ct = default)
    {
        ValidationResult validationResult = await createRatingValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        //we pass false because it has to be a member of the match, not just have a pending invite
        Match? match = await matchRepository.GetByIdAsync(createDto.MatchId, createDto.SentById, false, dbContext, ct);
        if (match is null)
        {
            return Result<PlayerRatingDto>.Fail("Match not found.");
        }

        var participantIds = match.Participants.Select(p => p.Id).ToHashSet();
        if (!participantIds.Contains(createDto.SentById) || !participantIds.Contains(createDto.ReceivedById))
        {
            return Result<PlayerRatingDto>.Fail("Both users must have participated in the match to create a rating.");
        }

        PlayerRating? existing = await playerRatingRepository.GetByIdAsync(createDto.SentById, createDto.ReceivedById, createDto.MatchId, dbContext, ct);
        if (existing is not null)
        {
            existing.Rating = createDto.Rating;
            existing.Comment = string.IsNullOrWhiteSpace(createDto.Comment) ? null : createDto.Comment.Trim();
        }
        else
        {
            existing = createDto.ToEntity();
            dbContext.PlayerRatings.Add(existing);
        }

        // Recalculate and update the user's average rating
        await UpdateUserAverageRatingAsync(createDto.ReceivedById, dbContext);
            
        await dbContext.SaveChangesAsync(ct);

        return Result<PlayerRatingDto>.Ok(existing.ToDto());
    }

    public async Task<Result<PlayerRatingDto>> UpdateRatingAsync(UpdatePlayerRatingDto updateDto,
        CancellationToken ct = default)
    {
        ValidationResult validationResult = await updateRatingValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        PlayerRating? rating = await playerRatingRepository.GetByIdAsync(updateDto.Id, dbContext, ct);
        if (rating == null)
        {
            return Result<PlayerRatingDto>.Fail($"Rating with id {updateDto.Id} not found.");
        }

        // Only the sender can update their rating
        if (rating.SentById != updateDto.SentById)
        {
            return Result<PlayerRatingDto>.Fail("Only the sender can update their rating.");
        }

        rating.UpdateEntity(updateDto);
        await dbContext.SaveChangesAsync(ct);

        return await GetRatingByIdAsync(rating.Id, ct);
    }

    public async Task<Result<bool>> DeleteRatingAsync(string ratingId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PlayerRating? rating = await playerRatingRepository.GetByIdAsync(ratingId, dbContext, ct);
        if (rating == null)
        {
            return Result<bool>.Fail($"Rating with id {ratingId} not found.");
        }

        // Only the sender can delete their rating
        if (rating.SentById != userId)
        {
            return Result<bool>.Fail("Only the sender can delete their rating.");
        }

        playerRatingRepository.Remove(rating, dbContext);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
    
    private async Task UpdateUserAverageRatingAsync(string userId, ApplicationDbContext dbContext)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, dbContext);
        if (user == null)
        {
            return;
        }
        
        PaginationResponse<List<PlayerRating>> receivedRatings = await playerRatingRepository.GetRatingsReceivedByUser(userId, dbContext, 1, int.MaxValue);
        if (!receivedRatings.Data.Any())
        {
            user.Rating = 0;
            return;
        }
        
        double average = receivedRatings.Data.Average(r => r.Rating);
        user.Rating = (float)Math.Round(average, 2);
    }
}