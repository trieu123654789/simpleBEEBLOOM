using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyPilot
    {
        [Header("Petal Landing Input")]
        [Tooltip("Press left trigger once to land; press again to leave. Replaces airborne position hold.")]
        public bool togglePetalLanding;
        bool landingRequested, landingButtonDown;
        BeePerchPoint selectedLandingPoint;

        bool SampleLandingInput(bool pressed)
        {
            bool rising = pressed && !landingButtonDown;
            landingButtonDown = pressed;
            if (rising && !leavingPerch) {
                landingRequested = !landingRequested;
                perchArmed = true;
            }
            return landingRequested;
        }

        BeePerchPoint LandingPoint(int flower)
        {
            if (selectedLandingPoint != null && (PerchedFlower < 0 || flower == PerchedFlower)) return selectedLandingPoint;
            var anchor = flower == 0 ? flowerAPerch : flowerBPerch;
            return anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
        }

        void SelectPetalLanding(ref int flower)
        {
            float best = float.PositiveInfinity;
            selectedLandingPoint = null;
            flower = -1;
            for (int i = 0; i < 2; i++) {
                var anchor = i == 0 ? flowerAPerch : flowerBPerch;
                var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
                if (root == null) continue;
                var points = root.landingPoints.Length > 0 ? root.landingPoints : new[] { root };
                foreach (var point in points) {
                    if (point == null || !point.isActiveAndEnabled) continue;
                    float distance = Vector3.Distance(InteractionPosition, point.ViewPosition);
                    if (distance > point.approachRadius || distance >= best || !HasLineOfSight(InteractionPosition, point.ViewPosition)) continue;
                    best = distance; flower = i; selectedLandingPoint = point;
                }
            }
        }

        void StepPetalResponses()
        {
            for (int i = 0; i < 2; i++) {
                var anchor = i == 0 ? flowerAPerch : flowerBPerch;
                var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
                if (root == null) continue;
                var points = root.landingPoints.Length > 0 ? root.landingPoints : new[] { root };
                foreach (var point in points) if (point != null)
                    point.ApplyPressure(PerchedFlower == i && point == LandingPoint(i) && IsFlowerSettled ? PetalPressure(point) : 0f);
            }
        }

        void RestorePetals()
        {
            foreach (var anchor in new[] { flowerAPerch, flowerBPerch }) {
                var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
                if (root == null) continue;
                root.Restore();
                foreach (var point in root.landingPoints) if (point != null) point.Restore();
            }
        }
    }
}
