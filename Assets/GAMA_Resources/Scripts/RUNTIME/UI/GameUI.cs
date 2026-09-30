using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SimulationManager simulationManager;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private Transform head;
    [SerializeField] private float spawnDistance;

    [SerializeField] private GameObject startContent;
    [SerializeField] private GameObject finalContent;
    [SerializeField] private GameObject finalContent_Win;
    [SerializeField] private GameObject finalContent_Lose;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI finalText;
    [SerializeField] private string winText = "YOU WON!!!";
    [SerializeField] private string loseText = "YOU LOST!!!";

    [SerializeField] private PlayerResourcesManager playerResourcesManager;
    private SubsidenceManager subsidenceManager;
    

    private WebSocket socket;
//    private bool connected = false;
    public static GameUI Instance = null;

    private string reportText = "";
    [SerializeField] private TextMeshProUGUI reportTextMeshPro;

    // Son: Update final Menu
    [SerializeField] private TextMeshProUGUI reportLivingTreesNumber;
    [SerializeField] private TextMeshProUGUI reportDeadTreesNumber;
    [SerializeField] private TextMeshProUGUI reportLakeNumber;
    [SerializeField] private TextMeshProUGUI reportPumpNumber;
    [SerializeField] private TextMeshProUGUI reportWaterGateNumber;
    [SerializeField] private TextMeshProUGUI reportEnemiesNumber;
    [SerializeField] private TextMeshProUGUI reportRemainingGroundwaterLevelLocal;
    [SerializeField] private TextMeshProUGUI reportRemainingGroundwaterLevelGlobal;

    // Son: Update Win and Lose report 
    [SerializeField] private TextMeshProUGUI win_reportLivingTreesNumber;
    [SerializeField] private TextMeshProUGUI win_reportDeadTreesNumber;
    [SerializeField] private TextMeshProUGUI win_reportPumpNumber;
    [SerializeField] private TextMeshProUGUI win_reportWaterGateNumber;
    [SerializeField] private TextMeshProUGUI win_reportEnemiesNumber;
    [SerializeField] private TextMeshProUGUI win_reportSubsidenceScore;

    [SerializeField] private TextMeshProUGUI lose_reportLivingTreesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportDeadTreesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportPumpNumber;
    [SerializeField] private TextMeshProUGUI lose_reportWaterGateNumber;
    [SerializeField] private TextMeshProUGUI lose_reportEnemiesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportSubsidenceScore;

    public bool endDone = false;

    public float SubsidenceScore = 0;
    public float LiveTreeNumber = 0;
    private float dtree = 0;
    public float DeadTreeNumber = 0;
    public float TotalTree = 0;
    public float NumberPumper = 0;
    public float TotalNeutralWater = 0;
    public float TotalMiningWater = 0;
    public float WaterGateBlockedNumber = 0;

    // ==== TreeBarrier counters (đếm RIÊNG cho cây rừng "PFB_TreeBarrier" do người chơi trồng) ====
    // Tách khỏi LiveTreeNumber/DeadTreeNumber (vốn đếm tất cả IDamageable: durian, rice, ...).
    // Public để các script khác có thể tham chiếu.
    public int TreeBarrierAlive = 0;   // số cây rừng còn sống
    public int TreeBarrierDead  = 0;   // số cây rừng đã chết
    public int TreeBarrierTotal = 0;   // tổng cây rừng đã trồng (alive + dead)

    // ==== Bổ sung chỉ số chấm điểm (yc người dùng) ====
    // Số enemy mặn xâm nhập nội đồng (đi hết tuyến waypoint mà không bị diệt/chặn).
    // Number of saltwater enemies that breached inland.
    public int EnemiesBreached = 0;
    // Số cây ăn quả / lương thực đã chết (lúa, sầu riêng, dừa, chuối, cam ...).
    // Number of fruit/crop trees that died.
    public int FruitTreeDead = 0;

    public float ScoreGame = 0;

    void Start()
    {
        //string ip = PlayerPrefs.GetString("IP");
        //if (NotValid(ip))
         //   ip = "127.0.0.1";
        // Son: turn_off IP
        // playerTextOutput = GameObject.FindGameObjectWithTag("textIP").GetComponentInChildren<TextMeshProUGUI>();
        //playerTextOutput.text = ip;

        ready = false;
        transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;
        startContent.SetActive(true);
        finalContent.SetActive(false);
        Instance = this;
        TotalTree = playerResourcesManager.TotalTree;
        GateBlockerZone.ResetCounter();   // reset bộ đếm enemy bị cổng chặn cho phiên chơi mới
        // Reset 2 counter mới cho phiên chơi mới (EnemiesBreached & FruitTreeDead lưu trong StatisticsManager).
        EnemiesBreached = 0;
        FruitTreeDead   = 0;

        // Tắt TẤT CẢ panel kết thúc khi vào game — chỉ bật Final_Win_English khi game kết thúc.
        if (finalContent_Win  != null) finalContent_Win.SetActive(false);
        if (finalContent_Lose != null) finalContent_Lose.SetActive(false);
        DeactivateSiblingPanel("Final_Win_Vietnamese");
        DeactivateSiblingPanel("Final_Lose_Vietnamese");
        DeactivateSiblingPanel("Final_Win_English");
        DeactivateSiblingPanel("Final_Lose_English");
        Debug.Log("Start Total Tree:"+ TotalTree);
        Debug.Log("Start Live Tree:"+ LiveTreeNumber);
        Debug.Log("Start Dead Tree:"+ DeadTreeNumber);
    }
    void Awake()
    {
        Instance = this;
        subsidenceManager = FindObjectOfType<SubsidenceManager>();
    }
    public void computeScore()
    {
        SubsidenceScore = subsidenceManager.SubsidenceScore;

        LiveTreeNumber = playerResourcesManager.CurrentRefillSources;
        DeadTreeNumber = playerResourcesManager.TotalTree - playerResourcesManager.CurrentRefillSources;
        NumberPumper = StatisticsManager.Instance.WaterPumpCount;
        TotalNeutralWater = StatisticsManager.Instance.EnemyKillCount;
        TotalMiningWater = 100 - subsidenceManager.RemainingWaterLevelLocal;
        WaterGateBlockedNumber = GateBlockerZone.TotalEnemiesBlocked;

        // Lấy số enemy xâm nhập nội đồng & số cây ăn quả chết từ StatisticsManager.
        // Read breached-enemy count & fruit-tree death count from StatisticsManager.
        if (StatisticsManager.Instance != null)
        {
            EnemiesBreached = StatisticsManager.Instance.EnemyBreachedCount;
            FruitTreeDead   = StatisticsManager.Instance.FruitTreeDeathCount;
        }

        // Đếm RIÊNG TreeBarrier (cây rừng trồng) — tách khỏi LiveTree/DeadTree (đếm tất cả IDamageable).
        CountTreeBarriers();

        // =====================================================================
        // SCORING FORMULA (Level 2 — kịch bản mới)
        // CÔNG THỨC TÍNH ĐIỂM
        //
        //   + Chặn 1 con mặn   : +15
        //   + Trồng 1 cây rừng : +10  (cây còn sống)
        //   + Đặt 1 máy bơm    : +10
        //   - Sụt lún (khai nước quá mức) : -20 / đơn vị SubsidenceScore
        //   - Cây trồng chết   : -10
        //   - Enemy xâm nhập nội đồng : -15
        //   - Cây ăn quả chết  : -5
        //
        // Điểm = Máy bơm × 10 + Cây sống × 10 + Mặn diệt × 15
        //      - (SubsidenceScore × 20 + Cây rừng chết × 10 + Enemy xâm nhập × 15 + Cây ăn quả chết × 5)
        // =====================================================================
        const int POINT_PER_PUMP        = 10;
        const int POINT_PER_LIVE_TREE   = 10;
        const int POINT_PER_ENEMY       = 15;
        const int PENALTY_PER_SUBSIDE   = 20;  // mỗi đơn vị SubsidenceScore = 1 lần khai thác quá mức
        const int PENALTY_PER_DEAD_TREE = 10;
        const int PENALTY_PER_BREACH    = 15;  // enemy lọt vào nội đồng
        const int PENALTY_PER_FRUIT_DIE = 5;   // cây ăn quả chết

        float positive = NumberPumper      * POINT_PER_PUMP
                       + TreeBarrierAlive  * POINT_PER_LIVE_TREE
                       + TotalNeutralWater * POINT_PER_ENEMY;

        float negative = SubsidenceScore   * PENALTY_PER_SUBSIDE
                       + TreeBarrierDead   * PENALTY_PER_DEAD_TREE
                       + EnemiesBreached   * PENALTY_PER_BREACH
                       + FruitTreeDead     * PENALTY_PER_FRUIT_DIE;

        ScoreGame = Mathf.Round(positive - negative);
    }

    /// <summary>
    /// Quét toàn scène và đếm cây rừng (TreeBarrier) còn sống / đã chết.
    /// Gọi trong computeScore(); có thể gọi bất kỳ lúc nào nếu cần lấy số liệu real-time.
    /// </summary>
    public void CountTreeBarriers()
    {
        // FindObjectsOfType chỉ trả về object active. Cây đã destroy sẽ không còn,
        // nên dùng counter tích lũy trong TreeBarrier nếu cần; ở đây dựa vào IsDead().
        TreeBarrier[] all = FindObjectsOfType<TreeBarrier>(true);
        int alive = 0, dead = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].IsDead()) dead++; else alive++;
        }
        TreeBarrierAlive = alive;
        TreeBarrierDead  = dead;
        TreeBarrierTotal = alive + dead;
    }

    /// <summary>
    /// Tìm và bind text vào các TMP child trong panel kết thúc (Final_Win_English ...).
    /// Tự động dò theo tên — không cần kéo thả trong Inspector.
    /// Mapping (theo screenshot Final_Win_English):
    ///   "Text (TMP)- Living Tree Number"  -> số cây rừng còn sống (LiveTreeNumber)
    ///   "Text (TMP)- Dead Trees Number"   -> số cây đã chết (DeadTreeNumber)
    ///   "Text (TMP)- Pump Number"         -> số máy bơm đặt (NumberPumper)
    ///   "Text (TMP)- WaterGate Number"    -> số enemy bị PFB_Gate_G2 chặn
    ///   "Text (TMP)- Enemies Number"      -> số enemy bị diệt (TotalNeutralWater)
    ///   "SCORE (TMP)"                     -> điểm tổng (ScoreGame)
    /// </summary>
    private void BindFinalPanelTexts(GameObject panel)
    {
        if (panel == null) return;

        // Living / Dead trong panel kết thúc hiển thị theo TreeBarrier (cây rừng trồng),
        // đúng theo kịch bản tính điểm Level 2.
        SetTMPText(panel, "Text (TMP)- Living Tree Number", TreeBarrierAlive.ToString());
        SetTMPText(panel, "Text (TMP)- Dead Trees Number",  TreeBarrierDead.ToString());
        SetTMPText(panel, "Text (TMP)- Pump Number",        NumberPumper.ToString());
        SetTMPText(panel, "Text (TMP)- WaterGate Number",   WaterGateBlockedNumber.ToString());
        SetTMPText(panel, "Text (TMP)- Enemies Number",     TotalNeutralWater.ToString());
        // Mới: số enemy xâm nhập nội đồng + số cây rừng đã trồng + số cây ăn quả chết.
        // New metrics: enemies breached inland, total planted trees, fruit trees dead.
        SetTMPText(panel, "Text (TMP)- Breached Number",    EnemiesBreached.ToString());
        SetTMPText(panel, "Text (TMP)- Planted Tree Number", TreeBarrierTotal.ToString());
        SetTMPText(panel, "Text (TMP)- Fruit Tree Dead Number", FruitTreeDead.ToString());
        SetTMPText(panel, "SCORE (TMP)",                    ScoreGame.ToString());
    }

    private static void SetTMPText(GameObject root, string childName, string value)
    {
        Transform t = FindChildRecursiveByName(root.transform, childName);
        if (t == null) return;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = value;
    }

    private static Transform FindChildRecursiveByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursiveByName(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Tìm panel theo tên trong scene (kể cả khi đang inactive) và tắt đi.
    /// Dùng để chắc chắn chỉ Final_Win_English hiển thị khi kết thúc.
    /// </summary>
    private static void DeactivateSiblingPanel(string panelName)
    {
        // Resources.FindObjectsOfTypeAll bao gồm cả object inactive.
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null) continue;
            if (t.name != panelName) continue;
            // Bỏ qua prefab assets (không thuộc scene).
            if (t.gameObject.scene.IsValid() == false) continue;
            t.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        ready = true;
        
       
        if (!endDone && (gameManager.CurrentGameStatus() == GameStatus.Win || gameManager.CurrentGameStatus() == GameStatus.Lose))
        {
            endDone = true;
            transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;
            startContent.SetActive(false);
            //finalContent.SetActive(true);
            computeScore();
            // Debug.Log("Total Tree:"+ TotalTree);
            // Debug.Log("Dead Tree:"+ DeadTreeNumber);
            // Debug.Log("Live Tree:"+ LiveTreeNumber);
            // Debug.Log("SubsidenceScore:"+ SubsidenceScore);
            // Debug.Log("ScoreGame:"+ ScoreGame);
            // Son : update menu win and lose 
            // Win/Lose dựa trên điểm: > 0 = thắng, ≤ 0 = thua.

            // Bảo đảm các panel kết thúc khác bị tắt — chỉ Final_Win_English hiển thị.
            DeactivateSiblingPanel("Final_Win_Vietnamese");
            DeactivateSiblingPanel("Final_Lose_Vietnamese");
            DeactivateSiblingPanel("Final_Lose_English");

            if (ScoreGame > 0f)
            {
                finalContent_Win.SetActive(true);
            }
            else finalContent_Lose.SetActive(true);

            Debug.Log($"[GameUI] Game ended. Score={ScoreGame}. " +
                      $"WinPanel.active={finalContent_Win.activeSelf} " +
                      $"LosePanel.active={finalContent_Lose.activeSelf}");

            

            if (gameManager.CurrentGameStatus() == GameStatus.Win)
            {
                finalText.text = winText;
            }
            if (gameManager.CurrentGameStatus() == GameStatus.Lose)
            {
                finalText.text = loseText;
            }



            // Add Report Text Here
            // reportText = "Living Trees: " + playerResourcesManager.CurrentRefillSources + "\n" +
            //              "Dead Trees: " + (playerResourcesManager.TotalTree - playerResourcesManager.CurrentRefillSources) + "\n" +
            //              "Lake Structures Built: " + StatisticsManager.Instance.LakeCount + "\n" +
            //              "WaterPump Structures Built: " + StatisticsManager.Instance.WaterPumpCount + "\n" +
            //              "SluiceGate Structures Built: " + StatisticsManager.Instance.SluiceGateCount + "\n" +
            //              "Enemies Neutralized: " + StatisticsManager.Instance.EnemyKillCount + "\n" +
            //              "Remaining Groundwater Level (Local): " + subsidenceManager.RemainingWaterLevelLocal + "\n" +
            //              "Remaining Groundwater Level (Global): " + subsidenceManager.RemainingWaterLevelGlobal + "\n" +
            //              "Subsidence Score: " + subsidenceManager.SubsidenceScore;


            // Son: Setup Final Report
            // reportTextMeshPro.text = reportText;
            // reportLivingTreesNumber.text = "" + playerResourcesManager.CurrentRefillSources;
            // reportDeadTreesNumber.text = "" + (playerResourcesManager.TotalTree - playerResourcesManager.CurrentRefillSources);
            // reportLakeNumber.text = "" + StatisticsManager.Instance.LakeCount;
            // reportPumpNumber.text = "" + StatisticsManager.Instance.WaterPumpCount;
            // reportWaterGateNumber.text = "" + StatisticsManager.Instance.SluiceGateCount;
            // reportEnemiesNumber.text = "" + StatisticsManager.Instance.EnemyKillCount;
            // reportRemainingGroundwaterLevelLocal.text = "Remaining Groundwater Level (Local): " + subsidenceManager.RemainingWaterLevelLocal;
            // reportRemainingGroundwaterLevelGlobal.text = "Remaining Groundwater Level (Global): " + subsidenceManager.RemainingWaterLevelGlobal;
        



            // Son: Update Win and Lose — bind text bằng tên TMP children trong panel đang hiện.
            // Tránh phụ thuộc vào reference Inspector cũ (vốn trỏ vào panel Vietnamese).
            GameObject activePanel = (ScoreGame > 0f) ? finalContent_Win : finalContent_Lose;
            BindFinalPanelTexts(activePanel);

            // Vẫn cập nhật các serialized ref cũ (nếu user còn liên kết) để không phá vỡ.
            if (win_reportLivingTreesNumber != null) win_reportLivingTreesNumber.text = "" + TreeBarrierTotal;
            if (win_reportDeadTreesNumber != null) win_reportDeadTreesNumber.text = "" + TreeBarrierDead;
            if (win_reportPumpNumber != null) win_reportPumpNumber.text = "" + NumberPumper;
            if (win_reportWaterGateNumber != null) win_reportWaterGateNumber.text = "" + WaterGateBlockedNumber;
            if (win_reportEnemiesNumber != null) win_reportEnemiesNumber.text = "" + EnemiesBreached;
            if (win_reportSubsidenceScore != null) win_reportSubsidenceScore.text = "" + ScoreGame;

            if (lose_reportLivingTreesNumber != null) lose_reportLivingTreesNumber.text = "" + TreeBarrierTotal;
            if (lose_reportDeadTreesNumber != null) lose_reportDeadTreesNumber.text = "" + TreeBarrierDead;
            if (lose_reportPumpNumber != null) lose_reportPumpNumber.text = "" + NumberPumper;
            if (lose_reportWaterGateNumber != null) lose_reportWaterGateNumber.text = "" + WaterGateBlockedNumber;
            if (lose_reportEnemiesNumber != null) lose_reportEnemiesNumber.text = "" + EnemiesBreached;
            if (lose_reportSubsidenceScore != null) lose_reportSubsidenceScore.text = "" + ScoreGame;

        }

        // transform.LookAt(new Vector3(head.position.x, transform.position.y, head.position.z));
        // transform.forward *= -1;
    }

    public void StartTutorialUI()
    {
        gameManager.StartTutorial();
        startContent.gameObject.SetActive(false);
    }

    public void ReConnect()
    {
        ConnectionManager.Instance.UpdateConnectionState(ConnectionState.DISCONNECTED);
    }

    public void StartUI()
    {
       /* // PlayerPrefs.SetString("IP", "localhost");
        PlayerPrefs.SetString("PORT", "1000");
        PlayerPrefs.SetString("IP", "127.0.0.1");
        // Son: turn_off IP
        //PlayerPrefs.SetString("IP", playerTextOutput.text);
        PlayerPrefs.Save();

        port = PlayerPrefs.GetString("PORT");
        host = PlayerPrefs.GetString("IP");
        // socket = new WebSocket("ws://" + host + ":" + port + "/");
        // socket.OnOpen += HandleConnectionOpen;
        // socket.Connect();*/

        startContent.gameObject.SetActive(false);
        gameManager.StartLevel();
        Debug.Log("SendTrees:");
        simulationManager.sendTrees();
        simulationManager.createEnemySpawner();

    }

    public void RetryUI()
    {
        gameManager.RestartLevel();
        Restart();
    }

    public List<float> toGAMACRS3D(Vector3 pos)
    {
        List<float> position = new List<float>();
        // position.Add((int)((pos.x - GamaCRSOffsetX) / GamaCRSCoefX * precision));
        // position.Add((int)((pos.z - GamaCRSOffsetY) / GamaCRSCoefY * precision));
        // position.Add((int)((pos.y - GamaCRSOffsetZ) / GamaCRSCoefZ * precision));
        position.Add((float)(pos.x * precision));
        position.Add((float)(pos.z * precision));
        position.Add((float)(pos.y * precision));

        return position;
    }

    // optional: define a scale between GAMA and Unity for the location given
    public float GamaCRSCoefX = 1.0f;
    public float GamaCRSCoefY = 1.0f;
    public float GamaCRSCoefZ = 1.0f;
    public float GamaCRSOffsetX = 0.001f;
    public float GamaCRSOffsetY = 0.001f;
    public float GamaCRSOffsetZ = 0.001f;

    public int precision = 1;


    public void Restart()
    {
        SimulationManager.Instance.RestartGame();
    }

    public void CountDeadTree()
    {
        dtree++;
    }

    public void DeletePlayer(GameObject obj)
    {
        // if (SimulationManager.Instance.IsGameState(GameState.GAME))
        // {
        //     int instanceId = obj.GetInstanceID();

        //     Dictionary<string, string> args = new Dictionary<string, string> {
        //     {"idP",ConnectionManager.Instance.GetConnectionId() },
        //     {"id", ""+  obj },
        //     {"iid",  ""+instanceId },
        //     };

        //     // Debug.Log("DeletePlayer: " + obj);

        //     // SendExecutableAsk("simulation[0]", "DeletePlayer", args);

        //     ConnectionManager.Instance.SendExecutableAsk("DeletePlayer", args);
        // }
    }
    public void UpdateConstructionPosition(GameObject obj)
    {
        // if (GetSocket() == null || !connected || finalContent.activeSelf) return;

 
            // Debug.Log("sent to GAMA: " + SimulationManager.Instance.currentState);
        // if (SimulationManager.Instance.IsGameState(GameState.GAME))// && UnityEngine.Random.Range(0.0f, 1.0f) < 0.002f)
        // {


        //     Vector2 vF = new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.z);
        //     Vector2 vR = new Vector2(transform.forward.x, transform.forward.z);
        //     vF.Normalize();
        //     vR.Normalize();
        //     float c = vF.x * vR.x + vF.y * vR.y;
        //     float s = vF.x * vR.y - vF.y * vR.x;
        //     int angle = (int)(((s > 0) ? -1.0 : 1.0) * (180 / Math.PI) * Math.Acos(c) * precision);

        //     List<float> p = toGAMACRS3D(obj.transform.position);
        //     float instanceId = obj.GetInstanceID();

        //     // Vector3 v = new Vector3(Camera.main.transform.position.x, player.transform.position.y, Camera.main.transform.position.z);
        //     // List<float> p = toGAMACRS3D(v);
        //     Dictionary<string, string> args = new Dictionary<string, string> {
        //     {"idP",ConnectionManager.Instance.GetConnectionId() },
        //     {"id", ""+  obj },
        //     {"iid",  ""+instanceId },
        //     {"x", "" +p[0]},
        //     {"y", "" +p[1]},
        //     {"z", "" +p[2]},
        //     {"angle", "" +angle}
        //     };

        //     // Debug.Log("move_player_external: " + player + " " + p[0] + "," + p[1] + "," + p[2]);


        //     // Debug.Log("sent to GAMA: " + obj);
        //     // ConnectionManager.Instance.SendExecutableAsk("construction_message", args);
        //     // SendExecutableAsk("simulation[0]", "move_player_external", args);
        // }
    }
    protected string host;
    protected string port;
    private static bool ready = false;
    private TextMeshProUGUI playerTextOutput;

    private static bool NotValid(string ip)
    {
        if (ip == null || ip.Length == 0) return false;
        string[] ipb = ip.Split(".");
        return (ipb.Length != 4);
    }


    public void OnTriggerEnterBtn(Text text)
    {
        string t = text.text;

        if (ready)
        {
            playerTextOutput.text += t;

        }
    }

    public void OnTriggerEnterDelete()
    {

        if (ready && playerTextOutput.text.Length > 0)
        {
            playerTextOutput.text = playerTextOutput.text.Substring(0, playerTextOutput.text.Length - 1);

        }
    }

    public void OnTriggerEnterCancel()
    {

        if (ready && playerTextOutput.text.Length > 0)
        {
            playerTextOutput.text = "";

        }
    }
    protected void HandleConnectionOpen(object sender, System.EventArgs e)
    {
      //  connected = true;
        Debug.Log("ConnectionManager: Connection opened");

    }

}
