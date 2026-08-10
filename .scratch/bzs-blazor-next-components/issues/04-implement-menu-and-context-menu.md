# Implement Menu and ContextMenu

Type: task
Status: resolved
Blocked by: 02

## Goal

Deliver `BzsMenu`, `BzsMenuItem`, and `BzsContextMenu` with one accessible menu
keyboard model and anchored rendering implementation.

## Scope

- Support button-triggered and context-triggered opening.
- Implement roving focus, Arrow/Home/End navigation, typeahead, Enter/Space,
  Escape, disabled items, separators, and focus restoration.
- Support item icons, shortcuts as display content, checkable state, and nested
  submenus only if the ticket-01 contract includes them.
- Keep command callbacks explicit and exactly once.

## Acceptance Criteria

- ARIA menu roles and state match the implemented interaction pattern.
- Context-menu coordinates are internal implementation detail.
- Pointer and keyboard selection produce the same public command result.
- Closing or disposing nested menus leaves no orphaned anchored layer.

## Verification

- Pure tests for enabled-item navigation and typeahead.
- bUnit tests for roles, state, callbacks, disabled items, and disposal.
- Playwright tests for pointer, keyboard, context invocation, nested placement
  if supported, zoom, RTL, and render-mode lifecycle.

## Out Of Scope

- Persistent route navigation and command palette search.

## Comments

- 2026-08-08: Implemented controlled `BzsMenu`, `BzsContextMenu`, and shared
  `BzsMenuItem` composition with native command activation, roving focus,
  Arrow/Home/End navigation, typeahead, disabled/checkable items, separators,
  internal context coordinates, focus restoration, lifecycle-safe anchored
  interop, and 15 passing focused tests. Native keyboard/context invocation,
  RTL, zoom, and render-mode browser evidence is consolidated under ticket 12.
