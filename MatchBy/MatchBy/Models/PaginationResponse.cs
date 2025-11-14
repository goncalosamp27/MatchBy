namespace MatchBy.Models;

public class PaginationResponse<T>
{
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public bool NextPageAvailable { get; set; }
    public bool PreviousPageAvailable { get; set; }
    public T Data { get; set; }
}

public class CursorPaginationResponse<T>
{
    public string? NextCursor { get; set; }
    public T Data { get; set; }
}
