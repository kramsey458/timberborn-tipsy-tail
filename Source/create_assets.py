"""Run with Blender 4.5: blender -b --python create_assets.py -- <exporter-directory>.
Creates original geometry, editable Blender source, Timbermesh models and previews.
Coordinates in helpers are game x, height, depth. No game geometry is copied.
"""
import bpy, math, sys, json, random, os
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Mod'/'Buildings'/'Wellbeing'/'TipsyTail'
ASSETS.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,sys.argv[sys.argv.index('--')+1])
from timbermesh_exporter import Exporter,ExportSettings
bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name,color,roughness=.7,metallic=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=roughness; bs.inputs['Metallic'].default_value=metallic
    return m
# These names resolve to existing Timberborn materials in the exported model.
wood=material('BaseWood_LightBrown.Folktails',(.43,.245,.10))
dark=material('BaseWood_Brown.Folktails',(.16,.07,.025))
white=material('BaseWood_White.Folktails',(.82,.75,.56))
metal=material('BaseMetal.Folktails',(.20,.23,.22),.4,.65)
thatch=material('ThatchedRoof.Folktails',(.63,.43,.12))
water=material('FountainWater',(.165,.482,.549),.22)
# Use the game's supplied reference materials for the preview as well as export.
reference=Path(os.environ.get('TIMBERBORN_PATH', r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))/'Timberborn_Data/StreamingAssets/Modding/TimberbornExampleModels.blend'
if reference.exists():
    names=[wood.name,dark.name,white.name,metal.name,thatch.name]
    for m in (wood,dark,white,metal,thatch):bpy.data.materials.remove(m)
    with bpy.data.libraries.load(str(reference),link=False) as (src,dst):dst.materials=list(names)
    wood,dark,white,metal,thatch=[bpy.data.materials[n] for n in names]
random.seed(42)

col=bpy.data.collections.new('TipsyTail.Model'); bpy.context.scene.collection.children.link(col)
def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o)
def coord(p): return (-p[0],-p[2],p[1])
def finish(o,name,mat):
    o.name=name;link(o)
    if mat:o.data.materials.append(mat)
    return o
def box(name,p,size,mat,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=coord(p));o=bpy.context.object
    o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    # Timberborn wood is an atlas: keep each face inside one painted plank island.
    uv=o.data.uv_layers.active.data
    wood_atlas=mat in (wood,dark,white)
    strip=.517+random.randrange(4)*.061
    for face in o.data.polygons:
        axis=max(range(3),key=lambda j:abs(face.normal[j]))
        a,b=((1,2) if axis==0 else (0,2) if axis==1 else (0,1))
        dims=(size[0],size[2],size[1])
        if dims[a]>dims[b]:a,b=b,a
        for li in face.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            uv[li].uv=((strip+(v[a]/dims[a]+.5)*.040,.535+(v[b]/dims[b]+.5)*.425) if wood_atlas else (v[a]*.5+.35,v[b]*.5+.35))
    if bevel:
        mod=o.modifiers.new('Worn edges','BEVEL');mod.width=bevel;mod.segments=1
    return finish(o,name,mat)
def cyl(name,p,r,depth,mat,vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=coord(p))
    o=bpy.context.object
    if mat in (wood,dark,white):
        uv=o.data.uv_layers.active.data
        for face in o.data.polygons:
            for li in face.loop_indices:
                old=uv[li].uv.copy()
                uv[li].uv=((.69+(old.x-.5)*.09,.185+(old.y-.5)*.09) if abs(face.normal.z)>.8 else (.035+old.x*.41,.54+old.y*.42))
    return finish(o,name,mat)
def beam(name,a,b,r,mat):
    va,vb=Vector(coord(a)),Vector(coord(b));o=cyl(name,(0,0,0),r,(vb-va).length,mat)
    o.location=(va+vb)/2;o.rotation_euler=(vb-va).to_track_quat('Z','Y').to_euler();return o
def empty(name,p,parent=None):
    o=bpy.data.objects.new(name,None);col.objects.link(o);o.location=coord(p)
    if parent:o.parent=parent
    o.empty_display_size=.15;return o
def text(name,string,p,size,mat):
    cu=bpy.data.curves.new(name,'FONT');cu.body=string;cu.align_x='CENTER';cu.size=size;cu.extrude=.004
    ob=bpy.data.objects.new(name,cu);col.objects.link(ob);ob.location=coord(p)
    ob.rotation_euler=(math.pi/2,0,math.pi);cu.materials.append(mat)
    bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    bpy.ops.object.convert(target='MESH');ob.select_set(False);return bpy.context.object

# All dimensions here are relative to ground level. Basin floor is two blocks down.
# Each narrow plank gets its own atlas island, avoiding a stretched single floor.
box('Basin floor backing',(2.5,-1.975,2.91),(3.94,.05,3.80),dark)
for row in range(4):
    for i in range(17):
        box('Basin floor plank',(.64+(i+.5)*3.72/17,-1.95,1.12+(row+.5)*3.55/4),
            (3.72/17-.006,.10,3.55/4-.008),dark if (i+row)%4==0 else wood,.008)
for x in (.51,4.49):
    for i in range(17):
        box('Deep basin stave',(x,-.86,1.09+i*.226),(.23,2.18,.218),wood,.018)
    for h in (-1.65,-.85,.12):
        box('Basin iron strap',(x+(-.125 if x<1 else .125),h,2.9),(.035,.065,3.91),metal,.006)
for z in (1.0,4.8):
    for i in range(18):
        box('Deep end stave',(.56+i*.228,-.86,z),(.22,2.18,.23),wood,.018)
    for h in (-1.65,-.85,.12):
        box('End iron strap',(2.5,h,z+(-.125 if z<2 else .125)),(4.13,.065,.035),metal,.006)
for x in (.52,4.48):
    for z in (1.02,4.79):cyl('Deep corner tenon',(x,-.86,z),.15,2.18,dark,8)
# Native terrain cutout hides complete tiles: a covered deck fills the outer strip.
for x in (.26,4.74):box('Deck substructure',(x,.03,3),(.50,.12,5.9),dark,.01)
box('Rear deck substructure',(2.5,.03,5.25),(4.94,.12,1.45),dark,.01)
for x in (.85,4.15):box('Front wing support',(x,.03,.55),(1.66,.12,.95),dark,.01)
box('Landing support',(2.5,.03,.75),(1.62,.12,.45),dark,.01)
for i in range(14):
    for x in (.26,4.74):box('Side deck plank',(x,.12,1.15+i*.245),(.49,.12,.225),wood,.012)
for i in range(20):
    x=.14+i*.247
    box('Rear deck plank',(x,.12,5.25),(.228,.12,1.45),wood,.012)
    if 1.7<x<3.3:
        box('Entry landing plank',(x,.12,.75),(.228,.12,.45),wood,.008)
    else:
        box('Front deck plank',(x,.12,.55),(.228,.12,.95),wood,.012)
for x in (.51,4.49):beam('Rounded coping log',(x,.24,.95),(x,.24,4.87),.09,dark)
beam('Rear coping log',(.4,.24,4.8),(4.6,.24,4.8),.09,dark)
for a,b in ((.4,1.72),(3.28,4.6)):
    beam('Front coping log',(a,.24,1),(b,.24,1),.09,dark)
surface=box('#PoolWater',(2.5,.15,2.9),(3.78,.025,3.60),water)
# Two equal risers lead to a deck at +0.18, with no plank or log crossing them.
box('Entry lower tread',(2.5,.045,.145),(1.46,.09,.27),wood,.008)
box('Entry upper tread',(2.5,.09,.41),(1.46,.18,.25),wood,.008)
for x in (1.66,3.34):
    beam('Step handrail',(x,.4,.08),(x,.58,.83),.045,dark)
    for z,h in ((.12,.42),(.83,.58)):beam('Handrail support',(x,.02,z),(x,h,z),.045,dark)
# A recessed ladder provides a clear route from the deck into the deep basin.
for x in (2.03,2.97):beam('Pool ladder rail',(x,.48,1.12),(x,-1.85,1.70),.045,dark)
for i in range(10):
    h=.13-i*.22;z=1.12+(.48-h)/2.33*.58
    box('Pool ladder tread',(2.5,h,z),(.96,.055,.15),wood,.008)

upper_start=set(col.objects)
# Attached bar, slatted front, shelves and stocked casks.
for i in range(12):box('Bar front stave',(.82+i*.302,.51,4.65),(.287,.56,.20),wood,.03)
for y in (.29,.76):beam('Bar frame',(.68,y,4.51),(4.34,y,4.51),.065,dark)
for i in range(3):box('Thick split-plank counter',(2.5,.89,4.39+i*.23),(3.97,.20,.225),wood,.035)
box('Back shelf',(2.5,1.06,5.55),(3.95,.12,.35),dark,.025)
for x in (.63,4.37):
    for z in (4.37,5.68):
        cyl('Canopy post',(x,1.54,z),.155,1.84,dark,8)
        for h in (1.85,1.91,1.97):cyl('Lashing',(x,h,z),.17,.035,wood,12)
    beam('Canopy brace',(x,1.82,4.37),(x,2.32,4.87),.09,wood)
for z in (4.33,5.72):beam('Roof cross beam',(.43,2.30,z),(4.57,2.30,z),.13,dark)
# Overlapping reed courses; rounded ragged eaves, rather than flat metal-like panels.
for side in (-1,1):
    for row in range(4):
        distance=.20+row*.245
        z=4.98+side*distance;h=2.89-distance*.50
        for i in range(14):
            x=.38+i*.32
            o=box('Overlapping reed bundle',(x,h+random.uniform(-.012,.012),z),(.34,.20,.40+random.uniform(-.025,.025)),thatch,.065)
            o.rotation_euler.x=side*.46
    for i in range(42):
        x=.25+i*.108;end=1.08+random.uniform(-.055,.055)
        beam('Reed fringe',(x,2.45,4.98+side*.93),(x,2.89-end*.5,4.98+side*end),.035,thatch)
beam('Bound ridge cap',(.25,2.90,4.98),(4.75,2.90,4.98),.09,thatch)

def barrel(x,z,r=.23,h=.58):
    cyl('Cask solid core',(x,.67+h/2,z),r-.018,h-.02,dark)
    for i in range(12):
        a=i*math.tau/12
        o=box('Cask stave',(x+math.cos(a)*(r-.04),.67+h/2,z+math.sin(a)*(r-.04)),(.10,h,.095),wood,.015)
        o.rotation_euler.z=-a
    for y in (.67+.10,.67+h-.10):cyl('Cask hoop',(x,y,z),r+.014,.06,metal)
    cyl('Cask lid',(x,.68+h,z),r*.87,.025,dark)
for x,z in ((.32,5.42),(4.65,5.45),(1.15,5.48),(3.75,5.48)):barrel(x,z)
# Four submerged stools with retained seating anchors.
for i,x in enumerate((1.15,2.05,2.95,3.85)):
    cyl('Submerged stool leg',(x,-.42,4.12),.10,2.0,dark,8)
    cyl('Submerged stool seat',(x,.63,4.12),.23,.10,wood)
    empty('#Slot#BarSeat'+str(i),(x,.67,4.12))
    cyl('Tankard',(x,1.08,4.51),.075,.19,dark,8)
    cyl('Tankard rim',(x,1.18,4.51),.078,.025,metal,8)
    beam('Tankard handle',(x+.065,1.00,4.51),(x+.065,1.10,4.51),.02,dark)
# Four swimming lanes and four bar seats: total eight occupants.
for i,z in enumerate((1.95,2.50,3.05,3.60)):
    lane=empty('#Slot#Swimming'+str(i),(0,0,0))
    empty('#MiscStart'+str(i),(1.05,.64,z),lane)
    empty('#MiscEnd'+str(i),(3.95,.64,z),lane)
empty('#Slot#Entrance',(2.5,.53,.08))

for x in (.72,4.28):
    beam('Lantern hook',(x,2.45,4.28),(x,2.06,4.28),.018,metal)
    box('Lantern glass',(x,1.96,4.28),(.16,.22,.16),white,.015)
    for y in (1.82,2.10):box('Lantern cap',(x,y,4.28),(.22,.05,.22),metal,.02)
    for dx in (-.085,.085):
        for dz in (-.085,.085):beam('Lantern cage',(x+dx,1.83,4.28+dz),(x+dx,2.08,4.28+dz),.012,metal)

# Shift top-level objects once. Lane endpoints inherit their parent translation.
for o in set(col.objects)-upper_start:
    if not o.parent:o.location.z-=.48
# Basin-only model for Timberborn's underground/slice display, with no slots/water.
underground=bpy.data.collections.new('TipsyTail.Underground')
bpy.context.scene.collection.children.link(underground)
for o in list(col.objects):
    if o.name.startswith(('Basin floor','Deep basin stave','Deep end stave','Deep corner tenon','Basin iron strap','End iron strap')):
        copy=o.copy();copy.data=o.data.copy();underground.objects.link(copy)
underground.hide_render=True

bpy.context.view_layer.update()
settings=ExportSettings(bpy.context,True,False,False)
Exporter.export_collection(col,str(ASSETS/'TipsyTail.Model.timbermesh'),settings)
Exporter.export_collection(underground,str(ASSETS/'TipsyTail.Underground.timbermesh'),settings)
# Unfinished appearance references the game's stock ConstructionBase5x5 in the
# blueprint, scaled to the 5x6 footprint. No custom construction mesh is exported.

# Presentation setup is excluded from game export.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.34,.32,.27,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
def area(name,p,power,size):
    bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size
    o.rotation_euler=(Vector((-2.5,-3,.7))-o.location).to_track_quat('-Z','Y').to_euler()
area('Warm key',(-5,2,9),1400,7);area('Soft fill',(3,-5,6),950,6)
bpy.ops.object.camera_add(location=(-8,11,9));cam=bpy.context.object
cam.rotation_euler=(Vector((-2.5,-3,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=9.2;scene.camera=cam
scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source'/'TipsyTail.blend'))
scene.render.filepath=str(ROOT/'TipsyTail-preview.png');bpy.ops.render.render(write_still=True)
surface.hide_render=True
cam.location=(-2.5,-3,14)
cam.rotation_euler=(0,0,math.pi);cam.data.ortho_scale=7.4
scene.render.filepath=str(ROOT/'TipsyTail-dry.png');bpy.ops.render.render(write_still=True)
surface.hide_render=False
cam.data.ortho_scale=9.2
cam.location=(-2.5,12,6.5)
cam.rotation_euler=(Vector((-2.5,-3,1))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(ROOT/'TipsyTail-front.png');bpy.ops.render.render(write_still=True)
cam.location=(-12,-3,6.5)
cam.rotation_euler=(Vector((-2.5,-3,1))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(ROOT/'TipsyTail-side.png');bpy.ops.render.render(write_still=True)
cam.location=(-8,11,9)
cam.rotation_euler=(Vector((-2.5,-3,1))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.film_transparent=True
scene.render.resolution_x=256;scene.render.resolution_y=256;cam.data.ortho_scale=8.6
scene.render.filepath=str(ASSETS/'TipsyTailIcon.png');bpy.ops.render.render(write_still=True)
print('TIPSY TAIL ASSETS COMPLETE')
