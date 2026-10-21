using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

public static class BeeRpcRetryChecks
{
    [MenuItem("Let It Bee/Check RPC Recovery")]
    public static void Begin() => _ = Run();

    public static async Task<string> Run()
    {
        Directory.CreateDirectory("Library/HoneyPilot");
        var checks = new List<string>();
        void Check(bool value, string label) { if (!value) throw new Exception(label); checks.Add(label); }
        try {
            var reply = new TaskCompletionSource<JObject>();
            var expected = new JObject { ["stored"] = 1 };
            int sends = 0;
            var result = await HoneyRpcRetry.WaitAsync(reply.Task, () => true,
                () => { if (++sends == 2) reply.SetResult(expected); }, "transact", () => Task.CompletedTask);
            Check(sends == 2 && ReferenceEquals(result, expected), "lost first acknowledgement retries and accepts confirmed state");

            reply = new TaskCompletionSource<JObject>(); sends = 0;
            try {
                await HoneyRpcRetry.WaitAsync(reply.Task, () => true, () => sends++, "snapshot", () => Task.CompletedTask);
                throw new Exception("Missing timeout");
            } catch (TimeoutException) { Check(sends == 3, "silent server has a bounded retry budget"); }

            reply = new TaskCompletionSource<JObject>(); sends = 0;
            try {
                await HoneyRpcRetry.WaitAsync(reply.Task, () => sends == 0, () => sends++, "snapshot", () => Task.CompletedTask);
                throw new Exception("Missing transport failure");
            } catch (InvalidOperationException) { Check(sends == 1, "closed transport stops retries immediately"); }

            reply = new TaskCompletionSource<JObject>(); sends = 0;
            var fault = new InvalidOperationException("GAMA refused test operation");
            try {
                await HoneyRpcRetry.WaitAsync(reply.Task, () => true, () => { sends++; reply.SetException(fault); }, "transact", () => Task.CompletedTask);
                throw new Exception("Missing server fault");
            } catch (InvalidOperationException ex) { Check(ReferenceEquals(ex, fault) && sends == 1, "server refusal is propagated without retrying"); }

            reply = new TaskCompletionSource<JObject>(); sends = 0;
            result = await HoneyRpcRetry.WaitAsync(reply.Task, () => true, () => sends++, "snapshot",
                () => { reply.SetResult(expected); return Task.CompletedTask; });
            Check(sends == 1 && ReferenceEquals(result, expected), "completed reply wins after a delayed Unity continuation");

            reply = new TaskCompletionSource<JObject>(); reply.SetResult(expected); sends = 0;
            result = await HoneyRpcRetry.WaitAsync(reply.Task, () => false, () => sends++, "snapshot", () => Task.CompletedTask);
            Check(sends == 0 && ReferenceEquals(result, expected), "already confirmed reply needs no further send");
            Directory.CreateDirectory("Library/HoneyPilot");
            File.WriteAllText("Library/HoneyPilot/rpc-retry-validation.json", JsonConvert.SerializeObject(new {passed=true,count=checks.Count,checks},Formatting.Indented));
            return "RPC recovery: " + checks.Count + " checks passed.";
        } catch (Exception ex) {
            File.WriteAllText("Library/HoneyPilot/rpc-retry-validation.json", JsonConvert.SerializeObject(new {passed=false,count=checks.Count,checks,error=ex.ToString()},Formatting.Indented));
            throw;
        }
    }
}
