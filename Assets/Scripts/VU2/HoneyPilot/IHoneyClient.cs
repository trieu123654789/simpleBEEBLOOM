using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeeBloom.HoneyPilot
{
    public interface IHoneyClient : IDisposable
    {
        Task<JObject> ConnectAsync(string endpoint, string model, JObject configuration);
        Task<JToken> AskAsync(string action, JObject args);
        Task<JObject> SnapshotAsync();
    }
}
