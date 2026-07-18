# Build the render-mode component Demo

Type: task  
Status: resolved  
Blocked by: 03, 04, 05, 06, 07

## Goal

Turn the Demo into the executable documentation and visual inspection surface for every v0.1 component and render mode.

## Scope

- Replace all template pages and styling with a compact productivity-oriented component catalog.
- Integrate the route and browser harness established by issue 01 rather than redefining it.
- Provide clear sections for theme, foundation, forms, feedback, overlays, and tabs.
- Show representative normal, hover/focus, active, disabled, loading, validation, empty, and error states.
- Provide Light/Dark/System and Compact/Comfortable controls.
- Provide English/Chinese and LTR/RTL demonstrations.
- Exercise static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto using real routed examples.
- Load only Bzs.Blazor assets and Demo-owned CSS; do not load Bootstrap or another UI framework.
- Keep operational controls compact and avoid marketing-page composition.
- Ensure desktop and mobile layouts do not overlap, clip, or shift under interaction.

## Acceptance Criteria

- Every v0.1 public component has an executable example.
- Each render mode has a visible route and reports its active runtime for verification.
- Theme and density controls work without owning persistence inside the library.
- Overlay host placement is correct for each interactive island.
- Browser console and network logs show no component errors during the catalog workflow.
- The Demo is suitable for automated screenshots and accessibility scans.

## Testing

- Build and run the Demo in Release.
- Navigate every route in Chromium and confirm runtime identity, assets, and interaction.
- Capture baseline desktop/mobile Light/Dark screenshots for later regression tests.

## Out of Scope

- Marketing content, authentication, backend APIs, and production hosting.

## Comments

- 2026-07-18: Replaced all render-mode placeholders with one real component workbench covering foundation, forms, feedback, overlays, and tabs. Static SSR emits meaningful package markup; Interactive Server, WebAssembly, and Auto execute the same workflows with one scoped host. Four focused browser tests verify runtime identity, state changes, assets, network, console, typed dialogs, toasts, and controlled components.
