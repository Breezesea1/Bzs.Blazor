# Implement DataGrid server operations

Type: task
Status: resolved
Blocked by: 04, 10

## Goal

Complete the first DataGrid release with asynchronous server data, filtering,
request cancellation, and accessible column menus.

## Scope

- Implement the feature-specific request/result contract accepted in ticket 01.
- Support total count, page, page size, single-column sort, and explicit filter
  descriptors for the accepted field types.
- Cancel superseded requests and suppress stale completions.
- Expose loading, error, retry, and empty results without discarding the last
  accepted data unexpectedly.
- Use the menu module for column operations without duplicating menu behavior.

## Acceptance Criteria

- Each observable grid state maps to an explicit provider request.
- Superseded, canceled, failed, and completed requests have deterministic
  outcomes and cannot reorder visible state.
- Unknown totals and changing totals follow documented paging behavior.
- Provider errors remain observable and retryable.
- The consumer can translate request descriptors to its own query layer without
  depending on internal types or DOM shape.

## Verification

- Deterministic concurrency tests with controllable provider completions.
- bUnit tests for requests, cancellation, errors, retry, filtering, and menus.
- Playwright tests against a real delayed endpoint in Server, WASM, and Auto.
- Trim/AOT smoke for generic grids, typed columns, and provider results.

## Out Of Scope

- Multi-column sort, advanced expression builders, editing, grouping,
  hierarchy, virtualization, export, and query-language adapters.

## Comments

- 2026-08-09: Implemented the typed provider request/result seam, cancellation
  and stale-result suppression, controlled filtering, retryable provider errors,
  known/unknown-total paging, accepted-row retention, and menu-backed column
  commands. Provider results validate configured `ItemKey` values before
  acceptance, including null and duplicate keys. Failure state is committed
  before `ProviderFailed` callbacks, and retained pager/sort semantics describe
  the accepted request.
- 2026-08-09: Added deterministic coordinator and bUnit regression coverage for
  supersession/disposal token races, invalid provider keys, initial custom error
  templates, callback disposal, case-sensitive filter reapply, rejected filter
  clearing, failed page retry for known and unknown totals, accepted sort ARIA
  state, and known-total shrink correction.
- 2026-08-09: Verification passed serially: provider/server tests 32/32; all
  DataGrid tests 48/48; full unit suite 352/352; `dotnet build
  Bzs.Blazor.slnx --configuration Release --no-restore --no-incremental` with
  0 warnings and 0 errors; `dotnet format whitespace --verify-no-changes
  --no-restore`; `git diff --check`; scoped forbidden reflection/dynamic API
  and trailing-whitespace scans. Real-browser, package-consumer, trim, and
  WASM AOT gates remain part of Ticket 12's cross-cutting release verification.
