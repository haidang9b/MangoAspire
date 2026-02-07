using Mango.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Orders.API.Dtos;

namespace Orders.API.Features.Orders;

public class GetUserOrders
{
    public class Query : IQuery<PaginatedItems<OrderDto>>
    {
        public required string UserId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        internal class Handler(OrdersDbContext dbContext) : IRequestHandler<Query, ResultModel<PaginatedItems<OrderDto>>>
        {
            public async Task<ResultModel<PaginatedItems<OrderDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = dbContext.OrderHeaders
                    .AsNoTracking()
                    .Where(x => x.UserId == request.UserId)
                    .OrderByDescending(x => x.OrderTime);

                var totalCount = await query.CountAsync(cancellationToken);

                var orders = await query
                    .Paginate(request.PageIndex, request.PageSize)
                    .Select(x => new OrderDto
                    {
                        Id = x.Id,
                        OrderTime = x.OrderTime,
                        OrderTotal = x.OrderTotal,
                        Status = x.Status.ToString(),
                        ItemCount = x.CartTotalItems
                    })
                    .ToListAsync(cancellationToken);

                var result = new PaginatedItems<OrderDto>(
                    request.PageIndex,
                    request.PageSize,
                    totalCount,
                    orders);

                return ResultModel<PaginatedItems<OrderDto>>.Create(result);
            }
        }
    }
}

