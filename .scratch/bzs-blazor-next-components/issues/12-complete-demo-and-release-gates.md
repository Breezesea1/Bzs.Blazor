# Complete Demo and release gates

Type: task
Status: resolved
Blocked by: 03, 04, 05, 06, 07, 08, 09, 10, 11

## Goal

Integrate the next component wave into the Demo and prove the complete package
through the repository's release gates.

## Scope

- Add focused Demo pages and navigation for every new public module.
- Exercise Static SSR, Interactive Server, Interactive WebAssembly, and
  Interactive Auto with the same representative public workflows.
- Add axe, keyboard, focus, reduced-motion, forced-colors, zoom, RTL, mobile,
  and visual coverage proportional to risk.
- Update package-consumer fixtures, required static assets, public API baseline,
  XML documentation, README, and release notes.
- Measure package, symbol, and AOT framework size and approve justified budget
  changes explicitly.

## Acceptance Criteria

- Every new public component has an executable, non-marketing Demo example.
- Browser tests cover current Chromium, Chrome, Edge, Firefox, WebKit, mobile
  Chrome, and mobile Safari according to repository policy.
- Package-only consumers exercise anchored interop, Autocomplete, FileUpload,
  and DataGrid from the produced NuGet.
- No unexpected runtime dependency, console error, failed network request,
  IL2xxx warning, or IL3050 warning remains.
- Release notes identify new public contracts and every intentionally deferred
  capability.

## Verification

- `dotnet build Bzs.Blazor.slnx --configuration Release`
- `dotnet test Bzs.Blazor.slnx --configuration Release --no-build`
- `pwsh scripts/verify-release.ps1`
- Manual keyboard review of Menu, Autocomplete, FileUpload, and DataGrid.

## Out Of Scope

- Publishing a tag or NuGet package before the maintainer explicitly approves
  the release candidate.

## Comments

- 2026-08-09: Added one executable Productivity workbench for navigation,
  pagination, status primitives, Popover, Tooltip, Menu, ContextMenu,
  Autocomplete, FileUpload, and provider DataGrid. The same catalog runs under
  Static SSR, Interactive Server, Interactive WebAssembly, Interactive Auto,
  and standalone WebAssembly. The package-only consumer exercises anchored
  interop, Autocomplete, FileUpload, and provider DataGrid from the produced
  NuGet in Server, WebAssembly, Auto, and AOT workflows.
- 2026-08-09: Browser hardening completed with `353/353` unit tests and `73/73`
  browser tests. The browser suite includes `5/5` visual baselines, `12/12`
  accessibility tests, Static SSR and all interactive render modes, standalone
  WebAssembly, axe, keyboard and focus, reduced motion, forced colors, 200%
  reflow-equivalent layout, RTL, and mobile touch. The required seven-target
  matrix passed Chromium, mobile Chrome, system Chrome, Edge, Firefox, WebKit,
  and mobile Safari with no unexpected console error, failed request, or HTTP
  error. Menu, Autocomplete, FileUpload, and DataGrid keyboard workflows are
  retained in the matrix test rather than relying only on a one-time review.
- 2026-08-09: Full `scripts/verify-release.ps1 -Configuration Release` passed
  without skips using the repository-local .NET 10 SDK and `wasm-tools`. The
  gate built with zero warnings and errors, packed and consumed
  `Bzs.Blazor.0.2.0.nupkg`, passed `24/24` package-consumer tests for both source
  and published hosts, and completed trimming and WebAssembly AOT without
  IL2xxx or IL3050 warnings.
- 2026-08-09: Approved measured release budgets for the expanded `0.2.0`
  public API, XML documentation, localized resources, CSS, and two new
  collocated JavaScript assets. The NuGet measured `259563 / 270336` bytes and
  the symbol package measured `154634 / 163840` bytes; their budgets replace
  the `0.1.13` limits of 196608 and 131072 bytes. The AOT `_framework` measured
  `37105493 / 41943040` bytes, so its existing budget remains unchanged.
