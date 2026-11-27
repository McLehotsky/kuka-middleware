# Implementation roadmap

How the KUKA FM Middleware is built, phase by phase. Each finished phase links its note in [history/](history/README.md), which records what the agents did, the decisions and the review result. Workflow and rules: [CLAUDE.md](CLAUDE.md). Spec: [.claude/system-definition.md](.claude/system-definition.md).

| # | Phase | Backlog | Status | History |
|---|---|---|---|---|
| 1 | Specification – requirements, use cases, activity diagrams, ERD, mockups | – | done | `documentation/wiki.md` |
| 2 | Backlog – Epic → Features → User Stories → Tasks with acceptance criteria | – | done | `documentation/backlog.md` |
| 3 | Claude workflow – `CLAUDE.md`, system definition, 5 agents, skills, empty structure | – | done | [01](history/2026-10-21-01-claude-workflow.md), [02](history/2026-10-21-02-readme.md), [03](history/2026-10-21-03-pr-workflow.md) |
| 4 | Scaffolding – .NET solution + EF Core migration per ERD, Vue 3 project, Dockerfiles, compose; `PRODUCT.md` with `impeccable init` | 4.1 Kontajnerizácia a databáza | done; compose run and verified locally, app runs only in Docker | [04](history/2026-10-21-04-scaffolding.md), [08](history/2026-10-28-08-docker.md), PR [#7](https://github.com/McLehotsky/kuka-middleware/pull/7) |
| 5 | KUKA FM access and sync – typed client with token, robot sync, mission sync | 3.1, 3.2 (UC7), 3.3 (UC5), UC1/UC2 KUKA calls | done; live-checked through the school VPN (robots synced, mission submitted and cancelled); static token, 401 without refresh (recorded deviation) | [16](history/2026-11-26-16-kuka-sync.md) PR [#14](https://github.com/McLehotsky/kuka-middleware/pull/14) |
| 6 | Mission management – create, cancel (local), list, detail with history; DESIGN.md from the mockups (6a). Built before phase 5; KUKA calls, 401 and audit follow in 5 and 8 | 1.1–1.4 (UC1–UC4) | done | [05](history/2026-10-21-05-mission-management.md), [06](history/2026-10-22-06-local-development.md), [09](history/2026-11-04-09-test-regressions.md) |
| 7 | Robots – robots API and view, robot picker in New mission. Built before phase 5 (no KUKA FM credentials yet); robot data comes from UC7 | 2.1 (UC6), 1.1 follow-up | done | [07](history/2026-10-28-07-robots.md), PR [#6](https://github.com/McLehotsky/kuka-middleware/pull/6) |
| 8 | Security, logging, CI/CD – API keys, AuditLog, structured logs, CI, CD | 4.2, 4.3, 4.4 | 4.2 done (+ Seq logging); 4.4 done (automatic CD after CI on main verified); 4.3 done (ruleset verified) | [10](history/2026-11-04-10-api-security-audit.md), [11](history/2026-11-11-11-azure-ci.md) PR [#9](https://github.com/McLehotsky/kuka-middleware/pull/9), [12](history/2026-11-25-12-ci-self-hosted.md) PR [#10](https://github.com/McLehotsky/kuka-middleware/pull/10), [13](history/2026-11-25-13-cd-seq.md) PR [#11](https://github.com/McLehotsky/kuka-middleware/pull/11), [14](history/2026-11-26-14-cd-verified.md) PR [#12](https://github.com/McLehotsky/kuka-middleware/pull/12), [15](history/2026-11-26-15-cd-trigger.md) PR [#13](https://github.com/McLehotsky/kuka-middleware/pull/13), [16](history/2026-11-26-16-kuka-sync.md) PR [#14](https://github.com/McLehotsky/kuka-middleware/pull/14), [18](history/2026-11-27-18-colored-diagrams.md) PR [#16](https://github.com/McLehotsky/kuka-middleware/pull/16) |
| 9 | Hardening and final review – edge cases, end-to-end run, Epic acceptance criteria | Epic | documentation updated (wiki §9, as-built diagrams, backlog status); hardening and end-to-end review open [17](history/2026-11-26-17-documentation.md) PR [#15](https://github.com/McLehotsky/kuka-middleware/pull/15), [18](history/2026-11-27-18-colored-diagrams.md) PR [#16](https://github.com/McLehotsky/kuka-middleware/pull/16) |

Phase order can change; the planner proposes it and the user decides. Open questions per phase: system-definition §14.

## Backlog status

State of every User Story in [documentation/backlog.md](documentation/backlog.md) against the current repository code. Updated with each phase's history note. **done** = all acceptance criteria met · **partial** = some criteria or tasks met, the rest named · **open** = not started.

Last checked: 2026-11-26 (phase 5, KUKA FM integration, and 4.4 closed).

| Backlog | User Story | Status | Done | Missing (phase) |
|---|---|---|---|---|
| 1.1 | UC1 – Create mission | done | Validation (400, at least 2 steps; ROLLER_MOVE 400 until Q19), API key (401), `submitMission` → `Submitted` + history and ID, KUKA reject / unreachable → `Failed` + 502 with `missionId`, audit of every attempt; live-checked | – (ROLLER_MOVE: Q19) |
| 1.2 | UC2 – Cancel mission | done | 404 / 409, `missionCancel` → `Cancelling` + history, KUKA error → 502, `xmin` concurrency (Q11) with one retry after sync writes, final `Cancelled` from sync; live-checked | – |
| 1.3 | UC3 – Missions list | done | GET /api/missions?state=active\|finished from our DB, list screen with filter and auto-refresh, filter tests | – (real-time source data needs UC5) |
| 1.4 | UC4 – Mission status history | done | GET /api/missions/{id} and /history ordered by time, 404; detail screen with timeline, tests | – |
| 2.1 | UC6 – Robots list | done | GET /api/robots from our DB with current mission, robots screen (mockup 6.5) with tiles, search, filter, cards / table and 5 s refresh, unit tests | – (robot data comes from UC7, 5) |
| 3.1 | KUKA FM access with access token | done with deviation | One typed client for the 4 calls, raw `Authorization` when a token is configured, calls and auth errors logged without the token, FM certificate trusted | Token cache/refresh and "401 → retry once": not implemented (static token, §4.7 missing; recorded in §14) |
| 3.2 | UC7 – Sync robots | done | Robot sync at startup and every 5 s, status 1–7 mapped, insert/update, `LastSeenAt`, failures change nothing; live: 3 robots | Battery scale to confirm on a robot that is not full |
| 3.3 | UC5 – Sync missions | done | Active missions every 5 s via jobQuery, status mapping incl. late transitions, history, `FinishedAt`, `LastSyncedAt`, one save per cycle, retry next cycle; live: InProgress and Cancelled confirmed | – |
| 4.1 | Containers and database | done | `docker compose up` starts web, api, postgres; EF migrations create the schema per ERD on API start; data survives restart and down / up (verified locally); `docker/start.bat` sets up `.env` and starts the stack | – |
| 4.2 | API security and logging | done | API key authentication + 401, hash-only keys for web-ui/external-system, authenticated mission ownership, JSON stdout logs plus Seq (Serilog, http://localhost:5341), create/cancel audit including identified failures; anonymous 401s technically logged; localhost-only nginx key injection; unit and local Docker checks | – |
| 4.3 | CI pipeline | done | `ci-cd/ci.yml` (`kuka-middleware-ci`) on the self-hosted Windows agent for GitHub push/PR: backend/frontend builds, analyzers/lint/type check, unit tests and published XML results; green on main and on PR #11; GitHub ruleset on main requires a PR and the `kuka-middleware-ci` check (verified 2026-11-26) | – |
| 4.4 | CD pipeline | done | `ci-cd/cd.yml` (`kuka-middleware-cd`) starts automatically after a green `kuka-middleware-ci` run on main (verified: CI `#20261126.16` → CD `#20261126.3`), secrets as secret pipeline variables (KUKA token optional), compose build + up on Docker Desktop, smoke check of web, `/api/health` and Seq | – |

**Epic acceptance criteria:** create mission – done (KUKA FM call, live-checked) · cancel mission – done · missions view – done, real-time via UC5 · robots view – done, real-time via UC7 · KUKA FM access token – done with the recorded deviation (static token, no refresh) · user operations logged – done (audit incl. KUKA outcomes) · automated CI/CD – done.
