using System.Globalization;
using Bzs.Blazor.Components.Pagination;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>
/// Renders controlled, one-based pagination commands without loading data.
/// </summary>
public sealed partial class BzsPagination : BzsComponentBase
{
    private IReadOnlyList<int?> _range = Array.Empty<int?>();

    [Inject]
    private IStringLocalizer<BzsPaginationResources> Localizer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the consumer-controlled one-based current page.
    /// </summary>
    /// <remarks>
    /// This value must be between 1 and <see cref="PageCount" />, inclusive. When
    /// <see cref="PageCount" /> is 0, this value must be 1. Update this value and
    /// <see cref="PageCount" /> together when an asynchronous result reduces the range.
    /// The component never changes this parameter itself.
    /// </remarks>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the callback invoked when the component requests a one-based page.
    /// </summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages. A value of 0 renders an empty range
    /// with all navigation commands disabled.
    /// </summary>
    [Parameter]
    public int PageCount { get; set; }

    /// <summary>
    /// Gets or sets the number of page buttons rendered on each side of the current page.
    /// </summary>
    [Parameter]
    public int SiblingCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of page buttons rendered at each range boundary.
    /// </summary>
    [Parameter]
    public int BoundaryCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether all pagination commands are unavailable.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the numeric range is replaced by a localized current-page summary.
    /// </summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>
    /// Gets or sets the accessible name of the pagination navigation landmark.
    /// A localized name is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the first-page command.
    /// A localized label is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? FirstPageLabel { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the previous-page command.
    /// A localized label is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? PreviousPageLabel { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the next-page command.
    /// A localized label is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? NextPageLabel { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the last-page command.
    /// A localized label is used when this value is empty.
    /// </summary>
    [Parameter]
    public string? LastPageLabel { get; set; }

    private bool IsFirstPageUnavailable => Disabled || PageCount == 0 || Page == 1;

    private bool IsLastPageUnavailable => Disabled || PageCount == 0 || Page == PageCount;

    private string EffectiveAccessibleName => FirstNonEmpty(
        AccessibleName,
        GetAdditionalAttribute("aria-label"),
        Localizer["PaginationLabel"].Value);

    private string EffectiveFirstPageLabel => GetEffectiveLabel(FirstPageLabel, "FirstPageLabel");

    private string EffectivePreviousPageLabel => GetEffectiveLabel(PreviousPageLabel, "PreviousPageLabel");

    private string EffectiveNextPageLabel => GetEffectiveLabel(NextPageLabel, "NextPageLabel");

    private string EffectiveLastPageLabel => GetEffectiveLabel(LastPageLabel, "LastPageLabel");

    private string CompactStatus => PageCount == 0
        ? Localizer["NoPages"].Value
        : Localizer["PageStatus", Page, PageCount].Value;

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var mode = Compact ? "compact" : "full";
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-pagination bzs-pagination--{mode}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["aria-label"] = EffectiveAccessibleName,
                ["data-bzs-pagination"] = mode,
            };

            if (Disabled)
            {
                attributes["aria-disabled"] = "true";
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _range = BzsPaginationRange.Create(Page, PageCount, SiblingCount, BoundaryCount);
    }

    private Task RequestFirstPageAsync() => RequestPageAsync(1);

    private Task RequestPreviousPageAsync() => RequestPageAsync(Page - 1);

    private Task RequestNextPageAsync() => RequestPageAsync(Page + 1);

    private Task RequestLastPageAsync() => RequestPageAsync(PageCount);

    private async Task RequestPageAsync(int requestedPage)
    {
        if (Disabled
            || PageCount == 0
            || requestedPage < 1
            || requestedPage > PageCount
            || requestedPage == Page)
        {
            return;
        }

        await PageChanged.InvokeAsync(requestedPage);
    }

    private string GetPageLabel(int page) => page == Page
        ? Localizer["CurrentPageLabel", page].Value
        : Localizer["PageLabel", page].Value;

    private static string FormatPage(int page) => page.ToString(CultureInfo.CurrentCulture);

    private string GetEffectiveLabel(string? suppliedLabel, string resourceKey) =>
        string.IsNullOrWhiteSpace(suppliedLabel)
            ? Localizer[resourceKey].Value
            : suppliedLabel.Trim();

    private string? GetAdditionalAttribute(string name)
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        foreach (var attribute in AdditionalAttributes)
        {
            if (attribute.Key.Equals(name, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(attribute.Value?.ToString()))
            {
                return attribute.Value.ToString()!.Trim();
            }
        }

        return null;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(static value => !string.IsNullOrWhiteSpace(value))!.Trim();

    private static string GetPageKey(int page) => $"page-{page.ToString(CultureInfo.InvariantCulture)}";

    private static string GetEllipsisKey(int index) => $"ellipsis-{index.ToString(CultureInfo.InvariantCulture)}";
}
