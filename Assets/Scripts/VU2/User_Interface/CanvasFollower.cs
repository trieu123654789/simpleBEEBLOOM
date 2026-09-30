using UnityEngine;
using UnityEngine.UI;

public class CanvasFollower : MonoBehaviour
{
    [SerializeField] Vector3 distanceFromCamera = new Vector3(0, 0, 2);
    [SerializeField] float smoothSpeed = 8.0f;

    [Tooltip("Kéo camera của XR rig vào đây. Nếu để trống sẽ tự tìm trong parent hierarchy.")]
    [SerializeField] Camera targetCamera;

    [Tooltip("Tắt blocksRaycasts trên Canvas chỉ hiển thị (KHÔNG chứa Button).\n"
           + "Canvas có Button sẽ chỉ dọn Graphic trang trí, giữ raycaster.")]
    [SerializeField] bool disableBlocksRaycasts = true;

    Transform cameraTransform;

    void OnEnable()
    {
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
        else
        {
            cameraTransform = FindCameraInParentHierarchy();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null) return;

        if (disableBlocksRaycasts)
            DisableRaycastBlockingOnDisplayCanvases();

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        
        transform.position = targetPosition;

        float cameraYaw = cameraTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera.z)
         + (cameraTransform.up * distanceFromCamera.y) + (cameraTransform.right * distanceFromCamera.x);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        float cameraYaw = cameraTransform.eulerAngles.y;
        float cameraPitch = cameraTransform.eulerAngles.x;
        Quaternion targetRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    private Transform FindCameraInParentHierarchy()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Camera cam = current.GetComponentInChildren<Camera>();
            if (cam != null) return cam.transform;
            current = current.parent;
        }
        return null;
    }

    /// <summary>
    /// Xử lý raycasting thông minh:
    /// - Canvas KHÔNG có Selectable → tắt hoàn toàn raycaster + raycastTarget
    /// - Canvas CÓ Selectable (Button) → giữ raycaster, chỉ tắt Graphic trang trí
    /// </summary>
    private void DisableRaycastBlockingOnDisplayCanvases()
    {
        // Duyệt từng Canvas con
        foreach (var canvas in GetComponentsInChildren<Canvas>(true))
        {
            bool hasSelectable = canvas.GetComponentInChildren<Selectable>(true) != null;

            if (!hasSelectable)
            {
                // Canvas chỉ hiển thị → tắt hoàn toàn
                SetBlocksRaycasts(canvas.gameObject, false);

                foreach (var gr in canvas.GetComponents<GraphicRaycaster>())
                    gr.enabled = false;

                foreach (var g in canvas.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;
            }
            else
            {
                // Canvas tương tác → chỉ tắt Graphic trang trí
                foreach (var g in canvas.GetComponentsInChildren<Graphic>(true))
                {
                    if (IsPartOfSelectable(g)) continue;
                    g.raycastTarget = false;
                }
            }
        }
    }

    private bool IsPartOfSelectable(Graphic g)
    {
        if (g.GetComponent<Selectable>() != null) return true;
        var parent = g.GetComponentInParent<Selectable>();
        return parent != null && g.transform.IsChildOf(parent.transform);
    }

    private void SetBlocksRaycasts(GameObject target, bool value)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = target.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = value;
    }
}