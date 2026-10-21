# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Primary: the operator** of a warehouse or production floor, working at a desktop PC.
  - Keeps the dashboard open for long stretches and watches missions and robots.
  - Now and then dispatches a new mission or cancels a running one.
  - Phone and tablet use is secondary.
- **Secondary: the external system**, a fictional ERP/WMS. It uses the REST API only, never the UI.

## Product Purpose

The ITS Middleware sits between an external system and KUKA AMR Fleet Manager (KUKA FM). Without it, KUKA FM runs on its own and operators have to work in its UI directly.

The middleware gives both people and systems one simple point of access to:
- dispatch missions;
- cancel missions;
- follow missions and robots as they change, without anyone opening KUKA FM.

Success means:
- An operator can dispatch, cancel and follow missions without touching KUKA FM.
- The state of every mission and robot is always current.
- Every user operation is logged.

## Positioning

**Simplicity, and state at a glance.**
- **Few actions:** only dispatch, cancel and watch. KUKA FM offers far more, and the middleware deliberately leaves the rest out.
- **Readable state:** the state of missions and robots can be read immediately and updates live.
- **History:** a mission's history is shown as a timeline.

## Operating Context

- Polling keeps the data current. Mission and robot state come from the middleware's own database, which background sync fills from KUKA FM. The UI never calls KUKA FM directly.
- Mission states:
  - active: `Submitted`, `InProgress`, `Cancelling`;
  - final: `Completed`, `Cancelled`, `Failed`.
- Robot states: Available, Busy and Offline, plus battery level, current node and the robot's mission.
- Cancelling a mission needs a confirmation. The final `Cancelled` state appears only once sync confirms it.
- Every UI request carries an API key.

## Capabilities and Constraints

Screens, following the mockups in `documentation/mockups/` (6.1–6.5):
- missions list with active / finished / all tabs;
- mission detail with a status timeline;
- new mission form;
- cancel dialog;
- robots view with cards or a table.

Constraints:
- Vue 3 + Vite + TypeScript.
- Plain scoped CSS with tokens, no UI library.
- UI language is **English**. The mockup copy is Slovak and gets translated.

Undecided:
- Refresh method and interval (Q2). The mockups show "Live · refresh 5 s".
- Allowed mission types and the fields of a target point (Q9).
- Audit log screen: marked "optional, later" in the mockups.

## Brand Commitments

- **The visual language of the project's own mockups is binding** (`documentation/mockups/6.1`–`6.5`, PNG/SVG). It covers layout, structure, the status style and the overall look.
- **Name:** "ITS Middleware", with the subtitle "KUKA AMR Fleet Manager".
- **No KUKA or FEI logo or brand identity.** The system is fictional, for a school project.

## Evidence on Hand

- Mockups 6.1–6.5 in `documentation/mockups/`.
- Use cases, ERD and acceptance criteria in `documentation/wiki.md` and `documentation/backlog.md`.
- No real data. The robot and mission codes in the mockups are examples and must not be presented as real.

## Product Principles

1. **State before action:** what is happening comes first, and actions come second.
2. **Only what an operator needs:** dispatch, cancel, watch. No KUKA FM features "just in case".
3. **Live, but calm:** updates are visible without distracting from the work.
4. **Trust through traceability:** history and audit show what happened and who did it.

## Accessibility & Inclusion

- Project rules from `CLAUDE.md`: keyboard control with visible focus, light and dark theme, 375 px and desktop width, and a `prefers-reduced-motion` fallback.
- A state is never shown by colour alone; the text is always there too, as in the mockups.
- No specific standard (such as WCAG level) has been required.
