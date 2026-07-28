using System.Globalization;
using System.Linq.Expressions;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Tests;

public sealed class SelectInteractionTests
{
    [Fact]
    public async Task SelectDisposalSwallowsCircuitDisconnect()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule("./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js");
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true).SetException(new JSDisconnectedException("Circuit disconnected."));
        var model = new SelectionModel { Choice = "production" };
        var expression = (Expression<Func<string>>)(() => model.Choice);
        var cut = context.Render<BzsSelect<string>>(parameters => parameters
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, expression)
            .Add(component => component.Options, Options));

        await cut.Instance.DisposeAsync();
    }

    [Fact]
    public void SelectInitializationSwallowsDisposedReferenceRace()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule("./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js");
        module.SetupVoid("initialize", _ => true)
            .SetException(new ObjectDisposedException("DotNetObjectReference"));
        module.SetupVoid("dispose", _ => true);
        var model = new SelectionModel { Choice = "production" };
        var expression = (Expression<Func<string>>)(() => model.Choice);

        _ = context.Render<BzsSelect<string>>(parameters => parameters
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, expression)
            .Add(component => component.Options, Options));
    }

    [Fact]
    public void SelectsRetryTransientInitializationWhenTheUserOpensThem()
    {
        var singleModel = new SelectionModel { Choice = "production" };
        AssertRetriesTransientInitialization<BzsSelect<string>, string>(
            singleModel,
            singleModel.Choice,
            () => singleModel.Choice,
            (builder, sequence) => builder.AddAttribute(sequence, nameof(BzsSelect<string>.Options), Options));

        var multiModel = new SelectionModel { Choices = ["production"] };
        AssertRetriesTransientInitialization<BzsMultiSelect<string>, IReadOnlyList<string>>(
            multiModel,
            multiModel.Choices,
            () => multiModel.Choices,
            (builder, sequence) => builder.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Options), Options));
    }

    [Fact]
    public void SearchableSelectFiltersDescriptionsAndSelectsTheVisibleOption()
    {
        using var context = CreateContext();
        var model = new SelectionModel { Choice = "production" };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsSelect<string>, string>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string>(model, value => model.Choice = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsSelect<string>.Options), Options)));

        var trigger = cut.Find("[role='combobox']");
        trigger.Click();
        Assert.Equal("true", cut.Find("[role='combobox']").GetAttribute("aria-expanded"));

        cut.Find("input[type='search']").Input("final approvals");
        var option = Assert.Single(cut.FindAll("[role='option']"));
        Assert.Contains("Review", option.TextContent, StringComparison.Ordinal);
        option.Click();

        Assert.Equal("review", model.Choice);
        Assert.Equal("false", cut.Find("[role='combobox']").GetAttribute("aria-expanded"));
        Assert.Equal("review", cut.Find("input[type='hidden']").GetAttribute("value"));
    }

    [Fact]
    public void SelectKeyboardNavigationSkipsDisabledOptions()
    {
        using var context = CreateContext();
        var model = new SelectionModel { Choice = "production" };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsSelect<string>, string>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string>(model, value => model.Choice = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsSelect<string>.Options), Options)));

        var trigger = cut.Find("[role='combobox']");
        trigger.KeyDown("ArrowDown");
        trigger = cut.Find("[role='combobox']");
        trigger.KeyDown("ArrowDown");

        Assert.EndsWith("-option-2", cut.Find("[role='combobox']").GetAttribute("aria-activedescendant"), StringComparison.Ordinal);
        cut.Find("[role='combobox']").KeyDown("Enter");
        Assert.Equal("review", model.Choice);
    }

    [Fact]
    public void RequiredEnhancedSelectsKeepNativeConstraintControls()
    {
        using var context = CreateContext();
        var model = new SelectionModel
        {
            Choice = "production",
            Choices = ["production"],
        };
        var editContext = new EditContext(model);

        var single = RenderForm(context, editContext, builder => AddInput<BzsSelect<string>, string>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string>(model, value => model.Choice = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsSelect<string>.Required), true);
                attributes.AddAttribute(sequence + 1, nameof(BzsSelect<string>.Options), Options);
            }));

        var singleConstraint = single.Find("select[data-bzs-select-constraint='true']");
        Assert.True(singleConstraint.HasAttribute("required"));
        Assert.False(singleConstraint.HasAttribute("name"));
        Assert.Equal("production", singleConstraint.QuerySelector("option[selected]")?.GetAttribute("value"));

        var multiple = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
            builder,
            0,
            model.Choices,
            () => model.Choices,
            EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Required), true);
                attributes.AddAttribute(sequence + 1, nameof(BzsMultiSelect<string>.Options), Options);
            }));

        var multiConstraint = multiple.Find("select[data-bzs-select-constraint='true']");
        Assert.True(multiConstraint.HasAttribute("required"));
        Assert.True(multiConstraint.HasAttribute("multiple"));
        Assert.False(multiConstraint.HasAttribute("name"));
        Assert.Equal("production", multiConstraint.QuerySelector("option[selected]")?.GetAttribute("value"));
    }

    [Fact]
    public async Task RequiredSelectWithoutPlaceholderKeepsAnEmptyStaticSsrSelection()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var model = new SelectionModel();
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsSelect<string>.Value)] = model.Choice,
            [nameof(BzsSelect<string>.ValueExpression)] =
                (Expression<Func<string>>)(() => model.Choice),
            [nameof(BzsSelect<string>.Required)] = true,
            [nameof(BzsSelect<string>.Options)] = Options,
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsSelect<string>>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var select = Assert.IsAssignableFrom<AngleSharp.Dom.IElement>(document.QuerySelector("select"));
        var emptyOption = Assert.IsAssignableFrom<AngleSharp.Dom.IElement>(
            select.QuerySelector("option[value='']"));

        Assert.True(select.HasAttribute("required"));
        Assert.True(emptyOption.HasAttribute("selected"));
        Assert.Contains("Select an option", emptyOption.TextContent, StringComparison.Ordinal);
        Assert.False(select.QuerySelector("option[value='production']")!.HasAttribute("selected"));
    }

    [Fact]
    public void ChoiceControlsLocalizeLibraryOwnedTextInZhHans()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("zh-Hans");
            CultureInfo.CurrentUICulture = new CultureInfo("zh-Hans");
            using var context = CreateContext();
            var model = new SelectionModel();
            var editContext = new EditContext(model);

            var single = RenderForm(context, editContext, builder => AddInput<BzsSelect<string>, string>(
                builder,
                0,
                model.Choice,
                () => model.Choice,
                EventCallback.Factory.Create<string>(model, value => model.Choice = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsSelect<string>.Options), Options)));

            Assert.Contains("请选择一个选项", single.Find("[role='combobox']").TextContent, StringComparison.Ordinal);
            single.Find("[role='combobox']").Click();
            Assert.Equal("搜索选项", single.Find("input[type='search']").GetAttribute("placeholder"));
            single.Find("input[type='search']").Input("missing");
            Assert.Equal("没有匹配的选项", single.Find("[role='status']").TextContent);

            var multiple = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
                builder,
                0,
                model.Choices,
                () => model.Choices,
                EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Options), Options)));

            Assert.Contains("请选择选项", multiple.Find("[role='combobox']").TextContent, StringComparison.Ordinal);
            multiple.Find("[role='combobox']").Click();
            Assert.Contains("全选", multiple.FindAll("button").Select(button => button.TextContent.Trim()));
            Assert.Contains("反选", multiple.FindAll("button").Select(button => button.TextContent.Trim()));
            Assert.Contains("清除", multiple.FindAll("button").Select(button => button.TextContent.Trim()));

            model.Choices = ["production", "lighting", "review"];
            var summary = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
                builder,
                0,
                model.Choices,
                () => model.Choices,
                EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Options), Options)));
            Assert.Contains("3 项已选择", summary.Find("[role='combobox']").TextContent, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void MultiSelectEmitsNewOrderedCollectionsAndStaticPostValues()
    {
        using var context = CreateContext();
        var model = new SelectionModel { Choices = ["production"] };
        var editContext = new EditContext(model);
        var original = model.Choices;
        var cut = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
            builder,
            0,
            model.Choices,
            () => model.Choices,
            EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Name), "profile.choices");
                attributes.AddAttribute(sequence + 1, nameof(BzsMultiSelect<string>.Options), Options);
            }));

        cut.Find("[role='combobox']").Click();
        cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Review", StringComparison.Ordinal)).Click();

        Assert.NotSame(original, model.Choices);
        Assert.Equal(["production", "review"], model.Choices);
        Assert.Equal(
            ["production", "review"],
            cut.FindAll("input[type='hidden'][name='profile.choices']").Select(input => input.GetAttribute("value")));
        Assert.Equal("true", cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Review", StringComparison.Ordinal)).GetAttribute("aria-selected"));
    }

    [Fact]
    public void MultiSelectVisibleActionsPreserveDisabledSelections()
    {
        using var context = CreateContext();
        var model = new SelectionModel { Choices = ["lighting"] };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
            builder,
            0,
            model.Choices,
            () => model.Choices,
            EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Options), Options)));

        cut.Find("[role='combobox']").Click();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Select all").Click();
        Assert.Equal(["production", "lighting", "review"], model.Choices);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear").Click();
        Assert.Equal(["lighting"], model.Choices);
        Assert.True(cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear").HasAttribute("disabled"));
    }

    [Fact]
    public void MultiSelectClearIsDisabledWithoutAVisibleEnabledSelection()
    {
        using var context = CreateContext();
        var model = new SelectionModel { Choices = ["production"] };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsMultiSelect<string>, IReadOnlyList<string>>(
            builder,
            0,
            model.Choices,
            () => model.Choices,
            EventCallback.Factory.Create<IReadOnlyList<string>>(model, value => model.Choices = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsMultiSelect<string>.Options), Options)));

        cut.Find("[role='combobox']").Click();
        cut.Find("input[type='search']").Input("review");

        Assert.True(cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear").HasAttribute("disabled"));
    }

    private static readonly IReadOnlyList<BzsSelectOption<string>> Options =
    [
        new("production", "Production") { Description = "Default workspace" },
        new("lighting", "Lighting", disabled: true),
        new("review", "Review") { Description = "Final approvals" },
    ];

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule("./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js");
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("setOpen", _ => true);
        module.SetupVoid("dispose", _ => true);
        return context;
    }

    private static void AssertRetriesTransientInitialization<TComponent, TValue>(
        object model,
        TValue value,
        Expression<Func<TValue>> valueExpression,
        Action<RenderTreeBuilder, int> addAttributes)
        where TComponent : IComponent
    {
        using var context = new BunitContext();
        var runtime = new RetryingJsRuntime();
        context.Services.AddBzsBlazor();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<TComponent, TValue>(
            builder,
            0,
            value,
            valueExpression,
            EventCallback.Factory.Create<TValue>(model, _ => { }),
            addAttributes));

        Assert.Equal(1, runtime.Module.InitializeAttempts);
        cut.Find("[role='combobox']").Click();
        Assert.Equal(2, runtime.Module.InitializeAttempts);
        Assert.Equal(1, runtime.Module.SetOpenCalls);
    }

    private static IRenderedComponent<EditForm> RenderForm(
        BunitContext context,
        EditContext editContext,
        RenderFragment childContent) => context.Render<EditForm>(parameters => parameters
            .Add(component => component.EditContext, editContext)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => childContent)));

    private static void AddInput<TComponent, TValue>(
        RenderTreeBuilder builder,
        int sequence,
        TValue value,
        Expression<Func<TValue>> valueExpression,
        EventCallback<TValue> valueChanged,
        Action<RenderTreeBuilder, int>? addAttributes = null)
        where TComponent : IComponent
    {
        builder.OpenComponent<TComponent>(sequence);
        builder.AddAttribute(sequence + 1, "Value", value);
        builder.AddAttribute(sequence + 2, "ValueChanged", valueChanged);
        builder.AddAttribute(sequence + 3, "ValueExpression", valueExpression);
        addAttributes?.Invoke(builder, sequence + 4);
        builder.CloseComponent();
    }

    private sealed class SelectionModel
    {
        public string Choice { get; set; } = string.Empty;
        public IReadOnlyList<string> Choices { get; set; } = [];
    }

    private sealed class RetryingJsRuntime : IJSRuntime
    {
        internal RetryingJsModule Module { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) => identifier == "import"
                ? ValueTask.FromResult((TValue)(object)Module)
                : ValueTask.FromResult(default(TValue)!);
    }

    private sealed class RetryingJsModule : IJSObjectReference
    {
        internal int InitializeAttempts { get; private set; }
        internal int SetOpenCalls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "initialize" && ++InitializeAttempts == 1)
            {
                throw new TaskCanceledException("Transient initialization cancellation.");
            }

            if (identifier == "setOpen")
            {
                SetOpenCalls++;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
