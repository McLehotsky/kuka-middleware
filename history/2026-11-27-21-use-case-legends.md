---
title: Grouped use case diagrams, goals diagram and legends
date: 2026-11-27
phase: 9
backlog: ["Epic – documentation of the realized solution"]
agents: [main session, user]
pr: 19
pr_url: https://github.com/McLehotsky/kuka-middleware/pull/19
---

# Grouped use case diagrams, goals diagram and legends

## Why

The original use case diagrams used a different numbering (UC01–UC08) than the use case texts (UC1–UC7), the goals diagram and the mission state diagram were plain Mermaid, and the coloured blocks and the KUKA status codes in the diagrams had no legend. The user had the diagrams redrawn (ChatGPT) and asked for a check.

## What changed

**user**
- `documentation/diagrams/1-ciele-a-stav.*`: starting and target state of the integration (replaces the Mermaid flowchart in §1).
- `documentation/diagrams/3.1-sprava-misii-realizacia.*` (UC1–UC4), `3.2-prehlad-robotov-realizacia.*` (UC6), `3.3-synchronizacia-realizacia.*` (UC5, UC7): grouped use case diagrams with the UC1–UC7 numbering of the texts; each use case description sits right under its group diagram.
- `documentation/diagrams/5-stavy-misie.*`: mission state diagram (replaces the Mermaid one) with a legend for the KUKA job status codes, UC2 and the state colours.
- Activity diagrams 4.0–4.5 and 9.6: the same colour legend added; nodes and arrows unchanged.
- `documentation/wiki.md` §1, §3, §5 (incl. `xmin` in the ERD) and `documentation/diagrams/README.md`.

**main session**
- History note, `history/README.md`, `implementation.md`.

## Decisions

- One diagram per use case was dropped in favour of three grouped diagrams (user decision 2026-11-27).
- Original use case diagrams (`3.1-sprava-misii`, `3.2-prehlad-robotov`, `3.3-synchronizacia`) stay as an archive, not referenced.

## Review

Main session check against the code and the wiki texts: use case actors match the UC texts; state transitions match `MissionStatuses.cs` (Created → Submitted/Failed/Cancelling; Submitted → InProgress; Submitted/InProgress → Cancelling; any active state → Completed 30/35, Cancelled 31, Failed 60); `xid Version "xmin"` matches the EF mapping. Cosmetic leftovers, accepted: crossing association lines in 3.1, frame title in 3.3 differs from 3.1/3.2.

## Out of scope / open items

- Copy to the Azure DevOps project wiki: 12 images to upload (5 new, 7 activity diagrams with legend).

## Verification

- All 13 relative image links in `wiki.md` resolve.
- PlantUML syntax and exports checked by the user; not re-run here.
