using Bzs.Blazor;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class DemoLanguageSwitcher : ComponentBase
{
    private const string EnglishCulture = "en-US";
    private const string ChineseCulture = "zh-Hans";

    private static readonly IReadOnlyList<BzsSelectOption<string>> LanguageOptions =
    [
        new(EnglishCulture, "English"),
        new(ChineseCulture, "中文"),
    ];

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether culture changes reload the static host root before restoring the route.
    /// </summary>
    [Parameter]
    public bool UseStaticHostRootReload { get; set; }

    private bool _isInteractive;

    private string SelectedCulture => DemoCulture.IsEnglish(new Uri(Navigation.Uri))
        ? EnglishCulture
        : ChineseCulture;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _isInteractive = true;
            StateHasChanged();
        }
    }

    private void ChangeCulture(string culture) =>
        Navigation.NavigateTo(GetCultureUrl(culture), forceLoad: true);

    private string GetCultureUrl(string culture)
    {
        var targetUrl = DemoCulture.WithCulture(new Uri(Navigation.Uri), culture);
        if (!UseStaticHostRootReload)
        {
            return targetUrl;
        }

        var baseUri = new Uri(Navigation.BaseUri);
        var reloadUrl = DemoCulture.WithCulture(baseUri, culture);
        var targetUri = new Uri(baseUri, targetUrl);
        var route = Navigation.ToBaseRelativePath(targetUri.ToString());
        return $"{reloadUrl}&route={Uri.EscapeDataString(route)}";
    }
}
