using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

// Separate demo transport: no Let It Bee singletons, PlayerPrefs or scene edits.
public sealed class GamaClient : IDisposable
{
    public const string SnapshotExpression = "[cycle, bee_agent[0].location.x, bee_agent[0].location.y, bee_agent[0].location.z, cargo, stored, visits, stress, flower collect [each.flower_id, each.location.x, each.location.y, each.nectar, each.pollinated]]";
    readonly ConcurrentDictionary<int, TaskCompletionSource<JToken>> pending = new();
    readonly TaskCompletionSource<string> hello = new(TaskCreationOptions.RunContinuationsAsynchronously);
    WebSocket socket;
    int sequence;
    string socketId, experimentId;
    public string ExperimentId => experimentId;
    public int Responses { get; private set; }

    public async Task Connect(string url, string model)
    {
        socket = new WebSocket(url) { WaitTime = TimeSpan.FromSeconds(5) };
        socket.OnMessage += (_, e) => {
            try {
                var message = JObject.Parse(e.Data);
                var type = (string)message["type"];
                if (type == "ConnectionSuccessful") { hello.TrySetResult((string)message["content"]); return; }
                int id = (int?)message["command"]?["request_id"] ?? -1;
                if (pending.TryRemove(id, out var reply)) {
                    if (type == "CommandExecutedSuccessfully") { Responses++; reply.TrySetResult(message["content"]); }
                    else reply.TrySetException(new Exception(type + ": " + message["content"]));
                }
                else if (type == "SimulationError" || type == "GamaServerError" || type == "RuntimeError")
                    Fail(new Exception(type + ": " + message["content"]));
            } catch (Exception ex) { Fail(ex); }
        };
        socket.OnError += (_, e) => Fail(new Exception(e.Message));
        socket.OnClose += (_, e) => Fail(new Exception("GAMA disconnected: " + e.Reason));
        await Task.Run(() => socket.Connect());
        socketId = await Timeout(hello.Task, 10000);
        experimentId = (string)await Command("load", new JObject {
            ["model"] = model, ["experiment"] = "LetItBeeLink", ["console"] = false
        }, 60000);
        // Desktop GAMA acknowledges load before its UI/controller initializes the simulation.
        // A successful snapshot, not the load acknowledgement, is our readiness signal.
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (true) {
            try {
                var ready = await Command("expression", new JObject {
                    ["expr"] = "length(bee_agent) = 1 and length(flower) = 10 and length(hive) = 1"
                });
                if (bool.TryParse(ready.ToString(), out bool initialized) && initialized) {
                    await Snapshot(); break;
                }
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("GAMA agents did not initialize within 30 seconds");
                await Task.Delay(200);
            }
            catch (Exception ex) when (ex.Message.Contains("Unable to find the experiment or simulation") && DateTime.UtcNow < deadline) {
                await Task.Delay(200);
            }
        }
    }

    public async Task<JToken> Command(string type, JObject args = null, int timeoutMs = 10000)
    {
        if (socket == null || !socket.IsAlive) throw new Exception("GAMA socket is not connected");
        int id = Interlocked.Increment(ref sequence);
        var request = args ?? new JObject();
        request["type"] = type;
        request["request_id"] = id;
        if (experimentId != null) { request["exp_id"] = experimentId; request["socket_id"] = socketId; }
        var reply = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = reply;
        try {
            await Task.Run(() => socket.Send(request.ToString(Formatting.None)));
            return await Timeout(reply.Task, timeoutMs);
        } finally { pending.TryRemove(id, out _); }
    }
    public Task<JToken> Ask(string action, JObject args) => Command("ask", new JObject {
        ["agent"] = "simulation[0]", ["action"] = action, ["args"] = args.ToString(Formatting.None)
    });
    public async Task<JArray> Snapshot()
    {
        JToken value = await Command("expression", new JObject { ["expr"] = SnapshotExpression });
        return value is JArray array ? array : JArray.Parse((string)value);
    }
    static async Task<T> Timeout<T>(Task<T> task, int milliseconds)
    {
        if (await Task.WhenAny(task, Task.Delay(milliseconds)) != task) throw new TimeoutException("GAMA response timed out");
        return await task;
    }
    void Fail(Exception error)
    {
        hello.TrySetException(error);
        foreach (var item in pending)
            if (pending.TryRemove(item.Key, out var reply)) reply.TrySetException(error);
    }
    public void Dispose()
    {
        Fail(new ObjectDisposedException(nameof(GamaClient)));
        // Headless GAMA destroys this connection's experiments on socket close.
        if (socket != null) { socket.CloseAsync(); socket = null; }
    }
}
