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
            "v0.4.0",
            "0.4.0",
            new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Feature release", "功能版本"),
            new DemoLocalizedText(
                "Productivity workflows, resizable navigation, and identity",
                "生产力工作流、可调整导航与身份展示"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.4.0 expands DataGrid productivity workflows, adds resizable navigation drawers and composable avatar identities, and centralizes Demo Catalog destinations across hosts.",
                "Bzs.Blazor 0.4.0 扩展 DataGrid 生产力工作流，新增可调整大小的导航抽屉与可组合头像身份，并在各宿主间统一 Demo Catalog 目的地。"),
            [
                new DemoLocalizedText(
                    "DataGrid adds toolbar composition, result and filter summaries, richer filtering, and explicit workflow controls.",
                    "DataGrid 新增工具栏组合、结果与筛选摘要、更丰富的筛选以及显式工作流控制。"),
                new DemoLocalizedText(
                    "NavigationDrawer supports pointer and keyboard resizing while preserving responsive shell alignment.",
                    "NavigationDrawer 支持指针与键盘调整大小，同时保持响应式应用壳对齐。"),
                new DemoLocalizedText(
                    "Avatar identities support visible names and optional trailing actions without hard-coded account semantics.",
                    "Avatar 身份支持可见名称和可选尾部操作，不硬编码账户语义。"),
                new DemoLocalizedText(
                    "Demo Catalog destinations now share host-aware routes, localized presentation, culture, and base-path behavior.",
                    "Demo Catalog 目的地现在共享宿主感知路由、本地化 presentation、文化参数和 base-path 行为。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("Productivity and navigation", "生产力与导航"),
                    [
                        new DemoLocalizedText(
                            "Provider-backed DataGrid workflows retain accepted rows, retry semantics, controlled state, and accessible commands.",
                            "基于 provider 的 DataGrid 工作流保留已接受行、重试语义、受控状态和可访问命令。"),
                        new DemoLocalizedText(
                            "Resizable drawers synchronize preferred width with the application shell and constrain it as containers change.",
                            "可调整抽屉会将首选宽度与应用壳同步，并在容器变化时约束有效宽度。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Identity and Demo delivery", "身份与 Demo 交付"),
                    [
                        new DemoLocalizedText(
                            "Avatar composition keeps image, fallback, visible identity, accessibility, and optional actions coherent.",
                            "Avatar 组合统一处理图片、fallback、可见身份、可访问性和可选操作。"),
                        new DemoLocalizedText(
                            "Full and standalone Demo hosts consume the same capability-filtered destination and runtime presentation.",
                            "完整与独立 Demo 宿主使用同一套按能力过滤的目的地与运行时 presentation。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Compatibility", "兼容性"),
                    [
                        new DemoLocalizedText(
                            "Existing URLs and render modes remain supported; the new public contracts are additive.",
                            "现有 URL 和渲染模式继续受支持；新增公共契约均为增量扩展。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes to previously published APIs or supported render modes.",
                "对已发布 API 和支持的渲染模式没有破坏性变更。")),
        new(
            "v0.3.0",
            "0.3.0",
            new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Feature release", "功能版本"),
            new DemoLocalizedText(
                "Forms, data workflows, and navigation drawers",
                "表单、数据工作流与导航抽屉"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.3.0 expands text and password input behavior, adds explicit DataGrid refresh and selection controls, and completes the modal navigation-drawer lifecycle across supported render modes.",
                "Bzs.Blazor 0.3.0 扩展文本与密码输入行为，新增显式 DataGrid 刷新和选择控制，并在所有受支持的渲染模式中完善模态导航抽屉生命周期。"),
            [
                new DemoLocalizedText(
                    "TextInput supports semantic input types and configurable change or input update timing.",
                    "TextInput 支持语义化输入类型，以及可配置的 change 或 input 更新时机。"),
                new DemoLocalizedText(
                    "PasswordInput adds an accessible, optional reveal control without changing EditForm integration.",
                    "PasswordInput 新增可访问且可选的明文显示控件，同时保持 EditForm 集成。"),
                new DemoLocalizedText(
                    "DataGrid can refresh providers explicitly, hide footer controls independently, and select the current page.",
                    "DataGrid 可显式刷新数据提供器、独立隐藏页脚控件，并选择当前页。"),
                new DemoLocalizedText(
                    "Modal navigation drawers now manage focus, Escape, background isolation, scroll locking, and responsive cleanup.",
                    "模态导航抽屉现在会管理焦点、Escape、背景隔离、滚动锁定和响应式清理。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("Forms and data workflows", "表单与数据工作流"),
                    [
                        new DemoLocalizedText(
                            "TextInput exposes Text, Email, and Search types with Change and Input update modes.",
                            "TextInput 提供 Text、Email 和 Search 类型，以及 Change 和 Input 更新模式。"),
                        new DemoLocalizedText(
                            "PasswordInput supports localized reveal and conceal labels while preserving controlled value flow.",
                            "PasswordInput 支持本地化的显示与隐藏标签，同时保留受控值流。"),
                        new DemoLocalizedText(
                            "DataGrid RefreshAsync, footer visibility options, and current-page select-all work with in-memory and provider-backed data.",
                            "DataGrid 的 RefreshAsync、页脚可见性选项和当前页全选同时适用于内存数据与提供器数据。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Navigation and delivery", "导航与交付"),
                    [
                        new DemoLocalizedText(
                            "Modal drawers coordinate focus restoration, background inertness, scroll locking, and viewport changes without leaking browser state.",
                            "模态抽屉会协调焦点恢复、背景 inert 状态、滚动锁定和视口变化，且不会泄漏浏览器状态。"),
                        new DemoLocalizedText(
                            "Package consumers verify the new capabilities in Server, WebAssembly, Auto, trimming, and WebAssembly AOT paths.",
                            "包使用方测试会在 Server、WebAssembly、Auto、裁剪和 WebAssembly AOT 路径中验证这些新能力。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes to previously published APIs; this feature release adds public components, options, and methods.",
                "对既有已发布 API 没有破坏性变更；本功能版本新增了公共组件、选项和方法。")),
        new(
            "v0.2.3",
            "0.2.3",
            new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Patch release", "补丁版本"),
            new DemoLocalizedText(
                "Bilingual Demo and shared landing page",
                "双语 Demo 与共享首页"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.2.3 improves Demo discovery with Chinese-first bilingual navigation, persistent theming, and a shared release-aware landing page while preserving the runtime API surface.",
                "Bzs.Blazor 0.2.3 通过中文优先的双语导航、持久化主题和共享版本首页改善 Demo 探索体验，同时保持运行时 API 不变。"),
            [
                new DemoLocalizedText(
                    "A Chinese-first catalog shell keeps the selected culture across Demo navigation.",
                    "中文优先的目录外壳会在 Demo 导航期间保留所选语言。"),
                new DemoLocalizedText(
                    "Light, dark, and system theme choices persist across the Demo hosts.",
                    "浅色、深色和跟随系统主题会在各 Demo 宿主间持久保存。"),
                new DemoLocalizedText(
                    "A shared responsive landing page presents installation, component previews, and release details.",
                    "共享响应式首页集中展示安装方式、组件预览和版本详情。"),
                new DemoLocalizedText(
                    "The static language fallback now matches the interactive segmented control before interactivity is ready.",
                    "静态语言 fallback 在交互就绪前也与分段选择控件保持一致。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("Demo experience", "Demo 体验"),
                    [
                        new DemoLocalizedText(
                            "The catalog shell, navigation, release surfaces, and landing content are shared across hosted and standalone Demo modes.",
                            "目录外壳、导航、版本界面和首页内容在托管与独立 Demo 模式间共享。"),
                        new DemoLocalizedText(
                            "Logo and favicon assets provide a consistent Bzs.Blazor identity across the Demo.",
                            "Logo 与 favicon 资源为整个 Demo 提供一致的 Bzs.Blazor 品牌识别。"),
                        new DemoLocalizedText(
                            "Theme selection is persisted while consumer applications remain responsible for their own theme policy.",
                            "Demo 会持久保存主题选择，而使用方应用仍负责自身的主题策略。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Reliability and delivery", "可靠性与交付"),
                    [
                        new DemoLocalizedText(
                            "Static SSR language links retain segmented styling with hover, focus, forced-colors, and reduced-motion behavior.",
                            "静态 SSR 语言链接保留分段样式，并覆盖悬停、焦点、强制颜色和减少动画模式。"),
                        new DemoLocalizedText(
                            "Browser coverage verifies the language fallback with JavaScript disabled.",
                            "浏览器测试会在禁用 JavaScript 时验证语言 fallback。"),
                        new DemoLocalizedText(
                            "DatePicker keyboard coverage now reflects standards-aligned month-end clamping.",
                            "DatePicker 键盘测试现在正确反映符合标准的月末截断行为。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes and no new runtime APIs; this release focuses on the Demo and release experience.",
                "没有破坏性变更，也没有新增运行时 API；本次发布聚焦 Demo 与版本体验。")),
        new(
            "v0.2.2",
            "0.2.2",
            new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
            new DemoLocalizedText("Patch release", "补丁版本"),
            new DemoLocalizedText(
                "Anchored overlay lifecycle hardening",
                "锚定浮层生命周期加固"),
            new DemoLocalizedText(
                "Bzs.Blazor 0.2.2 consolidates anchored overlay state behind a shared session and fixes concurrency, focus restoration, retry, and disposal behavior.",
                "Bzs.Blazor 0.2.2 通过共享会话统一锚定浮层状态，并修复并发、焦点恢复、重试和释放行为。"),
            [
                new DemoLocalizedText(
                    "Popover, Tooltip, Menu, ContextMenu, and Autocomplete share one internal overlay lifecycle.",
                    "Popover、Tooltip、Menu、ContextMenu 和 Autocomplete 共享同一套内部浮层生命周期。"),
                new DemoLocalizedText(
                    "Concurrent close requests coalesce into one controlled transition.",
                    "并发关闭请求会合并为一次受控状态转换。"),
                new DemoLocalizedText(
                    "Focus restoration survives controlled renders and transient JavaScript retries.",
                    "焦点恢复可跨越受控重渲染和瞬时 JavaScript 重试。"),
                new DemoLocalizedText(
                    "Disposal and rejected context-menu invocations no longer leak stale overlay state.",
                    "释放流程和被拒绝的上下文菜单调用不再遗留过期浮层状态。"),
            ],
            [
                new DemoReleaseSection(
                    new DemoLocalizedText("Concurrency and focus", "并发与焦点"),
                    [
                        new DemoLocalizedText(
                            "Latest-state-wins synchronization serializes initialization, retries, and close callbacks.",
                            "最新状态优先的同步机制会串行处理初始化、重试和关闭回调。"),
                        new DemoLocalizedText(
                            "Accepted closes preserve focus intent even when a controlled parent replays an intermediate open render.",
                            "即使受控父组件重放中间打开状态，已接受的关闭操作仍会保留焦点意图。"),
                    ]),
                new DemoReleaseSection(
                    new DemoLocalizedText("Compatibility", "兼容性"),
                    [
                        new DemoLocalizedText(
                            "Razor markup, CSS, JavaScript module paths, export names, and public APIs remain unchanged.",
                            "Razor 标记、CSS、JavaScript 模块路径、导出名称和公共 API 均保持不变。"),
                    ]),
            ],
            new DemoLocalizedText(
                "No breaking changes to previously published APIs or supported render modes.",
                "对已发布 API 和支持的渲染模式没有破坏性变更。")),
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
