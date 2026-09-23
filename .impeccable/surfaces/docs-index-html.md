---
version: 1
slug: "docs-index-html"
primary_target: "docs/index.html"
related_targets: []
---

# Surface brief: The Tipsy Tail site (docs/index.html, one page)

Scope: docs/index.html (Persuade, with the install and FAQ as Read sections on the same page). Audience: players looking for another well-being building, mostly single player, some co-op; weighing the 500-science unlock against the Lido or Swimming Pool they already have. Action: understand what the building is, gives (Tipsy Tail need in Social Life, up to +2 well-being, plus Wet fur at the Lido's rate: an extra source, not a replacement) and asks (5 × 6 level site, two solid soil layers below, three blocks clearance, a path, a staffed Hauling Post with water; 60 logs, 40 planks, 10 gears; 500 science), install it right (the -mod.zip under Assets, the TipsyTail folder with manifest.json directly inside, one version, enable, restart), keep it supplied, report problems. Proof: the Blender renders (labelled as renders), the exact numbers. Constraints: CI (Source/validate_version.py) needs at least one data-release-pinned="1.0.1" on a container that can take an appended pinned-note, and the exact markup data-release="tag">v1.0.1< (attribute last, no whitespace); release.js shared and byte-identical, loaded with data-repo="timbermods/timberborn-tipsy-tail" data-asset="-mod\.zip$"; download/notes links fall back to /releases/latest; the prerelease span keeps hidden; og:image stays images/TipsyTail-preview.png; anchors #install and #details; honest status (earlier builds played; the lake-water look, surface swimmers and water.cfg in a renamed folder not confirmed in game); no version history on the page. Decisions delegated to the agent.

## Direction contract

THESIS: The Tipsy Tail's site is the bar's own printed ephemera, laid out on the bar top: a stack of pulpboard coasters, each printed with one fact in two inks, a matchbook with the install steps inside the flap, and the menu card for what the building gives and asks. The building itself (the real renders, cut out) sits on the bar. It refuses the calm editorial template it wears now (cream paper, forest green, Georgia, tracked-caps eyebrows, a stat grid, a numbered 01–03 band).

OWN-WORLD: A bar top of warm oiled timber in daylight (light: honey plank #cfa972 under a pale sun wash; dark: the same bar after dusk, #3a2a1c under lamplight), pulpboard coasters (#f1e7d2, a produced fibre texture) printed in two spot inks: pool aqua (#2c8c9a, lighter #7fcfd8 on dark) and thatch-straw ochre (#b8862f), with a deep bark ink (#2d2118) for text. Shrikhand (display, self-hosted, OFL) for the bar's name, coaster titles and the big numbers, the way a bar's printed matter letters them; body system-ui. Two inks per printed piece, flat, slightly misregistered at most by a hairline; no gradients.

STORY: One look: a swim-up pool bar for your colony, eight beavers at once, a need of its own plus Wet fur, up to +2 well-being; what it asks. Then: the coasters (seats, well-being, the site, water), the menu card of what it gives and asks next to the Lido, the renders on the bar, keeping it supplied and why it stops, what is tested and what isn't, the matchbook install, the FAQ.

FIRST VIEWPORT: Left: "The Tipsy Tail" lettered as the bar's sign, the pitch "A swim-up pool bar for your beavers.", a two-sentence lead that names the need, Social Life, +2 well-being and Wet fur, the requirements line (5 × 6 on solid ground, a staffed Hauling Post with water, 500 science), Download (the tag fallback as CI needs it) + Install; right: the three-quarter render cut out and set on the bar top with two coasters under its corner (8 seats · +2 well-being).

FORM: Bar Ephemera, candidate 7 of 7 (seed 42ee40ab). Challengers weighed: jazz title card (competitive: flat colour fields and tight grotesk titles; kept its discipline: flat fields, no gradients), mixtape j-card (kept: ruled lists that fit their card exactly), vu-meter bridge (kept: the two needs drawn as simple meters that agree), HyperCard, coil tower, bioluminescent sea (declined). Raises taken: every printed piece has one job and two inks; the requirements fit on one card; numbers are always the game's numbers. Signature: the coaster stack, each coaster turnable on hover/focus to its back with the detail (motion off under reduced motion; the back is always readable in text for screen readers).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
