# The Tipsy Tail v0.2.3 — Still pool water

- Replaces the pool's frozen fountain-water texture layers with one flat, glossy surface in Timberborn's own water tint. This removes the static blotches, dark patches and sparkle specks visible in v0.2.2.
- Turns foam off entirely and makes the surface level, with no normal-map distortion.
- Uses renderer-local overrides on the pool surface only, leaving the game's fountains and other water materials unchanged.
- Only `Scripts/TipsyTail.Runtime.dll` changed since v0.2.2. The model, blueprint, capacity (four bar seats and four swimming lanes) and balance are identical, and v0.2.1 terrain cleanup on demolition is retained.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.3-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save to load the updated runtime. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. Runtime compilation, shader-property verification against the installed game (every overridden property exists and the foam and albedo math resolves to a flat tint), and terrain ownership/native integration regressions pass. Water appearance and behavior still require user testing in-game; no game session was launched for verification. This is a prototype prerelease.
