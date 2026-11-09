"""Combined HoneyPilotMap checks against an isolated GAMA instance."""
import json
from pathlib import Path
import shutil
import time
import uuid
import websocket
from validate_gama import Gama, ROOT, configuration


def main():
    folder = ROOT / 'Runtime' / ('water-map-check-' + uuid.uuid4().hex)
    folder.mkdir(parents=True)
    for name in ('LetItBeeHoneyPilot.gaml', 'LetItBeeWater.gaml'):
        shutil.copy2(ROOT / 'GAMA' / name, folder / name)
    checks = []
    sessions = []
    def check(ok, message):
        assert ok, message
        checks.append(message)
    def read(g, expr):
        return json.loads(g.command('expression', expr='to_json(' + expr + ')'))
    def viewer(progress):
        g = Gama.__new__(Gama)
        g.ws = websocket.create_connection('ws://127.0.0.1:6873', timeout=60)
        g.hello = json.loads(g.ws.recv())['content']
        g.sequence, g.experiment = 0, None
        g.experiment = g.command('load', model=str(folder/'LetItBeeHoneyPilot.gaml'), experiment='HoneyPilot', console=False,
            parameters=[dict(name='workspace_checkpoint', value=progress, type='string'),
                        dict(name='water_footprint_file', value=str(ROOT/'Water/gameplay-domain-survey.json'), type='string'),
                        dict(name='observation_map_file',value=str(ROOT/'Water/gameplay-observation-map.json'),type='string')])
        sessions.append(g)
        return g
    config = configuration()
    authority = Gama(config, model_path=folder/'LetItBeeHoneyPilot.gaml')
    sessions.append(authority)
    try:
        before = authority.snapshot()['state']
        missing = viewer(config['checkpoint'])
        check(not read(missing, 'water_ready') and 'NO DATA' in read(missing,'water_view_status'), 'missing session water is not reported as fresh zero salinity')
        check(read(missing, 'length(water_footprint)') > 0, 'actual Unity river footprint visible without inventing salinity')
        c = dict(schema='letitbee-water-grid-v1',domain_id='map-test',width=4,height=3,
                 cell_size_m=2.,origin_xz=[100.,200.],surface_y=.5,diffusivity_m2_s=.2,
                 depth_m=[1.]*12,salinity_g_l=[10.]+[0.]*11,faces=[])
        authority.ask('configure_water',water_json=json.dumps(c))
        authority.ask('advance_water',seconds=2.)
        authority.ask('read_water')
        path=Path(config['checkpoint']+'.water.json')
        saved_bytes=path.read_bytes()
        v=viewer(config['checkpoint'])
        check(read(v,'water_ready') and read(v,'water_readonly'), 'combined gameplay viewer automatically loads same-session water')
        check(read(v,'water_salt') == read(authority,'water_salt'), 'combined map reads authoritative salt masses')
        check(read(v,'water_shared_projection'), 'water uses gameplay projection')
        background=json.loads((ROOT/'Water/gameplay-observation-map.json').read_text())
        check(read(v,'background_ready'), 'actual Unity environment export loaded')
        check(read(v,'length(observation_landmark)')==len(background['landmarks']), 'all exported tree and house landmarks present')
        check(read(v,'length(observation_tile)')==len(background['tiles']), 'all sampled shoreline and ground strips present')
        check(not read(v,'water_draw_cells'), 'coarse water squares do not cover detailed shoreline')
        marker=read(v,'observation_landmark[0].world_position')
        check(abs(marker['x']-background['landmarks'][0]['position'][0])<1e-4 and abs(marker['y']-background['landmarks'][0]['position'][1])<1e-4, 'landmark preserves Unity X,Z position')
        for idx in (0,3,8,11):
            world=[100.+(idx%4+.5)*2,200.+(idx//4+.5)*2,0]
            a=read(v,f'(water_cell where (each.cell_id = {idx}))[0].location')
            left,top,zoom=read(v,'[map_left,map_top,map_zoom]')
            b=dict(x=25+(world[0]-left)*zoom,y=30+(top-world[1])*zoom,z=0)
            check(all(abs(a[k]-b[k])<1e-5 for k in b), f'cell {idx} aligns with gameplay projection within JSON rounding')
        check(read(v,'length(pilot_flower)')==2 and read(v,'length(pilot_slot)')==1, 'water overlay preserves flowers and hive')
        v.ask('set_water_running',running=True)
        v.ask('read_water')
        check(not read(v,'water_running') and path.read_bytes()==saved_bytes, 'read-only combined map cannot advance or overwrite checkpoint')
        saved=json.loads(saved_bytes)
        saved['saved_at_ms']=0
        path.write_text(json.dumps(saved))
        v.ask('read_workspace')
        check(not read(v,'water_view_fresh') and 'STALE' in read(v,'water_view_status'), 'old snapshot marked stale without pretending live salinity')
        old=read(v,'water_salt')
        saved['salt_kg'][0]=-1
        path.write_text(json.dumps(saved))
        v.ask('read_workspace')
        check('INVALID' in read(v,'water_view_status') and read(v,'water_salt')==old, 'invalid salt rejected while last complete field retained')
        path.write_text('{incomplete')
        v.ask('read_workspace')
        check('retrying' in read(v,'water_view_status') and read(v,'water_salt')==old, 'partial write safely retains previous field')
        path.unlink()
        v.ask('read_workspace')
        check(not read(v,'water_view_fresh') and 'NO DATA' in read(v,'water_view_status'), 'missing water checkpoint never reuses another session file')
        entries=[dict(id='slot-'+str(i),kind='hive',label='Hive cell',position=[100.+i*.1,210.,30.]) for i in range(7)]
        observations=dict(session=config['session'],source='map-fixture',sequence=1,received_at_ms=time.time()*1000,entities=entries)
        Path(config['checkpoint']+'.observations.json').write_text(json.dumps(observations))
        v.ask('read_workspace');v.ask('frame_map')
        center=read(v,'map_hive_center')
        check(abs(center['x']-100.3)<1e-5 and abs(center['y']-210.)<1e-5,'grouped hive marker uses centroid of actual slots')
        check(read(v,'length(synced_entity where (each.entity_kind = "hive"))')==7 and read(v,'length(pilot_slot)')==1,'all seven slot identities retained behind one hive marker')
        check(authority.snapshot()['state']==before, 'map operations preserve honey and pollination progress')
    finally:
        for g in reversed(sessions):
            try: g.command('stop')
            finally: g.close()
    result=dict(passed=True,count=len(checks),checks=checks)
    (ROOT/'Runtime/water-map-validation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))

if __name__ == '__main__': main()
