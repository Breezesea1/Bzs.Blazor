using System.Globalization;
using Bzs.Blazor;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Pages;

public partial class LandingPage : ComponentBase, IAsyncDisposable
{
    private const string InstallSnippet =
        "dotnet add package Bzs.Blazor --version 0.3.0\nbuilder.Services.AddBzsBlazor();";

    private readonly LandingDemoForm _demoForm = new();
    private LandingPageInterop? _interop;
    private bool _isInteractive;
    private bool _dialogOpen;
    private bool _copied;
    private bool _disposed;

    [Parameter]
    public bool IncludesServerRenderModes { get; set; }

    private bool IsChinese => DemoCulture.IsChinese(CultureInfo.CurrentUICulture.Name);

    private static DemoReleaseEntry Latest => DemoReleaseCatalog.Latest;

    private IReadOnlyList<BzsSelectOption<string>> WorkspaceOptions =>
    [
        new("production", DemoText.Landing.WorkspaceProduction),
        new("staging", DemoText.Landing.WorkspaceStaging),
        new("review", DemoText.Landing.WorkspaceReview),
    ];

    private IReadOnlyList<LandingFeature> Features =>
    [
        new("dependencies", DemoText.Landing.FeatureZeroDependenciesTitle, DemoText.Landing.FeatureZeroDependenciesBody, BzsIcons.Package),
        new("render-modes", DemoText.Landing.FeatureRenderModesTitle, DemoText.Landing.FeatureRenderModesBody, DemoNavIcons.AutoRender),
        new("themes", DemoText.Landing.FeatureThemesTitle, DemoText.Landing.FeatureThemesBody, DemoNavIcons.ThemeFoundation),
        new("accessibility", DemoText.Landing.FeatureAccessibilityTitle, DemoText.Landing.FeatureAccessibilityBody, BzsIcons.Success),
        new("localization", DemoText.Landing.FeatureLocalizationTitle, DemoText.Landing.FeatureLocalizationBody, DemoNavIcons.Languages),
        new("datagrid", DemoText.Landing.FeatureDataGridTitle, DemoText.Landing.FeatureDataGridBody, DemoNavIcons.Layout),
    ];

    private static IReadOnlyList<DemoCatalogDestination> Groups =>
        DemoCatalogDestinations.CapabilityCandidates;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _isInteractive = true;
            StateHasChanged();
        }
    }

    private string DestinationUrl(DemoCatalogDestination destination) =>
        DemoCatalogDestinations.GetHref(Navigation, destination);

    private static string FormatGroupIndex(int index) =>
        (index + 1).ToString("00", CultureInfo.InvariantCulture);

    private static string FormatReleaseDate(DateTimeOffset date) =>
        date.ToString("D", CultureInfo.CurrentUICulture);

    private Task ShowToast()
    {
        Toasts.Show(new BzsToastOptions
        {
            Title = DemoText.Landing.ToastTitle,
            Message = DemoText.Landing.ToastMessage,
            Severity = BzsToastSeverity.Success,
            Duration = TimeSpan.FromSeconds(10),
            AccessibleName = DemoText.Landing.ToastAccessibleName,
        });
        return Task.CompletedTask;
    }

    private Task OpenDialog()
    {
        _dialogOpen = true;
        return Task.CompletedTask;
    }

    private Task SetDialogOpen(bool open)
    {
        _dialogOpen = open;
        return Task.CompletedTask;
    }

    private Task CloseDialog()
    {
        _dialogOpen = false;
        return Task.CompletedTask;
    }

    private async Task CopyInstallAsync()
    {
        if (!_isInteractive || _disposed)
        {
            return;
        }

        _interop ??= new LandingPageInterop(JS);
        var copied = false;
        try
        {
            copied = await _interop.CopyTextAsync(InstallSnippet);
        }
        catch (Exception exception) when (DemoJsInteropExceptions.IsTransientInitializationFailure(exception)
            || exception is ObjectDisposedException)
        {
        }

        if (!copied)
        {
            return;
        }

        _copied = true;
        StateHasChanged();
        await Task.Delay(TimeSpan.FromSeconds(2));
        if (!_disposed)
        {
            _copied = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_interop is not null)
        {
            try
            {
                await _interop.DisposeAsync();
            }
            catch (Exception exception) when (DemoJsInteropExceptions.IsTransientDisposalFailure(exception))
            {
            }
        }
    }

    private sealed class LandingDemoForm
    {
        public string? Name { get; set; }

        public string Workspace { get; set; } = "production";

        public bool Notifications { get; set; } = true;
    }

    private sealed record LandingFeature(string Key, string Title, string Body, BzsIconData Icon);

}
