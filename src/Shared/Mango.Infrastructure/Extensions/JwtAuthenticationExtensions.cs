using Mango.Core.Auth;
using Mango.Core.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Mango.Infrastructure.Extensions;

public static class JwtAuthenticationExtensions
{
    /// <summary>The API scope every Mango access token must carry.</summary>
    public const string MangoScope = "mango";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Validates access tokens issued by whichever identity provider the AppHost started
        /// (authority from <c>ServiceUrls:IdentityApp</c>) and registers the shared
        /// <see cref="AuthPolicies"/>. The single place every API configures token validation.
        /// </summary>
        public IServiceCollection AddMangoJwtAuthentication(IConfiguration configuration)
        {
            var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>()
                ?? new ServiceUrlsOptions();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = serviceUrls.IdentityApp;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // Unchanged from the per-service setups this replaced. Neither provider
                        // issues a per-API audience yet, so any Mango token is accepted by every
                        // API; tightening this needs audiences configured in both providers.
                        ValidateAudience = false
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(AuthPolicies.ApiScope, policy => policy.RequireMangoScope())
                .AddPolicy(AuthPolicies.AdminOnly, policy => policy
                    .RequireMangoScope()
                    .RequireRole(AuthRoles.Admin));

            return services;
        }
    }

    extension(AuthorizationPolicyBuilder policy)
    {
        /// <summary>
        /// Requires an authenticated caller whose token carries the <c>mango</c> scope.
        /// Duende emits one <c>scope</c> claim per scope; OpenIddict emits a single
        /// space-delimited claim. <c>RequireClaim("scope", "mango")</c> would pass under one and
        /// fail under the other, so the claim values are split before matching.
        /// </summary>
        public AuthorizationPolicyBuilder RequireMangoScope() => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.FindAll("scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(MangoScope));
    }
}
