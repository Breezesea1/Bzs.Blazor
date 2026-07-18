# Combine global theme tokens with isolated component CSS

Bzs.Blazor will ship one global static stylesheet for semantic theme tokens and genuinely cross-component infrastructure, while individual components keep structural and visual rules in colocated CSS isolation files. Browser behavior uses colocated ES modules, the library does not create global JavaScript objects, and the first release introduces no Sass, Tailwind, PostCSS, Node, global reset, or consumer-side asset build requirement.
