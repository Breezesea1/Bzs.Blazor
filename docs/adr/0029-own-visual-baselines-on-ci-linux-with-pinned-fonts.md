# Own visual baselines on CI Linux with pinned fonts

Bzs.Blazor will capture and compare its visual regression baselines on GitHub's
Ubuntu runners with an explicitly installed font set (Liberation, Noto core,
and Noto CJK), and will refresh those baselines only through the dispatchable
`Refresh visual baselines` workflow that commits them from that same
environment, because baselines captured on the maintainer's Windows machine and
compared on the heterogeneous `windows-latest` pool failed spuriously whenever
runner font rasterization drifted. The Windows jobs keep only the branded
Chrome and Edge functional matrix, local pixel comparisons become advisory
rather than gating, and residual Ubuntu image drift is recovered through the
same one-click refresh workflow; ADR-0015's baseline commitment keeps its scope
but now names pinned CI Linux as the owning environment.
