namespace Mango.Core.Pagination;

public static class PaginationExtensions
{
    /// <summary>
    /// Applies Skip and Take to an IQueryable based on page index and size.
    /// Page index is 1-based (first page = 1).
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int? pageIndex, int? pageSize)
    {
        var pageIndexValue = Math.Max(1, pageIndex ?? 1);

        var pageSizeValue = Math.Max(10, pageSize ?? 10);

        return query.Skip(Math.Max(0, (pageIndexValue - 1)) * pageSizeValue).Take(pageSizeValue);
    }
}
