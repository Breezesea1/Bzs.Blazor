using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class EmptyStateAndChoiceTests
{
    [Fact]
    public void EmptyStateRequiresTitleAndRendersActionAndRootAttributes()
    {
        using var context = CreateContext();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsEmptyState>());
        Assert.Contains("Title", exception.Message, StringComparison.Ordinal);

        var cut = context.Render<BzsEmptyState>(parameters => parameters
            .Add(component => component.Title, "No results")
            .Add(component => component.Description, "Try another filter.")
            .Add(component => component.ActionContent, builder => builder.AddMarkupContent(0, "<button>Clear filters</button>"))
            .Add(component => component.Id, "empty-results")
            .Add(component => component.Class, "consumer-class")
            .Add(component => component.Style, "min-height: 12rem")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["data-state"] = "empty",
                ["aria-label"] = "Search results",
            }));

        var root = cut.Find("#empty-results");
        Assert.Equal("empty", root.GetAttribute("data-state"));
        Assert.Equal("Search results", root.GetAttribute("aria-label"));
        Assert.False(root.HasAttribute("aria-live"));
        Assert.Equal("No results", cut.Find("h3").TextContent);
        Assert.Equal("Clear filters", cut.Find("button").TextContent);
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void ToggleUpdatesTheModelAndEditContextAndExposesSwitchState()
    {
        using var context = CreateContext();
        var model = new ChoiceModel();
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsToggle, bool>(
            builder,
            0,
            model.Enabled,
            () => model.Enabled,
            EventCallback.Factory.Create<bool>(model, value => model.Enabled = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsToggle.OnText), "On");
                attributes.AddAttribute(sequence + 1, nameof(BzsToggle.OffText), "Off");
                attributes.AddAttribute(sequence + 2, nameof(BzsToggle.AdditionalAttributes),
                    new Dictionary<string, object> { ["aria-label"] = "Notifications" });
            }));

        var input = cut.Find("input[type='checkbox']");
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.Equal("false", input.GetAttribute("aria-checked"));
        Assert.Equal("Off", cut.Find("span[aria-hidden='true']").TextContent.Trim());

        input.Change(true);

        Assert.True(model.Enabled);
        Assert.Equal("true", cut.Find("input[type='checkbox']").GetAttribute("aria-checked"));
        Assert.Equal(editContext.Field(nameof(ChoiceModel.Enabled)), Assert.Single(changedFields));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ToggleSuppressesUnavailableInteractionAndReadOnlyPreservesPostValue(bool disabled, bool readOnly)
    {
        using var context = CreateContext();
        var model = new ChoiceModel { Enabled = true };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsToggle, bool>(
            builder,
            0,
            model.Enabled,
            () => model.Enabled,
            EventCallback.Factory.Create<bool>(model, value => model.Enabled = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsToggle.Name), "profile.enabled");
                attributes.AddAttribute(sequence + 1, nameof(BzsToggle.Disabled), disabled);
                attributes.AddAttribute(sequence + 2, nameof(BzsToggle.ReadOnly), readOnly);
            }));

        var input = cut.Find("input[type='checkbox']");
        input.Change(false);

        Assert.True(model.Enabled);
        Assert.True(input.HasAttribute("disabled"));
        Assert.Equal(readOnly ? "true" : null, input.GetAttribute("aria-readonly"));
        if (readOnly)
        {
            Assert.Equal("true", cut.Find("input[type='hidden'][name='profile.enabled']").GetAttribute("value"));
        }
        else
        {
            Assert.Empty(cut.FindAll("input[type='hidden']"));
        }
    }

    [Fact]
    public void RadioGroupUpdatesTheModelAndSharesNativeGroupState()
    {
        using var context = CreateContext();
        var model = new ChoiceModel { Choice = "draft" };
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("review", "Review", disabled: true),
            new("published", "Published"),
        ];

        var cut = RenderForm(context, editContext, builder => AddInput<BzsRadioGroup<string?>, string?>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsRadioGroup<string?>.Name), "profile.choice");
                attributes.AddAttribute(sequence + 1, nameof(BzsRadioGroup<string?>.Options), options);
            }));

        var radios = cut.FindAll("input[type='radio']");
        Assert.Equal(3, radios.Count);
        Assert.All(radios, radio => Assert.Equal("profile.choice", radio.GetAttribute("name")));
        Assert.True(radios[0].HasAttribute("checked"));
        Assert.True(radios[1].HasAttribute("disabled"));
        Assert.False(radios[2].HasAttribute("checked"));
        Assert.Equal("radiogroup", cut.Find("[role='radiogroup']").GetAttribute("role"));

        radios[1].Change("review");
        Assert.Equal("draft", model.Choice);

        cut.Find("input[value='published']").Change("published");
        Assert.Equal("published", model.Choice);
        Assert.True(cut.Find("input[value='published']").HasAttribute("checked"));
    }

    [Fact]
    public void ReadOnlyRadioGroupSuppressesInteractionAndPreservesPostValue()
    {
        using var context = CreateContext();
        var model = new ChoiceModel { Choice = "published" };
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("published", "Published"),
        ];

        var cut = RenderForm(context, editContext, builder => AddInput<BzsRadioGroup<string?>, string?>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsRadioGroup<string?>.Name), "profile.choice");
                attributes.AddAttribute(sequence + 1, nameof(BzsRadioGroup<string?>.ReadOnly), true);
                attributes.AddAttribute(sequence + 2, nameof(BzsRadioGroup<string?>.Options), options);
            }));

        cut.Find("input[value='draft']").Change("draft");

        Assert.Equal("published", model.Choice);
        Assert.All(cut.FindAll("input[type='radio']"), radio => Assert.True(radio.HasAttribute("disabled")));
        Assert.Equal("true", cut.Find("[role='radiogroup']").GetAttribute("aria-disabled"));
        Assert.Equal("published", cut.Find("input[type='hidden'][name='profile.choice']").GetAttribute("value"));
    }

    [Fact]
    public void RadioGroupRejectsDuplicateNativeAndTypedValues()
    {
        using var context = CreateContext();
        var model = new ChoiceModel { Choice = "one" };
        var expression = (Expression<Func<string?>>)(() => model.Choice);

        var duplicateNative = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsRadioGroup<string?>>(parameters => parameters
                .Add(component => component.Value, model.Choice)
                .Add(component => component.ValueExpression, expression)
                .Add(component => component.Options,
                [
                    new BzsSelectOption<string?>("one", "One", valueText: "same"),
                    new BzsSelectOption<string?>("two", "Two", valueText: "same"),
                ])));
        Assert.Contains("BzsRadioGroup", duplicateNative.Message, StringComparison.Ordinal);
        Assert.Contains("ValueText", duplicateNative.Message, StringComparison.Ordinal);

        var duplicateTyped = Assert.Throws<InvalidOperationException>(() =>
            context.Render<BzsRadioGroup<string?>>(parameters => parameters
                .Add(component => component.Value, model.Choice)
                .Add(component => component.ValueExpression, expression)
                .Add(component => component.Options,
                [
                    new BzsSelectOption<string?>("one", "One", valueText: "first"),
                    new BzsSelectOption<string?>("one", "Again", valueText: "second"),
                ])));
        Assert.Contains("BzsRadioGroup", duplicateTyped.Message, StringComparison.Ordinal);
        Assert.Contains("value", duplicateTyped.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RadioGroupAssociatesLabelDescriptionAndValidationWithTheGroup()
    {
        using var context = CreateContext();
        var model = new ChoiceModel();
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("published", "Published"),
        ];

        var cut = RenderForm(context, editContext, builder =>
        {
            builder.OpenComponent<DataAnnotationsValidator>(0);
            builder.CloseComponent();
            AddInput<BzsRadioGroup<string?>, string?>(
                builder,
                10,
                model.Choice,
                () => model.Choice,
                EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
                (attributes, sequence) =>
                {
                    attributes.AddAttribute(sequence, nameof(BzsRadioGroup<string?>.Id), "publishing-state");
                    attributes.AddAttribute(sequence + 1, nameof(BzsRadioGroup<string?>.Label), "Publishing state");
                    attributes.AddAttribute(sequence + 2, nameof(BzsRadioGroup<string?>.Description), "Choose one state.");
                    attributes.AddAttribute(sequence + 3, nameof(BzsRadioGroup<string?>.Required), true);
                    attributes.AddAttribute(sequence + 4, nameof(BzsRadioGroup<string?>.Options), options);
                });
        });

        await cut.InvokeAsync(editContext.Validate);

        var group = cut.Find("[role='radiogroup']");
        Assert.Equal("publishing-state", group.Id);
        var fieldLabel = cut.Find("label.bzs-field__label");
        Assert.Equal("publishing-state-label", fieldLabel.Id);
        Assert.Equal("publishing-state-option-0", fieldLabel.GetAttribute("for"));
        Assert.Equal("publishing-state-label", group.GetAttribute("aria-labelledby"));
        Assert.False(group.HasAttribute("aria-label"));
        Assert.Equal("true", group.GetAttribute("aria-required"));
        Assert.Equal("true", group.GetAttribute("aria-invalid"));
        Assert.Equal("publishing-state-description publishing-state-error", group.GetAttribute("aria-describedby"));
        Assert.Equal("Publishing state is required.", cut.Find("[role='alert']").TextContent);
        Assert.True(cut.Find("#publishing-state-option-0").HasAttribute("required"));
        Assert.False(cut.Find("#publishing-state-option-1").HasAttribute("required"));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        return context;
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

    private sealed class ChoiceModel
    {
        public bool Enabled { get; set; }

        [Required(ErrorMessage = "Publishing state is required.")]
        public string? Choice { get; set; }
    }
}
