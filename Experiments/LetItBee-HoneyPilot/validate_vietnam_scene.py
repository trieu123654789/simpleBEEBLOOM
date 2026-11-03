import json
import pathlib
import sys
import time
import uuid
from validate_simple import SimpleGama

ROOT = pathlib.Path(__file__).resolve().parent

def run(path):
    config = json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))
    entities = config.pop("world_entities")
    # Never spend or reset either scene's authored session.
    config["session"] = uuid.uuid4().hex
    config["world_source"] = uuid.uuid4().hex
    config["checkpoint"] = str(ROOT / "Runtime" / ("vietnam-test-" + config["session"] + ".json"))
    checks = []
    def check(ok, label):
        if not ok:
            raise AssertionError(label)
        checks.append(label)
        print("PASS: " + label, flush=True)
    client = SimpleGama(config)
    try:
        snap = client.snapshot()
        check(snap["state"]["session"] == config["session"], "Scene configuration accepted through SIMPLE")
        check(snap["state"]["hive_capacity"] == 7, "Seven authored sockets accepted")
        check(snap["state"]["flowers"] == config["flowers"], "Flower coordinates match Unity export")
        packet = dict(schema="beebloom-world-v1", source=config["world_source"], sequence=1, entities=entities)
        reply = client.ask("sync_world", world_json=json.dumps(packet))
        check(reply["world_sync"]["accepted"], "Scene world registry acknowledged")
        before_cycle = client.snapshot()["cycle"]
        time.sleep(.6)
        check(client.snapshot()["cycle"] > before_cycle, "GAMA simulation advances independently")
        for i, slot in enumerate(config["hive_slots"]):
            flower = i % 2
            pos = config["flowers"][flower]["position"]
            deadline = time.monotonic() + 35
            while client.snapshot()["state"]["nectar"][flower] < 1:
                if time.monotonic() > deadline:
                    raise TimeoutError("Server nectar refill did not finish")
                time.sleep(.5)
            client.transact("visit", pos, flower=flower)
            _, response = client.transact("collect", pos, flower=flower)
            check(response["response"]["accepted"], "Flower produces nectar for cell " + str(i+1))
            _, response = client.transact("pick", pos)
            check(response["response"]["accepted"], "Nectar pickup " + str(i+1))
            event = dict(event_id=uuid.uuid4().hex, session=config["session"], kind="deposit",
                         bee=slot["position"], hand=slot["position"], flower=-1,
                         chunk_id=response["state"]["chunk_id"], slot_id=slot["id"])
            response = client.repeat(event)
            check(response["response"]["accepted"] and response["state"]["stored"] == i+1,
                  "Deposit at authored socket " + slot["id"])
            check(client.repeat(event)["state"]["stored"] == i+1, "Deposit retry is idempotent " + str(i+1))
        before = client.snapshot()["state"]
        check(all(before["pollinated"]), "Both flowers pollinated")
    finally:
        client.close()
    client = SimpleGama(config)
    try:
        after = client.snapshot()["state"]
        check(after["stored"] == 7 and after["filled_slots"] == before["filled_slots"],
              "Reconnect restores seven filled sockets")
    finally:
        client.close()
    result = dict(passed=True, count=len(checks), transport="SIMPLE -> LinkToUnity -> GAMA",
                  configuration_source=str(path), checks=checks)
    (ROOT / "Runtime" / "vietnam-validation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result), flush=True)

if __name__ == "__main__":
    run(sys.argv[1])
