using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

using UnityEngine.UI;

// EN: Main simulation orchestrator between Unity gameplay and GAMA server messages.
//     Responsibilities:
//     1. Connection lifecycle: subscribe to ConnectionManager events, handle auth flow.
//     2. Inbound messages: route GAMA payloads to terrain, animation, spawn-rate, subsidence handlers.
//     3. Outbound sync: periodically send player position, enemy positions, ally positions to GAMA.
//     4. One-shot registration: send tree list, enemy spawner list, pumper list to GAMA.
//     5. Game state machine: MENU → WAITING → LOADING_DATA → GAME → END.
//     Subclasses (Solo, Multi, Interaction) override virtual methods for game-specific behavior.
// VI: Bộ điều phối mô phỏng chính giữa gameplay Unity và message server GAMA.
//     Trách nhiệm:
//     1. Vòng đời kết nối: đăng ký event ConnectionManager, xử lý luồng xác thực.
//     2. Message đến: định tuyến payload GAMA đến handler địa hình, animation, spawn-rate, sụt lún.
//     3. Đồng bộ ra: gửi định kỳ vị trí player, enemy, ally lên GAMA.
//     4. Đăng ký một lần: gửi danh sách cây, spawner, pumper lên GAMA.
//     5. Máy trạng thái: MENU → WAITING → LOADING_DATA → GAME → END.
//     Lớp con (Solo, Multi, Interaction) override các phương thức ảo cho hành vi riêng.
public class SimulationManager : MonoBehaviour
{
    // EN: XR input action for the primary right-hand button (triggers TriggerMainButton).
    // VI: Input action XR cho nút chính tay phải (kích hoạt TriggerMainButton).
    [SerializeField] protected InputActionReference primaryRightHandButton = null;
    // EN: XR input action for the reconnect button (triggers TryReconnect).
    // VI: Input action XR cho nút reconnect (kích hoạt TryReconnect).
    [SerializeField] protected InputActionReference TryReconnectButton = null;

    [Header("Base GameObjects")]
    // EN: The player XR rig root.
    // VI: Gốc XR rig của người chơi.
    [SerializeField] protected GameObject player;
    // EN: Ground plane scaled to match GAMA world bounds.
    // VI: Mặt đất được scale khớp với giới hạn thế giới GAMA.
    [SerializeField] protected GameObject Ground;


    // EN: Scaling and offset coefficients for GAMA ↔ Unity coordinate conversion.
    // VI: Hệ số co giãn và offset cho chuyển đổi tọa độ GAMA ↔ Unity.
    [Header("Coordinate conversion parameters")]
    [SerializeField] protected float GamaCRSCoefX = 1.0f;
    [SerializeField] protected float GamaCRSCoefY = 1.0f;
    [SerializeField] protected float GamaCRSOffsetX = 0.0f;
    [SerializeField] protected float GamaCRSOffsetY = 0.0f;
    // EN: Reference to the level/wave manager controlling spawns and timers.
    // VI: Tham chiếu đến level/wave manager điều khiển spawn và timer.
    [SerializeField] protected LevelManager levelManager;

    // EN: Reference to the game HUD/UI controller.
    // VI: Tham chiếu đến controller HUD/UI game.
    [SerializeField] protected GameUI gameUI;

    // EN: Player’s XR origin transform (used for position sync to GAMA).
    // VI: Transform XR origin của người chơi (dùng để đồng bộ vị trí lên GAMA).
    protected Transform XROrigin;

    // Z offset and scale
    [SerializeField] protected float GamaCRSOffsetZ = 0.0f;

    // EN: Objects whose position is tracked and sent back to GAMA via move_geoms_followed.
    // VI: Các đối tượng có vị trí được theo dõi và gửi lại GAMA qua move_geoms_followed.
    protected List<GameObject> toFollow;

    // EN: XR interaction manager for registering interactable objects.
    // VI: XR interaction manager để đăng ký các đối tượng tương tác.
    XRInteractionManager interactionManager;

    // ################################ EVENTS ################################
    // called when the current game state changes
    public static event Action<GameState> OnGameStateChanged;
    // called when the game is restarted
    //    public static event Action OnGameRestarted;

    // called when the world data is received
    //    public static event Action<WorldJSONInfo> OnWorldDataReceived;
    // ########################################################################

    // EN: Map of geometry name → [GameObject, PropertiesGAMA] for GAMA-created objects.
    // VI: Map tên hình học → [GameObject, PropertiesGAMA] cho các đối tượng GAMA tạo.
    protected Dictionary<string, List<object>> geometryMap;
    // EN: Map of property type ID → PropertiesGAMA definition.
    // VI: Map ID kiểu thuộc tính → định nghĩa PropertiesGAMA.
    protected Dictionary<string, PropertiesGAMA> propertyMap = null;

    // EN: Currently selected interactable objects.
    // VI: Các đối tượng tương tác đang được chọn.
    protected List<GameObject> SelectedObjects;


    // EN: Deferred flags — set by message handler, processed in FixedUpdate to avoid
    //     modifying Unity objects on the WebSocket callback thread.
    // VI: Cờ trì hoãn — được set bởi message handler, xử lý trong FixedUpdate để tránh
    //     thay đổi đối tượng Unity trên thread callback WebSocket.
    protected bool handleGeometriesRequested;
    protected bool handleGroundParametersRequested;

    // EN: Coordinate converter initialized from ConnectionParameter (GAMA CRS ↔ Unity).
    // VI: Bộ chuyển đổi tọa độ khởi tạo từ ConnectionParameter (GAMA CRS ↔ Unity).
    protected CoordinateConverter converter;
    // EN: Polygon mesh generator for GAMA geometries.
    // VI: Bộ tạo mesh polygon cho hình học GAMA.
    protected PolygonGenerator polyGen;
    // EN: Connection parameters received from GAMA (precision, world size, etc.).
    // VI: Tham số kết nối nhận từ GAMA (precision, kích thước thế giới, v.v.).
    protected ConnectionParameter parameters = null;
    // EN: Property definitions received from GAMA.
    // VI: Định nghĩa thuộc tính nhận từ GAMA.
    protected AllProperties propertiesGAMA;
    // EN: Initial world geometry data from GAMA.
    // VI: Dữ liệu hình học thế giới ban đầu từ GAMA.
    protected WorldJSONInfo infoWorld;
    // EN: Pending animation command from GAMA (processed in FixedUpdate then nulled).
    // VI: Lệnh animation đang chờ từ GAMA (xử lý trong FixedUpdate rồi null).
    protected AnimationInfo infoAnimation = null;
    // EN: Current game state in the state machine.
    // VI: Trạng thái game hiện tại trong máy trạng thái.
    public GameState currentState;

    // EN: Singleton instance.
    // VI: Instance singleton.
    public static SimulationManager Instance = null;


    // EN: Minimal delay between interactions.
    // VI: Khoảng thời gian tối thiểu giữa hai lần tương tác.
    protected float timeWithoutInteraction = 1.0f; //in second



    protected bool sendMessageToReactivatePositionSent = false;

    protected float maxTimePing = 1.0f;
    protected float currentTimePing = 0.0f;

    protected List<GameObject> toDelete;

    protected bool readyToSendPosition = false;

    protected bool readyToSendPositionInit = true;

    // EN: Base period for periodic position/state sync to GAMA.
    // VI: Chu kỳ cơ sở để đồng bộ vị trí/trạng thái định kỳ lên GAMA.
    protected float TimeSendPosition = 0.5f;
    // EN: Timer for enemy updates (staggered to avoid sending all packets at once).
    // VI: Bộ đếm gửi vị trí enemy (lệch pha để tránh dồn gói tin cùng lúc).
    protected float TimerSendPositionEnemy = 0.0f;



    // EN: Timer for fresh-water updates.
    // VI: Bộ đếm gửi cập nhật fresh-water.
    protected float TimerSendPositionFW = 0.0f;
    // EN: Timer for player position updates.
    // VI: Bộ đếm gửi cập nhật vị trí người chơi.
    protected float TimerSendPosition = 0.0f;

    protected List<GameObject> locomotion;
    protected MoveHorizontal mh = null;
    protected MoveVertical mv = null;

    // EN: Pending deferred data objects — set by inbound handler, consumed in FixedUpdate.
    //     Each is nulled after processing to avoid double-handling.
    // VI: Các đối tượng dữ liệu trì hoãn — set bởi handler đến, tiêu thụ trong FixedUpdate.
    //     Mỗi cái được null sau khi xử lý để tránh xử lý hai lần.
    protected DEMData data;
    protected DEMDataLoc dataLoc;
    protected TeleoportAreaInfo dataTeleport;
    protected WallInfo dataWall;
    protected EnableMoveInfo enableMove;
    protected FreshWaterSpawn infoPump;
    protected EnemySpawnerInfo infoEnemySp;
    protected SubsidenceInfo subsidenceInfo;

    // EN: UI button to start the game (disabled until GAMA says ready).
    // VI: Nút UI bắt đầu game (bị vô hiệu cho đến khi GAMA báo sẵn sàng).
    public Button StartButton;

    // EN: Registry of water pump barracks by InstanceID string — updated by GAMA spawn-rate messages.
    // VI: Sổ đăng ký pumper nước theo chuỗi InstanceID — cập nhật bởi message spawn-rate từ GAMA.
    private Dictionary<string, Barrack> waterPumps;
    // EN: Registry of enemy spawners by InstanceID string — updated by GAMA spawn-rate messages.
    // VI: Sổ đăng ký enemy spawner theo chuỗi InstanceID — cập nhật bởi message spawn-rate từ GAMA.
    private Dictionary<string, EnemySpawner> enemySpawners;
    // EN: Registry of tree barriers by InstanceID string — tracks planted trees that block enemies.
    // VI: Sổ đăng ký tree barrier theo chuỗi InstanceID — theo dõi cây trồng chặn enemy.
    private Dictionary<string, GameObject> treeBarriers;

    private bool sendReady = true;


    protected float TimeSendInit = 0.5f;
    protected float TimerSendInit;

    protected int RemainingTime = 0;

    protected StartGameParameters startGameParameters = null;

    private bool gameStarted = false;

    // ############################################ UNITY FUNCTIONS ############################################
    void Awake()
    {
        Instance = this;
        SelectedObjects = new List<GameObject>();
        // toDelete = new List<GameObject>();

        locomotion = new List<GameObject>(GameObject.FindGameObjectsWithTag("locomotion"));
        if (player != null)
        {
            mh = player.GetComponentInChildren<MoveHorizontal>();
            mv = player.GetComponentInChildren<MoveVertical>();
            XROrigin = player.transform;//.Find("XR Origin (XR Rig)");
        }
        playerMovement(false);
        toFollow = new List<GameObject>();
        waterPumps = new Dictionary<string, Barrack>();
        enemySpawners = new Dictionary<string, EnemySpawner>();
        treeBarriers = new Dictionary<string, GameObject>();

    }


    // EN: Subscribe to ConnectionManager events for server messages, connection state, and auth.
    // VI: Đăng ký event ConnectionManager cho message server, trạng thái kết nối và xác thực.
    void OnEnable()
    {
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnServerMessageReceived += HandleServerMessageReceived;
            ConnectionManager.Instance.OnConnectionAttempted += HandleConnectionAttempted;
            ConnectionManager.Instance.OnConnectionStateChanged += HandleConnectionStateChanged;
            Debug.Log("SimulationManager: OnEnable");
        }
        else
        {
            Debug.Log("No connection manager");
        }
    }

    // EN: Unsubscribe from ConnectionManager events to avoid memory leaks.
    // VI: Hủy đăng ký event ConnectionManager để tránh rò rỉ bộ nhớ.
    void OnDisable()
    {
        Debug.Log("SimulationManager: OnDisable");
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnServerMessageReceived -= HandleServerMessageReceived;
            ConnectionManager.Instance.OnConnectionAttempted -= HandleConnectionAttempted;
            ConnectionManager.Instance.OnConnectionStateChanged -= HandleConnectionStateChanged;
        }
    }

    void OnDestroy()
    {
        Debug.Log("SimulationManager: OnDestroy");
    }

    void Start()
    {
        // EN: Runtime initialization of maps/flags and initial stagger for timers.
        // VI: Khởi tạo map/cờ runtime và đặt lệch pha ban đầu cho các timer.
        geometryMap = new Dictionary<string, List<object>>();
        handleGeometriesRequested = false;
        // handlePlayerParametersRequested = false;
        handleGroundParametersRequested = false;
        if (player != null)
            interactionManager = player.GetComponentInChildren<XRInteractionManager>();
        OnEnable();
        TimerSendPositionEnemy = TimeSendPosition / 2.0f;
        TimerSendPosition = TimeSendPosition / 3.0f;

    }



    void FixedUpdate()
    {

        // EN: Physics-step pipeline for deferred operations and state-driven sync.
        // VI: Luồng xử lý theo bước vật lý cho tác vụ trì hoãn và đồng bộ theo trạng thái.

        if (sendMessageToReactivatePositionSent)
        {

            Dictionary<string, string> args = new Dictionary<string, string> {
            {"id",ConnectionManager.Instance.getUseMiddleware() ? ConnectionManager.Instance.GetConnectionId()  : ("\"" + ConnectionManager.Instance.GetConnectionId() +  "\"") }};

            ConnectionManager.Instance.SendExecutableAsk("player_position_updated", args);
            sendMessageToReactivatePositionSent = false;

        }
        if (handleGroundParametersRequested)
        {
            InitGroundParameters();
            handleGroundParametersRequested = false;

        }
        if (handleGeometriesRequested && infoWorld != null && infoWorld.isInit && propertyMap != null)
        {

            sendMessageToReactivatePositionSent = true;
            handleGeometriesRequested = false;
            UpdateGameState(GameState.GAME);

        }
        if (converter != null && data != null)
        {
            manageUpdateTerrain();
        }
        if (converter != null && dataLoc != null)
        {
            manageSetValueTerrain();
        }
        if (converter != null && dataTeleport != null)
        {
            manageTeleportationArea();
        }
        if (converter != null && dataWall != null)
        {
            manageWalls();
        }
        if (enableMove != null)
        {
            playerMovement(enableMove.enableMove);
            enableMove = null;
        }

        if (IsGameState(GameState.LOADING_DATA) && ConnectionManager.Instance.getUseMiddleware())
        {
            // EN: Keep requesting initial data while loading, at a fixed interval.
            // VI: Liên tục yêu cầu dữ liệu khởi tạo khi đang tải, theo chu kỳ cố định.
            if (TimerSendInit > 0)
                TimerSendInit -= Time.deltaTime;
            if (TimerSendInit <= 0)
            {
                TimerSendInit = TimeSendInit;
                Dictionary<string, string> args = new Dictionary<string, string> {
                             {"id", ConnectionManager.Instance.GetConnectionId() }
                        };
                ConnectionManager.Instance.SendExecutableAsk("send_init_data", args);
            }
        }



        if (infoAnimation != null)
        {
            updateAnimation();
            infoAnimation = null;
        }
        if (infoPump != null)
        {
            updateInfoSpawnRatePumper();
            infoPump = null;
        }
        if (infoEnemySp != null)
        {
            updateInfoSpawnRateEnemy();
            infoEnemySp = null;
        }

        if (subsidenceInfo != null)
        {
            updateSubsidence();
            subsidenceInfo = null;
        }





    }

    private void Update()
    {

        // EN: Frame-step pipeline for input, reconnect, and periodic outbound messages.
        // VI: Luồng xử lý theo frame cho input, reconnect và gửi message định kỳ.



        if (currentTimePing > 0)
        {
            currentTimePing -= Time.deltaTime;
            if (currentTimePing <= 0)
            {
                Debug.Log("Try to reconnect to the server");
                ConnectionManager.Instance.Reconnect();
            }
        }


        if (primaryRightHandButton != null && primaryRightHandButton.action.triggered)
        {
            TriggerMainButton();
        }
        if (TryReconnectButton != null && TryReconnectButton.action.triggered)
        {
            Debug.Log("TryReconnectButton activated");
            TryReconnect();
        }
        if (IsGameState(GameState.GAME))
        {
            // EN: Three independent timers to spread network traffic by data type.
            // VI: Ba timer độc lập để phân tải lưu lượng mạng theo từng loại dữ liệu.

            if (TimerSendPositionEnemy > 0)
            {
                TimerSendPositionEnemy -= Time.deltaTime;
            }
            if (TimerSendPositionFW > 0)
            {
                TimerSendPositionFW -= Time.deltaTime;
            }
            if (TimerSendPosition > 0)
            {
                TimerSendPosition -= Time.deltaTime;
            }
            if (TimerSendPositionEnemy <= 0)
            {
                sendEnemies();
                TimerSendPositionEnemy = TimeSendPosition;
            }
            if (TimerSendPositionFW <= 0)
            {
                sendFreshWater();
                TimerSendPositionFW = TimeSendPosition;
            }
            if (TimerSendPosition <= 0 && (gameUI == null || !gameUI.endDone))
            {
                updatePlayerPos();
                TimerSendPosition = TimeSendPosition;
            }

        }
        if (!sendReady)
        {
            sendReadyToGAMA();
        }
        if (startGameParameters != null && !gameStarted)
        {
            startGameWithTime();
            startGameParameters = null;
        }

        OtherUpdate();
    }

    public void startGameWithTime()
    {
        gameStarted = true;
        Debug.Log("START GAME");
        if (levelManager != null)
            levelManager.setWaveTime(startGameParameters.time_prep, startGameParameters.time_def);
        if (gameUI != null)
            gameUI.StartUI();

    }

    public void SendEndMessageToGAMA()
    {
        // EN: Notify server that this player has completed the game.
        // VI: Thông báo lên server rằng người chơi đã kết thúc game.
        Debug.Log("END OF GAME");
        if (StartButton != null)
            StartButton.interactable = true;
        Dictionary<string, string> args = new Dictionary<string, string> {
                    {"idP", ConnectionManager.Instance.GetConnectionId()} };

        ConnectionManager.Instance.SendExecutableAsk("player_finish_game", args);

    }

    public void sendReadyToGAMA()
    {
        // EN: One-shot readiness handshake before gameplay starts.
        // VI: Bắt tay trạng thái sẵn sàng một lần trước khi vào gameplay.
        if (StartButton != null && StartButton.interactable == false)
        {
            StartButton.interactable = true;
            Dictionary<string, string> args = new Dictionary<string, string> {
                    {"idP", ConnectionManager.Instance.GetConnectionId()} };

            ConnectionManager.Instance.SendExecutableAsk("player_ready", args);

        }
        sendReady = true;

    }

    public void ChangeState(string NewState)
    {
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"new_state", NewState }

        };

        ConnectionManager.Instance.SendExecutableAsk("change_state", args);
    }

    public void sendFreshWater()
    {

        // EN: Collect active ally objects and send compact CSV-like payloads.
        // VI: Thu thập object ally đang hoạt động và gửi payload dạng chuỗi nén kiểu CSV.

        GameObject[] freshWater = GameObject.FindGameObjectsWithTag("Ally");
        // action update_salty_water(string idP, string swsStr, string xsStr, string ysStr)


        string sws = ",";
        string xs = "";
        string ys = "";
        bool isFirst = true;
        foreach (GameObject t in freshWater)
        {
            if (!t.activeSelf) continue;

            if (isFirst)
            {
                sws += (t.GetInstanceID());
                xs += (int)(t.transform.position.x * parameters.precision);
                ys += (int)(t.transform.position.z * parameters.precision);
                isFirst = false;
            }
            else
            {
                sws += "," + (t.GetInstanceID());
                xs += "," + (int)(t.transform.position.x * parameters.precision);
                ys += "," + (int)(t.transform.position.z * parameters.precision);
            }
        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"fwsStr", sws },
              {"xsStr", xs },
              {"ysStr",ys}

        };

        Debug.Log($"[GAMA Send] update_fresh_water | allies count={sws.Split(',').Length - 1}");
        ConnectionManager.Instance.SendExecutableAsk("update_fresh_water", args);

    }

    public void updatePlayerPos()
    {

        // EN: Send player pose + gameplay KPIs to GAMA each cycle.
        // VI: Gửi tư thế người chơi + KPI gameplay lên GAMA theo chu kỳ.

        if (XROrigin == null || parameters == null)
        {
            return;
        }

        if (gameUI != null)
            gameUI.computeScore();

        //action update_player_pos(string idP, int x, int y, int o)
        Vector2 vF = new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.z);
        Vector2 vR = new Vector2(transform.forward.x, transform.forward.z);
        vF.Normalize();
        vR.Normalize();
        float c = vF.x * vR.x + vF.y * vR.y;
        float s = vF.x * vR.y - vF.y * vR.x;
        int angle = (int)(((s > 0) ? -1.0 : 1.0) * (180 / Math.PI) * Math.Acos(c) * parameters.precision);
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"x", ""+XROrigin.localPosition.x * parameters.precision },
              {"y",""+XROrigin.localPosition.z * parameters.precision},
               {"o",angle+"" },
            {"remaining_time", levelManager != null ? ((int) levelManager.CurrentTime)+"" : "0" },
            {"dtree", gameUI != null ? ((int) gameUI.DeadTreeNumber)+"" : "0" },
            {"fwater", gameUI != null ? ((int) gameUI.TotalNeutralWater)+"" : "0" },
            {"score", gameUI != null ? ((float) gameUI.ScoreGame)+"" : "0" },
            {"name_crop", "Rice:Durian:Shrimp" },
            {"life_tree", GetLifeTree()},
            {"quanlity", GetHarvestQuantity()}


        };
        
        ConnectionManager.Instance.SendExecutableAsk("update_player_pos", args);
        Debug.Log($"[GAMA Send] update_player_pos | score={args["score"]}, remaining_time={args["remaining_time"]}, dtree={args["dtree"]}, fwater={args["fwater"]}, life_tree={args["life_tree"]}, quanlity={args["quanlity"]}");
    }

    // EN: Returns remaining (alive) count per product: "rice_remaining:durian_remaining:shrimp_remaining".
    //     Uses SeasonalSummary (tracks ALL harvest sources) instead of David_Fruit._harvestCounts
    //     which only tracks Durian (Rice/Shrimp use separate scripts).
    // VI: Trả về số còn tồn tại mỗi SP: "lúa_còn:sầu_riêng_còn:tôm_còn".
    //     Dùng SeasonalSummary (theo dõi TẤT CẢ nguồn thu hoạch) thay vì David_Fruit._harvestCounts
    //     vốn chỉ theo dõi Sầu riêng (Lúa/Tôm dùng script riêng).
    private string GetLifeTree()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        int riceHarvested   = summary != null ? summary.GetTotalHarvestCount("Rice") : 0;
        int durianHarvested = summary != null ? summary.GetTotalHarvestCount("Durian") : 0;
        int shrimpHarvested = summary != null ? summary.GetTotalHarvestCount("Shrimp") : 0;
        int riceLife   = 25 - riceHarvested;
        int durianLife = 15 - durianHarvested;
        int shrimpLife = 5  - shrimpHarvested;
        return Mathf.Max(0, riceLife) + ":" + Mathf.Max(0, durianLife) + ":" + Mathf.Max(0, shrimpLife);
    }

    // EN: Returns harvested count per product: "rice_harvested:durian_harvested:shrimp_harvested".
    // VI: Trả về số đã thu hoạch mỗi SP: "lúa_đã_thu:sầu_riêng_đã_thu:tôm_đã_thu".
    private string GetHarvestQuantity()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        int rice   = summary != null ? summary.GetTotalHarvestCount("Rice") : 0;
        int durian = summary != null ? summary.GetTotalHarvestCount("Durian") : 0;
        int shrimp = summary != null ? summary.GetTotalHarvestCount("Shrimp") : 0;
        return rice + ":" + durian + ":" + shrimp;
    }


    public void createEnemySpawner()
    {
        // EN: Register all enemy spawners and send their initial positions.
        // VI: Đăng ký toàn bộ enemy spawner và gửi vị trí ban đầu.
        if (ConnectionManager.Instance == null)
        {
            Debug.LogWarning("[SimulationManager] createEnemySpawner skipped — no GAMA connection.");
            return;
        }
        if (levelManager == null)
        {
            Debug.LogWarning("[SimulationManager] createEnemySpawner skipped — no LevelManager.");
            return;
        }
        List<EnemySpawner> spawns = levelManager.Spawns;
        string idTs = ",";
        string xs = "";
        string ys = "";
        bool isFirst = true;
        enemySpawners = new Dictionary<string, EnemySpawner>();
        foreach (EnemySpawner s in spawns)
        {

            GameObject t = s.gameObject;
            string spawnKey = t.GetInstanceID() + "";
            if (enemySpawners.ContainsKey(spawnKey))
            {
                Debug.LogWarning("[SimulationManager] Duplicate EnemySpawner InstanceID skipped: " + spawnKey);
                continue;
            }
            enemySpawners.Add(spawnKey, t.GetComponent<EnemySpawner>());

            if (isFirst)
            {
                idTs += (t.GetInstanceID());
                xs += (int)(t.transform.position.x * parameters.precision);
                ys += (int)(t.transform.position.z * parameters.precision);
                isFirst = false;
            }
            else
            {
                idTs += "," + (t.GetInstanceID());
                xs += "," + (int)(t.transform.position.x * parameters.precision);
                ys += "," + (int)(t.transform.position.z * parameters.precision);
            }

        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"idESStr", idTs },
              {"xsStr", xs },
              {"ysStr",ys}

        };

        Debug.Log($"[GAMA Send] create_enemy_spawners | spawners count={idTs.Split(',').Length - 1}");
        ConnectionManager.Instance.SendExecutableAsk("create_enemy_spawners", args);
    }


    public void createMovePumper(GameObject pumper)
    {

        waterPumps.Add(pumper.GetInstanceID() + "", pumper.GetComponent<Barrack>());
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"idwp", pumper.GetInstanceID()+"" },
              {"x", ""+pumper.transform.position.x * parameters.precision },
              {"y",""+pumper.transform.position.z * parameters.precision}

        };

        
        ConnectionManager.Instance.SendExecutableAsk("move_create_pumper", args);
        Debug.Log($"[GAMA Send] move_create_pumper | idP={args["idP"]}, idwp={args["idwp"]}, x={args["x"]}, y={args["y"]}");
    }

    // =========================================================================
    // TREE BARRIER REGISTRATION — Đăng ký cây trồng chặn nước mặn với GAMA.
    // =========================================================================

    /// <summary>
    /// EN: Register a new TreeBarrier with GAMA server when planted.
    /// VI: Đăng ký TreeBarrier mới với GAMA server khi trồng.
    /// </summary>
    public void createTreeBarrier(GameObject treeBarrier)
    {
        if (ConnectionManager.Instance == null)
        {
            Debug.LogWarning("[SimulationManager] createTreeBarrier skipped — no GAMA connection.");
            return;
        }
        if (parameters == null)
        {
            Debug.LogWarning("[SimulationManager] createTreeBarrier skipped — parameters not initialized.");
            return;
        }

        string key = treeBarrier.GetInstanceID() + "";
        if (treeBarriers.ContainsKey(key))
        {
            Debug.LogWarning($"[SimulationManager] Duplicate TreeBarrier InstanceID skipped: {key}");
            return;
        }
        treeBarriers.Add(key, treeBarrier);

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
            {"idtb", treeBarrier.GetInstanceID() + ""},
            {"x", "" + treeBarrier.transform.position.x * parameters.precision},
            {"y", "" + treeBarrier.transform.position.z * parameters.precision}
        };

        ConnectionManager.Instance.SendExecutableAsk("create_tree_barrier", args);
        Debug.Log($"[GAMA Send] create_tree_barrier | idP={args["idP"]}, idtb={args["idtb"]}, x={args["x"]}, y={args["y"]}");
    }

    /// <summary>
    /// EN: Notify GAMA server when a TreeBarrier dies.
    /// VI: Thông báo GAMA server khi TreeBarrier chết.
    /// </summary>
    public void deleteTreeBarrier(GameObject treeBarrier)
    {
        if (ConnectionManager.Instance == null)
        {
            Debug.LogWarning("[SimulationManager] deleteTreeBarrier skipped — no GAMA connection.");
            return;
        }

        string key = treeBarrier.GetInstanceID() + "";
        if (treeBarriers.ContainsKey(key))
        {
            treeBarriers.Remove(key);
        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
            {"idtb", treeBarrier.GetInstanceID() + ""}
        };

        ConnectionManager.Instance.SendExecutableAsk("delete_tree_barrier", args);
        Debug.Log($"[GAMA Send] delete_tree_barrier | idP={args["idP"]}, idtb={args["idtb"]}");
    }

    public void sendEnemies()
    {

        // EN: Periodic enemy position sync.
        // VI: Đồng bộ vị trí enemy theo chu kỳ.

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        // action update_salty_water(string idP, string swsStr, string xsStr, string ysStr)


        string sws = ",";
        string xs = "";
        string ys = "";

        bool isFirst = true;

        foreach (GameObject t in enemies)
        {
            if (!t.activeSelf) continue;
            if (isFirst)
            {
                sws += (t.GetInstanceID());
                xs += (int)(t.transform.position.x * parameters.precision);
                ys += (int)(t.transform.position.z * parameters.precision);
                isFirst = false;
            }
            else
            {
                sws += "," + (t.GetInstanceID());
                xs += "," + (int)(t.transform.position.x * parameters.precision);
                ys += "," + (int)(t.transform.position.z * parameters.precision);
            }

        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"swsStr", sws },
              {"xsStr", xs },
              {"ysStr",ys}

        };

        Debug.Log($"[GAMA Send] update_salty_water | enemies count={sws.Split(',').Length - 1}");
        ConnectionManager.Instance.SendExecutableAsk("update_salty_water", args);

    }

    public void sendTrees()
    {
        // EN: Send active tree list and coordinates to server.
        // VI: Gửi danh sách cây đang hoạt động và tọa độ lên server.
        if (ConnectionManager.Instance == null)
        {
            Debug.LogWarning("[SimulationManager] sendTrees skipped — no GAMA connection.");
            return;
        }
        Debug.Log("SEND TREES TO GAMA");


        bool isFirst = true;

        string idTs = ",";
        string xs = "";
        string ys = "";
        foreach (GameObject t in GameObject.FindGameObjectsWithTag("Tree"))
        {
            if (!t.gameObject.activeSelf)
                continue;
            if (isFirst)
            {
                idTs += (t.GetInstanceID());
                xs += (int)(t.transform.position.x * parameters.precision);
                ys += (int)(t.transform.position.z * parameters.precision);
                isFirst = false;
            }
            else
            {
                idTs += "," + (t.GetInstanceID());
                xs += "," + (int)(t.transform.position.x * parameters.precision);
                ys += "," + (int)(t.transform.position.z * parameters.precision);
            }

        }

        Dictionary<string, string> args = new Dictionary<string, string> {
            {"idP", ConnectionManager.Instance.GetConnectionId()},
             {"idTsStr", idTs },
              {"xsStr", xs },
              {"ysStr",ys}

        };

        Debug.Log($"[GAMA Send] create_trees | trees count={idTs.Split(',').Length - 1}");
        ConnectionManager.Instance.SendExecutableAsk("create_trees", args);
        Debug.Log("Finish SEND TREES TO GAMA");
    }

    // EN: Update subsidence data on the SubsidenceManager component.
    // VI: Cập nhật dữ liệu sụt lún lên component SubsidenceManager.
    private void updateSubsidence()
    {
        SubsidenceManager subMan = GameObject.FindGameObjectWithTag("subsidenceManager").GetComponent<SubsidenceManager>();
        subMan.SubsidenceScore = subsidenceInfo.subsi_score;
        subMan.RemainingWaterLevelLocal = (0.0f + subsidenceInfo.waterLocal) / parameters.precision;
        subMan.RemainingWaterLevelGlobal = (0.0f + subsidenceInfo.waterGlobal) / parameters.precision;
        // Debug.Log("" + subMan.RemainingWaterLevelLocal);
    }
    // EN: Apply GAMA-sent spawn rates to each registered EnemySpawner and restart their auto-spawn.
    // VI: Áp dụng spawn rate từ GAMA cho từng EnemySpawner đã đăng ký và khởi động lại auto-spawn.
    private void updateInfoSpawnRateEnemy()
    {
        for (int i = 0; i < infoEnemySp.enemyspawners.Count; i++)
        {
            EnemySpawner es = enemySpawners[infoEnemySp.enemyspawners[i]];
            es.SpawnRate = (0.0f + infoEnemySp.spawnrates[i]) / parameters.precision;
            es.ReStartAutoSpawn(1);
        }
    }

    // EN: Apply GAMA-sent spawn rates to each registered water pumper (Barrack).
    //     Note: the rate is halved before applying.
    // VI: Áp dụng spawn rate từ GAMA cho từng pumper nước (Barrack) đã đăng ký.
    //     Lưu ý: tốc độ được chia 2 trước khi áp dụng.
    private void updateInfoSpawnRatePumper()
    {
        for (int i = 0; i < infoPump.pumpers.Count; i++)
        {
            Barrack b = waterPumps[infoPump.pumpers[i]];
            b.SpawnRate = (0.0f + infoPump.spawnrates[i] / 2) / parameters.precision;
        }
    }

    // EN: Apply GAMA animation commands to named geometry objects.
    //     Sets Animator parameters (int/float/bool) and fires triggers.
    // VI: Áp dụng lệnh animation GAMA lên các đối tượng geometry được đặt tên.
    //     Set tham số Animator (int/float/bool) và kích hoạt trigger.
    private void updateAnimation()
    {

        foreach (String n in infoAnimation.names)
        {
            if (!geometryMap.ContainsKey(n)) continue;
            List<object> o = geometryMap[n];

            if (o == null && o.Count == 0) continue;
            GameObject obj = (GameObject)o[0];

            Animator m_animator = obj.GetComponent<Animator>();
            if (m_animator == null)
            {
                m_animator = obj.GetComponentInChildren<Animator>();
            }

            if (m_animator != null)
            {
                foreach (ParameterVal p in infoAnimation.parameters)
                {
                    if (p.type.Equals("int"))
                        m_animator.SetInteger(p.key, p.intVal);
                    else if (p.type.Equals("float"))
                        m_animator.SetFloat(p.key, p.floatVal);
                    else if (p.type.Equals("bool"))
                        m_animator.SetBool(p.key, p.boolVal);
                }
                foreach (String t in infoAnimation.triggers)
                {
                    m_animator.SetTrigger(t);

                }
            }

        }

    }
    // EN: Build or rebuild a TeleportationArea from GAMA polygon data.
    //     Creates extruded meshes and adds MeshColliders for XR teleportation.
    // VI: Xây hoặc xây lại TeleportationArea từ dữ liệu polygon GAMA.
    //     Tạo mesh đùn và thêm MeshCollider cho teleportation XR.
    private void manageTeleportationArea()
    {
        if (polyGen == null)
        {
            polyGen = PolygonGenerator.GetInstance();
            polyGen.Init(converter);
        }
        TeleportationArea ta = null;
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Teleportation");
        foreach (GameObject o in objs)
        {
            if (o.name.Equals(dataTeleport.teleportId))
            {
                ta = o.GetComponent<TeleportationArea>();
                if (ta != null)
                {
                    foreach (Collider col in ta.colliders)
                    {
                        GameObject.DestroyImmediate(col.gameObject);
                    }
                    ta.colliders.Clear();
                }
                break;

            }
        }
        if (ta == null)
        {
            GameObject prefabObj = Resources.Load("Prefabs/Player/TeleportAreaRaw") as GameObject;
            GameObject obj = Instantiate(prefabObj);

            ta = obj.GetComponent<TeleportationArea>();
            obj.name = dataTeleport.teleportId;
            obj.tag = "Teleportation";
        }


        for (int i = 0; i < dataTeleport.pointsGeom.Count; i++)
        {
            List<int> pt = dataTeleport.pointsGeom[i].c;
            float YoffSet = (0.0f + dataTeleport.offsetYGeom[i]) / (0.0f + parameters.precision);

            PropertiesGAMA prop = new PropertiesGAMA();
            prop.id = dataTeleport.teleportId + "_" + i;
            prop.hasCollider = true;
            prop.isInteractable = false;
            prop.isGrabable = false;
            prop.hasPrefab = false;
            prop.visible = true;
            prop.is3D = true;
            prop.height = dataTeleport.height;
            prop.toFollow = false;

            GameObject obj = polyGen.GeneratePolygons(false, prop.id, pt, prop, parameters.precision);

            obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y + YoffSet, obj.transform.position.z);
            MeshCollider mc = obj.AddComponent<MeshCollider>();
            mc.sharedMesh = polyGen.bottomMesh;
            obj.transform.parent = ta.gameObject.transform;
            ta.colliders.Add(mc);


        }
        //to take into account the new colliders
        ta.enabled = false;
        ta.enabled = true;

        dataTeleport = null;
    }

    // EN: Build invisible wall colliders from GAMA data.
    //     NOTE: Currently disabled (entire body is commented out).
    // VI: Xây collider tường vô hình từ dữ liệu GAMA.
    //     GHI CHÚ: Hiện đang bị vô hiệu (toàn bộ thân hàm bị comment).
    private void manageWalls()
    {

        //    if (polyGen == null)
        //     {
        //         polyGen = PolygonGenerator.GetInstance();
        //         polyGen.Init(converter);
        //     }

        //     GameObject wallObj = new GameObject("Walls");

        //     GameObject[] objs =   GameObject.FindGameObjectsWithTag("InvisibleWall");
        //     foreach (GameObject o in objs)
        //     {
        //         if (o.name.Equals(dataWall.wallId))
        //         GameObject.DestroyImmediate(o);

        //     }

        //     for (int i = 0; i < dataWall.pointsGeom.Count;i++ )
        //     {
        //         List<int> pt = dataWall.pointsGeom[i].c;
        //         float YoffSet = (0.0f + dataWall.offsetYGeom[i]) / (0.0f + parameters.precision);

        //         PropertiesGAMA prop = new PropertiesGAMA();
        //         prop.id = dataWall.wallId;
        //         prop.hasCollider = true;
        //         prop.tag = "InvisibleWall";
        //         prop.isInteractable = false;
        //         prop.isGrabable = false;
        //         prop.hasPrefab = false;
        //         prop.visible = false;
        //         prop.height = dataWall.height;
        //         prop.is3D = true;
        //         prop.toFollow = false;

        //        GameObject obj = polyGen.GeneratePolygons(false, dataWall.wallId, pt, prop, parameters.precision);

        //         obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y + YoffSet, obj.transform.position.z);
        //         obj.transform.parent = wallObj.transform;
        //         MeshCollider mc = obj.AddComponent<MeshCollider>();
        //         mc.sharedMesh = polyGen.surroundMesh;

        //     }

        //     dataWall = null;
    }


    // EN: Apply a partial DEM heightmap patch at (indexX, indexY) on the named Terrain.
    //     If the new valMax exceeds the current terrain height, rescales existing heights.
    // VI: Áp dụng miếng vá heightmap DEM tại (indexX, indexY) trên Terrain được đặt tên.
    //     Nếu valMax mới vượt quá chiều cao terrain hiện tại, co giãn lại các height hiện có.
    private void manageSetValueTerrain()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (dataLoc.rows.Count == 0) return;
        foreach (Terrain t in terrains)
        {

            if (t.name == dataLoc.id)
            {
                float valMax = t.terrainData.size.y;

                int resolution = t.terrainData.heightmapResolution;

                if (dataLoc.valMax > valMax)
                {
                    float oldV = valMax;
                    valMax = dataLoc.valMax;
                    float[,] heightsT = new float[t.terrainData.heightmapResolution, t.terrainData.heightmapResolution];
                    for (int j = 0; j < resolution; j++)
                    {
                        for (int i = 0; i < resolution; i++)
                        {
                            float v = t.terrainData.GetHeight(i, j);
                            heightsT[i, j] = v * oldV / valMax;
                        }
                    }

                    t.terrainData.SetHeights(0, 0, heightsT);
                }
                float[,] heights = new float[dataLoc.rows[0].h.Count, dataLoc.rows.Count];
                int x = 1;
                foreach (Row r in dataLoc.rows)
                {
                    int y = 0;
                    foreach (int v in r.h)
                    {
                        heights[dataLoc.rows.Count - x, y] = ((v + 0.0f) / (valMax + 0.0f));
                        y++;
                    }
                    x++;
                }

                t.terrainData.SetHeights(dataLoc.indexX, resolution - 1 - dataLoc.indexY, heights);
                break;
            }
        }
        dataLoc = null;
    }

    // EN: Replace the entire heightmap of the named Terrain with full DEM data from GAMA.
    //     Also repositions and resizes the terrain to match GAMA world dimensions.
    // VI: Thay thế toàn bộ heightmap của Terrain được đặt tên bằng dữ liệu DEM đầy đủ từ GAMA.
    //     Cũng định vị lại và thay đổi kích thước terrain cho khớp với kích thước thế giới GAMA.
    private void manageUpdateTerrain()
    {
        Terrain[] terrains = Terrain.activeTerrains;

        foreach (Terrain t in terrains)
        {

            if (t.name == data.id)
            {
                t.gameObject.transform.position = new Vector3(0, 0, -1 * data.sizeY);
                t.terrainData.size = new Vector3(data.sizeX, data.valMax, data.sizeY);
                float[,] heights = new float[t.terrainData.heightmapResolution, t.terrainData.heightmapResolution];
                int x = 1;
                foreach (Row r in data.rows)
                {
                    int y = 0;
                    foreach (int v in r.h)
                    {
                        heights[data.rows.Count - x, y] = ((v + 0.0f) / (data.valMax + 0.0f));

                        y++;
                    }
                    x++;
                }
                t.terrainData.SetHeights(0, 0, heights);

                break;
            }
        }
        data = null;
    }


    // EN: Enable or disable player locomotion (horizontal/vertical movement + locomotion GameObjects).
    // VI: Bật hoặc tắt di chuyển người chơi (di chuyển ngang/dọc + các GameObject locomotion).
    void playerMovement(Boolean active)
    {
        foreach (GameObject loc in locomotion)
        {
            loc.SetActive(active);
        }
        if (mh != null)
        {
            mh.enabled = active;
        }
        if (mv != null)
        {
            mv.enabled = active;
        }
        readyToSendPositionInit = active;
    }




    // ############################################ GAMESTATE UPDATER ############################################
    public void UpdateGameState(GameState newState)
    {

        // EN: Centralized game-state transition + side effects (requests/notifications).
        // VI: Chuyển trạng thái game tập trung + tác vụ phụ (request/thông báo).

        switch (newState)
        {
            case GameState.MENU:
                Debug.Log("SimulationManager: UpdateGameState -> MENU");
                break;

            case GameState.WAITING:
                Debug.Log("SimulationManager: UpdateGameState -> WAITING");
                break;


            case GameState.LOADING_DATA:
                Debug.Log("SimulationManager: UpdateGameState -> LOADING_DATA");
                if (ConnectionManager.Instance.getUseMiddleware())
                {
                    Dictionary<string, string> args = new Dictionary<string, string> {
                         {"id", ConnectionManager.Instance.GetConnectionId() }
                    };
                    ConnectionManager.Instance.SendExecutableAsk("send_init_data", args);
                }
                TimerSendInit = TimeSendInit;
                break;

            case GameState.GAME:
                Debug.Log("SimulationManager: UpdateGameState -> GAME");
                if (ConnectionManager.Instance.getUseMiddleware())
                {
                    Dictionary<string, string> args = new Dictionary<string, string> {
                         {"id", ConnectionManager.Instance.GetConnectionId() }
                    };
                    ConnectionManager.Instance.SendExecutableAsk("player_ready_to_receive_geometries", args);
                }
                break;

            case GameState.END:
                Debug.Log("SimulationManager: UpdateGameState -> END");
                break;

            case GameState.CRASH:
                Debug.Log("SimulationManager: UpdateGameState -> CRASH");
                break;

            default:
                Debug.Log("SimulationManager: UpdateGameState -> UNKNOWN");
                break;
        }

        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }



    // ############################# INITIALIZERS ####################################


    // EN: Scale and position the Ground plane to match the GAMA world bounding box.
    // VI: Co giãn và đặt vị trí mặt đất (Ground) cho khớp bounding box thế giới GAMA.
    private void InitGroundParameters()
    {
        Debug.Log("GroundParameters : Beginnig ground initialization");
        if (Ground == null)
        {
            // Debug.LogError("SimulationManager: Ground not set");
            return;
        }
        Vector3 ls = converter.fromGAMACRS(parameters.world[0], parameters.world[1], 0);

        if (ls.z < 0)
            ls.z = -ls.z;
        if (ls.x < 0)
            ls.x = -ls.x;
        ls.y = Ground.transform.localScale.y;

        Ground.transform.localScale = ls;
        Vector3 ps = converter.fromGAMACRS(parameters.world[0] / 2, parameters.world[1] / 2, 0);

        Ground.transform.position = ps;
        Debug.Log("SimulationManager: Ground parameters initialized");
    }


    // EN: Send tracked "toFollow" object positions back to GAMA as a batch.
    //     Format: separator-delimited names and coordinates.
    // VI: Gửi lô vị trí các đối tượng "toFollow" đang theo dõi lại GAMA.
    //     Định dạng: tên và tọa độ phân cách bằng ký tự ngăn.
    private void UpdateGameToFollowPosition()
    {
        if (toFollow.Count > 0)
        {


            String names = "";
            String points = "";
            string sep = ConnectionManager.Instance.MessageSeparator;

            foreach (GameObject obj in toFollow)
            {
                names += obj.name + sep;
                List<int> p = converter.toGAMACRS3D(obj.transform.position);

                points += p[0] + sep;

                points += p[1] + sep;
                points += p[2] + sep;

            }
            Dictionary<string, string> args = new Dictionary<string, string> {
            {"ids", names  },
            {"points", points},
            {"sep", sep}
            };

            ConnectionManager.Instance.SendExecutableAsk("move_geoms_followed", args);

        }
    }


    // EN: Alternative player position sender using CoordinateConverter (3D with GAMA CRS).
    //     NOTE: This method is NOT called in the Update loop — appears to be dead code.
    //     The active version is updatePlayerPos() which sends raw Unity coords * precision.
    // VI: Phương thức gửi vị trí người chơi thay thế dùng CoordinateConverter (3D với GAMA CRS).
    //     GHI CHÚ: Phương thức này KHÔNG được gọi trong Update loop — có vẻ là dead code.
    //     Phiên bản đang dùng là updatePlayerPos() gửi tọa độ Unity thô * precision.
    private void UpdatePlayerPosition()
    {
        Vector2 vF = new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.z);
        Vector2 vR = new Vector2(transform.forward.x, transform.forward.z);
        vF.Normalize();
        vR.Normalize();
        float c = vF.x * vR.x + vF.y * vR.y;
        float s = vF.x * vR.y - vF.y * vR.x;
        int angle = (int)(((s > 0) ? -1.0 : 1.0) * (180 / Math.PI) * Math.Acos(c) * parameters.precision);



        //  Vector3 v = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - yOffsetCamera, Camera.main.transform.position.z);
        Vector3 v = new Vector3(XROrigin.localPosition.x, XROrigin.localPosition.y, XROrigin.localPosition.z);

        List<int> p = converter.toGAMACRS3D(v);
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"id",ConnectionManager.Instance.getUseMiddleware() ? ConnectionManager.Instance.GetConnectionId()  : ("\"" + ConnectionManager.Instance.GetConnectionId() +  "\"") },
            {"x", "" +p[0]},
            {"y", "" +p[1]},
            {"z", "" +p[2]},
            {"angle", "" +angle}
        };


        ConnectionManager.Instance.SendExecutableAsk("move_player_external", args);

    }


    // EN: Configure a newly created/instantiated GameObject: set name, tag,
    //     add to toFollow list if needed, and wire up XR interaction components.
    // VI: Cấu hình GameObject mới tạo/khởi tạo: đặt tên, tag,
    //     thêm vào danh sách toFollow nếu cần, và gắn các component tương tác XR.
    private void instantiateGO(GameObject obj, String name, PropertiesGAMA prop)
    {
        obj.name = name;
        if (prop.toFollow)
        {
            toFollow.Add(obj);
        }
        if (prop.tag != null && !string.IsNullOrEmpty(prop.tag))
            obj.tag = prop.tag;

        if (prop.isInteractable)
        {
            XRBaseInteractable interaction = null;
            if (prop.isGrabable)
            {
                interaction = obj.AddComponent<XRGrabInteractable>();
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (prop.constraints != null && prop.constraints.Count == 6)
                {
                    if (prop.constraints[0])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionX;
                    if (prop.constraints[1])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionY;
                    if (prop.constraints[2])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionZ;
                    if (prop.constraints[3])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezeRotationX;
                    if (prop.constraints[4])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezeRotationY;
                    if (prop.constraints[5])
                        rb.constraints = rb.constraints | RigidbodyConstraints.FreezeRotationZ;
                }


            }
            else
            {

                interaction = obj.AddComponent<XRSimpleInteractable>();


            }
            if (interaction.colliders.Count == 0)
            {
                Collider[] cs = obj.GetComponentsInChildren<Collider>();
                if (cs != null)
                {
                    foreach (Collider c in cs)
                    {
                        interaction.colliders.Add(c);
                    }
                }
            }
            interaction.interactionManager = interactionManager;
            interaction.selectEntered.AddListener(SelectInteraction);
            interaction.firstHoverEntered.AddListener(HoverEnterInteraction);
            interaction.hoverExited.AddListener(HoverExitInteraction);

        }
    }



    // EN: Load a prefab from Resources, scale it, add colliders, register in geometryMap,
    //     and configure interaction via instantiateGO.
    // VI: Tải prefab từ Resources, scale, thêm collider, đăng ký vào geometryMap,
    //     và cấu hình tương tác qua instantiateGO.
    private GameObject instantiatePrefab(String name, PropertiesGAMA prop, bool initGame)
    {
        if (prop.prefabObj == null)
        {
            prop.loadPrefab(parameters.precision);
        }
        GameObject obj = Instantiate(prop.prefabObj);
        float scale = ((float)prop.size) / parameters.precision;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        obj.SetActive(true);

        if (prop.hasCollider)
        {
            if (obj.TryGetComponent<LODGroup>(out var lod))
            {
                foreach (LOD l in lod.GetLODs())
                {
                    GameObject b = l.renderers[0].gameObject;
                    Collider c = b.GetComponent<Collider>();
                    if (c == null)
                    {
                        BoxCollider bc = b.AddComponent<BoxCollider>();
                    }
                    // b.tag = obj.tag;
                    // b.name = obj.name;
                    //bc.isTrigger = prop.isTrigger;
                }

            }
            else
            {
                Collider c = obj.GetComponent<Collider>();
                if (c == null)
                {
                    BoxCollider bc = obj.AddComponent<BoxCollider>();
                }

                // bc.isTrigger = prop.isTrigger;
            }
        }
        List<object> pL = new List<object>();
        pL.Add(obj); pL.Add(prop);
        if (!initGame) geometryMap.Add(name, pL);
        instantiateGO(obj, name, prop);
        return obj;
    }






    // ############################################# HANDLERS ########################################
    // EN: When the middleware authenticates this player, transition to LOADING_DATA.
    // VI: Khi middleware xác thực người chơi này, chuyển sang trạng thái LOADING_DATA.
    private void HandleConnectionStateChanged(ConnectionState state)
    {
        Debug.Log("HandleConnectionStateChanged: " + state);
        // player has been added to the simulation by the middleware
        if (state == ConnectionState.AUTHENTICATED)
        {
            Debug.Log("SimulationManager: Player added to simulation, waiting for initial parameters");
            UpdateGameState(GameState.LOADING_DATA);
        }
    }


    // EN: Virtual hook for subclass-specific per-frame logic (called at end of Update).
    // VI: Hook ảo cho logic theo frame riêng của lớp con (gọi cuối Update).
    protected virtual void OtherUpdate()
    {

    }

    // EN: Virtual hook for main controller button press.
    // VI: Hook ảo khi nhấn nút chính trên controller.
    protected virtual void TriggerMainButton()
    {

    }

    // EN: Virtual hook for XR hover-enter on interactable objects.
    // VI: Hook ảo khi tia XR đi vào đối tượng tương tác.
    protected virtual void HoverEnterInteraction(HoverEnterEventArgs ev)
    {
    }

    // EN: Virtual hook for XR hover-exit on interactable objects.
    // VI: Hook ảo khi tia XR rời khỏi đối tượng tương tác.
    protected virtual void HoverExitInteraction(HoverExitEventArgs ev)
    {

    }

    // EN: Virtual hook for XR select (grab/click) on interactable objects.
    // VI: Hook ảo khi chọn (grab/click) trên đối tượng tương tác XR.
    protected virtual void SelectInteraction(SelectEnterEventArgs ev)
    {

    }

    // EN: Utility: change all renderer materials on a GameObject to the given color.
    // VI: Tiện ích: đổi tất cả material renderer trên GameObject sang màu cho trước.
    static public void ChangeColor(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.gameObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = color;
        }
    }
    // EN: Virtual hook called after all GAMA geometries have been loaded.
    // VI: Hook ảo được gọi sau khi tất cả geometry GAMA đã được tải.
    protected virtual void AdditionalInitAfterGeomLoading()
    {

    }
    // EN: Virtual hook for handling unknown/custom GAMA message keys (the "default" case).
    // VI: Hook ảo xử lý các khóa message GAMA không xác định/tùy chỉnh (case "default").
    protected virtual void ManageOtherMessages(string content)
    {

    }

    private void HandleServerMessageReceived(String firstKey, String content)
    {

        // EN: Main inbound message router by payload key.
        // VI: Bộ định tuyến message vào theo khóa dữ liệu.

        if (content == null || content.Equals("{}")) return;
        switch (firstKey)
        {
            case "subsidences":
                subsidenceInfo = SubsidenceInfo.CreateFromJSON(content);
                break;

            case "pumpers":
                infoPump = FreshWaterSpawn.CreateFromJSON(content);
                break;

            case "enemyspawners":
                infoEnemySp = EnemySpawnerInfo.CreateFromJSON(content);
                break;

            // handle general informations about the simulation
            case "precision":

                parameters = ConnectionParameter.CreateFromJSON(content);
                converter = new CoordinateConverter(parameters.precision, GamaCRSCoefX, GamaCRSCoefY, GamaCRSCoefY, GamaCRSOffsetX, GamaCRSOffsetY, GamaCRSOffsetZ);
                TimeSendPosition = (0.0f + parameters.minPlayerUpdateDuration) / (parameters.precision + 0.0f);
                // Init ground and player
                // await Task.Run(() => InitGroundParameters());
                // await Task.Run(() => InitPlayerParameters()); 
                // handlePlayerParametersRequested = true;   
                handleGroundParametersRequested = true;
                handleGeometriesRequested = true;


                break;

            case "properties":
                propertiesGAMA = AllProperties.CreateFromJSON(content);
                propertyMap = new Dictionary<string, PropertiesGAMA>();
                foreach (PropertiesGAMA p in propertiesGAMA.properties)
                {
                    propertyMap.Add(p.id, p);
                }
                break;

            // handle agents while simulation is running
            case "pointsLoc":
                if (infoWorld == null)
                {
                    infoWorld = WorldJSONInfo.CreateFromJSON(content);
                }
                break;
            case "endOfGame":
                EndOfGameInfo infoEoG = EndOfGameInfo.CreateFromJSON(content);
                StaticInformation.endOfGame = infoEoG.endOfGame;
                SceneManager.LoadScene("End of Game Menu");
                break;
            case "rows":
                data = DEMData.CreateFromJSON(content);
                break;
            case "wallId":
                dataWall = WallInfo.CreateFromJSON(content);
                break;
            case "teleportId":
                dataTeleport = TeleoportAreaInfo.CreateFromJSON(content);
                break;
            case "indexX":
                dataLoc = DEMDataLoc.CreateFromJSON(content);
                break;
            case "enableMove":
                enableMove = EnableMoveInfo.CreateFromJSON(content);
                break;
            case "triggers":
                infoAnimation = AnimationInfo.CreateFromJSON(content);
                break;
            case "readyToStart":
                sendReady = false;
                break;
            case "startGame":
                startGameParameters = StartGameParameters.CreateFromJSON(content);
                break;
            default:
                ManageOtherMessages(content);
                break;
        }

    }

    // EN: Handle connection success/failure. On success from MENU state, transition to WAITING.
    // VI: Xử lý kết nối thành công/thất bại. Nếu thành công từ MENU, chuyển sang WAITING.
    private void HandleConnectionAttempted(bool success)
    {
        Debug.Log("SimulationManager: Connection attempt " + (success ? "successful" : "failed"));
        if (success)
        {
            if (IsGameState(GameState.MENU))
            {
                Debug.Log("SimulationManager: Successfully connected to middleware");
                UpdateGameState(GameState.WAITING);
            }
        }
        else
        {
            // stay in MENU state
            Debug.Log("Unable to connect to middleware");
        }
    }

    private void TryReconnect()
    {
        // EN: Send ping_GAMA and wait for timeout; reconnect if no response.
        // VI: Gửi ping_GAMA và chờ timeout; tự reconnect nếu không có phản hồi.
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"id",ConnectionManager.Instance.getUseMiddleware() ? ConnectionManager.Instance.GetConnectionId()  : ("\"" + ConnectionManager.Instance.GetConnectionId() +  "\"") }};

        ConnectionManager.Instance.SendExecutableAsk("ping_GAMA", args);

        currentTimePing = maxTimePing;
        Debug.Log("Sent Ping test");

    }

    // ############################################# UTILITY FUNCTIONS ########################################


    // EN: Send a GAMA restart command to reinitialize the simulation.
    // VI: Gửi lệnh restart GAMA để khởi tạo lại mô phỏng.
    public void RestartGame()
    {
        Debug.Log("RESTART GAMA SIM ");
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"id", ConnectionManager.Instance.GetConnectionId()   }};

        ConnectionManager.Instance.SendExecutableAsk("restart", args);
    }

    // EN: Check if the current state matches the given state.
    // VI: Kiểm tra trạng thái hiện tại có khớp với trạng thái cho trước không.
    public bool IsGameState(GameState state)
    {
        return currentState == state;
    }

    // EN: Get the current game state.
    // VI: Lấy trạng thái game hiện tại.
    public GameState GetCurrentState()
    {
        return currentState;
    }


}


// ############################################################
// EN: Game lifecycle states for the SimulationManager state machine.
// VI: Các trạng thái vòng đời game cho máy trạng thái SimulationManager.
public enum GameState
{
    // EN: Not connected to middleware/GAMA. Initial state.
    // VI: Chưa kết nối middleware/GAMA. Trạng thái ban đầu.
    MENU,
    // EN: Connected to middleware, waiting for GAMA to authenticate this player.
    // VI: Đã kết nối middleware, đang chờ GAMA xác thực người chơi này.
    WAITING,
    // EN: Authenticated; requesting and receiving initial data (properties, world, DEM).
    // VI: Đã xác thực; đang yêu cầu và nhận dữ liệu ban đầu (properties, world, DEM).
    LOADING_DATA,
    // EN: Simulation running — periodic sync active.
    // VI: Mô phỏng đang chạy — đồng bộ định kỳ đang hoạt động.
    GAME,
    // EN: Game ended normally.
    // VI: Game kết thúc bình thường.
    END,
    // EN: Unrecoverable error / connection lost.
    // VI: Lỗi không khôi phục được / mất kết nối.
    CRASH
}



// EN: Extension method providing a non-boxing TryGetComponent fallback
//     for older Unity versions that don't have it built-in.
// VI: Extension method cung cấp TryGetComponent không boxing dự phòng
//     cho các phiên bản Unity cũ chưa có sẵn.
public static class Extensions
{
    public static bool TryGetComponent<T>(this GameObject obj, T result) where T : Component
    {
        return (result = obj.GetComponent<T>()) != null;
    }
}