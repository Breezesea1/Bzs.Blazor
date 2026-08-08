# Consolidate interop and CI

Status: resolved

## Scope

- Shared internal JS module lifecycle and transient diagnostic logging.
- Fast pull-request CI and complete `main`/tag release gates.
- Package, symbols, and AOT size budgets.

## Acceptance

- Feature wrappers remain typed and own feature operations.
- Transient interop failures degrade safely and are observable at debug level.
- Pull requests receive build/unit feedback without WASM AOT installation.
- Full release verification remains mandatory on `main` and tags.
- Size budget violations fail release verification with actionable values.

## Comments

- Added a shared typed JS module lifecycle with retry-safe imports, transient
  diagnostics, race-safe disposal, and mandatory cleanup after feature errors.
- Split fast pull-request feedback from full main/tag gates and enforced NuGet,
  symbols, and WASM AOT size budgets.
- Verified through the complete Release gate for Bzs.Blazor 0.1.12.
