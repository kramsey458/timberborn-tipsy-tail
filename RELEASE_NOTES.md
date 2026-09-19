# The Tipsy Tail v0.2.6 — Still-lake water look

- Reworks the pool water toward the in-game lake look: a static texture, ripples moving on the game's own animation clock at the lake's speeds and scale, a slate teal-blue tint (instead of v0.2.5's saturated blue) and crisp white glints off the ripples.
- Removes every motion driven by raw Unity time (albedo drift, ripple offset, noise and gloss scrolling), which can jitter in multiplayer host sessions. This is why some builds looked like they were stuttering.
- Honest limit: the pool water is opaque, so it is not see-through and does not darken with depth like the real lake. Every standalone water material in the game is opaque, and the lake shader needs the game's water simulation data, so this is a close look-alike, not the same shader.
- Water presets are updated (**TipsyTail-v0.2.6-water-presets.zip**): a darker or lighter tint, finer ripples, and three diagnostics: no motion at all, the previous raw-time motion, and the untouched vanilla fountain water. Copy one into `Mods/TipsyTail` as `water.cfg` and the pool changes within a few seconds. They help tell whether the pool is behind any lag you see.
- The optional `water.cfg` check now runs every 2 seconds and compares only size and timestamp, so it adds no measurable cost.
- Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.5. Wet fur relief, capacity and terrain cleanup on demolition are unchanged.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.6-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. Offline checks against the game's own files confirm that the shader reads only UV1 on its connected layers, that every raw-Time input passes through three speed properties which the mod zeroes, that the exported pool surface has no UV1 and the runtime supplies one, and that every shader property the mod touches exists. **The water appearance has not been verified in game by the author, and a reported lag is unresolved: the game log shows no errors from this mod.** This is a prototype prerelease.
