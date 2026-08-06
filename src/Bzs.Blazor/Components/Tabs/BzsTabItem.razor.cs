namespace Bzs.Blazor;

/// <summary>
/// Declares a tab title and panel for a containing <see cref="BzsTabs" /> component.
/// </summary>
public sealed partial class BzsTabItem : BzsComponentBase, IDisposable
{
    private readonly string _generatedTabId = $"bzs-tab-{Guid.NewGuid():N}";
    private readonly string _generatedPanelId = $"bzs-tab-panel-{Guid.NewGuid():N}";
    private BzsTabs? _registeredTabs;

    /// <summary>
    /// Gets or sets the unique value used to identify this tab.
    /// </summary>
    [Parameter, EditorRequired]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the visible and accessible title of this tab.
    /// </summary>
    [Parameter, EditorRequired]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets whether this tab cannot be selected or focused.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the content rendered in this tab's panel.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the containing tab collection while the item is composed inside <see cref="BzsTabs" />.
    /// </summary>
    [CascadingParameter]
    public BzsTabs? Tabs { get; set; }

    internal string EffectiveValue => Value!;

    internal string EffectiveTitle => Title!;

    internal string TabId => GetExplicitId() ?? _generatedTabId;

    internal string PanelId => _generatedPanelId;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new ArgumentException("BzsTabItem requires a non-empty Value.", nameof(Value));
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentException("BzsTabItem requires a non-empty Title.", nameof(Title));
        }

        if (Tabs is null)
        {
            throw new InvalidOperationException("BzsTabItem must be a child of BzsTabs.");
        }

        var explicitId = GetExplicitId();
        if (explicitId is not null && explicitId.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("BzsTabItem Id cannot contain whitespace.", nameof(Id));
        }

        if (!ReferenceEquals(_registeredTabs, Tabs))
        {
            _registeredTabs?.Unregister(this);
            _registeredTabs = Tabs;
        }

        _registeredTabs.RegisterOrUpdate(this);
    }

    internal ElementReference TabElement { get; set; }

    internal ValueTask FocusAsync() => TabElement.FocusAsync();

    internal IReadOnlyDictionary<string, object> BuildTabAttributes(bool isSelected, bool isFocusable)
    {
        var attributes = new Dictionary<string, object>(
            BuildAttributes("bzs-tabs__tab"),
            StringComparer.OrdinalIgnoreCase);

        RemoveControlledAttributes(attributes);
        attributes["id"] = TabId;
        attributes["type"] = "button";
        attributes["role"] = "tab";
        attributes["aria-selected"] = isSelected ? "true" : "false";
        attributes["aria-controls"] = PanelId;
        attributes["tabindex"] = isFocusable ? "0" : "-1";
        attributes["data-bzs-tab-value"] = EffectiveValue;

        if (Disabled)
        {
            attributes["disabled"] = "disabled";
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    internal IReadOnlyDictionary<string, object> BuildPanelAttributes(bool isSelected)
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = PanelId,
            ["class"] = "bzs-tabs__panel",
            ["role"] = "tabpanel",
            ["tabindex"] = "0",
            ["aria-labelledby"] = TabId,
            ["data-bzs-tab-panel"] = EffectiveValue,
        };

        if (!isSelected)
        {
            attributes["hidden"] = "hidden";
        }

        return attributes;
    }

    private string? GetExplicitId()
    {
        if (!string.IsNullOrWhiteSpace(Id))
        {
            return Id.Trim();
        }

        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (attribute.Key.Equals("id", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(attribute.Value?.ToString()))
            {
                return attribute.Value.ToString()!.Trim();
            }
        }

        return null;
    }

    private static void RemoveControlledAttributes(IDictionary<string, object> attributes)
    {
        foreach (var name in new[]
        {
            "id", "type", "role", "disabled", "tabindex", "onclick", "onkeydown",
            "aria-selected", "aria-controls", "aria-disabled",
        })
        {
            attributes.Remove(name);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registeredTabs?.Unregister(this);
        _registeredTabs = null;
    }
}
