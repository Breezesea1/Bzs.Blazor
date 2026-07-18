# Add browser, accessibility, and visual gates

Type: task  
Status: resolved  
Blocked by: 08

## Goal

Establish the cross-module browser and accessibility gates that prove the component package works through its public UI rather than only compiling.

## Scope

- Configure Playwright coverage for Chromium, Firefox, WebKit, mobile Chrome emulation, mobile Safari emulation, and Windows Chrome/Edge channels where available.
- Add representative workflows for forms, theme switching, toast, dialog, drawer, tabs, localization, RTL, and render-mode transitions.
- Add axe scans to representative complete component states.
- Add explicit keyboard tests where automated accessibility scans cannot prove behavior.
- Add reduced-motion, forced-colors, 200% zoom, and responsive layout checks.
- Add a deliberately limited visual regression suite for Light/Dark and desktop/mobile key states.
- Keep newer browser APIs progressive and verify fallback behavior.

## Acceptance Criteria

- Supported browser projects pass or have an explicitly documented environment-only skip.
- No critical or serious axe violations remain in the covered Demo states.
- Keyboard workflows complete without pointer input.
- Dialog focus behavior, toast announcements, form validation, and tabs satisfy the accepted accessibility contract.
- Visual baselines cover the agreed representative states without snapshotting every component variation.
- Test failures identify user-visible behavior rather than private markup details.

## Testing

- Run the complete browser matrix against a locally started Release Demo.
- Preserve failure screenshots, traces, and console logs as test artifacts.

## Out of Scope

- Real-device cloud infrastructure procurement and formal external WCAG certification.

## Comments

- 2026-07-19: Completed the Release browser gates with 36/36 focused Chromium tests and all seven matrix targets passing: Chromium, mobile Chrome, Chrome, Edge, Firefox, WebKit, and mobile Safari. Covered axe states reported no critical or serious violations; keyboard/focus, toast/form/tabs accessibility, reduced-motion, forced-colors, 200% reflow, responsive layout, and the four approved Light/Dark desktop/mobile visual baselines passed. Failure screenshots, traces, console/network logs, and matrix TRX files are written under `TestResults/` and `artifacts/release/browser-matrix/`; no environment-only skips were needed on this machine.
