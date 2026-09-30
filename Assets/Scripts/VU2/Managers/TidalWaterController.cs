using UnityEngine;

/// <summary>
/// TidalWaterController — Điều khiển mực nước theo mùa + dao động thủy triều.
/// 
/// Quy tắc:
///   - Mùa mưa (Preparation) → mực nước cơ sở cao (highTideY)
///   - Mùa khô (Defense)      → mực nước cơ sở thấp (lowTideY)
///   - Trong mỗi mùa, thủy triều (TidalClockManager) tạo dao động cục bộ:
///       Triều cường (Spring Tide) → nước dâng thêm (+ springTideOffset)
///       Triều kém   (Neap Tide)   → nước hạ thêm  (+ neapTideOffset)
///   - Chuyển đổi mượt bằng Lerp.
///
/// Cách dùng: Gắn lên object nước trong scene.
///            Tự động lắng nghe LevelManager + TidalClockManager.
/// </summary>
public class TidalWaterController : MonoBehaviour
{
    [Header("Mực nước theo mùa (giá trị tuyệt đối)")]
    [Tooltip("Y tuyệt đối khi mùa mưa (Preparation - nước dâng cao).")]
    public float highTideY = 0.1f;

    [Tooltip("Y tuyệt đối khi mùa khô (Defense - nước rút).")]
    public float lowTideY = -0.4f;

    [Header("Dao động thủy triều (cộng thêm vào mực nước mùa)")]
    [Tooltip("Offset Y khi triều cường (Spring Tide). Dương = nước dâng thêm.")]
    public float springTideOffset = 0.15f;

    [Tooltip("Offset Y khi triều kém (Neap Tide). Âm = nước hạ thêm.")]
    public float neapTideOffset = -0.1f;

    [Header("Chuyển đổi")]
    [Tooltip("Tốc độ Lerp chuyển mực nước mùa (cao = nhanh).")]
    public float lerpSpeed = 1f;

    [Tooltip("Tốc độ Lerp dao động thủy triều (cao = nhanh).")]
    public float tidalLerpSpeed = 2f;

    private float _seasonBaseY;
    private float _tidalOffset;

    void Start()
    {
        _seasonBaseY = highTideY;
        _tidalOffset = 0f;
        LevelManager.OnWaveStepChanged += OnWaveStepChanged;
        TidalClockManager.OnTidalIntensityUpdated += OnTidalIntensityUpdated;
    }

    void OnDestroy()
    {
        LevelManager.OnWaveStepChanged -= OnWaveStepChanged;
        TidalClockManager.OnTidalIntensityUpdated -= OnTidalIntensityUpdated;
    }

    void OnWaveStepChanged(WaveStep step)
    {
        if (step == WaveStep.Preparation)
            _seasonBaseY = highTideY;   // Mùa mưa → nước dâng
        else
            _seasonBaseY = lowTideY;    // Mùa khô → nước rút
    }

    void OnTidalIntensityUpdated(float intensity)
    {
        // intensity: 0 = triều kém mạnh nhất, 1 = triều cường mạnh nhất
        _tidalOffset = Mathf.Lerp(neapTideOffset, springTideOffset, intensity);
    }

    void Update()
    {
        float targetY = _seasonBaseY + _tidalOffset;
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * lerpSpeed);
        transform.position = pos;
    }
}
