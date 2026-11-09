using System;
using UnityEngine;

// Uses existing frond bones at runtime. Meshes, bind poses and shaders stay authored.
[DisallowMultipleComponent, DefaultExecutionOrder(300)]
public sealed class BeeFlightFoliage : MonoBehaviour
{
    [Serializable] public sealed class Frond { public Transform bone, tip; }
    public BeeController bee;
    public Frond[] fronds = new Frond[0];
    [Min(0.1f)] public float responseRadius = 3f;
    [Range(0f, 2f)] public float maxBendDegrees = 0.8f;
    [Min(1)] public int maxActiveTargets = 8;
    [Min(0.1f)] public float responseSpeed = 5f;
    Quaternion[] rest, applied;
    float[] weights, distances;
    bool[] changed, selected;
    void OnEnable()
    {
        if (bee == null) bee = GetComponent<BeeController>();
        int n = fronds.Length;
        rest = new Quaternion[n]; applied = new Quaternion[n]; weights = new float[n]; distances = new float[n];
        changed = new bool[n]; selected = new bool[n];
    }
    void Update() { RestoreRotations(); }
    void LateUpdate()
    {
        if (bee == null || rest == null) return;
        bool flying = !bee.IsFlowerPerched && !bee.ExternalMovementLocked && !LetItBee.UI.BeeMenuInput.Blocked;
        Vector3 player = bee.transform.position;
        for (int i = 0; i < rest.Length; i++) {
            var f = fronds[i]; selected[i] = false; distances[i] = float.PositiveInfinity;
            if (f == null || f.bone == null || !f.bone.gameObject.activeInHierarchy) continue;
            Vector3 a = f.bone.position, ab = (f.tip != null ? f.tip.position : a) - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(player - a, ab) / ab.sqrMagnitude) : 0f;
            distances[i] = Vector3.Distance(player, a + ab * t);
        }
        for (int slot = 0; flying && slot < maxActiveTargets; slot++) {
            int nearest = -1; float distance = responseRadius;
            for (int i = 0; i < distances.Length; i++)
                if (!selected[i] && distances[i] < distance) { nearest = i; distance = distances[i]; }
            if (nearest < 0) break;
            selected[nearest] = true;
        }
        for (int i = 0; i < rest.Length; i++) {
            var f = fronds[i]; if (f == null || f.bone == null) continue;
            float target = selected[i] ? Mathf.Pow(1f - distances[i] / responseRadius, 2f) * Mathf.Clamp01(bee.Speed / 3f) : 0f;
            weights[i] = Mathf.Lerp(weights[i], target, 1f - Mathf.Exp(-responseSpeed * Time.deltaTime));
            if (weights[i] < 0.001f) continue;
            var t = f.bone; Vector3 away = Vector3.ProjectOnPlane(t.position - player, Vector3.up);
            if (away.sqrMagnitude < 0.001f) away = bee.transform.forward;
            Vector3 axis = t.InverseTransformDirection(Vector3.Cross(Vector3.up, away.normalized));
            rest[i] = t.localRotation;
            applied[i] = rest[i] * Quaternion.AngleAxis(weights[i] * maxBendDegrees, axis.normalized);
            t.localRotation = applied[i]; changed[i] = true;
        }
    }
    void RestoreRotations()
    {
        if (changed == null) return;
        for (int i = 0; i < changed.Length; i++) {
            if (!changed[i] || fronds[i] == null || fronds[i].bone == null) continue;
            var t = fronds[i].bone;
            if (Quaternion.Angle(t.localRotation, applied[i]) < 0.001f) t.localRotation = rest[i];
            changed[i] = false;
        }
    }
    void OnDisable() { RestoreRotations(); }
}
