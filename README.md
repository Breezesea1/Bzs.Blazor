# Bzs.Blazor

`Bzs.Blazor` is a .NET 10 Razor Class Library for compact, themeable Blazor
components. It supports Interactive Server, Interactive WebAssembly,
Interactive Auto, and useful passive markup during static SSR. The runtime
package has no third-party UI or reference-application dependency.

## Install and register

After a release is published to nuget.org, install the package into both the
server host and WebAssembly client projects that render Bzs.Blazor components:

```text
dotnet add package Bzs.Blazor --version 0.1.1
```

Register the library once during application startup. The call is idempotent,
so shared startup code may call it safely in both hosting models.

```csharp
builder.Services.AddBzsBlazor();
```

The registration establishes the library's standard .NET localization support.
Consumer applications continue to choose render modes, theme persistence, and
their own application-localization policy.

## Run the Demo with Aspire

The Aspire AppHost orchestrates the Demo without adding Aspire dependencies to
the `Bzs.Blazor` runtime package. Start it from the repository root:

```powershell
aspire run
```

The repository-level `aspire.config.json` selects the Demo AppHost. Open the
`bzs-demo` endpoint from the Aspire dashboard. For a parallel local instance
with randomized ports and isolated user secrets, run `aspire run --isolated`.

## GitHub Pages Demo

After GitHub Pages is enabled, the workflow publishes the static demo to
[`https://breezesea1.github.io/Bzs.Blazor/`](https://breezesea1.github.io/Bzs.Blazor/).
The site runs entirely as Interactive WebAssembly and therefore demonstrates
only browser-hosted routes; the Static SSR, Interactive Server, and Interactive
Auto routes remain available through the Aspire-hosted demo above.

The Pages workflow deploys automatically from `master` only while the repository
is public. While the repository is private, push-triggered runs are skipped
because Pages is unavailable under the current repository plan. After Pages is
enabled, run the workflow manually once or make a new `master` commit to deploy.

## Localization convention

Library-owned strings use standard `.resx` resource sets. The neutral resource
is English and acts as the fallback; Simplified Chinese companions use the
`.zh-Hans.resx` suffix. Resource marker types and their matching resources live
under `Localization/` in the RCL so public component namespaces remain
`Bzs.Blazor`. Each component adds only the resource keys it owns.

## Package contents

The package includes XML documentation, a portable symbol package, deterministic
build settings, and a `LICENSE` file containing the project MIT license plus
Lucide and Feather attribution. Source Link maps package sources to the
[GitHub repository](https://github.com/Breezesea1/Bzs.Blazor). The package is
in `0.x` development, where documented breaking changes remain possible.

## Publishing

A strict SemVer tag such as `v0.1.1` starts the GitHub Actions release workflow.
The workflow runs the release gates, packs the package and symbols, and publishes
to nuget.org through the protected `nuget-production` environment. Creating a
tag does not bypass the environment's configured approvals or protections.

Publication uses NuGet.org Trusted Publishing. Configure a GitHub policy for
`Breezesea1/Bzs.Blazor`, workflow `publish-nuget.yml`, and environment
`nuget-production`, then set the environment variable `NUGET_USERNAME` to the
NuGet.org profile username that created the policy (not an email address or
package owner name). GitHub Actions exchanges its
OIDC identity for a short-lived NuGet key; no long-lived NuGet API key is stored.

## Local verification

```text
dotnet restore Bzs.Blazor.slnx
dotnet build Bzs.Blazor.slnx --configuration Release --no-restore
dotnet test Bzs.Blazor.slnx --configuration Release --no-build
dotnet pack src/Bzs.Blazor/Bzs.Blazor.csproj --configuration Release --no-build
```

Run the complete browser, package-only consumer, trimming, and WebAssembly AOT
release gate with:

```powershell
pwsh scripts/verify-release.ps1
```

Release notes are recorded in `docs/releases/0.1.1.md`.
