"""Checks Story layout migration on the isolated GAMA test server (6873).
Creates new test checkpoints only; never loads the player checkpoint.
"""
import copy
import json
from validate_gama import Gama, configuration

def main():
    config = configuration()
    config["allow_layout_update"] = True
    config["hive_slots"] = [config["slot"]] + [dict(id=f"slot-{i}", position=[i, 4, 5.2]) for i in range(1, 7)]
    model = Gama(config)
    try:
        assert model.snapshot()["response"]["accepted"]
        for kind, position in [("visit", [0, 0, 1.5]), ("collect", [0, 0, 1.5]), ("pick", [0, 0, 1.5]), ("deposit", [0, 4, 5.2])]:
            _, result = model.transact(kind, position, flower=0)
            assert result["response"]["accepted"], (kind, result["response"])
        before = model.snapshot()["state"]
        assert before["stored"] == 1
        for flower in config["flowers"]:
            flower["position"][0] += 20
        for cell in config["hive_slots"]:
            cell["position"][1] += 8
        model.ask("configure", config_json=json.dumps(config))
        result = model.snapshot()
        assert result["response"]["accepted"]
        for key in ["stored", "filled_slots", "nectar", "pollinated", "pollen_source", "chunk_id"]:
            assert before[key] == result["state"][key], key
        assert result["state"]["flowers"] == config["flowers"]
        assert result["state"]["hive_slots"] == config["hive_slots"]
        model.ask("configure", config_json=json.dumps(config))
        assert model.snapshot()["state"] == result["state"]
        wrong = copy.deepcopy(config)
        wrong["flowers"][0]["id"] = "another-flower"
        model.ask("configure", config_json=json.dumps(wrong))
        assert not model.snapshot()["response"]["accepted"]
        assert model.snapshot()["state"] == result["state"]
        print("PASS: seven-cell layout migration preserves progress; reconnect is idempotent; identity changes rejected")
    finally:
        model.command("stop")
        model.close()

if __name__ == "__main__":
    main()
