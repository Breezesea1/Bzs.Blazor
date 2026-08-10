# Implement navigation menu and breadcrumbs

Type: task
Status: resolved
Blocked by: 01

## Goal

Complete the existing app-shell module with `BzsNavMenu`, `BzsNavItem`, and
`BzsBreadcrumbs`.

## Scope

- Compose persistent route navigation inside `BzsNavigationDrawer` without
  coupling to a particular application router configuration.
- Support icons, labels, nested groups, disabled items, and active state.
- Use native links or `NavLink` behavior where navigation is intended.
- Render semantic breadcrumb navigation with current-page indication and
  overflow behavior for narrow viewports.

## Acceptance Criteria

- Navigation remains useful under static SSR.
- Active state has a non-color indicator and can be consumer-controlled where
  router matching is insufficient.
- Nested navigation has a complete keyboard and disclosure model.
- Breadcrumbs expose an accessible navigation label and current item.

## Verification

- bUnit tests for route and controlled active state, semantics, templates, and
  disabled behavior.
- Demo integration in desktop, mobile, LTR, RTL, light, and dark app shells.
- Browser tests for keyboard navigation and responsive overflow.

## Out Of Scope

- Popup command menus and application-owned authorization filtering.

## Comments

- 2026-08-08: Implemented `BzsNavMenu`, `BzsNavItem`, `BzsBreadcrumbs`, and
  `BzsBreadcrumbItem` with router/controlled active state, nested controlled
  disclosure, disabled behavior, templates, semantic current-page output,
  logical RTL styling, and passive static SSR. Escape bubbles from nested
  links and restores disclosure focus. Eight focused tests pass; browser and
  responsive evidence remains centralized in ticket 12.
