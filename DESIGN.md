---
name: The Tipsy Tail
description: A swim-up pool bar's printed ephemera laid out on its own bar top; oiled timber in daylight or after dusk, pulpboard printed in two inks.
colors:
  bar-honey: "#cfa972"
  bar-dusk: "#3a2a1c"
  pulpboard: "#f1e7d2"
  bark-ink: "#2d2118"
  bark-muted: "#5b4a39"
  on-bar-day: "#2d2118"
  on-bar-day-muted: "#4c3a28"
  on-bar-dusk: "#f3e6cf"
  on-bar-dusk-muted: "#d8c4a3"
  pool-aqua: "#1a6773"
  thatch-ochre: "#74520e"
  pool-aqua-print: "#2c8c9a"
  thatch-ochre-print: "#b8862f"
  link-day: "#1f6a76"
  link-dusk: "#8fd9e2"
  focus-day: "#1f5f6a"
  focus-dusk: "#f1e7d2"
  flap-cream: "#f7efdf"
  step-cream: "#fff8ea"
typography:
  display:
    fontFamily: "Shrikhand, Georgia, serif"
    fontSize: "clamp(3.2rem, 7.4vw, 5.8rem)"
    fontWeight: 400
    lineHeight: 0.95
    letterSpacing: "0.005em"
  headline:
    fontFamily: "Shrikhand, Georgia, serif"
    fontSize: "clamp(2rem, 4vw, 2.9rem)"
    fontWeight: 400
    lineHeight: 1.05
    letterSpacing: "0.005em"
  pitch:
    fontFamily: "Shrikhand, Georgia, serif"
    fontSize: "clamp(1.3rem, 2.4vw, 1.75rem)"
    fontWeight: 400
    lineHeight: 1.2
  coaster-number:
    fontFamily: "Shrikhand, Georgia, serif"
    fontSize: "clamp(2.6rem, 6vw, 4rem)"
    fontWeight: 400
    lineHeight: 1
  sign-small:
    fontFamily: "Shrikhand, Georgia, serif"
    fontSize: "1.6rem"
    fontWeight: 400
    lineHeight: 1.1
  title:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "1.2rem"
    fontWeight: 750
    lineHeight: 1.1
  body:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 400
    lineHeight: 1.62
  label:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "0.94rem"
    fontWeight: 700
    lineHeight: 1.4
  mono:
    fontFamily: "ui-monospace, Cascadia Mono, SFMono-Regular, Consolas, Liberation Mono, monospace"
    fontSize: "0.88em"
    fontWeight: 400
rounded:
  chip: "3px"
  pulpboard: "6px"
  meter: "7px"
  matchbook: "8px"
  flap: "14px"
  ticket: "999px"
  coaster: "50%"
spacing:
  gutter: "clamp(16px, 4vw, 32px)"
  wrap: "1160px"
  narrow: "820px"
  section: "clamp(48px, 7vw, 88px)"
  card: "clamp(22px, 4vw, 36px)"
  grid-gap: "clamp(24px, 5vw, 56px)"
  ruled-row: "9px"
  stack: "12px"
components:
  button-primary:
    backgroundColor: "{colors.pool-aqua}"
    textColor: "#ffffff"
    typography: "{typography.label}"
    rounded: "{rounded.ticket}"
    padding: "0 24px"
    height: "52px"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.on-bar-day}"
    rounded: "{rounded.ticket}"
    padding: "0 24px"
    height: "52px"
  button-ghost-dusk:
    backgroundColor: "transparent"
    textColor: "{colors.on-bar-dusk}"
    rounded: "{rounded.ticket}"
    padding: "0 24px"
    height: "52px"
  card-stock:
    backgroundColor: "{colors.pulpboard}"
    textColor: "{colors.bark-ink}"
    rounded: "{rounded.pulpboard}"
    padding: "{spacing.card}"
  coaster:
    backgroundColor: "{colors.pulpboard}"
    textColor: "{colors.pool-aqua}"
    typography: "{typography.coaster-number}"
    rounded: "{rounded.coaster}"
    width: "27%"
  coaster-ochre:
    backgroundColor: "{colors.pulpboard}"
    textColor: "{colors.thatch-ochre}"
    rounded: "{rounded.coaster}"
  mini-coaster:
    backgroundColor: "{colors.pulpboard}"
    rounded: "{rounded.coaster}"
    size: "104px"
  matchbook-flap:
    backgroundColor: "{colors.pool-aqua}"
    textColor: "{colors.flap-cream}"
    typography: "{typography.sign-small}"
    rounded: "{rounded.flap}"
    padding: "18px 24px 16px"
  step-number:
    backgroundColor: "{colors.thatch-ochre}"
    textColor: "{colors.step-cream}"
    rounded: "{rounded.coaster}"
    size: "32px"
  faq-item:
    backgroundColor: "{colors.pulpboard}"
    textColor: "{colors.bark-ink}"
    rounded: "{rounded.pulpboard}"
    padding: "10px 18px"
    height: "56px"
  nav-link:
    textColor: "{colors.on-bar-day}"
    padding: "0 12px"
    height: "44px"
---

# Design System: The Tipsy Tail

## Overview

**Creative North Star: "Bar Ephemera"**

The site is the bar's own printed matter, laid out on the bar top. The page ground is a plank of oiled timber (a produced texture) under a pale sun wash; after dusk the same bar darkens under a lamp pool. Everything that carries information is a printed piece sitting on that timber: pulpboard coasters, a menu card, a delivery slip, a printed notice, a matchbook, question cards. Each printed piece is pulpboard stock printed in bark ink plus at most two spot inks, pool aqua and thatch ochre. The building itself appears as its real model renders, cut out of their grey studio backgrounds and standing on the bar with a soft contact shadow, always captioned as model renders.

Density is relaxed but exact: big Shrikhand numbers carry the game's figures, and the detail sits in ruled lists that fit their card. Pieces are laid slightly askew (coasters turned −11° to 12°, the slip at −1.2°), the way things land on a counter, but text inside them is never rotated beyond that. The system follows the operating system's light/dark preference; the printed pieces stay daylight pulpboard in both, only the bar under them changes.

The world explicitly replaced a calm editorial template (cream paper, forest green, Georgia, tracked-caps eyebrows, a stat grid, a numbered 01–03 band); none of those return.

**Key Characteristics:**
- Timber bar top as the page ground, textured, with a sun wash (day) or lamp pool (dusk).
- Printed pieces on pulpboard stock: bark ink plus pool aqua and thatch ochre, flat fills.
- Shrikhand lettering for the bar's name, section titles, big numbers and small signs; system-ui for everything read.
- Real renders cut out and standing on the bar, labelled as model renders.
- Pieces set a few degrees askew; soft cast shadows, never hard offsets.
- One signature interaction: coasters turn over on hover or focus.

## Colors

Warm timber and pulpboard, printed in two darkened spot inks over a deep bark ink.

### Primary
- **Pool Aqua** (`pool-aqua`): the first spot ink. Primary buttons, the matchbook flap, links inside printed pieces, "What it gives" and "Tested" titles, aqua coasters, the Tipsy Tail meter, check marks, the FAQ plus. Darkened from the print aqua so it holds small text on pulpboard.

### Secondary
- **Thatch Ochre** (`thatch-ochre`): the second spot ink. Ochre coasters, "What it asks" and "Not confirmed" titles, the Wet fur meter, open-item rings, the matchbook step numbers. Darkened from the print ochre for the same reason.

### Tertiary
- **Print Aqua** (`pool-aqua-print`) and **Print Ochre** (`thatch-ochre-print`): the inks at full brightness. Only where no small text rides on them: text selection (print aqua) and the favicon coaster mark (both).

### Neutral
- **Honey Plank** (`bar-honey`): the daylight bar colour under the bar-day texture; also the light theme-color.
- **Bar After Dusk** (`bar-dusk`): the dark bar colour under the bar-dusk texture; the dark theme-color.
- **Pulpboard** (`pulpboard`): every printed piece, always with the pulpboard fibre texture, in both themes. Also the dusk focus ring.
- **Bark Ink** (`bark-ink`): all text on printed pieces, and text on the daylight bar. Its alpha tints make the pieces' rules (dotted 35%), meter tracks (10%), code chips (7–8%) and the pinned note ground (6%).
- **Faded Bark** (`bark-muted`): secondary text printed on pulpboard (slip heading note, status foot, pinned note).
- **Bar Text** (`on-bar-day` / `on-bar-dusk`) and **Bar Text Muted** (`on-bar-day-muted` / `on-bar-dusk-muted`): text lying directly on the bar (hero, section intros, captions, footer), switched per theme.
- **Links on the Bar** (`link-day` / `link-dusk`) and **Focus** (`focus-day` / `focus-dusk`): per-theme link and 3px focus-ring colours for things lying on the bar.
- **Flap Cream** (`flap-cream`): reversed-out lettering on the aqua matchbook flap and its perforation. **Step Cream** (`step-cream`, #fff8ea), a slightly warmer cream, is the digits on the ochre step-number discs; button labels use plain white.

### Named Rules
**The Two Inks Rule.** A printed piece uses bark ink plus at most pool aqua and thatch ochre, as flat fills. No third hue, no CSS gradients on a piece; the only tonal variation is the pulpboard texture itself.

**The Darkened Ink Rule.** Any ink carrying text, rules or a meter uses the darkened values (`pool-aqua`, `thatch-ochre`). The print values are for selection and marks too big or too wordless to need contrast.

**The Stock Stays Daylight Rule.** Printed pieces never invert in dark mode. Only the bar, the wash and text lying on the bar change with the theme.

## Typography

**Display Font:** Shrikhand (self-hosted woff2, OFL, `font-display: swap`; Georgia, serif fallback)
**Body Font:** system-ui stack
**Label/Mono Font:** ui-monospace stack for file names and paths

**Character:** Shrikhand is the bar's painted and printed lettering: fat, upright, a little tipsy. It is only ever at weight 400 and only in large sizes; the system face does all the reading at heavy weights (650–750) for labels, so the two never compete.

### Hierarchy
- **Display** (Shrikhand 400, clamp(3.2rem, 7.4vw, 5.8rem), 0.95): the bar's name in the hero, as its sign.
- **Headline** (Shrikhand 400, clamp(2rem, 4vw, 2.9rem), 1.05): section titles; inside the matchbook it drops to clamp(1.7rem, 3vw, 2.2rem).
- **Pitch** (Shrikhand 400, clamp(1.3rem, 2.4vw, 1.75rem), 1.2): the one-line pitch under the sign.
- **Coaster Number** (Shrikhand 400, clamp(2.6rem, 6vw, 4rem), 1): the game's figure on a coaster front; 2rem on mini coasters.
- **Sign Small** (Shrikhand 400, 1.6rem): headings on printed pieces (menu columns, slip, flap, footer name); 1.35rem for install notes; 1.5rem for the header brand.
- **Title** (system 750, 1.2rem, 1.1): h3 on the bar and on the status notice.
- **Body** (system 400, 1.0625rem, 1.62): all reading text; lead 1.12rem at 56ch; section intros 1.08rem; narrow column 820px.
- **Label** (system 700–750, 0.8–0.95rem): ruled-list keys, meter names and values (tabular numbers), coaster captions, captions and small print (0.9–0.94rem, 400, muted).

### Named Rules
**The Sign Lettering Rule.** Shrikhand is for names, titles and numbers only, never a sentence longer than the pitch, never bold, never tracked.

**The Game's Numbers Rule.** Every big number is a figure the game uses (8, +2, 0.5, 12, 0.6), set in Shrikhand with a short system-type caption under it.

## Layout

A single centred column capped at 1160px with a fluid gutter (clamp(16px, 4vw, 32px)); the FAQ narrows to 820px, section intros to 740px, the slip to 760px, the coaster stack to 980px. Sections breathe on clamp(48px, 7vw, 88px) vertical padding with no dividers between them: the bar is continuous.

Two-column grids split with a clamp(24px, 5vw, 56px) gap (menu card, status, install), except the hero, which uses clamp(24px, 4vw, 56px); the install grid is weighted 1.15 : 0.85 toward the matchbook. The render row is three columns, bottom-aligned so the cut-outs stand on a common bar line.

The coaster stack is a flex row of four 27%-wide discs overlapping by 2.5% each side, turned −11°, 7°, −3°, 12° and nudged vertically (0, 18px, −6px, 14px) with a deliberate stacking order; the focused or hovered coaster rises to the top.

Responsive: at 900px every two-column grid collapses to one, the hero render centres at 560px wide, the coasters wrap two by two at 52% width. At 560px the renders stack, ruled lists and slip rows put their key above the value, the meter track drops under its label, and mini coasters shrink to 96px. Tap targets hold at least 44px (nav 44, buttons 52, FAQ rows 56).

## Elevation & Depth

Depth is physical: pieces lie on the bar and cast soft, low shadows onto it, each with a 1px bottom lip for the stock's edge. Cut-out renders use a `drop-shadow` filter that follows their silhouette. There are no floating panels, no glows and no hard offset shadows.

### Shadow Vocabulary
- **Stock on the bar** (`box-shadow: 0 1px 0 rgba(0,0,0,.12), 0 14px 22px -16px rgba(40,24,10,.6)`): menu card, slip, notice.
- **Coaster** (`box-shadow: 0 1px 0 rgba(0,0,0,.12), 0 14px 18px -14px rgba(40,24,10,.6)`; mini coaster `0 10px 14px -10px`): each coaster face.
- **Matchbook** (`box-shadow: 0 1px 0 rgba(0,0,0,.15), 0 18px 26px -18px rgba(40,24,10,.7)`): the thickest piece, the deepest shadow.
- **Flap fold** (`box-shadow: 0 6px 8px -4px rgba(0,0,0,.35)`): the matchbook flap over its book.
- **Question card** (`box-shadow: 0 1px 0 rgba(0,0,0,.1)`): FAQ cards, lip only; they lie flat in a pile.
- **Render on the bar** (`filter: drop-shadow(0 22px 18px rgba(40,22,8,.38))`, row renders `0 16px 14px rgba(40,22,8,.35)`).

### Named Rules
**The Lying Flat Rule.** Shadows are cast downward onto the bar with a negative spread and a warm brown tint; nothing hovers. Hover lifts a ticket by 1px, no more.

## Shapes

Round and softly cut, like die-cut pulpboard. Coasters are circles with a printed ring: a pulpboard border (7px, 5px on minis) and an ink outline inset into the face (−15px, −11px on minis) in the coaster's own ink. Cards take 6px corners, the matchbook 8px with its flap rounded 14px at the bottom and edged with a dashed perforation in 60% flap cream. Buttons are pill tickets (999px). Meter tracks round at 7px. Dotted 1.5px rules at 35% bark ink divide every ruled list, slip row and step. Icons are small SVG masks (check, dashed ring, plus) filled with an ink colour.

## Components

### Buttons
Printed tickets: firm, short, one per job.
- **Shape:** pill (999px), 52px tall, 24px side padding, 2px border.
- **Primary:** pool aqua fill and border, white label, 750 weight at 1.05rem.
- **Hover / Focus:** lifts 1px over 0.15s on `cubic-bezier(.2,.8,.2,1)`; focus is the global 3px ring offset 3px.
- **Ghost:** transparent with a bar-text border and label; sits beside the primary on the bar.

### Cards / Containers
- **Corner Style:** 6px.
- **Background:** pulpboard colour with the pulpboard fibre texture, bark ink text, aqua links.
- **Shadow Strategy:** Stock on the bar (see Elevation).
- **Border:** none; rules inside are dotted bark ink.
- **Internal Padding:** clamp(22px, 4vw, 36px).

### Navigation
A plain bar header: Shrikhand brand at 1.5rem on the left, four system-type links at 650 weight on the right, 44px tall, underlined on hover, wrapping on narrow screens. A 2px rule in 35% bark ink closes the header and opens the footer.

### Coaster (signature)
A pulpboard disc with a printed ring in one ink (aqua or ochre). The front carries a Shrikhand number and a short caption; hover or keyboard focus turns it over (0.6s rotateY with `backface-visibility: hidden`) to a back in bark ink with a title and one or two sentences. Both faces stay in the DOM so the back is always readable to assistive tech; under reduced motion the turn happens without animation. Coasters are laid as an overlapping, rotated stack; two mini coasters (104px) sit under the hero render's corner.

### Menu Card
A two-column card: an aqua Shrikhand column title for what the building gives, an ochre one for what it asks. Each column is a ruled list, a 7.5em key column in bold and a value column, one fact per row, dotted rules between. The gives column ends with two need meters and a "Next to the Lido" line above a dotted rule.

### Need Meter
A labelled bar: name (bold), a 14px track in 10% bark ink with a flat ink fill at the value's width, and a right-aligned tabular value ("0.6 an hour"). Aqua for the Tipsy Tail need, ochre for Wet fur. Meters that are compared share one scale.

### Delivery Slip
A pulpboard slip turned −1.2°, centred at 760px: a head with a small aqua Shrikhand title and a muted note over a 2px bark-ink rule, then a definition list of ruled rows.

### Printed Notice
The status as a pulpboard card: a bark-ink Shrikhand headline, two columns titled aqua (confirmed, check marks) and ochre (not yet confirmed, dashed rings), a muted foot line. It carries `data-release-pinned`; release.js may append a pinned note on a 6% bark-ink ground at 4px corners.

### Matchbook
The install as a matchbook: an aqua flap with the bar's name in flap cream and a dashed perforation, folded 10px over the pulpboard book; inside, numbered steps with ochre Shrikhand number discs (32px, digits in #fff8ea) and dotted rules, then the download ticket; a produced striker strip (22px) along the bottom under a 2px bark rule.

### Question Card
FAQ items as pulpboard cards (6px, lip shadow, 10px apart): a bold 56px summary row with an aqua plus mask that turns 45° to an x when open (0.2s), answer padded 18px.

## Do's and Don'ts

### Do:
- **Do** put information on a printed piece (pulpboard with texture, bark ink) and let the bar carry only the sign, the pitch, section intros, renders and captions.
- **Do** print each piece in bark ink plus pool aqua and/or thatch ochre, using the darkened ink values for any text, rule or meter.
- **Do** set the game's own numbers in Shrikhand with a short system-type caption.
- **Do** lay pieces a few degrees askew (within about ±12° for coasters, about 1° for large cards) and keep text horizontal inside anything larger.
- **Do** cut renders out and stand them on the bar with a silhouette drop shadow, captioned as model renders.
- **Do** keep every printed piece's texture and raster produced by the repository scripts, with a provenance sidecar next to it.
- **Do** keep interactive targets at 44px or more and every turnover or open state reachable by keyboard focus.

### Don't:
- **Don't** bring back the replaced template: cream paper ground, forest green, Georgia headings, tracked-caps eyebrows or kickers, a stat grid, a numbered 01–03 band.
- **Don't** use hard offset shadows, glows or floating panels; pieces lie on the bar.
- **Don't** set sentences, body copy or bold weights in Shrikhand.
- **Don't** add a third spot ink or a CSS gradient to a printed piece.
- **Don't** invert the pulpboard pieces for dark mode.
- **Don't** present a render or an edited render as an in-game screenshot.
