using System;
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
    }
}
