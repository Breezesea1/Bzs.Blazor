# Bzs.Blazor.Demo.WebAssembly

| Setting | Value |
| --- | --- |
| Hosting | GitHub Pages static site |
| Interactivity mode | WebAssembly |
| Interactivity scope | Global |
| Prerendering | None |

## Rendering Configuration

- This project is a standalone WebAssembly host. It has no ASP.NET Core server,
  server prerendering, API endpoints, or authentication backend.
- Shared routes are declared once in `Bzs.Blazor.Demo.Catalog/Routes`, which this
  host's router scans through `AdditionalAssemblies` (ADR-0040). This host
  declares only the home route, which passes `IncludesServerRenderModes="false"`.
- Do not add `@rendermode` directives here. The entire application already runs
  in WebAssembly.

## Hosting Boundary

- Keep server, static SSR, and Interactive Auto examples in the Aspire-hosted
  `Bzs.Blazor.Demo` application.
- Keep reusable example markup and behavior in `Bzs.Blazor.Demo.Catalog` so the
  Aspire and GitHub Pages hosts do not drift.
- Browser code cannot use `HttpContext`, a server file system, server-only
  services, or direct database access.
- GitHub Pages deployment must preserve the `/Bzs.Blazor/` base path, SPA
  `404.html` fallback, and `.nojekyll` marker.
