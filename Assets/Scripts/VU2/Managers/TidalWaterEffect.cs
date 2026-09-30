using UnityEngine;

// =============================================================================
// TidalWaterEffect - Controls visual water level and shader based on tidal state.
// TidalWaterEffect - Điều khiển mực nước trực quan và shader theo trạng thái triều.
//
// BEHAVIOR:
// - Triều cường (Spring Tide): 
//   → Nước biển dâng cao → water object moves UP
//   → Shader speed increases (dòng chảy vào mạnh)
//   → Salinity contrast increases (nước mặn rõ hơn)
//   → Water color shifts to deeper blue
// - Triều kém (Neap Tide):
//   → Nước biển hạ thấp → water object moves DOWN
//   → Shader speed reverses (dòng chảy ra)
//   → Salinity contrast decreases
//   → Exposed mudflat area visible (bãi bồi lộ ra)
//
// HÀNH VI:
// - Triều cường:
//   → Nước dâng cao → object nước di chuyển LÊN
//   → Tốc độ shader tăng (dòng mặn chảy vào mạnh)
//   → Contrast độ mặn tăng (nước mặn rõ hơn)
//   → Màu nước chuyển xanh đậm hơn
// - Triều kém:
//   → Nước hạ thấp → object nước di chuyển XUỐNG
//   → Tốc độ shader đảo (dòng chảy ra)
//   → Contrast độ mặn giảm
//   → Bãi bồi ven biển lộ ra
// =============================================================================
public class TidalWaterEffect : MonoBehaviour
{
    // =========================================================================
    // WATER LEVEL VISUAL
    // HIỆU ỨNG MỰC NƯỚC
    // =========================================================================
    [Header("Water Level Object / Object mực nước")]
    
    [Tooltip("The water surface object to move up/down.\n"
           + "Object mặt nước để di chuyển lên/xuống.")]
    public Transform waterSurface;

    [Tooltip("Base Y position of the water surface (normal/neutral level).\n"
           + "Vị trí Y cơ sở của mặt nước (mức bình thường).")]
    public float baseWaterY = 0f;

    [Tooltip("Maximum Y offset during Spring Tide (water rises this much).\n"
           + "Offset Y tối đa khi triều cường (nước dâng bao nhiêu).")]
    public float springTideRiseHeight = 2.0f;

    [Tooltip("Maximum Y offset during Neap Tide (water drops this much, negative).\n"
           + "Offset Y khi triều kém (nước hạ bao nhiêu, âm).")]
    public float neapTideDropHeight = -1.5f;

    [Tooltip("Speed of water level transition.\n"
           + "Tốc độ chuyển đổi mực nước.")]
    public float waterLevelSmoothing = 1.5f;

    // =========================================================================
    // MUDFLAT / BÃI BỒI
    // =========================================================================
    [Header("Mudflat / Bãi bồi")]
    
    [Tooltip("Mudflat objects that appear when water recedes during Neap Tide.\n"
           + "Object bãi bồi xuất hiện khi nước rút triều kém.")]
    public GameObject[] mudflatObjects;

    [Tooltip("Tidal intensity threshold below which mudflats become visible.\n"
           + "Ngưỡng cường độ triều mà dưới đó bãi bồi lộ ra.")]
    [Range(0f, 1f)]
    public float mudflatVisibleThreshold = 0.3f;

    // =========================================================================
    // WATER SHADER
    // SHADER NƯỚC
    // =========================================================================
    [Header("Water Shader / Shader nước")]
    
    [Tooltip("Renderer of the water surface (auto-find if null).\n"
           + "Renderer mặt nước (tự tìm nếu null).")]
    public Renderer waterRenderer;

    [Tooltip("Water speed during Spring Tide (positive = flowing inward).\n"
           + "Tốc độ nước khi triều cường (dương = chảy vào).")]
    public float springTideShaderSpeed = 1.0f;

    [Tooltip("Water speed during Neap Tide (negative = flowing outward).\n"
           + "Tốc độ nước khi triều kém (âm = chảy ra).")]
    public float neapTideShaderSpeed = -0.6f;

    [Tooltip("Salinity contrast during Spring Tide.\n"
           + "Contrast độ mặn khi triều cường.")]
    public float springTideSalinityContrast = 4f;

    [Tooltip("Salinity contrast during Neap Tide.\n"
           + "Contrast độ mặn khi triều kém.")]
    public float neapTideSalinityContrast = 8f;

    [Tooltip("Speed of shader parameter transition.\n"
           + "Tốc độ chuyển đổi tham số shader.")]
    public float shaderTransitionSpeed = 2f;

    // =========================================================================
    // WATER COLOR
    // MÀU NƯỚC
    // =========================================================================
    [Header("Water Color / Màu nước")]
    
    [Tooltip("Water color during Spring Tide (deep salty blue).\n"
           + "Màu nước khi triều cường (xanh mặn đậm).")]
    public Color springTideColor = new Color(0.1f, 0.25f, 0.55f, 0.85f);

    [Tooltip("Water color during Neap Tide (light fresh green-blue).\n"
           + "Màu nước khi triều kém (xanh lá nhạt).")]
    public Color neapTideColor = new Color(0.15f, 0.45f, 0.4f, 0.7f);

    [Tooltip("Shader property name for water color.\n"
           + "Tên property shader cho màu nước.")]
    public string colorPropertyName = "_BaseColor";

    // =========================================================================
    // FOAM / WAVE EFFECT
    // BỌT NƯỚC / HIỆU ỨNG SÓNG
    // =========================================================================
    [Header("Wave Effect / Hiệu ứng sóng")]
    
    [Tooltip("Wave height multiplier during Spring Tide.\n"
           + "Hệ số chiều cao sóng khi triều cường.")]
    public float springTideWaveHeight = 1.5f;

    [Tooltip("Wave height multiplier during Neap Tide.\n"
           + "Hệ số chiều cao sóng khi triều kém.")]
    public float neapTideWaveHeight = 0.3f;

    [Tooltip("Shader property name for wave height.\n"
           + "Tên property shader cho chiều cao sóng.")]
    public string waveHeightPropertyName = "_WaveHeight";

    // =========================================================================
    // INTERNAL
    // NỘI BỘ
    // =========================================================================
    private TidalClockManager _manager;
    private Material _waterMaterial;
    private float _currentWaterY;
    private float _currentShaderSpeed;
    private float _currentSalinityContrast;
    private float _currentWaveHeight;
    private Color _currentWaterColor;
    private bool _hasMaterial;

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================
    private void Start()
    {
        _manager = TidalClockManager.Instance;
        if (_manager == null)
        {
            Debug.LogWarning("[TidalWaterEffect] TidalClockManager not found!");
            enabled = false;
            return;
        }

        // Cache water material.
        // Cache material nước.
        if (!waterRenderer && waterSurface)
            waterRenderer = waterSurface.GetComponent<Renderer>();

        if (waterRenderer)
        {
            _waterMaterial = waterRenderer.material;
            _hasMaterial = true;
        }

        // Initialize water Y position.
        // Khởi tạo vị trí Y mặt nước.
        if (waterSurface)
            baseWaterY = waterSurface.position.y;

        _currentWaterY = baseWaterY;
        _currentShaderSpeed = 0f;
        _currentSalinityContrast = 6f;
        _currentWaveHeight = 0.5f;
        _currentWaterColor = Color.Lerp(neapTideColor, springTideColor, 0.5f);

        // Hide mudflats initially.
        // Ẩn bãi bồi ban đầu.
        SetMudflatVisibility(false);

        // Subscribe to tidal intensity updates.
        // Đăng ký lắng nghe cập nhật cường độ triều.
        TidalClockManager.OnTidalIntensityUpdated += OnTidalIntensityUpdated;
    }

    private void OnDestroy()
    {
        TidalClockManager.OnTidalIntensityUpdated -= OnTidalIntensityUpdated;
    }

    // =========================================================================
    // UPDATE
    // CẬP NHẬT
    // =========================================================================
    private void OnTidalIntensityUpdated(float intensity)
    {
        UpdateWaterLevel(intensity);
        UpdateShaderParameters(intensity);
        UpdateMudflatVisibility(intensity);
    }

    /// <summary>
    /// Smoothly move water surface based on tidal intensity.
    /// Di chuyển mượt mặt nước dựa trên cường độ triều.
    /// Intensity 1.0 → water at springTideRiseHeight above base.
    /// Intensity 0.0 → water at neapTideDropHeight below base.
    /// </summary>
    private void UpdateWaterLevel(float intensity)
    {
        if (!waterSurface) return;

        // Map intensity 0..1 to neapDrop..springRise.
        // Ánh xạ cường độ 0..1 sang mức nước hạ..dâng.
        float targetY = baseWaterY + Mathf.Lerp(neapTideDropHeight, springTideRiseHeight, intensity);

        _currentWaterY = Mathf.MoveTowards(_currentWaterY, targetY, waterLevelSmoothing * Time.deltaTime);

        Vector3 pos = waterSurface.position;
        pos.y = _currentWaterY;
        waterSurface.position = pos;
    }

    /// <summary>
    /// Update water shader parameters for visual feedback.
    /// Cập nhật tham số shader nước cho hiệu ứng trực quan.
    /// </summary>
    private void UpdateShaderParameters(float intensity)
    {
        if (!_hasMaterial || !_waterMaterial) return;

        float dt = shaderTransitionSpeed * Time.deltaTime;

        // Speed: neap (outward) → spring (inward).
        // Tốc độ: triều kém (ra) → triều cường (vào).
        float targetSpeed = Mathf.Lerp(neapTideShaderSpeed, springTideShaderSpeed, intensity);
        _currentShaderSpeed = Mathf.MoveTowards(_currentShaderSpeed, targetSpeed, dt);
        if (_waterMaterial.HasProperty("_Speed"))
            _waterMaterial.SetFloat("_Speed", _currentShaderSpeed);

        // Salinity contrast.
        // Contrast độ mặn.
        float targetContrast = Mathf.Lerp(neapTideSalinityContrast, springTideSalinityContrast, intensity);
        _currentSalinityContrast = Mathf.MoveTowards(_currentSalinityContrast, targetContrast, dt);
        if (_waterMaterial.HasProperty("_Salinity_Contrast"))
            _waterMaterial.SetFloat("_Salinity_Contrast", _currentSalinityContrast);

        // Water color.
        // Màu nước.
        Color targetColor = Color.Lerp(neapTideColor, springTideColor, intensity);
        _currentWaterColor = Color.Lerp(_currentWaterColor, targetColor, dt);
        if (_waterMaterial.HasProperty(colorPropertyName))
            _waterMaterial.SetColor(colorPropertyName, _currentWaterColor);

        // Wave height.
        // Chiều cao sóng.
        float targetWave = Mathf.Lerp(neapTideWaveHeight, springTideWaveHeight, intensity);
        _currentWaveHeight = Mathf.MoveTowards(_currentWaveHeight, targetWave, dt);
        if (_waterMaterial.HasProperty(waveHeightPropertyName))
            _waterMaterial.SetFloat(waveHeightPropertyName, _currentWaveHeight);
    }

    /// <summary>
    /// Show/hide mudflat objects based on tidal intensity.
    /// Hiển thị/ẩn bãi bồi dựa trên cường độ triều.
    /// Mudflats appear when intensity drops below threshold (Neap Tide).
    /// Bãi bồi xuất hiện khi cường độ dưới ngưỡng (triều kém).
    /// </summary>
    private void UpdateMudflatVisibility(float intensity)
    {
        bool shouldShow = intensity < mudflatVisibleThreshold;
        SetMudflatVisibility(shouldShow);
    }

    private void SetMudflatVisibility(bool visible)
    {
        if (mudflatObjects == null) return;
        foreach (var obj in mudflatObjects)
        {
            if (obj && obj.activeSelf != visible)
                obj.SetActive(visible);
        }
    }
}
