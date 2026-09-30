
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// EN: Startup menu screen controller. Displays the player ID (from StaticInformation),
//     lets the user toggle middleware mode, configure IP, and launch the main scene.
//     Saves preferences (IP, PORT, MIDDLEWARE flag) to PlayerPrefs.
// VI: Controller màn hình menu khởi động. Hiển thị player ID (từ StaticInformation),
//     cho phép người dùng chuyển đổi chế độ middleware, cấu hình IP và khởi chạy scene chính.
//     Lưu cài đặt (IP, PORT, cờ MIDDLEWARE) vào PlayerPrefs.
public class MenuController : MonoBehaviour
{
    // EN: Default server IP.
    // VI: IP server mặc định.
    [SerializeField] private string host = "127.0.0.1";
    // EN: Port when using middleware proxy.
    // VI: Port khi dùng middleware proxy.
    [SerializeField] private string portWithMiddleware = "8080";
    // EN: Port for direct GAMA connection (no middleware).
    // VI: Port cho kết nối GAMA trực tiếp (không middleware).
    [SerializeField] private string portWithoutMiddleware = "1000";
    // EN: Whether middleware routing is enabled.
    // VI: Có bật định tuyến qua middleware không.
    private bool useMiddleWare;
    // EN: UI element showing current IP and port.
    // VI: Phần tử UI hiển thị IP và port hiện tại.
    TextMeshProUGUI textMP;
    // EN: Toggle for middleware mode.
    // VI: Toggle cho chế độ middleware.
    Toggle m_Toggle;

    public void Start()
    {

        textMP = GameObject.FindGameObjectWithTag("textIP").GetComponent<TextMeshProUGUI>();
        m_Toggle = GameObject.FindGameObjectWithTag("useMiddleWare").GetComponent<Toggle>();
        GameObject ob = GameObject.FindGameObjectWithTag("textPN");
        TextMeshProUGUI textPN = ob.GetComponent<TextMeshProUGUI>();
        textPN.text = "Player id: " + StaticInformation.getId();
       
        if (!PlayerPrefs.HasKey("MIDDLEWARE") || PlayerPrefs.GetString("MIDDLEWARE").Length == 0)
            PlayerPrefs.SetString("MIDDLEWARE", "N");
        useMiddleWare = PlayerPrefs.GetString("MIDDLEWARE").Equals("Y");
        m_Toggle.SetIsOnWithoutNotify(useMiddleWare);

        string port = useMiddleWare ? portWithMiddleware : portWithoutMiddleware;
       
        string ip = PlayerPrefs.GetString("IP");
        if (ip.Length == 0)
        {
            ip = host;
            PlayerPrefs.SetString("IP", ip);
        }
        textMP.text = "Current IP: " + ip + "/" + port;

        

    }

    // EN: Called when the middleware toggle changes. Updates the displayed port.
    // VI: Được gọi khi toggle middleware thay đổi. Cập nhật port hiển thị.
    public void OnValueMiddleWare()
    {
        useMiddleWare = m_Toggle.isOn;
        string port = useMiddleWare ? portWithMiddleware : portWithoutMiddleware;
        textMP.text = "Current IP: " + PlayerPrefs.GetString("IP") + ":" + port;

    }

    // EN: Save preferences and load the main game scene.
    // VI: Lưu cài đặt và tải scene game chính.
    public void StartBtn()
    {
        PlayerPrefs.SetString("MIDDLEWARE", useMiddleWare ? "Y" : "N") ;
        string port = useMiddleWare ? portWithMiddleware : portWithoutMiddleware;
        PlayerPrefs.SetString("PORT", port);
        SceneManager.LoadScene("Main Scene");
    }

    // EN: Navigate to the IP configuration scene.
    // VI: Chuyển đến scene cấu hình IP.
    public void IPBtn()
    {
        SceneManager.LoadScene("IP Menu");
    }
}
