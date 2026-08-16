using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor;

/// <summary>Renders a native password input integrated with EditContext.</summary>
public sealed partial class BzsPasswordInput : BzsInputBase<string?>, IAsyncDisposable
{
    private const string ModulePath = "./_content/Bzs.Blazor/Components/Form/BzsPasswordInput.razor.js";

    private ElementReference _inputReference;
    private BzsJsModule? _interop;
    private bool _revealed;
    private bool _restoreSelectionPending;
    private bool _disposed;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>Gets or sets whether the input offers an interactive visibility control.</summary>
    [Parameter] public bool Revealable { get; set; }

    /// <summary>Gets or sets the accessible label for the action that reveals the password.</summary>
    [Parameter] public string? ShowPasswordText { get; set; }

    /// <summary>Gets or sets the accessible label for the action that hides the password.</summary>
    [Parameter] public string? HidePasswordText { get; set; }

    private IReadOnlyDictionary<string, object> InputAttributes =>
        BuildInputAttributes("bzs-input bzs-password-input__input", _revealed && Revealable ? "text" : "password");

    private string RevealAccessibleLabel => _revealed
        ? GetLabel(HidePasswordText, "HidePassword")
        : GetLabel(ShowPasswordText, "ShowPassword");

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

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Revealable)
        {
            _revealed = false;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || !_restoreSelectionPending)
        {
            return;
        }

        _restoreSelectionPending = false;
        await GetInterop().TryInvokeVoidAsync("restoreFocusAndSelection", _inputReference);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_interop is not null)
        {
            await _interop.DisposeAsync();
        }
    }

    private void OnChanged(ChangeEventArgs args)
    {
        if (!Disabled && !ReadOnly)
        {
            CurrentValueAsString = args.Value?.ToString();
        }
    }

    private async Task ToggleRevealAsync()
    {
        if (Disabled)
        {
            return;
        }

        await GetInterop().TryInvokeVoidAsync("captureSelection", _inputReference);
        if (_disposed)
        {
            return;
        }

        _revealed = !_revealed;
        _restoreSelectionPending = true;
    }

    private string GetLabel(string? overrideText, string resourceKey) =>
        string.IsNullOrWhiteSpace(overrideText) ? Localize(resourceKey) : overrideText.Trim();

    private BzsJsModule GetInterop() => _interop ??= new BzsJsModule(
        JsRuntime,
        ModulePath,
        LoggerFactory,
        new BzsJsModuleOptions(TreatInvalidOperationDuringImportAsTransient: true));
}
