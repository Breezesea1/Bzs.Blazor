# Refactor DateInput state

Status: resolved

## Scope

- Extract pure date conversion, Gregorian culture, range, calendar-grid, and
  navigation logic.
- Consolidate only proven Select/MultiSelect navigation duplication.

## Acceptance

- Public API and rendered semantics remain unchanged.
- Pure logic has focused unit coverage.
- Existing form, browser, package, trimming, and AOT gates pass.

## Comments

- Extracted date value conversion, Gregorian calendar calculations, and shared
  Select/MultiSelect navigation into focused internal helpers with unit tests.
- Verified that public API and rendered behavior remain unchanged through the
  complete Release gate for Bzs.Blazor 0.1.12.
