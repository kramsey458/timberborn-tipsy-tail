---
target_identity: "file:C:\\Users\\Kyler\\code\\timberborn-tipsy-tail-site-redesign\\file:C:\\Users\\Kyler\\code\\timberborn-tipsy-tail-site-redesign\\docs"
timestamp: 2026-09-23T20-30-36Z
slug: timberborn-tipsy-tail-site-redesign-docs-4c8b3910
---
---
target: The Tipsy Tail site
total_score: 21
max_score: 36
na_heuristics: 7
p0_count: 1
p1_count: 5
---
# Critique: The Tipsy Tail site (docs/index.html)
Method: dual-agent (A design review, B CI + detector + browser + audit)
Tests: (1) FAIL - the benefit (Tipsy Tail need, Social Life, +2 well-being) appears nowhere; 8 beavers first at y~950/1464; requirements scattered over four places; (2) FAIL - Lido named only as a same-rate comparison, "extra source, not a replacement" never said; .status states unconfirmed things (lake water, surface swimmers) as fact; version history in the heading, the pinned "In version 1.0" list and an earlier-versions FAQ; (3) PARTIAL - Assets and folder named, but no -mod.zip name, no Source code / water-presets warning, no "manifest.json directly inside", update steps buried in the FAQ.
Heuristics 21/36 (Acceptable). Cognitive load moderate.
Priority: [P0] benefit absent; [P1] status overclaims + history; [P1] pinned block is a changelog; [P1] install mistakes not prevented; [P1] phone nav hides Install; [P1] images 4.7 MB (hero LCP 11.3 s on slow 4G), no skip link; [P2] requirements not in one block, Lido line missing; renders on grey clash, two renders unused; focus ring 2.97:1; tap targets <44px; [P3] 10px text, brand href="#", no dark theme, favicon.ico 404, og tags thin.
CI: validate_version.py 20/20; needs data-release-pinned="1.0.1" and the exact data-release="tag">v1.0.1< markup.
Identity: calm editorial template (cream, forest green, Georgia, tracked eyebrows, stat grid, numbered 01-03 band); nothing of water, thatch or the bar.
Evidence: four Blender renders (preview, front, side, dry), not in-game screenshots; captions must keep saying so.
