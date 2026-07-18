# Complete repository and package foundation

Type: task  
Status: resolved  
Blocked by: none

## Goal

Finish the repository scaffold so every later component task builds against one stable .NET 10 package, test solution, and Auto-mode Demo foundation.

## Scope

- Audit the generated solution and all project references.
- Finish package metadata for `Bzs.Blazor` version `0.1.0`.
- Keep the runtime package free of third-party UI and reference-application dependencies.
- Include MIT licensing, Lucide attribution, README, XML documentation, Source Link, symbols, deterministic output, and trim/AOT metadata.
- Remove unused template components, Bootstrap assets, sample weather/counter behavior, and redundant nested solution artifacts.
- Establish the minimal component-catalog shell, render-mode route placeholders, and Playwright harness that later component tickets extend.
- Establish localization resource naming and fallback conventions; each component ticket owns the resource keys it introduces.
- Establish the idempotent public registration entry point, initially covering framework/localization defaults and ready for later scoped overlay registrations.
- Preserve the accepted repository instructions, glossary, ADRs, spec, and issue tracker configuration.
- Resolve package vulnerability warnings in development/test dependencies.
- Establish clean restore, build, test, and pack commands from the repository root.

## Acceptance Criteria

- The root solution contains the runtime package, unit tests, browser tests, Demo host, and Demo client exactly once.
- All projects target .NET 10 and restore without vulnerability or package downgrade warnings.
- The runtime package has no third-party UI package reference.
- The Demo retains per-page Interactive Auto support and can reference the runtime package from both host and client.
- The Demo and browser-test projects provide an executable harness before component implementation tickets begin.
- The public registration entry point builds in both host and client projects.
- Default template UI and unused Bootstrap assets are gone.
- A Release build and initial package creation succeed.
- `git diff --check` reports no whitespace errors.

## Testing

- Restore and build the complete solution.
- Run the empty/baseline test projects.
- Start the minimal Demo and execute one placeholder browser smoke test.
- Pack `Bzs.Blazor` and inspect package contents for metadata, documentation, licenses, symbols, and static asset layout.

## Out of Scope

- Implementing library components.
- Publishing to nuget.org.

## Comments

- 2026-07-18: Claimed for implementation under the accepted v0.1 spec. Work is limited to repository, package, Demo harness, localization convention, registration foundation, and baseline verification.
- 2026-07-18: Completed foundation, four render-mode placeholders, idempotent registration, unit/browser harnesses, package metadata, template cleanup, pack inspection, and vulnerability scan. Source Link remains enabled but requires the repository's first commit and remote URL for final release verification.
