# Let consumers own theme mode persistence

`BzsThemeProvider` will render semantic theme tokens for Light, Dark, and System modes and report mode changes through its interface. Bzs.Blazor may observe `prefers-color-scheme` for System mode, but it will not automatically persist choices in local storage, cookies, or another application-owned store; each consumer application controls that policy.
