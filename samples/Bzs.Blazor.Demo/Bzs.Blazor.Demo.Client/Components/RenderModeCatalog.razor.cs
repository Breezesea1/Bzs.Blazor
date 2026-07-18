using System.ComponentModel.DataAnnotations;
using Bzs.Blazor.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Bzs.Blazor.Demo.Client.Components;

public partial class RenderModeCatalog
{
    private static readonly IReadOnlyList<BzsSelectOption<string>> WorkspaceOptions =
    [
        new("production", "Production"),
        new("lighting", "Lighting"),
        new("review", "Review"),
    ];

    private readonly CatalogForm _form = new()
    {
        WorkItem = "Shot review",
        Notes = "Confirm the final camera notes before the next review.",
        NotifyOwners = true,
        ReviewerCount = 3,
        ReviewDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
        Workspace = "production",
    };

    private readonly EditContext _editContext;
    private BzsThemeMode _mode = BzsThemeMode.Light;
    private BzsDensity _density = BzsDensity.Compact;
    private bool _isRuntimeReady;
    private int _interactionCount;
    private string _activeTab = "summary";
    private bool _isControlledDialogOpen;
    private bool _isDrawerOpen;
    private string _formStatus = "The work item is ready for review.";
    private string _controlledDialogStatus = "Controlled dialog is closed.";
    private string _drawerStatus = "Drawer is closed.";
    private string _serviceDialogStatus = "No service dialog result yet.";

    public RenderModeCatalog()
    {
        _editContext = new EditContext(_form);
    }

    [Inject]
    private IBzsDialogService Dialogs { get; set; } = default!;

    [Inject]
    private IBzsToastService Toasts { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string RuntimeName { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string RuntimeStatus { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string RuntimeTestId { get; set; } = string.Empty;

    [Parameter]
    public bool IsInteractive { get; set; }

    private bool CanInteract => IsInteractive && _isRuntimeReady;

    private string TitleId => $"{RuntimeTestId}-title";
    private string FoundationTitleId => $"{RuntimeTestId}-foundation-title";
    private string FormTitleId => $"{RuntimeTestId}-form-title";
    private string FeedbackTitleId => $"{RuntimeTestId}-feedback-title";
    private string TabsTitleId => $"{RuntimeTestId}-tabs-title";
    private string OverlaysTitleId => $"{RuntimeTestId}-overlays-title";
    private string FormName => $"{RuntimeTestId}-form";
    private string ThemeTestId => $"{RuntimeTestId}-theme";
    private string RuntimeStatusTestId => $"{RuntimeTestId}-runtime-status";
    private string ReadinessTestId => $"{RuntimeTestId}-runtime-readiness";
    private string CounterTestId => $"{RuntimeTestId}-counter";
    private string FormTestId => $"{RuntimeTestId}-form";
    private string WorkItemInputId => $"{RuntimeTestId}-work-item";
    private string NotesInputId => $"{RuntimeTestId}-notes";
    private string NotifyInputId => $"{RuntimeTestId}-notify-owners";
    private string ReviewerInputId => $"{RuntimeTestId}-reviewer-count";
    private string ReviewDateInputId => $"{RuntimeTestId}-review-date";
    private string WorkspaceInputId => $"{RuntimeTestId}-workspace";
    private string WorkItemTestId => $"{RuntimeTestId}-work-item";
    private string NotesTestId => $"{RuntimeTestId}-notes";
    private string NotifyTestId => $"{RuntimeTestId}-notify-owners";
    private string ReviewerTestId => $"{RuntimeTestId}-reviewer-count";
    private string ReviewDateTestId => $"{RuntimeTestId}-review-date";
    private string WorkspaceTestId => $"{RuntimeTestId}-workspace";
    private string SaveTestId => $"{RuntimeTestId}-save";
    private string FormStatusTestId => $"{RuntimeTestId}-form-status";
    private string TabsTestId => $"{RuntimeTestId}-tabs";
    private string TabsStatusTestId => $"{RuntimeTestId}-tabs-status";
    private string OpenDialogTestId => $"{RuntimeTestId}-open-controlled-dialog";
    private string OpenDrawerTestId => $"{RuntimeTestId}-open-drawer";
    private string OpenServiceDialogTestId => $"{RuntimeTestId}-open-service-dialog";
    private string ShowToastTestId => $"{RuntimeTestId}-show-toast";
    private string ControlledDialogStatusTestId => $"{RuntimeTestId}-controlled-dialog-status";
    private string DrawerStatusTestId => $"{RuntimeTestId}-drawer-status";
    private string ServiceDialogStatusTestId => $"{RuntimeTestId}-service-dialog-status";
    private string ControlledDialogTestId => $"{RuntimeTestId}-controlled-dialog";
    private string CompleteControlledDialogTestId => $"{RuntimeTestId}-complete-controlled-dialog";
    private string DrawerTestId => $"{RuntimeTestId}-drawer";
    private string CloseDrawerTestId => $"{RuntimeTestId}-close-drawer";
    private string OverlayHostTestId => $"{RuntimeTestId}-overlay-host";

    protected override void OnParametersSet()
    {
        ValidateRequiredParameter(RuntimeName, nameof(RuntimeName));
        ValidateRequiredParameter(RuntimeStatus, nameof(RuntimeStatus));
        ValidateRequiredParameter(RuntimeTestId, nameof(RuntimeTestId));
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && IsInteractive)
        {
            _isRuntimeReady = true;
            StateHasChanged();
        }
    }

    private void Increment() => _interactionCount++;

    private Task SetModeAsync(BzsThemeMode mode)
    {
        _mode = mode;
        return Task.CompletedTask;
    }

    private Task SetDensityAsync(BzsDensity density)
    {
        _density = density;
        return Task.CompletedTask;
    }

    private void SaveForm()
    {
        _formStatus = CanInteract
            ? $"Saved {_form.WorkItem}."
            : "Static form markup remains read-only.";
    }

    private Task SetActiveTabAsync(string value)
    {
        _activeTab = value;
        return Task.CompletedTask;
    }

    private Task OpenControlledDialogAsync()
    {
        if (!CanInteract)
        {
            return Task.CompletedTask;
        }

        _isControlledDialogOpen = true;
        _controlledDialogStatus = "Controlled dialog is open.";
        return Task.CompletedTask;
    }

    private Task SetControlledDialogOpenAsync(bool open)
    {
        _isControlledDialogOpen = open;
        return Task.CompletedTask;
    }

    private Task CompleteControlledDialogAsync()
    {
        _isControlledDialogOpen = false;
        _controlledDialogStatus = "Controlled dialog completed.";
        return Task.CompletedTask;
    }

    private Task HandleControlledDialogDismissedAsync(BzsDialogDismissReason reason)
    {
        _controlledDialogStatus = $"Controlled dialog dismissed by {reason}.";
        return Task.CompletedTask;
    }

    private Task OpenDrawerAsync()
    {
        if (!CanInteract)
        {
            return Task.CompletedTask;
        }

        _isDrawerOpen = true;
        _drawerStatus = "Drawer is open.";
        return Task.CompletedTask;
    }

    private Task SetDrawerOpenAsync(bool open)
    {
        _isDrawerOpen = open;
        return Task.CompletedTask;
    }

    private Task CloseDrawerAsync()
    {
        _isDrawerOpen = false;
        _drawerStatus = "Drawer is closed.";
        return Task.CompletedTask;
    }

    private Task HandleDrawerDismissedAsync(BzsDialogDismissReason reason)
    {
        _drawerStatus = $"Drawer dismissed by {reason}.";
        return Task.CompletedTask;
    }

    private async Task OpenServiceDialogAsync()
    {
        if (!CanInteract)
        {
            return;
        }

        var result = await Dialogs.ShowAsync<OverlayResultDialog, bool>(
            parameters => parameters.Add(
                component => component.Prompt,
                $"Complete the {RuntimeName} catalog workflow?"),
            new BzsDialogOptions
            {
                Title = "Catalog service dialog",
                AccessibleName = "Catalog service dialog",
                Modal = true,
                CloseOnEscape = true,
                CloseOnBackdropClick = true,
                ShowCloseButton = false,
                InitialFocusSelector = "#service-dialog-complete",
            });

        _serviceDialogStatus = result.IsCompleted
            ? result.Value == true ? "Completed: true" : "Completed: false"
            : $"Result: {result.Kind}";
    }

    private void ShowToast()
    {
        if (!CanInteract)
        {
            return;
        }

        Toasts.Show(new BzsToastOptions
        {
            Title = "Catalog toast",
            Message = $"{RuntimeName} rendered this notification through its overlay host.",
            Severity = BzsToastSeverity.Success,
            Duration = Timeout.InfiniteTimeSpan,
            AccessibleName = "Catalog overlay toast",
        });
    }

    private static void ValidateRequiredParameter(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"RenderModeCatalog requires {parameterName}.");
        }
    }

    private sealed class CatalogForm
    {
        [Required(ErrorMessage = "Enter a work item.")]
        public string? WorkItem { get; set; }

        public string? Notes { get; set; }

        public bool NotifyOwners { get; set; }

        [Range(1, 20, ErrorMessage = "Choose between 1 and 20 reviewers.")]
        public int ReviewerCount { get; set; }

        public DateOnly ReviewDate { get; set; }

        [Required(ErrorMessage = "Choose a workspace.")]
        public string Workspace { get; set; } = string.Empty;
    }
}
