# Support composition extension, not component inheritance

Consumers extend Bzs.Blazor through render-fragment slots, typed templates, semantic parameters, additional attributes, CSS variables, wrapping components, and interfaces at proven seams. Public components do not expose protected state machines, virtual lifecycle hooks, CSS builders, or browser adapters as compatibility contracts; shared base classes standardize framework behavior but are not advertised as inheritance extension points, preserving implementation locality and freedom to evolve.
