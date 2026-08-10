# Implement Pagination

Type: task
Status: resolved
Blocked by: 01

## Goal

Deliver a controlled `BzsPagination` suitable for standalone lists and the
later DataGrid module.

## Scope

- Support page count, current page, current-page callback, sibling/boundary
  count, first/previous/next/last commands, disabled state, and compact mode.
- Define behavior when item totals or page sizes change asynchronously.
- Localize library-owned command labels.
- Keep page numbering conventions explicit at the public interface.

## Acceptance Criteria

- The component never mutates the supplied current page.
- Invalid ranges fail fast or normalize through an explicitly documented
  result contract chosen in ticket 01.
- Keyboard focus does not jump unexpectedly after a page change.
- Static SSR exposes useful links or buttons with clear disabled state.

## Verification

- Pure range-generation tests including small, large, zero, and changing page
  counts.
- bUnit tests for callbacks, labels, disabled state, and static output.
- Browser tests for compact mobile layout and 200% zoom.

## Out Of Scope

- Data loading and URL query-string synchronization.

## Comments

- 2026-08-08: Implemented controlled one-based `BzsPagination`, pure bounded
  range generation, compact/disabled states, localized English and Simplified
  Chinese labels, stable keyed commands, and semantic static SSR. Nine focused
  tests pass; mobile and 200% zoom evidence remains centralized in ticket 12.
