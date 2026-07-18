using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class FormsTests
{
    [Fact]
    public void TextInputUpdatesTheControlledValueAndNotifiesItsEditContextField()
    {
        using var context = CreateContext();
        var model = new FormModel { Text = "Before" };
        var editContext = new EditContext(model);
        var changedFields = new List<FieldIdentifier>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsTextInput, string?>(
            builder,
            0,
            model.Text,
            () => model.Text,
            EventCallback.Factory.Create<string?>(model, value => model.Text = value)));

        cut.Find("input").Change("After");

        Assert.Equal("After", model.Text);
        Assert.Equal(editContext.Field(nameof(FormModel.Text)), Assert.Single(changedFields));
    }

    [Fact]
    public void TextAreaUpdatesTheControlledValue()
    {
        using var context = CreateContext();
        var model = new FormModel { Notes = "Before" };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsTextArea, string?>(
            builder,
            0,
            model.Notes,
            () => model.Notes,
            EventCallback.Factory.Create<string?>(model, value => model.Notes = value)));

        cut.Find("textarea").Change("After");

        Assert.Equal("After", model.Notes);
    }

    [Fact]
    public void CheckboxUpdatesTheControlledValue()
    {
        using var context = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsCheckbox, bool>(
            builder,
            0,
            model.Enabled,
            () => model.Enabled,
            EventCallback.Factory.Create<bool>(model, value => model.Enabled = value)));

        cut.Find("input").Change(true);

        Assert.True(model.Enabled);
    }

    [Fact]
    public void NumberInputUpdatesTheControlledValue()
    {
        using var context = CreateContext();
        var model = new FormModel { Quantity = 3 };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsNumberInput<int>, int>(
            builder,
            0,
            model.Quantity,
            () => model.Quantity,
            EventCallback.Factory.Create<int>(model, value => model.Quantity = value)));

        cut.Find("input").Change("12");

        Assert.Equal(12, model.Quantity);
    }

    [Fact]
    public void DateInputUpdatesTheControlledValue()
    {
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 17) };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        cut.Find("input").Change("2026-07-18");

        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
    }

    [Fact]
    public void SelectUpdatesTheControlledValue()
    {
        using var context = CreateContext();
        var model = new FormModel { Choice = "draft" };
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("published", "Published"),
        ];

        var cut = RenderForm(context, editContext, builder => AddInput<BzsSelect<string?>, string?>(
            builder,
            0,
            model.Choice,
            () => model.Choice,
            EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsSelect<string?>.Options), options)));

        cut.Find("select").Change("published");

        Assert.Equal("published", model.Choice);
    }

    [Fact]
    public void InvalidNumberInputKeepsTheControlledValueAndReportsAStableFieldError()
    {
        using var context = CreateContext();
        var model = new FormModel { Quantity = 42 };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsNumberInput<int>, int>(
            builder,
            0,
            model.Quantity,
            () => model.Quantity,
            EventCallback.Factory.Create<int>(model, value => model.Quantity = value)));

        cut.Find("input").Change("not-a-number");

        Assert.Equal(42, model.Quantity);
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("The Quantity field must be a number.", cut.Find("[role=alert]").TextContent);
    }

    [Fact]
    public void NumberAndDateInputsParseValuesUsingTheCurrentCulture()
    {
        using var culture = new CultureScope("fr-FR");
        using var context = CreateContext();
        var model = new FormModel
        {
            Amount = 0m,
            DueDate = new DateOnly(2026, 1, 1),
        };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder =>
        {
            AddInput<BzsNumberInput<decimal>, decimal>(
                builder,
                0,
                model.Amount,
                () => model.Amount,
                EventCallback.Factory.Create<decimal>(model, value => model.Amount = value));
            AddInput<BzsDateInput<DateOnly>, DateOnly>(
                builder,
                10,
                model.DueDate,
                () => model.DueDate,
                EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value));
        });

        var inputs = cut.FindAll("input");
        inputs[0].Change("1,5");
        inputs[1].Change("18/07/2026");

        Assert.Equal(1.5m, model.Amount);
        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
    }

    [Fact]
    public void NumberInputTreatsTheHtmlNumberValueAsInvariantBeforeUsingTheCurrentCulture()
    {
        using var culture = new CultureScope("de-DE");
        using var context = CreateContext();
        var model = new FormModel { Amount = 0m };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsNumberInput<decimal>, decimal>(
            builder,
            0,
            model.Amount,
            () => model.Amount,
            EventCallback.Factory.Create<decimal>(model, value => model.Amount = value)));

        cut.Find("input").Change("1.5");

        Assert.Equal(1.5m, model.Amount);
    }

    [Fact]
    public void ReadOnlyCheckboxAndSelectDisableInteractionAndPreserveStaticFormValues()
    {
        using var context = CreateContext();
        var model = new FormModel { Enabled = true, Choice = "published" };
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("published", "Published"),
        ];
        var cut = RenderForm(context, editContext, builder =>
        {
            AddInput<BzsCheckbox, bool>(builder, 0, model.Enabled, () => model.Enabled,
                EventCallback.Factory.Create<bool>(model, value => model.Enabled = value),
                (attributes, sequence) =>
                {
                    attributes.AddAttribute(sequence, nameof(BzsCheckbox.Name), "profile.enabled");
                    attributes.AddAttribute(sequence + 1, nameof(BzsCheckbox.ReadOnly), true);
                });
            AddInput<BzsSelect<string?>, string?>(builder, 10, model.Choice, () => model.Choice,
                EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
                (attributes, sequence) =>
                {
                    attributes.AddAttribute(sequence, nameof(BzsSelect<string?>.Name), "profile.choice");
                    attributes.AddAttribute(sequence + 1, nameof(BzsSelect<string?>.ReadOnly), true);
                    attributes.AddAttribute(sequence + 2, nameof(BzsSelect<string?>.Options), options);
                });
        });

        var checkbox = cut.Find("input[type='checkbox']");
        var select = cut.Find("select");
        checkbox.Change(false);
        select.Change("draft");

        Assert.True(model.Enabled);
        Assert.Equal("published", model.Choice);
        Assert.True(checkbox.HasAttribute("disabled"));
        Assert.True(select.HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("input[type='hidden'][name='profile.enabled']").GetAttribute("value"));
        Assert.Equal("published", cut.Find("input[type='hidden'][name='profile.choice']").GetAttribute("value"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DisabledOrReadOnlyTextInputSuppressesChanges(bool disabled, bool readOnly)
    {
        using var context = CreateContext();
        var model = new FormModel { Text = "Source" };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsTextInput, string?>(
            builder,
            0,
            model.Text,
            () => model.Text,
            EventCallback.Factory.Create<string?>(model, value => model.Text = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsTextInput.Disabled), disabled);
                attributes.AddAttribute(sequence + 1, nameof(BzsTextInput.ReadOnly), readOnly);
            }));

        var input = cut.Find("input");
        input.Change("Changed");

        Assert.Equal("Source", model.Text);
        Assert.Equal(disabled, input.HasAttribute("disabled"));
        Assert.Equal(readOnly ? "readonly" : null, input.GetAttribute("readonly"));
        Assert.Equal(readOnly ? "true" : null, input.GetAttribute("aria-readonly"));
    }

    [Fact]
    public async Task FieldConnectsLabelDescriptionRequiredStateAndDataAnnotationsError()
    {
        using var context = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder =>
        {
            builder.OpenComponent<DataAnnotationsValidator>(0);
            builder.CloseComponent();
            AddInput<BzsTextInput, string?>(
                builder,
                10,
                model.Text,
                () => model.Text,
                EventCallback.Factory.Create<string?>(model, value => model.Text = value),
                (attributes, sequence) =>
                {
                    attributes.AddAttribute(sequence, nameof(BzsTextInput.Id), "profile-name");
                    attributes.AddAttribute(sequence + 1, nameof(BzsTextInput.Label), "Profile name");
                    attributes.AddAttribute(sequence + 2, nameof(BzsTextInput.Description), "Shown to collaborators.");
                    attributes.AddAttribute(sequence + 3, nameof(BzsTextInput.Required), true);
                });
        });

        await cut.InvokeAsync(editContext.Validate);

        var input = cut.Find("input");
        var label = cut.Find("label");

        Assert.Equal("profile-name", label.GetAttribute("for"));
        Assert.Equal("profile-name", input.Id);
        Assert.Contains("*", label.TextContent, StringComparison.Ordinal);
        Assert.Equal("true", input.GetAttribute("aria-required"));
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal("profile-name-description profile-name-error", input.GetAttribute("aria-describedby"));
        Assert.Equal("Profile name is required.", cut.Find("[role=alert]").TextContent);
    }

    [Fact]
    public void InputPreservesAllowedAdditionalAttributesAndProtectsControlledNativeAttributes()
    {
        using var context = CreateContext();
        var model = new FormModel { Text = "Source" };
        var editContext = new EditContext(model);
        var additional = new Dictionary<string, object>
        {
            ["aria-describedby"] = "external-help",
            ["data-form-field"] = "profile-name",
            ["id"] = "untrusted-id",
            ["name"] = "untrusted-name",
            ["type"] = "email",
            ["value"] = "untrusted-value",
        };

        var cut = RenderForm(context, editContext, builder => AddInput<BzsTextInput, string?>(
            builder,
            0,
            model.Text,
            () => model.Text,
            EventCallback.Factory.Create<string?>(model, value => model.Text = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsTextInput.Id), "profile-name");
                attributes.AddAttribute(sequence + 1, nameof(BzsTextInput.Name), "profile.name");
                attributes.AddAttribute(sequence + 2, nameof(BzsTextInput.Description), "Shown to collaborators.");
                attributes.AddAttribute(sequence + 3, nameof(BzsTextInput.AdditionalAttributes), additional);
            }));

        var input = cut.Find("input");

        Assert.Equal("profile-name", input.Id);
        Assert.Equal("profile.name", input.GetAttribute("name"));
        Assert.Equal("text", input.GetAttribute("type"));
        Assert.Equal("Source", input.GetAttribute("value"));
        Assert.Equal("external-help profile-name-description", input.GetAttribute("aria-describedby"));
        Assert.Equal("profile-name", input.GetAttribute("data-form-field"));
    }

    [Fact]
    public void NativeControlsEmitStableNamesAndValuesForStaticFormPosts()
    {
        using var context = CreateContext();
        var model = new FormModel
        {
            Text = "Ada",
            Notes = "A note",
            Enabled = true,
            Quantity = 7,
            DueDate = new DateOnly(2026, 7, 18),
            Choice = "published",
        };
        var editContext = new EditContext(model);
        IReadOnlyList<BzsSelectOption<string?>> options =
        [
            new("draft", "Draft"),
            new("published", "Published"),
        ];

        var cut = RenderForm(context, editContext, builder =>
        {
            AddInput<BzsTextInput, string?>(builder, 0, model.Text, () => model.Text,
                EventCallback.Factory.Create<string?>(model, value => model.Text = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsTextInput.Name), "profile.text"));
            AddInput<BzsTextArea, string?>(builder, 10, model.Notes, () => model.Notes,
                EventCallback.Factory.Create<string?>(model, value => model.Notes = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsTextArea.Name), "profile.notes"));
            AddInput<BzsCheckbox, bool>(builder, 20, model.Enabled, () => model.Enabled,
                EventCallback.Factory.Create<bool>(model, value => model.Enabled = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsCheckbox.Name), "profile.enabled"));
            AddInput<BzsNumberInput<int>, int>(builder, 30, model.Quantity, () => model.Quantity,
                EventCallback.Factory.Create<int>(model, value => model.Quantity = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsNumberInput<int>.Name), "profile.quantity"));
            AddInput<BzsDateInput<DateOnly>, DateOnly>(builder, 40, model.DueDate, () => model.DueDate,
                EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
                (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly>.Name), "profile.dueDate"));
            AddInput<BzsSelect<string?>, string?>(builder, 50, model.Choice, () => model.Choice,
                EventCallback.Factory.Create<string?>(model, value => model.Choice = value),
                (attributes, sequence) =>
                {
                    attributes.AddAttribute(sequence, nameof(BzsSelect<string?>.Name), "profile.choice");
                    attributes.AddAttribute(sequence + 1, nameof(BzsSelect<string?>.Options), options);
                });
        });

        Assert.Equal("Ada", cut.Find("input[name='profile.text']").GetAttribute("value"));
        Assert.Equal("A note", cut.Find("textarea[name='profile.notes']").TextContent);
        var checkbox = cut.Find("input[name='profile.enabled']");
        Assert.Equal("true", checkbox.GetAttribute("value"));
        Assert.True(checkbox.HasAttribute("checked"));
        Assert.Equal("7", cut.Find("input[name='profile.quantity']").GetAttribute("value"));
        Assert.Equal("2026-07-18", cut.Find("input[name='profile.dueDate']").GetAttribute("value"));
        Assert.Equal("published", cut.Find("select[name='profile.choice'] option[selected]").GetAttribute("value"));
    }

    [Fact]
    public void SelectRejectsDuplicateNativeValuesAndDuplicateTypedValues()
    {
        using var context = CreateContext();
        var model = new FormModel { Choice = "one" };
        var valueExpression = (Expression<Func<string?>>)(() => model.Choice);

        var duplicateNativeValue = Assert.Throws<InvalidOperationException>(() => context.Render<BzsSelect<string?>>(parameters => parameters
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, valueExpression)
            .Add(component => component.Options,
            [
                new BzsSelectOption<string?>("one", "One", valueText: "duplicate"),
                new BzsSelectOption<string?>("two", "Two", valueText: "duplicate"),
            ])));
        Assert.Contains("ValueText", duplicateNativeValue.Message, StringComparison.Ordinal);

        var duplicateTypedValue = Assert.Throws<InvalidOperationException>(() => context.Render<BzsSelect<string?>>(parameters => parameters
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, valueExpression)
            .Add(component => component.Options,
            [
                new BzsSelectOption<string?>("one", "One", valueText: "first"),
                new BzsSelectOption<string?>("one", "Again", valueText: "second"),
            ])));
        Assert.Contains("value", duplicateTypedValue.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LibraryOwnedParseErrorsUseSimplifiedChineseResources()
    {
        using var culture = new CultureScope("zh-Hans");
        using var context = CreateContext();
        var model = new FormModel { Quantity = 2 };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsNumberInput<int>, int>(
            builder,
            0,
            model.Quantity,
            () => model.Quantity,
            EventCallback.Factory.Create<int>(model, value => model.Quantity = value)));

        cut.Find("input").Change("invalid");

        Assert.Equal("Quantity 字段必须是数字。", cut.Find("[role=alert]").TextContent);
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

    private sealed class FormModel
    {
        [Required(ErrorMessage = "Profile name is required.")]
        public string? Text { get; set; }

        public string? Notes { get; set; }

        public bool Enabled { get; set; }

        public int Quantity { get; set; }

        public decimal Amount { get; set; }

        public DateOnly DueDate { get; set; }

        public string? Choice { get; set; }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        public CultureScope(string cultureName)
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
