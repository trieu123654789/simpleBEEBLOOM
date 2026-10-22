using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // One continuous collision surface per flower, sampled from its animated mesh.
    // The saved model, bone poses and small authored landing patches stay intact.
    [DisallowMultipleComponent]
    public sealed class BeeFlowerSurface : MonoBehaviour
    {
        public SkinnedMeshRenderer flowerMesh;
        [Range(5f, 30f)] public float collisionRefreshRate = 15f;
        Mesh bakedMesh;
        MeshCollider surface;
        float refreshRemaining;
        Collider[] patches;
        bool[] patchStates;
        readonly System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        int[] triangles;

        public bool Ready => surface != null && surface.enabled;

        public bool Refresh(float deltaTime, bool force = false)
        {
            if (flowerMesh == null) return false;
            refreshRemaining -= Mathf.Max(0f, deltaTime);
            if (Ready && !force && refreshRemaining > 0f) return true;
            if (surface == null) {
                var go = new GameObject("Animated flower collision");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                surface = go.AddComponent<MeshCollider>();
                bakedMesh = new Mesh { name = "Flower collision pose" };
                bakedMesh.MarkDynamic();
                var points = GetComponent<BeePerchPoint>();
                if (points != null) {
                    patches = new Collider[points.landingPoints.Length];
                    patchStates = new bool[patches.Length];
                    for (int i = 0; i < patches.Length; i++) {
                        patches[i] = points.landingPoints[i]?.landingSurface;
                        if (patches[i] == null) continue;
                        patchStates[i] = patches[i].enabled;
                        patches[i].enabled = false;
                    }
                }
            }
            // BakeMesh includes renderer scale; applying lossyScale again would double it.
            flowerMesh.BakeMesh(bakedMesh);
            bakedMesh.GetVertices(vertices);
            if (triangles == null) triangles = bakedMesh.triangles;
            surface.transform.SetPositionAndRotation(flowerMesh.transform.position, flowerMesh.transform.rotation);
            surface.transform.localScale = Vector3.one;
            Vector3 scale = surface.transform.lossyScale;
            surface.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            surface.sharedMesh = null;
            surface.sharedMesh = bakedMesh;
            refreshRemaining = 1f / Mathf.Max(5f, collisionRefreshRate);
            return true;
        }

        public bool Sample(Vector3 near, float probeDistance, out RaycastHit hit)
        {
            hit = default;
            return Ready && surface.Raycast(new Ray(near + Vector3.up * probeDistance, Vector3.down),
                out hit, probeDistance * 2f) && hit.normal.y >= 0.4f;
        }

        public bool Supports(Vector3 near, float probeDistance, float edgeMargin, out RaycastHit hit)
        {
            if (!Sample(near, probeDistance, out hit)) return false;
            if (edgeMargin <= 0f) return true;
            RaycastHit edge;
            return Sample(hit.point + Vector3.right * edgeMargin, probeDistance, out edge)
                && Sample(hit.point - Vector3.right * edgeMargin, probeDistance, out edge)
                && Sample(hit.point + Vector3.forward * edgeMargin, probeDistance, out edge)
                && Sample(hit.point - Vector3.forward * edgeMargin, probeDistance, out edge);
        }

        public Vector3 FollowContact(int triangle, Vector3 barycentric, Vector3 fallback)
        {
            int first = triangle * 3;
            if (!Ready || triangles == null || first < 0 || first + 2 >= triangles.Length) return fallback;
            return surface.transform.TransformPoint(vertices[triangles[first]] * barycentric.x
                + vertices[triangles[first + 1]] * barycentric.y + vertices[triangles[first + 2]] * barycentric.z);
        }

        void OnEnable()
        {
            if (surface != null) surface.enabled = true;
            if (patches != null) foreach (var patch in patches) if (patch != null) patch.enabled = false;
        }
        void OnDisable()
        {
            if (surface != null) surface.enabled = false;
            if (patches != null) for (int i = 0; i < patches.Length; i++)
                if (patches[i] != null) patches[i].enabled = patchStates[i];
        }
        void OnDestroy()
        {
            if (surface != null) { if (Application.isPlaying) Destroy(surface.gameObject); else DestroyImmediate(surface.gameObject); }
            if (bakedMesh != null) { if (Application.isPlaying) Destroy(bakedMesh); else DestroyImmediate(bakedMesh); }
        }
    }
}
