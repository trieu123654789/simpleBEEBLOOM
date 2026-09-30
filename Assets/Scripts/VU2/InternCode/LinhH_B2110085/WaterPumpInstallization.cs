using UnityEngine;
using UnityEngine.UI;

public class WaterPumpInstallization : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] public Transform installedPoint;
    [SerializeField] public GameObject waterPumpButton;
    [SerializeField] public GameObject waterPumpPrefab;

    private bool playerInside = false;

    private void Start()
    {
        if (waterPumpButton != null) { waterPumpButton.SetActive(false); }

        // đăng ký sự kiện cho nút đặt máy bơm
        waterPumpButton.GetComponent<Button>().onClick.AddListener(InstallWaterPump);
    }

    private void OnDisable()
    {
        if (waterPumpButton != null)
        {
            waterPumpButton.GetComponent<Button>().onClick.RemoveAllListeners();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        playerInside = true;

        // nếu trong khu vực đã có máy bơm thì không hiển thị nút đặt máy bơm
        if (transform.childCount != 0) { return; }

        if (other.CompareTag("Player"))
        {
            ToggleWaterPumpButton(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        playerInside = false;

        if (other.CompareTag("Player"))
        {
            ToggleWaterPumpButton(false);
        }
    }

    private void ToggleWaterPumpButton(bool enable)
    {
        waterPumpButton.SetActive(enable);
    }

    private void InstallWaterPump()
    {
        // nếu trong khu vực đã có máy bơm rồi thì không đặt nữa
        if (transform.childCount != 0) { return; }

        // chỉ đặt máy bơm tại khu vực mà người chơi đứng trong đó
        if (!playerInside) { return; }

        var waterPump = Instantiate(waterPumpPrefab, transform);
        waterPump.SetActive(true);

        ToggleWaterPumpButton(false);
    }
}