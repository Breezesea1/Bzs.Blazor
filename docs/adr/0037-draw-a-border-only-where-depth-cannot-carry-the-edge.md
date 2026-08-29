# Draw a border only where depth cannot carry the edge

Bzs.Blazor decides a surface's border from what that edge has to communicate, not
from the component that happens to own it. An in-flow surface whose only job is
to sit at a depth level — a raised card, an inset well, a grouped control shell —
expresses that depth through `--bzs-shadow-raised` or `--bzs-shadow-inset` and
draws no border, which is what ADR-0003 already implies by making depth semantic.
A border appears for exactly four reasons:

1. **The edge carries meaning the fill cannot.** A field's typing surface, a
   progress track's full extent, an empty state's placeholder region. Colour is
   `--bzs-border`.
2. **The surface is detached** and floats over content it cannot predict — every
   dialog, drawer, menu, context menu, popover, tooltip, toast, and anchored
   option panel. Colour is `--bzs-border`.
3. **A structural division inside an already-delimited container** — grid row and
   column rules, header and footer splits, dividers, menu separators. Colour is
   `--bzs-border-subtle`.
4. **State** — an active tab, a selected page, a checked ring, a focused field.
   Colour is a semantic token (`--bzs-primary`, `--bzs-error`, `--bzs-info`), never
   a border token. `currentColor` satisfies this when the element's own `color` is
   token-derived, which is how `BzsBadge` carries severity.

Those are the only two border colours the library owns. `--bzs-border` is
load-bearing and must reach 3:1 against `--bzs-canvas`, `--bzs-surface`,
`--bzs-surface-raised`, `--bzs-surface-inset`, and `--bzs-surface-overlay`, because
a reason-1 or reason-2 edge is the boundary a visitor relies on and it can land on
any of them; the shipped `#5c6570` and `#7d8894` already clear that at a worst case
of 4.34:1 and 3.85:1, so no palette change is needed to adopt the floor.
`--bzs-border-subtle` is held only to being perceptible and to reading more softly
than `--bzs-border` on every surface, not to 3:1: a reason-3 rule divides a
container that is itself already delimited, and applying the boundary floor to
interior rules is what made full-strength hairlines read as hard outlines on
near-white cards in the first place. It replaces the five ad-hoc
`color-mix(in srgb, var(--bzs-border) N%, transparent)` strengths (78%, 72%, 68%,
62%, 58%) that `BzsDataGrid` invented to soften the same token locally, and ships
as the flat resolution of the most-used of them: `#8b939c` light, `#636d78` dark.

`--bzs-border-subtle` becomes a positional member of `BzsThemeColors` so a custom
theme controls it like any other colour. That is a breaking constructor change,
acceptable before 1.0 on the same reasoning ADR-0030 used for `Scrim`, and
preferable to a token only built-in themes can reach.

## Consequences

- These surfaces stop drawing a container edge and rely on depth: `BzsMessage`,
  `BzsChip`, `BzsFileUpload`'s per-file card, and `BzsDataGrid`'s
  `__surface` and `__header-area` frames. `BzsMessage` keeps its `0.25rem`
  severity stripe, which is reason 4 — that stripe, not a box, is what a visitor
  reads severity from, and it is why the same component looked bordered while the
  buttons beside it did not.
- `BzsToast` keeps its full border. It looks like `BzsMessage` but floats, so
  reason 2 applies and the two deliberately diverge. `BzsProgress`'s track and
  `BzsEmptyState`'s dashed edge keep theirs under reason 1: an empty light-mode
  track sits at 1.05:1 against the page, so its extent is unreadable without the
  border, and shadow cannot substitute on a 0.625rem bar.
- `BzsRadioGroup` was already correct, so the divergence between the Demo
  Catalog's header controls resolves against the demo: `.demo-theme-switch` drops
  its hand-rolled `border: 1px solid var(--bzs-border)` and gains the
  forced-colors `CanvasText` edge `.demo-language-fallback` already has. The demo
  owns no border decisions of its own and stops writing a literal `1px` in the 28
  places it does, since those never tracked `--bzs-border-width`. Every edge the
  demo still draws turns out to be an interior division, so its `--demo-border`
  and `--layout-border` aliases of `--bzs-border` fall out entirely and are
  replaced by `-subtle` aliases; a demo edge that needs the load-bearing token
  should reach for `--bzs-border` directly rather than reviving an alias.
- Depth disappears under `@media (forced-colors: active)`, so every surface that
  relies on depth alone must restore an edge there. Components using
  `var(--bzs-border)` get this free — the global block repoints the token to
  `CanvasText` — but the ones that now have no border at all need an explicit
  `outline`, and `BzsToggle`'s track already lacks one today.
- The theme contrast gate widens from `--bzs-border` against canvas and
  surface-inset to all five surface levels, and gains a second gate asserting
  `--bzs-border-subtle` reads more softly than `--bzs-border` on each of them
  while staying perceptible. Neither gate can see which token a component
  actually reached for, so nothing yet stops a future overlay panel from
  adopting the subtle token; that remains a review obligation.
- `BzsDataGrid`'s state rings mix a primary tint into a border token. Those that
  ride on an interior division — the status pill, the filter-chip family hover,
  the active filter option — mix into `--bzs-border-subtle` so the resting and
  active states share a strength; the search field's focus ring keeps mixing into
  `--bzs-border` because the field itself is a reason-1 surface.
- Seven of the eleven committed visual baselines change. Six carry the demo
  header's theme switch (`foundation-light-desktop`, `foundation-dark-desktop`,
  `tabs-light-mobile`, `productivity-light-desktop`, and both `forms-*`), and
  `productivity-light-desktop` additionally carries Chip, FileUpload, and all four
  grids; `auto-dark-mobile` changes through Message. The four `landing-*`
  baselines render neither the catalog header nor any affected component and
  should come back byte-equivalent. Per ADR-0029 they are refreshed through the
  Linux workflow, never locally.
- `AccessibilityGateTests`' `HasBorderAsync` assertions stay valid: they cover a
  focused text input, a focused tab, and a tooltip, all of which keep borders
  under reasons 1, 4, and 2.
