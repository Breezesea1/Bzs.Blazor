namespace Bzs.Blazor.Demo.Client;

internal sealed record DemoLocalizedText(string English, string Chinese)
{
    public string Resolve(bool isChinese) => isChinese ? Chinese : English;
}

internal sealed record DemoReleaseSection(
    DemoLocalizedText Heading,
    IReadOnlyList<DemoLocalizedText> Items);

internal sealed record DemoReleaseEntry(
    string Id,
    string Version,
    DateTimeOffset PublishedAt,
    DemoLocalizedText Status,
    DemoLocalizedText Title,
    DemoLocalizedText Summary,
    IReadOnlyList<DemoLocalizedText> Highlights,
    IReadOnlyList<DemoReleaseSection> Sections,
    DemoLocalizedText Compatibility);

internal static class DemoReleaseCatalog
{
    public static IReadOnlyList<DemoReleaseEntry> All { get; } =
    [
        new(
            "v0.2.1",
            "0.2.1",
            new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Patch release", "补丁版本"),
            new DemoLocalizedText(
                "Release announcement and UI hardening",
                "版本公告与 UI 稳定性"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.2.1 finalizes the productivity component wave with accessibility, keyboard interaction, and Demo release experience fixes.",
                "Bzs.Blazor 0.2.1 通过可访问性、键盘交互和 Demo 版本公告修复，完成生产力控件系列的发布收尾。"),
            [
                new DemoLocalizedText(
                    "Localized Breadcrumb defaults and culture-aware Badge counts.",
                    "本地化 Breadcrumb 默认名称，并按当前文化格式化 Badge 数字。"),
                new DemoLocalizedText(
                    "Menu keyboard navigation no longer scrolls the content region, which is now keyboard focusable by default.",
                    "Menu 键盘导航不再滚动内容区，主内容区也默认支持键盘聚焦。"),
                new DemoLocalizedText(
                    "A localized What's new entry now provides a latest-release badge, tooltip, dialog, and complete release archive.",
                    "新增本地化更新公告入口，提供最新版徽标、工具提示、弹窗和完整版本归档。"),
                new DemoLocalizedText(
                    "Announcement persistence and JavaScript module cleanup now degrade safely when browser APIs are unavailable.",
                    "浏览器 API 不可用时，公告持久化和 JavaScript 模块清理现在会安全降级。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("Fixes and accessibility", "修复与可访问性"),
                    [
                        new DemoLocalizedText(
                            "Breadcrumb landmarks use localized defaults while preserving explicit consumer labels.",
                            "Breadcrumb 地标使用本地化默认名称，同时保留使用方显式标签。"),
                        new DemoLocalizedText(
                            "Arrow, Home, and End navigation in Menu prevents browser scrolling while retaining predictable focus movement.",
                            "Menu 的方向键、Home 和 End 导航会阻止浏览器滚动，并保持可预测的焦点移动。"),
                        new DemoLocalizedText(
                            "Badge counts use the active culture, and scrollable main content is keyboard reachable with an overridable tabindex.",
                            "Badge 数字使用当前文化，且可滚动主内容支持键盘访问并允许覆盖 tabindex。"),
                        new DemoLocalizedText(
                            "Unused overlay trigger references were removed without changing public component contracts.",
                            "移除未使用的浮层触发器引用，不改变公共组件契约。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Release experience", "发布体验"),
                    [
                        new DemoLocalizedText(
                            "The unread badge tracks only the latest announcement and acknowledgement stores only that release identifier.",
                            "未读徽标仅跟踪最新公告，确认时也只保存该版本标识。"),
                        new DemoLocalizedText(
                            "The release archive presents highlights, categorized details, deferred scope, and compatibility notes in English and Simplified Chinese.",
                            "版本归档以英文和简体中文展示主要更新、分类详情、延期范围和兼容性说明。"),
                        new DemoLocalizedText(
                            "Storage failures fall back to session acknowledgement, and failed module imports no longer rethrow during disposal.",
                            "存储失败时降级为会话内已读，模块导入失败也不会在释放时再次抛出。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes to the public contracts introduced by the 0.2.0 release candidate.",
                "对 0.2.0 候选版本引入的公共契约没有破坏性变更。")),
        new(
            "v0.2.0",
            "0.2.0",
            new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Release candidate", "候选版本"),
            new DemoLocalizedText(
                "The productivity component wave",
                "生产力控件系列"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.2.0 adds the interaction and data primitives needed for compact, data-heavy application workflows.",
                "Bzs.Blazor 0.2.0 加入面向紧凑型数据应用工作流的交互与数据基础控件。"),
            [
                new DemoLocalizedText(
                    "Popover, tooltip, menu, context menu, navigation, breadcrumbs, and pagination.",
                    "新增弹出层、工具提示、菜单、上下文菜单、导航、面包屑和分页。"),
                new DemoLocalizedText(
                    "Badges, chips, avatars, and skeletons for dense status surfaces.",
                    "新增适用于高密度状态界面的徽标、标签、头像和骨架屏。"),
                new DemoLocalizedText(
                    "Provider-backed autocomplete and controlled file selection with EditForm integration.",
                    "新增支持数据提供器的自动完成，以及集成 EditForm 的受控文件选择。"),
                new DemoLocalizedText(
                    "A typed DataGrid with sorting, filtering, paging, selection, templates, and asynchronous providers.",
                    "新增类型化 DataGrid，支持排序、筛选、分页、选择、模板和异步数据提供器。"),
                new DemoLocalizedText(
                    "The same productivity workbench now runs across Static SSR, Server, WebAssembly, Auto, and standalone WebAssembly.",
                    "同一套生产力工作台现已覆盖静态 SSR、Server、WebAssembly、Auto 和独立 WebAssembly。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("New public contracts", "新增公共契约"),
                    [
                        new DemoLocalizedText(
                            "Anchored interaction: Popover, Tooltip, Menu, MenuItem, and ContextMenu with logical placement and controlled dismissal.",
                            "锚定交互：Popover、Tooltip、Menu、MenuItem 和 ContextMenu，支持逻辑定位与受控关闭。"),
                        new DemoLocalizedText(
                            "Navigation and status: NavMenu, NavItem, Breadcrumbs, Pagination, Skeleton, Badge, Chip, and Avatar.",
                            "导航与状态：NavMenu、NavItem、Breadcrumbs、Pagination、Skeleton、Badge、Chip 和 Avatar。"),
                        new DemoLocalizedText(
                            "Asynchronous input: provider-backed Autocomplete and EditForm-integrated FileUpload.",
                            "异步输入：基于数据提供器的 Autocomplete 与集成 EditForm 的 FileUpload。"),
                        new DemoLocalizedText(
                            "Tabular data: typed DataGrid columns, providers, requests, results, sorting, selection, and filter contracts.",
                            "表格数据：类型化 DataGrid 列、数据提供器、请求、结果、排序、选择和筛选契约。"),
                        new DemoLocalizedText(
                            "All new runtime strings ship in English and Simplified Chinese without adding a third-party UI dependency.",
                            "所有新增运行时文案均提供英文和简体中文，且没有增加第三方 UI 依赖。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Deliberately deferred", "明确延期范围"),
                    [
                        new DemoLocalizedText(
                            "DataGrid editing, grouping, hierarchy, drag-and-drop, frozen columns, virtualization, export, and persisted preferences.",
                            "DataGrid 编辑、分组、层级、拖放、冻结列、虚拟化、导出和持久化偏好。"),
                        new DemoLocalizedText(
                            "Nested submenus and Autocomplete free-text values; upload transport, storage, security scanning, retry, and resume remain consumer-owned.",
                            "嵌套子菜单和 Autocomplete 自由文本；上传传输、存储、安全扫描、重试与续传仍由使用方负责。"),
                        new DemoLocalizedText(
                            "TreeView, Accordion, Stepper, advanced pickers, Slider, Rating, Splitter, Timeline, and DropZone.",
                            "TreeView、Accordion、Stepper、高级选择器、Slider、Rating、Splitter、Timeline 和 DropZone。"),
                        new DemoLocalizedText(
                            "Charts, Scheduler, Gantt, Spreadsheet, rich text, maps, media, AI/chat, and a public generic portal adapter.",
                            "Charts、Scheduler、Gantt、Spreadsheet、富文本、地图、媒体、AI/聊天以及公共通用 portal 适配器。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes to previously shipped public APIs.",
                "对已发布的公共 API 没有破坏性变更。")),
    ];

    public static DemoReleaseEntry Latest => All[0];
}
