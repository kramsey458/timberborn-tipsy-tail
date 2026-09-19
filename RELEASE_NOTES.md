# The Tipsy Tail v0.2.2 — Calm pool water and swimming slots

- Stops all four scrolling water texture layers and the shader ripple motion on the pool surface.
- Removes foam and reduces normal-map distortion for a calm, contained pool appearance.
- Uses renderer-local overrides, leaving the game's fountains and other water materials unchanged.
- Removes both corner sitting slots and their submerged benches/brackets. Visitors now use four bar seats and four swimming lanes; capacity remains eight.
- Retains v0.2.1 terrain cleanup on demolition.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.2-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save to load the updated model and runtime. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. Runtime compilation, model/blueprint/material/geometry checks, shader-property verification against the installed game, and terrain ownership/native integration regressions pass. Water appearance and behavior still require user testing in-game; no game session was launched for verification. This is a prototype prerelease.
