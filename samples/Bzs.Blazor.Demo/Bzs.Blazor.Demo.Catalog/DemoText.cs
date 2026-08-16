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

    private static string Get(string chinese, string english) =>
        DemoCulture.IsChinese(CultureInfo.CurrentUICulture.Name) ? chinese : english;
}
