using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.Tests;

public sealed class DemoCatalogDestinationTests
{
    [Fact]
    public void DestinationIdentityHasStableUniqueIdsAndRoutes()
    {
        var destinations = DemoCatalogDestinations.Catalog
            .Concat(DemoCatalogDestinations.Project)
            .Concat(DemoCatalogDestinations.Runtimes)
            .ToArray();

        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Id).Distinct().Count());
        Assert.Equal(destinations.Length, destinations.Select(destination => destination.Route).Distinct().Count());
        Assert.DoesNotContain(destinations, destination => string.IsNullOrWhiteSpace(destination.Id));
        Assert.DoesNotContain(destinations, destination => string.IsNullOrWhiteSpace(destination.Route));
    }

    [Fact]
    public void RuntimeIdentityCarriesHostAvailabilityWithoutCulturePresentation()
    {
        Assert.False(DemoCatalogDestinations.StaticSsr.IsAvailable(
            DemoCatalogHostCapabilities.SharedCatalog
            | DemoCatalogHostCapabilities.StandaloneRuntime));
        Assert.True(DemoCatalogDestinations.InteractiveWebAssembly.IsAvailable(
            DemoCatalogHostCapabilities.SharedCatalog
            | DemoCatalogHostCapabilities.StandaloneRuntime));
        Assert.True(DemoCatalogDestinations.InteractiveServer.IsAvailable(
            DemoCatalogHostCapabilities.SharedCatalog
            | DemoCatalogHostCapabilities.FullRenderModes));
    }
}
