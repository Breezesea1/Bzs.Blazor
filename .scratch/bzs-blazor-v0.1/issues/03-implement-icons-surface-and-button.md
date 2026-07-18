# Implement icons, surface, and button

Type: task  
Status: resolved  
Blocked by: 01, 02

## Goal

Deliver the first visible vertical slice of the component library using the accepted icon model and semantic surface system.

## Scope

- Implement strongly typed SVG icon data and rendering.
- Embed the curated Lucide icons required by the v0.1 component set with correct attribution.
- Permit consumer-created icon data without an icon provider service.
- Implement semantic Base, Raised, Inset, and Overlay surfaces.
- Implement button variants, sizes, loading state, disabled state, button type, start/end icons, accessible naming, and controlled click events.
- Keep visual states theme-token-driven and avoid hard-coded component theme colors.
- Support Compact and Comfortable density.
- Ensure passive rendering remains meaningful under static SSR.

## Acceptance Criteria

- Decorative and meaningful icons expose correct accessibility behavior.
- Buttons support keyboard activation, submit/reset semantics, disabled/loading behavior, and visible focus.
- Surfaces express semantic depth without heavy nested shadows.
- No component requires JavaScript for baseline behavior.
- Public names and namespaces follow the accepted convention.

## Testing

- bUnit behavior tests for icon semantics, button events, disabled/loading states, attributes, and surface parameters.
- Demo examples for every surface level, button variant, density, Light theme, and Dark theme.
- axe and keyboard smoke checks for the vertical slice.

## Out of Scope

- Form controls and overlay services.

## Comments

- 2026-07-18: Completed strongly typed Lucide-derived icons, semantic surfaces, native buttons, complete Lucide/Feather attribution, external accessible naming, Compact/Comfortable Demo states, 18 focused unit tests, and keyboard/browser smoke coverage. Axe integration remains owned by Ticket 09.
