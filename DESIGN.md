---
name: Bremmer Artistry
description: A working potter's journal and events site — pieces logged like kiln-side ledger entries, not shelved like a catalog.
colors:
  kiln-char: "#0c2435"
  shelf: "#122c39"
  shelf-line: "#2c4b5c"
  bisque: "#ffffff"
  bisque-dark: "#9aabb3"
  copper-glaze: "#a6885c"
  copper-glaze-dim: "#7d6845"
  copper-glaze-bright: "#aa8e64"
  paper: "#ffffff"
  paper-line: "#d0d4d5"
  ink: "#0c2435"
  ink-soft: "#5b6b73"
typography:
  display:
    fontFamily: "Fraunces, serif"
    fontSize: "clamp(2rem, 5vw, 3.2rem)"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: normal
  headline:
    fontFamily: "Fraunces, serif"
    fontSize: "clamp(1.5rem, 3vw, 2.2rem)"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: normal
  title:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1.1rem"
    fontWeight: 500
    lineHeight: 1.3
    letterSpacing: normal
  body:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.75
    letterSpacing: normal
  label:
    fontFamily: "IBM Plex Mono, ui-monospace, monospace"
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: 1.4
    letterSpacing: "0.06em"
rounded:
  none: "0px"
  sm: "2px"
  pill: "999px"
spacing:
  xs: "0.4rem"
  sm: "0.75rem"
  md: "1.5rem"
  lg: "2.5rem"
  page-x: "clamp(1rem, 4vw, 3rem)"
components:
  button-primary:
    backgroundColor: transparent
    textColor: "{colors.paper}"
    rounded: "{rounded.sm}"
    padding: "0.65rem 1.2rem"
  button-primary-hover:
    backgroundColor: "{colors.copper-glaze}"
    textColor: "{colors.kiln-char}"
  chip:
    backgroundColor: transparent
    textColor: "{colors.bisque-dark}"
    rounded: "{rounded.pill}"
    padding: "0.3rem 0.65rem"
  chip-active:
    backgroundColor: "{colors.copper-glaze-dim}"
    textColor: "{colors.paper}"
  nav-link:
    textColor: "{colors.bisque-dark}"
  nav-link-active:
    textColor: "{colors.copper-glaze}"
---

# Design System: Bremmer Artistry

## 1. Overview

**Creative North Star: "The Kiln-Lit Ledger"**

A dim studio at night, the only warmth coming off the kiln — dark walls, a shelf of glazed work
catching copper light, and every piece logged the way a shop keeps its ledger: dated, plainspoken,
by hand. The system reads as documentation of real work happening, not a display case built to sell
it. Density stays low and unhurried; nothing on the page is trying to close a sale in the next five
seconds. It rejects, by name, the cold corporate/SaaS-template gallery — no stock hero-metric
layouts, no tech-startup gloss, no gradient-and-glass polish standing between the visitor and the
pottery.

**Key Characteristics:**
- Dark, warm-neutral base with a single copper-gold accent used sparingly
- A dedicated light "paper" register (the worksheet) that only appears for the detail view — the
  one place the system shifts from workshop-dark to ledger-paper-light
- Monospace type for anything procedural (nav, labels, dates, filters); serif for anything that
  names or introduces a piece or section; sans for anything read at length
- Flat by default — depth is reserved for exactly one moment (the worksheet card), not scattered
  across every surface

## 2. Colors

A dark, warm-neutral studio palette with one saturated accent; a separate light "paper" register
exists only for the worksheet detail view.

### Primary
- **Copper Glaze** (`#a6885c`): The single accent. Marks active/selected state, links, focus, and
  hover fills. Used sparingly — outlines and borders at rest, solid fill only on interaction.
- **Copper Glaze Dim** (`#7d6845`): The same accent desaturated/darkened for "already active" states
  (selected filter chips, current calendar view toggle) so the brighter Copper Glaze stays reserved
  for hover/focus.
- **Copper Glaze Bright** (`#aa8e64`): The same accent lightened just enough to clear WCAG AA's
  4.5:1 text-contrast floor on Shelf backgrounds at small label sizes (e.g. `.event-date`'s
  0.72rem mono label) — base Copper Glaze only reaches 4.36:1 there. Use in place of base Copper
  Glaze for small procedural/label text sitting directly on Shelf; base Copper Glaze remains
  correct everywhere else (links, larger text, Kiln Char backgrounds, hover/focus fills).

### Neutral
- **Kiln Char** (`#0c2435`): The page background across the entire dark register — deep
  navy-charcoal, not black. Also doubles as **Ink** (`#0c2435`), the body text color on the light
  paper register — same value, opposite role: page-dark in one context, text-dark in the other.
- **Shelf** (`#122c39`): Raised surface on the dark register — card backgrounds, input fields,
  photo placeholders before an image loads.
- **Shelf Line** (`#2c4b5c`): Borders and dividers on the dark register — nav underline, filter bar
  rule, card outlines.
- **Bisque** (`#ffffff`): Primary text color on the dark register (headings, wordmark) and,
  identically, the worksheet's paper background — same hex, deliberately dual-purpose: light text
  on dark surfaces, and the light surface itself when the register flips.
- **Bisque Dark** (`#9aabb3`): Secondary/muted text on the dark register — body copy, captions,
  nav links at rest.
- **Paper Line** (`#d0d4d5`): Borders and dividers on the light paper register (worksheet field
  rules), the light-register counterpart to Shelf Line.
- **Ink Soft** (`#5b6b73`): Secondary/muted text on the light paper register — the worksheet's
  counterpart to Bisque Dark. Deliberately darker than Bisque Dark's value despite both being
  "muted secondary text," since Ink Soft sits on a white/paper background and needs to clear
  WCAG AA's 4.5:1 contrast floor there, not the dark register's.

### Named Rules
**The One Accent Rule.** Copper Glaze is the only saturated color in the system. It never appears
as a background at rest — only as an outline, a link, or a hover/active fill. If a second accent
color is ever needed, that's a sign the design is drifting toward "full palette," not this system.

**The Two Registers Rule.** The dark register (Kiln Char/Shelf/Bisque) is the site's default state.
The light paper register (Bisque-as-background/Ink/Paper Line) is reserved for exactly one context:
the piece worksheet. Don't introduce a third register, and don't let the light register bleed into
list/grid views — the flip from dark to light is what makes the worksheet feel like a distinct,
physical object.

## 3. Typography

**Display Font:** Fraunces (serif)
**Body Font:** Inter (with system-ui fallback)
**Label/Mono Font:** IBM Plex Mono

**Character:** Fraunces carries warmth and craft into every headline — its optical sizing keeps it
from reading as generic "elegant serif." Inter stays plain and legible at body sizes. IBM Plex Mono
marks anything procedural (nav, dates, filters, field labels) as workshop-ledger language: uppercase,
tracked out, quietly technical.

### Hierarchy
- **Display** (600, `clamp(2rem, 5vw, 3.2rem)`, 1.1): Hero headline only (Home page `h1`).
- **Headline** (600, `clamp(1.5rem, 3vw, 2.2rem)`, 1.2): Section headings, Pottery Journal page
  title, content-page `h1`.
- **Title** (500–700, 0.92–1.2rem, 1.3): Card/tile titles — event card `h3`, gallery tile title.
  Weight varies by context (500 for gallery tiles, 700 for event cards) but size and role stay
  consistent.
- **Body** (400, 0.85–1rem, 1.6–1.85): Paragraph copy — hero intro, FAQ answers, worksheet notes.
  Cap prose measure at 65–75ch; the existing `.content-page` max-width (720px) already enforces
  this.
- **Label** (400, 0.65–0.85rem, letter-spacing 0.02–0.1em, uppercase where used): Nav links, filter
  labels, chips, dates, event tags, worksheet field labels/values.

### Named Rules
**The Ledger-Language Rule.** Anything that records a fact rather than makes a statement (a date, a
filter, a field label, a nav item) is set in IBM Plex Mono, usually uppercase and tracked. Anything
that introduces or names something (a headline, a piece title, an event name) is set in Fraunces or
Inter. Don't set procedural information in the display serif — it reads as decoration, not record.

## 4. Elevation

Flat page, one lifted object. Every dark-register surface (nav, cards, tiles, calendar, filter bar)
sits at the same visual depth — borders and background-tone shifts (Shelf vs. Kiln Char) carry
hierarchy, not shadow. The single exception is the piece worksheet: a real shadow
(`0 18px 40px rgba(0, 0, 0, 0.45)`) lifts it off the dark page like a physical card set on a
workbench. That contrast is the point — depth is earned by the one object in the system meant to
feel like a discrete, handled object, not applied as ambient polish everywhere.

### Shadow Vocabulary
- **Worksheet lift** (`box-shadow: 0 18px 40px rgba(0, 0, 0, 0.45)`): Reserved for the piece detail
  worksheet. Do not reuse on cards, tiles, or nav.
- **Tag drop** (`box-shadow: 0 2px 4px rgba(0, 0, 0, 0.35)`): The small rotated specimen tag on each
  gallery tile — a much smaller, tighter shadow than the worksheet lift, appropriate to a paper tag
  rather than a whole card.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest everywhere except the worksheet. If a new
component reaches for a drop shadow "to make it pop," that's the wrong tool — use a border or a
Shelf-vs-Kiln-Char tone shift instead. Shadows in this system mean "this is a physical object," not
"this is elevated UI."

## 5. Components

Understated until touched: buttons, chips, and nav links stay quiet — outlines and muted tone at
rest — and only take on the warm copper fill on hover, focus, or active state. Restraint at rest is
what makes the accent color mean something when it does appear.

### Buttons
- **Shape:** Sharp, near-flat corners (2px radius) — never pill-shaped except chips/toggles.
- **Primary:** Transparent background, 1px Copper Glaze border, Bisque/Paper text, mono label
  styling (uppercase, 0.06em tracking), padding `0.65rem 1.2rem`.
- **Hover / Focus:** Background fills Copper Glaze, text flips to Kiln Char. Transition on
  `background` and `color`, 0.15s ease.

### Chips (filter chips, view toggle)
- **Style:** No background at rest, 1px Shelf Line border, Bisque Dark text, fully rounded (999px),
  mono label styling.
- **State:** Hover shifts border to Copper Glaze Dim and text to Bisque/Paper. Active/selected fills
  Copper Glaze Dim background with Bisque/Paper text — the dimmer accent variant, keeping full-
  brightness Copper Glaze reserved for momentary hover/focus.

### Cards / Containers
- **Corner Style:** Square (no radius) for event cards, gallery tiles, calendar cells.
- **Background:** Shelf on the dark register.
- **Shadow Strategy:** None — see Elevation. Depth comes from the Shelf-vs-Kiln-Char background
  contrast plus a 1px Shelf Line border, not shadow.
- **Border:** 1px Shelf Line.
- **Internal Padding:** `1.25rem 1.4rem 1.5rem` (event card body); tiles have no internal padding,
  image bleeds to the tile edge.

### Inputs / Fields
- **Style:** Shelf background, 1px Shelf Line border, Bisque/Paper text, mono type, 2px radius.
- **Focus:** Border shifts to Copper Glaze plus a soft `0 0 0 2px rgba(175, 137, 88, 0.35)` glow —
  the only place a glow effect is used in the system; reserve it for input focus, not decoration.

### Navigation
- Mono, uppercase, 0.06em tracked, Bisque Dark at rest. Hover brightens to Bisque/Paper. The active
  page gets Copper Glaze text with a matching Copper Glaze underline (`border-bottom`) — the only
  sanctioned underline-as-indicator in the system; don't add underlines elsewhere as decoration.

### The Worksheet (signature component)
The piece detail view is the system's one deliberately physical object: a Bisque/Paper card with a
1px Ink border, lifted with the Worksheet-lift shadow, laid out as a bordered field table (mono
labels, Inter values) with a photo box bearing hand-drawn-style corner marks and a notes/glaze
section below. It is the "specimen card" the whole North Star points toward — every other surface in
the system stays quiet so this one can read as the object worth stopping for.

### Lightbox
The Gallery's only overlay: a fixed, full-viewport Kiln Char scrim (`rgba(7, 38, 56, 0.92)`) behind
a large centered photo, with close/prev/next controls in the quiet-until-touched idiom (1px Shelf
Line border, Copper Glaze on hover/focus). A mono caption below the image states the piece title and
position (`"Bowls — 20250924_110013 — 4 of 14"`), browsing every photo in the current Gallery
category, not just the clicked piece's own images — the Journal's per-piece worksheet photo nav is a
separate, narrower interaction. First user of the system's z-index scale (`z-index: 100` for the
scrim; nothing else in the system stacks yet, so no fuller scale exists until a second layer is
needed).

## 6. Do's and Don'ts

### Do:
- **Do** keep Copper Glaze (`#a6885c`) as the only saturated color in the system — outline/link/hover
  only, never a background at rest.
- **Do** reserve the light paper register (Bisque background, Ink text, Paper Line borders) for the
  worksheet detail view alone; every list/grid/nav surface stays on the dark register.
- **Do** set procedural information (dates, labels, filters, nav) in IBM Plex Mono, uppercase and
  tracked; reserve Fraunces for headlines and piece/event names.
- **Do** keep buttons, chips, and nav quiet at rest — border and muted tone only — so the copper fill
  on hover/active actually reads as a state change.
- **Do** limit real drop shadows to the worksheet card and its tag; every other surface stays flat,
  with borders and Shelf/Kiln-Char tone shifts carrying hierarchy instead.

### Don't:
- **Don't** build a cold corporate/SaaS-template gallery — no stock hero-metric layouts, no
  gradient-and-glass polish, no tech-startup gloss standing between the visitor and the pottery
  (direct PRODUCT.md anti-reference).
- **Don't** add a second saturated accent color; if a screen seems to need one, that's drift toward
  a different color strategy than this system uses.
- **Don't** apply the worksheet's drop shadow (or any new drop shadow) to cards, tiles, or nav —
  shadow means "physical object," and only the worksheet earns that.
- **Don't** set a headline, nav label, or button in the wrong register's neutral colors (e.g. Ink on
  the dark background, or Bisque Dark on the light paper) — each register has its own paired text
  colors and they don't cross over.
- **Don't** use pill-shaped (999px) corners outside chips and the view/calendar toggle buttons —
  every other component stays sharp (0–2px radius).
