# Implement tabs, localization, and RTL completion

Type: task  
Status: resolved  
Blocked by: 01, 02, 03, 04, 05, 06

## Goal

Complete the v0.1 navigation primitive and the cross-cutting language and direction behavior used by every component family.

## Scope

- Implement controlled tab selection and tab-item content composition.
- Implement keyboard navigation, disabled tabs, focus movement, and accessible tab relationships.
- Add English and Simplified Chinese resource sets for all library-owned strings introduced by v0.1 components.
- Audit and complete resource keys introduced by issues 02-06 rather than redefining their ownership.
- Ensure components follow the current UI culture without owning a culture state.
- Replace physical left/right spacing and placement assumptions with logical properties.
- Verify inherited `dir` behavior for forms, tabs, icons, dialog, drawer, toast, and buttons.
- Ensure application-owned text remains parameters or content rather than library resources.

## Acceptance Criteria

- Tabs support arrow, Home, End, activation, disabled-state, and controlled-selection behavior.
- Every library-owned visible or accessible string has an English and Simplified Chinese resource.
- No component hard-codes application copy.
- RTL reverses logical layout where appropriate without reversing semantic icon meaning incorrectly.
- Culture changes affect library strings and number/date formatting through standard .NET mechanisms.

## Testing

- bUnit tests for controlled tabs, keyboard events, roles/relationships, localization fallback, culture switching, and RTL attributes/layout hooks.
- Demo sections in English, Simplified Chinese, LTR, and RTL.
- Browser keyboard tests for tabs in desktop and mobile viewports.

## Out of Scope

- Additional translations and application-level culture persistence.

## Comments

- 2026-07-18: Completed controlled/uncontrolled Tabs, automatic/manual activation, disabled-aware keyboard navigation, conditional browser default suppression, Static SSR quiescence, inherited RTL direction lookup, English/Chinese resource parity, invariant select option values, logical mobile layout, Tabs Demo, 11 focused unit tests, and 6 Playwright workflows. Raw HTTP verification confirms tabs and panels are present before hydration.
