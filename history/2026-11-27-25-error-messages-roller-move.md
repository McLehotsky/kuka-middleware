---
title: Clear 502 messages, failed-mission link and RACK_MOVE-only form
date: 2026-11-27
phase: 9
backlog: ["1.1 UC1 – Zadať misiu", "1.2 UC2 – Zrušiť misiu", "[FE] Obrazovka Nová misia (mockup 6.2)", "[FE] Tlačidlo Zrušiť s potvrdením"]
agents: [planner, frontend-expert, reviewer, main session]
pr: 23
pr_url: https://github.com/McLehotsky/kuka-middleware/pull/23
---

# Clear 502 messages, failed-mission link and RACK_MOVE-only form

## Why

Open items Q16, Q19 and Q20 (system-definition §14):
- the UI showed every 502 as "KUKA FM did not respond", also when KUKA FM rejected the mission with a reason, and also when the api container was down and nginx answered with its own HTML page;
- the 502 of a failed dispatch carries the `missionId` of the `Failed` mission, but the UI did not use it;
- the New mission form offered ROLLER_MOVE, which the API rejects with 400 (vendor `binCode` per step is missing).

The user approved the planner's plan on 2026-11-27 with the recommended answers. The assignment (and the teacher's sample, "RACK_MOVE only") does not require ROLLER_MOVE.

## What changed

**frontend-expert**
- `frontend/src/api/problem.ts` (+ tests): `ProblemInfo.missionId`; a 502 with ProblemDetails shows its `detail` (fallback unchanged); a 502/503/504 without a ProblemDetails body shows "API unavailable. The middleware is not responding – try again in a moment.".
- `frontend/src/features/missions/missionForm.ts` (+ test): `OFFERED_MISSION_TYPES = ['RACK_MOVE']`, validation against it.
- `frontend/src/features/missions/MissionNewView.vue`: one-option type select; error panel shows the reason and the link "Open the failed mission" to the `Failed` mission; stale lead fixed to "The mission is sent to KUKA FM right away."
- `frontend/src/api/types.ts`: unused `MISSION_TYPES` removed; the `MissionType` union still matches §8.
- `PRODUCT.md`: mission types line.

**main session**
- `documentation/wiki.md` §6.2, §6.4, §9.9; `.claude/system-definition.md` §14 (Q16 and Q20 decided, Q19 narrowed to the `binCode` decision) and two phase notes.
- History note, `history/README.md`, `implementation.md`.

## Decisions

- No API contract change; the API keeps rejecting ROLLER_MOVE with 400. ROLLER_MOVE is removed from the UI, not shown disabled.
- Q16 is solved in the UI (non-JSON 502/503/504 = API unavailable); nginx unchanged.
- Q19 stays open only for the `binCode` decision.
- The missions list with earlier data keeps its stale banner ("Showing data from … – refresh failed. Retrying every 5 s.") without the reason; the reviewer judged this acceptable for Q16 (first load shows "API unavailable"; it recovers by itself).

## Review

Reviewer verdict: **approve**. All 1.1 UC1 and 1.2 UC2 criteria met or unchanged. Q19: only RACK_MOVE offered, client validation rejects ROLLER_MOVE. Q20: `detail` shown, failed dispatch linked via a type-checked `missionId`, vendor text only by interpolation (no `v-html`). Q16: "API unavailable" on every screen through `problemOf`.
Findings: system-definition line 454 and §14 rows (main session, fixed); link font token `--text-body-small-strong` (frontend-expert, optional, fixed); stale-banner question (accepted as is).

## Out of scope / open items

- Q19 `binCode` for ROLLER_MOVE; token live check (Azure variable not yet reaching the CD stack); battery scale; Q10, Q12–Q15.
- Azure DevOps wiki: §6.2, §6.4 and §9.9 text to paste.

## Verification

- `frontend/`: `npm run typecheck`, `npm run lint` clean; `npm run test` 110/110 (13 new tests, written first and seen failing); `npm run build` OK; `npm audit --omit=dev --audit-level=high` 0 vulnerabilities.
- Separate dev stack (`docker compose -p kuka-middleware-dev`, web on 8082, Seq on 5342, fresh DB; the CD stack on 8081 untouched), school VPN on, local `docker/.env` with the KUKA FM token (64 characters) — robot sync worked with the token, no 401 in the log:
  - New mission: the type select has one option, RACK_MOVE.
  - Mission to an unknown node `NO-SUCH-NODE-1` (robot 1): KUKA FM rejected it; the panel showed "KUKA FM rejected the request (code 100001): com.kuka.interfaces.plugin.common.InterfaceAPIException: no container in start node:[{NO-SUCH-NODE-1}]" and the link; Enter on the link opened M-2026-0001 in `Failed` with the error.
  - 375 px, light theme: no horizontal overflow, the long message wraps.
  - `stop api`: nginx answers 502 `text/html`; Robots shows "Robots could not be loaded – API unavailable…" (dark theme); after `start api` it recovered to 3 robots without a reload.
