---
title: KUKA FM token blanked by macro expansion in CD
date: 2026-11-27
phase: 9
backlog: ["3.1 Prístup ku KUKA FM API s access tokenom", "4.4 CD pipeline"]
agents: [ops-infra-expert, reviewer, main session]
---

# KUKA FM token blanked by macro expansion in CD

## Why

The user set the secret pipeline variable `KukaFmAccessToken` in `kuka-middleware-cd`, but the CD run of 2026-11-27 22:04 wrote an empty `KUKA_FM_ACCESS_TOKEN` into `C:\deploy\kuka-middleware\docker\.env.prod`, and the api container had no token.

Cause: Azure Pipelines expands `$(KukaFmAccessToken)` everywhere in inline script text before the script runs. The sentinel `'$(KukaFmAccessToken)'`, meant to detect an undefined variable (left as literal macro text), therefore became the token itself:
- "Write production environment file": `$kukaToken -ceq '<token>'` was true for every defined token, so the token was blanked.
- "Validate deployment variables": the allow-list check never ran for a defined token.

The user approved the fix on 2026-11-27.

## What changed

**ops-infra-expert**
- `ci-cd/cd.yml`: both sentinels are built at runtime, `('$' + '(KukaFmAccessToken)')`, which Azure cannot expand; comments explain why. The macro stays only in the `env:` mappings.

**main session**
- History note, `history/README.md`, `implementation.md`.

## Decisions

- Behaviour unchanged otherwise: undefined or empty variable = no token; a defined token is validated (`^[A-Za-z0-9._+/=-]+$`) and written to `.env.prod`.

## Review

Reviewer verdict: **approve**, no findings. Diagnosis confirmed; no other expandable sentinel in `cd.yml` or `ci.yml` (`Contains('$(')` and PowerShell `$(...)` subexpressions are not macros). Side benefit: the secret is no longer expanded into the agent's temporary script files, only passed through the environment. 4.4 criteria met; 3.1 "Authorization on every call" met in code, confirmed on the deployed system after the next CD run.

## Out of scope / open items

- After the merge: confirm that the CD run writes a non-empty `KUKA_FM_ACCESS_TOKEN` (length only) and that the api container calls KUKA FM without 401.
- The local dev stack already ran with the token from `docker/.env` on 2026-11-27 (robot sync, no 401; history note 25).

## Verification

- Windows PowerShell 5.1 simulation of both steps (`$(KukaFmAccessToken)` substituted in the script text the way Azure does), 64-hex dummy token, never printed:

  | Case | `main`: validate / token length | branch: validate / token length |
  |---|---|---|
  | undefined (literal macro) | pass / 0 | pass / 0 |
  | empty | pass / 0 | pass / 0 |
  | defined | pass / **0** | pass / **64** |
  | defined, invalid | **pass** | throws the allow-list error |

- `docker compose -f docker/docker-compose.yml config --quiet` exit 0; `cd.yml` and `ci.yml` parse (PyYAML, implementer).
