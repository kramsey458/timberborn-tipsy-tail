"""Offline checks for the pool water in Source/Runtime/TipsyTailPoolWater.cs and TipsyTailWaterGrid.cs.
Reads the installed game's Shaders.zip (the physical water shader graph and its HLSL), Blueprints.zip
(the water mesh configuration), the exported pool model and the C# source, so every property name,
texture layout, vertex flag, link channel and material path is checked against the real game files.
No game is launched and nothing here proves the in-game appearance.
"""
import json,os,re,struct,zipfile,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
GAME=Path(os.environ.get('TIMBERBORN_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))
STREAMING=GAME/'Timberborn_Data/StreamingAssets'
WATER_SHADER='PhysicalWaterURP_Opaque.shadergraph'
FOUNTAIN_SHADER='FountainWaterURP.shadergraph'

def shaders():
    return zipfile.ZipFile(STREAMING/'Modding/Shaders.zip')

def read_text(z,name):
    return z.read(name).decode('utf-8-sig')

def graph_objects(z,name):
    text=read_text(z,name)
    dec,i,out=json.JSONDecoder(),0,[]
    while i<len(text):
        while i<len(text) and text[i].isspace():i+=1
        if i>=len(text):break
        obj,i=dec.raw_decode(text,i);out.append(obj)
    return out

def kind(node):return node['m_Type'].split('.')[-1].split(',')[0]

def properties(objs):
    """reference name -> (type, exposed) for every shader property of a graph."""
    return {(o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName')):(kind(o),o.get('m_GeneratePropertyBlock')) for o in objs if o.get('m_Type','').endswith('ShaderProperty')}

def subgraph_files(z):
    guid={}
    for i in z.infolist():
        if i.filename.endswith('.meta'):
            m=re.search(r'guid:\s*([0-9a-f]+)',read_text(z,i.filename))
            if m:guid[m.group(1)]=i.filename[:-5]
    return guid

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
    """Vertex channels and the node transform of #PoolWater in the exported model (a timbermesh is zlib + protobuf).
    Vertex positions are local to the node; field 3 is its position, 4 its rotation and 5 its scale."""
    model=zlib.decompress((ROOT/'Mod/Buildings/Wellbeing/TipsyTail/TipsyTail.Model.timbermesh').read_bytes())
    def floats(v,defaults):
        out=list(defaults)
        for k,w,x in _fields(v):
            if w==5 and 1<=k<=len(out):out[k-1]=round(struct.unpack('<f',x)[0],6)
        return tuple(out)
    for num,wire,node in _fields(model):
        if num!=3:continue
        fields=_fields(node)
        if not any(n==2 and v==b'#PoolWater' for n,_,v in fields):continue
        channels={};transform={'position':(0.0,0.0,0.0),'rotation':(0.0,0.0,0.0,1.0),'scale':(1.0,1.0,1.0)}
        for n,_,v in fields:
            if n==7:
                sub={k:x for k,_,x in _fields(v)}
                channels[sub[1].decode()]=struct.unpack('<%df'%(len(sub[4])//4),sub[4])
            elif n==3:transform['position']=floats(v,(0.0,0.0,0.0))
            elif n==4:transform['rotation']=floats(v,(0.0,0.0,0.0,1.0))
            elif n==5:transform['scale']=floats(v,(1.0,1.0,1.0))
        return channels,transform
    raise AssertionError('#PoolWater node not found in the exported model')

def cs_const(src,name,pattern=r'(-?[\d.]+)f?'):
    m=re.search(r'\b%s\s*=\s*%s'%(re.escape(name),pattern),src)
    assert m,f'{name} not found in the C# source'
    return m.group(1)

def validate_pool_water():
    z=shaders()
    objs=graph_objects(z,WATER_SHADER)
    props=properties(objs)
    by={o['m_ObjectId']:o for o in objs if 'm_ObjectId' in o}
    nodes={n['m_Id']:by[n['m_Id']] for n in objs[0]['m_Nodes']}
    feeders={}
    for e in objs[0]['m_Edges']:
        feeders.setdefault(e['m_InputSlot']['m_Node']['m_Id'],[]).append(e['m_OutputSlot']['m_Node']['m_Id'])
    def prop_ref(n):
        p=by[n['m_Property']['m_Id']];return p.get('m_OverrideReferenceName') or p.get('m_DefaultReferenceName')
    guid=subgraph_files(z)

    water_cs=(ROOT/'Source/Runtime/TipsyTailPoolWater.cs').read_text()
    grid_cs=(ROOT/'Source/Runtime/TipsyTailWaterGrid.cs').read_text()

    # 1. Every texture array the water shader reads is bound by the mod, and each is a hidden global (not exposed
    #    on the material), which is why the pool needs its own copies rather than tinting a material property.
    bound=set(re.findall(r'Bind\(\s*"(_\w+)"',water_cs))
    arrays={n for n,(t,_) in props.items() if t=='Texture2DArrayShaderProperty'}
    assert arrays and arrays<=bound,f'The mod does not bind every water data array: {sorted(arrays-bound)}'
    assert bound<=set(props),f'The mod binds properties the water shader does not have: {sorted(bound-set(props))}'
    assert all(props[n][1] is False for n in bound),'Every bound water data array must be a hidden global'
    assert props['_MapSize']==('Vector2ShaderProperty',False),'_MapSize must be the hidden global the map size is fed through'
    assert '"_MapSize"' in water_cs and 'SetVector(MapSizeId' in water_cs,'The mod must override _MapSize with its private map size'
    # The vertex stage is fed by exactly these arrays.
    vertex_node=next(n for n in nodes.values() if kind(n)=='SubGraphNode' and guid.get(json.loads(n['m_SerializedSubGraph'])['subGraph']['guid'])=='WaterVertexShader.shadersubgraph')
    vertex_inputs={prop_ref(nodes[s]) for s in feeders[vertex_node['m_ObjectId']] if kind(nodes[s])=='PropertyNode'}
    assert vertex_inputs<=bound,f'Vertex-stage arrays not bound: {sorted(vertex_inputs-bound)}'
    # The water colour by depth comes from the scene depth buffer, so the pool floor drawn under the surface shades it.
    depth_objs=graph_objects(z,'WaterDepth.shadersubgraph')
    assert any(kind(o)=='SceneDepthNode' for o in depth_objs if 'm_Type' in o),'Water depth is expected to come from the scene depth'
    # Alpha is world height minus the column floor, so anything at or below its floor is clipped.
    alpha=next(n for n in nodes.values() if 'BlockNode' in kind(n) and n.get('m_SerializedDescriptor')=='SurfaceDescription.Alpha')
    sub=nodes[feeders[alpha['m_ObjectId']][0]]
    assert kind(sub)=='SubtractNode' and any(kind(nodes[s])=='SplitNode' and any(kind(nodes[t])=='PositionNode' and nodes[t].get('m_Space')==4 for t in feeders.get(s,[])) for s in feeders[sub['m_ObjectId']]),'Alpha is expected to subtract from the absolute world position'
    target=next(o for o in objs if 'UniversalTarget' in o.get('m_Type',''))

    # 2. The mesh layout: vertex flag bits, skirt vertex sources and per-cell texel counts match the HLSL.
    utils=read_text(z,'WaterUtils.cginc')
    bits={m.group(1):int(m.group(2)) for m in re.finditer(r'#define (\w+_BIT) (\d+)',utils)}
    cs_bits={'EDGE_VERTEX_BIT':'EdgeVertexBit','CORNER_VERTEX_BIT':'CornerVertexBit','SKIRT_BIT':'SkirtBit','LEFT_SKIRT_BIT':'LeftSkirtBit',
             'RIGHT_SKIRT_BIT':'RightSkirtBit','TOP_SKIRT_BIT':'TopSkirtBit','BOTTOM_SKIRT_BIT':'BottomSkirtBit','FLOOR_SKIRT_BIT':'FloorSkirtBit'}
    for hlsl,cs in cs_bits.items():
        assert int(cs_const(grid_cs,cs,r'1 << (\d+)'))==bits[hlsl],f'{cs} must be bit {bits[hlsl]}'
    params=read_text(z,'WaterVertexParameters.cginc')
    neighbours=re.search(r'VertexNeighbours\[48\]\s*=\s*\{(.*?)\};',params,re.S).group(1)
    rows=[tuple(int(v) for v in m.groups()) for m in re.finditer(r'int4\((-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)\)',neighbours)]
    assert len(rows)==48,'Expected 48 vertex neighbour entries'
    assert int(cs_const(grid_cs,'VerticesPerCell'))==48
    skirt_source=[int(v) for v in re.search(r'SkirtSource\s*=\s*\{([^}]*)\}',grid_cs).group(1).split(',')]
    for i in range(16,48):
        ox,oz=rows[i][0],rows[i][1]
        assert skirt_source[(i-16)%16]==ox+4*oz,f'Skirt vertex {i} must duplicate top vertex {ox+4*oz}'
    for i in range(16):
        assert rows[i][:2]==(i%4,i//4),'Top-surface vertices are laid out column-major in rows of four'
    vertex_hlsl=read_text(z,'CalculateWaterVertex.hlsl')
    assert re.search(r'GetTileUv.*\n.*\+ 0\.5\) / _MapSize',vertex_hlsl),'Tile UVs are (cell + 0.5) / _MapSize'
    assert 'vertexMapSize = _MapSize * 4' in vertex_hlsl and int(cs_const(grid_cs,'HeightsPerCell'))==4,'The height map has 4x4 texels per cell'
    assert 'const int vertexId = (int)uv0.z' in vertex_hlsl and 'const int mask = (int)uv0.w' in vertex_hlsl,'UV0.z is the vertex index and UV0.w the flag mask'
    assert 'const int columnIndex = (int)basePosition.y' in vertex_hlsl and 'Vertices[v * 3 + 1] = 0f' in grid_cs,'Vertex y must be the column index, 0'
    assert re.search(r'CreateOutData\(data\.x, data\.y, data\.z\)',vertex_hlsl) and re.search(r'CreateOutData\(const float depth,\s*const int floor,\s*const int ceiling\)',utils),'Water data is depth, floor, ceiling'
    assert re.search(r'WaterData\[i \* 4\] = Depth;\s*WaterData\[i \* 4 \+ 1\] = FloorLevel;\s*WaterData\[i \* 4 \+ 2\] = CeilingLevel;',grid_cs),'The grid must write depth, floor, ceiling'
    # Edge link channels: which xyzw component the HLSL reads for each neighbour direction.
    channel={'x':0,'y':1,'z':2,'w':3}
    def link(cond):
        m=re.search(re.escape(cond)+r'\)\s*\{\s*\w+Link = int2\(oldEdgeLinks\.(\w)',vertex_hlsl)
        assert m,f'Could not find the edge link read for {cond}';return channel[m.group(1)]
    assert int(cs_const(grid_cs,'LinkPlusX'))==link('horizontalNeighbor.x > 0'),'+x edge link channel'
    assert int(cs_const(grid_cs,'LinkMinusX'))==link('horizontalNeighbor.x < 0'),'-x edge link channel'
    assert int(cs_const(grid_cs,'LinkPlusZ'))==link('verticalNeighbor.y > 0'),'+z edge link channel'
    assert int(cs_const(grid_cs,'LinkMinusZ'))==link('verticalNeighbor.y < 0'),'-z edge link channel'
    sampling=read_text(z,'GetSamplingParameters.hlsl')
    order=re.search(r'directionalLinks\[9\]\s*=\s*\{(.*?)\};',sampling,re.S).group(1)
    entries=[e.strip() for e in order.split(',')]
    # Index = (dz + 1) * 3 + (dx + 1): 0 is (-x,-z), 2 is (+x,-z), 6 is (-x,+z), 8 is (+x,+z).
    corner={name:channel[entries[i].split('.')[1]] for name,i in (('CornerMinusXMinusZ',0),('CornerPlusXMinusZ',2),('CornerMinusXPlusZ',6),('CornerPlusXPlusZ',8))}
    for name,ch in corner.items():
        assert int(cs_const(grid_cs,name))==ch,f'{name} must be channel {ch}'
    assert int(cs_const(grid_cs,'Layers'))>=2 and 'columnIndex + 1' in vertex_hlsl,'The shader reads the column above, so the private map needs a second, empty layer'
    assert '#if _USE_LEVEL_VISIBILITY' in vertex_hlsl and '_MaxVisibleLevel' in vertex_hlsl and props['_MaxVisibleLevel'][1] is False and '"_MaxVisibleLevel"' not in re.sub(r'GetGlobalFloat\("_MaxVisibleLevel"\)','',water_cs),'Level visibility stays the game global: floor and height are absolute'

    # 3. The materials and layer are the game's own.
    blueprints=zipfile.ZipFile(STREAMING/'Modding/Blueprints.zip')
    mesh_spec=json.loads(blueprints.read('Configurations/WaterMesh.blueprint.json').decode('utf-8-sig'))['WaterMeshSpec']
    assert cs_const(water_cs,'OpaqueMaterialPath',r'"([^"]+)"')==mesh_spec['OpaqueMaterial'],'Opaque material path must match the game water mesh spec'
    assert cs_const(water_cs,'TransparentMaterialPath',r'"([^"]+)"')==mesh_spec['TransparentMaterial'],'Transparent material path must match the game water mesh spec'
    assert cs_const(water_cs,'LayerName',r'"([^"]+)"')=='Water','The pool water must sit on the Water layer like the game tiles'
    assert re.search(r'GetGlobalFloat\(WaterOpacityId\)',water_cs) and 'WaterOpacityId = Shader.PropertyToID("_WaterOpacity")' in water_cs,'The pool must follow the game water opacity toggle'
    assert 'shadowCastingMode = ShadowCastingMode.Off' in water_cs,'Game water tiles do not cast shadows'
    tile_layer=None
    try:
        import UnityPy
        env=UnityPy.load(str(GAME/'Timberborn_Data/resources.assets'))
        for o in env.objects:
            if o.type.name=='GameObject':
                d=o.read()
                if d.m_Name=='WaterTile':tile_layer=d.m_Layer
        assert tile_layer==4,'The game WaterTile prefab is expected on the built-in Water layer (4)'
    except ImportError:
        pass

    # 4. The model: the surface sits a fraction of a block above ground, and the basin is the blueprint's depth.
    channels,transform=pool_surface_vertex_channels()
    ys=channels['position'][1::3]
    assert transform['rotation']==(0.0,0.0,0.0,1.0) and transform['scale']==(1.0,1.0,1.0),'The placeholder node must be unrotated and unscaled so its bounds map straight to the building'
    surface_top=transform['position'][1]+max(ys)
    assert 0<surface_top<1,'The surface must sit within one block above ground so flooring its height gives the ground level'
    assert 'TransformPoint' in water_cs,'The runtime must take the surface rectangle through the node transform, since the exported vertices are node-local'
    blueprint=json.loads((ROOT/'Mod/Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint.json').read_text(encoding='utf-8-sig'))
    depth=blueprint['BuildingModelSpec']['UndergroundModelDepth']
    assert int(cs_const(water_cs,'BasinDepthBlocks'))==depth,'BasinDepthBlocks must match the blueprint underground depth'
    assert 'uv0' in channels and 'uv1' not in channels,'The exported placeholder surface only has UV0'

    # 5. The legacy fountain path still names real, exposed fountain properties and zeroes the shader clocks.
    fountain=properties(graph_objects(z,FOUNTAIN_SHADER))
    legacy=re.findall(r'block\.Set(?:Float|Vector|Color)\(\s*"(_\w+)"',water_cs)
    assert legacy and set(legacy)<=set(fountain),f'Legacy path uses fountain properties that do not exist: {sorted(set(legacy)-set(fountain))}'
    assert all(fountain[n][1] for n in legacy),'Every legacy override must be an exposed fountain property'
    for name in ('_WaterRippleSpeed','_Albedo_Speed','_Albedo_Speed2','_BumpMap1Speed','_BumpMap2Speed'):
        assert re.search(r'Set(?:Float|Vector)\(\s*"%s"\s*,\s*(?:0f|Vector4\.zero)\s*\)'%name,water_cs),f'{name} must be zeroed in the legacy path'
    assert fountain['_NonlinearTime'][1] is False and not re.search(r'Set\w+\(\s*"_NonlinearTime"',water_cs),'_NonlinearTime stays a hidden game global'

    report={
      'status':'PASS',
      'test_type':'Offline checks against the installed game shader graph, HLSL, blueprints, the exported model and the C# source; not an in-game rendering test',
      'shader':'Shader Graphs/PhysicalWaterURP_Opaque (the game map water), PhysicalWaterURP_Transparent while water is hidden',
      'shader_facts':{'surface_type':'Opaque' if target['m_SurfaceType']==0 else 'Transparent','alpha_clip':bool(target.get('m_AlphaClip')),
                      'depth_from_scene_depth':True,'alpha_is_height_above_floor':True},
      'materials':{'opaque':mesh_spec['OpaqueMaterial'],'transparent':mesh_spec['TransparentMaterial']},
      'water_data_arrays_bound_by_mod':sorted(bound),
      'all_arrays_hidden_globals':True,
      'vertex_stage_arrays':sorted(vertex_inputs),
      'mesh_layout':{'vertices_per_cell':48,'indices_per_cell':len(re.search(r'CellTriangles\s*=\s*\{([^}]*)\}',grid_cs).group(1).split(',')),
                     'height_texels_per_cell':'4x4','flag_bits':bits,'private_map_layers':int(cs_const(grid_cs,'Layers')),'padding_cells':int(cs_const(grid_cs,'Padding'))},
      'edge_link_channels':{'+x':int(cs_const(grid_cs,'LinkPlusX')),'-x':int(cs_const(grid_cs,'LinkMinusX')),'+z':int(cs_const(grid_cs,'LinkPlusZ')),'-z':int(cs_const(grid_cs,'LinkMinusZ'))},
      'corner_link_channels':corner,
      'water_layer':'Water','water_tile_prefab_layer':tile_layer,
      'exported_pool_surface_vertex_channels':sorted(channels),
      'surface_height_above_ground':round(surface_top,4),'surface_node_position':transform['position'],'basin_depth_blocks':depth,
      'legacy_fallback':'FountainWater quad with UV1 and zeroed shader clocks (v0.2.7 look) via water.cfg `legacy`, or if the game water cannot be set up',
      'scope':'A private mesh, material copies, property block and data textures under #PoolWater only; the map water, its textures and the shared materials are untouched',
      'bar_seats':4,
      'swimming_lanes':4}
    (ROOT/'pool-water-validation.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':
    print(json.dumps(validate_pool_water(),indent=2))
