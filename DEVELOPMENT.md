# The Tipsy Tail — prototype 0.2.2

A timber-and-thatch swim-up pool bar for **Timberborn 1.1.2.4 public**, both factions.

## What's new in v0.2.2

Calm pool water: scrolling textures, ripple motion and foam are disabled for the pool only, with much gentler surface distortion. The two corner seats are removed. Eight visitors now use four bar seats and four swimming lanes. Restart the game and reload your save after updating.


## 0.2.1 demolition fix

Tipsy Tail now uses a bundled, building-specific terrain-cutout controller. It owns one reference per hidden tile and releases cached coordinates on demolition, even during reentrant model callbacks. Previews do not hide terrain. Ground heights, texture data and cutouts owned by other buildings are not changed.

For an already-stranded hole after removing a pool, install this version, restart Timberborn and reload the save. The game rebuilds cutout masks on load. This does not require editing the save or filling terrain manually. The mod includes Scripts/TipsyTail.Runtime.dll; keep it with the installation. Multiplayer users need the same version on every machine.

Eight ownership regression checks and a native-assembly integration harness pass, including a reproduction of the old handler leaking 30 references and the new handler releasing all 30. See terrain-cleanup-validation.json. In-game testing remains outstanding.

## 0.2.0 visual changes

- The unbuilt site uses the game's native dirt-and-stakes construction base, stretched from 5 × 5 to 5 × 6, matching the campfire construction-site style.
- The basin extends two blocks below ground. Two underground block layers reserve soil beneath the footprint; the finished building uses the native terrain cutout. This is the underground-building mechanism, not permanent terrain deletion or simulated river water.
- The floor has 68 individually mapped wooden planks instead of one stretched texture. Basin walls and stool supports extend down to the floor.
- Two low entrance steps rise to the deck at 0.18 blocks above ground. A clear opening and ten-tread ladder lead into the pool.
- Water now uses Timberborn's teal FountainWater, replacing the breeding-pod material with pink foam. The lido uses map water; this contained pool uses native fountain water instead. Actual shader appearance still needs user verification.
- The counter top is 0.51 blocks above ground, 0.32 above the bar-seat anchors. It was lowered by 0.55 relative to the stools. Animation alignment still needs user verification.
- The central hanging tail ornament and its hangers were removed.
- The earlier material-registration and construction-stage crash fixes are retained.

## Placement and existing saves

Requires a level **5 × 6 ground site with two solid soil layers beneath it**, plus three blocks of clearance above ground. Platforms and pre-excavated empty pits do not meet the requirement. Construction makes the recessed basin visible without a separate excavation step. The entrance stays at surface level.

The template ID stays TipsyTail. Native saves store surface elevation separately from the underground offset, so the BaseZ change does not intentionally lower the entire building. Existing placements on thin terrain, platforms, or above other underground buildings may be incompatible. Use a separate test save and rebuild those placements on suitable ground. Save migration has not been tested in-game.

## Gameplay

| Setting | Value |
| --- | --- |
| Factions / category | Folktails and Iron Teeth / Well-being |
| Construction / unlock | 60 logs, 40 planks, 10 gears / 500 science |
| Capacity | Four bar seats, two soaking seats, two swimming lanes |
| Recreation | Tipsy Tail need in Social Life; up to +2 well-being |
| Satisfaction / decay | 0.6 per hour / 0.1 per day |
| Input | Water delivered by haulers; staffed Hauling Post required |
| Reserve | 60 water, plus at most one unit being consumed |
| Drain | 0.5 water/hour, or 12 per game day while operating |
| Staffing / power | No bartender job or power required |

The drain is a chosen constant balance value and continues without visitors. Native paused/blocked building rules pause consumption. Empty pools stop recreation and hide the water; resupply restores operation. The decorative surface has a fixed level and does not gradually fall with inventory. Swimming uses native ForcedSwimming; bar visitors use Sitting. No custom drinking animations are included.

## Installation

Copy Mod contents into Documents/Timberborn/Mods/TipsyTail. Restart Timberborn to load changed assets and blueprints. Enable The Tipsy Tail in Mod Manager. If updating manually, remove the obsolete Buildings/Wellbeing/TipsyTail/TipsyTail.ConstructionStage0.Model.timbermesh. Do not install multiple versions at once.

The mod ZIP contains a TipsyTail folder ready to copy into Mods. The separate source ZIP includes editable Blender assets, scripts and reports. No Unity editor, Blender installation, BepInEx or separately installed dependency is needed to play. A small cleanup DLL is bundled with this version.

## Verification

Offline checks cover both exported meshes, triangle indices and finite buffers, hierarchy, eight visitor slots, swimming endpoints, the switchable water node, the underground footprint, terrain-cutout coordinates, faction material resolution and duplicates, and construction-stage counts. Geometry checks measure floor depth, stool supports, counter height relative to seating, clear stair footprint, deck alignment and removal of the ornament.

See validation.json, geometry-validation.json and material-regression.json. These are **not in-game playtests**. The game was not launched, restarted or controlled for testing, as requested.

User checks remaining: load/place in both factions, confirm terrain cutout and depth placement, inspect water under game lighting and seated adults/kits, confirm hauling and swimming, run dry/refill, and save/reload. Multiplayer has not been tested.

## Assets and rebuilding

Source/TipsyTail.blend is the editable model. Preview, front and side PNGs show the complete model including the normally buried basin. TipsyTail-dry.png shows the floor from above. These are Blender renders with approximate water, not game screenshots.

```text
python Source/build_mod.py PATH_TO_EXTRACTED_BLUEPRINTS
blender -b --python-exit-code 1 --python Source/create_assets.py -- PATH_TO_TIMBERMESH_EXPORTER
blender -b --python-exit-code 1 --python Source/validate_assets.py -- PATH_TO_TIMBERMESH_EXPORTER PATH_TO_EXTRACTED_BLUEPRINTS
blender -b --python-exit-code 1 --python Source/validate_geometry.py
```

Blueprint inputs come from the installed game's StreamingAssets/Modding/Blueprints.zip. The exporter directory is src/timbermesh_blender_plugin in [Mechanistry's Timbermesh repository](https://github.com/mechanistry/timbermesh). Generated with Blender 4.5.3. Geometry and scripts are original; the game supplies referenced materials and the native construction base. The public Blender source uses placeholder materials. Game textures and shaders are not bundled. Set TIMBERBORN_PATH when rebuilding against another local game installation.

To tune drain, change GoodConsumingBuildingSpec.ConsumedGoods[0].GoodPerHour. Inventory is ceil(FullInventoryWorkHours × GoodPerHour). Keep visitor capacity synchronized with model slots.

## Build the bundled terrain controller

Requires .NET SDK 8 and your installed game assemblies. Run before packaging:

```text
dotnet build Source/Runtime/TipsyTail.Runtime.csproj -c Release
dotnet run --project Source/Runtime/Tests/Tests.csproj -c Release
dotnet run --project Source/Runtime/Tests/Native/Integration.csproj -c Release
```

Copy only Source/Runtime/bin/Release/netstandard2.1/TipsyTail.Runtime.dll into Mod/Scripts. Game DLLs are referenced for compilation and tests, not redistributed. Override TimberbornManagedDir when building against a non-default game installation. The native integration harness also specifies its local Managed directory in its project and Program.cs.
