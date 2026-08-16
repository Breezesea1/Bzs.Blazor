using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a native text input integrated with EditContext.</summary>
public sealed partial class BzsTextInput : BzsInputBase<string?>
{
    private bool _isComposing;
    private string? _trailingCompositionValue;

    /// <summary>Gets or sets the constrained native text-family type.</summary>
    [Parameter] public BzsTextInputType InputType { get; set; } = BzsTextInputType.Text;

    /// <summary>Gets or sets when native changes commit the input value.</summary>
    [Parameter] public BzsInputUpdateMode UpdateMode { get; set; } = BzsInputUpdateMode.Change;

    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-text-input", GetNativeInputType()),
                StringComparer.OrdinalIgnoreCase);
            attributes.Remove("oninput");
            attributes.Remove("oncompositionstart");
            attributes.Remove("oncompositionend");
            attributes.Remove("onbzscompositionend");
            if (UpdateMode == BzsInputUpdateMode.Input)
            {
                attributes["oncompositionstart"] = EventCallback.Factory.Create<EventArgs>(
                    this,
                    OnCompositionStarted);
                attributes["onbzscompositionend"] = EventCallback.Factory.Create<ChangeEventArgs>(
                    this,
                    OnCompositionEnded);
            }
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(InputType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(InputType),
                InputType,
                "The text input type is not supported.");
        }

        if (!Enum.IsDefined(UpdateMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(UpdateMode),
                UpdateMode,
                "The text input update mode is not supported.");
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out string? result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    private void OnChanged(ChangeEventArgs args)
    {
        Commit(args.Value?.ToString());
    }

    private void OnInput(ChangeEventArgs args)
    {
        var value = args.Value?.ToString();
        if (_isComposing)
        {
            return;
        }

        if (_trailingCompositionValue is not null
            && string.Equals(_trailingCompositionValue, value, StringComparison.Ordinal))
        {
            _trailingCompositionValue = null;
            return;
        }

        _trailingCompositionValue = null;
        Commit(value);
    }

    private void OnCompositionStarted(EventArgs _)
    {
        _isComposing = true;
        _trailingCompositionValue = null;
    }

    private void OnCompositionEnded(ChangeEventArgs args)
    {
        _isComposing = false;
        var value = args.Value?.ToString();
        _trailingCompositionValue = value;
        Commit(value);
    }

    private void Commit(string? value)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = value;
        }
    }

    private string GetNativeInputType() => InputType switch
    {
        BzsTextInputType.Email => "email",
        BzsTextInputType.Search => "search",
        _ => "text",
    };
}
