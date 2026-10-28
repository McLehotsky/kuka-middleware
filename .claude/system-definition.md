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

### Approved dependencies (2026-10-21)

New packages and images still need the user's approval. Patch updates of the ones below don't.

| Area | Package / image | Version |
|---|---|---|
| NuGet | `Microsoft.EntityFrameworkCore.Design` | 10.0.13 |
| NuGet | `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 |
| NuGet | `EFCore.NamingConventions` | 10.0.1 |
| NuGet | `Microsoft.EntityFrameworkCore.Relational` | 10.0.13, aligns the EF Core version so that `Design` can stay `PrivateAssets="all"` |
| .NET tool | `dotnet-ef` (local tool manifest) | 10.0.13 |
| NuGet (tests) | `xunit.v3` | 4.0.2 |
| NuGet (tests) | `NSubstitute` | 6.2.0, added in phase 6 |
| npm | `vue` | 3.5.44 |
| npm | `vue-router` | 5.4.0 |
| npm (dev) | `vite` | 8.3.5 |
| npm (dev) | `@vitejs/plugin-vue` | 6.0.10 |
| npm (dev) | `typescript` | 6.0.3, not 7.x because `vue-tsc` needs the TypeScript JS API |
| npm (dev) | `vue-tsc` | 3.3.13 |
| npm (dev) | `@vue/tsconfig` | 0.9.1 |
| npm (dev) | `vitest` | 5.0.5 |
| npm (dev) | `eslint` | 10.13.0 |
| npm (dev) | `eslint-plugin-vue` | 10.11.1 |
| npm (dev) | `@vue/eslint-config-typescript` | 14.9.0 |
| npm (dev) | `@types/node` | 24.x |
| Docker | `mcr.microsoft.com/dotnet/sdk`, `mcr.microsoft.com/dotnet/aspnet` | 10.0 (exact patch pinned) |
| Docker | `node` | 24.22.1-alpine |
| Docker | `nginx` | 1.30.6-alpine |
| Docker | `postgres` | 18.6-alpine |

Runtime: .NET SDK 10.0.402 and Node 24 LTS locally. Docker is not installed locally, so compose is verified in CI.

- `dotnet test` runs on Microsoft.Testing.Platform (opted in via `backend/global.json`). For that reason the VSTest packages `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` are not used.
- The test project ignored exit code 8 ("zero tests ran") until the first tests; removed in phase 6.
- Dependency audit covers runtime packages only:
  - `npm audit --omit=dev --audit-level=high`.
  - `dotnet list package --vulnerable --include-transitive`.
  - Known dev-only finding: GHSA-vfj7-8cjw-p6xm (`braces` via `@vue/eslint-config-typescript`), no fixed version yet; re-check on every update.

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
- **Not final** (list filter `?state=active` and the Active tab, Q-D): every non-final status, i.e. also `Created`.
- **Cancellable** (UC2, Q-C): `Created`, `Submitted`, `InProgress`. `Cancelling` and final statuses → 409.
- **Phase 6 transition (Q-B):** without a KUKA client, cancel sets `Cancelling` locally with a history row (Source `Api`, message "cancel requested"); phase 5 inserts the `cancelMission` call before it. So `Created → Cancelling` exists too.
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
| `MissionStep` | `UNIQUE (MissionId, Sequence)` | no duplicate step order (added, not in wiki) |

Schema is created by EF Core migrations applied on API startup.

### Physical schema (approved 2026-10-21)

- **Naming:** C# entities and properties PascalCase; PostgreSQL tables and columns **snake_case** via `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`), e.g. `mission_status_history.changed_at`.
- **Nullability:** every column is NOT NULL except the ones listed below.
- **Strings:** `varchar(n)` with the lengths below.

| Table | Nullable columns | String lengths | Notes |
|---|---|---|---|
| `api_client` | – | `name` 100, `key_hash` 128, `role` 32 | `role` enum `Operator`, `ExternalSystem` |
| `robot` | `name`, `battery_level`, `current_node`, `last_seen_at` | `kuka_robot_id` 64, `name` 100, `status` 32, `current_node` 64 | `status` enum `Available`, `Busy`, `Offline` |
| `mission` | `external_ref`, `robot_id`, `submitted_at`, `finished_at`, `last_synced_at`, `error_message` | `mission_code` 64, `external_ref` 100, `mission_type` 64, `status` 32, `error_message` 1000 | `mission_code` generated by the middleware on creation (UNIQUE NOT NULL); `priority` default 2 (Q-F); `status` enum per the state machine (§5) |
| `mission_step` | – | `node_code` 64, `action` 64 | `action` stored as string, values `Load`, `Unload`, `Wait` (Q9) |
| `mission_status_history` | `previous_status`, `message` | `previous_status` 32, `status` 32, `source` 32, `message` 1000 | `source` enum `Api`, `Sync` |
| `audit_log` | `mission_id`, `http_status`, `details`, `correlation_id` | `action` 32, `correlation_id` 64 | `action` enum `DispatchMission`, `CancelMission`; `details` `jsonb` |

`mission_type` is stored as string, values `RACK_MOVE`, `ROLLER_MOVE` validated by the API (Q9).

**Delete behaviour:**
- `mission` → `mission_step`, `mission_status_history`: **Cascade**.
- `api_client` → `mission`, `audit_log` and `robot` → `mission`: **Restrict**.
- `mission` → `audit_log`: **SetNull** (the audit row stays).

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

### Mission DTOs (phase 6)

- **Request `CreateMissionRequest`:**
  - `missionType`: `RACK_MOVE` | `ROLLER_MOVE`.
  - `priority?`: 1–3, default 2.
  - `robotId?`: must exist.
  - `externalRef?`: ≤100 characters.
  - `steps[]`: at least 1, each `{ nodeCode (1–64, not blank), action (Load | Unload | Wait) }`.
  - Validation errors → 400 `ValidationProblem` with keys like `steps[2].nodeCode`. The index is 0-based, matching the JSON array; the stored `sequence` is 1-based.
  - `missionType`, `action` and `state` match exactly and are case-sensitive.
- **Create response:** 201 `{ id, missionCode }` + `Location: /api/missions/{id}`.
- **`MissionSummary`** (list and cancel response): `id, missionCode, externalRef, missionType, priority, status, robotName, createdAt, lastSyncedAt`.
- **`MissionDetail`:** `MissionSummary` + `submittedAt, finishedAt, errorMessage, createdBy, steps[{ sequence, nodeCode, action }]`.
- **`MissionHistoryEntry`:** `previousStatus, status, source, message, changedAt`, ordered by `changedAt` ascending.
- The list is ordered by `createdAt` descending; an invalid `state` → 400. Cancel → 200 + `MissionSummary`.
- Enums are strings in JSON. Missing optional values are `null`.
- **Mission code (Q-A):** `M-{yyyy}-{NNNN}`. The year comes from `TimeProvider`; the number comes from a PostgreSQL sequence that never resets and is zero-padded to 4 digits.

### Robot DTOs (phase 7)

- **`RobotSummary`** (`GET /api/robots` returns an array): `id, kukaRobotId, name, status, batteryLevel, currentNode, currentMission, lastSeenAt, updatedAt`.
  - `status`: `Available` | `Busy` | `Offline`.
  - `currentMission`: `{ id, missionCode }` of the robot's newest non-final mission (by `createdAt`), or `null` (Q-N).
- The list is ordered by `name` ascending, robots without a name by `kukaRobotId` (Q-O). Empty table → `[]`.
- Enums are strings in JSON. Missing optional values are `null`.

DTOs for later endpoints are written here before they are implemented.

---

## 9. Frontend

Screens follow the mockups in `documentation/mockups/`. Their **visual language is binding** (user decision 2026-10-21): top bar with app name and KUKA FM / sync status, left navigation, tables and cards, status pills, battery bars. `DESIGN.md` is derived from them when the first screen is built. Product context: `PRODUCT.md`.

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
| Q3 | Sync intervals for missions and robots (wiki example 5 s). | 5 |
| Q6 | Log sink: stdout only, or also Seq / another viewer. | 8 |
| Q7 | Azure DevOps: agent / pool, deployment target, static analysis tools. | 8 |
| Q8 | How the web UI gets its API key without exposing it (e.g. injected by nginx for same-origin requests). | 8 |
| Q11 | Cancel race: two parallel cancels both pass the Cancellable check. Add a concurrency token (e.g. PostgreSQL `xmin`) mapped to 409, together with `cancelMission`. | 5 |
| Q10 | Container hardening: run nginx as non-root (e.g. `nginxinc/nginx-unprivileged`, needs approval) and give the api a compose healthcheck (aspnet image has no curl). | 8 |
| Q12 | Robot name ordering is case-sensitive (`StringComparer.Ordinal`); switch to case-insensitive? | any |
| Q13 | New mission: if the chosen robot goes Offline while the form is open, the form warns but still submits. Block the submit instead? | any |
| Q14 | Mobile nav drawer visibly slides out when the viewport crosses 768 px (transition runs on the breakpoint change, since phase 6). Suppress the transition on media change? | 9 |
| Q15 | `.impeccable/design.json` is older than `DESIGN.md` (phase 7 added `dark-offline-surface`). Re-sync with `impeccable document`? | any |

### Decided
| # | Decision | Date |
|---|---|---|
| Q4 | Backend tests xUnit + NSubstitute (plain `Assert`); frontend Vitest for composables and pure functions only. | 2026-10-21 |
| Q5 | Frontend state in composables (no Pinia); plain scoped CSS with tokens from `DESIGN.md` (no Tailwind, no UI library). | 2026-10-21 |
| Q2 | Real-time UI by **polling every 5 s** (no SignalR), as in the mockups. | 2026-10-21 |
| Q9 | Mission types `RACK_MOVE`, `ROLLER_MOVE`. Step = `nodeCode` (required) + action `Load` / `Unload` / `Wait`; at least 1 step. Priority 1–3 (1 high, 2 normal = default, 3 low). Robot optional (none = KUKA FM decides). `externalRef` optional. | 2026-10-21 |

### Phase 6 scope (user decision 2026-10-21)
Phase 6 is built **before** phase 5 (KUKA client, sync) and phase 8 (API keys, audit, Docker):
- **No KUKA FM call yet:** UC1 stores the mission as `Created` with its first history row; `submitMission`, the 502 path and `Submitted` come in phase 5. UC2's KUKA call (`cancelMission`) also comes in phase 5.
- **No auth and no audit yet:** all missions are attributed to one seeded `ApiClient` "web-ui" (role `Operator`, no usable key). API key checks (401) and `AuditLog` writes come in phase 8.
- Local development uses a locally installed PostgreSQL 18 (`kuka_middleware` database, connection string in user-secrets); Docker later.
- Acceptance criteria that depend on these parts are "later phase" in the plan and the review.
- Further phase 6 decisions:
  - **Q-E:** tab counts are computed on the client from one unfiltered list call; the API still supports `state`.
  - **Q-F:** priority default 2, also as DB default.
  - **Q-G:** extra filters, search and pagination from mockup 6.1 are out of scope.
  - **Q-H:** steps are reordered with keyboard-accessible move up / move down buttons.
  - **Q-I:** the robot field shows only "Automatic (KUKA FM decides)" until phase 7; the API already accepts and validates `robotId`.
  - **Q-J:** the header shows only the "Live · refresh 5 s" indicator; KUKA FM status, sync time and the operator avatar come in phases 5 and 8; the Audit log nav item is omitted.
  - **Q-K:** no upper limit on steps.
  - **Q-L:** the seeded client's `KeyHash` is a sentinel that can never match; real keys come in phase 8.

### Phase 7 scope (user decision 2026-10-28)
Phase 7 (UC6) is built **before** phase 5: there are no KUKA FM credentials yet, so the `robot` table is filled only by UC7 later.
- **Q-M:** no seed data for robots (no script, no seeder, no migration seed). Without UC7 the Robots screen shows its empty state.
- **Q-N:** the API derives each robot's current mission (newest non-final mission) for the "Mission" row of the card, linked to the mission detail.
- **Q-O:** robots are ordered by name, falling back to `kukaRobotId`, ascending.
- **Q-P:** cards show "updated X ago" from `updatedAt`; Offline cards show "last seen X ago" from `lastSeenAt` ("—" if null). The header sync chip still waits for phase 5 (Q-J).
- **Q-R:** the New mission robot field lists "Automatic (KUKA FM decides)" plus all robots with their state; Offline robots are disabled in the UI. The API rule stays "`robotId` must exist". Replaces Q-I.
- **Q-S:** the Robots screen follows mockup 6.5 fully: 4 stat tiles (Total / Available / Busy / Offline), search by name or KUKA ID, status filter, and a Cards / Table view toggle. Tiles, search and filter work on the client from one `GET /api/robots` call (as Q-E); the API has no query parameters.
- Robot states in the UI: Available, Busy, Offline (stat tiles: Total / Available / Busy / Offline).
