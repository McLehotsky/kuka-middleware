---
title: Local development guide
date: 2026-10-22
phase: 6
backlog: []
agents: [main session]
pr: 5
pr_url: https://github.com/McLehotsky/kuka-middleware/pull/5
---

# Local development guide

## Why

The user wanted one Markdown file with the commands to run the app locally. Starting the API in their own terminal also failed with "ConnectionString property has not been initialized": the user-secrets set from the sandboxed Claude session are not visible in the user's Windows profile, so the guide had to explain the one-time database setup too.

## What changed

- `LOCAL-DEVELOPMENT.md` (new), covering:
  - prerequisites;
  - the one-time PostgreSQL role, database and user-secrets setup, with placeholder passwords;
  - dependency install;
  - running the API and the web UI in two terminals;
  - tests and checks;
  - command-line examples;
  - resetting the database;
  - troubleshooting;
  - Docker for later.
- `README.md`: Getting started links to the guide.
- `CLAUDE.md`, Commands: links to the guide and asks to keep it in sync.

## Decisions

- English, like the rest of the repo outside `documentation/`.
- PowerShell commands, run from the repository root, because the main dev PC is Windows.
- No real passwords anywhere in the guide, only `<your-password>`.

## Review

Not reviewed by the `reviewer` agent: documentation only, no code.

## Out of scope / open items

- The user's own terminal still has no connection string. The user sets it with step 2.2 of the guide when they want to run the API themselves.

## Verification

| Check | Result |
|---|---|
| `dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json` | Restore was successful |
| `psql --version` | PostgreSQL 18.6 |
| `dotnet run` + `Invoke-RestMethod /api/health` | Healthy |
| POST example from section 6 | 201, created `M-2026-0008` |
| `GET /api/missions?state=active` example | 8 missions returned |
