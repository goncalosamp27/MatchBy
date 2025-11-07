using MatchBy.Models;

namespace MatchBy.Services.Matches;

public interface IMatchesService
{
    Task<List<Match>> GetMatches();
    Task<Match?> GetMatchById(string matchId);
    Task<bool> CreateMatch(Match match);
    Task<bool> UpdateMatch(string matchId, Match updatedMatch);
    Task<bool> DeleteMatch(string matchId);
}
