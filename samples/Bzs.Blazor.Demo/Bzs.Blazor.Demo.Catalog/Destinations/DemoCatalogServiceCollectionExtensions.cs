using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bzs.Blazor.Demo.Client;

/// <summary>Registers the services shared by every Demo Catalog host.</summary>
public static class DemoCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddDemoCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<DemoDestinationLinks>();
        return services;
    }
}
