# Bzs.Blazor v0.1.0 发布与加固最终交接

- 生成时间：2026-07-20（UTC+08:00）
- 仓库：`git@github.com:Breezesea1/Bzs.Blazor.git`
- 仓库可见性：private
- NuGet：<https://www.nuget.org/packages/Bzs.Blazor/0.1.0>
- 前序详细交接：`docs/handoffs/2026-07-19-bzs-blazor-v0.1.0-release-handoff.md`

本文是 `Bzs.Blazor 0.1.0` 首发及发布后加固完成后的最终接手基线。所有“当前”状态都是生成本文时的时间点证据；执行发布、恢复或权限变更前必须重新查询外部系统。

## 一句话结论

`Bzs.Blazor 0.1.0` 已通过 GitHub Actions OIDC Trusted Publishing 发布到 NuGet.org；发布控制加固已合入 `master`，package static-assets/integrity 校验、完整本地 release gate，以及 CI 使用的 Node 24 action pins 均已通过 Ubuntu/Windows hosted CI。发布专用的 `download-artifact v8` 只完成 SHA/tag/runtime 静态核对，等待下一次正式版本发布动态验证。`v0.1.0` 必须保持不变，下次发布只能使用新版本和新 tag。

## 最终状态快照

生成本文前工作区是 clean；生成本文后只有本文件是预期的新文件，尚未提交或 push。

| 项目 | 值 |
| --- | --- |
| 分支 | `master` |
| 快照 HEAD | `8b30ebc78524657d30bf09cd2e4180274437075f` |
| 快照 `origin/master` | `8b30ebc78524657d30bf09cd2e4180274437075f` |
| 加固实现 commit | `f091881d72e7ece40bff66751e959d59612879df` |
| 加固状态同步 commit | `8b30ebc78524657d30bf09cd2e4180274437075f` |
| `v0.1.0` tag object | `dc393f99be0f9d127bec046ac3cf53f4a9bb36d3` |
| `v0.1.0` peeled commit | `e737f182fc99b487958af092b29c764a9ee75280` |
| NuGet | `Bzs.Blazor 0.1.0`, listed |
| NuGet published time | `2026-07-19T12:42:09.003Z` |
| GitHub Release object | 不存在，tag endpoint 返回 HTTP 404 |

`master` 领先发布 tag 是预期状态：发布结果文档、发布控制加固和交接状态同步都发生在首发 tag 之后。不要用移动 tag 的方式让 tag 追上 `master`。

## 已完成事项

### 0.1.0 首发

- 通过 annotated tag `v0.1.0` 固定发布源码 `e737f182...`。
- GitHub Actions 使用 `NuGet/login` 和 OIDC 兑换短期 NuGet key。
- 主包和 `.snupkg` 均成功发布。
- 没有保留长期 `NUGET_API_KEY` 作为自动 fallback。
- NuGet flat-container、registration 和 search API 均能查询 `0.1.0`。
- 使用仅官方源、独立 package cache 的全新 Blazor Web App 成功 restore/build。

### 发布授权决策

- **受控 push 一个合法发布 tag，就是最终人工发布授权。**
- `nuget-production` 不增加 required reviewer。
- tag push 后，两端 release gates 成功即自动进入 OIDC login 和 NuGet push。
- 该决策记录在 `docs/adr/0024-authorize-nuget-publication-through-controlled-tag-pushes.md`。

### Node 24 actions

所有 action 继续固定完整 commit SHA：

| Action | 版本 | SHA | 验证状态 |
| --- | --- | --- | --- |
| `actions/checkout` | `v7.0.0` | `9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0` | Ubuntu/Windows CI 已运行 |
| `actions/setup-dotnet` | `v6.0.0` | `a98b56852c35b8e3190ac28c8c2271da59106c68` | Ubuntu/Windows CI 已运行 |
| `actions/upload-artifact` | `v7.0.1` | `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | Ubuntu/Windows CI 已运行 |
| `actions/download-artifact` | `v8.0.1` | `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` | tag workflow 未重跑；tag/SHA/runtime 已静态核对 |
| `NuGet/login` | `v1.2.0` | `8d196754b4036150537f80ac539e15c2f1028841` | 首发旧 pin 已运行；当前 SHA/tag/runtime 已静态核对 |

对应 action metadata 都声明 `node24`。最终 CI 日志没有 Node 20 deprecation warning。

### Package static-assets/integrity

`scripts/verify-release.ps1` 现在把真实 `.nupkg` 和消费者生成的 endpoint manifest 传给 package-consumer tests：

- source consumer 使用 `staticwebassets.build.endpoints.json`；
- published consumer 使用 `staticwebassets.publish.endpoints.json`；
- 枚举 `.nupkg` 内全部 CSS/JS，包括全局 CSS、scoped CSS bundle 和 collocated JS；
- 忽略 gzip/Brotli 派生 endpoint，避免把压缩字节与包内原始 entry 混算；
- 对每个原始 ZIP entry 计算 `sha256-<base64>`；
- 缺 package asset、缺 endpoint metadata、缺 integrity、冲突 integrity 或 hash 不匹配均 fail closed。

专项测试共 7 个，覆盖正确 hash、selector/header 压缩表示、缺 metadata、metadata 指向缺失 asset、缺 integrity、hash 不匹配和冲突 integrity。

## 产品边界

- `.NET 10` Razor Class Library。
- 公共包、assembly 和根命名空间都是 `Bzs.Blazor`。
- 核心包没有第三方 UI runtime dependency。
- 支持 Interactive Server、Interactive WebAssembly 和 Interactive Auto。
- 被动组件在 static SSR 下保留有意义的 HTML。
- 消费应用选择 render mode，并负责主题偏好持久化。
- CoreApi/CoreApi.Client 只是 Reference Application，不得成为 runtime、domain 或 project dependency。
- 视觉语言是克制的 neumorphism，不做全局重阴影或第三方 DOM 覆盖。

`0.1.0` 公共交付面包括 Foundation、Forms、Feedback、Overlay 和 Tabs；实际 tab item 类型是 `BzsTabItem`。DataGrid、Tree、Scheduler、Charts、MultiSelect、ContextMenu、Popover、Sidebar、upload、media viewer、Radzen adapter 和业务复合组件不属于首版。

## 当前 CI/CD 合同

常规 CI 由 `master` push/PR 触发：

```text
Ubuntu release gate
├── .NET SDK + wasm-tools
├── unit/component tests
├── Chromium/Firefox/WebKit browser coverage
├── pack + package inspection
├── source package consumer + build manifest integrity
├── trimming + WASM AOT
└── published package consumer + publish manifest integrity

Windows branded-browser gate
├── visual regression baselines
└── Chrome + Edge browser matrix
```

发布 workflow 由 `v*.*.*` tag push 触发：

1. 严格解析带 `v` 前缀的 SemVer。
2. 要求 tag version 与项目唯一 `Version` 完全一致。
3. Ubuntu/Windows 从 tagged source 重跑全部必需门禁。
4. `publish` job 等待两端成功并只下载已验证 artifact。
5. `nuget-production` environment 进入 OIDC 信任边界。
6. `NuGet/login` 兑换短期 key。
7. 主包以 `--no-symbols` 发布，再显式发布 `.snupkg`。

## 验证证据

### 首发

- [CI run 29686673611](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29686673611)：发布源码 `e737f182...`，成功。
- [Publish NuGet run 29687047745](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29687047745)：`v0.1.0`，OIDC、主包和 symbols 成功。
- [CI run 29687905506](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29687905506)：发布结果文档 commit `c31d912...`，成功。

### 加固实现

- [CI run 29717988747](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29717988747)：commit `f091881...`，成功。
- Release gate：约 `13m39s`。
- Windows branded browsers：约 `4m36s`。
- Source package consumer：`19/19`。
- Published package consumer：`19/19`。
- AOT：39 个 assembly，包括 `Bzs.Blazor.dll`。
- 无 `IL2xxx`、`IL3050` 或 Node 20 deprecation warning。
- 同一 SHA 只触发 CI，没有触发 Publish NuGet。

### 最终 master

- [CI run 29718590336](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29718590336)：commit `8b30ebc...`，成功。
- Release gate：约 `12m37s`。
- Windows branded browsers：约 `5m38s`。
- Source package consumer：`19/19`。
- Published package consumer：`19/19`。
- AOT：39 个 assembly。
- 完整 release verification 成功。
- 同一 SHA 只有 CI run，没有 Publish NuGet run。

本地完整 gate 也已通过：unit/component `107/107`、Chromium（含视觉回归）`36/36`、7 个 browser-matrix 目标、source/published package consumer 各 `19/19`、39 assembly AOT，且未跳过 visual regression、browser matrix 或 AOT。

## 当前外部权限状态

`nuget-production` environment API 的实时结果：

- `can_admins_bypass=true`；
- `protection_rules` 只有 `branch_policy`；
- `protected_branches=false`；
- `custom_branch_policies=true`；
- 已记录的 deployment policy 是 tag pattern `v*.*.*`；
- 没有 required reviewer。

仓库 rulesets API 返回 HTTP 403：当前 private repository 需要升级 GitHub Pro 或改为 public 才能通过该接口查看。因此，**tag 创建/更新权限是否真正受控仍无法从 API 证明**。这不是“没有 ruleset”的证据，只是可见性被阻断。

## 未完成事项

### P1：发布前证明 tag 权限边界

当前策略把 tag push 视为最终授权，所以发布前必须确认谁能创建或更新 `v*` tag。若当前计划或 UI 不能提供可验证证据，不要 push 发布 tag。

当前 workflow 也没有自动验证：

- tag peeled commit 等于最新 `origin/master`；
- tag commit 等于预期 release SHA；
- 同一 SHA 的 branch CI 已成功。

这些仍是人工发布步骤。真正抵御有写权限主体绕过人工检查，需要一个不能被普通发布主体取消或修改的保护边界。

### P2：symbols-only 恢复入口

当前 workflow 先 push 主包，成功后再 push `.snupkg`。如果主包成功而 symbols 失败，完整 rerun 会在主包 HTTP 409 处停止，无法到达 symbols push。

后续需要一个受 `nuget-production` 和 OIDC 保护的 symbols-only 入口，按原 run ID 下载精确 artifact。入口完成前遇到部分成功必须停止并单独评审，不要直接 rerun，也不要使用 `--skip-duplicate` 掩盖状态。

NuGet.org 会给主包增加 `.signature.p7s` repository signature，所以远端 nupkg 与原 artifact 整包 hash 不同是正常现象。恢复核验必须：

- 验证 repository signature；
- 比较 package ID/version/repository commit；
- 比较除 `.signature.p7s` 外的逐条目内容。

### P2：决定 lock file 和 NuGet cache 策略

仓库当前没有 `packages.lock.json`。先确定 locked restore 并验证 `dotnet restore --locked-mode`，再考虑 `setup-dotnet cache: true`；不要只 hash `*.csproj` 作为 cache key。

### P3：只在真实成本出现时拆分 release gate

统一 release script 目前同时提供顺序控制、package/AOT 验证和证据汇总。只有耗时或定位成本成为真实问题时再拆分；发布 job 仍必须依赖全部必需门禁。

## 下一个版本发布 Runbook

1. 在 `master` 更新 `src/Bzs.Blazor/Bzs.Blazor.csproj` 中唯一的 `Version`。
2. 更新 release notes、Breaking Changes 和必要 package metadata。
3. 运行 `pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release`。
4. 确认 visual regression、browser matrix 和 WASM AOT 都没有 skipped。
5. 确认无 `IL2xxx`/`IL3050`，source/published package consumer 与 integrity 校验通过。
6. 提交并 push `master`，等待目标 commit 的 Ubuntu/Windows branch CI 成功。
7. 记录精确 release SHA。
8. 在最新 `origin/master` 上创建 annotated candidate tag，但先不要 push。
9. `git fetch origin master --tags`。
10. 要求 candidate tag peeled commit、release SHA 和 `origin/master` 完全相等。
11. 用 `gh run list --commit <sha> --workflow CI` 确认同一 SHA 的 CI 成功。
12. 确认 tag version 与项目 `Version` 完全一致。
13. 复核 `nuget-production` environment、tag deployment policy 和实际 tag 创建/更新权限。
14. 明确确认这是有意的正式发布。
15. 将 `git push origin <tag>` 视为最终人工授权；push 后不会等待额外审批。
16. 等待发布 workflow 的 Ubuntu/Windows gates、OIDC login、主包和 symbols 全部成功。
17. 等待 NuGet flat-container、registration 和 search API 传播。
18. 使用独立 cache、仅 nuget.org 的全新消费者 restore/build。
19. 记录 run URL、tag object、peeled commit、NuGet 页面和消费证据。

## 接手检查命令

```powershell
git status --short --branch
git log -5 --oneline --decorate
git rev-parse HEAD
git rev-parse origin/master
git rev-parse 'v0.1.0^{}'
git ls-remote origin refs/heads/master refs/tags/v0.1.0 'refs/tags/v0.1.0^{}'

gh run view 29717988747
gh run view 29718590336
gh run list --commit 8b30ebc78524657d30bf09cd2e4180274437075f

gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production `
    --jq '{can_admins_bypass,protection_rules,deployment_branch_policy}'
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production/deployment-branch-policies `
    --jq '.branch_policies[] | {name,type}'

Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/bzs.blazor/index.json
Invoke-RestMethod https://api.nuget.org/v3/registration5-gz-semver2/bzs.blazor/index.json
```

只在需要重新验证代码时运行完整本地 gate：

```powershell
pwsh -NoProfile -File scripts/verify-release.ps1 -Configuration Release
```

## 关键文件

- `CONTEXT.md`：领域语言和产品边界。
- `docs/adr/0024-authorize-nuget-publication-through-controlled-tag-pushes.md`：tag push 最终授权决策。
- `docs/specs/2026-07-18-bzs-blazor-v0.1.md`：已发布 v0.1 规格。
- `docs/releases/0.1.0.md`：首版 release notes 和安装入口。
- `docs/nuget-trusted-publishing-research-2026-07-19.md`：OIDC、environment 和恢复边界。
- `.github/workflows/ci.yml`：常规 CI。
- `.github/workflows/publish-nuget.yml`：tag 发布 workflow。
- `scripts/verify-release.ps1`：统一 release gate。
- `tests/Bzs.Blazor.PackageConsumerTests/StaticWebAssetIntegrityVerifier.cs`：integrity 验证实现。
- `tests/Bzs.Blazor.PackageConsumerTests/StaticWebAssetIntegrityVerifierTests.cs`：integrity 专项测试。
- `docs/handoffs/2026-07-19-bzs-blazor-v0.1.0-release-handoff.md`：首发全过程详细证据。

## 明确禁止事项

- 不移动、覆盖或删除 `v0.1.0`。
- 不重新发布 `Bzs.Blazor 0.1.0` 主包。
- 不为验证 workflow 创建测试发布 tag。
- 不把 `--skip-duplicate` 作为正式发布或恢复默认项。
- 不恢复长期 NuGet API key 作为 OIDC 静默 fallback。
- 不把 CoreApi/CoreApi.Client、MudBlazor 或 Radzen 引入 core runtime dependency。
- 不把 render mode 或主题持久化策略放进组件库。
- 不为规避 trim/AOT warning 添加无依据 suppression。
- 不把被忽略的 `artifacts/` 当作唯一长期发布证据。
