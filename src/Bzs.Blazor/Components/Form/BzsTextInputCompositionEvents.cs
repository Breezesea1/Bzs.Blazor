using Microsoft.AspNetCore.Components;

namespace Bzs.Blazor;

[EventHandler("oncompositionstart", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("oncompositionend", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
internal static class BzsTextInputCompositionEvents
{
}
