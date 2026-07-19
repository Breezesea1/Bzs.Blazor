# NuGet.org Trusted Publishing 与 GitHub Actions 调研

查询日期：2026-07-19（UTC+08:00）。本文只引用 NuGet/Microsoft 与 GitHub 的官方文档和 NuGet 官方 Action 仓库。事实以查询日为准；“建议”和“风险判断”是针对本仓库 `.github/workflows/publish-nuget.yml` 的分析，不是服务方承诺。

## 结论摘要

**NuGet.org Trusted Publishing 支持直接连接 GitHub Actions。** GitHub Actions 为发布 job 签发 OIDC JWT，`NuGet/login` 将它提交给 NuGet.org token service；NuGet.org 按预先登记的 Trusted Publishing policy 校验后返回有效期 1 小时的临时 API key。[N1](#N1) [N2](#N2)

当前 workflow 可以迁移，而且不需要改变验证、打包或 environment approval 的总体结构。最小改造是：

1. 在 NuGet.org 为 `Breezesea1/Bzs.Blazor` 建立 GitHub Trusted Publishing policy，workflow 填 `publish-nuget.yml`，environment 填 `nuget-production`。
2. 将 `publish` job 的 `permissions: {}` 改为仅 `id-token: write`。
3. 在实际 push 前增加固定 commit SHA 的 `NuGet/login`，用 NuGet.org 用户名兑换短期 key。
4. push 改用 login step 的 `NUGET_API_KEY` 输出；验证成功后删除 GitHub environment 中的长期 `NUGET_API_KEY` secret。
5. 保留 `nuget-production` 的 required reviewers 和 deployment branch/tag protection，并明确处理已发布版本的 rerun。

实施状态：本仓库的工作区已按上述方案切换为 `NuGet/login` OIDC 登录，并从
`nuget-production` environment variable `NUGET_USERNAME` 读取 NuGet.org profile
username。服务端 Trusted Publishing policy 和 GitHub environment 的实时配置仍需由
仓库所有者在对应 UI 中完成。

NuGet policy 页面没有独立的 branch 或 tag 字段；当前 `on.push.tags`、严格 tag/version 校验、GitHub environment deployment rules 和受保护 tag 创建权限因此仍是安全边界，不能因启用 OIDC 而删除。[N1](#N1) [G1](#G1) [G2](#G2)

## 官方工作原理

Trusted Publishing 的链路是：

1. 发布 job 获得 `id-token: write`，由 GitHub OIDC provider 签发 JWT。该权限只允许请求 OIDC token，不授予仓库或其它资源的写权限。[G1](#G1) [G2](#G2)
2. `NuGet/login` 请求 audience 为 `https://www.nuget.org` 的 OIDC token，提交到默认 token endpoint `https://www.nuget.org/api/v2/token`。[N1](#N1) [N2](#N2)
3. NuGet.org 验证 JWT 及 Trusted Publishing policy，返回临时 API key。临时 key 有效 1 小时；一个 OIDC token 只能兑换一个临时 key，所以应在发布前临近请求，不能缓存到后续 run。[N1](#N1)
4. `dotnet nuget push` 仍通过 `--api-key` 接收该短期 key。Trusted Publishing 消除的是长期 secret，不是 NuGet push 命令的 API-key 参数协议。[N1](#N1) [N3](#N3)

官方推荐 `NuGet/login@v1`，其输入 `user` 是创建 policy 的 **NuGet.org profile username**，不是邮箱，也不一定等于 GitHub owner；输出名仍是 `NUGET_API_KEY`。这里的输出只是 job 内短期凭证，不是需要预存的 repository/environment secret。[N1](#N1) [N2](#N2)

## NuGet.org 侧配置

在 NuGet.org 账号的 Trusted Publishing 页面创建 GitHub policy。官方页面列出的匹配字段如下：[N1](#N1)

| NuGet 字段 | 本仓库建议值 | 说明 |
| --- | --- | --- |
| Repository Owner | `Breezesea1` | 个人或组织 owner；不填 URL。提交前应以 GitHub 当前 owner 为准。 |
| Repository | `Bzs.Blazor` | 不含 owner。 |
| Workflow File | `publish-nuget.yml` | 官方要求只填文件名，不含 `.github/workflows/`。 |
| Environment | `nuget-production` | 可选，但当前 publish job 已使用该 environment，应填写以缩小信任范围。 |
| （不属于 policy 字段）NuGet login `user` | 创建 policy 的实际 NuGet.org profile username | 这是 workflow 中 `NuGet/login` 的 `user:` 输入，不要把它误填到 policy 表单，也不要填邮箱。 |

官方配置说明中**没有 branch 或 tag policy 字段**。因此不能在 NuGet UI 里登记 `master`、`v*.*.*` 或单个 tag；这些限制由 GitHub workflow trigger、workflow 内校验、environment deployment branch/tag rules 和受保护 tag 权限共同完成。[N1](#N1) [G1](#G1) [G2](#G2)

其他 NuGet 生命周期约束：组织 policy 在创建者离开组织时会 inactive；private repository 的 policy 可能先临时 active 7 天，若未成功发布会 inactive。首次成功发布后，NuGet.org 会获得 repository/owner ID，使 policy 能抵抗删除后以同名仓库重建的 resurrection attack。[N1](#N1)

## GitHub Actions 侧配置

### 权限与 login

迁移前的 `publish` job 显式使用 `permissions: {}`，因此无法请求 OIDC token。当前实现已只在 `publish` job 使用：

```yaml
permissions:
  id-token: write
```

该 job 不 checkout 源码，现有步骤也不调用 GitHub API，所以不需要 `contents: read`。顶层 `contents: read` 可继续服务 `verify` job；job 级 `permissions` 会覆盖它。[G1](#G1) [G2](#G2)

在下载并检查 artifact 后、push 前增加 login，且沿用本仓库所有 Actions 固定完整 commit SHA 的供应链约定：

```yaml
- name: Log in to NuGet.org with OIDC
  id: nuget-login
  uses: NuGet/login@8d196754b4036150537f80ac539e15c2f1028841 # v1 peeled commit, verify before merging
  with:
    user: <NUGET_ORG_PROFILE_USERNAME>
```

以上 SHA 是查询日 `NuGet/login` 官方仓库 annotated `v1` tag 的 peeled commit（tag object SHA 与 commit SHA 不同）。正式改造时仍应重新核对 tag、commit 和 action source，再由依赖更新流程维护。[N2](#N2)

### push 与 `NUGET_API_KEY`

当前实现将 login step 输出只注入发布步骤：

```yaml
env:
  NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}
  PACKAGE_VERSION: ${{ needs.verify.outputs.version }}
```

现有 `dotnet nuget push ... --api-key $env:NUGET_API_KEY` 可保留。迁移完成并成功做一次受保护发布后，应删除 GitHub environment 中的长期 `NUGET_API_KEY` secret；workflow 不再需要它。若需要回滚，重新建立 scoped NuGet API key 是显式应急动作，不应长期并存为静默 fallback，因为 fallback 会掩盖 policy 配置错误并恢复长期凭证风险。[N1](#N1) [N3](#N3)

### 符号包

NuGet.org 接受 `.snupkg` 并要求使用 V3 endpoint `https://api.nuget.org/v3/index.json`；官方符号包文档允许分别 push `.nupkg` 与 `.snupkg`。[N4](#N4)

当前 workflow 先以 `--no-symbols` push `.nupkg`，再显式 push 匹配的 `.snupkg`。这不会隐式重复推送 symbols，因为 `--no-symbols` 明确关闭第一条命令的 symbol push；迁移到 Trusted Publishing 后可继续用同一个短期 key 执行两条命令。[N3](#N3) [N4](#N4)

建议暂时保留当前显式两阶段方式，因为它能分别报告 package 与 symbols 的失败，且不依赖 CLI 的同目录自动发现。代价是主包成功而 symbol push 失败时会出现部分成功；NuGet 包版本不可覆盖，修复只能对同一版本重新推 symbols，不能重发主包。

## Environment、branch 与 tag 边界

GitHub OIDC token 包含 `repository`、`repository_id`、`repository_owner`、`workflow_ref`、`ref`、`ref_type`、`environment`、`run_id`、`run_attempt` 等 claims。[G2](#G2) NuGet policy 暴露的是 owner/repository/workflow/environment 的受支持子集，不应假定它会额外执行 workflow 中未登记的 branch/tag 规则。[N1](#N1)

当前 job 使用 `environment: nuget-production`。GitHub 默认 OIDC subject 在使用 environment 时以 environment context 为主；没有 environment 且不是 PR 时才分别形成 branch 或 tag context。因此不能把“OIDC token 里有 `ref`”等同于“NuGet policy 已限制到某个 tag”。[G2](#G2)

建议在 GitHub 仓库设置中确认：

- `nuget-production` 启用 required reviewers，避免任意满足 policy 的 run 自动发布。
- environment deployment rules 选择 `Selected branches and tags`，只允许与 `v*.*.*` 一致的 tag pattern（GitHub environment UI 的保护分支/标签选择与仓库 tag ruleset 是两层设置）。
- `v*` tag 的创建/更新权限受保护；否则能创建 tag 的账号可触发候选发布。
- 保留 workflow 的严格 SemVer、tag 与项目版本完全一致校验；它是 NuGet policy 不提供的业务约束。
- environment 名称大小写和 NuGet policy 中填写值保持一致，并确认实际 GitHub owner/repository 名称与 policy 匹配。

GitHub 官方明确建议：workflow 或 OIDC policy 使用 environment 时，应为 environment 配置 protection rules，并可限制允许部署的 branches/tags。[G1](#G1)

## Rerun 与幂等风险

GitHub rerun 使用最初触发 workflow 的 actor 权限，并沿用原始事件的 `GITHUB_SHA` 与 `GITHUB_REF`；run 在 30 天内可 rerun，单个 run 最多 50 次。[G3](#G3) 因此：

- 每次 rerun 必须重新执行 `NuGet/login`，请求新的 OIDC token 并兑换新的 1 小时临时 key；不能复用上一 attempt 的 token/key。[N1](#N1)
- rerun 仍指向原 tag/commit，但这并不保证发布幂等。主包已经成功时，再 push 同版本会失败。
- 当前 `concurrency` 只避免同一 ref 的并行 run；它不能把已经完成的发布变成可重入操作。
- 不建议对首次发布默认使用 `--skip-duplicate`，因为它可能把“这个版本已被其它 run 发布”降级为警告并掩盖发布所有权问题。
- 建议保留失败即停，并写明恢复手册：若主包已存在，先在 NuGet.org 验证包 hash/metadata 与该 run artifact 一致；一致时只重推缺失的 `.snupkg`，不一致时停止并调查。不要盲目 rerun 整个 publish job。

若团队明确选择“rerun 自动恢复”而接受上述可见性代价，可为两个 push 增加 `--skip-duplicate`；该选项按 NuGet push 文档将 HTTP 409 视为 warning。[N3](#N3) 这应作为显式发布策略决定，而不是 OIDC 迁移的必要步骤。

## 对当前 workflow 的具体改造顺序

1. **NuGet.org 先配置 policy**：`Breezesea1` / `Bzs.Blazor` / `publish-nuget.yml` / `nuget-production`，记录创建 policy 的 NuGet profile username。
2. **核对 GitHub environment**：required reviewers、deployment tag restrictions、受保护 tag 创建权限。先完成保护再授予 OIDC。
3. **最小 workflow patch**：把 publish job 改为 `id-token: write`；下载/检查 artifact 后执行 pinned `NuGet/login`；把输出注入现有 push step。
4. **保留现有 release gate**：tag/version 校验、完整测试、上传后再下载 verified artifact、两阶段 package/symbol push 均不需要因 OIDC 改动。
5. **做受控首发**：在 environment approval 后观察 login、主包和 symbols 三个阶段；不要输出 token，也不要启用会打印环境变量的 debug 脚本。
6. **移除长期 secret**：首发成功后删除 `nuget-production` 的 `NUGET_API_KEY` secret，并确认 workflow 不再引用 `secrets.NUGET_API_KEY`。
7. **记录恢复策略**：明确 package 成功/symbol 失败、login policy mismatch、重复版本三种情况；不要将旧 API key 留作自动 fallback。

## 不确定项与上线前检查

仓库文件无法证明 NuGet.org policy 或 GitHub environment 的实时配置。实施前需要在两个服务 UI 中确认 owner/repository、NuGet username、environment protection、deployment tag rules 和 policy active 状态。

本次没有真实调用 NuGet token endpoint，也没有发布测试包，因此无法验证当前账号是否有包 owner 权限、policy 是否 active 或 `nuget-production` 是否要求审批。`NuGet/login` 的 action tag SHA 也应在合并改造时重新核验。

## Sources

所有来源访问日期均为 2026-07-19。

- <a id="N1"></a>**[N1] NuGet.org Trusted Publishing（Microsoft Learn）**：https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- <a id="N2"></a>**[N2] NuGet 官方 OIDC login Action**：https://github.com/NuGet/login；实现：https://github.com/NuGet/login/blob/main/src/index.ts
- <a id="N3"></a>**[N3] `dotnet nuget push` 官方文档**：https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push
- <a id="N4"></a>**[N4] NuGet `.snupkg` 符号包官方文档**：https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg
- <a id="G1"></a>**[G1] GitHub 官方：为外部服务配置 OIDC**：https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-cloud-providers
- <a id="G2"></a>**[G2] GitHub 官方：OIDC claims、subject 与 workflow permissions**：https://docs.github.com/en/actions/reference/security/oidc
- <a id="G3"></a>**[G3] GitHub 官方：re-running workflows and jobs**：https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs
