# The Tipsy Tail v0.2.7 — Water that actually moves

- Fixes the completely still pool water of v0.2.6. That version left the ripples on a hidden ripple clock in the water shader that only the game can feed, and it never moved the pool.
- The mod now animates the ripples itself, every frame, by sliding the water surface's texture coordinates. It runs on real elapsed time, so it keeps moving while the game is paused and never jumps after a frame hitch.
- The two ripple layers drift in opposite directions, like the lake's, so the pattern shimmers and evolves instead of just sliding. The heading also wanders slowly.
- The default speed was chosen by sampling the real ripple texture across the pool: the pattern is half-changed after about 3 seconds, which reads as calm moving water. Much faster and it boils.
- The animation does nothing while the pool is off screen, and one shared animation serves every pool.
- Colour is unchanged from v0.2.6. Honest limit: the pool water is opaque, so it is not see-through and does not darken with depth like the real lake. Every standalone water material in the game is opaque, and the lake shader needs the game's water simulation data.
- Presets are updated (**TipsyTail-v0.2.7-water-presets.zip**): slower and livelier speeds, a darker or lighter tint, finer ripples, and two diagnostics: no motion at all, and the untouched vanilla fountain water. Copy one into `Mods/TipsyTail` as `water.cfg` and the pool changes within a few seconds. They help tell whether the pool is behind any lag you see.
- Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.6. Wet fur relief, capacity and terrain cleanup on demolition are unchanged.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.7-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. Offline checks against the game's own files confirm that the shader's ripple clock is a hidden, unexposed global, that every raw-Time input and every bump-speed multiplier is zeroed so no shader clock is used, that the mod slides UV1 every frame and gives the second ripple layer a negative tiling, that every shader property the mod touches exists and is exposed, and that the default speed is in the calm range measured from the real ripple texture. **The water appearance has not been verified in game by the author, and a reported lag is unresolved: the game log shows no errors from this mod.** This is a prototype prerelease.
