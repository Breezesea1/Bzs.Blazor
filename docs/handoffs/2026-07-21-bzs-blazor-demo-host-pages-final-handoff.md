# Bzs.Blazor Demo Host、Aspire 与 GitHub Pages 上线最终交接

- 生成时间：2026-07-21（UTC+08:00）
- 仓库：`git@github.com:Breezesea1/Bzs.Blazor.git`
- 仓库 URL：<https://github.com/Breezesea1/Bzs.Blazor>
- 当前可见性：public
- 静态 Demo：<https://breezesea1.github.io/Bzs.Blazor/>
- NuGet：<https://www.nuget.org/packages/Bzs.Blazor/0.1.0>
- 前序发布与加固基线：`docs/handoffs/2026-07-20-bzs-blazor-v0.1.0-release-hardening-final-handoff.md`

本文承接 `v0.1.0` 首发和发布加固交接，记录 Demo 托管结构、Aspire 入口、GitHub Pages 首次上线，以及仓库公开化后的运维和权限边界。文中的外部系统状态均为生成本文时的快照；发布、权限或站点故障处置前必须重新查询。

## 一句话结论

`Bzs.Blazor` 的本地/服务器 Demo 已由 Aspire AppHost 统一启动，静态 Demo 已作为独立的 Interactive WebAssembly 应用发布到 GitHub Pages。共享示例内容位于 `Bzs.Blazor.Demo.Catalog`，因此 Aspire 主机与静态主机不会复制页面实现。仓库现为 public，Pages 使用 GitHub Actions 成功发布；这也意味着源码、提交历史和所有未来提交默认对公众可见。

## 最终状态快照

生成本文前工作区为 clean；保存本文后本文件是预期的唯一工作区改动，尚未提交或 push。

| 项目 | 值 |
| --- | --- |
| 分支 | `master` |
| 快照 HEAD | `4f8a1c6be3c5556c43f68018313129bb7353ade3` |
| 快照 `origin/master` | `4f8a1c6be3c5556c43f68018313129bb7353ade3` |
| ahead / behind | `0 / 0` |
| Demo/Pages 实现 commit | `4f8a1c6 feat: add Aspire and Pages demo hosts` |
| 实现规模 | 55 files changed，`+886/-18` |
| 仓库可见性 | `PUBLIC`，默认分支 `master` |
| Pages build type | `workflow` |
| Pages source | `master`，path `/` |
| Pages HTTPS | enforced |
| Pages URL | <https://breezesea1.github.io/Bzs.Blazor/> |
| CI / Pages 关系 | 同一 `master` push 并行触发；Pages 不等待 CI |
| `master` branch protection | 不存在；endpoint 返回 `Branch not protected` |
| repository rulesets | 空列表 |
| 最新 Pages run | [29794153123](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29794153123)，success |
| 最新 CI run | [29761669925](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29761669925)，success |

`4f8a1c6` 位于 `v0.1.0` 发布后；它不改变已经发布的 package、tag 或 NuGet 版本。`v0.1.0` 必须继续保持不变，下一次 package 发布只能使用新的版本和新的 tag。

## 这次完成的内容

### Demo 主机拆分

Demo 的内容与主机职责已经明确拆开：

| 项目 | 责任 | 运行边界 |
| --- | --- | --- |
| `Bzs.Blazor.Demo.Catalog` | 共享 catalog 页面、组件示例、行为和样式 | 不拥有任何托管或 render-mode 决策 |
| `Bzs.Blazor.Demo.Client` | 供服务器 Demo 使用的 WebAssembly client，以及 Interactive Auto/WebAssembly 的 route wrapper | 浏览器目标；不拥有 Interactive Server 或 static SSR 页面 |
| `Bzs.Blazor.Demo` | ASP.NET Core Demo 主机 | static SSR 和 Interactive Server 页面，以及与 Client 协作的 Interactive Auto/WebAssembly 示例入口 |
| `Bzs.Blazor.Demo.WebAssembly` | 独立静态 Demo 主机 | 全局 Interactive WebAssembly；没有 ASP.NET Core server、API、认证或数据库 |
| `Bzs.Blazor.Demo.AppHost` | Aspire 编排入口 | 注册并启动 `bzs-demo` 服务器 Demo |

结构关系如下：

```text
Aspire AppHost
  └─ bzs-demo (Bzs.Blazor.Demo, server-hosted demo)
       └─ Demo.Client render-mode wrappers
            └─ Demo.Catalog shared specimens

GitHub Pages
  └─ Demo.WebAssembly (standalone static host)
       └─ Demo.Catalog shared specimens
```

这不是把服务器 Demo 复制为静态站点：Pages 只承载浏览器内可运行的 WebAssembly 目录。静态 SSR、Interactive Server 和 Interactive Auto 的行为仍必须通过 Aspire 启动的 `bzs-demo` 验证。

### Aspire 入口

仓库根目录的 `aspire.config.json` 指向 `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.AppHost/Bzs.Blazor.Demo.AppHost.csproj`。AppHost 创建 distributed application，并以 `http` launch profile 注册 `bzs-demo`。

从仓库根目录启动：

```powershell
aspire run
```

需要并行、隔离的本地实例时使用：

```powershell
aspire run --isolated
```

不要把 Aspire Hosting 引入 `src/Bzs.Blazor`。AppHost 是 demo 编排层，核心 package 仍是没有第三方 UI runtime dependency 的 .NET 10 Razor Class Library。

### GitHub Pages 静态宿主

`.github/workflows/deploy-demo-pages.yml` 在 `master` push 或手动 `workflow_dispatch` 时运行：

1. 发布 `Bzs.Blazor.Demo.WebAssembly` 到 `artifacts/pages`。
2. 执行 `scripts/prepare-github-pages.ps1`。
3. 校验 `index.html` 的根 base tag，再改写为 `<base href="/Bzs.Blazor/" />`。
4. 生成同内容的 `404.html` 和 `.nojekyll`。
5. 上传 Pages artifact，并在 `github-pages` environment 中部署。

workflow 对常规 push 的执行条件是 repository 非 private。当前仓库已公开，因此今后的 `master` push 会自动触发 Pages 发布；`workflow_dispatch` 可用于显式重发当前分支。

权限保持最小化：build job 只有 `contents: read` 与 `pages: read`，deploy job 只有 `pages: write` 与 `id-token: write`。不要为了调试而扩大为全局 `write-all`。

**CI 不是当前 Pages workflow 的部署前置门禁。** `ci.yml` 和 `deploy-demo-pages.yml` 都独立监听 `master` push，Pages deploy 只等待自身的静态 build job。因此，一个提交可能在完整 CI 约十余分钟后失败之前，已于约两分钟内被发布到 Pages。当前 `master` 也没有 branch protection。将最新的同 SHA CI 成功视为上线后的验证证据，而不是当前 workflow 强制执行的部署许可；若产品需要“只发布已通过 CI 的 SHA”，必须另行修改 CI/CD 设计，而不能只依赖本 handoff 的运行约定。

## 在线行为与验证证据

### 当前远端 workflow

- [CI run 29761669925](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29761669925)：`4f8a1c6...`，成功。
  - `Release gate` 成功，约 13m36s。
  - `Windows branded browsers` 成功，约 6m27s。
  - CI 仍在 `master` push 和 PR 上触发，覆盖 release gate、视觉回归和 Chrome/Edge browser matrix。
- [Deploy demo to GitHub Pages run 29794153123](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29794153123)：`4f8a1c6...`，手动 dispatch，成功。
  - [Build static demo job](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29794153123/job/88521957602)：发布静态 Demo、准备 base path、配置 Pages、上传 artifact 全部成功。
  - [Deploy static demo job](https://github.com/Breezesea1/Bzs.Blazor/actions/runs/29794153123/job/88522119459)：部署 `github-pages` environment 成功。

本次推送产生的 push Pages run `29761669855` 被有意跳过，因为当时仓库仍为 private；仓库公开并启用 Pages 后，手动 run `29794153123` 成为了实际首次发布证据。不要把该 skipped run 当作部署失败。

### 浏览器和 HTTP 验证

上线后已在真实 Pages URL 验证：

- 根路径 `https://breezesea1.github.io/Bzs.Blazor/` 返回 HTTP `200`，页面标题为 `Bzs.Blazor catalog`。
- 直接打开 `/forms` 能加载 Forms catalog，页面显示 `Interactive runtime ready` 和完整的表单控件。
- 直接打开 `/render-modes/webassembly` 能加载 WebAssembly catalog，页面显示 `Interactive runtime ready`；交互计数器已从 `Interaction count: 0` 成功变为 `Interaction count: 1`。
- 页面 console 没有应用错误。
- 任意未知路径可由 `404.html` 回退进入静态应用并显示其应用内 NotFound 页面。

**重要：GitHub Pages 对 SPA 深链接的 HTTP 语义。** 直接 `curl` `/forms` 或未知路径会得到 HTTP `404`，即使浏览器已收到 `404.html`、启动 WebAssembly 并正确渲染该路由。对此静态 SPA，不能用“深链接必须返回 HTTP 200”作为健康判据；应在浏览器中验证 route 实际渲染。根路径仍应保持 HTTP `200`。

本次代码提交前已完成本地验证：Release build 0 warnings/errors，unit/component `107/107`，browser `36/36`，独立 WebAssembly publish、trimming/native assets 和 Pages fallback 均已检查。保存本文没有重新运行完整本地 release gate；文档改动不改变运行时或 workflow 行为。

## 公开仓库后的权限与安全边界

仓库从 private 改为 public 是为当前 GitHub Pages 方案所作的明确运营决策。其直接后果如下：

- 所有当前和未来 Git source、issue/PR 元数据以及默认可见的 Actions 信息都可能被公众访问。
- Demo 不能包含真实客户数据、内部 endpoint、credential、dashboard token、私有 URL 或可识别的生产数据。
- 每次新增示例、截图、workflow log 或测试 fixture 前，都应按“公开输出”审查。
- GitHub Pages 是公开静态站点；不要把访问控制、秘密、服务器授权、文件写入或数据库能力设计进该 host。
- 当前已公开的全部 commit 和 `v0.1.0` annotated tag 元数据包含个人 Git author/tagger email。这个暴露已经进入公开历史；不要为此例行重写历史或移动 release tag。后续提交应先决定并配置 GitHub noreply email 或已接受的公开邮箱；若要处理既有历史，必须作为单独、审计过的迁移，并评估 tag、NuGet provenance 和所有 clone 的影响。

### NuGet 发布权限仍需维护

前序 handoff 中，repository rulesets API 因 private plan 限制返回 403，无法判断 tag 保护情况。仓库公开后，这个 API 已可查询，且当前返回空列表：**没有可见的 repository-level ruleset。**

`nuget-production` environment 仍有 `branch_policy` protection rule，允许 tag pattern `v*.*.*`；它只限制哪些 ref 能进入部署 environment，**不等价于限制谁能创建、移动或更新发布 tag**。`can_admins_bypass=true` 也仍为真。

因此，发布前的 P1 没有消失，反而有了更清晰的操作结论：在把受控 tag push 视为最终发布授权之前，需要在 GitHub 仓库设置中建立并验证 tag protection/ruleset，确保普通写权限主体不能随意创建或更新 `v*` tag。未完成前，不要为验证 workflow 创建或 push 测试发布 tag。

Pages workflow 的 `github-pages` environment 与 NuGet 的 `nuget-production` 是不同边界。Pages 发布成功不代表 NuGet 发布授权、OIDC 信任策略或 tags 已被加固。

## 接手运维 Runbook

### 修改 Demo 或组件后

1. 在本地通过 Aspire 验证服务器 Demo 需要覆盖的 render mode。
2. 如修改共享示例，同时检查 `Demo.Catalog` 在 `Demo.Client` 和 `Demo.WebAssembly` 两个主机中的行为。
3. 对静态主机执行 publish，并确认 `index.html` 原始 base tag 仍为 `/`，由准备脚本改写；不要在源文件中硬编码 `/Bzs.Blazor/`。
4. 运行与改动风险相称的 build、unit/browser tests；发布前仍按前序 handoff 执行完整 `scripts/verify-release.ps1`。
5. push `master` 后认识到 Pages 和 CI 会并行运行：在同 SHA CI 成功前，将已发布的 Pages 内容视为未完成验证；若 CI 失败，立即处置或回滚相应 `master` 提交。
6. 在同一 SHA 的 CI 和 Pages run 都成功后，用浏览器访问根路径、一个已知深链接（例如 `/forms`）和一个未知路径，不要只依赖 curl 深链接状态码。

本地静态 publish 与 Pages 准备的可复现命令：

```powershell
dotnet publish `
  samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/Bzs.Blazor.Demo.WebAssembly.csproj `
  --configuration Release `
  --output artifacts/pages

pwsh -NoProfile -File scripts/prepare-github-pages.ps1 `
  -PublishedWwwroot artifacts/pages/wwwroot `
  -BasePath /Bzs.Blazor/
```

### 重新检查线上状态

```powershell
git status --short --branch
git rev-parse HEAD
git rev-parse origin/master
git rev-list --left-right --count HEAD...origin/master

gh repo view Breezesea1/Bzs.Blazor --json nameWithOwner,visibility,isPrivate,url,defaultBranchRef
gh api repos/Breezesea1/Bzs.Blazor/pages `
  --jq '{public,build_type,source,https_enforced,html_url}'

gh run view 29794153123
gh run view 29761669925
gh run list --workflow deploy-demo-pages.yml --limit 10

curl.exe -sS -o NUL -w "root %{http_code} %{url_effective}\n" `
  -L https://breezesea1.github.io/Bzs.Blazor/
```

发布权限复核：

```powershell
gh api repos/Breezesea1/Bzs.Blazor/rulesets
gh api repos/Breezesea1/Bzs.Blazor/branches/master/protection
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production `
  --jq '{can_admins_bypass,protection_rules,deployment_branch_policy}'
gh api repos/Breezesea1/Bzs.Blazor/environments/nuget-production/deployment-branch-policies `
  --jq '.branch_policies[] | {id,name,type}'
```

## 明确禁止事项

- 不把 `Bzs.Blazor.Demo.WebAssembly` 当成服务器 Demo，也不向它加入 server-only dependency、认证、API、数据库或 `HttpContext` 使用。
- 不把共享 catalog 页面复制回两个 host；共享示例应继续留在 `Bzs.Blazor.Demo.Catalog`。
- 不删除 Pages 的 `/Bzs.Blazor/` base-path 改写、`404.html` fallback 或 `.nojekyll` marker。
- 不将 Pages 深链接的 HTTP 404 误判为浏览器路由故障；先检查实际渲染。
- 不把 Pages 部署成功误写成“已通过 CI 才发布”；在当前 workflow 下二者并行，且 `master` 没有 branch protection。
- 不在 public repo、Pages artifact、截图或日志中提交秘密、token、生产数据或内部地址。
- 不为移除已公开的 author/tagger email 轻率重写历史、移动 `v0.1.0` 或破坏 package provenance。
- 不将 Pages 成功视为 NuGet 生产发布授权的证明。
- 不移动、覆盖或删除 `v0.1.0`，也不为验证发布 workflow 创建测试 tag。
- 不以 `--skip-duplicate` 掩盖 NuGet 主包/符号包部分成功的恢复问题。

## 关键文件

- `aspire.config.json`：仓库默认 Aspire AppHost。
- `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.AppHost/AppHost.cs`：`bzs-demo` 的 Aspire 注册。
- `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/`：两种 Demo host 共用的示例内容。
- `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Client/`：服务器 Demo 的 render-mode wrapper。
- `samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/`：Pages 独立静态 host 及其约束说明。
- `scripts/prepare-github-pages.ps1`：base path、SPA fallback、`.nojekyll` 准备。
- `.github/workflows/deploy-demo-pages.yml`：静态 Demo 的 CI/CD 定义。
- `.github/workflows/ci.yml`：`master`/PR 的常规验证门禁。
- `.github/workflows/publish-nuget.yml`：严格 SemVer tag 的 NuGet 发布 workflow。
- `docs/handoffs/2026-07-20-bzs-blazor-v0.1.0-release-hardening-final-handoff.md`：package 发布、OIDC、integrity、symbols 恢复和完整 release runbook。
