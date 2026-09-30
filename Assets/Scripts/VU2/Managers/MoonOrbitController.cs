using UnityEngine;

/// <summary>
/// MoonOrbitController — Điều khiển MoonIcon quay vòng tròn qua 4 PositionMarker.
/// Gắn lên TidalClock_Panel. Tự tìm MoonIcon và 4 Marker theo tên con.
///
/// Hierarchy yêu cầu (trong prefab):
///   ClockBackground
///     ├─ EarthIcon          (0, 0)   ← tâm
///     ├─ MoonOrbit          (0, 0)
///     │    └─ MoonIcon      (-60, 0) ← script sẽ di chuyển trực tiếp object này
///     ├─ PositionMarker_1   (-60, 0) ← LEFT
///     ├─ PositionMarker_2   (0, -60) ← BOTTOM
///     ├─ PositionMarker_3   (60, 0)  ← RIGHT
///     └─ PositionMarker_4   (0, 60)  ← TOP
///
/// Chu kỳ mặc định: 60 giây / vòng (1 phút).
/// Bắt đầu quay khi game Start.
/// </summary>
public class MoonOrbitController : MonoBehaviour
{
    [Header("Cấu hình")]
    [Tooltip("Thời gian 1 vòng quay (giây). Mặc định = 120s = 2 phút.")]
    public float cycleDuration = 120f;

    [Tooltip("Quay theo chiều kim đồng hồ?")]
    public bool clockwise = false;

    [Header("Tham chiếu (tự động tìm nếu để trống)")]
    [Tooltip("MoonIcon — object con bên trong MoonOrbit, di chuyển trực tiếp.")]
    public RectTransform moonIcon;
    public RectTransform marker1;
    public RectTransform marker2;
    public RectTransform marker3;
    public RectTransform marker4;

    // --- Nội bộ ---
    private Vector2 _center;      // Tâm vòng tròn (= 0,0 trong không gian ClockBackground)
    private float _radius;        // Bán kính quỹ đạo
    private float _startAngle;    // Góc bắt đầu (từ Marker_1)
    private float _elapsed;
    private bool _ready;
    private RectTransform _moonIconParent; // MoonOrbit — cha của MoonIcon
    private GameManager _gameManager;

    /// <summary>
    /// Marker zone hiện tại: 1, 2, 3, hoặc 4 (tương ứng PositionMarker_1..4).
    /// Dùng cho script nước để biết Moon đang ở vùng nào.
    /// </summary>
    public int CurrentMarkerZone { get; private set; } = 1;

    /// <summary>
    /// Tiến trình 0→1 trong chu kỳ hiện tại.
    /// </summary>
    public float NormalizedProgress { get; private set; }

    /// <summary>
    /// Singleton để script khác truy cập dễ dàng.
    /// </summary>
    public static MoonOrbitController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        AutoFindReferences();

        if (moonIcon == null || marker1 == null || marker2 == null ||
            marker3 == null || marker4 == null)
        {
            Debug.LogError("[MoonOrbitController] Thiếu tham chiếu! Kiểm tra con của TidalClock_Panel.");
            enabled = false;
            return;
        }

        // Lưu cha của MoonIcon (MoonOrbit)
        _moonIconParent = moonIcon.parent as RectTransform;

        // Tâm = trung bình vị trí 4 marker (trong không gian cha của chúng — ClockBackground)
        _center = (marker1.anchoredPosition + marker2.anchoredPosition +
                   marker3.anchoredPosition + marker4.anchoredPosition) / 4f;

        // Bán kính = khoảng cách trung bình từ tâm đến 4 marker
        _radius = (Vector2.Distance(_center, marker1.anchoredPosition) +
                   Vector2.Distance(_center, marker2.anchoredPosition) +
                   Vector2.Distance(_center, marker3.anchoredPosition) +
                   Vector2.Distance(_center, marker4.anchoredPosition)) / 4f;

        // Góc bắt đầu = hướng từ tâm đến Marker_1
        Vector2 dir1 = marker1.anchoredPosition - _center;
        _startAngle = Mathf.Atan2(dir1.y, dir1.x);

        // Quy đổi center từ không gian ClockBackground → không gian MoonOrbit
        if (_moonIconParent != null)
            _center -= _moonIconParent.anchoredPosition;

        // Đặt MoonIcon ở vị trí Marker_1 ngay từ đầu
        Vector2 marker1InLocal = marker1.anchoredPosition;
        if (_moonIconParent != null)
            marker1InLocal -= _moonIconParent.anchoredPosition;
        moonIcon.anchoredPosition = marker1InLocal;

        _elapsed = 0f;
        _ready = true;

        Debug.Log($"[MoonOrbitController] Sẵn sàng — Tâm={_center}, R={_radius:F1}, " +
                  $"Chu kỳ={cycleDuration}s, Chiều={( clockwise ? "kim đồng hồ" : "ngược kim" )}");
    }

    void Update()
    {
        if (!_ready) return;

        // Only orbit when game is in progress
        if (_gameManager != null && _gameManager.CurrentGameStatus() != GameStatus.InProgress) return;

        _elapsed += Time.deltaTime;

        // Tỷ lệ tiến trình 0→1 trong 1 chu kỳ (lặp lại)
        float t = (_elapsed % cycleDuration) / cycleDuration;

        // Góc quay: từ startAngle, đi hết 360° theo chiều đã chọn
        float direction = clockwise ? -1f : 1f;
        float angle = _startAngle + direction * t * 2f * Mathf.PI;

        // Vị trí mới trên vòng tròn (trong không gian cha của MoonIcon = MoonOrbit)
        float x = _center.x + Mathf.Cos(angle) * _radius;
        float y = _center.y + Mathf.Sin(angle) * _radius;

        moonIcon.anchoredPosition = new Vector2(x, y);

        // Xác định marker zone: chia 4 phần đều nhau
        // 0.000–0.125 & 0.875–1.0 = Marker 1
        // 0.125–0.375 = Marker 2
        // 0.375–0.625 = Marker 3
        // 0.625–0.875 = Marker 4
        NormalizedProgress = t;
        if (t < 0.125f || t >= 0.875f)
            CurrentMarkerZone = 1;
        else if (t < 0.375f)
            CurrentMarkerZone = 2;
        else if (t < 0.625f)
            CurrentMarkerZone = 3;
        else
            CurrentMarkerZone = 4;
    }

    /// <summary>
    /// Tự động tìm các con theo tên nếu chưa gán trong Inspector.
    /// </summary>
    private void AutoFindReferences()
    {
        if (moonIcon == null)
            moonIcon = FindChild("MoonIcon");
        if (marker1 == null)
            marker1 = FindChild("PositionMarker_1");
        if (marker2 == null)
            marker2 = FindChild("PositionMarker_2");
        if (marker3 == null)
            marker3 = FindChild("PositionMarker_3");
        if (marker4 == null)
            marker4 = FindChild("PositionMarker_4");
    }

    private RectTransform FindChild(string childName)
    {
        Transform found = FindDeep(transform, childName);
        if (found != null)
            return found as RectTransform;

        Debug.LogWarning($"[MoonOrbitController] Không tìm thấy con '{childName}' trong {gameObject.name}");
        return null;
    }

    /// <summary>
    /// Tìm kiếm đệ quy trong tất cả con/cháu.
    /// </summary>
    private Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeep(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
