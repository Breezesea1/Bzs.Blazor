using Microsoft.JSInterop;

namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// Shared classification of JS interop failures that are expected while the interactive
/// runtime is unavailable (prerender) or while a circuit is tearing down, so hosts can
/// swallow them without duplicating the predicate on an unrelated component's interop.
/// </summary>
internal static class DemoJsInteropExceptions
{
    internal static bool IsTransientInitializationFailure(Exception exception) =>
        exception is JSDisconnectedException or InvalidOperationException or TaskCanceledException;

    internal static bool IsTransientDisposalFailure(Exception exception) =>
        exception is JSDisconnectedException or TaskCanceledException;
}
