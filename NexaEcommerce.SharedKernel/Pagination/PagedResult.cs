namespace NexaEcommerce.SharedKernel.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrev)
{
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);

        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);
        totalItems = Math.Max(0, totalItems);

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems / (double)pageSize);

        return new PagedResult<T>(
            items,
            page,
            pageSize,
            totalItems,
            totalPages,
            page < totalPages,
            page > 1 && totalPages > 0);
    }
}