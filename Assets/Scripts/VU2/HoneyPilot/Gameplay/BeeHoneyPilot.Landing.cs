using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyPilot
    {
        [Header("Petal Landing Input")]
        [Tooltip("Tap left trigger to land or leave a petal. Hold it in flight to brake and hover.")]
        public bool togglePetalLanding;
        [Min(0.1f), Tooltip("Maximum press duration for a landing/takeoff tap. Longer presses only hover.")]
        public float landingTapDuration = 0.25f;
        bool landingRequested, landingButtonDown;
        float landingPressElapsed;
        [Header("Petal Flyby Response")]
        [Min(0.1f)] public float petalFlybyRadius = 2.5f;
        [Range(0f, 0.5f)] public float petalFlybyStrength = 0.3f;
        BeePerchPoint selectedLandingPoint;

        bool SampleLandingInput(bool pressed, float deltaTime)
        {
            if (beeController != null && beeController.ExternalMovementLocked) {
                landingButtonDown = pressed;
                landingPressElapsed = landingTapDuration + 1f;
                return PerchedFlower >= 0;
            }
            if (pressed) {
                if (!landingButtonDown) landingPressElapsed = 0f;
                landingPressElapsed += Mathf.Max(0f, deltaTime);
            }
            bool tap = !pressed && landingButtonDown && landingPressElapsed <= landingTapDuration;
            landingButtonDown = pressed;
            if (tap && !leavingPerch) {
                // A request lasts one interaction step unless a petal actually accepts it.
                landingRequested = PerchedFlower < 0;
                perchArmed = true;
            }
            return landingRequested;
        }

        BeePerchPoint LandingPoint(int flower)
        {
            if (selectedLandingPoint != null && (PerchedFlower < 0 || flower == PerchedFlower)) return selectedLandingPoint;
            var anchor = Perch(flower);
            return anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
        }

        void SelectPetalLanding(ref int flower)
        {
            float best = float.PositiveInfinity;
            selectedLandingPoint = null;
            flower = -1;
            for (int i = 0; i < FlowerCount; i++) {
                var anchor = Perch(i);
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
            for (int i = 0; i < FlowerCount; i++) {
                var anchor = Perch(i);
                var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
                if (root == null) continue;
                var points = root.landingPoints.Length > 0 ? root.landingPoints : new[] { root };
                foreach (var point in points) if (point != null) {
                    float pressure = PerchedFlower == i && point == LandingPoint(i) && IsFlowerSettled ? PetalPressure(point) : 0f;
                    if (PerchedFlower < 0 && beeController != null && !beeController.ExternalMovementLocked
                        && !LetItBee.UI.BeeMenuInput.Blocked) {
                        float proximity = 1f - Mathf.Clamp01(Vector3.Distance(InteractionPosition, point.transform.position) / petalFlybyRadius);
                        pressure = proximity * proximity * Mathf.Clamp01(beeController.Speed / 3f) * petalFlybyStrength;
                    }
                    point.ApplyPressure(pressure);
                }
            }
        }

        void RestorePetals()
        {
            foreach (var anchor in AllFlowerPerches()) {
                var root = anchor != null ? anchor.GetComponent<BeePerchPoint>() : null;
                if (root == null) continue;
                root.Restore();
                foreach (var point in root.landingPoints) if (point != null) point.Restore();
            }
        }
    }
}
