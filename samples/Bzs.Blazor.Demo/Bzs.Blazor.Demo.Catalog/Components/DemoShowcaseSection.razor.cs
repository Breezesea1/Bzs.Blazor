using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor.Demo.Client.Components;

/// <summary>
/// One showcase section: a bilingual heading, an optional description, then a live example. Pages
/// supply the wrapper class, the copy, and the example; this owns the heading block and its
/// accessible name wiring so the structure is defined once instead of once per page.
/// </summary>
public sealed partial class DemoShowcaseSection
{
    /// <summary>Gets or sets the classes applied to the section wrapper.</summary>
    [Parameter]
    public string Class { get; set; } = "demo-panel";

    /// <summary>
    /// Gets or sets an extra class on the heading block, for the pages that style their headings
    /// beyond the shared treatment.
    /// </summary>
    [Parameter]
    public string? HeaderClass { get; set; }

    /// <summary>Gets or sets the section heading.</summary>
    [Parameter, EditorRequired]
    public string? Heading { get; set; }

    /// <summary>
    /// Gets or sets the id given to the heading, which also becomes the section's accessible name.
    /// </summary>
    [Parameter, EditorRequired]
    public string? HeadingId { get; set; }

    /// <summary>Gets or sets the heading level: 2 for a section, 3 for a subsection.</summary>
    [Parameter]
    public int HeadingLevel { get; set; } = 2;

    /// <summary>
    /// Gets or sets the plain-text description shown under the heading. Sections that only need a
    /// heading leave both description parameters unset.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a description containing markup, for the sections whose prose names a parameter
    /// or a CSS value. Takes precedence over <see cref="Description" />.
    /// </summary>
    [Parameter]
    public RenderFragment? DescriptionContent { get; set; }

    /// <summary>Gets or sets the live example and any status copy under the heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets attributes forwarded to the section element, such as lang or dir.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string HeaderClasses => string.IsNullOrWhiteSpace(HeaderClass)
        ? "demo-showcase-section-header"
        : $"demo-showcase-section-header {HeaderClass.Trim()}";
}
