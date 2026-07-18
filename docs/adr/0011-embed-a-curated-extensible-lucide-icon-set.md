# Embed a curated extensible Lucide icon set

Bzs.Blazor will embed only the Lucide SVG data needed by its components and expose it through strongly typed `BzsIconData` values such as the `BzsIcons` catalog, without a JavaScript, npm, font, or host-asset dependency. Consumers may construct additional icon data, icons inherit `currentColor`, accessibility semantics are explicit, and the NuGet package retains Lucide's ISC license and attribution.
