# Support all interactive Blazor render modes

Bzs.Blazor will support Interactive Server, Interactive WebAssembly, and Interactive Auto, while allowing passive components to render under static SSR. Consumer applications own render-mode selection; library components must not depend on server-only services or a WebAssembly host environment, and browser-only behavior must be isolated behind lifecycle-safe JS interop.
