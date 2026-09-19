"""Record the game's own water material values that the pool water is calibrated against.
Needs UnityPy (pip install UnityPy) and the installed game; it only reads resources.assets.
Writes water-material-reference.json, which validate_pool_water.py uses to check the UV scale.
"""
import json,os,statistics
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[1]
GAME=Path(os.environ.get('TIMBERBORN_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))
NAMES=('FountainWater','PhysicalWater_Opaque','PhysicalWater_Transparent')
TILINGS=('_MainTexTiling','_MainTexTiling2','_BumpMap1Tiling','_BumpMap2Tiling','_MetallicGlossMapTiling','_DetailAlbedoTexTiling')

def read_materials():
    env=UnityPy.load(str(GAME/'Timberborn_Data'/'resources.assets'))
    found={}
    for o in env.objects:
        if o.type.name!='Material':continue
        m=o.read()
        if m.m_Name not in NAMES:continue
        sp=m.m_SavedProperties
        found[m.m_Name]={
            'render_queue':m.m_CustomRenderQueue,
            'colors':{k:[round(c.r,4),round(c.g,4),round(c.b,4),round(c.a,4)] for k,c in sp.m_Colors},
            'floats':{k:round(v,5) for k,v in sp.m_Floats},
            'textures':{k:t.m_Texture.read().m_Name for k,t in sp.m_TexEnvs if t.m_Texture and t.m_Texture.path_id}}
    return found

def main():
    mats=read_materials()
    assert set(mats)==set(NAMES),f'Missing materials: {set(NAMES)-set(mats)}'
    fountain,lake=mats['FountainWater']['floats'],mats['PhysicalWater_Opaque']['floats']
    # The lake's tilings against the fountain's, for the layers both materials share.
    pairs={'albedo':('_MainTexTiling','_MainTexTiling'),'bump1':('_BumpMap1Tiling','_BumpMap1Tiling'),
           'bump2':('_BumpMap2Tiling','_BumpMap2Tiling'),'gloss':('_MetallicGlossMapTiling','_MetallicGlossMapTiling')}
    ratios={k:round(lake[l]/fountain[f],4) for k,(f,l) in pairs.items()}
    report={
        'note':'Read from the installed game with UnityPy; not an in-game measurement.',
        'game_version_file':(GAME/'Timberborn_Data'/'StreamingAssets'/'Version.txt').read_text().strip(),
        'lake_to_fountain_tiling_ratio':ratios,
        'ratio_min':min(ratios.values()),'ratio_max':max(ratios.values()),'ratio_median':round(statistics.median(ratios.values()),4),
        'fountain_water_emission_color':mats['FountainWater']['colors'].get('_EmissionColor'),
        'lake_deep_water_color':mats['PhysicalWater_Opaque']['colors'].get('_AlbedoAtMaxDepth'),
        'shared_textures':{k:v for k,v in mats['FountainWater']['textures'].items() if v in mats['PhysicalWater_Opaque']['textures'].values()},
        'materials':mats}
    (ROOT/'water-material-reference.json').write_text(json.dumps(report,indent=1)+'\n')
    return report

if __name__=='__main__':
    r=main()
    print(json.dumps({k:r[k] for k in ('lake_to_fountain_tiling_ratio','ratio_min','ratio_max','ratio_median','fountain_water_emission_color','lake_deep_water_color','shared_textures')},indent=1))
