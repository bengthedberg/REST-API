namespace Movies.Contracts.Responses;

public class PageResponse<T>
{
    public required IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public bool HasNextPage => TotalCount > (Page * PageSize);
}