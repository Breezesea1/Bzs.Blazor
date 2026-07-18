using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>
/// Renders a token-driven native button with controlled click behavior.
/// </summary>
public partial class BzsButton : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the semantic visual treatment of the button.
    /// </summary>
    [Parameter]
    public BzsButtonVariant Variant { get; set; } = BzsButtonVariant.Primary;

    /// <summary>
    /// Gets or sets the size adjustment applied on top of the active density.
    /// </summary>
    [Parameter]
    public BzsButtonSize Size { get; set; } = BzsButtonSize.Medium;

    /// <summary>
    /// Gets or sets the native button behavior. The default is <see cref="BzsButtonType.Button" />.
    /// </summary>
    [Parameter]
    public BzsButtonType Type { get; set; } = BzsButtonType.Button;

    /// <summary>
    /// Gets or sets whether the button is unavailable for interaction.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the button displays a loading indicator and suppresses clicks.
    /// </summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>
    /// Gets or sets the optional decorative icon displayed before the button content.
    /// </summary>
    [Parameter]
    public BzsIconData? StartIcon { get; set; }

    /// <summary>
    /// Gets or sets the optional decorative icon displayed after the button content.
    /// </summary>
    [Parameter]
    public BzsIconData? EndIcon { get; set; }

    /// <summary>
    /// Gets or sets an explicit accessible name. Supply this for an icon-only button.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>
    /// Gets or sets the content displayed within the button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the controlled callback invoked after an enabled button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> Click { get; set; }

    private bool IsInteractionDisabled => Disabled || Loading;

    private string VariantName => Variant switch
    {
        BzsButtonVariant.Primary => "primary",
        BzsButtonVariant.Secondary => "secondary",
        BzsButtonVariant.Outline => "outline",
        BzsButtonVariant.Ghost => "ghost",
        BzsButtonVariant.Danger => "danger",
        _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The button variant is not supported."),
    };

    private string SizeName => Size switch
    {
        BzsButtonSize.Small => "small",
        BzsButtonSize.Medium => "medium",
        BzsButtonSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, "The button size is not supported."),
    };

    private string TypeName => Type switch
    {
        BzsButtonType.Button => "button",
        BzsButtonType.Submit => "submit",
        BzsButtonType.Reset => "reset",
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "The button type is not supported."),
    };

    private IReadOnlyDictionary<string, object> ButtonAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-button bzs-button--{VariantName} bzs-button--{SizeName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["type"] = TypeName,
                ["data-bzs-variant"] = VariantName,
                ["data-bzs-size"] = SizeName,
            };

            attributes.Remove("onclick");
            if (IsInteractionDisabled)
            {
                attributes["disabled"] = "disabled";
            }
            else
            {
                attributes.Remove("disabled");
            }

            if (Loading)
            {
                attributes["aria-busy"] = "true";
            }
            else
            {
                attributes.Remove("aria-busy");
            }

            var accessibleName = GetAccessibleName();
            if (accessibleName is not null)
            {
                attributes["aria-label"] = accessibleName;
            }
            else
            {
                attributes.Remove("aria-label");
            }

            return attributes;
        }
    }

    private string? GetAccessibleName()
    {
        if (!string.IsNullOrWhiteSpace(AccessibleName))
        {
            return AccessibleName.Trim();
        }

        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (attribute.Key.Equals("aria-label", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(attribute.Value?.ToString()))
            {
                return attribute.Value.ToString()!.Trim();
            }
        }

        return null;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "The button variant is not supported.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "The button size is not supported.");
        }

        if (!Enum.IsDefined(Type))
        {
            throw new ArgumentOutOfRangeException(nameof(Type), Type, "The button type is not supported.");
        }

        if (ChildContent is null && !HasAccessibleName())
        {
            throw new InvalidOperationException(
                "BzsButton requires an AccessibleName when no ChildContent is provided.");
        }
    }

    private async Task HandleClickAsync(MouseEventArgs eventArgs)
    {
        if (IsInteractionDisabled)
        {
            return;
        }

        await Click.InvokeAsync(eventArgs);
    }

    private bool HasAccessibleName()
    {
        if (GetAccessibleName() is not null)
        {
            return true;
        }

        return AdditionalAttributes?.Any(attribute =>
            attribute.Key.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(attribute.Value?.ToString())) == true;
    }
}
