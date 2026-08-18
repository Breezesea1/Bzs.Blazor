using Bzs.Blazor;
using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Pages;

public partial class NavigationDrawerShowcase : ComponentBase
{
    private bool _acceptDismissals = true;
    private bool _closeOnBackdropClick = true;
    private bool _closeOnEscape = true;
    private string _dialogStatus = DemoText.NavigationDrawer.DialogNotOpened;
    private DismissalRequest _dismissalRequest;
    private bool _isInteractive;
    private bool _open;
    private BzsNavigationDrawerPosition _position = BzsNavigationDrawerPosition.Start;
    private bool _useInitialFocus = true;
    private BzsNavigationDrawerVariant _variant = BzsNavigationDrawerVariant.Temporary;

    [Inject]
    private IBzsDialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string? InitialFocusSelector => _useInitialFocus
        ? "#navigation-drawer-primary-action"
        : null;

    private string LastRequestLabel => _dismissalRequest switch
    {
        DismissalRequest.Accepted => DemoText.NavigationDrawer.Accepted,
        DismissalRequest.Rejected => DemoText.NavigationDrawer.Rejected,
        _ => DemoText.NavigationDrawer.NoRequest,
    };

    private string PositionLabel => _position == BzsNavigationDrawerPosition.Start
        ? DemoText.NavigationDrawer.Start
        : DemoText.NavigationDrawer.End;

    private string VariantLabel => _variant switch
    {
        BzsNavigationDrawerVariant.Temporary => DemoText.NavigationDrawer.Temporary,
        BzsNavigationDrawerVariant.Persistent => DemoText.NavigationDrawer.Persistent,
        BzsNavigationDrawerVariant.Responsive => DemoText.NavigationDrawer.Responsive,
        _ => throw new ArgumentOutOfRangeException(nameof(_variant), _variant, null),
    };

    private void OpenDrawer()
    {
        _dismissalRequest = DismissalRequest.None;
        _open = true;
    }

    private void CloseDrawer() => _open = false;

    private Task HandleOpenChangedAsync(bool open)
    {
        if (open)
        {
            _open = true;
            return Task.CompletedTask;
        }

        _dismissalRequest = _acceptDismissals
            ? DismissalRequest.Accepted
            : DismissalRequest.Rejected;
        if (_acceptDismissals)
        {
            _open = false;
        }

        return Task.CompletedTask;
    }

    private async Task OpenNestedDialogAsync()
    {
        var result = await DialogService.ShowAsync<OverlayResultDialog, bool>(
            parameters => parameters.Add(
                component => component.Prompt,
                DemoText.NavigationDrawer.NestedDialogPrompt),
            new BzsDialogOptions
            {
                Title = DemoText.NavigationDrawer.NestedDialogTitle,
                AccessibleName = DemoText.NavigationDrawer.NestedDialogTitle,
                CloseOnEscape = true,
                InitialFocusSelector = "#service-dialog-complete",
            });

        _dialogStatus = result.IsCompleted
            ? DemoText.NavigationDrawer.DialogCompleted
            : DemoText.NavigationDrawer.DialogDismissed;
    }

    private void SetPosition(BzsNavigationDrawerPosition position) => _position = position;

    private void SetVariant(BzsNavigationDrawerVariant variant) => _variant = variant;

    private string RouteUrl(string route) => DemoCulture.PreserveCulture(
        new Uri(Navigation.Uri),
        new Uri(Navigation.BaseUri),
        route);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _isInteractive = true;
            StateHasChanged();
        }
    }

    private enum DismissalRequest
    {
        None,
        Accepted,
        Rejected,
    }
}
