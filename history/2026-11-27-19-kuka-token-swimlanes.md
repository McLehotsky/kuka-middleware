---
title: KUKA FM token per manual ch. 4.7 and swimlane diagrams
date: 2026-11-27
phase: 9
backlog: ["3.1 KUKA FM access with access token", "Epic – KUKA FM access secured by an access token", "Epic – documentation of the realized solution"]
agents: [main session, user]
---

# KUKA FM token per manual ch. 4.7 and swimlane diagrams

## Why

The user provided the full vendor manual KUKA.AMR Fleet 2.15 (733 pages, kept locally as `documentation/KUKA_AMR_Fleet_2.15_en.pdf`, not committed). Chapter 4.7 "Interface platform → Authentication" (pages 135–136) describes the access token: it is generated manually in the KUKA FM web UI ("Generate an Access Token"), bound to the account, shown again only after the account password, and there is no API to obtain or refresh it and no documented expiry. Notes 16 and 17 called our static token a deviation from backlog 3.1 because chapter 4.7 was missing; that was wrong. This note corrects it (history notes are not rewritten). The user also asked for the as-built diagrams in the swimlane style of the original diagrams, including one for UC3/UC4/UC6.

## What changed

**main session** (text written with a general-purpose helper agent)
- `.claude/system-definition.md` §4, §12, §14 "Phase 5 scope": token handling per KUKA manual ch. 4.7; cache/refresh steps not applicable; 401 logged, call fails, operator generates a new token in the KUKA FM UI.
- `documentation/wiki.md`, `documentation/backlog.md`: realization notes, §8 and Epic checklist (token criterion ticked), 9.2, 9.8, 9.9; backlog 3.1 "splnené".
- `README.md`, `implementation.md`: 3.1 done, Epic token criterion done.
- `.gitignore`: the 46 MB vendor manual stays local.

**user**
- `documentation/diagrams/`: 4.0, 4.1, 4.2, 4.4, 4.5 and 9.6 redrawn with vertical swimlanes (Klient / Middleware / KUKA FM, resp. Vývojár / CI / CD / GitHub); new `4.3-uc03-uc06-prezeranie-realizacia` (Klient / Middleware / PostgreSQL); 9.1 title numbered; linked in wiki §4.3.
- `backend/.../Kuka/IKukaTokenProvider.cs`, `KukaClient.cs`: comments only, referring to manual ch. 4.7 instead of a deviation.

## Decisions

- Static token from configuration is the vendor-intended method (ch. 4.7); backlog 3.1 criteria on token cache/issue/refresh are not applicable and 3.1 is done.
- The vendor manual is not committed (size); documentation names it as "plná príručka KUKA.AMR Fleet 2.15, kap. 4.7".

## Review

Main session check (docs and comments only): every diagram compared with the code – KUKA client token/401 handling, UC1/UC2 status codes, audit and cancel retry, UC4 parallel detail/history load and polling stop on final status or 404 (`MissionDetailView.vue`), UC5/UC7 timing and mappings, deployment ports, CI/CD with the ruleset. No findings.

## Out of scope / open items

- Token and 401 path not live-tested (token not configured; KUKA FM answers without one).
- Copy `documentation/wiki.md` to the Azure DevOps project wiki with the new images (user step).
- Q10, Q12–Q16, Q19, Q20; battery scale.

## Verification

- `pdftotext` of the manual: ch. 4.7 Authentication text read; every API chapter refers to it for `${accessToken}`; no token API, no expiry.
- All 8 relative image links in `wiki.md` resolve.
- PlantUML syntax checked by the user; not re-run here (Docker Desktop was stopped).
- Backend change is comments only; CI builds and tests it on the PR.
