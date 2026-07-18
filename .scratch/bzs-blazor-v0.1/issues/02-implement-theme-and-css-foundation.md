# Implement theme and CSS foundation

Type: task  
Status: resolved  
Blocked by: 01

## Goal

Implement the semantic theme module that all components consume, including compact density, restrained neumorphic surfaces, Light/Dark/System modes, strict-CSP defaults, and runtime custom-theme nonce support.

## Scope

- Implement common component identity, class, style escape hatch, and additional-attribute behavior.
- Implement semantic theme, color, depth, shape, typography, motion, density, and surface-level types.
- Provide built-in Light and Dark theme values and System-mode behavior.
- Ship global token CSS without a global reset or consumer-side asset build.
- Implement the theme provider and cascaded theme context.
- Keep built-in themes external-CSS-only for `style-src 'self'` compatibility.
- Require a CSP nonce for runtime custom theme CSS and reject unsafe configuration clearly.
- Support external custom-theme CSS as the preferred strict-CSP extension path.
- Use a collocated, lifecycle-safe ES module only where System-mode observation requires browser behavior.
- Support static SSR without browser calls and deterministic Light fallback without a provider.
- Include reduced-motion and forced-colors foundations.

## Acceptance Criteria

- Theme mode and density are consumer-controlled.
- Built-in themes emit no inline style or dynamic style element.
- Runtime custom theme values cannot render without an explicit nonce.
- Light and Dark schemes use independent semantic tokens.
- System mode works after interactive rendering and does not fail during prerender.
- Components can consume tokens without reading theme objects or building inline styles.
- Theme interop is asynchronously disposed and survives circuit disconnection cleanup.

## Testing

- bUnit tests for provider output, controlled mode, custom-theme nonce validation, default fallback, and prerender-safe behavior.
- Browser smoke tests for Light/Dark/System switching and reduced-motion behavior.
- Strict-CSP Demo check confirming built-in themes render under `style-src 'self'`.
- Strict-CSP Demo check confirming an external custom theme can override semantic variables without inline CSS.

## Out of Scope

- Theme persistence.
- Additional named theme families beyond built-in Light and Dark.

## Comments

- 2026-07-18: Completed semantic Light/Dark/System themes, independent depth tokens, Compact/Comfortable density, strict-CSP built-in assets, nonce-protected and scope-validated runtime themes, System-mode interop, accessibility media overrides, Demo coverage, 16 unit tests, and 2 browser tests.
