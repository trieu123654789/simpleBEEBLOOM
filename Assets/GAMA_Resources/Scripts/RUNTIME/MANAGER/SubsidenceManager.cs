using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubsidenceManager : MonoBehaviour
{
    /* 
    Subsidence Manager: (vn) -> Quản lý sụt lún 
    - Subsidence Levels 
    - Tree Objects
    - Surface Water 

    ----------------------------------
    Message By Hồng Sơn: 
    We are processing subsidence data from Gamma
    
    */
    private bool isSubsidence = false;
    private float currentWaterLevel = 1f;
    private float currentWaterLevelGlobal = 1f;
    public static float currentSubsidenceLevel = 0f;

    [SerializeField] private float subsidenceLevel1 = 2f;
    [SerializeField] private float subsidenceLevel2 = 5f;
    [SerializeField] private float subsidenceLevel3 = 7f;
    [SerializeField] private float subsidenceLevelRatio = 0.2f;

    private List<GameObject> subsidenceLevels = new List<GameObject>();
    private List<Tree> treeObjects = new List<Tree>();
    private GameObject waterSurface;

    [SerializeField] private float waterRiseSpeed = 0.01f;
    [SerializeField] private float waterHeight = 1f;
    [SerializeField] private float waterLevelRatio = 0.2f;

    [Header("Water Surface (gán trực tiếp hoặc để tên child)")]
    [Tooltip("Kéo object water surface vào đây. Nếu để trống sẽ tìm theo waterSurfaceChildName trong children.")]
    [SerializeField] private GameObject waterSurfaceOverride;
    [Tooltip("Tên child dùng để tìm water surface khi waterSurfaceOverride bỏ trống. Mặc định 'SF_Water'. Nếu dùng SF_Water_Sea (1) thì gõ chính xác tên vào đây.")]
    [SerializeField] private string waterSurfaceChildName = "SF_Water";
    [Tooltip("Nếu bật: khi water level cạn thì tự SetActive(true) cho water surface. Tắt đi nếu bạn muốn tự kiểm soát hiển thị water surface.")]
    [SerializeField] private bool autoEnableOnFlood = false;

    [Header("Water Level by Season (Y world)")]
    [Tooltip("Mực nước mùa mưa (Saltwater_Intrusion = 0). Mặc định y = 0.")]
    [SerializeField] private float rainyWaterY = 0f;
    [Tooltip("Mực nước mùa khô (Saltwater_Intrusion = 1). Mặc định y = -1.")]
    [SerializeField] private float dryWaterY = -1f;
    [Tooltip("Độ mượt khi chuyển giữa mùa mưa/khô (lerp speed). 0 = nhảy tức thời.")]
    [SerializeField] private float seasonSmoothing = 1f;
    [Tooltip("Mức dâng tối đa do sụt lún (y world). Tổng season + subsidence không vượt quá mức này.")]
    [SerializeField] private float subsidenceMaxY = 0.5f;

    [Header("Tide (đồng hồ thuỷ triều)")]
    [Tooltip("Bật ảnh hưởng của thuỷ triều (Moon Orbit) lên mực nước.")]
    [SerializeField] private bool enableTide = true;
    [Tooltip("Biên độ thuỷ triều (mét). Mực nước dao động từ baseY - amplitude đến baseY + amplitude.")]
    [SerializeField] private float tideAmplitude = 0.2f;
    [Tooltip("Số chu kỳ thuỷ triều trên 1 vòng quay mặt trăng. 2 = bán nhật triều (2 đỉnh + 2 đáy / vòng), 1 = nhật triều.")]
    [SerializeField] private float tideCyclesPerOrbit = 2f;
    [Tooltip("Lệch pha (0..1). 0 = bắt đầu vị trí trung bình & dâng lên.")]
    [Range(0f, 1f)] [SerializeField] private float tidePhaseOffset = 0f;
    [Tooltip("Độ mượt khi chuyển mức nước (lerp speed). 0 = nhảy tức thời.")]
    [SerializeField] private float tideSmoothing = 0f;

    private float _waterBaseY;          // Y gốc của water surface khi Start (không còn dùng trực tiếp, giữ để fallback)
    private float _seasonY = 0f;        // Y theo mùa (đã smooth)
    private float _subsidenceRise = 0f; // Tỉ lệ dâng do sụt lún, [0,1]: 0 = mực mùa bình thường, 1 = dâng đến subsidenceMaxY
    [Tooltip("Tốc độ dâng/rút của mức sụt lún (per second). Cao hơn = phản ứng nhanh hơn.")]
    [SerializeField] private float subsidenceRiseSpeed = 0.05f;
    private float _displayedTide = 0f;  // tide hiện tại (đã smooth)
    private bool _waterBaseCaptured = false;

    [SerializeField] private Vector3 rotationModifier = Vector3.one;

    //Getter
    public float RemainingWaterLevelLocal
    {
        get { return currentWaterLevel; }
        set { currentWaterLevel = value; }
    }
    public float RemainingWaterLevelGlobal
    {
        get { return currentWaterLevelGlobal; }
        set { currentWaterLevelGlobal = value; }
    }
    public float SubsidenceScore
    {
        get { return currentSubsidenceLevel; } 
        set { currentSubsidenceLevel = value; }
    }

    public void IncreaseSubsidenceLevel()
    {
        currentSubsidenceLevel += subsidenceLevelRatio;
    }

    public void DecreaseWaterLevel()
    {
        currentWaterLevel -= waterLevelRatio;
    }

    void Start()
    {
        InitializeSubsidenceLevels();
        InitializeTreeObjects();
        InitializeSurfaceWater();
    }

    void InitializeSubsidenceLevels()
    {
        currentSubsidenceLevel = 0;
        for (int i = 1; i <= 3; i++)
        {
            GameObject subsidenceLevel = transform.Find("Subsidence_Lvl_" + i)?.gameObject;
            if (subsidenceLevel != null)
            {
                subsidenceLevels.Add(subsidenceLevel);
                subsidenceLevel.SetActive(false);
            }
        }
    }

    void InitializeTreeObjects()
    {
        Tree[] trees = FindObjectsOfType<Tree>();
        foreach (Tree tree in trees)
        {
            treeObjects.Add(tree);
        }
    }

    void InitializeSurfaceWater()
    {
        // 1) Ưu tiên reference do user gán trong Inspector.
        if (waterSurfaceOverride != null)
        {
            waterSurface = waterSurfaceOverride;
        }
        // 2) Fallback tìm theo tên child (mặc định 'SF_Water', có thể đổi sang 'SF_Water_Sea (1)').
        else if (!string.IsNullOrEmpty(waterSurfaceChildName))
        {
            Transform t = transform.Find(waterSurfaceChildName);
            if (t != null) waterSurface = t.gameObject;
        }

        // Lưu Y gốc (fallback) và khởi tạo seasonY = mực mùa hiện tại.
        if (waterSurface != null && !_waterBaseCaptured)
        {
            _waterBaseY = waterSurface.transform.localPosition.y;
            _seasonY = ComputeSeasonBaseY();
            _subsidenceRise = 0f;
            _displayedTide = 0f;
            _waterBaseCaptured = true;

            // Ép Y của water surface về đúng baseY ngay frame đầu
            // → tránh hiện tượng "nước dâng cao" trong vài frame đầu khi smoothing lợi chưa xướng hết, hoặc rules chưa cache kịp.
            Vector3 p0 = waterSurface.transform.localPosition;
            p0.y = _seasonY;
            waterSurface.transform.localPosition = p0;
        }
    }


    int tick = 0;
    void Update()
    {
        HandleSubsidence();
        ActivateSubsidenceLevels();
        ApplyWaterLevelEffect();
        Flooded(SubsidenceScore); //Kiểm tra mức độ lũ lụt
        ApplyWaterSurfacePosition(); // Tổng hợp subsidenceY + tide → set position cuối cùng
        //Debug.Log("SubsidenceScore: " + SubsidenceScore);
        GameManager gg = FindObjectOfType<GameManager>();
        if (gg != null && gg.CurrentGameStatus() == GameStatus.InProgress)
        {
            tick++;
            //Debug.Log("tick: " + tick);
            if (tick >= 1000)
            {
                //Debug.Log("Ask GAMA");
                tick = 0;
            }
        }
    }

    void HandleSubsidence()
    {
        isSubsidence = currentWaterLevel == 0 || currentSubsidenceLevel >= 1;
    }

    void RotateTrees()
    {
        foreach (Tree tree in treeObjects)
        {
            if (tree != null)
            {
                Vector3 rotationDelta = rotationModifier;
                tree.transform.Rotate(rotationDelta);
            }
        }
    }

    void ActivateSubsidenceLevels()
    {
        if (currentSubsidenceLevel >= subsidenceLevel3)
        {
            if (subsidenceLevels[2]?.activeSelf == false)
            {
                ActivateSubsidenceLevel(3);
                RotateTrees();
                PlaySubsidenceSfx();
                //Flooded(-30);
                Debug.Log("subsidenceLevel3");
            }
        }
        else if (currentSubsidenceLevel >= subsidenceLevel2)
        {
            if (subsidenceLevels[1]?.activeSelf == false)
            {
                ActivateSubsidenceLevel(2);
                RotateTrees();
                PlaySubsidenceSfx();
                //Flooded(-20);
                Debug.Log("subsidenceLevel2");
            }
        }
        else if (currentSubsidenceLevel >= subsidenceLevel1)
        {
            if (subsidenceLevels[0]?.activeSelf == false)
            {
                ActivateSubsidenceLevel(1);
                RotateTrees();
                PlaySubsidenceSfx();
                //Flooded(-10);
                Debug.Log("subsidenceLevel1");
            }
        }
    }

    /// <summary>Phát SFX nứt đất + cây đổ khi sụt lún kích hoạt level mới.</summary>
    void PlaySubsidenceSfx()
    {
        if (AudioManager.instance == null) return;
        Vector3 pos = waterSurface != null ? waterSurface.transform.position : transform.position;
        AudioManager.instance.PlayCrackingGround(pos);
        if (treeObjects != null && treeObjects.Count > 0)
            AudioManager.instance.PlayTreeFalling(pos);
    }

    void ActivateSubsidenceLevel(int level)
    {
        // for (int i = 0; i < subsidenceLevels.Count; i++)
        // {
        //     if (i == level - 1)
        //     {
        //         subsidenceLevels[i].SetActive(true);
        //     }
        // }
    }

    public void Flooded(float level)
    {
        // Null-guard: SF_Water có thể bị tắt/xoá khỏi scene.
        if (waterSurface == null) return;
        if (!_waterBaseCaptured) return;

        // Tỉ lệ dâng mong muốn theo SubsidenceScore: 0 điểm → 0, 3+ điểm → 1.
        // rise = 0 → base = seasonY (mùa mưa 0, mùa khô -0.5) → KHÔNG dâng.
        // rise = 1 → base = subsidenceMaxY (vd 0.5) → dâng tối đa.
        float targetRise = Mathf.Clamp01(SubsidenceScore / 3f);
        float step = subsidenceRiseSpeed * Time.deltaTime;
        if (_subsidenceRise < targetRise)
            _subsidenceRise = Mathf.Min(_subsidenceRise + step, targetRise);
        else if (_subsidenceRise > targetRise)
            _subsidenceRise = Mathf.Max(_subsidenceRise - step, targetRise);
    }

    void ApplyWaterLevelEffect()
    {
        if (waterSurface == null) return;
        if (!_waterBaseCaptured) return;

        if (currentWaterLevel <= 0f)
        {
            if (autoEnableOnFlood && waterSurface.activeSelf == false)
            {
                waterSurface.SetActive(true);
            }
            // Nếu user đã chủ ý tắt và autoEnableOnFlood = false → không tiếp tục đẩy subsidence.
            if (!waterSurface.activeSelf) return;

            // Khi water level trong game cạn → đẩy rise lên dần tới 1.
            float step = subsidenceRiseSpeed * Time.deltaTime;
            if (_subsidenceRise < 1f)
                _subsidenceRise = Mathf.Min(_subsidenceRise + step, 1f);
        }
    }

    /// <summary>
    /// Set vị trí cuối cùng của water surface = Lerp(seasonY, subsidenceMaxY, rise) + tideOffset.
    /// Gọi sau cùng mỗi frame trong Update.
    /// </summary>
    void ApplyWaterSurfacePosition()
    {
        if (waterSurface == null) return;
        if (!_waterBaseCaptured) return;

        // 1) Season Y — lerp giữa rainyWaterY ↔ dryWaterY theo Saltwater_Intrusion.
        float seasonTarget = ComputeSeasonBaseY();
        if (seasonSmoothing > 0f)
            _seasonY = Mathf.Lerp(_seasonY, seasonTarget, Time.deltaTime * seasonSmoothing);
        else
            _seasonY = seasonTarget;

        // 2) Tide offset (sin theo Moon Orbit).
        float tideTarget = ComputeTideOffset();
        if (tideSmoothing > 0f)
            _displayedTide = Mathf.Lerp(_displayedTide, tideTarget, Time.deltaTime * tideSmoothing);
        else
            _displayedTide = tideTarget;

        // 3) Base = Lerp(seasonY, subsidenceMaxY, rise).
        //    → rise = 0: base = seasonY (mùa mưa 0, mùa khô -0.5)
        //    → rise = 1: base = subsidenceMaxY (=0.5 trong cả 2 mùa)
        float clampedRise = Mathf.Clamp01(_subsidenceRise);
        float baseY = Mathf.Lerp(_seasonY, subsidenceMaxY, clampedRise);

        Vector3 p = waterSurface.transform.localPosition;
        p.y = baseY + _displayedTide;
        waterSurface.transform.localPosition = p;
    }

    /// <summary>Lấy mực nước theo mùa từ Saltwater_Intrusion (0=mưa → 1=khô).</summary>
    float ComputeSeasonBaseY()
    {
        // Ưu tiên đọc qua IGameRules instance (VU2_1 cho Level 1, VU2_2 cho Level 2),
        // tránh nhầm field static của class không có instance trong scene.
        if (_cachedRules == null)
        {
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb is IGameRules rules) { _cachedRules = rules; break; }
            }
        }
        float intrusion = _cachedRules != null
            ? Mathf.Clamp01(_cachedRules.SaltwaterIntrusion)
            : Mathf.Clamp01(RulesoftheGame_VU2_1.Saltwater_Intrusion);
        return Mathf.Lerp(rainyWaterY, dryWaterY, intrusion);
    }
    private IGameRules _cachedRules;

    /// <summary>
    /// Tide offset = sin(2π * (progress * cyclesPerOrbit + phase)) * amplitude.
    /// progress lấy từ MoonOrbitController.NormalizedProgress (0..1 mỗi vòng quay).
    /// </summary>
    float ComputeTideOffset()
    {
        if (!enableTide) return 0f;
        var moon = MoonOrbitController.Instance;
        if (moon == null) return 0f;
        float t = moon.NormalizedProgress;
        float angle = (t * tideCyclesPerOrbit + tidePhaseOffset) * 2f * Mathf.PI;
        return Mathf.Sin(angle) * tideAmplitude;
    }
}

