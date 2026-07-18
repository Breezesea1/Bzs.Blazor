# Separate public overlay semantics and share internal infrastructure

BzsDialog and BzsDrawer remain controlled Razor components, while command-driven workflows use separate scoped `IBzsDialogService` and `IBzsToastService` interfaces rendered through one `BzsOverlayHost` per interactive application root. Dialog, drawer, and toast keep distinct public semantics; queueing, stacking, dismissal reasons, focus management, scroll locking, and lifecycle-safe browser interop are shared only inside the internal overlay module, and command-driven drawer support is deferred.
