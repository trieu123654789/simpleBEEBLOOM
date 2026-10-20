"""Private sessions only: 32 existing GAMA checks plus honey interaction checks.
Defaults to SIMPLE; --direct uses the local headless GAMA server on 6875.
Requires no other connected player. Never modifies the user checkpoint.
"""
import sys,pathlib,json
ROOT = pathlib.Path(__file__).resolve().parent
import validate_gama as base
Original=base.Gama
if '--direct' not in sys.argv:
    from validate_simple import SimpleGama as IsolatedGama
else:
    class IsolatedGama(Original):
        def __init__(self,config):super().__init__(config,port=6875)
        def close(self):
            try:self.command('stop')
            finally:super().close()
base.Gama=IsolatedGama
base.main()
checks=[]
def check(value,label):
    if not value:raise AssertionError(label)
    checks.append(label)
g=IsolatedGama(base.configuration())
try:
    a=[0,0,1.5];slot=[0,4,5.2]
    g.transact('visit',a,flower=0);g.transact('collect',a,flower=0);g.transact('pick',a)
    _,s=g.transact('deposit',slot,hand=[0,4,5.62],held_chunk=[0,4,5.5])
    check(s['response']['accepted'] and s['state']['stored']==1,'honey inside socket accepts release when palm is outside')
finally:g.close()
g=IsolatedGama(base.configuration())
try:
    g.transact('visit',[0,0,1.5],flower=0);g.transact('collect',[0,0,1.5],flower=0);g.transact('pick',[0,0,1.5])
    _,s=g.transact('deposit',[0,4,5.2],hand=[0,4,5.2],held_chunk=[0,4,5.6])
    check(not s['response']['accepted'] and s['state']['chunk_state']=='held','fake chunk beyond hand reach is rejected')
    _,s=g.transact('deposit',[0,4,5.2],hand=[0,4,5.5],held_chunk=[0,4,5.6])
    check(not s['response']['accepted'] and s['state']['stored']==0,'honey outside socket cannot deposit with palm alone')
    req,s=g.transact('drop',[0,4,5.2],hand=[0,4,5.5],held_chunk=[0,4,5.6])
    check(s['response']['accepted'] and s['state']['chunk_pos']==[0,4,5.6],'drop uses released honey centre')
    check(g.repeat(req)['state']['chunk_pos']==[0,4,5.6],'retry does not move released honey again')
finally:g.close()
pconfig=base.configuration();g=IsolatedGama(pconfig)
try:
    a=[0,0,1.5];nectar=[0,0,1.68]
    g.ask('set_pose',pose_json=json.dumps(dict(bee=a,hand=a,nectar_positions=[nectar,[2,0,1.68]])))
    g.transact('visit',a,flower=0);_,s=g.transact('collect',a,flower=0)
    check(s['state']['chunk_pos']==nectar,'confirmed nectar spawns at the visible flower surface')
    moved=[0,0,1.7]
    g.ask('set_pose',pose_json=json.dumps(dict(bee=a,hand=a,nectar_positions=[moved,[2,0,1.7]])))
    check(g.snapshot()['state']['chunk_pos']==moved,'ready nectar follows animated visible position')
    _,s=g.transact('pick',[0,0,2.2],hand=[0,0,2.88])
    check(s['response']['accepted'],'grab radius is measured against visible nectar rather than hidden head bone')
finally:g.close()
pconfig=base.configuration();g=IsolatedGama(pconfig)
try:
    surface=[0,0,1.6];side=[0,1.3,1.6]
    g.ask('set_pose',pose_json=json.dumps(dict(bee=side,hand=side,flower_surfaces=[surface,[2,0,1.6]],flower_radii=[0.6,0.6])))
    _,s=g.transact('visit',[0,2,1.6],flower=0)
    check(not s['response']['accepted'],'blossom volume still rejects distant visits')
    _,s=g.transact('visit',side,flower=0)
    check(s['response']['accepted'],'safe branch standoff accepts flower contact beyond hidden bone radius')
    _,s=g.transact('collect',side,flower=0)
    check(s['response']['accepted'],'nectar collection uses the same blossom volume as the Unity perch')
    g.ask('set_pose',pose_json=json.dumps(dict(bee=side,hand=side,flower_surfaces=[surface,[2,0,1.6]],flower_radii=[100,100])))
    _,s=g.transact('visit',[0,3,1.6],flower=0)
    check(not s['response']['accepted'],'unbounded reported flower radius cannot grant remote interaction')
finally:g.close()
p=ROOT / "Runtime" / "interaction-validation.json"
p.parent.mkdir(parents=True,exist_ok=True)
p.write_text(json.dumps(dict(passed=True,count=len(checks),legacyChecks=32,transport="direct" if "--direct" in sys.argv else "SIMPLE",checks=checks),indent=2))
print(p.read_text())
