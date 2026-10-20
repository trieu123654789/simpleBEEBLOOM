using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeeBloom.HoneyPilot
{
    // Each send repeats the same correlation/envelope. Transactions retain their
    // event ID, so a lost acknowledgement cannot allocate or deposit twice.
    public static class HoneyRpcRetry
    {
        public static async Task<JObject> WaitAsync(Task<JObject> reply, Func<bool> ready,
            Action send, string operation, Func<Task> waitBudget = null, int attempts = 3)
        {
            for (int attempt = 0; attempt < attempts; attempt++) {
                if (reply.IsCompleted) return await reply;
                if (!ready()) throw new InvalidOperationException("SIMPLE connection closed while waiting for " + operation);
                send();
                await Task.WhenAny(reply, waitBudget != null ? waitBudget() : Task.Delay(4000));
                // Unity may resume after both the reply and the budget have
                // completed. Accept the reply rather than the old timer winner.
                if (reply.IsCompleted) return await reply;
            }
            throw new TimeoutException("SIMPLE/GAMA did not acknowledge " + operation + " after retries");
        }
    }
}
