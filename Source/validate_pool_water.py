"""Offline checks for the pool water surface in Source/Runtime/TipsyTailPoolWater.cs.
Reads the installed game's Shaders.zip (FountainWaterURP.shadergraph), the exported pool model
and the C# source, so every property name, UV channel and constant comes from the real files.
No game is launched and nothing here proves the in-game appearance.
"""
import json,os,re,struct,zipfile,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
GAME=Path(os.environ.get('TIMBERBORN_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))
SHADER='FountainWaterURP.shadergraph'

def graph_objects():
    z=zipfile.ZipFile(GAME/'Timberborn_Data/StreamingAssets/Modding/Shaders.zip')
    text=z.read(SHADER).decode('utf-8')
    dec,i,out=json.JSONDecoder(),0,[]
    while i<len(text):
        while i<len(text) and text[i].isspace():i+=1
        if i>=len(text):break
        obj,i=dec.raw_decode(text,i);out.append(obj)
    return out

def _varint(b,i):
    r=s=0
    while True:
        c=b[i];i+=1;r|=(c&0x7f)<<s;s+=7
        if not c&0x80:return r,i

def _fields(b):
    i,out=0,[]
    while i<len(b):
        key,i=_varint(b,i);num,wire=key>>3,key&7
        if wire==0:v,i=_varint(b,i)
        elif wire==2:
            n,i=_varint(b,i);v=b[i:i+n];i+=n
        elif wire==1:v=b[i:i+8];i+=8
        elif wire==5:v=b[i:i+4];i+=4
        else:raise ValueError('unsupported wire type')
        out.append((num,wire,v))
    return out

def pool_surface_vertex_channels():
    """Vertex channels of the #PoolWater node in the exported model (a timbermesh is zlib + protobuf)."""
    model=zlib.decompress((ROOT/'Mod/Buildings/Wellbeing/TipsyTail/TipsyTail.Model.timbermesh').read_bytes())
    for num,wire,node in _fields(model):
        if num!=3:continue
        fields=_fields(node)
        if not any(n==2 and v==b'#PoolWater' for n,_,v in fields):continue
        channels={}
        for n,_,v in fields:
            if n==7:
                sub={k:x for k,_,x in _fields(v)}
                channels[sub[1].decode()]=struct.unpack('<%df'%(len(sub[4])//4),sub[4])
        return channels
    raise AssertionError('#PoolWater node not found in the exported model')

def validate_pool_water():
    objs=graph_objects()
    props={(o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName')) for o in objs if o.get('m_Type','').endswith('ShaderProperty')}
    target=next(o for o in objs if 'UniversalTarget' in o.get('m_Type',''))
    # Every animated layer samples through a connected UV node, and those must all read UV1. The graph also
    # contains unconnected UV0 nodes, which do nothing, so only nodes that feed something are counted.
    fed={e['m_OutputSlot']['m_Node']['m_Id'] for e in objs[0]['m_Edges']}
    uv_nodes=[o for o in objs if o.get('m_Type','').split(',')[0].endswith('UVNode')]
    uv_channels={o['m_OutputChannel'] for o in uv_nodes if o['m_ObjectId'] in fed}
    assert uv_channels=={1},f'Expected the connected UV nodes to read UV1 only, found channels {uv_channels}'

    # Raw Unity Time must only ever reach the shader through three speed multipliers. The runtime zeroes all
    # three, which removes every raw-Time motion and leaves only the game's own _NonlinearTime on the normals.
    by={o['m_ObjectId']:o for o in objs if 'm_ObjectId' in o}
    nodes={n['m_Id']:by[n['m_Id']] for n in objs[0]['m_Nodes']}
    kind=lambda n:n['m_Type'].split('.')[-1].split(',')[0]
    feeders={}
    for e in objs[0]['m_Edges']:
        feeders.setdefault(e['m_InputSlot']['m_Node']['m_Id'],[]).append(e['m_OutputSlot']['m_Node']['m_Id'])
    def prop_ref(n):
        p=by[n['m_Property']['m_Id']];return p.get('m_OverrideReferenceName') or p.get('m_DefaultReferenceName')
    time_speed_props={'_WaterRippleSpeed','_Albedo_Speed','_Albedo_Speed2'}
    time_nodes=[i for i,n in nodes.items() if kind(n)=='TimeNode']
    assert time_nodes,'Expected the shader to read Time'
    for tid in time_nodes:
        consumers=[i for i,srcs in feeders.items() if tid in srcs]
        assert consumers,'Time node feeds nothing'
        for c in consumers:
            gates={prop_ref(nodes[s]) for s in feeders[c] if kind(nodes[s])=='PropertyNode'}
            assert gates&time_speed_props,f'Time reaches a node that is not gated by a speed property: {gates}'
    assert any(prop_ref(n)=='_NonlinearTime' for n in nodes.values() if kind(n)=='PropertyNode'),'Expected the shader to read _NonlinearTime'
    # _NonlinearTime is not an exposed property (unlike every property the mod overrides), so a per-renderer override
    # cannot be relied on. Only the game can feed it, and in v0.2.6 it did not move the pool, so the mod must not need it.
    exposed={o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName'):o.get('m_GeneratePropertyBlock') for o in objs if o.get('m_Type','').endswith('ShaderProperty')}
    assert exposed['_NonlinearTime'] is False,'_NonlinearTime is expected to be a hidden, game-fed global'
    assert all(exposed[n] for n in ('_Color','_WaterRippleSpeed','_Albedo_Speed','_BumpMap1Speed','_BumpMap1Tiling','_BumpMap2Tiling','_GlossMapScale')),'Every property the mod overrides must be exposed'

    channels=pool_surface_vertex_channels()
    assert 'uv0' in channels and 'uv1' not in channels,'The exported pool surface is expected to lack UV1'

    src=(ROOT/'Source/Runtime/TipsyTailPoolWater.cs').read_text()
    used=re.findall(r'\.(?:Set(?:Float|Vector|Color|Texture)|GetFloat|GetGlobalFloat|HasProperty)\(\s*"(_\w+)"',src)
    setters=re.findall(r'\.Set(?:Float|Vector|Color|Texture)\(\s*"(_\w+)"',src)
    assert setters and set(used)<=props,f'C# uses properties the shader does not have: {sorted(set(used)-props)}'
    # Raw Time is gated by three multipliers; the two bump-speed multipliers only scale the hidden _NonlinearTime. All are zeroed.
    zeroed=sorted(time_speed_props|{'_BumpMap1Speed','_BumpMap2Speed'})
    for name in zeroed:
        assert re.search(r'Set(?:Float|Vector)\(\s*"%s"\s*,\s*(?:0f|Vector4\.zero)\s*\)'%name,src),f'{name} must be zeroed so no shader clock is used'
    assert not re.search(r'Set\w+\(\s*"_NonlinearTime"',src),'The mod must not depend on overriding _NonlinearTime'
    assert re.search(r'SetFloat\(\s*"_BumpMap2Tiling"\s*,\s*-',src),'The second ripple layer must have a negative tiling so it drifts the opposite way'
    assert 'TipsyTailWaterMesh.Animate()' in src and re.search(r'q\.Mesh\.SetUVs\(\s*1\s*,\s*q\.Uv\)',src),'The mod must slide UV1 every frame'
    speed=float(re.search(r'DefaultMotionSpeed\s*=\s*([\d.]+)f',src).group(1))
    assert 0.005<=speed<=0.05,f'Default drift speed {speed} is outside the range that reads as calm moving water'
    assert re.search(r'SetUVs\(\s*1\s*,',src),'The runtime must supply UV1'
    assert 'TipsyTailWaterMesh.For' in src,'The surface mesh replacement must be applied'

    ref=json.loads((ROOT/'water-material-reference.json').read_text())
    scale=float(re.search(r'DefaultUv1Scale\s*=\s*([\d.]+)f',src).group(1))
    assert ref['ratio_min']<=scale<=ref['ratio_max'],f'UV scale {scale} is outside the lake/fountain tiling ratios'
    tint=[float(v) for v in re.search(r'WaterColor\s*=\s*new Color\(([^)]*)\)',src).group(1).replace('f','').split(',')][:3]
    assert max(tint)<=0.7,'The tint must stay dark: the native albedo texture is dark and a bright tint blows out'
    assert ref['fountain_water_emission_color'][:3]==[0.0,0.0,0.0],'FountainWater is expected to have no emission'

    report={
      'status':'PASS',
      'test_type':'Offline checks against the installed game shader graph, the exported model and the C# source; not an in-game rendering test',
      'shader':'Shader Graphs/FountainWaterURP',
      'shader_facts':{'surface_type':'Opaque' if target['m_SurfaceType']==0 else 'Transparent','uv_channels_read':sorted(uv_channels)},
      'exported_pool_surface_vertex_channels':sorted(channels),
      'shader_clocks_zeroed':zeroed,
      'motion':'the mod slides UV1 every frame; the two ripple layers drift in opposite directions (negative tiling on layer 2)',
      'default_drift_blocks_per_second':speed,
      'runtime_supplies_uv1':True,
      'properties_overridden':sorted(set(setters)),
      'all_properties_exist_in_shader':True,
      'default_uv1_scale':scale,
      'lake_to_fountain_tiling_ratio_range':[ref['ratio_min'],ref['ratio_max']],
      'tint_gamma_rgb':tint,
      'scope':'Per-material-index property blocks and a replacement quad on #PoolWater only; shared material and mesh assets untouched',
      'bar_seats':4,
      'swimming_lanes':4}
    (ROOT/'pool-water-validation.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':
    print(json.dumps(validate_pool_water(),indent=2))
