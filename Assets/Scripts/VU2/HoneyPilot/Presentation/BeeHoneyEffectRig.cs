using System;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public enum BeeHoneyEffectKind { Flower, Carry, Hive }

    [DisallowMultipleComponent, AddComponentMenu("Let It Bee/Presentation/Honey Effect Rig")]
    public sealed class BeeHoneyEffectRig : MonoBehaviour
    {
        public BeeHoneyEffectKind effectKind;
        public LineRenderer[] lines = Array.Empty<LineRenderer>();
        public Transform[] droplets = Array.Empty<Transform>();
        public AudioSource[] audioSources = Array.Empty<AudioSource>();
        public MeshFilter motesFilter;
        public MeshRenderer motesRenderer;

        public LineRenderer FindLine(string objectName) => Array.Find(lines, x => x != null && x.name == objectName);
        public AudioSource FindAudio(string objectName) => Array.Find(audioSources, x => x != null && x.name == objectName);
    }
}
