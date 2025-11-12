using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services;

public class MatchesService(ApplicationDbContext applicationDbContext) : IMatchesService
{
    public async Task<List<Match>> GetMatches()
    {
        List<Match> matches = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .ToListAsync();
        return matches;
    }

    public async Task<Match?> GetMatchById(string matchId)
    {
        Match? match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        return match;
    }

    public async Task<bool> CreateMatch(Match match)
    {
        match.Id = $"match_{Guid.CreateVersion7()}";
        match.MatchDateTimeUtc = DateTime.SpecifyKind(match.MatchDateTimeUtc, DateTimeKind.Utc);
        match.CreatedAtUtc = DateTime.UtcNow;
        await applicationDbContext.Matches.AddAsync(match);
        await applicationDbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> UpdateMatch(string matchId, Match updatedMatch)
    {
        Match? match = await applicationDbContext
            .Matches
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match is null)
        {
            return false;
        }

        //propertys mapper
        match.Description = updatedMatch.Description;
        match.Location = updatedMatch.Location;
        match.MatchDateTimeUtc = DateTime.SpecifyKind(updatedMatch.MatchDateTimeUtc, DateTimeKind.Utc);
        match.MatchDateTimeUtc = updatedMatch.MatchDateTimeUtc;
        match.minPlayers = updatedMatch.minPlayers;
        match.maxPlayers = updatedMatch.maxPlayers;
        match.Sport = updatedMatch.Sport;
        match.Status = updatedMatch.Status;
        match.CreatorId = updatedMatch.CreatorId;
        match.UpdatedAtUtc = updatedMatch.UpdatedAtUtc;

        await applicationDbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMatch(string matchId)
    {
        await applicationDbContext.Matches.Where(m => m.Id.Equals(matchId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.DeletedAtUtc, DateTime.UtcNow));
        return true;
    }
    
    public async Task<bool> JoinMatch(string matchId, string userId)
    {
        Match? match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == matchId);
        
        if (match is null)
        {
            return false;
        }
        
        if (match.Participants.Count >= match.maxPlayers)
        {
            return false;
        }  
        
        if (match.Participants.Any(u => u.Id == userId))
        {
            return false;
        }
        ApplicationUser? user = await applicationDbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) 
        {
            return false;
        }
        match.Participants.Add(user);
        await applicationDbContext.SaveChangesAsync();
        return true;
    }
}
