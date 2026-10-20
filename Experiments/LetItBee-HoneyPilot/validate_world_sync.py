"""Private real-GAMA checks for the generic observation layer, not a VR mock."""
import copy
import json
import pathlib
import uuid
import websocket
from validate_gama import Gama, configuration

ROOT=pathlib.Path(__file__).resolve().parents[2]
FOLDER=ROOT/'Library/HoneyPilot/WorldSync'

def main():
    FOLDER.mkdir(parents=True,exist_ok=True)
    config=configuration();config['world_source']=uuid.uuid4().hex
    config['checkpoint']=str(FOLDER/('validation-'+config['session']+'.json'))
    # A unique URI ensures this verifies today's source, not a GAMA compiler cache.
    text=(ROOT/'Experiments/LetItBee-HoneyPilot/GAMA/LetItBeeHoneyPilot.gaml').read_text(encoding='utf-8-sig')
    text=text.replace('    action frame_map {','    action capture(string target) { save snapshot(self,"HoneyPilotMap",{800,800}) to: target; }\n    action frame_map {')
    model=FOLDER/('WorldCheck-'+uuid.uuid4().hex+'.gaml');model.write_text(text,encoding='utf-8')
    g=Gama.__new__(Gama);g.config=config;g.sequence=0;g.experiment=None
    g.ws=websocket.create_connection('ws://127.0.0.1:6875',timeout=30)
    g.hello=json.loads(g.ws.recv())['content']
    checks=[]
    def check(ok,label):
        if not ok:raise AssertionError(label)
        checks.append(label)
    def expr(code):return json.loads(g.command('expression',expr='to_json('+code+')'))
    def send(data):
        g.ask('sync_world',world_json=data if isinstance(data,str) else json.dumps(data,separators=(',',':')))
        return expr('world_response')
    def actors():return expr('synced_entity collect [each.entity_id,each.entity_kind,each.entity_position.x,each.entity_position.y,each.entity_position.z,each.location.x,each.location.y]')
    def entity(key,kind,pos):return dict(id=key,kind=kind,label=key,position=pos)
    base=[entity('player-bee','bee',[10,20,2]),entity('flower-a','flower',[0,0,1.5]),entity('flower-b','flower',[2,0,1.5]),entity('slot-a','hive',[0,4,5.2])]
    def packet(seq,entries):return dict(schema='letitbee-world-v1',source=config['world_source'],sequence=seq,entities=entries)
    try:
        g.experiment=g.command('load',model=str(model),experiment='HoneyPilot',console=False)
        g.ask('configure',config_json=json.dumps(config))
        check(g.snapshot()['response']['accepted'],'fresh source compiles and configuration is accepted')
        g.transact('collect',[0,0,1.5],flower=0)
        progress=copy.deepcopy(g.snapshot()['state'])
        check(progress['chunk_state']=='ready','private fixture has real honey progress to protect')
        reply=send(packet(1,base));check(reply['accepted'] and reply['count']==4,'base observation snapshot creates exactly four actors')
        check(len(actors())==4,'reported count agrees with actual GAMA agent population')
        expanded=base+[entity('extra-bee','bee',[10,21,3]),entity('extra-flower','flower',[-8,-4,2])]
        reply=send(packet(2,expanded));check(reply['bees']==2 and reply['flowers']==3 and reply['hives']==1,'adding known object kinds changes real GAMA populations')
        points={a[0]:a for a in actors()}
        check(points['extra-bee'][6]<points['player-bee'][6],'Unity +Z / GAMA +Y displays north/up')
        check(all(24.99<=a[5]<=165.01 and 29.99<=a[6]<=170.01 for a in points.values()),'frame includes all actors, including a bee outside the old flower/hive frame')
        g.ask('capture',target=(FOLDER/'world-sync-expanded.png').as_posix())
        moved=copy.deepcopy(expanded);moved[-1]['position']=[-20,12,4]
        reply=send(packet(3,moved));check(reply['accepted'],'moving an object updates the current snapshot')
        check(next(a for a in actors() if a[0]=='extra-flower')[2:5]==[-20,12,4],'position and altitude survive into the actual GAMA actor')
        again=send(packet(3,moved));check(again['reason']=='replayed' and len(actors())==6,'exact replay does not create duplicate actors')
        check(not send(packet(2,base))['accepted'] and len(actors())==6,'outdated snapshot cannot delete newer actors')
        check(not send(packet(3,base))['accepted'] and len(actors())==6,'same sequence with different content is rejected')
        invalid=packet(4,expanded+[expanded[0]])
        check(not send(invalid)['accepted'] and len(actors())==6,'duplicate IDs reject the entire update atomically')
        invalid=packet(4,[entity('unknown','river',[1,2,3])])
        check(not send(invalid)['accepted'] and len(actors())==6,'unsupported kinds do not silently create fake river models')
        invalid=packet(4,[entity('broken','bee',[1,2])])
        check(not send(invalid)['accepted'] and len(actors())==6,'malformed point leaves previous agents intact')
        check(not send('{')['accepted'] and len(actors())==6,'partial JSON cannot erase observations')
        invalid=packet(4,base);invalid['source']='another-stream'
        check(not send(invalid)['accepted'] and len(actors())==6,'a different source cannot overwrite the configured stream')
        invalid=packet(4,base);invalid['schema']='future-unsupported'
        check(not send(invalid)['accepted'],'unsupported schema is rejected')
        invalid=packet(4,[entity('many-'+str(i),'bee',[i,0,1]) for i in range(257)])
        check(not send(invalid)['accepted'] and len(actors())==6,'bounded first-version population rejects an oversized snapshot atomically')
        reply=send(packet(4,base));check(reply['count']==4 and len(actors())==4,'removal in a full snapshot deletes stale agents')
        reply=send(packet(5,[]));check(reply['count']==0 and len(actors())==0,'empty full snapshot removes all observation actors')
        reply=send(packet(6,expanded));check(reply['count']==6,'a full reconnect snapshot restores actors without duplicates')
        check(g.snapshot()['state']==progress,'all additions, removals, movement and replays preserve honey progress')
        g.ask('configure',config_json=json.dumps(config))
        check(len(actors())==6,'same-source reconnect configuration retains actor identities')
        old_source=config['world_source'];config['world_source']=uuid.uuid4().hex
        g.ask('configure',config_json=json.dumps(config))
        check(len(actors())==0,'new application stream starts a clean observation registry')
        delayed=packet(99,expanded);delayed['source']=old_source
        check(not send(delayed)['accepted'],'delayed packet from previous application cannot repopulate actors')
        check(send(packet(1,expanded))['accepted'] and len(actors())==6,'new stream can safely restart its sequence counter')
        check(g.snapshot()['state']==progress,'new-stream replacement also retains gameplay checkpoint state')
        result=dict(passed=True,count=len(checks),checks=checks,transport='Actual GAMA 6875, unique source URI; own gameplay checkpoint')
        (FOLDER/'gama-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
    finally:
        if g.experiment:g.command('stop')
        g.close()

if __name__=='__main__':main()
