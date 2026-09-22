# The Tipsy Tail v1.0.0 — First full release

A timber-and-thatch swim-up pool bar for Timberborn 1.1.2.4, for Folktails and Iron Teeth. Eight visitors, four at the bar and four in the water, relieve a Tipsy Tail need worth up to +2 well-being and the native Wet fur need, while haulers keep a 60-water reserve topped up.

- The pool is drawn with the game's own lake water (since v0.2.8): it darkens with depth, shows the floor, the ladder and swimming beavers through the shallows, and ripples, glints and reflects like the map's still water. The map's water and its simulation are untouched.
- Swimmers now sit in the water. The game keeps a swimming slot at the water surface, but the pool's swimming lanes were exported 0.48 blocks above it, so swimmers floated. The runtime now moves every lane end to the surface height when the building wakes, the same rule the Lido and Swimming Pool follow.
- The gameplay wiring was audited against the game's own code: both attraction effects (Tipsy Tail and Wet fur) apply to every visitor for as long as they stay, scaled by the water reserve, so recreation, wet fur relief and the pool surface stop together when the pool runs dry and return on resupply; the eight slots match the capacity the game insists on; the access point, need registration and pause rules follow the native buildings; missing translations fall back to English.
- Everything else is as in v0.2.8: the two-block basin with individually mapped planks and a ladder, the campfire-style construction site, scoped terrain-cutout cleanup so demolition reveals the original ground, and the optional `water.cfg` diagnostics (**TipsyTail-v1.0.0-water-presets.zip**).

Install by extracting the **TipsyTail** folder from **TipsyTail-v1.0.0-mod.zip** into `Documents/Timberborn/Mods`, replacing any earlier version, then restart Timberborn and reload your save. Multiplayer players need matching versions. Saves from v0.2.x load unchanged.

Built against Timberborn 1.1.2.4. Offline checks against the game's own files cover the model, blueprints, materials, geometry, wet fur, terrain cutout ownership and the water shader data; the recreation and wet fur logic was verified in the decompiled game code. The swimmer height correction is new in this release and worth a glance in your own colony.
