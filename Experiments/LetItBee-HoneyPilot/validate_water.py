"""Conservation, boundaries and protocol checks against actual isolated GAMA (6873)."""
import copy
import json
from pathlib import Path
import shutil
import uuid
import websocket
from validate_gama import Gama, ROOT, configuration


def main():
    # Unique import paths avoid the server's compiled-model cache.
    folder = ROOT / 'Runtime' / ('water-check-' + uuid.uuid4().hex)
    folder.mkdir(parents=True)
    for name in ('LetItBeeWater.gaml', 'LetItBeeWaterReview.gaml', 'LetItBeeHoneyPilot.gaml', 'LetItBeeSimpleVR.gaml'):
        shutil.copy2(ROOT / 'GAMA' / name, folder / name)
    checks = []

    def check(condition, label):
        assert condition, label
        checks.append(label)

    def open_model(experiment='WaterReview', name='LetItBeeWaterReview.gaml', port=6873, live_file=''):
        g = Gama.__new__(Gama)
        g.ws = websocket.create_connection(f'ws://127.0.0.1:{port}', timeout=60)
        g.hello = json.loads(g.ws.recv())['content']
        g.sequence, g.experiment = 0, None
        g.experiment = g.command('load', model=str(folder/name), experiment=experiment, console=False,
            parameters=[dict(name='water_config_file', value='', type='string'),
                        dict(name='live_water_file',value=live_file,type='string')] if experiment == 'WaterReview' else [])
        return g

    def read(g, expression):
        return json.loads(g.command('expression', expr='to_json(' + expression + ')'))

    def state(g):
        g.ask('read_water')
        return read(g, 'water_state')

    def configure(g, c):
        g.ask('configure_water', water_json=json.dumps(c))
        return read(g, 'water_response')

    def domain(w=5, h=3):
        return dict(schema='letitbee-water-grid-v1', domain_id='test-domain', width=w, height=h,
                    cell_size_m=2., origin_xz=[100., 200.], surface_y=.5,
                    diffusivity_m2_s=.2, depth_m=[1.]*(w*h), salinity_g_l=[0.]*(w*h), faces=[])

    g = open_model()
    try:
        c = domain(); c['salinity_g_l'][6] = 20.
        check(configure(g, c)['accepted'], 'valid closed basin accepted')
        before = state(g)
        for _ in range(12): g.ask('advance_water', seconds=10.)
        after = state(g)
        check(abs(after['mass_error_kg']) < 1e-8, 'closed basin conserves salt')
        check(min(after['salinity_g_l']) >= 0 and max(after['salinity_g_l']) <= 20, 'diffusion remains bounded and nonnegative')
        check(after['salinity_g_l'][6] < 20 and after['salinity_g_l'][7] > 0, 'salt mixes into adjacent wet cells')
        configure(g, c)
        check(state(g) == after, 'configuration retry preserves running state')
        changed = copy.deepcopy(c); changed['cell_size_m'] = 3.
        check(not configure(g, changed)['accepted'] and state(g) == after, 'changed domain cannot silently reset accepted water')
    finally:
        g.command('stop'); g.close()

    g = open_model()
    try:
        c = domain(); c['depth_m'] = [0. if i%5 == 2 else 1. for i in range(15)]
        c['salinity_g_l'][5] = 20.
        check(configure(g, c)['accepted'], 'land barrier accepted')
        for _ in range(10): g.ask('advance_water', seconds=10.)
        s = state(g)
        check(all(s['salinity_g_l'][i] == 0 for i in range(15) if i%5 >= 2), 'salt never crosses dry barrier or wraps grid rows')
        check(abs(s['mass_error_kg']) < 1e-8, 'barrier does not absorb or generate salt')
    finally:
        g.command('stop'); g.close()

    for reverse in (False, True):
        g = open_model()
        try:
            c = domain(8, 1); c['diffusivity_m2_s'] = 0.
            cells = list(range(8)); cells = cells[::-1] if reverse else cells
            c['faces'] = [dict(**{'from':a,'to':b}, discharge_m3_s=.8, boundary_salinity_g_l=15.)
                          for a,b in zip([-1]+cells,cells+[-1])]
            check(configure(g, c)['accepted'], 'balanced reverse flow accepted' if reverse else 'balanced channel flow accepted')
            for _ in range(4): g.ask('advance_water', seconds=5.)
            s = state(g); values = s['salinity_g_l']
            check(values[cells[0]] > values[cells[-1]] > 0, 'front follows prescribed reverse flow' if reverse else 'front follows prescribed forward flow')
            check(abs(s['mass_error_kg']) < 1e-8, 'boundary exchange accounted for exactly')
            check(min(values) >= 0 and max(values) <= 15, 'advection stays within source concentration')
        finally:
            g.command('stop'); g.close()

    g = open_model()
    try:
        c = domain(); c['faces'] = [dict(**{'from':0,'to':1},discharge_m3_s=1.,boundary_salinity_g_l=0.)]
        check(not configure(g, c)['accepted'], 'unbalanced prescribed discharge rejected')
        c = domain(); c['depth_m'][0] = 0.; c['salinity_g_l'][0] = 3.
        check(not configure(g, c)['accepted'], 'salt in dry cell rejected')
        c = domain(); c['salinity_g_l'] = [0.]
        check(not configure(g, c)['accepted'], 'malformed dimensions rejected without partial mutation')
        check(not state(g)['ready'], 'invalid configuration leaves model unconfigured')
    finally:
        g.command('stop'); g.close()

    # Actual authored geometry, with explicitly hypothetical test flow.
    g = open_model()
    try:
        c = json.loads((ROOT/'Water/gameplay-flow-demo.json').read_text())
        check(configure(g,c)['accepted'], 'sampled Unity river accepts balanced review flow')
        g.ask('advance_water', seconds=10.)
        s=state(g)
        check(abs(s['mass_error_kg']) < 1e-7, 'sampled river conserves boundary salt exchange')
        check(all(value == 0 for value,depth in zip(s['salinity_g_l'],c['depth_m']) if depth == 0), 'sampled river keeps all blocked land cells dry')
    finally:
        g.command('stop'); g.close()

    # Imported module must leave the honey state untouched.
    g = open_model('HoneyPilot', 'LetItBeeHoneyPilot.gaml')
    restored = None
    observer = None
    try:
        config = configuration(); g.ask('configure',config_json=json.dumps(config))
        before = read(g, 'state')
        water_config=domain();water_config['salinity_g_l'][0]=10.
        check(configure(g, water_config)['accepted'], 'water config accepted in gameplay model')
        g.ask('advance_water',seconds=2.)
        check(read(g, 'state') == before, 'water operations preserve nectar, pollination and hive checkpoint')
        saved = state(g)
        restored = open_model('HoneyPilot', 'LetItBeeHoneyPilot.gaml')
        restored.ask('configure',config_json=json.dumps(config))
        check(configure(restored,water_config)['accepted'], 'new experiment accepts matching saved water domain')
        recovered = state(restored)
        check(recovered['salinity_g_l'] == saved['salinity_g_l'] and recovered['time_seconds'] == saved['time_seconds'],
              'environmental checkpoint survives a fresh experiment without resetting honey')
        observer=open_model(live_file=config['checkpoint']+'.water.json')
        check(read(observer,'water_readonly') is True and read(observer,'water_salt') == read(restored,'water_salt'),
              'read-only viewer receives authoritative salt field')
        restored.ask('advance_water',seconds=3.);state(restored);observer.ask('observe_water')
        check(read(observer,'water_time_seconds') == 5., 'live viewer follows new saved revision')
        observer.ask('set_water_running',running=True)
        check(read(observer,'water_running') is False, 'live viewer refuses to run an independent simulation')
    finally:
        if observer:
            observer.command('stop');observer.close()
        if restored:
            restored.command('stop'); restored.close()
        g.command('stop'); g.close()

    # Compile actual LinkToUnity experiment without registering a live player.
    g = open_model('LetItBeeVR', 'LetItBeeSimpleVR.gaml', port=6875)
    try:
        check(read(g, 'water_ready') is False, 'SIMPLE model compiles with water disabled by default')
    finally:
        g.command('stop'); g.close()

    result = dict(passed=True, count=len(checks), checks=checks)
    (ROOT/'Runtime/water-validation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))


if __name__ == '__main__': main()
