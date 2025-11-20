using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.PlayerRatings;

public class PlayerRatingService(
    ApplicationDbContext applicationDbContext,
    IValidator<CreatePlayerRatingDto> createRatingValidator,
    IValidator<UpdatePlayerRatingDto> updateRatingValidator) : IPlayerRatingService
{
    public async Task<Result<PlayerRatingDto>> GetRatingById(string ratingId, CancellationToken ct = default)
    {
        PlayerRating? rating = await applicationDbContext
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
        // Check if the match exists
        bool matchExists = await applicationDbContext.Matches.AnyAsync(m => m.Id == matchId, ct);
        if (!matchExists)
        {
            return Result<PaginationResponse<List<PlayerRatingDto>>>.Fail($"Match with id {matchId} not found.");
        }

        IQueryable<PlayerRating> query = applicationDbContext
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

        List<PlayerRating> ratings = await query
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
        IQueryable<PlayerRating> query = applicationDbContext
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

        List<PlayerRating> ratings = await query
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
        IQueryable<PlayerRating> query = applicationDbContext
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

        List<PlayerRating> ratings = await query
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

    public async Task<Result<PlayerRatingDto>> CreateRating(CreatePlayerRatingDto createDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await createRatingValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        // Check if sender exists
        bool senderExists = await applicationDbContext.Users.AnyAsync(u => u.Id == createDto.SentById, ct);
        if (!senderExists)
        {
            return Result<PlayerRatingDto>.Fail($"Sender with id {createDto.SentById} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await applicationDbContext.Users.AnyAsync(u => u.Id == createDto.ReceivedById, ct);
        if (!receiverExists)
        {
            return Result<PlayerRatingDto>.Fail($"Receiver with id {createDto.ReceivedById} not found.");
        }

        // Check if match exists
        Match? match = await applicationDbContext.Matches
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == createDto.MatchId, ct);

        if (match == null)
        {
            return Result<PlayerRatingDto>.Fail($"Match with id {createDto.MatchId} not found.");
        }

        // Check if both users participated in the match
        bool senderParticipated = match.Participants.Any(p => p.Id == createDto.SentById) || match.CreatorId == createDto.SentById;
        bool receiverParticipated = match.Participants.Any(p => p.Id == createDto.ReceivedById) || match.CreatorId == createDto.ReceivedById;

        if (!senderParticipated)
        {
            return Result<PlayerRatingDto>.Fail("The sender must have participated in the match to give a rating.");
        }

        if (!receiverParticipated)
        {
            return Result<PlayerRatingDto>.Fail("The receiver must have participated in the match to receive a rating.");
        }

        // Check if rating already exists
        bool existingRating = await applicationDbContext.PlayerRatings
            .AnyAsync(r => r.SentById == createDto.SentById 
                        && r.ReceivedById == createDto.ReceivedById 
                        && r.MatchId == createDto.MatchId, ct);

        if (existingRating)
        {
            return Result<PlayerRatingDto>.Fail("A rating already exists for this user in this match.");
        }

        PlayerRating rating = createDto.ToEntity();
        await applicationDbContext.PlayerRatings.AddAsync(rating, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetRatingById(rating.Id, ct);
    }

    public async Task<Result<PlayerRatingDto>> UpdateRating(UpdatePlayerRatingDto updateDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await updateRatingValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<PlayerRatingDto>.Fail(validationResult.ToString());
        }

        PlayerRating? rating = await applicationDbContext.PlayerRatings
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
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetRatingById(rating.Id, ct);
    }

    public async Task<Result<bool>> DeleteRating(string ratingId, string userId, CancellationToken ct = default)
    {
        PlayerRating? rating = await applicationDbContext.PlayerRatings
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
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<double>> GetAverageRatingForUser(string userId, CancellationToken ct = default)
    {
        // Check if user exists
        bool userExists = await applicationDbContext.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            return Result<double>.Fail($"User with id {userId} not found.");
        }

        List<PlayerRating> ratings = await applicationDbContext.PlayerRatings
            .Where(r => r.ReceivedById == userId)
            .ToListAsync(ct);

        if (!ratings.Any())
        {
            return Result<double>.Ok(0.0);
        }

        double averageRating = ratings.Average(r => r.Rating);
        return Result<double>.Ok(Math.Round(averageRating, 2));
    }
}




