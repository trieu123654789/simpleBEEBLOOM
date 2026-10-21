using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // Read the saved rig in world space. Never scale the plant or write bones.
    public sealed class BeeFlowerGeometry
    {
        readonly Transform head;
        readonly Transform[] petals;
        int sampledFrame = -1;
        float sampledRadius;
        public BeeFlowerGeometry(Transform flowerHead)
        {
            head = flowerHead;
            petals = head.GetComponentsInChildren<Transform>(true);
        }
        public Vector3 Surface => head.position + Vector3.up * Mathf.Max(0.025f, Mathf.Abs(head.lossyScale.y) * 0.01875f);
        public Vector3 Nectar => Surface + Vector3.up * 0.1f;
        public float Radius {
            get {
                if (Application.isPlaying && sampledFrame == Time.frameCount) return sampledRadius;
                float radius = 0f;
                foreach (var petal in petals) {
                    if (petal != null && petal.name.StartsWith("Petal_")) {
                        radius = Mathf.Max(radius, Vector3.ProjectOnPlane(petal.position - head.position, Vector3.up).magnitude);
                        // A closed blossom's tip points upward. Its bone-chain
                        // length still gives clearance for the open petals.
                        float reach = 0f; var bone = petal;
                        while (bone != null && bone != head && bone.parent != null) {
                            reach += Vector3.Distance(bone.position,bone.parent.position); bone = bone.parent;
                        }
                        radius = Mathf.Max(radius, reach * 1.1f);
                    }
                }
                sampledFrame = Time.frameCount;
                return sampledRadius = Mathf.Min(1.5f, radius);
            }
        }
        public float Distance(Vector3 position) => Mathf.Max(0f, Vector3.Distance(position, Surface) - Radius);
        public float ViewDistance(float preferred) => Mathf.Max(preferred, Radius > 0f ? Radius / Mathf.Tan(35f * Mathf.Deg2Rad) + 0.2f : 0f);
    }
}
