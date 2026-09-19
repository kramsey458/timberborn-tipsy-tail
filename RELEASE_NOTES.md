# The Tipsy Tail v0.2.5 — Moving lake water, stutter fix

- Fixes the pool water stutter and replaces the neon-cyan pool of v0.2.3 and v0.2.4, which are superseded by this version.
- Root cause: the game's water material animates every layer through the mesh's second UV set, and the pool surface only had the first. Every pixel therefore sampled the same point, so the whole surface pulsed together instead of showing ripples moving across it. The pool surface now gets a proper second UV set at runtime.
- The new look uses the game's own animated lake textures at the lake's ripple scale, the lake's gentle ripple speeds and a lake-blue tint. Only the pool is affected; fountains and map water are unchanged.
- New optional live tuning: copy a preset from **TipsyTail-v0.2.5-water-presets.zip** into `Mods/TipsyTail` as `water.cfg` and the pool changes within about a second, with no restart. Presets cover a finer or coarser ripple scale, a deeper or lighter blue, and one that turns the fix off for comparison. It only changes how the pool is drawn.
- Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.4. Wet fur relief, capacity and terrain cleanup on demolition are unchanged.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.5-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. Offline checks confirm the cause and the fix against the game's own files: the shader reads only UV1 on its connected layers, the exported pool surface has no UV1, the runtime supplies one, every shader property the mod touches exists, and the UV scale sits inside the ratio between the lake's and the fountain's texture tilings. **The water appearance has not been verified in game by the author.** The lake-scale default is an estimate, so please try the presets and report which one looks right. This is a prototype prerelease.
