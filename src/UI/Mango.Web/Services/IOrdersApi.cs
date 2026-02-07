using Mango.RestApis;
using Mango.Web.Models;
using Refit;

namespace Mango.Web.Services;

public interface IOrdersApi
{
    [Get("/api/orders")]
    Task<ResultDto<PaginatedItemsDto<OrderDto>>> GetUserOrdersAsync(int pageIndex = 0, int pageSize = 10);

    [Get("/api/orders/{orderId}")]
    Task<ResultDto<OrderDetailDto>> GetOrderByIdAsync(Guid orderId);
}
