using System.Globalization;
using Bzs.Blazor.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>
/// Renders a semantic typed DataGrid with controlled client sorting, paging,
/// and row selection.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed partial class BzsDataGrid<TItem> : BzsComponentBase
{
    private const string ModulePath = "./_content/Bzs.Blazor/Components/DataGrid/BzsDataGrid.razor.js";
    private const string TrueFilterDraft = "true";
    private const string FalseFilterDraft = "false";
    private static readonly IReadOnlyList<int> DefaultPageSizeOptions =
        Array.AsReadOnly(new[] { 10, 25, 50 });
    private readonly string _instanceId = $"bzs-data-grid-{Guid.NewGuid():N}";
    private readonly List<BzsDataGridColumn<TItem>> _columns = [];
    private readonly Dictionary<BzsDataGridColumn<TItem>, ColumnState> _columnStates = [];
    private readonly Dictionary<string, string> _filterDrafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _filterOperatorDrafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BzsDataGridFilter> _observedFilters = new(StringComparer.Ordinal);
    private ElementReference _selectAllReference;
    private ElementReference _tableReference;
    private BzsJsModule? _interop;
    private DotNetObjectReference<BzsDataGrid<TItem>>? _selfReference;
    private HashSet<object?>? _selectedItemKeys;
    private HashSet<object?>? _expandedItemKeys;
    private IReadOnlyList<TItem>? _queriedItems;
    private BzsDataGridProviderSession<TItem>? _providerSession;
    private IBzsDataGridProvider<TItem>? _sessionProvider;
    private string? _openColumnMenuKey;
    private bool _pageSizeMenuOpen;
    private bool _columnChooserOpen;
    private string? _columnLayoutFingerprint;
    private int _nextColumnCompositionOrder;
    private int _interactionBatchDepth;
    private bool _disposed;

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>Gets or sets the complete in-memory item collection.</summary>
    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Gets or sets the asynchronous page provider used instead of <see cref="Items" />.</summary>
    [Parameter]
    public IBzsDataGridProvider<TItem>? Provider { get; set; }

    /// <summary>Gets or sets the controlled filters combined with logical AND.</summary>
    [Parameter]
    public IReadOnlyList<BzsDataGridFilter> Filters { get; set; } = Array.Empty<BzsDataGridFilter>();

    /// <summary>Gets or sets the callback that requests a new immutable filter snapshot.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<BzsDataGridFilter>> FiltersChanged { get; set; }

    /// <summary>Gets or sets the callback invoked for the current provider request failure.</summary>
    [Parameter]
    public EventCallback<Exception> ProviderFailed { get; set; }

    /// <summary>Gets or sets the declarative typed columns.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets plain table-caption text.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>Gets or sets custom table-caption content.</summary>
    [Parameter]
    public RenderFragment? CaptionContent { get; set; }

    /// <summary>Gets or sets consumer-owned commands rendered beside the table caption.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>Gets or sets the table accessible name used when no caption is supplied.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the controlled one-based page.</summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the callback that requests a one-based page.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>Gets or sets the controlled number of rows per page.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Gets or sets the callback that requests a page-size change.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Gets or sets the positive page sizes exposed in the footer.</summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = DefaultPageSizeOptions;

    /// <summary>Gets or sets whether the numeric pager uses its compact presentation.</summary>
    [Parameter]
    public bool CompactPagination { get; set; }

    /// <summary>Gets or sets whether the footer displays the page-size selector.</summary>
    [Parameter]
    public bool ShowPageSizeSelector { get; set; } = true;

    /// <summary>Gets or sets whether the footer displays pagination controls.</summary>
    [Parameter]
    public bool ShowPagination { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the footer displays the current result range when at least one footer control is visible.
    /// </summary>
    [Parameter]
    public bool ShowResultSummary { get; set; }

    /// <summary>Gets or sets whether active filters are summarized below the caption.</summary>
    [Parameter]
    public bool ShowFilterSummary { get; set; } = true;

    /// <summary>Gets or sets the controlled global search text used by client-side and provider queries.</summary>
    [Parameter]
    public string? SearchText { get; set; }

    /// <summary>Gets or sets the callback that requests a global search text change.</summary>
    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    /// <summary>Gets or sets the accessible label of the global search field.</summary>
    [Parameter]
    public string? SearchTextLabel { get; set; }

    /// <summary>Gets or sets whether the toolbar displays the global search field.</summary>
    [Parameter]
    public bool ShowSearch { get; set; }

    /// <summary>Gets or sets whether client-side filtering and searching are applied to Items.</summary>
    [Parameter]
    public bool ClientFiltering { get; set; }

    /// <summary>Gets or sets the controlled visible-column keys.</summary>
    [Parameter]
    public IReadOnlyList<string> HiddenColumnKeys { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the callback that requests a visible-column snapshot.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> HiddenColumnKeysChanged { get; set; }

    /// <summary>Gets or sets whether the toolbar displays the column chooser.</summary>
    [Parameter]
    public bool ShowColumnChooser { get; set; }

    /// <summary>Gets or sets the accessible label of the column chooser.</summary>
    [Parameter]
    public string? ColumnChooserText { get; set; }

    /// <summary>Gets or sets the table density.</summary>
    [Parameter]
    public BzsDataGridDensity Density { get; set; }

    /// <summary>Gets or sets whether alternating rows receive a striped treatment.</summary>
    [Parameter]
    public bool Striped { get; set; }

    /// <summary>Gets or sets whether every cell receives a visible border.</summary>
    [Parameter]
    public bool Bordered { get; set; }

    /// <summary>Gets or sets whether the header remains visible while the viewport scrolls.</summary>
    [Parameter]
    public bool StickyHeader { get; set; }

    /// <summary>Gets or sets the row template rendered beneath an expanded row.</summary>
    [Parameter]
    public RenderFragment<TItem>? DetailTemplate { get; set; }

    /// <summary>
    /// Gets or sets whether rows are expanded unless listed in <see cref="ExpandedItemKeys" />.
    /// </summary>
    /// <remarks>
    /// <see cref="ExpandedItemKeys" /> always lists the rows whose expansion differs from this
    /// default, so a grid that starts expanded receives its collapsed keys through the same
    /// controlled parameter.
    /// </remarks>
    [Parameter]
    public bool DetailRowsExpanded { get; set; }

    /// <summary>
    /// Gets or sets the controlled row keys whose expansion differs from
    /// <see cref="DetailRowsExpanded" />.
    /// </summary>
    [Parameter]
    public IReadOnlyList<object?> ExpandedItemKeys { get; set; } = Array.Empty<object?>();

    /// <summary>Gets or sets the callback that requests expanded row keys.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<object?>> ExpandedItemKeysChanged { get; set; }

    /// <summary>Gets or sets the callback invoked when a row is activated.</summary>
    [Parameter]
    public EventCallback<BzsDataGridRowEventArgs<TItem>> RowClick { get; set; }

    /// <summary>Gets or sets a function that supplies an additional class for each row.</summary>
    [Parameter]
    public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>Gets or sets whether rows expose an activation command.</summary>
    [Parameter]
    public bool RowClickable { get; set; }

    /// <summary>Gets or sets whether resizable columns expose a pointer and keyboard resize handle.</summary>
    [Parameter]
    public bool ResizableColumns { get; set; }

    /// <summary>Gets or sets the callback invoked after a browser-owned column resize completes.</summary>
    [Parameter]
    public EventCallback<BzsDataGridColumnResizeEventArgs> ColumnResized { get; set; }

    /// <summary>Gets or sets the precedence-ordered controlled sorts used when <see cref="MultiSort" /> is enabled.</summary>
    [Parameter]
    public IReadOnlyList<BzsDataGridSort> Sorts { get; set; } = Array.Empty<BzsDataGridSort>();

    /// <summary>Gets or sets the callback that requests a precedence-ordered sort snapshot.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<BzsDataGridSort>> SortsChanged { get; set; }

    /// <summary>Gets or sets whether sorting accumulates across columns through <see cref="Sorts" />.</summary>
    [Parameter]
    public bool MultiSort { get; set; }

    /// <summary>Gets or sets the controlled single-column sort.</summary>
    [Parameter]
    public BzsDataGridSort? Sort { get; set; }

    /// <summary>Gets or sets the callback that requests a sort change.</summary>
    [Parameter]
    public EventCallback<BzsDataGridSort?> SortChanged { get; set; }

    /// <summary>Gets or sets the controlled row-selection mode.</summary>
    [Parameter]
    public BzsDataGridSelectionMode SelectionMode { get; set; }

    /// <summary>Gets or sets whether multiple selection includes a current-page select-all control.</summary>
    [Parameter]
    public bool ShowSelectAll { get; set; }

    /// <summary>Gets or sets the accessible label for the current-page select-all control.</summary>
    [Parameter]
    public string? SelectAllText { get; set; }

    /// <summary>Gets or sets the stable row-key selector required by row selection.</summary>
    [Parameter]
    public Func<TItem, object?>? ItemKey { get; set; }

    /// <summary>Gets or sets the comparer used for row keys.</summary>
    [Parameter]
    public IEqualityComparer<object?>? ItemKeyComparer { get; set; }

    /// <summary>Gets or sets the controlled item used by single selection.</summary>
    [Parameter]
    public TItem? SelectedItem { get; set; }

    /// <summary>Gets or sets the callback that requests a single selected item.</summary>
    [Parameter]
    public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Gets or sets the controlled items used by multiple selection.</summary>
    [Parameter]
    public IReadOnlyList<TItem> SelectedItems { get; set; } = Array.Empty<TItem>();

    /// <summary>Gets or sets the callback that requests multiple selected items.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Gets or sets whether loading content replaces the current rows.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Gets or sets an externally owned error that replaces the current rows.</summary>
    [Parameter]
    public Exception? Error { get; set; }

    /// <summary>Gets or sets custom loading-row content.</summary>
    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Gets or sets custom empty-row content.</summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Gets or sets custom error-row content.</summary>
    [Parameter]
    public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>Gets or sets loading text used by the default state row.</summary>
    [Parameter]
    public string? LoadingText { get; set; }

    /// <summary>Gets or sets empty text used by the default state row.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>Gets or sets error text used by the default state row.</summary>
    [Parameter]
    public string? ErrorText { get; set; }

    /// <summary>Gets or sets retry-command text used after provider failures.</summary>
    [Parameter]
    public string? RetryText { get; set; }

    /// <summary>Gets or sets the page-size field label.</summary>
    [Parameter]
    public string? PageSizeText { get; set; }

    /// <summary>Gets or sets the pagination landmark name.</summary>
    [Parameter]
    public string? PaginationAccessibleName { get; set; }

    /// <summary>Gets or sets the command text used to clear every active filter.</summary>
    [Parameter]
    public string? ClearAllFiltersText { get; set; }

    /// <summary>Gets or sets the row-selection column label.</summary>
    [Parameter]
    public string? SelectionColumnText { get; set; }

    /// <summary>Gets or sets a function that names each row-selection control.</summary>
    [Parameter]
    public Func<TItem, string>? RowAccessibleName { get; set; }

    /// <summary>
    /// Reloads the current provider request and completes when that request succeeds, fails, or is superseded.
    /// </summary>
    /// <remarks>
    /// Provider failures remain available through <see cref="ProviderFailed" /> and the rendered error state.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the grid is configured with <see cref="Items" /> instead of <see cref="Provider" />.</exception>
    public async Task RefreshAsync()
    {
        Task? completion = null;
        await InvokeAsync(() => { completion = QueueRefresh(); });
        if (completion is not null)
        {
            await completion;
        }
    }

    private Task? QueueRefresh()
    {
        if (_disposed)
        {
            return null;
        }

        if (Provider is null)
        {
            throw new InvalidOperationException("BzsDataGrid RefreshAsync requires Provider mode.");
        }

        return _providerSession?.QueueRefresh(CreateProviderRequest(), RendererInfo.IsInteractive);
    }

    internal IReadOnlyList<BzsDataGridColumn<TItem>> Columns => _columns;

    private IReadOnlyList<BzsDataGridColumn<TItem>> VisibleColumns
    {
        get
        {
            if (_columns.Count == 0)
            {
                return Array.Empty<BzsDataGridColumn<TItem>>();
            }

            var visible = new List<BzsDataGridColumn<TItem>>(_columns.Count);
            foreach (var column in _columns)
            {
                if (IsColumnVisible(column))
                {
                    visible.Add(column);
                }
            }
            return visible;
        }
    }

    private bool IsColumnVisible(BzsDataGridColumn<TItem> column)
    {
        if (!column.Visible)
        {
            return false;
        }

        foreach (var key in HiddenColumnKeys)
        {
            if (string.Equals(key, column.EffectiveKey, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private IReadOnlyList<BzsDataGridColumn<TItem>> HideableColumns
    {
        get
        {
            var hideable = new List<BzsDataGridColumn<TItem>>(_columns.Count);
            foreach (var column in _columns)
            {
                if (column.Hideable)
                {
                    hideable.Add(column);
                }
            }
            return hideable;
        }
    }

    private IReadOnlyList<TItem> SourceItems => Provider is null
        ? Items ?? Array.Empty<TItem>()
        : _providerSession?.AcceptedItems ?? Array.Empty<TItem>();

    /// <summary>
    /// Gets the client-side items after filtering and searching, before sorting and paging.
    /// </summary>
    private IReadOnlyList<TItem> QueriedItems
    {
        get
        {
            if (Provider is not null || !ClientFiltering)
            {
                return SourceItems;
            }

            return _queriedItems ??= BuildQueriedItems();
        }
    }

    private IReadOnlyList<TItem> BuildQueriedItems()
    {
        var items = SourceItems;
        var predicates = new List<Func<TItem, bool>>(Filters.Count + 1);
        foreach (var filter in Filters)
        {
            var column = FindColumn(filter.ColumnKey);
            if (column is null)
            {
                continue;
            }

            var captured = filter;
            var capturedColumn = column;
            predicates.Add(item => BzsDataGridOperations.Matches(captured, capturedColumn.GetFilterValue(item)));
        }

        if (EffectiveSearchText is { } search)
        {
            var searchColumns = new List<BzsDataGridColumn<TItem>>(_columns.Count);
            foreach (var column in _columns)
            {
                if (IsColumnVisible(column))
                {
                    searchColumns.Add(column);
                }
            }

            predicates.Add(item =>
            {
                var texts = new string?[searchColumns.Count];
                for (var index = 0; index < searchColumns.Count; index++)
                {
                    texts[index] = searchColumns[index].GetSearchText(item);
                }
                return BzsDataGridOperations.MatchesSearch(texts, search);
            });
        }

        return BzsDataGridOperations.Filter(items, predicates);
    }

    private IReadOnlyList<TItem> SortedItems
    {
        get
        {
            var queried = QueriedItems;
            var steps = BuildSortSteps();
            return steps.Count == 0
                ? queried
                : BzsDataGridOperations.Sort(queried, steps);
        }
    }

    private IReadOnlyList<BzsDataGridOperations.SortStep<TItem>> BuildSortSteps()
    {
        var sorts = EffectiveSorts;
        if (sorts.Count == 0)
        {
            return Array.Empty<BzsDataGridOperations.SortStep<TItem>>();
        }

        var steps = new List<BzsDataGridOperations.SortStep<TItem>>(sorts.Count);
        foreach (var sort in sorts)
        {
            var column = FindColumn(sort.ColumnKey);
            if (column is null)
            {
                continue;
            }

            steps.Add(new BzsDataGridOperations.SortStep<TItem>(column.Compare, sort.Direction));
        }
        return steps;
    }

    private IReadOnlyList<TItem> Rows
    {
        get
        {
            if (Provider is not null)
            {
                return SourceItems;
            }

            return BzsDataGridOperations.Paginate(SortedItems, ClientPage, PageSize);
        }
    }

    private int ClientItemCount => Provider is null ? QueriedItems.Count : SourceItems.Count;

    private int PageCount => ClientItemCount == 0
        ? 0
        : (int)((ClientItemCount + (long)PageSize - 1) / PageSize);

    /// <summary>
    /// Gets the page count derived from the unfiltered items, which is the range a consumer
    /// can validate <see cref="Page" /> against before client filtering narrows the result.
    /// </summary>
    private int SourcePageCount => SourceItems.Count == 0
        ? 0
        : (int)((SourceItems.Count + (long)PageSize - 1) / PageSize);

    /// <summary>
    /// Gets the page actually rendered, which client filtering may clamp below the controlled
    /// <see cref="Page" /> until the consumer accepts the requested correction.
    /// </summary>
    private int ClientPage => Math.Min(Page, Math.Max(1, PageCount));

    private bool EffectiveLoading => Provider is null
        ? Loading
        : _providerSession?.IsLoading != false;

    private Exception? EffectiveError => Provider is null ? Error : _providerSession?.Error;

    private bool HasAcceptedProviderResult =>
        Provider is not null && _providerSession?.HasAcceptedResult == true;

    private bool ControlsDisabled => Provider is null && (Loading || Error is not null);

    private int? AcceptedTotalCount => _providerSession?.AcceptedTotalCount;

    private int AcceptedPage => AcceptedRequest?.Page ?? Page;

    private int AcceptedPageSize => AcceptedRequest?.PageSize ?? PageSize;

    private BzsDataGridRequest? AcceptedRequest => _providerSession?.AcceptedRequest;

    private int AcceptedPageCount => AcceptedTotalCount is not int totalCount || totalCount == 0
        ? 0
        : (int)((totalCount + (long)AcceptedPageSize - 1) / AcceptedPageSize);

    private bool AcceptedHasNextPage => _providerSession?.AcceptedHasNextPage == true;

    private BzsDataGridSort? DisplayedSort => Provider is null ? Sort : AcceptedRequest?.Sort;

    private IReadOnlyList<BzsDataGridSort> DisplayedSorts => Provider is null
        ? EffectiveSorts
        : AcceptedRequest?.Sorts ?? Array.Empty<BzsDataGridSort>();

    /// <summary>Gets the precedence-ordered controlled sorts, projecting single sort when multi-sort is off.</summary>
    private IReadOnlyList<BzsDataGridSort> EffectiveSorts => MultiSort
        ? Sorts
        : Sort is null
            ? Array.Empty<BzsDataGridSort>()
            : [Sort];

    private int ColumnSpan => Math.Max(1, VisibleColumns.Count + LeadingColumnCount);

    private int LeadingColumnCount =>
        (SelectionMode == BzsDataGridSelectionMode.None ? 0 : 1)
        + (DetailTemplate is null ? 0 : 1);

    private string CaptionId => $"{_instanceId}-caption";

    private string SelectionInputName => $"{_instanceId}-selection";

    private bool HasCaption => CaptionContent is not null || !string.IsNullOrWhiteSpace(Caption);

    private bool ShowToolbarControls => ShowSearch || ShowColumnChooser && HideableColumns.Count > 0;

    private bool ShowCaptionArea => ToolbarContent is not null
        || ShowToolbarControls
        || SelectedCount > 0
        || ShowFilterSummary && Filters.Count > 0
        || CaptionContent is null && HasCaption;

    private bool ShowFooterRow => VisibleColumns.Any(static column => column.HasFooter);

    private string DensityName => Density == BzsDataGridDensity.Comfortable ? "comfortable" : "compact";

    private string? EffectiveSearchText => Normalize(SearchText);

    private string? TableAccessibleName => HasCaption
        ? null
        : Normalize(AccessibleName) ?? Localize("DataGridAccessibleName");

    private string? TableLabelledBy => HasCaption ? CaptionId : null;

    private int SelectedCount => SelectionMode switch
    {
        BzsDataGridSelectionMode.Single => SelectedItem is null ? 0 : 1,
        BzsDataGridSelectionMode.Multiple => SelectedItems.Count,
        _ => 0,
    };

    private string? ResultSummary
    {
        get
        {
            var rows = Rows;
            if (Provider is not null && !HasAcceptedProviderResult)
            {
                return null;
            }

            var totalCount = Provider is null ? ClientItemCount : AcceptedTotalCount;
            if (totalCount is null)
            {
                return Localize("DataGridUnknownResultSummaryText", rows.Count);
            }
            if (totalCount == 0 || rows.Count == 0)
            {
                return Localize("DataGridZeroResultSummaryText");
            }

            var page = Provider is null ? ClientPage : AcceptedPage;
            var pageSize = Provider is null ? PageSize : AcceptedPageSize;
            var first = (page - 1L) * pageSize + 1;
            var last = Math.Min(totalCount.Value, first + rows.Count - 1L);
            return Localize("DataGridResultSummaryText", first, last, totalCount.Value);
        }
    }

    private string EffectiveLoadingText => Normalize(LoadingText) ?? Localize("DataGridLoadingText");
    private string EffectiveEmptyText => Normalize(EmptyText) ?? Localize("DataGridEmptyText");
    private string EffectiveErrorText => Normalize(ErrorText) ?? Localize("DataGridErrorText");
    private string EffectiveRetryText => Normalize(RetryText) ?? Localize("DataGridRetryText");
    private string EffectivePageSizeText => Normalize(PageSizeText) ?? Localize("DataGridPageSizeText");
    private string EffectiveClearAllFiltersText =>
        Normalize(ClearAllFiltersText) ?? Localize("DataGridClearAllFiltersText");
    private string EffectivePaginationAccessibleName =>
        Normalize(PaginationAccessibleName) ?? Localize("DataGridPaginationAccessibleName");
    private string EffectiveSelectionColumnText =>
        Normalize(SelectionColumnText) ?? Localize("DataGridSelectionColumnText");
    private string EffectiveSelectAllText =>
        Normalize(SelectAllText) ?? Localize("DataGridSelectAllText");
    private string EffectiveSortAscendingText => Localize("DataGridSortAscendingText");
    private string EffectiveSortDescendingText => Localize("DataGridSortDescendingText");
    private string EffectiveClearSortText => Localize("DataGridClearSortText");
    private string EffectiveClearFilterText => Localize("DataGridClearFilterText");
    private string EffectiveSortText => Localize("DataGridSortText");
    private string EffectiveConditionText => Localize("DataGridConditionText");
    private string EffectiveValueText => Localize("DataGridValueText");
    private string EffectiveApplyText => Localize("DataGridApplyText");
    private string EffectiveContainsText => Localize("DataGridContainsText");
    private string EffectiveNotContainsText => Localize("DataGridNotContainsText");
    private string EffectiveStartsWithText => Localize("DataGridStartsWithText");
    private string EffectiveEndsWithText => Localize("DataGridEndsWithText");
    private string EffectiveEqualsText => Localize("DataGridEqualsText");
    private string EffectiveIsEmptyText => Localize("DataGridIsEmptyText");
    private string EffectiveIsNotEmptyText => Localize("DataGridIsNotEmptyText");
    private string EffectiveAnyOfText => Localize("DataGridAnyOfText");
    private string EffectiveNotEqualsText => Localize("DataGridNotEqualsText");
    private string EffectiveLessThanText => Localize("DataGridLessThanText");
    private string EffectiveLessThanOrEqualText => Localize("DataGridLessThanOrEqualText");
    private string EffectiveGreaterThanText => Localize("DataGridGreaterThanText");
    private string EffectiveGreaterThanOrEqualText => Localize("DataGridGreaterThanOrEqualText");
    private string EffectiveAnyText => Localize("DataGridAnyText");
    private string EffectiveTrueText => Localize("DataGridTrueText");
    private string EffectiveFalseText => Localize("DataGridFalseText");
    private string EffectivePreviousPageText => Localize("DataGridPreviousPageText");
    private string EffectiveNextPageText => Localize("DataGridNextPageText");
    private string EffectiveSearchTextLabel => Normalize(SearchTextLabel) ?? Localize("DataGridSearchText");
    private string EffectiveClearSearchText => Localize("DataGridClearSearchText");
    private string EffectiveColumnChooserText => Normalize(ColumnChooserText) ?? Localize("DataGridColumnChooserText");
    private string EffectiveDetailColumnText => Localize("DataGridDetailColumnText");
    private string EffectiveAddSortText => Localize("DataGridAddSortText");

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var classes = $"bzs-data-grid bzs-data-grid--{DensityName}";
            var attributes = new Dictionary<string, object>(
                BuildAttributes(classes),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-data-grid"] = "true",
                ["data-bzs-density"] = DensityName,
                ["data-bzs-striped"] = Striped ? "true" : "false",
                ["data-bzs-bordered"] = Bordered ? "true" : "false",
                ["data-bzs-sticky-header"] = StickyHeader ? "true" : "false",
                ["data-bzs-resizable-columns"] = ResizableColumns ? "true" : "false",
            };
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if ((Items is null) == (Provider is null))
        {
            throw new InvalidOperationException("BzsDataGrid requires exactly one of Items or Provider.");
        }

        if (Provider is not null && (Loading || Error is not null))
        {
            throw new InvalidOperationException(
                "Provider mode owns loading and error state; Loading and Error cannot also be supplied.");
        }

        if (Filters is null)
        {
            throw new InvalidOperationException("BzsDataGrid Filters cannot be null.");
        }

        if (Sorts is null || HiddenColumnKeys is null || ExpandedItemKeys is null)
        {
            throw new InvalidOperationException("BzsDataGrid collection parameters cannot be null.");
        }

        if (!Enum.IsDefined(Density))
        {
            throw new ArgumentOutOfRangeException(nameof(Density), Density, "The DataGrid density is not supported.");
        }

        if (!MultiSort && Sorts.Count > 1)
        {
            throw new InvalidOperationException("BzsDataGrid Sorts requires MultiSort when more than one sort is supplied.");
        }

        if (Provider is null && Filters.Count > 0 && !ClientFiltering)
        {
            throw new InvalidOperationException("DataGrid filters in Items mode require ClientFiltering.");
        }

        if (ChildContent is null)
        {
            throw new InvalidOperationException("BzsDataGrid requires ChildContent columns.");
        }

        if (CaptionContent is not null && !string.IsNullOrWhiteSpace(Caption))
        {
            throw new InvalidOperationException("BzsDataGrid accepts Caption or CaptionContent, but not both.");
        }

        if (PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "PageSize must be positive.");
        }

        if (PageSizeOptions is null
            || PageSizeOptions.Count == 0
            || PageSizeOptions.Any(static option => option <= 0)
            || PageSizeOptions.Distinct().Count() != PageSizeOptions.Count
            || !PageSizeOptions.Contains(PageSize))
        {
            throw new ArgumentException(
                "PageSizeOptions must contain distinct positive values including PageSize.",
                nameof(PageSizeOptions));
        }

        if (!Enum.IsDefined(SelectionMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(SelectionMode),
                SelectionMode,
                "The DataGrid selection mode is not supported.");
        }

        if (SelectionMode != BzsDataGridSelectionMode.None && ItemKey is null)
        {
            throw new InvalidOperationException("BzsDataGrid requires ItemKey when row selection is enabled.");
        }

        if (SelectedItems is null)
        {
            throw new InvalidOperationException("BzsDataGrid SelectedItems cannot be null.");
        }

        if (Provider is null)
        {
            _queriedItems = null;
            ValidatePage();
        }
        else if (Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Page), Page, "A provider page must be positive.");
        }

        SynchronizeProvider();
        ValidateItemKeys();
        _selectedItemKeys = CreateSelectedItemKeys();
        _expandedItemKeys = CreateExpandedItemKeys();
        SynchronizeFilterDrafts();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (ShowCurrentPageSelectAll && RendererInfo.IsInteractive)
        {
            await SynchronizeSelectAllAsync();
        }

        if (RendererInfo.IsInteractive)
        {
            await SynchronizeColumnLayoutAsync();
        }

        if (Provider is null
            || !RendererInfo.IsInteractive
            || _interactionBatchDepth > 0)
        {
            return;
        }

        if (_providerSession?.Submit(CreateProviderRequest()) is { } load)
        {
            await load;
        }
    }

    private void SynchronizeProvider()
    {
        if (ReferenceEquals(_sessionProvider, Provider))
        {
            return;
        }

        _providerSession?.Dispose();
        _providerSession = null;
        _sessionProvider = Provider;
        if (Provider is not null)
        {
            _providerSession = new BzsDataGridProviderSession<TItem>(
                Provider,
                new BzsDataGridProviderSessionCallbacks<TItem>(
                    StateChanged: StateHasChanged,
                    ResultAccepted: () => _selectedItemKeys = CreateSelectedItemKeys(),
                    ValidateItemKeys: items => ValidateItemKeys(items),
                    ProviderFailed: error => ProviderFailed.InvokeAsync(error),
                    PageCorrectionRequested: page => PageChanged.InvokeAsync(page),
                    UnhandledError: DispatchExceptionAsync));
        }
    }

    internal void RegisterOrUpdate(BzsDataGridColumn<TItem> column)
    {
        var compositionOrder = _nextColumnCompositionOrder++;
        var state = ColumnState.Create(column);
        foreach (var existing in _columns)
        {
            if (!ReferenceEquals(existing, column)
                && string.Equals(existing.EffectiveKey, state.Key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BzsDataGrid requires unique column keys. The key '{state.Key}' appears more than once.");
            }
        }

        var stateChanged = !_columnStates.TryGetValue(column, out var current) || current != state;
        var previousOrder = _columns.IndexOf(column);
        if (previousOrder >= 0)
        {
            _columns.RemoveAt(previousOrder);
        }
        var nextOrder = Math.Min(compositionOrder, _columns.Count);
        _columns.Insert(nextOrder, column);
        _columnStates[column] = state;
        _queriedItems = null;
        if (stateChanged || previousOrder != nextOrder)
        {
            StateHasChanged();
        }
    }

    internal void Unregister(BzsDataGridColumn<TItem> column)
    {
        if (!_columnStates.Remove(column))
        {
            return;
        }

        _columns.Remove(column);
        _queriedItems = null;
        StateHasChanged();
    }

    private BzsDataGridColumn<TItem>? GetSortColumn()
    {
        if (Sort is null)
        {
            return null;
        }

        if (!Enum.IsDefined(Sort.Direction))
        {
            throw new ArgumentOutOfRangeException(nameof(Sort), Sort.Direction, "The DataGrid sort direction is not supported.");
        }

        var column = _columns.FirstOrDefault(candidate =>
            string.Equals(candidate.EffectiveKey, Sort.ColumnKey, StringComparison.Ordinal));
        if (column is null)
        {
            if (_columns.Count == 0)
            {
                return null;
            }

            throw new InvalidOperationException($"The DataGrid sort column '{Sort.ColumnKey}' is not registered.");
        }

        if (!column.Sortable)
        {
            throw new InvalidOperationException($"The DataGrid column '{Sort.ColumnKey}' is not sortable.");
        }

        return column;
    }

    private Task RequestSortAsync(BzsDataGridColumn<TItem> column)
    {
        // The cycle follows the direction the column currently renders, so a multi-sort
        // grid that supplies only Sorts still advances the column a consumer can see.
        var current = GetSortDirection(column);
        if (MultiSort && EffectiveSorts.Count > 1)
        {
            return current is null
                ? RequestMultiSortAsync(column)
                : current == BzsDataGridSortDirection.Ascending
                    ? RequestReplaceColumnSortAsync(column, BzsDataGridSortDirection.Descending)
                    : RequestClearColumnSortAsync(column);
        }

        BzsDataGridSort? requested = current is null
            ? new(column.EffectiveKey, BzsDataGridSortDirection.Ascending)
            : current == BzsDataGridSortDirection.Ascending
                ? new(column.EffectiveKey, BzsDataGridSortDirection.Descending)
                : null;
        return RequestSortChangeAsync(requested);
    }

    /// <summary>Replaces one column's direction while preserving multi-sort precedence.</summary>
    private async Task RequestReplaceColumnSortAsync(
        BzsDataGridColumn<TItem> column,
        BzsDataGridSortDirection direction)
    {
        var replaced = EffectiveSorts
            .Select(sort => string.Equals(sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal)
                ? new BzsDataGridSort(column.EffectiveKey, direction)
                : sort)
            .ToArray();
        await RequestStateWithPageResetAsync(async () =>
        {
            IReadOnlyList<BzsDataGridSort> snapshot = Array.AsReadOnly(replaced);
            await SortsChanged.InvokeAsync(snapshot);
            await SortChanged.InvokeAsync(snapshot.Count == 0 ? null : snapshot[0]);
        });
    }

    private async Task RequestSpecificSortAsync(
        BzsDataGridColumn<TItem> column,
        BzsDataGridSortDirection direction)
    {
        if (MultiSort && GetSortDirection(column) is not null && EffectiveSorts.Count > 1)
        {
            await RequestReplaceColumnSortAsync(column, direction);
        }
        else
        {
            await RequestSortChangeAsync(new BzsDataGridSort(column.EffectiveKey, direction));
        }

        _openColumnMenuKey = null;
    }

    private async Task RequestClearSortAsync()
    {
        await RequestSortChangeAsync(null);
        _openColumnMenuKey = null;
    }

    /// <summary>Removes one column from the controlled sort, keeping other multi-sort entries.</summary>
    private async Task RequestClearColumnSortAsync(BzsDataGridColumn<TItem> column)
    {
        if (!MultiSort)
        {
            await RequestClearSortAsync();
            return;
        }

        var remaining = EffectiveSorts
            .Where(sort => !string.Equals(sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal))
            .ToArray();
        if (remaining.Length == EffectiveSorts.Count)
        {
            return;
        }

        await RequestStateWithPageResetAsync(async () =>
        {
            IReadOnlyList<BzsDataGridSort> snapshot = Array.AsReadOnly(remaining);
            await SortsChanged.InvokeAsync(snapshot);
            await SortChanged.InvokeAsync(snapshot.Count == 0 ? null : snapshot[0]);
        });
        _openColumnMenuKey = null;
    }

    private string GetResizeColumnLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridResizeColumnText", column.EffectiveAccessibleName!);

    /// <summary>
    /// Gets the resize separator position as a percentage of the visible columns, which keeps the
    /// separator role valid before the browser reports a measured width.
    /// </summary>
    private int GetColumnWidthValue(BzsDataGridColumn<TItem> column)
    {
        var columns = VisibleColumns;
        for (var index = 0; index < columns.Count; index++)
        {
            if (ReferenceEquals(columns[index], column))
            {
                return columns.Count == 0 ? 0 : (index + 1) * 100 / columns.Count;
            }
        }
        return 0;
    }

    private object GetDetailRenderKey(TItem item, int rowIndex) => ItemKey is null
        ? $"detail-{rowIndex}"
        : new DetailKey(GetItemKey(item), KeyComparer);

    private Task RequestSortChangeAsync(BzsDataGridSort? requested)
    {
        var currentSorts = EffectiveSorts;
        var requestedSorts = requested is null
            ? Array.Empty<BzsDataGridSort>()
            : new[] { requested };
        if (BzsDataGridRequestEquality.SortListsEqual(currentSorts, requestedSorts))
        {
            return Task.CompletedTask;
        }

        return RequestStateWithPageResetAsync(async () =>
        {
            if (MultiSort)
            {
                await SortsChanged.InvokeAsync(Array.AsReadOnly(requestedSorts));
            }

            await SortChanged.InvokeAsync(requested);
        });
    }

    private async Task RequestMultiSortAsync(BzsDataGridColumn<TItem> column)
    {
        if (!column.Sortable || ControlsDisabled)
        {
            return;
        }

        var current = EffectiveSorts.ToList();
        var index = current.FindIndex(sort => string.Equals(sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal));
        if (index < 0)
        {
            current.Add(new BzsDataGridSort(column.EffectiveKey, BzsDataGridSortDirection.Ascending));
        }
        else if (current[index].Direction == BzsDataGridSortDirection.Ascending)
        {
            current[index] = new BzsDataGridSort(column.EffectiveKey, BzsDataGridSortDirection.Descending);
        }
        else
        {
            current.RemoveAt(index);
        }

        await RequestStateWithPageResetAsync(async () =>
        {
            IReadOnlyList<BzsDataGridSort> snapshot = Array.AsReadOnly(current.ToArray());
            await SortsChanged.InvokeAsync(snapshot);
            await SortChanged.InvokeAsync(snapshot.Count == 0 ? null : snapshot[0]);
        });
        _openColumnMenuKey = null;
    }

    private async Task RequestPageSizeAsync(int requested)
    {
        _pageSizeMenuOpen = false;
        if (!PageSizeOptions.Contains(requested) || requested == PageSize)
        {
            return;
        }

        _interactionBatchDepth++;
        try
        {
            if (Page != 1)
            {
                await PageChanged.InvokeAsync(1);
            }
            await PageSizeChanged.InvokeAsync(requested);
        }
        finally
        {
            _interactionBatchDepth--;
            if (_interactionBatchDepth == 0 && !_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private Task RequestPageAsync(int requestedPage) =>
        requestedPage < 1
            ? Task.CompletedTask
            : requestedPage == Page
                ? Provider is not null && _providerSession?.Error is not null
                    ? RetryProviderAsync()
                    : Task.CompletedTask
                : PageChanged.InvokeAsync(requestedPage);

    private async Task RequestSelectionAsync(TItem item)
    {
        if (SelectionMode == BzsDataGridSelectionMode.Single)
        {
            await SelectedItemChanged.InvokeAsync(item);
            return;
        }

        if (SelectionMode != BzsDataGridSelectionMode.Multiple)
        {
            return;
        }

        var selectedKeys = _selectedItemKeys ?? CreateSelectedItemKeys()!;
        var requestedKey = GetItemKey(item);
        var removeRequestedItem = selectedKeys.Contains(requestedKey);
        var selected = new List<TItem>(SelectedItems.Count + (removeRequestedItem ? 0 : 1));
        var currentItemsByKey = new Dictionary<object, TItem>(KeyComparer);
        foreach (var currentItem in SourceItems)
        {
            currentItemsByKey[GetItemKey(currentItem)] = currentItem;
        }

        foreach (var selectedItem in SelectedItems)
        {
            var selectedKey = GetItemKey(selectedItem);
            if (removeRequestedItem && KeyComparer.Equals(selectedKey, requestedKey))
            {
                continue;
            }

            selected.Add(currentItemsByKey.TryGetValue(selectedKey, out var currentItem)
                ? currentItem
                : selectedItem);
        }

        if (!removeRequestedItem)
        {
            selected.Add(item);
        }

        await SelectedItemsChanged.InvokeAsync(selected);
    }

    private async Task RequestCurrentPageSelectionAsync(ChangeEventArgs args)
    {
        if (!ShowCurrentPageSelectAll)
        {
            return;
        }

        var rows = Rows;
        if (rows.Count == 0)
        {
            return;
        }

        var selectCurrentPage = args.Value is bool isChecked
            ? isChecked
            : !AreAllCurrentRowsSelected(rows);
        var currentItemsByKey = new Dictionary<object, TItem>(KeyComparer);
        foreach (var row in rows)
        {
            currentItemsByKey.Add(GetItemKey(row), row);
        }

        var selected = new List<TItem>(SelectedItems.Count + rows.Count);
        var selectedCurrentPageKeys = new HashSet<object?>(KeyComparer);
        foreach (var selectedItem in SelectedItems)
        {
            var selectedKey = GetItemKey(selectedItem);
            if (currentItemsByKey.TryGetValue(selectedKey, out var currentItem))
            {
                if (selectCurrentPage && selectedCurrentPageKeys.Add(selectedKey))
                {
                    selected.Add(currentItem);
                }

                continue;
            }

            selected.Add(selectedItem);
        }

        if (selectCurrentPage)
        {
            foreach (var row in rows)
            {
                if (selectedCurrentPageKeys.Add(GetItemKey(row)))
                {
                    selected.Add(row);
                }
            }
        }

        await SelectedItemsChanged.InvokeAsync(selected);
    }

    private bool IsColumnMenuOpen(BzsDataGridColumn<TItem> column) =>
        string.Equals(_openColumnMenuKey, column.EffectiveKey, StringComparison.Ordinal);

    private Task SetColumnMenuOpen(BzsDataGridColumn<TItem> column, bool open)
    {
        _openColumnMenuKey = open ? column.EffectiveKey : null;
        return Task.CompletedTask;
    }

    private string GetColumnMenuLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridColumnMenuText", column.EffectiveAccessibleName!);

    private string GetColumnSortLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridColumnSortText", column.EffectiveAccessibleName!);

    private string GetFilterOperatorLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridFilterOperatorText", column.EffectiveAccessibleName!);

    private string GetFilterValueLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridFilterValueText", column.EffectiveAccessibleName!);

    private string GetApplyFilterLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridApplyFilterText", column.EffectiveAccessibleName!);

    private string GetClearFilterLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridClearColumnFilterText", column.EffectiveAccessibleName!);

    private string GetClearFilterLabel(string columnKey) =>
        Localize("DataGridClearColumnFilterText", FindColumn(columnKey)?.EffectiveAccessibleName ?? columnKey);

    private BzsDataGridColumn<TItem>? FindColumn(string columnKey) =>
        Columns.FirstOrDefault(column => string.Equals(column.EffectiveKey, columnKey, StringComparison.Ordinal));

    private bool IsSort(BzsDataGridColumn<TItem> column, BzsDataGridSortDirection direction) =>
        GetSortDirection(column) == direction;

    private BzsDataGridFilter? GetFilter(BzsDataGridColumn<TItem> column) =>
        Filters.FirstOrDefault(filter => string.Equals(
            filter.ColumnKey,
            column.EffectiveKey,
            StringComparison.Ordinal));

    private string GetFilterDraft(BzsDataGridColumn<TItem> column) =>
        _filterDrafts.GetValueOrDefault(column.EffectiveKey, string.Empty);

    private int GetFilterOperatorValue(BzsDataGridColumn<TItem> column) =>
        _filterOperatorDrafts.GetValueOrDefault(column.EffectiveKey);

    private bool IsFilterOperatorSelected(BzsDataGridColumn<TItem> column, int value) =>
        GetFilterOperatorValue(column) == value;

    private bool IsFilterDraftSelected(BzsDataGridColumn<TItem> column, string value) =>
        string.Equals(GetFilterDraft(column), value, StringComparison.Ordinal);

    private void SetFilterDraft(BzsDataGridColumn<TItem> column, string? value) =>
        _filterDrafts[column.EffectiveKey] = value ?? string.Empty;

    private void SetFilterOperator(BzsDataGridColumn<TItem> column, int value) =>
        _filterOperatorDrafts[column.EffectiveKey] = value;

    private IReadOnlyList<FilterOption> GetComparisonFilterOptions() =>
    [
        new((int)BzsDataGridComparisonOperator.Equals, EffectiveEqualsText),
        new((int)BzsDataGridComparisonOperator.NotEquals, EffectiveNotEqualsText),
        new((int)BzsDataGridComparisonOperator.LessThan, EffectiveLessThanText),
        new((int)BzsDataGridComparisonOperator.LessThanOrEqual, EffectiveLessThanOrEqualText),
        new((int)BzsDataGridComparisonOperator.GreaterThan, EffectiveGreaterThanText),
        new((int)BzsDataGridComparisonOperator.GreaterThanOrEqual, EffectiveGreaterThanOrEqualText),
    ];

    private IReadOnlyList<FilterOption> GetTextFilterOptions() =>
    [
        new((int)BzsDataGridTextOperator.Contains, EffectiveContainsText),
        new((int)BzsDataGridTextOperator.NotContains, EffectiveNotContainsText),
        new((int)BzsDataGridTextOperator.StartsWith, EffectiveStartsWithText),
        new((int)BzsDataGridTextOperator.EndsWith, EffectiveEndsWithText),
        new((int)BzsDataGridTextOperator.Equals, EffectiveEqualsText),
        new((int)BzsDataGridTextOperator.IsEmpty, EffectiveIsEmptyText),
        new((int)BzsDataGridTextOperator.IsNotEmpty, EffectiveIsNotEmptyText),
    ];

    private bool TextFilterDraftRequiresValue(BzsDataGridColumn<TItem> column)
    {
        var @operator = (BzsDataGridTextOperator)GetFilterOperatorValue(column);
        return !Enum.IsDefined(@operator) || BzsDataGridTextFilter.RequiresValue(@operator);
    }

    private IReadOnlyList<string> GetChoiceDraft(BzsDataGridColumn<TItem> column)
    {
        var draft = GetFilterDraft(column);
        return draft.Length == 0
            ? Array.Empty<string>()
            : draft.Split('\u001f', StringSplitOptions.RemoveEmptyEntries);
    }

    private bool IsChoiceSelected(BzsDataGridColumn<TItem> column, string choice)
    {
        foreach (var value in GetChoiceDraft(column))
        {
            if (string.Equals(value, choice, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private void ToggleChoiceDraft(BzsDataGridColumn<TItem> column, string choice)
    {
        var selected = new List<string>(GetChoiceDraft(column));
        var index = selected.FindIndex(value => string.Equals(value, choice, StringComparison.Ordinal));
        if (index >= 0)
        {
            selected.RemoveAt(index);
        }
        else
        {
            selected.Add(choice);
        }

        _filterDrafts[column.EffectiveKey] = string.Join('\u001f', selected);
    }

    private Task HandleFilterKeyDownAsync(
        BzsDataGridColumn<TItem> column,
        KeyboardEventArgs args) =>
        args.Key == "Enter" ? RequestApplyFilterAsync(column) : Task.CompletedTask;

    private async Task RequestApplyFilterAsync(BzsDataGridColumn<TItem> column)
    {
        var draft = GetFilterDraft(column);
        if (column.FilterKind == BzsDataGridFilterKind.Text && !TextFilterDraftRequiresValue(column))
        {
            var @operator = (BzsDataGridTextOperator)GetFilterOperatorValue(column);
            await RequestFilterChangeAsync(
                column.EffectiveKey,
                new BzsDataGridTextFilter(column.EffectiveKey, @operator));
            _openColumnMenuKey = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(draft))
        {
            await RequestFilterChangeAsync(column.EffectiveKey, null);
            _openColumnMenuKey = null;
            return;
        }

        BzsDataGridFilter? requested = column.FilterKind switch
        {
            BzsDataGridFilterKind.Text =>
                CreateTextFilter(column, draft),
            BzsDataGridFilterKind.Number when decimal.TryParse(
                draft,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) =>
                CreateNumberFilter(column, number),
            BzsDataGridFilterKind.Date when DateOnly.TryParseExact(
                draft,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date) =>
                CreateDateFilter(column, date),
            BzsDataGridFilterKind.Boolean when bool.TryParse(draft, out var boolean) =>
                new BzsDataGridBooleanFilter(column.EffectiveKey, boolean),
            BzsDataGridFilterKind.Choice =>
                new BzsDataGridChoiceFilter(column.EffectiveKey, draft.Split('\u001f', StringSplitOptions.RemoveEmptyEntries)),
            _ => GetFilter(column),
        };

        await RequestFilterChangeAsync(column.EffectiveKey, requested);
        _openColumnMenuKey = null;
    }

    private async Task RequestClearFilterAsync(BzsDataGridColumn<TItem> column)
    {
        await RequestFilterChangeAsync(column.EffectiveKey, null);
        _openColumnMenuKey = null;
    }

    private async Task RequestClearFilterAsync(string columnKey)
    {
        await RequestFilterChangeAsync(columnKey, null);
        _openColumnMenuKey = null;
    }

    private async Task RequestClearAllFiltersAsync()
    {
        if (Filters.Count == 0)
        {
            return;
        }

        await RequestStateWithPageResetAsync(
            () => FiltersChanged.InvokeAsync(Array.Empty<BzsDataGridFilter>()));
        _openColumnMenuKey = null;
    }

    private string GetFilterSummary(string columnKey, BzsDataGridFilter filter)
    {
        var column = FindColumn(columnKey);
        return Localize(
            "DataGridFilterSummaryText",
            column?.EffectiveAccessibleName ?? columnKey,
            GetFilterOperatorText(filter),
            FormatFilterSummaryValue(filter));
    }

    private string GetFilterOperatorText(BzsDataGridFilter filter) => filter switch
    {
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.Contains } => EffectiveContainsText,
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.NotContains } => EffectiveNotContainsText,
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.StartsWith } => EffectiveStartsWithText,
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.EndsWith } => EffectiveEndsWithText,
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.IsEmpty } => EffectiveIsEmptyText,
        BzsDataGridTextFilter { Operator: BzsDataGridTextOperator.IsNotEmpty } => EffectiveIsNotEmptyText,
        BzsDataGridTextFilter => EffectiveEqualsText,
        BzsDataGridNumberFilter { Operator: BzsDataGridComparisonOperator.NotEquals }
            or BzsDataGridDateFilter { Operator: BzsDataGridComparisonOperator.NotEquals } => EffectiveNotEqualsText,
        BzsDataGridNumberFilter { Operator: BzsDataGridComparisonOperator.LessThan }
            or BzsDataGridDateFilter { Operator: BzsDataGridComparisonOperator.LessThan } => EffectiveLessThanText,
        BzsDataGridNumberFilter { Operator: BzsDataGridComparisonOperator.LessThanOrEqual }
            or BzsDataGridDateFilter { Operator: BzsDataGridComparisonOperator.LessThanOrEqual } => EffectiveLessThanOrEqualText,
        BzsDataGridNumberFilter { Operator: BzsDataGridComparisonOperator.GreaterThan }
            or BzsDataGridDateFilter { Operator: BzsDataGridComparisonOperator.GreaterThan } => EffectiveGreaterThanText,
        BzsDataGridNumberFilter { Operator: BzsDataGridComparisonOperator.GreaterThanOrEqual }
            or BzsDataGridDateFilter { Operator: BzsDataGridComparisonOperator.GreaterThanOrEqual } => EffectiveGreaterThanOrEqualText,
        BzsDataGridBooleanFilter => EffectiveEqualsText,
        BzsDataGridChoiceFilter => EffectiveAnyOfText,
        _ => EffectiveEqualsText,
    };

    private BzsDataGridTextFilter CreateTextFilter(
        BzsDataGridColumn<TItem> column,
        string value)
    {
        var @operator = (BzsDataGridTextOperator)GetFilterOperatorValue(column);
        if (!Enum.IsDefined(@operator))
        {
            @operator = BzsDataGridTextOperator.Contains;
        }
        var caseSensitive = GetFilter(column) is BzsDataGridTextFilter text && text.CaseSensitive;
        return new BzsDataGridTextFilter(column.EffectiveKey, value, @operator, caseSensitive);
    }

    private BzsDataGridNumberFilter CreateNumberFilter(
        BzsDataGridColumn<TItem> column,
        decimal value) =>
        new(column.EffectiveKey, value, GetComparisonOperator(column));

    private BzsDataGridDateFilter CreateDateFilter(
        BzsDataGridColumn<TItem> column,
        DateOnly value) =>
        new(column.EffectiveKey, value, GetComparisonOperator(column));

    private BzsDataGridComparisonOperator GetComparisonOperator(BzsDataGridColumn<TItem> column)
    {
        var @operator = (BzsDataGridComparisonOperator)GetFilterOperatorValue(column);
        return Enum.IsDefined(@operator) ? @operator : BzsDataGridComparisonOperator.Equals;
    }

    private Task RequestFilterChangeAsync(string columnKey, BzsDataGridFilter? requested)
    {
        var filters = Filters
            .Where(filter => !string.Equals(filter.ColumnKey, columnKey, StringComparison.Ordinal))
            .Append(requested)
            .Where(static filter => filter is not null)
            .Cast<BzsDataGridFilter>()
            .OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal)
            .ToArray();
        if (BzsDataGridRequestEquality.FilterListsEqual(Filters, filters))
        {
            return Task.CompletedTask;
        }

        IReadOnlyList<BzsDataGridFilter> snapshot = Array.AsReadOnly(filters);
        return RequestStateWithPageResetAsync(() => FiltersChanged.InvokeAsync(snapshot));
    }

    private async Task RequestStateWithPageResetAsync(Func<Task> requestStateChange)
    {
        _interactionBatchDepth++;
        try
        {
            if (Page != 1)
            {
                await PageChanged.InvokeAsync(1);
            }
            await requestStateChange();
        }
        finally
        {
            _interactionBatchDepth--;
            if (_interactionBatchDepth == 0 && !_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private Task RetryProviderAsync()
    {
        if (_disposed || Provider is null)
        {
            return Task.CompletedTask;
        }

        _providerSession?.Retry();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task RequestSearchTextAsync(ChangeEventArgs args)
    {
        var requested = Normalize(args.Value?.ToString());
        if (string.Equals(requested, EffectiveSearchText, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return RequestStateWithPageResetAsync(() => SearchTextChanged.InvokeAsync(requested));
    }

    private Task ClearSearchTextAsync() => EffectiveSearchText is null
        ? Task.CompletedTask
        : RequestStateWithPageResetAsync(() => SearchTextChanged.InvokeAsync(null));

    private Task RequestColumnVisibilityAsync(BzsDataGridColumn<TItem> column, bool visible)
    {
        if (!column.Hideable)
        {
            return Task.CompletedTask;
        }

        var hidden = new List<string>(HiddenColumnKeys.Count + 1);
        foreach (var key in HiddenColumnKeys)
        {
            if (!string.Equals(key, column.EffectiveKey, StringComparison.Ordinal))
            {
                hidden.Add(key);
            }
        }

        if (!visible)
        {
            hidden.Add(column.EffectiveKey);
        }

        if (hidden.Count == HiddenColumnKeys.Count && visible)
        {
            return Task.CompletedTask;
        }

        IReadOnlyList<string> snapshot = Array.AsReadOnly(hidden.ToArray());
        return HiddenColumnKeysChanged.InvokeAsync(snapshot);
    }

    private Task ToggleColumnVisibilityAsync(BzsDataGridColumn<TItem> column) =>
        RequestColumnVisibilityAsync(column, !IsColumnVisible(column));

    private bool IsRowExpanded(TItem item)
    {
        if (DetailTemplate is null)
        {
            return false;
        }

        if (ItemKey is null)
        {
            return DetailRowsExpanded;
        }

        var contains = _expandedItemKeys?.Contains(GetItemKey(item)) == true;
        return DetailRowsExpanded ? !contains : contains;
    }

    private Task ToggleRowExpansionAsync(TItem item)
    {
        if (DetailTemplate is null || ItemKey is null)
        {
            return Task.CompletedTask;
        }

        var key = GetItemKey(item);
        var keys = new List<object?>(ExpandedItemKeys.Count + 1);
        var removed = false;
        foreach (var existing in ExpandedItemKeys)
        {
            if (KeyComparer.Equals(existing, key))
            {
                removed = true;
                continue;
            }

            keys.Add(existing);
        }

        if (!removed)
        {
            keys.Add(key);
        }

        IReadOnlyList<object?> snapshot = Array.AsReadOnly(keys.ToArray());
        return ExpandedItemKeysChanged.InvokeAsync(snapshot);
    }

    private Task RequestRowActivationAsync(TItem item, int rowIndex) =>
        RowClickable && RowClick.HasDelegate
            ? RowClick.InvokeAsync(new BzsDataGridRowEventArgs<TItem>(item, rowIndex))
            : Task.CompletedTask;

    private Task HandleRowKeyDownAsync(TItem item, int rowIndex, KeyboardEventArgs args) =>
        RowClickable && args.Key is "Enter" or " " or "Spacebar"
            ? RequestRowActivationAsync(item, rowIndex)
            : Task.CompletedTask;

    private string GetRowClass(TItem item)
    {
        var custom = Normalize(RowClass?.Invoke(item));
        return custom is null ? "bzs-data-grid__row" : $"bzs-data-grid__row {custom}";
    }

    private string GetExpandRowLabel(TItem item, int rowIndex) => IsRowExpanded(item)
        ? Localize("DataGridCollapseRowText", GetRowOrdinal(rowIndex))
        : Localize("DataGridExpandRowText", GetRowOrdinal(rowIndex));

    private long GetRowOrdinal(int rowIndex) =>
        ((long)(Provider is null ? ClientPage : AcceptedPage) - 1)
            * (Provider is null ? PageSize : AcceptedPageSize)
        + rowIndex
        + 1;

    private IReadOnlyList<TItem> FooterItems => Provider is null ? SortedItems : SourceItems;

    private async ValueTask SynchronizeColumnLayoutAsync()
    {
        var fingerprint = CreateColumnLayoutFingerprint();
        if (string.Equals(_columnLayoutFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _columnLayoutFingerprint = fingerprint;
        _selfReference ??= DotNetObjectReference.Create(this);
        await GetInterop().TryInvokeVoidAsync(
            "applyColumnLayout",
            _tableReference,
            ResizableColumns,
            _selfReference);
    }

    private string CreateColumnLayoutFingerprint()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(ResizableColumns ? '1' : '0').Append(LeadingColumnCount);
        foreach (var column in VisibleColumns)
        {
            builder
                .Append('\u001f')
                .Append(column.EffectiveKey)
                .Append('\u001e')
                .Append(column.EffectiveWidth)
                .Append('\u001e')
                .Append(column.EffectiveMinWidth)
                .Append('\u001e')
                .Append(column.Resizable ? '1' : '0')
                .Append(column.StickyName);
        }
        return builder.ToString();
    }

    /// <summary>Receives a browser-owned column width after a pointer or keyboard resize.</summary>
    /// <param name="columnKey">The unique key of the resized column.</param>
    /// <param name="width">The committed width in CSS pixels.</param>
    [JSInvokable]
    public Task ReportColumnResizedAsync(string columnKey, double width)
    {
        if (_disposed || string.IsNullOrWhiteSpace(columnKey) || width <= 0)
        {
            return Task.CompletedTask;
        }

        return ColumnResized.InvokeAsync(new BzsDataGridColumnResizeEventArgs(columnKey.Trim(), width));
    }

    private bool IsSelected(TItem item) => SelectionMode switch
    {
        BzsDataGridSelectionMode.Single => SelectedItem is not null && KeysEqual(SelectedItem, item),
        BzsDataGridSelectionMode.Multiple => IsSelectedMultiple(item),
        _ => false,
    };

    private bool IsSelectedMultiple(TItem item) =>
        _selectedItemKeys?.Contains(GetItemKey(item)) == true;

    private bool ShowCurrentPageSelectAll =>
        ShowSelectAll && SelectionMode == BzsDataGridSelectionMode.Multiple;

    private bool AreAllCurrentRowsSelected(IReadOnlyList<TItem> rows) =>
        rows.Count > 0 && rows.All(IsSelectedMultiple);

    private string GetCurrentPageSelectAllState(IReadOnlyList<TItem> rows)
    {
        if (rows.Count == 0 || !rows.Any(IsSelectedMultiple))
        {
            return "false";
        }

        return AreAllCurrentRowsSelected(rows) ? "true" : "mixed";
    }

    private ValueTask<bool> SynchronizeSelectAllAsync()
    {
        var rows = Rows;
        var allSelected = AreAllCurrentRowsSelected(rows);
        var indeterminate = !allSelected && rows.Any(IsSelectedMultiple);
        return GetInterop().TryInvokeVoidAsync(
            "synchronize",
            _selectAllReference,
            allSelected,
            indeterminate);
    }

    private BzsJsModule GetInterop() => _interop ??= new BzsJsModule(
        JsRuntime,
        ModulePath,
        options: new BzsJsModuleOptions(TreatInvalidOperationDuringImportAsTransient: true));

    private bool KeysEqual(TItem left, TItem right) =>
        KeyComparer.Equals(GetItemKey(left), GetItemKey(right));

    private IEqualityComparer<object?> KeyComparer =>
        ItemKeyComparer ?? EqualityComparer<object?>.Default;

    private object GetItemKey(TItem item) => ItemKey?.Invoke(item)
        ?? throw new InvalidOperationException("BzsDataGrid ItemKey returned null.");

    private object GetRenderKey(TItem item, int rowIndex) => ItemKey is null
        ? rowIndex
        : new RendererKey(GetItemKey(item), KeyComparer);

    private void BeginColumnComposition() => _nextColumnCompositionOrder = 0;

    private string GetSelectionLabel(TItem item, int rowIndex)
    {
        return Normalize(RowAccessibleName?.Invoke(item))
            ?? Localize("DataGridSelectRowText", GetRowOrdinal(rowIndex));
    }

    private BzsIconData? GetSortIcon(BzsDataGridColumn<TItem> column)
    {
        var direction = GetSortDirection(column);
        if (direction is null)
        {
            return null;
        }

        return direction == BzsDataGridSortDirection.Ascending
            ? BzsIcons.ChevronUp
            : BzsIcons.ChevronDown;
    }

    private BzsDataGridSortDirection? GetSortDirection(BzsDataGridColumn<TItem> column)
    {
        foreach (var sort in DisplayedSorts)
        {
            if (string.Equals(sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal))
            {
                return sort.Direction;
            }
        }
        return null;
    }

    /// <summary>Gets the one-based precedence of a column within a multi-sort, or zero when unsorted.</summary>
    private int GetSortPrecedence(BzsDataGridColumn<TItem> column)
    {
        var sorts = DisplayedSorts;
        if (sorts.Count < 2)
        {
            return 0;
        }

        for (var index = 0; index < sorts.Count; index++)
        {
            if (string.Equals(sorts[index].ColumnKey, column.EffectiveKey, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }
        return 0;
    }

    private void ValidatePage()
    {
        // Client filtering can narrow the result below the controlled page, which is a
        // transient state the consumer resets on its own; only the unfiltered range is a
        // configuration error.
        var pageCount = SourcePageCount;
        if (Page < 1 || pageCount == 0 && Page != 1 || pageCount > 0 && Page > pageCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Page),
                Page,
                "Page must be one when there are no items and within the available one-based range otherwise.");
        }
    }

    private void ValidateItemKeys(IReadOnlyList<TItem>? items = null)
    {
        if (ItemKey is null)
        {
            return;
        }

        var comparer = ItemKeyComparer ?? EqualityComparer<object?>.Default;
        var keys = new HashSet<object?>(comparer);
        foreach (var item in items ?? SourceItems)
        {
            var key = ItemKey(item)
                ?? throw new InvalidOperationException("BzsDataGrid ItemKey returned null.");
            if (!keys.Add(key))
            {
                throw new InvalidOperationException($"BzsDataGrid ItemKey returned the duplicate key '{key}'.");
            }
        }
    }

    private HashSet<object?>? CreateSelectedItemKeys()
    {
        if (SelectionMode != BzsDataGridSelectionMode.Multiple)
        {
            return null;
        }

        var keys = new HashSet<object?>(KeyComparer);
        foreach (var item in SelectedItems)
        {
            keys.Add(GetItemKey(item));
        }
        return keys;
    }

    private HashSet<object?>? CreateExpandedItemKeys()
    {
        if (DetailTemplate is null || ItemKey is null)
        {
            return null;
        }

        var keys = new HashSet<object?>(KeyComparer);
        foreach (var key in ExpandedItemKeys)
        {
            if (key is not null)
            {
                keys.Add(key);
            }
        }
        return keys;
    }

    private BzsDataGridRequest CreateProviderRequest()
    {
        var sorts = EffectiveSorts;
        foreach (var sort in sorts)
        {
            var column = _columns.FirstOrDefault(candidate => string.Equals(
                candidate.EffectiveKey,
                sort.ColumnKey,
                StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"The DataGrid sort column '{sort.ColumnKey}' is not registered.");
            if (!column.Sortable)
            {
                throw new InvalidOperationException($"The DataGrid column '{sort.ColumnKey}' is not sortable.");
            }
        }

        var filters = Filters
            .OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal)
            .ToArray();
        foreach (var filter in filters)
        {
            var column = _columns.FirstOrDefault(candidate => string.Equals(
                candidate.EffectiveKey,
                filter.ColumnKey,
                StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"The DataGrid filter column '{filter.ColumnKey}' is not registered.");
            if (!FilterMatchesKind(filter, column.FilterKind))
            {
                throw new InvalidOperationException(
                    $"The DataGrid filter for column '{filter.ColumnKey}' does not match its FilterKind.");
            }
        }

        return new BzsDataGridRequest(
            Page,
            PageSize,
            sorts.Count == 0 ? null : sorts[0],
            filters,
            sorts,
            EffectiveSearchText);
    }

    private void SynchronizeFilterDrafts()
    {
        var current = Filters.ToDictionary(static filter => filter.ColumnKey, StringComparer.Ordinal);
        foreach (var observed in _observedFilters.ToArray())
        {
            if (current.ContainsKey(observed.Key))
            {
                continue;
            }

            _filterDrafts[observed.Key] = string.Empty;
            _filterOperatorDrafts.Remove(observed.Key);
            _observedFilters.Remove(observed.Key);
        }

        foreach (var filter in current.Values)
        {
            if (_observedFilters.TryGetValue(filter.ColumnKey, out var observed)
                && BzsDataGridRequestEquality.FiltersEqual(observed, filter))
            {
                continue;
            }

            _filterDrafts[filter.ColumnKey] = FormatFilterValue(filter);
            _filterOperatorDrafts[filter.ColumnKey] = GetFilterOperator(filter);
            _observedFilters[filter.ColumnKey] = filter;
        }
    }

    private static string FormatFilterValue(BzsDataGridFilter filter) => filter switch
    {
        BzsDataGridTextFilter text => text.Value,
        BzsDataGridNumberFilter number => number.Value.ToString(CultureInfo.InvariantCulture),
        BzsDataGridDateFilter date => date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        BzsDataGridBooleanFilter boolean => boolean.Value ? "true" : "false",
        BzsDataGridChoiceFilter choice => string.Join(", ", choice.Values),
        _ => string.Empty,
    };

    private string FormatFilterSummaryValue(BzsDataGridFilter filter) => filter switch
    {
        BzsDataGridBooleanFilter { Value: true } => EffectiveTrueText,
        BzsDataGridBooleanFilter => EffectiveFalseText,
        _ => FormatFilterValue(filter),
    };

    private static int GetFilterOperator(BzsDataGridFilter filter) => filter switch
    {
        BzsDataGridTextFilter text => (int)text.Operator,
        BzsDataGridNumberFilter number => (int)number.Operator,
        BzsDataGridDateFilter date => (int)date.Operator,
        _ => 0,
    };

    private static bool FilterMatchesKind(BzsDataGridFilter filter, BzsDataGridFilterKind kind) =>
        filter switch
        {
            BzsDataGridTextFilter => kind == BzsDataGridFilterKind.Text,
            BzsDataGridNumberFilter => kind == BzsDataGridFilterKind.Number,
            BzsDataGridDateFilter => kind == BzsDataGridFilterKind.Date,
            BzsDataGridBooleanFilter => kind == BzsDataGridFilterKind.Boolean,
            BzsDataGridChoiceFilter => kind == BzsDataGridFilterKind.Choice,
            _ => false,
        };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _providerSession?.Dispose();
        _providerSession = null;
        _sessionProvider = null;
        if (_interop is not null)
        {
            if (_columnLayoutFingerprint is not null)
            {
                await _interop.TryInvokeVoidAsync("disposeColumnLayout", _tableReference);
            }

            await _interop.DisposeAsync();
        }

        _selfReference?.Dispose();
        _selfReference = null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string Localize(string key, params object[] arguments) =>
        Localizer[key, arguments].Value;

    private sealed record FilterOption(int Value, string Label);

    private sealed record ColumnState(
        string Key,
        string? Title,
        string? AccessibleName,
        bool Sortable,
        BzsDataGridFilterKind FilterKind,
        string? Format,
        Func<TItem, object?>? ValueSelector,
        RenderFragment<TItem>? CellTemplate,
        RenderFragment? HeaderTemplate,
        Comparison<TItem>? SortComparison,
        string? Id,
        string? CssClass,
        string? Style,
        string? AttributesFingerprint)
    {
        internal static ColumnState Create(BzsDataGridColumn<TItem> column) => new(
            column.EffectiveKey,
            column.EffectiveTitle,
            column.EffectiveAccessibleName,
            column.Sortable,
            column.FilterKind,
            column.Format,
            column.ValueSelector,
            column.CellTemplate,
            column.HeaderTemplate,
            column.SortComparison,
            column.Id,
            column.Class,
            column.Style,
            CreateAttributesFingerprint(column.AdditionalAttributes));

        private static string? CreateAttributesFingerprint(IReadOnlyDictionary<string, object>? attributes) =>
            attributes is null
                ? null
                : string.Join(
                    "\u001f",
                    attributes
                        .OrderBy(static attribute => attribute.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(static attribute => $"{attribute.Key}={attribute.Value}"));
    }

    private sealed class RendererKey
    {
        private readonly object _key;
        private readonly IEqualityComparer<object?> _comparer;

        internal RendererKey(object key, IEqualityComparer<object?> comparer)
        {
            _key = key;
            _comparer = comparer;
        }

        public override bool Equals(object? obj) =>
            obj is RendererKey other
            && ReferenceEquals(_comparer, other._comparer)
            && _comparer.Equals(_key, other._key);

        public override int GetHashCode() => _comparer.GetHashCode(_key);
    }

    private sealed class DetailKey
    {
        private readonly object _key;
        private readonly IEqualityComparer<object?> _comparer;

        internal DetailKey(object key, IEqualityComparer<object?> comparer)
        {
            _key = key;
            _comparer = comparer;
        }

        public override bool Equals(object? obj) =>
            obj is DetailKey other
            && ReferenceEquals(_comparer, other._comparer)
            && _comparer.Equals(_key, other._key);

        public override int GetHashCode() => _comparer.GetHashCode(_key) ^ 0x5f5f;
    }
}
