# Bzs.Blazor

`Bzs.Blazor` is a .NET 10 Razor Class Library for compact, themeable Blazor
components. It supports Interactive Server, Interactive WebAssembly,
Interactive Auto, and useful passive markup during static SSR. The runtime
package has no third-party UI or reference-application dependency.

## Install and register

Install the package into both the server host and WebAssembly client projects
that render Bzs.Blazor components:

```text
dotnet add package Bzs.Blazor --version 0.1.0
```

Register the library once during application startup. The call is idempotent,
so shared startup code may call it safely in both hosting models.

```csharp
builder.Services.AddBzsBlazor();
```

The registration establishes the library's standard .NET localization support.
Consumer applications continue to choose render modes, theme persistence, and
their own application-localization policy.

## Localization convention

Library-owned strings use standard `.resx` resource sets. The neutral resource
is English and acts as the fallback; Simplified Chinese companions use the
`.zh-Hans.resx` suffix. Resource marker types and their matching resources live
under `Localization/` in the RCL so public component namespaces remain
`Bzs.Blazor`. Each component adds only the resource keys it owns.

## Package contents

The package includes XML documentation, a portable symbol package, deterministic
build settings, and a `LICENSE` file containing the project MIT license plus
Lucide and Feather attribution. Source Link becomes
resolvable when the repository has both a commit and a real remote URL; the
local verifier reports that metadata boundary explicitly rather than inventing
a repository location. The package is currently in `0.x` development and is
intended for local validation before any public NuGet publication.

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

Release notes are recorded in `docs/releases/0.1.0.md`.
