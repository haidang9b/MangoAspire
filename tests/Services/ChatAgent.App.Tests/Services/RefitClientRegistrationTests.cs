using ChatAgent.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Shouldly;

namespace ChatAgent.App.Tests.Services;

/// <summary>
/// Guards the Refit registration path. Refit 14 moved the reflection request builder into
/// an opt-in package, so <c>AddRefitClient</c> throws at resolution time unless every
/// method on the interface generates inline and the generated overload is used. That
/// failure only surfaces when the container resolves the client — it compiles cleanly —
/// so it needs a test rather than a build check.
/// </summary>
public class RefitClientRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddRefitGeneratedClient<ICartApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://shoppingcart-api"));

        services.AddRefitGeneratedClient<ICouponsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://coupons-api"));

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Resolve_When_CartApiIsRegistered_Then_ReturnsAGeneratedClient()
    {
        using var provider = BuildProvider();

        var client = provider.GetRequiredService<ICartApi>();

        client.ShouldNotBeNull();
        // The generated implementations live under Refit.Implementation; a reflection-built
        // proxy would not.
        client.GetType().FullName.ShouldStartWith("Refit.Implementation");
    }

    [Fact]
    public void Resolve_When_CouponsApiIsRegistered_Then_ReturnsAGeneratedClient()
    {
        using var provider = BuildProvider();

        var client = provider.GetRequiredService<ICouponsApi>();

        client.ShouldNotBeNull();
        client.GetType().FullName.ShouldStartWith("Refit.Implementation");
    }
}
