# Freeze contracts and acceptance matrix

Type: task
Status: resolved
Blocked by: none

## Goal

Define the public interfaces, module ownership, compatibility limits, and
acceptance matrix before implementation begins.

## Scope

- Inventory existing public contracts and reusable internal behavior.
- Define public parameter/event/template shapes for tickets 02-11.
- Record an ADR for anchored-overlay ownership and another for DataGrid data
  requests if existing ADRs do not fully decide those seams.
- Define static SSR and interactive behavior for every new public module.
- Estimate package, symbol, and AOT size impact and propose measured budgets.
- Add the expected public names to the planning baseline without changing the
  shipped API baseline prematurely.

## Acceptance Criteria

- Every planned public state value has an explicit owner.
- Public interfaces expose no browser adapter or internal state machine.
- Autocomplete and DataGrid provider contracts remain feature-specific.
- DataGrid first-release and deferred capabilities are unambiguous.
- All later tickets can be implemented without making a new architectural
  decision hidden inside their code changes.

## Verification

- Review the contracts against all affected ADRs.
- Validate representative Razor usage examples compile in a scratch test or
  design fixture.
- Record unresolved questions as `needs-info`, not implementation assumptions.

## Out Of Scope

- Runtime component implementation.

## Comments

- 2026-08-08: Completed the current public-contract inventory, accepted ADRs
  0025 and 0026, documented planned public contract shapes in the effort spec,
  fixed DataGrid first-release exclusions, and retained feature-specific
  provider interfaces. Package-size budgets remain measured release gates;
  adjustments require evidence in ticket 12.
