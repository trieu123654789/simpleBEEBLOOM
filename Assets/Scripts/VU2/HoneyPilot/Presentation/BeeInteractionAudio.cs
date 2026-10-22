using UnityEngine;

namespace LetItBee.HoneyPilot
{
    [CreateAssetMenu(menuName = "Let It Bee/Interaction Audio")]
    public sealed class BeeInteractionAudio : ScriptableObject
    {
        public AudioClip pollenPop, nectarSip, honeyFormation, honeyPickup, hiveDeposit;
        public AudioClip landingClip;
        [Range(0f, 1f)] public float landingVolume = 0.6f;
        [Min(0f), Tooltip("Seconds from the clip start to the main formation accent.")]
        public float formationImpactSeconds = 0.35f;
        [Min(0f), Tooltip("Seconds from the clip start to the main pickup accent.")]
        public float pickupImpactSeconds = 0.15f;
        [Range(0f, 1f)] public float landingHapticIntensity = 0.07f;
        [Range(0, 30)] public int pollenParticleCount = 8;
        [Range(0f, 1f)] public float pollenVolume = 0.7f;
        [Range(0f, 1f)] public float sipVolume = 0.45f;
        [Range(0f, 1f)] public float formationVolume = 0.7f;
        [Range(0f, 1f)] public float pickupVolume = 0.45f;
        [Range(0f, 1f)] public float depositVolume = 0.8f;
        [Min(0f)] public float fadeSeconds = 0.12f;
        [Tooltip("Time of the strongest deposit transient within the clip, aligned to the seated animation.")]
        [Min(0f)] public float depositImpactSeconds = 0.5f;
    }
}
