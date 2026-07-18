# MudBlazor 与 Radzen Blazor 调研

查询日期：2026-07-17（UTC+08:00）。本稿只引用厂商/项目官方文档、官方 GitHub 仓库、NuGet 和官方价格页。版本、GitHub 指标和价格会变化；除非另有说明，下面的“事实”均是该日的快照，“判断”是面向本项目的技术选型建议，而非厂商承诺。

当前仓库尚无应用代码或项目文件，因此本文只能完成通用选型调研，不能判断现有页面的迁移成本、CSS 冲突或与既有设计系统的契合度。

## 结论摘要

**判断：**两者的核心组件库均为 MIT，可在商业应用中使用、修改和分发；不能把“MIT”理解为有 SLA 或厂商赔偿。若产品希望统一的 Material Design 视觉、较少依赖 JavaScript、社区式协作和源码可深度定制，优先试用 MudBlazor。若后台业务屏幕以数据表、排程、图表、表单及 Material/Fluent 多主题为中心，并且团队愿意为设计器、模板和确定响应时间购买订阅，优先试用 Radzen；但免费 `Radzen.Blazor` 组件包本身不等于付费 Studio/VS 工具。

无论选哪一个，先以本项目真实的 Blazor Web App（含所需交互渲染模式）完成两张代表性页面、一个远端 DataGrid 和一项无障碍键盘测试，再定案。不要在同一应用的同一设计系统中长期混用两套 CSS 变量、overlay/provider 和图标体系。

## 快照对比

| 维度 | MudBlazor | Radzen Blazor |
| --- | --- | --- |
| 定位（事实） | Material Design Blazor 组件框架，官方 README 强调易用、可扩展、几乎不需要 JavaScript。[M1](#M1) | 原生 Blazor UI 组件库；当前 README 声明 145+ 组件，含 DataGrid、Scheduler、Gantt、Spreadsheet、Charts 等。[R1](#R1) |
| 当前稳定版（事实） | NuGet 最新稳定版 `9.7.0`；官方发行页发布时间 2026-07-09。[M2](#M2) [M3](#M3) | NuGet 最新稳定版 `11.1.5`；官方发行页发布时间 2026-07-15。[R2](#R2) [R3](#R3) |
| 目标 .NET（事实） | 官方支持矩阵声明 MudBlazor 9.x 对 `.NET 8`、`.NET 9`、`.NET 10` 为 Full Support；9.7.0 NuGet 包含对应依赖组。[M1](#M1) [M4](#M4) | `Radzen.Blazor.csproj` 明确为 `net8.0;net9.0;net10.0`；README 明确支持 Blazor Server、WebAssembly 与 .NET MAUI Blazor Hybrid。[R1](#R1) |
| 许可证（事实） | MIT。[M5](#M5) | MIT。[R4](#R4) |
| 视觉语言（事实） | 一套以 Material Design 为中心的主题/调色板体系。[M1](#M1) [M6](#M6) | Material、Material 3、Fluent 等主题及明暗变体；组件项目也明确宣称 Material 与 Fluent UI。[R1](#R1) [R5](#R5) |
| 商业产品边界（事实） | 核心库按 MIT 发布；官方 README 指向文档、GitHub Discussions 与 Discord。未在本次一手来源中找到 MudBlazor 官方“按席位商业支持/SLA”价格表，不能假定存在。[M1](#M1) | `Radzen.Blazor` 开源组件包按 MIT 发布；Radzen 另售 Studio/VS、高级主题、模板、UI blocks、AI 支持和邮件支持。[R4](#R4) [R6](#R6) |

## 逐项事实

### 1. 组件范围与开发模型

**MudBlazor（事实）**

- 官方组件索引覆盖布局/导航、按钮与输入、选择器、对话框与通知、表单、表格、DataGrid、图表、日期时间、文件上传等类别；应以组件索引而非营销数量作为需求逐项核验依据。[M7](#M7)
- `MudDataGrid` 和 `MudChart` 都有官方组件文档与示例。[M8](#M8) [M9](#M9)
- 源码 README 声明组件以 C# 编写，“except where absolutely necessary” 才使用 JavaScript；这是架构取向而不是“零 JS”的保证。[M1](#M1)
- 表单以 `MudForm`、验证器和输入组件为核心；官方验证文档包含 Data Annotations、FluentValidation 等路径。[M10](#M10)

**Radzen Blazor（事实）**

- 当前 README 列出 145+ 原生组件，除常用表单/布局外还包括 DataGrid、PivotDataGrid、Scheduler、Gantt、Timeline、Spreadsheet、Charts、Heatmap、Treemap、Sankey 与 Gauge 等。[R1](#R1)
- 官方演示目录提供 DataGrid、Chart、Scheduler、Editor、Upload、DataFilter、DropZone 等可交互示例；具体能力以各组件 API/示例为准。[R7](#R7)
- 表单以 `RadzenTemplateForm`、`RadzenDataAnnotationValidator` 和 Required/Length/NumericRange 等验证器为主；官方测试项目包含相应测试。异步、跨字段及服务器错误回填仍需以 PoC 验证。[R8](#R8)
- 官方 DataGrid 文档覆盖排序、筛选、分页、虚拟化、编辑、层级与按需加载等示例；图表页覆盖多种 series 与配置。[R9](#R9) [R10](#R10)

**判断：**两套库的“普通业务控件”重合很高。真正应比较的是：远程数据的排序/过滤协议、主从/分组/虚拟滚动、单元格编辑错误呈现、图表导出需求、排程是否刚需，以及设计语言是否需要 Fluent。若 Scheduler 是核心需求，Radzen 将有更少的第三方补洞风险；若 Material 是唯一视觉目标，MudBlazor 的体系更单一。

### 2. 主题、可访问性与本地化

**主题（事实）**

- MudBlazor 安装/自定义文档要求在应用根放置 `MudThemeProvider`，并提供 `MudTheme` 配置。[M6](#M6)
- Radzen 通过 `RadzenTheme` 和 CSS 变量集成主题，并提供 `ThemeService` 进行运行时主题切换。[R5](#R5) [R11](#R11)

**可访问性（事实）**

- MudBlazor 提供 accessibility 文档、组件级 ARIA 参数及键盘/无障碍测试；本次未找到其对全组件库作出的 WCAG 等级或认证承诺。[M11](#M11)
- Radzen 官方 accessibility 文档说明其组件可帮助满足 WAI-ARIA、WCAG 2.2、Section 508 与键盘兼容要求，并提供 WCAG AA 主题配色及 VPAT/ACR；官方也明确警示，组件配置和业务页面结构仍可能造成障碍，**使用组件本身不等于最终产品自动合规**。[R12](#R12)

**本地化（事实）**

- MudBlazor 可通过自定义 `MudLocalizer`、`IStringLocalizer`/ResX、字典或 `ILocalizationInterceptor` 本地化；众包翻译另由 `MudBlazor.Translations` 包提供。官方同时注明仍有部分组件尚不支持本地化。[M12](#M12)
- Radzen 支持基于资源的本地化，可覆盖用户可见标签、tooltip、ARIA label、过滤操作和分页文字；官方提供 `ILocalizer`、satellite `.resx` 与组件参数覆盖等路径。[R13](#R13)

**判断：**两者都需要应用层自己决定资源所有权、语言切换持久化、日期/数字文化和服务器端数据的排序文化。可访问性应纳入验收：键盘 Tab/Shift+Tab、Esc、焦点陷阱、屏幕阅读器名称、缩放 200%、高对比度和表格操作；不能因组件库声明或主题名而免测。

### 3. SSR、Blazor Web App、WASM 与 Server

**事实：**MudBlazor 9.x 支持 .NET 8/9/10，但官方明确写明 **不支持纯静态渲染**；其 providers 必须与使用它们的组件处在同一 interactive render mode。[M1](#M1) [M13](#M13) Radzen README 明确支持 Blazor Server、Blazor WebAssembly 与 MAUI Blazor Hybrid；当前入门文档要求交互功能使用 `InteractiveServer`、`InteractiveWebAssembly` 或 `InteractiveAuto`，但本次没有找到其对“纯静态 SSR 可完整使用”的明确承诺。[R1](#R1) [R14](#R14)

**判断：**应把“支持”定义为目标应用的完整渲染组合是否通过：静态 SSR 首屏、Interactive Server、Interactive WebAssembly（若使用）及 Auto（若使用）。特别验证 dialogs/popovers/snackbars、浏览器 API 依赖、预渲染后二次加载、断线重连、导航增强和流式渲染。不要仅凭 NuGet TFM 或官网示例宣布已兼容 Blazor Web App。

### 4. 安装与最小集成

**MudBlazor（事实）**：安装 `MudBlazor` NuGet 包；在 DI 注册 `AddMudServices()`；在布局/根组件加入主题、Popover、Dialog、Snackbar provider，并加载 `_content/MudBlazor/MudBlazor.min.css` 与所需 JS。官方安装页是版本升级时的唯一准绳。[M2](#M2) [M13](#M13)

**Radzen（事实）**：安装 `Radzen.Blazor`；在 `_Imports.razor` 引入命名空间；注册 `AddRadzenComponents()`；在 `App.razor` 加入 `RadzenTheme` 与官方 JS。Dialog、Notification、ContextMenu、Tooltip 通过布局中的 `RadzenComponents` 承载，并设置与页面一致的交互 render mode。[R2](#R2) [R14](#R14)

**判断：**先制作“最小可替换壳”：仅用一套 provider，集中设置主题和 icon/font，禁止业务页面直接复制 CDN/静态资源标签。这样试用结束时可以整包撤换，不会把全局 CSS 与服务注册扩散到每个页面。

### 5. 测试、维护活跃度与社区

**事实（查询日快照）**

- MudBlazor 仓库公开约 10.5k stars、1.66k forks、800 open issues，默认分支 `dev`，最近推送为 2026-07-16；其测试项目使用 NUnit、Microsoft.Testing、coverlet，并由 README 链接 GitHub Actions、Codecov、Discussions 和 Discord。[M14](#M14) [M15](#M15) [M1](#M1)
- Radzen Blazor 仓库公开约 4.3k stars、952 forks、28 open issues，默认分支 `master`，最近推送为 2026-07-17；其测试项目使用 bUnit、xUnit、coverlet。[R15](#R15) [R16](#R16)

**判断：**star、open issue 数和“有测试项目”都不是质量或响应时间的充分证据。MudBlazor 的公开讨论/Discord 生态更显眼，Radzen 的付费支持边界更明确。应抽查最近 3 个与本项目关键组件相关的 issue/PR 的首响、修复和回归记录，并在 PoC 里锁定包版本及跑自有 bUnit/Playwright 回归。

### 6. 商业支持与价格边界

**事实：**Radzen 官方价格页在查询日列示 Community 免费，Pro 为 **USD 799/开发者/年**，Team 为 **USD 1,999/3 开发者/年**；页面说明 Pro 邮件支持目标为 24 小时、Team 为 16 小时，并有 15 天试用。价格页也把高级主题、模板、UI blocks、Studio/Visual Studio 工具与组件库区分开来。税费、续订、地域货币、许可范围以购买时条款为准。[R6](#R6)

**事实：**MudBlazor 核心库的官方仓库/许可证明确为 MIT；本次限定的一手来源中没有核验到与 Radzen 等价的官方付费 SLA 价格产品，故采购计划不能把社区渠道当成合同支持。[M1](#M1) [M5](#M5)

**判断：**若投标、生产故障响应或合规要求需要可购买且可写入合同的支持责任，Radzen 的产品边界更易采购，但必须由采购确认“支持的是哪个产品、组件库问题是否在覆盖范围、响应时间是否业务时段/工作日、是否含修复承诺”。若不需要该边界，MIT 核心库成本不是主要差异，团队学习成本与屏幕匹配度才是。

## 优缺点（判断，基于上述事实）

| 方案 | 主要优势 | 主要代价/风险 |
| --- | --- | --- |
| MudBlazor | Material 设计一致性强；C# 优先、JS 依赖较少；MIT 且公开社区/测试信号较强；适合以应用 UI 和定制主题为主的团队。 | 视觉路线较集中在 Material；无本次可核验的付费 SLA；复杂数据/排程能力必须以实际需求而非组件清单确认；公开 issue 体量较大，需自测升级。 |
| Radzen Blazor | 组件清单直接覆盖 DataGrid、Scheduler、Charts；Material 与 Fluent 选择更宽；同一厂商有设计器/模板/支持订阅，后台 CRUD 场景可少造工具。 | 免费 MIT 组件与付费 Studio/支持是不同边界，成本与续订需单独预算；引入设计器生成代码前须审查可维护性；更丰富的主题和全局样式意味着设计治理不可省略。 |

## 推荐与决策门槛

**推荐（条件式判断）：**

1. 默认从 **MudBlazor PoC** 开始，前提是产品视觉可接受 Material 且不把商业 SLA、可视化设计器或 Scheduler 作为硬性需求。
2. 若核心页面是高密度运营后台，明确需要 Scheduler/Fluent，或采购已要求厂商响应窗口，优先做 **Radzen PoC** 并让采购同时核验订阅条款。
3. 不建议仅因“组件数量更多”或“GitHub stars 更多”定案。两周内用同一验收清单和同一数据 API 对比后，选择总定制代码更少、键盘/SSR 缺陷更少的一方。

## PoC、迁移与退出清单

### 试用（两者均执行）

- [ ] 建一个独立样例项目或 feature branch，固定 `MudBlazor 9.7.0` / `Radzen.Blazor 11.1.5`，记录 .NET SDK、浏览器和运行模式。
- [ ] 实现三类真实屏幕：编辑表单（含服务端错误）、远端 DataGrid（分页/排序/筛选/虚拟滚动/编辑）、含 dialog/popover/notification 的工作流；Radzen 候选另实现 Scheduler。
- [ ] 以目标的 Blazor Web App SSR + 交互渲染模式运行，验证预渲染、刷新、深链、断线重连、增强导航和首屏加载；若交付 WASM/Server，也分别跑。
- [ ] 使用真实 i18n 资源测试中文、英文及日期/数字/时区；验证动态切换文化后 popup、验证文本、表头与 aria label。
- [ ] 自动和人工做无障碍验收：axe/Playwright 扫描只是起点，必须实测键盘、焦点恢复、屏幕阅读器、缩放和高对比度。
- [ ] 量化：首屏、DataGrid 交互、包体/静态资源、样式覆盖行数、每屏自定义代码、bUnit/端到端测试稳定性、升级一次后的破坏量。

### 迁移/退出（在决定前预留）

- [ ] 业务页面不要直接依赖库特有的主题变量、服务或 CSS class；封装 `AppDialog`、`AppToast`、`AppDataGrid` 的薄适配层，领域模型和查询参数保持库无关。
- [ ] 将 DataGrid 的排序、过滤、分页 DTO 定义为后端契约，避免把库的事件/表达式格式透传到 API。
- [ ] 图标、色板、spacing、断点、验证消息统一从应用设计 token/资源取得；保留基础表单与表格的截图回归。
- [ ] 切换库时先并行迁移 provider/主题和低风险控件，再迁移表单，最后迁移 DataGrid/图表/Scheduler；每一步保留可运行基线。不要在全局加载两套样式后一次性替换。

## Sources

### MudBlazor

- <a id="M1"></a>**[M1] 官方 GitHub README**：https://github.com/MudBlazor/MudBlazor
- <a id="M2"></a>**[M2] NuGet 包页**：https://www.nuget.org/packages/MudBlazor/9.7.0
- <a id="M3"></a>**[M3] 官方发行 `v9.7.0`**：https://github.com/MudBlazor/MudBlazor/releases/tag/v9.7.0
- <a id="M4"></a>**[M4] MudBlazor 9.7.0 NuGet 元数据**：https://api.nuget.org/v3-flatcontainer/mudblazor/9.7.0/mudblazor.nuspec
- <a id="M5"></a>**[M5] MIT LICENSE（官方源码）**：https://github.com/MudBlazor/MudBlazor/blob/dev/LICENSE
- <a id="M6"></a>**[M6] 官方主题自定义文档**：https://mudblazor.com/customization/overview
- <a id="M7"></a>**[M7] 官方组件索引**：https://mudblazor.com/components
- <a id="M8"></a>**[M8] 官方 DataGrid 文档**：https://mudblazor.com/components/datagrid
- <a id="M9"></a>**[M9] 官方 Chart 文档**：https://mudblazor.com/components/chart
- <a id="M10"></a>**[M10] 官方表单验证文档**：https://mudblazor.com/components/form#validation
- <a id="M11"></a>**[M11] 官方 accessibility 文档**：https://mudblazor.com/features/accessibility
- <a id="M12"></a>**[M12] 官方本地化文档**：https://mudblazor.com/features/localization
- <a id="M13"></a>**[M13] 官方安装文档**：https://mudblazor.com/getting-started/installation
- <a id="M14"></a>**[M14] GitHub 仓库 API（活跃度快照）**：https://api.github.com/repos/MudBlazor/MudBlazor
- <a id="M15"></a>**[M15] 官方测试项目**：https://github.com/MudBlazor/MudBlazor/blob/dev/src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj

### Radzen

- <a id="R1"></a>**[R1] 官方 README 与 `Radzen.Blazor.csproj`**：https://github.com/radzenhq/radzen-blazor/blob/master/README.md；https://github.com/radzenhq/radzen-blazor/blob/master/Radzen.Blazor/Radzen.Blazor.csproj
- <a id="R2"></a>**[R2] NuGet 包页**：https://www.nuget.org/packages/Radzen.Blazor/11.1.5
- <a id="R3"></a>**[R3] 官方发行 `v11.1.5`**：https://github.com/radzenhq/radzen-blazor/releases/tag/v11.1.5
- <a id="R4"></a>**[R4] MIT LICENSE（官方源码）**：https://github.com/radzenhq/radzen-blazor/blob/master/LICENSE
- <a id="R5"></a>**[R5] 官方主题文档**：https://blazor.radzen.com/themes
- <a id="R6"></a>**[R6] Radzen 官方价格页**：https://www.radzen.com/pricing
- <a id="R7"></a>**[R7] 官方组件演示目录**：https://blazor.radzen.com
- <a id="R8"></a>**[R8] 官方测试项目（验证器测试）**：https://github.com/radzenhq/radzen-blazor/tree/master/Radzen.Blazor.Tests
- <a id="R9"></a>**[R9] 官方 DataGrid 文档**：https://blazor.radzen.com/datagrid
- <a id="R10"></a>**[R10] 官方 Charts 文档**：https://blazor.radzen.com/charts
- <a id="R11"></a>**[R11] 官方 ThemeService 文档**：https://blazor.radzen.com/theme-service
- <a id="R12"></a>**[R12] 官方 accessibility 文档**：https://blazor.radzen.com/accessibility
- <a id="R13"></a>**[R13] 官方本地化文档**：https://blazor.radzen.com/localization
- <a id="R14"></a>**[R14] 官方入门安装文档**：https://blazor.radzen.com/get-started
- <a id="R15"></a>**[R15] GitHub 仓库 API（活跃度快照）**：https://api.github.com/repos/radzenhq/radzen-blazor
- <a id="R16"></a>**[R16] 官方测试项目**：https://github.com/radzenhq/radzen-blazor/blob/master/Radzen.Blazor.Tests/Radzen.Blazor.Tests.csproj
