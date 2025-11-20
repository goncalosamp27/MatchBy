using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.PlayerRating;

public sealed class PlayerRatingService : IPlayerRatingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PlayerRatingService> _logger;

    public PlayerRatingService(ApplicationDbContext db, ILogger<PlayerRatingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PlayerRatingDto>> CreatePlayerRatingAsync(CreatePlayerRatingDto request)
    {
        if (request.SentById == request.ReceivedById)
        {
            return Result<PlayerRatingDto>.Fail("You cannot rate yourself.");
        }

        try
        {
            Match match = await _db.Matches
                .Include(m => m.Participants)
                .SingleOrDefaultAsync(m => m.Id == request.MatchId);

            if (match is null)
            {
                return Result<PlayerRatingDto>.Fail("Match not found.");
            }

            var participantIds = match.Participants.Select(p => p.Id).ToHashSet();

            if (!participantIds.Contains(request.SentById))
            {
                return Result<PlayerRatingDto>.Fail("You must be a participant of the match to rate players.");
            }

            if (!participantIds.Contains(request.ReceivedById))
            {
                return Result<PlayerRatingDto>.Fail("The player you are trying to rate is not a participant of this match.");
            }

            Models.PlayerRating? existing = await _db.PlayerRatings
                .SingleOrDefaultAsync(r =>
                    r.SentById == request.SentById &&
                    r.ReceivedById == request.ReceivedById &&
                    r.MatchId == request.MatchId);

            if (existing is not null)
            {
                existing.Rating = request.Rating;
                existing.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
            }
            else
            {
                existing = request.ToEntity();
                _db.PlayerRatings.Add(existing);
            }

            await _db.SaveChangesAsync();

            // Recalculate and update the user's average rating
            await UpdateUserAverageRatingAsync(request.ReceivedById);

            _logger.LogInformation("Rating from {SenterId} to {ReceiverId} for match {MatchId} saved successfully",
            request.SentById, request.ReceivedById, request.MatchId);

            return Result<PlayerRatingDto>.Ok(existing.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating player rating");
            return Result<PlayerRatingDto>.Fail("An error occurred while rating the player.");
        }
    }

    private async Task UpdateUserAverageRatingAsync(string userId)
    {
        float average = await _db.PlayerRatings
                            .Where(r => r.ReceivedById == userId)
                            .AverageAsync(r => (float?)r.Rating) ??
                        0f;

        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(u => u.Rating, (float)Math.Round(average, 2)));
    }


}
