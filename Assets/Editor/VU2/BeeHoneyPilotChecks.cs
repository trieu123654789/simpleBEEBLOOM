using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BeeHoneyPilotChecks
{
    public const string ResultPath = "Library/HoneyPilot/unity-client-validation.json";
    public static string Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run client integration checks in Edit Mode.");
        _ = Run(); return "Unity C# client integration checks started.";
    }
    static async Task Run()
    {
        var checks = new List<string>(); HoneyGamaClient client = null;
        try {
            var pilot = GameObject.Find("LetItBee Honey Pilot").GetComponent<BeeHoneyPilot>();
            void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks.Add(label); }
            Check(pilot.flowerAHead != pilot.flowerBHead && pilot.plantAId != pilot.plantBId, "two independent flower plants");
            Check(pilot.honeyMesh != null && Mathf.Abs(pilot.honeyMesh.bounds.size.x - 0.16f) < 0.001f, "physical nectar model is 16 cm wide");
            Check(pilot.slot.position.y < pilot.beeRoot.GetComponent<BeeController>().maxFlightAltitude - 1f, "slot below saved flight ceiling");
            Check(pilot.gameObject.GetComponentsInChildren<Camera>(true).Length == 0, "pilot adds no camera");
            JObject config = pilot.BuildConfiguration(); string session = Guid.NewGuid().ToString("N");
            config["session"] = session;
            config["checkpoint"] = pilot.checkpointDirectoryOnServer.TrimEnd('/') + "/check-" + session + ".json";
            Vector3 a = pilot.flowerAHead.position, b = pilot.flowerBHead.position, slot = pilot.slot.position;
            client = new HoneyGamaClient(); JObject snapshot = await client.ConnectAsync(pilot.endpoint, pilot.modelOnServer, config);
            Check((string)snapshot["state"]["schema"] == HoneyGamaClient.Schema, "actual Unity client connects to actual GAMA model");
            long cycle = (long)snapshot["cycle"]; await Task.Delay(350); snapshot = await client.SnapshotAsync();
            Check((long)snapshot["cycle"] > cycle, "GAMA advances independently");
            JObject Request(string kind, Vector3 bee, Vector3 hand, int flower = -1) => new JObject {
                ["event_id"] = Guid.NewGuid().ToString("N"), ["session"] = session, ["kind"] = kind, ["flower"] = flower,
                ["bee"] = BeeHoneyPilot.ToGama(bee), ["hand"] = BeeHoneyPilot.ToGama(hand),
                ["chunk_id"] = (string)snapshot["state"]["chunk_id"], ["slot_id"] = "slot-a"
            };
            async Task Send(JObject request) {
                await client.AskAsync("transact", new JObject { ["request_json"] = request.ToString(Formatting.None) }); snapshot = await client.SnapshotAsync();
            }
            await Send(Request("collect", a + Vector3.up * 10f, a, 0));
            Check(!(bool)snapshot["response"]["accepted"], "GAMA refuses collection from outside contact range");
            await Send(Request("visit", a, a, 0)); Check((int)snapshot["state"]["pollen_source"] == 0, "visit A acquires pollen");
            var collect = Request("collect", a, a, 0); await Send(collect);
            Check((string)snapshot["state"]["chunk_state"] == "ready", "collect allocates a real GAMA chunk identity");
            string identity = (string)snapshot["state"]["chunk_id"]; await Send(collect);
            Check((string)snapshot["state"]["chunk_id"] == identity && (int)snapshot["state"]["nectar"][0] == 0, "Unity retry cannot duplicate nectar");
            await Send(Request("pick", a, a)); Check((string)snapshot["state"]["chunk_state"] == "held", "grip transaction picks the allocated chunk");
            await Send(Request("visit", b, b, 1)); Check((bool)snapshot["state"]["pollinated"][1], "flower B receives A pollen while cargo is held");
            var deposit = Request("deposit", slot, slot); await Send(deposit);
            Check((int)snapshot["state"]["stored"] == 1 && (string)snapshot["state"]["chunk_state"] == "stored", "deliberate release fills one slot");
            client.Dispose(); client = new HoneyGamaClient(); snapshot = await client.ConnectAsync(pilot.endpoint, pilot.modelOnServer, config);
            Check((int)snapshot["state"]["stored"] == 1 && (bool)snapshot["state"]["pollinated"][1], "fresh Unity connection restores model progress");
            await Send(deposit); Check((int)snapshot["state"]["stored"] == 1 && (bool)snapshot["response"]["accepted"], "lost deposit acknowledgement can be safely retried after reconnect");
            Write(new { passed = true, count = checks.Count, checks, finishedUtc = DateTime.UtcNow.ToString("O") });
            Debug.Log("Let It Bee honey pilot: " + checks.Count + " Unity/GAMA client integration checks passed.");
        } catch (Exception ex) {
            Write(new { passed = false, count = checks.Count, checks, error = ex.ToString() }); Debug.LogError("Honey pilot check: " + ex.Message);
        } finally { client?.Dispose(); }
    }
    static void Write(object value) { Directory.CreateDirectory(Path.GetDirectoryName(ResultPath)); File.WriteAllText(ResultPath, JsonConvert.SerializeObject(value, Formatting.Indented)); }
}
