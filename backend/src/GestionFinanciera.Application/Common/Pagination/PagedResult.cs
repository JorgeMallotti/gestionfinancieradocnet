namespace GestionFinanciera.Application.Common.Pagination;

/// <summary>
/// Standard paginated envelope. Every list endpoint returns this so the frontend
/// can render pagination without refetching whole collections.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page * PageSize < TotalCount;

    public bool HasPreviousPage => Page > 1;
}

/// <summary>Query parameters accepted by list endpoints.</summary>
public sealed record PaginationQuery(int Page = 1, int PageSize = 20);
