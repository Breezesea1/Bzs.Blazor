# Implement async Autocomplete

Type: task
Status: resolved
Blocked by: 02

## Goal

Deliver `BzsAutocomplete<TValue>` for query-driven remote or large suggestion
sets without overloading the existing closed-set `BzsSelect<TValue>` contract.

## Scope

- Integrate with `BzsInputBase<TValue>` and native EditForm validation.
- Support asynchronous suggestions, cancellation, debounce, minimum query
  length, loading, empty, error, retry, and result templates.
- Define strict selection versus free-text behavior explicitly.
- Implement editable-combobox keyboard and ARIA behavior.
- Reuse anchored placement without exposing its browser implementation.

## Acceptance Criteria

- Superseded and canceled requests cannot overwrite current results.
- Provider failures are observable and retryable without losing input text.
- Selection, clearing, free text, and validation produce documented values.
- Static SSR renders a usable text input without invoking the provider.

## Verification

- Deterministic tests for debounce, cancellation, stale completion, errors, and
  value parsing.
- bUnit tests for EditContext integration and combobox semantics.
- Playwright tests for real typing, keyboard selection, focus, mobile, and all
  interactive render modes.

## Out Of Scope

- General command-palette behavior and multi-value token input.

## Comments

- 2026-08-08: Implemented strict-selection `BzsAutocomplete<TValue>` with a
  feature-specific typed provider, cancellation, debounce, stale-result
  suppression, loading/empty/error/retry states, controlled values, EditContext
  validation, typed templates, localized English and Simplified Chinese text,
  stable static-SSR form values, anchored listbox semantics, and 14 passing
  focused tests. Arbitrary free text remains deferred by the frozen effort
  contract. Real typing, focus, mobile, and render-mode browser evidence is
  consolidated under ticket 12.
