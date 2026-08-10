# Implement Tooltip

Type: task
Status: resolved
Blocked by: 02

## Goal

Deliver `BzsTooltip` through the anchored-overlay module with accessible input
and timing behavior.

## Scope

- Support pointer hover, keyboard focus, and touch-safe dismissal behavior.
- Support configurable show/hide delay, placement, disabled state, and rich
  tooltip content.
- Connect trigger and tooltip semantics without replacing required accessible
  names with optional descriptive text.
- Suppress motion under reduced-motion preferences.

## Acceptance Criteria

- Keyboard-only users can reveal and dismiss the tooltip.
- Tooltips never trap focus or become required for task completion.
- Rapid trigger changes cannot display stale content.
- Disabled, hidden, and disposed tooltips leave no active timers or listeners.

## Verification

- Deterministic timing tests using `TimeProvider` or an equivalent internal
  test seam.
- bUnit accessibility and lifecycle tests.
- Playwright pointer, focus, touch, reduced-motion, zoom, and mobile checks.

## Out Of Scope

- Interactive popover content and menu behavior.

## Comments

- 2026-08-08: Implemented `BzsTooltip` with a contextual trigger template so
  the consumer's real button, link, or input owns focus, events, and
  `aria-describedby` without an extra tab stop. Added cancellable hover/focus
  delays, completed-tap touch behavior, disabled/disposal cleanup, anchored
  positioning, bounded interop retry, rich/plain content, and 9 passing
  focused tests. Browser pointer, touch, reduced-motion, zoom, mobile, and
  render-mode evidence is consolidated under ticket 12.
