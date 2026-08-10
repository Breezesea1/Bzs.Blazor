# Use a separate anchored overlay module

Bzs.Blazor will implement Popover, Tooltip, Menu, ContextMenu, Autocomplete,
and later picker positioning through one internal anchored-overlay module that
owns anchor measurement, logical placement, collision fallback, outside
interaction, Escape handling, and lifecycle-safe browser interop. These public
modules keep distinct interfaces and controlled durable state, while the
internal implementation remains separate from the full-screen dialog, drawer,
toast, and `BzsOverlayHost` coordination module. CSS Anchor Positioning and the
platform Popover API are progressive enhancements rather than required
foundations, and no public portal or browser-positioning adapter is exposed.
