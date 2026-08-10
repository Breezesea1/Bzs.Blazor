using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>
/// Renders a route link or a controlled disclosure group inside <see cref="BzsNavMenu" />.
/// </summary>
public sealed partial class BzsNavItem : BzsComponentBase
{
    private readonly string _itemsId = $"bzs-nav-items-{Guid.NewGuid():N}";
    private ElementReference _summaryElement;

    /// <summary>Gets or sets the visible text used when <see cref="LabelContent" /> is not supplied.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Gets or sets custom visible label content.</summary>
    [Parameter]
    public RenderFragment? LabelContent { get; set; }

    /// <summary>Gets or sets the optional decorative icon shown before the label.</summary>
    [Parameter]
    public BzsIconData? Icon { get; set; }

    /// <summary>Gets or sets the destination for a route item. Disclosure groups do not have a destination.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Gets or sets how the router matches <see cref="Href" /> when <see cref="Active" /> is not supplied.</summary>
    [Parameter]
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    /// <summary>
    /// Gets or sets an explicit active-state override. A <see langword="null" /> value uses router matching.
    /// </summary>
    [Parameter]
    public bool? Active { get; set; }

    /// <summary>Gets or sets whether the item cannot be activated or focused.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a disclosure group is open.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Gets or sets the callback used to request a disclosure-state change.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Gets or sets an explicit accessible name for custom or icon-only label content.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets attributes applied to the rendered link.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? LinkAttributes { get; set; }

    /// <summary>Gets or sets nested <see cref="BzsNavItem" /> components for a disclosure group.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool HasNestedItems => ChildContent is not null;

    private string EffectiveHref => Href!.Trim();

    private RenderFragment ItemContent => builder =>
    {
        var sequence = 0;
        if (Icon is not null)
        {
            builder.OpenComponent<BzsIcon>(sequence++);
            builder.AddAttribute(sequence++, nameof(BzsIcon.Icon), Icon);
            builder.AddAttribute(sequence++, nameof(BzsIcon.Class), "bzs-nav-item__icon");
            builder.CloseComponent();
        }

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "bzs-nav-item__label");
        if (LabelContent is not null)
        {
            builder.AddContent(sequence++, LabelContent);
        }
        else
        {
            builder.AddContent(sequence++, Label!.Trim());
        }
        builder.CloseElement();
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-nav-item"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-nav-item"] = HasNestedItems ? "group" : "link",
            };

            if (Disabled)
            {
                attributes["data-bzs-disabled"] = "true";
            }

            if (Active.HasValue)
            {
                attributes["data-bzs-active"] = Active.Value ? "true" : "false";
            }

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> DisclosureAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["class"] = "bzs-nav-item__disclosure",
                ["data-bzs-open"] = Open ? "true" : "false",
            };

            if (Open)
            {
                attributes["open"] = string.Empty;
            }

            if (Disabled)
            {
                attributes["inert"] = string.Empty;
            }

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> SummaryAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["class"] = "bzs-nav-item__summary",
                ["aria-controls"] = _itemsId,
            };

            AddAccessibleName(attributes);
            if (Disabled)
            {
                attributes["aria-disabled"] = "true";
                attributes["tabindex"] = "-1";
            }

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> RouterLinkAttributes => BuildLinkAttributes(isControlled: false);

    private IReadOnlyDictionary<string, object> ControlledLinkAttributes => BuildLinkAttributes(isControlled: true);

    private IReadOnlyDictionary<string, object> DisabledItemAttributes
    {
        get
        {
            var attributes = BuildLinkAttributes(isControlled: false);
            var disabledAttributes = new Dictionary<string, object>(attributes, StringComparer.OrdinalIgnoreCase)
            {
                ["class"] = CombineClasses(GetAttribute(attributes, "class"), "bzs-nav-item__link--disabled"),
                ["aria-disabled"] = "true",
                ["tabindex"] = "-1",
            };
            disabledAttributes.Remove("aria-current");
            foreach (var attribute in new[] { "target", "rel", "download", "hreflang", "referrerpolicy", "ping" })
            {
                disabledAttributes.Remove(attribute);
            }

            foreach (var attribute in disabledAttributes.Keys
                .Where(static name => name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                .ToArray())
            {
                disabledAttributes.Remove(attribute);
            }
            return disabledAttributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label) && LabelContent is null)
        {
            throw new InvalidOperationException("BzsNavItem requires Label or LabelContent.");
        }

        if (HasNestedItems && !string.IsNullOrWhiteSpace(Href))
        {
            throw new InvalidOperationException("BzsNavItem cannot specify both Href and nested ChildContent.");
        }

        if (!HasNestedItems && string.IsNullOrWhiteSpace(Href))
        {
            throw new InvalidOperationException("A BzsNavItem link requires Href.");
        }

        if (!Enum.IsDefined(Match))
        {
            throw new ArgumentOutOfRangeException(nameof(Match), Match, "The navigation link match mode is not supported.");
        }
    }

    private Task RequestToggleAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return OpenChanged.InvokeAsync(!Open);
    }

    private async Task HandleDisclosureKeyDownAsync(KeyboardEventArgs args)
    {
        if (!Disabled && Open && args.Key == "Escape")
        {
            await OpenChanged.InvokeAsync(false);
            await _summaryElement.FocusAsync();
        }
    }

    private IReadOnlyDictionary<string, object> BuildLinkAttributes(bool isControlled)
    {
        var attributes = LinkAttributes is null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(LinkAttributes, StringComparer.OrdinalIgnoreCase);

        var customClass = GetAttribute(attributes, "class");
        attributes.Remove("href");
        attributes.Remove("class");
        attributes.Remove("aria-current");
        attributes.Remove("aria-disabled");
        attributes.Remove("tabindex");
        attributes["class"] = CombineClasses(
            "bzs-nav-item__link",
            isControlled && Active == true ? "bzs-nav-item__link--active" : null,
            customClass);

        AddAccessibleName(attributes);
        if (isControlled && Active == true)
        {
            attributes["aria-current"] = "page";
        }

        return attributes;
    }

    private void AddAccessibleName(IDictionary<string, object> attributes)
    {
        if (!string.IsNullOrWhiteSpace(AccessibleName))
        {
            attributes["aria-label"] = AccessibleName.Trim();
        }
    }

    private static string? GetAttribute(IReadOnlyDictionary<string, object> attributes, string name) =>
        attributes.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static string CombineClasses(params string?[] classes) =>
        string.Join(" ", classes.Where(static value => !string.IsNullOrWhiteSpace(value)));
}
