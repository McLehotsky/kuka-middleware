---
title: API key authentication, operation audit and JSON logs
date: 2026-11-04
phase: 8
backlog: ["4.2 API security and logging", "UC1 authenticated mission ownership", "UC2 operation audit"]
agents: [main session, reviewer]
---

# API key authentication, operation audit and JSON logs

## Why

Backlog 4.2 can be completed without KUKA FM licenses. The user approved a trusted localhost-only UI whose requests receive the web-ui key from nginx. Anonymous 401s go to technical logs, keeping the existing audit schema.

## What changed

- `backend/Auth/`: scoped current client, X-API-Key validation, active hash lookup, startup configuration of web-ui/external-system hashes without changing identities or reactivating disabled clients.
- `backend/Audit/`: buffered operation responses and a separate audit DbContext, capturing successful and failed identified attempts before the response is sent. Missing missions are recorded in details without an invalid FK.
- Backend startup/service wiring: authenticated mission ownership, public health, JSON stdout logging, EF SQL command log level Warning. No schema change or additional dependency.
- `docker/`: localhost-only publication; runtime nginx template with a key-only envsubst whitelist; explicit external keys preserved; Host/Origin/Fetch-Metadata guards; idempotent secret initializer used by start.bat.
- Tests: authentication, audit, secret exclusion from structured logs, failed audit persistence, key configuration, and authenticated mission ownership.
- Specification, local development guide, commands and roadmap updated.

## Decisions

- `X-API-Key`; independent random 32-byte lowercase hex keys. Raw keys remain in local secret configuration; DB stores SHA-256 hashes.
- Access to the local UI grants web-ui operations, with no individual login. An explicit external key selects the external client.
- Identified create/cancel attempts are audited. Anonymous authentication rejections are logged without DB audit rows, as approved.
- Buffer create/cancel responses until audit persistence finishes. An audit failure propagates to the outer exception handler instead of sending a successful response.

## Review

Independent correctness/security, tests, infrastructure, and reuse/clarity reviews approved after fixes. Findings addressed: Host/Origin validation before trusted key injection; regression coverage for structured logging and key exclusion; audit persistence failure; external key configuration validation. No remaining actionable findings in those reviews.

| Backlog 4.2 criterion | Evidence |
|---|---|
| Requests without a valid key are rejected | Auth unit cases; direct internal API returns 401; wrong explicit proxy key returns 401. The local UI is the approved trusted proxy exception. |
| Structured activity/error logs on stdout | JSON console, operation/rejection logs, capturing-logger tests and live secret-exclusion check. |
| Every identified create/cancel attempt is audited | Unit cases and live DB rows for 201/200/400/404/409; malformed JSON is included. Anonymous 401 policy verified separately. |

## Out of scope / open items

- KUKA client/token/sync and KUKA-specific error outcomes remain in phase 5; live verification requires licenses and token acquisition needs chapter 4.7.
- CI/CD and container hardening Q10 remain planned. No operator login or additional audit UI.
- GitHub CLI is not authenticated. Local commits are reviewable, but pushing branches/opening PRs requires user GitHub sign-in. This branch is based on the separate test-fix commit until that PR is merged.

## Verification

- Tests were written before the implementation; the initial run failed to compile because Auth/Audit types did not yet exist.
- Backend: 134/134 unit cases passed, 0 skipped; build with warnings-as-errors passed with 0 warnings/errors. Existing mission and robot cases remain green.
- Frontend: 95/95 cases in 6 files; type check, lint and production image build passed.
- Compose validation, API/web image builds and nginx configuration check passed.
- Live checks: public health and trusted UI 200; explicit invalid key and direct unkeyed internal API 401; explicit external key 200. Foreign Host, foreign/different-port Origin, and cross-site fetch metadata 403.
- Invalid JSON and null steps 400, unknown cancel 404. Both clients created missions with correct ownership (201), cancelled them (200), received 409 on repeated cancel, and preserved both history rows. Corresponding audit rows had correct actors, actions, statuses and success flags.
- Anonymous 401 did not add an audit row; rejection JSON log was present. API logs and frontend assets contained no configured raw key.
- Restart preserved client IDs and disabled state; the temporarily disabled external client was restored immediately after the check.
- Environment initializer verified idempotent, generated distinct valid keys, and preserved existing DB credentials/port. Own marked smoke missions were deleted; their audit rows remain.
- Browser: Missions and Robots loaded through the rebuilt local UI without entering a key. No live KUKA calls were made.
