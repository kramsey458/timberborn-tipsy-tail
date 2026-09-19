"""Build the Tipsy Tail data mod from installed Timberborn 1.1.2.4 blueprints.
Usage: python build_mod.py <path-to-extracted-Blueprints.zip>
Assets are generated separately by create_assets.py in Blender.
"""
import csv,copy,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MOD=ROOT/'Mod'; BASE=Path(sys.argv[1])
def read(p):return json.loads((BASE/p).read_text(encoding='utf-8-sig'))
def write(p,d):
    p=MOD/p;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
b=read('Buildings/Wellbeing/Campfire/Campfire.Folktails.blueprint.json')
for k in ['AttractionFireSpec','FireSpec','IlluminatorLightObjectsSpec','TemplateAttachmentsSpec']:
    b.pop(k,None)
b['BuildingSpec'].update(SelectionSoundName='Lido',BuildingCost=[{'Id':'Log','Amount':60},{'Id':'Plank','Amount':40},{'Id':'Gear','Amount':10}],ScienceCost=500)
b['BuildingModelSpec'].update(UndergroundModelName='#Underground',UndergroundModelDepth=2)
b['TemplateSpec']['TemplateName']='TipsyTail'
# WetFur is a Common need, so both factions already load it. 0.5/hour matches Lido and SwimmingPool.
b['AttractionSpec']['Effects']=[{'NeedId':'TipsyTail','PointsPerHour':0.6,'SatisfyToMaxValue':False},{'NeedId':'WetFur','PointsPerHour':0.5,'SatisfyToMaxValue':False}]
b['EnterableSpec']['CapacityFinished']=8
b['TransformSlotInitializerSpec']['Slots']=[{'SlotKeyword':'BarSeat','Animation':'Sitting','Inanimate':False,'RandomizeYRotation':False,'WaterSlot':False}]
b['PatrollingSlotInitializerSpec']={'PatrollingSlots':[{'BaseMovementSpeed':0.45,'MaxRandomDeviationOfMovementSpeed':0.1,'SlotKeyword':'Swimming','Animation':'ForcedSwimming','WaterSlot':False}]}
b['BlockObjectSpec'].update(Size={'X':5,'Y':6,'Z':5},Entrance={'HasEntrance':True,'Coordinates':{'X':2,'Y':-1,'Z':2}},BaseZ=2,Flippable=False)
# BlockObjectSpec uses (height * depth + depthCoordinate) * width + x.
# Native underground blocks reserve existing soil, exactly as UndergroundPile.
b['BlockObjectSpec']['Blocks']=[{'MatterBelow':'Ground' if h==2 else 'Any','Occupations':'None' if h<2 else 'Bottom, Top, Corners, Path, Middle','Stackable':'None','OccupyAllBelow':False,'Underground':h<2} for h in range(5) for z in range(6) for x in range(5)]
# Cutout coordinates are grid-relative (BaseZ included); model/slot coordinates
# are surface-relative because BlockObject.UpdateTransform adds BaseZ itself.
b.pop('BuildingTerrainCutoutSpec',None)
b['TipsyTailTerrainCutoutSpec']={'CutoutTiles':[{'X':x,'Y':z,'Z':2} for x in range(5) for z in range(6)]}
b['TipsyTailPoolWaterSpec']={}
b['UndergroundDepthDescriberSpec']={'Depth':2}
b['BuildingAccessibleSpec'].update(LocalAccess={'X':2.5,'Y':2.0,'Z':0.08},ForceOneFinalAccess=False)
b['PlaceableBlockObjectSpec']['ToolOrder']=85
b['LabeledEntitySpec']={'DisplayNameLocKey':'Building.TipsyTail.DisplayName','DescriptionLocKey':'Building.TipsyTail.Description','FlavorDescriptionLocKey':'Building.TipsyTail.FlavorDescription','Icon':'Buildings/Wellbeing/TipsyTail/TipsyTailIcon'}
# Without GoodConsumingAttractionSpec there is no visitor-dependent consumption toggle.
# GoodConsumingBuilding is an IBuildingEfficiencyProvider, so an empty reserve gates recreation.
b['GoodConsumingBuildingSpec']={'FullInventoryWorkHours':120,'ConsumedGoods':[{'GoodId':'Water','GoodPerHour':0.5}]}
b['GoodConsumingAttractionSurfaceControllerSpec']={'SurfaceName':'#PoolWater','AttachmentIds':[]}
# The native visualizer reserves the first unfinished child as its base.
# We have one persistent construction visual, hence zero extra thresholds.
b['ConstructionSiteProgressVisualizerSpec']={'ProgressThresholds':[]}
def colliders(boxes):
    return {'BoxColliders':[{'Center':dict(zip(('X','Y','Z'),p)),'Size':dict(zip(('X','Y','Z'),s))} for p,s in boxes],'SphereColliders':[],'CapsuleColliders':[]}
# Reuse the stock dirt-and-perimeter-stakes base (same family as Campfire).
# Unity model axes are X=width, Y=height, Z=depth: stretch 5x5 to 5x6.
construction_base=read('ConstructionBases/ConstructionBase5x5/ConstructionBase5x5.blueprint.json')
construction_base['TransformSpec']={'Position':{'X':0.0,'Y':0.0,'Z':0.0},'Rotation':{'X':0.0,'Y':0.0,'Z':0.0},'Scale':{'X':1.0,'Y':1.0,'Z':1.2}}
b['Children']={
    '#Finished':{'TimbermeshSpec':{'Model':'Buildings/Wellbeing/TipsyTail/TipsyTail.Model'},'CollidersSpec':colliders([((2.5,.09,3),(5,.18,6)),((2.5,1.2,5),(4.3,2.4,1.7))])},
    '#Unfinished':{'Children':{'ConstructionBase':construction_base}},
    '#Underground':{'TimbermeshSpec':{'Model':'Buildings/Wellbeing/TipsyTail/TipsyTail.Underground'}}}
write(Path('Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint.json'),b)
for faction in ['Folktails','IronTeeth']:
    write(Path(f'TemplateCollections/TemplateCollection.Buildings.{faction}.blueprint.json'),{'TemplateCollectionSpec':{'CollectionId':f'Buildings.{faction}','Blueprints#append':['Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint']}})
    write(Path(f'NeedCollections/NeedCollection.{faction}.blueprint.json'),{'NeedCollectionSpec':{'CollectionId':faction,'Needs#append':['TipsyTail']}})
# MaterialRepository only loads Common plus the CURRENT faction's collections.
# This shared model uses Folktails timber and the contained FountainWater material.
# Register the missing dependencies explicitly in each faction; resource presence
# alone does not make a material available to the Timbermesh importer.
material_dependencies=[
    'Materials/UberAtlas/Materials/Folktails/BaseMetal.Folktails',
    'Materials/UberAtlas/Materials/Folktails/BaseWood_Brown.Folktails',
    'Materials/UberAtlas/Materials/Folktails/BaseWood_LightBrown.Folktails',
    'Materials/UberAtlas/Materials/Folktails/BaseWood_White.Folktails',
    'Materials/UberAtlas/Materials/Folktails/ThatchedRoof.Folktails',
    'Buildings/Monuments/FountainOfJoy/FountainWater',
]
common=read('MaterialCollections/MaterialCollection.Common.blueprint.json')['MaterialCollectionSpec']['Materials']
for faction in ['Folktails','IronTeeth']:
    existing=read(f'MaterialCollections/MaterialCollection.{faction}.blueprint.json')['MaterialCollectionSpec']['Materials']
    missing=[p for p in material_dependencies if p not in common+existing]
    write(Path(f'MaterialCollections/MaterialCollection.{faction}.blueprint.json'),{'MaterialCollectionSpec':{'CollectionId':faction,'Materials#append':missing}})
need=read('Needs/Need.Beaver.Campfire.blueprint.json')
need['NeedSpec'].update(Id='TipsyTail',Order=45,DisplayNameLocKey='Building.TipsyTail.DisplayName',FavorableWellbeing=2)
write(Path('Needs/Need.Beaver.TipsyTail.blueprint.json'),need)
write(Path('Buildings/Wellbeing/TipsyTail/TipsyTailIcon.png.meta.json'),{'isSprite':True})
write(Path('manifest.json'),{'Name':'The Tipsy Tail','Version':'0.2.6.0','Id':'Kyler.TipsyTail','MinimumGameVersion':'1.1.2.4','Description':'A self-contained swim-up pool bar for both factions. Two-block underground basin, hauled water, and eight visitors. Includes scoped terrain-cutout cleanup so demolition reveals the original ground.','RequiredMods':[]})
loc=MOD/'Localizations';loc.mkdir(exist_ok=True)
with (loc/'enUS.csv').open('w',newline='',encoding='utf-8') as f:
    w=csv.writer(f);w.writerow(['ID','Text','Comment'])
    w.writerow(['Building.TipsyTail.DisplayName','The Tipsy Tail',''])
    w.writerow(['Building.TipsyTail.Description','A self-contained pool and swim-up bar built two blocks into solid ground. Requires a level 5 x 6 site with two soil layers beneath it. Visitors also wet their fur here. Haulers supply water. Holds 60 water and evaporates 12 per day while operating, even without visitors. Recreation, including wet fur relief, stops when dry. Requires a staffed Hauling Post.',''])
    w.writerow(['Building.TipsyTail.FlavorDescription','Leave your worries on the shore. Bring your own tail.',''])
print('Built mod data:',MOD)
