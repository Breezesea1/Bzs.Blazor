# Require trim-safe and WASM AOT-safe releases

Bzs.Blazor releases must publish under trimming and WebAssembly AOT without IL2xxx or IL3050 warnings and must pass AOT smoke tests for dynamic dialogs, generic inputs, localization, theme switching, and JavaScript modules. Runtime assembly scanning, string type lookup, dynamic generic construction, `Expression.Compile`, and `DynamicInvoke` are excluded; dialog types remain statically reachable and parameter metadata or setters are generated or otherwise proven trim-safe rather than hidden behind warning suppressions.
