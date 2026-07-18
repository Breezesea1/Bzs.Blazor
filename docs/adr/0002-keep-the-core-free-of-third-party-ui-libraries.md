# Keep the core free of third-party UI libraries

The `Bzs.Blazor` core package will depend only on .NET and ASP.NET Core Blazor abstractions, owning its component behavior, theme tokens, CSS, icons, and required browser interop. Integrations with libraries such as Radzen will live in optional adapter packages so general consumers do not inherit unrelated UI dependencies or styling contracts.
