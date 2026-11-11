---
title: Mission validation and history regression tests
date: 2026-11-04
phase: 6
backlog: ["1.1 UC1 mission validation and unit tests", "1.2 UC2 cancellation unit tests", "1.4 UC4 history preservation"]
agents: [main session]
---

# Mission validation and history regression tests

## Why

The test review found that a null entry in `steps` caused HTTP 500 instead of 400, cancellation tests did not protect existing history, and Vitest accepted a run with no tests.

## What changed

- Backend: reject null steps with an indexed validation error; test the first and second positions, invalid-input persistence guards, and preservation of existing history for every cancellable status.
- Frontend: remove `passWithNoTests: true`.
- Specification and roadmap: reference the supplied KUKA.AMR Fleet 2.15 chapter-7 API PDF and record that live verification is blocked by missing licenses. Authentication chapter 4.7 is outside the supplied excerpt and is still needed for token acquisition.

## Decisions

- Keep the middleware API and existing dependencies unchanged.
- Use temporary containers with the project's existing .NET SDK and Node image versions for verification; host tools do not meet the project requirements.

## Review

Independent reviewer verdict: approve, with no actionable findings against UC1 invalid input, UC2 cancellation, UC4 complete history, and the testing policy. The reviewer inspected the diff and the recorded verification; the test runs were performed by the main session. The changes do not implement KUKA communication or alter middleware status mappings.

## Out of scope / open items

- Live KUKA integration cannot be verified without licenses.
- Phase 5 client, token handling, sync and mapping decisions remain open; see system-definition Q1.

## Verification

- Before the validator fix: 100 backend cases, 97 passed and 3 new null-step cases failed with `NullReferenceException`.
- After the fix: `dotnet build --no-restore --no-incremental` passed with 0 warnings/errors; `dotnet test --no-build --no-restore` passed 100/100.
- History mutation check in an isolated container copy: intentionally clearing history caused all 3 new history-preservation cases to fail. The workspace source was not mutated.
- Frontend: type check, lint, all 95 tests in 6 files, and production build passed.
- Empty Vitest filter: exit code changed from 0 before the configuration fix to 1 after it.
- Compose configuration and API image build passed; the local API container was rebuilt.
- Local HTTP checks: empty steps and null first/second steps all returned 400 with indexed field errors where applicable; mission count stayed at 1; health returned `Healthy`.
- No live KUKA calls were made. Temporary verification containers were removed after checks.
