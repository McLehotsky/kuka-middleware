---
title: Live test of the completed and cancelled mission paths
date: 2026-11-27
phase: 9
backlog: ["1.1 UC1 – Zadať misiu", "1.2 UC2 – Zrušiť misiu", "3.3 UC5 – Synchronizovať stav misií", "Epic"]
agents: [main session]
---

# Live test of the completed and cancelled mission paths

## Why

The user asked for a final check that everything works. The live test of 2026-11-26 covered `InProgress` and `Cancelled` only; the `Completed` path (job 30) had never been seen live.

## What changed

**main session**
- `documentation/wiki.md` §9.2: a second live test bullet (2026-11-27); §9.8: Epic row "Zadať novú misiu" cites M-2026-0039 to M-2026-0041, 3.3 lists `Completed` as checked live.
- `documentation/backlog.md`: the same 3.3 row (the tables are kept identical).
- History note, `history/README.md`, `implementation.md`.

## Decisions

- Test missions use only `Wait` steps on the robot's current node, so the robot does not move (as for M-2026-0039).

## Review

No code change; no reviewer run. Facts below are copied from the API responses, the database and the API logs.

## Out of scope / open items

- Unchanged: live token and 401 path, battery scale on a robot that is not fully charged, Q10, Q12–Q16, Q19, Q20.
- Legacy mission M-2026-0004 (`Created` since 2026-11-11, never known to KUKA FM) still shows as active; to be closed manually as `Cancelled` like M-2026-0005 if the user agrees.
- CI does not run `npm audit` and the NuGet vulnerability check (the teacher's sample does); proposal for the user.

## Verification

All on 2026-11-27, school VPN on, deployed stack from CD on http://localhost:8081 (sources identical to `main`).

- Backend in `mcr.microsoft.com/dotnet/sdk:10.0.402`: `dotnet build -c Release -warnaserror` 0 errors; `dotnet test` 322/322 passed; `dotnet list package --vulnerable --include-transitive` none; `dotnet ef migrations has-pending-model-changes` "No changes have been made to the model since the last migration."
- Frontend: `npm run typecheck`, `npm run lint` clean; `npm run test` 97/97 passed; `npm run build` OK; `npm audit --omit=dev --audit-level=high` 0 vulnerabilities.
- `docker compose -f docker/docker-compose.yml config --quiet` OK; web, `/api/health` and Seq answer 200; a wrong `X-API-Key` gets 401; robot sync reports 3 robots `Available`.
- **M-2026-0040** (robot 1, 2 × `Wait` at `Labak-CHTN-15`): `Created` → `Submitted` (Api) → `InProgress` (Sync, job 20) → `Completed` (Sync, job 30); `SubmittedAt` and `FinishedAt` set; `POST /cancel` afterwards → 409.
- **M-2026-0041** (robot 1, 6 × `Wait`): `Created` → `Submitted` → `InProgress` (job 20) → `Cancelling` (Api, "cancel requested", 200) → `Cancelled` (Sync, job 31) about 4 s later.
- `audit_log`: DispatchMission 201 (0040), CancelMission 409 failed (0040), DispatchMission 201 (0041), CancelMission 200 (0041), all by `web-ui`. No Error entries in the API log; robot 1 back to `Available` without a current mission.
