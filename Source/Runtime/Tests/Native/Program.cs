using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Timberborn.BlockSystem;
using Timberborn.BlockObjectModelSystem;
using Timberborn.ModManagerScene;
using Timberborn.TerrainSystem;
using UnityEngine;
using Kyler.TipsyTail;

class Program
{
    const string Managed=@"C:\Program Files (x86)\Steam\steamapps\common\Timberborn\Timberborn_Data\Managed";
    static void Main()
    {
        AssemblyLoadContext.Default.Resolving += (context,name) => {
            string p=Path.Combine(Managed,name.Name+".dll");
            return File.Exists(p)?context.LoadFromAssemblyPath(p):null;
        };
        Run();
        StartMod();
    }
    static object Empty(Type t)=>RuntimeHelpers.GetUninitializedObject(t);
    static void Field(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var block=(BlockObject)Empty(typeof(BlockObject));
        var stateType=typeof(BlockObject).Assembly.GetType("Timberborn.BlockSystem.BlockObjectState");
        var state=Empty(stateType);
        Field(state,"_state",Enum.ToObject(stateType.GetNestedType("State",BindingFlags.NonPublic),1));
        Field(block,"_blockObjectState",state);
        Field(block,"_blockObjectSpec",new BlockObjectSpec{Size=new Vector3Int(5,6,5),BaseZ=2,Blocks=Enumerable.Repeat(new BlockSpec(),150).ToImmutableArray()});
        var model=(BlockObjectModelController)Empty(typeof(BlockObjectModelController));
        model.SetModelState(true,true,false);
        var terrain=DispatchProxy.Create<ITerrainService,TerrainProxy>();
        var proxy=(TerrainProxy)(object)terrain;
        proxy.Model=model;
        var tiles=Enumerable.Range(0,30).Select(i=>new Vector3Int(i%5,i/5,2)).ToImmutableArray();
        // Run the actual installed native handler with one synchronous model event
        // during its first terrain notification; no Unity scene or game is launched.
        var nativeType=Assembly.Load("Timberborn.Buildings").GetType("Timberborn.Buildings.BuildingTerrainCutout");
        var native=Activator.CreateInstance(nativeType,new object[]{new TerrainCutout(terrain)});
        var nativeSpecType=nativeType.Assembly.GetType("Timberborn.Buildings.BuildingTerrainCutoutSpec");
        var nativeSpec=Activator.CreateInstance(nativeSpecType);
        nativeSpecType.GetProperty("CutoutTiles").SetValue(nativeSpec,tiles);
        Field(native,"_blockObject",block);Field(native,"_blockObjectModelController",model);Field(native,"_buildingTerrainCutoutSpec",nativeSpec);
        nativeType.GetMethod("InitializeEntity").Invoke(native,null);
        nativeType.GetMethod("DeleteEntity").Invoke(native,null);
        int leaked=proxy.Counts.Values.Sum();
        if(leaked!=30)throw new Exception("Native reentrancy reproduction changed: "+leaked);
        Console.WriteLine("REPRODUCED: installed native handler leaves 30 cutout references after reentrant model update and deletion.");
        proxy.Counts.Clear();proxy.Reentered=false;
        var custom=new TipsyTailTerrainCutout(terrain);
        Field(custom,"_block",block);Field(custom,"_model",model);
        Field(custom,"_spec",new TipsyTailTerrainCutoutSpec{CutoutTiles=tiles});
        Field(custom,"_lease",new CutoutLease<Vector3Int>(terrain.SetCutout,terrain.UnsetCutout));
        custom.InitializeEntity();custom.PostInitializeEntity();
        if(proxy.Counts.Values.Any(n=>n!=1))throw new Exception("Duplicate owned references");
        custom.DeleteEntity();custom.DeleteEntity();model.SetModelState(true,true,false);
        if(proxy.Counts.Values.Any(n=>n!=0))throw new Exception("Custom cleanup leaked");
        Console.WriteLine("PASS: compiled replacement releases all 30 references under the same native callbacks; repeated deletion/late events stay clear.");
        TipsyTailTerrainCutout CreateCustom()
        {
            var instance=new TipsyTailTerrainCutout(terrain);
            Field(instance,"_block",block);Field(instance,"_model",model);
            Field(instance,"_spec",new TipsyTailTerrainCutoutSpec{CutoutTiles=tiles});
            Field(instance,"_lease",new CutoutLease<Vector3Int>(terrain.SetCutout,terrain.UnsetCutout));
            return instance;
        }
        void CheckClear(string label)
        {
            if(proxy.Counts.Values.Any(n=>n!=0))throw new Exception(label);
            Console.WriteLine("PASS: "+label);
        }
        proxy.Counts.Clear();
        Field(state,"_state",Enum.ToObject(stateType.GetNestedType("State",BindingFlags.NonPublic),2));
        custom=CreateCustom();custom.InitializeEntity();custom.DeleteEntity();
        CheckClear("Placement preview/cancellation never hides the ground");
        Field(state,"_state",Enum.ToObject(stateType.GetNestedType("State",BindingFlags.NonPublic),0));
        custom=CreateCustom();custom.InitializeEntity();custom.DeleteEntity();
        CheckClear("Unbuilt site deletion never hides the ground");
        Field(state,"_state",Enum.ToObject(stateType.GetNestedType("State",BindingFlags.NonPublic),1));
        custom=CreateCustom();custom.InitializeEntity();
        model.SetModelState(false,false,false);
        CheckClear("Underground/model hiding releases the terrain mask");
        model.SetModelState(true,true,false);
        foreach(var orientation in Enum.GetValues(typeof(Timberborn.Coordinates.Orientation)))
        {
            custom.OnPrePlacementChanged();
            CheckClear("Relocation releases the old footprint before moving");
            Field(block,"<Orientation>k__BackingField",orientation);
            Field(block,"<Coordinates>k__BackingField",new Vector3Int(13,17,5));
            custom.OnPostPlacementChanged();
            var expected=tiles.Select(t=>block.TransformCoordinates(t)).ToHashSet();
            var actual=proxy.Counts.Where(p=>p.Value==1).Select(p=>p.Key).ToHashSet();
            if(!expected.SetEquals(actual) || proxy.Counts.Values.Any(n=>n>1))throw new Exception("Rotated footprint mismatch");
        }
        custom.DeleteEntity();CheckClear("All rotations and relocation delete cleanly");
    }
    // Load the runtime DLL the way the game's ModCodeStarter does (from bytes) and start its IModStarter types the way
    // the game picks them (assignable, not abstract, parameterless constructor), then ask the pool tuner for water.cfg.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StartMod()
    {
        var loaded=Assembly.Load(File.ReadAllBytes(typeof(TipsyTailTerrainCutout).Assembly.Location));
        if(loaded.Location!="")throw new Exception("A byte-loaded assembly has a location: "+loaded.Location);
        Console.WriteLine("PASS: The runtime DLL loaded from bytes, as the game loads it, has no Assembly.Location");
        var starters=loaded.GetTypes().Where(t=>typeof(IModStarter).IsAssignableFrom(t)&&!t.IsAbstract&&t.GetConstructor(Type.EmptyTypes)!=null).ToList();
        if(starters.Count!=1)throw new Exception("Expected one mod starter, found "+starters.Count);
        Console.WriteLine("PASS: The game's mod loader finds exactly one mod starter: "+starters[0].FullName);
        var modPath=Path.Combine(Path.GetTempPath(),"Mods","TipsyTail-v1.0.0-mod");
        ((IModStarter)Activator.CreateInstance(starters[0])).StartMod(new ModEnvironment{ModPath=modPath,OriginPath=modPath});
        var configPath=(string)loaded.GetType("Kyler.TipsyTail.TipsyTailWaterTuner").GetMethod("ConfigPath",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        if(configPath!=Path.Combine(modPath,"water.cfg"))throw new Exception("water.cfg resolved to "+configPath);
        Console.WriteLine("PASS: Once the mod has started, the pool tuner reads water.cfg from the renamed mod folder");
    }
    class ModEnvironment:IModEnvironment{public string ModPath{get;set;}public string OriginPath{get;set;}}
}
public class TerrainProxy:DispatchProxy
{
    public Dictionary<Vector3Int,int> Counts=new();
    public BlockObjectModelController Model;
    public bool Reentered;
    protected override object Invoke(MethodInfo method,object[] args)
    {
        if(method.Name=="SetCutout")
        {
            var tile=(Vector3Int)args[0];Counts[tile]=Counts.GetValueOrDefault(tile)+1;
            if(!Reentered){Reentered=true;Model.SetModelState(true,true,false);}
            return null;
        }
        if(method.Name=="UnsetCutout")
        {
            var tile=(Vector3Int)args[0];Counts[tile]=Math.Max(0,Counts.GetValueOrDefault(tile)-1);return null;
        }
        throw new Exception("Unexpected terrain API: "+method.Name);
    }
}
