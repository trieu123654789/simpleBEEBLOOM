using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Quản lý hệ thống xây dựng dựa trên SurfaceConnector.
/// Flow: Nhấn Button (vd: Button_Pump) → icon "!" hiện trên ray tay phải
///       → Ray chạm SurfaceConnector → ghost hiện tại vị trí đó
///       → Nhấn cò (trigger) → xây dựng construction tại vị trí đã chọn.
/// </summary>
public class BuildSystemManager : MonoBehaviour
{
    [SerializeField] private List<ConstructionSO> constructions;
    [SerializeField] private BuildUI buildIU;

    [Header("Build Ray (XR Ray Interactor)")]
    [SerializeField] private XRRayInteractor buildRayInteractor;

    [Header("Build Mode Indicator")]
    [Tooltip("Icon '!' hiển thị trên ray tay phải khi đang ở chế độ xây dựng")]
    [SerializeField] private GameObject buildModeIndicator;

    [Header("Connector Detection")]
    [Tooltip("LayerMask cho SurfaceConnector (BuildConnector layer)")]
    [SerializeField] private LayerMask connectorLayerMask;
    [Tooltip("Bán kính tìm kiếm Connector xung quanh điểm raycast để dễ thao tác hơn")]
    [SerializeField] private float connectorDetectionRadius = 1.5f;

    [Header("UI Canvases (tự tắt blocksRaycasts khi build)")]
    [Tooltip("Kéo các Canvas UI vào đây để ray không bị UI chặn khi đang build")]
    [SerializeField] private CanvasGroup[] uiCanvasGroups;

    [SerializeField] private SubsidenceManager subsidenceManager;

    [Header("Carry-and-Plant Mode")]
    [Tooltip("Transform được dùng làm điểm gắn vật cầm trên tay (vd: AttachTransform của right controller, hoặc 1 child rỗng dưới Main Camera Offset). Cần thiết khi ConstructionSO có requireCarryToPlant = true.")]
    [SerializeField] private Transform handAttachPoint;
    [Tooltip("Offset cục bộ so với handAttachPoint khi đặt vật cầm.")]
    [SerializeField] private Vector3 carryLocalOffset = new Vector3(0f, 0f, 0.15f);
    [Tooltip("Xoay cục bộ (Euler) khi đặt vật cầm trên tay.")]
    [SerializeField] private Vector3 carryLocalEuler = Vector3.zero;
    [Tooltip("(Tuỳ chọn) Transform vị trí Player dùng để kiểm tra PlantingZone. Nếu để trống sẽ tự dùng Camera.main.")]
    [SerializeField] private Transform playerOriginOverride;
    [Tooltip("Thời gian (giây) sau khi vào carry mode không cho phép trồng — tránh trồng nhầm do trigger XR vẫn đang giữ.")]
    [SerializeField] private float carryGraceTime = 0.4f;
    [Tooltip("Material áp lên ghost khi player đang đứng trong PlantingZone (hợp lệ — có thể trồng). Thường là màu xanh trong suốt.")]
    [SerializeField] private Material carryPreviewValidMaterial;
    [Tooltip("Material áp lên ghost khi player ở NGOÀI PlantingZone (không trồng được). Thường là màu đỏ trong suốt.")]
    [SerializeField] private Material carryPreviewInvalidMaterial;
    [Tooltip("Scale của ghost preview so với prefab gốc (vd: 0.33 = 1/3). Áp dụng cho carry mode (Tree).")]
    [SerializeField] private float carryPreviewScale = 1f / 3f;

    private bool isBuilding = false;
    private int currentBuildingIndex = 0;
    private GameObject ghostConstruction;
    private Connector currentTargetConnector;

    // Carry-mode runtime state
    private bool isCarrying = false;
    private GameObject carriedObject;
    private float carryStartTime;
    private GameObject carryPreviewGhost;
    private PlantingZone carryPreviewZone;

    // Getters
    public bool IsBuilding => isBuilding;
    public bool IsCarrying => isCarrying;
    public List<ConstructionSO> Constructions => constructions;

    public void Start()
    {
        foreach (ConstructionSO c in constructions)
        {
            c.Init();
        }
        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);
    }

    private void Update()
    {
        UpdateCooldowns(Time.deltaTime);
        if (isBuilding)
        {
            if (isCarrying) ProcessCarrying();
            else ProcessBuilding();
        }
    }

    /// <summary>
    /// Bắt đầu chế độ xây dựng cho construction tại index.
    /// Gọi từ BuildUI khi nhấn Button (vd: Button_Pump index=1).
    /// </summary>
    public void StartBuilding(int constructionIndex)
    {
        if (!IsBuildable(constructionIndex))
        {
            Debug.Log($"[BuildSystem] Cannot build index {constructionIndex}: cooldown or no quantity");
            return;
        }

        // Nếu đang build thứ khác, huỷ trước
        if (isBuilding)
            CancelBuilding();

        isBuilding = true;
        currentBuildingIndex = constructionIndex;
        currentTargetConnector = null;

        ConstructionSO so = constructions[constructionIndex];

        // === Carry-and-Plant mode ===
        if (so.requireCarryToPlant)
        {
            BeginCarryMode(so);
            Debug.Log($"[BuildSystem] CARRY mode ON - construction: {so.name}. Đi tới PlantingZone để trồng.");
            return;
        }

        // === Default ray + ghost mode ===
        // Hiện icon "!" trên ray tay phải
        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(true);

        // Tắt UI chặn raycast để ray xuyên qua UI đến SurfaceConnector
        SetUIBlocksRaycasts(false);

        Debug.Log($"[BuildSystem] Build mode ON - construction: {so.name}");
    }

    /// <summary>
    /// Huỷ chế độ xây dựng (nhấn nút cancel hoặc chọn construction khác).
    /// </summary>
    public void CancelBuilding()
    {
        DestroyGhost();
        EndCarryMode();
        isBuilding = false;
        currentTargetConnector = null;

        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);

        SetUIBlocksRaycasts(true);
    }

    public bool IsBuildable(int constructionIndex)
    {
        ConstructionSO construction = constructions[constructionIndex];
        return construction.CurrentTime <= 0 && construction.CurrentQuantity > 0;
    }

    /// <summary>
    /// Xác nhận xây dựng tại vị trí SurfaceConnector đang chọn.
    /// Gọi khi nhấn cò (trigger) từ InputManager.
    /// </summary>
    public void Build()
    {
        // Carry-and-Plant flow: chỉ trồng được khi đang đứng trong PlantingZone.
        if (isBuilding && isCarrying)
        {
            BuildFromCarry();
            return;
        }

        if (!isBuilding || ghostConstruction == null || currentTargetConnector == null) return;

        var ghost = ghostConstruction.GetComponent<GhostConstruction>();
        if (ghost == null || !ghost.IsBuildable) return;

        // Tạo construction thật tại vị trí ghost
        // UseIdentityRotation → dùng rotation gốc của prefab (vd: cây đứng thẳng)
        Quaternion buildRotation = ghost.UseIdentityRotation
            ? constructions[currentBuildingIndex].finalPrefab.transform.rotation
            : ghostConstruction.transform.rotation;
        GameObject construction = Instantiate(
            constructions[currentBuildingIndex].finalPrefab,
            ghostConstruction.transform.position,
            buildRotation
        );

        // SFX: tiếng đặt công trình (3D tại vị trí xây).
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBuilding(ghostConstruction.transform.position);

        var remover = construction.GetComponent<ConstructionRemover>();
        if (remover != null)
            remover.buildSystemManager = this;

        // Cập nhật cooldown và số lượng
        constructions[currentBuildingIndex].ResetCooldown();
        constructions[currentBuildingIndex].DecreaseQuantity();
        if (currentBuildingIndex < buildIU.ImageCooldownList.Count
            && buildIU.ImageCooldownList[currentBuildingIndex] != null)
            buildIU.ImageCooldownList[currentBuildingIndex].fillAmount = 1;

        // Disable connectors đã sử dụng
        Connector[] connectors = ghostConstruction.GetComponentsInChildren<Connector>();
        foreach (Connector connector in connectors)
        {
            connector.UpdateConnector(false);
        }

        // Hiệu ứng game
        subsidenceManager.IncreaseSubsidenceLevel();
        subsidenceManager.DecreaseWaterLevel();

        // Thống kê
        UpdateStatistics(currentBuildingIndex);
        StatisticsManager.Instance.AddActionHistory(
            "Build",
            constructions[currentBuildingIndex].name,
            ghostConstruction.transform.position
        );

        FinishBuilding();
    }

    public string GetConstructionInfo(int constructionIndex)
    {
        return constructions[constructionIndex].description;
    }

    // ========== PRIVATE METHODS ==========

    /// <summary>
    /// Mỗi frame khi đang ở chế độ build:
    /// 1. Raycast từ tay phải
    /// 2. Nếu chạm SurfaceConnector (trực tiếp hoặc gần) → hiện ghost tại đó
    /// 3. Nếu không chạm → ẩn ghost
    /// </summary>
    private void ProcessBuilding()
    {
        if (buildRayInteractor == null) return;

        if (buildRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // 1. Kiểm tra hit trực tiếp lên Connector
            Connector hitConnector = hit.collider.GetComponent<Connector>();
            if (hitConnector != null && hitConnector.canConnectTo)
            {
                SetTargetConnector(hitConnector);
                return;
            }

            // 2. Tìm Connector gần điểm raycast (OverlapSphere) → dễ thao tác hơn
            Collider[] nearby = Physics.OverlapSphere(hit.point, connectorDetectionRadius, connectorLayerMask);
            Connector bestConnector = FindBestConnector(nearby);
            if (bestConnector != null)
            {
                SetTargetConnector(bestConnector);
                return;
            }
        }

        // Không tìm thấy Connector hợp lệ → ẩn ghost
        ClearTargetConnector();
    }

    private Connector FindBestConnector(Collider[] colliders)
    {
        foreach (Collider col in colliders)
        {
            Connector connector = col.GetComponent<Connector>();
            if (connector != null && connector.canConnectTo)
                return connector;
        }
        return null;
    }

    /// <summary>
    /// Khi tìm thấy Connector hợp lệ → tạo/hiện ghost tại vị trí Connector.
    /// Chỉ reposition ghost khi target Connector thay đổi (tránh xung đột với GhostConstruction snap).
    /// </summary>
    private void SetTargetConnector(Connector connector)
    {
        bool connectorChanged = (connector != currentTargetConnector);
        currentTargetConnector = connector;

        if (ghostConstruction == null)
        {
            ghostConstruction = Instantiate(constructions[currentBuildingIndex].modelBuildPrefab);
            ghostConstruction.transform.position = connector.transform.position;
            var ghost = ghostConstruction.GetComponent<GhostConstruction>();
            if (ghost == null || !ghost.UseIdentityRotation)
                ghostConstruction.transform.rotation = connector.transform.rotation;
        }
        else if (connectorChanged)
        {
            // Di chuyển ghost đến connector mới, GhostConstruction sẽ snap chính xác
            ghostConstruction.transform.position = connector.transform.position;
            var ghost = ghostConstruction.GetComponent<GhostConstruction>();
            if (ghost == null || !ghost.UseIdentityRotation)
                ghostConstruction.transform.rotation = connector.transform.rotation;
        }

        if (!ghostConstruction.activeSelf)
            ghostConstruction.SetActive(true);
    }

    private void ClearTargetConnector()
    {
        currentTargetConnector = null;
        if (ghostConstruction != null && ghostConstruction.activeSelf)
            ghostConstruction.SetActive(false);
    }

    private void FinishBuilding()
    {
        DestroyGhost();
        isBuilding = false;
        currentTargetConnector = null;

        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);

        // Bật lại UI raycast rồi mở lại Build UI
        SetUIBlocksRaycasts(true);
        if (buildIU != null)
            buildIU.ToggleMenu();
    }

    private void DestroyGhost()
    {
        if (ghostConstruction != null)
        {
            Destroy(ghostConstruction);
            ghostConstruction = null;
        }
    }

    private void UpdateStatistics(int index)
    {
        switch (index)
        {
            case 0: StatisticsManager.Instance.IncreateSluiceGateCount(); break;
            case 1: StatisticsManager.Instance.IncreateWaterPumpCount(); break;
            case 2: StatisticsManager.Instance.IncreateLakeCount(); break;
            case 3: StatisticsManager.Instance.IncreateTreeBarrierCount(); break;
        }
    }

    private void UpdateCooldowns(float deltaTime)
    {
        foreach (var construction in constructions)
        {
            construction.DecreaseCooldown(deltaTime);
        }
    }

    /// <summary>
    /// Bật/tắt blocksRaycasts trên tất cả UI CanvasGroup.
    /// Khi build mode ON → tắt để XR ray xuyên qua UI đến SurfaceConnector.
    /// Khi build mode OFF → bật lại để UI hoạt động bình thường.
    /// </summary>
    private void SetUIBlocksRaycasts(bool value)
    {
        if (uiCanvasGroups == null) return;
        foreach (var cg in uiCanvasGroups)
        {
            if (cg != null)
                cg.blocksRaycasts = value;
        }
    }

    // =========================================================================
    // CARRY-AND-PLANT MODE
    // =========================================================================

    /// <summary>
    /// Bắt đầu chế độ "cầm cây trên tay": instantiate prefab cầm tay làm con của handAttachPoint,
    /// và bật highlight cho mọi PlantingZone đang có người chơi đứng trong.
    /// </summary>
    private void BeginCarryMode(ConstructionSO so)
    {
        isCarrying = true;
        carryStartTime = Time.time;

        Transform attach = ResolveHandAttachPoint();
        if (attach == null)
        {
            Debug.LogWarning("[BuildSystem] Không tìm được điểm gắn vật cầm tay. " +
                             "Hãy gán 'Hand Attach Point', hoặc đảm bảo 'Build Ray Interactor' đã được gán.");
        }
        else
        {
            GameObject prefab = so.carryPrefab != null ? so.carryPrefab : so.modelBuildPrefab;
            if (prefab != null)
            {
                carriedObject = Instantiate(prefab, attach);
                carriedObject.transform.localPosition = carryLocalOffset;
                carriedObject.transform.localRotation = Quaternion.Euler(carryLocalEuler);
                carriedObject.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, so.carryScale);

                // Tắt physics / collider để không cản chuyển động của controller
                foreach (var rb in carriedObject.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                foreach (var col in carriedObject.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
                // Tắt các script gameplay không liên quan trên ghost/sapling (vd: GhostConstruction, TreeBarrier...)
                foreach (var mb in carriedObject.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (mb is GhostConstruction) mb.enabled = false;
                }
            }
        }

        SetUIBlocksRaycasts(true); // không cần ray, để UI hoạt động bình thường
        if (buildModeIndicator != null) buildModeIndicator.SetActive(false);

        // Highlight mọi PlantingZone trong scene để player biết đi đâu
        Vector3 pPos = GetPlayerPosition();
        PlantingZone.UpdateHighlights(pPos, true);
        // Hiện ghost preview ngay từ đầu (zone hiện tại nếu có, không thì zone gần nhất).
        PlantingZone insideZone = PlantingZone.FindZoneContaining(pPos);
        PlantingZone targetZone = insideZone != null ? insideZone : PlantingZone.FindNearestZone(pPos);
        UpdateCarryPreviewGhost(targetZone, insideZone != null);
    }

    /// <summary>Lấy vị trí để đối chiếu với PlantingZone (HMD camera).</summary>
    private Vector3 GetPlayerPosition()
    {
        if (playerOriginOverride != null) return playerOriginOverride.position;
        if (Camera.main != null) return Camera.main.transform.position;
        if (buildRayInteractor != null) return buildRayInteractor.transform.position;
        return transform.position;
    }

    /// <summary>
    /// Tự động tìm điểm gắn vật cầm tay theo thứ tự ưu tiên:
    /// 1. handAttachPoint do user gán
    /// 2. Build Ray Interactor (đã có sẵn = tay phải)
    /// 3. Main Camera (fallback cuối cùng — vật sẽ "dán" trước mặt)
    /// </summary>
    private Transform ResolveHandAttachPoint()
    {
        if (handAttachPoint != null) return handAttachPoint;
        if (buildRayInteractor != null) return buildRayInteractor.transform;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    /// <summary>Mỗi frame trong chế độ carry: cập nhật highlight zone theo vị trí player + ghost preview tại vị trí trồng.</summary>
    private void ProcessCarrying()
    {
        Vector3 playerPos = GetPlayerPosition();
        PlantingZone.UpdateHighlights(playerPos, true);

        // Ghost LUÔN hiện: nếu player đang trong zone → xanh (hợp lệ); nếu không → hiện ở zone gần nhất với màu đỏ.
        PlantingZone insideZone = PlantingZone.FindZoneContaining(playerPos);
        PlantingZone targetZone = insideZone != null ? insideZone : PlantingZone.FindNearestZone(playerPos);
        UpdateCarryPreviewGhost(targetZone, insideZone != null);
    }

    /// <summary>
    /// Hiện ghost preview của cây tại vị trí PlantingZone (luôn hiện khi có ít nhất 1 zone).
    /// <paramref name="isValid"/> = true → áp validMaterial (xanh); false → invalidMaterial (đỏ).
    /// </summary>
    private void UpdateCarryPreviewGhost(PlantingZone zone, bool isValid)
    {
        if (zone == null)
        {
            if (carryPreviewGhost != null && carryPreviewGhost.activeSelf)
                carryPreviewGhost.SetActive(false);
            carryPreviewZone = null;
            return;
        }

        ConstructionSO so = constructions[currentBuildingIndex];
        GameObject sourcePrefab = so.modelBuildPrefab != null ? so.modelBuildPrefab : so.finalPrefab;
        if (sourcePrefab == null) return;

        if (carryPreviewGhost == null)
        {
            carryPreviewGhost = Instantiate(sourcePrefab);
            // Tắt mọi script gameplay để ghost chỉ là visual (không tự đổi material, không bắt enemy, ...)
            foreach (var mb in carryPreviewGhost.GetComponentsInChildren<MonoBehaviour>())
            {
                mb.enabled = false;
            }
            // Tắt vật lý / collider để không chặn player.
            foreach (var rb in carryPreviewGhost.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var col in carryPreviewGhost.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            // Scale ghost xuống (vd: 1/3) cho khỏi quá to so với player.
            float s = Mathf.Max(0.01f, carryPreviewScale);
            carryPreviewGhost.transform.localScale = sourcePrefab.transform.localScale * s;
        }

        if (zone != carryPreviewZone)
        {
            carryPreviewZone = zone;
            // Dùng rotation gốc của finalPrefab để cây đứng thẳng đúng với khi trồng thật.
            Quaternion rot = so.finalPrefab != null
                ? so.finalPrefab.transform.rotation
                : sourcePrefab.transform.rotation;
            carryPreviewGhost.transform.SetPositionAndRotation(zone.PlantPosition, rot);
        }

        // Áp material valid/invalid theo trạng thái.
        Material mat = isValid ? carryPreviewValidMaterial : carryPreviewInvalidMaterial;
        if (mat != null)
        {
            ApplyGhostMaterial(carryPreviewGhost, mat);
        }

        if (!carryPreviewGhost.activeSelf)
            carryPreviewGhost.SetActive(true);
    }

    private static void ApplyGhostMaterial(GameObject ghost, Material mat)
    {
        foreach (var mr in ghost.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            mr.materials = mats;
        }
        foreach (var smr in ghost.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mats = new Material[smr.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            smr.materials = mats;
        }
    }

    /// <summary>
    /// Xác nhận trồng cây: chỉ thành công khi đang đứng trong 1 PlantingZone hợp lệ.
    /// </summary>
    private void BuildFromCarry()
    {
        // Tránh trồng nhầm frame đầu (XR trigger vẫn đang giữ sau khi click Button_Tree).
        if (Time.time - carryStartTime < carryGraceTime) return;

        Vector3 playerPos = GetPlayerPosition();
        PlantingZone zone = PlantingZone.FindZoneContaining(playerPos);
        if (zone == null)
        {
            Debug.Log($"[BuildSystem] Chưa ở trong vùng được phép trồng (player @ {playerPos}). Hãy đi đến PlantingZone (vùng được highlight).");
            return;
        }

        ConstructionSO so = constructions[currentBuildingIndex];
        if (so.finalPrefab == null) return;

        Vector3 pos = zone.PlantPosition;
        Quaternion rot = so.finalPrefab.transform.rotation; // dùng rotation gốc của prefab (cây đứng thẳng)

        GameObject planted = Instantiate(so.finalPrefab, pos, rot);

        // SFX: tiếng đặt cây / trồng thành công (3D tại vị trí trồng).
        if (AudioManager.instance != null) AudioManager.instance.PlayBuilding(pos);

        var remover = planted.GetComponent<ConstructionRemover>();
        if (remover != null) remover.buildSystemManager = this;

        so.ResetCooldown();
        so.DecreaseQuantity();
        if (currentBuildingIndex < buildIU.ImageCooldownList.Count
            && buildIU.ImageCooldownList[currentBuildingIndex] != null)
            buildIU.ImageCooldownList[currentBuildingIndex].fillAmount = 1;

        if (subsidenceManager != null)
        {
            subsidenceManager.IncreaseSubsidenceLevel();
            subsidenceManager.DecreaseWaterLevel();
        }

        UpdateStatistics(currentBuildingIndex);
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.AddActionHistory("Build", so.name, pos);
        }

        zone.NotifyPlanted();

        EndCarryMode();
        FinishBuilding();
    }

    /// <summary>Dọn dẹp visual cây trên tay và tắt highlight zone.</summary>
    private void EndCarryMode()
    {
        if (carriedObject != null)
        {
            Destroy(carriedObject);
            carriedObject = null;
        }
        if (carryPreviewGhost != null)
        {
            Destroy(carryPreviewGhost);
            carryPreviewGhost = null;
        }
        carryPreviewZone = null;
        if (isCarrying)
        {
            PlantingZone.ClearAllHighlights();
            isCarrying = false;
        }
    }
}
