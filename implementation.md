# Implementation roadmap

How the KUKA FM Middleware is built, phase by phase. Each finished phase links its note in [history/](history/README.md), which records what the agents did, the decisions and the review result. Workflow and rules: [CLAUDE.md](CLAUDE.md). Spec: [.claude/system-definition.md](.claude/system-definition.md).

| # | Phase | Backlog | Status | History |
|---|---|---|---|---|
| 1 | Specification – requirements, use cases, activity diagrams, ERD, mockups | – | done | `documentation/wiki.md` |
| 2 | Backlog – Epic → Features → User Stories → Tasks with acceptance criteria | – | done | `documentation/backlog.md` |
| 3 | Claude workflow – `CLAUDE.md`, system definition, 5 agents, skills, empty structure | – | done | [01](history/2026-10-21-01-claude-workflow.md), [02](history/2026-10-21-02-readme.md), [03](history/2026-10-21-03-pr-workflow.md) |
| 4 | Scaffolding – .NET solution + EF Core migration per ERD, Vue 3 project, Dockerfiles, compose; `PRODUCT.md` with `impeccable init` | 4.1 Kontajnerizácia a databáza | done (compose verified in CI later) | [04](history/2026-10-21-04-scaffolding.md) |
| 6a | `DESIGN.md` derived from the mockups, when the first screen is built | – | planned | |
| 5 | KUKA FM access and sync – typed client with token, robot sync, mission sync | 3.1, 3.2 (UC7), 3.3 (UC5) | planned | |
| 6 | Mission management – dispatch, cancel, list, detail with history | 1.1–1.4 (UC1–UC4) | planned | |
| 7 | Robots – robots API and view | 2.1 (UC6) | planned | |
| 8 | Security, logging, CI/CD – API keys, AuditLog, structured logs, CI, CD | 4.2, 4.3, 4.4 | planned | |
| 9 | Hardening and final review – edge cases, end-to-end run, Epic acceptance criteria | Epic | planned | |

Phase order can change; the planner proposes it and the user decides. Open questions per phase: system-definition §14.
