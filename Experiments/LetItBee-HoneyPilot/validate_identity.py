"""Rename regression checks against a real GAMA server, using isolated copies only."""
import argparse
import copy
import json
import pathlib
import uuid
import validate_gama as core


def main(port):
    folder = core.ROOT / 'Runtime' / ('identity-check-' + uuid.uuid4().hex)
    folder.mkdir(parents=True)
    checks = []

    def check(value, label):
        if not value:
            raise AssertionError(label)
        checks.append(label)

    config = core.configuration()
    config['checkpoint'] = str(folder / 'fresh.json')
    g = core.Gama(config, port)
    try:
        snap = g.snapshot()
        check(snap['response']['accepted'], 'fresh Let It Bee configuration accepted')
        check(snap['state']['schema'] == 'letitbee-honey-pilot-v1', 'fresh schema is canonical')
        a, h = [0., 0., 1.5], [0., 4., 5.2]
        g.transact('visit', a, flower=0)
        g.transact('collect', a, flower=0)
        g.transact('pick', a)
        deposit, _ = g.transact('deposit', h)
    finally:
        g.close()

    modern = json.loads(pathlib.Path(config['checkpoint']).read_text())
    legacy = copy.deepcopy(modern)
    legacy['state']['schema'] = 'beebloom-honey-pilot-v1'
    legacy_path = folder / 'legacy.json'
    original_bytes = json.dumps(legacy).encode()
    legacy_path.write_bytes(original_bytes)
    config['checkpoint'] = str(legacy_path)

    wrong = copy.deepcopy(config)
    wrong['slot']['position'][0] += 1
    g = core.Gama(wrong, port)
    try:
        check(not g.snapshot()['response']['accepted'], 'incompatible geometry still refused')
        check(legacy_path.read_bytes() == original_bytes, 'refused migration leaves checkpoint bytes intact')
        check(not pathlib.Path(str(legacy_path) + '.before-letitbee.json').exists(), 'refused migration creates no backup or checkpoint write')
    finally:
        g.close()

    g = core.Gama(config, port)
    try:
        check(g.snapshot()['response']['accepted'], 'old checkpoint accepted with identical geometry and session')
        migrated = json.loads(legacy_path.read_text())
        backup_path = pathlib.Path(str(legacy_path) + '.before-letitbee.json')
        check(json.loads(backup_path.read_text()) == legacy, 'pre-migration backup preserves old state and all receipts')
        check(migrated == modern, 'migration changes schema only; resources, progress and receipts unchanged')
        snap = g.repeat(deposit)
        check(snap['state']['stored'] == 1 and snap['state']['revision'] == modern['state']['revision'], 'old deposit receipt survives migration without double counting')
        before_backup = backup_path.read_bytes()
    finally:
        g.close()

    g = core.Gama(config, port)
    try:
        check(g.snapshot()['state'] == modern['state'], 'reconnect preserves migrated progress')
        check(backup_path.read_bytes() == before_backup, 'reconnect never overwrites migration backup')
    finally:
        g.close()
    result = dict(passed=True, count=len(checks), checks=checks)
    (core.ROOT / 'Runtime/identity-validation.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=6875)
    main(parser.parse_args().port)
