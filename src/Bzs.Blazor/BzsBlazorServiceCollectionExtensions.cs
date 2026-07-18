using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bzs.Blazor;

/// <summary>
/// Provides service registration for Bzs.Blazor.
/// </summary>
public static class BzsBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Bzs.Blazor services required by the current package version.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The same <paramref name="services" /> instance.</returns>
    /// <remarks>
    /// This method is idempotent. It currently configures the standard .NET
    /// localization services and provides a stable registration seam for later
    /// Bzs.Blazor services.
    /// </remarks>
    public static IServiceCollection AddBzsBlazor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(BzsBlazorServiceRegistrationMarker))
            {
                return services;
            }
        }

        services.AddLocalization();
        services.TryAddScoped<BzsToastService>(_ => new BzsToastService());
        services.TryAddScoped<IBzsToastService>(static provider =>
            provider.GetRequiredService<BzsToastService>());
        services.TryAddScoped<BzsOverlayCoordinator>(_ => new BzsOverlayCoordinator());
        services.TryAddScoped<BzsOverlayHostRegistry>(static provider =>
            new BzsOverlayHostRegistry(provider.GetRequiredService<BzsOverlayCoordinator>()));
        services.TryAddScoped<BzsDialogService>(static provider => new BzsDialogService(
            provider.GetRequiredService<BzsOverlayCoordinator>(),
            provider.GetRequiredService<BzsOverlayHostRegistry>()));
        services.TryAddScoped<IBzsDialogService>(static provider =>
            provider.GetRequiredService<BzsDialogService>());
        services.AddSingleton(new BzsBlazorServiceRegistrationMarker());

        return services;
    }

    private sealed class BzsBlazorServiceRegistrationMarker
    {
    }
}
