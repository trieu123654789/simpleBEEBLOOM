// =============================================================================
// BeeAltitudeHold - Giu con ong bay SAT mat nuoc / mat dat nhu anh mau.
// BeeAltitudeHold - Giữ con ong bay SÁT mặt nước / mặt đất như ảnh mẫu.
//
// VI SAO CAN
//   Trong anh mau, mat nuoc nam ngay duoi tam nhin: nguoi choi thay hoa sen,
//   la sen rat gan, horizon o khoang 45% man hinh. Muon co khung hinh do thi
//   ong phai bay thap (~1.5m) chu khong bay cao tuy y. Script nay:
//     1. Do do cao that bang raycast xuong duoi moi frame.
//     2. Chan cung: khong cho xuyen xuong duoi minClearance (khong lan xuong nuoc).
//     3. Khi LUOI (khong dap canh) va dang bay ngang -> ha dan ve do cao
//        cruiseAltitude => cam giac "luot sat mat nuoc".
//
// THU TU CHAY / ORDER
//   BeeController di chuyen trong Update(), script nay chinh lai trong
//   LateUpdate() -> vi tri dung truoc khi render, khong bi giat.
//
// LUU Y
//   Raycast moi frame chi 1 tia -> rat re. Nhung nho bo lop khong can thiet
//   (UI, trigger) ra khoi surfaceMask de khong bat nham collider la.
// =============================================================================
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BeeController))]
public class BeeAltitudeHold : MonoBehaviour
{
    // =========================================================================
    // SURFACE / BE MAT
    // =========================================================================
    [Header("Surface / Bề mặt")]
    [Tooltip("Lop duoc coi la 'mat nuoc / mat dat'. Bo UI va trigger ra.")]
    public LayerMask surfaceMask = ~0;

    [Tooltip("Bat dau tia do tu cao hon tam ong bao nhieu (m) de tranh tu cham chinh minh.")]
    public float probeLift = 0.05f;

    [Tooltip("Bo qua collider cua chinh XR rig (nguoi choi) khi do dat.")]
    public bool ignorePlayerRig = true;

    [Tooltip("XR Origin. De trong tu tim tu camera chinh.")]
    public Transform playerRig;

    // =========================================================================
    // ALTITUDE / DO CAO
    // =========================================================================
    [Header("Altitude / Độ cao")]
    [Tooltip("Do cao 'luot' mong muon so voi mat nuoc (m). Anh mau ~1.2-1.8m.")]
    public float cruiseAltitude = 1.5f;

    [Tooltip("Khoang cach toi thieu phai giu (m). Duoi muc nay bi day len.")]
    public float minClearance = 0.45f;

    [Tooltip("Toc do ha do cao khi luoi (m/s). Nho = luot moi, lon = tut nhanh.")]
    public float descendSpeed = 0.9f;

    [Tooltip("Toc do day len khi cham san (m/s).")]
    public float pushSpeed = 8f;

    [Tooltip("Chi tu ha do cao khi huong bay gan nhu nam ngang (do).")]
    public float levelPitchLimit = 20f;

    [Tooltip("Do cao toi da duoc phep bay (m). 0 = khong gioi han.")]
    public float hardCeiling = 0f;

    [Tooltip("Bat/tat viec tu ha do cao ve cruiseAltitude khi luoi.")]
    public bool autoCruiseWhenGliding = true;

    // =========================================================================
    // DEBUG / KIEM TRA
    // =========================================================================
    [Header("Debug")]
    [Tooltip("Ve tia do trong Scene view de can chinh.")]
    public bool drawDebugRay;

    // =========================================================================
    // INTERNAL / NOI BO
    // =========================================================================
    private BeeController _bee;
    private readonly RaycastHit[] _hits = new RaycastHit[8];

    /// <summary>Do cao hien tai so voi mat nuoc (m). -1 = khong thay mat.</summary>
    public float CurrentAltitude { get; private set; } = -1f;

    /// <summary>Y cua mat nuoc / mat dat ben duoi. / Surface height below.</summary>
    public float CurrentSurfaceY { get; private set; }

    private void Awake()
    {
        _bee = GetComponent<BeeController>();
    }

    private void LateUpdate()
    {
        if (_bee != null && _bee.IsPositionHolding) return;
        if (!TryProbeSurface(out float surfaceY))
        {
            CurrentAltitude = -1f;
            return;
        }

        CurrentSurfaceY = surfaceY;
        CurrentAltitude = transform.position.y - surfaceY;

        float stepPerFrame = pushSpeed * Time.deltaTime;

        // --- 1. San cung: khong cho lan xuong duoi minClearance ---
        float floorY = surfaceY + minClearance;
        if (transform.position.y < floorY)
        {
            Vector3 position = transform.position;
            position.y = Mathf.MoveTowards(position.y, floorY, stepPerFrame);
            transform.position = position;
        }

        // --- 2. Tran cung (tuy chon) ---
        if (hardCeiling > 0f)
        {
            float ceilingY = surfaceY + hardCeiling;
            if (transform.position.y > ceilingY)
            {
                Vector3 position = transform.position;
                position.y = Mathf.MoveTowards(position.y, ceilingY, stepPerFrame);
                transform.position = position;
            }
        }

        // --- 3. Luot sat mat nuoc khi dang luoi va bay ngang ---
        if (!autoCruiseWhenGliding || _bee == null || _bee.useNaturalFall || _bee.enableRhythmFlight) return;
        if (_bee.IsFlapping) return;
        if (Mathf.Abs(_bee.Pitch) > levelPitchLimit) return;   // nguoi choi dang chu dong len/xuong
        if (CurrentAltitude <= cruiseAltitude) return;

        Vector3 glide = transform.position;
        glide.y = Mathf.MoveTowards(glide.y, surfaceY + cruiseAltitude,
            descendSpeed * Time.deltaTime);
        transform.position = glide;
    }

    // =========================================================================
    // TryProbeSurface - Raycast xuong duoi, tra ve Y cua mat gan nhat.
    //   Bo qua: chinh con ong, XR rig, va trigger.
    // =========================================================================
    private bool TryProbeSurface(out float surfaceY)
    {
        surfaceY = 0f;

        Vector3 origin = transform.position + Vector3.up * probeLift;
        float maxDistance = Mathf.Max(Mathf.Max(cruiseAltitude, minClearance), hardCeiling) + 40f;

        int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, maxDistance,
            surfaceMask, QueryTriggerInteraction.Ignore);
        if (count <= 0) return false;

        float bestY = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider collider = _hits[i].collider;
            if (collider == null) continue;
            if (BeeMapSurfaces.IsLegacyWalkingProxy(collider.transform)) continue;
            if (IsSelf(collider.transform)) continue;
            if (ignorePlayerRig && playerRig != null && collider.transform.IsChildOf(playerRig)) continue;

            if (_hits[i].point.y > bestY) bestY = _hits[i].point.y;
        }

        if (bestY == float.NegativeInfinity) return false;

        surfaceY = bestY;
        if (drawDebugRay)
        {
            Debug.DrawLine(origin, new Vector3(origin.x, bestY, origin.z), Color.cyan);
        }
        return true;
    }

    /// <summary>Collider co thuoc ve con ong (BeeRoot) khong.</summary>
    private bool IsSelf(Transform candidate)
    {
        return candidate == transform || candidate.IsChildOf(transform) || transform.IsChildOf(candidate);
    }
}
