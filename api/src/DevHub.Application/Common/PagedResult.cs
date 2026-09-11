namespace DevHub.Application.Common;

/// <summary>
/// One page of a list endpoint. The shape matches the API pagination envelope in
/// docs/tech-specs/api-conventions.md, so handlers return it and controllers serialize it as is.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);
}
