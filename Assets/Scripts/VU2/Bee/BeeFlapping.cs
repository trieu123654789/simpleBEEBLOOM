// =============================================================================
// BeeFlapping - Visual + audio feedback for the bee's wing beat.
// BeeFlapping - Hiệu ứng hình ảnh và âm thanh cho động tác vỗ cánh.
//
// COST NOTE / GHI CHÚ CHI PHÍ:
//   Dự án KHÔNG cài com.unity.animation.rigging. Vì vậy cánh được xoay bằng
//   transform quanh trục local Z thay vì dùng rig. Rẻ và ổn định trên Quest.
//
// HIERARCHY EXPECTED / CẤU TRÚC CẦN CÓ:
//   BeeRoot (BeeController + BeeFlapping)
//   ├── Body
//   │   ├── Wing_L   <- gán vào trường wingLeft
//   │   └── Wing_R   <- gán vào trường wingRight
//   └── CameraOffset <- gắn Main Camera của XR vào đây
//
// Để trống wingLeft/wingRight thì script tự tìm theo tên cho tiện.
// =============================================================================
using UnityEngine;

public class BeeFlapping : MonoBehaviour
{
    // =========================================================================
    // WING TRANSFORMS / CÁNH
    // =========================================================================
    [Header("Wings / Cánh")]
    [Tooltip("Cánh trái. Để trống sẽ tự tìm child tên 'Wing_L'.")]
    public Transform wingLeft;

    [Tooltip("Cánh phải. Để trống sẽ tự tìm child tên 'Wing_R'.")]
    public Transform wingRight;

    [Tooltip("Trục xoay của cánh (local). Z = vồ cánh lên/xuống.")]
    public Vector3 wingAxis = Vector3.forward;

    // =========================================================================
    // FLAP SETTINGS / CÀI ĐẶT ĐẬP CÁNH
    // =========================================================================
    [Header("Flap / Đập cánh")]
    [Tooltip("Số vòng đập cánh mỗi giây khi bay tối đa.")]
    public float flapFrequency = 12f;

    [Tooltip("Góc vồ cánh (độ).")]
    public float flapAngle = 55f;

    [Tooltip("Góc cánh nghỉ khi không đập (độ) - cánh dang rộng.")]
    public float restAngle = 12f;

    // =========================================================================
    // AUTO FLAP / TỰ VỖ CÁNH - giúp nhìn giống ong thật
    // =========================================================================
    [Header("Auto flap / Tu vô canh (on gia)")]
    [Tooltip("Tu vô cánh liên tuc khi ong dang bay, khong can giu Space.")]
    public bool autoFlapWhileFlying = true;

    [Tooltip("Dung do bay (0-1) de bat dau vô cánh tu dong.")]
    [Range(0f, 1f)]
    public float autoFlapSpeedThreshold = 0.06f;

    [Tooltip("Vô cánh chậm khi ong đứng yên / treo (trông như đập nghỉ).")]
    public bool idleFlap = true;

    [Tooltip("Số vòng/giây khi đứng yên (chậm).")]
    public float idleFlapFrequency = 2.2f;

    [Tooltip("Biên độ góc vồ cánh khi đứng yên (nhỏ).")]
    public float idleFlapAngle = 24f;

    [Tooltip("Nâng/hạ thân theo nhịp cánh (m) - tạo cảm giác sống.")]
    public float bodyBobAmplitude = 0.012f;

    [Tooltip("Góc gập cánh khi lưới (độ) - cánh hơi khép lại.")]
    public float glideFoldAngle = 26f;

    [Tooltip("Dung khi script khac (test PC) tu dieu khien: toc do bay thuc te 0-1. -1 = dung Speed cua BeeController.")]
    [Range(-1f, 1f)]
    public float ExternalSpeedRatio = -1f;

    [Tooltip("Cuc day dap canh tu script khac (test PC): 0 = tu chay, 1 = dap manh (Space).")]
    [Range(0f, 1f)]
    public float ExternalFlapBoost = -1f;

    [Tooltip("Tốc độ chuyển tiếp giữa đập và nghỉ (độ/giây).")]
    public float flapSmoothing = 20f;

    // =========================================================================
    // FORWARD TILT / NGHIÊNG KHI BAY NHANH
    // =========================================================================
    [Header("Forward Tilt / Nghiêng")]
    [Tooltip("Độ nghiêng thân xuống khi bay hết tốc (độ).")]
    public float fullSpeedPitchDown = 25f;

    [Tooltip("Transform thân ong. Để trống sẽ tự tìm child tên 'Body'.")]
    public Transform body;

    // =========================================================================
    // AUDIO / ÂM THANH
    // =========================================================================
    [Header("Audio / Âm thanh")]
    [Tooltip("AudioSource phát tiếng vỗ cánh.")]
    public AudioSource flapAudioSource;

    [Tooltip("Clip tiếng vỗ cánh (nên loop).")]
    public AudioClip flapClip;

    [Tooltip("Volume tối đa của tiếng vỗ cánh (0-1).")]
    [Range(0f, 1f)]
    public float flapVolume = 0.5f;

    [Tooltip("Pitch khi đứng yên.")]
    [Range(0.5f, 2f)]
    public float minPitch = 0.8f;

    [Tooltip("Pitch khi bay hết tốc.")]
    [Range(0.5f, 2f)]
    public float maxPitch = 1.4f;

    // =========================================================================
    // INTERNAL STATE / TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private BeeController _bee;
    private float _flapPhase;       // Pha nhịp vỗ cánh (radian)
    private float _restBlend;       // 0 = đập, 1 = nghỉ (đã smooth)
    private float _currentTilt;     // Góc nghiêng thân hiện tại (độ)
    private Vector3 _wingAxisNormalized;
    private Vector3 _bodyBasePos;

    private void Awake()
    {
        _bee = GetComponent<BeeController>();

        // Tự tìm nếu không gán thủ công. Hỗ trợ 2 cấu trúc:
        //   BeeRoot/Body/Wing_L            (flat)
        //   BeeRoot/BeeVisual/Body/Wing_L  (first-person rig)
        if (body == null) body = FindDeep(transform, "Body");
        if (wingLeft == null) wingLeft = FindDeep(transform, "Wing_L");
        if (wingRight == null) wingRight = FindDeep(transform, "Wing_R");

        if (body != null) _bodyBasePos = body.localPosition;
        _wingAxisNormalized = wingAxis.sqrMagnitude > 0.0001f ? wingAxis.normalized : Vector3.forward;

        SetupAudio();
    }

    // =========================================================================
    // FindDeep - Tìm child theo tên ở mọi cấp (chỉ chạy 1 lần trong Awake).
    // =============================================================================
    private static Transform FindDeep(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;

            Transform deep = FindDeep(child, childName);
            if (deep != null) return deep;
        }
        return null;
    }

    private void OnEnable()
    {
        // Đảm bảo clip loop để tiếng vỗ cánh liên tục.
        if (flapAudioSource != null)
        {
            flapAudioSource.loop = true;
        }
    }

    private void SetupAudio()
    {
        if (flapAudioSource == null)
        {
            flapAudioSource = GetComponent<AudioSource>();
        }

        if (flapAudioSource == null) return;

        if (flapClip != null)
        {
            flapAudioSource.clip = flapClip;
        }
        flapAudioSource.loop = true;
        flapAudioSource.playOnAwake = false;
        flapAudioSource.volume = flapVolume;
    }

    private void Update()
    {
        UpdateWingBeat();
        UpdateForwardTilt();
        UpdateAudio();
    }

    // =========================================================================
    // UpdateWingBeat - Nhịp vỗ cánh, nhanh hơn khi bay nhanh.
    // =========================================================================
    private void UpdateWingBeat()
    {
        float speedNorm = _bee != null ? _bee.SpeedNormalized : 0f;
        if (ExternalSpeedRatio >= 0f) speedNorm = ExternalSpeedRatio;
        bool pressing = _bee != null && _bee.IsFlapping;

        // Cu dap manh tu script khac (test PC): Space = dap toi da.
        float boost = Mathf.Clamp01(ExternalFlapBoost);
        if (boost > 0.05f) pressing = true;

        // Ong that vô cánh LIEN TUC khi bay, khong chi khi bam Space.
        bool flying = _bee != null && speedNorm > autoFlapSpeedThreshold;
        bool flapping = pressing || (autoFlapWhileFlying && flying);
        bool hovering = idleFlap && !flapping;

        // Nhip: bam Space > bay > dung yen (cham)
        float frequency;
        float amplitude;
        if (pressing)
        {
            frequency = flapFrequency * Mathf.Lerp(0.55f, 1f, speedNorm);
            amplitude = flapAngle;
        }
        else if (flapping)
        {
            frequency = flapFrequency * 0.72f * Mathf.Lerp(0.6f, 1f, speedNorm);
            amplitude = flapAngle * 0.8f;
        }
        else if (hovering)
        {
            frequency = idleFlapFrequency;
            amplitude = idleFlapAngle;
        }
        else
        {
            frequency = 0f;
            amplitude = 0f;
        }

        // Cu dap manh: nhip nhanh + bien do lon hon -> nhin ro la "bam dap".
        if (boost > 0.05f)
        {
            frequency *= (1f + boost * 0.9f);
            amplitude *= (1f + boost * 0.35f);
        }

        // Target blend: 0 = dang dap, 1 = nghi (canh gap lai khi luoi).
        float targetRest = flapping ? 0f : 1f;
        _restBlend = Mathf.Lerp(_restBlend, targetRest,
            1f - Mathf.Exp(-flapSmoothing * Time.deltaTime));

        if (frequency > 0f)
        {
            _flapPhase += frequency * Time.deltaTime * Mathf.PI * 2f;
            if (_flapPhase > Mathf.PI * 2f) _flapPhase -= Mathf.PI * 2f;
        }

        float wave = Mathf.Sin(_flapPhase);

        // Canh gap them khi khong dap (dang luoi) cho thong hien khong gian.
        float rest = Mathf.Max(restAngle, glidingFold(speedNorm));
        float angle = Mathf.Lerp(wave * amplitude, rest, _restBlend);

        ApplyWingAngle(wingLeft, angle);
        ApplyWingAngle(wingRight, -angle);

        // Nang/ha than theo nhip canh -> co cam giac "song".
        if (body != null && bodyBobAmplitude > 0f)
        {
            body.localPosition = _bodyBasePos + Vector3.up * (wave * bodyBobAmplitude * (0.35f + 0.65f * Mathf.Clamp01(speedNorm)));
        }
    }

    private float glidingFold(float speedNorm)
    {
        if (speedNorm > autoFlapSpeedThreshold) return restAngle;
        return Mathf.Lerp(glideFoldAngle, restAngle, speedNorm / Mathf.Max(0.0001f, autoFlapSpeedThreshold));
    }

    // =========================================================================
    // ApplyWingAngle - Xoay một cánh về góc cho trước.
    // =========================================================================
    private void ApplyWingAngle(Transform wing, float angle)
    {
        if (wing == null) return;
        wing.localRotation = Quaternion.AngleAxis(angle, _wingAxisNormalized);
    }

    // =========================================================================
    // UpdateForwardTilt - Nghiêng thân xuống khi bay nhanh (cảm giác lặn).
    // =============================================================================
    private void UpdateForwardTilt()
    {
        if (body == null) return;

        float speedNorm = _bee != null ? _bee.SpeedNormalized : 0f;
        float targetTilt = speedNorm * fullSpeedPitchDown;

        // Lưu tilt hiện tại thay vì đọc localEulerAngles (vốn trả về 0-360
        // và làm Lerp sai khi góc vượt 0 độ).
        float tilt = Mathf.Lerp(_currentTilt, targetTilt,
            1f - Mathf.Exp(-10f * Time.deltaTime));
        _currentTilt = tilt;
        body.localRotation = Quaternion.Euler(tilt, 0f, 0f);
    }

    // =========================================================================
    // UpdateAudio - Pitch và volume theo tốc độ bay.
    // =============================================================================
    private void UpdateAudio()
    {
        if (flapAudioSource == null) return;

        float speedNorm = _bee != null ? _bee.SpeedNormalized : 0f;

        // Chỉ phát khi đang bay hoặc vừa dừng (tránh tiếng ù ù khi đứng yên).
        if (flapAudioSource.isPlaying)
        {
            flapAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedNorm);
            flapAudioSource.volume = flapVolume * Mathf.Lerp(0.5f, 1f, speedNorm);
        }
    }

    // =========================================================================
    // PUBLIC API / HÀM CÔNG KHAI
    // =========================================================================

    /// <summary>
    /// Bật/tắt tiếng vỗ cánh (dùng khi vào menu hoặc pause).
    /// Bật/tắt tiếng vỗ cánh.
    /// </summary>
    public void SetFlapAudioEnabled(bool enabled)
    {
        if (flapAudioSource == null) return;

        if (enabled)
        {
            if (flapAudioSource.clip != null && !flapAudioSource.isPlaying)
            {
                flapAudioSource.Play();
            }
        }
        else if (flapAudioSource.isPlaying)
        {
            flapAudioSource.Stop();
        }
    }
}

