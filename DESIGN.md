---
name: ITS Middleware
description: Calm, dense operator dashboard for dispatching, cancelling and watching KUKA AMR missions and robots.
colors:
  # --- Light theme: extracted from documentation/mockups/6.1-6.5 (generate.py) unless noted ---
  primary: "#1F4E79"
  primary-hover: "#173D60"            # derived
  on-primary: "#FFFFFF"
  topbar: "#1F4E79"
  topbar-subtitle: "#B9CDE2"
  topbar-chip: "#2C628F"
  connected-dot: "#4ADE80"
  bg: "#FFFFFF"
  surface: "#FFFFFF"
  panel: "#F3F5F8"
  row-highlight: "#F7FAFD"
  offline-surface: "#FAFBFC"
  nav-active: "#DCE6F1"
  border: "#C9D1DB"
  control-border: "#8A96A3"           # user decision: input/select/secondary-button border, >= 3:1 on white
  divider: "#E4E8EE"
  text: "#1E2A36"
  text-muted: "#5E6A78"               # user decision: one step darker than the mockups' #6B7785, AA on every surface
  text-disabled: "#A0AAB6"
  danger: "#B42318"
  danger-text: "#9B1C1C"
  danger-text-soft: "#7A2E2E"
  danger-surface: "#FDECEC"
  danger-border: "#F3B4B4"
  warning-text: "#8A5A00"
  success: "#22A559"
  success-text: "#1E6B3A"
  live-surface: "#F0F7F2"
  live-border: "#B7DFC4"
  battery-track: "#ECEFF3"
  battery-high: "#1E9A52"             # darkened from #22A559 for >= 3:1 on the track
  battery-mid: "#B07F00"              # darkened from #D69E00
  battery-low: "#B42318"
  battery-offline: "#7A8591"          # darkened from #A0AAB6
  scrim: "#0B1620"
  status-created-bg: "#ECEFF3"
  status-created-fg: "#4A5563"
  status-submitted-bg: "#E3ECFA"
  status-submitted-fg: "#2B5BA8"
  status-inprogress-bg: "#DCEBFF"
  status-inprogress-fg: "#1F4E79"
  status-cancelling-bg: "#FFF1D6"
  status-cancelling-fg: "#8A5A00"
  status-completed-bg: "#DDF3E4"
  status-completed-fg: "#1E6B3A"
  status-cancelled-bg: "#F3F5F8"      # user decision: quieter than Created, plus a × icon and 1px border
  status-cancelled-fg: "#5E6A78"
  status-cancelled-border: "#C9D1DB"
  status-failed-bg: "#FBE0E0"
  status-failed-fg: "#9B1C1C"
  focus-ring: "#1F4E79"               # derived
  focus-ring-on-dark: "#FFFFFF"       # derived (inside the navy top bar)
  # --- Dark theme: DERIVED (the mockups are light only); same hues and relationships ---
  dark-primary: "#2F6AA0"
  dark-primary-hover: "#28608F"
  dark-primary-text: "#8CB8E6"
  dark-topbar: "#132A40"
  dark-topbar-subtitle: "#9DB6CF"
  dark-topbar-chip: "#22476A"
  dark-bg: "#0F1720"
  dark-surface: "#161F2A"
  dark-panel: "#121A24"
  dark-panel-raised: "#1A2531"
  dark-row-highlight: "#1C2632"
  dark-nav-active: "#1C3550"
  dark-border: "#3A4756"
  dark-control-border: "#6B7785"
  dark-divider: "#263140"
  dark-text: "#E3E8EE"
  dark-text-muted: "#9AA6B4"
  dark-text-disabled: "#6E7B8A"
  dark-danger: "#F28B82"
  dark-danger-solid: "#B8362C"
  dark-danger-text: "#F4A3A3"
  dark-danger-surface: "#3A1A1C"
  dark-danger-border: "#6E2E30"
  dark-warning-text: "#E8B75A"
  dark-success: "#34C46F"
  dark-success-text: "#7FD7A0"
  dark-live-surface: "#132B1E"
  dark-live-border: "#245C3A"
  dark-battery-track: "#2A333E"
  dark-battery-high: "#34C46F"
  dark-battery-mid: "#E0AA1A"
  dark-battery-low: "#E5534B"
  dark-battery-offline: "#7D8895"
  dark-scrim: "#000000"
  dark-status-created-bg: "#2A333E"
  dark-status-created-fg: "#C3CCD6"
  dark-status-submitted-bg: "#1E2E4A"
  dark-status-submitted-fg: "#A9C4F2"
  dark-status-inprogress-bg: "#173452"
  dark-status-inprogress-fg: "#8EC1F5"
  dark-status-cancelling-bg: "#3A2C0E"
  dark-status-cancelling-fg: "#F2C76B"
  dark-status-completed-bg: "#143524"
  dark-status-completed-fg: "#7FD7A0"
  dark-status-cancelled-bg: "#20262D"
  dark-status-cancelled-fg: "#A9B2BC"
  dark-status-cancelled-border: "#3A4756"
  dark-status-failed-bg: "#43191B"
  dark-status-failed-fg: "#F4A3A3"
  dark-focus-ring: "#8CB8E6"
typography:
  page-title:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "24px"
    fontWeight: 700
    lineHeight: 1.25
  brand:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "17px"
    fontWeight: 700
    lineHeight: 1.3
  dialog-title:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "17px"
    fontWeight: 700
    lineHeight: 1.3
  stat-value:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "26px"
    fontWeight: 700
    lineHeight: 1.1
    fontFeature: "\"tnum\" 1"
  section-title:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "14px"
    fontWeight: 700
    lineHeight: 1.4
  nav:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.4
  body:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "13px"
    fontWeight: 400
    lineHeight: 1.45
  value:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "13.5px"
    fontWeight: 600
    lineHeight: 1.4
  label:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.4
  pill:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1
  meta:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11.5px"
    fontWeight: 600
    lineHeight: 1.4
  table-header:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "0.04em"
  nav-section:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "10.5px"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "0.06em"
  button:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "13px"
    fontWeight: 600
    lineHeight: 1
  strong:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "13px"
    fontWeight: 700
    lineHeight: 1.4
  body-small:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "12.5px"
    fontWeight: 400
    lineHeight: 1.45
  body-small-strong:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "12.5px"
    fontWeight: 600
    lineHeight: 1.4
  small:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: 1.4
  hint:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11.5px"
    fontWeight: 400
    lineHeight: 1.4
  error:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 400
    lineHeight: 1.3
  tag:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 600
    lineHeight: 1.3
  count:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1
  pill-sm:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 600
    lineHeight: 1
  caption:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "10.5px"
    fontWeight: 400
    lineHeight: 1.2
  brand-compact:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "15px"
    fontWeight: 700
    lineHeight: 1.3
  mark:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "9px"
    fontWeight: 800
    lineHeight: 1
  control-touch:
    fontFamily: "Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.2
rounded:
  hairline: "2px"
  tag: "3px"
  control: "4px"
  button: "5px"
  card: "6px"
  dialog: "8px"
  pill: "999px"
spacing:
  xxs: "4px"
  xs: "6px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  xxl: "32px"
  topbar-height: "56px"
  sidebar-width: "200px"
  touch-target: "44px"
components:
  topbar:
    backgroundColor: "{colors.topbar}"
    textColor: "{colors.on-primary}"
    height: "56px"
    padding: "0 20px"
  topbar-chip:
    backgroundColor: "{colors.topbar-chip}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.pill}"
    height: "26px"
    padding: "0 14px"
  sidebar:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.text}"
    width: "200px"
    padding: "24px 10px"
  nav-item:
    textColor: "{colors.text}"
    typography: "{typography.nav}"
    rounded: "{rounded.button}"
    height: "36px"
    padding: "0 12px 0 20px"
  nav-item-active:
    backgroundColor: "{colors.nav-active}"
    textColor: "{colors.primary}"
    rounded: "{rounded.button}"
    height: "36px"
  tab:
    textColor: "{colors.text-muted}"
    typography: "{typography.nav}"
    height: "37px"
    padding: "0 12px"
  tab-active:
    textColor: "{colors.primary}"
    typography: "{typography.section-title}"
  count-badge:
    backgroundColor: "{colors.battery-track}"
    textColor: "{colors.text-muted}"
    rounded: "{rounded.pill}"
    height: "18px"
    width: "24px"
  count-badge-active:
    backgroundColor: "{colors.nav-active}"
    textColor: "{colors.primary}"
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.label}"
    rounded: "{rounded.button}"
    height: "34px"
    padding: "0 16px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.button}"
    height: "34px"
    padding: "0 16px"
  button-danger-outline:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.danger}"
    rounded: "{rounded.button}"
    height: "34px"
    padding: "0 16px"
  button-danger-solid:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.button}"
    height: "34px"
    padding: "0 16px"
  button-small:
    rounded: "{rounded.button}"
    height: "26px"
    padding: "0 12px"
  button-large:
    rounded: "{rounded.button}"
    height: "40px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    height: "34px"
    padding: "0 10px"
  input-error:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    height: "34px"
  status-pill:
    typography: "{typography.pill}"
    rounded: "{rounded.pill}"
    height: "20px"
    padding: "0 9px"
  status-pill-inprogress:
    backgroundColor: "{colors.status-inprogress-bg}"
    textColor: "{colors.status-inprogress-fg}"
  status-pill-failed:
    backgroundColor: "{colors.status-failed-bg}"
    textColor: "{colors.status-failed-fg}"
  live-indicator:
    backgroundColor: "{colors.live-surface}"
    textColor: "{colors.success-text}"
    rounded: "{rounded.pill}"
    height: "26px"
    padding: "0 14px 0 10px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.card}"
    padding: "16px"
  table-header-row:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.text-muted}"
    typography: "{typography.table-header}"
    height: "38px"
    padding: "0 16px"
  table-row:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
    height: "46px"
    padding: "0 16px"
  stat-tile:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.card}"
    height: "72px"
    padding: "0 18px"
  battery-bar:
    backgroundColor: "{colors.battery-track}"
    rounded: "{rounded.card}"
    height: "12px"
  dialog:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.dialog}"
    width: "460px"
    padding: "24px"
  error-panel:
    backgroundColor: "{colors.danger-surface}"
    textColor: "{colors.danger-text}"
    rounded: "{rounded.card}"
    padding: "16px"
---

# Design System: ITS Middleware

Source of truth: the project's mockups `documentation/mockups/6.1`–`6.5` (SVG/PNG, generated by `generate.py`). Every light-theme value in the frontmatter is **extracted** from them unless the comment says `derived`. The whole dark theme, the responsive behaviour, motion, focus and the empty/loading/error states are **derived** (the mockups do not show them) and are marked as such below.

## Overview

**Creative North Star: "The Quiet Control Room"**

A navy-capped, white, flat workspace that an operator can leave open for a whole shift. The screen is mostly white and pale grey-blue; colour is spent almost entirely on **state**: status pills, battery bars, the Live indicator and the single navy accent. Everything else is text, thin borders and whitespace. Density is moderate-to-high (46 px table rows, 13 px body text), tuned for a desktop PC at arm's length rather than for touch.

The layout is a classic app shell: a 56 px navy top bar with the product name and connection/sync chips, a 200 px pale sidebar, and a content column with a breadcrumb, a 24 px page title and cards or a bordered table. Depth comes from 1 px borders and tonal panels, not shadows; the only lifted object is the confirmation dialog.

Updates arrive every 5 s without drama: values change in place, the "synced n s ago" chip ticks, and nothing slides, bounces or flashes.

**Key Characteristics:**
- Navy top bar + pale sidebar + white content; one accent colour (`primary`).
- State is always a tinted pill **with its English status text**, never a colour alone.
- Flat surfaces, 1 px cool-grey borders, small consistent radii (4–8 px, pills fully round).
- System font stack, no web fonts; hierarchy by size and weight, not by typeface.
- Calm motion: short CSS transitions on state change only.

## Colors

A cool, low-saturation slate-and-navy neutral base with a disciplined set of semantic tints that only appear where they mean something.

### Primary
- **Harbor Navy** (`primary`): the top bar, primary buttons, the active nav item text and its 4 px indicator bar, the active tab and its 3 px underline, mission codes and robot/mission links, the current step in a timeline, the selected page in pagination. Hover darkens to `primary-hover` (derived).
- **Mist Blue** (`nav-active`): background of the active nav item, the selected tab's count badge and the selected segment of the Cards/Table toggle.
- **Top-bar companions:** `topbar-subtitle` for "KUKA AMR Fleet Manager", `topbar-chip` for the connection and sync chips, `connected-dot` for the "KUKA FM connected" dot.

### Neutral
- **Paper White** (`bg`, `surface`): page, cards, table body, inputs, secondary buttons.
- **Cool Panel** (`panel`): sidebar, table header row, the "Total" stat tile, source tags (API/Sync).
- **Row Highlight** (`row-highlight`): optional emphasis of one table row (newest/updated row in 6.1).
- **Offline Card** (`offline-surface`): background of an offline robot card.
- **Steel Border** (`border`): 1 px borders of cards, tables, sidebar edge, tab baseline, timeline connectors (2 px).
- **Control Border** (`control-border`, user decision): 1 px borders of inputs, selects and secondary buttons – darker than `border` so form controls reach 3:1 against white.
- **Hairline Divider** (`divider`): row separators inside tables and the card footer rule.
- **Ink** (`text`): primary text. **Slate** (`text-muted`): breadcrumb, labels, meta, inactive tabs, placeholders, timestamps – on every surface. It is one step darker than the mockups' #6B7785 (user decision) so it stays ≥ 4.5:1 on panels, badges and tinted rows too. **Fog** (`text-disabled`): disabled items only (e.g. the "Audit log – later" nav entry, drag handles) – never for readable content.

### Semantic
- **Danger** (`danger`): cancel buttons (outline and solid), input error border and error message, the "!" in the cancel dialog. `danger-text` / `danger-text-soft` on `danger-surface` with `danger-border` form the form/API error panel.
- **Warning** (`warning-text`): "cancelling…" inline text in tables, "last seen 14 min ago" on offline robots.
- **Success** (`success`, `success-text`): the Live indicator (`live-surface`, `live-border`, dot `success`), completed timeline steps.
- **Battery:** fill `battery-high` (≥ 50 %), `battery-mid` (25–49 %, from `generate.py`; not visible in the rendered PNGs). All fills are slightly darkened from the mockups (user decision) to reach ≥ 3:1 against the track, `battery-low` (< 25 %), `battery-offline` for an offline robot regardless of level; track `battery-track`.
- **Scrim** (`scrim`) at 45 % opacity behind dialogs.

### Status pill palette

| Status | Light bg / fg | Dark bg / fg (derived) | Light / dark contrast |
|---|---|---|---|
| Created | `status-created-*` | `dark-status-created-*` | 6.6 / 7.9 |
| Submitted | `status-submitted-*` | `dark-status-submitted-*` | 5.6 / 7.7 |
| InProgress | `status-inprogress-*` | `dark-status-inprogress-*` | 7.2 / 6.7 |
| Cancelling | `status-cancelling-*` | `dark-status-cancelling-*` | 5.3 / 8.5 |
| Completed | `status-completed-*` | `dark-status-completed-*` | 5.6 / 7.8 |
| Cancelled | `status-cancelled-*` + border + × icon | `dark-status-cancelled-*` + border + × icon | 5.1 / 7.1 |
| Failed | `status-failed-*` | `dark-status-failed-*` | 6.5 / 7.6 |

**Created vs. Cancelled** (user decision): Created keeps the solid grey pill; Cancelled is quieter – lighter background, 1 px border, muted text and a small leading × icon (`aria-hidden`), still labelled "Cancelled".

Robot states reuse the mission palette: **Available** = Completed colours, **Busy** = InProgress colours, **Offline** = Created colours. The pill always shows the word (English: Available / Busy / Offline; mission statuses keep their enum names as in the mockups).

### Dark theme (derived)
Not in the mockups. It keeps the light theme's relationships: a navy top bar (`dark-topbar`) darker than the page accent, a sidebar (`dark-panel`) slightly darker than the cards (`dark-surface`) on a near-black blue page (`dark-bg`), and each status as a deep tint of its hue with a light foreground of the same hue. The accent splits in two: `dark-primary` as a filled button background (white text 5.7:1) and `dark-primary-text` for links, active nav/tab text, codes and the focus ring (8.0:1 on surface). It follows `prefers-color-scheme` by default; a small Light / Dark / System toggle in the top bar overrides it (user decision), stored in `localStorage` (every access wrapped in try/catch, falling back to System).

### Contrast (checked, WCAG 2.x)
All text pairs above are ≥ 4.5:1 in both themes, e.g. text/bg 14.6 light, 14.7 dark; `text-muted` on white 5.51 and on panel 5.05; white on `primary` 8.66; `topbar-subtitle` on navy 5.32; `danger` on white 6.57; `dark-text-muted` on `dark-surface` 6.72. `text-disabled` (2.35) is for disabled content only. Non-text elements reach ≥ 3:1 (user decision): `control-border` on white 3.01, battery fills on the track 3.1–5.7 (dark 3.45–6.05), `dark-control-border` on dark surface 3.64. Card and table borders (`border`) are decorative grouping lines and stay as in the mockups. The percentage numeral beside a battery bar always carries the value.

### Named Rules
**The State Owns Colour Rule.** Tinted colour (green, amber, red, the status blues) appears only to express a state. Decoration never borrows a state colour.

**The Word Beside the Colour Rule.** Every coloured state carries its text: pills show the status name, battery bars show "78 %", stat tiles show the label, offline cards add "⚠ last seen …". Removing the colour must lose nothing.

**The One Muted Rule.** There is a single muted text colour (`text-muted`, dark: `dark-text-muted`) and it is AA on every surface; never lighten it for a subtler look.

## Typography

**Display Font:** none separate.
**Body Font:** system stack `Segoe UI, system-ui, -apple-system, Helvetica, Arial, sans-serif` (the mockups use `Segoe UI, Helvetica, Arial, sans-serif`; `system-ui`/`-apple-system` added so non-Windows systems get their native UI face). No web font is downloaded.

**Character:** a neutral workplace sans; hierarchy comes from a tight size range (10.5–26 px) and three weights (400, 600, 700).

### Hierarchy
- **Page title** (700, 24 px): one per screen ("Missions", "M-2026-0142").
- **Brand / Dialog title / Robot name** (700, 17 px): top-bar product name, dialog heading, robot card name.
- **Stat value** (700, 26 px, tabular figures): numbers in summary tiles.
- **Section title** (700, 14 px): card titles ("Mission parameters", "Status history"), active tab.
- **Nav** (400, 14 px; 700 when active): sidebar items, inactive tabs.
- **Body** (400, 13 px): table cells, inputs, dialog copy, timeline messages. Dates and type codes may use 12.5 px.
- **Value** (600, 13.5 px): values in key/value detail grids, step node codes.
- **Label** (600, 12 px): form labels, button text is 13 px/600, breadcrumb 12 px/400.
- **Pill** (600, 12 px; 11 px inside the status history timeline).
- **Meta** (600, 11.5 px): keys in detail grids and robot cards; 400 for footers ("updated 2 s ago").
- **Table header** (700, 11 px, UPPERCASE, `text-muted`): column heads, step-table heads.
- **Nav section** (700, 10.5 px, UPPERCASE): "NAVIGATION" in the sidebar; version line 10.5 px/400.

Letter-spacing on the two uppercase roles and all line-heights are derived (the SVGs do not define them).

### Supporting roles
Each role is a CSS custom property `--text-<name>` in `frontend/src/styles/tokens.css`; components never write a literal `font:` value.
- **button** (600, 13 px): Text of all buttons.
- **strong** (700, 13 px): Error-panel heading, "Add step" link, step numbers.
- **body-small** (400, 12.5 px): Secondary body: error-panel and dialog messages, stale-data notes, timeline messages, type codes.
- **body-small-strong** (600, 12.5 px): Timeline timestamps.
- **small** (400, 12 px): Breadcrumb, card meta, footnotes, Live indicator, "cancelling…".
- **hint** (400, 11.5 px): Field hints and card footers.
- **error** (400, 11 px): Inline field error messages.
- **tag** (600, 11 px): Source tag (API / Sync) in the history timeline.
- **count** (700, 11 px): Count badges on tabs, numbers in step circles.
- **pill-sm** (600, 11 px): Status pills inside the history timeline.
- **caption** (400, 10.5 px): Version line and the "coming soon" note in the sidebar.
- **brand-compact** (700, 15 px): Product name in the top bar below 768 px.
- **mark** (800, 9 px): The "ITS" letters in the brand mark.
- **control-touch** (400, 16 px): Inputs below 768 px (16 px prevents iOS zoom on focus).

### Named Rules
**The Tabular Numbers Rule.** Times, counts, percentages and codes use `font-variant-numeric: tabular-nums` so live updates do not jitter.

**The Italic Means Pending Rule.** Italic is reserved for transient or placeholder text: placeholders, "cancelling…", "waiting for the next change…", "last seen …".

## Layout

**Extracted (desktop, 1280 × 800 canvas):**
- Top bar 56 px, full width; brand mark 24 px square at 20 px from the left; chips 26 px high on the right, avatar 28 px.
- Sidebar 200 px, `panel` background, 1 px `border` on its right edge; nav section label 24 px below the top bar; nav items 36 px tall at a 42 px pitch, inset 10 px; a divider separates the optional items; version line at the bottom.
- Content column: 24 px left/right padding; breadcrumb 28 px below the top bar; page title below it with the primary action right-aligned on the same line.
- Tabs row below the title (tab ≈112 px, 6 px gap, 3 px underline, 1 px baseline across the content width); filter row 34 px controls with 12 px gaps, Live indicator right-aligned.
- Table: header 38 px, rows 46 px, cell inset 16 px; pagination 24 px below.
- Two-column screens (new mission, detail): main card column 560–600 px, 24 px gutter, side column fills the rest. Cards stack with 18–24 px gaps.
- Robots: 4 stat tiles (72 px) and a 3-column card grid, 16 px gaps.

**Spacing rhythm:** 4 / 6 / 8 / 12 / 16 / 24 px (`spacing` tokens); 16 px is the default inner padding, 24 px separates regions.

**Responsive (derived – not in the mockups):**
- **≥ 1100 px:** as the mockups.
- **768–1099 px:** robot grid 2 columns, stat tiles 2 × 2, two-column screens become one column (summary/history card moves below the main card), filter row wraps.
- **< 768 px (designed at 375 px):** sidebar collapses into an off-canvas drawer opened by a menu button at the left of the top bar (focus trapped, closes on Escape and on navigation); top bar keeps the brand and a compact connection dot + sync time, subtitle and user name hide. Content padding 16 px. Tables reflow into one stacked row-card per item: code + status pill on the first line, key/value pairs below, actions as full-width buttons at the bottom. Filters stack full-width. Robot grid 1 column. Dialog becomes full-width minus 16 px margins, buttons stacked (primary action last on the right on desktop, first on top on mobile). Every interactive control is at least 44 × 44 px on this width (`touch-target`); the theme toggle collapses into one button that cycles Light → Dark → System.
- No horizontal page scroll at any width.

## Elevation & Depth

Flat by default. Depth is built from tonal layers (white surface on white page, `panel` for the sidebar and table headers) and 1 px `border` lines. No shadows on cards, tables, tiles or buttons.

### Shadow Vocabulary
- **Dialog** (`box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25)`): only the modal dialog, over a `scrim` at 45 % opacity (dark theme: `dark-scrim` at 60 %, derived, plus a 1 px `dark-border` on the dialog).

### Named Rules
**The One Lifted Object Rule.** Only a modal dialog may cast a shadow. If something needs emphasis, use a border, a tint or a 4 px accent bar – not elevation.

## Shapes

Small, consistent, slightly softened rectangles; pills are fully round.
- **Hairline (2 px):** accent bars (active nav indicator, stat-tile left bar), tab underline (1 px).
- **Tag (3 px):** source tags (API / Sync) in the history timeline.
- **Control (4 px):** inputs, selects, icon buttons (30 × 30 remove-step), pagination buttons.
- **Button (5 px):** all text buttons, nav items, the Cards/Table toggle.
- **Card (6 px):** cards, tables, stat tiles, error panel, battery bar ends.
- **Dialog (8 px):** modal dialog.
- **Pill (full):** status pills (20 px high), count badges (18 px), top-bar chips and the Live indicator (26 px), avatar and timeline dots (circles).

Borders are 1 px everywhere; 1.5 px for chevrons and timeline step rings; 2 px for timeline connectors.

## Components

### Top bar
Navy (`topbar`), 56 px. Left: white 24 px rounded square "ITS" mark, "ITS Middleware" (brand), subtitle "KUKA AMR Fleet Manager" in `topbar-subtitle`. Right: connection chip (green `connected-dot` + "KUKA FM connected"), sync chip ("⟳ synced 3 s ago"), white avatar circle with initials + user name. Chips are status, not buttons. Focus ring inside the bar uses `focus-ring-on-dark`. When disconnected (derived): the dot turns `danger` and the text says "KUKA FM unreachable".

### Sidebar navigation
- **Default:** icon + label, `text`, nav typography, transparent.
- **Hover (derived):** `divider` background (dark: `dark-panel-raised`).
- **Active:** `nav-active` background, 4 px `primary` bar on the left edge, label 700 in `primary`; `aria-current="page"`.
- **Disabled/later:** `text-disabled`, italic helper line – not focusable.

### Page header and breadcrumb
Breadcrumb (12 px, `text-muted`, "Missions / M-2026-0142", segments are links except the last). Below: page title, optionally followed by a status pill (detail) and a muted 13 px subtitle line; right side holds the actions (primary, or secondary + danger outline).

### Tabs with count badges
Text tabs (Active / Finished / All). Inactive: `text-muted`, 400. Active: `primary`, 700, 3 px `primary` underline over a 1 px `border` baseline; count badge switches from `count-badge` to `count-badge-active`. Implement as `role="tablist"` with arrow-key navigation.

### Table
Bordered container (`border`, card radius), header row on `panel` with uppercase table-header text and a sort arrow on the sorted column (`aria-sort`). Rows 46 px separated by `divider`; the mission code is a `primary` 600 link; empty values show "—" in `text-muted`. Row actions are small buttons (26 px): "Detail" secondary, "Cancel" danger outline; while Cancelling, the action slot shows italic `warning-text` "cancelling…". Hover row (derived): `row-highlight`.

### Status pill
20 px, fully round, padding 9 px, pill typography, bg/fg from the status palette; width follows the text. Never interactive. In the history timeline: "previous → new" pills at 11 px.

### Buttons
- **Shape:** 5 px radius, 13 px/600 text, heights 26 (small, in tables), 34 (default), 40 (form footer).
- **Primary:** `primary` fill, white text; hover `primary-hover`. One per view.
- **Secondary / outline:** white, 1 px `control-border`, `text`; hover (derived) `panel` background.
- **Danger outline:** white, 1 px `danger` border, `danger` text; hover (derived) `danger-surface`. Used to start a cancel.
- **Danger solid:** `danger` fill, white text – only the confirming button inside the cancel dialog.
- **Disabled (derived):** 50 % opacity, `cursor: not-allowed`, no hover change. **Busy (derived):** keep width, replace label with "Sending…" and `aria-busy`.

### Inputs and selects
34 px, white, 1 px `control-border`, 4 px radius, 10 px text inset, 13 px text. Label above (12 px/600 `text-muted`, required marked " *"). Placeholder italic `text-muted`. Select: 10 px chevron (1.5 px `text-muted` stroke) 18 px from the right. Search field: leading "⌕" glyph/icon.
- **Error:** border `danger`, message 11 px `danger` 15 px below the field, linked with `aria-describedby`, `aria-invalid="true"`.
- **Focus (derived):** border `primary` + focus ring.
- **Summary of errors:** the error panel (below) lists them.

### Cards and stat tiles
- **Card:** white, 1 px `border`, 6 px radius; optional title row (section title, 16 px inset, 40 px tall) separated by a 1 px `border` line; optional right-aligned meta in the title row ("min. 1 step").
- **Key/value grid:** meta key (11.5 px/600 muted) above value (13.5 px/600), two columns, 42 px row pitch.
- **Stat tile:** 72 px, label 12.5 px/600 muted + stat value coloured with the state fg; 4 px left bar in the state fg; the "Total" tile uses `panel` and no bar.
- **Robot card:** name + id, state pill top-right; Battery row (bar + "78 %" bold right-aligned), Node, Mission link "M-2026-0142 ›"; footer below a `divider` line with "updated 2 s ago". Offline: `offline-surface`, muted text, `battery-offline` fill, footer "⚠ last seen 14 min ago" in italic `warning-text`.

### Battery bar
12 px track (`battery-track`), fully rounded fill coloured by level (see Colors). Always paired with the numeric percentage; expose as `role="meter"` with `aria-valuenow`.

### Live indicator
26 px pill, `live-surface` with `live-border`, 8 px `success` dot, text "Live · refresh 5 s" in `success-text`. When polling fails (derived): switch to warning colours (`status-cancelling-*`) and "Paused · retrying" text; never animate the dot continuously.

### Timeline (status history and steps)
- **History:** 14 px dots in the new status's fg colour, 2 px `border` connectors; time (12.5 px/600 muted), "prev → new" pills, a source tag (API / Sync: `panel`, 1 px `border`, 3 px radius, 11 px/600 muted) right-aligned, message line below. A final dashed hollow dot with italic "waiting for the next change (polling 5 s)…".
- **Steps:** 20 px circles – done: `success` fill with ✓; current: `primary` fill with number and italic "robot is here" right-aligned; to do: white with 1.5 px `primary` ring and `primary` number.

### Dialog (cancel confirmation)
460 px, white, 8 px radius, dialog shadow, scrim. Header: 32 px `danger-surface` circle with a bold `danger` "!" + dialog title. Body 13 px, followed by an italic 12 px muted note ("The action is recorded in the audit log."). Footer right-aligned: secondary "Keep mission" + danger solid "Yes, cancel mission". Uses native `<dialog>`/`aria-modal`, focus moves to the safe (secondary) button, Escape closes, focus returns to the trigger.

### Error panel
`danger-surface`, 1 px `danger-border`, 6 px radius, 16 px padding; heading "⚠ The form contains errors" (13 px/700 `danger-text`), bullet list 12.5 px, explanatory text in `danger-text-soft`. Also used for ProblemDetails messages (400/401/404/409/502) in plain words, with `role="alert"`.

### Empty, loading and error states (derived)
- **Empty:** inside the table/card frame, centred, 13 px muted sentence + one action where it makes sense ("No active missions. Dispatch a new mission." with a primary button). Keep headers visible.
- **Loading (first load only):** 3–5 skeleton rows of `battery-track`-coloured bars at real row height; no shimmer. Later polls never show a loader – data updates in place and the sync chip updates.
- **Error:** error panel with a secondary "Try again" button; if older data exists, keep showing it and add a warning note "Showing data from 13:40:12 – refresh failed".

### Focus ring (derived)
`outline: 2px solid` `focus-ring` (dark: `dark-focus-ring`; inside the top bar: `focus-ring-on-dark`), `outline-offset: 2px`, shown on `:focus-visible` for every interactive element, including table links and tabs. Never removed without a replacement.

### Motion (derived)
Restrained, CSS transitions only (no GSAP needed for these components).
- Durations: `fast` 120 ms (hover, press, focus), `base` 180 ms (pill colour change on a status update, tab underline, row insert fade), `slow` 240 ms (dialog and drawer enter; exit 160 ms).
- Easing: standard `cubic-bezier(0.2, 0, 0, 1)` for enter/state change, exit `cubic-bezier(0.4, 0, 1, 1)`.
- Animate only `opacity`, `transform` (dialog: scale 0.98 → 1 + fade; drawer: translateX) and `background-color`/`color`.
- No looping animation (no pulsing Live dot, no shimmer).
- `prefers-reduced-motion: reduce`: all durations become 0 ms except opacity fades which may stay ≤ 120 ms; no transforms.
- Motion tokens live in `.impeccable/design.json` → `extensions.motion`.

### Copy (English)
UI copy is English; the mockups' Slovak is only the layout reference. Examples: Misie → Missions, Nová misia → New mission, Roboty → Robots, Aktívne / Ukončené / Všetky → Active / Finished / All, Zrušiť → Cancel, "Live · obnova 5 s" → "Live · refresh 5 s", "KUKA FM pripojené" → "KUKA FM connected", "sync pred 3 s" → "synced 3 s ago", Dostupný / Zaneprázdnený → Available / Busy, Batéria / Uzol / Misia → Battery / Node / Mission. Status enum names (InProgress, Cancelling, …) stay as in the mockups. Layout must tolerate longer English strings (pills and buttons size to content).

## Do's and Don'ts

### Do:
- **Do** take every colour, radius and size from the frontmatter tokens (as CSS custom properties); light values are the mockups' values.
- **Do** show every status as a pill with its name, and every battery level with its percentage.
- **Do** keep one primary button per view; cancel actions use danger outline, and only the dialog's confirm button is danger solid.
- **Do** use `control-border` for form controls and `border` only for cards, tables and dividers.
- **Do** update polled data in place, with tabular numbers, without loaders or flashes.
- **Do** provide both themes, the 375 px layout, visible focus and the reduced-motion fallback for every screen.

### Don't:
- **Don't** build the yellow dashed developer notes (`#FFF8C5` / `#D4B106`) from the mockups – they are annotations, not UI.
- **Don't** add shadows to cards, tables or buttons; only the dialog is lifted.
- **Don't** introduce new accent hues, gradients or a web font.
- **Don't** use `text-disabled` for readable content or colour as the only carrier of state.
- **Don't** add KUKA or FEI logos or brand identity; the "ITS" mark is the only logo.
- **Don't** present the mockups' robot and mission codes as real data.
