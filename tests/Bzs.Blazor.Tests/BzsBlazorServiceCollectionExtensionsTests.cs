using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor.Tests;

public sealed class BzsBlazorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBzsBlazorRegistersLocalizationOnlyOnce()
    {
        var services = new ServiceCollection();

        var firstResult = services.AddBzsBlazor();
        var registrationCount = services.Count;
        var factoryRegistrationCount = services.Count(
            descriptor => descriptor.ServiceType == typeof(IStringLocalizerFactory));
        var localizerRegistrationCount = services.Count(
            descriptor => descriptor.ServiceType == typeof(IStringLocalizer<>));

        var secondResult = services.AddBzsBlazor();

        Assert.Same(services, firstResult);
        Assert.Same(services, secondResult);
        Assert.Equal(registrationCount, services.Count);
        Assert.Equal(1, factoryRegistrationCount);
        Assert.Equal(1, localizerRegistrationCount);
        Assert.Equal(factoryRegistrationCount, services.Count(
            descriptor => descriptor.ServiceType == typeof(IStringLocalizerFactory)));
        Assert.Equal(localizerRegistrationCount, services.Count(
            descriptor => descriptor.ServiceType == typeof(IStringLocalizer<>)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(BzsToastService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IBzsToastService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(BzsOverlayCoordinator)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(BzsOverlayHostRegistry)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(BzsDialogService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IBzsDialogService)));
    }

    [Fact]
    public async Task AddBzsBlazorMapsOneScopedCoreInstanceToEachPublicServiceInterface()
    {
        var services = new ServiceCollection();
        services.AddBzsBlazor();
        await using var provider = services.BuildServiceProvider();
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();

        var first = firstScope.ServiceProvider;
        var second = secondScope.ServiceProvider;
        var firstDialogService = first.GetRequiredService<BzsDialogService>();
        var firstToastService = first.GetRequiredService<BzsToastService>();
        var firstCoordinator = first.GetRequiredService<BzsOverlayCoordinator>();
        var firstHostRegistry = first.GetRequiredService<BzsOverlayHostRegistry>();

        Assert.Same(firstDialogService, first.GetRequiredService<IBzsDialogService>());
        Assert.Same(firstToastService, first.GetRequiredService<IBzsToastService>());
        Assert.Same(firstCoordinator, first.GetRequiredService<BzsOverlayCoordinator>());
        Assert.Same(firstHostRegistry, first.GetRequiredService<BzsOverlayHostRegistry>());
        Assert.NotSame(firstDialogService, second.GetRequiredService<BzsDialogService>());
        Assert.NotSame(firstToastService, second.GetRequiredService<BzsToastService>());
        Assert.NotSame(firstCoordinator, second.GetRequiredService<BzsOverlayCoordinator>());
        Assert.NotSame(firstHostRegistry, second.GetRequiredService<BzsOverlayHostRegistry>());
    }

    [Fact]
    public void AddBzsBlazorRejectsANullServiceCollection()
    {
        Assert.Throws<ArgumentNullException>(
            () => BzsBlazorServiceCollectionExtensions.AddBzsBlazor(null!));
    }
}
