# The Tipsy Tail v1.0.1 — water.cfg follows the mod folder

A maintenance release of the first full release. Gameplay, saves and multiplayer are unchanged.

- The optional `water.cfg` diagnostics file is now read from the folder the game loaded the mod from, whatever it is called and wherever it is: a renamed folder such as `TipsyTail-v1.0.1-mod`, the macOS mods folder, or a folder outside Documents. Before, it was only ever looked for in `Documents/Timberborn/Mods/TipsyTail`, because the game loads mod DLLs in a way that hides their location. The presets are in **TipsyTail-v1.0.1-water-presets.zip**.
- The website's "In version 1.0" notes now list what the release contains, and its earlier-version FAQ says which saves each step applies to.
- For contributors: the version is chosen once, in the runtime csproj, and a check keeps every other copy in step; the runtime DLL now builds byte for byte from source; GitHub Actions runs the game-free tests on every pull request.
- Only `Scripts/TipsyTail.Runtime.dll` and the manifest version changed in the playable mod since v1.0.0. The model, blueprints, wet fur relief, capacity and terrain cleanup are unchanged.

Install by extracting the **TipsyTail** folder from **TipsyTail-v1.0.1-mod.zip** into `Documents/Timberborn/Mods`, replacing any earlier version, then restart Timberborn. Multiplayer players need matching versions. Saves from v1.0.0 and v0.2.x load unchanged.

Built against Timberborn 1.1.2.4. Offline checks against the game's own files cover the model, blueprints, materials, geometry, wet fur, terrain cutout ownership and the water shader data; a native harness loads the DLL the way the game does and confirms `water.cfg` resolves inside a renamed mod folder.
