using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Renders a command, checkable command, or separator inside a Bzs menu.</summary>
public sealed partial class BzsMenuItem : BzsComponentBase, IDisposable
{
    private ElementReference _buttonElement;
    private IBzsMenuOwner? _registeredOwner;

    [CascadingParameter]
    private IBzsMenuOwner? Owner { get; set; }

    /// <summary>Gets or sets the command text used for display, naming, and typeahead.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Gets or sets custom visible command content. Text remains required for naming and typeahead.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets an optional decorative command icon.</summary>
    [Parameter]
    public BzsIconData? Icon { get; set; }

    /// <summary>Gets or sets optional decorative shortcut content.</summary>
    [Parameter]
    public RenderFragment? ShortcutContent { get; set; }

    /// <summary>Gets or sets whether this item renders as a separator.</summary>
    [Parameter]
    public bool Separator { get; set; }

    /// <summary>Gets or sets whether this command is unavailable.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets whether this item is a controlled checkable command.</summary>
    [Parameter]
    public bool Checkable { get; set; }

    /// <summary>Gets or sets the controlled checked state.</summary>
    [Parameter]
    public bool Checked { get; set; }

    /// <summary>Gets or sets the callback that requests a checked-state change.</summary>
    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    /// <summary>Gets or sets the callback invoked when this command is activated.</summary>
    [Parameter]
    public EventCallback Activated { get; set; }

    internal string EffectiveText => Text?.Trim() ?? string.Empty;

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes(Separator
                    ? "bzs-menu-item bzs-menu-item--separator"
                    : "bzs-menu-item"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = Separator ? "separator" : "none",
            };
            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> ButtonAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = "button",
                ["class"] = "bzs-menu-item__button",
                ["role"] = Checkable ? "menuitemcheckbox" : "menuitem",
                ["tabindex"] = Owner?.GetTabIndex(this) ?? -1,
            };

            if (Checkable)
            {
                attributes["aria-checked"] = Checked ? "true" : "false";
            }

            if (Disabled)
            {
                attributes["aria-disabled"] = "true";
                attributes["disabled"] = "disabled";
            }

            if (ChildContent is not null)
            {
                attributes["aria-label"] = EffectiveText;
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Owner is null)
        {
            throw new InvalidOperationException("BzsMenuItem must be rendered inside BzsMenu or BzsContextMenu.");
        }

        if (Separator)
        {
            if (!string.IsNullOrWhiteSpace(Text)
                || ChildContent is not null
                || Icon is not null
                || ShortcutContent is not null
                || Checkable
                || Checked
                || Activated.HasDelegate
                || CheckedChanged.HasDelegate)
            {
                throw new InvalidOperationException("A separator BzsMenuItem cannot define command content or callbacks.");
            }
        }
        else if (string.IsNullOrWhiteSpace(Text))
        {
            throw new InvalidOperationException("A command BzsMenuItem requires Text.");
        }

        if (Checked && !Checkable)
        {
            throw new InvalidOperationException("BzsMenuItem cannot be checked when Checkable is false.");
        }

        if (!ReferenceEquals(_registeredOwner, Owner))
        {
            _registeredOwner?.Unregister(this);
            _registeredOwner = Owner;
        }
        _registeredOwner.RegisterOrUpdate(this);
    }

    internal ValueTask FocusAsync() => _buttonElement.FocusAsync();

    internal Task RefreshAsync() => InvokeAsync(StateHasChanged);

    internal async Task InvokeCommandAsync()
    {
        if (Disabled || Separator)
        {
            return;
        }

        if (Checkable)
        {
            await CheckedChanged.InvokeAsync(!Checked);
        }
        await Activated.InvokeAsync();
    }

    private Task ActivateAsync() => Owner?.ActivateItemAsync(this) ?? Task.CompletedTask;

    private Task HandleKeyDownAsync(KeyboardEventArgs args) =>
        Owner?.HandleItemKeyDownAsync(this, args) ?? Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
        _registeredOwner?.Unregister(this);
        _registeredOwner = null;
    }
}
