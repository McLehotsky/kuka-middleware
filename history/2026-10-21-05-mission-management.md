---
title: Mission management – API and screens (UC1–UC4) + DESIGN.md
date: 2026-10-21
phase: 6
backlog: ["1.1 UC1 Zadať misiu", "1.2 UC2 Zrušiť misiu", "1.3 UC3 Zobraziť zoznam misií", "1.4 UC4 Zobraziť históriu stavov misie", "6a DESIGN.md"]
agents: [planner, backend-expert, frontend-expert, reviewer, main session]
pr: 4
pr_url: https://github.com/McLehotsky/kuka-middleware/pull/4
---

# Mission management – API and screens (UC1–UC4) + DESIGN.md

## Why

Operators need to create, list, open and cancel missions. That is the core of the product. The user chose to build this phase before the KUKA FM client (phase 5) and before auth, audit and Docker (phase 8). So this phase covers everything that works against the middleware's own database.

## What changed

**main session**
- `.claude/system-definition.md`:
  - Q2: polling every 5 s.
  - Q9: mission fields.
  - Phase 6 scope: no KUKA call, no auth, no audit, seeded client `web-ui`.
  - Mission DTOs in §8, including 0-based validation keys and the mission code format.
  - State rules: not final, cancellable, the local Cancelling transition.
  - Q-A to Q-L.
  - Q11 (cancel concurrency, phase 5).
  - The priority default fixed to 2.
- `PRODUCT.md`: the Q2 and Q9 items are no longer marked undecided.
- Local PostgreSQL 18 installed with winget, with the `kuka_middleware` database and role. The connection string is only in user-secrets.

**backend-expert** (`backend/`)
- `Missions/`:
  - DTOs, status sets, a pure validation function.
  - `IMissionStore` / `MissionStore`, a small store for this feature only.
  - `IMissionService` / `MissionService`.
  - Thin endpoints: POST `/api/missions`, GET list `?state=`, GET detail, GET history, POST cancel.
- `Program.cs`:
  - ProblemDetails.
  - Enums as JSON strings.
  - `TimeProvider`.
  - `BadRequestExceptionHandler`: a malformed body returns 400 and logs one warning.
- Migrations:
  - `SeedWebUiClient`: client `web-ui`, sequence `mission_code_seq`, priority default 2.
  - `AdvanceApiClientIdentity`: `setval` on the client id sequence.
- Tests: 76 xUnit + NSubstitute unit tests. The `--ignore-exit-code 8` setting is removed.

**frontend-expert** (`frontend/`, `DESIGN.md`, `.impeccable/design.json`)
- `DESIGN.md` (6a), derived from the mockups with the Impeccable document flow:
  - Light theme taken from the mockups.
  - Dark theme derived.
  - Contrast raised to AA for text and to 3:1 for non-text elements.
  - Cancelled gets its own pill.
  - 27 typography roles.
  - Motion tokens.
- `styles/tokens.css`, `base.css`.
- `AppShell`:
  - Top bar with a Live indicator and a Light / Dark / System toggle.
  - Left navigation; below 768 px it is a drawer.
- Shared components: `StatusPill`, `LiveIndicator`, `ThemeToggle`, `ErrorPanel`, `AppIcon`.
- `src/api/`: a typed client, the only place that calls `fetch`, plus ProblemDetails mapping.
- `usePolling` (5 s, never overlaps, queued refresh), `useTheme`, `useLiveStatus`.
- Screens:
  - Missions list (6.1): tabs with counts; stacked cards on mobile.
  - New mission (6.2): validation, steps that can be moved up and down.
  - Mission detail (6.3): status timeline.
  - Cancel dialog (6.4): a native `<dialog>`.
- Tests: 69 Vitest tests for composables and pure functions.

## Decisions

- **Q-A:** mission code format `M-{yyyy}-{NNNN}`, numbered by a DB sequence.
- **Q-B:** cancel without KUKA sets `Cancelling` locally, with a history row "cancel requested".
- **Q-C:** missions in Created, Submitted or InProgress can be cancelled.
- **Q-D:** the Active filter means "not final", so it includes Created.
- **Q-E:** tab counts are computed on the client.
- **Q-F:** priority defaults to 2.
- **Q-G:** search, extra filters and pagination are out of scope.
- **Q-H:** steps are moved with up and down buttons.
- **Q-I:** the robot field only offers "Automatic" until phase 7.
- **Q-J:** the header shows only Live and the theme toggle.
- **Q-K:** no limit on the number of steps.
- **Q-L:** the seed client gets a sentinel `KeyHash`.
- **DESIGN.md:**
  - Muted text `#5E6A78`.
  - Control borders and battery fills reach ≥3:1.
  - Cancelled differs from Created.
  - The theme toggle state is stored in localStorage.
- **Copy:** "Dispatch to KUKA FM is not connected yet."; "Robots – coming soon".
- **Accepted deviations:**
  - The list and history are sorted in memory (no pagination yet).
  - Mission ids are Guid v7.
  - The list opens on the All tab, as in mockup 6.1.
  - Icons are inline SVG.

## Review

The first review gave **approve** with 15 minor findings and no blocker or major:
- The seeded identity was not advanced.
- Logging of malformed requests.
- A file that was not UTF-8.
- Missing "stores nothing" tests.
- "Keep mission" stayed active while the request ran.
- The New mission and Robots copy.
- 44 px touch targets.
- Table roles in the mobile card layout.
- Typography literals instead of tokens.
- The steps error not linked to its control.
- History timestamps without a date.
- Tests checking several behaviours at once.
- The priority default in the spec.

The user chose to fix them all now. The cancel concurrency token is the exception: it moved to phase 5 as Q11. The re-check gave **approve** and also confirmed the extra fix found in the browser: a refresh after cancel queues one fresh request instead of joining a poll that is already running.

| User Story | Criterion | Status |
|---|---|---|
| UC1 | Form / POST: type, steps, optional robot | met |
| UC1 | Invalid input → 400 | met |
| UC1 | 401; KUKA FM not called | later phase (8 / 5) |
| UC1 | Stored with a first history row, ID returned | met for Created; Submitted in phase 5 |
| UC1 | Failed + 502 | later phase (5) |
| UC2 | Only non-final missions, UI confirmation | met |
| UC2 | 404 / 409 | met |
| UC2 | Cancelling + history row | met locally; cancelMission in phase 5 |
| UC2 | Cancelled confirmed by sync | later phase (5) |
| UC3 | List with status, active / finished filter | met |
| UC3 | Data only from our DB | met |
| UC3 | Continuous refresh | met (5 s polling) |
| UC4 | History ordered by time | met |
| UC4 | Unknown mission → 404 | met |
| 6a | DESIGN.md derived from the mockups | met |

## Out of scope / open items

- **Phase 5:**
  - `submitMission` / `cancelMission`, `Submitted`, `Failed` + 502.
  - `Cancelled` set by sync.
  - Q11: concurrency token on cancel.
- **Phase 7:** robot selection in the form, the robots screen.
- **Phase 8:** API keys (401), AuditLog, header chips (KUKA FM status, sync time, operator).
- **Not verified in a browser** (the tool cannot emulate them; checked in code only):
  - reduced motion
  - real Tab-key traversal
  - the dialog backdrop
- **Optional:** a test that a queued refresh does nothing after `stop()`.
- **Test data in the local DB:** missions M-2026-0001 to 0006, most of them in Cancelling.

## Verification

| Command | Result |
|---|---|
| `dotnet build --no-incremental` | 0 warnings, 0 errors |
| `dotnet test` | 76/76 passed (red before implementing, then green) |
| `dotnet ef migrations has-pending-model-changes` | no changes |
| `dotnet list package --vulnerable --include-transitive` | none |
| `npm run typecheck` / `lint` / `test` / `build` | OK, 69/69 tests |
| `npm audit --omit=dev --audit-level=high` | 0 vulnerabilities |
| curl against local PostgreSQL | 201 + Location, 400 with field keys, 404, 409, ordering, filters as specified in §8 |
| Browser (built-in pane, real API) | create → list → detail → cancel, light and dark themes, 375 px, focus handling after the dialog and drawer, 404 page |
