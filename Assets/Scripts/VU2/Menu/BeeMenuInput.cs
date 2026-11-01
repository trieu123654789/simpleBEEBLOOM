using UnityEngine;

namespace LetItBee.UI
{
    // Only blocks local player actions. The simulation and tracked head remain live.
    public static class BeeMenuInput
    {
        public static bool Blocked { get; internal set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Blocked = false; }
    }
}