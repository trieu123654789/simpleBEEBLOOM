"""Read/validate the actual GAMA server; requires websocket-client (already installed here)."""
import json
import pathlib
import sys
import time
import websocket

ROOT = pathlib.Path(__file__).resolve().parent
SNAPSHOT = '[cycle, bee_agent[0].location.x, bee_agent[0].location.y, bee_agent[0].location.z, cargo, stored, visits, stress, flower collect [each.flower_id, each.location.x, each.location.y, each.nectar, each.pollinated]]'

def main():
    port = int(sys.argv[1]) if len(sys.argv)>1 else 6871
    ws = websocket.create_connection(f'ws://127.0.0.1:{port}', timeout=40)
    hello = json.loads(ws.recv())
    print('Handshake:', hello)
    sequence = 0
    experiment = None
    def command(kind, **kwargs):
        nonlocal sequence
        sequence += 1
        request = dict(type=kind, request_id=sequence, **kwargs)
        if experiment:
            request['exp_id'] = experiment
            request['socket_id'] = hello['content']
        ws.send(json.dumps(request))
        while True:
            response = json.loads(ws.recv())
            if response.get('command', {}).get('request_id') == sequence:
                if response['type'] != 'CommandExecutedSuccessfully':
                    raise RuntimeError(json.dumps(response, ensure_ascii=False))
                return response.get('content')
    try:
        experiment = command('load', model=str(ROOT/'GAMA/LetItBeeDemo.gaml'), experiment='LetItBeeLink', console=False)
        print('Loaded:', experiment)
        if port == 6872: time.sleep(3)  # GUI load is acknowledged before initialization completes.
        command('step', nb_step=1, sync=True)
        print('Snapshot:', command('expression', expr=SNAPSHOT))
        print('Ask:', command('ask', agent='simulation[0]', action='set_pose', args=json.dumps(dict(px=8.0,py=8.0,pz=2.0))))
        command('step', nb_step=1, sync=True)
        print('After pose:', command('expression', expr=SNAPSHOT))
    finally:
        if experiment: command('stop')
        ws.close()

if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
