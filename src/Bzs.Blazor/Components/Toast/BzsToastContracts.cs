using System.Collections.ObjectModel;

namespace Bzs.Blazor;

/// <summary>
/// Identifies a toast within one scoped <see cref="IBzsToastService" /> instance.
/// </summary>
/// <param name="Value">The opaque toast identifier.</param>
public readonly record struct BzsToastId(Guid Value);

/// <summary>
/// Selects the semantic severity of a toast notification.
/// </summary>
public enum BzsToastSeverity
{
    /// <summary>Reports neutral informational feedback.</summary>
    Information,

    /// <summary>Reports successful completion.</summary>
    Success,

    /// <summary>Reports feedback that needs attention without interrupting the user.</summary>
    Warning,

    /// <summary>Reports an operation failure.</summary>
    Error,
}

/// <summary>
/// Describes why a toast was removed from its scoped service.
/// </summary>
public enum BzsToastDismissReason
{
    /// <summary>The user explicitly dismissed the toast.</summary>
    Manual,

    /// <summary>The configured display duration elapsed.</summary>
    Automatic,

    /// <summary>A later toast with the same group or duplicate key replaced it.</summary>
    Replaced,

    /// <summary>A newer toast exceeded the configured queue limit.</summary>
    Overflow,

    /// <summary>The toast service was disposed with the toast still active.</summary>
    ServiceDisposed,

    /// <summary>The timer infrastructure faulted while the toast was active.</summary>
    Faulted,
}

/// <summary>
/// Describes an interaction that temporarily pauses automatic toast dismissal.
/// </summary>
public enum BzsToastPauseReason
{
    /// <summary>The pointer is over the toast.</summary>
    Hover,

    /// <summary>Keyboard focus is within the toast.</summary>
    Focus,
}

/// <summary>
/// Configures a toast published through <see cref="IBzsToastService" />.
/// </summary>
public sealed record BzsToastOptions
{
    /// <summary>
    /// Gets or sets the required notification message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional concise toast title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the semantic severity of the notification.
    /// </summary>
    public BzsToastSeverity Severity { get; init; } = BzsToastSeverity.Information;

    /// <summary>
    /// Gets or sets an optional display duration. A <see langword="null" /> value
    /// uses the severity-specific default. Use <see cref="Timeout.InfiniteTimeSpan" />
    /// for a toast that remains until explicitly dismissed.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets or sets an optional mutually exclusive group. Publishing another toast
    /// in the same group replaces active group members.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Gets or sets an optional key used to replace a matching active toast.
    /// </summary>
    public string? DuplicateKey { get; init; }

    /// <summary>
    /// Gets or sets whether the toast renders an explicit dismiss control.
    /// </summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>
    /// Gets or sets an optional accessible name for the toast live region.
    /// </summary>
    public string? AccessibleName { get; init; }
}

/// <summary>
/// Configures queue and timing defaults for a scoped <see cref="BzsToastService" />.
/// </summary>
public sealed record BzsToastServiceOptions
{
    /// <summary>
    /// Gets or sets the maximum active toast count. A new toast over this limit
    /// dismisses the oldest active toast with <see cref="BzsToastDismissReason.Overflow" />.
    /// </summary>
    public int MaximumVisibleToasts { get; init; } = 5;

    /// <summary>Gets or sets the default information-toast display duration.</summary>
    public TimeSpan InformationDuration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the default success-toast display duration.</summary>
    public TimeSpan SuccessDuration { get; init; } = TimeSpan.FromSeconds(4);

    /// <summary>Gets or sets the default warning-toast display duration.</summary>
    public TimeSpan WarningDuration { get; init; } = TimeSpan.FromSeconds(6);

    /// <summary>Gets or sets the default error-toast display duration.</summary>
    public TimeSpan ErrorDuration { get; init; } = TimeSpan.FromSeconds(8);
}

/// <summary>
/// Represents immutable toast state rendered by a host or <see cref="BzsToast" />.
/// </summary>
public sealed record BzsToastSnapshot
{
    /// <summary>
    /// Initializes a toast snapshot.
    /// </summary>
    /// <param name="id">The scoped toast identifier.</param>
    /// <param name="severity">The semantic severity.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="title">The optional toast title.</param>
    /// <param name="group">The optional replacement group.</param>
    /// <param name="duplicateKey">The optional duplicate replacement key.</param>
    /// <param name="duration">The resolved display duration, or <see langword="null" /> for persistent toasts.</param>
    /// <param name="dismissible">Whether a visual dismiss control should be rendered.</param>
    /// <param name="accessibleName">The optional accessible name for the toast live region.</param>
    public BzsToastSnapshot(
        BzsToastId id,
        BzsToastSeverity severity,
        string message,
        string? title,
        string? group,
        string? duplicateKey,
        TimeSpan? duration,
        bool dismissible,
        string? accessibleName = null)
    {
        Id = id;
        Severity = severity;
        Message = message;
        Title = title;
        Group = group;
        DuplicateKey = duplicateKey;
        Duration = duration;
        Dismissible = dismissible;
        AccessibleName = accessibleName;
    }

    /// <summary>Gets the scoped toast identifier.</summary>
    public BzsToastId Id { get; }

    /// <summary>Gets the semantic severity.</summary>
    public BzsToastSeverity Severity { get; }

    /// <summary>Gets the toast message.</summary>
    public string Message { get; }

    /// <summary>Gets the optional toast title.</summary>
    public string? Title { get; }

    /// <summary>Gets the optional replacement group.</summary>
    public string? Group { get; }

    /// <summary>Gets the optional duplicate replacement key.</summary>
    public string? DuplicateKey { get; }

    /// <summary>Gets the resolved display duration, or <see langword="null" /> when persistent.</summary>
    public TimeSpan? Duration { get; }

    /// <summary>Gets whether a visual dismiss control should be rendered.</summary>
    public bool Dismissible { get; }

    /// <summary>Gets the optional accessible name for the toast live region.</summary>
    public string? AccessibleName { get; }
}

/// <summary>
/// Provides the immutable active-toast snapshot after a service state change.
/// </summary>
public sealed class BzsToastChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes the event data.
    /// </summary>
    /// <param name="snapshot">The active toast snapshot.</param>
    public BzsToastChangedEventArgs(IReadOnlyList<BzsToastSnapshot> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = new ReadOnlyCollection<BzsToastSnapshot>(snapshot.ToArray());
    }

    /// <summary>Gets the active toast snapshot.</summary>
    public IReadOnlyList<BzsToastSnapshot> Snapshot { get; }
}

/// <summary>
/// Provides the toast and reason for a dismissal.
/// </summary>
public sealed class BzsToastDismissedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes the event data.
    /// </summary>
    /// <param name="toast">The toast that was removed.</param>
    /// <param name="reason">The reason it was removed.</param>
    public BzsToastDismissedEventArgs(BzsToastSnapshot toast, BzsToastDismissReason reason)
    {
        Toast = toast ?? throw new ArgumentNullException(nameof(toast));
        Reason = reason;
    }

    /// <summary>Gets the toast that was removed.</summary>
    public BzsToastSnapshot Toast { get; }

    /// <summary>Gets the removal reason.</summary>
    public BzsToastDismissReason Reason { get; }
}

/// <summary>
/// Publishes and manages toast state within one application service scope.
/// </summary>
public interface IBzsToastService : IAsyncDisposable
{
    /// <summary>
    /// Occurs after active toast state changes. The event data contains a stable snapshot
    /// suitable for a host to render.
    /// </summary>
    event EventHandler<BzsToastChangedEventArgs>? Changed;

    /// <summary>
    /// Occurs after an active toast is removed and exposes its dismissal reason.
    /// </summary>
    event EventHandler<BzsToastDismissedEventArgs>? ToastDismissed;

    /// <summary>
    /// Gets a stable snapshot of active toasts in visual order from oldest to newest.
    /// </summary>
    IReadOnlyList<BzsToastSnapshot> Snapshot { get; }

    /// <summary>
    /// Adds a toast and returns its scoped identifier.
    /// </summary>
    /// <param name="options">The toast content and behavior.</param>
    /// <returns>The identifier of the added toast.</returns>
    BzsToastId Show(BzsToastOptions options);

    /// <summary>
    /// Removes a toast when it is active.
    /// </summary>
    /// <param name="id">The scoped toast identifier.</param>
    /// <param name="reason">The reason for removal.</param>
    /// <returns><see langword="true" /> when the toast was active and removed.</returns>
    bool Dismiss(BzsToastId id, BzsToastDismissReason reason = BzsToastDismissReason.Manual);

    /// <summary>
    /// Pauses automatic dismissal for one interaction reason.
    /// </summary>
    /// <param name="id">The scoped toast identifier.</param>
    /// <param name="reason">The active pause reason.</param>
    /// <returns><see langword="true" /> when the pause state changed.</returns>
    bool Pause(BzsToastId id, BzsToastPauseReason reason);

    /// <summary>
    /// Removes one active pause reason and resumes automatic dismissal when none remain.
    /// </summary>
    /// <param name="id">The scoped toast identifier.</param>
    /// <param name="reason">The pause reason to remove.</param>
    /// <returns><see langword="true" /> when the pause state changed.</returns>
    bool Resume(BzsToastId id, BzsToastPauseReason reason);
}
