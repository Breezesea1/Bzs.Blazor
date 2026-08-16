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
    private static readonly IReadOnlyList<int> DefaultPageSizeOptions =
        Array.AsReadOnly(new[] { 10, 25, 50 });
    private readonly string _instanceId = $"bzs-data-grid-{Guid.NewGuid():N}";
    private readonly List<BzsDataGridColumn<TItem>> _columns = [];
    private readonly Dictionary<BzsDataGridColumn<TItem>, ColumnState> _columnStates = [];
    private readonly Dictionary<string, string> _filterDrafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _filterOperatorDrafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BzsDataGridFilter> _observedFilters = new(StringComparer.Ordinal);
    private HashSet<object?>? _selectedItemKeys;
    private BzsDataGridRequestCoordinator<TItem>? _requestCoordinator;
    private IBzsDataGridProvider<TItem>? _coordinatorProvider;
    private ProviderRefresh? _pendingRefresh;
    private ProviderRefresh? _activeRefresh;
    private BzsDataGridRequest? _lastStartedRequest;
    private BzsDataGridRequest? _acceptedRequest;
    private BzsDataGridResult<TItem>? _acceptedResult;
    private Exception? _providerError;
    private string? _openColumnMenuKey;
    private int _nextColumnCompositionOrder;
    private int _interactionBatchDepth;
    private bool _providerLoading;
    private bool _disposed;

    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

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
        ProviderRefresh? refresh = null;
        await InvokeAsync(() => { refresh = QueueRefresh(); });
        if (refresh is not null)
        {
            await refresh.Completion.Task;
        }
    }

    private ProviderRefresh? QueueRefresh()
    {
        if (_disposed)
        {
            return null;
        }

        if (Provider is null)
        {
            throw new InvalidOperationException("BzsDataGrid RefreshAsync requires Provider mode.");
        }

        var pendingRefresh = _pendingRefresh;
        _pendingRefresh = null;
        SupersedeRefresh(pendingRefresh);
        SupersedeRefresh(_activeRefresh);
        var refresh = new ProviderRefresh(Provider, CreateProviderRequest());
        if (RendererInfo.IsInteractive)
        {
            StartRefresh(refresh);
            return refresh;
        }

        _pendingRefresh = refresh;
        StateHasChanged();
        return refresh;
    }

    internal IReadOnlyList<BzsDataGridColumn<TItem>> Columns => _columns;

    private IReadOnlyList<TItem> SourceItems => Provider is null
        ? Items ?? Array.Empty<TItem>()
        : _acceptedResult?.Items ?? Array.Empty<TItem>();

    private IReadOnlyList<TItem> Rows
    {
        get
        {
            if (Provider is not null)
            {
                return SourceItems;
            }

            var column = GetSortColumn();
            return BzsDataGridOperations.Apply(
                SourceItems,
                column is null ? null : column.Compare,
                Sort?.Direction,
                Page,
                PageSize);
        }
    }

    private int PageCount => SourceItems.Count == 0
        ? 0
        : (int)((SourceItems.Count + (long)PageSize - 1) / PageSize);

    private bool EffectiveLoading => Provider is null
        ? Loading
        : _providerLoading || _acceptedResult is null && _providerError is null;

    private Exception? EffectiveError => Provider is null ? Error : _providerError;

    private bool HasAcceptedProviderResult => Provider is not null && _acceptedResult is not null;

    private bool ControlsDisabled => Provider is null && (Loading || Error is not null);

    private int? AcceptedTotalCount => _acceptedResult?.TotalCount;

    private int AcceptedPage => _acceptedRequest?.Page ?? Page;

    private int AcceptedPageCount => AcceptedTotalCount is not int totalCount || totalCount == 0
        ? 0
        : (int)((totalCount + (long)(_acceptedRequest?.PageSize ?? PageSize) - 1)
            / (_acceptedRequest?.PageSize ?? PageSize));

    private bool AcceptedHasNextPage => _acceptedResult?.HasNextPage == true;

    private BzsDataGridSort? DisplayedSort => Provider is null ? Sort : _acceptedRequest?.Sort;

    private int ColumnSpan => Math.Max(1, Columns.Count + (SelectionMode == BzsDataGridSelectionMode.None ? 0 : 1));

    private string PageSizeInputId => $"{_instanceId}-page-size";

    private string SelectionInputName => $"{_instanceId}-selection";

    private string? TableAccessibleName => CaptionContent is not null || !string.IsNullOrWhiteSpace(Caption)
        ? null
        : Normalize(AccessibleName) ?? Localize("DataGridAccessibleName");

    private string EffectiveLoadingText => Normalize(LoadingText) ?? Localize("DataGridLoadingText");
    private string EffectiveEmptyText => Normalize(EmptyText) ?? Localize("DataGridEmptyText");
    private string EffectiveErrorText => Normalize(ErrorText) ?? Localize("DataGridErrorText");
    private string EffectiveRetryText => Normalize(RetryText) ?? Localize("DataGridRetryText");
    private string EffectivePageSizeText => Normalize(PageSizeText) ?? Localize("DataGridPageSizeText");
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
    private string EffectiveContainsText => Localize("DataGridContainsText");
    private string EffectiveStartsWithText => Localize("DataGridStartsWithText");
    private string EffectiveEndsWithText => Localize("DataGridEndsWithText");
    private string EffectiveEqualsText => Localize("DataGridEqualsText");
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

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-data-grid"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-data-grid"] = "true",
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
            ValidatePage();
        }
        else if (Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Page), Page, "A provider page must be positive.");
        }

        SynchronizeProvider();
        ValidateItemKeys();
        _selectedItemKeys = CreateSelectedItemKeys();
        SynchronizeFilterDrafts();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed
            || Provider is null
            || !RendererInfo.IsInteractive
            || _interactionBatchDepth > 0)
        {
            return;
        }

        var request = CreateProviderRequest();
        if (_pendingRefresh is { } pendingRefresh)
        {
            _pendingRefresh = null;
            if (!ReferenceEquals(pendingRefresh.Provider, Provider)
                || !RequestsEqual(pendingRefresh.Request, request))
            {
                SupersedeRefresh(pendingRefresh);
            }
            else
            {
                StartRefresh(pendingRefresh);
                return;
            }
        }

        if (RequestsEqual(_lastStartedRequest, request))
        {
            return;
        }

        SupersedeRefresh(_activeRefresh);
        _lastStartedRequest = request;
        await LoadProviderAsync(request);
    }

    private void SynchronizeProvider()
    {
        if (ReferenceEquals(_coordinatorProvider, Provider))
        {
            return;
        }

        SupersedeRefresh(_pendingRefresh);
        SupersedeRefresh(_activeRefresh);
        _pendingRefresh = null;
        _activeRefresh = null;
        _requestCoordinator?.Dispose();
        _requestCoordinator = null;
        _coordinatorProvider = Provider;
        _lastStartedRequest = null;
        _acceptedRequest = null;
        _acceptedResult = null;
        _providerError = null;
        _providerLoading = false;
        if (Provider is not null)
        {
            _requestCoordinator = new BzsDataGridRequestCoordinator<TItem>(Provider);
        }
    }

    private async Task LoadProviderAsync(BzsDataGridRequest request)
    {
        var coordinator = _requestCoordinator;
        if (coordinator is null)
        {
            return;
        }

        _providerLoading = true;
        _providerError = null;
        StateHasChanged();
        var outcome = await coordinator.LoadAsync(request);
        if (_disposed || !ReferenceEquals(_requestCoordinator, coordinator) || !outcome.IsCurrent)
        {
            return;
        }

        _providerLoading = false;
        if (outcome.Error is not null)
        {
            _providerError = outcome.Error;
            if (!_disposed)
            {
                StateHasChanged();
            }

            await ProviderFailed.InvokeAsync(outcome.Error);
            return;
        }

        var result = outcome.Result!;
        try
        {
            ValidateProviderResult(request, result);
            ValidateItemKeys(result.Items);
        }
        catch (InvalidOperationException exception)
        {
            _providerError = exception;
            if (!_disposed)
            {
                StateHasChanged();
            }

            await ProviderFailed.InvokeAsync(exception);
            return;
        }
        if (result.TotalCount is int totalCount)
        {
            var pageCount = totalCount == 0
                ? 0
                : (int)((totalCount + (long)request.PageSize - 1) / request.PageSize);
            var lastValidPage = Math.Max(1, pageCount);
            if (request.Page > lastValidPage)
            {
                StateHasChanged();
                await PageChanged.InvokeAsync(lastValidPage);
                return;
            }
        }

        _acceptedRequest = request;
        _acceptedResult = result;
        _providerError = null;
        _selectedItemKeys = CreateSelectedItemKeys();
        StateHasChanged();
    }

    private async Task LoadRefreshAsync(ProviderRefresh refresh)
    {
        _activeRefresh = refresh;
        _lastStartedRequest = refresh.Request;
        try
        {
            await LoadProviderAsync(refresh.Request);
        }
        finally
        {
            CompleteRefresh(refresh);
        }
    }

    private async void StartRefresh(ProviderRefresh refresh)
    {
        try
        {
            await LoadRefreshAsync(refresh);
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    private static void ValidateProviderResult(
        BzsDataGridRequest request,
        BzsDataGridResult<TItem> result)
    {
        if (result.Items.Count > request.PageSize)
        {
            throw new InvalidOperationException("The DataGrid provider returned more items than the requested page size.");
        }

        if (result.TotalCount is not int totalCount)
        {
            return;
        }

        var offset = (request.Page - 1L) * request.PageSize;
        if (offset >= totalCount && totalCount > 0 && result.Items.Count != 0)
        {
            throw new InvalidOperationException("The DataGrid provider returned items beyond its known total.");
        }
        if (offset < totalCount && result.Items.Count > totalCount - offset)
        {
            throw new InvalidOperationException("The DataGrid provider returned more items than remain in its known total.");
        }
        if (totalCount == 0 && result.Items.Count != 0)
        {
            throw new InvalidOperationException("A DataGrid provider result with a zero total cannot contain items.");
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
        BzsDataGridSort? requested = Sort is null
            || !string.Equals(Sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal)
            ? new(column.EffectiveKey, BzsDataGridSortDirection.Ascending)
            : Sort.Direction == BzsDataGridSortDirection.Ascending
                ? new(column.EffectiveKey, BzsDataGridSortDirection.Descending)
                : null;
        return RequestSortChangeAsync(requested);
    }

    private Task RequestSpecificSortAsync(
        BzsDataGridColumn<TItem> column,
        BzsDataGridSortDirection direction) =>
        RequestSortChangeAsync(new BzsDataGridSort(column.EffectiveKey, direction));

    private Task RequestClearSortAsync() => RequestSortChangeAsync(null);

    private Task RequestSortChangeAsync(BzsDataGridSort? requested)
    {
        if (SortsEqual(Sort, requested))
        {
            return Task.CompletedTask;
        }

        return RequestStateWithPageResetAsync(() => SortChanged.InvokeAsync(requested));
    }

    private async Task RequestPageSizeAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var requested)
            || !PageSizeOptions.Contains(requested)
            || requested == PageSize)
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
                ? Provider is not null && _providerError is not null
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

    private string GetFilterOperatorLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridFilterOperatorText", column.EffectiveAccessibleName!);

    private string GetFilterValueLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridFilterValueText", column.EffectiveAccessibleName!);

    private string GetApplyFilterLabel(BzsDataGridColumn<TItem> column) =>
        Localize("DataGridApplyFilterText", column.EffectiveAccessibleName!);

    private BzsDataGridFilter? GetFilter(BzsDataGridColumn<TItem> column) =>
        Filters.FirstOrDefault(filter => string.Equals(
            filter.ColumnKey,
            column.EffectiveKey,
            StringComparison.Ordinal));

    private string GetFilterDraft(BzsDataGridColumn<TItem> column) =>
        _filterDrafts.GetValueOrDefault(column.EffectiveKey, string.Empty);

    private int GetFilterOperatorValue(BzsDataGridColumn<TItem> column) =>
        _filterOperatorDrafts.GetValueOrDefault(column.EffectiveKey);

    private void SetFilterDraft(BzsDataGridColumn<TItem> column, string? value) =>
        _filterDrafts[column.EffectiveKey] = value ?? string.Empty;

    private void SetFilterOperator(BzsDataGridColumn<TItem> column, string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            _filterOperatorDrafts[column.EffectiveKey] = parsed;
        }
    }

    private Task RequestApplyFilterAsync(BzsDataGridColumn<TItem> column)
    {
        var draft = GetFilterDraft(column);
        if (string.IsNullOrWhiteSpace(draft))
        {
            return RequestFilterChangeAsync(column.EffectiveKey, null);
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
            _ => GetFilter(column),
        };

        return RequestFilterChangeAsync(column.EffectiveKey, requested);
    }

    private Task RequestClearFilterAsync(BzsDataGridColumn<TItem> column)
    {
        return RequestFilterChangeAsync(column.EffectiveKey, null);
    }

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
        if (FilterListsEqual(Filters, filters))
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

        _lastStartedRequest = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private static void SupersedeRefresh(ProviderRefresh? refresh) =>
        refresh?.Completion.TrySetResult();

    private void CompleteRefresh(ProviderRefresh refresh)
    {
        if (ReferenceEquals(_activeRefresh, refresh))
        {
            _activeRefresh = null;
        }

        refresh.Completion.TrySetResult();
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
            ?? Localize(
                "DataGridSelectRowText",
                ((long)(Provider is null ? Page : AcceptedPage) - 1)
                    * (Provider is null ? PageSize : _acceptedRequest?.PageSize ?? PageSize)
                    + rowIndex
                    + 1);
    }

    private BzsIconData? GetSortIcon(BzsDataGridColumn<TItem> column)
    {
        var sort = DisplayedSort;
        if (sort is null || !string.Equals(sort.ColumnKey, column.EffectiveKey, StringComparison.Ordinal))
        {
            return null;
        }

        return sort.Direction == BzsDataGridSortDirection.Ascending
            ? BzsIcons.ChevronUp
            : BzsIcons.ChevronDown;
    }

    private void ValidatePage()
    {
        if (Page < 1 || PageCount == 0 && Page != 1 || PageCount > 0 && Page > PageCount)
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

    private BzsDataGridRequest CreateProviderRequest()
    {
        _ = GetSortColumn();
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

        return new BzsDataGridRequest(Page, PageSize, Sort, filters);
    }

    private void SynchronizeFilterDrafts()
    {
        if (Provider is null && Filters.Count != 0)
        {
            throw new InvalidOperationException("DataGrid filters require Provider mode.");
        }

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
                && FiltersEqual(observed, filter))
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
        _ => string.Empty,
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
            _ => false,
        };

    private static bool RequestsEqual(BzsDataGridRequest? left, BzsDataGridRequest right) =>
        left is not null
        && left.Page == right.Page
        && left.PageSize == right.PageSize
        && SortsEqual(left.Sort, right.Sort)
        && FilterListsEqual(left.Filters, right.Filters);

    private static bool SortsEqual(BzsDataGridSort? left, BzsDataGridSort? right) =>
        ReferenceEquals(left, right)
        || left is not null
            && right is not null
            && left.Direction == right.Direction
            && string.Equals(left.ColumnKey, right.ColumnKey, StringComparison.Ordinal);

    private static bool FilterListsEqual(
        IReadOnlyList<BzsDataGridFilter> left,
        IReadOnlyList<BzsDataGridFilter> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var orderedLeft = left.OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal).ToArray();
        var orderedRight = right.OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < orderedLeft.Length; index++)
        {
            if (!FiltersEqual(orderedLeft[index], orderedRight[index]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool FiltersEqual(BzsDataGridFilter left, BzsDataGridFilter right) =>
        string.Equals(left.ColumnKey, right.ColumnKey, StringComparison.Ordinal)
        && (left, right) switch
        {
            (BzsDataGridTextFilter first, BzsDataGridTextFilter second) =>
                first.Operator == second.Operator
                && first.CaseSensitive == second.CaseSensitive
                && string.Equals(first.Value, second.Value, StringComparison.Ordinal),
            (BzsDataGridNumberFilter first, BzsDataGridNumberFilter second) =>
                first.Operator == second.Operator && first.Value == second.Value,
            (BzsDataGridDateFilter first, BzsDataGridDateFilter second) =>
                first.Operator == second.Operator && first.Value == second.Value,
            (BzsDataGridBooleanFilter first, BzsDataGridBooleanFilter second) =>
                first.Value == second.Value,
            _ => false,
        };

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        SupersedeRefresh(_pendingRefresh);
        SupersedeRefresh(_activeRefresh);
        _pendingRefresh = null;
        _activeRefresh = null;
        _requestCoordinator?.Dispose();
        _requestCoordinator = null;
        _coordinatorProvider = null;
        return ValueTask.CompletedTask;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string Localize(string key, params object[] arguments) =>
        Localizer[key, arguments].Value;

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

    private sealed class ProviderRefresh(
        IBzsDataGridProvider<TItem> provider,
        BzsDataGridRequest request)
    {
        internal IBzsDataGridProvider<TItem> Provider { get; } = provider;

        internal BzsDataGridRequest Request { get; } = request;

        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
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
}
