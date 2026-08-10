# Use feature-specific typed data providers

Bzs.Blazor will expose separate strongly typed asynchronous provider contracts
for Autocomplete suggestions and DataGrid pages because their request,
cancellation, error, and result semantics differ. DataGrid columns use typed
selectors and templates instead of reflection-based string property paths as
the primary interface, and provider requests use explicit sort, filter, page,
and page-size descriptors without leaking a database or HTTP query language.
Internal cancellation and stale-result suppression may be shared only after
duplicated behavior is proven; runtime assembly scanning, `Expression.Compile`,
`DynamicInvoke`, and dynamic generic construction remain excluded.
