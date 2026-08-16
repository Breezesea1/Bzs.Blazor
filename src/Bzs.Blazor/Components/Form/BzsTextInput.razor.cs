using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>Renders a native text input integrated with EditContext.</summary>
public sealed partial class BzsTextInput : BzsInputBase<string?>
{
    private bool _isComposing;
    private string? _composedValue;
    private string? _trailingCompositionValue;

    /// <summary>Gets or sets the constrained native text-family type.</summary>
    [Parameter] public BzsTextInputType InputType { get; set; } = BzsTextInputType.Text;

    /// <summary>Gets or sets when native changes commit the input value.</summary>
    [Parameter] public BzsInputUpdateMode UpdateMode { get; set; } = BzsInputUpdateMode.Change;

    private IReadOnlyDictionary<string, object> InputAttributes =>
        BuildInputAttributes("bzs-input bzs-text-input", GetNativeInputType());

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
            _composedValue = value;
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
        _composedValue = null;
        _trailingCompositionValue = null;
    }

    private void OnCompositionEnded(EventArgs _)
    {
        _isComposing = false;
        if (_composedValue is not null)
        {
            _trailingCompositionValue = _composedValue;
            Commit(_composedValue);
        }
        _composedValue = null;
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
