# Verification Tiers

How to choose verification for a change in Bzs.Blazor. Three **Verification Tiers** are
cumulative: every tier includes the checks of the tier below it, so a higher tier never
replaces the lower one. Each tier names the repository entry point that performs it; this
guide selects the entry point and points at the scripts and workflows instead of restating
what they do.

| Tier | Entry point | Reach for it when |
| --- | --- | --- |
| Fast | `scripts/verify-fast.ps1` | Any change, and always before the tiers below it. |
| Browser | `tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1`, plus `docs/browser-test-matrix.md` | The change alters user-visible interaction: keyboard, focus, an Anchored Overlay, JavaScript interop, a render mode, responsive behavior, or anything else a visitor can perceive. |
| Release | `scripts/verify-release.ps1` | You are preparing a release, a version bump, a tag, or a package, or you changed trim/AOT-relevant, packaging, or provenance-affecting surface. |

`CONTEXT.md` defines *Verification Tier* and *Release Gate*; this guide is the repository
mapping from change to entry point.

## Fast

`scripts/verify-fast.ps1` is the base tier: it restores `Bzs.Blazor.slnx`, builds it in
Release, and runs `tests/Bzs.Blazor.Tests`. Pass `-Configuration` to override the Release
default. In CI the `fast-verification` job in `.github/workflows/ci.yml` runs exactly this
script on every pull request and on every push to `main`.

Run Fast first for any change. It is the tier that catches a broken build, a failing unit
test, a public-API analyzer error, and a trim/AOT analyzer warning in the library.

## Browser

Real-browser verification is Fast plus the browser checks that cover the behavior you
changed. Functional browser coverage and pixel comparison are separate concerns: this tier
is functional, and the pixel gate belongs to the Linux-owned visual regression described
below.

Reach for it when the change touches, or could plausibly disturb:

- keyboard interaction or focus management
- an Anchored Overlay (popover, tooltip, menu, context menu, select panel, autocomplete, date picker)
- JavaScript interop, including a collocated ES module
- render-mode behavior, including Interactive Auto's WebAssembly upgrade
- responsive or mobile layout behavior
- any other user-visible interaction, including accessibility semantics and RTL handling

`tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1` is the entry point: it restores and
builds first, then runs the target matrix sequentially, and each target starts its own local
Release Demo unless `-DemoBaseUrl` or `BZS_DEMO_BASE_URL` supplies an already-running one.
`docs/browser-test-matrix.md` holds the matrix reference — target order, browser install,
the focused accessibility gate command, and where artifacts land — so change the matrix
there rather than here.

## Release

`scripts/verify-release.ps1` is the complete pre-release entry point: Fast, plus the rest of
the release checks. It reads the version from `src/Bzs.Blazor/Bzs.Blazor.csproj` unless
`-Version` is supplied, and refuses a `-Version` that does not match the project. It runs:

- clean, restore, build in Release, and the full test suite (the test suite drops the visual
  regression tests on a non-Linux host, per the visual baseline rules below)
- the browser matrix through `run-browser-matrix.ps1` unless `-SkipBrowserMatrix` is passed
- `pack` of `src/Bzs.Blazor/Bzs.Blazor.csproj`, including the symbol package, with
  package-content and size-budget assertions
- a temporary package-only consumer copied from `scripts/package-consumer` that must resolve
  the packed package rather than a project reference, plus its smoke run across the
  `server`, `wasm`, and `auto` render modes
- unless `-SkipAot` is passed, a trimmed WebAssembly AOT publish that rejects IL trim and AOT
  warnings, an ICU and satellite-resource check on the published AOT output, the published
  consumer smoke in Production, and the published `_framework` size budget
- a verification summary record under the ignored release output root, including the Source
  Link status

A **Release Gate** is this tier plus the environment-owned checks and provenance
requirements that a developer machine cannot reproduce: the pinned Linux visual regression
job, the tag and version rules, the successful-CI precondition, and the protected
publication environment. `.github/workflows/ci.yml` runs `verify-release.ps1
-SkipVisualRegression` as its `release-gate` job on a push to `main`, because the pixel gate
runs beside it in the `visual-regression` job of the same workflow.
`.github/workflows/publish-nuget.yml` runs the same script in its `verify` job for the
tagged commit and gates publication on that job, the Windows branded-browser job, and the
visual regression job.

### Release provenance

Publication is authorized by a strict `v`-prefixed SemVer tag, and the tag alone does not
authorize anything:

- The tag version must equal the single `Version` property in `src/Bzs.Blazor/Bzs.Blazor.csproj`.
- The tag must point at the current `origin/main` commit; wait for CI to finish rather than
  tagging ahead of it.
- A successful `ci.yml` push run must already exist for that exact commit.
- Publication uses NuGet.org Trusted Publishing: GitHub exchanges its OIDC identity for a
  short-lived key, `NUGET_USERNAME` is set on the `nuget-production` environment, and no
  long-lived API key is stored. Creating the tag does not bypass that environment's
  approvals or protections.
- Source Link comes from `Microsoft.SourceLink.GitHub` with `EmbedUntrackedSources` enabled
  in the package project. `verify-release.ps1` reports in its summary whether a Git remote
  URL is available, so a local run from a clone without `origin` cannot attest to
  resolvable Source Link URLs.

Do not publish by pushing a package by hand or by re-running `pack` outside this sequence;
the verified package and its symbols are the artifacts the workflow uploads and pushes.

## Visual baselines

Pixel baselines are owned and captured by the pinned Linux CI environment, not by a
developer machine or the Windows jobs (ADR-0029:
`docs/adr/0029-own-visual-baselines-on-ci-linux-with-pinned-fonts.md`). The owning jobs
install the pinned fonts and the repository-owned fontconfig at
`.github/fontconfig/99-bzs-visual-baselines.conf` on `ubuntu-latest`, then run
`scripts/verify-visual-regression.ps1`. The `windows-branded-browsers` jobs are
functional-only and deliberately never compare pixels.

A key difference between the tiers: the solution-wide test run carries the visual regression
tests (they live in `tests/Bzs.Blazor.BrowserTests`), and `verify-release.ps1` skips them on
a non-Linux host with a warning because another operating system rasterizes full-page pixels
differently. `-IncludeVisualRegression` forces that advisory comparison anywhere,
`-SkipVisualRegression` always wins, and Linux hosts (including CI) keep the strict default.
Both this script and `scripts/verify-visual-regression.ps1` refuse to run when
`BZS_UPDATE_VISUAL_BASELINES=1`. The Linux CI visual jobs stay strict, so a change that moves
pixels passes the gate only after the baselines below are refreshed.

Sequence for an intended pixel change:

1. Change the styling or theme and push. A pull request runs Fast verification; the release
   gate and the pixel gate are push-only in `ci.yml`, so a green pull request is not pixel
   approval. Expect the push to `main` to fail the `visual-regression` job until step 2 lands.
2. Dispatch the manual `Refresh visual baselines` workflow
   (`.github/workflows/refresh-visual-baselines.yml`). It recaptures baselines in the pinned
   environment through `BZS_UPDATE_VISUAL_BASELINES=1`, runs
   `scripts/restore-equivalent-baselines.ps1` to drop captures the gate would already accept
   (PNG encoding is not byte-reproducible), and pushes a `visual-baselines/<run-id>` branch
   with a compare link. It is the only supported way to update a baseline.
3. Review the proposed images on that branch, keep only the pixel movement the change
   intends, and merge the branch.
4. The merge to `main` re-runs the `visual-regression` job on the pushed commit; that run is
   the post-merge verification, and the same gate runs again in `publish-nuget.yml` for the
   release tag. If the refresh under- or over-shot, dispatch it again rather than editing
   the PNGs by hand.

## Local demo prerequisites

The browser matrix starts the Demo itself with a Release build it does not perform:

```text
dotnet run --project samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Bzs.Blazor.Demo.csproj --configuration Release --no-build --no-launch-profile
```

It sets `ASPNETCORE_ENVIRONMENT=Development` and `DetailedErrors=true`, waits for the
process to report its listening address, and stops the process tree when the run ends. Three
prerequisites follow from that:

- **Build in Release first.** `--no-build` means the Demo must already exist in Release. Run
  the Fast tier before browser work: it builds `Bzs.Blazor.slnx`, which includes the Demo
  projects.
- **Stop a process that holds the build outputs.** A Demo that is already running (started by
  hand, by the Aspire AppHost at `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.AppHost`, or left
  over from an interrupted matrix run) holds the Demo binaries and makes the next build fail
  with a file-in-use error. Stop that process, then rebuild.
- **Run in Development mode before real-browser inspection.** The matrix sets this for you;
  when driving the Demo by hand, keep the Development environment and detailed errors so a
  failure surfaces as an error page instead of a blank page. `dotnet run` on
  `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Bzs.Blazor.Demo.csproj` uses the Development
  launch profile by default.

To inspect a Demo you already have running, pass its address to the matrix as
`-DemoBaseUrl`, or set `BZS_DEMO_BASE_URL`, so no second process is started.

## Keeping this guide accurate

Point at the scripts, workflows, and ADRs; do not copy their commands or their success
criteria here, because a copy drifts. Record no test totals, baseline counts, or timing
budgets as permanent guidance — read them from the scripts and the current run. When a tier
gains, loses, or renames an entry point, update the tier table at the top of this guide and
the pointer in `AGENTS.md` together.
