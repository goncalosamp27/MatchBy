using MatchBy.Enums;

namespace MatchBy.DTOs.Match;

public record MatchQueryParametersDto
{
    public string? Country { get; init; }
    public string? City { get; init; }
    public int? MaxDistanceInKm { get; init; }
    public double? UserLatitude { get; init; }
    public double? UserLongitude { get; init; }
    public Status? MatchStatus { get; init; }
    public MinimumPlayersAverage MinimumPlayersAverage { get; init; } = MinimumPlayersAverage.All;
    public DateOnly? FromDateUtc { get; init; }
    public DateOnly? ToDateUtc { get; init; }
    public int? FromTimeUtc { get; init; }
    public int? ToTimeUtc { get; init; }
    public SortBy SortBy { get; init; } = SortBy.MatchDateTime;
    public OrderBy OrderBy { get; init; } = OrderBy.Ascending;
    public List<Sports> SportsList { get; init; } = [];
    public string? Q { get; init; }
    public string? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 6;
}

public enum MinimumPlayersAverage
{
    All = 0,
    OneStars = 1,
    TwoStars = 2,
    ThreeStars = 3,
    FourStars = 4,
    FiveStars = 5,
}

public enum SortBy
{
    MatchDateTime = 0,
    PlayersAverage = 1,
    Distance = 2
}

public enum OrderBy
{
    Ascending = 0,
    Descending = 1
}

public enum Status
{
    Cancelled = 0,
    Pendent = 1,
    Completed = 2,
    Confirmed = 3,
    All = 4
}