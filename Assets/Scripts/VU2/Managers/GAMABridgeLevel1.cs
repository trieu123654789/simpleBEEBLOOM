using System.Collections.Generic;
using System.Text;
using UnityEngine;

// =============================================================================
// EN: GAMABridgeLevel1 — Runtime bootstrapper that connects Level1 to GAMA.
//     Level1 uses RulesoftheGame_VU2_1 (its own game controller) instead of the
//     GAMA GameManager/GameUI/LevelManager stack that Level2 uses.
//     This bridge instantiates the ManagersMulti prefab (ConnectionManager +
//     SimulationManagerMulti) at runtime and wires it to Level1's XR player.
//     Also periodically sends all MainMenuDetailsScrore data to GAMA via
//     "player_position_updated".
//
// VI: GAMABridgeLevel1 — Bootstrapper runtime kết nối Level1 với GAMA.
//     Level1 dùng RulesoftheGame_VU2_1 (controller riêng) thay vì stack
//     GameManager/GameUI/LevelManager mà Level2 dùng.
//     Bridge này tạo prefab ManagersMulti (ConnectionManager +
//     SimulationManagerMulti) lúc runtime và nối với XR player của Level1.
//     Đồng thời gửi định kỳ toàn bộ dữ liệu MainMenuDetailsScrore lên GAMA
//     qua "player_position_updated".
//
// SETUP: Attach this script to the GameManager object in SCN_VU2_Level1_New.
//        Assign the XR player reference in the Inspector.
//        Call NotifyGameStarted() from RulesoftheGame_VU2_1.StartGame().
//
// CÀI ĐẶT: Gắn script này vào object GameManager trong SCN_VU2_Level1_New.
//           Gán tham chiếu XR player trong Inspector.
//           Gọi NotifyGameStarted() từ RulesoftheGame_VU2_1.StartGame().
// =============================================================================
public class GAMABridgeLevel1 : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("EN: The XR player rig (XR Interaction Setup). / VI: XR player rig.")]
    [SerializeField] private GameObject player;

    [Tooltip("EN: Optional ground plane for GAMA coordinate mapping. / VI: Mặt đất (tùy chọn) cho ánh xạ tọa độ GAMA.")]
    [SerializeField] private GameObject ground;

    [Header("GAMA Connection Settings")]
    [Tooltip("EN: IP address of GAMA server. / VI: Địa chỉ IP server GAMA.")]
    [SerializeField] private string gamaServerIP = "localhost";

    [Header("Send Interval / Chu kỳ gửi")]
    [Tooltip("EN: Seconds between each data send to GAMA. / VI: Số giây giữa mỗi lần gửi dữ liệu lên GAMA.")]
    [SerializeField] private float sendInterval = 0.5f;

    private SimulationManagerMulti simulationManager;
    private bool gamaReady = false;
    private bool gameStarted = false;
    private float sendTimer = 0f;
    private float posTimer = 0f;

    // EN: Cached HUD reference for salinity readings.
    // VI: Tham chiếu HUD được cache để đọc độ mặn.
    private David_SeasonHUD _seasonHUD;

    // EN: Area multiplier matching TotalBoard display (count × 10 = area).
    // VI: Hệ số diện tích khớp với TotalBoard (count × 10 = diện tích).
    private const int AREA_MULTIPLIER = 10;
    private const int DURIAN_MAX_AREA = 150;
    private const int RICE_MAX_AREA = 250;

    public static GAMABridgeLevel1 Instance { get; private set; }

    // EN: DISABLED — Level1 now uses Level2's system (ManagersMulti pre-placed in scene).
    //     GAMABridgeLevel1 is kept as a stub so RulesoftheGame_VU2_1 references don't break.
    // VI: ĐÃ TẮT — Level1 giờ dùng hệ thống Level2 (ManagersMulti đặt sẵn trong scene).
    //     GAMABridgeLevel1 giữ lại dạng stub để tham chiếu từ RulesoftheGame_VU2_1 không lỗi.

    void Awake()
    {
        Instance = this;
        // SpawnManagersMulti(); // DISABLED — ManagersMulti is now pre-placed in scene like Level2
    }

    private void SpawnManagersMulti()
    {
        // EN: Load and instantiate ManagersMulti from Resources.
        // VI: Load và tạo instance ManagersMulti từ Resources.
        GameObject prefab = Resources.Load<GameObject>("Prefabs/GAMA/ManagersMulti");
        if (prefab == null)
        {
            Debug.LogError("[GAMABridgeLevel1] ManagersMulti prefab not found in Resources/Prefabs/GAMA/. " +
                           "GAMA connection will NOT work.");
            return;
        }

        GameObject instance = Instantiate(prefab);
        instance.name = "ManagersMulti";

        // EN: Wire up SimulationManagerMulti with Level1's player and ground.
        // VI: Nối SimulationManagerMulti với player và ground của Level1.
        simulationManager = instance.GetComponentInChildren<SimulationManagerMulti>();
        if (simulationManager != null)
        {
            simulationManager.InitReferences(player, ground);
            Debug.Log("[GAMABridgeLevel1] SimulationManagerMulti initialized with player and ground references.");
        }
        else
        {
            Debug.LogError("[GAMABridgeLevel1] SimulationManagerMulti not found in ManagersMulti prefab.");
        }

        // EN: Configure ConnectionManager IP if needed.
        // VI: Cấu hình IP ConnectionManager nếu cần.
        ConnectionManager connMgr = instance.GetComponentInChildren<ConnectionManager>();
        if (connMgr != null)
        {
            Debug.Log("[GAMABridgeLevel1] ConnectionManager found. GAMA connection will start automatically.");
        }
        else
        {
            Debug.LogError("[GAMABridgeLevel1] ConnectionManager not found in ManagersMulti prefab.");
        }

        gamaReady = true;
    }

    // EN: Called by RulesoftheGame_VU2_1.StartGame() to trigger GAMA game-start actions.
    //     Sends tree positions and enemy spawner data to GAMA server.
    // VI: Được gọi bởi RulesoftheGame_VU2_1.StartGame() để kích hoạt hành động bắt đầu game GAMA.
    //     Gửi vị trí cây và dữ liệu spawner kẻ thù lên GAMA server.
    public void NotifyGameStarted()
    {
        // DISABLED — Level1 now uses Level2's SimulationManager system.
        // SimulationManager.Update() handles sendEnemies/sendFreshWater/updatePlayerPos natively.
        Debug.Log("[GAMABridgeLevel1] NotifyGameStarted called but DISABLED — using Level2 system.");
    }

    // =========================================================================
    // Update - Periodically sends MainMenuDetailsScrore data to GAMA.
    // Update - Gửi định kỳ dữ liệu MainMenuDetailsScrore lên GAMA.
    // =========================================================================
    void Update()
    {
        // DISABLED — Level1 now uses Level2's SimulationManager.Update() for all GAMA communication.
        return;
    }

    // =========================================================================
    // SendScoreDataToGAMA - Collects all data displayed in MainMenuDetailsScrore
    // and sends it to GAMA via "player_position_updated".
    //
    // Data sent (matching TotalBoard + SeasonHUD display):
    //   id               : connection ID
    //   total_score      : tổng điểm (Thuan_23127_GameManager.Score)
    //   water_level      : mực nước % (GameRulesProvider.CurrentWaterLevelPercent)
    //   saltwater_intrusion : xâm nhập mặn (GameRulesProvider.Saltwater_Intrusion)
    //   inside_salinity  : độ mặn trong đê (‰)
    //   outside_salinity : độ mặn ngoài đê (‰)
    //   current_phase    : pha mùa hiện tại (Rainy1, Dry1, Dry2)
    //   current_month    : chỉ số tháng game (1-6)
    //   display_month    : tháng lịch hiển thị (11,12,1,2,3,4)
    //   time_remaining   : thời gian còn lại (giây)
    //   game_active      : game đang chạy (true/false)
    //   phase_data       : dữ liệu 3 pha mỗi sản phẩm
    //                      Format: "key:DT1:SL1:DT2:SL2:DT3:SL3;..."
    //                      DT = Diện tích (area), SL = Sản lượng (score)
    //                      Phase 3 uses DTMT (diện tích mất trồng) for Durian/Rice
    //   product_names    : danh sách tên sản phẩm "Durian;Rice;Shrimp"
    //
    // SendScoreDataToGAMA - Thu thập toàn bộ dữ liệu hiển thị trong
    // MainMenuDetailsScrore và gửi lên GAMA qua "player_position_updated".
    // =========================================================================
    private void SendScoreDataToGAMA()
    {
        var args = new Dictionary<string, string>();

        // --- Connection ID ---
        args["id"] = ConnectionManager.Instance.getUseMiddleware()
            ? ConnectionManager.Instance.GetConnectionId()
            : ("\"" + ConnectionManager.Instance.GetConnectionId() + "\"");

        // --- Total Score / Tổng điểm ---
        var gm = Thuan_23127_GameManager.Instance;
        args["total_score"] = (gm != null ? gm.Score : 0).ToString();

        // --- Game state / Trạng thái game ---
        args["water_level"] = GameRulesProvider.CurrentWaterLevelPercent.ToString("F1");
        args["saltwater_intrusion"] = GameRulesProvider.Saltwater_Intrusion.ToString("F2");
        args["current_phase"] = GameRulesProvider.CurrentPhase.ToString();
        args["current_month"] = GameRulesProvider.CurrentMonthIndex.ToString();
        args["display_month"] = GetDisplayMonth(GameRulesProvider.CurrentMonthIndex).ToString();
        args["time_remaining"] = ((int)GameRulesProvider.TimeRemaining).ToString();
        args["game_active"] = GameRulesProvider.GameActive.ToString();

        // --- Salinity (inside/outside dyke) / Độ mặn (trong/ngoài đê) ---
        float insideSal = 0f;
        float outsideSal = 0f;
        if (_seasonHUD != null)
        {
            if (_seasonHUD.insideDykeArea != null)
                insideSal = _seasonHUD.insideDykeArea.GetAreaSalinity();
            if (_seasonHUD.outsideDykeArea != null)
                outsideSal = _seasonHUD.outsideDykeArea.GetAreaSalinity();
        }
        args["inside_salinity"] = insideSal.ToString("F2");
        args["outside_salinity"] = outsideSal.ToString("F2");

        // --- Per-product 3-phase data (TotalBoard) / Dữ liệu 3 pha mỗi SP ---
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null)
        {
            var data = summary.GetAllPhaseData();
            var sbNames = new StringBuilder();
            var sbPhase = new StringBuilder();

            foreach (var (key, icon, scores, counts) in data)
            {
                if (sbNames.Length > 0) sbNames.Append(";");
                sbNames.Append(key);

                if (sbPhase.Length > 0) sbPhase.Append(";");

                bool isDurian = key.Contains("Durian") || key == "Plant:1";
                bool isRice = key.Contains("Rice") || key == "Plant:11";
                bool isShrimp = key.Contains("Shrimp");
                int totalHarvestedArea = (counts[0] + counts[1]) * AREA_MULTIPLIER;

                // Phase 1 (T11–T1): DT and SL
                int dt1 = counts[0] * AREA_MULTIPLIER;
                int sl1 = scores[0];

                // Phase 2 (T2–T3): DT and SL
                int dt2 = counts[1] * AREA_MULTIPLIER;
                int sl2 = scores[1];

                // Phase 3 (T4): DTMT (lost area) and SL — special rules per product
                int dt3;
                int sl3;
                if (isDurian)
                {
                    dt3 = Mathf.Max(0, DURIAN_MAX_AREA - totalHarvestedArea);
                    sl3 = 0;
                }
                else if (isRice)
                {
                    dt3 = Mathf.Max(0, RICE_MAX_AREA - totalHarvestedArea);
                    sl3 = 0;
                }
                else if (isShrimp)
                {
                    dt3 = 0;
                    sl3 = scores[2];
                }
                else
                {
                    dt3 = counts[2] * AREA_MULTIPLIER;
                    sl3 = scores[2];
                }

                // Format: "DT1:SL1:DT2:SL2:DT3:SL3"
                sbPhase.Append($"{dt1}:{sl1}:{dt2}:{sl2}:{dt3}:{sl3}");
            }

            args["product_names"] = sbNames.ToString();
            args["phase_data"] = sbPhase.ToString();
        }
        else
        {
            args["product_names"] = "";
            args["phase_data"] = "";
        }

        Debug.Log($"[GAMABridgeLevel1 Send] player_position_updated | score={args["total_score"]}, time_remaining={args["time_remaining"]}, phase={args["current_phase"]}");
        ConnectionManager.Instance.SendExecutableAsk("player_position_updated", args);
    }

    // =========================================================================
    // SendUpdatePlayerPosToGAMA - Sends update_player_pos matching Level2's
    // SimulationManager.updatePlayerPos() structure so GAMA receives the same
    // action from both levels.
    // =========================================================================
    private void SendUpdatePlayerPosToGAMA()
    {
        if (player == null) return;

        var gm = Thuan_23127_GameManager.Instance;
        var summary = Thuan_23127_SeasonalSummary.Instance;

        // EN: Harvest counts for life_tree and quanlity (same logic as SimulationManager).
        // VI: Số lượng thu hoạch cho life_tree và quanlity (cùng logic với SimulationManager).
        int riceH   = summary != null ? summary.GetTotalHarvestCount("Rice") : 0;
        int durianH = summary != null ? summary.GetTotalHarvestCount("Durian") : 0;
        int shrimpH = summary != null ? summary.GetTotalHarvestCount("Shrimp") : 0;

        string lifeTree = Mathf.Max(0, 25 - riceH) + ":" + Mathf.Max(0, 15 - durianH) + ":" + Mathf.Max(0, 5 - shrimpH);
        string quanlity = riceH + ":" + durianH + ":" + shrimpH;

        // EN: Use default precision=1000 (Level1 doesn't access SimulationManager.parameters).
        // VI: Dùng precision mặc định=1000 (Level1 không truy cập SimulationManager.parameters).
        const int precision = 1000;
        int px = (int)(player.transform.position.x * precision);
        int py = (int)(player.transform.position.z * precision);

        // EN: Camera forward angle (same formula as SimulationManager.updatePlayerPos).
        // VI: Góc hướng camera (cùng công thức với SimulationManager.updatePlayerPos).
        int angle = 0;
        if (Camera.main != null)
        {
            Vector2 vF = new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.z).normalized;
            Vector2 vR = new Vector2(player.transform.forward.x, player.transform.forward.z).normalized;
            float c = vF.x * vR.x + vF.y * vR.y;
            float s = vF.x * vR.y - vF.y * vR.x;
            angle = (int)(((s > 0) ? -1.0 : 1.0) * (180 / System.Math.PI) * System.Math.Acos(c) * precision);
        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
            {"x", "" + px},
            {"y", "" + py},
            {"o", angle + ""},
            {"remaining_time", ((int)GameRulesProvider.TimeRemaining).ToString()},
            {"dtree", "0"},
            {"fwater", "0"},
            {"score", (gm != null ? gm.Score : 0).ToString()},
            {"name_crop", "Rice:Durian:Shrimp"},
            {"life_tree", lifeTree},
            {"quanlity", quanlity}
        };

        Debug.Log($"[GAMABridgeLevel1 Send] update_player_pos | score={args["score"]}, remaining_time={args["remaining_time"]}, life_tree={args["life_tree"]}, quanlity={args["quanlity"]}");
        ConnectionManager.Instance.SendExecutableAsk("update_player_pos", args);
    }

    // =========================================================================
    // GetDisplayMonth - Converts game month index (1-6) to calendar month.
    // GetDisplayMonth - Chuyển chỉ số tháng game (1-6) sang tháng lịch.
    //
    // Game month 1→Nov(11), 2→Dec(12), 3→Jan(1), 4→Feb(2), 5→Mar(3), 6→Apr(4)
    // =========================================================================
    private static int GetDisplayMonth(int gameMonth)
    {
        return ((gameMonth + 9) % 12) + 1;
    }
}
