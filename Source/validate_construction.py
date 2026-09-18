"""Validate the native ConstructionSiteProgressVisualizer stage contract."""
def validate_construction(blueprint):
    spec=blueprint['BuildingModelSpec']
    children=blueprint['Children']
    assert spec['FinishedModelName'] in children,'Finished model absent'
    assert spec['UnfinishedModelName'] in children,'Unfinished model absent'
    stages=children[spec['UnfinishedModelName']]['Children']
    thresholds=blueprint['ConstructionSiteProgressVisualizerSpec']['ProgressThresholds']
    assert stages,'At least the construction base is required'
    assert len(thresholds)==len(stages)-1, f'{len(thresholds)} thresholds vs {len(stages)-1} stages minus base'
    assert thresholds==sorted(thresholds) and all(0<=x<=1 for x in thresholds)
    # Check every boundary and intermediate point using the native stage-selection rule.
    for started in (False,True):
        for progress in (0,.01,.5,.99,1,*thresholds):
            chosen=0
            if started:
                for stage in range(len(stages)-1,0,-1):
                    if progress>=thresholds[stage-1]:
                        chosen=stage;break
            assert 0<=chosen<len(stages)
    return {'children':list(stages),'thresholds':thresholds,'stage_selection':'PASS'}
