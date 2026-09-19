"""Offline checks that the Tipsy Tail satisfies the native WetFur need.
Reads the installed game's Blueprints.zip and the generated mod blueprint, so the need
definition, availability in both factions and the native points-per-hour rate come from
the real files. No game is launched and nothing here proves in-game behaviour.
"""
import json,os,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
GAME=Path(os.environ.get('TIMBERBORN_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))
MOD=ROOT/'Mod'
NEED='WetFur'

def validate_wet_fur():
    z=zipfile.ZipFile(GAME/'Timberborn_Data/StreamingAssets/Modding/Blueprints.zip')
    game=lambda n:json.loads(z.read(n).decode('utf-8-sig'))
    need=game(f'Needs/Need.Beaver.{NEED}.blueprint.json')['NeedSpec']
    assert need['Id']==NEED and need['CharacterType']=='Beaver','WetFur must be a native beaver need'
    common=game('NeedCollections/NeedCollection.Common.blueprint.json')['NeedCollectionSpec']
    assert NEED in common['Needs'],'WetFur must be in the Common collection so both factions load it'

    # Every native building that grants WetFur, with its rate.
    native={}
    for n in z.namelist():
        if n.startswith('Buildings/') and n.endswith('.blueprint.json'):
            for e in game(n).get('AttractionSpec',{}).get('Effects',[]):
                if e['NeedId']==NEED:native[n.rsplit('/',1)[1].replace('.blueprint.json','')]=e['PointsPerHour']
    swim={k:native[k] for k in native if k.startswith(('Lido.','SwimmingPool.'))}
    assert len(swim)==2 and len(set(swim.values()))==1,'Native swimming venues should share one WetFur rate'
    rate=next(iter(swim.values()))

    b=json.loads((MOD/'Buildings/Wellbeing/TipsyTail/TipsyTail.blueprint.json').read_text(encoding='utf-8'))
    effects=b['AttractionSpec']['Effects']
    ids=[e['NeedId'] for e in effects]
    assert len(ids)==len(set(ids)),'Duplicate need effects'
    assert ids[0]=='TipsyTail' and effects[0]['PointsPerHour']==0.6,'Own recreation effect must be retained first'
    wet=[e for e in effects if e['NeedId']==NEED]
    assert len(wet)==1,'Expected exactly one WetFur effect'
    assert wet[0]['PointsPerHour']==rate,'WetFur rate must match the native swimming venues'
    assert wet[0]['SatisfyToMaxValue'] is False,'Native venues do not satisfy to max'
    # Every referenced need must resolve for each faction: Common plus that faction's collection.
    for faction in ('Folktails','IronTeeth'):
        mod=json.loads((MOD/f'NeedCollections/NeedCollection.{faction}.blueprint.json').read_text(encoding='utf-8'))['NeedCollectionSpec']
        available=set(common['Needs'])|set(mod['Needs#append'])
        assert set(ids)<=available,f'{faction}: unresolved need in {set(ids)-available}'
    assert 'wet fur' in (MOD/'Localizations/enUS.csv').read_text(encoding='utf-8').lower(),'Description should mention wet fur'
    assert b['EnterableSpec']['CapacityFinished']==8

    report={
      'status':'PASS',
      'test_type':'Offline comparison against the installed game blueprints and the generated mod blueprint; not an in-game test',
      'need':{'id':NEED,'group':need['NeedGroupId'],'daily_delta':need['DailyDelta'],'favorable_wellbeing':need['FavorableWellbeing'],'in_common_collection':True},
      'tipsy_tail_effects':effects,
      'native_wet_fur_sources_points_per_hour':dict(sorted(native.items())),
      'matched_native_rate':rate,
      'available_to_factions':['Folktails','IronTeeth'],
      'runtime_code_changed':False,
      'note':'Data-only change: the effect is applied by the native AttractionSpec, so no new runtime code or need collection is involved.'}
    (ROOT/'wet-fur-validation.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__=='__main__':
    print(json.dumps(validate_wet_fur(),indent=2))
