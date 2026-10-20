using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BeeBloom.HoneyPilot
{
    public sealed class HoneySimpleClient : IHoneyClient
    {
        readonly Transform parent;
        readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> pending = new();
        BeeSimpleConnection connection;
        bool disposed;
        public bool WaitingForAdmission => connection != null && connection.TransportReady
            && !connection.IsConnectionState(ConnectionState.AUTHENTICATED);

        public HoneySimpleClient(Transform parent) { this.parent = parent; }

        public async Task<JObject> ConnectAsync(string endpoint, string model, JObject configuration)
        {
            if (ConnectionManager.Instance != null)
                throw new InvalidOperationException("Another SIMPLE ConnectionManager is active. Keep one connection manager for Bee Bloom.");
            var go = new GameObject("Bee Bloom SIMPLE Connection (runtime)");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            connection = go.AddComponent<BeeSimpleConnection>();
            connection.Configure(endpoint);
            connection.OnServerMessageReceived += Receive;
            go.SetActive(true);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!connection.IsConnectionState(ConnectionState.AUTHENTICATED)) {
                if (disposed) throw new ObjectDisposedException(nameof(HoneySimpleClient));
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("SIMPLE is not ready: launch BeeBloomVR and add the player in WebPlatform.");
                await Task.Delay(100);
            }
            JObject result = await Request("configure", new JObject {
                ["config_json"] = BeeConnectionProfile.ForSimpleServer(configuration).ToString(Formatting.None)
            });
            if ((string)result["state"]?["schema"] != HoneyGamaClient.Schema || (bool?)result["response"]?["accepted"] != true)
                throw new InvalidOperationException("SIMPLE/GAMA configuration refused: " + result["response"]);
            return result;
        }

        public async Task<JToken> AskAsync(string action, JObject args) => await Request(action, args);
        public Task<JObject> SnapshotAsync() => Request("snapshot", new JObject());

        async Task<JObject> Request(string operation, JObject payload)
        {
            if (disposed || connection == null || !connection.TransportReady || !connection.IsConnectionState(ConnectionState.AUTHENTICATED))
                throw new InvalidOperationException("SIMPLE player is not authenticated");
            string correlation = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[correlation] = completion;
            var envelope = new JObject { ["correlation"] = correlation, ["operation"] = operation, ["payload"] = payload };
            try {
                var arguments = new Dictionary<string,string> {
                    ["id"] = connection.GetConnectionId(), ["envelope_json"] = envelope.ToString(Formatting.None)
                };
                return await HoneyRpcRetry.WaitAsync(completion.Task,
                    () => !disposed && connection != null && connection.TransportReady
                        && connection.IsConnectionState(ConnectionState.AUTHENTICATED),
                    () => connection.SendExecutableAsk("bee_request", arguments), operation);
            } finally { pending.TryRemove(correlation, out _); }
        }

        void Receive(string key, string json)
        {
            try {
                var content = JObject.Parse(json);
                // LinkToUnity wraps custom send_message payloads under 'message'.
                var reply = (JObject)(content["bee_bloom"] ?? content["message"]?["bee_bloom"]);
                string id = (string)reply?["correlation"];
                if (id == null || !pending.TryRemove(id, out var completion)) return;
                string fault = (string)reply["fault"];
                if (!string.IsNullOrEmpty(fault)) completion.TrySetException(new InvalidOperationException("GAMA: " + fault));
                else completion.TrySetResult((JObject)reply["snapshot"]);
            } catch (Exception ex) {
                foreach (var item in pending) item.Value.TrySetException(ex);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var item in pending) item.Value.TrySetException(new ObjectDisposedException(nameof(HoneySimpleClient)));
            pending.Clear();
            if (connection != null) {
                connection.OnServerMessageReceived -= Receive;
                connection.Shutdown();
                UnityEngine.Object.Destroy(connection.gameObject);
            }
        }
    }
}
