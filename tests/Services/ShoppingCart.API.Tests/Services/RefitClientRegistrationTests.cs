using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace ShoppingCart.API.Tests.Services;

/// <summary>
/// Guards the Refit registration path. Refit 14 moved the reflection request builder into
/// an opt-in package, so a client only resolves when Refit's source generator has run over
/// the interface (which needs a direct Refit reference in the declaring project) and the
/// generated overload is used. The failure surfaces only when the container resolves the
/// client — it compiles cleanly — so it needs a test rather than a build check.
/// </summary>
public class RefitClientRegistrationTests
{
    [Fact]
    public void Resolve_When_CouponsApiIsRegistered_Then_ReturnsAGeneratedClient()
    {
        var services = new ServiceCollection();
        services.AddRefitGeneratedClient<ICouponsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://coupons-api"));
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ICouponsApi>();

        client.ShouldNotBeNull();
        // The generated implementations live under Refit.Implementation; a reflection-built
        // proxy would not.
        client.GetType().FullName.ShouldStartWith("Refit.Implementation");
    }
}
