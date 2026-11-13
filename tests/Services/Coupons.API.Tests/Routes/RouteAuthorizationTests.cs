using Coupons.API.Routes;
using Mango.Core.Auth;
using Mediator.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Coupons.API.Tests.Routes;

/// <summary>
/// Coupons.API was anonymous, so coupon codes could be enumerated and the user token that
/// ShoppingCart and ChatAgent forward was ignored.
/// </summary>
public class RouteAuthorizationTests
{
    [Fact]
    public void MapCouponEndpoints_When_GettingCouponByCode_Then_RequiresApiScope()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        // Lets minimal-API parameter inference resolve ISender as a service rather than a body.
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using var app = builder.Build();
        app.MapCouponEndpoints();

        // Act
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/api/coupons/{code}");

        // Assert
        endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldBeNull();
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .ShouldContain(data => data.Policy == AuthPolicies.ApiScope);
    }
}
