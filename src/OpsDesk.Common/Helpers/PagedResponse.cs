namespace OpsDesk.Common.Helpers;

public sealed class PagedResponse<T>
{
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public List<T> Items { get; init; } = new();
}
