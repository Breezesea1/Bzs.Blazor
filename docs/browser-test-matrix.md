# Browser Test Matrix

The browser suite has two execution paths:

| Coverage             | Browser selection           | Purpose                                                                                                                                    |
| -------------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `PageTest` suite     | Playwright Chromium         | Fast, deterministic behavior, axe, forced-colors, zoom, and mobile-emulation gates. It does not read a `BROWSER` environment variable.     |
| `BrowserMatrixTests` | Manual `IPlaywright` launch | A representative Interactive Auto public-UI workflow across desktop engines plus Pixel 5 Chromium and iPhone 13 WebKit device descriptors. |

The accessibility gate covers an Interactive Auto catalog completion, invalid form validation, an open controlled dialog, and tabs. It fails only for axe `critical` or `serious` violations and includes the rule help plus affected nodes in the assertion message. Keyboard/focus behavior remains covered by the focused browser tests.

Pixel baselines are owned by Windows Chromium because operating-system font metrics and rasterization change full-page pixels and mobile document height. Linux release gates run the remaining behavior, accessibility, browser-matrix, package, trimming, and AOT coverage with `verify-release.ps1 -SkipVisualRegression`; the Windows branded-browser job runs all four visual baselines before Chrome and Edge. `verify-visual-regression.ps1` parses the TRX and requires exactly four executed and passed tests, so zero discovery or skipped baselines fail the gate.

## Install

Restore and build the test project before installing the matching Playwright browsers:

```powershell
dotnet restore Bzs.Blazor.slnx
dotnet build Bzs.Blazor.slnx --configuration Release --no-restore
pwsh tests/Bzs.Blazor.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium firefox webkit
```

The sequential matrix target order is `chromium`, `mobile-chrome`, `chrome`, `msedge`, `firefox`, `webkit`, and `mobile-safari`. `mobile-chrome` launches Playwright Chromium with the `Pixel 5` descriptor; `mobile-safari` launches Playwright WebKit with the `iPhone 13` descriptor. Chrome and Edge channel coverage uses locally installed stable browser executables. On Windows the runner probes the standard per-machine and per-user installation directories. On Linux it looks for `google-chrome` or `google-chrome-stable`, and `microsoft-edge`, on `PATH`. The runner reports every unavailable optional engine or channel as an explicit `SKIP`. Chromium is the required local gate; its absence fails the matrix and prints the installation command, while the dependent Pixel 5 target is skipped.

## Run

Run the focused Chromium accessibility gate:

```powershell
dotnet test tests/Bzs.Blazor.BrowserTests/Bzs.Blazor.BrowserTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AccessibilityGateTests"
```

Run the sequential cross-browser matrix. The script restores and builds first, then runs the seven targets in the order above. Each target starts its own local Release Demo unless `BZS_DEMO_BASE_URL` or `-DemoBaseUrl` supplies an already-running demo. The workflow verifies Interactive Auto runtime readiness and counter interaction, theme selection, form save, tabs, controlled-dialog focus and Escape dismissal, drawer close, service-dialog completion, toast visibility, Simplified Chinese content, and inherited RTL keyboard behavior.

```powershell
pwsh tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1
pwsh tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1 -DemoBaseUrl http://127.0.0.1:5000
pwsh tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1 -Targets chrome,msedge
pwsh tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1 -Targets chrome,msedge -RequireAllTargets
```

Set `-Targets` to run only a selected subset in the supplied order. Unknown or duplicate target names are rejected before restore and build. Add `-RequireAllTargets` when every selected browser must be installed; a missing browser then fails instead of skipping. Set `-ArtifactsDirectory` to redirect test output. The default is `TestResults/browser-matrix`.

The runner first uses a nonzero `PLAYWRIGHT_BROWSERS_PATH` when locating installed Playwright browsers. Otherwise it uses `%LOCALAPPDATA%/ms-playwright` on Windows and `$XDG_CACHE_HOME/ms-playwright`, or `$HOME/.cache/ms-playwright` when `XDG_CACHE_HOME` is unset, on Linux.

## Artifacts

Each matrix target writes its TRX result below `TestResults/browser-matrix/<target>/`. The target artifact directory is cleared before each run so stale evidence cannot survive. The manual-launch workflow also preserves `workflow.png`, `rtl.png`, `trace.zip`, `console.log`, `requests.log`, `request-failures.log`, and `responses.log` in that target directory, including when the workflow fails. The trace can be opened with:

```powershell
pwsh tests/Bzs.Blazor.BrowserTests/bin/Release/net10.0/playwright.ps1 show-trace TestResults/browser-matrix/chromium/trace.zip
```

This matrix intentionally does not create visual pixel baselines. Visual snapshot approval remains a separate, deliberately limited release gate.

Focused `PageTest` failures use the same best-effort evidence set under `TestResults/browser-gates/<test>/`: screenshot, trace, console, request, response, and request-failure logs. Each test directory is cleared before new failure artifacts are written.
