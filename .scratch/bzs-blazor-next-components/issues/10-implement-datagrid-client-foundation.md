# Implement DataGrid client foundation

Type: task
Status: resolved
Blocked by: 06, 07

## Goal

Deliver the first `BzsDataGrid<TItem>` vertical slice with semantic output,
typed columns, in-memory data operations, paging, and selection.

## Scope

- Implement `BzsDataGrid<TItem>` and typed column definitions from ticket 01.
- Support field and template cells without string-based reflection as the
  primary contract.
- Support controlled sort, page, page size, and single/multiple selection.
- Render loading, empty, and error states through composable templates and the
  new status primitives.
- Preserve semantic table output and responsive horizontal overflow.

## Acceptance Criteria

- Static SSR renders headers, rows, cells, captions, and state content with
  correct table semantics.
- Consumer item instances are never mutated.
- Sorting and paging are deterministic, culture-aware where appropriate, and
  stable under equal keys.
- Row identity and selection survive equivalent item-collection replacement
  according to the documented key contract.
- No runtime assembly scanning, dynamic generic construction, or unproven
  reflection path is introduced.

## Verification

- Pure tests for sort, page, identity, and selection calculations.
- bUnit tests for public state, templates, callbacks, semantics, empty/loading/
  error states, and static output.
- Browser tests for keyboard-reachable controls, responsive overflow, zoom,
  forced colors, and interactive handoff.

## Out Of Scope

- Server data, filtering, editing, grouping, hierarchy, virtualization,
  column reordering, frozen columns, export, and persisted preferences.

## Comments

- 2026-08-09: Implemented the semantic client-data DataGrid foundation with
  typed field/template columns, stable culture-aware sorting, controlled
  one-based paging, controlled keyed selection, state templates, localization,
  static SSR markup, and responsive overflow. Independent review corrections
  aligned renderer identity with custom key comparers, preserved declarative
  conditional-column order, exposed accessible names for non-sortable template
  headers, and reduced multiple-selection matching to comparer-aware set
  lookups. Verification passed 16/16 focused DataGrid tests, 320/320 full unit
  tests, and the unsuppressed Release solution build with zero warnings and
  errors. Browser-specific controlled-input rejection and responsive/zoom/
  forced-colors checks remain assigned to ticket 12.
