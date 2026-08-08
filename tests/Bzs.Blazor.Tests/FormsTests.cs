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
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 17) };
        var editContext = new EditContext(model);

        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        var input = cut.Find("input[role='combobox']");
        input.Change("7/18/2026");

        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
        Assert.Equal("7/18/2026", input.GetAttribute("value"));
    }

    [Fact]
    public void DateInputOpensAnAccessibleCalendarAndSelectsADate()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 17) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        var input = cut.Find("input[role='combobox']");
        Assert.Equal("false", input.GetAttribute("aria-expanded"));

        input.Click();

        input = cut.Find("input[role='combobox']");
        var dialog = cut.Find("[role='dialog']");
        Assert.Equal("true", input.GetAttribute("aria-expanded"));
        Assert.Equal(dialog.Id, input.GetAttribute("aria-controls"));
        Assert.Equal(
            "true",
            cut.FindAll("[role='gridcell']").Single(day =>
                day.GetAttribute("aria-label") == new DateOnly(2026, 7, 17).ToString("D", CultureInfo.CurrentCulture))
                .GetAttribute("aria-selected"));

        cut.FindAll("[role='gridcell']").Single(day =>
            day.GetAttribute("aria-label") == new DateOnly(2026, 7, 18).ToString("D", CultureInfo.CurrentCulture))
            .Click();

        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
        Assert.Equal("7/18/2026", cut.Find("input[role='combobox']").GetAttribute("value"));
        Assert.Equal("false", cut.Find("input[role='combobox']").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(18)]
    public void DateInputCalendarSelectionClearsParsingErrors(int selectedDay)
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var originalDate = new DateOnly(2026, 7, 17);
        var selectedDate = new DateOnly(2026, 7, selectedDay);
        var model = new FormModel { DueDate = originalDate };
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(FormModel.DueDate));
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        var input = cut.Find("input[role='combobox']");
        input.Change("not-a-date");

        Assert.Equal(originalDate, model.DueDate);
        Assert.NotEmpty(editContext.GetValidationMessages(field));
        Assert.Equal("not-a-date", cut.Find("input[role='combobox']").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input[role='combobox']").GetAttribute("aria-invalid"));
        Assert.Single(cut.FindAll("[role='alert']"));

        cut.Find("input[role='combobox']").Click();
        cut.Find($"[data-date='2026-07-{selectedDay:D2}']").Click();

        input = cut.Find("input[role='combobox']");
        Assert.Equal(selectedDate, model.DueDate);
        Assert.Equal(selectedDate.ToString("d", CultureInfo.CurrentCulture), input.GetAttribute("value"));
        Assert.Empty(editContext.GetValidationMessages(field));
        Assert.Null(input.GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public void DateInputDisablesOutOfRangeDaysAndRejectsOutOfRangeText()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly>.Min), new DateOnly(2026, 7, 17));
                attributes.AddAttribute(sequence + 1, nameof(BzsDateInput<DateOnly>.Max), new DateOnly(2026, 7, 19));
            }));

        var input = cut.Find("input[role='combobox']");
        input.Click();

        var days = cut.FindAll("[role='gridcell']");
        Assert.True(days.Single(day =>
            day.GetAttribute("aria-label") == new DateOnly(2026, 7, 16).ToString("D", CultureInfo.CurrentCulture))
            .HasAttribute("disabled"));
        Assert.False(days.Single(day =>
            day.GetAttribute("aria-label") == new DateOnly(2026, 7, 17).ToString("D", CultureInfo.CurrentCulture))
            .HasAttribute("disabled"));

        input.Change("7/20/2026");

        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
        Assert.Equal("true", cut.Find("input[role='combobox']").GetAttribute("aria-invalid"));
        Assert.NotEmpty(cut.Find("[role='alert']").TextContent);
    }

    [Theory]
    [InlineData(2027, 5, 10, 2027, 6, 20)]
    [InlineData(2100, 1, 1, 2101, 12, 31)]
    public void DateInputClampsAnOpenCalendarWhenTheAllowedRangeChanges(
        int minYear,
        int minMonth,
        int minDay,
        int maxYear,
        int maxMonth,
        int maxDay)
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        DateOnly? min = null;
        DateOnly? max = null;
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly>.Min), min);
                attributes.AddAttribute(sequence + 1, nameof(BzsDateInput<DateOnly>.Max), max);
            }));
        cut.Find("input[role='combobox']").Click();

        min = new DateOnly(minYear, minMonth, minDay);
        max = new DateOnly(maxYear, maxMonth, maxDay);
        cut.Render();

        Assert.Equal(min.Value.ToString("Y", CultureInfo.CurrentCulture), cut.Find("[role='grid']").GetAttribute("aria-label"));
        Assert.Equal(min.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), cut.Find("[tabindex='0']").GetAttribute("data-date"));
        cut.Find("[data-bzs-date-picker-period='year']").Click();
        var yearListbox = cut.Find("[role='listbox'][aria-label='Year']");
        Assert.Equal(
            Enumerable.Range(min.Value.Year, max.Value.Year - min.Value.Year + 1).Select(year => year.ToString(CultureInfo.InvariantCulture)),
            yearListbox.QuerySelectorAll("[role='option']").Select(option => option.TextContent.Trim()));
    }

    [Theory]
    [InlineData("month")]
    [InlineData("year")]
    public void DateInputKeepsAnOpenPeriodMenuActiveDescendantInsideAChangedRange(string period)
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        DateOnly? min = null;
        DateOnly? max = null;
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly>.Min), min);
                attributes.AddAttribute(sequence + 1, nameof(BzsDateInput<DateOnly>.Max), max);
            }));

        cut.Find("input[role='combobox']").Click();
        var accessibleName = period == "month" ? "Month" : "Year";
        cut.Find($"[role='combobox'][aria-label='{accessibleName}']").Click();

        min = new DateOnly(2030, 5, 10);
        max = period == "month"
            ? new DateOnly(2030, 6, 20)
            : new DateOnly(2031, 6, 20);
        cut.Render();

        var trigger = cut.Find($"[role='combobox'][aria-label='{accessibleName}']");
        var activeOptionId = trigger.GetAttribute("aria-activedescendant");
        Assert.NotNull(activeOptionId);
        var activeOption = cut.FindAll("[role='option']").Single(option => option.Id == activeOptionId);
        Assert.Equal("option", activeOption.GetAttribute("role"));
    }

    [Fact]
    public void DateInputPeriodMenusSupportLocalizedTypeahead()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2220, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        cut.Find("input[role='combobox']").Click();
        var month = cut.Find("[role='combobox'][aria-label='Month']");
        month.Click();

        month.KeyDown("J");
        Assert.Equal("January", GetActiveOptionText(cut, month));
        month.KeyDown("J");
        Assert.Equal("June", GetActiveOptionText(cut, month));

        var year = cut.Find("[role='combobox'][aria-label='Year']");
        year.Click();
        foreach (var key in "2222")
        {
            year.KeyDown(key.ToString());
        }

        Assert.Equal("2222", GetActiveOptionText(cut, year));
    }

    [Fact]
    public void ClearableNullableDateInputClearsTheControlledValue()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { OptionalDueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly?>.Clearable), true)));

        cut.Find("input[role='combobox']").Click();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear").Click();

        Assert.Null(model.OptionalDueDate);
        Assert.Null(cut.Find("input[role='combobox']").GetAttribute("value"));
        Assert.Equal("false", cut.Find("input[role='combobox']").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearableNullableDateInputClearsParsingErrors(bool startsWithValue)
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        DateOnly? initialDate = startsWithValue ? new DateOnly(2026, 7, 18) : null;
        var model = new FormModel { OptionalDueDate = initialDate };
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(FormModel.OptionalDueDate));
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value),
            (attributes, sequence) => attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly?>.Clearable), true)));

        var input = cut.Find("input[role='combobox']");
        input.Change("not-a-date");

        Assert.Equal(initialDate, model.OptionalDueDate);
        Assert.NotEmpty(editContext.GetValidationMessages(field));
        Assert.Equal("not-a-date", cut.Find("input[role='combobox']").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input[role='combobox']").GetAttribute("aria-invalid"));
        Assert.Single(cut.FindAll("[role='alert']"));

        cut.Find("input[role='combobox']").Click();
        var clear = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear");
        Assert.False(clear.HasAttribute("disabled"));
        clear.Click();

        input = cut.Find("input[role='combobox']");
        Assert.Null(model.OptionalDueDate);
        Assert.True(string.IsNullOrEmpty(input.GetAttribute("value")));
        Assert.Empty(editContext.GetValidationMessages(field));
        Assert.Null(input.GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.Equal("false", input.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public void DateInputRejectsClearableForANonNullableValue()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);

        var exception = Assert.Throws<InvalidOperationException>(() => RenderForm(
            context,
            editContext,
            builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
                builder,
                0,
                model.DueDate,
                () => model.DueDate,
                EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
                (attributes, sequence) => attributes.AddAttribute(
                    sequence,
                    nameof(BzsDateInput<DateOnly>.Clearable),
                    true))));

        Assert.Contains(nameof(BzsDateInput<DateOnly>.Clearable), exception.Message, StringComparison.Ordinal);
        Assert.Contains("nullable TValue", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DateInputRejectsAnInvalidDateFormatWithAnActionableException()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);

        var exception = Assert.Throws<InvalidOperationException>(() => RenderForm(
            context,
            editContext,
            builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
                builder,
                0,
                model.DueDate,
                () => model.DueDate,
                EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
                (attributes, sequence) => attributes.AddAttribute(
                    sequence,
                    nameof(BzsDateInput<DateOnly>.DateFormat),
                    "Q"))));

        Assert.Contains(nameof(BzsDateInput<DateOnly>.DateFormat), exception.Message, StringComparison.Ordinal);
        Assert.Contains("'Q'", exception.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData("yyyy")]
    [InlineData("yyyy-MM")]
    public void DateInputRejectsAFormatThatDoesNotPreserveTheCompleteDate(string dateFormat)
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);

        var exception = Assert.Throws<InvalidOperationException>(() => RenderForm(
            context,
            editContext,
            builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
                builder,
                0,
                model.DueDate,
                () => model.DueDate,
                EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
                (attributes, sequence) => attributes.AddAttribute(
                    sequence,
                    nameof(BzsDateInput<DateOnly>.DateFormat),
                    dateFormat))));

        Assert.Contains(nameof(BzsDateInput<DateOnly>.DateFormat), exception.Message, StringComparison.Ordinal);
        Assert.Contains(dateFormat, exception.Message, StringComparison.Ordinal);
        Assert.Contains("year, month, and day", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DateInputAcceptsACompleteCustomDateFormat()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
            (attributes, sequence) => attributes.AddAttribute(
                sequence,
                nameof(BzsDateInput<DateOnly>.DateFormat),
                "yyyy/MM/dd")));

        Assert.Equal("2026/07/18", cut.Find("input[role='combobox']").GetAttribute("value"));
        cut.Find("input[role='combobox']").Click();
        cut.Find("[data-date='2026-07-19']").Click();

        Assert.Equal(new DateOnly(2026, 7, 19), model.DueDate);
        Assert.Equal("2026/07/19", cut.Find("input[role='combobox']").GetAttribute("value"));
    }

    [Fact]
    public void DateInputUsesLocalizedGregorianDatesForANonGregorianCulture()
    {
        using var culture = new CultureScope("ar-SA");
        using var context = CreateContext();
        var localizedGregorianCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        localizedGregorianCulture.DateTimeFormat.Calendar = CultureInfo.CurrentCulture.OptionalCalendars
            .OfType<GregorianCalendar>()
            .First();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        var input = cut.Find("input[role='combobox']");
        Assert.NotEqual(model.DueDate.ToString("d", CultureInfo.CurrentCulture), input.GetAttribute("value"));
        Assert.Equal(model.DueDate.ToString("d", localizedGregorianCulture), input.GetAttribute("value"));

        var changedDate = new DateOnly(2026, 7, 19);
        input.Change(changedDate.ToString("d", localizedGregorianCulture));
        Assert.Equal(changedDate, model.DueDate);

        cut.Find("input[role='combobox']").Click();
        var changedNativeDate = changedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.Equal(
            changedDate.ToString("Y", localizedGregorianCulture),
            cut.Find("[role='grid']").GetAttribute("aria-label"));
        Assert.Equal(
            changedDate.ToString("D", localizedGregorianCulture),
            cut.Find($"[data-date='{changedNativeDate}']").GetAttribute("aria-label"));
        cut.Find("[data-bzs-date-picker-period='month']").Click();
        Assert.Equal(
            localizedGregorianCulture.DateTimeFormat.GetMonthName(changedDate.Month),
            cut.Find("[role='listbox'][aria-label='Month'] [role='option'][aria-selected='true']").TextContent.Trim());
    }

    [Fact]
    public void DateInputUsesAnExplicitCultureForLanguageDirectionAndPickerText()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var componentCulture = CultureInfo.GetCultureInfo("zh-Hans");
        using var context = CreateContext();
        var model = new FormModel { OptionalDueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly?>.Culture), componentCulture);
                attributes.AddAttribute(sequence + 1, nameof(BzsDateInput<DateOnly?>.Clearable), true);
            }));

        var input = cut.Find("input[role='combobox']");
        Assert.Equal("zh-Hans", input.GetAttribute("lang"));
        Assert.Equal("ltr", input.GetAttribute("dir"));
        Assert.Equal(model.OptionalDueDate.Value.ToString("d", componentCulture), input.GetAttribute("value"));

        cut.Find("button[aria-label='打开日历']").Click();

        Assert.Equal("选择日期", cut.Find("[role='dialog']").GetAttribute("aria-label"));
        var monthCombobox = cut.Find("[data-bzs-date-picker-period='month']");
        Assert.Equal("月份", monthCombobox.GetAttribute("aria-label"));
        Assert.Equal("七月", monthCombobox.TextContent.Trim());
        Assert.Equal("年份", cut.Find("[data-bzs-date-picker-period='year']").GetAttribute("aria-label"));
        Assert.Contains(cut.FindAll("button"), button => button.TextContent.Trim() == "今天");
        Assert.Contains(cut.FindAll("button"), button => button.TextContent.Trim() == "清除");

        monthCombobox.Click();
        Assert.Equal(
            "七月",
            cut.Find("[role='listbox'][aria-label='月份'] [role='option'][aria-selected='true']").TextContent.Trim());
        Assert.Same(originalCulture, CultureInfo.CurrentCulture);
        Assert.Same(originalUiCulture, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void DateInputWithAnExplicitCultureDoesNotAcceptAmbientCultureOnlyFormats()
    {
        using var culture = new CultureScope("en-GB");
        using var context = CreateContext();
        var componentCulture = CultureInfo.GetCultureInfo("en-US");
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value),
            (attributes, sequence) =>
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateOnly>.Culture), componentCulture)));

        var input = cut.Find("input[role='combobox']");
        input.Change("31/12/2026");

        Assert.Equal(new DateOnly(2026, 7, 18), model.DueDate);
        Assert.Equal("true", cut.Find("input[role='combobox']").GetAttribute("aria-invalid"));

        cut.Find("input[role='combobox']").Change("12/31/2026");

        Assert.Equal(new DateOnly(2026, 12, 31), model.DueDate);
        Assert.Null(cut.Find("input[role='combobox']").GetAttribute("aria-invalid"));

        cut.Find("input[role='combobox']").Change("2027-01-02");

        Assert.Equal(new DateOnly(2027, 1, 2), model.DueDate);
    }

    [Fact]
    public void DateInputUsesTheBrowserLocalDateForToday()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value)));

        cut.Find("input[role='combobox']").Click();

        Assert.Equal("2031-02-03", cut.Find("[aria-current='date']").GetAttribute("data-date"));
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Today").Click();
        Assert.Equal(new DateOnly(2031, 2, 3), model.OptionalDueDate);
    }

    [Fact]
    public void DateInputWaitsForTheBrowserDateBeforeOpeningAfterInitializationRecovers()
    {
        using var culture = new CultureScope("en-US");
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var dateModule = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js");
        var initialization = dateModule
            .Setup<string>("initialize", _ => true)
            .SetException(new TaskCanceledException("Date module is still loading."));
        dateModule.SetupVoid("setOpen", _ => true);
        dateModule.SetupVoid("focusActiveDay", _ => true);
        dateModule.SetupVoid("dispose", _ => true);
        var model = new FormModel();
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value)));

        initialization.SetResult("2031-02-03");
        cut.Find("input[role='combobox']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("input[role='combobox']").GetAttribute("aria-expanded"));
            Assert.Equal("2031-02-03", cut.Find("[aria-current='date']").GetAttribute("data-date"));
        });
    }

    [Fact]
    public void DateInputCancelsAPendingOpenRequestOnEscape()
    {
        using var culture = new CultureScope("en-US");
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var dateModule = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js");
        var initialization = dateModule
            .Setup<string>("initialize", _ => true)
            .SetException(new TaskCanceledException("Date module is still loading."));
        dateModule.SetupVoid("setOpen", _ => true);
        dateModule.SetupVoid("focusActiveDay", _ => true);
        dateModule.SetupVoid("dispose", _ => true);
        var model = new FormModel();
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value)));

        var input = cut.Find("input[role='combobox']");
        input.Click();
        input.KeyDown("Escape");
        initialization.SetResult("2031-02-03");
        cut.Render();

        Assert.Equal("false", cut.Find("input[role='combobox']").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public void DateInputRetriesTransientOpenSynchronizationAndRecoversOnALaterRender()
    {
        using var culture = new CultureScope("en-US");
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var dateModule = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js");
        dateModule.Setup<string>("initialize", _ => true).SetResult("2031-02-03");
        var setOpen = dateModule.SetupVoid("setOpen", _ => true)
            .SetException(new TaskCanceledException("Date module call was interrupted."));
        dateModule.SetupVoid("focusActiveDay", _ => true);
        dateModule.SetupVoid("dispose", _ => true);
        var model = new FormModel();
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly?>, DateOnly?>(
            builder,
            0,
            model.OptionalDueDate,
            () => model.OptionalDueDate,
            EventCallback.Factory.Create<DateOnly?>(model, value => model.OptionalDueDate = value)));

        cut.Find("input[role='combobox']").Click();

        setOpen.VerifyInvoke("setOpen", 2);
        setOpen.SetVoidResult();
        cut.Render();
        setOpen.VerifyInvoke("setOpen", 3);
        cut.Render();
        setOpen.VerifyInvoke("setOpen", 3);
    }

    [Fact]
    public async Task DateInputDisposalCancelsPendingInitializationBeforeCleaningUp()
    {
        using var culture = new CultureScope("en-US");
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var dateModule = context.JSInterop.SetupModule(
            "./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js");
        var initialization = dateModule.Setup<string>("initialize", _ => true);
        dateModule.SetupVoid("setOpen", _ => true);
        dateModule.SetupVoid("focusActiveDay", _ => true);
        var dispose = dateModule.SetupVoid("dispose", _ => true).SetVoidResult();
        var model = new FormModel();
        var expression = (Expression<Func<DateOnly?>>)(() => model.OptionalDueDate);
        var cut = context.Render<BzsDateInput<DateOnly?>>(parameters => parameters
            .Add(component => component.Value, model.OptionalDueDate)
            .Add(component => component.ValueExpression, expression));

        var disposal = cut.Instance.DisposeAsync().AsTask();

        await disposal.WaitAsync(TimeSpan.FromSeconds(5));
        dispose.VerifyInvoke("dispose");
        dateModule.VerifyNotInvoke("setOpen");

        initialization.SetResult("2031-02-03");
    }

    [Fact]
    public async Task DateInputDisposalClearsParsingMessagesFromTheEditContext()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { DueDate = new DateOnly(2026, 7, 18) };
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(FormModel.DueDate));
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateOnly>, DateOnly>(
            builder,
            0,
            model.DueDate,
            () => model.DueDate,
            EventCallback.Factory.Create<DateOnly>(model, value => model.DueDate = value)));

        cut.Find("input[role='combobox']").Change("not-a-date");
        Assert.NotEmpty(editContext.GetValidationMessages(field));

        await context.DisposeComponentsAsync();

        Assert.Empty(editContext.GetValidationMessages(field));
    }

    [Fact]
    public void DateTimeOffsetInputSupportsTheMinimumCalendarDate()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel { OffsetDueDate = DateTimeOffset.MinValue };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateTimeOffset>, DateTimeOffset>(
            builder,
            0,
            model.OffsetDueDate,
            () => model.OffsetDueDate,
            EventCallback.Factory.Create<DateTimeOffset>(model, value => model.OffsetDueDate = value),
            (attributes, sequence) =>
            {
                attributes.AddAttribute(sequence, nameof(BzsDateInput<DateTimeOffset>.Min), DateOnly.MinValue);
                attributes.AddAttribute(sequence + 1, nameof(BzsDateInput<DateTimeOffset>.Max), new DateOnly(1, 1, 2));
            }));

        cut.Find("input[role='combobox']").Click();
        cut.Find("[data-date='0001-01-01']").Click();

        Assert.Equal(1, model.OffsetDueDate.Year);
        Assert.Equal(1, model.OffsetDueDate.Month);
        Assert.Equal(1, model.OffsetDueDate.Day);
        Assert.Equal(TimeSpan.Zero, model.OffsetDueDate.Offset);
    }

    [Fact]
    public void DateTimeOffsetInputPreservesTheControlledValueOffset()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var expectedOffset = TimeSpan.FromHours(5.5);
        var model = new FormModel
        {
            OffsetDueDate = new DateTimeOffset(2026, 7, 18, 15, 30, 0, expectedOffset),
        };
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateTimeOffset>, DateTimeOffset>(
            builder,
            0,
            model.OffsetDueDate,
            () => model.OffsetDueDate,
            EventCallback.Factory.Create<DateTimeOffset>(model, value => model.OffsetDueDate = value)));

        cut.Find("input[role='combobox']").Change("7/19/2026");
        Assert.Equal(new DateOnly(2026, 7, 19), DateOnly.FromDateTime(model.OffsetDueDate.DateTime));
        Assert.Equal(expectedOffset, model.OffsetDueDate.Offset);
        Assert.Equal(TimeOnly.MinValue, TimeOnly.FromDateTime(model.OffsetDueDate.DateTime));

        cut.Find("input[role='combobox']").Click();
        cut.Find("[data-date='2026-07-20']").Click();
        Assert.Equal(new DateOnly(2026, 7, 20), DateOnly.FromDateTime(model.OffsetDueDate.DateTime));
        Assert.Equal(expectedOffset, model.OffsetDueDate.Offset);
    }

    [Fact]
    public void NullableDateTimeOffsetInputUsesUtcWhenItHasNoExistingOffset()
    {
        using var culture = new CultureScope("en-US");
        using var context = CreateContext();
        var model = new FormModel();
        var editContext = new EditContext(model);
        var cut = RenderForm(context, editContext, builder => AddInput<BzsDateInput<DateTimeOffset?>, DateTimeOffset?>(
            builder,
            0,
            model.OptionalOffsetDueDate,
            () => model.OptionalOffsetDueDate,
            EventCallback.Factory.Create<DateTimeOffset?>(model, value => model.OptionalOffsetDueDate = value)));

        cut.Find("input[role='combobox']").Change("7/19/2026");

        Assert.NotNull(model.OptionalOffsetDueDate);
        Assert.Equal(new DateOnly(2026, 7, 19), DateOnly.FromDateTime(model.OptionalOffsetDueDate.Value.DateTime));
        Assert.Equal(TimeSpan.Zero, model.OptionalOffsetDueDate.Value.Offset);
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

        cut.Find("[role='combobox']").Click();
        cut.FindAll("[role='option']").Single(option => option.TextContent.Contains("Published", StringComparison.Ordinal)).Click();

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
        var select = cut.Find("[role='combobox']");
        checkbox.Change(false);
        select.Click();

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
        Assert.Equal(
            model.DueDate.ToString("d", CultureInfo.CurrentCulture),
            cut.Find("input[type='text'][name='profile.dueDate']").GetAttribute("value"));
        Assert.Equal("published", cut.Find("input[type='hidden'][name='profile.choice']").GetAttribute("value"));
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
        var module = context.JSInterop.SetupModule("./_content/Bzs.Blazor/Components/Form/BzsSelect.razor.js");
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("setOpen", _ => true);
        module.SetupVoid("dispose", _ => true);
        var dateModule = context.JSInterop.SetupModule("./_content/Bzs.Blazor/Components/Form/BzsDateInput.razor.js");
        dateModule.Setup<string>("initialize", _ => true).SetResult("2031-02-03");
        dateModule.SetupVoid("setOpen", _ => true);
        dateModule.SetupVoid("focusActiveDay", _ => true);
        dateModule.SetupVoid("scrollActivePeriodOption", _ => true);
        dateModule.SetupVoid("dispose", _ => true);
        return context;
    }

    private static string GetActiveOptionText(IRenderedComponent<EditForm> cut, AngleSharp.Dom.IElement trigger)
    {
        var activeOptionId = trigger.GetAttribute("aria-activedescendant");
        Assert.NotNull(activeOptionId);
        return cut.FindAll("[role='option']")
            .Single(option => option.Id == activeOptionId)
            .TextContent
            .Trim();
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

        public DateOnly? OptionalDueDate { get; set; }

        public DateTimeOffset OffsetDueDate { get; set; }

        public DateTimeOffset? OptionalOffsetDueDate { get; set; }

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
