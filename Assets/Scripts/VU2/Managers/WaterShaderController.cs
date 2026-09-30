using UnityEngine;

public class WaterShaderController : MonoBehaviour
{
    [Header("Water Renderer")]
    [Tooltip("Renderer component of water object (auto-find if empty)")]
    public Renderer waterRenderer;
    
    [Header("Rainy Season Settings / Cài đặt mùa mưa")]
    [Tooltip("Speed during rainy season")]
    public float rainySpeed = -0.5f;
    
    [Tooltip("Salinity Contrast during rainy season")]
    public float rainySalinityContrast = 7f;
    
    [Header("Dry Season Settings / Cài đặt mùa khô")]
    [Tooltip("Speed during dry season")]
    public float drySpeed = 0.5f;
    
    [Tooltip("Salinity Contrast during dry season")]
    public float drySalinityContrast = 5f;
    
    [Header("Tidal Modulation / Điều biến thủy triều")]
    [Tooltip("Hệ số tốc độ shader bổ sung khi triều cường (nhân thêm vào speed mùa).\n"
           + "Extra speed multiplier at Spring Tide peak.")]
    public float springTideSpeedBoost = 1.5f;

    [Tooltip("Hệ số tốc độ shader khi triều kém (nhân vào speed mùa).\n"
           + "Speed multiplier at Neap Tide peak.")]
    public float neapTideSpeedBoost = 0.4f;

    [Tooltip("Salinity Contrast bổ sung khi triều cường.\n"
           + "Extra Salinity Contrast offset at Spring Tide.")]
    public float springTideSalinityOffset = -2f;

    [Tooltip("Salinity Contrast bổ sung khi triều kém.\n"
           + "Salinity Contrast offset at Neap Tide.")]
    public float neapTideSalinityOffset = 1f;

    [Header("Transition Settings / Cài đặt chuyển đổi")]
    [Tooltip("Duration of smooth transition between seasons (seconds)")]
    public float transitionDuration = 10f;

    private Material _waterMaterial;
    private Coroutine _transitionCoroutine;
    private float _seasonBaseSpeed;
    private float _seasonBaseSalinity;
    private float _tidalSpeedMultiplier = 1f;
    private float _tidalSalinityOffset = 0f;
    
    private void Start()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }
        if (waterRenderer != null)
        {
            _waterMaterial = waterRenderer.material;
        }
        else
        {
            return;
        }

        _seasonBaseSpeed = rainySpeed;
        _seasonBaseSalinity = rainySalinityContrast;

        LevelManager.OnWaveStepChanged += OnWaveStepChanged;
        TidalClockManager.OnTidalIntensityUpdated += OnTidalIntensityUpdated;

        // Start with rainy season settings (Preparation phase is first)
        ApplySeasonSettings(isRainy: true);
    }
    
    private void OnDestroy()
    {
        LevelManager.OnWaveStepChanged -= OnWaveStepChanged;
        TidalClockManager.OnTidalIntensityUpdated -= OnTidalIntensityUpdated;
    }
    
    private void OnWaveStepChanged(WaveStep step)
    {
        if (_waterMaterial == null) return;
        bool isRainy = step == WaveStep.Preparation;
        _seasonBaseSpeed = isRainy ? rainySpeed : drySpeed;
        _seasonBaseSalinity = isRainy ? rainySalinityContrast : drySalinityContrast;
        ApplySeasonSettings(isRainy);
    }

    private void OnTidalIntensityUpdated(float intensity)
    {
        // intensity: 0 = triều kém, 1 = triều cường
        _tidalSpeedMultiplier = Mathf.Lerp(neapTideSpeedBoost, springTideSpeedBoost, intensity);
        _tidalSalinityOffset = Mathf.Lerp(neapTideSalinityOffset, springTideSalinityOffset, intensity);

        if (_waterMaterial == null) return;

        // Apply tidal modulation on top of season base (every frame)
        float finalSpeed = _seasonBaseSpeed * _tidalSpeedMultiplier;
        float finalSalinity = _seasonBaseSalinity + _tidalSalinityOffset;

        _waterMaterial.SetFloat("_Speed", finalSpeed);
        _waterMaterial.SetFloat("_Salinity_Contrast", finalSalinity);
    }

    private void ApplySeasonSettings(bool isRainy)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }
        
        float currentSpeed = _waterMaterial.GetFloat("_Speed");
        float currentSalinityContrast = _waterMaterial.GetFloat("_Salinity_Contrast");

        float targetSpeed = isRainy ? rainySpeed : drySpeed;
        float targetSalinityContrast = isRainy ? rainySalinityContrast : drySalinityContrast;

        _transitionCoroutine = StartCoroutine(TransitionShaderValues(
            currentSpeed, targetSpeed,
            currentSalinityContrast, targetSalinityContrast
        ));
    }
    
    private System.Collections.IEnumerator TransitionShaderValues(
        float startSpeed, float endSpeed,
        float startSalinity, float endSalinity)
    {
        _waterMaterial.SetFloat("_Speed", endSpeed);
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            float currentSalinity = Mathf.Lerp(startSalinity, endSalinity, smoothT);
            
            _waterMaterial.SetFloat("_Salinity_Contrast", currentSalinity);
            
            yield return null;
        }
        
        _waterMaterial.SetFloat("_Salinity_Contrast", endSalinity);
        
        _transitionCoroutine = null;
    }
}
