using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Tự động tắt XR Device Simulator khi có kính VR thực tế (Quest/Vive/Index...) kết nối,
/// tránh xung đột đè vị trí tay cầm và tia raycast.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class XRDeviceSimulatorAutoToggle : MonoBehaviour
{
    private void Awake()
    {
        CheckAndToggle();
    }

    private void Update()
    {
        // Kiểm tra lại nếu kính VR được bật sau khi Play
        if (Time.frameCount % 60 == 0)
        {
            CheckAndToggle();
        }
    }

    private void CheckAndToggle()
    {
        var hmds = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmds);

        bool hasRealHmd = false;
        foreach (var dev in hmds)
        {
            if (dev.isValid && !dev.name.ToLowerInvariant().Contains("mock") && !dev.name.ToLowerInvariant().Contains("simulat"))
            {
                hasRealHmd = true;
                break;
            }
        }

        if (hasRealHmd || (XRSettings.isDeviceActive && !XRSettings.loadedDeviceName.ToLowerInvariant().Contains("mock")))
        {
            Debug.Log("[XRDeviceSimulatorAutoToggle] 🥽 Đã kết nối kính VR thực tế: Tự động tắt XR Device Simulator để trả quyền điều khiển 100% cho tay cầm thật.");
            gameObject.SetActive(false);
        }
    }
}
