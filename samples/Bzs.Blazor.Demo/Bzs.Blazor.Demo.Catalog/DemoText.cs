using System.Globalization;

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

    private static string Get(string chinese, string english) =>
        DemoCulture.IsChinese(CultureInfo.CurrentUICulture.Name) ? chinese : english;
}
