# Bzs.Blazor architecture hardening, stages 4-6

Status: resolved

## Objective

Improve lifecycle coverage, reduce complex component maintenance cost, and
shorten pull-request feedback without changing the public component contract.

## Completed prerequisites

- Stage 1: packaged DateInput assets and runtime consumption coverage.
- Stage 2: release tags bound to current `main` and exact-SHA CI.
- Stage 3: public API baseline and composition-only inheritance boundary.

These prerequisites shipped in Bzs.Blazor 0.1.11.

## Stage 4: lifecycle coverage

- Exercise a real Static SSR POST from a package-only consumer.
- Cover text, number, date, checkbox, radio, select, and multi-select values,
  including server validation and repeated multi-select form values.
- Verify form labels and tab ARIA relationships before and after interactive
  prerender handoff, and verify inputs remain focusable on both sides of the
  handoff.
- Lock the dialog startup contract: a registered static host returns
  `Unavailable`; calls without a registered host remain configuration errors.

## Stage 5: internal state refactoring

- Extract DateInput value conversion and Gregorian culture handling.
- Extract calendar range, grid, and navigation calculations into testable
  internal helpers.
- Share only proven Select/MultiSelect filtering and enabled-option navigation
  algorithms; component rendering and transient state remain local.
- Preserve public API, DOM semantics, keyboard behavior, and JS calls.

## Stage 6: engineering efficiency

- Centralize collocated JS module import/disposal and transient-failure
  diagnostics without hiding non-transient failures.
- Keep typed feature-specific interop wrappers.
- Run a fast build/unit gate on pull requests and retain complete Release/AOT
  and branded-browser gates on `main` and release tags.
- Enforce explicit NuGet, symbols, and AOT framework size budgets.

## Verification

- Release build without warnings.
- Unit, package-consumer, browser, and accessibility tests.
- Chromium, mobile Chrome, Chrome, and Edge matrix.
- Pack, static assets, Source Link, symbols, trimming, and WASM AOT.
- Public API baseline remains unchanged unless explicitly approved.
