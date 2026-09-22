using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Kyler.TipsyTail;

static class Program
{
    static int tests;
    static void Assert(bool condition, string name)
    { if (!condition) throw new Exception(name); tests++; Console.WriteLine("PASS: " + name); }
    static void Main()
    {
        var counts = new Dictionary<int,int>();
        var soil = Enumerable.Range(0,30).Select(i => (height:7,texture:i%4)).ToArray();
        var original = soil.ToArray();
        Action<int> acquire = t => counts[t] = counts.GetValueOrDefault(t)+1;
        Action<int> release = t => { if(counts.GetValueOrDefault(t)<1) throw new Exception("Unowned release"); counts[t]--; };
        var tiles = Enumerable.Range(0,30).ToArray();
        CutoutLease<int> lease = null;
        lease = new CutoutLease<int>(t => {acquire(t); lease.Show(tiles);}, release);
        lease.Show(tiles); lease.Show(tiles.Concat(tiles));
        Assert(counts.Values.All(n=>n==1), "Repeated/reentrant show owns each of 30 tiles once");
        lease.Dispose(); lease.Dispose(); lease.Show(tiles);
        Assert(counts.Values.All(n=>n==0), "Deletion reveals every original ground tile and cannot reacquire");
        Assert(soil.SequenceEqual(original), "Ground heights and appearance data are never mutated");
        counts.Clear();
        acquire(5); // Independent building owns a cutout at a shared tile.
        lease = new CutoutLease<int>(acquire,release);
        lease.Show(tiles); lease.Dispose();
        Assert(counts[5]==1 && counts.Where(p=>p.Key!=5).All(p=>p.Value==0), "Deletion preserves another owner's cutout");
        counts.Clear();
        lease = new CutoutLease<int>(acquire,release);
        lease.Show(tiles); lease.Hide(); lease.Hide(); lease.Show(new[]{100,101}); lease.Dispose();
        Assert(counts.Values.All(n=>n==0), "Hide, reshow and relocation release cached old coordinates");
        counts.Clear();
        lease = new CutoutLease<int>(t => {acquire(t);lease.Dispose();},release);
        lease.Show(tiles);
        Assert(counts.Values.All(n=>n==0), "Deletion inside an acquisition callback rolls back the partial cutout");
        counts.Clear();
        lease = new CutoutLease<int>(acquire,t=>{release(t);lease.Show(tiles);});
        lease.Show(tiles);lease.Dispose();
        Assert(counts.Values.All(n=>n==0), "Late model callbacks during deletion cannot reopen the hole");
        counts.Clear();
        lease = new CutoutLease<int>(acquire,release);lease.Dispose();
        Assert(counts.Count==0, "Unbuilt/cancelled entity deletion does not decrement unrelated cutouts");
        Console.WriteLine($"{tests} terrain ownership regression checks passed.");
        WaterGridChecks();
        ConfigPathChecks();
    }

    // water.cfg is looked up in the folder the game loaded the mod from (IModEnvironment.ModPath). The game loads mod
    // DLLs from bytes, so the assembly location is always empty and cannot stand in for it.
    static void ConfigPathChecks()
    {
        int before = tests;
        string documents = Path.Combine(Path.GetTempPath(), "Beaver", "Documents");
        string documented = Path.Combine(documents, "Timberborn", "Mods", "TipsyTail");
        string renamed = Path.Combine(documents, "Timberborn", "Mods", "TipsyTail-v1.0.0-mod");
        Assert(TipsyTailPaths.ResolveConfigPath(renamed, documents) == Path.Combine(renamed, "water.cfg"), "A renamed mod folder reads water.cfg from inside itself");
        // On macOS the game puts its user data folder (and so Mods) under MyDocuments/Documents/Timberborn, which is
        // ~/Documents/Timberborn on disk, not MyDocuments/Timberborn.
        string mac = Path.Combine(documents, "Documents", "Timberborn", "Mods", "TipsyTail");
        Assert(TipsyTailPaths.ResolveConfigPath(mac, documents) == Path.Combine(mac, "water.cfg"), "The macOS mods folder reads water.cfg from the mod folder, not Documents/Timberborn");
        string elsewhere = Path.Combine(Path.GetTempPath(), "steamapps", "workshop", "content", "1062090", "123456");
        Assert(TipsyTailPaths.ResolveConfigPath(elsewhere, documents) == Path.Combine(elsewhere, "water.cfg"), "A mod folder outside Documents reads water.cfg from inside itself");
        Assert(TipsyTailPaths.ResolveConfigPath(documented, documents) == Path.Combine(documented, "water.cfg"), "The documented install folder still reads Mods/TipsyTail/water.cfg");
        Assert(TipsyTailPaths.ResolveConfigPath(null, documents) == Path.Combine(documented, "water.cfg") && TipsyTailPaths.ResolveConfigPath("", documents) == Path.Combine(documented, "water.cfg"), "Without a mod path the documented Documents/Timberborn/Mods/TipsyTail folder is the fallback");
        Assert(TipsyTailPaths.ResolveConfigPath(null, "") == null, "Without a mod path or a Documents folder there is no water.cfg, rather than a path relative to the game folder");
        Console.WriteLine($"{tests - before} config path checks passed.");
    }

    // The pool's private water map must match the layout the game's water shader expects (see TipsyTailWaterGrid).
    static void WaterGridChecks()
    {
        int before = tests;
        // A pool at world x 100.61..104.39, z 20.10..23.70 (the exported surface at a building placed at 100, 19), surface 0.1625
        // above ground level 12, basin floor two blocks down.
        var g = TipsyTailWaterGrid.Build(100.61f, 104.39f, 20.10f, 23.70f, 12.1625f, 10, 30);
        Assert(g.CellsX == 5 && g.CellsZ == 4 && g.FirstTileX == 100 && g.FirstTileZ == 20, "Cells follow the world's tiles, clipped to the surface rectangle");
        Assert(g.Width == 7 && g.Height == 6, "The private map has one empty cell of padding on every side");
        Assert(g.Vertices.Length == 20 * 48 * 3 && g.Uv0.Length == 20 * 48 * 4 && g.Triangles.Length == 20 * 126, "48 vertices and 126 indices per cell, as in the game's water mesh");
        Assert(g.Triangles.All(t => t >= 0 && t < 20 * 48), "Every index points at a vertex");
        // First cell: x 100.61..101, z 20.10..21. Top vertex i sits at column i % 4 and row i / 4, thirds apart.
        float X(int v) => g.Vertices[v * 3]; float Y(int v) => g.Vertices[v * 3 + 1]; float Z(int v) => g.Vertices[v * 3 + 2];
        Assert(Math.Abs(X(0) - 100.61f) < 1e-5 && Math.Abs(Z(0) - 20.10f) < 1e-5 && Math.Abs(X(3) - 101f) < 1e-5 && Math.Abs(Z(12) - 21f) < 1e-5, "Top-surface corners span the clipped cell");
        Assert(Math.Abs(X(5) - (100.61f + (101f - 100.61f) / 3f)) < 1e-5 && Math.Abs(Z(5) - (20.10f + 0.9f / 3f)) < 1e-5, "Interior vertices sit a third of the way in");
        Assert(Enumerable.Range(0, 20 * 48).All(v => Y(v) == 0f), "Vertex y is the column index (0); the shader lifts it to the water height");
        for (int i = 16; i < 48; i++)
        {
            int top = TipsyTailWaterGrid.SkirtSource[(i - 16) % 16];
            Assert(X(i) == X(top) && Z(i) == Z(top), $"Skirt vertex {i} duplicates top vertex {top}");
        }
        // The last cell (x 104..104.39, z 23..23.70) and its UV0 cell coordinates include the padding.
        int last = 19 * 48;
        Assert(Math.Abs(X(last + 15) - 104.39f) < 1e-5 && Math.Abs(Z(last + 15) - 23.70f) < 1e-5, "The far corner lands on the surface rectangle's corner");
        Assert(g.Uv0[last * 4] == 5 && g.Uv0[last * 4 + 1] == 4 && g.Uv0[0] == 1 && g.Uv0[1] == 1, "UV0.xy is the cell's coordinate in the private map, padding included");
        Assert(Enumerable.Range(0, 48).All(i => g.Uv0[i * 4 + 2] == i), "UV0.z is the vertex index within the cell");
        // Flag masks, as WaterMesh.UpdateWaterMesh sets them.
        const int Edge = 1, Corner = 2, Skirt = 4, Left = 8, Right = 16, Top = 32, Bottom = 64, Floor = 128;
        Assert(TipsyTailWaterGrid.Mask(5) == 0 && TipsyTailWaterGrid.Mask(10) == 0 && TipsyTailWaterGrid.Mask(1) == Edge && TipsyTailWaterGrid.Mask(0) == (Edge | Corner), "Top-surface masks: interior 0, ring edge, corners edge+corner");
        Assert(TipsyTailWaterGrid.Mask(17) == (Edge | Skirt | Bottom) && TipsyTailWaterGrid.Mask(16) == (Edge | Skirt | Bottom | Corner), "Edge skirts on the -z side carry the bottom skirt bit");
        Assert(TipsyTailWaterGrid.Mask(21) == (Edge | Skirt | Left) && TipsyTailWaterGrid.Mask(25) == (Edge | Skirt | Right) && TipsyTailWaterGrid.Mask(29) == (Edge | Skirt | Top), "Edge skirts on the -x, +x and +z sides");
        Assert(TipsyTailWaterGrid.Mask(33) == (Edge | Skirt | Bottom | Floor) && TipsyTailWaterGrid.Mask(47) == (Edge | Skirt | Top | Floor | Corner), "Floor skirts add the floor bit");
        Assert(Enumerable.Range(0, 48).All(i => g.Uv0[i * 4 + 3] == TipsyTailWaterGrid.Mask(i)), "UV0.w is the flag mask");
        // Data textures: pool cells hold still, clean, deep water; everything else is empty.
        int W = g.Width; int texel(int cx, int cz) => (cz + 1) * W + cx + 1;
        int center = texel(2, 1);
        Assert(Math.Abs(g.WaterData[center * 4] - 2.1625f) < 1e-5 && g.WaterData[center * 4 + 1] == 10 && g.WaterData[center * 4 + 2] == 30, "Water data is depth, floor, ceiling");
        Assert(g.WaterData[0] == 0 && g.WaterData[(W * g.Height - 1) * 4] == 0 && g.WaterData.Skip(g.Texels * 4).All(v => v == 0), "Padding and the column above are empty");
        Assert(g.EdgeLinks.Skip(g.Texels * 4).All(v => v == -1) && g.EdgeLinks[0] == -1, "Unlinked cells and the column above link to nothing");
        Assert(g.EdgeLinks[center * 4 + 0] == 0 && g.EdgeLinks[center * 4 + 1] == 0 && g.EdgeLinks[center * 4 + 2] == 0 && g.EdgeLinks[center * 4 + 3] == 0, "An interior cell links to all four neighbours in column 0");
        int corner = texel(0, 0);
        Assert(g.EdgeLinks[corner * 4 + TipsyTailWaterGrid.LinkPlusZ] == 0 && g.EdgeLinks[corner * 4 + TipsyTailWaterGrid.LinkPlusX] == 0 && g.EdgeLinks[corner * 4 + TipsyTailWaterGrid.LinkMinusX] == -1 && g.EdgeLinks[corner * 4 + TipsyTailWaterGrid.LinkMinusZ] == -1, "A corner cell links only inward, so the outer sides drop skirts");
        Assert(g.CornerLinks[corner * 4 + TipsyTailWaterGrid.CornerPlusXPlusZ] == 0 && g.CornerLinks[corner * 4 + TipsyTailWaterGrid.CornerMinusXMinusZ] == -1 && g.CornerLinks[corner * 4 + TipsyTailWaterGrid.CornerMinusXPlusZ] == -1 && g.CornerLinks[corner * 4 + TipsyTailWaterGrid.CornerPlusXMinusZ] == -1, "Diagonal links follow the same rule");
        int hw = W * 4;
        Assert(g.Heights.Length == 2 * hw * g.Height * 4 && Enumerable.Range(0, 4).All(oz => Enumerable.Range(0, 4).All(ox => g.Heights[((1 + 1) * 4 + oz) * hw + (1 + 2) * 4 + ox] == 12.1625f)), "Each pool cell has 4x4 height texels at the surface height");
        Assert(g.Heights[0] == 0 && g.Heights.Skip(g.HeightTexels).All(v => v == 0), "Height texels outside the pool are empty");
        Assert(g.Outflows.All(v => v == 0) && g.Contaminations.All(v => v == 0) && g.Waterfalls.All(v => v == 0) && g.SourceMask.All(v => v == 0) && g.Skirts.All(v => v == 0), "Still, clean water: no flow, contamination, waterfalls, sources or skirt flags");
        // Rotated or flipped buildings just give a different rectangle; a tile-aligned rectangle needs no slivers.
        var aligned = TipsyTailWaterGrid.Build(3f, 6f, 8f, 12f, 5.5f, 3, 20);
        Assert(aligned.CellsX == 3 && aligned.CellsZ == 4 && aligned.Vertices[3 * 3] == 4f && aligned.Vertices[(2 * 48 + 3) * 3] == 6f, "A tile-aligned rectangle produces whole cells only");
        bool threw = false;
        try { TipsyTailWaterGrid.Build(1f, 2f, 1f, 2f, 3f, 3, 20); } catch (ArgumentException) { threw = true; }
        Assert(threw, "A surface at or below its floor is rejected");
        Console.WriteLine($"{tests - before} water grid layout checks passed.");
    }
}
