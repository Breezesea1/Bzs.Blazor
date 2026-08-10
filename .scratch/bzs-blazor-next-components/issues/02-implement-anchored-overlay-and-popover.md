# Implement anchored overlay and Popover

Type: task
Status: resolved
Blocked by: 01

## Goal

Deliver a controlled `BzsPopover` backed by one deep internal anchored-overlay
module.

## Scope

- Implement anchor measurement, preferred placement, collision fallback,
  viewport constraints, outside interaction, Escape handling, and optional
  focus restoration.
- Use logical start/end placement and support RTL structure.
- Keep CSS Anchor Positioning and the platform Popover API progressive.
- Import browser behavior only after interactive rendering and dispose it
  asynchronously through the repository's lifecycle-safe JS module pattern.
- Preserve trigger and passive content meaning under static SSR.

## Acceptance Criteria

- `Open` and `OpenChanged` remain controlled and exactly-once per transition.
- Repositioning does not loop or visibly detach during scroll, resize, or zoom.
- Disconnect and disposal paths do not leak listeners or hide non-transient
  failures.
- Popover does not require `BzsOverlayHost`.
- The public interface exposes placement intent, not browser coordinates.

## Verification

- Pure tests for placement and collision calculations.
- bUnit tests for controlled state, parameters, callbacks, and static output.
- Playwright tests for scroll, resize, zoom, RTL, outside click, Escape, focus
  restoration, and Server/WASM/Auto lifecycle.

## Out Of Scope

- Tooltip timing, menu keyboard models, arbitrary command-driven portals.

## Comments

- 2026-08-08: Implemented controlled `BzsPopover` and a typed, collocated
  anchored-overlay interop module. Independent review identified and the main
  thread corrected controlled-dismissal desynchronization, fixed-position
  sizing, transformed containing-block coordinates, disabled dismissal,
  focus restoration, transient retry, queued-callback guards, and
  exception-safe cleanup. Nine focused Popover tests pass. The unsuppressed
  Release solution build passes with zero warnings; cross-mode Playwright
  positioning and focus evidence remains centralized in ticket 12.
