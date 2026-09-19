# The Tipsy Tail v0.2.1 — Demolition terrain cleanup

Fixes a terrain-cutout cleanup failure that could leave a blue rectangle where a demolished Tipsy Tail stood.

- Tracks exactly which terrain tiles the building hides, releasing each once on demolition.
- Handles repeated/reentrant model callbacks, hidden models, relocation and late deletion callbacks without leaking cutouts.
- Avoids a separate placement-preview cutout owner; placing or cancelling a preview does not hide the ground.
- Leaves soil heights and terrain data untouched, preserving the existing ground and other buildings' cutouts.

**Updating an affected save:** replace the old mod folder with this version, restart Timberborn, and reload your save. Terrain cutouts are rebuilt during loading, so an already-stranded cutout from a removed building should disappear. No save editing or manual terraforming is needed for this visual-cutout issue.

Download **TipsyTail-v0.2.1-mod.zip** and extract its TipsyTail folder into `Documents/Timberborn/Mods`. Keep the included `Scripts/TipsyTail.Runtime.dll` with the mod. No separate framework or mod is required. For multiplayer, every player must use the same version.

The fix was compiled against Timberborn 1.1.2.4. An offline test using the installed game's actual native handler reproduced 30 leaked cutout references after a reentrant callback; the replacement cleared all 30 in the same harness. Additional checks cover all four rotations, moving, construction cancellation, model hiding, other cutout owners and callbacks during deletion. Asset/blueprint checks pass. These are offline tests, not an in-game playtest; multiplayer remains unverified.

The two-block depth requirement, pool model, water consumption and recreation balance are unchanged. This remains a prototype prerelease.
