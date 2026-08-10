# Version announcement component research

Date: 2026-08-09

## Executive recommendation

Use a **hybrid, non-blocking "What's new" experience** in the Bzs.Blazor Demo:

1. Put a persistent "What's new" entry in the Demo app bar or navigation, with a text-equivalent unread badge.
2. On an interactive page, let an explicit user action open a short, controlled dialog containing the current release summary and a link to the full notes.
3. Keep a dedicated, addressable release page as the canonical long-form experience and the static-SSR fallback.
4. Never auto-open the dialog merely because the Demo version changed. Keep the badge until the release is explicitly acknowledged.

This combines the useful parts of both references. MudBlazor uses an app-bar notification menu, a persisted unread indicator, announcement routes, and a separate full release feed; Radzen uses a dedicated changelog route plus a static `Upd` badge on its navigation entry. Neither inspected implementation uses an automatic release modal, toast, banner, or drawer ([MudBlazor app-bar source](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Shared/AppbarButtons.razor#L3-L29), [MudBlazor routes](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Announcements/AnnouncementOverviewPage.razor#L1-L61), [Radzen changelog metadata](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Services/ExampleService.cs#L4449-L4457)).

The reusable library surface should be a controlled, presentation-only `BzsAnnouncement` composite. Release identity, release content, the decision to show an unread marker, and persistence belong to the Demo. This follows the repository's controlled-state and consumer-owned-persistence decisions rather than putting Demo policy into the runtime package.

## Primary-source evidence

All external sources in this section were accessed on **2026-08-09**. Repository links are pinned to the inspected commits: MudBlazor `007a6a6c9d0dc1a8aba21e3282572f46f862bb0c` and Radzen.Blazor `50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b`.

| Project / surface | Observed current experience | State, accessibility, and responsive details | Primary sources | Access date |
| --- | --- | --- | --- | --- |
| MudBlazor app-bar notifications | A bell opens a menu containing up to five notification links. A dot badge indicates unread notifications and a "Mark as read" command clears it. Each item links to a dedicated announcement route. | The menu has `AriaLabel="Notifications"`; the trigger is wrapped in a tooltip. The unread query and message load run after first render. | [AppbarButtons markup](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Shared/AppbarButtons.razor#L3-L30), [AppbarButtons lifecycle](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Shared/AppbarButtons.razor.cs#L53-L69), [live announcements](https://mudblazor.com/mud/announcements) | 2026-08-09 |
| MudBlazor unread persistence | Read state is stored in browser local storage under a timestamp key. Newness is `PublishDate > lastReadTimestamp`; "mark all" writes the current UTC date, while visiting an announcement can advance the timestamp to that announcement's publish date. | This is per-browser state and requires browser interactivity. The source registers Blazored.LocalStorage and accesses it only through asynchronous calls. | [notification service](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Services/Notifications/InMemoryNotificationService.cs#L10-L60), [service registration](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Extensions/DocsViewExtension.cs#L36-L47) | 2026-08-09 |
| MudBlazor announcement archive and details | `/mud/announcements` renders an editorial card archive; `/mud/announcements/{Id}` renders a full article. The in-memory catalog currently includes major-version announcements for v7, v8, and v9 rather than mirroring every patch release. | The archive grid changes from one column at `xs` to two at `sm` and three at `md`. Announcement detail uses a medium-width container. Visiting the overview marks all as read; visiting a detail route marks that item as read. | [archive route and grid](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Announcements/AnnouncementOverviewPage.razor#L1-L61), [archive read behavior](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Announcements/AnnouncementOverviewPage.razor.cs#L16-L26), [detail route](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Announcements/AnnoucementPage.razor#L1-L34), [detail read behavior](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Announcements/AnnoucementPage.razor.cs#L17-L31), [catalog entries](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Services/Notifications/InMemoryNotificationService.cs#L69-L102) | 2026-08-09 |
| MudBlazor release feed and navigation | A separate `/mud/project/releases` page fetches GitHub releases and renders version/date plus release body. Announcements and Releases are both permanent navigation/footer entries. At access time, the latest GitHub release API response was `v9.8.0`, published 2026-08-05. | Release rows stack at `xs` and split into 4/8 columns at `md`. The mobile docs drawer contains the same app-bar buttons, while the desktop app bar also contains them. | [release page](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Pages/Mud/Project/Releases.razor#L1-L59), [GitHub API client](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Services/GitHubApiClient.cs#L46-L58), [navigation entries](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Shared/NavMenu.razor#L71-L80), [responsive layout](https://github.com/MudBlazor/MudBlazor/blob/007a6a6c9d0dc1a8aba21e3282572f46f862bb0c/src/MudBlazor.Docs/Shared/DocsLayout.razor#L4-L38), [latest release API](https://api.github.com/repos/MudBlazor/MudBlazor/releases/latest), [GitHub releases](https://github.com/MudBlazor/MudBlazor/releases) | 2026-08-09 |
| Radzen.Blazor changelog | `/changelog` is a long, addressable page organized by major version and section. It uses semantic heading tags, lists, anchors, and badges such as Breaking, New, Feature, and Update. At access time, the page began with the v11 changelog. | The content is normal document flow rather than an overlay. Its only local page style adds list spacing, so it can reflow with the surrounding responsive Demo layout. | [changelog source](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Pages/Changelog.razor#L1-L35), [page tail/style](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Pages/Changelog.razor#L877-L889), [live changelog](https://blazor.radzen.com/changelog) | 2026-08-09 |
| Radzen.Blazor changelog discoverability | The changelog is an `Example` with `Updated = true`. The shared navigation template renders that as a visible `Upd` badge beside the entry. This flag is source metadata, not a per-user read-state mechanism. | The navigation item has an `aria-label` equal to the example name. It is rendered in the shared panel menu used by the Demo sidebar. No read/dismiss callback or persistence store appears in this path. | [changelog metadata](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Services/ExampleService.cs#L4449-L4457), [navigation badge](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Shared/NavigationItem.razor#L3-L16), [panel menu](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/RadzenBlazorDemos/Shared/PanelMenu.razor#L5-L39) | 2026-08-09 |
| Radzen.Blazor patch releases | The README links directly to latest/all GitHub releases and states that features ship in frequent releases. At access time, the latest GitHub release API response was `v11.2.2`, published 2026-08-04. This patch-release surface is separate from the major-version changelog page. | GitHub owns the release-page interaction; the Radzen Demo does not add per-user acknowledgment to it. | [README release links](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/README.md#L12-L16), [release cadence statement](https://github.com/radzenhq/radzen-blazor/blob/50cafbbf9b84a0a77d12b5f68a4781c8da0c2e2b/README.md#L78-L84), [latest release API](https://api.github.com/repos/radzenhq/radzen-blazor/releases/latest), [GitHub releases](https://github.com/radzenhq/radzen-blazor/releases) | 2026-08-09 |

### What the references do not establish

The inspected sources do not establish that an auto-opening release modal, announcement drawer, or release toast improves engagement or comprehension. They establish the opposite implementation choice: permanent navigation plus user-initiated menus/pages. The sources also do not establish cross-device read synchronization, authenticated read state, or a standard rule for which patch versions deserve an editorial announcement.

## Local constraints and evidence

| Constraint | Repository evidence | Consequence for this feature |
| --- | --- | --- |
| Render modes | `docs/adr/0001-support-all-interactive-blazor-render-modes.md:3`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Program.cs:20`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Program.cs:103`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/AGENTS.md:12`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/AGENTS.md:16` | The canonical release content and trigger must remain useful in static HTML. Browser persistence and dialog behavior must wait for interactivity and must work in Server, WebAssembly, Auto, and standalone WebAssembly. |
| Existing Demo shell | `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/Layout/MainLayout.razor:13`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/Layout/MainLayout.razor:36`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/Layout/NavMenu.razor:23` | Put the entry in the existing dense app bar or navigation; do not create another shell-level drawer. Keep the full page inside the existing main content region. |
| Controlled state | `docs/adr/0008-use-controlled-component-state-and-native-blazor-form-contracts.md:3`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:27`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:31`; `src/Bzs.Blazor/Components/Popover/BzsPopover.razor.cs:27`; `src/Bzs.Blazor/Components/Popover/BzsPopover.razor.cs:31` | `Open` and unread/read state flow into the component; callbacks request changes. The component must not silently change caller-owned state. |
| Overlay semantics and lifecycle | `docs/adr/0009-separate-public-overlay-semantics-and-share-internal-infrastructure.md:3`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:43`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:132`; `src/Bzs.Blazor/Components/Dialog/BzsDrawer.razor.cs:168` | Reuse the existing dialog semantics and lifecycle-safe interop. Do not make a version-announcement service, a command-driven drawer, or a second overlay infrastructure. |
| Passive SSR degradation | `docs/adr/0018-degrade-passive-rendering-and-fail-fast-on-command-configuration-errors.md:3`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/Routes.razor:4`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/App.razor:19` | Render a normal link to the dedicated page before interactivity. Do not render a modal as the only route to release information. Preserve route focus behavior and the skip-link/main-content structure. |
| Accessibility target | `docs/adr/0012-target-wcag-2-2-aa-without-claiming-application-compliance.md:3`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:93`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:101`; `src/Bzs.Blazor/Components/Dialog/BzsDialog.razor.cs:108`; `src/Bzs.Blazor/Components/Toast/BzsToast.razor.cs:65` | Use a labelled dialog with existing focus management, make unread status available without color/shadow alone, and avoid treating a non-urgent release as an assertive live-region toast. |
| Localization and RTL | `docs/adr/0014-use-standard-dotnet-localization-and-support-rtl-structure.md:3`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Program.cs:8`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Program.cs:10`; `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components/Layout/MainLayout.razor:33` | Library-owned labels use `.resx`/`IStringLocalizer`; release prose remains Demo-owned and must be supplied per supported culture. Use logical CSS properties and culture-formatted dates. |
| Visual language and density | `docs/adr/0003-use-restrained-semantic-neumorphism.md:3`; `docs/adr/0019-default-to-compact-productivity-oriented-visual-density.md:3` | Use one restrained raised/overlay surface, compact controls, and a comparatively flat release list. Do not build decorative nested cards or make shadow the unread signal. |
| Existing release source material | `docs/releases/0.2.0.md:1`; `docs/releases/0.2.0.md:7`; `docs/releases/0.2.0.md:26`; `docs/releases/0.2.0.md:57` | The Demo announcement should summarize, categorize, and link to release information rather than creating a second authoritative release narrative. |
| Persistence ownership precedent | `docs/adr/0004-let-consumers-own-theme-mode-persistence.md:3` | Keep local-storage policy in the Demo. The runtime library should expose state and callbacks, not choose a browser key or persistence backend. |

## Decision matrix

Scores use 1 (poor) through 5 (strong) for the Bzs.Blazor Demo. "Interruption" is scored higher when the pattern is less disruptive.

| Pattern | Discoverability | Low interruption | Durable/detail capacity | Static SSR | Mobile | Per-upgrade semantics | Decision |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Toast | 4 | 3 | 1 | 1 | 3 | 2 | Reject for release announcements. It is transient, competes with operational feedback, and its live-region semantics are disproportionate for non-urgent content. |
| Auto-opening modal/dialog | 5 | 1 | 3 | 1 | 3 | 5 | Reject auto-open. It interrupts the task, requires interactive focus management, and cannot be the only static-SSR path. |
| User-invoked modal/dialog | 4 | 4 | 3 | 2 | 4 | 5 | Use only as the short interactive summary. Keep full notes elsewhere and size it as a mobile-safe scrolling surface. |
| Drawer/panel | 3 | 3 | 4 | 2 | 2 | 4 | Reject for v1. It duplicates or competes with the Demo's navigation drawer and adds responsive state without improving the core task. |
| Banner | 5 | 3 | 2 | 5 | 3 | 4 | Defer. A dismissible banner can work, but it consumes persistent content space and is more visually dominant than the reference implementations. |
| Dedicated page | 2 | 5 | 5 | 5 | 5 | 2 | Required as the canonical history and fallback, but insufficient alone because users may not discover an upgrade. |
| Hybrid entry + dialog + page | 5 | 5 | 5 | 5 | 5 | 5 | **Recommended.** The entry carries unread state, the dialog provides a concise summary after user intent, and the page carries durable detail. |

## Recommended v1 contract and state ownership

### Reusable library component

Add a controlled, composition-oriented `BzsAnnouncement` only if this experience is intended for library consumers, not just the Demo. It should compose existing `BzsBadge`, `BzsButton`/link, and `BzsDialog` behavior rather than introduce another overlay service.

Recommended public inputs and events:

| Member | Purpose |
| --- | --- |
| `bool Open` / `EventCallback<bool> OpenChanged` | Controlled dialog visibility. |
| `bool Unread` | Presentation-only unread state; the component does not persist or mutate it. |
| `string Title` | Required, caller-owned announcement heading. |
| `string? Version` and `DateTimeOffset? PublishedAt` | Optional display metadata; neither determines unread state. |
| `string DetailsHref` | Required addressable fallback/canonical destination. The trigger remains a real link when interactivity is unavailable. |
| `RenderFragment? TriggerContent` | Optional trigger customization with a localized library default such as "What's new". |
| `RenderFragment ChildContent` | Required concise summary content. |
| `RenderFragment? Actions` | Optional composed actions; the component should not prescribe package/update behavior. |
| `EventCallback Acknowledged` | Explicit request for the owner to record this announcement as read. Separate from closing. |
| `EventCallback<BzsDialogDismissReason> Dismissed` | Reports close button, Escape, or backdrop dismissal through existing semantics. Dismissal does not imply acknowledgment. |
| `string? AccessibleName`, `string? UnreadAccessibleText` | Override library-owned accessible labels without encoding Demo copy in the library. |

Do **not** include a release catalog, `ReleaseId`, semantic-version comparison, local-storage key, data fetcher, NuGet/GitHub client, auto-open policy, or Demo route in the reusable component. Those are product/application concerns, not presentation.

If no non-Demo consumer is identified before implementation, the safer v1 is a Demo-local `DemoVersionAnnouncement` composed from existing public components. Extract `BzsAnnouncement` only after the second use validates the generic contract; this avoids publishing a release-domain API based on one sample.

### Demo-owned release data

The Demo should own a small typed catalog such as `DemoReleaseAnnouncement` with a stable, opaque `Id`, localized title/summary, version label, publish date, details route, and optional categories. The catalog should be source-controlled with the Demo and should point to the existing `docs/releases/<version>.md` narrative or a Demo route derived from the same release input. Do not make `src/Bzs.Blazor` read repository files or depend on the sample.

Use the latest editorial announcement as the badge target. Do not automatically announce every patch: the MudBlazor and Radzen sources both separate curated major-version/changelog content from their complete GitHub release streams, as shown in the evidence table.

### Demo-owned persistence and interaction sequence

Use a Demo-only abstraction such as `IDemoAnnouncementReadStore`. A browser-local implementation can store a versioned JSON set of acknowledged opaque IDs, for example under `bzs.demo.announcements.read.v1`. A set is safer than a timestamp because it does not depend on clock ordering and remains stable across rollback/redeployment.

1. During static SSR or prerender, render the ordinary "What's new" link and the full release route. Do not guess unread state and do not auto-open.
2. After interactive rendering, load the read store through lifecycle-safe JS interop.
3. If the current announcement ID is absent, set the Demo-owned `Unread` state and expose it visually and in the trigger's accessible name.
4. When the user activates the entry, open the controlled summary dialog. Opening or closing via Escape/backdrop does not mark it read.
5. When the user invokes "Mark as read"/"Got it", persist the ID, clear `Unread`, and close. A deliberate visit to the dedicated detail page may perform the same acknowledgment after interactive rendering.
6. If storage is unavailable, keep the feature functional in memory for the current session and leave the canonical page reachable. Persistence failure must not block navigation or dialog dismissal.

## Cross-cutting requirements

### Accessibility

- Give the trigger a visible text label where space allows. If it is icon-only, provide an accessible name and tooltip.
- Express unread state in text available to assistive technology, for example "What's new, unread"; a dot, color, or neumorphic shadow is supplementary only.
- Use the existing labelled dialog semantics, focus containment, Escape handling, focus restoration, and dismissal reasons.
- Do not announce initial unread-state hydration through an assertive live region. Release information is not an error or urgent operational event.
- Keep "Close" and "Mark as read" distinct. Escape/backdrop dismissal must not silently acknowledge content.
- Preserve semantic headings and ordinary links on the dedicated page. Focus the page heading through the Demo's existing route-focus behavior.

### Responsive behavior

- Keep the app-bar entry compact, but provide a stable touch target and prevent the unread badge from changing layout dimensions.
- Constrain the dialog to the viewport, use one content column on narrow screens, and scroll the dialog body rather than the page behind it.
- Limit the dialog to a short summary. Put long categorized notes, migration instructions, and code samples on the dedicated page.
- Do not open a second drawer over the existing responsive navigation drawer.

### SSR and render modes

- The release page and trigger link are the baseline. Dialog opening and local-storage access are progressive enhancements after interactivity.
- Avoid reading browser storage in initialization or prerender lifecycle methods. Load it after interactive rendering and tolerate temporary JS/circuit unavailability.
- Prevent an unread-badge flash by rendering an indeterminate/neutral trigger until state is loaded, rather than assuming every prerendered request is unread.
- Verify the trigger's fallback URL under both the hosted Demo root and the standalone GitHub Pages base path.
- Do not place Demo release data or storage code in the runtime package; that would couple static SSR and WebAssembly consumers to an application policy.

### Localization and RTL

- Localize library-owned labels through the existing resources. Keep version strings and opaque IDs invariant.
- Supply Demo-owned release title, summary, category labels, and action copy for `en-US` and `zh-Hans`; use the active culture to format dates.
- Use logical CSS properties, preserve document direction, and verify start/end icon and badge placement in RTL.
- Treat a missing translation as an explicit content-quality issue; do not silently combine languages inside one announcement.

### Persistence semantics

- Scope state to browser/profile by default and document that private browsing, storage clearing, or another device may show the badge again.
- Persist only explicit acknowledgment. Do not equate opening the navigation menu, closing the dialog, or timing out with read state.
- Version the storage schema and keep announcement IDs stable even if localized copy changes.
- Do not store release prose, user identity, telemetry, or timestamps unless a later requirement needs them.

## Risks and non-goals

### Risks

- **Premature public API:** a release-specific library component may encode Demo policy. Mitigation: start Demo-local unless a second consumer exists, and keep any extracted component presentation-only.
- **Two release narratives:** a Demo catalog can drift from `docs/releases`. Mitigation: designate the release note as canonical, keep the dialog summary short, and add release-process validation when implemented.
- **Hydration mismatch or badge flash:** prerender cannot know browser-local read state. Mitigation: render a neutral SSR state and resolve it only after interactivity.
- **Per-browser repetition:** local storage does not synchronize across devices and can be cleared. This is acceptable for a public Demo unless authenticated state becomes a requirement.
- **Overlay overload:** long notes make the dialog difficult on mobile and at zoom. Mitigation: enforce summary-sized content and route to the dedicated page.
- **Base-path errors:** the standalone WebAssembly site uses a non-root base path. Mitigation: generate links through the Demo's existing route/base-path helpers and test both hosts.

### Non-goals for v1

- Detecting the consumer application's installed NuGet package version.
- Fetching GitHub or NuGet release data at runtime.
- Automatically opening an announcement on first load or after every patch.
- Cross-device or authenticated read-state synchronization.
- A general notification inbox, release toast queue, announcement drawer, or marketing banner system.
- Rendering untrusted remote Markdown/HTML inside the dialog.
- Replacing the existing `docs/releases` files or GitHub release process.
