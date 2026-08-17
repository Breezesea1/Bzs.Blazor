# Bzs.Blazor

Bzs.Blazor is a personally maintained, general-purpose Blazor component library. It provides reusable UI and visual-system building blocks without depending on a particular business application.

## Language

**Bzs.Blazor**:
The general-purpose component library and visual system developed in this repository for reuse across unrelated Blazor applications.
_Avoid_: CoreApi UI package, DY3D-only design system, Radzen replacement project

**Reference Application**:
An existing application whose proven UI behavior and components may inform Bzs.Blazor without becoming a runtime, domain, or project dependency. Its needs are evidence for capability candidates, not implementation commitments; CoreApi and CoreApi.Client are the initial reference applications.
_Avoid_: Host application, source project, required dependency

**Capability Candidate**:
A general-purpose Bzs.Blazor capability proposed from reference demand or framework analysis and awaiting an Adopt, Hold, Reject, or Merge decision.
_Avoid_: Migration blocker, required feature, parity gap

**Neumorphic Surface**:
A semantic surface treatment that uses restrained highlight and shadow to communicate raised, inset, or overlay depth while preserving contrast and information density.
_Avoid_: Shadow on every element, soft block, global embossing

**Theme Mode**:
The active light, dark, or system-following appearance selected by a consumer application and rendered by the Bzs.Blazor theme provider.
_Avoid_: Stored theme, application preference, color scheme setting

**Anchored Overlay**:
Transient content positioned relative to an on-page anchor or invocation point, including popovers, tooltips, menus, context menus, and autocomplete suggestions.
_Avoid_: Floating panel, popover infrastructure, portal

**Demo Catalog**:
The shared set of demonstration pages and chrome, hosted in `Bzs.Blazor.Demo.Catalog` and reused by the standalone WebAssembly and server demo hosts, through which visitors explore the component library. All visitor-facing copy is bilingual: zh-Hans is the default culture and en-US is available through the language switcher.
_Avoid_: Demo app, sample site, playground

**Landing Page**:
The `/` route of the Demo Catalog, which introduces Bzs.Blazor to first-time visitors and routes them toward installation, the component groups, and releases. Its copy is bilingual (zh-Hans and en-US) with zh-Hans as the primary language.
_Avoid_: Home page, overview page, dashboard
