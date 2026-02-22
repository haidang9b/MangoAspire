using Mediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Mediator.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the custom Mediator and scans the provided assemblies
    /// for all IRequestHandler and INotificationHandler implementations.
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.TryAddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        RegisterHandlers(services, assemblies);
        RegisterNotificationHandlers(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerInterfaceType = typeof(IRequestHandler<,>);

        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces(), (type, iface) => (type, iface))
            .Where(x => x.iface.IsGenericType &&
                        x.iface.GetGenericTypeDefinition() == handlerInterfaceType);

        foreach (var (handlerType, serviceType) in handlerTypes)
        {
            services.TryAddTransient(serviceType, handlerType);
        }
    }

    private static void RegisterNotificationHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerInterfaceType = typeof(INotificationHandler<>);

        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces(), (type, iface) => (type, iface))
            .Where(x => x.iface.IsGenericType &&
                        x.iface.GetGenericTypeDefinition() == handlerInterfaceType);

        // Notification handlers use Add (not TryAdd) to allow multiple handlers per notification
        foreach (var (handlerType, serviceType) in handlerTypes)
        {
            services.AddTransient(serviceType, handlerType);
        }
    }
}
