using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyPilot
    {
        [Serializable]
        public sealed class FlowerReference
        {
            public Transform head, perch;
            public Animator animator;
            public string id, plantId;
        }
        [Header("Additional flowers — A and B remain compatible with existing scenes")]
        public FlowerReference[] additionalFlowers = Array.Empty<FlowerReference>();
        public int FlowerCount => 2 + (additionalFlowers?.Length ?? 0);
        public string FlowerId(int i) => i < 2 ? (i == 0 ? "flower-a" : "flower-b") : additionalFlowers[i - 2].id;
        string PlantId(int i) => i < 2 ? (i == 0 ? plantAId : plantBId) : additionalFlowers[i - 2].plantId;
        Transform Perch(int i) => i < 2 ? (i == 0 ? flowerAPerch : flowerBPerch) : additionalFlowers[i - 2].perch;
        public Transform[] AllFlowerHeads() => Enumerable.Range(0, FlowerCount).Select(Head).ToArray();
        Transform[] AllFlowerPerches() => Enumerable.Range(0, FlowerCount).Select(Perch).ToArray();
        JArray FlowerSamples(Func<int,JToken> sample) => new JArray(Enumerable.Range(0, FlowerCount).Select(sample));
        FlowerResponse[] extraResponses;
        RuntimeAnimatorController[] extraControllers;
        TextMesh[] extraLabels;
        void InitializeAdditionalFlowers()
        {
            if (FlowerCount > 31) throw new InvalidOperationException("A scene supports at most 31 interactive flowers.");
            var ids = Enumerable.Range(0, FlowerCount).Select(FlowerId).ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != FlowerCount || AllFlowerHeads().Any(h => h == null))
                throw new InvalidOperationException("Every interactive flower needs a head and a unique ID.");
            extraResponses = additionalFlowers.Select(f => new FlowerResponse(f.head)).ToArray();
            extraControllers = additionalFlowers.Select(f => f.animator != null ? f.animator.runtimeAnimatorController : null).ToArray();
            foreach (var flower in additionalFlowers) if (flower.animator != null && pilotFlowerIdle != null) flower.animator.runtimeAnimatorController = pilotFlowerIdle;
        }
        void BuildAdditionalLabels() => extraLabels = additionalFlowers.Select(f => Label("Flower cue " + f.id)).ToArray();
        void StepAdditionalResponses()
        {
            for (int n = 0; n < (extraResponses?.Length ?? 0); n++) {
                int i = n + 2;
                extraResponses[n].Apply(PerchedFlower == i ? perchTime : 0f, contact == i ? Charge01 : 0f,
                    Enhanced ? honeyFeedback.FlowerBloom(i) : 0f, Enhanced ? honeyFeedback.FlowerRebound(i) : 0f);
            }
        }
        void UpdateAdditionalLabels()
        {
            for (int n = 0; n < (extraLabels?.Length ?? 0); n++) {
                int i = n + 2; var label = extraLabels[n]; label.text = FlowerHint(i);
                if (Time.time < failureUntil && contact == i && failureKind != "pick" && failureKind != "deposit" && failureKind != "drop") label.text = failureHint;
                if (ChunkState == "held" && Vector3.Distance(InteractionPosition, slot.position) <= 2.8f) label.text = "";
                PlaceLabel(label, Geometry(i).Surface + Vector3.up * 0.25f);
            }
        }
        void RestoreAdditionalFlowers()
        {
            for (int n = 0; n < (extraResponses?.Length ?? 0); n++) {
                extraResponses[n].Restore();
                if (additionalFlowers[n].animator != null) additionalFlowers[n].animator.runtimeAnimatorController = extraControllers[n];
            }
            if (extraLabels != null) foreach (var label in extraLabels) if (label != null) ReleaseRuntime(label.gameObject);
        }
    }
}
