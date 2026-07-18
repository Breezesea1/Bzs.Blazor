# Implement dialog, drawer, and overlay host

Type: task  
Status: resolved  
Blocked by: 01, 02, 03, 05

## Goal

Implement controlled Dialog and Drawer components plus the scoped, typed dialog command path and shared internal overlay infrastructure.

## Scope

- Implement controlled Dialog and Drawer open-state contracts.
- Implement one overlay host per interactive root with duplicate/missing-host diagnostics.
- Extend the public registration entry point with scoped dialog, toast, coordinator, and host-state lifetimes for both host and client DI containers.
- Integrate the toast service snapshot from issue 05 into the overlay host; this ticket is the sole owner of host rendering and coordinator integration.
- Implement internal scoped coordination for stacking, dismissal, focus restoration, scroll locking, and host disposal.
- Implement dialog service invocation by component type with property-expression parameters.
- Avoid string-key parameter dictionaries, expression compilation, dynamic invocation, runtime type scanning, and dynamic generic construction.
- Implement explicit typed result states for completion, cancellation, dismissal, unavailability, and host disposal.
- Provide a cascaded typed dialog context for completion and cancellation.
- Implement initial focus, focus trapping, Escape, backdrop policy, close button, and focus restoration.
- Keep Drawer declarative-only in v0.1.
- Use collocated ES modules and asynchronous, disconnect-safe disposal.
- Register host lifecycle state during static rendering and interactive activation so the service can distinguish a present static host, an active interactive host, a missing host, and a disposed host.

## Acceptance Criteria

- Controlled and service-driven dialogs share behavior without two sources of truth.
- Dialog tasks complete exactly once under close, cancellation, host disposal, and navigation races.
- Missing and duplicate hosts fail with actionable exceptions.
- Static SSR with a present host can render controlled initial markup and returns explicit unavailability for command calls; static SSR without a host and interactive calls without a host both fail as configuration errors; disposed-host calls return the explicit host-disposed outcome.
- Dialog parameter metadata remains trim- and AOT-safe.
- Drawer supports modal and nonmodal semantics, logical placement, keyboard closure, and accessible labels.

## Testing

- Unit tests for coordinator state machines, scope isolation, exactly-once completion, cancellation races, and host disposal.
- Separate tests for present-static, missing-static, active-interactive, missing-interactive, duplicate, and disposed host states.
- bUnit tests for controlled parameters, callbacks, result states, roles, labels, and backdrop/Escape policy.
- Playwright tests for focus trap, initial focus, focus restoration, scroll locking, nested ordering, and Server/WASM/Auto behavior.
- Use the early Demo/browser harness from issue 01; add only overlay examples and focused browser workflows owned by this ticket.
- AOT smoke test for dynamic dialog creation and parameter delivery.

## Out of Scope

- Command-driven Drawer, Tooltip, Popover, ContextMenu, and arbitrary portal APIs.

## Comments

- 2026-07-18: Completed controlled Dialog/Drawer, scoped typed dialog service, expression-selected parameters, explicit result states, host lifecycle diagnostics, exactly-once coordinator cleanup, toast integration, focus trap/restoration, nested scroll locking, localized controls, Overlay Demo, 21 focused unit tests, and 5 Playwright workflows. The framework-required DAM(All) contract is explicit on dynamic dialog component types; no trimming warning suppression is used. Cross-browser and full render-mode coverage remain Tickets 08-10.
