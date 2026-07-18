# Verify components through unit, browser, accessibility, and package tests

Bzs.Blazor will use xUnit and bUnit for public behavior, Microsoft Playwright against a Server/WASM/Auto demo for real lifecycle and interaction, axe plus manual keyboard checks for accessibility, and a limited light/dark desktop/mobile screenshot baseline for visual regressions. Release verification also builds, tests, packs, and installs the produced NuGet package into a temporary consumer project; tests avoid internal DOM and CSS-class assertions.
