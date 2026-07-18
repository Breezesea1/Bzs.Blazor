# Bzs.Blazor v0.1

Status: `ready-for-agent`

## Problem Statement

The user needs a personally maintained, general-purpose Blazor component library that can be reused across unrelated applications without importing the historical dependencies, domain models, styling assumptions, or runtime coupling of CoreApi. Existing BZS-owned controls prove that the desired interaction patterns are useful, but they currently live inside an application client assembly, use inconsistent naming and organization, depend on host-owned assets or styling pipelines in places, and do not provide a stable package, theme contract, render-mode guarantee, accessibility target, or release verification seam.

The desired library must preserve the useful behavior learned from MudBlazor, Radzen.Blazor, CoreApi, and CoreApi.Client while remaining its own small, coherent product. It must support compact productivity interfaces with restrained neumorphism, first-class light and dark themes, modern Blazor render modes, native forms, accessible overlays, localization, AOT safety, strict-CSP defaults, and real NuGet consumption.

## Solution

Build `Bzs.Blazor` as a single .NET 10 Razor Class Library and NuGet package with no third-party UI runtime dependency. Organize implementation modules by component concept while exposing one public namespace and a consistent `Bzs` prefix. Provide a semantic theme system, curated extensible Lucide icons, controlled component state, native Blazor form integration, scoped dialog and toast coordination, accessible interaction behavior, localization, and compact light/dark visual defaults.

The first release will deliver a deliberately limited but complete component set: theme provider, surface, icon, button, field and core inputs, message, progress, toast, dialog, drawer, tabs, and the required registration and overlay host. Reference applications are used only as behavioral evidence. The package will be exercised through a real Demo spanning static SSR and interactive render modes, tested at public interfaces, published locally as `0.1.0`, installed into a temporary consumer, and required to pass trimming and WebAssembly AOT checks.

## User Stories

1. As a Blazor developer, I want to install one `Bzs.Blazor` package, so that I can start using the component library without coordinating multiple package versions.
2. As a Blazor developer, I want the package to target .NET 10, so that it aligns with my current applications and toolchain.
3. As a Blazor developer, I want components to work in Interactive Server, so that I can use them in circuit-based applications.
4. As a Blazor developer, I want components to work in Interactive WebAssembly, so that I can run the same controls in the browser.
5. As a Blazor developer, I want components to work in Interactive Auto, so that my application can transition between server and browser execution safely.
6. As a Blazor developer, I want passive component markup to remain meaningful under static SSR, so that noninteractive pages still render usable HTML.
7. As a library consumer, I want the application to choose render modes, so that the component library does not impose an application-wide rendering architecture.
8. As a library consumer, I want the core package to avoid MudBlazor, Radzen.Blazor, Fluent UI, and other UI runtime dependencies, so that I do not inherit unrelated assets or vendor contracts.
9. As a library consumer, I want one public namespace and consistent `Bzs` component names, so that components are easy to discover and import.
10. As a maintainer, I want implementation modules organized by concept, so that related behavior, styling, and tests stay local.
11. As a maintainer, I want internal helpers hidden from consumers, so that implementation can evolve during the 0.x period.
12. As a developer, I want parameters to flow down and `EventCallback` events to flow up, so that component state follows standard Blazor conventions.
13. As a developer, I want bindable value and open-state pairs, so that components work naturally with `@bind`.
14. As a developer, I want form controls based on Blazor's input contracts, so that they participate in `EditForm`, `EditContext`, validation, and culture-sensitive parsing.
15. As a developer, I want components to avoid mutating parameters, so that parent state remains authoritative.
16. As a developer, I want extension through composition, templates, attributes, and CSS variables, so that I can customize controls without subclassing internal state machines.
17. As a designer, I want restrained neumorphic surfaces, so that controls communicate depth without making dense interfaces visually heavy.
18. As a productivity-tool user, I want compact default controls, so that I can scan and operate dense forms efficiently.
19. As a touch user, I want a comfortable density option, so that the same components remain usable on less dense interfaces.
20. As a user, I want a coherent light theme, so that components fit neutral productivity applications.
21. As a user, I want a separately designed dark theme, so that dark mode is not a simple color inversion.
22. As an application developer, I want Light, Dark, and System theme modes, so that I can respect user preference.
23. As an application developer, I want to own theme persistence, so that I can choose local storage, cookies, profiles, or another store.
24. As a security-conscious developer, I want built-in themes to work with strict CSP, so that the library does not require `unsafe-inline`.
25. As a developer with a custom runtime theme, I want an explicit nonce integration path, so that dynamic CSS remains compatible with my CSP policy.
26. As a developer, I want strongly typed semantic theme tokens, so that customization describes purpose instead of raw numbered colors and shadows.
27. As a developer, I want a curated built-in icon catalog, so that common controls do not require an additional icon package.
28. As a developer, I want to supply custom strongly typed icon data, so that the built-in catalog does not limit application-specific icons.
29. As a user, I want buttons and form controls to expose clear focus, disabled, loading, validation, and active states, so that interaction remains understandable without relying on shadows alone.
30. As a form author, I want text, textarea, number, date, checkbox, and select controls, so that I can build standard business forms consistently.
31. As a form author, I want field labels, descriptions, required indication, and errors connected accessibly, so that validation is understandable to all users.
32. As an application developer, I want controlled dialog and drawer components, so that I can own overlay state declaratively.
33. As an application developer, I want an awaitable dialog service, so that confirmation and editing workflows can return explicit results.
34. As an application developer, I want dialog parameters selected through component properties instead of string keys, so that refactoring remains safer.
35. As an application developer, I want dialog completion, cancellation, dismissal, unavailability, and host disposal to be distinguishable, so that workflows do not overload null results.
36. As an application developer, I want a scoped toast service, so that unrelated components can publish notifications without sharing state across users.
37. As a user, I want toast timing, dismissal, and live-region behavior to remain predictable, so that notifications are useful without being disruptive.
38. As a user, I want dialogs to manage initial focus, focus trapping, Escape, backdrop behavior, and focus restoration, so that overlays work with keyboard and assistive technology.
39. As a user, I want tabs to support keyboard navigation and controlled selection, so that tabbed content is accessible and predictable.
40. As a user, I want reduced-motion and forced-colors behavior, so that components remain usable with accessibility preferences enabled.
41. As a user, I want interfaces to remain usable at 200% zoom, so that text enlargement does not cause overlapping or clipped controls.
42. As an application developer, I want English and Simplified Chinese library strings, so that built-in interaction text works in my common locales.
43. As an RTL application developer, I want logical layout properties and inherited document direction, so that the component structure adapts without a separate implementation.
44. As a developer, I want browser support aligned with current stable Chrome, Edge, Firefox, Safari, iOS Safari, and Android Chrome, so that the support promise is clear.
45. As a developer, I want newer platform APIs treated as progressive enhancements, so that core workflows do not fail on browsers that lack Popover or CSS Anchor Positioning.
46. As a developer, I want configuration errors to fail with actionable messages, so that missing or duplicate overlay hosts are not silent runtime defects.
47. As a developer, I want browser or circuit loss to degrade only enhancement behavior, so that callbacks, results, and cleanup are not lost.
48. As a maintainer, I want components verified through public behavior rather than internal markup snapshots, so that refactoring does not create test churn.
49. As a maintainer, I want the package to publish trim- and AOT-clean, so that WASM consumers do not discover hidden reflection failures.
50. As a maintainer, I want the generated NuGet installed into a temporary consumer, so that package metadata and static assets are verified from the consumer's perspective.
51. As a maintainer, I want a real component Demo across render modes, so that visual and lifecycle behavior can be inspected before release.
52. As a maintainer, I want the first version released locally under MIT and SemVer `0.1.0`, so that the public interface can mature before nuget.org publication.
53. As a user, I want inline messages with clear semantic severity and composable content, so that page-level feedback is consistent and accessible.
54. As a user, I want determinate and indeterminate progress with accessible labels and values, so that ongoing work is understandable without relying on animation alone.
55. As a library consumer, I want one documented registration call, so that service lifetimes and defaults are configured consistently in Server and WebAssembly applications.
56. As a library consumer, I want to place one overlay host in each interactive root, so that dialog and toast commands have an explicit rendering destination.

## Implementation Decisions

- Publish one runtime module named `Bzs.Blazor`; keep Demo and test modules outside the package.
- Use .NET 10 as the only target framework for the first release.
- Keep the runtime module free of third-party UI dependencies and reference-application dependencies.
- Organize source by component concept while keeping all public components in one namespace.
- Prefix public components with `Bzs`, CSS classes with `bzs-`, and custom properties with `--bzs-`.
- Use matching Razor and code-behind names for public components; add isolated CSS and collocated ES modules only where needed.
- Provide a lightweight common component base for identity, classes, style escape hatch, and unmatched attributes.
- Build a shared input base on Blazor's native input contract rather than inventing a separate form system.
- Keep component values controlled through standard bindable parameter/event pairs.
- Treat component inheritance as unsupported; expose composition and semantic customization instead.
- Provide `BzsThemeProvider` with Light, Dark, and System requested modes while leaving persistence to the consumer.
- Precompile built-in theme tokens into external CSS for strict-CSP compatibility.
- Require an explicit nonce when arbitrary runtime theme values produce dynamic CSS.
- Prefer consumer-supplied external custom-theme CSS as the strict-CSP customization path.
- Model theme customization with semantic color, depth, shape, typography, motion, and density concepts.
- Represent theme customization as immutable strongly typed records rather than raw string dictionaries.
- Make components consume generated semantic custom properties rather than theme objects or inline visual values.
- Use restrained Base, Raised, Inset, and Overlay surface levels; keep dense controls comparatively flat.
- Default to Compact density and provide Comfortable density.
- Embed a curated Lucide icon subset as strongly typed SVG data and permit custom icon data.
- Ship the initial foundation, form, feedback, overlay, and tab component families defined in the accepted scope.
- Keep DataGrid and other advanced data or media components outside the first release.
- Provide controlled dialog and drawer components.
- Provide scoped dialog and toast interfaces rendered through a single overlay host per interactive root.
- Keep dialog, drawer, and toast public semantics separate while sharing internal overlay coordination.
- Keep drawer command invocation outside the first release.
- Invoke dynamic dialogs by component type and property-expression parameter selection; do not expose string-key parameter dictionaries as the common interface.
- Return explicit typed dialog result states.
- Avoid assembly scanning, runtime type names, dynamic generic construction, expression compilation, and dynamic invocation.
- Generate or otherwise preserve dialog parameter metadata in a trim- and AOT-safe manner.
- Use collocated ES modules and asynchronous disposal for browser behavior; create no global JavaScript object.
- Use standard .NET localization resources for library-owned strings, initially English and Simplified Chinese.
- Follow the current UI culture for parsing and formatting, and inherit host text direction.
- Target WCAG 2.2 AA at the component and built-in-theme level without claiming automatic application compliance.
- Support current evergreen desktop and mobile browsers; defer formal Hybrid and desktop-shell guarantees.
- Use mature dialog primitives with fallback behavior; treat newer positioning and popover APIs as optional enhancements.
- Fall back to built-in light tokens when no theme provider exists.
- Emit meaningful inert markup under static SSR and never invoke browser APIs during prerender.
- Fail fast for invalid parameters, missing hosts, and duplicate hosts; never silently discard command work.
- Return explicit command unavailability only when static SSR has a registered static overlay host; static SSR without a host remains a missing-host configuration error.
- Release under MIT and Semantic Versioning beginning at `0.1.0`.
- Include documentation, licenses, icon attribution, symbols, deterministic build metadata, and Source Link in package output.
- Validate locally before any decision to publish publicly.
- Allow 0.x breaking public-interface changes only when they are documented in release notes.
- Reimplement proven reference-application behavior behind the new interfaces instead of moving application code wholesale.

## Testing Decisions

- The highest acceptance seam is a generated `Bzs.Blazor` NuGet package installed into a temporary consumer application with no project reference. That package consumer is itself the trim/AOT publish target and validates package metadata, static assets, registration, public components, and representative AOT behavior as a real consumer experiences them.
- The primary interactive acceptance seam is the component Demo rendered through static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto. Browser tests exercise behavior through visible UI and public events rather than internal implementation.
- The browser matrix covers Chromium, Firefox, WebKit, Android Chrome emulation, iOS Safari emulation, and Windows Chrome/Edge channels where the environment provides them. Environment-only skips must be explicit.
- bUnit tests cover component parameter/event contracts, form integration, localization, theme output, dialog and toast state transitions, cancellation, disposal, and error behavior.
- Browser tests cover keyboard interaction, focus restoration, backdrop and Escape handling, theme switching, reduced motion, forced colors, zoom, mobile layout, and render-mode lifecycle.
- Accessibility checks combine axe scans with explicit keyboard and focus tests. Automated checks supplement but do not replace manual interaction verification.
- Visual regression snapshots cover representative Light/Dark and desktop/mobile states. They remain deliberately limited and do not turn every pixel change into a release blocker.
- Release publishing performs trimming and WebAssembly AOT and treats IL2xxx and IL3050 warnings as failures.
- Dynamic dialogs, generic inputs, localization, theme switching, and collocated JavaScript modules all run in AOT output to prove that compatibility is functional rather than merely warning-suppressed.
- JavaScript behavior is tested through component outcomes and browser state. Tests do not couple to module-internal implementation details.
- Tests assert roles, accessible names, events, state results, and user-visible behavior. They avoid brittle assertions on private DOM hierarchy and CSS class strings.
- The current repository has no substantive test prior art beyond generated empty test projects. Test scenarios may be informed by proven CoreApi and CoreApi.Client behavior around inputs, dialog/drawer focus, toast state, tabs, and render-mode migration, but no test project references those applications or their domain models.

## Out of Scope

- DataGrid, Tree, Scheduler, Gantt, Charts, Spreadsheet, and pivot data components.
- MultiSelect, ContextMenu, Popover, Tooltip, navigation sidebar, and application-shell drawer families. This does not exclude the controlled overlay component `BzsDrawer`.
- Upload controls, image viewers, video players, and media-specific runtime assets.
- Radzen, MudBlazor, Fluent UI, or other adapter packages.
- CoreApi or CoreApi.Client migration to consume the new package.
- Compatibility aliases for existing application-specific component names.
- Business models, routes, authorization rules, data services, and application workflows.
- A global CSS reset, Tailwind, Sass, PostCSS, or a consumer-side Node build requirement.
- Runtime theme persistence in local storage, cookies, profiles, or another application store.
- Built-in translations beyond English and Simplified Chinese.
- A claim that consumer applications automatically meet WCAG requirements.
- Internet Explorer, obsolete browsers, compatibility modes, and old embedded WebViews.
- Formal support guarantees for MAUI Blazor Hybrid, Electron, or desktop WebView shells.
- Public nuget.org publication during the initial implementation effort.
- Component inheritance as a supported extension mechanism.

## Further Notes

- The repository is a new single-context project with an accepted glossary and 23 ADRs defining the decisions summarized here.
- Initial solution, package, test, and Demo scaffolding already exists, but the v0.1 component implementation has not begun.
- Existing CoreApi and CoreApi.Client source remains read-only reference material during this effort.
- A previous inspection found inconsistent component naming, host-owned Tailwind/assets, business coupling, and at least one suspicious dropdown module-path mismatch in the reference application. These findings reinforce clean reimplementation rather than direct movement.
- The issue tracker for this repository is local Markdown. Implementation tickets under this effort use the `ready-for-agent` status.
