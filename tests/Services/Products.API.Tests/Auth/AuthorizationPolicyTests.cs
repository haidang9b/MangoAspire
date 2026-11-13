using Mango.Core.Auth;
using Mango.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Products.API.Tests.Auth;

/// <summary>
/// The shared policies registered by <c>AddMangoJwtAuthentication()</c>. Claims are shaped as
/// JwtBearer produces them after inbound mapping: <c>role</c> becomes <see cref="ClaimTypes.Role"/>,
/// <c>scope</c> is left as-is.
/// </summary>
public class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationPolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMangoJwtAuthentication(new ConfigurationBuilder().Build());
        _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal SignedIn(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    [Fact]
    public async Task AuthorizeAsync_When_ScopeIsOneClaimPerScope_Then_ApiScopeSucceeds()
    {
        // Arrange — Duende emits one scope claim per granted scope.
        var user = SignedIn(new Claim("scope", "openid"), new Claim("scope", "mango"));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.ApiScope);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_When_ScopeIsSpaceDelimited_Then_ApiScopeSucceeds()
    {
        // Arrange — OpenIddict emits a single space-delimited scope claim.
        var user = SignedIn(new Claim("scope", "openid profile mango"));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.ApiScope);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_When_MangoScopeIsMissing_Then_ApiScopeFails()
    {
        // Arrange — e.g. a client-credentials token for another client's scopes.
        var user = SignedIn(new Claim("scope", "read write profile"));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.ApiScope);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_When_ScopeOnlyContainsMangoAsSubstring_Then_ApiScopeFails()
    {
        // Arrange
        var user = SignedIn(new Claim("scope", "mango-admin notmango"));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.ApiScope);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_When_CallerIsNotAuthenticated_Then_ApiScopeFails()
    {
        // Arrange — claims present, but no authentication type: nothing validated them.
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "mango")]));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.ApiScope);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_When_AdminHasMangoScope_Then_AdminOnlySucceeds()
    {
        // Arrange
        var user = SignedIn(new Claim("scope", "mango"), new Claim(ClaimTypes.Role, AuthRoles.Admin));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.AdminOnly);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_When_CustomerHasMangoScope_Then_AdminOnlyFails()
    {
        // Arrange
        var user = SignedIn(new Claim("scope", "mango"), new Claim(ClaimTypes.Role, AuthRoles.Customer));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.AdminOnly);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_When_AdminLacksMangoScope_Then_AdminOnlyFails()
    {
        // Arrange
        var user = SignedIn(new Claim("scope", "openid profile"), new Claim(ClaimTypes.Role, AuthRoles.Admin));

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, null, AuthPolicies.AdminOnly);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }
}
