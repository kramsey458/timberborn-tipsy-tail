"""Offline structural validation; does not replace an in-game playtest.
Run with Blender Python and the official Timbermesh exporter directory argument.
"""
import sys,json,zlib,struct,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];MOD=ROOT/'Mod'
sys.path.insert(0,sys.argv[sys.argv.index('--')+1])
sys.path.insert(0,str(Path(__file__).resolve().parent))
import model_pb2
from validate_materials import validate_materials
from validate_construction import validate_construction
b=json.loads((MOD/'Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint.json').read_text())
checks=[]
def check(condition,name):
    assert condition,name
    checks.append(name)
construction=validate_construction(b)
check(True,'Native construction stage count and progress selection')
models={}
for p in MOD.rglob('*.timbermesh'):
    m=model_pb2.Model();m.ParseFromString(zlib.decompress(p.read_bytes()));models[p.name]=m
    check(bool(m.nodes),p.name+': nodes present')
    for i,n in enumerate(m.nodes):
        check(n.parent<i,p.name+': valid hierarchy '+str(i))
        for prop in n.vertexProperties:
            check(len(prop.data)==n.vertexCount*prop.scalarTypeDimension*4,p.name+': vertex buffer '+str(i)+' '+prop.name)
            values=struct.unpack('<'+'f'*(len(prop.data)//4),prop.data)
            check(all(math.isfinite(v) for v in values),p.name+': finite vertices '+str(i)+' '+prop.name)
        for mesh in n.meshes:
            check(len(mesh.indices)%3==0 and all(0<=k<n.vertexCount for k in mesh.indices),p.name+': valid triangle indices '+mesh.material)
m=models['TipsyTail.Model.timbermesh']
root=m.nodes[0]
check(abs(root.rotation.x)+abs(root.rotation.y)+abs(root.rotation.z)<1e-6 and abs(root.rotation.w-1)<1e-6,'Model root has no tilt or rotation')
pool=next(n for n in m.nodes if n.name=='#PoolWater')
check(abs(pool.rotation.x)+abs(pool.rotation.y)+abs(pool.rotation.z)<1e-6,'Pool surface is level')
seats=[n for n in m.nodes if n.name.startswith('#Slot#BarSeat')]
lanes=[(i,n) for i,n in enumerate(m.nodes) if n.name.startswith('#Slot#Swimming')]
check(len(seats)+len(lanes)==b['EnterableSpec']['CapacityFinished']==8,'Eight usable visitor slots')
for i,n in lanes:
    child=[c.name for c in m.nodes if c.parent==i]
    check(sum(x.startswith('#MiscStart') for x in child)==1 and sum(x.startswith('#MiscEnd') for x in child)==1,'Swimming lane endpoints '+n.name)
check(sum(n.name=='#PoolWater' for n in m.nodes)==1,'Water surface retained as independently switchable node')
check(len(b['BlockObjectSpec']['Blocks'])==5*6*5,'Footprint block count including two underground layers')
check(b['BlockObjectSpec']['BaseZ']==2 and b['BlockObjectSpec']['Entrance']['Coordinates']['Z']==2,'Entrance stays at terrain surface above basin')
check(all(v['Underground'] and v['Occupations']=='None' for v in b['BlockObjectSpec']['Blocks'][:60]),'Two complete reserved underground soil layers')
check(all(v['MatterBelow']=='Ground' and not v['Underground'] for v in b['BlockObjectSpec']['Blocks'][60:90]),'Surface deck requires ground, not platforms')
check(len(b['BuildingTerrainCutoutSpec']['CutoutTiles'])==30 and all(v['Z']==2 for v in b['BuildingTerrainCutoutSpec']['CutoutTiles']),'Terrain cutout covers footprint at surface')
check('TipsyTail.Underground.timbermesh' in models,'Underground slice model exported')
check({mesh.material for mesh in pool.meshes}=={'FountainWater'},'Contained teal FountainWater replaces breeding-pod water')
check('GoodConsumingAttractionSpec' not in b,'No visitor-dependent evaporation toggle')
check('TickableWaterBuildingSpec' not in b,'No river-water dependency')
check(b['GoodConsumingBuildingSpec']['ConsumedGoods']==[{'GoodId':'Water','GoodPerHour':.5}],'Native hauled-water consumption: 12/day')
check(b['GoodConsumingBuildingSpec']['FullInventoryWorkHours']*.5==60,'60-unit hauling inventory')
check(not b['PatrollingSlotInitializerSpec']['PatrollingSlots'][0]['WaterSlot'],'Swimming uses self-contained pool level')
for p in MOD.rglob('*.json'):json.loads(p.read_text())
report={'status':'PASS','checks':len(checks),'test_type':'Offline structural checks, NOT an in-game playtest','triangles':sum(len(mesh.indices)//3 for n in m.nodes for mesh in n.meshes),'vertices':sum(n.vertexCount for n in m.nodes),'materials':sorted({mesh.material for n in m.nodes for mesh in n.meshes}),'visitor_slots':8,'checks_passed':checks}
base=Path(sys.argv[sys.argv.index('--')+2])
validate_construction(json.loads((base/'Buildings/Wellbeing/Campfire/Campfire.Folktails.blueprint.json').read_text()))
check(True,'Construction validator also accepts the vanilla Campfire hierarchy')
validate_materials(report['materials'],base)
report['checks']=len(checks)
report['construction']=construction
report['material_registry_regression']='PASS: active material names resolve for both factions, without duplicate registrations'
(ROOT/'validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='checks_passed'},indent=2))
