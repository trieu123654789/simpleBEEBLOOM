using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed class BeePerchPoint : MonoBehaviour
    {
        public Vector3 viewOffset = new Vector3(0f, 0.32f, -0.15f);
        [Min(0.1f), InspectorName("Landing Detection Radius"), Tooltip("Maximum approach distance in world metres.")] public float approachRadius = 1.2f;
        [InspectorName("Surface Visual"), Tooltip("Petal bone that bends under the bee's weight.")] public Transform branchVisual;
        [Tooltip("Place the bee body on this surface while the view uses the authored offset.")]
        public bool standOnBranch;
        public Vector2 supportSize = new Vector2(0.18f, 0.18f);
        [Range(0f, 6f), InspectorName("Petal Bend Angle"), Tooltip("Maximum resting bend in degrees.")] public float landingBendDegrees = 2f;
        [Min(0.002f)] public float branchColliderRadius = 0.012f;
        [Tooltip("Solid landing patch saved in the scene. Its collider dimensions remain editable.")]
        public BoxCollider landingSurface;
        [Tooltip("Solid collider along the branch, saved in the scene.")]
        public CapsuleCollider branchSurface;
        [Header("Petal Landing")]
        public bool isPetalSurface;
        public BeePerchPoint[] landingPoints = new BeePerchPoint[0];
        [Min(0.1f), Tooltip("Natural spring frequency in Hz.")] public float springFrequency = 1.6f;
        [Range(0.1f, 2f)] public float dampingRatio = 0.6f;
        Quaternion supportRotationLocal;
        GameObject support;
        bool ownsSupport, ownsBranchCollider;
        Transform branchCollider;
        Vector3 branchContactLocal, branchEndLocal;
        Vector3 authoredColliderLocal;
        Quaternion authoredColliderRotation;
        float load, loadVelocity;
        bool contactCached;
        Quaternion baseRotation, appliedRotation;
        bool deformed;
        public Vector3 ViewPosition => transform.position + viewOffset;
        public Vector3 SupportPosition => contactCached && branchVisual != null
            ? branchVisual.TransformPoint(branchContactLocal) : transform.position;
        public Vector3 BodyPosition(float radius, float clearance)
        {
            Vector3 normal = landingSurface != null ? landingSurface.transform.up : Vector3.up;
            return SupportPosition + normal * (radius + 0.006f) + Vector3.up * Mathf.Max(0f, clearance - radius);
        }
        void Start()
        {
            if (!standOnBranch || support != null) return;
            if (branchVisual != null) {
                branchContactLocal = branchVisual.InverseTransformPoint(transform.position);
                branchEndLocal = branchVisual.childCount > 0 ? branchVisual.GetChild(0).localPosition : branchContactLocal;
                contactCached = true;
            }
            if (landingSurface != null) support = landingSurface.gameObject;
            else {
                ownsSupport = true;
                support = new GameObject("Branch landing surface");
                support.layer = gameObject.layer;
                support.transform.SetParent(transform, false);
                // Keep the contact patch level, but move it with the deforming branch.
                support.transform.rotation = Quaternion.identity;
                Vector3 scale = support.transform.lossyScale;
                support.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                    1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
                var collider = support.AddComponent<BoxCollider>();
                collider.size = new Vector3(supportSize.x, 0.015f, supportSize.y);
                collider.center = new Vector3(0f, -collider.size.y * 0.5f, 0f);
            }
            if (branchVisual != null) supportRotationLocal = Quaternion.Inverse(branchVisual.rotation) * support.transform.rotation;
            if (branchSurface != null) {
                branchCollider = branchSurface.transform;
                if (branchVisual != null) {
                    authoredColliderLocal = branchVisual.InverseTransformPoint(branchCollider.position);
                    authoredColliderRotation = Quaternion.Inverse(branchVisual.rotation) * branchCollider.rotation;
                }
            }
            else if (branchVisual != null && !isPetalSurface) {
                ownsBranchCollider = true;
                branchCollider = new GameObject("Branch collision").transform;
                branchCollider.gameObject.layer = gameObject.layer;
                branchCollider.SetParent(support.transform, false);
                var shaft = branchCollider.gameObject.AddComponent<CapsuleCollider>();
                shaft.direction = 2; shaft.radius = branchColliderRadius;
            }
            SyncSupport();
        }
        void SyncSupport()
        {
            if (support == null) return;
            support.transform.position = SupportPosition;
            if (isPetalSurface && branchVisual != null) support.transform.rotation = branchVisual.rotation * supportRotationLocal;
            if (branchCollider == null || branchVisual == null) return;
            if (!ownsBranchCollider) {
                // Follow the branch without replacing the collider pose saved in the Inspector.
                branchCollider.position = branchVisual.TransformPoint(authoredColliderLocal);
                branchCollider.rotation = branchVisual.rotation * authoredColliderRotation;
                return;
            }
            Vector3 start = branchVisual.position, end = branchVisual.TransformPoint(branchEndLocal);
            Vector3 direction = end - start;
            branchCollider.position = (start + end) * 0.5f;
            if (direction.sqrMagnitude > 0.000001f) branchCollider.rotation = Quaternion.LookRotation(direction);
            if (ownsBranchCollider) branchCollider.GetComponent<CapsuleCollider>().height = direction.magnitude + 2f * branchColliderRadius;
        }
        void OnDestroy()
        {
            if (ownsBranchCollider && !ownsSupport && branchCollider != null) {
                if (Application.isPlaying) Destroy(branchCollider.gameObject); else DestroyImmediate(branchCollider.gameObject);
            }
            if (ownsSupport && support != null) {
                if (Application.isPlaying) Destroy(support); else DestroyImmediate(support);
            }
        }
        public void ApplyPressure(float pressure) => ApplyPressure(pressure, Time.deltaTime);
        public void ApplyPressure(float pressure, float deltaTime)
        {
            if (branchVisual == null) return;
            // A damped spring gives one small landing dip and a settling rebound.
            float remaining = Mathf.Clamp(deltaTime, 0f, 0.1f);
            while (remaining > 0f) {
                float dt = Mathf.Min(remaining, 1f / 120f);
                float omega = Mathf.PI * 2f * Mathf.Max(0.1f, springFrequency);
                loadVelocity += (omega * omega * (Mathf.Clamp01(pressure) - load) - 2f * dampingRatio * omega * loadVelocity) * dt;
                load += loadVelocity * dt; remaining -= dt;
            }
            if (!deformed || Quaternion.Angle(branchVisual.localRotation, appliedRotation) > 0.001f)
                baseRotation = branchVisual.localRotation;
            Vector3 direction = branchVisual.TransformVector(contactCached ? branchContactLocal : Vector3.forward);
            Vector3 axis = branchVisual.InverseTransformDirection(Vector3.Cross(direction.normalized, Vector3.down));
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.right;
            appliedRotation = baseRotation * Quaternion.AngleAxis(load * landingBendDegrees, axis.normalized);
            branchVisual.localRotation = appliedRotation; deformed = true;
            SyncSupport();
        }
        public void Restore()
        {
            if (deformed && branchVisual != null && Quaternion.Angle(branchVisual.localRotation, appliedRotation) < 0.001f)
                branchVisual.localRotation = baseRotation;
            deformed = false; load = loadVelocity = 0f;
            SyncSupport();
        }
        void OnEnable() { if (support != null && (Application.isPlaying || ownsSupport)) support.SetActive(true); }
        void OnDisable() { Restore(); if (support != null && (Application.isPlaying || ownsSupport)) support.SetActive(false); }
    }
}
