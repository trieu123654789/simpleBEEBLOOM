using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // Connection settings only. Never deserializes into scene components.
    public static class BeeConnectionProfile
    {
        public const string FileName = "letitbee-connection.json";
        public const string LegacyFileName = "beebloom-connection.json";
        public static string ConfigPath {
            get {
#if UNITY_EDITOR
                string newPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Experiments/LetItBee-HoneyPilot/Runtime/" + FileName));
                if (File.Exists(newPath)) return newPath;
                string legacyPath = Path.Combine(Path.GetDirectoryName(newPath), LegacyFileName);
                return File.Exists(legacyPath) ? legacyPath : newPath;
#else
                string newPath = Path.Combine(Application.persistentDataPath, FileName);
                if (File.Exists(newPath)) return newPath;
                string legacyPath = Path.Combine(Application.persistentDataPath, LegacyFileName);
                if (File.Exists(legacyPath)) return legacyPath;
                return newPath;
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
            string schema = (string)json["schema"];
            if (schema != "letitbee-connection-v1" && schema != "beebloom-connection-v1")
                throw new InvalidDataException("Expected connection schema letitbee-connection-v1 or beebloom-connection-v1 in " + path);
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
