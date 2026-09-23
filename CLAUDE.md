# CLAUDE.md

The Tipsy Tail: a Timberborn mod that adds one building, a swim-up pool bar (`Mod/`: blueprints, localization, the
runtime DLL; `Source/`: the Blender model, generator/validator scripts, `Source/Runtime/` C# runtime and its `Tests`).
Changes land on `main` by PR → merge. CI (`.github/workflows/tests.yml`, on every PR and push to main) runs:
`dotnet run --project Source/Runtime/Tests/Tests.csproj -c Release` (8 terrain + 58 water grid + 6 config path checks
pass) and `python Source/validate_version.py` (20 of 20 copies agree). Building the DLL, `build_mod.py` and the other
validators need the installed game or Blender; see DEVELOPMENT.md.

## Standing rules

- Never launch or drive Timberborn, and never touch installed mods or saves. The maintainer (Kyler) playtests himself.
- Commit on a branch and open a PR. Merge only when Kyler says so in the chat.
- Assume fresh games: no old-save compatibility notes beyond the upgrade facts in PRODUCT.md.
- The version is chosen only in `<Version>` of `Source/Runtime/TipsyTail.Runtime.csproj`; every other copy is written
  by hand and checked by `validate_version.py` (see step 2 below).

## Website

- **Where:** `docs/`: one page, `index.html`, with `style.css`, `release.js`, `favicon.svg`, `fonts/`, `images/`,
  `.nojekyll`. Live at https://timbermods.github.io/timberborn-tipsy-tail/.
- **Published:** GitHub Pages (legacy build) serves `main:/docs`, so merging to main publishes; a build takes about a
  minute. No build step.
- **Look:** "Bar Ephemera", a poolside bar. The page is the bar's own printed matter on its timber bar top: coasters,
  a menu card with need meters, a delivery slip, a printed status notice, a matchbook install, question cards, and the
  cut-out model renders standing on the bar. The look is fixed: updates extend it and never restyle it.
- **Design records (read these before any site change):**
  - `PRODUCT.md`: the facts, voice, and every site contract (CI, release.js, og:image, terminology, honest status).
  - `DESIGN.md`: the visual system and its named rules, the source of truth for the look.
  - `.impeccable/surfaces/docs-index-html.md`: the direction contract.
  - `.impeccable/design.json`: tokens and component snippets.
  - `.impeccable/critique/`: the pre-redesign critique.

### Design rules (from DESIGN.md; keep them)

- **Two Inks**: a printed piece is bark ink #2d2118 plus at most pool aqua and thatch ochre, flat. No third hue, no
  CSS gradient on a piece; the only tonal variation is the pulpboard texture.
- **Darkened Ink**: any ink carrying text, rules or a meter uses `--aqua` #1a6773 / `--ochre` #74520e. The print inks
  #2c8c9a / #b8862f are only for `::selection` and the favicon.
- **Stock Stays Daylight**: pulpboard pieces (#f1e7d2 + `pulpboard.webp`) never invert in dark; only the bar, the wash
  and text lying on the bar change.
- **Sign Lettering**: Shrikhand only for names, titles and numbers, weight 400, never a sentence, never bold/tracked.
- **Game's Numbers**: every big number is a figure the game uses, in Shrikhand with a short system-type caption.
- **Lying Flat**: shadows fall down onto the bar, warm brown, negative spread; nothing hovers; tickets lift 1px.
- Colour roles: aqua = primary buttons, matchbook flap, links in pieces, "What it gives"/"Tested", the Tipsy Tail meter,
  checks, FAQ plus. Ochre = "What it asks"/"Not confirmed", the Wet fur meter, dashed open rings, step discs.
- Tokens, in `:root` of `style.css`, dark overrides in the one `@media (prefers-color-scheme: dark)` block:
  `--bar` #cfa972 → #3a2a1c, `--bar-tex` bar-day → bar-dusk.webp, `--wash` wash-day → wash-dusk.webp, `--on-bar`
  #2d2118 → #f3e6cf, `--on-bar-muted` #4c3a28 → #d8c4a3, `--link` #1f6a76 → #8fd9e2, `--focus` #1f5f6a → #f1e7d2.
  Unchanged: `--stock` #f1e7d2, `--ink` #2d2118, `--muted` #5b4a39, `--aqua`, `--ochre`. Flap lettering #f7efdf,
  step-disc digits #fff8ea (`step-cream`).
- Fonts: Shrikhand 400 only, self-hosted `docs/fonts/shrikhand-latin-400-normal.woff2` (OFL, `OFL-Shrikhand.txt`
  alongside, preloaded); everything read is the system-ui stack (650–750 for labels), paths in ui-monospace. No other
  webfonts, nothing from a CDN at runtime.
- Textures and art: `bar-day/bar-dusk.webp` (planks), `pulpboard.webp`, `wash-day/wash-dusk.webp`, `striker.webp`, made
  by `docs/images/make_textures.py` (numpy + Pillow, fixed seeds 6101–6104). The render cut-outs
  `tipsytail-{preview,front,side,dry}-cut.webp` come from `docs/images/make_cutouts.py` over the original Blender
  renders `TipsyTail-{preview,front,side,dry}.png` (corner-sampled grey keyed to a soft matte, trimmed, ≤1000px). Run
  both from `docs/images/`. Change a script and re-run it rather than editing images, then record provenance
  (`$IMP` as in step 7): `"$IMP/scripts/impeccable" embed-prompt <file> --prompt "Origin: ..."` (writes the `.json` sidecar; `embed-prompt
  --scan .` finds missing ones). Keep `TipsyTail-preview.png`: it is the og:image. Renders are always captioned as
  model renders; never present one as an in-game screenshot.
- Themes: light and dark follow the OS only (`prefers-color-scheme`); there is no toggle and no storage key. Check both.
- Phones: no horizontal scroll at 390px; tap targets ≥ 44px (nav 44, buttons 52, FAQ rows 56). Grids collapse at
  900px; at 560px renders stack and ruled rows put the key above the value.
- Motion: the coaster turnover (0.6s rotateY on hover/focus, both faces in the DOM, `tabindex="0"`). Everything
  respects `prefers-reduced-motion` (the global rule in style.css kills transitions).
- Don't: bring back the replaced template (cream paper ground, forest green, Georgia headings, tracked-caps
  eyebrows/kickers, a stat grid, a numbered 01–03 band); hard offset shadows, glows or floating panels; body copy or
  bold in Shrikhand; a third ink or a gradient on a piece; nested cards; side-stripe accents; official Timberborn logos
  or key art; stock or generated imagery.
- New components: build them from these tokens and the components in DESIGN.md, match the neighbouring pieces, and add
  them to DESIGN.md and `.impeccable/design.json`.

### Content rules

- Describe the mod as it is now. No "New in 1.x", "added in …" or version history on the page; that lives in the
  README's "What's new"/"Earlier versions", RELEASE_NOTES.md and the GitHub release notes.
- The status notice ("What is tested, and what isn't") matches the README's release status and PRODUCT.md's honest
  status exactly. Never invent numbers, reviews, download counts or screenshots.
- Every game number comes from the mod's files; re-check them there, never from memory:
  - `Mod/Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint.json`: 8 at once (`EnterableSpec.CapacityFinished`; four
    `BarSeat` slots sitting, four `Swimming` patrol slots); 0.6 an hour Tipsy Tail and 0.5 an hour Wet fur
    (`AttractionSpec.Effects[].PointsPerHour`); water drain 0.5 an hour (`GoodConsumingBuildingSpec…GoodPerHour`, so 12
    a day) and the 60 reserve (`FullInventoryWorkHours` 120 × 0.5); 60 logs, 40 planks, 10 gears (`BuildingCost`);
    500 science (`ScienceCost`); 5 × 6 (`BlockObjectSpec.Size` X/Y); two soil layers (`UndergroundModelDepth` 2);
    Well-being (`ToolGroupId`).
  - `Mod/Needs/Need.Beaver.TipsyTail.blueprint.json`: +2 well-being (`FavorableWellbeing`), Social Life
    (`NeedGroupId`), decays 0.1 a day (`DailyDelta`).
  - `Mod/TemplateCollections/*.Folktails|IronTeeth.blueprint.json`: both factions. `Mod/manifest.json`:
    `MinimumGameVersion` 1.1.2.4, name "The Tipsy Tail". `Mod/Localizations/enUS.csv`: in-game text and flavour line.
  - Clearance (three blocks), "platforms/pre-dug pits don't qualify": README "Placement and balance", DEVELOPMENT.md.
- Meter fills are `style="--v: <points per hour>"` on one shared 0–1 scale; change `--v` with the number.
- Keep the credits and the footer's "unofficial … not affiliated with or endorsed by Mechanistry" line and Timbermods
  links. Terminology per PRODUCT.md: Tipsy Tail need, Social Life, Well-being, Wet fur, staffed Hauling Post, haulers,
  Lido, Swimming Pool, bar seats, swimming lanes, Assets, `Documents\Timberborn\Mods`, `water.cfg`, `Player.log`, `5 × 6`.
- `docs/release.js` is shared across timbermods sites and byte-identical (md5 b64ba248…): replace it, never edit it.
  It is loaded with `data-repo="timbermods/timberborn-tipsy-tail" data-asset="-mod\.zip$"`; keep the
  `data-release-href` fallbacks to `/releases/latest` and the `hidden` "Pre-release ·" span.

### Update the website for a new release

When asked to "update the website for the latest release, consistent with the design":
1. Read the release and the docs: `gh release list -R timbermods/timberborn-tipsy-tail -L 3`,
   `gh release view v<X> -R timbermods/timberborn-tipsy-tail`, README, RELEASE_NOTES.md, DEVELOPMENT.md's "What's
   new", and `git diff v<old>..v<X> -- Mod/` for changed numbers. List every player-facing change.
2. Bump the version copies (`grep -rn "1\.0\.1" docs README.md DEVELOPMENT.md RELEASE_NOTES.md`). The validator reads
   the csproj `<Version>` and then requires, with regexes: in `docs/index.html` at least one and every
   `data-release-pinned="v?X.Y.Z"` (on `.status.card-stock`; it must stay a container release.js can append a
   `<p class="pinned-note">` to), and at least one and every `data-release="tag">vX.Y.Z<` (the hero's
   `<span data-release="tag">v1.0.1</span>`: attribute last, no whitespace, text exactly `v` + version). Outside the
   site: README release links (`/releases/download|tag/vX`), every `TipsyTail-vX-` asset name, the first
   `## What's new in vX` and the `**vX is ` status line; DEVELOPMENT.md's title and first What's new heading;
   RELEASE_NOTES.md's title and asset names; `Mod/manifest.json` (via `build_mod.py`) and the rebuilt DLL.
3. Update every place the site states a changed fact: `<title>`, meta description, og:title/description; the hero
   lead, `.asks` line and small print ("For Timberborn 1.1.2.4 · Folktails and Iron Teeth"); the two hero mini coasters
   (8, +2); the four `.coaster` fronts and backs; the `.menu-card` ruled lists ("What it gives"/"What it asks"), the
   two meters and the "Next to the Lido" line; the `.slip` rows (reserve, drain, runs dry, map water); the status
   notice's Tested / Not confirmed lists and foot; the matchbook `.steps` (asset name pattern `TipsyTail-v…-mod.zip`)
   and `.install-notes` (Updating, Co-op); the FAQ `details.q` items; PRODUCT.md's Operating Context and honest status.
4. Put new content into the existing components: a new fact → a `.ruled-list` row or a slip row; a new headline figure
   → a coaster (keep four in the stack); played/unplayed items → the status `.checks` / `.checks.open` lists; a new
   install or update step → the matchbook `.steps` or `.install-notes`; a question → a new `details.q`. Remove
   claims that stopped being true. Don't restyle anything.
5. Test: `python Source/validate_version.py` (must print "N of N copies agree"; 20 of 20 at 1.0.1) and
   `dotnet run --project Source/Runtime/Tests/Tests.csproj -c Release` (exit 0). Nothing else tests the site.
6. Preview: `python -m http.server 8787 -d docs` (background), open http://localhost:8787/. With the personal
   `impeccable-site-flow` skill: `python ~/.claude/skills/impeccable-site-flow/scripts/capsite.py http://localhost:8787/
   <out-dir> ""` (desktop, dark and 390px phone; must report overflow 0); otherwise the Browser pane in both colour
   schemes at desktop and mobile. Check the changed sections, hover/focus a coaster, then stop the server.
7. Optional but recommended: run the detector,
   `IMP=$(ls -d ~/.claude/plugins/cache/impeccable/impeccable/*/skills/impeccable | tail -1); "$IMP/scripts/impeccable"
   detect --json docs` (exits 2 when it finds anything; parse from the first `[`). Known false positives (all 20
   findings; there is no `.impeccable/config.json`): `cramped-padding` on mini-coaster, menu-card, status and book
   (clamp and nested padding); `flat-type-hierarchy` (misreads the Shrikhand h2); `#fff` button labels and the
   coaster `outline` currentColor; `border-radius: 4px` on the skip link and pinned note; off-ramp sizes 1.12rem lead,
   .9/.8rem captions, 1.02rem coaster-back h3, 1.7rem mini number, clamp(1.7rem, 3vw, 2.2rem) matchbook h2 (all
   documented in DESIGN.md prose). Anything beyond these is new: fix it or record it in DESIGN.md.
8. If the look changed (a new component or layout), update DESIGN.md and `.impeccable/design.json`.
9. Update the README if it repeats the facts.
10. Ship: branch → commit → push → `gh pr create`. After Kyler says merge: `gh pr merge <n> --merge` (that publishes
    `main:/docs`), then verify:
    - `gh api repos/timbermods/timberborn-tipsy-tail/pages/builds/latest -q .status` is `built`;
    - `curl -s https://timbermods.github.io/timberborn-tipsy-tail/ | grep -c 'data-release="tag">v<X><'` is 1.

### Full redesign

A new look goes through the whole Impeccable flow (init → critique → audit → direction → build → finish review →
DESIGN.md). With the personal skill: "use the impeccable-site-flow skill to redesign this site".
