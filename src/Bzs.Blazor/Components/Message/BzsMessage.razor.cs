namespace Bzs.Blazor;

/// <summary>
/// Renders inline, semantic feedback with composable content.
/// </summary>
public partial class BzsMessage : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the semantic severity of the message.
    /// </summary>
    [Parameter]
    public BzsMessageSeverity Severity { get; set; } = BzsMessageSeverity.Information;

    /// <summary>
    /// Gets or sets the optional concise message heading.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the content rendered inside the message.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string SeverityName => Severity switch
    {
        BzsMessageSeverity.Information => "information",
        BzsMessageSeverity.Success => "success",
        BzsMessageSeverity.Warning => "warning",
        BzsMessageSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The message severity is not supported."),
    };

    private bool IsAssertive => Severity == BzsMessageSeverity.Error;

    private BzsIconData SeverityIcon => Severity switch
    {
        BzsMessageSeverity.Information => BzsIcons.Info,
        BzsMessageSeverity.Success => BzsIcons.Success,
        BzsMessageSeverity.Warning => BzsIcons.Warning,
        BzsMessageSeverity.Error => BzsIcons.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The message severity is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-message bzs-message--{SeverityName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = IsAssertive ? "alert" : "status",
                ["aria-live"] = IsAssertive ? "assertive" : "polite",
                ["aria-atomic"] = "true",
                ["data-bzs-message-severity"] = SeverityName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The message severity is not supported.");
        }
    }
}
