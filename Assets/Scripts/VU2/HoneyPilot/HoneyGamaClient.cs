using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace LetItBee.HoneyPilot
{
    // Dedicated transport: does not touch SIMPLE singletons or shared InputActions.
    public sealed class HoneyGamaClient : IHoneyClient
    {
        public const string Schema = "beebloom-honey-pilot-v1";
        public const string SnapshotExpression = "to_json([\"state\"::state,\"response\"::response,\"cycle\"::cycle,\"flower_positions\"::flower_positions,\"slot_position\"::slot_coordinates,\"world_sync\"::world_response])";
        readonly ConcurrentDictionary<int, TaskCompletionSource<JToken>> pending = new();
        readonly TaskCompletionSource<string> hello = new(TaskCreationOptions.RunContinuationsAsynchronously);
        WebSocket socket;
        string socketId, experimentId;
        int sequence;
        bool disposed;

        public async Task<JObject> ConnectAsync(string endpoint, string model, JObject configuration)
        {
            socket = new WebSocket(endpoint) { WaitTime = TimeSpan.FromSeconds(3) };
            socket.OnMessage += (_, e) => {
                try {
                    var message = JObject.Parse(e.Data);
                    string type = (string)message["type"];
                    if (type == "ConnectionSuccessful") { hello.TrySetResult((string)message["content"]); return; }
                    int id = (int?)message["command"]?["request_id"] ?? -1;
                    if (pending.TryRemove(id, out var reply)) {
                        if (type == "CommandExecutedSuccessfully") reply.TrySetResult(message["content"]);
                        else reply.TrySetException(new InvalidOperationException(type + ": " + message["content"]));
                    } else if (type == "SimulationError" || type == "RuntimeError") Fail(new Exception(type + ": " + message["content"]));
                } catch (Exception ex) { Fail(ex); }
            };
            socket.OnClose += (_, e) => Fail(new Exception("GAMA disconnected: " + e.Reason));
            socket.OnError += (_, e) => Fail(new Exception(e.Message));
            await Task.Run(() => socket.Connect());
            socketId = await WithTimeout(hello.Task, 8000);
            experimentId = (string)await CommandAsync("load", new JObject {
                ["model"] = model, ["experiment"] = "HoneyPilot", ["console"] = false
            }, 60000);
            await AskAsync("configure", new JObject { ["config_json"] = configuration.ToString(Formatting.None) });
            JObject snapshot = await SnapshotAsync();
            if ((string)snapshot["state"]?["schema"] != Schema || (bool?)snapshot["response"]?["accepted"] != true)
                throw new InvalidOperationException("GAMA configuration refused: " + snapshot["response"]);
            await CommandAsync("play");
            return snapshot;
        }

        public Task<JToken> AskAsync(string action, JObject args) => CommandAsync("ask", new JObject {
            ["agent"] = "simulation[0]", ["action"] = action, ["args"] = args.ToString(Formatting.None)
        });

        public async Task<JObject> SnapshotAsync()
        {
            JToken token = await CommandAsync("expression", new JObject { ["expr"] = SnapshotExpression });
            return token is JObject obj ? obj : JObject.Parse((string)token);
        }

        async Task<JToken> CommandAsync(string type, JObject fields = null, int timeout = 8000)
        {
            if (disposed || socket == null || !socket.IsAlive) throw new InvalidOperationException("GAMA socket unavailable");
            int id = Interlocked.Increment(ref sequence);
            JObject request = fields ?? new JObject();
            request["type"] = type; request["request_id"] = id;
            if (experimentId != null) { request["exp_id"] = experimentId; request["socket_id"] = socketId; }
            var reply = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[id] = reply;
            try {
                string json = request.ToString(Formatting.None);
                await Task.Run(() => socket.Send(json));
                return await WithTimeout(reply.Task, timeout);
            } finally { pending.TryRemove(id, out _); }
        }

        static async Task<T> WithTimeout<T>(Task<T> task, int timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task) throw new TimeoutException("GAMA response timeout");
            return await task;
        }
        void Fail(Exception error)
        {
            hello.TrySetException(error);
            foreach (var item in pending) if (pending.TryRemove(item.Key, out var reply)) reply.TrySetException(error);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Fail(new ObjectDisposedException(nameof(HoneyGamaClient)));
            // The model checkpoints each receipt before ACK, so a fresh headless
            // experiment on reconnect restores authority and deduplication together.
            if (socket != null) { socket.CloseAsync(); socket = null; }
        }
    }
}
