using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// Projects a <see cref="DemoCatalogDestination" /> into the address a visitor follows,
/// carrying the host base path and the visitor's current culture.
/// </summary>
public sealed class DemoDestinationLinks
{
    private readonly NavigationManager _navigation;

    public DemoDestinationLinks(NavigationManager navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        _navigation = navigation;
    }

    /// <summary>Gets the link to a destination, optionally with a query and a fragment.</summary>
    public string To(DemoCatalogDestination destination, string? query = null, string? fragment = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var relativePath = destination.Route;
        if (!string.IsNullOrWhiteSpace(query))
        {
            relativePath = $"{relativePath}?{query.TrimStart('?')}";
        }

        if (!string.IsNullOrWhiteSpace(fragment))
        {
            relativePath = $"{relativePath}#{fragment.TrimStart('#')}";
        }

        return DemoCulture.PreserveCulture(
            new Uri(_navigation.Uri),
            new Uri(_navigation.BaseUri),
            relativePath);
    }
}
