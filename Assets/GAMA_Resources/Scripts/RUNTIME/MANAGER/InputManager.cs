using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Teleportation Input--------------------")]
    public GameObject teleportationRay;
    public InputActionProperty teleportActive;

    [Header("Building Input-------------------------")]
    public BuildSystemManager buildManager;
    public BuildUI buildUI;
    public InputActionProperty buildActive;
    public GameObject buildRay;
    public InputActionProperty buildAction;
    [Tooltip("Cò trái (Left Trigger) — bóp để HOÀN TÁC/HUỶ thao tác xây dựng đang diễn ra (trồng cây, đặt máy bơm, cổng...).")]
    public InputActionProperty cancelBuildAction;

    // Edge-detect cho cancel để 1 lần bóp = 1 lần huỷ (tránh huỷ liên tục khi giữ).
    private bool cancelHeldLastFrame = false;

    void Update()
    {
        // teleportation interaction
        teleportationRay.SetActive(teleportActive.action.ReadValue<Vector2>() != Vector2.zero);

        // build system interaction
        if (buildActive.action.triggered)
        {
            if (buildManager.IsBuilding) buildManager.CancelBuilding();
            else
            {
                buildUI.ToggleMenu();
                buildUI.ToggleRemoveConstruction(false);
            }
            
        }

        // Cò trái (Left Trigger) → huỷ thao tác xây/trồng đang diễn ra.
        // Phát hiện sườn lên (press) để tránh gọi CancelBuilding nhiều lần khi giữ.
        bool cancelHeldNow = cancelBuildAction.action != null
            && cancelBuildAction.action.ReadValue<float>() >= 0.5f;
        if (cancelHeldNow && !cancelHeldLastFrame)
        {
            if (buildManager.IsBuilding)
            {
                buildManager.CancelBuilding();
                Debug.Log("[InputManager] Cancel build by Left Trigger.");
            }
        }
        cancelHeldLastFrame = cancelHeldNow;

        bool shouldShowBuildRay = buildManager.IsBuilding;
        buildRay.SetActive(shouldShowBuildRay);

        if (buildAction.action.ReadValue<float>() >= 0.5f)
        {
            buildManager.Build();
        }
    }

}
