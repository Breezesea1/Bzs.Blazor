namespace Bzs.Blazor;

/// <summary>Defines the preferred logical placement of anchored content.</summary>
public enum BzsPopoverPlacement
{
    /// <summary>Places content below the anchor and aligns logical start edges.</summary>
    BottomStart,
    /// <summary>Places content below the anchor and centers it.</summary>
    Bottom,
    /// <summary>Places content below the anchor and aligns logical end edges.</summary>
    BottomEnd,
    /// <summary>Places content above the anchor and aligns logical start edges.</summary>
    TopStart,
    /// <summary>Places content above the anchor and centers it.</summary>
    Top,
    /// <summary>Places content above the anchor and aligns logical end edges.</summary>
    TopEnd,
    /// <summary>Places content beside the logical start edge of the anchor.</summary>
    Start,
    /// <summary>Places content beside the logical end edge of the anchor.</summary>
    End,
}
