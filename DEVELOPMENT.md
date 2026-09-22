# The Tipsy Tail — 1.0.0

A timber-and-thatch swim-up pool bar for **Timberborn 1.1.2.4 public**, both factions.

## What's new in 1.0.0

First full release. An audit against the game's own code (Timberborn 1.1.2.4, decompiled) confirmed the gameplay wiring. `Attraction` applies both `AttractionSpec` effects, the Tipsy Tail need and Wet fur, to every visitor for as long as they stay, multiplied by the building's efficiency; `GoodConsumingBuilding` is that efficiency provider and reports 0 once the reserve and the unit being consumed are both gone, so recreation, wet fur relief and the pool surface (`GoodConsumingAttractionSurfaceController`) stop together and return on resupply. The need is filtered through `FactionNeedService`, which is why both need-collection appends are needed. `FixedSlotManager` throws unless there are at least as many `#Slot#` nodes as the capacity of eight; the model has four `#Slot#BarSeat` and four `#Slot#Swimming` nodes, each lane with one `#MiscStart` and one `#MiscEnd` child, which is exactly what `SlotRetriever` looks for. `BuildingAccessibleSpec.LocalAccess` is transformed through the block object's placement, so its Y of 2.0 is the base-relative ground level in the same way the native Underground Pile uses 1.0. Consumption pauses with the native pause and blocking rules, and the game's `LocalizationLoader` falls back to the English text (with a log warning) for languages the mod does not ship.

One correction came out of it. The game keeps a swimming slot at the water surface (`PatrollingSlot.MoveDestinationToWaterLevel` for `WaterSlot: true`, which is how the Lido and the Swimming Pool place swimmers), but the exported lanes sat 0.64 blocks above ground, 0.48 above the pool surface, so swimmers floated. The runtime now moves every `#MiscStart` and `#MiscEnd` under a `#Slot#Swimming` node to the surface height of the `#PoolWater` placeholder when the building wakes and again after initialization, taking positions through the building root so it is independent of the building's own transform and safe to repeat. The model is unchanged (Blender is not needed).

The legacy fountain path also no longer touches the game-water object during the frame it is being destroyed. Version strings, docs and the website are updated for the release.

## 0.2.8 the game's own water

The game's own water. The pool surface is drawn with `PhysicalWater_Opaque`, the material the map's lakes and rivers are drawn with, instead of an imitation built from the fountain material. That shader takes nothing from the mesh but a cell coordinate, a vertex index and a flag mask per vertex (UV0); every vertex position and every colour comes from a set of map-sized texture arrays that the game's `WaterRenderer` and its `WaterColumnPostprocessor` compute shader fill from the simulation each tick: `_NewWaterData` (depth, floor and ceiling per column), `_NewEdgeLinks`, `_NewCornerLinks`, `_NewBaseCornerLinks`, `_NewSkirts`, a 4 × 4-per-cell `_NewWaterHeights`, `_NewOutflows`, `_NewContaminations`, `_NewWaterfalls`, their `_Old*` twins and `_WaterSourceMask`, all set as hidden shader globals together with `_MapSize`. The colour by depth comes from the scene depth buffer and the see-through shallows from the camera opaque texture, so the pool floor drawn under the surface shades it exactly as a lake bed does.

`TipsyTailWaterGrid` (pure C#) builds a private copy of all that for one pool: a tiny map, the pool's cells plus one empty cell of padding and two layers because the shader reads the column above, describing still, clean water of the pool's depth and nothing else, plus a mesh in the game's own layout of 48 vertices and 126 indices per cell with the same vertex flags, taken from the decompiled `WaterMesh` and `CalculateWaterVertex.hlsl`. Cells follow the world's tiles clipped to the exported surface's rectangle, so the water stops at the basin walls, and the unlinked outer sides drop the shader's own skirts down to the basin floor inside the walls. `TipsyTailPhysicalWater` uploads the arrays as `Texture2DArray`s and binds them with `_MapSize` on the renderer's property block and on private copies of the opaque and transparent water materials, so they take precedence over the game's globals for this renderer only, and parents the mesh under `#PoolWater` at the world origin (the shader needs absolute positions). The object sits on the Water layer like the game's tiles, so the game's water back-face depth pass covers it, and it follows the game's `_WaterOpacity` toggle by switching to `PhysicalWater_Transparent`. The map water, its textures and the shared materials are untouched. Per frame the cost is a transform check and one global float read per pool.

Floor and height are absolute world heights: the surface height is the exported placeholder's top, the basin floor is two blocks (the blueprint's underground depth) below the ground level obtained by flooring that height, so the level-visibility cut-away behaves as for the lake. The exported `#PoolWater` mesh stays as a placeholder (its renderer is disabled) and is still what placement previews show. If the water materials cannot be loaded, or `water.cfg` says `legacy`, the pool falls back to the v0.2.7 look below, and the presets are now the default game water, the legacy ripples and the untouched fountain water.

`Source/validate_pool_water.py` now checks, against the installed game's shader graph, HLSL, blueprints and the exported model, that every texture array the water shader reads is bound by the mod and is a hidden global, that the vertex flag bits, skirt vertex sources, edge and corner link channels, water data channel order and 4 × 4 height texels match `WaterUtils.cginc`, `WaterVertexParameters.cginc`, `CalculateWaterVertex.hlsl` and `GetSamplingParameters.hlsl`, that the material paths match `Configurations/WaterMesh.blueprint.json`, that the surface sits within one block above ground and the basin depth matches the blueprint, and that the legacy path is intact. `Source/Runtime/Tests` gains 58 checks of the generated mesh and data map. In-game appearance has not been verified.

## 0.2.7 self-driven ripples (superseded by 0.2.8)

Self-driven ripples. In v0.2.6 the two normal maps were left to scroll on `_NonlinearTime`, assuming the game feeds it. The shader graph shows that property is not exposed (`m_GeneratePropertyBlock` is false, unlike every property the mod overrides), so it is a hidden global that a per-renderer override cannot be relied on to change, and it did not move the pool at all. The runtime now zeroes every shader clock (`_WaterRippleSpeed`, `_Albedo_Speed`, `_Albedo_Speed2`, and the two bump-speed multipliers that only scale `_NonlinearTime`) and animates UV1 itself. `TipsyTailWaterTuner.Update` integrates a drift offset in blocks every frame from `Time.unscaledDeltaTime` (capped at 0.05 s so a hitch never makes the ripples jump), with the heading wandering 35 degrees either way over 90 seconds, and `TipsyTailWaterMesh.Animate` rewrites the four UV1 vertices of the shared surface quad, so the cost does not depend on the number of pools and there is no allocation. It only runs while some pool renderer is visible. `_BumpMap2Tiling` is negative, so the second ripple layer drifts opposite to the first as the UV1 slides.

The default speed is 0.02 blocks per second. Sampling the real `Water_N` texture across the pool with the mod's tilings gives the time for the ripple pattern to lose half its correlation: about 12 s at 0.005, 6 s at 0.01, 3 s at 0.02, 2 s at 0.03 and 1.2 s at 0.05, and at 0.2 the pattern is different after one second. `speed k` in `water.cfg` overrides it, and `Source/validate_pool_water.py` asserts that the default stays between 0.005 and 0.05.

The validator now also asserts that `_NonlinearTime` is unexposed, that every overridden property is exposed, that all five clock multipliers are zeroed, that the mod never sets `_NonlinearTime`, that the second layer's tiling is negative and that `Animate` is wired in. The presets in `water-presets/` are now about speed: default, slower, livelier and no motion, plus the untouched vanilla fountain water, darker, lighter and finer variants. In-game appearance has not been verified; opaque water still cannot be see-through.

## 0.2.6 still-lake water (motion revised in 0.2.7)

Still-lake water. The shader graph shows that raw Unity Time reaches the material only through three speed multipliers (`_WaterRippleSpeed`, `_Albedo_Speed`, `_Albedo_Speed2`), and that the two normal maps are the only layers that also use the game's own `_NonlinearTime` (set by `Timberborn.TimeSystem`'s `NonlinearAnimationManager`). The runtime zeroes the three Time multipliers, so the albedo, gloss and noise are static like a still lake and only the normal maps move, at the lake's own speeds (0.01, 0.008 and -0.008, -0.01) and tilings (0.1 and 0.14 in lake units, set as 0.1 and 0.14 divided by the default UV scale). Bump strengths are the lake's (0.5, 0.75), the gloss scale is 2.2 so the sun throws crisp glints, and the tint is a slate teal-blue, `(0.15, 0.38, 0.46)`, because the earlier saturated blue rendered as blue plastic. The `Source/validate_pool_water.py` check now asserts that every Time node is gated by one of the three multipliers and that the runtime zeroes all three.

Limit: every standalone water material in the game (fountain, breeding pod, bad water, the water good) is opaque; only `PhysicalWater_*` is transparent, and its vertex program is a water-tile engine that needs about ten simulation texture arrays. So the pool cannot be see-through or depth-shaded like the lake.

The `water.cfg` check now runs every 2 seconds and reads the file only when its size or timestamp changes, with a content check every 10 seconds. The presets in `water-presets/` were replaced with a diagnostic set: no motion, the previous raw-time motion and the untouched vanilla fountain water, plus darker, lighter and finer variants. In-game appearance has not been verified.

## 0.2.5 water surface fix (look revised in 0.2.6)

Water surface fix. `FountainWaterURP` animates every layer through UV1: the albedo scroll, both normal maps, the gloss map, the noise blend and the ripple offset all sample through the one connected UV node, which reads channel 1. The exported `#PoolWater` node only has `position`, `normal`, `tangent` and `uv0` (the game's Timbermesh loader maps vertex properties by name, `uv0`, `uv1` and so on), so UV1 was zero everywhere. Every pixel sampled the same point in each texture and the whole surface pulsed together; a simulation with the game's textures shows zero spatial variation and large uniform swings over time. The v0.2.2 freeze and the v0.2.3 flat colour treated symptoms, and v0.2.3 also rendered far too bright.

The runtime now replaces the surface mesh on `#PoolWater`, per instance, with a single upward quad built from the original bounds, keeping UV0 and adding UV1 in block units multiplied by `DefaultUv1Scale` (0.14). The material is double sided, so one quad is enough. 0.14 comes from the lake's tilings being 0.07 to 0.16 times the fountain's (`water-material-reference.json`, read from the game with `Source/inspect_water_materials.py`). `_WaterRippleSpeed` is scaled with the UV so the drift in blocks per second stays put, the bump speeds are the lake's own, and the tint stays a dark blue because the native albedo texture is dark and a bright tint blows out. Everything else is the game's own animated material.

Live tuning: if `water.cfg` exists in the mod folder it is compared by content every half second and applied to every pool. Commands are `native`, `uv1 on|off`, `uv1scale k`, `color`, `float`, `vector`, `mul`, `tex` and `log`; `log` writes the real mesh and material values to Player.log. The presets are in `water-presets/`. The override is renderer-local and cannot affect gameplay or multiplayer.

In-game appearance has not been verified. The UV scale and speeds are estimates from the lake material.

## 0.2.4 wet fur

Wet fur. `AttractionSpec.Effects` now lists `WetFur` at 0.5 points per hour after the Tipsy Tail's own 0.6, so all eight visitors satisfy the native need. That matches the Lido and Swimming Pool, the two native swimming venues, which share the same rate. Wet fur is in `NeedCollection.Common`, so neither faction needs a need-collection change. Because the effect lives in `AttractionSpec`, it is gated by the same building efficiency as recreation and stops while the reserve is empty. The building description mentions it.

The change is generated by `Source/build_mod.py`. Only the blueprint, localization and manifest differ from v0.2.3; the model and runtime code are unchanged (the DLL is rebuilt only to carry version 0.2.4). `Source/validate_wet_fur.py` checks the need, its availability in both factions and the native rate against the installed game's blueprints (wet-fur-validation.json). In-game behaviour has not been verified.

## 0.2.3 still pool water (superseded by 0.2.5)

Still pool water. `FountainWaterURP` builds its colour from scrolling texture layers blended through a noise map, a detail layer and depth-based foam, and takes specular and normals from textures too. Freezing the scroll speeds in v0.2.2 left those layers as static blotches, dark patches and sparkle specks. The bundled runtime now replaces every texture input on the pool's `#PoolWater` renderer with a constant: white albedo, a neutral detail layer, a flat gloss map, zero normal-map strength and zero metallic. Foam is pushed out of range with a negative offset, which also avoids the divide by zero the v0.2.2 override could hit. The result is one flat, glossy surface in Timberborn's own water tint, (0.197, 0.573, 0.708) from WaterOutputParticleColors, at smoothness 0.7. Overrides are renderer-local property blocks, so the shared material, fountains and map water are untouched.

Only `Scripts/TipsyTail.Runtime.dll` changed relative to v0.2.2; the model, blueprint and balance are identical. `Source/validate_pool_water.py` checks every overridden property against the installed game's shader graph and evaluates the foam and albedo math with the real constants (pool-water-validation.json). The in-game appearance has not been verified; the tint and smoothness are named constants in `Source/Runtime/TipsyTailPoolWater.cs` for tuning.

## 0.2.2 calm water and swimming slots

Scrolling textures, ripple motion and foam were disabled for the pool only, with much gentler surface distortion. The two corner seats were removed. Eight visitors use four bar seats and four swimming lanes.


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
| Capacity | Four bar seats, four swimming lanes |
| Recreation | Tipsy Tail need in Social Life; up to +2 well-being |
| Wet fur | Native Wet fur need, 0.5 points per hour (same as Lido and Swimming Pool) |
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

In-game checks worth repeating after any change: placement in both factions, the terrain cutout and depth, seated adults and kits, swimmers at the water surface, hauling, run dry and refill, and save/reload. Multiplayer has been played with matching versions on both sides.

## Assets and rebuilding

Source/TipsyTail.blend is the editable model. Preview, front and side PNGs show the complete model including the normally buried basin. TipsyTail-dry.png shows the floor from above. These are Blender renders with approximate water, not game screenshots.

```text
python Source/build_mod.py PATH_TO_EXTRACTED_BLUEPRINTS
blender -b --python-exit-code 1 --python Source/create_assets.py -- PATH_TO_TIMBERMESH_EXPORTER
blender -b --python-exit-code 1 --python Source/validate_assets.py -- PATH_TO_TIMBERMESH_EXPORTER PATH_TO_EXTRACTED_BLUEPRINTS
blender -b --python-exit-code 1 --python Source/validate_geometry.py
python Source/validate_pool_water.py
python Source/validate_wet_fur.py
python Source/inspect_water_materials.py   # needs UnityPy; refreshes water-material-reference.json
```

`validate_pool_water.py` reads the installed game's `Shaders.zip` and `Blueprints.zip`, the exported pool model, `Source/Runtime/TipsyTailPoolWater.cs` and `Source/Runtime/TipsyTailWaterGrid.cs`. It confirms that the mod binds every data texture the game's water shader reads and that each is a hidden global, that the generated mesh layout and data channels match the water shader's HLSL, that the material paths match the game's water mesh configuration, that the model and blueprint agree with the runtime's height assumptions, and that the legacy fountain path is intact. It rewrites pool-water-validation.json. The `Tests` project also checks the generated mesh and data map without Unity.

`validate_wet_fur.py` reads the installed game's `Blueprints.zip` and the generated mod blueprint. It confirms the Wet fur need exists and is in the common collection, that the Tipsy Tail lists it exactly once at the same rate as the native Lido and Swimming Pool, and that every effect resolves for both factions. It rewrites wet-fur-validation.json.

Blueprint inputs come from the installed game's StreamingAssets/Modding/Blueprints.zip. The exporter directory is src/timbermesh_blender_plugin in [Mechanistry's Timbermesh repository](https://github.com/mechanistry/timbermesh). Generated with Blender 4.5.3. Geometry and scripts are original; the game supplies referenced materials and the native construction base. The public Blender source uses placeholder materials. Game textures and shaders are not bundled. Set TIMBERBORN_PATH when rebuilding against another local game installation.

To tune drain, change GoodConsumingBuildingSpec.ConsumedGoods[0].GoodPerHour. Inventory is ceil(FullInventoryWorkHours × GoodPerHour). Keep visitor capacity synchronized with model slots.

## Version number

The version is chosen in one place: `<Version>` in `Source/Runtime/TipsyTail.Runtime.csproj`. The DLL is compiled with it and `build_mod.py` writes it into `Mod/manifest.json` with a fourth `.0`. The README download links, first What's new heading and release status, the website's fallback tag and `data-release-pinned`, the title of this file and RELEASE_NOTES.md are written by hand for each release, and the committed DLL must be rebuilt. `python Source/validate_version.py` lists every copy and fails if one disagrees or can no longer be found. It needs no game files and runs in CI.

## Reproducible runtime DLL

The runtime build below is reproducible: the csproj keeps the git commit out of the informational version, so the same source, game assemblies and .NET SDK always give the same bytes. `python Source/validate_runtime_dll.py` builds the runtime in place and from a copy outside git, requires the two to be byte-identical, and requires Mod/Scripts/TipsyTail.Runtime.dll to be byte-identical to them, so a source change without a rebuilt DLL copied in is caught. Run it before packaging; it needs the game assemblies (TIMBERBORN_PATH). The v1.0.0 DLL predates this and still carries its build commit (`1.0.0+faec863…`), so the check rebuilds with that stamp to compare; the stamp disappears when the DLL is next rebuilt and copied in.

## Continuous integration

`.github/workflows/tests.yml` runs on every pull request and every push to main. It runs the Tests project and `validate_version.py`, which need no game files. The runtime build, the native integration harness, `build_mod.py`, `validate_runtime_dll.py` and the Blender checks need the game or Blender, so run them locally before packaging. `validate_pool_water.py` and `validate_wet_fur.py` also run in the workflow when the runner's `TIMBERBORN_PATH` points at an installed game, such as a self-hosted runner; GitHub's hosted runners skip them.

## Build the bundled terrain controller

Requires .NET SDK 8 and your installed game assemblies. Run before packaging:

```text
dotnet build Source/Runtime/TipsyTail.Runtime.csproj -c Release
dotnet run --project Source/Runtime/Tests/Tests.csproj -c Release
dotnet run --project Source/Runtime/Tests/Native/Integration.csproj -c Release
```

Copy only Source/Runtime/bin/Release/netstandard2.1/TipsyTail.Runtime.dll into Mod/Scripts. Game DLLs are referenced for compilation and tests, not redistributed. Override TimberbornManagedDir when building against a non-default game installation. The native integration harness also specifies its local Managed directory in its project and Program.cs.
