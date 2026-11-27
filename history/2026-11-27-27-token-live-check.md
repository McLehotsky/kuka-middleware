---
title: KUKA FM token checked live after the CD fix
date: 2026-11-27
phase: 9
backlog: ["3.1 Prístup ku KUKA FM API s access tokenom", "4.4 CD pipeline", "Epic"]
agents: [main session]
---

# KUKA FM token checked live after the CD fix

## Why

Open item since phase 5: the KUKA FM token and the 401 path had never been checked live. After PR #24 (history note 26) the user confirmed that CD ran successfully with the secret variable `KukaFmAccessToken` set.

## What changed

**main session**
- `documentation/wiki.md` §9.2: live test with the token; §9.8 row 3.1; §9.9: the token item removed.
- `documentation/backlog.md`: row 3.1 (kept identical to wiki §9.8).
- `.claude/system-definition.md` phase 5 notes: live check with the token.
- History note, `history/README.md`, `implementation.md`.

## Decisions

- The 401 path stays covered by unit tests only: the lab KUKA FM does not check the token, so a 401 cannot be provoked live.

## Review

Documentation only, facts copied from the deployed stack, the API and direct KUKA FM calls; no reviewer run.

## Out of scope / open items

- Battery scale on a robot that is not fully charged; Q19 `binCode`; Q10, Q12–Q15.
- Azure DevOps wiki: §9.2, §9.8 row 3.1 and §9.9 to paste.

## Verification

All on 2026-11-27 around 22:50, school VPN on, deployed stack from CD on http://localhost:8081. Token values were never printed.
- `C:\deploy\kuka-middleware\docker\.env.prod` (written 22:47 by CD): `KUKA_FM_ACCESS_TOKEN` length 64, equal to the local `docker/.env` value; `KukaFm__AccessToken` in the `api` container: length 64; api restarted by CD.
- API log since the restart: no Warning or Error entries; robots updated every 5 s.
- **M-2026-0042** (robot 1, 6 × `Wait` at `Labak-CHTN-15`): `Created` → `Submitted` (Api) → `InProgress` (Sync, job 20) → `Cancelling` (Api, 200) → `Cancelled` (Sync, job 31); no warnings.
- Direct `POST https://kukafm.its.fei/interfaces/api/amr/robotQuery` from the host (`--cacert docker/certs/kukafm.crt`): no `Authorization` → 200; `Authorization: wrong-token-123` → 200; the configured token → 200, all with robot data.
