using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

/// <summary>
/// A live preview beside the code that produces it, as the layout page pairs them for every
/// primitive it demonstrates. The page supplies the preview, the caption, and the snippet markup;
/// this owns the two-column frame and the figure structure.
/// </summary>
public sealed partial class DemoLayoutExample
{
    /// <summary>
    /// Gets or sets an extra class on the example frame, for the app shell example whose preview
    /// spans the full width instead of sitting beside its code.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the label above the preview, for an example that says something more specific
    /// than that the preview is live. Unset falls back to the catalog's bilingual wording.
    /// </summary>
    [Parameter]
    public string? PreviewLabel { get; set; }

    /// <summary>Gets or sets the language shown in the figure caption.</summary>
    [Parameter]
    public string CodeLanguage { get; set; } = "Razor";

    /// <summary>Gets or sets the caption's summary of what the snippet demonstrates.</summary>
    [Parameter, EditorRequired]
    public string? CodeSummary { get; set; }

    /// <summary>Gets or sets the live component composition being demonstrated.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? Preview { get; set; }

    /// <summary>Gets or sets the snippet markup, which the page writes as pre and code elements.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? Code { get; set; }

    private string PreviewLabelText => string.IsNullOrWhiteSpace(PreviewLabel)
        ? DemoText.Layout.LivePreview
        : PreviewLabel;

    private string ExampleClasses => string.IsNullOrWhiteSpace(Class)
        ? "demo-layout-example"
        : $"demo-layout-example {Class.Trim()}";
}
