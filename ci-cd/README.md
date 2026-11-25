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
4. Allow the pipeline to use the agent pool. Because `pool.name` is the macro `$(AgentPoolName)`, Azure shows no **Permit** button; the run fails with "Pipeline does not have permissions to use the referenced pool(s) Default". Open **Project settings > Agent pools > Default > Security > Pipeline permissions**, choose **+** and add `kuka-middleware-ci` (and later `kuka-middleware-cd`).
5. Add the two variables above, then **Run**.
6. After the first successful run, protect GitHub `main` (Settings > Rules > Rulesets or Branch protection): require a pull request and the Azure Pipelines status check `kuka-middleware-ci`, with no routine bypass. YAML alone does not configure GitHub branch protection.

## Checks and results

- Backend: SDK from `backend/global.json`, NuGet restore, Release build with .NET analyzers and warnings as errors, xUnit tests using Microsoft.Testing.Platform.
- Frontend: Node 24.22.1, `npm ci`, TypeScript, ESLint, Vitest, production build.
- Every step is one native command in Windows PowerShell; a non-zero exit code fails the step.
- Both jobs publish XML results to the Azure **Tests** tab, including failed test runs. A failed check or missing test report fails the job.
- Checkout uses `clean: true` because the self-hosted workspace is reused between runs.

No PostgreSQL, Docker stack, Fleet credentials, API keys or real robot calls are used. Deployment is `cd.yml` below.

# Azure Pipelines CD

`cd.yml` (pipeline name `kuka-middleware-cd`) deploys the stack (web, api, postgres, seq) with Docker Compose on the same self-hosted agent. It has no branch or PR trigger: it starts automatically when a `kuka-middleware-ci` run on `main` succeeds (pipeline resource `ci`), so a merge into `main` deploys without manual steps. It can also be run manually; the first step fails unless the run is for `refs/heads/main`.

With a pipeline resource trigger from the same repository, `checkout: self` uses the same commit that the CI run built, so the deployed code is exactly the code that passed CI.

## CD agent prerequisites

- Docker Desktop is installed on the agent PC, running, and the user is logged in to Windows (Docker Desktop's engine runs only in a user session). The first step (`docker version`, `docker compose version`) fails early otherwise.
- The agent runs as Local System. Local System has no Docker CLI context of its own, so the CLI uses the default pipe `npipe:////./pipe/docker_engine`, which Docker Desktop also provides. This is not yet verified on the agent; if the check step cannot reach the engine, add the pipeline variable `DOCKER_HOST` = `npipe:////./pipe/dockerDesktopLinuxEngine`.
- `docker.exe` must be on the **system** PATH (Docker Desktop adds `C:\Program Files\Docker\Docker\resources\bin`); the Compose plugin is found in `C:\Program Files\Docker\cli-plugins`. Restart the agent service after installing Docker Desktop so it picks up the new PATH.
- Base images are pulled anonymously (Local System has no Docker Hub login).

## CD pipeline variables

Set these under **Pipelines > kuka-middleware-cd > Edit > Variables** (or in a variable group linked to the pipeline). Secret variables are not passed to scripts automatically; `cd.yml` maps each one explicitly with `env:` and never prints it.

| Variable | Secret | Example / rule | Purpose |
|---|---|---|---|
| `AgentPoolName` | no | `Default` | Agent pool |
| `AgentName` | no | `jozef-agent` | Selects the agent |
| `WebPort` | no | `8081` | Host port of the web UI (`WEB_PORT`), integer 1024-65535; 8080 is taken on the agent PC |
| `DeployPath` | no | `C:\deploy\kuka-middleware` | Dedicated deploy folder: canonical absolute path (no `.`/`..`), at least two levels below a drive root, outside the agent work folder; if it exists and is not empty it must contain `docker\docker-compose.yml` from a previous deploy |
| `DbName` | no | `kuka_middleware` | `POSTGRES_DB` |
| `DbUser` | no | `kuka` | `POSTGRES_USER` |
| `DbPassword` | **yes** | no `;` | `POSTGRES_PASSWORD` |
| `WebUiApiKey` | **yes** | 64 lowercase hex | `WEB_UI_API_KEY` |
| `ExternalSystemApiKey` | **yes** | 64 lowercase hex, different from `WebUiApiKey` | `EXTERNAL_SYSTEM_API_KEY` |

No value may be empty or contain quotes, `$`, `#`, whitespace or line breaks. Leave **Let users override this value when running this pipeline** off for every variable (especially `DeployPath`), so a manual run cannot redirect the deployment.

The deployment uses the Compose project `kuka-middleware`, the same as local development, so it reuses the existing `pgdata` and `seq-data` volumes and replaces the locally started containers. Therefore:

- `DbName`, `DbUser` and `DbPassword` must equal the values in the local `docker/.env` that the `pgdata` volume was created with. PostgreSQL sets the password only when the volume is first initialized; with a different password the API cannot connect and the smoke check fails.
- Use the same API keys as `docker/.env` too, or the external system / local tools using the old keys get 401. Read them from `docker/.env` on the agent PC and paste them into the secret variables; do not post them anywhere else.

## Create the CD pipeline

1. **Pipelines > New pipeline > GitHub > McLehotsky/kuka-middleware > Existing Azure Pipelines YAML file**, path `/ci-cd/cd.yml`; save (do not run yet).
2. Rename it to `kuka-middleware-cd`. The CI pipeline must be named exactly `kuka-middleware-ci` (resource `source`).
3. Add the variables above, mark the three secrets with the lock icon and leave the override option off.
4. Add `kuka-middleware-cd` to **Project settings > Agent pools > Default > Security > Pipeline permissions** (no **Permit** button appears for the macro pool name).
5. Run it once manually from `main`. If Azure asks to permit the pipeline resource `kuka-middleware-ci`, open the run and choose **View > Permit**.
6. Afterwards every successful CI run on `main` starts a deployment.

## Deploy steps

1. Clean checkout of the CI commit.
2. Fail unless the run is for `main`; check that the Docker engine and Compose plugin are reachable.
3. Validate the variables (rules above), including the `DeployPath` guards against a drive root, `..` segments, the agent work folder and unrelated existing data.
4. `robocopy /MIR` the sources to `DeployPath` (without `.git`). Everything else in `DeployPath` is deleted, so use a dedicated folder.
5. Fill `docker/.env.prod` (committed template with `__NAME__` placeholders) from the variables and write it only to `DeployPath\docker\.env.prod` (UTF-8 without BOM, LF). The step fails on an unknown or missing placeholder in the template, or if a placeholder pattern remains after filling; filled values are never printed. Secrets never go into the repository, appsettings or images.
6. `docker compose -f docker/docker-compose.yml --env-file docker/.env.prod build --no-cache`, then `up -d`. Volumes are never removed (`down -v` is not used).
7. Smoke check `http://127.0.0.1:<WebPort>/`, `http://127.0.0.1:<WebPort>/api/health` and Seq `http://127.0.0.1:5341/`: up to 30 attempts each (5 s timeout, 5 s pause), HTTP 200 required. On failure the pipeline prints `docker compose ps` and the last 200 log lines of `api` and `web`, then fails.
8. `docker builder prune -af` and `docker image prune -f` (dangling images only) run always, also after a failure, and keep the agent disk from filling up. It also clears the build cache of local development on the same Docker Desktop; the next local `up --build` is slower, no data is lost.

## Protect the deploy folder

The filled `DeployPath\docker\.env.prod` contains the DB password and API keys. Until the folder is restricted it inherits read access for local **Users**, so anyone logged in to the PC can read the secrets. Restrict it to SYSTEM and Administrators, e.g. in an elevated prompt (create the folder first if it does not exist yet):

```
icacls C:\deploy\kuka-middleware /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F"
```

## Local development after a deployment

The deployed and the local stack are the same Compose project. Running `docker compose -f docker/docker-compose.yml up -d --build` from the repository replaces the deployed containers with the local working copy (data stays in the volumes). Seq is at http://localhost:5341 in both cases.
