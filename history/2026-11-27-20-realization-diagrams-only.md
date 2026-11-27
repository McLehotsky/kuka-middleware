---
title: Section 4 shows only the realization diagrams
date: 2026-11-27
phase: 9
backlog: ["Epic – documentation of the realized solution"]
agents: [main session]
---

# Section 4 shows only the realization diagrams

## Why

Wiki section 4 showed each original design diagram next to an almost identical realization diagram, which confused readers. The teacher's sample project keeps one set of process diagrams. The user decided to keep only the realization diagrams.

## What changed

**main session**
- `documentation/wiki.md` §4.0–4.5: original design images (`4.0-kuka-token`, `4.1-uc01-zadat-misiu`, `4.2-uc02-zrusit-misiu`, `4.3-uc03-uc06-prezeranie`, `4.4-uc07-sync-misii`, `4.5-uc08-sync-robotov`) removed; the realization diagram is the main image of each subsection with its "Realizácia" note below. Removed the outdated note "pri opakovanej odpovedi 401 sa token obnoví najviac raz" (not applicable per KUKA manual ch. 4.7) and references to the removed diagrams ("nie 202 ako v pôvodnom diagrame", "stav Error", "alternatívne SignalR push"). Status table row 4 updated.
- The original `.puml`/`.svg`/`.png` files stay in `documentation/diagrams/` as the archive of the first design; they are no longer referenced by the wiki.

## Decisions

- One set of process diagrams in the wiki: the as-built ones (user decision 2026-11-27). Use case diagrams (3.x), ERD and state diagram (§5), mockups (§6) and 9.1/9.6 stay.

## Review

Docs only; checked by the main session: §4 has 6 images, all `-realizacia`; no reference to removed images remains; text matches the code as in note 19.

## Out of scope / open items

- Copy the updated text to the Azure DevOps project wiki (user step; prepared with the existing attachment links).

## Verification

- `wiki.md`: 16 images in total; §4 references only `diagrams/4.x-…-realizacia.svg`; all relative links resolve.
- Azure text: same content with the existing `/.attachments/…` links; 16 images, no relative paths.
