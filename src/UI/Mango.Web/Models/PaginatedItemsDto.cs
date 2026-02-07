namespace Mango.Web.Models;

public class PaginatedItemsDto<T> where T : class
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public long Count { get; set; }
    public IEnumerable<T> Data { get; set; } = [];
    
    public int TotalPages => (int)Math.Ceiling(Count / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}
