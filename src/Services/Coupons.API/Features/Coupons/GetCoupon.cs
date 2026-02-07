using Coupons.API.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Coupons.API.Features.Coupons;

public class GetCoupon
{
    public record Query : IQuery<CouponDto>
    {
        public required string Code { get; init; }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.Code).NotEmpty();
            }
        }

        internal class Handler(CouponDbContext dbContext) : IRequestHandler<Query, ResultModel<CouponDto>>
        {
            public async Task<ResultModel<CouponDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var coupon = await dbContext.Coupons
                    .Where(x => x.Code == request.Code)
                    .Select(coupon => new CouponDto
                    {
                        Id = coupon.Id,
                        Code = coupon.Code,
                        DiscountAmount = coupon.DiscountAmount
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return ResultModel<CouponDto>.Create(coupon);
            }
        }
    }
}
