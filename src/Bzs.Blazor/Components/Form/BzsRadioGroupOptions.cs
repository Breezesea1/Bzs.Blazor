namespace Bzs.Blazor;

/// <summary>Selects the visual treatment of a <see cref="BzsRadioGroup{TValue}" />.</summary>
public enum BzsRadioGroupVariant
{
    /// <summary>Uses the connected radio group treatment.</summary>
    Standard,

    /// <summary>Uses individually raised options within an inset segmented surface.</summary>
    Segmented,
}
