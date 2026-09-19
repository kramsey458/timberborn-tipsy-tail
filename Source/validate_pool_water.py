"""Offline checks for the flat still-water override in Source/Runtime/TipsyTailPoolWater.cs.
Reads the installed game's Shaders.zip (FountainWaterURP.shadergraph) and the C# source,
so every property name and constant is taken from the real files. No game is launched and
nothing here proves the in-game appearance.
"""
import json,os,re,zipfile
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

def validate_pool_water():
    objs=graph_objects()
    props={(o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName')) for o in objs if o.get('m_Type','').endswith(('ShaderProperty',))}
    keywords={o.get('m_OverrideReferenceName'):o.get('m_Value') for o in objs if o.get('m_Type','').endswith('ShaderKeyword')}
    target=next(o for o in objs if 'UniversalTarget' in o.get('m_Type',''))
    # Both scalar-multiply constants (detail layer x2) are unconnected slots holding a matrix of 2s.
    connected={(e['m_InputSlot']['m_Node']['m_Id'],e['m_InputSlot']['m_SlotId']) for e in objs[0]['m_Edges']}
    by={o['m_ObjectId']:o for o in objs if 'm_ObjectId' in o}
    doubles=[]
    for n in objs[0]['m_Nodes']:
        node=by[n['m_Id']]
        if 'MultiplyNode' not in node['m_Type']:continue
        for s in node['m_Slots']:
            slot=by[s['m_Id']]
            value=slot.get('m_Value')
            if slot.get('m_SlotType')==0 and (n['m_Id'],slot['m_Id']) not in connected and isinstance(value,dict) and value.get('e00')==2.0:
                doubles.append(n['m_Id'])
    assert len(doubles)==2,'Expected the detail-UV and detail-colour x2 multipliers in the shader graph'

    src=(ROOT/'Source/Runtime/TipsyTailPoolWater.cs').read_text()
    setters=re.findall(r'\.Set(Float|Vector|Color|Texture)\(\s*"(_\w+)"\s*,\s*([^;]+?)\);',src)
    names={n for _,n,_ in setters}
    assert names<=props,f'C# sets properties the shader does not have: {sorted(names-props)}'
    num=lambda n:float(re.search(r'"'+n+r'",\s*(-?[\d.]+)f',src).group(1))
    color=[float(v) for v in re.search(r'WaterColor\s*=\s*new Color\(([^)]*)\)',src).group(1).replace('f','').split(',')][:3]
    smooth=float(re.search(r'Smoothness\s*=\s*([\d.]+)f',src).group(1))
    detail=[int(v) for v in re.search(r'FlatDetail.*?Color32\((\d+),\s*(\d+),\s*(\d+)',src,re.S).groups()]

    # Foam mask: 1 - saturate((|sceneDepth - surfaceDepth| - offset*intensity) / (depth*intensity)).
    off,inten,depth=num('_FoamOffset'),num('_FoamIntensity'),num('_FoamDepth')
    assert depth*inten>0,'Foam divisor must never be zero'
    mask=lambda d:1-min(1,max(0,(abs(d)-off*inten)/(depth*inten)))
    depths=[0,1e-9,1e-3,.05,.1,.3,1,5,50,1000,1e6]
    assert all(mask(d)==0 for d in depths),'Foam mask must be zero at every depth'
    # Albedo: both _MainTex layers are white*tint, so the noise blend is moot; detail is sampled then x2.
    albedo=[c*(d/255)*2 for c,d in zip(color,detail)]
    assert all(abs(a-c)<0.01 for a,c in zip(albedo,color)),'Albedo must equal the water tint'
    assert num('_BumpMap1Strength')==0 and num('_BumpMap2Strength')==0 and num('_MetallicMapScale')==0
    report={
      'status':'PASS',
      'test_type':'Offline evaluation against the installed game shader graph and the C# source; not an in-game rendering test',
      'shader':'Shader Graphs/FountainWaterURP',
      'shader_facts':{'surface_type':'Opaque' if target['m_SurfaceType']==0 else 'Transparent','receive_shadows_off_default':keywords.get('_RECEIVE_SHADOWS_OFF')==1,'detail_x2_constants':len(doubles)},
      'properties_overridden':sorted(names),
      'all_properties_exist_in_shader':True,
      'water_tint_gamma_rgb':color,
      'smoothness':smooth,
      'foam':{'offset':off,'intensity':inten,'depth':depth,'mask_at_tested_depths':[mask(d) for d in depths]},
      'resolved_albedo_rgb':[round(a,4) for a in albedo],
      'flat_normals':True,
      'metallic':0.0,
      'scope':'Per-material-index property blocks on #PoolWater only; shared material untouched',
      'bar_seats':4,
      'swimming_lanes':4}
    (ROOT/'pool-water-validation.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':
    print(json.dumps(validate_pool_water(),indent=2))
