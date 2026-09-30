// using UnityEngine;
// using UnityEngine.UI;
//
// public class Thuan_23127_WaterPumpInstallization : MonoBehaviour
// {
//     [Header("Setup")]
//     [SerializeField] public Transform installedPoint;
//     [SerializeField] public GameObject waterPumpButton;
//     [SerializeField] public GameObject waterPumpPrefab;
//     
//     // THÊM: Vị trí mà nước sẽ bắn tới (Ví dụ: kéo 1 cái cây hoặc 1 empty object vào đây)
//     [SerializeField] public Transform waterDestinationPoint; 
//
//     private bool playerInside = false;
//
//     private void Start()
//     {
//         if (waterPumpButton != null) { waterPumpButton.SetActive(false); }
//         waterPumpButton.GetComponent<Button>().onClick.AddListener(InstallWaterPump);
//     }
//
//     private void OnDisable()
//     {
//         if (waterPumpButton != null)
//         {
//             waterPumpButton.GetComponent<Button>().onClick.RemoveAllListeners();
//         }
//     }
//
//     void OnTriggerEnter(Collider other)
//     {
//         playerInside = true;
//         if (transform.childCount != 0) { return; }
//         if (other.CompareTag("Player")) { ToggleWaterPumpButton(true); }
//     }
//
//     void OnTriggerExit(Collider other)
//     {
//         playerInside = false;
//         if (other.CompareTag("Player")) { ToggleWaterPumpButton(false); }
//     }
//
//     private void ToggleWaterPumpButton(bool enable)
//     {
//         waterPumpButton.SetActive(enable);
//     }
//
//     private void InstallWaterPump()
//     {
//         if (transform.childCount != 0) { return; }
//         if (!playerInside) { return; }
//
//         var waterPump = Instantiate(waterPumpPrefab, transform);
//         waterPump.SetActive(true);
//
//         // Lấy script Controller từ máy bơm vừa tạo ra
//         var pumpController = waterPump.GetComponent<Thuan_23127_WaterPumpController>();
//         if (pumpController != null && waterDestinationPoint != null)
//         {
//             // Truyền vị trí đích vào để máy bơm bắt đầu hoạt động
//             pumpController.InitializePump(waterDestinationPoint.position);
//         }
//         else
//         {
//             Debug.LogWarning("Chưa gắn Script Controller vào Prefab hoặc chưa gán Destination Point!");
//         }
//         // ------------------------------
//
//         ToggleWaterPumpButton(false);
//     }
// }
//
//
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem; 

public class Thuan_23127_WaterPumpInstallization : MonoBehaviour
{
    [Header("Setup VR")]
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private Thuan_23127_GhostConstruction ghostConstructionScript;
    
    [Header("Input Settings (2 Hands)")]
    [SerializeField] private InputActionProperty leftHandAction;  // Nút bấm tay trái
    [SerializeField] private InputActionProperty rightHandAction; // Nút bấm tay phải

    [Header("Real Pump Setup")]
    [SerializeField] public GameObject waterPumpPrefab;
    [SerializeField] public Transform waterDestinationPoint;

    private bool isPlaced = false;
    private bool isHovering = false; 

    private void OnEnable()
    {
        if (interactable == null) interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
        
        if (ghostConstructionScript != null) ghostConstructionScript.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    private void Update()
    {
        if (isPlaced || !isHovering) return;

        // - Check cả 2 tay ---
        bool pressedLeft = leftHandAction.action != null && leftHandAction.action.WasPressedThisFrame();
        bool pressedRight = rightHandAction.action != null && rightHandAction.action.WasPressedThisFrame();
        bool pressedMouse = Input.GetMouseButtonDown(0); // Test trên máy tính

        // Chỉ cần 1 trong các nút được bấm là Đặt
        if (pressedLeft || pressedRight || pressedMouse)
        {
            TryToInstall();
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (isPlaced) return;
        isHovering = true; 
        if (ghostConstructionScript != null) ghostConstructionScript.gameObject.SetActive(true);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        isHovering = false; 
        if (isPlaced) return;
        if (ghostConstructionScript != null) ghostConstructionScript.gameObject.SetActive(false);
    }

    private void TryToInstall()
    {
        if (ghostConstructionScript == null)
        {
            Debug.Log("Lỗi: Chưa gắn Ghost Script!");
            return;
        }

        if (ghostConstructionScript.IsBuildable == false)
        {
            Debug.Log("Ghost đang ĐỎ (Vướng vật cản) -> Không đặt được.");
            return; 
        }

        InstallWaterPump();
    }

    public void InstallWaterPump()
    {
        if (waterPumpPrefab == null) return;

        if (ghostConstructionScript != null) ghostConstructionScript.gameObject.SetActive(false);

        var waterPump = Instantiate(waterPumpPrefab, transform.position, transform.rotation);
        waterPump.SetActive(true);

        var pumpController = waterPump.GetComponent<Thuan_23127_WaterPumpController>();
        if (pumpController != null && waterDestinationPoint != null)
        {
            pumpController.InitializePump(waterDestinationPoint.position);
        }

        isPlaced = true;
        if (interactable != null) interactable.enabled = false;
        
        Debug.Log("THÀNH CÔNG: Đã đặt máy bơm!");
    }
}