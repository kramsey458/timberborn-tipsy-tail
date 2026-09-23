# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Timberborn players looking for another well-being building, mostly in single player; some play co-op with friends
(every player needs the same mod and game version). Mostly non-technical: they arrive from the Timbermods catalog
(https://timbermods.github.io/), the repo's README or a friend's link, want to see in one look what the building is
and whether it fits their colony, and then install it without breaking their game. Some are deciding whether it is
worth the unlock (500 science) against the Lido or Swimming Pool they already have. Returning players come back to
update to a new version or to report something odd in their colony.

## Product Purpose

The website for **The Tipsy Tail** (https://github.com/timbermods/timberborn-tipsy-tail), a Timberborn mod that adds
one building: a timber-and-thatch swim-up pool bar, recessed two blocks into the ground, that haulers keep supplied
with water. Eight beavers use it at once, four on bar seats and four in swimming lanes. It relieves its own need
(Tipsy Tail, in Social Life, up to +2 well-being) and the game's native Wet fur need.

Success, in order:
1. The visitor understands what the building is and what it asks of their colony: a level 5 × 6 site with two solid
   soil layers beneath and three blocks of clearance above, a path to the entrance, and a staffed Hauling Post with
   access to water.
2. They download the right file (`TipsyTail-v….-mod.zip` under **Assets**, not "Source code" and not the
   water-presets or public-source zips) and install it right: the `TipsyTail` folder in `Documents\Timberborn\Mods`,
   `manifest.json` directly inside `Mods\TipsyTail`, one version only, enabled in the mod manager, game restarted.
3. They unlock it under Well-being, build it on suitable ground and keep it supplied, knowing why it stops (no water)
   and what a platform or pre-dug pit does (it doesn't qualify).
4. When something's wrong, they send a useful report: a GitHub issue with game and mod versions, faction, other
   mods, multiplayer or not, a screenshot and any exception from `Player.log`.

## Positioning

A swimming venue you can put on any level, solid ground, fed by haulers instead of built into the map's water. The
game's own swimming buildings (the Lido and the Swimming Pool) relieve Wet fur; the Tipsy Tail relieves Wet fur at the
same 0.5 points per hour and adds a separate need of its own, so it is an extra source of well-being, not a
replacement. Both factions get it. It adds no water to the map simulation and needs no other mod (no Harmony, no
BepInEx). It is the one building mod in the Timbermods catalog; don't compare it with other mods beyond that.

## Operating Context

- **Current release: v1.0.1**, a maintenance release of the first full release (v1.0.0), published 2026-09-22 as a
  stable GitHub **Latest** release. Release assets: `TipsyTail-v1.0.1-mod.zip` (the mod),
  `TipsyTail-v1.0.1-water-presets.zip` (optional diagnostics), `TipsyTail-v1.0.1-public-source.zip` and
  `TipsyTail-v1.0.1-SHA256SUMS.txt`. `release.js` fills the version and zip link on the site.
- Built for Timberborn **1.1.2.4** (manifest `MinimumGameVersion` 1.1.2.4); other game versions are unverified. Mod
  name in the mod manager: **The Tipsy Tail** (manifest Id `Kyler.TipsyTail`, `RequiredMods` empty).
- **Factions:** Folktails and Iron Teeth, under **Well-being**.
- **Cost and unlock:** 60 logs, 40 planks, 10 gears; 500 science. Under construction it shows the game's native
  dirt-and-stakes site (campfire style).
- **Placement:** 5 × 6 level ground footprint; two solid soil layers underneath (the basin is an underground block,
  revealed with the game's native terrain cutout); three blocks of clearance above. Platforms and empty pre-dug pits
  don't qualify. Demolition reveals the original ground again.
- **Input:** Water, delivered by haulers from a staffed Hauling Post. No bartender job, no workers, no power. Reserve
  60 water (plus at most one unit being consumed); drains 0.5 per hour, 12 per game day, while operating, even with
  no visitors. The game's normal pause and blocking rules pause consumption. It does not draw river or lake water.
- **Visitors:** eight (four bar seats, beavers sitting; four swimming lanes, beavers swimming at the water surface).
- **Effects:** Tipsy Tail need (Social Life group) at 0.6 per hour, decaying 0.1 per day, up to +2 well-being; native
  Wet fur need at 0.5 per hour, the same as the Lido and Swimming Pool. Both scale with the building's supply: when
  the water runs out, recreation, wet fur relief and the pool surface stop together, and all three return on resupply.
- **Look:** the pool is drawn with the game's own lake water (it darkens with depth, shows the floor and swimmers
  through the shallows, ripples and glints). The surface has a fixed height; it doesn't fall as the reserve drains.
- **Optional `water.cfg`** in the mod folder (read from wherever the game loaded the mod, every few seconds, no
  restart): presets for the default game water (its `log` line writes the pool's real values to Player.log), the older
  `legacy` ripples, and the untouched vanilla fountain water. Appearance only; no effect on gameplay or multiplayer.
- **Multiplayer:** every player installs the same mod version and runs the same game version.
- **Upgrading:** replace the old `TipsyTail` folder with the new one, keep only one version installed, restart and
  reload the save. Saves from v1.0.0 and v0.2.x load unchanged. Pools placed before v0.2.0 on thin ground or
  platforms may need rebuilding.
- **Reporting:** https://github.com/timbermods/timberborn-tipsy-tail/issues/new/choose (bug report template: game
  version, mod version, faction, other enabled mods, multiplayer yes/no, what happened, steps, expected, screenshot or
  log exception). `Player.log` is in `%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn`; remove personal
  information before posting.

## Capabilities and Constraints

- **Stack and hosting:** one static page, `docs/index.html`, with `docs/style.css`, `docs/release.js`, `docs/images/`
  and `docs/.nojekyll`. Plain HTML/CSS, no build step, no framework, system fonts. GitHub Pages (legacy build) serves
  `main:/docs` at https://timbermods.github.io/timberborn-tipsy-tail/, so the site goes live when the change is merged
  to `main`. It is one of the timbermods sites (MixedStorage's and MultiColony's are siblings) and should stay light
  and work at phone width.
- **Contracts CI enforces** (`.github/workflows/tests.yml` runs `python Source/validate_version.py` on every pull
  request and push to `main`; the version is chosen only in `<Version>` in `Source/Runtime/TipsyTail.Runtime.csproj`):
  - `docs/index.html` must contain **at least one** `data-release-pinned="1.0.1"` (a leading `v` is allowed), and
    **every** `data-release-pinned` must name the current version.
  - `docs/index.html` must contain **at least one** element whose markup reads exactly `data-release="tag">v1.0.1<`,
    and every such fallback must name the current version. The regex needs `data-release="tag"` as the **last
    attribute** of the tag, immediately followed by `>` and the text `v` + version; any reordering, extra attribute
    after it, whitespace or wrapped text breaks the check.
  - The same check covers README.md (release links, asset names `TipsyTail-v….-`, the first `## What's new in v…`
    heading, the `**v… is ` status line), DEVELOPMENT.md's title and first What's new heading, RELEASE_NOTES.md, the
    manifest and the runtime DLL. No other site test exists: nothing checks links, images, `hidden` or layout.
- **`release.js` contract (not tested, but the page depends on it):** the page loads `release.js` with
  `data-repo="timbermods/timberborn-tipsy-tail"` and `data-asset="-mod\.zip$"` (so the lookup picks the mod zip, never
  the water-presets or source zip). Download buttons use `data-release-href="download"` and notes links
  `data-release-href="notes"`, each with a working fallback `href` to `/releases/latest` (right for this repo: it has
  a stable Latest release). The "Pre-release ·" label carries `data-release-show="prerelease"` and the `hidden`
  attribute; don't give it a CSS `display` that defeats `hidden`. A `data-release-pinned` element gets a
  `<p class="pinned-note">` appended when a newer release exists, so it must be a container that can take one.
  `release.js` is a byte-for-byte copy shared across timbermods sites (identical to MixedStorage's): replace it with
  the shared version, never edit it.
- **Social card:** `og:image` is the absolute URL
  `https://timbermods.github.io/timberborn-tipsy-tail/images/TipsyTail-preview.png`; keep it resolving.
- **Terminology** as in game and README: The Tipsy Tail (building and mod name); Tipsy Tail need; Social Life;
  Well-being; Wet fur; Water; Hauling Post (staffed); haulers; Lido; Swimming Pool; Folktails; Iron Teeth; bar seats;
  swimming lanes; reserve; Assets; `Documents\Timberborn\Mods`; `water.cfg`; `Player.log`. Use `5 × 6` with the
  multiplication sign.
- **Sources of truth:** README.md (player facts), DEVELOPMENT.md (mechanics and validation), RELEASE_NOTES.md (the
  current release), the blueprints and `Mod/Localizations/enUS.csv`. There is no separate CHANGELOG file: version
  history lives in the README's "Earlier versions" section and the GitHub release notes. Where the site and README
  disagree, flag it; don't guess.
- **Describe the mod as it is now.** The current page still carries history (the pinned "In version 1.0" list with
  "New in 1.0.1", and an earlier-versions FAQ); history belongs in the changelog. Keep only the upgrade facts players
  need (replace the folder, one version installed, restart and reload, which saves load unchanged, pre-v0.2.0
  placements may need rebuilding). Whatever replaces the pinned block must still satisfy the `data-release-pinned`
  check above.
- **Honest status:** earlier builds were played in game, including multiplayer with matching versions (players
  reported the too-bright v0.2.3 water and a stutter in multiplayer host sessions). The current release has **not been
  confirmed in game** for: the lake-water look (v0.2.8), swimmers at the surface (v1.0.0; "worth a glance in your own
  colony"), and `water.cfg` in a renamed folder (v1.0.1, proven by a native harness). The recreation, wet fur and
  supply wiring was audited in the game's decompiled code, and offline checks against the game's own files cover the
  model, blueprints, materials, geometry, wet fur, terrain cutout and water shader data; none of them launches the
  game. Migration of pre-v0.2.0 placements hasn't been playtested. Say this plainly, without scaring people off.

## Brand Commitments

- Voice: a fellow player sharing a fun building. Warm and a little playful where the game itself is ("Leave your
  worries on the shore. Bring your own tail." is the building's in-game flavor text), then clear and exact for
  numbers, placement and install. Never hype.
- No official Timberborn logos or key art. The game's own item icons (Water, Log, Plank, Gear, science) are allowed
  where used and credited as Timberborn's; the current site uses none.
- License: MIT for the original project files (model, generator scripts, blueprints, localization, runtime code,
  docs, site), copyright 2026 Timbermods. Timberborn, its materials, animations and construction-base assets belong to
  Mechanistry and are not covered. Meshes were exported with Mechanistry's Timbermesh exporter (see CREDITS.md).
- Unofficial community mod, not affiliated with or endorsed by Mechanistry. Maintained by Timbermods; the footer links
  to https://github.com/timbermods and the catalog at https://timbermods.github.io/.

## Evidence on Hand

- **Blender model renders, not game screenshots** (1400 × 1200 PNG, about 2 to 2.5 MB each), in `docs/images/`:
  `TipsyTail-preview.png` (three-quarter view; hero and og:image), `TipsyTail-front.png` and `TipsyTail-side.png`
  (not used on the page yet), `TipsyTail-dry.png` (top-down view of the floor planks and ladder, drained). They show
  the whole model including the lower basin, which is buried in normal placement, on a flat grey background, and the
  water as a flat pale-blue approximation, not the game's lake water. Every use needs a caption saying so.
- `Mod/thumbnail.png` and `Mod/Buildings/Wellbeing/TipsyTail/TipsyTailIcon.png` (256 × 256, the same render, used
  in game as the mod thumbnail and building icon).
- The Blender source (`Source/TipsyTail.blend`) could produce further renders; the water presets are text files.
- **No in-game screenshots or clips exist** (CREDITS.md says the page shows model renders). Leave marked slots for the
  maintainer's own shots (the pool in a colony, beavers at the bar and swimming, the dry pool, the building panel)
  and never fake or paint over a render to pass it off as in-game.
- No testimonials, download counts, player quotes or press. Don't invent any.

## Product Principles

1. **Show what it asks of the colony.** Footprint, soil depth, clearance, cost, unlock and a staffed Hauling Post
   with water are the facts that decide whether it works; never bury them.
2. **Exact numbers, as the game has them.** 8 visitors, 60 water, 0.5 per hour (12 per day) even when empty, +2
   well-being, Wet fur at 0.5 per hour; state what stops when it runs dry and what brings it back.
3. **Renders labelled as renders.** The model images are honest about being Blender renders with approximate water
   and a basin that is buried in game; real screenshots replace them only when the maintainer has taken them.
4. **Install right the first time.** The mod zip under Assets, the `TipsyTail` folder in `Documents\Timberborn\Mods`,
   one version only, restart; same version for every player in multiplayer.
5. **Honest about what's been played, and about the mod as it is now.** Checks and code audits are named as such;
   version history lives in the changelog.
