using Microsoft.AspNetCore.Components.Web;
using Bzs.Blazor.Localization;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>Renders one immutable toast snapshot.</summary>
public sealed partial class BzsToast : BzsComponentBase
{
    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    /// <summary>Gets or sets the toast snapshot to render.</summary>
    [Parameter]
    [EditorRequired]
    public BzsToastSnapshot? Toast { get; set; }

    /// <summary>Gets or sets the accessible label for the dismiss control.</summary>
    [Parameter]
    public string? DismissLabel { get; set; }

    /// <summary>Gets or sets the callback raised when manual dismissal is requested.</summary>
    [Parameter]
    public EventCallback<BzsToastDismissReason> DismissRequested { get; set; }

    /// <summary>Gets or sets the callback raised when automatic dismissal should pause.</summary>
    [Parameter]
    public EventCallback<BzsToastPauseReason> PauseRequested { get; set; }

    /// <summary>Gets or sets the callback raised when one pause reason ends.</summary>
    [Parameter]
    public EventCallback<BzsToastPauseReason> ResumeRequested { get; set; }

    private string EffectiveDismissLabel => string.IsNullOrWhiteSpace(DismissLabel)
        ? Localizer["DismissNotification"].Value
        : DismissLabel.Trim();

    private string SeverityName => Toast!.Severity switch
    {
        BzsToastSeverity.Information => "information",
        BzsToastSeverity.Success => "success",
        BzsToastSeverity.Warning => "warning",
        BzsToastSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(Toast), Toast.Severity, "The toast severity is not supported."),
    };

    private BzsIconData SeverityIcon => Toast!.Severity switch
    {
        BzsToastSeverity.Information => BzsIcons.Info,
        BzsToastSeverity.Success => BzsIcons.Success,
        BzsToastSeverity.Warning => BzsIcons.Warning,
        BzsToastSeverity.Error => BzsIcons.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(Toast), Toast.Severity, "The toast severity is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-toast bzs-toast--{SeverityName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = Toast!.Severity == BzsToastSeverity.Error ? "alert" : "status",
                ["aria-live"] = Toast.Severity == BzsToastSeverity.Error ? "assertive" : "polite",
                ["aria-atomic"] = "true",
                ["data-bzs-toast-severity"] = SeverityName,
            };

            if (!string.IsNullOrWhiteSpace(Toast.AccessibleName))
            {
                attributes["aria-label"] = Toast.AccessibleName.Trim();
            }
            else
            {
                attributes["aria-label"] = Localizer["NotificationLabel"].Value;
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Toast);
        if (!Enum.IsDefined(Toast.Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(Toast), Toast.Severity, "The toast severity is not supported.");
        }
    }

    private Task RequestDismissAsync(MouseEventArgs _) =>
        DismissRequested.InvokeAsync(BzsToastDismissReason.Manual);
}
