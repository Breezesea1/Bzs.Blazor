using Bzs.Blazor.Localization;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>Renders an ordered breadcrumb trail with current-page semantics.</summary>
public sealed partial class BzsBreadcrumbs : BzsComponentBase
{
    private int _currentIndex = -1;

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    /// <summary>Gets or sets the breadcrumb items.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<BzsBreadcrumbItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the accessible name of the breadcrumb navigation landmark.
    /// A localized name is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets custom content for each breadcrumb label.</summary>
    [Parameter]
    public RenderFragment<BzsBreadcrumbItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets decorative content rendered between breadcrumb items.</summary>
    [Parameter]
    public RenderFragment? SeparatorContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-breadcrumbs"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-breadcrumbs"] = "true",
                ["aria-label"] = EffectiveAccessibleName,
            };

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        var explicitCurrentIndexes = Items
            .Select(static (item, index) => (item, index))
            .Where(static pair => pair.item.Current == true)
            .Select(static pair => pair.index)
            .ToArray();

        if (explicitCurrentIndexes.Length > 1)
        {
            throw new InvalidOperationException("BzsBreadcrumbs supports only one explicitly current item.");
        }

        if (explicitCurrentIndexes.Length == 1)
        {
            _currentIndex = explicitCurrentIndexes[0];
        }
        else if (Items.Count > 0 && Items[^1].Current != false)
        {
            _currentIndex = Items.Count - 1;
        }
        else
        {
            _currentIndex = -1;
        }
    }

    private string EffectiveAccessibleName => string.IsNullOrWhiteSpace(AccessibleName)
        ? Localizer["BreadcrumbAccessibleName"].Value
        : AccessibleName.Trim();

    private RenderFragment RenderItem(BzsBreadcrumbItem item) => builder =>
    {
        if (ItemTemplate is not null)
        {
            builder.AddContent(0, ItemTemplate(item));
        }
        else
        {
            builder.AddContent(0, item.Label);
        }
    };
}
