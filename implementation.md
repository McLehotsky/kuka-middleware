# Implementation roadmap

How the KUKA FM Middleware is built, phase by phase. Each finished phase links its note in [history/](history/README.md), which records what the agents did, the decisions and the review result. Workflow and rules: [CLAUDE.md](CLAUDE.md). Spec: [.claude/system-definition.md](.claude/system-definition.md).

| # | Phase | Backlog | Status | History |
|---|---|---|---|---|
| 1 | Specification – requirements, use cases, activity diagrams, ERD, mockups | – | done | `documentation/wiki.md` |
| 2 | Backlog – Epic → Features → User Stories → Tasks with acceptance criteria | – | done | `documentation/backlog.md` |
| 3 | Claude workflow – `CLAUDE.md`, system definition, 5 agents, skills, empty structure | – | done | [01](history/2026-10-21-01-claude-workflow.md), [02](history/2026-10-21-02-readme.md), [03](history/2026-10-21-03-pr-workflow.md) |
| 4 | Scaffolding – .NET solution + EF Core migration per ERD, Vue 3 project, Dockerfiles, compose; `PRODUCT.md` with `impeccable init` | 4.1 Kontajnerizácia a databáza | done; compose run and verified locally, app runs only in Docker | [04](history/2026-10-21-04-scaffolding.md), [08](history/2026-10-28-08-docker.md), PR [#7](https://github.com/McLehotsky/kuka-middleware/pull/7) |
| 5 | KUKA FM access and sync – typed client with token, robot sync, mission sync | 3.1, 3.2 (UC7), 3.3 (UC5) | planned | |
| 6 | Mission management – create, cancel (local), list, detail with history; DESIGN.md from the mockups (6a). Built before phase 5; KUKA calls, 401 and audit follow in 5 and 8 | 1.1–1.4 (UC1–UC4) | done | [05](history/2026-10-21-05-mission-management.md), [06](history/2026-10-22-06-local-development.md) |
| 7 | Robots – robots API and view, robot picker in New mission. Built before phase 5 (no KUKA FM credentials yet); robot data comes from UC7 | 2.1 (UC6), 1.1 follow-up | done | [07](history/2026-10-28-07-robots.md), PR [#6](https://github.com/McLehotsky/kuka-middleware/pull/6) |
| 8 | Security, logging, CI/CD – API keys, AuditLog, structured logs, CI, CD | 4.2, 4.3, 4.4 | planned | |
| 9 | Hardening and final review – edge cases, end-to-end run, Epic acceptance criteria | Epic | planned | |

Phase order can change; the planner proposes it and the user decides. Open questions per phase: system-definition §14.

## Backlog status

State of every User Story in [documentation/backlog.md](documentation/backlog.md) against the code on `main`. Updated with each phase's history note. **done** = all acceptance criteria met · **partial** = some criteria or tasks met, the rest named · **open** = not started.

Last checked: 2026-10-28 (phase 7).

| Backlog | User Story | Status | Done | Missing (phase) |
|---|---|---|---|---|
| 1.1 | UC1 – Create mission | partial | POST /api/missions with validation (400), mission stored with the first history entry and its ID returned; New mission screen with optional robot selection | `submitMission` call, `Submitted`, `Failed` + 502 (5); 401 (8) |
| 1.2 | UC2 – Cancel mission | partial | POST /api/missions/{id}/cancel with 404 / 409, `Cancelling` + history entry; cancel button with confirmation | `cancelMission` call, final `Cancelled` set by sync (5); concurrency token Q11 (5) |
| 1.3 | UC3 – Missions list | done | GET /api/missions?state=active\|finished from our DB, list screen with filter and auto-refresh, filter tests | – (real-time source data needs UC5) |
| 1.4 | UC4 – Mission status history | done | GET /api/missions/{id} and /history ordered by time, 404; detail screen with timeline, tests | – |
| 2.1 | UC6 – Robots list | done | GET /api/robots from our DB with current mission, robots screen (mockup 6.5) with tiles, search, filter, cards / table and 5 s refresh, unit tests | – (robot data comes from UC7, 5) |
| 3.1 | KUKA FM access with access token | open | – | Everything (5), blocked: no KUKA FM credentials yet |
| 3.2 | UC7 – Sync robots | open | – | Everything (5), blocked: no KUKA FM credentials yet |
| 3.3 | UC5 – Sync missions | open | – | Everything (5), blocked: no KUKA FM credentials yet |
| 4.1 | Containers and database | done | `docker compose up` starts web, api, postgres; EF migrations create the schema per ERD on API start; data survives restart and down / up (verified locally); `docker/start.bat` sets up `.env` and starts the stack | – |
| 4.2 | API security and logging | partial | `api_client` and `audit_log` tables, `web-ui` client seed | API key authentication + 401, structured stdout logs, audit of create / cancel, tests (8) |
| 4.3 | CI pipeline | open | – | Everything (8) |
| 4.4 | CD pipeline | partial | `/api/health` endpoint | Deploy stage, secrets in a variable group, post-deploy smoke test (8) |

**Epic acceptance criteria:** create mission – partial (no KUKA FM call) · cancel mission – partial (local only) · missions view – done in the UI, real-time data needs UC5 · robots view – done in the UI, real-time data needs UC7 · KUKA FM access token – open · user operations logged – open · automated CI/CD – open.
