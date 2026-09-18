"""Regression checks for the material-registry load crash in 0.1.1.
Uses paths resolved from the installed game's ResourceManager, not just all
Material objects found somewhere in resources.assets. No game is launched.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def validate_materials(used_materials,base):
    base=Path(base)
    resolved=json.loads((ROOT/'material-registry.json').read_text())
    report={}
    def read(p):return json.loads(p.read_text())
    common=read(base/'MaterialCollections/MaterialCollection.Common.blueprint.json')['MaterialCollectionSpec']['Materials']
    for faction in ('Folktails','IronTeeth'):
        file=f'MaterialCollections/MaterialCollection.{faction}.blueprint.json'
        native=read(base/file)['MaterialCollectionSpec']['Materials']
        patch=read(ROOT/'Mod'/file)['MaterialCollectionSpec']
        assert patch['CollectionId']==faction
        assert set(patch)=={'CollectionId','Materials#append'},'Must append without replacing native materials'
        paths=list(dict.fromkeys(common+native+patch['Materials#append']))
        assert all(p in resolved for p in paths),'A registered path is absent from the game resource manager'
        names=[resolved[p] for p in paths]
        assert len(names)==len(set(names)),'Duplicate material name would fail MaterialRepository.Load'
        missing=set(used_materials)-set(names)
        assert not missing,f'{faction}: material registry is missing {missing}'
        baseline=set(used_materials)-{resolved[p] for p in common+native}
        expected=set() if faction=='Folktails' else {m for m in used_materials if m.endswith('.Folktails') or m=='FountainWater'}
        assert baseline==expected,'Unexpected faction-specific material dependencies'
        report[faction]={'missing_before_fix':sorted(baseline),'missing_after_fix':sorted(missing),'added_paths':patch['Materials#append'],'all_paths_resolved':True,'duplicate_names':False}
    (ROOT/'material-regression.json').write_text(json.dumps({'status':'PASS','tested_game_version':'1.1.2.4','factions':report},indent=2))
    return report

if __name__=='__main__':
    import sys
    used=json.loads((ROOT/'validation.json').read_text())['materials']
    print(json.dumps(validate_materials(used,sys.argv[1]),indent=2))
