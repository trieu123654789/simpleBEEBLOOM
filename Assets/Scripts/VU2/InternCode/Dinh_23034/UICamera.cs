using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class UICamera : MonoBehaviour
{
    [Header("UI điều khiển bằng tay phải (Secondary button)")]
    [SerializeField] private GameObject UISetting;

    [Header("UI điều khiển bằng tay trái (Primary button = X)")]
    [SerializeField] private GameObject startGameMenuUI;

    private bool isUISettingOpen = false;
    private bool isStartGameMenuOpen = false;

    private bool rightSecondaryPrev = false;
    private bool leftPrimaryPrev = false;

    // Cache NonBlockingCanvas trên Canvas cha (nếu có)
    private NonBlockingCanvas _nonBlockingCanvas;

    private void Start()
    {
        _nonBlockingCanvas = GetComponentInParent<NonBlockingCanvas>();
        if (_nonBlockingCanvas == null)
            _nonBlockingCanvas = GetComponentInChildren<NonBlockingCanvas>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleUISetting();
        if (Input.GetKeyDown(KeyCode.O))
            ToggleStartGameMenu();

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid &&
            rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightPressed))
        {
            if (rightPressed && !rightSecondaryPrev)
                ToggleUISetting();
            rightSecondaryPrev = rightPressed;
        }

        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid &&
            leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPressed))
        {
            if (leftPressed && !leftPrimaryPrev)
                ToggleStartGameMenu();
            leftPrimaryPrev = leftPressed;
        }
    }

    public void ToggleUISetting()
    {
        if (!UISetting) return;

        isUISettingOpen = !isUISettingOpen;
        UISetting.SetActive(isUISettingOpen);
        UpdateRaycasterState();
        Debug.Log((isUISettingOpen ? "Show" : "Hide") + " UISetting");
    }

    public void ToggleStartGameMenu()
    {
        if (!startGameMenuUI) return;

        isStartGameMenuOpen = !isStartGameMenuOpen;
        startGameMenuUI.SetActive(isStartGameMenuOpen);
        UpdateRaycasterState();
        Debug.Log((isStartGameMenuOpen ? "Show" : "Hide") + " StartGameMenuUI");
    }

    /// <summary>
    /// Bật GraphicRaycaster khi có panel tương tác đang mở, tắt khi tất cả panel đóng.
    /// Enable GraphicRaycaster when any interactive panel is open, disable when all closed.
    /// </summary>
    private void UpdateRaycasterState()
    {
        bool anyOpen = isUISettingOpen || isStartGameMenuOpen;
        if (_nonBlockingCanvas != null)
        {
            if (anyOpen)
                _nonBlockingCanvas.EnableRaycasters();
            else
                _nonBlockingCanvas.DisableRaycasters();
        }
    }
}