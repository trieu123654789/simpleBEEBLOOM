using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // Optional observer/transport. Does not change water materials, meshes or flower resources.
    [DisallowMultipleComponent]
    public sealed class BeeWaterGrid : MonoBehaviour
    {
        [Header("Reviewed water domain")]
        public TextAsset configuration;
        [Tooltip("Enable only after reviewing the wet-cell mask and prescribed face discharges.")]
        public bool connectToGama;
        public bool runTransport;
        [Min(0.5f)] public float snapshotIntervalSeconds = 1f;
        [Header("Scene view diagnostics")]
        public bool showGrid = true;
        [Min(0.01f)] public float displayMaximumSalinity = 35f;
        public string Status { get; private set; } = "Not connected";
        public JObject Snapshot { get; private set; }
        public bool IsFresh => Snapshot != null && Time.realtimeSinceStartup - receivedAt < snapshotIntervalSeconds * 3f;
        IHoneyClient acceptedClient;
        string acceptedText;
        DateTime nextUpdate;
        float receivedAt;
        bool sentRunning;
        JObject domain;
        string parsedText;

        public static int CellIndex(Vector3 position, Vector2 origin, float cellSize, int width, int height)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.z) || cellSize <= 0 || width <= 0 || height <= 0) return -1;
            int x = Mathf.FloorToInt((position.x-origin.x)/cellSize);
            int z = Mathf.FloorToInt((position.z-origin.y)/cellSize);
            return x >= 0 && x < width && z >= 0 && z < height ? z*width+x : -1;
        }
        public static Vector3 CellCenter(int index, Vector2 origin, float cellSize, int width, float surfaceY)
            => new Vector3(origin.x+(index%width+0.5f)*cellSize, surfaceY, origin.y+(index/width+0.5f)*cellSize);

        // Called by the existing serialized gameplay RPC loop. Never opens a second SIMPLE player.
        public async Task SynchronizeAsync(IHoneyClient client)
        {
            if (!isActiveAndEnabled || !connectToGama || configuration == null) return;
            if (DateTime.UtcNow < nextUpdate) return;
            nextUpdate = DateTime.UtcNow.AddSeconds(Math.Max(0.5, snapshotIntervalSeconds));
            try {
                string text = configuration.text;
                if (acceptedClient != client || acceptedText != text) {
                    var reply = await ReadReply(client, "configure_water", new JObject { ["water_json"] = text });
                    if ((bool?)reply["water_response"]?["accepted"] != true)
                        throw new InvalidOperationException("Water configuration rejected: " + reply["water_response"]);
                    acceptedClient = client; acceptedText = text; sentRunning = !runTransport;
                }
                if (sentRunning != runTransport) {
                    await ReadReply(client, "set_water_running", new JObject { ["running"] = runTransport });
                    sentRunning = runTransport;
                }
                var result = await ReadReply(client, "read_water", new JObject());
                if (this == null || !isActiveAndEnabled) return;
                var state = result["water"] as JObject;
                var config = JObject.Parse(text);
                int width = (int)config["width"], height = (int)config["height"];
                if ((string)state?["schema"] != "letitbee-water-state-v1" || (bool?)state["ready"] != true
                    || (string)state["domain_id"] != (string)config["domain_id"]
                    || (int?)state["width"] != width || (int?)state["height"] != height
                    || !JToken.DeepEquals(state["origin_xz"],config["origin_xz"])
                    || Math.Abs((double)state["cell_size_m"]-(double)config["cell_size_m"]) > 1e-6
                    || Math.Abs((double)state["surface_y"]-(double)config["surface_y"]) > 1e-5
                    || !(state["salinity_g_l"] is JArray values) || values.Count != width*height)
                    throw new InvalidOperationException("Water snapshot does not match this grid.");
                foreach (var value in values) if (!double.IsFinite((double)value) || (double)value < -1e-6)
                    throw new InvalidOperationException("Invalid salinity value.");
                Snapshot = state; receivedAt = Time.realtimeSinceStartup;
                Status = "GAMA water revision " + state["revision"] + " | mass error " + state["mass_error_kg"] + " kg";
            } catch (Exception ex) {
                // Water diagnostics must not reset a honey transaction or disconnect gameplay.
                Status = "Water unavailable: " + ex.Message;
                acceptedClient = null; nextUpdate = DateTime.UtcNow.AddSeconds(5);
            }
        }
        static async Task<JObject> ReadReply(IHoneyClient client, string operation, JObject args)
        {
            var reply = await client.AskAsync(operation,args) as JObject;
            if (reply != null && reply["water_response"] != null) return reply;
            // Direct GAMA diagnostic transport, without SIMPLE envelopes.
            return await client.SnapshotAsync();
        }
        void OnDrawGizmosSelected()
        {
            if (!showGrid || configuration == null) return;
            try {
                if (parsedText != configuration.text) { parsedText = configuration.text; domain = JObject.Parse(parsedText); }
                int width = (int)domain["width"], height = (int)domain["height"];
                if (width < 1 || height < 1 || width*height > 4096) return;
                float dx = (float)domain["cell_size_m"], y = (float)domain["surface_y"];
                var origin = new Vector2((float)domain["origin_xz"][0],(float)domain["origin_xz"][1]);
                var depth = (JArray)domain["depth_m"];
                var salinity = IsFresh ? (JArray)Snapshot["salinity_g_l"] : (JArray)domain["salinity_g_l"];
                for (int i=0; i<width*height; i++) {
                    if ((float)depth[i] <= 0) continue;
                    Gizmos.color = Application.isPlaying && !IsFresh ? Color.gray : Color.Lerp(Color.cyan, new Color(1f,0.3f,0.1f), (float)salinity[i]/displayMaximumSalinity);
                    Gizmos.DrawWireCube(CellCenter(i,origin,dx,width,y+0.03f),new Vector3(dx*0.95f,0.01f,dx*0.95f));
                }
            } catch { /* A partial/invalid authoring file is reported by GAMA on configure. */ }
        }
    }
}
