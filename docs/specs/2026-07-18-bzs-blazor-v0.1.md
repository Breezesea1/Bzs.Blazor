# Bzs.Blazor v0.1 Specification

- Status: released
- Target release: `0.1.0`
- Target framework: `.NET 10`

## Product

`Bzs.Blazor` is a personally maintained, general-purpose Blazor Razor Class Library. It is not a CoreApi package and is not a replacement project for MudBlazor or Radzen.Blazor. CoreApi and CoreApi.Client are reference applications whose proven behavior may inform clean implementations.

The first release publishes one MIT-licensed NuGet package with no third-party UI runtime dependency. It supports Interactive Server, Interactive WebAssembly, and Interactive Auto. Passive components render under static SSR; command-driven behavior requires an interactive runtime.

## Visual System

The default visual language is restrained neumorphism for compact productivity tools:

- `Base`, `Raised`, `Inset`, and `Overlay` semantic surfaces.
- Compact controls around 34-36px and 6-8px radii.
- Light uses cool neutral gray surfaces; dark uses charcoal surfaces.
- Blue is the primary accent, with independent success, warning, error, and info colors.
- Dense lists, menus, and data displays remain comparatively flat.
- Shadows never carry state or meaning alone.
- `Compact` is the default density; `Comfortable` is optional.

Built-in light and dark themes are precompiled into external CSS and selected with `data-bzs-theme`. Runtime custom themes require a host-provided CSP nonce. Consumers own theme persistence.

## Source And Package Shape

```text
Bzs.Blazor.slnx
├── src/Bzs.Blazor/
├── tests/Bzs.Blazor.Tests/
├── tests/Bzs.Blazor.BrowserTests/
├── samples/Bzs.Blazor.Demo/
└── samples/Bzs.Blazor.Demo.Client/
```

The package source is organized by concept while all public components use the `Bzs.Blazor` namespace:

```text
Components/
├── Theme/
├── Surface/
├── Icon/
├── Button/
├── Form/
├── Message/
├── Progress/
├── Toast/
├── Dialog/
├── Drawer/
└── Tabs/
```

Public components use the `Bzs` prefix. CSS classes use `bzs-`; CSS custom properties use `--bzs-`. Public components have matching `.razor` and `.razor.cs` files plus optional `.razor.css` and `.razor.js` files. Internal helpers stay internal and concept-local.

## Public Registration

```csharp
builder.Services.AddBzsBlazor();
```

Interactive applications place one host in each interactive root that uses command-driven dialog or toast services:

```razor
<BzsThemeProvider Mode="@mode" ModeChanged="@OnModeChanged">
    @Body
    <BzsOverlayHost />
</BzsThemeProvider>
```

Components and hosts never choose their own render mode.

## Common Component Interface

Public state is controlled:

- Parameters flow down.
- `EventCallback<T>` events flow up.
- Components never mutate parameter values.
- Bindable pairs use standard names such as `Value`/`ValueChanged` and `Open`/`OpenChanged`.
- Only transient focus, animation, hover, and search state stays internal.
- Collection parameters use `IReadOnlyList<T>`.
- Required parameters use `[EditorRequired]`.

`BzsComponentBase` exposes `Id`, `Class`, `Style`, and unmatched attributes. `BzsInputBase<TValue>` derives from Blazor `InputBase<TValue>` and participates in `EditForm`, `EditContext`, validation, culture-sensitive parsing, and static SSR form output.

Components are extended through composition, templates, semantic parameters, attributes, and CSS variables. Component inheritance is not a supported compatibility contract.

## Theme Interface

```csharp
public enum BzsThemeMode { Light, Dark, System }
public enum BzsDensity { Compact, Comfortable }
public enum BzsSurfaceLevel { Base, Raised, Inset, Overlay }
```

`BzsTheme` exposes immutable semantic records for:

- light and dark color schemes;
- depth/shadow tokens;
- shape tokens;
- typography tokens;
- motion tokens.

`BzsThemeProvider` accepts `Theme`, `Mode`, `ModeChanged`, and optional `CspNonce`. Built-in themes emit no inline styles. System mode observes `prefers-color-scheme` only after interactivity begins. Without a provider, components use deterministic built-in light tokens.

## Icons

`BzsIcon` renders strongly typed `BzsIconData`. `BzsIcons` contains the curated Lucide subset required by the package. Icons inherit `currentColor`; decorative icons are hidden from assistive technology, while meaningful icons require an accessible name. Consumers may construct additional icon data. The package retains Lucide ISC attribution.

## v0.1 Components

### Foundation

- `BzsThemeProvider`
- `BzsSurface`
- `BzsIcon`
- `BzsButton`

### Forms

- `BzsField`
- `BzsTextInput`
- `BzsTextArea`
- `BzsNumberInput<TValue>`
- `BzsDateInput<TValue>`
- `BzsCheckbox`
- `BzsSelect<TValue>`

### Feedback

- `BzsMessage`
- `BzsProgress`
- `BzsToast`
- `IBzsToastService`

### Overlay

- `BzsDialog`
- `IBzsDialogService`
- `BzsDrawer`
- `BzsOverlayHost`

### Navigation

- `BzsTabs`
- `BzsTabItem`

DataGrid, Tree, Scheduler, Charts, MultiSelect, ContextMenu, Popover, Sidebar, upload, media viewers, Radzen adapters, and business composites are outside v0.1.

## Dialog Contract

Controlled usage uses `Open` and `OpenChanged`. Command-driven usage follows:

```csharp
var result = await dialogs.ShowAsync<DeleteItemDialog, bool>(
    parameters => parameters.Add(x => x.Item, item),
    new BzsDialogOptions { Title = "Delete item" });
```

Parameter expressions identify properties without `Expression.Compile` or `DynamicInvoke`. `BzsDialogResult<TResult>` distinguishes completed, cancelled, dismissed, unavailable, and host-disposed outcomes. Dialog content completes through a cascaded `BzsDialogContext<TResult>`.

The implementation must remain trim-safe and WASM AOT-safe. Component types are statically reachable; parameter metadata or setters are generated or otherwise proven safe.

## Overlay State

Public dialog, drawer, and toast semantics remain separate. Internally, a scoped coordinator owns queueing, stacking, dismiss reasons, focus restoration, scroll locking, and host registration. Scoped means per circuit under Server and per browser tab under WebAssembly. Duplicate hosts and service calls without a host fail with actionable errors.

Drawer is declarative-only in v0.1. Toast state is service-owned and rendered by the overlay host.

## Localization And Direction

Library-owned strings use `.resx` and `IStringLocalizer`, initially shipping English and Simplified Chinese. Components follow `CultureInfo.CurrentUICulture`; application content remains consumer-owned. CSS uses logical properties and inherits the host `dir`. RTL structure is supported, but no RTL-language translation is claimed in v0.1.

## Accessibility

Built-in components and themes target WCAG 2.2 AA without claiming that consumer applications are automatically compliant. Requirements include keyboard operation, visible focus, accessible labels and validation relationships, dialog focus management, appropriate live regions, reduced-motion support, forced-colors degradation, and usability at 200% zoom.

## Browser Support

Supported: current stable Chrome, Edge, Firefox, Safari, iOS Safari, and Android Chrome. Internet Explorer, obsolete browsers, compatibility modes, and old embedded WebViews are unsupported. MAUI Blazor Hybrid and desktop shells are best effort until dedicated smoke tests exist. Popover and CSS Anchor Positioning are progressive enhancements only.

## Error And Degradation Policy

- Static SSR emits meaningful inert HTML and never invokes browser APIs.
- Missing theme providers fall back to built-in light tokens.
- Missing/duplicate overlay hosts and invalid parameters fail fast.
- Temporary JS or circuit loss may skip focus/scroll enhancement but must not lose callbacks, results, or cleanup.
- Command paths never silently drop work.

## Verification Gates

- xUnit + bUnit public behavior tests.
- Playwright Chromium, Firefox, WebKit, mobile emulation, and Windows branded browser coverage.
- axe and manual keyboard checks.
- Limited light/dark desktop/mobile visual snapshots.
- Release trimming and WASM AOT publish with no IL2xxx or IL3050 warnings.
- `dotnet pack` followed by installation and build in a temporary consumer.

## Release Policy

Package ID, assembly, and root namespace are `Bzs.Blazor`. The package is MIT licensed and starts at `0.1.0`, which is published on nuget.org. Breaking changes are permitted during 0.x only when documented.

NuGet publication is authorized by the controlled push of a strict `v<SemVer>` tag; `nuget-production` does not require an additional reviewer. The release workflow retains exact tag/project-version equality, Ubuntu and Windows release gates, OIDC Trusted Publishing, the `nuget-production` environment, and restricted tag creation and update permissions. Before pushing a release tag, the operator verifies that its peeled commit equals both the intended release SHA and current `origin/master`, and confirms successful CI for that same SHA. The workflow does not currently enforce these commit and CI checks, and the effective tag ruleset has not yet been proven from repository evidence.
