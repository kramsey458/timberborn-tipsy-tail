# Changelog

What changed in each version of The Tipsy Tail, newest first. Each version's notes are also on the [releases page](https://github.com/timbermods/timberborn-tipsy-tail/releases).

## v1.0.1

`water.cfg` is now read from the folder the game loaded the mod from, whatever it is called and wherever it is: a renamed folder such as `TipsyTail-v1.0.1-mod`, the macOS mods folder, or a folder outside Documents. Before, it was only ever looked for in `Documents/Timberborn/Mods/TipsyTail`. Only the pool's appearance diagnostics are affected. Gameplay, saves and multiplayer are unchanged. Only `Scripts/TipsyTail.Runtime.dll` and the manifest changed since v1.0.0. Restart the game after updating.

## v1.0.0

The first full release. The pool keeps the game's own lake water from v0.2.8. Swimmers now sit at the water surface like the game's own swimmers instead of floating above it. The recreation, wet fur, slot, access and water-supply wiring was audited against the game's code. Only `Scripts/TipsyTail.Runtime.dll` and the manifest changed since v0.2.8. Restart the game and reload your save after updating.

## v0.2.8

The game's own water. The pool surface is now drawn with the same material the map's lakes use, on the game's own water mesh layout, fed by a small private copy of the data the game's water renderer keeps for the map. So the pool looks like still lake water of the same depth: it darkens with depth, the floor, ladder and swimmers show through the shallows, and the ripples, glints and reflections are the game's, not an imitation. The map water and its simulation are untouched; the pool only borrows the material. If the game water cannot be set up on your machine, the pool falls back to the v0.2.7 look, and `water.cfg` with `legacy` forces that for comparison. Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.7. Restart the game and reload your save after updating.

## v0.2.7 (superseded by v0.2.8)

Water animation fix: the pool water now actually moves. v0.2.6 left the ripples on a hidden ripple clock in the water shader that only the game can feed, and it never moved the pool, so the water was completely still. The mod now animates the ripples itself, every frame, by sliding the water surface's texture coordinates. It runs on real elapsed time, so it keeps moving while the game is paused, and the two ripple layers drift in opposite directions, like the lake's, so the pattern shimmers and evolves. The default speed was chosen by sampling the real ripple texture: the pattern half-changes in about 3 seconds, calm rather than busy. Color is unchanged from v0.2.6. **The pool water still cannot be see-through like the real lake:** every standalone water material in the game is opaque. Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.6. Restart the game and reload your save after updating.

## v0.2.6

Water look: a slate teal-blue tint and a glossier surface so the sun throws white glints off the ripples, and all raw Unity time motion removed. Its ripple animation depended on a hidden game clock and did not move the pool; that was fixed in v0.2.7.

## v0.2.5

Water fix: the pool surface was missing the second UV set that the game's water material animates everything through, so the whole surface pulsed together. It now has one, at the lake's ripple scale. This replaced the neon-cyan pool of v0.2.3 and v0.2.4; its saturated-blue tint was revised in v0.2.6.

## v0.2.4

Wet fur: the Tipsy Tail now also satisfies the game's native Wet fur need for every visitor, at 0.5 points per hour, the same rate as the Lido and Swimming Pool. It works for both factions with no extra setup, and existing pools pick it up when you load your save. Recreation and wet fur relief both stop while the pool is dry. Only the building data changed; the model and runtime code are the same as v0.2.3.

## v0.2.3

Still pool water: the pool surface no longer shows the fountain material's frozen texture layers, which had become static blotches, dark patches and sparkle specks. It is now one flat, glossy surface in Timberborn's own water tint, with no foam. Only the pool is affected; the game's fountains and map water are unchanged. **This flat surface rendered far too bright in game and was replaced in v0.2.5.**

## v0.2.2

Calm pool water: scrolling textures, ripple motion and foam are disabled for the pool only, with much gentler surface distortion. The two corner seats are removed. Eight visitors now use four bar seats and four swimming lanes. Restart the game and reload your save after updating.

## v0.2.1

Demolition now releases the pool's terrain cutout without changing the original soil. A bundled cleanup controller handles repeated callbacks and avoids separate preview cutouts. **To clear a hole left by an already-removed pool, update, restart and reload your save.** The game recreates its temporary cutout map during loading. Keep the bundled `Scripts/TipsyTail.Runtime.dll`; no external framework is needed.

An offline native-assembly test reproduced the old leak and verified the replacement under the same callbacks, plus rotation, relocation and construction-cancellation tests. In-game and multiplayer verification remain outstanding.

## v0.2.0

Recessed the basin by two blocks, replaced the stretched floor with individual planks, rebuilt the entrance steps, added a pool ladder, switched the purple breeding-pod water to teal fountain water, lowered the counter relative to seated beavers, removed the hanging ornament, and adopted the native dirt-and-stakes construction site. Earlier material and construction-stage crash fixes are retained.

The depth requirement changed in v0.2.0. Old placements on platforms, thin terrain or above other underground buildings may need rebuilding. Save migration from those builds has not been playtested.
