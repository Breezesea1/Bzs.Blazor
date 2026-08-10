# Bzs.Blazor Next-Component Roadmap

Status: complete

## Objective

Extend the current `0.1.13` package from a strong foundation, form, layout, and
overlay set into a practical productivity-oriented library. The roadmap
prioritizes capabilities shared by MudBlazor and Radzen.Blazor without treating
feature-count parity as a product goal.

The strategic target is a useful `BzsDataGrid<TItem>`. Delivery starts with the
anchored-overlay, navigation, paging, loading-state, and asynchronous-input
modules that DataGrid and later components can reuse.

## Baseline

The runtime package currently exposes 33 public Razor components covering:

- Theme, surface, icon, and button foundations.
- App shell, app bar, navigation drawer, grid, stack, container, and spacing.
- Native EditForm-integrated text, number, date, checkbox, radio, toggle,
  select, and multi-select inputs.
- Message, progress, toast, dialog, drawer, empty state, and tabs.

Existing capabilities must not be duplicated by shallow aliases. In
particular, `BzsSurface` covers paper/card-style composition, `BzsButton`
covers icon-only buttons, and native `EditForm` remains the form container.

## Delivery Checklist

| Done | Ticket | Deliverable | Blocked by |
| --- | --- | --- | --- |
| [x] | [01](../../.scratch/bzs-blazor-next-components/issues/01-freeze-contracts-and-acceptance-matrix.md) | Freeze public contracts, module ownership, and acceptance matrix | None |
| [x] | [02](../../.scratch/bzs-blazor-next-components/issues/02-implement-anchored-overlay-and-popover.md) | Anchored overlay module and controlled `BzsPopover` | 01 |
| [x] | [03](../../.scratch/bzs-blazor-next-components/issues/03-implement-tooltip.md) | Accessible `BzsTooltip` | 02 |
| [x] | [04](../../.scratch/bzs-blazor-next-components/issues/04-implement-menu-and-context-menu.md) | `BzsMenu`, `BzsMenuItem`, and `BzsContextMenu` | 02 |
| [x] | [05](../../.scratch/bzs-blazor-next-components/issues/05-implement-navigation-and-breadcrumbs.md) | `BzsNavMenu`, `BzsNavItem`, and `BzsBreadcrumbs` | 01 |
| [x] | [06](../../.scratch/bzs-blazor-next-components/issues/06-implement-pagination.md) | Controlled `BzsPagination` | 01 |
| [x] | [07](../../.scratch/bzs-blazor-next-components/issues/07-implement-status-primitives.md) | `BzsSkeleton`, `BzsBadge`, `BzsChip`, and `BzsAvatar` | 01 |
| [x] | [08](../../.scratch/bzs-blazor-next-components/issues/08-implement-async-autocomplete.md) | Async `BzsAutocomplete<TValue>` | 02 |
| [x] | [09](../../.scratch/bzs-blazor-next-components/issues/09-implement-file-upload.md) | EditForm-integrated `BzsFileUpload` | 01 |
| [x] | [10](../../.scratch/bzs-blazor-next-components/issues/10-implement-datagrid-client-foundation.md) | Semantic and client-data `BzsDataGrid<TItem>` foundation | 06, 07 |
| [x] | [11](../../.scratch/bzs-blazor-next-components/issues/11-implement-datagrid-server-operations.md) | Server-data DataGrid operations and column menus | 04, 10 |
| [x] | [12](../../.scratch/bzs-blazor-next-components/issues/12-complete-demo-and-release-gates.md) | Demo, accessibility, browser, package, trim, and AOT release gates | 03-11 |

Detailed acceptance criteria live under
`.scratch/bzs-blazor-next-components/issues/`.

## Milestones

### A. Anchored Interaction

Tickets 01-04 establish one deep internal module for anchor measurement,
placement, collision handling, outside interaction, Escape handling, and
lifecycle-safe browser integration. Popover, Tooltip, Menu, and ContextMenu
keep separate public semantics and use that implementation through internal
seams.

This module must not turn `BzsOverlayHost` into a universal portal. Full-screen
dialog/drawer coordination and element-anchored interaction remain distinct
unless implementation evidence proves a smaller shared seam.

### B. Navigation And Lightweight State

Tickets 05-07 complete the app-shell navigation story and add the paging and
loading/status primitives required by data-heavy screens. These modules are
primarily passive under static SSR and progressively add interaction.

### C. Asynchronous Inputs

Tickets 08-09 add remote suggestion loading and file selection. Provider
contracts remain feature-specific: Autocomplete query semantics must not be
forced into the later DataGrid request model. Cancellation, stale-result
suppression, and validation are observable behavior at each module interface.

### D. DataGrid

Tickets 10-11 deliver DataGrid in two vertical slices:

1. Semantic table output, typed columns, templates, loading/empty/error states,
   client sorting, paging, and selection.
2. Server data requests, filtering, cancellation, stale-result suppression,
   and accessible column menus.

The first release deliberately excludes cell editing, grouping, hierarchy,
column drag-and-drop, frozen columns, virtualization, export, and persistence.
Those capabilities require separate evidence and tickets.

### E. Release Hardening

Ticket 12 makes the new public interfaces real package guarantees. The final
gate covers Static SSR, Interactive Server, Interactive WebAssembly,
Interactive Auto, current browser targets, keyboard and axe checks, package
consumption, trimming, and WASM AOT. Package and framework size budget changes
must be measured and explicitly approved rather than silently relaxed.

## Follow-On Backlog

Do not start these until the DataGrid milestone is accepted:

- `BzsTreeView<TItem>` with lazy loading and tree keyboard semantics.
- `BzsAccordion` and `BzsStepper`.
- `BzsTimeInput<TValue>`, `BzsDateRangeInput<TValue>`,
  `BzsSlider<TValue>`, and `BzsRating`.
- `BzsSplitter`, `BzsTimeline`, and `BzsDropZone`.

Charts, Scheduler, Gantt, Spreadsheet, rich-text editing, maps, media viewers,
and AI/chat modules remain outside the core package. If real consumers require
them, prefer optional adapter packages around proven domain engines.

## Working Rules

- Deliver tickets in dependency order; do not implement DataGrid as one large
  change.
- Default to one ticket per pull request so public-interface review and browser
  evidence stay bounded.
- Keep public state controlled through standard bindable pairs.
- Keep public types sealed and extend behavior through templates, semantic
  parameters, attributes, and CSS variables.
- Preserve meaningful passive markup under static SSR.
- Treat modern Popover and CSS Anchor Positioning features as progressive
  enhancements, not required browser foundations.
- Test observable behavior through public interfaces; internal helper tests are
  allowed only where they protect complex pure logic.
- Update the public API baseline and XML documentation intentionally for every
  added public contract.

## Completion Criteria

- Tickets 01-12 are `resolved` with evidence recorded in their comments.
- Every new public component has an executable Demo example.
- The DataGrid first-release scope is usable against both in-memory and remote
  data without consumer-owned coordination races.
- No third-party UI runtime dependency is added to `Bzs.Blazor`.
- The complete release verification passes without IL2xxx or IL3050 warnings.
