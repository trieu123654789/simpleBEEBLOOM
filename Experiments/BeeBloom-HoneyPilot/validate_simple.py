"""Real SIMPLE WebPlatform + LinkToUnity integration. Run with Unity stopped.

Uses its own player, sessions and checkpoints; never spends the player's nectar.
The configured WebPlatform must select the BeeBloomVR experiment.
"""
import json
import pathlib
import time
import uuid
import websocket
import validate_gama as core

ROOT = pathlib.Path(__file__).resolve().parent


class Monitor:
    def __init__(self):
        self.ws = websocket.create_connection('ws://127.0.0.1:8001', timeout=30)
        self.state = json.loads(self.ws.recv())
        self.ws.recv()  # settings

    def send(self, kind, **fields):
        self.ws.send(json.dumps(dict(type=kind, **fields)))

    def wait(self, predicate):
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            if predicate(self.state):
                return self.state
            item = json.loads(self.ws.recv())
            if item.get('type') == 'json_state':
                self.state = item
        raise TimeoutError(str(self.state))

    def reset(self):
        self.wait(lambda s: s['gama']['connected'])
        players = self.state.get('player', {})
        if isinstance(players, dict):
            for player in players.values():
                if player.get('connected') and player.get('id') != 'BeeBloomProtocolCheck':
                    raise RuntimeError('Another player is connected; stop Unity before isolated checks.')
            for player in players.values():
                self.send('remove_player_headset', id=player['id'])
            if players:
                self.wait(lambda s: not s.get('player'))
        if self.state['gama']['experiment_state'] != 'NONE':
            self.send('stop_experiment')
            self.wait(lambda s: s['gama']['experiment_state'] == 'NONE')
        self.send('get_simulation_by_index', simulationIndex=0)
        self.send('launch_experiment')
        self.wait(lambda s: s['gama']['experiment_state'] in ('PAUSED','RUNNING'))
        self.send('resume_experiment')
        self.wait(lambda s: s['gama']['experiment_state'] == 'RUNNING')

    def close(self):
        self.ws.close()


class SimpleGama:
    def __init__(self, config):
        self.config = config
        self.mon = Monitor()
        self.mon.reset()
        self.player = 'BeeBloomProtocolCheck'
        self.ws = websocket.create_connection('ws://127.0.0.1:8080', timeout=15)
        self.ws.send(json.dumps(dict(type='connection', id=self.player, set_heartbeat='false')))
        while True:
            message = self.receive()
            if message.get('in_game'):
                break
        self.ws.send(json.dumps(dict(type='ask', agent='simulation[0].unity_linker[0]',
            action='new_connection', args=json.dumps(dict(id=self.player)))))
        self.ask('configure', config_json=json.dumps(config))

    def receive(self):
        value = json.loads(self.ws.recv())
        if value.get('type') == 'ping':
            self.ws.send(json.dumps(dict(type='pong')))
        return value

    def ask(self, action, **args):
        correlation = uuid.uuid4().hex
        envelope = dict(correlation=correlation, operation=action, payload=args)
        self.ws.send(json.dumps(dict(type='ask', action='bee_request', agent='simulation[0].unity_linker[0]',
            args=json.dumps(dict(id=self.player, envelope_json=json.dumps(envelope))))))
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            reply = self.receive().get('contents', {}).get('bee_bloom')
            if reply and reply['correlation'] == correlation:
                if reply['fault'] and action != 'configure':
                    raise RuntimeError(reply['fault'])
                self.last = reply['snapshot']
                return self.last
        raise TimeoutError('No SIMPLE acknowledgement for ' + action)

    def snapshot(self):
        return self.ask('snapshot')

    transact = core.Gama.transact
    repeat = core.Gama.repeat

    def close(self):
        self.mon.send('remove_player_headset', id=self.player)
        time.sleep(0.3)
        self.ws.close()
        self.mon.close()


if __name__ == '__main__':
    core.Gama = SimpleGama
    core.main()
    result = json.loads((ROOT/'Runtime/gama-validation.json').read_text())
    result['transport'] = 'SIMPLE WebPlatform -> LinkToUnity -> GAMA'
    (ROOT/'Runtime/simple-validation.json').write_text(json.dumps(result, indent=2))
