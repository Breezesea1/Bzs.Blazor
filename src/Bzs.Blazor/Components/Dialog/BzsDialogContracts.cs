using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Bzs.Blazor;

/// <summary>
/// Identifies how a command-driven dialog finished.
/// </summary>
public enum BzsDialogResultKind
{
    /// <summary>The dialog content supplied a result.</summary>
    Completed,

    /// <summary>The dialog content or caller cancelled the dialog.</summary>
    Cancelled,

    /// <summary>The dialog was dismissed without a content result.</summary>
    Dismissed,

    /// <summary>The registered host is static and cannot run a dialog command.</summary>
    Unavailable,

    /// <summary>The registered host was disposed before the dialog finished.</summary>
    HostDisposed,
}

/// <summary>
/// Identifies the interaction that dismissed a command-driven dialog.
/// </summary>
public enum BzsDialogDismissReason
{
    /// <summary>The dialog's close control was used.</summary>
    CloseButton,

    /// <summary>The dialog backdrop was used.</summary>
    Backdrop,

    /// <summary>The Escape key was used.</summary>
    Escape,

    /// <summary>The dialog was dismissed by application code.</summary>
    Programmatic,
}

/// <summary>
/// Configures a command-driven dialog request.
/// </summary>
public sealed record BzsDialogOptions
{
    /// <summary>Gets or sets the optional visible dialog title.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the accessible name used when a visible title is not sufficient.
    /// </summary>
    public string? AccessibleName { get; init; }

    /// <summary>Gets or sets whether the dialog blocks interaction with its background.</summary>
    public bool Modal { get; init; } = true;

    /// <summary>Gets or sets whether Escape may dismiss the dialog.</summary>
    public bool CloseOnEscape { get; init; } = true;

    /// <summary>Gets or sets whether a backdrop interaction may dismiss the dialog.</summary>
    public bool CloseOnBackdropClick { get; init; } = true;

    /// <summary>Gets or sets whether the host renders a close control.</summary>
    public bool ShowCloseButton { get; init; } = true;

    /// <summary>
    /// Gets or sets an optional selector used by an interactive host as the initial
    /// focus target. Invalid or unavailable targets fall back to the host default.
    /// </summary>
    public string? InitialFocusSelector { get; init; }
}

/// <summary>
/// Represents the explicit outcome of a command-driven dialog.
/// </summary>
/// <typeparam name="TResult">The value supplied by dialog content on completion.</typeparam>
public sealed record BzsDialogResult<TResult>
{
    private BzsDialogResult(
        BzsDialogResultKind kind,
        TResult? value = default,
        BzsDialogDismissReason? dismissReason = null)
    {
        Kind = kind;
        Value = value;
        DismissReason = dismissReason;
    }

    /// <summary>Gets the outcome kind.</summary>
    public BzsDialogResultKind Kind { get; }

    /// <summary>Gets the outcome kind using result-oriented terminology.</summary>
    public BzsDialogResultKind Status => Kind;

    /// <summary>Gets the value supplied when <see cref="Kind" /> is <see cref="BzsDialogResultKind.Completed" />.</summary>
    public TResult? Value { get; }

    /// <summary>Gets the value supplied when the dialog completed.</summary>
    public TResult? Result => Value;

    /// <summary>Gets the dismissal reason when <see cref="Kind" /> is <see cref="BzsDialogResultKind.Dismissed" />.</summary>
    public BzsDialogDismissReason? DismissReason { get; }

    /// <summary>Gets whether dialog content completed the request.</summary>
    public bool IsCompleted => Kind == BzsDialogResultKind.Completed;

    /// <summary>Gets whether the request was cancelled.</summary>
    public bool IsCancelled => Kind == BzsDialogResultKind.Cancelled;

    /// <summary>Gets whether the request was dismissed.</summary>
    public bool IsDismissed => Kind == BzsDialogResultKind.Dismissed;

    /// <summary>Gets whether the registered host was static.</summary>
    public bool IsUnavailable => Kind == BzsDialogResultKind.Unavailable;

    /// <summary>Gets whether the host was disposed.</summary>
    public bool IsHostDisposed => Kind == BzsDialogResultKind.HostDisposed;

    /// <summary>Creates a completed result.</summary>
    public static BzsDialogResult<TResult> Completed(TResult value) =>
        new(BzsDialogResultKind.Completed, value);

    /// <summary>Creates a cancelled result.</summary>
    public static BzsDialogResult<TResult> Cancelled() =>
        new(BzsDialogResultKind.Cancelled);

    /// <summary>Creates a dismissed result.</summary>
    public static BzsDialogResult<TResult> Dismissed(
        BzsDialogDismissReason reason = BzsDialogDismissReason.Programmatic) =>
        new(BzsDialogResultKind.Dismissed, dismissReason: reason);

    /// <summary>Creates a result for a static, noninteractive host.</summary>
    public static BzsDialogResult<TResult> Unavailable() =>
        new(BzsDialogResultKind.Unavailable);

    /// <summary>Creates a result for a disposed host.</summary>
    public static BzsDialogResult<TResult> HostDisposed() =>
        new(BzsDialogResultKind.HostDisposed);
}

/// <summary>
/// Provides dialog content with exactly-once completion operations.
/// </summary>
/// <typeparam name="TResult">The dialog result value.</typeparam>
public sealed class BzsDialogContext<TResult>
{
    private readonly Func<TResult, bool> _complete;
    private readonly Func<bool> _cancel;
    private readonly Func<BzsDialogDismissReason, bool> _dismiss;

    internal BzsDialogContext(
        Func<TResult, bool> complete,
        Func<bool> cancel,
        Func<BzsDialogDismissReason, bool> dismiss)
    {
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _dismiss = dismiss ?? throw new ArgumentNullException(nameof(dismiss));
    }

    /// <summary>Completes the dialog with <paramref name="result" />.</summary>
    /// <returns><see langword="true" /> when this operation completed the dialog.</returns>
    public bool Complete(TResult result) => _complete(result);

    /// <summary>Cancels the dialog.</summary>
    /// <returns><see langword="true" /> when this operation completed the dialog.</returns>
    public bool Cancel() => _cancel();

    /// <summary>Dismisses the dialog without a content result.</summary>
    /// <param name="reason">The interaction that dismissed the dialog.</param>
    /// <returns><see langword="true" /> when this operation completed the dialog.</returns>
    public bool Dismiss(BzsDialogDismissReason reason = BzsDialogDismissReason.Programmatic)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "The dialog dismissal reason is not supported.");
        }

        return _dismiss(reason);
    }
}

/// <summary>
/// Builds trim-safe component parameters for one dialog request.
/// </summary>
/// <typeparam name="TComponent">The dialog content component.</typeparam>
public sealed class BzsDialogParameterBuilder<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>
    where TComponent : IComponent
{
    private readonly List<BzsDialogParameter> _parameters = [];
    private readonly HashSet<string> _parameterNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds a value for a public, writable <see cref="ParameterAttribute" /> property.
    /// </summary>
    /// <typeparam name="TValue">The property value type.</typeparam>
    /// <param name="property">A simple component-property expression.</param>
    /// <param name="value">The value delivered to the component.</param>
    /// <returns>This builder for fluent parameter construction.</returns>
    public BzsDialogParameterBuilder<TComponent> Add<TValue>(
        Expression<Func<TComponent, TValue>> property,
        TValue value)
    {
        ArgumentNullException.ThrowIfNull(property);

        var propertyInfo = GetParameterProperty(property);
        if (!_parameterNames.Add(propertyInfo.Name))
        {
            throw new InvalidOperationException(
                $"The dialog parameter '{propertyInfo.Name}' was supplied more than once for '{typeof(TComponent).Name}'.");
        }

        _parameters.Add(new BzsDialogParameter(propertyInfo.Name, value));
        return this;
    }

    internal IReadOnlyList<BzsDialogParameter> Build() => _parameters.ToArray();

    private static PropertyInfo GetParameterProperty<TValue>(
        Expression<Func<TComponent, TValue>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException(
                "A dialog parameter expression must be a simple public instance [Parameter] property access.",
                nameof(expression));
        }

        if (memberExpression.Expression is null)
        {
            throw new ArgumentException(
                "A dialog parameter expression cannot target a static property.",
                nameof(expression));
        }

        if (memberExpression.Expression is not ParameterExpression parameterExpression
            || parameterExpression != expression.Parameters[0])
        {
            throw new ArgumentException(
                "A dialog parameter expression must access a property directly on the dialog component.",
                nameof(expression));
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new ArgumentException(
                "A dialog parameter expression must identify a property.",
                nameof(expression));
        }

        if (propertyInfo.GetIndexParameters().Length != 0)
        {
            throw new ArgumentException(
                "A dialog parameter expression cannot target an indexer.",
                nameof(expression));
        }

        if (propertyInfo.GetMethod is not { IsPublic: true, IsStatic: false }
            || propertyInfo.SetMethod is not { IsPublic: true, IsStatic: false })
        {
            throw new ArgumentException(
                $"The dialog parameter '{propertyInfo.Name}' must be a public writable instance property.",
                nameof(expression));
        }

        if (!Attribute.IsDefined(propertyInfo, typeof(ParameterAttribute), inherit: true))
        {
            throw new ArgumentException(
                $"The dialog property '{propertyInfo.Name}' must have [Parameter].",
                nameof(expression));
        }

        return propertyInfo;
    }
}

internal readonly record struct BzsDialogParameter(string Name, object? Value);

/// <summary>
/// Opens component-typed dialog content through the scoped overlay host.
/// </summary>
public interface IBzsDialogService
{
    /// <summary>
    /// Opens <typeparamref name="TComponent" /> and completes when its cascaded
    /// <see cref="BzsDialogContext{TResult}" /> reports an outcome.
    /// </summary>
    /// <remarks>
    /// Call this method only after the interactive application root has rendered.
    /// A registered host that is still rendering statically returns
    /// <see cref="BzsDialogResultKind.Unavailable" /> without queuing work. A call
    /// made before any host registers is treated as a configuration error.
    /// </remarks>
    /// <typeparam name="TComponent">The component rendered as dialog content.</typeparam>
    /// <typeparam name="TResult">The result supplied by dialog content.</typeparam>
    /// <param name="parameters">Optional component parameters selected by property expression.</param>
    /// <param name="options">Optional dialog presentation and dismissal options.</param>
    /// <param name="ct">Cancels an active dialog request with an explicit cancelled result.</param>
    /// <returns>The explicit dialog outcome.</returns>
    Task<BzsDialogResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent,
        TResult>(
        Action<BzsDialogParameterBuilder<TComponent>>? parameters = null,
        BzsDialogOptions? options = null,
        CancellationToken ct = default)
        where TComponent : IComponent;
}
