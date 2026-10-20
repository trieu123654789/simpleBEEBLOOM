"""Integration tests against actual GAMA 2025.6.4, never a mock simulation."""
import copy
import json
import pathlib
import sys
import uuid
import websocket

ROOT = pathlib.Path(__file__).resolve().parent
SNAPSHOT = 'to_json(["state"::state,"response"::response,"cycle"::cycle])'

class Gama:
    def __init__(self, config, port=6873):
        self.config = config
        self.ws = websocket.create_connection(f'ws://127.0.0.1:{port}', timeout=45)
        self.hello = json.loads(self.ws.recv())['content']
        self.sequence, self.experiment = 0, None
        self.experiment = self.command('load', model=str(ROOT/'GAMA/LetItBeeHoneyPilot.gaml'), experiment='HoneyPilot', console=False)
        self.ask('configure', config_json=json.dumps(config))
    def command(self, kind, **fields):
        self.sequence += 1
        request = dict(type=kind, request_id=self.sequence, **fields)
        if self.experiment: request.update(exp_id=self.experiment, socket_id=self.hello)
        self.ws.send(json.dumps(request))
        while True:
            response = json.loads(self.ws.recv())
            if response.get('command', {}).get('request_id') == self.sequence:
                if response['type'] != 'CommandExecutedSuccessfully':
                    content=response.get('content')
                    if isinstance(content,dict) and 'errors' in content:
                        content=[e for e in content['errors'] if e.get('type')=='Error']
                    raise RuntimeError(json.dumps(content))
                return response.get('content')
    def ask(self, action, **args):
        return self.command('ask', action=action, agent='simulation[0]', args=json.dumps(args))
    def snapshot(self): return json.loads(self.command('expression', expr=SNAPSHOT))
    def transact(self, kind, bee, hand=None, flower=-1, **extra):
        request = dict(event_id=uuid.uuid4().hex, session=self.config['session'], kind=kind,
            bee=bee, hand=hand or bee, flower=flower, chunk_id=self.snapshot()['state']['chunk_id'], slot_id='slot-a', **extra)
        self.ask('transact', request_json=json.dumps(request))
        return request, self.snapshot()
    def repeat(self, request):
        self.ask('transact', request_json=json.dumps(request)); return self.snapshot()
    def close(self): self.ws.close()

def configuration():
    key = uuid.uuid4().hex
    return dict(session=key, checkpoint=str(ROOT/'Runtime'/f'test-{key}.json'),
        flowers=[dict(id='flower-a',plant_id='plant-a',species='chrysanthemum-pilot',position=[0.,0.,1.5]),
                 dict(id='flower-b',plant_id='plant-b',species='chrysanthemum-pilot',position=[2.,0.,1.5])],
        slot=dict(id='slot-a',position=[0.,4.,5.2]),contact_radius=1.0,grab_radius=1.2,snap_radius=.35)

def main():
    checks=[]
    def check(ok, label):
        if not ok: raise AssertionError(label)
        checks.append(label)
    config=configuration(); g=Gama(config)
    a,b,h=[0.,0.,1.5],[2.,0.,1.5],[0.,4.,5.2]
    try:
        check(g.snapshot()['response']['accepted'], 'configuration accepted')
        initial=g.snapshot()['state']; check(initial['nectar']==[1,1], 'two nectar resources')
        _,s=g.transact('visit',[20,20,20],flower=0);check(not s['response']['accepted'], 'reject remote pollination')
        req,s=g.transact('collect',a,flower=0)
        check(s['response']['accepted'] and s['state']['nectar']==[0,1], 'collect consumes exactly one resource')
        check(s['state']['pollinated']==[False,False], 'collection alone is not pollination')
        repeated=g.repeat(req);check(repeated['state']==s['state'], 'duplicate collect is idempotent')
        collision=copy.deepcopy(req);collision['kind']='deposit';s=g.repeat(collision)
        check(not s['response']['accepted'] and s['response']['reason']=='event_id_conflict', 'reject event ID reused for another action')
        _,s=g.transact('visit',a,flower=0);check(s['state']['pollen_source']==0, 'flower A supplies carried pollen')
        visits=s['state']['visits'];_,s=g.transact('visit',a,flower=0)
        check(s['state']['visits']==visits and not s['state']['pollinated'][0], 'standing on source cannot pollinate itself or inflate visits')
        _,s=g.transact('pick',[20,20,20]);check(not s['response']['accepted'], 'reject remote pickup')
        _,s=g.transact('pick',a);check(s['state']['chunk_state']=='held', 'pick actual registered chunk')
        _,s=g.transact('collect',b,flower=1);check(not s['response']['accepted'] and s['state']['nectar'][1]==1, 'cannot create second chunk while carrying')
        _,s=g.transact('visit',b,flower=1);check(s['state']['pollinated'][1], 'cross pollination while carrying nectar')
        _,s=g.transact('deposit',a);check(not s['response']['accepted'] and s['state']['stored']==0, 'reject distant slot deposit')
        req,s=g.transact('deposit',h);check(s['state']['stored']==1 and s['state']['chunk_state']=='stored','correct slot accepts deliberate deposit')
        check(g.repeat(req)['state']['stored']==1, 'duplicate deposit cannot double count')
        _,s=g.transact('deposit',h);check(not s['response']['accepted'] and s['state']['stored']==1, 'occupied slot refuses another deposit')
        _,s=g.transact('collect',b,flower=1);check(not s['response']['accepted'], 'one-slot pilot stops allocating after completion')
        expected=g.snapshot()['state'];g.close();g=Gama(config)
        check(g.snapshot()['state']==expected, 'reconnect restores GAMA checkpoint')
        check(g.repeat(req)['state']['stored']==1, 'receipt deduplication survives reconnect')
        wrong=copy.deepcopy(config);wrong['slot']['position'][0]+=1
        g.ask('configure',config_json=json.dumps(wrong));check(not g.snapshot()['response']['accepted'], 'geometry changes require explicit new session')
    finally:g.close()
    for scenario in ['same_plant','different_species','drop_recover','wrong_identity']:
        config=configuration()
        if scenario=='same_plant':config['flowers'][1]['plant_id']='plant-a'
        if scenario=='different_species':config['flowers'][1]['species']='other-species'
        g=Gama(config)
        try:
            g.transact('visit',a,flower=0)
            if scenario in ['same_plant','different_species']:
                _,s=g.transact('visit',b,flower=1);check(not s['state']['pollinated'][1],scenario+' cannot count as cross pollination')
            else:
                g.transact('collect',a,flower=0);g.transact('pick',a)
                if scenario=='drop_recover':
                    g.transact('drop',b);_,s=g.transact('pick',b)
                    check(s['state']['chunk_state']=='held' and s['state']['nectar']==[0,1],'drop/recover preserves identity and resource count')
                    _,s=g.transact('deposit',h);check(s['state']['stored']==1,'recovered chunk can be deposited')
                else:
                    request=dict(event_id=uuid.uuid4().hex,session=config['session'],kind='deposit',bee=h,hand=h,flower=-1,chunk_id='wrong',slot_id='slot-a')
                    s=g.repeat(request);check(not s['response']['accepted'],'reject wrong chunk identity')
                    request['event_id']=uuid.uuid4().hex;request['chunk_id']=s['state']['chunk_id'];request['slot_id']='wrong'
                    s=g.repeat(request);check(not s['response']['accepted'],'reject wrong slot identity')
        finally:g.close()
    config=configuration();g=Gama(config)
    try:
        moved_a=[5.,0.,1.5];moved_b=[7.,0.,1.5];moved_slot=[5.,4.,5.2]
        g.ask('set_pose',pose_json=json.dumps(dict(bee=moved_a,hand=moved_a,flowers=[moved_a,moved_b],slot=moved_slot)))
        _,s=g.transact('visit',a,flower=0);check(not s['response']['accepted'],'moving flower rejects obsolete interaction position')
        _,s=g.transact('visit',moved_a,flower=0);check(s['response']['accepted'],'moving flower accepts current interaction position')
        _,s=g.transact('collect',moved_a,flower=0);check(s['state']['chunk_pos']==moved_a,'nectar spawns at animated flower position')
        g.transact('pick',moved_a)
        _,s=g.transact('deposit',h);check(not s['response']['accepted'],'moving slot rejects obsolete snap position')
        _,s=g.transact('deposit',moved_slot);check(s['state']['stored']==1,'moving slot accepts current snap position')
    finally:g.close()
    result=dict(passed=True,count=len(checks),checks=checks)
    (ROOT/'Runtime/gama-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))

if __name__=='__main__':main()
