# The Tipsy Tail v0.2.4 — Wet fur relief

- The Tipsy Tail now satisfies the game's native Wet fur need for every visitor, at 0.5 points per hour. That is the same rate the Lido and Swimming Pool use for their swimming visitors.
- Works for both Folktails and Iron Teeth with no extra setup, because Wet fur is part of the game's common need set. Existing pools pick it up when you load your save.
- Recreation and wet fur relief both stop while the pool is dry, and resume when haulers restore the water.
- The in-game description now mentions wet fur.
- Data-only change: the model and runtime code are the same as v0.2.3, including the flat still-water surface and v0.2.1 terrain cleanup on demolition. The DLL is rebuilt only to carry the new version number.

Install by replacing the old TipsyTail folder with the folder from **TipsyTail-v0.2.4-mod.zip**, including Scripts/TipsyTail.Runtime.dll. Restart Timberborn and reload your save. Multiplayer players need matching versions.

Built against Timberborn 1.1.2.4. The wet fur effect is checked offline against the game's own blueprints: the need exists, both factions load it, and the rate matches the native swimming venues. Runtime compilation and terrain ownership/native integration regressions pass. The effect and the water appearance still require user testing in-game; no game session was launched for verification. This is a prototype prerelease.
