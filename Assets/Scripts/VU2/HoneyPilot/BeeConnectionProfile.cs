using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BeeBloom.HoneyPilot
{
    // Connection settings only. Never deserializes into scene components.
    public static class BeeConnectionProfile
    {
        public const string FileName = "beebloom-connection.json";
        public static string ConfigPath {
            get {
#if UNITY_EDITOR
                return Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Experiments/BeeBloom-HoneyPilot/Runtime/" + FileName));
#else
                return Path.Combine(Application.persistentDataPath, FileName);
#endif
            }
        }

        public static string Resolve(string savedEndpoint, string path = null)
        {
            path ??= ConfigPath;
            if (!File.Exists(path)) return ValidateEndpoint(savedEndpoint);
            var json = JObject.Parse(File.ReadAllText(path));
            foreach (var field in json.Properties())
                if (field.Name != "schema" && field.Name != "simpleEndpoint")
                    throw new InvalidDataException("Unknown connection setting: " + field.Name);
            if ((string)json["schema"] != "beebloom-connection-v1")
                throw new InvalidDataException("Expected connection schema beebloom-connection-v1 in " + path);
            return ValidateEndpoint((string)json["simpleEndpoint"]);
        }

        public static string ValidateEndpoint(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                || uri.Scheme != "ws" || string.IsNullOrEmpty(uri.Host) || uri.Port < 1 || uri.Port > 65535
                || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException("Use ws://<PC address>:8080 without a path, credentials or query.");
            // SIMPLE's current base connector constructs a host:port URI itself.
            if (uri.HostNameType == UriHostNameType.IPv6)
                throw new InvalidDataException("This SIMPLE connector requires an IPv4 address or hostname.");
            return "ws://" + uri.Host + ":" + uri.Port;
        }

        public static JObject ForSimpleServer(JObject configuration)
        {
            var result = (JObject)configuration.DeepClone();
            string session = (string)result["session"];
            if (!Guid.TryParseExact(session, "N", out _))
                throw new InvalidDataException("SIMPLE checkpoint session must be a 32-character GUID.");
            // A Quest must never need the PC's model/checkpoint filesystem path.
            result.Remove("checkpoint");
            result["checkpoint_key"] = session;
            return result;
        }
    }
}
