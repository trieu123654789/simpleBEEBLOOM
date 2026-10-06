// =============================================================================
// BeeGameStats - Du lieu gameplay ma HUD con ong hien thi.
// BeeGameStats - Dữ liệu gameplay mà HUD con ong hiển thị.
//
// VAI TRO / ROLE
//   Day la "mot nguon su that" cho HUD: so mat da giao ve to, muc mat hoa
//   dang giu, tien do nhiem vu, co nguy hiem (ong dich gan).
//   HUD chi DOC (poll) moi frame -> khong can event, khong lo miss event.
//
// VI SAO TACH RIENG
//   HUD khong nen biet ve hoa/to ong/ong dich. Sau nay he thong thu mat
//   chi can goi AddHoney()/SetDanger() la HUD tu cap nhat dung.
//
// API NHANH / QUICK API
//   AddNectar01(0.1f)  -> nhat mat hoa
//   AddHoney(1)        -> giao mat ve to
//   SetDanger(true)    -> ong dich gan -> HUD hien banner do
// =============================================================================
using UnityEngine;

public class BeeGameStats : MonoBehaviour
{
    // =========================================================================
    // HONEY / MAT ONG (binh mat goc tren trai)
    // =========================================================================
    [Header("Honey / Mật ong")]
    [Tooltip("Muc tieu so mat can giao (anh mau ghi /20).")]
    public int honeyGoal = 20;

    [Tooltip("So mat da giao ve to.")]
    public int honeyDelivered;

    // =========================================================================
    // NECTAR / MAT HOA (thanh goc duoi trai)
    // =========================================================================
    [Header("Nectar / Mật hoa")]
    [Tooltip("Luong mat hoa dang giu, chuan hoa 0-1.")]
    [Range(0f, 1f)]
    public float nectar01 = 0.45f;

    [Tooltip("Tu dong doi mat hoa thanh mat ong khi day binh.")]
    public bool autoDeliverWhenFull = true;

    // =========================================================================
    // QUEST / NHIEM VU (panel goc tren trai)
    // =========================================================================
    [Header("Quest / Nhiệm vụ")]
    public string questTitle = "Thu thập mật hoa";
    public string questSubtitle = "Đưa mật về tổ";

    // =========================================================================
    // DANGER / NGUY HIEM (banner do ben phai)
    // =========================================================================
    [Header("Danger / Kẻ thù")]
    [Tooltip("Co nguy hiem - bat khi ong dich trong tam tan cong.")]
    public bool dangerActive;

    [Tooltip("Thoi gian giu banner sau lan SetDanger(true) cuoi (giay).")]
    public float dangerHoldSeconds = 3f;

    private float _dangerTimer;

    // =========================================================================
    // PUBLIC API / HAM CONG KHAI
    // =========================================================================

    /// <summary>Nhat them mat hoa (0-1). Day binh thi tu dang ky giao mat.</summary>
    public void AddNectar01(float amount)
    {
        if (amount <= 0f) return;

        nectar01 = Mathf.Clamp01(nectar01 + amount);

        if (autoDeliverWhenFull && nectar01 >= 1f)
        {
            AddHoney(1);
            nectar01 = 0f;
        }
    }

    /// <summary>Giao them mat ve to. Dat muc tieu thi tra ve true (hoan thanh).</summary>
    public bool AddHoney(int amount)
    {
        if (amount <= 0) return false;

        honeyDelivered = Mathf.Clamp(honeyDelivered + amount, 0, honeyGoal);
        return IsComplete;
    }

    /// <summary>Bat/tat canh bao nguy hiem trong dangerHoldSeconds giay.</summary>
    public void SetDanger(bool active)
    {
        dangerActive = active;
        if (active) _dangerTimer = dangerHoldSeconds;
    }

    /// <summary>Da giao du mat chua.</summary>
    public bool IsComplete => honeyDelivered >= honeyGoal;

    /// <summary>Tien do 0-1 (dung cho thanh tien do nhiem vu).</summary>
    public float Progress01 => honeyGoal > 0 ? Mathf.Clamp01((float)honeyDelivered / honeyGoal) : 0f;

    /// <summary>Bat dau man moi / thu lai.</summary>
    public void ResetRun()
    {
        honeyDelivered = 0;
        nectar01 = 0f;
        dangerActive = false;
        _dangerTimer = 0f;
    }

    private void Update()
    {
        // Banner do tu tat sau khi het "hieu luc" -> giong canh bao tam thoi.
        if (!dangerActive || _dangerTimer <= 0f) return;

        _dangerTimer -= Time.deltaTime;
        if (_dangerTimer <= 0f) dangerActive = false;
    }
}
