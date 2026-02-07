namespace Mango.Core.Pagination;

public static class PaginationExtensions
{
    /// <summary>
    /// Applies Skip and Take to an IQueryable based on page index and size.
    /// Page index is 1-based (first page = 1).
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int? pageIndex, int? pageSize)
    {
        var pageIndexValue = pageIndex ?? 1;
        if (pageIndex == null || pageIndex < 1)
            pageIndexValue = 1;

        var pageSizeValue = pageSize ?? 10;
        if (pageSize == null || pageSize < 1)
            pageSizeValue = 10;

        return query.Skip((pageIndexValue - 1) * pageSizeValue).Take(pageSizeValue);
    }
}
