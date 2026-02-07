using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Orders.API.Dtos;

namespace Orders.API.Features.Orders;

public class GetOrderById
{
    public class Query : IQuery<OrderDetailDto>
    {
        public Guid OrderId { get; set; }
        public required string UserId { get; set; }

        internal class Handler(OrdersDbContext dbContext) : IRequestHandler<Query, ResultModel<OrderDetailDto>>
        {
            public async Task<ResultModel<OrderDetailDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var order = await dbContext.OrderHeaders
                    .AsNoTracking()
                    .Include(x => x.OrderDetails)
                    .Where(x => x.Id == request.OrderId && x.UserId == request.UserId)
                    .Select(x => new OrderDetailDto
                    {
                        Id = x.Id,
                        OrderTime = x.OrderTime,
                        PickupDate = x.PickupDate,
                        OrderTotal = x.OrderTotal,
                        DiscountTotal = x.DiscountTotal,
                        Status = x.Status.ToString(),
                        PaymentStatus = x.PaymentStatus,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        Email = x.Email,
                        Phone = x.Phone,
                        CouponCode = x.CouponCode,
                        CancelReason = x.CancelReason,
                        Items = x.OrderDetails.Select(d => new OrderItemDto
                        {
                            Id = d.Id,
                            ProductId = d.ProductId,
                            ProductName = d.ProductName ?? "Unknown",
                            Price = d.Price,
                            Count = d.Count
                        }).ToList()
                    })

                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new DataVerificationException("Order not found");

                return ResultModel<OrderDetailDto>.Create(order);
            }
        }
    }
}
