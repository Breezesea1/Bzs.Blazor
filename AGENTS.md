# Bzs.Blazor

| Setting | Value |
| --- | --- |
| Target framework | .NET 10 |
| Library | Razor Class Library |
| Demo interactivity | Auto, per page |
| Public namespace | `Bzs.Blazor` |

## Architecture

- `src/Bzs.Blazor` is the only runtime package.
- Public components use the `Bzs` prefix and stay in the `Bzs.Blazor` namespace.
- Source folders are organized by component concept; folder names do not leak into the public namespace.
- The runtime package has no third-party UI dependency and no dependency on reference applications.
- Components support Interactive Server, Interactive WebAssembly, and Interactive Auto. Passive markup must remain useful under static SSR.
- Consumer applications choose render modes and own theme persistence.

## Component Rules

- Parameters flow down and `EventCallback<T>` events flow up.
- Components never mutate parameter values.
- Form controls derive from `BzsInputBase<TValue>` and integrate with `EditForm` and `EditContext`.
- Extend components through composition, templates, semantic parameters, attributes, and CSS variables, not inheritance.
- Use matching `.razor` and `.razor.cs` files for public components. Add colocated `.razor.css` and `.razor.js` only when needed.
- JavaScript interop uses collocated ES modules, runs only after interactive rendering, and is disposed asynchronously.

## Styling

- Global tokens live in `wwwroot/bzs.blazor.css`; component structure lives in CSS isolation files.
- CSS classes use `bzs-`; custom properties use `--bzs-`.
- Do not add a global reset or require Sass, Tailwind, PostCSS, or a consumer-side Node build.
- Built-in themes must work with `style-src 'self'`; runtime custom theme CSS requires an explicit CSP nonce.

## Verification

- Keep Release trimming and WASM AOT free of IL2xxx and IL3050 warnings.
- Test public behavior, not internal DOM shape or CSS class strings.
- Run build, unit tests, browser tests, pack, and temporary-package consumption before release.

## Agent skills

### Issue tracker

Issues and specs are tracked as GitHub issues on this repo (via the `gh` CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

The repo uses the default five-role label vocabulary. See `docs/agents/triage-labels.md`.

### Domain docs

This is a single-context repo with root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.
