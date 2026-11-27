# History of changes

One note per finished phase or task, newest first. Format and rules: [CLAUDE.md → History of changes](../CLAUDE.md#history-of-changes).

| Date | Note | Phase | Summary |
|---|---|---|---|
| 2026-11-27 | [24 · CI vulnerability checks](2026-11-27-24-ci-vulnerability-checks.md) | 9 | CI runs NuGet and npm vulnerability checks; legacy mission M-2026-0004 closed |
| 2026-11-27 | [23 · Live test, completed path](2026-11-27-23-live-test-completed.md) | 9 | All checks green (322 / 97 tests); live missions reach Completed and Cancelled |
| 2026-11-27 | [22 · ERD and state exits](2026-11-27-22-state-diagram-erd.md) | 9 | As-built ERD replaces Mermaid; state diagram shows exits from every active state |
| 2026-11-27 | [21 · Use cases and legends](2026-11-27-21-use-case-legends.md) | 9 | Grouped use case diagrams UC1–UC7, goals and state diagrams, colour and code legends |
| 2026-11-27 | [20 · Realization diagrams only](2026-11-27-20-realization-diagrams-only.md) | 9 | Wiki §4 keeps only the as-built diagrams; outdated token note removed |
| 2026-11-27 | [19 · Token per KUKA 4.7](2026-11-27-19-kuka-token-swimlanes.md) | 9 | Token is per vendor manual ch. 4.7 (3.1 done); swimlane diagrams incl. new 4.3 |
| 2026-11-27 | [18 · Readable diagrams](2026-11-27-18-colored-diagrams.md) | 9 | Colourful hand-laid-out as-built diagrams; GitHub ruleset verified, 4.3 done |
| 2026-11-26 | [17 · Documentation](2026-11-26-17-documentation.md) | 9 | Wiki notes and §9 "Realizácia", 7 as-built PlantUML diagrams, backlog status, README |
| 2026-11-26 | [16 · KUKA FM integration](2026-11-26-16-kuka-sync.md) | 5 | KUKA client, submit/cancel, robot and mission sync live-checked via VPN; 4.4 closed; 322 backend / 97 frontend tests |
| 2026-11-26 | [15 · CD trigger](2026-11-26-15-cd-trigger.md) | 8 | CD was not queued after CI on main; `cd.yml` trigger rewritten to re-register it |
| 2026-11-26 | [14 · CD verified](2026-11-26-14-cd-verified.md) | 8 | First CI and CD runs green on the self-hosted agent; middleware deployed to Docker Desktop (8081) with Seq; pipeline naming and pool permissions learned |
| 2026-11-25 | [13 · CD and Seq](2026-11-25-13-cd-seq.md) | 8 | `ci-cd/cd.yml` deploys to local Docker after CI on main with secret pipeline variables and smoke check; Serilog to stdout and Seq; 134 backend tests |
| 2026-11-25 | [12 · CI on the self-hosted agent](2026-11-25-12-ci-self-hosted.md) | 8 | `ci-cd/ci.yml` on `jozef-agent` with PowerShell steps; fork PR builds must be off; first agent run and branch protection pending |
| 2026-11-11 | [11 · Azure CI](2026-11-11-11-azure-ci.md) | 8 | GitHub push/PR pipeline with build, static checks, 134 backend / 95 frontend tests and XML reports; Azure run and branch protection pending |
| 2026-11-04 | [10 · API security and audit](2026-11-04-10-api-security-audit.md) | 8 | Backlog 4.2: API keys, authenticated ownership, audit, JSON logs, trusted localhost-only UI; 134 backend / 95 frontend tests |
| 2026-11-04 | [09 · Test regressions](2026-11-04-09-test-regressions.md) | 6 | Null steps return 400; cancellation preserves history; 100 backend / 95 frontend tests pass; KUKA API reference and license limitation recorded |
| 2026-10-28 | [08 · Docker](2026-10-28-08-docker.md) | 4 | App runs only in Docker Compose; 4.1 verified (start, schema, persistence); local dev flow dropped; `docker/start.bat` |
| 2026-10-28 | [07 · Robots](2026-10-28-07-robots.md) | 7 | Robots API and screen (UC6) with 18 tests, robot picker in New mission, backlog status in `implementation.md` |
| 2026-10-22 | [06 · Local development guide](2026-10-22-06-local-development.md) | 6 | `LOCAL-DEVELOPMENT.md` with the commands to run the app locally |
| 2026-10-21 | [05 · Mission management](2026-10-21-05-mission-management.md) | 6 | Mission API (UC1–UC4) with 76 tests, DESIGN.md from the mockups, missions screens with 69 tests |
| 2026-10-21 | [04 · Scaffolding](2026-10-21-04-scaffolding.md) | 4 | .NET API + EF migration per ERD, Vue 3 skeleton, Docker files, PRODUCT.md |
| 2026-10-21 | [03 · Pull requests in the workflow](2026-10-21-03-pr-workflow.md) | 3 | Agents open PRs with `gh`, user merges; PR format in `CLAUDE.md` |
| 2026-10-21 | [02 · Project README](2026-10-21-02-readme.md) | 3 | `README.md` with main idea, technology, structure and workflow |
| 2026-10-21 | [01 · Claude workflow](2026-10-21-01-claude-workflow.md) | 3 | `CLAUDE.md`, system definition, 5 agents, committed skills, empty project structure |
