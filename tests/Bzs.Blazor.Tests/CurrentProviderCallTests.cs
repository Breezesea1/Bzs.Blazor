namespace Bzs.Blazor.Tests;

public sealed class CurrentProviderCallTests
{
    [Fact]
    public async Task SuccessfulCurrentOperationReturnsItsValue()
    {
        using var call = new BzsCurrentProviderCall<int>();

        var outcome = await call.RunAsync(_ => ValueTask.FromResult(42));

        var succeeded = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Succeeded>(outcome);
        Assert.Equal(42, succeeded.Value);
    }

    [Fact]
    public async Task CurrentOperationFailureReturnsTheProviderException()
    {
        using var call = new BzsCurrentProviderCall<int>();
        var error = new InvalidOperationException("provider down");

        var outcome = await call.RunAsync(_ => ValueTask.FromException<int>(error));

        var failed = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Failed>(outcome);
        Assert.Same(error, failed.Error);
    }

    [Fact]
    public async Task AReplacementSupersedesAnEarlierOperationEvenWhenItIgnoresCancellation()
    {
        var firstCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<CancellationToken> firstStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var call = new BzsCurrentProviderCall<int>();

        var first = call.RunAsync(token =>
        {
            firstStarted.TrySetResult(token);
            return new ValueTask<int>(firstCompletion.Task);
        });
        var firstToken = await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = call.RunAsync(_ => new ValueTask<int>(secondCompletion.Task));

        secondCompletion.SetResult(2);
        firstCompletion.SetResult(1);

        var current = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Succeeded>(await second);
        var stale = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await first);
        Assert.Equal(2, current.Value);
        Assert.True(firstToken.IsCancellationRequested);
        Assert.NotNull(stale);
    }

    [Fact]
    public async Task ConcurrentCompletionsStillAcceptOnlyTheCurrentInvocation()
    {
        var firstCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var call = new BzsCurrentProviderCall<int>();

        var first = call.RunAsync(_ => new ValueTask<int>(firstCompletion.Task));
        var second = call.RunAsync(_ => new ValueTask<int>(secondCompletion.Task));
        using var barrier = new Barrier(3);
        var completeFirst = Task.Run(() =>
        {
            barrier.SignalAndWait();
            firstCompletion.SetResult(1);
        });
        var completeSecond = Task.Run(() =>
        {
            barrier.SignalAndWait();
            secondCompletion.SetResult(2);
        });

        barrier.SignalAndWait();
        await Task.WhenAll(completeFirst, completeSecond);

        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await first);
        var current = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Succeeded>(await second);
        Assert.Equal(2, current.Value);
    }

    [Fact]
    public async Task AThrowingCancellationCallbackDoesNotPreventTheReplacementFromRunning()
    {
        var firstCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var call = new BzsCurrentProviderCall<int>();

        var first = call.RunAsync(token =>
        {
            token.Register(static () => throw new InvalidOperationException("callback failed"));
            firstStarted.TrySetResult();
            return new ValueTask<int>(firstCompletion.Task);
        });
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = call.RunAsync(_ => ValueTask.FromResult(2));
        firstCompletion.SetResult(1);

        var current = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Succeeded>(await second);
        Assert.Equal(2, current.Value);
        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await first);
    }

    [Fact]
    public async Task ExplicitCancellationSupersedesTheCurrentOperation()
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var call = new BzsCurrentProviderCall<int>();

        var task = call.RunAsync(async token =>
        {
            started.TrySetResult(token);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 1;
        });
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        call.Cancel();

        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await task);
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposingTheModuleSupersedesTheCurrentOperationAndRejectsNewCalls()
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var call = new BzsCurrentProviderCall<int>();

        var task = call.RunAsync(async token =>
        {
            started.TrySetResult(token);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 1;
        });
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        call.Dispose();

        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await task);
        Assert.True(token.IsCancellationRequested);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => call.RunAsync(_ => ValueTask.FromResult(2)));
    }

    [Fact]
    public async Task ACurrentOperationCanceledExceptionIsAFailureWhenTheModuleDidNotCancelIt()
    {
        using var call = new BzsCurrentProviderCall<int>();
        var error = new OperationCanceledException("provider timeout");

        var outcome = await call.RunAsync(_ => ValueTask.FromException<int>(error));

        var failed = Assert.IsType<BzsCurrentProviderCallOutcome<int>.Failed>(outcome);
        Assert.Same(error, failed.Error);
    }

    [Fact]
    public async Task AProviderCancellationAwareOperationBecomesSupersededWhenTheModuleCancelsIt()
    {
        using var call = new BzsCurrentProviderCall<int>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var task = call.RunAsync(async token =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 1;
        });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        call.Cancel();

        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await task);
    }

    [Fact]
    public async Task CancellationDoesNotDisposeTheTokenSourceBeforeProviderCleanupCompletes()
    {
        using var call = new BzsCurrentProviderCall<int>();
        var cleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? cleanupError = null;

        var task = call.RunAsync(async token =>
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                try
                {
                    _ = token.WaitHandle;
                }
                catch (Exception error)
                {
                    cleanupError = error;
                }

                cleanup.TrySetResult();
            }

            return 1;
        });

        call.Cancel();
        await cleanup.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsType<BzsCurrentProviderCallOutcome<int>.Superseded>(await task);
        Assert.Null(cleanupError);
    }
}
