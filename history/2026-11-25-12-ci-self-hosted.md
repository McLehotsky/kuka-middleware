---
title: CI on the self-hosted Windows agent
date: 2026-11-25
phase: 8
backlog: ["4.3 CI pipeline - build, tests, static analysis on the self-hosted agent"]
agents: [main session, planner, ops-infra-expert, reviewer]
---

# CI on the self-hosted Windows agent

## Why

The teacher asked for CI and CD on a self-hosted Azure Pipelines agent, following the sample project FEI-thridos/ITS. The team's agent `jozef-agent` (pool `Default`, Windows service as Local System) runs on the PC with Docker Desktop. CI moves from the Microsoft-hosted Ubuntu agent to this agent; CD (4.4) follows in the next PR. The previous CI PR [#9](https://github.com/McLehotsky/kuka-middleware/pull/9) (note 11) is merged.

## What changed

**ops-infra-expert**
- `ci-cd/azure-pipelines.yml` renamed to `ci-cd/ci.yml` (Azure pipeline `kuka-middleware-ci`).
- Pool `name: $(AgentPoolName)` with demand `Agent.Name -equals $(AgentName)`; both values are pipeline variables in Azure, not in the file.
- All `bash:` steps are `powershell:` steps (Windows PowerShell 5.1), one native command each, so the exit code fails the step.
- Checkouts use `clean: true` (reused workspace). Test result paths use `$env:COMMON_TESTRESULTSDIRECTORY`.
- Frontend tests: `npm run test '--' ...`; an unquoted `--` is swallowed by PowerShell before `npm.ps1`, so Vitest treated the output path as a test filter.
- `ci-cd/README.md`: agent prerequisites, pipeline variables, YAML path and pipeline name, fork PR builds disabled, GitHub required check.

**main session**
- `.claude/system-definition.md` §10 CI bullet and §14 Q7; `implementation.md` phase 8 row and backlog 4.3.

## Decisions

- Teacher's style: separate `ci.yml` (and later `cd.yml`) in `ci-cd/`, pool and agent from pipeline variables (user decision 2026-11-25).
- Windows PowerShell 5.1 only (no `pwsh` on the agent).
- No new tasks or dependencies; `UseDotNet@2` installs SDK 10.0.402 from `global.json`, `NodeTool@0` Node 24.22.1.

## Review

Reviewer: changes required, then approve. Steps are correct for PowerShell 5.1 (exit codes, `'--'` quoting, test result paths, folded scalars, `clean: true`), confirmed through a local stand-in for the PowerShell@2 wrapper.
- O1 (major): the repository is public and the agent runs as Local System, so fork PRs could run code as SYSTEM on the PC. Fixed: README prerequisite to disable fork PR builds in Azure, header comment in `ci.yml`.
- O2 (minor): README explains the failure when the pool variables are missing. Fixed.
- M1 (minor): Q7 lost the open CD item. Fixed.

Re-check after the fixes: **approve**.

## Out of scope / open items

- User steps in Azure: disable fork PR builds, point the pipeline to `/ci-cd/ci.yml`, rename it `kuka-middleware-ci`, add `AgentPoolName=Default` and `AgentName=jozef-agent`, permit the pool on the first run.
- First run on the agent (backend build and tests) and the GitHub required status check (4.3 criterion 3) still to verify.
- ESLint does not fail on warnings (`--max-warnings 0`); unchanged.
- CD and Seq: next PR.

## Verification

- `ci-cd/ci.yml` parses (PyYAML): 2 jobs, 8 `powershell` steps, no `bash`.
- Frontend in Windows PowerShell 5.1: `npm ci` exit 0, `npm run typecheck` exit 0, `npm run lint` exit 0, `npm run test '--' --reporter=default --reporter=junit --outputFile ...` 6 files / 95 tests passed with `frontend.xml` written, `npm run build` exit 0. The unquoted `--` variant fails with exit 1.
- `docker compose -f docker/docker-compose.yml config --quiet`: exit 0.
- Backend not built locally: host has only .NET SDK 9.0.100, `global.json` pins 10.0.402. Left to the first agent run.
