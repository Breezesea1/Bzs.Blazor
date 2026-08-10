using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bzs.Blazor;

/// <summary>Renders native file selection with metadata validation and EditContext integration.</summary>
public sealed partial class BzsFileUpload : BzsInputBase<IReadOnlyList<IBrowserFile>>
{
    private readonly string _fallbackInputKey = Guid.NewGuid().ToString("N");
    private ValidationMessageStore? _validationMessages;
    private EditContext? _subscribedEditContext;
    private string? _localValidationError;
    private int _inputKeyVersion;

    /// <summary>Gets or sets whether the browser may select more than one file.</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>Gets or sets the browser accept filter, such as <c>image/*,.pdf</c>.</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>Gets or sets the maximum number of selected files. Null means no additional limit.</summary>
    [Parameter] public int? MaximumFileCount { get; set; }

    /// <summary>Gets or sets the maximum size in bytes for each selected file.</summary>
    [Parameter] public long? MaximumFileSize { get; set; }

    /// <summary>Gets or sets the maximum combined size in bytes for the selection.</summary>
    [Parameter] public long? MaximumTotalSize { get; set; }

    /// <summary>Gets or sets consumer-owned progress values keyed by the selected browser file.</summary>
    [Parameter] public IReadOnlyDictionary<IBrowserFile, double?>? Progress { get; set; }

    /// <summary>Gets or sets the callback raised after a browser selection is applied.</summary>
    [Parameter] public EventCallback<IReadOnlyList<IBrowserFile>> SelectionChanged { get; set; }

    /// <summary>Gets or sets the callback raised after a selected file is removed.</summary>
    [Parameter] public EventCallback<IBrowserFile> FileRemoved { get; set; }

    /// <summary>Gets or sets the callback raised after the selection is cleared.</summary>
    [Parameter] public EventCallback Cleared { get; set; }

    /// <summary>Gets or sets a template for each selected file.</summary>
    [Parameter] public RenderFragment<IBrowserFile>? FileTemplate { get; set; }

    /// <summary>Gets or sets a template rendered when there are no selected files.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Gets or sets the empty-selection text.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Gets or sets the selected-files accessible name.</summary>
    [Parameter] public string? FilesLabel { get; set; }

    /// <summary>Gets or sets the progress accessible name.</summary>
    [Parameter] public string? ProgressLabel { get; set; }

    /// <summary>Gets or sets the remove command text.</summary>
    [Parameter] public string? RemoveText { get; set; }

    /// <summary>Gets or sets the clear command text.</summary>
    [Parameter] public string? ClearText { get; set; }

    /// <summary>Gets or sets the required validation message.</summary>
    [Parameter] public string? RequiredValidationMessage { get; set; }

    /// <summary>Gets or sets the maximum-count validation message. The <c>{0}</c> token is replaced with the limit.</summary>
    [Parameter] public string? MaximumFileCountValidationMessage { get; set; }

    /// <summary>Gets or sets the per-file-size validation message. The <c>{0}</c> token is replaced with the limit.</summary>
    [Parameter] public string? MaximumFileSizeValidationMessage { get; set; }

    /// <summary>Gets or sets the total-size validation message. The <c>{0}</c> token is replaced with the limit.</summary>
    [Parameter] public string? MaximumTotalSizeValidationMessage { get; set; }

    private IReadOnlyList<IBrowserFile> SelectedFiles => Value ?? Array.Empty<IBrowserFile>();
    private string CurrentError => _localValidationError ?? FieldError ?? string.Empty;
    private string EffectiveEmptyText => GetText(EmptyText, "FileUploadEmpty");
    private string EffectiveFilesLabel => GetText(FilesLabel, "FileUploadFilesLabel");
    private string EffectiveProgressLabel => GetText(ProgressLabel, "FileUploadProgressLabel");
    private string EffectiveRemoveText => GetText(RemoveText, "FileUploadRemove");
    private string EffectiveClearText => GetText(ClearText, "FileUploadClear");
    private string InputKey => $"{_fallbackInputKey}-{_inputKeyVersion}";

    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildInputAttributes("bzs-input bzs-file-upload__input", "file", supportsReadOnly: false),
                StringComparer.OrdinalIgnoreCase);
            attributes.Remove("accept");
            attributes.Remove("multiple");

            if (!string.IsNullOrWhiteSpace(Accept))
            {
                attributes["accept"] = Accept.Trim();
            }

            if (Multiple)
            {
                attributes["multiple"] = "multiple";
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out IReadOnlyList<IBrowserFile> result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = Array.Empty<IBrowserFile>();
        validationErrorMessage = null;
        return true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (MaximumFileCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumFileCount),
                MaximumFileCount,
                "MaximumFileCount must be greater than zero when specified.");
        }

        if (MaximumFileSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumFileSize),
                MaximumFileSize,
                "MaximumFileSize must be greater than zero when specified.");
        }

        if (MaximumTotalSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumTotalSize),
                MaximumTotalSize,
                "MaximumTotalSize must be greater than zero when specified.");
        }

        if (Progress is not null && Progress.Values.Any(static value =>
            value is double progress && (!double.IsFinite(progress) || progress < 0 || progress > 100)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Progress),
                Progress,
                "Progress values must be null or finite values from 0 through 100.");
        }

        if (EditContext is not null && _subscribedEditContext is null)
        {
            _subscribedEditContext = EditContext;
            _subscribedEditContext.OnValidationRequested += OnValidationRequested;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _validationMessages is not null)
        {
            _validationMessages.Clear(FieldIdentifier);
            EditContext?.NotifyValidationStateChanged();
        }

        if (disposing && _subscribedEditContext is not null)
        {
            _subscribedEditContext.OnValidationRequested -= OnValidationRequested;
            _subscribedEditContext = null;
        }

        base.Dispose(disposing);
    }

    private async Task OnInputChangedAsync(InputFileChangeEventArgs args)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        var files = args.GetMultipleFiles(Math.Max(args.FileCount, 1)).ToArray();
        var selected = Multiple ? files : files.Take(1).ToArray();
        CurrentValue = selected;
        SetValidation(selected, files.Length > 1 && !Multiple);
        await SelectionChanged.InvokeAsync(selected);
    }

    private async Task RemoveAsync(int index, IBrowserFile file)
    {
        if (Disabled || ReadOnly || index < 0 || index >= SelectedFiles.Count)
        {
            return;
        }

        var selected = SelectedFiles.Where((_, itemIndex) => itemIndex != index).ToArray();
        CurrentValue = selected;
        _inputKeyVersion++;
        SetValidation(selected);
        await FileRemoved.InvokeAsync(file);
    }

    private async Task ClearAsync()
    {
        if (Disabled || ReadOnly || SelectedFiles.Count == 0)
        {
            return;
        }

        CurrentValue = Array.Empty<IBrowserFile>();
        _inputKeyVersion++;
        SetValidation(Array.Empty<IBrowserFile>());
        await Cleared.InvokeAsync();
    }

    private void SetValidation(IReadOnlyList<IBrowserFile> files, bool singleSelectionError = false)
    {
        var errors = new List<string>();
        if (Required && files.Count == 0)
        {
            errors.Add(string.IsNullOrWhiteSpace(RequiredValidationMessage)
                ? Localize("FileUploadRequired")
                : RequiredValidationMessage.Trim());
        }

        var countLimit = Multiple ? MaximumFileCount : 1;
        if (singleSelectionError || countLimit is int maximumCount && files.Count > maximumCount)
        {
            var template = string.IsNullOrWhiteSpace(MaximumFileCountValidationMessage)
                ? Localize("FileUploadMaximumCount", countLimit ?? 1)
                : MaximumFileCountValidationMessage.Trim();
            errors.Add(string.IsNullOrWhiteSpace(MaximumFileCountValidationMessage)
                ? template
                : ApplyLimit(template, countLimit ?? 1));
        }

        if (MaximumFileSize is long maximumFileSize
            && files.Any(file => file.Size > maximumFileSize))
        {
            var template = string.IsNullOrWhiteSpace(MaximumFileSizeValidationMessage)
                ? Localize("FileUploadMaximumFileSize", maximumFileSize)
                : MaximumFileSizeValidationMessage.Trim();
            errors.Add(string.IsNullOrWhiteSpace(MaximumFileSizeValidationMessage)
                ? template
                : ApplyLimit(template, maximumFileSize));
        }

        if (MaximumTotalSize is long maximumTotalSize && ExceedsTotalSize(files, maximumTotalSize))
        {
            var template = string.IsNullOrWhiteSpace(MaximumTotalSizeValidationMessage)
                ? Localize("FileUploadMaximumTotalSize", maximumTotalSize)
                : MaximumTotalSizeValidationMessage.Trim();
            errors.Add(string.IsNullOrWhiteSpace(MaximumTotalSizeValidationMessage)
                ? template
                : ApplyLimit(template, maximumTotalSize));
        }

        _localValidationError = errors.Count == 0 ? null : string.Join(" ", errors);
        if (EditContext is not null)
        {
            _validationMessages ??= new ValidationMessageStore(EditContext);
            _validationMessages.Clear(FieldIdentifier);
            foreach (var error in errors)
            {
                _validationMessages.Add(FieldIdentifier, error);
            }

            EditContext.NotifyValidationStateChanged();
        }
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args) =>
        SetValidation(SelectedFiles);

    private static bool ExceedsTotalSize(IReadOnlyList<IBrowserFile> files, long maximumTotalSize)
    {
        long total = 0;
        foreach (var file in files)
        {
            if (file.Size > maximumTotalSize - total)
            {
                return true;
            }

            total += file.Size;
        }

        return false;
    }

    private string FormatFileDetails(IBrowserFile file) =>
        $"{FormatSize(file.Size)} - {file.ContentType}";

    private static string FormatSize(long size) =>
        size.ToString("N0", CultureInfo.CurrentCulture) + " bytes";

    private static string FormatProgressValue(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatProgressText(double value) =>
        value.ToString("0.##", CultureInfo.CurrentCulture) + "%";

    private static string ApplyLimit(string template, long limit) =>
        template.Replace("{0}", limit.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);

    private string GetText(string? suppliedText, string resourceKey) =>
        string.IsNullOrWhiteSpace(suppliedText) ? Localize(resourceKey) : suppliedText.Trim();
}
