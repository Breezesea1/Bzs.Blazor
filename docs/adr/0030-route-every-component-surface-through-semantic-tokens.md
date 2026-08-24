# Route every component surface through semantic tokens

Bzs.Blazor will express every component background, shadow, and border colour
through a `--bzs-*` token rather than a literal value, so a theme change moves
the whole library at once. Literal radii survive only where the shape is the
point — circles, pills, and deliberate corner resets. Modal scrims gain a `--bzs-scrim` token instead of
repeating a hardcoded colour in the dialog, drawer, and navigation drawer, and
that token is defined per scheme so dark mode no longer reuses a light-mode
wash. Every floating panel — dialog, drawer, menu, context menu, popover,
toast, tooltip, select, multi-select, autocomplete, and the date picker's panel
and period menu — uses `--bzs-surface-overlay` with `--bzs-shadow-overlay` and
`--bzs-radius-overlay`, which retires the tooltip's inverted text-as-background
treatment and the date picker's stacked raised-plus-overlay shadow. Per
ADR-0003's rule that dense lists stay comparatively flat, option rows and
calendar day cells tint with `--bzs-surface-inset` on hover instead of lifting
on a raised shadow, and reserve depth for the pressed state. Consumer-facing
override hooks such as `--bzs-app-bar-shadow` and the navigation drawer
background and directional shadows keep their names but fall back to tokens, so
existing overrides continue to work. The scrim is also a strongly typed
`BzsThemeColors.Scrim` value so a custom theme controls it like any other colour;
adding it to that positional record is a breaking constructor change, which is
acceptable before 1.0 and is preferable to a token that only built-in themes can
reach. The Demo Catalog stops maintaining parallel palettes: its `--demo-*` and
`--layout-*` surface, text, border, status, shadow, and radius tokens become
aliases of the library tokens, its two divergent purples collapse into one
brand accent, and the only values it still owns are that accent, the status
`-text` variants the library has no counterpart for, and non-colour geometry.
Because those tokens now derive from `--bzs-*`, the demo's former re-points of
`--bzs-canvas`, `--bzs-text`, and friends would have become circular and are
removed; a consumer override survives only where the demo still owns a real
value, which is what `--bzs-primary` and `--bzs-focus-ring` continue to
demonstrate.
