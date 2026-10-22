using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyPilot
    {
        [Header("Flower Surface Walking")]
        public bool enablePetalWalking;
        [Min(0.01f), Tooltip("Walking speed in world metres per second; independent of XR scale.")]
        public float petalWalkSpeed = 0.22f;
        [Min(0.01f)] public float walkAcceleration = 0.8f;
        [Min(0.02f)] public float surfaceProbeDistance = 0.35f;
        [Range(0f, 0.03f)] public float edgeMargin = 0.008f;
        [Min(0.005f)] public float maxSurfaceStepHeight = 0.04f;
        public bool IsWalkingOnFlower => surfaceWalking && walkVelocity.sqrMagnitude > 0.0009f;
        bool surfaceWalking;
        Vector3 walkVelocity, walkingContact;
        int walkingTriangle;
        Vector3 walkingBarycentric;
        BeeFlowerSurface walkingSurface;

        BeeFlowerSurface FlowerSurface(int index)
        {
            var anchor = index == 0 ? flowerAPerch : flowerBPerch;
            return anchor != null ? anchor.GetComponent<BeeFlowerSurface>() : null;
        }

        void RefreshFlowerSurfaces(float dt)
        {
            if (!enablePetalWalking || beeRoot == null) return;
            for (int i = 0; i < 2; i++) {
                var surface = FlowerSurface(i);
                if (surface != null && (PerchedFlower == i || Vector3.Distance(beeRoot.position, Head(i).position) < 3f))
                    surface.Refresh(dt);
            }
        }

        bool StepSurfaceWalking(float dt, Vector3 input)
        {
            if (!enablePetalWalking || !togglePetalLanding || !bodyPerch || leavingPerch) return false;
            var surface = FlowerSurface(PerchedFlower);
            if (surface == null || !surface.Ready) return false;
            if (!surfaceWalking) {
                if (!IsFlowerSettled) return false;
                var point = LandingPoint(PerchedFlower);
                RaycastHit entry;
                if (point == null || !surface.Sample(point.SupportPosition, surfaceProbeDistance, out entry)) return false;
                walkingContact = entry.point; walkingTriangle = entry.triangleIndex; walkingBarycentric = entry.barycentricCoordinate;
                walkingSurface = surface;
                surfaceWalking = true; walkVelocity = Vector3.zero;
            }
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            walkVelocity = Vector3.MoveTowards(walkVelocity, Vector3.ClampMagnitude(input, 1f) * petalWalkSpeed,
                walkAcceleration * dt);
            Vector3 contact = walkingSurface.FollowContact(walkingTriangle, walkingBarycentric, walkingContact);
            int previousTriangle = walkingTriangle;
            Vector3 previousBarycentric = walkingBarycentric;
            RaycastHit support;
            // Follow deformation only where there is a real surface below the last safe position.
            if (walkingSurface.Sample(contact, surfaceProbeDistance, out support)) contact = support.point;
            Vector3 passiveMotion = contact - walkingContact;
            Vector3 delta = walkVelocity * dt;
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.008f));
            Vector3 step = delta / steps;
            for (int i = 0; i < steps; i++) {
                Vector3 proposed = contact + step;
                if (WalkableStep(proposed, contact, out support)) RememberContact(support, ref contact);
                else {
                    // Slide along an edge; an unsupported step never releases the landing latch.
                    bool moved = false;
                    if (Mathf.Abs(step.x) > 0.000001f && WalkableStep(contact + Vector3.right * step.x, contact, out support)) { RememberContact(support, ref contact); moved = true; }
                    if (Mathf.Abs(step.z) > 0.000001f && WalkableStep(contact + Vector3.forward * step.z, contact, out support)) { RememberContact(support, ref contact); moved = true; }
                    if (!moved) { walkVelocity = Vector3.zero; break; }
                }
            }
            walkingContact = contact;
            // Cancel flower spring motion at the eye, but keep deliberate walking movement visible.
            if (perchRig != null) perchRig.PerchViewOffset -= passiveMotion;
            Vector3 target = contact + Vector3.up * (Mathf.Max(beeController.BodyRadius, beeController.groundClearance) + 0.006f);
            // A horizontal sweep prevents walking through unrelated map obstacles.
            if (beeController.groundCollision) {
                Vector3 swept = beeController.ResolveGroundCollision(target, walkVelocity);
                if (Vector3.ProjectOnPlane(swept - target, Vector3.up).sqrMagnitude > 0.000004f) {
                    walkingTriangle = previousTriangle; walkingBarycentric = previousBarycentric;
                    walkingContact = walkingSurface.FollowContact(walkingTriangle, walkingBarycentric, walkingContact);
                    target = walkingContact + Vector3.up * (Mathf.Max(beeController.BodyRadius, beeController.groundClearance) + 0.006f);
                    walkVelocity = Vector3.zero;
                } else target.y = Mathf.Max(target.y, swept.y);
            }
            beeRoot.position = perchAnchor = target;
            perchViewTarget = perchRig != null ? perchRig.PerchViewOffset : Vector3.zero;
            previousPerchDistance = perchBlockedTime = 0f;
            UpdateLoadedPetal();
            return true;
        }

        bool WalkableStep(Vector3 proposed, Vector3 current, out RaycastHit support)
        {
            return walkingSurface.Supports(proposed, surfaceProbeDistance, edgeMargin, out support)
                && Mathf.Abs(support.point.y - current.y) <= maxSurfaceStepHeight;
        }

        void RememberContact(RaycastHit hit, ref Vector3 contact)
        {
            contact = hit.point; walkingTriangle = hit.triangleIndex; walkingBarycentric = hit.barycentricCoordinate;
        }

        void UpdateLoadedPetal()
        {
            var anchor = PerchedFlower == 0 ? flowerAPerch : flowerBPerch;
            var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
            if (root == null) return;
            float closest = float.PositiveInfinity;
            foreach (var point in root.landingPoints) if (point != null) {
                float distance = Vector3.ProjectOnPlane(point.SupportPosition - walkingContact, Vector3.up).sqrMagnitude;
                if (distance < closest) { closest = distance; selectedLandingPoint = point; }
            }
        }

        float PetalPressure(BeePerchPoint point)
        {
            if (!surfaceWalking) return 1f;
            Vector3 origin = point.branchVisual != null ? point.branchVisual.position : Head(PerchedFlower).position;
            float reference = Vector3.ProjectOnPlane(point.SupportPosition - origin, Vector3.up).magnitude;
            return Mathf.Clamp01(Vector3.ProjectOnPlane(walkingContact - origin, Vector3.up).magnitude / Mathf.Max(0.05f, reference));
        }

        void StopSurfaceWalking()
        {
            surfaceWalking = false; walkingSurface = null; walkVelocity = Vector3.zero;
        }
    }
}
