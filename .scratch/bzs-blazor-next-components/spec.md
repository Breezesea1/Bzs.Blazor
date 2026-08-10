# Bzs.Blazor Next Components

Status: `resolved`

## Problem Statement

Bzs.Blazor has a coherent foundation and 33 public Razor components, but common
productivity workflows still require consumer-owned implementations for
anchored overlays, menus, navigation content, paging, remote suggestions, file
selection, and tabular data. Copying the broad MudBlazor or Radzen.Blazor
catalog would exceed the maintenance capacity of a personally maintained
library and introduce many shallow interfaces.

The next effort must close the highest-value gaps while preserving the
library's existing render-mode, accessibility, CSP, localization, composition,
and AOT guarantees.

## Solution

Deliver the next component wave as a dependency-ordered sequence whose
strategic result is a focused first-release DataGrid. First build a deep
internal anchored-overlay module, then public interaction, navigation, paging,
status, asynchronous-input, and DataGrid modules on top of proven seams.

Public modules retain distinct semantics. Internal implementation is shared
only when deleting the shared module would force the same non-trivial behavior
back into multiple callers.

## User Stories

1. As a user, I want tooltips that work with pointer, focus, touch, keyboard,
   reduced motion, and zoom so that supplemental labels remain accessible.
2. As a developer, I want a controlled popover so that I own durable open state
   while the library owns transient positioning and dismissal behavior.
3. As a keyboard user, I want menus and context menus with predictable roving
   focus, Escape, typeahead, and focus restoration.
4. As an application developer, I want route navigation and breadcrumbs that
   compose naturally inside the existing app shell.
5. As a user, I want paging controls with clear current-page and total-page
   semantics.
6. As a developer, I want status primitives for loading, counts, filters, and
   identities without writing new visual-system CSS in every application.
7. As a form author, I want asynchronous suggestions with cancellation and
   stale-result suppression for large or remote option sets.
8. As a form author, I want file selection integrated with EditForm validation
   while the application retains upload transport ownership.
9. As a developer, I want a typed DataGrid that supports both in-memory and
   remote data without coupling application queries to internal DOM behavior.
10. As a static SSR consumer, I want meaningful navigation, table, and form
    markup before interactivity begins.
11. As a maintainer, I want public behavior verified across all supported
    render modes, browsers, package consumption, trimming, and WASM AOT.

## Interface Decisions

- `Open`/`OpenChanged`, `Value`/`ValueChanged`, and other durable state remain
  controlled. Components never mutate parameter values.
- Anchor measurement, placement, collision handling, outside interaction, and
  lifecycle-safe interop live in one internal module. Public Popover, Tooltip,
  Menu, and ContextMenu interfaces do not expose browser adapters.
- `BzsOverlayHost` remains the command host for dialog and toast. Anchored
  components do not require it unless a later implementation decision proves a
  real shared seam and records that decision in an ADR.
- Autocomplete and DataGrid use feature-specific provider contracts. They share
  internal cancellation utilities only after duplicated behavior is proven.
- DataGrid exposes typed columns and templates rather than reflection-based
  string property paths as the primary interface.
- Runtime type scanning, `Expression.Compile`, `DynamicInvoke`, and unproven
  reflection patterns remain prohibited.
- File upload covers selection, validation, metadata, and progress presentation;
  consumers own HTTP endpoints, storage, authentication, retry, and resume.
- Dense menus, lists, and tables remain visually flat relative to raised
  controls and overlays.
- Library-owned strings ship in English and Simplified Chinese and follow the
  active UI culture.

## Planned Public Contracts

- `BzsPopover` owns its native trigger and accepts `TriggerContent`,
  `ChildContent`, `Open`/`OpenChanged`, logical placement, disabled state,
  trigger naming, and dismissal policy. It does not expose DOM coordinates.
- `BzsTooltip` decorates supplied trigger content and owns transient
  hover/focus/touch state, show/hide delays, placement, and descriptive text or
  rich content. Tooltip visibility is not durable application state.
- `BzsMenu` owns a native trigger and composes `BzsMenuItem` children.
  `BzsContextMenu` decorates a target region and composes the same menu-item
  model. Commands use `EventCallback`, while roving focus and typeahead remain
  internal state. Nested submenus are deferred from the first slice.
- `BzsNavMenu` composes `BzsNavItem` links and disclosure groups.
  `BzsBreadcrumbs` consumes an `IReadOnlyList<BzsBreadcrumbItem>` and marks the
  final item as the current page unless explicitly overridden.
- `BzsPagination` uses one-based `Page`, `PageChanged`, `PageCount`, sibling and
  boundary counts, disabled state, and an accessible name. It never loads data.
- `BzsAutocomplete<TValue>` derives from `BzsInputBase<TValue>`, consumes a
  feature-specific suggestion provider returning typed select options, and
  exposes debounce, minimum query length, loading/empty/error text, and an
  option template. The first slice requires selection from provider results;
  arbitrary free text is deferred.
- `BzsFileUpload` wraps native `InputFile`, exposes controlled selected browser
  files and callbacks, validates count and size, and presents consumer-owned
  progress. It does not read or upload file content itself.
- `BzsDataGrid<TItem>` composes typed `BzsDataGridColumn<TItem>` children and
  exposes either in-memory items or a feature-specific asynchronous provider,
  never both. Page, page size, sort, filters, and selection are controlled.
  Columns use typed selectors or cell templates; row identity requires an
  explicit key selector when controlled selection is enabled.

## DataGrid First-Release Scope

- Typed field and template columns.
- In-memory items and an asynchronous server-data provider.
- Single-column sorting.
- Explicit filter descriptors for supported field types.
- Controlled page, page size, and selection.
- Loading, empty, error, and retry presentation.
- Accessible table semantics and keyboard-reachable controls.
- Static SSR output followed by safe interactive handoff.

## Verification Decisions

- bUnit and xUnit assert public state, callbacks, requests, cancellation,
  validation, roles, names, and error behavior.
- Playwright covers pointer and keyboard interaction, positioning, focus,
  Server/WASM/Auto lifecycle, mobile layout, zoom, and directionality.
- Static SSR tests assert meaningful inert output and no browser invocation.
- Package-consumer smoke tests exercise every new static asset and representative
  public workflow from the produced NuGet.
- Release verification remains trim- and AOT-clean and measures package-size
  changes.

## Out Of Scope

- DataGrid editing, grouping, hierarchy, column drag-and-drop, frozen columns,
  virtualization, export, and persisted preferences.
- TreeView, Accordion, Stepper, time/range pickers, Slider, Rating, Splitter,
  Timeline, and DropZone in this effort.
- Charts, Scheduler, Gantt, Spreadsheet, rich-text editing, maps, media viewers,
  AI/chat modules, and third-party UI runtime dependencies.
- A public generic portal manager or browser-positioning adapter.
- Upload transport, storage, virus scanning, retry, and resumable protocols.
- Replacement wrappers for `BzsSurface`, `BzsButton`, native `EditForm`, or
  other already-covered behavior.

## Delivery Order

1. Public contracts and acceptance matrix.
2. Anchored overlay and Popover.
3. Tooltip and menu family.
4. Navigation, paging, and status primitives.
5. Autocomplete and file upload.
6. DataGrid client slice.
7. DataGrid server slice.
8. Cross-cutting Demo, browser, package, trim, and AOT verification.

## Comments

- 2026-08-08: Ticket 01 accepted the separate anchored-overlay module and
  feature-specific typed data-provider decisions in ADRs 0025 and 0026. The
  planned public contract shapes and deferred capabilities are now explicit.
- 2026-08-09: Tickets 02-12 completed the planned public modules, shared
  Productivity Demo, package-only consumption, browser matrix, trimming, and
  WebAssembly AOT gates for `0.2.0`.
