using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

// Runtime scale and floor reference. Saved flight/HUD tuning remains authored data.
[DisallowMultipleComponent, DefaultExecutionOrder(-200), RequireComponent(typeof(BeeController), typeof(BeeCameraRig))]
public sealed class BeeEmbodiment : MonoBehaviour
{
    [Range(0.05f, 1f)] public float playerScale = 0.15f;
    public bool alignFloorAtFirstTracking = true;
    [Range(0f, 1f)] public float hoverViewStrength = 0.35f;
    public bool reducedMotion;
    [Min(1f), Tooltip("Extra continuous-button flight time at miniature scale. Gesture strokes keep their existing cost.")]
    public float continuousFlightEnduranceMultiplier = 1f;
    BeeCameraRig rig;
    BeeController bee;
    BeeAltitudeHold altitude;
    Vector3 originalScale;
    float originalRadius, originalClearance, originalMinimum;
    float originalNearClip;
    bool applied, floorAligned;
    readonly Dictionary<FieldInfo, float> motionBefore = new Dictionary<FieldInfo, float>();
    public float Scale => Mathf.Clamp(playerScale, 0.05f, 1f);
    public float HoverOffset => !reducedMotion && bee != null ? bee.HoldVisualBob * Scale * hoverViewStrength : 0f;

    void Start()
    {
        rig = GetComponent<BeeCameraRig>(); bee = GetComponent<BeeController>();
        altitude = GetComponent<BeeAltitudeHold>();
        if (rig == null || rig.playerRig == null || rig.trackedCamera == null || rig.playerRig == transform || applied) return;
        originalScale = rig.playerRig.localScale;
        rig.playerRig.localScale = originalScale * Scale;
        originalNearClip = rig.trackedCamera.nearClipPlane;
        rig.trackedCamera.nearClipPlane = Mathf.Max(0.003f, originalNearClip * Scale);
        originalRadius = bee.bodyRadius; originalClearance = bee.groundClearance;
        bee.bodyRadius *= Scale; bee.groundClearance *= Scale;
        foreach (string name in new[] { "moveSpeed", "reverseSpeed", "strafeSpeed", "moveAcceleration", "moveDeceleration", "glideDrag",
            "maxAscendSpeed", "maxFallSpeed", "gravity", "fallAcceleration", "comfortFallAcceleration", "comfortMaxFallSpeed",
            "flapLift", "strokeLiftScale", "restGlideSinkSpeed", "restGlideDeceleration", "hoverDescentRate", "skimDistance", "skimMinSpeed" }) {
            var field = typeof(BeeController).GetField(name);
            float value = (float)field.GetValue(bee); motionBefore[field] = value; field.SetValue(bee, value * Scale);
        }
        var drain = typeof(BeeController).GetField("staminaDrainPerSecond");
        motionBefore[drain] = bee.staminaDrainPerSecond;
        bee.staminaDrainPerSecond /= Mathf.Max(1f, continuousFlightEnduranceMultiplier);
        if (altitude != null) { originalMinimum = altitude.minClearance; altitude.minClearance *= Scale; }
        applied = true;
        rig.SnapToBee();
    }
    void OnEnable() { if (rig != null && !applied) Start(); }

    public void AlignTrackedFloor(Vector3 initialHeadOffset)
    {
        if (!applied || floorAligned || !alignFloorAtFirstTracking || rig == null) return;
        // Never use a fallback standing height: wait for real tracking.
        if (initialHeadOffset.y < 0.2f) return;
        float best = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(transform.position + Vector3.up * 0.1f, Vector3.down, 30f,
            bee.groundMask.value != 0 ? bee.groundMask.value : Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(rig.playerRig)
                || BeeMapSurfaces.IsLegacyWalkingProxy(hit.transform)) continue;
            best = Mathf.Max(best, hit.point.y);
        }
        if (float.IsNegativeInfinity(best)) return;
        Vector3 position = transform.position;
        position.y = best + rig.playerRig.TransformVector(initialHeadOffset).y - transform.TransformVector(rig.eyeOffset).y;
        position.y = Mathf.Max(best + bee.groundClearance, position.y);
        transform.position = position; floorAligned = true;
    }

    void OnDisable()
    {
        if (!applied) return;
        if (rig != null && rig.playerRig != null) rig.playerRig.localScale = originalScale;
        if (rig != null && rig.trackedCamera != null) rig.trackedCamera.nearClipPlane = originalNearClip;
        if (bee != null) { bee.bodyRadius = originalRadius; bee.groundClearance = originalClearance; }
        if (bee != null) foreach (var item in motionBefore) item.Key.SetValue(bee, item.Value);
        motionBefore.Clear();
        if (altitude != null) altitude.minClearance = originalMinimum;
        applied = floorAligned = false;
    }
}
