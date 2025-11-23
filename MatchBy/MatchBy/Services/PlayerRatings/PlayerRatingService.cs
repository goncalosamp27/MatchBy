using System.Linq.Dynamic.Core;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using Sentry.Protocol;

namespace MatchBy.Services.PlayerRatings;

public class PlayerRatingService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreatePlayerRatingDto> createRatingValidator,
    IValidator<UpdatePlayerRatingDto> updateRatingValidator,
    ILogger<PlayerRatingService> logger) : IPlayerRatingService
{
    public async Task<Result<PlayerRatingDto>> GetRatingById(string ratingId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PlayerRating? rating = await dbContext
            .PlayerRatings
            .AsNoTracking()
            .Include(r => r.SentBy)
            .Include(r => r.ReceivedBy)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Creator)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Participants)
            .FirstOrDefaultAsync(r => r.Id == ratingId, ct);

        return rating == null
            ? Result<PlayerRatingDto>.Fail($"Rating with id {ratingId} not found.")
            : Result<PlayerRatingDto>.Ok(rating.ToDto());
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsForMatch(
        string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if the match exists
        bool matchExists = await dbContext.Matches.AnyAsync(m => m.Id == matchId, ct);
        if (!matchExists)
        {
            return Result<PaginationResponse<List<PlayerRatingDto>>>.Fail($"Match with id {matchId} not found.");
        }

        IQueryable<Models.PlayerRating> query = dbContext
            .PlayerRatings
            .AsNoTracking()
            .Include(r => r.SentBy)
            .Include(r => r.ReceivedBy)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Creator)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Participants)
            .Where(r => r.MatchId == matchId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ratingDtos = ratings.Select(r => r.ToDto()).ToList();

        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(
            new PaginationResponse<List<PlayerRatingDto>>
            {
                Data = ratingDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsGivenByUser(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Models.PlayerRating> query = dbContext
            .PlayerRatings
            .AsNoTracking()
            .Include(r => r.SentBy)
            .Include(r => r.ReceivedBy)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Creator)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Participants)
            .Where(r => r.SentById == userId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ratingDtos = ratings.Select(r => r.ToDto()).ToList();

        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(
            new PaginationResponse<List<PlayerRatingDto>>
            {
                Data = ratingDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsReceivedByUser(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Models.PlayerRating> query = dbContext
            .PlayerRatings
            .AsNoTracking()
            .Include(r => r.SentBy)
            .Include(r => r.ReceivedBy)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Creator)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Participants)
            .Where(r => r.ReceivedById == userId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ratingDtos = ratings.Select(r => r.ToDto()).ToList();

        return Result<PaginationResponse<List<PlayerRatingDto>>>.Ok(
            new PaginationResponse<List<PlayerRatingDto>>
            {
                Data = ratingDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PlayerRatingDto>> CreatePlayerRatingAsync(CreatePlayerRatingDto createDto,
        CancellationToken ct = default)
    {
        if (createDto.SentById == createDto.ReceivedById)
        {
            return Result<PlayerRatingDto>.Fail("You cannot rate yourself.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        try
        {
            Match match = await dbContext.Matches
                .Include(m => m.Participants)
                .SingleOrDefaultAsync(m => m.Id == createDto.MatchId, ct);

            if (match is null)
            {
                return Result<PlayerRatingDto>.Fail("Match not found.");
            }

            var participantIds = match.Participants.Select(p => p.Id).ToHashSet();

            if (!participantIds.Contains(createDto.SentById))
            {
                return Result<PlayerRatingDto>.Fail("You must be a participant of the match to rate players.");
            }

            if (!participantIds.Contains(createDto.ReceivedById))
            {
                return Result<PlayerRatingDto>.Fail(
                    "The player you are trying to rate is not a participant of this match.");
            }

            Models.PlayerRating? existing = await dbContext.PlayerRatings
                .SingleOrDefaultAsync(r =>
                    r.SentById == createDto.SentById &&
                    r.ReceivedById == createDto.ReceivedById &&
                    r.MatchId == createDto.MatchId, ct);

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

            await dbContext.SaveChangesAsync(ct);

            // Recalculate and update the user's average rating
            await UpdateUserAverageRatingAsync(createDto.ReceivedById);

            logger.LogInformation("Rating from {SenterId} to {ReceiverId} for match {MatchId} saved successfully",
                createDto.SentById, createDto.ReceivedById, createDto.MatchId);

            return Result<PlayerRatingDto>.Ok(existing.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating player rating");
            return Result<PlayerRatingDto>.Fail("An error occurred while rating the player.");
        }
    }

    public async Task<Result<PlayerRatingDto>> CreateRating(CreatePlayerRatingDto createDto,
        CancellationToken ct = default)
    {
        ValidationResult validationResult = await createRatingValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if sender exists
        bool senderExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.SentById, ct);
        if (!senderExists)
        {
            return Result<PlayerRatingDto>.Fail($"Sender with id {createDto.SentById} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.ReceivedById, ct);
        if (!receiverExists)
        {
            return Result<PlayerRatingDto>.Fail($"Receiver with id {createDto.ReceivedById} not found.");
        }

        // Check if match exists
        Match? match = await dbContext.Matches
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == createDto.MatchId, ct);

        if (match == null)
        {
            return Result<PlayerRatingDto>.Fail($"Match with id {createDto.MatchId} not found.");
        }

        // Check if both users participated in the match
        bool senderParticipated = match.Participants.Any(p => p.Id == createDto.SentById) ||
                                  match.CreatorId == createDto.SentById;
        bool receiverParticipated = match.Participants.Any(p => p.Id == createDto.ReceivedById) ||
                                    match.CreatorId == createDto.ReceivedById;

        if (!senderParticipated)
        {
            return Result<PlayerRatingDto>.Fail("The sender must have participated in the match to give a rating.");
        }

        if (!receiverParticipated)
        {
            return Result<PlayerRatingDto>.Fail(
                "The receiver must have participated in the match to receive a rating.");
        }

        // Check if rating already exists
        bool existingRating = await dbContext.PlayerRatings
            .AnyAsync(r => r.SentById == createDto.SentById
                           && r.ReceivedById == createDto.ReceivedById
                           && r.MatchId == createDto.MatchId, ct);

        if (existingRating)
        {
            return Result<PlayerRatingDto>.Fail("A rating already exists for this user in this match.");
        }

        Models.PlayerRating rating = createDto.ToEntity();
        await dbContext.PlayerRatings.AddAsync(rating, ct);
        await dbContext.SaveChangesAsync(ct);

        return await GetRatingById(rating.Id, ct);
    }

    public async Task<Result<PlayerRatingDto>> UpdateRating(UpdatePlayerRatingDto updateDto,
        CancellationToken ct = default)
    {
        ValidationResult validationResult = await updateRatingValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        Models.PlayerRating? rating = await dbContext.PlayerRatings
            .FirstOrDefaultAsync(r => r.Id == updateDto.Id, ct);

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

        return await GetRatingById(rating.Id, ct);
    }

    public async Task<Result<bool>> DeleteRating(string ratingId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        Models.PlayerRating? rating = await dbContext.PlayerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId, ct);

        if (rating == null)
        {
            return Result<bool>.Fail($"Rating with id {ratingId} not found.");
        }

        // Only the sender can delete their rating
        if (rating.SentById != userId)
        {
            return Result<bool>.Fail("Only the sender can delete their rating.");
        }

        rating.DeletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<double>> GetAverageRatingForUser(string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if user exists
        bool userExists = await dbContext.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            return Result<double>.Fail($"User with id {userId} not found.");
        }

        List<Models.PlayerRating> ratings = await dbContext.PlayerRatings
            .Where(r => r.ReceivedById == userId)
            .ToListAsync(ct);

        if (!ratings.Any())
        {
            return Result<double>.Ok(0.0);
        }

        double averageRating = ratings.Average(r => r.Rating);
        return Result<double>.Ok(Math.Round(averageRating, 2));
    }

    private async Task UpdateUserAverageRatingAsync(string userId)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        float average = await dbContext.PlayerRatings
            .Where(r => r.ReceivedById == userId)
            .AverageAsync(r => (float?)r.Rating) ?? 0f;

        await dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(u => u.Rating, (float)Math.Round(average, 2)));
    }
}