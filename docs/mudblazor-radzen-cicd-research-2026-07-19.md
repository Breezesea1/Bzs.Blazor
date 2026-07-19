# MudBlazor 与 Radzen Blazor CI/CD 调研

查询日期：2026-07-19（UTC+08:00）。本文只引用项目官方 GitHub 仓库、GitHub Actions 官方资料、NuGet/Microsoft 官方资料。上游仓库事实固定到查询时的 commit：MudBlazor [`745ce826`](https://github.com/MudBlazor/MudBlazor/commit/745ce826e7bbba9f0ed6ac797704fc7f0830d34d)，Radzen Blazor [`6d2a0dd8`](https://github.com/radzenhq/radzen-blazor/commit/6d2a0dd81a76386744d8436383c685e8ca4809a0)。“建议”针对本仓库发布 `v0.1.0` 时采用的 `.github/workflows/ci.yml` 与 `.github/workflows/publish-nuget.yml`，不代表上游承诺。

## 结论摘要

本仓库不应把任一上游 workflow 当作发布模板直接复制。MudBlazor 与 Radzen 都把常规 CI 和 NuGet 发布放在不同 workflow 文件，但查询日的正式发布仍使用长期 NuGet API key；MudBlazor 的 tag 发布不验证项目版本、也不依赖同次完整 CI，Radzen 则由人工 dispatch 决定是否发布。两者均只在 Ubuntu 运行组件/unit 测试，没有真实浏览器矩阵。相比之下，本仓库当前的严格 SemVer tag/项目版本一致性、OIDC 短期凭证、verified artifact 交接、Ubuntu 与 Windows 双门禁、显式 `.nupkg`/`.snupkg` 发布以及 production environment 更适合首个公开包，应保留。[M-CI] [M-NU] [R-CI] [R-NU] [N-OIDC]

值得采用的是更小的局部模式：MudBlazor 的 CI 按测试项目并行、`fail-fast: false`、格式检查与覆盖率结果独立；MudBlazor 和 Radzen 都在 push 前检查包内静态资产，Radzen 还逐项复算 static-web-assets integrity hash。这些模式能缩短反馈或提高 Razor Class Library 包完整性，但应加入本仓库现有 release gate，而不是替代它。[M-CI] [M-NU] [R-NU]

## 当前实现基线

本报告评估的实现已提交为 `e737f18`，并由 annotated tag `v0.1.0` 固定到同一 commit：

- `ci.yml`：PR 与 `master` push 触发；Ubuntu release gate 安装 `wasm-tools` 和 Playwright Chromium/Firefox/WebKit；Windows 另跑 visual regression 与系统 Chrome/Edge；始终上传测试证据。
- `publish-nuget.yml`：`v*.*.*` tag 触发；严格解析 SemVer，并要求 tag 与 `Bzs.Blazor.csproj` 中唯一的 `Version` 完全一致；Ubuntu 完成 release gate 并上传精确命名的 `.nupkg`/`.snupkg`，Windows 浏览器 job 通过后，独立 `publish` job 才从 artifact 发布。
- 发布 job 使用 `environment: nuget-production`、仅授予 `id-token: write`，通过固定 commit SHA 的 `NuGet/login` 换取短期 API key；主包先以 `--no-symbols` 发布，再显式发布 `.snupkg`。
- 所有外部 actions 均固定到完整 commit SHA；`global.json` 与 workflow 都固定 .NET SDK `10.0.302`。

## v0.1.0 实跑结果

- [`CI run 29686673611`](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29686673611) 在 `e737f18` 上成功完成 Ubuntu release gate 与 Windows branded-browser/visual gate。
- [`Publish NuGet run 29687047745`](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29687047745) 从 tag `v0.1.0` 在同一 commit 上成功完成两端门禁、GitHub OIDC 登录以及 `.nupkg`/`.snupkg` 发布。
- NuGet.org 的 flat-container、registration 和 search API 均已索引 `Bzs.Blazor 0.1.0`。一个全新 Blazor Web App 使用独立 `NUGET_PACKAGES`、禁用缓存并仅指定 `https://api.nuget.org/v3/index.json`，成功还原包、编译 `<BzsButton>`、解析 `_content/Bzs.Blazor/bzs.blazor.css`，Release 构建结果为 0 warning、0 error。

## 逐项对比

| 维度                      | MudBlazor                                                                                                                                                                                                                                                  | Radzen Blazor                                                                                                                                                | 对本仓库的结论                                                                                                                                                                                                                                                                                                                                             |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Runner OS 与矩阵          | 正式 CI 全部 `ubuntu-latest`。`build-test` 用 include matrix 跑 3 个测试项目，`build-only` 用 matrix 构建 WASM/Server 文档应用，均 `fail-fast: false`。[M-CI]                                                                                              | CI 与 NuGet 发布均只用 `ubuntu-latest`，没有 OS、SDK 或项目 matrix。[R-CI] [R-NU]                                                                            | 保留 Ubuntu release gate + Windows Chrome/Edge。可借鉴 MudBlazor 的项目级并行，但本仓库目前单一 unit/browser/package gate 是否拆分，应先以耗时数据决定。不要为“和上游一致”删掉 Windows job。                                                                                                                                                               |
| CI/发布分离               | `build-test-mudblazor.yml` 与 `deploy-mudblazor-nuget.yml` 分离；正式 NuGet workflow 由 tag 独立触发，不通过 `needs`、reusable workflow 或 `workflow_run` 依赖完整 CI。nightly workflow 才使用 `workflow_run` 且要求 CI success。[M-CI] [M-NU] [M-NIGHTLY] | `ci.yml` 与手动 `nuget.yml` 分离；发布 workflow 自己 build、pack、完整性检查、test、push。[R-CI] [R-NU]                                                      | 保留独立 CI/发布文件，也保留发布 workflow 内自给自足的验证。不能让 tag 发布只“相信此前某次 branch CI”；tag 指向的源码和待发布 artifact 必须在同次 run 内验证。                                                                                                                                                                                             |
| Tag 与版本验证            | tag pattern 为 `v[0-9]+.[0-9]+.[0-9]+*`，只去掉 `v` 后传给 `dotnet pack /p:Version=...`；没有严格 SemVer 解析或与项目版本比对。[M-NU]                                                                                                                      | NuGet workflow 只支持 `workflow_dispatch`，包版本来自项目中的 `<Version>11.1.6</Version>`；没有 tag 输入或 tag/version 校验。[R-NU] [R-PROJ]                 | 当前严格 SemVer + 项目唯一 `Version` 完全一致明显更强，应保留。不要复制 MudBlazor 的宽松 glob/字符串剥离，也不要以 Radzen 的人工按钮替代可审计 tag。                                                                                                                                                                                                       |
| NuGet 认证                | `secrets.NUGET_KEY` 长期 API key。[M-NU]                                                                                                                                                                                                                   | `secrets.NUGET_API_KEY` 长期 API key。[R-NU]                                                                                                                 | 不采用。保留 NuGet Trusted Publishing：GitHub OIDC + `NuGet/login` 返回短期 key，并把 `id-token: write` 限定在发布 job。[N-OIDC] [N-LOGIN]                                                                                                                                                                                                                 |
| `.nupkg` / `.snupkg`      | 项目启用 `IncludeSymbols` 与 `snupkg`；workflow pack 后执行 `dotnet nuget push nupkgs/*.nupkg`，依靠 NuGet CLI 在同目录自动发布 symbols，没有单独检查 `.snupkg` 存在。[M-PROJ] [M-NU] [N-SYMBOLS]                                                          | 项目启用 `IncludeSymbols` 与 `snupkg`；dry-run artifact 显式上传两者，真实 push 只传 `*.nupkg`，同样依靠 CLI 自动 symbols 行为。[R-PROJ] [R-NU] [N-SYMBOLS]  | 当前 artifact 对两种文件都 `if-no-files-found: error`，并在下载后逐个存在性检查，证据更清楚。保留先主包 `--no-symbols`、再显式 symbols；接受“主包成功、symbols 失败”需要定向恢复的已知部分成功边界。                                                                                                                                                       |
| 浏览器测试                | CI 构建 WASM/Server docs，但仓库树中没有 Playwright/Selenium 项目；主要测试项目是组件/unit tests，workflow 没有真实浏览器步骤。[M-CI] [M-TEST]                                                                                                             | 测试项目使用 bUnit/xUnit，CI 和发布 workflow 没有 Playwright/Selenium 或真实浏览器步骤。[R-CI] [R-NU] [R-TEST]                                               | 不照搬。Bzs.Blazor 明确支持静态 SSR、Server、WASM、Auto 和 evergreen browsers，真实 Playwright 测试是产品契约门禁。当前 Windows visual baselines 与系统 Chrome/Edge 补足 Linux Playwright 浏览器，方向正确。                                                                                                                                               |
| 发布门禁                  | 正式 NuGet job 自行 restore、pack、检查 CSS/JS 后直接 push；不运行 `build-test` workflow 的完整测试矩阵，也未设置 GitHub environment。nightly 反而要求主 CI 成功且限制原仓库。[M-NU] [M-NIGHTLY]                                                           | 单一发布 job 内 build、pack、integrity 检查、unit test 后按 dispatch input 直接 push；没有 environment 或独立 approval job。[R-NU]                           | 不降低当前门禁。保留 `publish needs: [verify, windows-branded-browsers]`、verified artifact 交接、`nuget-production` environment。可借鉴 package-specific integrity 检查并纳入 `verify-release.ps1`。                                                                                                                                                      |
| Action SHA 与 Node 运行时 | workflow 使用浮动 major tags：`checkout@v7`、`setup-dotnet@v6`、`cache@v6`、`upload-artifact@v7` 等。查询日官方 action metadata 显示这些版本使用 Node 24。[M-CI] [A-CHECKOUT] [A-DOTNET] [A-CACHE]                                                         | 使用 `checkout@v4`、`setup-dotnet@v4`、`upload-artifact@v4` 等浮动 major tags；官方 metadata 显示这些版本使用 Node 20。[R-CI] [R-NU] [A-CHECKOUT] [A-DOTNET] | 不采用浮动 major tag。GitHub 官方把完整 commit SHA 视为不可变 release 的唯一方式；保留本仓库 SHA pin。[G-SEC] 当前 checkout/setup/artifact v4 pins 使用 Node 20，而 pinned `NuGet/login` 使用 Node 24；升级 actions 时应核对运行时和 runner 支持，但无需为了统一 Node 版本而无证据升级。                                                                   |
| 缓存                      | 设置工作区 `NUGET_PACKAGES`，以所有 `*.csproj` hash 为 key 缓存 NuGet；build composite action restore 相同 key。key 未显式包含 `global.json`、props、targets 或 lock file。[M-CI] [M-BUILD]                                                                | NuGet CI/发布没有依赖缓存；仅部署容器使用 Docker `type=gha` layer cache，与 NuGet 包发布无关。[R-CI] [R-NU] [R-DEPLOY]                                       | 暂不复制 MudBlazor key。本仓库没有 `packages.lock.json`；`setup-dotnet` 官方内建缓存要求 lock file，否则报错。[A-DOTNET] 先决定并引入 locked restore，再用 lock file 作为缓存依赖；不要用仅 `*.csproj` hash 的缓存掩盖中央 props/SDK 变化。                                                                                                                |
| Workload 安装             | 正式 CI/发布没有 `dotnet workload install`；其 CI 只 build docs WASM 项目。[M-CI] [M-NU]                                                                                                                                                                   | CI/发布没有 workload 安装。[R-CI] [R-NU]                                                                                                                     | 不照搬。Bzs release gate 明确做 WASM AOT/trim 验证，`wasm-tools` 是真实依赖。保留 `dotnet workload install wasm-tools --skip-manifest-update`；官方说明该 flag 阻止 install 时下载更新 manifest，减少 CI 漂移。[D-WORKLOAD] 若未来升级 `setup-dotnet@v6`，不要无评估改用其 `workloads:` 输入，因为官方说明它会先执行 `dotnet workload update`。 [A-DOTNET] |

## 值得采用的上游模式

### 1. 在包发布前验证静态资产内容

MudBlazor 在 pack 后直接检查包内是否含 `MudBlazor.min.css` 与 `MudBlazor.min.js`；Radzen 解包 `.nupkg`，从 `Microsoft.AspNetCore.StaticWebAssets.props` 读取每项 `RelativePath`/`Integrity` 并重新计算 SHA-256，还会在未发现可校验资产时失败。[M-NU] [R-NU]

建议将这一思想接到本仓库现有 package verification，而不是新增绕开 release gate 的临时 shell：至少验证 `wwwroot/bzs.blazor.css`、组件隔离 CSS bundle、collocated JS 和静态 web assets 元数据存在；若 `.props` 含 integrity，则复算所有声明文件。具体路径必须先以本仓库实际 `.nupkg` 结构为准，不能原样复制 Radzen 的固定路径。

### 2. 大型测试集按职责并行且不 fail-fast

MudBlazor 的项目 matrix 使用 `fail-fast: false`，让多个测试项目在一个失败时仍产出其他结果，并在未取消时上传覆盖率/测试报告。[M-CI] 对本仓库，只有当 release gate 的实际耗时或故障定位已成为问题时，才值得拆成 unit、browser、package/AOT 独立 jobs；发布仍必须 `needs` 所有必需门禁。当前 release script 还承担顺序与 artifact 汇总，盲目矩阵化会重复 restore/build/workload 成本。

### 3. 保持正式包与 nightly 包的发布通道分离

MudBlazor 将 nightly 放到 GitHub Packages，并限制只在原仓库、成功 CI 后运行；正式版本才发布 nuget.org。[M-NIGHTLY] 本仓库尚无 nightly 需求，不应现在增加。但未来若需要预览包，应采用不同 feed、明确 prerelease version 和独立 workflow，不要让 `--skip-duplicate` 或手动开关混入正式发布语义。

## 不应照搬的模式

1. **长期 API key**：两个上游都仍使用 repository secret。当前 NuGet OIDC policy 与短期 key 能缩小泄漏半径，不应回退。[M-NU] [R-NU] [N-OIDC]
2. **宽松或缺失的版本来源校验**：MudBlazor 的触发 glob 不是严格 SemVer 验证；Radzen 的 manual dispatch 不证明源码 commit 与发布版本的 tag 关系。保留本仓库 fail-closed 校验。[M-NU] [R-NU]
3. **发布不依赖完整跨平台门禁**：上游的 UI/发布需求与本仓库不同。不能以其 Ubuntu-only unit tests 为理由移除真实浏览器、WASM AOT、trim warning、临时包消费或 Windows visual/browser 检查。
4. **浮动 action major tag**：GitHub 官方明确指出完整 commit SHA 是唯一不可变引用。上游的 `@v4`/`@v7` 便于维护，但供应链边界弱于本仓库现有策略。[G-SEC]
5. **仅 `*.csproj` hash 的 NuGet cache**：依赖解析还受 `global.json`、props/targets、NuGet config 与 transitive resolution 影响。本仓库当前没有 lock file，先加缓存不是无风险优化。[M-CI] [A-DOTNET]
6. **Radzen 的 `--skip-duplicate`**：正式首发时把 HTTP 409 降为 warning 可能掩盖重复版本或错误 run。保留当前失败可见性；若要支持 rerun 恢复，应先建立验证远端包 hash 与本次 artifact 一致的操作流程。[R-NU] [D-PUSH]
7. **把 `setup-dotnet@v6 workloads:` 当作等价替换**：该 action 官方说明会先更新 workload，再安装；本仓库当前 `--skip-manifest-update` 是刻意减少 manifest 漂移。升级 action 与改变 workload 更新策略应分开决策。[A-DOTNET] [D-WORKLOAD]

## 对当前 workflow 的优先建议

1. **维持当前发布架构，不因上游而降级**：`verify` 与 Windows job 必须都成功，`publish` 只消费已验证 artifact，并通过 `nuget-production` + OIDC 发布。
2. **把 package static-assets/integrity 检查列为下一项增强**：这是两上游最值得迁移的共同经验，且直接覆盖 Razor Class Library 的高风险交付面。
3. **维持 action 完整 SHA pin，同时建立升级检查项**：核对 release tag 对应 commit、action metadata 的 Node runtime、GitHub-hosted runner 支持和 release notes。查询日本仓库的 v4 action pins 与注释版本一致；`NuGet/login` 的 pin 对应 `v1.2.0` 且使用 Node 24。[A-CHECKOUT] [A-DOTNET] [A-LOGIN]
4. **暂不启用 NuGet cache**：先引入并验证 `packages.lock.json` + `dotnet restore --locked-mode` 的团队策略，再考虑 `setup-dotnet cache: true`。缓存是性能优化，不是发布正确性条件。[A-DOTNET]
5. **保留显式 workload install**：SDK 已由 `global.json`/workflow 固定，`--skip-manifest-update` 避免安装时主动刷新 manifest。若安装时长明显，再研究 runner image 预装状态或 workload package cache，不能直接删除 AOT 所需 workload。[D-WORKLOAD]
6. **不要增加无需求的 OS/SDK matrix**：本包目标是 `net10.0`，固定 SDK 的 release reproducibility 比测试多个 10.0 patch 更重要。Windows 的目的应继续是 visual baseline 与 branded browser，不是重复所有 Ubuntu 工作。

## 边界与未验证项

- 上游对比是静态源码与官方文档核对，没有触发 MudBlazor 或 Radzen 的 workflow。本仓库随后实际发布了 `Bzs.Blazor 0.1.0`。
- 成功发布证明当前 GitHub environment 与 NuGet.org Trusted Publishing tuple 可完成 OIDC 交换；仓库文件仍不能证明 `nuget-production` 的 required reviewers 或 deployment tag rules 当前在服务端的具体配置。
- 本报告不修改 workflow。package integrity 检查、lock file/caching 或 action 版本升级都需要独立实现与测试。
- 上游分支会继续变化，所以事实引用尽量使用查询日 commit 永久链接；action 官方文档链接反映查询日最新状态。

## 官方来源

所有来源访问日期均为 **2026-07-19**。

### MudBlazor

- <a id="M-CI"></a>**[M-CI] CI workflow**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/.github/workflows/build-test-mudblazor.yml
- <a id="M-NU"></a>**[M-NU] 正式 NuGet workflow**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/.github/workflows/deploy-mudblazor-nuget.yml
- <a id="M-NIGHTLY"></a>**[M-NIGHTLY] Nightly workflow**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/.github/workflows/deploy-mudblazor-nightly.yml
- <a id="M-BUILD"></a>**[M-BUILD] Composite build action**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/.github/actions/build/action.yml
- <a id="M-PROJ"></a>**[M-PROJ] Package project**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/src/MudBlazor/MudBlazor.csproj
- <a id="M-TEST"></a>**[M-TEST] Unit-test project**：https://github.com/MudBlazor/MudBlazor/blob/745ce826e7bbba9f0ed6ac797704fc7f0830d34d/src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj

### Radzen Blazor

- <a id="R-CI"></a>**[R-CI] CI workflow**：https://github.com/radzenhq/radzen-blazor/blob/6d2a0dd81a76386744d8436383c685e8ca4809a0/.github/workflows/ci.yml
- <a id="R-NU"></a>**[R-NU] NuGet workflow**：https://github.com/radzenhq/radzen-blazor/blob/6d2a0dd81a76386744d8436383c685e8ca4809a0/.github/workflows/nuget.yml
- <a id="R-DEPLOY"></a>**[R-DEPLOY] Demo deploy workflow**：https://github.com/radzenhq/radzen-blazor/blob/6d2a0dd81a76386744d8436383c685e8ca4809a0/.github/workflows/deploy.yml
- <a id="R-PROJ"></a>**[R-PROJ] Package project**：https://github.com/radzenhq/radzen-blazor/blob/6d2a0dd81a76386744d8436383c685e8ca4809a0/Radzen.Blazor/Radzen.Blazor.csproj
- <a id="R-TEST"></a>**[R-TEST] Unit-test project**：https://github.com/radzenhq/radzen-blazor/blob/6d2a0dd81a76386744d8436383c685e8ca4809a0/Radzen.Blazor.Tests/Radzen.Blazor.Tests.csproj

### GitHub Actions、NuGet 与 .NET

- <a id="G-SEC"></a>**[G-SEC] GitHub Secure use reference（完整 commit SHA）**：https://docs.github.com/en/actions/reference/security/secure-use#using-third-party-actions
- <a id="A-CHECKOUT"></a>**[A-CHECKOUT] `actions/checkout` 官方仓库与 action metadata**：https://github.com/actions/checkout；本仓库 pinned v4.2.2：https://github.com/actions/checkout/blob/11bd71901bbe5b1630ceea73d27597364c9af683/action.yml；查询日 v7.0.0：https://github.com/actions/checkout/blob/9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0/action.yml
- <a id="A-DOTNET"></a>**[A-DOTNET] `actions/setup-dotnet` 官方仓库、缓存与 workloads 文档**：https://github.com/actions/setup-dotnet；本仓库 pinned v4.3.1：https://github.com/actions/setup-dotnet/blob/67a3573c9a986a3f9c594539f4ab511d57bb3ce9/action.yml；查询日 v6.0.0：https://github.com/actions/setup-dotnet/blob/a98b56852c35b8e3190ac28c8c2271da59106c68/action.yml
- <a id="A-CACHE"></a>**[A-CACHE] `actions/cache` 官方仓库与查询日 v6.1.0 metadata**：https://github.com/actions/cache；https://github.com/actions/cache/blob/55cc8345863c7cc4c66a329aec7e433d2d1c52a9/action.yml
- <a id="A-LOGIN"></a>**[A-LOGIN] 本仓库 pinned `NuGet/login` v1.2.0 action metadata**：https://github.com/NuGet/login/blob/8d196754b4036150537f80ac539e15c2f1028841/action.yml
- <a id="N-OIDC"></a>**[N-OIDC] NuGet.org Trusted Publishing**：https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- <a id="N-LOGIN"></a>**[N-LOGIN] NuGet 官方 OIDC login action**：https://github.com/NuGet/login
- <a id="N-SYMBOLS"></a>**[N-SYMBOLS] NuGet `.snupkg` 创建与发布**：https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg
- <a id="D-PUSH"></a>**[D-PUSH] `dotnet nuget push` 官方文档**：https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push
- <a id="D-WORKLOAD"></a>**[D-WORKLOAD] `dotnet workload install` 官方文档**：https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-install
