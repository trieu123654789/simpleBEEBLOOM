"""Full-hive tests on real GAMA, using disposable sessions only."""
import copy
import json
import pathlib
from validate_gama import Gama, configuration

ROOT = pathlib.Path(__file__).resolve().parent
checks = []

def check(ok, label):
    if not ok:
        raise AssertionError(label)
    checks.append(label)

def full_config(old=None):
    c = copy.deepcopy(old or configuration())
    c['hive_slots'] = [dict(id='slot-a' if i == 0 else f'slot-{i}', position=[i*0.6,4.,5.2]) for i in range(7)]
    c['nectar_refill_seconds'] = 1.0
    return c

def dispose(g):
    try:
        if hasattr(g,'command'): g.command('stop')
    finally: g.close()

def run(factory=Gama):
    a,b=[0.,0.,1.5],[2.,0.,1.5]
    c=full_config(); g=factory(c)
    try:
        check(g.snapshot()['response']['accepted'], 'seven-cell configuration accepted')
        _,s=g.transact('collect',a,flower=0)
        check(not s['response']['accepted'], 'full hive requires confirmed flower contact')
        for i in range(7):
            fi=i%2; bee=a if fi==0 else b
            # Simulate elapsed time, not a client-side grant.
            if isinstance(g,Gama): g.command('step', nb_step=12, sync=True)
            else:
                import time
                time.sleep(1.3)
            g.transact('visit',bee,flower=fi)
            req,s=g.transact('collect',bee,flower=fi)
            check(s['response']['accepted'], f'nectar available for cell {i+1}')
            chunk=s['state']['chunk_id']
            g.transact('pick',bee)
            cell=c['hive_slots'][i]
            if i>0:
                request=dict(event_id=__import__('uuid').uuid4().hex,session=c['session'],kind='deposit',bee=c['slot']['position'],hand=c['slot']['position'],flower=-1,chunk_id=chunk,slot_id='slot-a')
                s=g.repeat(request)
                check(not s['response']['accepted'], 'occupied cell cannot consume another chunk')
            request=dict(event_id=__import__('uuid').uuid4().hex,session=c['session'],kind='deposit',bee=cell['position'],hand=cell['position'],flower=-1,chunk_id=chunk,slot_id=cell['id'])
            s=g.repeat(request)
            check(s['response']['accepted'] and s['state']['stored']==i+1,f'cell {i+1} filled once')
            check(g.repeat(request)['state']['stored']==i+1,'duplicate deposit cannot increment')
        check(len(s['state']['filled_slots'])==7 and all(s['state']['pollinated']),'full hive and pollination retained')
        g.transact('visit',a,flower=0)
        _,s=g.transact('collect',a,flower=0)
        check(not s['response']['accepted'],'full hive stops allocating extra honey')
        before=g.snapshot()['state']
    finally: dispose(g)
    g=factory(c)
    try:
        after=g.snapshot()['state']
        check(after['filled_slots']==before['filled_slots'] and after['stored']==7,'reconnect restores every filled cell')
        bad=copy.deepcopy(c);bad['hive_slots'][2]['position'][0]+=1
        g.ask('configure',config_json=json.dumps(bad))
        check(not g.snapshot()['response']['accepted'],'changed hive layout rejected without reset')
    finally: dispose(g)

    # Upgrade a completed original single-cell checkpoint, preserving its receipts.
    old=configuration();g=factory(old)
    try:
        g.transact('visit',a,flower=0);g.transact('collect',a,flower=0);g.transact('pick',a)
        req,s=g.transact('deposit',old['slot']['position']); previous=copy.deepcopy(s['state'])
        expanded=full_config(old)
        g.ask('configure',config_json=json.dumps(expanded));s=g.snapshot()
        check(s['response']['accepted'] and s['state']['stored']==1 and s['state']['hive_capacity']==7,'upgrade preserves old honey as 1/7')
        check(s['state']['pollen_source']==previous['pollen_source'] and s['state']['nectar']==previous['nectar'] and s['state']['last_contact']==previous['last_contact'],'upgrade preserves pollen, contact and resource counts')
        check(g.repeat(req)['state']['stored']==1,'old receipt remains idempotent after upgrade')
        g.ask('configure',config_json=json.dumps(old))
        check(not g.snapshot()['response']['accepted'],'old single-cell client cannot silently downgrade expanded session')
    finally: dispose(g)
    result=dict(passed=True,count=len(checks),checks=checks)
    (ROOT/'Runtime/full-hive-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))

if __name__=='__main__': run()
