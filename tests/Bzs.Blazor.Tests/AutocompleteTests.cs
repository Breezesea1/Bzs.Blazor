using System.Linq.Expressions;
using System.Globalization;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class AutocompleteTests
{
    [Fact]
    public async Task CoordinatorDebouncesBeforeInvokingProvider()
    {
        var delayStarted = new TaskCompletionSource<(TimeSpan Delay, CancellationToken Token)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDelay = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new DelegateProvider<string>((query, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string>>>([new(query, query)]));
        using var coordinator = new BzsAutocompleteRequestCoordinator<string>(
            provider,
            async (delay, token) =>
            {
                delayStarted.TrySetResult((delay, token));
                await releaseDelay.Task.WaitAsync(token);
            });

        var request = coordinator.QueryAsync("al", TimeSpan.FromMilliseconds(275));
        var observedDelay = await delayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromMilliseconds(275), observedDelay.Delay);
        Assert.Equal(0, provider.CallCount);

        releaseDelay.SetResult();
        var result = await request;

        Assert.True(result.IsCurrent);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal("al", Assert.Single(result.Suggestions).Value);
    }

    [Fact]
    public async Task CoordinatorCancelsSupersededProviderRequest()
    {
        var firstRequestStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new DelegateProvider<string>(async (query, token) =>
        {
            if (query == "first")
            {
                firstRequestStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }

            return [new(query, query)];
        });
        using var coordinator = new BzsAutocompleteRequestCoordinator<string>(provider);

        var first = coordinator.QueryAsync("first", TimeSpan.Zero);
        var firstToken = await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = await coordinator.QueryAsync("second", TimeSpan.Zero);
        var superseded = await first;

        Assert.True(firstToken.IsCancellationRequested);
        Assert.False(superseded.IsCurrent);
        Assert.True(second.IsCurrent);
        Assert.Equal("second", Assert.Single(second.Suggestions).Value);
    }

    [Fact]
    public async Task CoordinatorRejectsStaleCompletionWhenProviderIgnoresCancellation()
    {
        var firstCompletion = new TaskCompletionSource<IReadOnlyList<BzsAutocompleteOption<string>>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<IReadOnlyList<BzsAutocompleteOption<string>>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new DelegateProvider<string>((query, _) =>
        {
            if (query == "first")
            {
                firstStarted.TrySetResult();
                return new ValueTask<IReadOnlyList<BzsAutocompleteOption<string>>>(firstCompletion.Task);
            }

            secondStarted.TrySetResult();
            return new ValueTask<IReadOnlyList<BzsAutocompleteOption<string>>>(secondCompletion.Task);
        });
        using var coordinator = new BzsAutocompleteRequestCoordinator<string>(provider);

        var first = coordinator.QueryAsync("first", TimeSpan.Zero);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = coordinator.QueryAsync("second", TimeSpan.Zero);
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        secondCompletion.SetResult([new("second", "Second")]);
        var current = await second;
        firstCompletion.SetResult([new("first", "First")]);
        var stale = await first;

        Assert.True(current.IsCurrent);
        Assert.Equal("second", Assert.Single(current.Suggestions).Value);
        Assert.False(stale.IsCurrent);
        Assert.Empty(stale.Suggestions);
    }

    [Fact]
    public void ProviderFailureIsObservableAndRetryPreservesTheQuery()
    {
        using var context = CreateContext();
        Exception? observedFailure = null;
        var provider = new SequenceProvider<string?>(
            (_, _) => ValueTask.FromException<IReadOnlyList<BzsAutocompleteOption<string?>>>(
                new InvalidOperationException("Provider unavailable.")),
            (_, _) => ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>(
                [new("alpha", "Alpha")]));
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(
            context,
            editContext,
            model,
            provider,
            builder => builder.AddAttribute(
                10,
                nameof(BzsAutocomplete<string?>.ProviderFailed),
                EventCallback.Factory.Create<Exception>(model, error => observedFailure = error)));

        cut.Find("[role='combobox']").Input("al");

        Assert.Contains("Suggestions could not be loaded", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.Equal("al", cut.Find("[role='combobox']").GetAttribute("value"));
        Assert.Equal("Provider unavailable.", observedFailure?.Message);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Retry").Click();

        Assert.Equal("al", cut.Find("[role='combobox']").GetAttribute("value"));
        Assert.Equal("Alpha", Assert.Single(cut.FindAll("[role='option']")).TextContent.Trim());
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public void StrictModeClearsTheOldSelectionAndRejectsUnmatchedText()
    {
        using var context = CreateContext();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([]));
        var model = new AutocompleteModel { Choice = "existing" };
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(context, editContext, model, provider);

        cut.Find("[role='combobox']").Input("custom");
        cut.Find("[role='combobox']").KeyDown("Enter");

        Assert.Null(model.Choice);
        Assert.Contains("must match a suggestion", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.Equal("custom", cut.Find("[role='combobox']").GetAttribute("value"));
    }

    [Fact]
    public void AcceptedControlledClearDoesNotEraseTheUsersActiveQuery()
    {
        using var context = CreateContext();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([]));
        var model = new AutocompleteModel { Choice = "existing" };
        var expression = (Expression<Func<string?>>)(() => model.Choice);
        IRenderedComponent<BzsAutocomplete<string?>>? cut = null;
        cut = context.Render<BzsAutocomplete<string?>>(parameters => parameters
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, expression)
            .Add(component => component.Provider, provider)
            .Add(component => component.DebounceDelay, TimeSpan.Zero)
            .Add(component => component.ValueChanged, value =>
            {
                model.Choice = value;
                cut!.Render(updated => updated.Add(component => component.Value, value));
            }));

        cut.Find("[role='combobox']").Input("custom");

        Assert.Null(model.Choice);
        Assert.Equal("custom", cut.Find("[role='combobox']").GetAttribute("value"));
    }

    [Fact]
    public void KeyboardSelectionSkipsDisabledOptionsAndClearResetsTheValue()
    {
        using var context = CreateContext();
        var options = new BzsAutocompleteOption<string?>[]
        {
            new("disabled", "Disabled", disabled: true),
            new("alpha", "Alpha"),
            new("beta", "Beta"),
        };
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>(options));
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);
        var cut = RenderAutocomplete(context, editContext, model, provider);

        var input = cut.Find("[role='combobox']");
        input.Input("a");
        input = cut.Find("[role='combobox']");

        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("listbox", input.GetAttribute("aria-haspopup"));
        Assert.Equal("true", input.GetAttribute("aria-expanded"));
        Assert.Equal(cut.Find("[role='listbox']").Id, input.GetAttribute("aria-controls"));
        Assert.EndsWith("-option-1", input.GetAttribute("aria-activedescendant"), StringComparison.Ordinal);

        input.KeyDown("ArrowDown");
        input = cut.Find("[role='combobox']");
        Assert.EndsWith("-option-2", input.GetAttribute("aria-activedescendant"), StringComparison.Ordinal);
        input.KeyDown("Enter");

        Assert.Equal("beta", model.Choice);
        Assert.Equal("Beta", cut.Find("[role='combobox']").GetAttribute("value"));
        Assert.Equal("false", cut.Find("[role='combobox']").GetAttribute("aria-expanded"));

        cut.Find("button[aria-label='Clear']").Click();

        Assert.Null(model.Choice);
        Assert.Equal(string.Empty, cut.Find("[role='combobox']").GetAttribute("value"));
        Assert.Equal(2, changedFields.Count(field => field.Equals(new FieldIdentifier(model, nameof(model.Choice)))));
    }

    [Fact]
    public void ResultTemplateRendersProviderSuggestions()
    {
        using var context = CreateContext();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>(
                [new("alpha", "Alpha") { Description = "First project" }]));
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(
            context,
            editContext,
            model,
            provider,
            builder => builder.AddAttribute(
                10,
                nameof(BzsAutocomplete<string?>.ResultTemplate),
                (RenderFragment<BzsAutocompleteOption<string?>>)(option => content =>
                    content.AddContent(0, $"{option.Label}: {option.Description}"))));

        cut.Find("[role='combobox']").Input("al");

        Assert.Equal("Alpha: First project", Assert.Single(cut.FindAll("[role='option']")).TextContent.Trim());
    }

    [Fact]
    public void QueriesShorterThanTheMinimumDoNotInvokeTheProvider()
    {
        using var context = CreateContext();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([new("alpha", "Alpha")]));
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(
            context,
            editContext,
            model,
            provider,
            builder => builder.AddAttribute(10, nameof(BzsAutocomplete<string?>.MinimumQueryLength), 2));

        cut.Find("[role='combobox']").Input("a");

        Assert.Equal(0, provider.CallCount);
        Assert.Equal("false", cut.Find("[role='combobox']").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task PendingProviderRequestRendersLoadingState()
    {
        using var context = CreateContext();
        var providerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerCompletion = new TaskCompletionSource<IReadOnlyList<BzsAutocompleteOption<string?>>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new DelegateProvider<string?>((_, _) =>
        {
            providerStarted.TrySetResult();
            return new ValueTask<IReadOnlyList<BzsAutocompleteOption<string?>>>(providerCompletion.Task);
        });
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(context, editContext, model, provider);

        var input = cut.Find("[role='combobox']");
        var inputEvent = input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = "al" });
        await providerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("[role='listbox']").GetAttribute("aria-busy"));
            Assert.Equal("Loading suggestions", cut.Find("[role='status']").TextContent.Trim());
        });

        providerCompletion.SetResult([new("alpha", "Alpha")]);
        await inputEvent;

        Assert.Equal("false", cut.Find("[role='listbox']").GetAttribute("aria-busy"));
        Assert.Equal("Alpha", Assert.Single(cut.FindAll("[role='option']")).TextContent.Trim());
    }

    [Fact]
    public async Task DisablingCancelsTheProviderAndClosesTheSuggestionPanel()
    {
        using var context = CreateContext();
        var requestStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new DelegateProvider<string?>(async (_, cancellationToken) =>
        {
            requestStarted.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return [];
        });
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(context, editContext, model, provider);

        var inputEvent = cut.Find("[role='combobox']")
            .TriggerEventAsync("oninput", new ChangeEventArgs { Value = "al" });
        var token = await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.FindComponent<BzsAutocomplete<string?>>()
            .Render(parameters => parameters.Add(component => component.Disabled, true));
        await inputEvent;

        Assert.True(token.IsCancellationRequested);
        Assert.Equal("false", cut.Find("[role='combobox']").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role='listbox']"));
    }

    [Fact]
    public void LibraryOwnedAutocompleteTextUsesTheActiveUiCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
            using var context = CreateContext();
            var provider = new DelegateProvider<string?>((_, _) =>
                ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([]));
            var model = new AutocompleteModel();
            var editContext = new EditContext(model);
            var cut = RenderAutocomplete(context, editContext, model, provider);

            cut.Find("[role='combobox']").Input("al");

            Assert.Equal("未找到建议", cut.Find("[role='status']").TextContent.Trim());
            Assert.Equal("清除", cut.Find("button.bzs-autocomplete__clear").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task DisposalCleansUpBothInteropModules()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var overlayModule = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        overlayModule.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        overlayModule.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        var overlayDispose = overlayModule.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true)
            .SetVoidResult();
        var keyboardModule = context.JSInterop.SetupModule(BzsAutocompleteInterop.ModulePath);
        keyboardModule.SetupVoid(BzsAutocompleteInterop.InitializeMethod, _ => true).SetVoidResult();
        var keyboardDispose = keyboardModule.SetupVoid(BzsAutocompleteInterop.DisposeMethod, _ => true)
            .SetVoidResult();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([]));
        var model = new AutocompleteModel();
        var editContext = new EditContext(model);
        var cut = RenderAutocomplete(context, editContext, model, provider);

        await cut.FindComponent<BzsAutocomplete<string?>>().Instance.DisposeAsync();

        overlayDispose.VerifyInvoke(BzsAnchoredOverlayInterop.DisposeMethod, 1);
        keyboardDispose.VerifyInvoke(BzsAutocompleteInterop.DisposeMethod, 1);
    }

    [Fact]
    public async Task StaticRenderingDoesNotInvokeTheProvider()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var provider = new DelegateProvider<string?>((_, _) =>
            ValueTask.FromResult<IReadOnlyList<BzsAutocompleteOption<string?>>>([new("alpha", "Alpha")]));
        var model = new AutocompleteModel { Choice = "initial" };
        var expression = (Expression<Func<string?>>)(() => model.Choice);
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsAutocomplete<string?>.Value)] = model.Choice,
            [nameof(BzsAutocomplete<string?>.ValueExpression)] = expression,
            [nameof(BzsAutocomplete<string?>.Provider)] = provider,
            [nameof(BzsAutocomplete<string?>.Label)] = "Project",
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsAutocomplete<string?>>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal(0, provider.CallCount);
        Assert.Equal("initial", document.QuerySelector("[role='combobox']")?.GetAttribute("value"));
        Assert.Null(document.QuerySelector("[role='combobox']")?.GetAttribute("name"));
        Assert.Equal("initial", document.QuerySelector("input[type='hidden']")?.GetAttribute("value"));
        Assert.Equal("false", document.QuerySelector("[role='combobox']")?.GetAttribute("aria-expanded"));
        Assert.Equal("Project", document.QuerySelector("label")?.TextContent.Trim());
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        var keyboardModule = context.JSInterop.SetupModule(BzsAutocompleteInterop.ModulePath);
        keyboardModule.SetupVoid(BzsAutocompleteInterop.InitializeMethod, _ => true).SetVoidResult();
        keyboardModule.SetupVoid(BzsAutocompleteInterop.DisposeMethod, _ => true).SetVoidResult();
        return context;
    }

    private static IRenderedComponent<EditForm> RenderAutocomplete(
        BunitContext context,
        EditContext editContext,
        AutocompleteModel model,
        IBzsAutocompleteProvider<string?> provider,
        Action<RenderTreeBuilder>? addAttributes = null)
    {
        var expression = (Expression<Func<string?>>)(() => model.Choice);
        return context.Render<EditForm>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<BzsAutocomplete<string?>>(0);
                builder.AddAttribute(1, nameof(BzsAutocomplete<string?>.Value), model.Choice);
                builder.AddAttribute(
                    2,
                    nameof(BzsAutocomplete<string?>.ValueChanged),
                    EventCallback.Factory.Create<string?>(model, value => model.Choice = value));
                builder.AddAttribute(3, nameof(BzsAutocomplete<string?>.ValueExpression), expression);
                builder.AddAttribute(4, nameof(BzsAutocomplete<string?>.Provider), provider);
                builder.AddAttribute(5, nameof(BzsAutocomplete<string?>.DebounceDelay), TimeSpan.Zero);
                addAttributes?.Invoke(builder);
                builder.CloseComponent();
            })));
    }

    private sealed class DelegateProvider<TValue>(
        Func<string, CancellationToken, ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>>> callback)
        : IBzsAutocompleteProvider<TValue>
    {
        private int _callCount;

        internal int CallCount => Volatile.Read(ref _callCount);

        public ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>> GetSuggestionsAsync(
            string query,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return callback(query, cancellationToken);
        }
    }

    private sealed class SequenceProvider<TValue>(
        params Func<string, CancellationToken, ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>>>[] callbacks)
        : IBzsAutocompleteProvider<TValue>
    {
        private int _callCount;

        internal int CallCount => Volatile.Read(ref _callCount);

        public ValueTask<IReadOnlyList<BzsAutocompleteOption<TValue>>> GetSuggestionsAsync(
            string query,
            CancellationToken cancellationToken)
        {
            var index = Interlocked.Increment(ref _callCount) - 1;
            return callbacks[index](query, cancellationToken);
        }
    }

    private sealed class AutocompleteModel
    {
        public string? Choice { get; set; }
    }
}
