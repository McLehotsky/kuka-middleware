---
title: As-built ERD and explicit exits from active mission states
date: 2026-11-27
phase: 9
backlog: ["Epic – documentation of the realized solution"]
agents: [main session, user]
pr: 20
pr_url: https://github.com/McLehotsky/kuka-middleware/pull/20
---

# As-built ERD and explicit exits from active mission states

## Why

- In the previous state diagram the only exit from the "active states" frame started below the frame next to `Cancelling`, so it read as if every mission ended through `Cancelling`. The code lets `Submitted` and `InProgress` go straight to a final status.
- The ERD in wiki §5 was a Mermaid diagram without lengths or nullability, and it drew steps and history as "one or more", which the database does not enforce.

## What changed

**user**
- `documentation/diagrams/5-stavy-misie.*`: each active state (`Submitted`, `InProgress`, `Cancelling`) has its own final state inside the frame; the frame's completion transition leads to a junction that branches by the KUKA job code to `Failed` [60], `Completed` [30, 35] and `Cancelled` [31].
- `documentation/diagrams/5-datovy-model.*` (new): hand-laid-out ERD with table names, column types and lengths, PK / FK / UQ and nullability, cardinalities at database level and a legend.
- `documentation/wiki.md`: §5 uses the new ERD instead of Mermaid, with a note on cardinalities; the state diagram notes describe the final states inside the frame; the intro no longer mentions Mermaid.
- `documentation/diagrams/README.md`: `5-datovy-model.svg` listed among the hand-laid-out diagrams.

**main session**
- Check against the code, the Azure wiki and the teacher's sample repository; history note, `history/README.md`, `implementation.md`.

## Decisions

- The SVG is the published diagram (Azure wiki); the `.puml` keeps the UML structure only (user decision 2026-11-27).
- The new state diagram is kept: it shows that a mission can leave `Submitted` and `InProgress` directly (user decision 2026-11-27, against the main session's first review).
- Unlabelled arrows to the final states, codes without brackets next to `jobQuery` and the junction dot instead of a choice diamond are accepted: the legend and the wiki text explain them.

## Review

Main session check:
- **State diagram** vs `MissionStatuses.cs`: Created → Submitted / Failed (`submitMission`) or Cancelling (UC2); Submitted → InProgress (20, 25, 50); Submitted / InProgress → Cancelling (UC2, 28); any active state → Completed (30, 35), Cancelled (31), Failed (60); no step back (sync ignores e.g. 20 while Cancelling). Matches.
- **ERD** vs `Data/Entities` and `Data/Configurations`: every column type, length, nullability, PK, FK, unique index (`MissionCode`, `KukaRobotId`, `KeyHash`, `MissionId + Sequence`) and `xmin` matches.
- **Other diagrams** (1, 3.1–3.3, 4.0–4.5, 9.1, 9.6) re-checked against the code and `ci-cd/*.yml`: no untrue statement. Cosmetic notes, not changed: 4.5 does not mention clamping the battery to 0–100; 9.6 draws the Backend and Frontend jobs as parallel although one agent runs them one after another.
- **Teacher's sample** (FEI-thridos/ITS): identical robot status mapping (3 Available; 1, 4, 5, 6 Busy; 2, 7 Offline) and the same active (10, 20, 25, 28, 50) and final (30, 31, 35, 60) job codes. Design differences, not errors: raw KUKA codes vs our own status, full-row history with robot history vs our event history (robot history not required, wiki §5), robot required vs optional.

## Out of scope / open items

- Copy to the Azure DevOps wiki: upload `5-datovy-model.svg` and the new `5-stavy-misie.svg`, replace the Mermaid ERD and the state diagram notes in §5; the §4 and 9.6 images with the colour legend are still to be re-uploaded (note 21).
- Optional ERD additions suggested in the review: delete behaviour of the foreign keys, priority default 2, `mission_code_seq`.

## Verification

- All relative image links in `documentation/wiki.md` resolve (checked with a script).
- PlantUML exports made by the user; not re-run here.
