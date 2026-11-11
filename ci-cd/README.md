# Azure Pipelines CI

`azure-pipelines.yml` builds and checks the GitHub source on pushes to all branches and pull requests targeting `main`. Backend and frontend jobs have no dependency on each other. An organization with one hosted parallel job runs them sequentially.

## Connect the pipeline

1. Install/authorize the **Azure Pipelines GitHub App** for `McLehotsky/kuka-middleware`. Repository owner/admin approval may be required. The Azure Configure screen warns if the app is not installed.
2. In the Azure DevOps project, choose **Pipelines > New pipeline > GitHub** and select this repository.
3. Select **Existing Azure Pipelines YAML file**. Select the branch containing the file and `/ci-cd/azure-pipelines.yml` as its path. Choose **Continue**, review the file, then **Run**.
4. After the first successful run, protect GitHub `main` with required pull requests and the Azure Pipelines status check shown on that run. YAML alone does not configure GitHub branch protection. Ensure this rule applies to the team, with no routine bypass.
5. After merging the pipeline PR, use `main` as the pipeline's default branch for manual/scheduled runs.

The pipeline uses Microsoft-hosted `ubuntu-24.04`. If Azure reports that no hosted parallelism is available, request the Microsoft-hosted grant or arrange an approved self-hosted pool and update `pool` accordingly. No deployment target is needed for CI; deployment is backlog 4.4.

## Checks and results

- Backend: SDK from `backend/global.json`, NuGet restore, Release build with .NET analyzers and warnings as errors, xUnit tests using Microsoft.Testing.Platform.
- Frontend: Node 24.22.1, `npm ci`, TypeScript, ESLint, Vitest, production build.
- Both jobs publish XML results to the Azure **Tests** tab, including failed test runs. A failed check or missing test report fails the job. Empty test discovery fails through each runner's defaults.
- Backend reports use xUnit 4's built-in `--report-xunit-xml` support; frontend reports use Vitest's built-in JUnit reporter. No additional package is needed.

No PostgreSQL, Docker stack, Fleet credentials, API keys or real robot calls are used. The pipeline validates source code and unit behaviour; it does not deploy or validate live KUKA integration.
