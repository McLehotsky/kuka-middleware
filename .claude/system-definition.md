# KUKA FM Middleware – System Definition

Specification (**what** to build) for all agents. Read it fully before any change.
It is an English distillation of `documentation/wiki.md` and `documentation/backlog.md` (both Slovak). If this file conflicts with code, this file wins; if it conflicts with `documentation/`, raise the conflict to the user.
How to work (agents, workflow, rules, conventions) is defined only in `CLAUDE.md`.

---

## 1. Purpose and scope

- **Current state:** KUKA AMR Fleet Manager (KUKA FM) runs standalone, not connected to ERP/PLM/WMS – operators dispatch and watch missions directly in its UI.
- **Goal:** a middleware that mediates mission control and monitoring for another (fictional) system and unifies access to KUKA FM through its own REST API and web UI.

```
Operator ──► Web UI ──► Middleware API ──► PostgreSQL
External system ──REST──► Middleware API ──REST + token──► KUKA FM
```

### Functional requirements
| ID | Requirement |
|---|---|
| FR1 | Dispatch a new mission through our endpoint, which calls KUKA FM `submitMission`. |
| FR2 | Cancel a running mission through our endpoint, which calls KUKA FM `cancelMission`. |
| FR3 | Periodically poll the state of active missions via the KUKA FM Query jobs API and store it in our DB. |
| FR4 | Periodically poll robot states (available / busy / offline) via the KUKA FM robot query API and store them in our DB. |
| FR5 | Keep the **history** of mission states (not only the last state) for basic reporting. |
| FR6 | Web UI with the list of missions and their current and historical state. |
| FR7 | Web UI with the list of robots and their current state. |

### Non-functional requirements
| ID | Requirement |
|---|---|
| NFR1 | Containerized, persistent database. |
| NFR2 | Our API is protected – an unauthorized / invalid request is rejected (API key per client, §6). |
| NFR3 | Every call to KUKA FM carries a valid `Authorization` token, checked before each call (needed from the very start, otherwise no call works). |
| NFR4 | Structured logs of our own activity and errors. |
| NFR5 | Deployable through a CI/CD pipeline (build, test, static analysis, redeploy). |

### Not in scope (unless the user decides otherwise)
- Robot status history (only the current robot state is stored).
- Operator login / users (API keys per client instead; §6).
- KUKA → middleware callbacks (polling only).

---

## 2. Actors

| Actor | Type | Description |
|---|---|---|
| Operator | primary (human) | Works with the web UI. |
| External system | primary (system) | Fictional ERP/WMS, calls our REST API. |
| Scheduler (polling) | primary (timer) | Internal service that periodically runs synchronization. |
| KUKA FM | secondary (system) | Executes missions, provides mission and robot states. |

---

## 3. Technology stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core minimal API (C#, current .NET LTS) |
| ORM / DB | Entity Framework Core + PostgreSQL |
| Background jobs | hosted / `BackgroundService` |
| Backend tests | xUnit + NSubstitute, plain `Assert` (no FluentAssertions), unit tests only |
| Frontend | Vue 3 + Vite + TypeScript, Vue Router; state in composables (no Pinia); plain scoped CSS with CSS custom properties from `DESIGN.md` (no Tailwind, no UI component library) |
| Frontend tests | Vitest – unit tests for composables and pure functions only (no component or E2E tests) |
| Web server | nginx (serves the SPA, proxies `/api`) |
| Containers | Docker + docker compose |
| CI/CD | Azure DevOps Pipelines (YAML) |
| Work tracking | Azure Boards (`documentation/backlog.csv`) |

Exact versions and every package are proposed in the scaffolding phase and need the user's approval.

---

## 4. KUKA FM integration

- All calls (`submitMission`, `cancelMission`, Query jobs, robot query) go through **one typed client**.
- **Token subprocess (wiki diagram 4.0, used by UC1, UC2, UC5, UC7):**
  1. Before a call, check the token in the cache.
  2. If missing or invalid, obtain a new token from KUKA FM and store it in the cache.
  3. Send the call with the `Authorization` header.
  4. On a `401` response, invalidate the token, obtain a new one and retry **at most once** (no infinite loop).
  5. Log successful and failed calls and authentication errors (never the token itself).
- KUKA FM base URL, credentials and intervals come from configuration (§11), never hardcoded.
- Endpoint names, payloads, token endpoint and KUKA status codes → **open item Q1** (needs the KUKA FM API documentation).

---

## 5. Business rules

### UC1 – Dispatch mission *(FR1)*
1. Actor provides mission parameters: mission type, target points (ordered steps), optionally a robot.
2. Authorize the request, validate input. Invalid → **400**, unauthorized → **401**; KUKA FM is not called in either case.
3. Call `submitMission` with a valid token.
4. On success store the mission with status `Created`/`Submitted` and the first history row; return the mission ID.
5. KUKA FM rejects or is unreachable → mission is `Failed`, the error is logged, API returns **502**.
6. Every attempt (also failed) is written to `AuditLog`.

### UC2 – Cancel mission *(FR2)*
1. Mission must exist (**404**) and must not be finished (**409**).
2. Call `cancelMission`.
3. On success set status `Cancelling` and write a history row.
4. The final `Cancelled` state is set only by mission sync (UC5) when KUKA FM confirms it.
5. Every attempt (also failed) is written to `AuditLog`. The UI asks for confirmation first.

### UC3 – List missions *(FR6)*
- Missions with current status, optional filter active / finished. Read **only from our DB**, KUKA FM is not called. UI refreshes continuously (real-time, see Q2).

### UC4 – Mission status history *(FR5, FR6)*
- From the mission detail, all history rows ordered by time. Unknown mission → **404**.

### UC5 – Mission sync *(FR3, FR5)*
1. Scheduler runs on an interval (e.g. 5 s).
2. Loads active missions (`Submitted`, `InProgress`, `Cancelling`) and calls Query jobs.
3. On a status change: update `Mission.Status`, append a history row.
4. A final status (`Completed`, `Cancelled`, `Failed`) sets `FinishedAt`.
5. Every processed mission gets `LastSyncedAt`; changes are saved in one transaction.
6. KUKA FM unreachable → error logged, retry in the next cycle.

### UC6 – List robots *(FR7)*
- Robots with state Available / Busy / Offline, read only from our DB, UI refreshes continuously.

### UC7 – Robot sync *(FR4)*
1. Scheduler runs on an interval and calls the robot query API.
2. KUKA state is mapped to Available / Busy / Offline (mapping → Q1).
3. New robots are inserted; existing ones get their state and `LastSeenAt` updated.
4. A failed call is logged and data stays unchanged.

### Mission state machine
```
[*] → Created            POST /api/missions
Created → Submitted      submitMission OK
Created → Failed         submitMission error
Submitted → InProgress   sync – robot started
Submitted → Cancelling   cancel
InProgress → Cancelling  cancel
InProgress → Completed   sync – finished
InProgress → Failed      sync – error
Cancelling → Cancelled   sync – confirmed
```
- **Active** (synced by UC5): `Submitted`, `InProgress`, `Cancelling`.
- **Final** (sets `FinishedAt`): `Completed`, `Cancelled`, `Failed`.
- Mapping to KUKA job states → Q1.

---

## 6. Security and audit

- **API key per client (`ApiClient`):** one key for the operator web UI, one for the external system. Only the key **hash** is stored. A request without a valid key → **401**. Seeded keys come from configuration/secrets, never from the repo.
- If operator login is introduced later, `ApiClient` is replaced / extended by `User` and the FKs in `Mission` and `AuditLog` are redirected.
- **Audit vs. technical logs:** user operations (dispatch, cancel) go to the `AuditLog` table, including failed ones (acceptance criterion "all user operations are logged"). Technical structured logs (NFR4) go to stdout, not to the DB.

---

## 7. Data model (PostgreSQL)

Source: wiki §5. Enums stored as strings (`HasConversion<string>()`), timestamps `timestamptz` (UTC).

| Table | Columns |
|---|---|
| `ApiClient` | `Id` int PK, `Name`, `KeyHash`, `Role`, `IsActive`, `CreatedAt` |
| `Robot` | `Id` int PK, `KukaRobotId`, `Name`, `Status`, `BatteryLevel` int, `CurrentNode`, `LastSeenAt`, `UpdatedAt` |
| `Mission` | `Id` uuid PK, `MissionCode`, `ExternalRef`, `MissionType`, `Priority` int, `Status`, `RobotId` FK?, `CreatedByClientId` FK, `CreatedAt`, `SubmittedAt`, `FinishedAt`, `LastSyncedAt`, `ErrorMessage` |
| `MissionStep` | `Id` int PK, `MissionId` FK, `Sequence` int, `NodeCode`, `Action` |
| `MissionStatusHistory` | `Id` bigint PK, `MissionId` FK, `PreviousStatus`, `Status`, `Source`, `Message`, `ChangedAt` – append-only |
| `AuditLog` | `Id` bigint PK, `ApiClientId` FK, `MissionId` FK?, `Action`, `Success`, `HttpStatus` int, `Details` jsonb, `CorrelationId`, `Timestamp` |

Relations: ApiClient 1–0..* Mission · ApiClient 1–0..* AuditLog · Robot 0..1–0..* Mission · Mission 1–1..* MissionStep · Mission 1–1..* MissionStatusHistory · Mission 0..1–0..* AuditLog.

| Table | Index / constraint | Reason |
|---|---|---|
| `Mission` | `UNIQUE (MissionCode)` | pairing with KUKA FM |
| `Mission` | `(Status)` | UC5 selects active missions, UI filters |
| `Mission` | `(CreatedAt DESC)` | mission list |
| `MissionStatusHistory` | `(MissionId, ChangedAt)` | mission timeline (UC4) |
| `Robot` | `UNIQUE (KukaRobotId)` | upsert in UC7 |
| `ApiClient` | `UNIQUE (KeyHash)` | client lookup on auth |
| `AuditLog` | `(Timestamp DESC)`, `(MissionId)` | operations overview |

Schema is created by EF Core migrations applied on API startup.

---

## 8. Middleware REST API

Base path `/api`, JSON (camelCase), errors as RFC 7807 ProblemDetails, every endpoint except `/api/health` requires a valid API key.

| Method | Path | Purpose | Responses |
|---|---|---|---|
| POST | `/api/missions` | Dispatch (UC1) | 201 + id, 400, 401, 502 |
| POST | `/api/missions/{id}/cancel` | Cancel (UC2) | 200, 401, 404, 409, 502 |
| GET | `/api/missions?state=active\|finished` | List (UC3), no `state` = all | 200, 401 |
| GET | `/api/missions/{id}` | Mission detail (UC4) | 200, 401, 404 |
| GET | `/api/missions/{id}/history` | Status history, ordered by time (UC4) | 200, 401, 404 |
| GET | `/api/robots` | Robots with state (UC6) | 200, 401 |
| GET | `/api/health` | Liveness / smoke check after deploy | 200 |

Request/response DTO fields are defined in the phase that builds them, derived from §7, and written here before implementation.

---

## 9. Frontend

Screens follow the mockups in `documentation/mockups/` (simple wireframes, not final design – visual design comes from `DESIGN.md`).

| Mockup | Screen | Use case |
|---|---|---|
| 6.1 | Missions list – status, filter active / finished, continuous refresh | UC3 |
| 6.2 | New mission – mission type, ordered target points, optional robot | UC1 |
| 6.3 | Mission detail – data + status timeline | UC4 |
| 6.4 | Cancel mission – confirmation dialog | UC2 |
| 6.5 | Robots – state Available / Busy / Offline, battery, node, continuous refresh | UC6 |

API calls via relative `/api/...`; the web UI uses its own API key (how it is provided to the browser → Q8). UI language English.

---

## 10. Containers and deployment

- `docker compose up` starts **web** (nginx + built SPA, proxies `/api`), **api** and **PostgreSQL** with a named volume; data survives container restarts.
- **CI:** build backend and frontend, run tests, static analysis; fails on any error; PR into main only with a green pipeline.
- **CD:** after merge into main, deploy without manual steps; secrets (KUKA credentials, DB password, API keys) only in secret variables (variable group); after deploy check the web and `/api/health`, otherwise the pipeline fails.

---

## 11. Configuration

All KUKA FM parameters (base URL, credentials, timeouts, mission defaults), sync intervals, DB connection and seeded API keys come from configuration / environment. The repo holds only defaults without credentials and `.env.example` with placeholders. Exact keys are defined in the phase that first uses them.

---

## 12. Testing policy

- Unit tests for every acceptance criterion of a backend User Story (backlog `[TEST]` tasks), edge cases first:
  - UC1: missing / invalid fields, unauthorized, KUKA reject, KUKA unreachable → `Failed` + 502, audit row on every attempt.
  - UC2: unknown mission (404), each final status (409), already `Cancelling`, KUKA error.
  - UC3/UC4: active / finished filter, history order, 404.
  - Token: cached token reused, missing token issued, 401 → refresh once then fail, header present on every call.
  - UC5: no change, status change, each final status sets `FinishedAt`, `LastSyncedAt` set, KUKA unreachable.
  - UC7: new robot, existing robot updated, every KUKA state mapped, failed call leaves data unchanged.
  - Auth: missing / wrong / inactive API key → 401.
- Backend: **xUnit + NSubstitute**, plain `Assert`, no FluentAssertions, no EF InMemory / Testcontainers / integration tests. Use `[Theory]` for boundary sets.
- Frontend: **Vitest** unit tests for composables and pure functions only (polling start/stop and no overlap, form validation, state badge mapping). No component, snapshot or E2E tests.
- One behaviour per test; no tests for trivial DTOs or framework code.

---

## 13. Acceptance criteria

Epic (wiki §8):
- [ ] A new mission can be dispatched.
- [ ] A running mission can be cancelled.
- [ ] Running missions (updated in real time) and finished missions can be viewed.
- [ ] Robot states (updated in real time) can be viewed.
- [ ] Access to the KUKA FM API is secured by an access token.
- [ ] All operations performed by a user are logged.
- [ ] Testing and deployment are fully automated by a CI/CD pipeline.

Per User Story: `documentation/backlog.md` → **Akceptačné kritériá** (authoritative). The planner and reviewer map and check them one by one.

---

## 14. Open items

Questions, not decisions. Ask the user before implementing anything that depends on them.

| # | Question | Needed in phase |
|---|---|---|
| Q1 | Exact KUKA FM API: endpoint names and payloads (`submitMission`, `cancelMission`, Query jobs, robot query), how the token is obtained, job and robot status codes and their mapping to our states. Source document? | 5 |
| Q2 | Real-time UI: periodic polling (interval?) or SignalR push? | 6 |
| Q3 | Sync intervals for missions and robots (wiki example 5 s). | 5 |
| Q6 | Log sink: stdout only, or also Seq / another viewer. | 8 |
| Q7 | Azure DevOps: agent / pool, deployment target, static analysis tools. | 8 |
| Q8 | How the web UI gets its API key without exposing it (e.g. injected by nginx for same-origin requests). | 8 |
| Q9 | Mission types allowed in UC1 and the fields of a target point (node code, action). | 6 |

### Decided
| # | Decision | Date |
|---|---|---|
| Q4 | Backend tests xUnit + NSubstitute (plain `Assert`); frontend Vitest for composables and pure functions only. | 2026-10-21 |
| Q5 | Frontend state in composables (no Pinia); plain scoped CSS with tokens from `DESIGN.md` (no Tailwind, no UI library). | 2026-10-21 |
