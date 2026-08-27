# Route every anchored surface through the overlay seam with a DOM contract

Bzs.Blazor routes all eight anchored surfaces — Popover, Tooltip, Menu, ContextMenu, Autocomplete, Select, MultiSelect, and the date picker — through the single internal anchored-overlay module, and expresses each surface's render-time positioning facts as DOM contract attributes (`data-bzs-anchor`, `data-bzs-anchored-panel`, `data-bzs-anchored-match-width`, `data-bzs-anchored-nested-open`) rather than as fields on the overlay state record. Static facts belong in the markup the module already queries, so the state record stays narrow and every adapter learns the same small interface; the state record carries only what genuinely changes between interactions. The platform Popover API remains an optional top-layer mode for surfaces that opt into it, consistent with ADR-0025 treating it as progressive enhancement.

## Consequences

- Escape moves to the module's document-level handler for every surface, so a nested open menu inside a panel must mark itself with `data-bzs-anchored-nested-open` to keep its own Escape level; without that marker the module would close the outer surface and stop propagation before the inner level sees the key.
- `CloseFromBrowserAsync` takes `bool restoreFocus = false` on every anchored surface. This changes the already-shipped parameterless signature on Select, MultiSelect, and the date picker, aligned during 0.5.0 before release.
- Contract attributes are read on every position pass, never cached per instance, because Blazor replaces the panel element across renders.
