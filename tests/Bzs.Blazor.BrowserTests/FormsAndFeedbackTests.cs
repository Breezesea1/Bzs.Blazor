using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class FormsAndFeedbackTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task DatePickerFollowsTheHeaderLanguageSelection()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms?next=culture=zh-Hans");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "English", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        await Page.GotoAsync($"{server.BaseUrl}/forms?next=x");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "English", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a date" });
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "Month" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await Page.GetByRole(AriaRole.Link, new() { Name = "中文", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?next=x&culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "中文", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel.GetByRole(AriaRole.Combobox, new() { Name = "月份" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        var catalogNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor catalog", Exact = true });
        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Feedback", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/feedback?culture=zh-Hans");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "中文", Exact = true }))
            .ToHaveAttributeAsync("aria-current", "page");

        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Forms", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" })).ToBeVisibleAsync();
        await input.PressAsync("Escape");

        await catalogNavigation.GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/?culture=zh-Hans");
        var componentGroups = Page.GetByRole(AriaRole.Region, new() { Name = "Component groups" });
        await componentGroups.GetByRole(AriaRole.Link, new() { Name = "03 Forms", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/forms?culture=zh-Hans");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DatePickerOpensAtThePointerAndClosesWithEscape()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 1200);
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.EvaluateAsync("element => element.scrollIntoView({ block: 'center' })");
        var inputBox = await input.BoundingBoxAsync();
        Assert.NotNull(inputBox);

        var clickPosition = new Position { X = 40, Y = 12 };
        var clickX = inputBox.X + clickPosition.X;
        var clickY = inputBox.Y + clickPosition.Y;
        await input.ClickAsync(new() { Position = clickPosition });

        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        await Expect(panel).ToBeVisibleAsync();
        var panelBox = await panel.BoundingBoxAsync();
        Assert.NotNull(panelBox);
        Assert.InRange(Math.Abs(panelBox.X - clickX), 0, 2);
        Assert.InRange(Math.Abs(panelBox.Y - (clickY + 8)), 0, 2);

        await Page.EvaluateAsync("window.scrollBy(0, 40)");
        await Page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(resolve))");
        var scrolledInputBox = await input.BoundingBoxAsync();
        var scrolledPanelBox = await panel.BoundingBoxAsync();
        Assert.NotNull(scrolledInputBox);
        Assert.NotNull(scrolledPanelBox);
        Assert.InRange(Math.Abs(scrolledPanelBox.X - (scrolledInputBox.X + clickPosition.X)), 0, 2);
        Assert.InRange(Math.Abs(scrolledPanelBox.Y - (scrolledInputBox.Y + clickPosition.Y + 8)), 0, 2);

        await input.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        var openCalendar = Page.GetByRole(AriaRole.Button, new() { Name = "打开日历" });
        await openCalendar.ClickAsync();
        await Expect(panel).ToBeVisibleAsync();
        await Expect(openCalendar).ToBeFocusedAsync();
        await openCalendar.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();

        await input.ClickAsync(new() { Position = clickPosition });
        await Expect(panel).ToBeVisibleAsync();
        var month = Page.GetByRole(AriaRole.Combobox, new() { Name = "月份" });
        await month.FocusAsync();
        await month.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DatePickerSelectionClosesThePanelAndUpdatesTheInput()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        var initialValue = await input.InputValueAsync();
        await input.ClickAsync();

        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        await Expect(panel).ToBeVisibleAsync();
        var selectedDate = await panel.Locator("[data-bzs-date-picker-day='true'][aria-selected='true']")
            .GetAttributeAsync("data-date");
        var differentEnabledDay = panel
            .Locator("[data-bzs-date-picker-day='true']:not([disabled]):not([aria-selected='true'])")
            .First;
        var targetDate = await differentEnabledDay.GetAttributeAsync("data-date");
        Assert.NotNull(selectedDate);
        Assert.NotNull(targetDate);
        Assert.NotEqual(selectedDate, targetDate);

        await differentEnabledDay.ClickAsync();

        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).Not.ToHaveValueAsync(initialValue);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task DatePickerSupportsCompleteKeyboardSelection()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        var initialInputValue = await input.InputValueAsync();
        await input.FocusAsync();
        await input.PressAsync("ArrowDown");

        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        await Expect(panel).ToBeVisibleAsync();
        var focusedDay = panel.Locator("[data-bzs-date-picker-day='true'][tabindex='0']");
        await Expect(focusedDay).ToBeFocusedAsync();
        var initialDate = ParseNativeDate(await focusedDay.GetAttributeAsync("data-date"));

        await focusedDay.PressAsync("ArrowRight");
        await Expect(focusedDay).ToBeFocusedAsync();
        var nextDate = initialDate.AddDays(1);
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(nextDate));

        await focusedDay.PressAsync("ArrowLeft");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(initialDate));

        await focusedDay.PressAsync("ArrowUp");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(initialDate.AddDays(-7)));

        await focusedDay.PressAsync("ArrowDown");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(initialDate));

        await focusedDay.PressAsync("ArrowRight");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(nextDate));

        await focusedDay.PressAsync("PageDown");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(nextDate.AddMonths(1)));

        await focusedDay.PressAsync("PageUp");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(nextDate));

        var firstDayOfWeek = CultureInfo.GetCultureInfo("zh-Hans").DateTimeFormat.FirstDayOfWeek;
        var daysSinceStartOfWeek = ((int)nextDate.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        var expectedStartOfWeek = nextDate.AddDays(-daysSinceStartOfWeek);
        await focusedDay.PressAsync("Home");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(expectedStartOfWeek));
        await focusedDay.PressAsync("End");
        await Expect(focusedDay).ToBeFocusedAsync();
        await Expect(focusedDay).ToHaveAttributeAsync("data-date", FormatNativeDate(expectedStartOfWeek.AddDays(6)));

        await focusedDay.PressAsync("Enter");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).Not.ToHaveValueAsync(initialInputValue);
    }

    [Fact]
    public async Task DatePickerPeriodMenusUseAriaListboxesAndUpdateTheCalendar()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();

        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        var grid = panel.GetByRole(AriaRole.Grid);
        await Expect(panel).ToBeVisibleAsync();

        var month = panel.GetByRole(AriaRole.Combobox, new() { Name = "月份" });
        var initialMonthLabel = await grid.GetAttributeAsync("aria-label");
        Assert.NotNull(initialMonthLabel);
        await month.ClickAsync();

        var monthListbox = panel.GetByRole(AriaRole.Listbox, new() { Name = "月份" });
        await Expect(monthListbox).ToBeVisibleAsync();
        await Expect(month).ToHaveAttributeAsync("aria-expanded", "true");
        var selectedMonth = monthListbox.GetByRole(AriaRole.Option, new() { Selected = true });
        await Expect(selectedMonth).ToBeVisibleAsync();
        await Expect(selectedMonth).ToHaveAttributeAsync("aria-selected", "true");

        await monthListbox.GetByRole(AriaRole.Option, new() { Selected = false }).First.ClickAsync();

        await Expect(monthListbox).ToHaveCountAsync(0);
        await Expect(month).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(grid).Not.ToHaveAttributeAsync("aria-label", initialMonthLabel);

        var year = panel.GetByRole(AriaRole.Combobox, new() { Name = "年份" });
        var initialYearLabel = await grid.GetAttributeAsync("aria-label");
        Assert.NotNull(initialYearLabel);
        await year.ClickAsync();

        var yearListbox = panel.GetByRole(AriaRole.Listbox, new() { Name = "年份" });
        await Expect(yearListbox).ToBeVisibleAsync();
        await Expect(year).ToHaveAttributeAsync("aria-expanded", "true");
        var selectedYear = yearListbox.GetByRole(AriaRole.Option, new() { Selected = true });
        await Expect(selectedYear).ToBeVisibleAsync();
        await Expect(selectedYear).ToHaveAttributeAsync("aria-selected", "true");

        await yearListbox.GetByRole(AriaRole.Option, new() { Selected = false }).First.ClickAsync();

        await Expect(yearListbox).ToHaveCountAsync(0);
        await Expect(year).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(grid).Not.ToHaveAttributeAsync("aria-label", initialYearLabel);
    }

    [Fact]
    public async Task DatePickerPeriodMenusSupportKeyboardNavigationAndSelection()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.ClickAsync();

        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = "选择日期" });
        var grid = panel.GetByRole(AriaRole.Grid);
        var month = panel.GetByRole(AriaRole.Combobox, new() { Name = "月份" });
        await month.FocusAsync();
        await month.PressAsync("ArrowDown");

        var monthListbox = panel.GetByRole(AriaRole.Listbox, new() { Name = "月份" });
        await Expect(monthListbox).ToBeVisibleAsync();
        var initialActiveMonth = await month.GetAttributeAsync("aria-activedescendant");
        Assert.NotNull(initialActiveMonth);

        await month.PressAsync("ArrowDown");
        var activeMonthAfterDown = await month.GetAttributeAsync("aria-activedescendant");
        Assert.NotNull(activeMonthAfterDown);
        await Expect(Page.Locator($"#{activeMonthAfterDown}")).ToBeVisibleAsync();

        await month.PressAsync("ArrowUp");
        var activeMonthAfterUp = await month.GetAttributeAsync("aria-activedescendant");
        Assert.NotNull(activeMonthAfterUp);
        await Expect(Page.Locator($"#{activeMonthAfterUp}")).ToBeVisibleAsync();

        await month.PressAsync("Home");
        var firstMonthId = await monthListbox.GetByRole(AriaRole.Option).First.GetAttributeAsync("id");
        Assert.NotNull(firstMonthId);
        await Expect(month).ToHaveAttributeAsync("aria-activedescendant", firstMonthId);

        await month.PressAsync("End");
        var lastMonthId = await monthListbox.GetByRole(AriaRole.Option).Last.GetAttributeAsync("id");
        Assert.NotNull(lastMonthId);
        await Expect(month).ToHaveAttributeAsync("aria-activedescendant", lastMonthId);

        var selectedMonthId = await monthListbox.GetByRole(AriaRole.Option, new() { Selected = true })
            .GetAttributeAsync("id");
        Assert.NotNull(selectedMonthId);
        if (selectedMonthId == lastMonthId)
        {
            await month.PressAsync("ArrowUp");
        }

        var monthLabelBeforeSelection = await grid.GetAttributeAsync("aria-label");
        Assert.NotNull(monthLabelBeforeSelection);
        await month.PressAsync("Enter");
        await Expect(monthListbox).ToHaveCountAsync(0);
        await Expect(month).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(grid).Not.ToHaveAttributeAsync("aria-label", monthLabelBeforeSelection);

        var year = panel.GetByRole(AriaRole.Combobox, new() { Name = "年份" });
        await year.FocusAsync();
        await year.PressAsync("Space");

        var yearListbox = panel.GetByRole(AriaRole.Listbox, new() { Name = "年份" });
        await Expect(yearListbox).ToBeVisibleAsync();
        var selectedYearId = await yearListbox.GetByRole(AriaRole.Option, new() { Selected = true })
            .GetAttributeAsync("id");
        var lastYearId = await yearListbox.GetByRole(AriaRole.Option).Last.GetAttributeAsync("id");
        Assert.NotNull(selectedYearId);
        Assert.NotNull(lastYearId);
        await year.PressAsync(selectedYearId == lastYearId ? "ArrowUp" : "ArrowDown");
        await Expect(year).Not.ToHaveAttributeAsync("aria-activedescendant", selectedYearId);

        var activeYearId = await year.GetAttributeAsync("aria-activedescendant");
        Assert.NotNull(activeYearId);
        await Expect(Page.Locator($"#{activeYearId}")).ToBeVisibleAsync();

        var yearLabelBeforeSelection = await grid.GetAttributeAsync("aria-label");
        Assert.NotNull(yearLabelBeforeSelection);
        await year.PressAsync("Space");
        await Expect(yearListbox).ToHaveCountAsync(0);
        await Expect(year).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(grid).Not.ToHaveAttributeAsync("aria-label", yearLabelBeforeSelection);

        await year.FocusAsync();
        await year.PressAsync("Space");
        await Expect(yearListbox).ToBeVisibleAsync();
        var pageActiveYearId = await year.GetAttributeAsync("aria-activedescendant");
        var yearBeforePage = int.Parse(
            await GetActivePeriodOptionTextAsync(year),
            CultureInfo.InvariantCulture);
        await year.PressAsync("PageDown");
        await Expect(year).Not.ToHaveAttributeAsync("aria-activedescendant", pageActiveYearId!);
        Assert.Equal(
            yearBeforePage + 10,
            int.Parse(await GetActivePeriodOptionTextAsync(year), CultureInfo.InvariantCulture));
        pageActiveYearId = await year.GetAttributeAsync("aria-activedescendant");
        await year.PressAsync("PageUp");
        await Expect(year).Not.ToHaveAttributeAsync("aria-activedescendant", pageActiveYearId!);
        Assert.Equal(
            yearBeforePage,
            int.Parse(await GetActivePeriodOptionTextAsync(year), CultureInfo.InvariantCulture));

        var selectedYearText = (await yearListbox.GetByRole(AriaRole.Option, new() { Selected = true })
            .TextContentAsync() ?? string.Empty).Trim();
        var targetYear = yearListbox.GetByRole(AriaRole.Option, new() { Selected = false }).First;
        var targetYearText = (await targetYear.TextContentAsync() ?? string.Empty).Trim();
        var targetYearId = await targetYear.GetAttributeAsync("id");
        Assert.NotEmpty(selectedYearText);
        Assert.NotEmpty(targetYearText);
        Assert.NotNull(targetYearId);
        Assert.NotEqual(selectedYearText, targetYearText);
        await year.FocusAsync();
        await Page.Keyboard.TypeAsync(targetYearText);
        await Expect(year).ToHaveAttributeAsync("aria-activedescendant", targetYearId);
        Assert.Equal(targetYearText, await GetActivePeriodOptionTextAsync(year));

        var yearLabelBeforeTab = await grid.GetAttributeAsync("aria-label");
        Assert.NotNull(yearLabelBeforeTab);
        await year.PressAsync("Tab");
        await Expect(yearListbox).ToHaveCountAsync(0);
        await Expect(grid).Not.ToHaveAttributeAsync("aria-label", yearLabelBeforeTab);
        await Expect(year).Not.ToBeFocusedAsync();

        await month.FocusAsync();
        await month.PressAsync("ArrowDown");
        await Expect(monthListbox).ToBeVisibleAsync();
        await month.PressAsync("Escape");
        await Expect(monthListbox).ToHaveCountAsync(0);
        await Expect(month).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(panel).ToBeVisibleAsync();

        await month.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DatePickerRemainsScrollableInsideAShortViewport()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(844, 320);
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.EvaluateAsync("element => element.scrollIntoView({ block: 'center' })");
        await input.ClickAsync(new() { Position = new Position { X = 40, Y = 12 } });

        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        await Expect(panel).ToBeVisibleAsync();
        var panelBox = await panel.BoundingBoxAsync();
        Assert.NotNull(panelBox);
        Assert.InRange(panelBox.Y, 7, 9);
        Assert.InRange(panelBox.Y + panelBox.Height, 311, 313);
        Assert.True(await panel.EvaluateAsync<bool>(
            "element => element.scrollHeight > element.clientHeight && getComputedStyle(element).overflowY === 'auto'"));

        var today = panel.GetByRole(AriaRole.Button, new() { Name = "今天" });
        await today.ScrollIntoViewIfNeededAsync();
        await Expect(today).ToBeVisibleAsync();
        await today.ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DatePickerRemainsInteractiveInsideFixedContainmentContexts()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 1000);
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        var field = input.Locator("xpath=ancestor::*[contains(@class, 'bzs-field')][1]");
        await field.EvaluateAsync(
            "element => { element.style.height = `${element.getBoundingClientRect().height}px`; }");
        var clickPosition = new Position { X = 40, Y = 12 };
        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");

        foreach (var containment in new[] { "layout", "paint", "content", "strict" })
        {
            await field.EvaluateAsync("(element, value) => { element.style.contain = value; }", containment);
            await input.EvaluateAsync("element => element.scrollIntoView({ block: 'center' })");
            var inputBox = await input.BoundingBoxAsync();
            Assert.NotNull(inputBox);

            await input.ClickAsync(new() { Position = clickPosition });
            await Expect(panel).ToBeVisibleAsync();
            var panelBox = await panel.BoundingBoxAsync();
            Assert.NotNull(panelBox);
            Assert.InRange(Math.Abs(panelBox.X - (inputBox.X + clickPosition.X)), 0, 2);
            Assert.InRange(Math.Abs(panelBox.Y - (inputBox.Y + clickPosition.Y + 8)), 0, 2);

            var grid = panel.GetByRole(AriaRole.Grid);
            var initialMonth = await grid.GetAttributeAsync("aria-label");
            Assert.NotNull(initialMonth);
            var nextMonth = panel.GetByRole(AriaRole.Button, new() { Name = "下个月" });
            Assert.True(await nextMonth.EvaluateAsync<bool>(
                "element => { const rect = element.getBoundingClientRect(); const hit = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2); return hit !== null && element.contains(hit); }"),
                $"Expected the next-month button to receive pointer hits inside contain: {containment}.");

            await nextMonth.ClickAsync();
            await Expect(grid).Not.ToHaveAttributeAsync("aria-label", initialMonth);

            await nextMonth.PressAsync("Escape");
            await Expect(panel).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task DatePickerFallbackPositionsInsideFixedContainingBlocks()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 1000);
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        var field = input.Locator("xpath=ancestor::*[contains(@class, 'bzs-field')][1]");
        await field.EvaluateAsync(
            "element => { element.style.height = '32rem'; }");
        var clickPosition = new Position { X = 40, Y = 12 };
        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        var containingBlocks = new[]
        {
            new { Property = "will-change", Value = "transform" },
            new { Property = "content-visibility", Value = "auto" },
            new { Property = "container-type", Value = "inline-size" },
            new { Property = "will-change", Value = "content-visibility" },
            new { Property = "will-change", Value = "container-type" },
        };

        foreach (var containingBlock in containingBlocks)
        {
            await field.EvaluateAsync(
                "(element, setting) => { element.style.willChange = ''; element.style.contentVisibility = ''; element.style.containerType = ''; element.style.setProperty(setting.property, setting.value); }",
                new { property = containingBlock.Property, value = containingBlock.Value });
            await input.EvaluateAsync("element => element.scrollIntoView({ block: 'center' })");
            var inputBox = await input.BoundingBoxAsync();
            Assert.NotNull(inputBox);

            await input.ClickAsync(new() { Position = clickPosition });
            await Expect(panel).ToBeVisibleAsync();
            await panel.EvaluateAsync(
                "element => { if (element.matches(':popover-open')) element.hidePopover(); element.removeAttribute('popover'); }");
            await Page.EvaluateAsync(
                "() => { window.dispatchEvent(new Event('resize')); return new Promise(resolve => requestAnimationFrame(resolve)); }");

            await Expect(panel).ToBeVisibleAsync();
            Assert.False(await panel.EvaluateAsync<bool>("element => element.matches(':popover-open')"));
            var panelBox = await panel.BoundingBoxAsync();
            Assert.NotNull(panelBox);
            var horizontalDelta = Math.Abs(panelBox.X - (inputBox.X + clickPosition.X));
            var verticalDelta = Math.Abs(panelBox.Y - (inputBox.Y + clickPosition.Y + 8));
            Assert.True(
                horizontalDelta <= 2,
                $"Expected fallback horizontal alignment for {containingBlock.Property}: {containingBlock.Value}; delta was {horizontalDelta}.");
            Assert.True(
                verticalDelta <= 2,
                $"Expected fallback vertical alignment for {containingBlock.Property}: {containingBlock.Value}; delta was {verticalDelta}.");

            var grid = panel.GetByRole(AriaRole.Grid);
            var initialMonth = await grid.GetAttributeAsync("aria-label");
            Assert.NotNull(initialMonth);
            var nextMonth = panel.GetByRole(AriaRole.Button, new() { Name = "下个月" });
            Assert.True(await nextMonth.EvaluateAsync<bool>(
                "element => { const rect = element.getBoundingClientRect(); const hit = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2); return hit !== null && element.contains(hit); }"),
                $"Expected the fallback panel to receive pointer hits for {containingBlock.Property}: {containingBlock.Value}.");

            await nextMonth.ClickAsync();
            await Expect(grid).Not.ToHaveAttributeAsync("aria-label", initialMonth);

            await nextMonth.PressAsync("Escape");
            await Expect(panel).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task DatePickerWidthStaysInsideANarrowViewportWhenScaled()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(320, 640);
        await Page.GotoAsync(ChineseFormsUrl);
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        var field = input.Locator("xpath=ancestor::*[contains(@class, 'bzs-field')][1]");
        await field.EvaluateAsync(
            "element => { element.style.inlineSize = '220px'; element.style.transform = 'scale(1.25, .8)'; element.style.transformOrigin = 'top left'; }");
        await input.EvaluateAsync("element => element.scrollIntoView({ block: 'center' })");
        await input.ClickAsync(new() { Position = new Position { X = 40, Y = 12 } });

        var panel = Page.Locator("[data-bzs-date-picker-panel='true']");
        await Expect(panel).ToBeVisibleAsync();
        var panelBox = await panel.BoundingBoxAsync();
        Assert.NotNull(panelBox);
        Assert.InRange(panelBox.X, 7, 9);
        Assert.InRange(panelBox.X + panelBox.Width, 311, 313);

        await input.PressAsync("Escape");
        await Expect(panel).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task SearchableSelectsCloseAfterOutsidePointerInteraction()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var releaseNotes = Page.GetByLabel("Release notes");
        foreach (var name in new[] { "Workspace", "Review areas" })
        {
            var select = Page.GetByRole(AriaRole.Combobox, new() { Name = name });
            await select.ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "true");

            await releaseNotes.ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "false");
        }
    }

    [Fact]
    public async Task SearchableSelectKeyboardDoesNotSubmitTheForm()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var workspace = Page.GetByRole(AriaRole.Combobox, new() { Name = "Workspace" });
        await workspace.FocusAsync();
        await workspace.PressAsync("Space");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "true");

        var search = Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search options" });
        await search.FillAsync("final approvals");
        await search.PressAsync("Enter");

        await Expect(workspace).ToContainTextAsync("Review");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.GetByRole(AriaRole.Status).Last).ToHaveTextAsync("Ready to validate the profile.");
    }

    [Fact]
    public async Task SearchableSelectPanelAlignsInsideATransformedContainer()
    {
        BeginBrowserGateTest();
        var pageErrors = new ConcurrentQueue<string>();
        Page.PageError += (_, error) => pageErrors.Enqueue(error);
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        foreach (var name in new[] { "Workspace", "Review areas" })
        {
            var select = Page.GetByRole(AriaRole.Combobox, new() { Name = name });
            var field = select.Locator("xpath=ancestor::*[contains(@class, 'bzs-field')][1]");
            await field.EvaluateAsync("element => { element.style.transform = 'scale(.8)'; element.style.transformOrigin = 'top left'; }");
            await select.ClickAsync();

            var panel = field.Locator("[data-bzs-select-panel='true']");
            await Expect(panel).ToBeVisibleAsync();
            var triggerBox = await select.BoundingBoxAsync();
            var panelBox = await panel.BoundingBoxAsync();
            Assert.NotNull(triggerBox);
            Assert.NotNull(panelBox);
            Assert.InRange(Math.Abs(triggerBox.X - panelBox.X), 0, 1);
            Assert.InRange(Math.Abs(triggerBox.Width - panelBox.Width), 0, 1);

            await Page.GetByLabel("Release notes").ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "false");
        }

        Assert.Empty(pageErrors);
    }

    [Fact]
    public async Task EnhancedChoiceControlsPreserveNativeRequiredAndLabelBehavior()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var constraints = Page.Locator("[data-bzs-select-constraint='true']");
        await Expect(constraints).ToHaveCountAsync(2);
        for (var index = 0; index < 2; index++)
        {
            var isValid = await constraints.Nth(index).EvaluateAsync<bool>(
                "element => { element.selectedIndex = -1; return element.checkValidity(); }");
            Assert.False(isValid);
        }

        var workspace = Page.GetByRole(AriaRole.Combobox, new() { Name = "Workspace" });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).ClickAsync();
        await Expect(workspace).ToBeFocusedAsync();
        await Expect(Page.GetByRole(AriaRole.Status).Last)
            .ToHaveTextAsync("Ready to validate the profile.");

        var selectedWorkflow = Page.Locator("#profile-workflow-option-2");
        var selectedWorkflowLabel =
            Page.Locator("label.bzs-radio-group__option[for='profile-workflow-option-2']");

        await selectedWorkflowLabel.ClickAsync();
        await Expect(selectedWorkflow).ToBeCheckedAsync();

        var workflowLabel = Page.Locator("#profile-workflow-label");
        await Expect(workflowLabel).ToHaveAttributeAsync("for", "profile-workflow-option-2");
        await workflowLabel.ClickAsync();
        await Expect(selectedWorkflow).ToBeFocusedAsync();
        await Expect(selectedWorkflow).ToBeCheckedAsync();
    }

    [Fact]
    public async Task FormsAcceptKeyboardInputValidateInlineAndFitAtTwoHundredPercentZoom()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(640, 720);
        await Page.GotoAsync($"{server.BaseUrl}/forms");

        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var fullName = Page.GetByLabel("Full name");
        await fullName.FocusAsync();
        await fullName.PressAsync("Control+A");
        await fullName.PressSequentiallyAsync("Alicia Santos");
        await fullName.PressAsync("Tab");

        var workEmail = Page.GetByLabel("Work email");
        await workEmail.FocusAsync();
        await workEmail.PressAsync("Control+A");
        await workEmail.PressSequentiallyAsync("not-an-email");
        await workEmail.PressAsync("Tab");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).PressAsync("Enter");
        await Expect(Page.GetByText("Enter a valid work email.")).ToBeVisibleAsync();
        await Expect(workEmail).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.GetByLabel("Disabled assignment")).ToBeDisabledAsync();
        await Expect(Page.GetByLabel("Read-only identifier")).ToHaveAttributeAsync("readonly", "readonly");

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalOverflow);
    }

    [Fact]
    public async Task TimedToastPausesForHoverAndKeyboardFocus()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/feedback");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var showTimedToast = Page.GetByRole(AriaRole.Button, new() { Name = "Show timed toast" });
        var toast = Page.GetByRole(AriaRole.Status, new() { Name = "Timed toast" });

        await showTimedToast.ClickAsync();
        await Expect(toast).ToBeVisibleAsync();
        await toast.HoverAsync();
        await Page.WaitForTimeoutAsync(1800);
        await Expect(toast).ToBeVisibleAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(toast).ToHaveCountAsync(0);

        await showTimedToast.ClickAsync();
        await Expect(toast).ToBeVisibleAsync();
        await toast.GetByRole(AriaRole.Button, new() { Name = "Dismiss notification" }).FocusAsync();
        await Page.WaitForTimeoutAsync(1800);
        await Expect(toast).ToBeVisibleAsync();
        await showTimedToast.FocusAsync();
        await Expect(toast).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ToastCloseLiveRegionAndReducedMotionRemainAccessible()
    {
        BeginBrowserGateTest();
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.GotoAsync($"{server.BaseUrl}/feedback");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var persistentToastButton = Page.GetByRole(AriaRole.Button, new() { Name = "Show persistent toast" });
        var transitionDurations = await persistentToastButton.EvaluateAsync<string[]>(
            "element => getComputedStyle(element).transitionDuration.split(',').map(value => value.trim())");
        Assert.NotEmpty(transitionDurations);
        Assert.All(transitionDurations, duration => Assert.True(IsZeroDuration(duration), $"Expected zero transition duration, got {duration}."));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Show persistent toast" }).ClickAsync();
        var persistentToast = Page.GetByRole(AriaRole.Status, new() { Name = "Persistent toast" });
        await Expect(persistentToast).ToHaveAttributeAsync("aria-live", "polite");
        await persistentToast.GetByRole(AriaRole.Button, new() { Name = "Dismiss notification" }).ClickAsync();
        await Expect(persistentToast).ToHaveCountAsync(0);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Show error toast" }).ClickAsync();
        var errorToast = Page.GetByRole(AriaRole.Alert, new() { Name = "Save failure toast" });
        await Expect(errorToast).ToHaveAttributeAsync("aria-live", "assertive");

        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Interactive runtime ready");
        var loadingIcon = Page.GetByRole(AriaRole.Button, new() { Name = "Saving" })
            .Locator(".bzs-button__loading-icon");
        await Expect(loadingIcon).ToBeVisibleAsync();
        Assert.Equal(
            "none",
            await loadingIcon.EvaluateAsync<string>("element => getComputedStyle(element).animationName"));
    }

    private static bool IsZeroDuration(string duration) =>
        string.Equals(duration, "0s", StringComparison.Ordinal)
        || string.Equals(duration, "0ms", StringComparison.Ordinal);

    private static DateOnly ParseNativeDate(string? value)
    {
        Assert.NotNull(value);
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatNativeDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string ChineseFormsUrl => $"{server.BaseUrl}/forms?culture=zh-Hans";

    private static Task<string> GetActivePeriodOptionTextAsync(ILocator trigger) =>
        trigger.EvaluateAsync<string>(
            "element => document.getElementById(element.getAttribute('aria-activedescendant'))?.textContent?.trim() ?? ''");
}
