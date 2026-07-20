# Bzs.Blazor v0.1.0 发布交接

- 生成时间：2026-07-19（UTC+08:00）
- 最后更新：2026-07-20（UTC+08:00），发布后加固已合入 `master` 并通过 CI
- 仓库：`git@github.com:Breezesea1/Bzs.Blazor.git`
- NuGet：<https://www.nuget.org/packages/Bzs.Blazor/0.1.0>

本文是 `Bzs.Blazor 0.1.0` 设计、实现、验证、CI/CD 和 NuGet 首发工作的时间点交接。接手时先复核外部状态；不要把本文中的时间点证据自动解释为未来状态。

## 当前结论

`Bzs.Blazor 0.1.0` 已发布到 NuGet.org，并已通过常规 NuGet v3 API 索引。首发使用 GitHub Actions OIDC Trusted Publishing，没有使用长期 NuGet API key。

发布源码固定在 annotated tag `v0.1.0`：

| 项目 | 当前值 |
| --- | --- |
| 加固分支 | `master` |
| 加固实现 commit | `f091881d72e7ece40bff66751e959d59612879df` |
| 加固 CI | run `29717988747`, success |
| `v0.1.0` tag object | `dc393f99be0f9d127bec046ac3cf53f4a9bb36d3` |
| `v0.1.0` peeled commit | `e737f182fc99b487958af092b29c764a9ee75280` |
| NuGet package | `Bzs.Blazor 0.1.0`, listed |
| NuGet published time | `2026-07-19T12:42:09.003Z` |

加固实现 commit `f091881` 比发布 tag 多两个提交：发布结果文档 `c31d912` 和发布控制加固 `f091881`。这是预期状态；本交接的后续状态同步可能再产生纯文档提交，接手时用 `git log` 复核最新 `master`。不要移动、删除或覆盖 `v0.1.0`，也不要尝试重新发布同版本主包。

发布授权 ADR/文档同步、Node 24 action pins，以及 package static-assets/integrity 校验已提交并 push 为 `f091881`。针对该精确 SHA 的 CI run `29717988747` 已在 Ubuntu 和 Windows hosted runners 上成功；没有创建或 push 新 tag，也没有触发 Publish NuGet workflow。

仓库当前没有 GitHub Release 对象；只有 Git tag、仓库内 release notes 和 NuGet.org package。`gh release view v0.1.0` 返回 `release not found`。

## 产品边界

`Bzs.Blazor` 是个人维护的通用 Blazor Razor Class Library，不是 CoreApi 包，也不是 MudBlazor 或 Radzen.Blazor 的替代项目。

- 目标框架是 `.NET 10`。
- 核心包没有第三方 UI runtime dependency。
- 支持 Interactive Server、Interactive WebAssembly 和 Interactive Auto。
- 被动组件在 static SSR 下保留有意义的 HTML；命令式交互需要 interactive runtime。
- 消费应用选择 render mode，并负责主题偏好持久化。
- 公共组件使用 `Bzs` 前缀，公共命名空间统一为 `Bzs.Blazor`。
- 源码按组件概念组织，文件夹名不泄漏到公共命名空间。
- CoreApi/CoreApi.Client 只作为行为和 UI 参考，不得成为 runtime、domain 或 project dependency。
- 视觉语言是克制的 neumorphism：语义 surface token、独立 light/dark 主题、紧凑生产力界面，不做全局重阴影或第三方 DOM 覆盖。

主要架构入口：

- `CONTEXT.md`
- `docs/specs/2026-07-18-bzs-blazor-v0.1.md`
- `docs/adr/`
- `src/Bzs.Blazor/`
- `src/Bzs.Blazor/wwwroot/bzs.blazor.css`

## v0.1.0 交付面

首包包含以下公共组件和服务：

- Foundation：`BzsThemeProvider`、`BzsSurface`、`BzsIcon`、`BzsButton`
- Forms：`BzsField`、`BzsTextInput`、`BzsTextArea`、`BzsNumberInput<TValue>`、`BzsDateInput<TValue>`、`BzsCheckbox`、`BzsSelect<TValue>`
- Feedback：`BzsMessage`、`BzsProgress`、`BzsToast`、`IBzsToastService`
- Overlay：`BzsDialog`、`IBzsDialogService`、`BzsDrawer`、`BzsOverlayHost`
- Navigation：`BzsTabs`、`BzsTabItem`

DataGrid、Tree、Scheduler、Charts、MultiSelect、ContextMenu、Popover、Sidebar、upload、media viewer、Radzen adapter 和业务复合组件不属于 `0.1.0`。

消费者注册方式：

```csharp
builder.Services.AddBzsBlazor();
```

需要命令式 dialog/toast 的 interactive root 放置：

```razor
<BzsThemeProvider Mode="@mode" ModeChanged="@OnModeChanged">
    @Body
    <BzsOverlayHost />
</BzsThemeProvider>
```

安装命令：

```powershell
dotnet add package Bzs.Blazor --version 0.1.0
```

## CI/CD 设计

CI 采用 Linux 主门禁、Windows 补充门禁：

```text
push/PR to master
├── Ubuntu release gate
│   ├── .NET SDK 10.0.302 + wasm-tools
│   ├── unit/component tests
│   ├── Chromium/Firefox/WebKit browser tests
│   ├── pack + package inspection
│   ├── temporary package consumer
│   ├── trimming
│   └── WASM AOT
└── Windows branded-browser gate
    ├── visual regression baselines
    └── Chrome + Edge
```

发布 workflow 由 `v*.*.*` tag 触发，并执行：

1. 严格解析带 `v` 前缀的 SemVer tag。
2. 要求 tag version 与 `src/Bzs.Blazor/Bzs.Blazor.csproj` 中唯一的 `Version` 完全一致。
3. Ubuntu 从 tagged source 重跑完整 release gate，生成 `.nupkg` 和 `.snupkg`。
4. Windows 从同一 tag 跑 visual baseline、Chrome 和 Edge。
5. `publish` job 必须等待两端成功，只下载已验证 artifact。
6. `nuget-production` environment 进入发布边界。
7. `NuGet/login` 使用 GitHub OIDC 换取短期 key。
8. 主包用 `--no-symbols` 发布，随后显式发布 `.snupkg`。
9. 受控 tag push 是最终人工发布授权；不增加 environment required reviewer。

关键文件：

- `.github/workflows/ci.yml`
- `.github/workflows/publish-nuget.yml`
- `scripts/verify-release.ps1`
- `scripts/verify-visual-regression.ps1`
- `tests/Bzs.Blazor.BrowserTests/run-browser-matrix.ps1`

## MudBlazor/Radzen 参考结论

没有直接复制 MudBlazor 或 Radzen 的 workflow。查询日的上游方案与本仓库的差异是：

- MudBlazor/Radzen 正式发布仍使用长期 NuGet API key；本仓库保留 OIDC 短期凭证。
- 上游主要是 Ubuntu unit/component test；本仓库保留 Linux 三浏览器、Windows Chrome/Edge、visual baseline、trim 和 AOT。
- MudBlazor tag 校验较宽松，Radzen 使用人工 dispatch；本仓库保留严格 tag/project version 一致性。
- 本仓库所有第三方 actions 固定完整 commit SHA，不改为浮动 major tag。
- 不采用 Radzen 的 `--skip-duplicate`，避免正式发布 rerun 掩盖重复版本问题。

上游调研提示的 package static-assets/integrity 加固现已完成：本仓库会枚举包内 CSS/JS，并将实际 ZIP entry 字节与临时消费者 build/publish endpoint manifest 中声明的 SHA-256 integrity 逐项核对。验证证据见“发布后加固验证”。

完整对比见 `docs/mudblazor-radzen-cicd-research-2026-07-19.md`。

## Trusted Publishing

成功的首发证明以下链路在发布时可用：

- GitHub repository/workflow/environment 与 NuGet Trusted Publishing policy 能匹配。
- `publish` job 的 `id-token: write` 足以请求 OIDC token。
- pinned `NuGet/login` 能换取短期 `NUGET_API_KEY`。
- 同一短期 key 能完成主包和 symbols 包发布。

GitHub environment API 的当前结果是：`nuget-production` 只有 `branch_policy`，允许 pattern 为 tag `v*.*.*`，没有 `required_reviewers`，并且 `can_admins_bypass=true`。首发中 verify job 完成约 3 秒后 publish job 自动开始，也证明当前没有人工审批停止点。

最终决策是：受控 push 一个满足 pattern、严格 SemVer 和项目版本校验的 tag，本身就是 NuGet 发布的最终人工授权；不为 `nuget-production` 增加 required reviewer。两端门禁成功后会自动进入 OIDC 和 NuGet push，接手人不能假定 GitHub 会再等待确认。

这项决策依赖 tag 创建/更新权限确实受控。仓库/API 尚未证明私有仓库实际生效的 tag ruleset；当前 workflow 也没有强制证明 tag peeled commit 等于当前 `origin/master`，或同一 SHA 的 branch CI 已成功。因此发布前必须实时复核 ruleset，并由发布人完成这两项 SHA 检查，直到它们被独立自动化且验证有效。

不要恢复长期 `NUGET_API_KEY` secret 作为静默 fallback。OIDC 失败应显式失败并调查 policy/environment mismatch。

## 验证证据

### 发布源码 CI

[CI run 29686673611](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29686673611) 针对 `e737f18` 成功：

- Windows branded browsers：成功，约 `5m43s`
- Ubuntu release gate：成功，约 `11m30s`

### NuGet 发布

[Publish NuGet run 29687047745](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29687047745) 针对 tag `v0.1.0` / commit `e737f18` 成功：

- Verify release package：成功，`13m13s`
- Windows branded browsers：成功，`4m43s`
- OIDC login：成功
- `.nupkg` push：成功
- `.snupkg` push：成功
- Publish job：约 `25s`

### 发布后 master CI

[CI run 29687905506](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29687905506) 针对 `c31d912` 成功：

- Windows branded browsers：`4m40s`
- Ubuntu release gate：`13m12s`

### 发布后加固 master CI

[CI run 29717988747](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29717988747) 针对 `f091881` 成功：

- Windows branded browsers：成功，`4m36s`
- Ubuntu release gate：成功，`13m39s`
- `actions/checkout v7`、`actions/setup-dotnet v6` 和 `actions/upload-artifact v7` 在两端 hosted runner 正常执行
- 完整 release gate：成功
- Package consumer：source 与 published 两轮各 `19/19`
- AOT：39 个 assembly，包括 `Bzs.Blazor.dll`
- 没有 `IL2xxx`、`IL3050` 或 Node 20 deprecation warning
- 同一 SHA 只触发 CI，没有触发 Publish NuGet workflow

### 测试与包消费

完整本地/CI release gate 的已记录结果：

- Unit/component：`107/107`
- Chromium browser：`36/36`
- Browser matrix：7 个目标通过
- Package consumer：`11/11`
- AOT：39 个 assembly，包括 `Bzs.Blazor.dll`
- Trim/AOT：无 `IL2xxx` 或 `IL3050`

发布后又创建了全新 Blazor Web App，并使用独立 `NUGET_PACKAGES`、禁用缓存、只指定 `https://api.nuget.org/v3/index.json`：

- 官方源成功还原 `Bzs.Blazor 0.1.0`
- 隔离缓存中的 nupkg 大小为 `114005` bytes
- package assets 类型为 `package`，没有本地 project reference
- `<BzsButton>` Razor 编译成功
- `_content/Bzs.Blazor/bzs.blazor.css` 静态资产解析成功
- Release build：0 warning、0 error

NuGet v3 flat-container、registration 和 search API 都已返回 `0.1.0`。

### 发布后加固验证

加固实现 commit `f091881` 在本地和 GitHub hosted runner 上均运行了完整 release gate：

- Unit/component：`107/107`
- Chromium browser（含视觉回归）：`36/36`
- Browser matrix：7 个目标通过
- Package consumer：source 与 published 两轮各 `19/19`
- Static-assets verifier：7 个独立正向/负向测试，并对实际 `.nupkg` 与消费者 build/publish endpoint manifest 逐项复算 SHA-256
- AOT：39 个 assembly，包括 `Bzs.Blazor.dll`
- Trim/AOT：无 `IL2xxx` 或 `IL3050`

本地证据由 CI run `29717988747` 的 Ubuntu/Windows hosted runner 结果补强；后续依赖升级仍需为对应新 commit 重新取得两端 CI 证据。

## 证据优先级与过时材料

以下材料中的部分文字已被实际发布覆盖，读取时不要倒退状态：

- `docs/specs/2026-07-18-bzs-blazor-v0.1.md` 已同步实际发布状态、`BzsTabItem` 公开组件名和 tag 授权决策。
- `.scratch/bzs-blazor-v0.1/issues/10-complete-aot-package-and-release-verification.md` 的 acceptance/out-of-scope 写着保持本地、不公开发布。这只描述当时 ticket 的权限边界，后续用户明确授权并完成了首发。第一条 comment 中的 `eng/verify-release.ps1` 旧路径也已失效，当前入口是 `scripts/verify-release.ps1`。
- `docs/releases/0.1.0.md` 已同步为 NuGet.org 可安装状态；如创建 GitHub Release，可使用该文件，但不要创建或移动 tag。
- `artifacts/release/verification-summary.md` 是被忽略、会被每次本地验证覆盖的临时汇总。当前工作区的最近一次完整 gate 显示 visual regression、browser matrix、WASM AOT 均未 skipped，但它仍不是 `v0.1.0` 发布门禁的持久汇总。发布事实优先使用 GitHub run、对应 job logs 和 NuGet.org。

`artifacts/` 已被 `.gitignore` 排除，不是持久证据仓库。GitHub workflow artifacts 也有 14 或 30 天 retention；需要长期保留的结论应进入 tracked 文档。

## 已知问题、已完成加固和后续工作

### 决策完成：受控 tag push 是最终发布授权

已明确接受“受控 tag push 就是最终发布授权”，不增加 `nuget-production` required reviewer。合法 tag 一旦 push，发布在两端门禁成功后自动继续。

持续控制是确保 tag 创建/更新权限确实被 GitHub ruleset 限制，并在每次发布前复核实际生效状态。在当前服务计划或私有仓库权限不允许查看/配置所需 ruleset 时，不要把未验证的保护写成既成事实，也不要 push 发布 tag。

当前 workflow 只验证 tag 文本和项目 version，没有验证 tag peeled commit 等于 `origin/master`，也没有查询同一 SHA 的 branch CI 结果。tag commit 还携带它自己的 workflow 和 release script。因此人工发布流程必须比较 tag/master SHA 并核对精确 commit 的成功 CI；在已选定的 tag-push 授权策略下，真正抵御有写权限主体绕过这些人工步骤依赖已验证、且不能由普通发布主体修改的 protected tag/ruleset。

### 完成：升级到 Node 24 runtime 的 pinned actions

两个 workflow 已将 `actions/checkout` 升级到 `v7.0.0`、`actions/setup-dotnet` 升级到 `v6.0.0`、`actions/upload-artifact` 升级到 `v7.0.1`、`actions/download-artifact` 升级到 `v8.0.1`；`NuGet/login v1.2.0` 保持不变。所有引用继续固定完整 commit SHA，官方 tag SHA 与各自 `action.yml` 的 `node24` runtime 已逐项核对。

CI run `29717988747` 已在 Ubuntu 和 Windows hosted runners 上执行这些 pins，并成功完成两端 artifact upload；日志中没有 Node 20 deprecation warning。发布 workflow 中的 `download-artifact v8` 和 `NuGet/login v1.2.0` 只有发布 tag workflow 才会运行，本次没有为验证它们创建 tag；其 SHA、官方 tag 和 `node24` metadata 已静态核对。

### 完成：增加 package static-assets/integrity 校验

校验已接入现有 `scripts/verify-release.ps1` 的 package-consumer 路径，没有新增绕开 release gate 的发布脚本：

- 从真实 `.nupkg` 枚举全部 CSS/JS，包括全局 CSS、scoped CSS bundle 和 collocated JS。
- 读取临时消费者实际生成的 `staticwebassets.build.endpoints.json`，要求每个未压缩 package asset 都有唯一 integrity metadata。
- 对 ZIP entry 原始字节逐项计算 `sha256-<base64>` 并 fail closed；压缩 endpoint 不与原始 package entry 混算。
- 独立测试覆盖正确 hash、缺失 metadata、metadata 指向缺失 asset、缺失 integrity、hash 不匹配和冲突 integrity。

### 完成：统一发布状态文档

spec/release notes 已更新为实际发布状态，公开组件名同步为 `BzsTabItem`，并新增 ADR 0024 记录 tag-push 授权策略。原 `.scratch` ticket 和 ADR 0017 的历史权限边界保持不变。若需要 GitHub Release，使用更新后的 `docs/releases/0.1.0.md` 创建，不要创建新 tag。

### P2：建立可执行的 symbols-only 恢复入口

当前 workflow 没有 `workflow_dispatch` 或 symbols-only 输入，并且固定先 push 主包、成功后才 push symbols。若主包已经成功而 `.snupkg` 失败，完整 rerun 会在主包 HTTP 409 处停止，无法到达 symbols push。

NuGet.org 还会给主包增加 repository signature（ZIP 中的 `.signature.p7s`），所以远端 flat-container nupkg 与原始 artifact 的整包字节 hash 正常情况下不同。恢复时不能用整包 hash 相等作为判据，应验证 repository signature，并比较 package ID/version/repository commit 以及除 `.signature.p7s` 外的逐条目内容。

后续应增加受 `nuget-production` 与 OIDC 保护的 symbols-only 恢复入口，按原 run ID 下载精确 artifact，并记录 artifact retention、主包/symbols 原始 hash。入口完成前，遇到部分成功必须停止并单独评审恢复方案；不要直接 rerun，也不要用 `0.1.0` 做恢复演练。

### P2：决定是否引入 lock file 和 NuGet cache

当前没有 `packages.lock.json`。先决定 locked restore 策略并验证 `dotnet restore --locked-mode`，再考虑 `setup-dotnet cache: true`。不要采用只 hash `*.csproj` 的缓存 key。

### P3：按耗时决定是否拆分 release gate

MudBlazor 的项目级 matrix 与 `fail-fast: false` 可改善并行反馈，但本仓库 release script 同时承担顺序、package/AOT 和证据汇总。只有实际耗时或定位成本成为问题时再拆分，发布 job 仍必须依赖全部必需门禁。

## 下一个版本的发布流程

1. 在 `master` 更新 `src/Bzs.Blazor/Bzs.Blazor.csproj` 的唯一 `Version`。
2. 更新 release notes、Breaking Changes 和必要的 package metadata。
3. 运行完整 release gate，确认无 IL2xxx/IL3050，并用临时 package consumer 验证。
4. 提交并 push `master`，等待目标 commit 的 branch CI 成功，并记录精确 SHA。
5. 在最新 `origin/master` 上创建 annotated tag，例如 `v0.1.1`，但先不要 push。
6. `git fetch origin master --tags` 后要求 candidate tag 的 peeled commit、预期 release SHA 和 `origin/master` 完全相等，并用 `gh run list --commit <sha> --workflow CI` 确认同一 SHA 的 CI 成功。
7. 再次确认 tag version 与项目 version 完全一致，并确认这是有意的正式发布。
8. 复核 `nuget-production` protection rules 与 tag 创建/更新权限；当前策略不设置 required reviewer，push tag 后不会再等待人工审批。
9. 将 push tag 作为最终人工发布授权；push 后等待发布 workflow 两端门禁和自动 publish。
10. 确认 OIDC、主包、symbols 三步成功。
11. 等待 NuGet v3 flat-container、registration 和 search 索引传播。
12. 使用隔离缓存和仅官方源的全新消费者 restore/build。
13. 记录成功 run URL、tag commit、NuGet 页面和消费验证。

不要在已发布版本上使用 `--skip-duplicate` 进行盲目恢复。当前没有 symbols-only 恢复入口；若主包成功而 symbols 失败，停止完整 rerun，按前述 repository signature/逐条目规则核验，并通过单独评审的恢复变更处理。

## 发布后官方源消费复现

使用全新临时目录，不复用仓库内 `artifacts/`。以下步骤保留独立 package cache、禁用 NuGet cache，并通过只含 nuget.org 的临时 config 排除本地 feed：

```powershell
$root = Join-Path $env:TEMP "bzs-nuget-consumer-$([guid]::NewGuid())"
$packages = Join-Path $root '.nuget-packages'

dotnet new blazor `
    --output $root `
    --name Bzs.NuGet.Consumer `
    --framework net10.0 `
    --interactivity None `
    --no-restore

dotnet add "$root/Bzs.NuGet.Consumer.csproj" package Bzs.Blazor `
    --version 0.1.0 `
    --no-restore
```

在 `$root/NuGet.Config` 写入：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

在临时项目中做三个最小消费修改：

- `Components/_Imports.razor` 增加 `@using Bzs.Blazor`。
- `Components/Pages/Home.razor` 增加 `<BzsButton>Bzs.Blazor 0.1.0</BzsButton>`。
- `Components/App.razor` 的 `<head>` 增加 `<link rel="stylesheet" href="@Assets["_content/Bzs.Blazor/bzs.blazor.css"]" />`。

然后执行：

```powershell
$originalNuGetPackages = $env:NUGET_PACKAGES

try {
    $env:NUGET_PACKAGES = $packages

    dotnet restore "$root/Bzs.NuGet.Consumer.csproj" `
        --configfile "$root/NuGet.Config" `
        --packages $packages `
        --no-http-cache `
        --force-evaluate

    dotnet build "$root/Bzs.NuGet.Consumer.csproj" `
        --configuration Release `
        --no-restore

    Select-String -Path "$root/obj/project.assets.json" -Pattern 'Bzs.Blazor/0.1.0'
    Select-String `
        -Path "$root/obj/Release/net10.0/staticwebassets.build.json" `
        -Pattern '_content/Bzs.Blazor/bzs.blazor.css'
}
finally {
    $env:NUGET_PACKAGES = $originalNuGetPackages
}
```

验收要求：restore/build 为 0 warning、0 error；`project.assets.json` 中 `Bzs.Blazor/0.1.0` 的类型是 `package` 且没有本地 project path；static-web-assets manifest 含包 CSS endpoint。临时目录不提交，验证完成后可删除。

## 接手检查命令

```powershell
git status --short --branch
git log -3 --oneline --decorate
git rev-parse 'v0.1.0^{}'
git ls-remote --tags origin v0.1.0 'v0.1.0^{}'

gh run view 29686673611
gh run view 29687047745
gh run view 29687905506

gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production `
    --jq '{can_admins_bypass,protection_rules,deployment_branch_policy}'
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production/deployment-branch-policies `
    --jq '.branch_policies[] | {name,type}'

Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/bzs.blazor/index.json
Invoke-RestMethod https://api.nuget.org/v3/registration5-gz-semver2/bzs.blazor/index.json
```

完整本地 release gate：

```powershell
pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release
```

该命令成本较高，会运行浏览器、package consumer、trim 和 AOT。只检查已记录发布状态时，不要无理由重跑；先检查 GitHub run、NuGet API 和当前 git 状态。

## 关键文档

- `CONTEXT.md`：领域语言和产品边界
- `docs/specs/2026-07-18-bzs-blazor-v0.1.md`：接受的 v0.1 规格
- `docs/plans/2026-07-18-bzs-blazor-v0.1-implementation.md`：实现计划
- `.scratch/bzs-blazor-v0.1/issues/`：本地 ticket 与验证记录
- `docs/mudblazor-radzen-blazor-research-2026-07-17.md`：组件库/产品调研
- `docs/mudblazor-radzen-cicd-research-2026-07-19.md`：上游 CI/CD 对比及实跑结果
- `docs/nuget-trusted-publishing-research-2026-07-19.md`：OIDC/Trusted Publishing 边界
- `docs/releases/0.1.0.md`：首版 release notes 和 NuGet 安装入口
- `docs/adr/0024-authorize-nuget-publication-through-controlled-tag-pushes.md`：tag push 最终授权决策和持续控制
- `.github/workflows/ci.yml`：常规 CI
- `.github/workflows/publish-nuget.yml`：tag 发布
- `scripts/verify-release.ps1`：统一 release gate

## 明确禁止事项

- 不移动、覆盖或删除 `v0.1.0`。
- 不重新发布 `Bzs.Blazor 0.1.0` 主包。
- 不把 CoreApi/CoreApi.Client、Radzen 或 MudBlazor 引入 core runtime dependency。
- 不把 render mode 或主题持久化策略放进组件库。
- 不为规避 trim/AOT warning 添加无依据 suppression。
- 不把长期 NuGet API key 留作 OIDC 自动 fallback。
- 不把忽略的 `artifacts/` 当作唯一长期发布证据。
