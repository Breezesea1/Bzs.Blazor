# Bzs.Blazor 0.2.0 Release Candidate Handoff

> Finalization note: the candidate described below was hardened and finalized
> as patch version `0.2.1`. Use `docs/releases/0.2.1.md` and tag `v0.2.1` for
> publication; the remaining `0.2.0` references document the original snapshot.

- 生成时间：2026-08-09 13:20（UTC+08:00）
- 仓库：`D:\Coding\Bzs.Blazor`
- 分支：`main`
- 快照 HEAD：`2c446313c94ae00916f439fb1f55c62de0bd7747`
- 当前版本：`0.2.0`
- 工作状态：实现和本地 release gate 已完成，全部改动尚未提交

## 一句话结论

MudBlazor/Radzen.Blazor 对标后的第一波 productivity 组件已经完成：锚定交互、菜单、导航、分页、状态组件、Autocomplete、FileUpload 和第一版 typed DataGrid 均已进入唯一 runtime package。共享 Productivity Demo、Static SSR、Server、WebAssembly、Auto、standalone WebAssembly、package-only consumer、七浏览器矩阵、trimming 和 WASM AOT 均已通过。当前没有已知代码阻塞；下一步只能在维护者明确授权后整理提交、push，并按发布流程创建严格的 `v0.2.0` tag。

## 接手后的第一步

1. 阅读本文件、`.scratch/bzs-blazor-next-components/spec.md` 和 Ticket 12：`.scratch/bzs-blazor-next-components/issues/12-complete-demo-and-release-gates.md`。
2. 执行 `git status --short`，确认没有本 handoff 之后新增的用户改动。
3. 若只是继续开发，保持当前工作树，不要提交、push 或创建 tag。
4. 若用户明确要求准备 release commit，先审查完整 diff 和所有 untracked source，再重新运行与新增改动相称的验证。
5. 若用户明确批准发布，先让同一 SHA 的 CI 完成，再按仓库发布约定创建 `v0.2.0` tag；不要提前发布或用测试 tag 探测 workflow。

完成标准：下一位代理能明确区分“已通过的本地 release candidate”和“仍需维护者授权的外部发布动作”，并且没有覆盖现有工作树中的任何用户改动。

## 已完成范围

### Runtime 组件

- Anchored interaction：`BzsPopover`、`BzsTooltip`、`BzsMenu`、`BzsMenuItem`、`BzsContextMenu`。
- Navigation：`BzsNavMenu`、`BzsNavItem`、`BzsBreadcrumbs`、`BzsPagination`。
- Status：`BzsSkeleton`、`BzsBadge`、`BzsChip`、`BzsAvatar`。
- Forms：provider-backed `BzsAutocomplete<TValue>` 和 EditForm-integrated `BzsFileUpload`。
- Data：typed `BzsDataGrid<TItem>`，包含 in-memory/provider data、sorting、typed filters、paging、page size、selection、loading、empty、retryable error、known/unknown totals 和 provider cancellation/stale-result suppression。

公共组件保持 `Bzs` 前缀和 `Bzs.Blazor` namespace。runtime package 没有新增第三方 UI dependency，也没有依赖 Demo 或 reference application。

### 架构与契约

- 锚定定位、碰撞、outside interaction 和生命周期安全 interop 收敛到内部 overlay module。
- Popover、Tooltip、Menu 和 ContextMenu 保持独立公共语义，不公开 DOM coordinates 或 browser adapter。
- Autocomplete 与 DataGrid 使用各自的 typed provider contract。
- DataGrid 主接口使用 typed columns/templates，没有把反射字符串路径作为主要模型。
- 所有 durable state 使用 `Open/OpenChanged`、`Value/ValueChanged` 等受控参数对。
- 新公共 API 已进入 `src/Bzs.Blazor/PublicAPI.Unshipped.txt`，中英文 runtime 文案已进入 resx。
- ADR：`docs/adr/0025-use-a-separate-anchored-overlay-module.md`、`docs/adr/0026-use-feature-specific-typed-data-providers.md`。

### Demo 与 render modes

共享页面位于：

```text
samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/Pages/Productivity.razor
```

同一页面由以下入口承载：

- Static SSR：`/productivity/static`
- Interactive Server：`/productivity/server`
- Interactive WebAssembly：`/productivity/webassembly`
- Interactive Auto：`/productivity/auto`
- standalone WebAssembly：`/productivity`

Productivity 页面包含全部新组件族的可执行工作流，不是 marketing specimen。`BzsNavItem` disclosure 和 removable `BzsChip` 已使用真实受控状态，浏览器测试会断言状态变化。

### GitHub Pages base path

Productivity 内部的 Overview、Productivity、Assigned、Waiting 和 Home breadcrumb 均通过 `DemoCulture.PreserveCulture` 生成 base-aware URL。standalone fixture 会把发布输出托管在 `/Bzs.Blazor/`，浏览器测试逐个点击这些链接并断言 URL 始终留在该 base path 下。

源 `index.html` 仍保留 `<base href="/" />`。真实 Pages 发布继续由 `scripts/prepare-github-pages.ps1` 改写 base tag、生成 `404.html` 和 `.nojekyll`；不要把 `/Bzs.Blazor/` 硬编码回源文件。

### 浏览器与 package error gates

- `BrowserGatePageTest` 收集 console error、`PageError` 和 structured `RequestFailed`。
- Server/WASM/Auto Productivity 和 standalone Productivity 工作流结尾都会调用 `AssertNoUnexpectedBrowserErrors`。
- 浏览器 gate 只忽略显式 abort：`net::ERR_ABORTED`、`NS_BINDING_ABORTED` 或 exact `ERR_ABORTED`。
- package consumer 同时拒绝 console error、HTTP `>=400` 和 transport-level request failure。
- package consumer 只允许 exact `net::ERR_ABORTED`，并有 5 个分类 theory case 固化该规则。

### 文档与延期范围

`docs/releases/0.2.0.md` 已列出新公共契约，并明确以下延期范围：

- DataGrid editing、grouping、hierarchy、drag/drop、frozen columns、virtualization、export 和 persisted preferences。
- Nested submenus、Autocomplete free text、upload transport/storage/security/retry/resume。
- TreeView、Accordion、Stepper、time/range pickers、Slider、Rating、Splitter、Timeline、DropZone。
- Charts、Scheduler、Gantt、Spreadsheet、rich text、maps、media、AI/chat。
- Public generic portal/positioning adapter、已有组件 replacement wrappers、第三方 UI runtime dependency。

## 最终验证证据

最终完整命令使用仓库本地 SDK：

```powershell
$repoDotnet = (Resolve-Path '.dotnet').Path
$env:DOTNET_ROOT = $repoDotnet
$env:PATH = "$repoDotnet;$env:PATH"
pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release
```

结果：

| Gate | 结果 |
| --- | --- |
| `dotnet format Bzs.Blazor.slnx --verify-no-changes --no-restore` | passed |
| Release build | 0 warnings / 0 errors |
| Unit/component | `353/353` |
| Browser suite | `73/73` |
| Accessibility | `12/12`，包含在 browser suite |
| Visual regression | `5/5`，未跳过 |
| Browser matrix | Chromium、mobile Chrome、Chrome、Edge、Firefox、WebKit、mobile Safari 全通过 |
| Package consumer source | `24/24` |
| Package consumer published | `24/24` |
| Trimming / WASM AOT | passed，无 `IL2xxx` 或 `IL3050` |
| `git diff --check` | passed；仅有现存 LF/CRLF 提示 |

产物预算：

| 产物 | 实测 / budget |
| --- | --- |
| `Bzs.Blazor.0.2.0.nupkg` | `259563 / 270336` bytes |
| `Bzs.Blazor.0.2.0.snupkg` | `154634 / 163840` bytes |
| AOT `_framework` | `37105493 / 41943040` bytes |

权威本地摘要：`artifacts/release/verification-summary.md`。

### SDK 注意事项

系统 `C:\Program Files\dotnet\dotnet.exe` 当前没有 `wasm-tools`。一次完整 gate 使用系统 SDK 时，已经通过 build、unit/browser、七浏览器矩阵、pack 和 source package consumer，随后在脚本的 workload precheck 停止。

仓库 `.dotnet\dotnet.exe` 安装了 `wasm-tools`。设置上面的 `DOTNET_ROOT` 和 `PATH` 后，完整 gate 在不跳过 visual、browser matrix 或 AOT 的情况下通过。后续代理不得把系统 SDK 的 workload precheck 误判为代码或 AOT 回归。

## 当前工作树

工作树是完整 0.2.0 milestone，尚未 stage、commit 或 push。它包含 Ticket 01-12 的 tracked modifications 和约 104 个 untracked files；保存本 handoff 后本文件是额外的 untracked file。

重要边界：

- 保留用户已有的 `.gitignore` 和 `global.json` 工作树状态；它们当前主要表现为 line-ending 差异。
- 不执行 `git reset --hard`、`git checkout --` 或其他会丢失工作树的操作。
- 不从 tracked diff 大小推断完整实现规模；大量新组件、测试、Demo route、ADR、roadmap 和 `.scratch` tickets 仍是 untracked。
- `artifacts/release` 和 TestResults 是验证产物，不应混入 release commit。
- 当前分支仍是 `main`，HEAD 仍是发布 `0.1.13` 的 `2c44631`；0.2.0 的所有实现均在未提交工作树中。

主要改动区域：

```text
src/Bzs.Blazor/Components/{Popover,Tooltip,Menu,Navigation,Pagination,Status,DataGrid}
src/Bzs.Blazor/Components/Form/BzsAutocomplete*
src/Bzs.Blazor/Components/Form/BzsFileUpload*
samples/Bzs.Blazor.Demo/**/Productivity*
tests/Bzs.Blazor.Tests/*{Autocomplete,FileUpload,Menu,Navigation,Pagination,Popover,Status,Tooltip,DataGrid}*
tests/Bzs.Blazor.BrowserTests/
tests/Bzs.Blazor.PackageConsumerTests/
scripts/package-consumer/
docs/releases/0.2.0.md
.scratch/bzs-blazor-next-components/
```

## 当前 Demo 进程

- URL：<http://127.0.0.1:5080/productivity/auto>
- PID：`101256`
- 监听：`127.0.0.1:5080`
- build configuration：Release
- ASP.NET Core environment：Development（launch settings）
- stdout：`artifacts/demo-5080.stdout.log`
- stderr：`artifacts/demo-5080.stderr.log`

已探测 `/productivity/auto`：HTTP `200`，响应包含 `data-testid="productivity-workbench"`。日志中的 HTTPS redirect port warning 不影响这个显式 HTTP 本地地址。

该进程只是当前 Windows 会话中的隐藏 `dotnet` 进程，不是 Windows service 或 scheduled task，也不具备重启持久性。PID 可能在环境变化后失效；接手时先用以下命令确认：

```powershell
Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue
```

## 剩余动作

代码范围内没有已知待修复项。外部动作保持未授权状态：

1. 维护者审查并明确批准 release candidate。
2. 获得明确要求后，整理并创建 release commit；提交前检查完整 staged diff。
3. push 后等待同一 SHA 的 CI 和 Pages workflow，核对真实线上结果。
4. 只有维护者明确批准发布且同 SHA gate 成功后，才创建严格 tag `v0.2.0`。
5. NuGet 发布后再核对 package、symbols、Source Link、Pages 和 release notes；这些不是当前本地 handoff 已完成的外部事实。

如果 handoff 之后任何 runtime、test、script、baseline 或 package metadata 改变，当前完整 gate 证据即不再覆盖新状态，必须重新运行相应门禁；涉及公共 API、package、AOT 或跨 render-mode 行为时，重新运行完整 release gate。

## 关键入口

- 路线与验收：`.scratch/bzs-blazor-next-components/spec.md`
- Ticket 12 证据：`.scratch/bzs-blazor-next-components/issues/12-complete-demo-and-release-gates.md`
- Roadmap：`docs/plans/2026-08-08-bzs-blazor-component-roadmap.md`
- Release notes：`docs/releases/0.2.0.md`
- Productivity Demo：`samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/Pages/Productivity.razor`
- Browser error gate：`tests/Bzs.Blazor.BrowserTests/BrowserGatePageTest.cs`
- Base-path fixture：`tests/Bzs.Blazor.BrowserTests/StandaloneWebAssemblyFixture.cs`
- Package consumer gate：`tests/Bzs.Blazor.PackageConsumerTests/PackageConsumerSmokeTests.cs`
- Release gate：`scripts/verify-release.ps1`
- Pages preparation：`scripts/prepare-github-pages.ps1`
- Final local evidence：`artifacts/release/verification-summary.md`
