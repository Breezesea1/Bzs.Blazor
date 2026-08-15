using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Tests;

public sealed class AnchoredOverlaySessionTests
{
    [Fact]
    public async Task SessionInitializesOnceAndSynchronizesElementAndPointAnchors()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        await using var session = CreateSession(context, static () => Task.CompletedTask);

        session.SetDesiredState(new BzsAnchoredOverlayState(
            Open: true,
            Placement: BzsPopoverPlacement.TopEnd,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true));
        await session.AfterRenderAsync(default);

        Assert.Single(module.Invocations[BzsAnchoredOverlaySession.InitializeMethod]);
        var elementSynchronization = Assert.Single(
            module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod]);
        Assert.Contains("top-end", elementSynchronization.Arguments);
        Assert.Contains(true, elementSynchronization.Arguments);

        session.SetDesiredState(new BzsAnchoredOverlayState(
            Open: true,
            Placement: BzsPopoverPlacement.BottomStart,
            CloseOnOutsideInteraction: true,
            CloseOnEscape: true,
            InvocationPoint: new BzsAnchoredOverlayInvocationPoint(120, 80)));
        await session.AfterRenderAsync(default);

        Assert.Single(module.Invocations[BzsAnchoredOverlaySession.SetOpenAtMethod]);
        var pointSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenAtMethod].Single();
        Assert.Contains(120d, pointSynchronization.Arguments);
        Assert.Contains(80d, pointSynchronization.Arguments);
    }

    [Fact]
    public async Task AcceptedCloseRestoresFocusOnceAfterControlledStateChanges()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, () =>
        {
            session!.SetDesiredState(OpenState with { Open = false });
            return Task.CompletedTask;
        });
        await using (session)
        {
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);

            await session.RequestCloseAsync(restoreFocus: true);
            await session.AfterRenderAsync(default);
            await session.AfterRenderAsync(default);

            var synchronizations = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].ToArray();
            Assert.Equal(2, synchronizations.Length);
            Assert.Contains(false, synchronizations[1].Arguments);
            Assert.Contains(true, synchronizations[1].Arguments.Skip(5));
        }
    }

    [Fact]
    public async Task RejectedCloseDoesNotLeakFocusRestorationIntoALaterClose()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        await using var session = CreateSession(context, static () => Task.CompletedTask);
        session.SetDesiredState(OpenState);
        await session.AfterRenderAsync(default);

        await session.RequestCloseAsync(restoreFocus: true);
        session.SetDesiredState(OpenState with { Open = false });
        await session.AfterRenderAsync(default);

        var closeSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Last();
        Assert.Contains(false, closeSynchronization.Arguments);
        Assert.DoesNotContain(true, closeSynchronization.Arguments.Skip(5));
    }

    [Fact]
    public async Task ConcurrentCloseRequestsCoalesceAndPreserveFocusIntent()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        var closeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseClose = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closeRequestCount = 0;
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, async () =>
        {
            closeRequestCount++;
            closeStarted.TrySetResult();
            await releaseClose.Task;
            session!.SetDesiredState(OpenState with { Open = false });
        });
        await using (session)
        {
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);

            var firstClose = session.RequestCloseAsync(restoreFocus: false);
            await closeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondClose = session.RequestCloseAsync(restoreFocus: true);

            Assert.Equal(1, closeRequestCount);
            releaseClose.TrySetResult();
            await Task.WhenAll(firstClose, secondClose);
            await session.AfterRenderAsync(default);

            Assert.Equal(1, closeRequestCount);
            var closeSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Last();
            Assert.Equal(true, closeSynchronization.Arguments.ElementAt(5));
        }
    }

    [Fact]
    public async Task AcceptedAsyncCloseRetainsFocusIntentAcrossIntermediateOpenRender()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        var closeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseClose = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, async () =>
        {
            closeStarted.TrySetResult();
            await releaseClose.Task;
            session!.SetDesiredState(OpenState with { Open = false });
        });
        await using (session)
        {
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);

            var close = session.RequestCloseAsync(restoreFocus: true);
            await closeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);
            releaseClose.TrySetResult();
            await close;
            await session.AfterRenderAsync(default);

            var closeSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Last();
            Assert.Equal(false, closeSynchronization.Arguments.ElementAt(1));
            Assert.Equal(true, closeSynchronization.Arguments.ElementAt(5));
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task InitializationRetryLimitIsAppliedAndPendingWorkRecoversLater(
        int immediateAttemptLimit)
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        var initialize = module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true)
            .SetException(new TaskCanceledException("Initialization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlaySession.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        await using var session = CreateSession(
            context,
            static () => Task.CompletedTask,
            immediateAttemptLimit);
        session.SetDesiredState(OpenState);

        await session.AfterRenderAsync(default);
        initialize.VerifyInvoke(BzsAnchoredOverlaySession.InitializeMethod, immediateAttemptLimit);

        initialize.SetVoidResult();
        await session.AfterRenderAsync(default);

        initialize.VerifyInvoke(BzsAnchoredOverlaySession.InitializeMethod, immediateAttemptLimit + 1);
        Assert.Single(module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SynchronizationRetryLimitIsAppliedAndPendingWorkRecoversLater(
        int immediateAttemptLimit)
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true).SetVoidResult();
        var setOpen = module.SetupVoid(BzsAnchoredOverlaySession.SetOpenMethod, _ => true)
            .SetException(new TaskCanceledException("Synchronization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        await using var session = CreateSession(
            context,
            static () => Task.CompletedTask,
            immediateAttemptLimit);
        session.SetDesiredState(OpenState);

        await session.AfterRenderAsync(default);
        setOpen.VerifyInvoke(BzsAnchoredOverlaySession.SetOpenMethod, immediateAttemptLimit);

        setOpen.SetVoidResult();
        await session.AfterRenderAsync(default);

        setOpen.VerifyInvoke(BzsAnchoredOverlaySession.SetOpenMethod, immediateAttemptLimit + 1);
    }

    [Fact]
    public async Task StateChangedDuringSynchronizationCommitsOnlyTheLatestVersion()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true).SetVoidResult();
        var setOpen = module.SetupVoid(BzsAnchoredOverlaySession.SetOpenMethod, _ => true);
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        await using var session = CreateSession(context, static () => Task.CompletedTask);
        session.SetDesiredState(OpenState);

        var synchronization = session.AfterRenderAsync(default).AsTask();
        await WaitUntilAsync(
            () => module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Count == 1);
        session.SetDesiredState(OpenState with { Open = false });
        setOpen.SetVoidResult();

        await synchronization;

        var invocations = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].ToArray();
        Assert.Equal(2, invocations.Length);
        Assert.Contains(true, invocations[0].Arguments);
        Assert.Contains(false, invocations[1].Arguments);
    }

    [Fact]
    public async Task ConcurrentRenderSynchronizationsConsumeFocusIntentOnce()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(
                BzsAnchoredOverlaySession.SetOpenMethod,
                invocation => invocation.Arguments.ElementAt(1) is true)
            .SetVoidResult();
        var closeSynchronization = module.SetupVoid(
            BzsAnchoredOverlaySession.SetOpenMethod,
            invocation => invocation.Arguments.ElementAt(1) is false);
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, () =>
        {
            session!.SetDesiredState(OpenState with { Open = false });
            return Task.CompletedTask;
        });
        await using (session)
        {
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);
            await session.RequestCloseAsync(restoreFocus: true);

            var firstRender = session.AfterRenderAsync(default).AsTask();
            await WaitUntilAsync(() => module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod]
                .Count(invocation => invocation.Arguments.ElementAt(1) is false) == 1);
            var secondRender = session.AfterRenderAsync(default).AsTask();

            Assert.Single(
                module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod],
                invocation => invocation.Arguments.ElementAt(1) is false);
            closeSynchronization.SetVoidResult();
            await Task.WhenAll(firstRender, secondRender);

            var closeInvocation = Assert.Single(
                module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod],
                invocation => invocation.Arguments.ElementAt(1) is false);
            Assert.Equal(true, closeInvocation.Arguments.ElementAt(5));
        }
    }

    [Fact]
    public async Task TransientCloseRetryConsumesFocusIntentAtMostOnce()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(
                BzsAnchoredOverlaySession.SetOpenMethod,
                invocation => invocation.Arguments.ElementAt(1) is true)
            .SetVoidResult();
        module.SetupVoid(
                BzsAnchoredOverlaySession.SetOpenMethod,
                invocation => invocation.Arguments.ElementAt(1) is false)
            .SetException(new TaskCanceledException("Close synchronization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, () =>
        {
            session!.SetDesiredState(OpenState with { Open = false });
            return Task.CompletedTask;
        });
        await using (session)
        {
            session.SetDesiredState(OpenState);
            await session.AfterRenderAsync(default);
            await session.RequestCloseAsync(restoreFocus: true);

            await session.AfterRenderAsync(default);

            var closeInvocations = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod]
                .Where(invocation => invocation.Arguments.ElementAt(1) is false)
                .ToArray();
            Assert.Equal(2, closeInvocations.Length);
            Assert.Single(closeInvocations, invocation => invocation.Arguments.ElementAt(5) is true);
        }
    }

    [Fact]
    public async Task FailedCloseCallbackDoesNotLeakFocusRestoration()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        await using var session = CreateSession(
            context,
            static () => throw new InvalidOperationException("Close failed."));
        session.SetDesiredState(OpenState);
        await session.AfterRenderAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.RequestCloseAsync(restoreFocus: true));
        session.SetDesiredState(OpenState with { Open = false });
        await session.AfterRenderAsync(default);

        var closeSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Last();
        Assert.DoesNotContain(true, closeSynchronization.Arguments.Skip(5));
    }

    [Fact]
    public async Task DisposeCleansUpOnceAndRejectsNewStateWhileLateBrowserCloseIsIgnored()
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        var closeRequests = 0;
        var session = CreateSession(context, () =>
        {
            closeRequests++;
            return Task.CompletedTask;
        });
        session.SetDesiredState(OpenState);
        await session.AfterRenderAsync(default);

        await session.DisposeAsync();
        await session.DisposeAsync();
        await session.CloseFromBrowserAsync(restoreFocus: true);

        Assert.Single(module.Invocations[BzsAnchoredOverlaySession.DisposeMethod]);
        Assert.Equal(0, closeRequests);
        Assert.Throws<ObjectDisposedException>(() => session.SetDesiredState(OpenState));
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.RequestCloseAsync(restoreFocus: true));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task BrowserCloseAppliesTheDesiredFocusRestorationPolicy(
        bool restoreFocusOnBrowserClose,
        bool expectedRestoreFocus)
    {
        using var context = new BunitContext();
        var module = SetupModule(context);
        BzsAnchoredOverlaySession? session = null;
        session = CreateSession(context, () =>
        {
            session!.SetDesiredState(OpenState with
            {
                Open = false,
                RestoreFocusOnBrowserClose = restoreFocusOnBrowserClose,
            });
            return Task.CompletedTask;
        });
        await using (session)
        {
            session.SetDesiredState(OpenState with
            {
                RestoreFocusOnBrowserClose = restoreFocusOnBrowserClose,
            });
            await session.AfterRenderAsync(default);

            await session.CloseFromBrowserAsync(restoreFocus: true);
            await session.AfterRenderAsync(default);

            var closeSynchronization = module.Invocations[BzsAnchoredOverlaySession.SetOpenMethod].Last();
            Assert.Equal(expectedRestoreFocus, closeSynchronization.Arguments.ElementAt(5));
        }
    }

    private static readonly BzsAnchoredOverlayState OpenState = new(
        Open: true,
        Placement: BzsPopoverPlacement.BottomStart,
        CloseOnOutsideInteraction: true,
        CloseOnEscape: true);

    private static BzsAnchoredOverlaySession CreateSession(
        BunitContext context,
        Func<Task> closeRequested,
        int immediateAttemptLimit = 2) =>
        new(
            context.Services.GetRequiredService<IJSRuntime>(),
            closeRequested,
            immediateAttemptLimit,
            context.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>());

    private static BunitJSModuleInterop SetupModule(BunitContext context)
    {
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlaySession.ModulePath);
        module.SetupVoid(BzsAnchoredOverlaySession.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlaySession.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlaySession.SetOpenAtMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlaySession.DisposeMethod, _ => true).SetVoidResult();
        return module;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout)
            {
                throw new TimeoutException("The expected JavaScript invocation did not occur.");
            }

            await Task.Yield();
        }
    }
}
