using System.Globalization;
using Bzs.Blazor;

namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// Typed bilingual text catalog for visitor-facing demo copy. Entries resolve zh-Hans or
/// en-US from <see cref="CultureInfo.CurrentUICulture"/>, which each host applies from the
/// <c>?culture=</c> URL parameter; zh-Hans is the default when no parameter is present.
/// </summary>
public static class DemoText
{
    public static class Chrome
    {
        public static string SkipLink => Get("跳至目录内容", "Skip to catalog content");

        public static string NavigationAccessibleName => Get("Bzs.Blazor 目录", "Bzs.Blazor catalog");

        public static string BrandTagline => Get("组件实验室", "Component lab");

        public static string CloseNavigation => Get("关闭导航", "Close navigation");

        public static string CatalogSection => Get("目录", "Catalog");

        public static string Overview => Get("概览", "Overview");

        public static string ThemeFoundation => Get("主题基础", "Theme foundation");

        public static string FoundationComponents => Get("基础组件", "Foundation components");

        public static string Forms => Get("表单", "Forms");

        public static string Productivity => Get("生产力", "Productivity");

        public static string Feedback => Get("反馈", "Feedback");

        public static string Tabs => Get("选项卡", "Tabs");

        public static string Overlays => Get("浮层", "Overlays");

        public static string Layout => Get("布局", "Layout");

        public static string NavigationDrawer => Get("导航抽屉", "Navigation drawer");

        public static string ProjectSection => Get("项目", "Project");

        public static string Releases => Get("版本发布", "Releases");

        public static string RenderModesSection => Get("渲染模式", "Render modes");

        public static string RuntimeSection => Get("运行时", "Runtime");

        public static string StaticSsr => Get("静态 SSR", "Static SSR");

        public static string InteractiveServer => Get("交互式服务器", "Interactive Server");

        public static string InteractiveWebAssembly => Get("交互式 WebAssembly", "Interactive WebAssembly");

        public static string InteractiveAuto => Get("交互式自动", "Interactive Auto");

        public static string DemoUser => Get("演示用户", "Demo User");

        public static string DemoUserAvatarInitial => Get("演", "D");

        public static string Administrator => Get("管理员", "Administrator");

        public static string Exit => Get("退出", "Exit");

        public static string SignOutAccessibleName => Get("演示退出操作，返回概览", "Demo sign-out action, returns to overview");

        public static string OpenNavigation => Get("打开导航", "Open navigation");

        public static string ResizeNavigationDrawer => Get("调整导航抽屉宽度", "Resize navigation drawer");

        public static string ComponentWorkbench => Get("组件工作台", "Component workbench");

        public static string AspireDemoHost => Get("Aspire 演示主机", "Aspire demo host");

        public static string StaticWebAssemblyHost => Get("静态 WebAssembly 主机", "Static WebAssembly host");

        public static string LanguageSwitcherAccessibleName => Get("目录语言", "Catalog language");

        public static string ThemeSwitcherAccessibleName => Get("目录主题", "Catalog theme");

        public static string ThemeLight => Get("浅色", "Light");

        public static string ThemeDark => Get("深色", "Dark");

        public static string ThemeSystem => Get("系统", "System");

        public static string InteractionError => Get("目录无法完成此交互。", "The catalog could not complete this interaction.");

        public static string Reload => Get("重新加载", "Reload");

        public static string WhatsNew => Get("更新公告", "What's new");

        public static string ViewAllReleases => Get("查看所有版本", "View all releases");

        public static string MarkAsRead => Get("标为已读", "Mark as read");

        public static string ReleaseDialogTitle(string version) => Get(
            $"Bzs.Blazor {version} 更新内容",
            $"What's new in Bzs.Blazor {version}");

        public static string UnreadReleaseAnnouncement(int count) => Get(
            $"{count.ToString(CultureInfo.CurrentCulture)} 个未读版本公告",
            $"{count.ToString(CultureInfo.CurrentCulture)} unread release announcement{(count == 1 ? string.Empty : "s")}");

        public static string ReleaseAnnouncementTriggerAccessibleName(int unreadCount) => unreadCount > 0
            ? Get(
                $"更新公告，{unreadCount.ToString(CultureInfo.CurrentCulture)} 个未读版本",
                $"What's new, {unreadCount.ToString(CultureInfo.CurrentCulture)} unread release announcement{(unreadCount == 1 ? string.Empty : "s")}")
            : WhatsNew;
    }

    public static class Landing
    {
        public static string PageTitle => Get("Bzs.Blazor 组件库", "Bzs.Blazor component library");

        public static string HeroEyebrow => Get(".NET 10 组件库", ".NET 10 component library");

        public static string HeroTitle => Get("为 Blazor 而生的紧凑组件库", "A compact component library for Blazor");

        public static string HeroSummary => Get(
            "50 个公开组件、克制的新拟态主题和全部四种渲染模式，来自一个零第三方 UI 依赖的包。",
            "50 public components, restrained neumorphic themes, and all four render modes from one package with zero third-party UI dependencies.");

        public static string HeroLogoAccessibleName => Get("Bzs.Blazor 徽标", "Bzs.Blazor logo");

        public static string InstallCta => Get("快速上手", "Get started");

        public static string GroupsCta => Get("浏览组件", "Browse components");

        public static string GitHubLink => Get("在 GitHub 上查看", "View on GitHub");

        public static string StripHeading => Get("亲自试一试", "Try it live");

        public static string StripSummary => Get(
            "无需离开首页：真实的按钮、表单控件、通知和对话框，全部来自库本身。",
            "Real buttons, form controls, a toast, and a dialog from the library itself — no need to leave this page.");

        public static string StripButtonsHeading => Get("按钮变体", "Button variants");

        public static string ButtonPrimary => Get("主要", "Primary");

        public static string ButtonSecondary => Get("次要", "Secondary");

        public static string ButtonOutline => Get("描边", "Outline");

        public static string ButtonGhost => Get("幽灵", "Ghost");

        public static string ButtonDanger => Get("危险", "Danger");

        public static string StripFormHeading => Get("紧凑表单", "Compact form");

        public static string NameLabel => Get("姓名", "Name");

        public static string WorkspaceLabel => Get("工作区", "Workspace");

        public static string WorkspaceProduction => Get("生产", "Production");

        public static string WorkspaceStaging => Get("预发", "Staging");

        public static string WorkspaceReview => Get("评审", "Review");

        public static string NotificationsLabel => Get("接收通知", "Receive notifications");

        public static string StripOverlaysHeading => Get("通知与对话框", "Toast and dialog");

        public static string ShowToast => Get("显示通知", "Show toast");

        public static string ToastTitle => Get("保存成功", "Saved");

        public static string ToastMessage => Get(
            "这条通知由 IBzsToastService 呈现。",
            "This notification is rendered through IBzsToastService.");

        public static string ToastAccessibleName => Get("演示通知", "Demo toast");

        public static string OpenDialog => Get("打开对话框", "Open dialog");

        public static string DialogTitle => Get("受控对话框", "Controlled dialog");

        public static string DialogBody => Get(
            "对话框通过 Open 与 OpenChanged 受控，按 Escape 即可关闭。",
            "This dialog is controlled through Open and OpenChanged, and closes with Escape.");

        public static string DialogClose => Get("关闭", "Close");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("等待交互运行时", "Waiting for interactive runtime");

        public static string InstallHeading => Get("安装", "Installation");

        public static string InstallSummary => Get(
            "一个包、一次注册。以下命令与 README 保持一致。",
            "One package and one registration call, identical to the README guidance.");

        public static string CopyInstall => Get("复制", "Copy");

        public static string CopyInstallAccessibleName => Get("复制安装命令", "Copy install commands");

        public static string Copied => Get("已复制", "Copied");

        public static string FeaturesHeading => Get("为什么是 Bzs.Blazor", "Why Bzs.Blazor");

        public static string FeatureZeroDependenciesTitle => Get("零 UI 依赖", "Zero UI dependencies");

        public static string FeatureZeroDependenciesBody => Get(
            "运行时只依赖 .NET，不会有第三方 UI 库进入你的依赖树。",
            "The runtime depends on .NET only — no third-party UI library enters your dependency tree.");

        public static string FeatureRenderModesTitle => Get("覆盖全部渲染模式", "Every render mode");

        public static string FeatureRenderModesBody => Get(
            "同一组组件运行于静态 SSR、Interactive Server、WebAssembly 和 Auto。",
            "The same components run under Static SSR, Interactive Server, WebAssembly, and Auto.");

        public static string FeatureThemesTitle => Get("新拟态主题", "Neumorphic themes");

        public static string FeatureThemesBody => Get(
            "克制的立体表面与语义化令牌，内置浅色和深色。",
            "Restrained raised surfaces with semantic tokens, in built-in light and dark.");

        public static string FeatureAccessibilityTitle => Get("可访问性", "Accessibility");

        public static string FeatureAccessibilityBody => Get(
            "以 WCAG 2.2 AA 为目标，键盘导航和读屏语义开箱即用。",
            "Targets WCAG 2.2 AA with keyboard navigation and screen-reader semantics built in.");

        public static string FeatureLocalizationTitle => Get("中英双语", "Chinese and English");

        public static string FeatureLocalizationBody => Get(
            "基于标准 .NET 本地化，运行时文案内置 zh-Hans 与 en-US。",
            "Standard .NET localization with built-in zh-Hans and en-US runtime strings.");

        public static string FeatureDataGridTitle => Get("类型化 DataGrid", "Typed DataGrid");

        public static string FeatureDataGridBody => Get(
            "排序、筛选、分页、选择、模板和异步数据提供器。",
            "Sorting, filtering, paging, selection, templates, and asynchronous providers.");

        public static string GroupsHeading => Get("组件分组", "Component groups");

        public static string GroupsSummary => Get("从首页直达每一个演示页面。", "Every demo page is one click away.");

        public static string GroupThemeFoundationDescription => Get("主题模式、密度与语义令牌。", "Theme modes, density, and semantic tokens.");

        public static string GroupFoundationDescription => Get("图标、表面、按钮与排版基础。", "Icons, surfaces, buttons, and typographic basics.");

        public static string GroupFormsDescription => Get("原生 Blazor 表单契约与验证。", "Native Blazor form contracts with validation.");

        public static string GroupProductivityDescription => Get("数据密集型工作台与类型化 DataGrid。", "Data-dense workbench and the typed DataGrid.");

        public static string GroupFeedbackDescription => Get("消息、通知与状态反馈。", "Messages, toasts, and status feedback.");

        public static string GroupTabsDescription => Get("选项卡、语言与方向切换。", "Tabs with language and direction switching.");

        public static string GroupOverlaysDescription => Get("对话框、抽屉与锚定浮层。", "Dialogs, drawers, and anchored overlays.");

        public static string GroupLayoutDescription => Get("应用壳、栅格与堆叠布局。", "App shell, grid, and stack primitives.");

        public static string GroupNavigationDrawerDescription => Get(
            "变体、焦点、Escape 与受控关闭生命周期。",
            "Variants, focus, Escape, and controlled close lifecycle.");

        public static string ReleaseHeading => Get("最新版本", "Latest release");

        public static string HighlightsHeading => Get("主要更新", "Highlights");

        public static string StaticSsrDescription => Get("有意义的被动标记。", "Meaningful passive markup.");

        public static string InteractiveServerDescription => Get("服务器线路交互。", "Server circuit interaction.");

        public static string InteractiveWebAssemblyDescription => Get("浏览器承载的交互。", "Browser-hosted interaction.");

        public static string InteractiveAutoDescription => Get("自动在服务器与浏览器之间选择。", "Automatic server-to-browser selection.");

        public static string StandaloneRuntimeDescription => Get("无需服务器运行时的浏览器交互。", "Browser-hosted interaction without a server runtime.");

        public static string FooterLinksAccessibleName => Get("项目资源", "Project resources");

        public static string FooterNuGet => Get("NuGet 包", "NuGet package");

        public static string FooterLicense => Get("MIT 许可证", "MIT license");

        public static string FooterNote => Get("基于 MIT 许可证发布。", "Released under the MIT license.");
    }

    public static class Productivity
    {
        public static string DataGridTitle => Get("数据工作台", "Data workbench");

        public static string DataGridDescription => Get(
            "类型化提供器负责分页、排序与筛选；表格统一呈现加载、错误、选择和结果状态。",
            "A typed provider owns paging, sorting, and filtering while the grid presents loading, error, selection, and result state as one workspace.");

        public static string ReviewQueue => Get("评审队列", "Review queue");

        public static string ReviewColumn => Get("评审", "Review");

        public static string OwnerColumn => Get("负责人", "Owner");

        public static string PriorityColumn => Get("优先级", "Priority");

        public static string RefreshGrid => Get("刷新数据网格", "Refresh DataGrid");

        public static string RefreshGridAccessibleName => Get("刷新当前数据网格请求", "Refresh the current DataGrid request");

        public static string GridRefreshReady => Get("数据网格已准备好刷新。", "The DataGrid is ready to refresh.");

        public static string GridRefreshed => Get("数据网格已使用相同的提供程序请求刷新。", "The DataGrid refreshed with the same provider request.");

        public static string CompactGridTitle => Get("紧凑数据网格", "Compact DataGrid");

        public static string CompactGridDescription => Get(
            "分页仍由受控参数决定，而页脚控件可以独立隐藏。",
            "Paging remains controlled while footer controls can be hidden independently.");

        public static string CompactGridReviewColumn => Get("评审", "Review");

        public static string CompactGridOwnerColumn => Get("负责人", "Owner");

        public static string CompactGridReleaseNotes => Get("发布说明", "Release notes");

        public static string CompactGridKeyboardAudit => Get("键盘审计", "Keyboard audit");

        public static string CompactGridNormalPriority => Get("普通", "Normal");

        public static string CompactGridHighPriority => Get("高", "High");

        public static string PageSizeOnlyGridTitle => Get("仅显示每页数量", "Page-size selector only");

        public static string PageSizeOnlyGridDescription => Get(
            "保留每页数量选择器，同时独立隐藏数字分页。",
            "Keep the page-size selector while independently hiding numeric pagination.");

        public static string SelectCurrentPage => Get("选择本页所有行", "Select all rows on this page");

        public static string GridSearchLabel => Get("搜索评审", "Search reviews");

        public static string GridColumnChooser => Get("选择显示的列", "Choose visible columns");

        public static string WorkbenchGridTitle => Get("成熟表格能力", "Mature grid capabilities");

        public static string WorkbenchGridDescription => Get(
            "同一个受控表格同时提供全局搜索、多列排序、列选择器、列宽调整、粘性表头、行详情与页脚聚合。",
            "One controlled grid combines global search, multi-column sorting, a column chooser, column resizing, a sticky header, row details, and footer aggregates.");

        public static string WorkbenchGridCaption => Get("评审组合", "Review portfolio");

        public static string StatusColumn => Get("状态", "Status");

        public static string HoursColumn => Get("工时", "Hours");

        public static string DueColumn => Get("截止日期", "Due");

        public static string ReviewCount => Get("评审数量", "Reviews");

        public static string DetailStatus => Get("当前状态", "Current status");

        public static string DetailNotes => Get("备注", "Notes");

        public static string StatusOpen => Get("进行中", "Open");

        public static string StatusBlocked => Get("已阻塞", "Blocked");

        public static string StatusDone => Get("已完成", "Done");

        public static string HarborLighting => Get("北港照明改造", "North Harbor lighting");

        public static string WarehouseAccessibility => Get("仓库无障碍评审", "Warehouse accessibility");

        public static string ReleaseNotesReview => Get("发布说明评审", "Release notes review");

        public static string TabletNavigation => Get("平板导航适配", "Tablet navigation");

        public static string ContrastAudit => Get("对比度审计", "Color contrast audit");

        public static string LocalizationPass => Get("本地化校对", "Localization pass");

        public static string HarborLightingNotes => Get("现场勘察待安排。", "Site survey pending.");

        public static string WarehouseAccessibilityNotes => Get(
            "等待语义令牌确认。",
            "Waiting on semantic tokens.");

        public static string ReleaseNotesReviewNotes => Get(
            "已随 0.5.0 发布说明一同发布。",
            "Published with the 0.5.0 notes.");

        public static string TabletNavigationNotes => Get(
            "抽屉宽度调整需要键盘复核。",
            "Drawer resize needs a keyboard pass.");

        public static string ContrastAuditNotes => Get("强制颜色模式复核排期中。", "Forced-colors sweep queued.");

        public static string LocalizationPassNotes => Get(
            "zh-Hans 与 en-US 文案已核对。",
            "zh-Hans and en-US copy verified.");
    }

    public static class Forms
    {
        public static string PasswordLabel => Get("访问密码", "Access password");

        public static string PasswordDescription => Get(
            "显示按钮仅在交互式运行时切换原生输入类型。",
            "The reveal action changes only the native input type while interactive.");

        public static string TextInputModesTitle => Get("文本输入类型和更新模式", "Text input types and update modes");

        public static string TextInputModesDescription => Get(
            "电子邮件和搜索输入保留原生语义。实时搜索会在每次已提交输入后更新，并在输入法组合期间等待最终文本。",
            "Email and search inputs retain native semantics. Live search updates after each committed input and waits for final IME text during composition.");

        public static string EmailLabel => Get("联系邮箱", "Contact email");

        public static string LiveSearchLabel => Get("实时搜索", "Live search");

        public static string LiveSearchPlaceholder => Get("输入以筛选", "Type to filter");

        public static string LiveSearchEmpty => Get("尚无实时搜索文本。", "No live search text yet.");

        public static string LiveSearchCommitted(string text) =>
            Get($"已提交的实时搜索：{text}", $"Committed live search: {text}");
    }

    public static class Releases
    {
        public static string PageTitle => Get("版本公告 - Bzs.Blazor", "Releases - Bzs.Blazor");

        public static string Eyebrow => Get("项目更新", "Project updates");

        public static string Title => Get("版本公告", "Release announcements");

        public static string Summary => Get(
            "查看 Bzs.Blazor 每个版本的重要变化、兼容性说明和新增控件。",
            "Review the important changes, compatibility notes, and new components in every Bzs.Blazor release.");

        public static string HighlightsHeading => Get("主要更新", "Highlights");
    }

    public static class Foundation
    {
        public static string PageTitle => Get("基础组件 - Bzs.Blazor", "Foundation components - Bzs.Blazor");

        public static string Eyebrow => Get("基础组件", "Foundation components");

        public static string Title => Get("图标、表面与按钮", "Icon, Surface, and Button");

        public static string Summary => Get(
            "第一批可见的组件只使用 Bzs.Blazor 的语义令牌。",
            "The first visible component slice uses only semantic Bzs.Blazor tokens.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("等待交互运行时", "Waiting for interactive runtime");

        public static string SurfacesHeading => Get("表面", "Surfaces");

        public static string SurfaceBase => Get("基础层", "Base");

        public static string SurfaceRaised => Get("凸起层", "Raised");

        public static string SurfaceInset => Get("凹陷层", "Inset");

        public static string SurfaceOverlay => Get("浮层", "Overlay");

        public static string ButtonsHeading => Get("按钮", "Buttons");

        public static string PrimaryAction(int count) => Get(
            $"主要操作 {count.ToString(CultureInfo.CurrentCulture)}",
            $"Primary action {count.ToString(CultureInfo.CurrentCulture)}");

        public static string Secondary => Get("次要", "Secondary");

        public static string Outline => Get("描边", "Outline");

        public static string Ghost => Get("幽灵", "Ghost");

        public static string Danger => Get("危险", "Danger");

        public static string Saving => Get("保存中", "Saving");

        public static string Disabled => Get("已禁用", "Disabled");

        public static string CloseExample => Get("关闭示例", "Close example");

        public static string SizeSmall => Get("小", "Small");

        public static string SizeMedium => Get("中", "Medium");

        public static string SizeLarge => Get("大", "Large");

        public static string ModeLight => Get("浅色", "Light");

        public static string ModeDark => Get("深色", "Dark");

        public static string DensityCompact => Get("紧凑密度", "Compact density");

        public static string DensityComfortable => Get("宽松密度", "Comfortable density");

        public static string IconsHeading => Get("图标", "Icons");

        public static string IconExamplesAccessibleName => Get("精选图标示例", "Curated icon examples");

        public static string IconSuccess => Get("成功", "Success");

        public static string IconInformation => Get("信息", "Information");

        public static string IconWarning => Get("警告", "Warning");

        public static string IconError => Get("错误", "Error");

        public static string IconCalendar => Get("日历", "Calendar");

        public static string IconShowPassword => Get("显示密码图标", "Show password icon");

        public static string IconHidePassword => Get("隐藏密码图标", "Hide password icon");
    }

    public static class ThemeFoundation
    {
        public static string PageTitle => Get("主题基础 - Bzs.Blazor", "Theme foundation - Bzs.Blazor");

        public static string Eyebrow => Get("主题基础", "Theme foundation");

        public static string Title => Get("浅色、深色与跟随系统", "Light, Dark, and System");

        public static string Summary => Get(
            "应用拥有模式和密度，Bzs.Blazor 只提供语义令牌。",
            "The application owns mode and density while Bzs.Blazor supplies semantic tokens.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("等待交互运行时", "Waiting for interactive runtime");

        public static string ControlsAccessibleName => Get("主题控件", "Theme controls");

        public static string ModeGroupAccessibleName => Get("主题模式", "Theme mode");

        public static string DensityGroupAccessibleName => Get("主题密度", "Theme density");

        public static string ModeLight => Get("浅色", "Light");

        public static string ModeDark => Get("深色", "Dark");

        public static string ModeSystem => Get("跟随系统", "System");

        public static string DensityCompact => Get("紧凑", "Compact");

        public static string DensityComfortable => Get("宽松", "Comfortable");

        public static string RequestedState(BzsThemeMode mode, BzsDensity density) => Get(
            $"请求的模式：{mode}；密度：{density}",
            $"Requested mode: {mode}; density: {density}");

        public static string ExternalOverrideNote => Get(
            "外部自定义 CSS 覆盖了语义主色令牌。",
            "External custom CSS overrides the semantic primary token.");
    }

    public static class Feedback
    {
        public static string PageTitle => Get("反馈 - Bzs.Blazor", "Feedback - Bzs.Blazor");

        public static string Eyebrow => Get("反馈", "Feedback");

        public static string Title => Get("状态与通知", "Status and notifications");

        public static string Summary => Get(
            "内联状态留在工作流内部，而作用域通知保持可独立操作。",
            "Inline status stays in the workflow while scoped notifications remain independently actionable.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("正在渲染反馈标记", "Rendering feedback markup");

        public static string MessageHeading => Get("消息", "Message");

        public static string MessageInformationTitle => Get("评审进行中", "Review in progress");

        public static string MessageInformationBody => Get(
            "还有两项交付物没有负责人。",
            "Two deliverables still need an owner.");

        public static string MessageSuccessTitle => Get("发布完成", "Publish complete");

        public static string MessageSuccessBody => Get(
            "已批准的包可以交给下游团队了。",
            "The approved package is ready for downstream teams.");

        public static string MessageWarningTitle => Get("截止日期临近", "Due date approaching");

        public static string MessageWarningBody => Get("照明评审明天到期。", "The lighting pass is due tomorrow.");

        public static string MessageErrorTitle => Get("保存被阻止", "Save blocked");

        public static string MessageErrorBody => Get(
            "发布前请先修正必填字段。",
            "Correct the required fields before publishing.");

        public static string ProgressHeading => Get("进度", "Progress");

        public static string ProgressPublishing => Get("正在发布资源", "Publishing assets");

        public static string ProgressChecking => Get("正在检查依赖", "Checking dependencies");

        public static string ToastHeading => Get("通知", "Toast");

        public static string ToastExamplesAccessibleName => Get("通知示例", "Toast examples");

        public static string ActiveToastExamplesAccessibleName => Get("活动通知示例", "Active toast examples");

        public static string ShowTimedToast => Get("显示定时通知", "Show timed toast");

        public static string ShowPersistentToast => Get("显示常驻通知", "Show persistent toast");

        public static string ShowErrorToast => Get("显示错误通知", "Show error toast");

        public static string NoActiveNotifications => Get("当前没有通知。", "No active notifications.");

        public static string TimedToastTitle => Get("交付已排队", "Delivery queued");

        public static string TimedToastMessage => Get(
            "指针悬停或获得焦点时，这条通知会暂停计时。",
            "This notification pauses while it is hovered or focused.");

        public static string TimedToastAccessibleName => Get("定时通知", "Timed toast");

        public static string PersistentToastTitle => Get("评审已保存", "Review saved");

        public static string PersistentToastMessage => Get(
            "这条通知会一直保留，直到你关闭它。",
            "This notification remains until you close it.");

        public static string PersistentToastAccessibleName => Get("常驻通知", "Persistent toast");

        public static string ErrorToastTitle => Get("保存失败", "Save failed");

        public static string ErrorToastMessage => Get(
            "评审未能保存。请修正字段值后重试。",
            "The review could not be saved. Try again after correcting the field values.");

        public static string ErrorToastAccessibleName => Get("保存失败通知", "Save failure toast");
    }

    public static class Layout
    {
        public static string PageTitle => Get("布局 - Bzs.Blazor", "Layout - Bzs.Blazor");

        public static string Eyebrow => Get("布局", "Layout");

        public static string Title => Get("应用壳、栅格与堆叠", "App Shell, Grid, and Stack");

        public static string Summary => Get(
            "无需 JavaScript 即可组合应用框架、响应式导航、页面宽度、弹性行和分隔线。每个实时预览都配有产生它的 Razor 代码。",
            "Compose an application frame, responsive navigation, page width, flexible rows, and separators without JavaScript. Each live preview is paired with the Razor that produces it.");

        public static string PaletteAccessibleName => Get("布局主题色", "Layout theme colors");

        public static string TonePrimary => Get("主色", "Primary");

        public static string ToneInfo => Get("信息", "Info");

        public static string ToneSuccess => Get("成功", "Success");

        public static string ToneWarning => Get("警告", "Warning");

        public static string ToneError => Get("错误", "Error");

        public static string AppShellHeading => Get("应用栏与导航抽屉", "App bar and navigation drawer");

        // The layout descriptions name API identifiers and CSS values, which stay untranslated inside
        // <code>. Each description is split around those tokens so both languages arrange their own
        // prose around them rather than forcing English word order onto Chinese.
        public static string AppShellDescriptionLead => Get(
            "按文档顺序组合外壳：导航抽屉、应用栏，然后是主内容。响应式抽屉在较宽的屏幕上预留空间，并在宽度低于 ",
            "Compose the shell in document order: navigation drawer, app bar, then main content. The responsive drawer reserves space on wider screens and becomes an overlay below ");

        public static string AppShellDescriptionTail => Get(" 时变为浮层。", ".");

        public static string InteractivePreview => Get("交互式预览", "Interactive preview");

        public static string LivePreview => Get("实时预览", "Live preview");

        public static string ControlledNavigation => Get("受控导航", "Controlled navigation");

        public static string ContainerHeading => Get("容器", "Container");

        public static string ContainerDescriptionLead => Get("使用 ", "Use ");

        public static string ContainerDescriptionMiddle => Get(
            " 获得分级的视口宽度，或设置 ",
            " for stepped viewport widths, or set ");

        public static string ContainerDescriptionTail => Get(
            " 指定单一的响应式最大宽度。默认包含内边距。",
            " for one responsive maximum. Gutters are included by default.");

        public static string FixedBreakpoints => Get("固定断点", "Fixed breakpoints");

        public static string FixedContentRegion => Get("固定宽度内容区", "Fixed content region");

        public static string GridHeading => Get("响应式栅格", "Responsive grid");

        public static string GridDescriptionLead => Get(
            "栅格项可跨 1-12 列。下面这些项在小屏上占满宽度，在 ",
            "Grid items span 1-12 columns. These items are full width on small screens, split at ");

        public static string GridDescriptionMiddle => Get(" 处分为两列，在 ", ", and form three equal columns at ");

        public static string GridDescriptionTail => Get(" 处形成三个等宽列。", ".");

        public static string TwelveColumnGrid => Get("12 列栅格", "12-column grid");

        public static string TileProduction => Get("生产", "Production");

        public static string TileReview => Get("评审", "Review");

        public static string TileArchive => Get("归档", "Archive");

        public static string StackHeading => Get("堆叠与间隔", "Stack and spacer");

        public static string StackDescriptionLead => Get("为水平工具栏设置 ", "Set ");

        public static string StackDescriptionMiddle => Get("。一个 ", " for horizontal toolbars. A ");

        public static string StackDescriptionTail => Get(
            " 会占满主轴上的剩余空间，把后续内容推到最远端。",
            " consumes the remaining main-axis space and pushes the following content to the far edge.");

        public static string ToolbarAlignment => Get("工具栏对齐", "Toolbar alignment");

        public static string Queue => Get("队列", "Queue");

        public static string QueueCount => Get("12 项", "12 items");

        public static string DividerHeading => Get("分隔线", "Dividers");

        public static string DividerDescriptionLead => Get(
            "分隔线可以是水平或垂直的，可由弹性行拉伸，也可固定在定位过的父元素内部。使用 ",
            "Dividers can be horizontal, vertical, stretched by a flex row, or pinned inside a positioned parent. Use ");

        public static string DividerDescriptionTail => Get(
            " 让线条与周围边缘保持距离。",
            " to keep the line away from surrounding edges.");

        public static string SeparatorVariants => Get("分隔线变体", "Separator variants");

        public static string StatusReady => Get("就绪", "Ready");

        public static string StatusInProgress => Get("进行中", "In progress");

        public static string StatusComplete => Get("已完成", "Complete");

        public static string AbsoluteBoundary => Get("绝对定位边界", "Absolute boundary");

        // The app-shell preview imitates the catalog's own chrome, so its labels intentionally reuse
        // the chrome catalog rather than repeating those translations here.
        public static string WorkspaceNavigation => Get("工作区导航", "Workspace navigation");

        public static string PreviewKicker => Get("今日", "Today");

        public static string PreviewHeading => Get("评审运营", "Review operations");

        public static string PreviewMetricReady => Get("就绪", "Ready");

        public static string PreviewMetricInReview => Get("评审中", "In review");

        public static string PreviewMetricBlocked => Get("已阻塞", "Blocked");

        public static string ReturnToOverview => Get("返回概览", "Return to overview");

        public static string LanguageAccessibleName => Get("语言", "Language");
    }

    public static class Tabs
    {
        public static string PageTitle => Get("选项卡 - Bzs.Blazor", "Tabs - Bzs.Blazor");

        public static string Eyebrow => Get("导航", "Navigation");

        public static string Title => Get("选项卡、语言与方向", "Tabs, language, and direction");

        public static string Summary => Get(
            "受控选中让应用状态保持显式，键盘行为则遵循排列方向与书写方向。",
            "Controlled selection keeps application state explicit while keyboard behavior follows orientation and direction.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("正在渲染选项卡标记", "Rendering tabs markup");

        public static string ExamplesAccessibleName => Get("选项卡示例", "Tabs examples");

        public static string AutomaticHeading => Get("自动激活的水平选项卡", "Automatic horizontal tabs");

        public static string AutomaticDescription => Get(
            "方向键、Home 和 End 会直接激活下一个可用选项卡。",
            "Arrow, Home, and End activate the next enabled tab.");

        public static string AutomaticTabsAccessibleName => Get("自动项目选项卡", "Automatic project tabs");

        public static string ManualHeading => Get("手动激活的垂直选项卡", "Manual vertical tabs");

        public static string ManualDescription => Get(
            "方向键只移动焦点；Enter 或空格才提交受控选中。",
            "Arrow keys move focus; Enter or Space commits the controlled selection.");

        public static string ManualTabsAccessibleName => Get("手动账户选项卡", "Manual account tabs");

        public static string OverviewTab => Get("概览", "Overview");

        public static string OverviewPanel => Get(
            "概览已激活：计划、负责人和里程碑均可查看。",
            "Overview is active: plan, owners, and milestones are ready to review.");

        public static string ScheduleTab => Get("日程不可用", "Schedule unavailable");

        public static string SchedulePanel => Get(
            "日程在交付计划获批前不可用。",
            "Schedule is unavailable until the delivery plan is approved.");

        public static string ActivityTab => Get("动态", "Activity");

        public static string ActivityPanel => Get(
            "动态已激活：有三条最近的工作流更新。",
            "Activity is active: three recent workflow updates are available.");

        public static string ProfileTab => Get("个人资料", "Profile");

        public static string ProfilePanel => Get(
            "个人资料保持选中，直到另一个获得焦点的选项卡被显式激活。",
            "Profile details stay selected until another focused tab is explicitly activated.");

        public static string PreferencesTab => Get("偏好设置", "Preferences");

        public static string PreferencesPanel => Get(
            "偏好设置包含此工作区的通知与密度选项。",
            "Preferences contain notification and density choices for this workspace.");

        public static string SecurityTab => Get("安全", "Security");

        public static string SecurityPanel => Get(
            "安全记录活动会话和必需的登录检查。",
            "Security records active sessions and required sign-in checks.");

        public static string SelectedValue(string value) => Get($"已选中：{value}", $"Selected: {value}");

        // The locale specimens below are the demonstration itself: each one shows how the component
        // behaves for a given language and writing direction, so their tab titles and panel copy stay
        // in their own language rather than following the visitor's culture.
        public static string RtlDescription => Get(
            "在这个水平选项卡列表中，ArrowRight 跟随物理右方向。",
            "ArrowRight follows the physical right direction for this horizontal tab list.");
    }

    public static class Overlays
    {
        public static string PageTitle => Get("浮层 - Bzs.Blazor", "Overlays - Bzs.Blazor");

        public static string Eyebrow => Get("浮层工作流", "Overlay workflow");

        public static string Title => Get("对话框、抽屉与浮层宿主", "Dialog, Drawer, and Host");

        public static string Summary => Get(
            "受控浮层和命令式对话框共用一个作用域宿主，同时保留明确的焦点与关闭行为。",
            "Controlled overlays and command-driven dialogs share one scoped host while preserving explicit focus and dismissal behavior.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("正在渲染浮层标记", "Rendering overlay markup");

        public static string ControlledDialogHeading => Get("受控对话框", "Controlled dialog");

        public static string ControlledDialogDescription => Get(
            "Escape 会关闭这个模态对话框，其背景关闭策略保持显式。",
            "Escape closes this modal dialog. Its backdrop policy remains explicit.");

        public static string OpenControlledDialog => Get("打开受控对话框", "Open controlled dialog");

        public static string AllowBackdropDismissal => Get("允许点击背景关闭对话框", "Allow dialog backdrop dismissal");

        public static string DrawersHeading => Get("受控抽屉", "Controlled drawers");

        public static string DrawersDescription => Get(
            "对比末尾侧的模态抽屉与起始侧的非模态抽屉。",
            "Compare an end-placed modal drawer with a start-placed nonmodal drawer.");

        public static string OpenModalDrawer => Get("打开模态抽屉", "Open modal drawer");

        public static string OpenNonmodalDrawer => Get("打开非模态抽屉", "Open nonmodal drawer");

        public static string ServiceDialogHeading => Get("类型化对话框服务", "Typed dialog service");

        public static string ServiceDialogDescription => Get(
            "组件参数通过属性表达式传入，并返回一个明确的布尔结果。",
            "A component parameter is supplied through a property expression and returns an explicit Boolean result.");

        public static string OpenServiceDialog => Get("打开服务对话框", "Open service dialog");

        public static string HostToastHeading => Get("宿主通知", "Host toast");

        public static string HostToastDescription => Get(
            "这条常驻通知由作用域浮层宿主呈现，而非页面本地标记。",
            "The scoped overlay host renders this persistent toast rather than page-local markup.");

        public static string ShowHostToast => Get("显示宿主通知", "Show host toast");

        public static string ControlledDialogBody => Get(
            "用这个对话框检查焦点锁定、背景策略和嵌套堆叠顺序。",
            "Use this dialog to inspect focus trapping, backdrop policy, and nested stack ordering.");

        public static string CompleteControlledDialog => Get("完成受控对话框", "Complete controlled dialog");

        public static string CancelControlledDialog => Get("取消受控对话框", "Cancel controlled dialog");

        public static string OpenNestedServiceDialog => Get("打开嵌套服务对话框", "Open nested service dialog");

        public static string ModalDrawerTitle => Get("模态抽屉", "Modal drawer");

        public static string ModalDrawerBody => Get(
            "这个末尾侧抽屉打开时会锁定背景滚动。",
            "This end-placed drawer locks background scrolling while it is open.");

        public static string CloseModalDrawer => Get("关闭模态抽屉", "Close modal drawer");

        public static string NonmodalDrawerTitle => Get("非模态抽屉", "Nonmodal drawer");

        public static string NonmodalDrawerBody => Get(
            "这个起始侧抽屉保持页面可滚动，且不设置 aria-modal。",
            "This start-placed drawer keeps the page scrollable and does not set aria-modal.");

        public static string CloseNonmodalDrawer => Get("关闭非模态抽屉", "Close nonmodal drawer");

        public static string ControlledDialogClosed => Get("受控对话框已关闭。", "Controlled dialog is closed.");

        public static string ControlledDialogOpenBackdropAllowed => Get(
            "受控对话框已打开，点击背景可以关闭它。",
            "Controlled dialog is open and its backdrop may dismiss it.");

        public static string ControlledDialogOpenBackdropDisabled => Get(
            "受控对话框已打开，点击背景不会关闭它。",
            "Controlled dialog is open and its backdrop is disabled.");

        public static string ControlledDialogCompleted => Get("受控对话框已完成。", "Controlled dialog completed.");

        public static string ControlledDialogCancelled => Get("受控对话框已取消。", "Controlled dialog cancelled.");

        public static string ControlledDialogDismissedBy(BzsDialogDismissReason reason) => Get(
            $"受控对话框已由 {reason} 关闭。",
            $"Controlled dialog dismissed by {reason}.");

        public static string NoDrawerOpen => Get("没有抽屉处于打开状态。", "No drawer is open.");

        public static string ModalDrawerOpen => Get("模态抽屉已打开。", "Modal drawer is open.");

        public static string ModalDrawerClosed => Get("模态抽屉已关闭。", "Modal drawer closed.");

        public static string ModalDrawerDismissedBy(BzsDialogDismissReason reason) => Get(
            $"模态抽屉已由 {reason} 关闭。",
            $"Modal drawer dismissed by {reason}.");

        public static string NonmodalDrawerOpen => Get("非模态抽屉已打开。", "Nonmodal drawer is open.");

        public static string NonmodalDrawerClosed => Get("非模态抽屉已关闭。", "Nonmodal drawer closed.");

        public static string NonmodalDrawerDismissedBy(BzsDialogDismissReason reason) => Get(
            $"非模态抽屉已由 {reason} 关闭。",
            $"Nonmodal drawer dismissed by {reason}.");

        public static string NoServiceDialogResult => Get("尚无服务对话框结果。", "No service dialog result yet.");

        public static string ServiceDialogPrompt => Get(
            "是否批准这个已暂存的浮层工作流？",
            "Approve the staged overlay workflow?");

        public static string ServiceDialogTitle => Get("服务对话框", "Service dialog");

        public static string ServiceDialogCompleted(bool value) => Get(
            $"已完成：{(value ? "true" : "false")}",
            $"Completed: {(value ? "true" : "false")}");

        public static string ServiceDialogResultKind(BzsDialogResultKind kind) => Get(
            $"结果：{kind}",
            $"Result: {kind}");

        public static string HostToastTitle => Get("宿主通知", "Host toast");

        public static string HostToastMessage => Get(
            "作用域浮层宿主呈现了这条常驻通知。",
            "The scoped overlay host rendered this persistent notification.");

        public static string HostToastAccessibleName => Get("浮层宿主通知", "Overlay host toast");
    }

    public static class NavigationDrawer
    {
        public static string PageTitle => Get("导航抽屉生命周期 - Bzs.Blazor", "Navigation drawer lifecycle - Bzs.Blazor");
        public static string Eyebrow => Get("导航与焦点", "Navigation and focus");

        public static string Title => Get("导航抽屉生命周期", "Navigation drawer lifecycle");

        public static string Summary => Get(
            "在一个受控工作台中检查临时、持久和响应式变体，以及模态焦点和关闭策略。",
            "Inspect temporary, persistent, and responsive variants together with modal focus and close policies in one controlled workbench.");

        public static string RuntimeReady => Get("交互运行时已就绪", "Interactive runtime ready");

        public static string RuntimeWaiting => Get("等待交互运行时", "Waiting for interactive runtime");

        public static string Configuration => Get("配置", "Configuration");

        public static string Variant => Get("变体", "Variant");

        public static string Temporary => Get("临时", "Temporary");

        public static string Persistent => Get("持久", "Persistent");

        public static string Responsive => Get("响应式", "Responsive");

        public static string Position => Get("位置", "Position");

        public static string Start => Get("起始侧", "Start");

        public static string End => Get("末尾侧", "End");

        public static string Behavior => Get("模态行为", "Modal behavior");

        public static string CloseOnEscape => Get("按 Escape 关闭", "Close on Escape");

        public static string CloseOnBackdrop => Get("点击背景关闭", "Close on backdrop click");

        public static string UseInitialFocus => Get("使用指定初始焦点", "Use explicit initial focus");

        public static string AcceptDismissals => Get("接受关闭请求", "Accept dismissal requests");

        public static string OpenDrawer => Get("打开导航抽屉", "Open navigation drawer");

        public static string Status => Get("生命周期状态", "Lifecycle status");

        public static string DrawerState => Get("抽屉状态", "Drawer state");

        public static string Open => Get("已打开", "Open");

        public static string Closed => Get("已关闭", "Closed");

        public static string LastRequest => Get("最近关闭请求", "Latest close request");

        public static string NoRequest => Get("尚无", "None yet");

        public static string Accepted => Get("已接受", "Accepted");

        public static string Rejected => Get("已拒绝，抽屉保持打开", "Rejected; drawer remains open");

        public static string PanelTitle => Get("工作区导航", "Workspace navigation");

        public static string PanelSummary => Get(
            "此面板使用当前工作台配置，并始终保留一个直接关闭操作。",
            "This panel uses the current workbench configuration and always keeps a direct close action available.");

        public static string PrimaryAction => Get("主要焦点目标", "Primary focus target");

        public static string OpenDialog => Get("打开嵌套对话框", "Open nested dialog");

        public static string CloseDrawer => Get("关闭导航抽屉", "Close navigation drawer");

        public static string NestedDialogTitle => Get("抽屉中的对话框", "Dialog from navigation drawer");

        public static string NestedDialogPrompt => Get(
            "此对话框由抽屉外的共享浮层宿主呈现。",
            "This dialog is rendered by the shared overlay host outside the drawer.");

        public static string DialogNotOpened => Get("尚未打开嵌套对话框", "Nested dialog not opened");

        public static string DialogCompleted => Get("嵌套对话框已完成", "Nested dialog completed");

        public static string DialogDismissed => Get("嵌套对话框已关闭", "Nested dialog dismissed");
    }

    private static string Get(string chinese, string english) =>
        DemoCulture.IsChinese(CultureInfo.CurrentUICulture.Name) ? chinese : english;
}
