using UnityEngine;

// Swept movement uses this frame's displacement, including ascent and sideways
// movement. Slide on contact instead of teleporting onto the top of a bank.
public sealed class BeeFlightCollision
{
    private readonly RaycastHit[] hits = new RaycastHit[64];

    private static bool Ignore(Collider c, Transform bee, Transform rig)
        => c == null || BeeMapSurfaces.IsLegacyWalkingProxy(c.transform) || c.transform == bee || c.transform.IsChildOf(bee)
        || (rig != null && (c.transform == rig || c.transform.IsChildOf(rig)));

    public Vector3 Move(Transform bee, Transform rig, Vector3 target, float radius,
        float clearance, int mask, out bool grounded, out bool ceiling)
    {
        grounded = ceiling = false;
        const float skin = 0.005f;
        Vector3 position = bee.position;
        Vector3 remaining = target - position;
        // A vertically offset sphere preserves both the saved horizontal radius
        // and the saved ground clearance.
        Vector3 offset = Vector3.up * (radius - Mathf.Max(radius, clearance));
        // Recover a shallow saved/spawn penetration of actual terrain only.
        // Never snap up through house roofs or bridges.
        int floors = Physics.RaycastNonAlloc(position + Vector3.up * 2f, Vector3.down,
            hits, 2f + Mathf.Max(radius, clearance) + skin, mask, QueryTriggerInteraction.Ignore);
        float floor = float.NegativeInfinity;
        for (int i = 0; i < floors; i++)
        {
            var h = hits[i];
            if (Ignore(h.collider, bee, rig) || h.normal.y < 0.3f) continue;
            if (!BeeMapSurfaces.IsGround(h.collider.name) && !BeeMapSurfaces.IsWater(h.collider.name)) continue;
            floor = Mathf.Max(floor, h.point.y + Mathf.Max(radius, clearance) + skin);
        }
        if (position.y < floor) { position.y = floor; grounded = true; }

        for (int step = 0; step < 4 && remaining.sqrMagnitude > 0.0000001f; step++)
        {
            float distance = remaining.magnitude;
            Vector3 direction = remaining / distance;
            int count = Physics.SphereCastNonAlloc(position + offset, radius, direction,
                hits, distance + skin, mask, QueryTriggerInteraction.Ignore);
            RaycastHit[] candidates = hits;
            if (count == hits.Length)
            {
                candidates = Physics.SphereCastAll(position + offset, radius, direction,
                    distance + skin, mask, QueryTriggerInteraction.Ignore);
                count = candidates.Length;
            }
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;
            for (int i = 0; i < count; i++)
            {
                var h = candidates[i];
                if (Ignore(h.collider, bee, rig) || Vector3.Dot(direction, h.normal) >= -0.0001f) continue;
                if (h.distance < nearest) { nearest = h.distance; best = h; }
            }
            if (float.IsPositiveInfinity(nearest)) { position += remaining; break; }
            float travel = Mathf.Clamp(nearest - skin, 0f, distance);
            position += direction * travel;
            remaining = Vector3.ProjectOnPlane(remaining - direction * travel, best.normal);
            grounded |= best.normal.y > 0.3f;
            ceiling |= best.normal.y < -0.3f;
        }

        return position;
    }
}
