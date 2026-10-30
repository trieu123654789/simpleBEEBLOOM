using UnityEngine;

namespace LetItBee.HoneyPilot
{
    [CreateAssetMenu(menuName = "Let It Bee/Honey Presentation Library")]
    public sealed class BeeHoneyPresentationLibrary : ScriptableObject
    {
        public const string ResourcePath = "BeePresentation/PresentationLibrary";
        [Header("Reusable Prefabs")]
        public GameObject nectarPrefab;
        public BeeHoneyEffectRig flowerEffectsPrefab;
        public BeeHoneyEffectRig carryEffectsPrefab;
        public BeeHoneyEffectRig hiveEffectsPrefab;
        public Mesh dropletMesh;
        [Header("Effect Colours")]
        public Color nectarColor = new Color(1f, 0.64f, 0.08f);
        public Color pollenColor = new Color(1f, 0.83f, 0.24f);
        public Color highlightColor = new Color(1f, 0.94f, 0.63f);
        [Header("Insertion Motion (seconds)")]
        [Min(0.05f)] public float alignmentDuration = 0.36f;
        [Min(0.05f)] public float insertionDuration = 0.6f;
        [Header("Effect Size")]
        [Range(0.25f, 3f)] public float lineWidthScale = 1f;
        [Range(0.25f, 3f)] public float particleSizeScale = 1f;
        [Header("Existing Audio Profile")]
        [Tooltip("Clip and accent timing. Pilot audio settings still take priority when assigned.")]
        public BeeInteractionAudio audioProfile;

        public bool HasEffectPrefabs => flowerEffectsPrefab != null && carryEffectsPrefab != null && hiveEffectsPrefab != null;
        public static BeeHoneyPresentationLibrary Load() => Resources.Load<BeeHoneyPresentationLibrary>(ResourcePath);

        // Explicit scene bindings keep precedence, preserving existing saved mesh/material choices.
        public static Transform CreateNectar(Transform parent, Mesh sceneMesh, Material sceneMaterial, string name)
        {
            var library = Load();
            var go = library != null && library.nectarPrefab != null
                ? Instantiate(library.nectarPrefab, parent, false) : new GameObject(name);
            if (go.transform.parent != parent) go.transform.SetParent(parent, false);
            go.name = name; go.layer = 2;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null) filter = go.AddComponent<MeshFilter>();
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
            if (sceneMesh != null) filter.sharedMesh = sceneMesh;
            if (sceneMaterial != null) renderer.sharedMaterial = sceneMaterial;
            return go.transform;
        }
    }
}
