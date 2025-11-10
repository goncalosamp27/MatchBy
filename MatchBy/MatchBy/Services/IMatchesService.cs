using MatchBy.Models;

namespace MatchBy.Services;

public interface IMatchesService
{
    Task<List<Match>> GetMatches();
    Task<List<Match>> GetCompletedMatches(string? userId);
    Task<Match?> GetMatchById(string matchId);
    Task<bool> CreateMatch(Match match);
    Task<bool> UpdateMatch(string matchId, Match updatedMatch);
    Task<bool> DeleteMatch(string matchId);
    Task<List<Match>> GetMatchesForUser(string userId);
    Task<List<Match>> GetMatchesExceptUser(string userId);
}

