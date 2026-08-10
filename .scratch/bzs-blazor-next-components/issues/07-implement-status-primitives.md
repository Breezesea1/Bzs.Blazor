# Implement status primitives

Type: task
Status: resolved
Blocked by: 01

## Goal

Deliver `BzsSkeleton`, `BzsBadge`, `BzsChip`, and `BzsAvatar` as small public
interfaces with meaningful reusable behavior.

## Scope

- Skeleton shapes, sizing, animation policy, and accessible loading guidance.
- Badge semantic variants, bounded counts, and non-color state communication.
- Chip display, selection/removal callbacks, disabled state, and icons.
- Avatar image, initials, icon fallback, size, shape, and accessible naming.
- Reuse existing theme, icon, button, and surface behavior instead of wrapping
  those components behind parallel option systems.

## Acceptance Criteria

- Skeleton animation respects reduced motion and is not exposed as content.
- Badge overflow and zero behavior are deterministic.
- Chip removal and selection are separate explicit commands.
- Avatar image failure produces a stable fallback without layout shift.

## Verification

- bUnit public-behavior and accessibility tests for all states.
- Limited visual baselines for representative light/dark and mobile states.
- Static SSR tests require no browser behavior for baseline rendering.

## Out Of Scope

- Chip drag/drop, presence synchronization, remote avatar fetching services.

## Comments

- 2026-08-08: Implemented `BzsSkeleton`, `BzsBadge`, `BzsChip`, and
  `BzsAvatar` under `Components/Status` with matching Razor, code-behind, and
  isolated CSS files. Added focused bUnit coverage for decorative skeleton
  semantics, bounded badge counts and zero behavior, controlled chip selection
  and separate removal commands, disabled interaction, and accessible avatar
  image/fallback output. `dotnet test tests/Bzs.Blazor.Tests/Bzs.Blazor.Tests.csproj
  --configuration Release` exited 0 with 235 passed, 0 failed, and 0 skipped.
  The first compilation reported expected `RS0016` entries for the new public
  contracts because public API baseline files are outside this ticket's
  assigned ownership; a run with only `RS0016` suppressed also passed all 235
  tests, and the orchestrator must add the consolidated baseline entries before
  final clean verification.
