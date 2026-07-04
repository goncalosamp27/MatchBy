
namespace MatchBy.DTOs.Team;

public record TeamQueryParametersDto
{
    public string UserId { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required string Query { get; init; }
    public required SortBy SortBy { get; init; }
    public required OrderBy OrderBy { get; init; }
    public required Privacy Privacy { get; init; }
}
public enum SortBy
{
    Name = 0,
    Description = 1,
    CreatedAt = 2,
    MembersCount = 3,
}

public enum OrderBy
{
    Ascending = 0,
    Descending = 1,
}

public enum Privacy
{
    All = 0,
    Public = 1,
    Private = 2,
}
