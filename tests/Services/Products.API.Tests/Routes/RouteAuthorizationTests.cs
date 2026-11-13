using Mango.Core.Auth;
using Mediator.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Routes;

namespace Products.API.Tests.Routes;

/// <summary>
/// Products.API was entirely anonymous. Catalog reads must stay anonymous (browsing before sign-in);
/// everything else — writes and the CDC snapshot trigger — must require <see cref="AuthPolicies.AdminOnly"/>.
/// </summary>
public class RouteAuthorizationTests
{
    private static readonly string[] AnonymousRoutes =
    [
        "GET /api/products/",
        "GET /api/products/{id:guid}",
        "GET /api/catalog-types/",
    ];

    private static List<RouteEndpoint> MapEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        // Lets minimal-API parameter inference resolve ISender as a service rather than a body.
        builder.Services.AddSingleton(Mock.Of<ISender>());

        using var app = builder.Build();
        app.MapProductsApi();
        app.MapCatalogTypesApi();
        app.MapCdcApi();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static string RouteKey(RouteEndpoint endpoint) =>
        $"{endpoint.Metadata.GetRequiredMetadata<IHttpMethodMetadata>().HttpMethods.Single()} {endpoint.RoutePattern.RawText}";

    [Theory]
    [InlineData("GET /api/products/")]
    [InlineData("GET /api/products/{id:guid}")]
    [InlineData("GET /api/catalog-types/")]
    public void MapProductsApi_When_RouteIsCatalogRead_Then_AllowsAnonymous(string route)
    {
        // Arrange
        var endpoints = MapEndpoints();

        // Act
        var endpoint = endpoints.Single(e => RouteKey(e) == route);

        // Assert
        endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData("POST /api/products/")]
    [InlineData("PUT /api/products/")]
    [InlineData("DELETE /api/products/{id:guid}")]
    [InlineData("POST /api/products/cdc-snapshots/")]
    public void MapProductsApi_When_RouteChangesData_Then_RequiresAdminOnly(string route)
    {
        // Arrange
        var endpoints = MapEndpoints();

        // Act
        var endpoint = endpoints.Single(e => RouteKey(e) == route);

        // Assert
        endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldBeNull();
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .ShouldContain(data => data.Policy == AuthPolicies.AdminOnly);
    }

    [Fact]
    public void MapProductsApi_When_AnyRouteIsAnonymous_Then_ItIsAKnownCatalogRead()
    {
        // Arrange
        var endpoints = MapEndpoints();

        // Act
        var anonymous = endpoints
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(RouteKey)
            .Order()
            .ToList();

        // Assert — a write route must never be opted out of AdminOnly.
        anonymous.ShouldBe(AnonymousRoutes.Order().ToList());
    }
}
