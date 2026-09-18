"""Check the reported stair/seat/depth defects directly in the editable model.
Run in Blender: blender -b --python validate_geometry.py
"""
import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/TipsyTail.blend'))
objects=list(bpy.data.collections['TipsyTail.Model'].objects)
def named(prefix):return [o for o in objects if o.name.startswith(prefix)]
def heights(o):
    vs=[(o.matrix_world@Vector(c)).z for c in o.bound_box]
    return min(vs),max(vs)
floor=named('Basin floor plank')
assert len(floor)==68
assert all(abs(heights(o)[0]+2)<1e-5 for o in floor)
assert not named('Carved beaver tail') and not named('Sign hanger') and not named('Tail carving')
counter=max(heights(o)[1] for o in named('Thick split-plank counter'))
seats=[o for o in named('#Slot#BarSeat') if int(o.name[-1])<4]
assert len(seats)==4
assert all(abs(counter-o.matrix_world.translation.z-.32)<1e-5 for o in seats)
stairs=[max(heights(o)) for o in named('Entry lower tread')+named('Entry upper tread')]
deck=max(heights(o)[1] for o in named('Entry landing plank'))
assert all(abs(a-b)<1e-5 for a,b in zip(stairs,[.09,.18]))
assert abs(deck-.18)<1e-5
# Clear the stair footprint: no front planks or foundations can pass through it.
for o in named('Front deck plank')+named('Front wing support'):
    x=-o.location.x;half=o.dimensions.x/2
    assert x+half<1.77 or x-half>3.23,o.name
assert not named('Foundation log')
assert len(named('Pool ladder tread'))==10
for leg in named('Submerged stool leg'):
    low,high=heights(leg)
    assert abs(low+1.9)<1e-5 and abs(high-.1)<1e-5
report={'status':'PASS','basin_floor_bottom':-2,'floor_planks':len(floor),'counter_top':round(counter,3),
        'bar_seat_anchor':.19,'counter_above_seated_anchor':.32,'entrance_tread_tops':stairs,
        'deck_top':deck,'ladder_treads':10,'hanging_ornament_removed':True,
        'test_type':'Offline geometric checks; game animations and shaders need user playtest'}
(ROOT/'geometry-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
