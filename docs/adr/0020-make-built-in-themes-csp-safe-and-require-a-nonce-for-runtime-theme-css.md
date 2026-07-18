# Make built-in themes CSP-safe and require a nonce for runtime theme CSS

Built-in light and dark tokens are precompiled into self-hosted external CSS and selected through a theme data attribute, so the default path works with a strict `style-src 'self'` policy and emits no style attributes or dynamic style elements. Arbitrary runtime `BzsTheme` values require an explicit host-provided CSP nonce, external custom theme assets remain the preferred strict-CSP extension path, and Bzs.Blazor never requires or silently falls back to `unsafe-inline`.
