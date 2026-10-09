# Architecture History

The repository's decided Architecture Candidates, kept as a navigation index. Each record
states the decision, the baseline it was taken against, the durable evidence behind it, and
what would reopen the question. The full argument stays in the linked ADR or pull request;
this index points at it rather than restating it.

Read this file before an architecture scan, an architecture refactor, or overlay design.
`CONTEXT.md` at the repository root defines *Architecture Candidate*, and `docs/adr/` holds the
decisions themselves.

## Record shape

Every record below carries the same fields:

| Field | Meaning |
| --- | --- |
| Candidate | The structural improvement that was proposed. |
| Baseline | The repository baseline the decision was taken against. |
| Decision | Adopt, Hold, Reject, or Merge. |
| Evidence | The ADR, pull request, and repository artifacts that carry the decision. |
| Rationale | Why the decision went that way, in one or two sentences. |
| Reconsider when | The condition that would justify reopening it, omitted when the decision is settled. |

## Decided candidates

All four candidates below were reviewed on 2026-10-04. The three adopted decisions are present
in `main` as of the PR #45 merge commit `e930213`.

### Modal-panel lifecycle owner — Adopt

- **Candidate**: give `BzsDialog` and `BzsDrawer` one internal owner for the lifecycle state
  and activation logic they each carried.
- **Baseline**: 2026-10-04 architecture review.
- **Decision**: Adopt — implemented by PR #43.
- **Evidence**: `src/Bzs.Blazor/Components/Dialog/Internal/BzsModalPanelLifecycle.cs` owns the
  bookkeeping, `tests/Bzs.Blazor.Tests/OverlayComponentTests.cs` covers it, and both shells
  keep their markup, parameters, and public events.
- **Rationale**: the open/modal/initial-focus diff, the pending-sync flag, and the render-time
  activate/deactivate were byte-identical between the two shells, so one owner removes the
  drift without touching behaviour or public API.
- **Reconsider when**: the two shells genuinely need different lifecycle rules. That
  difference belongs in the shells; widen the owner only if it is the same rule.

### Severity, level, and size naming — Adopt

- **Candidate**: collapse the enum-to-CSS-name, icon, and role knowledge that eight components
  typed independently into one internal module per concept under
  `src/Bzs.Blazor/Components/Common/Internal`.
- **Baseline**: 2026-10-04 architecture review.
- **Decision**: Adopt — implemented by PR #44.
- **Evidence**: `src/Bzs.Blazor/Components/Common/Internal/BzsSeverityNames.cs`,
  `src/Bzs.Blazor/Components/Common/Internal/BzsSizeNames.cs`, and
  `src/Bzs.Blazor/Components/Common/Internal/BzsSurfaceLevelNames.cs`, following the existing
  `src/Bzs.Blazor/Components/Layout/Internal/LayoutNames.cs` pattern, with
  `tests/Bzs.Blazor.Tests/MappingNamesTests.cs` covering the mappings.
- **Rationale**: each component re-derived the same mapping and could drift from the others;
  one module per concept keeps the mapping in a single place while the two public severity
  enums and every public parameter stay unchanged.
- **Reconsider when**: the two public severity enums are merged. That is a breaking change, so
  until it is deliberately taken the per-concept modules stay separate.

### Anchored Overlay Session deepening — Adopt

- **Candidate**: sink the overlay-owner protocol — the attempt-limit constant, the lazy session
  getter, the desired-state record, the browser-close bridge, and the dispose block — from the
  eight overlay owners into the session itself.
- **Baseline**: 2026-10-04 architecture review.
- **Decision**: Adopt — implemented by PR #45.
- **Evidence**: `src/Bzs.Blazor/Components/AnchoredOverlay/Internal/BzsAnchoredOverlaySession.cs`
  owns the protocol and `tests/Bzs.Blazor.Tests/AnchoredOverlaySessionTests.cs` covers it; the
  eight owners under `src/Bzs.Blazor/Components/Form/` (`BzsAutocomplete`, `BzsDateInput`,
  `BzsMultiSelect`, `BzsSelect`), `src/Bzs.Blazor/Components/Menu/` (`BzsContextMenu`,
  `BzsMenu`), `src/Bzs.Blazor/Components/Popover/`, and
  `src/Bzs.Blazor/Components/Tooltip/` now supply only what varies.
- **Rationale**: eight re-declarations of the same protocol had already drifted (the attempt
  limit stood at 3 in Tooltip), so the session owns the limits and timing and each owner
  creates it once in `OnInitialized`.
- **Reconsider when**: an owner needs a session lifecycle the session cannot express without a
  parameter standing in for that owner's variance. Fix the seam then; do not fork the protocol.

### Anchored key-mode registry — Reject

- **Candidate**: replace the four-branch `data-bzs-anchored-keys` switch in
  `src/Bzs.Blazor/Components/Popover/BzsPopover.razor.js` with a keys-contract registry that
  each overlay owner registers its own keydown handler into.
- **Baseline**: 2026-10-04 architecture review.
- **Decision**: Reject — recorded as ADR-0041, which stands as the decision of record.
- **Evidence**: `docs/adr/0041-keep-anchored-key-mode-interception-in-the-single-overlay-module.md`.
- **Rationale**: the proposal fails the deletion test. The intercepted surface is four small,
  mutually distinct `keydown` filters whose real behaviour already lives in each owner's C#
  state machine, so deleting the switch would force every owner to reimplement shared
  infrastructure. A registry would add cross-module handler registration, disposal-time
  unregistration, and synchronisation between the attribute and the registry, while deleting no
  complexity a caller would otherwise have to write. The DOM predicates the branches read are
  the `data-bzs-*` and ARIA contracts already locked by ADR-0036
  (`docs/adr/0036-name-the-elements-tests-need-through-the-dom-contract.md`), so one module
  reading them is no more a leak than it reading `data-bzs-anchor`.
- **Reconsider when**: a fifth key-mode, or branch growth, makes the switch itself the friction.
  Extending the switch is the first move; reopen the registry question explicitly and from
  ADR-0041 rather than re-proposing it as new.

## Deferred work

These open issues are follow-ups from the 2026-09-29 systemic review (PR #37). They are not
decisions: each is still an open question, and none of them records an adopted design.

- **#38 — Share one RTL direction read across the four JS modules that duplicate it.** Four
  interactive modules re-derive the writing direction from computed style independently.
- **#39 — Consolidate or document the three duplicated resize-observer wirings.** Three modules
  wire their own observer lifecycle with at least three cleanup conventions and one inconsistent
  feature guard.
- **#40 — Audit small-control unit coverage against public behavior.** Confirm each small
  control's public parameters, events, and ARIA semantics are exercised, and fill the gaps.
- **#41 — Document the NavigationDrawer module-level observer's intent.** Record why a body-wide
  `MutationObserver` with the `data-bzs-navigation-drawer-*` filter is the chosen trigger, why a
  `ResizeObserver` does not fit, and that the subtree cost is accepted.

**Issue #41 is not ADR-0041.** They touch different modules and different questions: #41 asks
for a comment on the `staticResizeObserver` field in
`src/Bzs.Blazor/Components/Layout/BzsNavigationDrawer.razor.js`, while ADR-0041 rejects a
keys-contract registry for the key-mode switch in
`src/Bzs.Blazor/Components/Popover/BzsPopover.razor.js`. Neither resolves, reopens, or constrains
the other.

## Updating this index

- Add a record when an Architecture Candidate reaches an explicit Adopt, Hold, Reject, or Merge
  decision, and give it the whole record shape at that moment.
- Record only decided candidates. An exploratory idea stays out until it is decided, and this
  index stays a decision record rather than a log of every proposal.
- Extend a record's *Reconsider when* line instead of adding a second record for the same
  candidate, and delete or supersede the record when a later decision replaces it.
- Move an issue out of Deferred work when it is decided (into a record) or closed (out of the
  index).
