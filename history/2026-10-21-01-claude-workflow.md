---
title: Claude workflow setup
date: 2026-10-21
phase: 3
backlog: []
agents: [main session]
---

# Claude workflow setup

## Why

Before any code, every piece of work needs the same path: task → plan → expert agents implement → independent review → the user approves. Modelled on the teacher's repository [FEI-thridos/ITS](https://github.com/FEI-thridos/ITS) (`CLAUDE.md` = how to work, `.claude/system-definition.md` = what to build, five agents). From the Lucent LLM-wiki workflow only the history of changes (this folder) is taken.

## What changed

- `CLAUDE.md` – agents and folder ownership, workflow, review by kind of change, history-note rule, working rules, backend / frontend / design and motion / infra / testing conventions, command placeholders, skills, future git rules.
- `.claude/system-definition.md` – English distillation of `documentation/wiki.md` and `documentation/backlog.md`: FR/NFR, actors, KUKA token subprocess, business rules UC1–UC7, mission state machine, security and audit, data model, REST API, screens, deployment, testing policy, acceptance criteria, open items Q1–Q9.
- `.claude/agents/` – `planner`, `backend-expert`, `frontend-expert`, `ops-infra-expert`, `reviewer`; plus the four `impeccable-*` agents shipped with Impeccable.
- `.claude/skills/` – committed third-party skills: `vue-development`, `impeccable`, `gsap-core`, `gsap-frameworks`, `gsap-timeline`, `gsap-performance`, `transitions-dev`, `transitions-polish`, `make-interfaces-feel-better`, `simplify-code`; sources and commits in `skills-manifest.json`.
- `.claude/settings.json` – Impeccable hooks (SessionStart, PostToolUse on Edit/Write, Stop).
- `.gitignore` (secrets, `.env`, build output, machine-local Claude files, Office lock files) and `.gitattributes` (LF).
- Empty structure: `backend/src`, `backend/tests`, `frontend`, `docker`, `ci-cd` (each with `.gitkeep`), `history/`, `implementation.md`.

## Decisions

- Fifth agent is `ops-infra-expert` (backlog has 10 `[DevOps]` tasks); testing is done by `backend-expert` (tests first) and checked by `reviewer`.
- English for code, UI, `.claude/`, `history/`; `documentation/` stays Slovak.
- Third-party skills are committed so the setup works right after cloning.
- Git repository on GitHub with branch → PR → merge flow; repo-level identity `mclehotsky@gmail.com`; LF line endings via `.gitattributes`. This setup is the initial commit on `main` (the only commit made without a PR).
- Where our wiki differs from the teacher's spec, our wiki wins: Vue 3, API key auth (`ApiClient`) and `AuditLog`, token issued/cached/refreshed once on 401, ERD from wiki §5, logs to stdout.

## Review

Not reviewed by the `reviewer` agent (configuration only, no code). Checked by hand: agent front matter, skill names, consistency of the system definition with the wiki.

## Out of scope / open items

- No application code, no scaffolding, `documentation/` unchanged.
- Open questions Q1–Q9 in system-definition §14 (KUKA FM API details, real-time method, test stack, state/CSS approach, log sink, Azure agent, web UI API key, mission types).
- Impeccable downloads its engine binary into `~/.impeccable/bin/` on first use.

## Verification

- All 9 agent files and 10 skills have valid front matter with `name` (checked with a script).
- `.claude/settings.json` is valid JSON with the three Impeccable hooks.
- To do by the user: open a new Claude Code session in this folder and confirm the agents and skills are listed.
