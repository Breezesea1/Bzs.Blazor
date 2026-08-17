# Bzs.Blazor Capability Enhancements

Status: accepted

Specification: [GitHub issue #10](https://github.com/Breezesea1/Bzs.Blazor/issues/10)

## Objective

Strengthen Bzs.Blazor through general-purpose capability candidates discovered
from BzsOIDC as a Reference Application. Reference demand is evidence for a
candidate, not an implementation commitment, and BootstrapBlazor feature parity
is not a product goal.

This plan evaluates each candidate on two independent axes:

- **Framework value**: reusable behavior, complexity hidden behind a small
  interface, accessibility and correctness leverage, and implementation
  locality.
- **Reference demand**: current BzsOIDC need, workflow impact, and the quality of
  available application-level alternatives.

Candidates use four dispositions:

- **Adopt**: specify and schedule for the current capability-enhancement cycle.
- **Hold**: valid general capability without enough present evidence to design or
  schedule safely.
- **Reject**: application composition is sufficient, or the interface would not
  earn its maintenance cost.
- **Merge**: the behavior belongs inside another adopted capability rather than
  behind a separate public interface.

## Baseline

The assessment targets Bzs.Blazor 0.2.2. The runtime and unit-test sources at the
planning commit match the `v0.2.2` tag.

Confirmed baseline behavior includes:

- `BzsTextInput` renders `type="text"` and commits on `change`.
- Input components derive from `BzsInputBase<TValue>` and preserve native
  `EditForm`, validation, controlled state, and Static SSR contracts.
- `BzsDataGrid<TItem>` owns provider loading, cancellation, stale-result
  suppression, retained accepted results, paging, and controlled selection.
- `BzsNavigationDrawer` supports controlled open state and backdrop closure but
  does not own modal focus lifecycle behavior.
- Dialog and drawer focus management, scroll locking, and restoration already
  exist behind internal overlay infrastructure.
- Consumers can provide application-owned `BzsIconData` values without expanding
  the built-in icon catalog.

## Candidate Disposition

| Candidate | Disposition | Reason |
| --- | --- | --- |
| Dedicated password input | Adopt | Hides form, masking, reveal, focus, localization, and Static SSR behavior behind one reusable interface. |
| Text/email/search types and update modes | Adopt | Establishes constrained native semantics and a tested live-input contract without admitting arbitrary input behavior. |
| Explicit DataGrid provider refresh | Adopt | Keeps cancellation, latest-state-wins coordination, retained rows, and error handling inside DataGrid. |
| Per-item navigation activation callback | Reject | `NavigationManager.LocationChanged` and `LinkAttributes` already cover application route reactions without another shallow interface. |
| Modal navigation-drawer lifecycle | Adopt | Focus containment, restoration, background interaction, viewport transitions, and disposal are complex reusable behavior. |
| DataGrid footer visibility | Adopt | DataGrid owns the footer markup, so semantic visibility controls are the only clean consumer seam. |
| Current-page DataGrid select all | Adopt | Centralizes key identity, indeterminate state, cross-page preservation, and controlled selection. |
| Busy dialog dismissal lock | Hold | Direction is sound, but declarative and service dialogs require materially different runtime state channels. |
| Declarative DataGrid column sizing | Hold | General value exists, but the current responsive design does not yet require a stable width interface. |
| Interactive DataGrid column resize | Reject | Only legacy parity evidence exists; pointer, keyboard, measurement, and persistence complexity are unjustified. |
| Link-styled action | Hold | Native anchors remain sufficient until repeated application markup proves a stable reusable contract. |
| Password reveal icons | Merge | Add `Eye` and `EyeOff` as part of the password-input deliverable. |
| Identity and administration icon catalog | Reject | Domain vocabulary remains application-owned; add only icons required by adopted framework components. |
| Titled panel | Reject | `BzsSurface` plus ordinary composition already covers the behavior. |
| Form-level validation summary | Hold | A real general convenience, but not required by the current adopted capability set. |

## Adopted Public Interfaces

### 1. Text Input Types And Update Modes

Add:

- `BzsTextInputType` with `Text`, `Email`, and `Search`.
- `BzsInputUpdateMode` with `Change` and `Input`.
- `BzsTextInput.InputType`, defaulting to `Text`.
- `BzsTextInput.UpdateMode`, defaulting to `Change`.

Behavior:

- `Change` preserves the existing commit contract.
- `Input` propagates committed native input events through `ValueChanged` and an
  active `EditContext`.
- IME composition holds intermediate values and commits the final composition
  once, without a duplicate trailing update.
- Native `inputmode` remains an unmatched HTML attribute and is not conflated
  with `UpdateMode`.
- `@bind-Value` works inside and outside `EditForm`; manual controlled usage
  retains the standard `InputBase` requirement for `ValueExpression`.
- Debouncing, search requests, and cancellation remain consumer concerns.

### 2. Password Input

Add:

- Sealed `BzsPasswordInput : BzsInputBase<string?>`.
- `Revealable`, defaulting to `false`.
- Optional `ShowPasswordText` and `HidePasswordText` overrides with localized
  defaults.
- Curated `BzsIcons.Eye` and `BzsIcons.EyeOff` values.

Behavior:

- Render `type="password"` by default and keep password values out of the
  JavaScript interop boundary.
- Keep reveal state internal as transient visual state.
- Preserve focus and caret position across reveal/hide transitions.
- Disable the reveal control with a disabled input; allow reveal for a read-only
  input.
- Preserve validation, stable native form names, static form posts, descriptions,
  errors, required state, autocomplete, and allowed unmatched attributes.
- Emit useful native password markup under Static SSR.

### 3. DataGrid Provider Refresh

Add public `Task BzsDataGrid<TItem>.RefreshAsync()`.

Behavior:

- Capture and reload the current page, page size, sort, and filters without
  replacing the provider.
- Use the existing request coordinator and latest-state-wins rules.
- Complete the returned task when that refresh succeeds, fails, or is superseded.
- Continue to surface failures through the component error state and
  `ProviderFailed` rather than redundantly rethrowing them.
- Preserve the last accepted rows during background loading or a retained-result
  error.
- Queue a pre-first-result refresh through the normal interactive lifecycle.
- Fail fast when invoked in Items mode and complete safely after disposal.
- Do not emit `SelectedItemsChanged` merely because refreshed row instances
  changed. Selection identity remains keyed by `ItemKey`.

### 4. DataGrid Footer Visibility

Add:

- `ShowPageSizeSelector`, defaulting to `true`.
- `ShowPagination`, defaulting to `true`.

Behavior:

- Existing output remains unchanged by default.
- Visual visibility does not alter controlled paging or provider request values.
- When both properties are false, omit the footer container.
- Do not add `FooterTemplate`, external pager composition, or an unpaged provider
  mode in this cycle.

### 5. Current-Page DataGrid Select All

Add:

- `ShowSelectAll`, defaulting to `false`.
- Optional `SelectAllText` with a localized default accessible label.

Behavior:

- Operate only on currently rendered client rows or the last accepted provider
  page.
- Checking adds current-page rows; unchecking removes only current-page rows.
- Preserve selections from other pages.
- Compute checked and indeterminate state from current-page keys.
- Invoke `SelectedItemsChanged` once with the complete candidate selection and
  never mutate the controlled parameter.
- Use current-page instances in the candidate while preserving consumer-supplied
  off-page instances.
- Render an empty-page header checkbox as disabled and unchecked.
- Retained rows remain selectable during background provider loading or error.

### 6. Modal Navigation-Drawer Lifecycle

Add:

- `CloseOnEscape`, defaulting to `true`.
- Optional `InitialFocusSelector`.

Behavior:

- Temporary presentation is modal while open; persistent presentation never
  traps or redirects focus.
- Responsive presentation derives modality from actual rendered CSS/backdrop
  state rather than a duplicate public breakpoint.
- Entering modal presentation establishes initial focus, Tab containment,
  background non-interactivity, and scroll locking.
- Accepted closure restores focus to a still-connected opener; a removed opener
  is skipped safely.
- Desktop-to-mobile viewport transitions activate modal constraints and move
  outside focus into the drawer.
- Mobile-to-desktop transitions release modal constraints without requesting an
  open-state change or performing close restoration.
- Variant changes, enhanced navigation, transient JavaScript unavailability,
  concurrent renders, and disposal remain lifecycle-safe.
- Reuse or deepen internal overlay infrastructure. Do not expose a public browser
  adapter, focus controller, breakpoint, `IsMobile`, or `TrapFocus` parameter.

## Delivery Checklist

| Done | Issue | Deliverable | Blocked by |
| --- | --- | --- | --- |
| [ ] | [#11](https://github.com/Breezesea1/Bzs.Blazor/issues/11) | Text input type and update modes | None |
| [ ] | [#12](https://github.com/Breezesea1/Bzs.Blazor/issues/12) | Dedicated password input and reveal icons | #11 |
| [ ] | [#13](https://github.com/Breezesea1/Bzs.Blazor/issues/13) | Explicit DataGrid provider refresh | None |
| [ ] | [#14](https://github.com/Breezesea1/Bzs.Blazor/issues/14) | DataGrid footer visibility | #13 |
| [ ] | [#15](https://github.com/Breezesea1/Bzs.Blazor/issues/15) | Current-page DataGrid select all | #14 |
| [ ] | [#16](https://github.com/Breezesea1/Bzs.Blazor/issues/16) | Modal navigation-drawer lifecycle | None |
| [ ] | [#17](https://github.com/Breezesea1/Bzs.Blazor/issues/17) | Cross-mode release gate | #11-#16 |

GitHub issue #10 is the parent specification. Issues #11-#17 are native
sub-issues, and the table's dependency relationships are also recorded through
GitHub's native blocked-by relationships. One implementation issue should
normally produce one pull request.

## Verification Matrix

### bUnit

- Parameter defaults and validation.
- Controlled values and callbacks.
- `EditContext` field notification.
- Attribute forwarding and controlled-attribute protection.
- Password reveal state and localized labels.
- DataGrid set operations, custom comparers, off-page preservation, retained
  rows, and rejected controlled updates.
- Footer markup and provider request invariants.

### Coordinator And Unit Tests

- Refresh latest-state-wins behavior.
- Cancellation and stale-result suppression.
- Failure, retry, provider replacement, and disposal.
- Refresh selection identity without an unsolicited selection callback.

### Browser Tests

- Password focus and caret preservation.
- IME composition and committed Chinese input.
- Navigation-drawer initial focus, fallback focus, Tab containment, Escape,
  backdrop, restoration, disconnected opener, viewport changes, variant changes,
  enhanced navigation, and disposal.
- Standalone WebAssembly for every capability that depends on browser events or
  JavaScript, with retained Server, WebAssembly, and Auto overlay regressions.

### Static SSR And Release

- Useful native password and text markup.
- Stable native form names and values.
- DataGrid table, caption, row, and footer semantics.
- Public API baseline and XML documentation.
- Bilingual Demo Catalog examples.
- `scripts/verify-fast.ps1 -Configuration Release`.
- Focused browser and accessibility gates.
- The full browser matrix with explicit optional-browser skips.
- `scripts/verify-release.ps1`, package consumption, trimming, and WASM AOT
  without weakening size or analyzer-warning budgets.

## Architecture Guardrails

- Preserve existing defaults and public behavior.
- Keep public state controlled and keep transient interaction state internal.
- Keep public modules sealed and composition-oriented.
- Do not introduce a public shared input base beyond the existing restricted
  `BzsInputBase<TValue>` contract.
- Do not expose browser adapters, composition trackers, focus controllers, or
  internal state machines.
- Keep authentication, antiforgery, permission evaluation, API clients, OIDC
  protocol transactions, preference persistence, domain editors, responsive
  page composition, and branding in BzsOIDC.
- Preserve Static SSR, Interactive Server, Interactive WebAssembly, Interactive
  Auto, and standalone WebAssembly support.
- Add no third-party UI runtime dependency.

## ADR Disposition

No new ADR is required. The accepted interfaces extend existing decisions:

- ADR-0008: controlled state and native Blazor form contracts.
- ADR-0009: separate public overlay semantics with shared internal
  infrastructure.
- ADR-0011: curated, extensible Lucide icons.
- ADR-0015: unit, browser, accessibility, and package verification.
- ADR-0022: composition extension instead of component inheritance.

## Reopening Held Candidates

A held candidate returns to design only with new evidence:

- **Busy dialog dismissal:** identify declarative or service-dialog ownership and
  demonstrate a required runtime state channel.
- **Declarative column sizing:** provide current responsive workflows that cannot
  be expressed through stable component semantics.
- **Link-styled action:** show repeated anchor composition with one stable semantic
  and visual contract.
- **Validation summary:** show repeated form-level validation behavior that should
  subscribe to `EditContext` behind one reusable interface.

Rejected candidates require materially different evidence, not only another
request for legacy feature parity.

## Completion Criteria

- Issues #11-#16 are closed with concrete verification evidence.
- Issue #17 passes and records the complete cross-mode release gate.
- Public defaults remain compatible with 0.2.2 behavior.
- Hold, Reject, and Merge dispositions remain outside implementation scope unless
  this plan is explicitly revised.
