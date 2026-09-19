# The Tipsy Tail

A self-contained swim-up pool bar for **Timberborn**. Built from timber and thatch, supplied by haulers, and made for eight beavers to unwind.

**[Download v0.2.2](https://github.com/kramsey458/timberborn-tipsy-tail/releases/download/v0.2.2/TipsyTail-v0.2.2-mod.zip) Â· [Project website](https://kramsey458.github.io/timberborn-tipsy-tail/) Â· [Report a bug](https://github.com/kramsey458/timberborn-tipsy-tail/issues/new/choose)**

![The Tipsy Tail model preview](docs/images/TipsyTail-preview.png)

*Blender model preview with approximate water. The lower basin is buried in normal placement.*

## What's new in v0.2.2

Calm pool water: scrolling textures, ripple motion and foam are disabled for the pool only, with much gentler surface distortion. The two corner seats are removed. Eight visitors now use four bar seats and four swimming lanes. Restart the game and reload your save after updating.


## Prototype status

**v0.2.2 is a prerelease targeting Timberborn 1.1.2.4.** Offline asset, blueprint, material and geometry checks pass. This release has not yet been verified in-game; water rendering, terrain cutouts, beaver alignment and save/reload need playtesting. Compatibility with other patches and multiplayer is unverified.

## Features

- A 5 Ã— 6 pool with an attached bar, four bar seats and four swimming lanes.
- A basin extending two blocks underground, with individually mapped wooden floor planks and an entry ladder.
- Water hauled from your settlement: 60-unit reserve, draining 12 units per day while operating.
- Recreation stops when dry; water and recreation return when supplies arrive.
- Native teal fountain water and a campfire-style dirt-and-stakes construction site.
- Available to both Folktails and Iron Teeth under Well-being.

## Install

1. Download **TipsyTail-v0.2.2-mod.zip** from [Releases](https://github.com/kramsey458/timberborn-tipsy-tail/releases/tag/v0.2.2), rather than GitHub's automatic source-code archive.
2. Close Timberborn and extract the included **TipsyTail** folder into your `Documents/Timberborn/Mods` folder. `manifest.json` must be directly inside `Mods/TipsyTail`.
3. Enable **The Tipsy Tail** in Mod Manager and restart when prompted.
4. Start with a separate test save. Unlock the building under Well-being, connect the front entrance to a path, and provide a staffed Hauling Post with access to water.

Upgrading: replace the old TipsyTail folder with the new one; keep only one version installed. No Unity editor, Blender installation, BepInEx or other mod is required to play.

## Placement and balance

| Requirement | Value |
| --- | --- |
| Ground footprint | 5 Ã— 6, level ground |
| Depth | Two solid soil layers underneath; no platforms or pre-dug empty pits |
| Above-ground clearance | Three blocks |
| Construction | 60 logs, 40 planks, 10 gears |
| Unlock | 500 science |
| Visitors | Eight |
| Recreation | Separate Tipsy Tail Social Life need; up to +2 well-being |
| Input | Water, delivered by haulers |
| Capacity / drain | 60 water / 0.5 per hour while operating |

The drain continues without visitors, but native pause/block rules pause consumption. The surface has a fixed height; this mod does not add water to the map simulation. No bartender job or custom drinking animation is included.

The depth requirement changed in v0.2.0. Old placements on platforms, thin terrain or above other underground buildings may need rebuilding. Save migration has not been playtested.

## What's new in v0.2.1

Demolition now releases the pool's terrain cutout without changing the original soil. A bundled cleanup controller handles repeated callbacks and avoids separate preview cutouts. **To clear a hole left by an already-removed pool, update, restart and reload your save.** The game recreates its temporary cutout map during loading. Keep the bundled `Scripts/TipsyTail.Runtime.dll`; no external framework is needed.

An offline native-assembly test reproduced the old leak and verified the replacement under the same callbacks, plus rotation, relocation and construction-cancellation tests. In-game and multiplayer verification remain outstanding.

## What's new in v0.2.0

Recessed the basin by two blocks, replaced the stretched floor with individual planks, rebuilt the entrance steps, added a pool ladder, switched the purple breeding-pod water to teal fountain water, lowered the counter relative to seated beavers, removed the hanging ornament, and adopted the native dirt-and-stakes construction site. Earlier material and construction-stage crash fixes are retained.

## Development and feedback

See [DEVELOPMENT.md](DEVELOPMENT.md) for rebuild commands, limitations and validation details. The procedural Blender source is in `Source/`. Native game textures and shaders are referenced by name, not bundled in the playable mod.

For a bug report, include your game/mod versions, faction, screenshot and relevant exception from `Player.log`. Remove personal information from logs before posting them publicly.

Unofficial community mod. Timberborn and its game assets belong to Mechanistry. See [CREDITS.md](CREDITS.md).
