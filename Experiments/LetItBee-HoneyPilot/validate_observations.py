"""Real GAMA observation/flower regression checks. Uses isolated test checkpoints."""
import copy
import json
import pathlib
import time
import websocket
import uuid
from validate_gama import Gama, configuration, ROOT, model_snapshot


def read(model, expr):
    return json.loads(model.command('expression', expr='to_json(' + expr + ')'))


def main():
    c = configuration()
    c['world_source'] = 'observation-regression'
    c['flowers'] = [dict(id=f'flower-{i}', plant_id=f'plant-{i}', species='chrysanthemum-pilot',
                         position=[float(i*3), 0., 1.5]) for i in range(9)]
    c['hive_slots'] = [c['slot']] + [dict(id=f'slot-{i}',position=[float(i),4.,5.2]) for i in range(1,7)]
    # GAMA servers cache compiled models by path. A unique snapshot tests this revision.
    model_path = model_snapshot()
    model = Gama(c, model_path=model_path)
    viewer = None
    checks = []
    def check(condition, name):
        assert condition, name
        checks.append(name)
    def publish(seq, position, forward=None, perched='', client_time=100.):
        player = dict(id='player-bee',kind='bee',label='Player',position=position,perched_flower_id=perched)
        if forward is not None:
            player['forward'] = forward
        entities = [player] + [dict(id=f['id'],kind='flower',label=f['id'],position=f['position']) for f in c['flowers']]
        entities += [dict(id=h['id'],kind='hive',label=h['id'],position=h['position']) for h in c['hive_slots']]
        payload = dict(schema='beebloom-world-v1',source=c['world_source'],sequence=seq,
                       entities=entities,observed_time=client_time)
        model.ask('sync_world',world_json=json.dumps(payload))
        check(read(model,'world_response')["accepted"], f'snapshot {seq} accepted')
        return payload
    try:
        model.ask('frame_map')
        check(sorted(read(model,'pilot_flower collect each.flower_index')) == list(range(9)), 'nine flower indices')
        before = model.snapshot()['state']
        payload = publish(1,[0,0,1.5],[0,1,0],'flower-0')
        check(model.snapshot()['state'] == before,'observation alone does not pollinate or consume nectar')
        obs = pathlib.Path(c['checkpoint']+'.observations.json')
        first = json.loads(obs.read_text())
        check('received_at_ms' in first,'server receipt time exported')
        time.sleep(.3)
        publish(2,[5,8,3],[1,0,0],client_time=-100000.)
        check(json.loads(obs.read_text())['sequence'] == 2,'clock rollback cannot freeze export')
        check(read(model,'perched_flower_id') == '', 'leaving flower clears perch')
        check(read(model,'list(first(synced_entity where (each.entity_id = "player-bee")).entity_position)') == [5.0,8.0,3.0], 'exact bee position retained')
        # A separate GUI-style experiment reads the actual sidecar, not a shared agent.
        viewer = Gama.__new__(Gama)
        viewer.ws = websocket.create_connection('ws://127.0.0.1:6873', timeout=45)
        viewer.hello = json.loads(viewer.ws.recv())['content']
        viewer.sequence, viewer.experiment = 0, None
        viewer.experiment = viewer.command('load',model=str(model_path),experiment='HoneyPilot',console=False,
            parameters=[dict(name='workspace_checkpoint',value=c['checkpoint'],type='string')])
        viewer.ask('read_workspace'); viewer.ask('frame_map')
        check(read(viewer,'world_sequence') == 2, 'viewer reads real sidecar')
        time.sleep(.3)
        publish(3,[9,12,4],[0,-1,0],'flower-1')
        viewer.ask('read_workspace'); viewer.ask('frame_map')
        check(read(viewer,'world_sequence') == 3, 'viewer rereads changed sidecar without restart')
        check(read(viewer,'perched_flower_id') == 'flower-1', 'viewer receives landed flower identity')
        check(read(viewer,'list(first(synced_entity where (each.entity_id = "player-bee")).entity_forward)') == [0.,-1.,0.], 'viewer receives camera direction')
        time.sleep(3.2)
        viewer.ask('read_workspace')
        check(not read(viewer,'gama.machine_time - observation_received_ms < 3000'), 'stale heartbeat is not live')
        check('STALE' in read(viewer,'view_status'), 'stale status shown')
        _, result = model.transact('visit',[0,0,1.5],flower=0)
        check(result['response']['accepted'] and not any(result['state']['pollinated']), 'first landing only loads pollen')
        _, result = model.transact('visit',[3,0,1.5],flower=1)
        check(result['state']['pollinated'][1] and result['state']['nectar'][1] == 1, 'cross pollination leaves nectar available')
        _, result = model.transact('collect',[3,0,1.5],flower=1)
        check(result['response']['accepted'] and result['state']['nectar'][1] == 0 and result['state']['pollinated'][1], 'collection changes nectar independently')
        state = copy.deepcopy(result['state'])
        c['world_source'] = 'reconnected-source'
        model.ask('configure',config_json=json.dumps(c))
        check(model.snapshot()['state'] == state, 'reconnect preserves gameplay progress')
        publish(1,[11,15,4])
        check(not read(model,'first(synced_entity where (each.entity_id = "player-bee")).has_direction'), 'legacy payload does not invent heading')
        viewer.ask('read_workspace'); viewer.ask('frame_map')
        check(read(viewer,'world_source') == 'reconnected-source' and read(viewer,'world_sequence') == 1,'viewer accepts new stream with reset sequence')
        report = dict(passed=True,count=len(checks),checks=checks)
        (ROOT/'Runtime/observation-validation.json').write_text(json.dumps(report,indent=2))
        print(json.dumps(report,indent=2))
    finally:
        if viewer and viewer.experiment:
            viewer.command('stop'); viewer.close()
        model.command('stop'); model.close()

if __name__ == '__main__':
    main()
