using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// David_SalinitySliderLive - Gradually increases salinity display from
//                             0.0 ‰ → 4.0 ‰ across the 3 game phases.
// David_SalinitySliderLive - Tăng dần hiển thị độ mặn từ 0.0 ‰ → 4.0 ‰
//                             xuyên suốt 3 giai đoạn game.
//
// ATTACH to the "salinty" GameObject in the scene.
// GẮN vào GameObject "salinty" trong scene.
//
// Timeline (default monthDuration = 30s):
//   T11 (0s)   → 0.0 ‰   Start of Phase 1
//   T12 (30s)  → 0.7 ‰
//   T1  (60s)  → 1.3 ‰   End of Phase 1
//   T2  (90s)  → 2.0 ‰   Start of Phase 2
//   T3  (120s) → 2.7 ‰   End of Phase 2
//   T4  (150s) → 3.3 ‰   Start of Phase 3
//   End (180s) → 4.0 ‰   End of game
// =============================================================================
public class David_SalinitySliderLive : MonoBehaviour
{
    // =========================================================================
    // REFERENCES — auto-found if left blank.
    // THAM CHIẾU — tự tìm nếu để trống.
    // =========================================================================
    [Header("UI References")]
    [Tooltip("Slider component under this GameObject")]
    public Slider salinitySlider;

    [Tooltip("Static label text — drag 'textsalinity' here (shows 'Độ mặn:')")]
    public Text labelText;

    [Tooltip("Dynamic value text — drag 'Header Text (1)' here (shows '0.0 ‰')")]
    public Text valueText;

    [Tooltip("Fill image for color feedback")]
    public Image salinityFill;

    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================
    [Header("Config")]
    [Tooltip("Max salinity ‰ reached at end of game (T4)")]
    public float maxSalinityPpt = 4f;

    [Tooltip("Static label shown on textsalinity")]
    public string label = "Độ mặn:";

    // =========================================================================
    // INTERNAL
    // =========================================================================

    private void Awake()
    {
        if (salinitySlider == null) salinitySlider = GetComponentInChildren<Slider>();
        if (salinityFill   == null && salinitySlider != null)
            salinityFill = salinitySlider.fillRect?.GetComponent<Image>();
    }

    private void Start()
    {
        // Set static label once — never changes.
        // Đặt nhãn tĩnh 1 lần — không bao giờ thay đổi.
        if (labelText != null) labelText.text = label;

        // Initialize value to 0.
        // Khởi tạo giá trị ở 0.
        if (salinitySlider != null) salinitySlider.value = 0f;
        if (valueText      != null) valueText.text = "0.0 ‰";
    }

    // =========================================================================
    // Update - Calculates salinity based on game time progression.
    // Update - Tính độ mặn dựa trên tiến trình thời gian game.
    //
    // Salinity increases gradually from 0 ‰ (T11) to 4 ‰ (end of T4).
    // Độ mặn tăng dần từ 0 ‰ (T11) đến 4 ‰ (cuối T4).
    //
    // Uses timeRemaining from RulesoftheGame_VU2_1:
    //   elapsed = totalDuration - timeRemaining
    //   progress = elapsed / totalDuration   (0.0 → 1.0)
    //   salinity = progress × maxSalinityPpt (0.0 → 4.0)
    // =========================================================================
    private void Update()
    {
        if (!GameRulesProvider.GameActive) return;

        // Total game = monthDuration × 6 (T11, T12, T1, T2, T3, T4).
        float totalDuration = GameRulesProvider.MonthDuration * 6f;
        if (totalDuration <= 0f) return;

        // timeRemaining counts UP: 0 (start) → totalDuration (end).
        // timeRemaining đếm TIẾN: 0 (bắt đầu) → totalDuration (kết thúc).
        float progress = Mathf.Clamp01(GameRulesProvider.TimeRemaining / totalDuration);

        // Salinity scales linearly: 0 ‰ at T11 → 4 ‰ at end of T4.
        // Độ mặn tăng tuyến tính: 0 ‰ tại T11 → 4 ‰ cuối T4.
        float salinityPpt = progress * maxSalinityPpt;

        // --- Slider ---
        if (salinitySlider != null)
            salinitySlider.value = Mathf.Clamp01(progress);

        // --- Value text on Header Text (1): "2.0 ‰" ---
        if (valueText != null)
            valueText.text = $"{salinityPpt:0.0} ‰";

        // --- Fill color ---
        //   Phase 1 region (0–1.3 ‰): Green  = nước ngọt
        //   Phase 2 region (1.3–3.3 ‰): Orange = xâm nhập
        //   Phase 3 region (3.3–4 ‰): Red    = mặn nặng
        if (salinityFill != null)
        {
            salinityFill.color = salinityPpt < 2f ? Color.green
                               : salinityPpt < 3f ? new Color(1f, 0.6f, 0f)
                               : Color.red;
        }
    }
}
