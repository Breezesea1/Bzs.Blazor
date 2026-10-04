namespace Bzs.Blazor;

internal static class BzsSurfaceLevelNames
{
    public static string Name(BzsSurfaceLevel level) => level switch
    {
        BzsSurfaceLevel.Base => "base",
        BzsSurfaceLevel.Raised => "raised",
        BzsSurfaceLevel.Inset => "inset",
        BzsSurfaceLevel.Overlay => "overlay",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The surface level is not supported."),
    };
}
