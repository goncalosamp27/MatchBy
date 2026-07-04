namespace MatchBy.Models;

public class PaginationResponse<T>
{
    public required int Page { get; set; }
    public required int TotalCount { get; set; }
    public required int PageSize { get; set; }
    public bool NextPageAvailable => Page * PageSize < TotalCount;
    public bool PreviousPageAvailable => Page > 0;
    public required T Data { get; set; }
}

public class CursorPaginationResponse<T>
{
    public string? NextCursor { get; set; }
    public T Data { get; set; }
}
