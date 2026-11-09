"""Water RPC smoke test through the real SIMPLE service; refuses active players."""
import json
import time
from validate_gama import ROOT, configuration
from validate_simple import SimpleGama


def main():
    config=configuration()
    water=json.loads((ROOT/'Water/channel-demo.json').read_text())
    checks=[]
    def check(condition,name):
        assert condition,name
        checks.append(name)
    g=SimpleGama(config)
    try:
        before=g.snapshot()['state']
        reply=g.ask('configure_water',water_json=json.dumps(water))
        check(reply['water_response']['accepted'],'SIMPLE configures separate water domain')
        check(reply['water']['domain_id']==water['domain_id'],'SIMPLE returns matching water domain')
        g.ask('set_water_running',running=True)
        time.sleep(2)
        first=g.ask('read_water')['water']
        check(first['time_seconds']>0 and first['salt_mass_kg']>0,'GAMA advances salt and sends it through SIMPLE')
        check(abs(first['mass_error_kg'])<1e-7,'SIMPLE salt field conserves boundary exchange')
        g.ask('configure_water',water_json=json.dumps(water))
        second=g.ask('read_water')['water']
        check(second['time_seconds']>=first['time_seconds'],'repeated SIMPLE configure never resets water')
        g.ask('set_water_running',running=False)
        paused=g.ask('read_water')['water']['time_seconds'];time.sleep(.5)
        check(g.ask('read_water')['water']['time_seconds']==paused,'water pause leaves gameplay server running')
        check(g.snapshot()['state']==before,'SIMPLE water operations preserve honey state')
    finally:g.close()
    result=dict(passed=True,count=len(checks),checks=checks)
    (ROOT/'Runtime/simple-water-validation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))


if __name__=='__main__':main()
