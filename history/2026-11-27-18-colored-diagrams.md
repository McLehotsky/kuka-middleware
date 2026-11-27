---
title: Readable as-built diagrams and GitHub ruleset
date: 2026-11-27
phase: 9
backlog: ["Epic – documentation of the realized solution", "4.3 CI pipeline – PR into main only with a green pipeline"]
agents: [main session, user]
---

# Readable as-built diagrams and GitHub ruleset

## Why

The user found the PlantUML renders of note 17 hard to read (block and text placement) and asked for colourful, simpler diagrams. Separately, the repository owner set the GitHub ruleset on `main`, which closes the last open criterion of backlog 4.3.

## What changed

**user** (diagrams drawn outside the agents, checked by the main session)
- `documentation/diagrams/`: the 7 as-built diagrams (`4.0-kuka-token-realizacia`, `4.1-uc01-zadat-misiu-realizacia`, `4.2-uc02-zrusit-misiu-realizacia`, `4.4-uc07-sync-misii-realizacia`, `4.5-uc08-sync-robotov-realizacia`, `9.1-nasadenie`, `9.6-ci-cd`) redrawn: hand-laid-out SVG, PNG exports in higher resolution, `.puml` kept as the editable model of each flow. One colour code: blue KUKA FM call, green success, red error, yellow database write, grey log/audit, lavender decision.
- `documentation/diagrams/README.md`: edit visuals in the SVG and export the PNG; keep PlantUML previews in a separate folder so they never overwrite the hand-made graphics.

**main session**
- Backlog 4.3 marked done: `documentation/wiki.md` (§8 CI/CD criterion ticked, 9.6, 9.8, Q7 removed from 9.9), `documentation/backlog.md`, `README.md`, `implementation.md`, `.claude/system-definition.md` (Q7 moved to Decided).

## Decisions

- SVG/PNG are the published graphics; `.puml` describes the flow and must be updated with flow changes, but re-rendering it must not overwrite the SVG.
- Activity diagrams rely on the shared colours and branch labels; only 9.1 has a legend.

## Review

Main session check (no reviewer agent; docs only): every diagram compared with the code and the system definition – token handling and 401, UC1/UC2 status codes and audit, cancel conflict retry, sync start and 5 s interval, job/robot status mappings, battery rule, Offline handling, ports, VPN, CI/CD steps and the ruleset. Findings and fixes by the user: the 9.6 cleanup step now names the dangling-image prune; the `[nie]` label in 4.5 moved next to "Ďalší robot?"; README for editing added.

## Out of scope / open items

- Copy `documentation/wiki.md` into the Azure DevOps project wiki and upload the 7 SVGs (user step).
- Q10, Q12–Q16, Q19, Q20; battery scale; token and 401 not live-tested.

## Verification

- Ruleset on `main` via `gh api repos/McLehotsky/kuka-middleware/rules/branches/main`: `deletion`, `non_fast_forward`, `pull_request` (0 approvals), `required_status_checks` with context `kuka-middleware-ci`.
- All 7 `.puml` files render with `plantuml/plantuml:1.2026.9` (into a scratch folder) without syntax errors.
- SVGs contain no external references or scripts; font `Segoe UI, Helvetica Neue, Arial, sans-serif`; all 7 relative image links in `wiki.md` resolve.
