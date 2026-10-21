// Backward-compatibility aliases for legacy code referencing BeeBloom.HoneyPilot
namespace BeeBloom.HoneyPilot
{
    [System.Obsolete("Use LetItBee.HoneyPilot.BeeConnectionProfile instead.")]
    public static class BeeConnectionProfile
    {
        public const string FileName = LetItBee.HoneyPilot.BeeConnectionProfile.FileName;
        public static string ConfigPath => LetItBee.HoneyPilot.BeeConnectionProfile.ConfigPath;
        public static string Resolve(string savedEndpoint, string path = null) => LetItBee.HoneyPilot.BeeConnectionProfile.Resolve(savedEndpoint, path);
    }
}
