# Implement feedback and toast

Type: task  
Status: resolved  
Blocked by: 01, 02, 03

## Goal

Implement inline feedback components and a scoped toast system with predictable timing, dismissal, localization, and accessibility behavior.

## Scope

- Implement Message severity variants and composable content.
- Implement determinate and indeterminate Progress behavior with accessible values and labels.
- Implement Toast rendering, options, IDs, dismiss reasons, queue limits, duplicate/group replacement policy, and default durations.
- Implement a scoped toast service suitable for Server circuits and WASM tabs.
- Implement toast models, visual item rendering, scoped service state, and a host-consumable snapshot/change contract. Do not implement `BzsOverlayHost` in this ticket.
- Pause automatic dismissal during hover and keyboard focus.
- Respect reduced motion and dispose timing/subscription resources correctly.
- Localize library-owned close and status labels.

## Acceptance Criteria

- Toast state is isolated across service scopes.
- Timers complete, pause, resume, and dispose without leaking work.
- Error and warning announcements remain accessible without unnecessarily interrupting the user.
- Queue overflow and replacement behavior are deterministic and observable through dismiss reasons.
- Static SSR renders inline Message and Progress. Toast service state can be tested independently of host rendering.

## Testing

- Unit tests for queue policy, timing, replacement, dismissal reasons, scope isolation, and disposal.
- bUnit tests for Message, Progress, Toast roles, accessible names, and consumer callbacks.
- Browser tests for hover/focus pause, close interaction, reduced motion, and live regions.
- Use the early Demo/browser harness from issue 01; add only focused feedback examples and tests owned by this ticket.

## Out of Scope

- Cross-tab notifications, server push, persistence, and notification history.

## Comments

- 2026-07-18: Completed Message, normalized Progress, localized feedback labels, immutable toast snapshots, deterministic replacement/overflow, scoped timing with multi-reason pause/resume, stale-timer protection, exactly-once dismissal, subscriber isolation, Feedback Demo, and focused browser coverage. OverlayHost rendering and final DI ownership remain Ticket 06.
