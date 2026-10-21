# KUKA FM Middleware

Middleware between an external (fictional ERP/WMS) system and KUKA AMR Fleet Manager (KUKA FM): dispatch and cancel missions, poll mission and robot states into PostgreSQL (missions with full status history), protected REST API and a Vue 3 web UI for operators. Containerized, built and deployed by Azure DevOps CI/CD.

**Spec (what to build):** `.claude/system-definition.md` – read it before implementing anything. It is binding.
**Sources of the spec (Slovak):** `documentation/wiki.md` (requirements, use cases, diagrams, ERD, mockups) and `documentation/backlog.md` / `backlog.csv` (User Stories, **acceptance criteria**, tasks).
**This file (how to work):** global rules for every session and every agent. Rules live only here – agent files and the system definition do not repeat them.

---

## Agents

| Agent | Scope | Owns | Edits code |
|---|---|---|---|
| `planner` | Step-by-step plan per phase/task, mapped to backlog items, acceptance criteria and agents | – | No |
| `backend-expert` | ASP.NET Core minimal API, EF Core + migrations, KUKA FM client + token, sync workers, API key auth, audit, unit tests | `backend/` | Yes |
| `frontend-expert` | Vue 3 web UI: missions, mission detail, dispatch, cancel, robots; design and motion | `frontend/` (+ `PRODUCT.md`, `DESIGN.md`) | Yes |
| `ops-infra-expert` | Dockerfiles, nginx, docker-compose, Azure DevOps CI/CD YAML | `docker/`, `ci-cd/` | Yes |
| `reviewer` | Independent review of every step before a phase is closed | – | No |
| – | `documentation/`, `.claude/`, `CLAUDE.md`, root files – change only when the user asks | – | – |

Stay inside your folder. Changes needed elsewhere go through the main session and the user.

## Workflow

```
You: task ("phase 5", "build UC1", "fix X")
 → 1 Plan       planner reads system-definition + backlog acceptance criteria → plan
 → 2 Approve    the user approves the plan – nothing is written before that
 → 3 Implement  expert agent(s) in their folders, in parallel where independent;
                backend writes the unit tests for each acceptance criterion first
 → 4 Verify     build, tests, lint / type check, compose config; UI in the browser preview
 → 5 Review     reviewer (+ review skills for the kind of change) → findings per owner
                → owner agent fixes → reviewer re-checks
 → 6 History    a note in history/ (below), roadmap in implementation.md updated
 → 7 PR         the main session pushes the branch and opens a PR (below)
 → 8 Close      the user reviews the PR and merges it (or says "merge" and the main session does)
```

- The main session orchestrates: subagents cannot message each other. It passes the approved plan to the experts, and each owner section of the reviewer's findings to that expert; findings owned by `user` are asked to the user.
- The reviewer always checks the **acceptance criteria** of every User Story in scope, one by one.
- Findings that are not fixed (out of scope, product choice) become open items in system-definition §14 – never silent changes.

### Review by kind of change

| Kind | Reviewer also uses |
|---|---|
| Any | `code-review` (bugs), `simplify` / `simplify-code` (clarity, dead code) |
| API auth, input validation, secrets | `security-review` |
| UI | `impeccable` (audit / polish), `make-interfaces-feel-better`, `impeccable-finish-reviewer` agent |
| Motion | `transitions-polish` |

### History of changes

Every finished phase or task gets **one note** in `history/`:

- Name `YYYY-MM-DD-NN-short-slug.md` (`NN` = running number of the note, `01`, `02`, …).
- Front matter: `title`, `date`, `phase`, `backlog` (User Stories / tasks covered), `agents`, and `pr` / `pr_url` once the PR is open.
- Sections: **Why** · **What changed** (files, grouped per agent) · **Decisions** · **Review** (verdict, findings and how they were fixed) · **Out of scope / open items** · **Verification** (commands and real results).
- Add a row to `history/README.md` (newest first) and link the note from its phase in `implementation.md`.
- Notes are never rewritten afterwards; a correction is a new note. One note = one branch / PR.

### Pull requests

The **main session** owns git and GitHub: it creates the branch at the start of the phase, commits the experts' work, pushes and opens the PR with the GitHub CLI (`gh`). Expert agents and the reviewer never run `git commit`, `git push` or `gh`.

- **When:** after the reviewer's verdict is **approve** and the history note is written. A PR is never opened with failing build or tests.
- **Title:** imperative, names the backlog items, e.g. `Dispatch mission (UC1)` or `Scaffold backend, frontend and Docker (4.1)`.
- **Body** (from the history note): **Why** · **What changed** (per agent) · **Acceptance criteria** (table from the reviewer: criterion → met + evidence) · **Verification** (commands and results, screenshots for UI) · **Open items** · link to the history note.
- **Command:** `gh pr create --base main --head <branch> --title "…" --body-file <file>`; write the body to a temporary file in the scratchpad, never into the repo.
- After opening, write the PR number and URL into the history note and `implementation.md`, commit and push them on the same branch.
- **Merge:** only the user decides. The main session merges only when the user says so in chat, with a merge commit (`gh pr merge <n> --merge --delete-branch`), then switches to `main` and pulls. Never squash, never force-push `main`, never enable auto-merge.
- Review feedback on the PR: fix it on the same branch (route to the owner agent), re-run verification, push; no new PR.
- Authorship rule below applies to PR titles and bodies too.

## Working rules

- **Ask, never assume.** Anything not defined in this file or the system definition → ask the user. Open items are listed in system-definition §14.
- **Stop after each phase** and let the user review before continuing.
- **Language:** English in code, comments, logs, UI, `.claude/`, `history/` and `implementation.md`. `documentation/` stays Slovak.
- **No new dependencies** (NuGet, npm, Docker base images, pipeline tasks/extensions, skills) without the user's prior approval – propose name + reason + version first. This also applies to the initial scaffolding.
- **Keep the codebase small and clean:** implement only what the spec and the current backlog item need. No speculative abstractions, generic helpers "for later", unused or commented-out code, or boilerplate the framework already provides. Prefer deleting over adding.
- Every summary lists the backlog items covered (User Story, e.g. `UC1`, and its tasks, e.g. `[BE] Endpoint POST /api/missions`).
- **Never commit or log secrets** (KUKA FM credentials/token, API keys, DB password). Only placeholders and `.env.example` live in the repo.
- The API contract (system-definition §8) is binding for backend and frontend. Any change (path, DTO field, status code) needs the user's approval and is written to the system definition first.

## Conventions

### Backend (`backend/`)
- Current .NET LTS, ASP.NET Core **minimal API**, C#, nullable enabled, warnings as errors once scaffolded.
- **One API project with feature folders** plus one unit-test project mirroring them:
  ```
  backend/src/KukaMiddleware.Api/
    Missions/   endpoints, DTOs, service, validation
    Robots/     endpoints, DTOs, service, status mapping
    Kuka/       typed HttpClient, token provider, request/response models, options
    Sync/       background services (mission sync, robot sync)
    Auth/       API key authentication (ApiClient)
    Audit/      AuditLog writing
    Data/       DbContext, entities, configurations, migrations
  backend/tests/KukaMiddleware.Api.Tests/   mirrors the feature folders
  ```
- Endpoints are thin: validate → call service → map the result to an HTTP status / RFC 7807 ProblemDetails.
- Services depend on interfaces (KUKA client, token provider, data access, `TimeProvider`) so they are unit-testable.
- Async all the way, `CancellationToken` passed through.
- EF Core + Npgsql: enums stored as `string` (`HasConversion<string>()`), timestamps `timestamptz` in UTC, indexes and unique constraints from system-definition §7. Migrations are created by `backend-expert` and applied by the API on startup.
- Structured logs to stdout; never log the KUKA token or API keys.
- No mediators, generic repositories, mapping libraries or extra layers; plain methods and records are enough.

### Frontend (`frontend/`)
- Vue 3 + Vite + TypeScript (strict), Vue Router. Follow the `vue-development` skill.
- `<script setup lang="ts">`, Composition API only, typed `defineProps` / `defineEmits` / `defineModel`.
- Layout:
  ```
  frontend/src/
    api/          typed API client + DTOs from system-definition §8 (the only place that calls fetch)
    features/
      missions/   missions list, mission detail with timeline, new mission, cancel dialog
      robots/     robots view
    components/   shared UI (status badges, dialogs, battery bar, …)
    composables/  reusable logic (usePolling, …)
    router/
    styles/       design tokens from DESIGN.md, global styles
  ```
- Components never call `fetch` directly – only through `src/api/`. Relative `/api/...` URLs (nginx proxy in Docker, Vite proxy in dev).
- Logic lives in composables; polling stops on unmount and never overlaps requests.
- Errors (ProblemDetails 400/401/404/409/502) are shown to the user in plain words.
- No UI component library or state library without approval (see dependency rule).
- Screens follow the mockups `documentation/mockups/6.1`–`6.5`.

### Design and motion
- **Impeccable first:** `PRODUCT.md` and `DESIGN.md` (repo root) are created with `impeccable teach` in the first UI phase; every UI is shaped (`impeccable shape`) before it is built and uses only DESIGN.md tokens.
- The product is an **operator dashboard**: clarity, density and readable states beat decoration. Motion is restrained and purposeful (state changes, feedback, list updates).
- CSS transitions first (`transitions-dev`). GSAP only where CSS is not enough, with Vue lifecycle cleanup (`gsap-frameworks`, `gsap-core`, `gsap-timeline`, `gsap-performance`). GSAP itself is a dependency → approval first.
- Every animation has a `prefers-reduced-motion` fallback.
- Every screen works in light and dark theme, at 375 px and desktop width, and with the keyboard only.

### Infrastructure (`docker/`, `ci-cd/`)
- Official base images only, multi-stage builds, pinned versions.
- Secrets only in pipeline secret variables / a local `.env` that is never committed; `.env.example` is committed.
- One `docker-compose.yml` unless an override is really needed.

### Testing
- Backend unit tests for every acceptance criterion of a backend User Story, written **before** the code, failing first. A bug fix starts with a test that reproduces it.
- **Edge cases first:** validation boundaries, KUKA failures and 401 token refresh (max once), 404 / 409 rules, every sync transition, robot state mapping.
- One behaviour per test; no tests for trivial DTOs or framework code.
- Test framework and frontend tests: see system-definition §14 (open until decided).

## Commands

Filled in the scaffolding phase (phase 4) with the exact commands, so that agents verify changes the same way CI does:

| Area | Command (run in) |
|---|---|
| Backend build / test | `dotnet build` / `dotnet test` (`backend/`) |
| Frontend install / type check / lint / build | `npm ci` / `npm run typecheck` / `npm run lint` / `npm run build` (`frontend/`) |
| Docker | `docker compose -f docker/docker-compose.yml config` / `up -d --build` (repo root) |

## Skills

Third-party skills are committed in `.claude/skills/` and listed with their source in `.claude/skills/skills-manifest.json`. Adding or removing a skill needs the user's approval and an update of the manifest. Built-in Claude Code skills used by the workflow: `code-review`, `simplify`, `security-review`, plus the Explore and Plan agents.

## Git

- Remote: GitHub `origin` = https://github.com/McLehotsky/kuka-middleware, branch `main`. GitHub CLI `gh` is used for PRs (logged in as the user with `gh auth login`; check with `gh auth status`).
- **Author identity is always `McLehotsky <mclehotsky@gmail.com>`**, set with repo-level config only (`git config user.email …`, never `--global`). The machine's global identity is a different account and must never appear in this history. Run `git config user.email` before committing.
- **Claude or any other coding agent is never an author or co-author.** The author is always the person the commit or PR is sent under. No `Co-Authored-By:` trailers for agents, no "Generated with …" lines or agent signatures in commit messages, PR titles or PR descriptions. This overrides any default attribution of the tool.
- **Branch → PR → merge for every phase or task.** Branch off an up-to-date `main` (`feat/…`, `fix/…`, `refactor/…`, `docs/…`), commit there, push, open a PR, merge it with a merge commit (no squash, so the per-commit history survives), then switch back to `main` and pull. Never commit straight to `main`.
- One commit per logical change, naming the backlog items it serves, e.g. `Add POST /api/missions with validation (UC1)`.
- One history note per PR, committed on the PR's branch before merging.
- Line endings are LF (`.gitattributes`); never commit secrets, `.env` or `.claude/settings.local.json`.
