# Test Static SSR and render handoff

Status: resolved

## Scope

- Package-only Static SSR POST and server validation.
- Form and tab relationships across prerender handoff.
- Dialog startup lifecycle contract.

## Acceptance

- A real browser POST binds all supported native form shapes.
- Invalid POST data produces server-rendered validation messages.
- `for`, `aria-controls`, and `aria-labelledby` references resolve before and
  after activation, and inputs are focusable before and after the handoff.
- Startup dialog calls with a registered static host return `Unavailable` and
  do not enqueue work.

## Comments

- Implemented package-only Static SSR POST and validation coverage, repeated
  multi-select binding, prerender handoff relationship/focus checks, and the
  registered static-host dialog contract.
- Verified through the complete Release gate for Bzs.Blazor 0.1.12.
