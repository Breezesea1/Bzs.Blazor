# Complete AOT, package, and release verification

Type: task  
Status: resolved  
Blocked by: 01, 02, 03, 04, 05, 06, 07, 08, 09

## Goal

Prove that `Bzs.Blazor 0.1.0` is consumable as a real local NuGet package and satisfies the accepted trimming, AOT, metadata, static asset, and documentation contracts.

## Scope

- Run clean Release build and all unit/browser tests.
- Use the temporary consumer installed from the generated `.nupkg`, with no project reference, as the WebAssembly trimming and AOT publish target.
- Treat IL2xxx and IL3050 warnings as release failures.
- Exercise dynamic dialogs, generic inputs, localization, theme switching, and JS modules in AOT output.
- Pack the runtime package and symbol package.
- Inspect package contents for README, license, Lucide attribution, XML docs, static CSS, scoped CSS, collocated JS, symbols, and deterministic metadata.
- Create a temporary consumer that installs the generated package rather than using a project reference.
- Verify registration, assets, components, overlay host, Server/WASM/Auto compilation, static SSR behavior, and representative AOT workflows from the temporary consumer.
- Produce release notes with a Breaking Changes section, even if empty.
- Record the local package path and final verification commands.

## Acceptance Criteria

- Clean restore, build, unit tests, browser tests, pack, and temporary-consumer build pass.
- The package-installed temporary consumer publishes with trimming and WASM AOT and produces no prohibited warnings.
- The package contains no reference-application or third-party UI runtime dependency.
- Package assets resolve from the consumer without repository-relative assumptions.
- Public XML documentation and licensing metadata are present.
- The work remains local; no nuget.org publish, commit, or push is performed without a separate request.

## Testing

- Run the full release command set from a clean repository state.
- Install the produced `.nupkg` into a newly generated consumer with no project reference, AOT-publish that consumer, and execute dialog, generic input, localization, theme, and JS-module smoke tests from the published output.

## Out of Scope

- Public release, signing infrastructure, CI publication credentials, and CoreApi migration.

## Comments

- 2026-07-19: `pwsh -NoProfile -File eng/verify-release.ps1 -Configuration Release` passed from clean outputs: Release build, 107/107 unit/component tests, 36/36 browser tests, all seven browser-matrix targets, package inspection, isolated NuGet-cache hash verification, and 11/11 package-consumer tests. Package-only Server/WASM/Auto smoke and published Server/AOT smoke exercised static SSR, dialogs, generic inputs, `zh-Hans` localization, themes, and collocated JS/static assets. Standalone WASM AOT compiled 39 assemblies including `Bzs.Blazor.dll` with no `IL2xxx` or `IL3050`; the package is `artifacts/release/packages/Bzs.Blazor.0.1.0.nupkg` and the evidence summary is `artifacts/release/verification-summary.md`. Work remained local at that checkpoint; Source Link was unresolved because the repository did not yet have a configured remote URL.
- 2026-07-19: Re-ran the same full Release gate through `scripts/verify-release.ps1` after the repository cleanup. All test counts, browser targets, package consumers, trimming, and 39-assembly AOT verification passed again. The package now contains one root `LICENSE` with the project MIT license plus complete Lucide ISC and Feather MIT notices, contains no separate `THIRD-PARTY-NOTICES.md`, and records the configured GitHub origin for Source Link.
