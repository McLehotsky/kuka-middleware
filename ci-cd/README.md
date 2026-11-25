# Azure Pipelines CI

`ci.yml` (pipeline name `kuka-middleware-ci`) builds and checks the GitHub source on pushes to all branches and pull requests targeting `main`. It runs on a self-hosted Windows agent; the backend and frontend jobs have no dependency on each other but run sequentially on that one agent.

## Agent prerequisites

- A self-hosted Azure Pipelines agent registered in the agent pool **Default**, installed as a Windows service running as **Local System**.
- The agent PC must be switched on and the agent online, otherwise runs wait in the queue.
- Running as Local System is the team's choice; Microsoft's default service account is Network Service. Because pipeline code runs as SYSTEM on this PC, fork pull requests must not build automatically: in **Pipelines > kuka-middleware-ci > Edit > ... > Triggers > Pull request validation**, turn off **Build pull requests from forks of this repository** (or require a team member's comment before building). Also keep the project/organization setting **Limit building pull requests from forked GitHub repositories** on (Project settings / Organization settings > Pipelines > Settings).
- Windows PowerShell 5.1 is enough (no `pwsh`). The pipeline installs the .NET SDK from `backend/global.json` and Node.js 24.22.1 itself; Docker Desktop is not needed for CI.

## Pipeline variables

Set these in Azure DevOps under **Pipelines > kuka-middleware-ci > Edit > Variables** (not secret):

| Variable | Value | Purpose |
|---|---|---|
| `AgentPoolName` | `Default` | Agent pool used by `pool.name` |
| `AgentName` | `jozef-agent` | Demand `Agent.Name -equals` that selects the agent |

If a run fails because pool `$(AgentPoolName)` is not found or no agent matches the demands, these variables are missing.

## Connect the pipeline

1. The Azure Pipelines GitHub App must be authorized for `McLehotsky/kuka-middleware` (already done for the existing pipeline).
2. Point the existing pipeline to the new file: **Pipelines > (pipeline) > Edit > ... > Settings > YAML file path** = `/ci-cd/ci.yml`, then save.
3. Rename the pipeline to `kuka-middleware-ci` (**Pipelines > (pipeline) > ... > Rename/move**). The CD pipeline will reference this name.
4. Add the two variables above, then **Run**. On the first run Azure asks to permit the pipeline to use the pool **Default**: open the run and choose **View > Permit**.
5. After the first successful run, protect GitHub `main` (Settings > Rules > Rulesets or Branch protection): require a pull request and the Azure Pipelines status check `kuka-middleware-ci`, with no routine bypass. YAML alone does not configure GitHub branch protection.

## Checks and results

- Backend: SDK from `backend/global.json`, NuGet restore, Release build with .NET analyzers and warnings as errors, xUnit tests using Microsoft.Testing.Platform.
- Frontend: Node 24.22.1, `npm ci`, TypeScript, ESLint, Vitest, production build.
- Every step is one native command in Windows PowerShell; a non-zero exit code fails the step.
- Both jobs publish XML results to the Azure **Tests** tab, including failed test runs. A failed check or missing test report fails the job.
- Checkout uses `clean: true` because the self-hosted workspace is reused between runs.

No PostgreSQL, Docker stack, Fleet credentials, API keys or real robot calls are used. Deployment is backlog 4.4.
