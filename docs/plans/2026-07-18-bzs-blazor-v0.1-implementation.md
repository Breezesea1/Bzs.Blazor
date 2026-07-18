# Bzs.Blazor v0.1 Implementation Plan

## Component Tree

```text
BzsThemeProvider
├── application content
└── BzsOverlayHost
    ├── service-driven BzsDialog instances
    └── service-driven BzsToast items

BzsField
└── BzsInputBase<TValue> controls

BzsTabs
└── BzsTab items
```

## State Flow

- Theme mode: consumer owns requested mode -> provider resolves effective mode -> cascaded immutable theme context -> components consume CSS tokens.
- Inputs: parent/EditContext owns value -> input emits `ValueChanged` and field notification.
- Dialog: caller enqueues typed request -> scoped coordinator -> host renders component -> dialog context completes result -> caller task completes once.
- Toast: caller publishes options -> scoped service owns bounded list/timers -> host renders -> dismiss reason removes item.
- Tabs: parent can own active value; otherwise the tab list owns only transient selected-tab state initialized from parameters.

## Delivery Order

1. Solution, package metadata, shared build settings, and project references.
2. Theme enums/records/provider, static CSS tokens, density and surface primitives.
3. Icon data/catalog and foundation components.
4. Form base, field shell, text inputs, number/date parsing, checkbox, select.
5. Message and progress.
6. Scoped overlay coordinator, declarative dialog/drawer, typed dialog service, toast service and host.
7. Tabs and keyboard behavior.
8. Demo pages across static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto.
9. bUnit, browser, accessibility, visual, trimming/AOT, and package-consumer verification.

## Definition Of Done

- Every component in the v0.1 scope is present in the Demo and documented by an executable example.
- Public XML documentation covers non-obvious parameters and service behavior.
- No project references `Dy3dTasks` or third-party UI libraries.
- Build/test/pack/AOT gates pass from a clean checkout.
- A local development server is running and its URL is reported for visual review.
