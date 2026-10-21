using UnityEngine;
using UnityEngine.XR;

// World-space feedback only: never writes movement, HUD, camera or head pose.
[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
public sealed class BeeSurfaceSkim : MonoBehaviour
{
    private const int PoolSize = 16;
    private readonly LineRenderer[] lines = new LineRenderer[PoolSize];
    private readonly float[] ages = new float[PoolSize];
    private readonly bool[] water = new bool[PoolSize];
    private readonly Vector3[] centers = new Vector3[PoolSize];
    private readonly Quaternion[] rotations = new Quaternion[PoolSize];
    private readonly RaycastHit[] hits = new RaycastHit[32];
    private BeeController bee;
    private BeeCameraRig rig;
    private Material material;
    private GameObject pool;
    private Vector3 previous;
    private float emission, haptic;
    private int cursor;
    public float Intensity { get; private set; }
    public bool OverWater { get; private set; }

    private void Awake()
    {
        bee = GetComponent<BeeController>();
        rig = GetComponent<BeeCameraRig>();
        previous = transform.position;
    }

    public static float EvaluateIntensity(float distance, float speed, float range, float minimum)
        => Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, range))
        * Mathf.Clamp01((speed - minimum) / 3f);

    private void EnsurePool()
    {
        if (pool != null) return;
        var shader = Resources.Load<Shader>("BeeSurfaceSkim");
        if (shader == null) return;
        material = new Material(shader);
        pool = new GameObject("Bee surface skim (runtime)");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pool, gameObject.scene);
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("Surface wake");
            go.transform.SetParent(pool.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 20;
            line.widthMultiplier = 0.018f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            lines[i] = line;
            ages[i] = 1f;
        }
    }

    private bool Probe(Vector3 direction, ref RaycastHit nearest, ref float distance)
    {
        int count = Physics.RaycastNonAlloc(transform.position, direction, hits,
            bee.skimDistance, bee.groundMask.value != 0 ? bee.groundMask.value : Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (BeeMapSurfaces.IsLegacyWalkingProxy(hit.transform)) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (rig != null && rig.playerRig != null && hit.transform.IsChildOf(rig.playerRig)) continue;
            if (hit.distance >= distance) continue;
            nearest = hit; distance = hit.distance; found = true;
        }
        return found;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 delta = transform.position - previous;
        previous = transform.position;
        float speed = dt > 0f ? delta.magnitude / dt : 0f;
        bool active = bee != null && bee.enabled && bee.enableSurfaceSkim && !bee.ExternalMovementLocked
            && !bee.IsPositionHolding && speed < 30f && speed > bee.skimMinSpeed;
        RaycastHit hit = default;
        float distance = float.PositiveInfinity;
        bool found = false;
        if (active)
        {
            found = Probe(Vector3.down, ref hit, ref distance);
            found |= Probe(delta.normalized, ref hit, ref distance);
            Vector3 side = Vector3.Cross(Vector3.up, delta.normalized);
            if (side.sqrMagnitude > 0.01f)
            {
                found |= Probe(side.normalized, ref hit, ref distance);
                found |= Probe(-side.normalized, ref hit, ref distance);
            }
        }
        Intensity = found ? EvaluateIntensity(distance, speed, bee.skimDistance, bee.skimMinSpeed) : 0f;
        OverWater = found && BeeMapSurfaces.IsWater(hit.collider.name);
        emission -= dt; haptic -= dt;
        if (Intensity > 0.03f)
        {
            EnsurePool();
            if (pool != null && emission <= 0f)
            {
                int index = cursor++ % PoolSize;
                centers[index] = hit.point + hit.normal * 0.025f;
                rotations[index] = Quaternion.FromToRotation(Vector3.up, hit.normal);
                water[index] = OverWater;
                ages[index] = 0f;
                lines[index].enabled = true;
                emission = Mathf.Lerp(0.16f, 0.06f, Intensity);
            }
            if (haptic <= 0f && bee.skimHapticStrength > 0f)
            {
                Pulse(XRNode.LeftHand); Pulse(XRNode.RightHand);
                haptic = 0.2f;
            }
        }
        if (pool == null) return;
        for (int i = 0; i < PoolSize; i++)
        {
            ages[i] += dt;
            float t = ages[i] / 0.7f;
            var line = lines[i];
            line.enabled = t < 1f;
            if (!line.enabled) continue;
            Color color = water[i] ? new Color(0.65f, 0.93f, 1f, 0.5f * (1f - t))
                : new Color(0.9f, 0.83f, 0.55f, 0.3f * (1f - t));
            line.startColor = line.endColor = color;
            line.loop = water[i];
            for (int j = 0; j < 20; j++)
            {
                float angle = j / 19f * (water[i] ? 2f * Mathf.PI : Mathf.PI);
                float radius = 0.12f + t * (water[i] ? 0.65f : 0.35f);
                line.SetPosition(j, centers[i] + rotations[i] * new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }

    private void Pulse(XRNode hand)
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);
        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0, bee.skimHapticStrength * Intensity, 0.035f);
    }

    private void OnDisable()
    {
        Intensity = 0f;
        previous = transform.position;
        foreach (var line in lines) if (line != null) line.enabled = false;
        for (int i = 0; i < PoolSize; i++) ages[i] = 1f;
    }

    private void OnDestroy()
    {
        if (pool != null) Destroy(pool);
        if (material != null) Destroy(material);
    }
}
