# Expose strongly typed semantic theme tokens

Consumers customize Bzs.Blazor through immutable, strongly typed semantic theme records covering light and dark color schemes plus shared depth, shape, typography, and motion tokens. `BzsThemeProvider` translates these values into `--bzs-*` custom properties, components consume only those properties, and raw string dictionaries or inline style generation are not part of the primary theme interface; direct CSS-variable overrides remain an escape hatch.
