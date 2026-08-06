namespace Bzs.Blazor;

/// <summary>Connects a label, description, input, and validation message.</summary>
public sealed partial class BzsField : BzsComponentBase
{
    /// <summary>Gets or sets the identifier of the native input.</summary>
    [Parameter, EditorRequired] public string? InputId { get; set; }
    /// <summary>Gets or sets the optional label identifier.</summary>
    [Parameter] public string? LabelId { get; set; }
    /// <summary>Gets or sets the label target; null uses InputId and an empty value omits for.</summary>
    [Parameter] public string? LabelFor { get; set; }
    /// <summary>Gets or sets the field label.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Gets or sets supporting field text.</summary>
    [Parameter] public string? Description { get; set; }
    /// <summary>Gets or sets the validation message.</summary>
    [Parameter] public string? Error { get; set; }
    /// <summary>Gets or sets whether a required indicator is shown.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Gets or sets the field control content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string DescriptionId => $"{InputId}-description";
    private string ErrorId => $"{InputId}-error";
    private string? EffectiveLabelFor => LabelFor is null
        ? InputId
        : string.IsNullOrWhiteSpace(LabelFor) ? null : LabelFor.Trim();
    private IReadOnlyDictionary<string, object> RootAttributes => BuildAttributes("bzs-field");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(InputId))
        {
            throw new InvalidOperationException("BzsField requires a non-empty InputId.");
        }
    }
}
