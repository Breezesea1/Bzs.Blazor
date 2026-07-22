# Bzs.Blazor v0.1.1 视觉改版、Pages 与 NuGet 发布最终交接

- 生成时间：2026-07-22（UTC+08:00）
- 仓库：`git@github.com:Breezesea1/Bzs.Blazor.git`
- 仓库 URL：<https://github.com/Breezesea1/Bzs.Blazor>
- 静态 Demo：<https://breezesea1.github.io/Bzs.Blazor/>
- NuGet：<https://www.nuget.org/packages/Bzs.Blazor/0.1.1>
- 前序 Demo/Pages 基线：`docs/handoffs/2026-07-21-bzs-blazor-demo-host-pages-final-handoff.md`
- 前序发布加固基线：`docs/handoffs/2026-07-20-bzs-blazor-v0.1.0-release-hardening-final-handoff.md`

本文承接 `v0.1.0` 首发、Demo host 拆分和 GitHub Pages 上线，记录 Auth/Savvy 风格视觉改版、`v0.1.1` patch release、同 SHA CI/Pages 验证、NuGet.org OIDC 发布，以及发布后实时检查结果。外部系统状态是生成本文时的快照；后续发布或权限处置前必须重新查询。本文末尾记录了同日 follow-up 的权限加固和验收结论。

## 一句话结论

`Bzs.Blazor 0.1.1` 已从 `master` 的 `ab8442e` 提交通过 annotated tag `v0.1.1` 正式发布：本地完整 release gate、同 SHA GitHub CI、GitHub Pages 部署和 NuGet OIDC 发布均成功，NuGet 公共索引已包含 `0.1.1`，线上 Pages 已返回新版共享 catalog CSS 和主题 token。公共组件 API、render-mode 支持、static SSR 边界和核心包的零第三方 UI runtime dependency 均未改变。后续已完成真实 Pages 浏览器人工验收，并创建了 active 的发布 tag ruleset；Pages 的 CI 前置编排和 focused overview-link 测试仍需 commit/push 才会影响线上流程。

## 最终状态快照

生成本文前工作区为 clean；保存初版本文后，本文件是预期的唯一工作区改动。本文 follow-up 额外产生了 Pages workflow、focused browser test 和本文的本地修改，均尚未 commit 或 push。

| 项目 | 值 |
| --- | --- |
| 分支 | `master` |
| HEAD | `ab8442ec20bf8adc8285e04b12da3a2c9d600608` |
| `origin/master` | `ab8442ec20bf8adc8285e04b12da3a2c9d600608` |
| ahead / behind | `0 / 0` |
| 发布提交 | `ab8442e release: prepare Bzs.Blazor 0.1.1` |
| package version | `0.1.1` |
| annotated tag | `v0.1.1` |
| tag object | `282641682e498561298e189d281bc521751a3adc` |
| peeled release commit | `ab8442ec20bf8adc8285e04b12da3a2c9d600608` |
| 仓库状态 | public，默认分支 `master` |
| CI | [29852090041](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852090041)，success，同 SHA |
| GitHub Pages | [29852090029](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852090029)，success，同 SHA |
| NuGet publish | [29852826220](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852826220)，success，tag `v0.1.1` |
| NuGet flat-container | `0.1.0`、`0.1.1` 均已索引 |
| `master` branch protection | 不存在；未在本次 follow-up 改变直接推送策略 |
| repository rulesets | [`#19528030 Protect published release tags`](https://github.com/Breezesea1/Bzs.Blazor/rules/19528030)，active，target `tag` |
| release-tag protection | 匹配 `refs/tags/v*.*.*`；禁止非 bypass 主体 creation、update、deletion；仅 `RepositoryRole=Admin` 可 bypass |
| `nuget-production` ref policy | `v*.*.*`，type `tag` |

以下三个值已经显式比对相等，不是仅根据 tag 名称推断：

```text
v0.1.1 peeled commit = ab8442ec20bf8adc8285e04b12da3a2c9d600608
origin/master         = ab8442ec20bf8adc8285e04b12da3a2c9d600608
Bzs.Blazor.csproj     = 0.1.1
```

`v0.1.0` 和 `v0.1.1` 都必须被视为不可变的已发布版本证据，不得移动、覆盖或删除。但当前仓库没有 tag ruleset，这一不可变性仍是操作纪律，不是 GitHub 已强制执行的权限保证。

## 本次改动范围

### Demo 视觉语言

Aspire server Demo 和 standalone WebAssembly Demo 已统一为更接近 `auth.dy3danimation.com` 与 `savvy.dy3danimation.com` 的紧凑、中性、克制拟态风格：

- 使用更轻的中性灰背景、清晰的 raised/inset surface 层级和克制阴影。
- 导航、toolbar、内容区、移动端菜单和首页组件组概览使用同一视觉规则。
- 保留 Demo 的工具型信息密度，没有引入营销式 hero、装饰性渐变或大面积单色主题。
- server Demo 与 Pages Demo 的导航和首页结构同步，避免两种 host 漂移。

共享结构样式集中到：

```text
samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/wwwroot/demo-catalog.css
```

两个 host 都直接加载该文件。原先重复的两个 `wwwroot/app.css` 以及两套 `MainLayout.razor.css`、`NavMenu.razor.css` 已删除；它们的有效职责由共享 catalog CSS 接管。

### 内置主题 token

运行时主题同步修改了：

```text
src/Bzs.Blazor/Components/Theme/BzsTheme.cs
src/Bzs.Blazor/wwwroot/bzs.blazor.css
```

内置 light/dark 主题继续使用语义 token，并保持 C# 主题定义与静态 CSS 一致。输入边界对比度已提高：light border 为 `#747c87`，dark border 为 `#66717e`。新增测试要求启用态 input border 相对 canvas 和 inset fill 都达到 WCAG non-text contrast 的 `3:1`。

### 测试与视觉基线

- 新增 `BuiltInInputBoundariesMeetNonTextContrast` 主题契约测试。
- 刷新 light/dark desktop 和 mobile 的四张视觉回归基线。
- 首页增加组件组概览，但公共组件 API 没有新增 breaking change。
- 没有向 `src/Bzs.Blazor` 引入第三方 UI dependency、Aspire dependency 或参考应用 dependency。

### 发布元数据

- `src/Bzs.Blazor/Bzs.Blazor.csproj` 的唯一 `Version` 从 `0.1.0` 更新为 `0.1.1`。
- `PackageReleaseNotes` 指向 `docs/releases/0.1.1.md`。
- README 的安装命令、tag 示例和 release-notes 链接更新为 `0.1.1`。
- 新增 `docs/releases/0.1.1.md`，明确本次无 breaking change。

## 本地验证证据

发布提交前执行：

```powershell
pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release
```

结果：

- clean、restore、Release build 全部成功，`0 warnings / 0 errors`。
- unit/component tests：`108/108`。
- browser tests：`36/36`。
- browser matrix：Chromium、Mobile Chrome、Chrome、Edge、Firefox、WebKit、Mobile Safari 全部成功。
- NuGet pack 成功生成 `Bzs.Blazor.0.1.1.nupkg` 和 `Bzs.Blazor.0.1.1.snupkg`。
- package consumer source tests：`19/19`。
- package consumer published-output tests：`19/19`。
- trimming、standalone WebAssembly publish 和 WASM AOT 成功，没有 IL2xxx 或 IL3050 警告。
- Pages 输出准备、base-path、SPA fallback 和静态资产检查成功。

提交前独立 staged review 没有发现实质性缺陷。本文 follow-up 新增了 `DemoSmokeTests.CatalogComponentGroupLinksNavigateToTheirSamples`，按完整可访问链接名称逐一验证六个首页 overview link 的路由和目标页标题；focused suite 结果为 `9/9`。随后已重新执行完整 `pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release`，成功写入 `artifacts/release/verification-summary.md`；visual regression、browser matrix 和 WASM AOT 均未跳过。这些验证仍不替代 workflow 变更 commit/push 后的 hosted CI/Pages run。

## GitHub CI 与 Pages 证据

### 同 SHA CI

[CI run 29852090041](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852090041) 针对 `ab8442e...` 成功：

- `Windows branded browsers` job `88707229350`：success，约 `4m23s`。
- `Release gate` job `88707229452`：success，约 `9m35s`。

该 run 在发布 tag 创建前完成，因此 `v0.1.1` 指向的提交已经先通过 `master` 的同 SHA 托管门禁。

### GitHub Pages

[Deploy demo to GitHub Pages run 29852090029](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852090029) 针对同一 `ab8442e...` 成功：

- `Build static demo` job `88707229619`：success。
- `Deploy static demo` job `88707490428`：success。

发布后实时 HTTP 检查：

- <https://breezesea1.github.io/Bzs.Blazor/> 返回 HTTP `200`，title 为 `Bzs.Blazor catalog`。
- 根页面包含 `<base href="/Bzs.Blazor/">`。
- `_content/Bzs.Blazor.Demo.Catalog/demo-catalog.css` 返回 HTTP `200`，长度 `13738`，包含新版共享 shell 规则。
- `_content/Bzs.Blazor/bzs.blazor.css` 返回 HTTP `200`，长度 `4206`，包含新版 light/dark 主题和 `#747c87`、`#66717e` token。

本次发布后完成了 workflow 和 HTTP/静态资产验证。随后用户已在真实 Pages URL 完成最终浏览器人工验收，覆盖深链接、菜单、主题切换和交互计数器；因此本次视觉版本不再存在人工浏览器验收缺口。该确认是人工验收结论，不替代 workflow、HTTP 或自动化浏览器证据。

发布时 Pages 与 CI 仍由同一 `master` push 并行触发，Pages 不等待 CI。本文 follow-up 已将 `.github/workflows/deploy-demo-pages.yml` 改为仅消费成功的 `CI` `workflow_run`，限定同仓库的 `master` push，并 checkout 被验证的 `head_sha`。该 workflow 变更尚未 commit/push；在其上线并完成一次 hosted run 前，线上部署流程仍是发布时的并行版本。

## NuGet v0.1.1 发布证据

[Publish NuGet run 29852826220](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29852826220) 由 annotated tag `v0.1.1` 触发，head SHA 为 `ab8442e...`，最终 success：

- `Verify release package` job `88709707429`：success，约 `10m14s`。
- `Windows branded browsers` job `88709707434`：success，约 `3m53s`。
- `Publish to nuget.org` job `88712198123`：success，约 `21s`。
- tag/project-version 校验成功。
- verified `.nupkg`、`.snupkg` artifact 上传成功。
- `NuGet/login` OIDC 登录成功，没有使用长期 NuGet API key。
- 主包和 symbols 的两个 `dotnet nuget push` 步骤均成功。

发布后 NuGet.org 实时检查：

- flat-container index 返回 `0.1.0` 和 `0.1.1`。
- `Bzs.Blazor.0.1.1.nupkg` 返回 HTTP `200`，Content-Length `114470`。
- registration JSON 已可查询 `0.1.1`。
- `.snupkg` 不通过普通 flat-container URL 暴露；symbols 成功的直接证据是 publish job 的合并 `Publish package and symbols` 步骤中，独立 `.snupkg` push 命令成功。不要用 `.snupkg` flat-container `404` 误判 symbols 发布失败。

安装命令：

```text
dotnet add package Bzs.Blazor --version 0.1.1
```

## 权限与安全边界

### 已完成的 P1：发布 tag ruleset

2026-07-22 follow-up 通过 GitHub REST API 创建并复核了 active repository ruleset [`#19528030 Protect published release tags`](https://github.com/Breezesea1/Bzs.Blazor/rules/19528030)：

- target 为 `tag`，ref pattern 为 `refs/tags/v*.*.*`。
- rules 为 `creation`、`update`、`deletion`，阻止无 bypass 权限的主体创建、移动或删除发布 tag。
- bypass actor 为 `RepositoryRole=Admin`，`bypass_mode=always`；当前管理员账号的 API 结果为 `current_user_can_bypass=always`。
- `master` 仍没有 branch protection；这次有意只加固 tag 发布授权边界，不改变既有直接推送工作流。
- `nuget-production` environment 仍有 `branch_policy`，允许 `v*.*.*` tag，`can_admins_bypass=true`。

环境的 tag pattern 只限制哪些 ref 可以进入 `nuget-production`；repository-level tag ruleset 才负责发布 tag 的创建、更新和删除边界。管理员仍可绕过规则，因此这提供的是最小权限和审计边界，不是“任何主体都绝对不可变”的承诺。已发布 tag 仍不得移动、覆盖或删除。

### 公开仓库边界

- 仓库和 Pages 都是 public；示例、截图、workflow log、测试 fixture 和 artifact 必须按公开输出审查。
- Pages host 只能提供静态 Interactive WebAssembly，不承载认证、秘密、服务器 API、数据库或生产数据。
- 已公开提交和 tag 中的 author/tagger email 不应通过轻率重写历史或移动 release tag 处理。
- `NUGET_USERNAME` 是 Actions environment variable，不是 secret。匿名公众不能读取，但拥有相应仓库权限的已认证 API 调用者可以查询其值；这里只能存放 NuGet 用户名等非敏感配置，凭据继续由 OIDC 短期交换，不得放入 Actions variable。

## 后续 patch 发布 Runbook

1. 从 clean `master` 开始，确认 `HEAD == origin/master`。
2. 更新 `Bzs.Blazor.csproj` 的唯一 `Version`、`PackageReleaseNotes`、README 和新的 `docs/releases/<version>.md`。
3. 执行完整本地 release gate，并保留 build、test、browser、consumer、trimming、AOT 和 pack 结果。
4. 只暂存本次范围，执行 `git diff --cached --check` 和独立 review。
5. commit 并 push `master`。
6. 等待该精确 SHA 的 CI 成功；同时确认 Pages run 成功并对线上站点做实际浏览器验收。
7. 创建新的 annotated tag，逐项验证 peeled commit、`origin/master` 和 project version 完全一致。
8. push 新 tag，等待 `Publish NuGet` 的 verify、Windows browser 和 NuGet publish jobs 全部成功。
9. 查询 NuGet flat-container、registration 和 package URL；区分 workflow push 成功与公共索引传播完成。
10. 保持所有既有 release tag 不变，不使用 `--skip-duplicate` 掩盖部分发布或恢复问题。

关键命令：

```powershell
git status --short --branch
git rev-parse HEAD
git rev-parse origin/master
git rev-list --left-right --count HEAD...origin/master

pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release

git tag -a v0.1.2 -m "Bzs.Blazor 0.1.2"
git rev-parse 'refs/tags/v0.1.2^{commit}'
git rev-parse origin/master
git push origin v0.1.2

gh run list --commit <release-sha> --workflow CI --limit 10
gh run list --workflow publish-nuget.yml --limit 10

Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/bzs.blazor/index.json
Invoke-RestMethod https://api.nuget.org/v3/registration5-gz-semver2/bzs.blazor/index.json
```

权限复核：

```powershell
gh api repos/Breezesea1/Bzs.Blazor/rulesets
gh api repos/Breezesea1/Bzs.Blazor/rulesets/19528030
gh api repos/Breezesea1/Bzs.Blazor/branches/master/protection
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production/deployment-branch-policies
```

## 明确禁止事项

- 不移动、覆盖或删除 `v0.1.0`、`v0.1.1` 以及任何已发布 tag。
- 不为测试 workflow 随意创建 `v*` tag；tag push 是真实生产发布授权。
- 不在 Pages workflow 修改尚未 commit/push 时，将线上部署流程写成已由 CI 前置放行。
- 不把根页面 HTTP `200` 或 CSS 可下载单独写成完整浏览器交互已验收；本次结论还依赖已记录的人工 Pages 浏览器验收。
- 不把 `.snupkg` flat-container `404` 写成 symbols publish 失败；先检查 job `88712198123` 的合并 `Publish package and symbols` 步骤，以及其中 `.snupkg` push 命令的成功日志。
- 不将 Demo.Catalog 内容复制回两个 host，也不恢复已删除的重复 host CSS。
- 不在 `src/Bzs.Blazor` 引入参考应用、Aspire 或第三方 UI runtime dependency。
- 不向静态 Pages host 加入认证、秘密、server-only dependency、API 或数据库。
- 不使用 `--skip-duplicate` 掩盖主包和 symbols 的部分成功状态。

## 关键文件

- `src/Bzs.Blazor/Bzs.Blazor.csproj`：package version 与 NuGet 元数据。
- `src/Bzs.Blazor/Components/Theme/BzsTheme.cs`：内置主题的 C# 语义 token。
- `src/Bzs.Blazor/wwwroot/bzs.blazor.css`：运行时全局主题 token。
- `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/wwwroot/demo-catalog.css`：两个 Demo host 的共享 shell 样式。
- `tests/Bzs.Blazor.Tests/ThemeTests.cs`：主题契约和输入边界对比度测试。
- `tests/Bzs.Blazor.BrowserTests/DemoSmokeTests.cs`：首页 overview link 的语义化导航断言。
- `tests/Bzs.Blazor.BrowserTests/VisualBaselines/`：本次更新的 light/dark desktop/mobile 基线。
- `docs/releases/0.1.1.md`：`0.1.1` 发布说明。
- `.github/workflows/ci.yml`：常规同 SHA 验证。
- `.github/workflows/deploy-demo-pages.yml`：GitHub Pages 发布。
- `.github/workflows/publish-nuget.yml`：严格 SemVer tag、完整门禁和 OIDC NuGet 发布。
- `scripts/verify-release.ps1`：本地与 hosted release gate 的共同入口。
