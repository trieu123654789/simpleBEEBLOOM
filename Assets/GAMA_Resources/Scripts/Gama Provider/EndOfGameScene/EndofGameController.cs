
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// EN: End-of-game screen controller. Displays the player ID and the
//     game result text stored in StaticInformation.endOfGame (received from GAMA).
//     Provides a reset button to return to the startup menu.
// VI: Controller màn hình kết thúc game. Hiển thị player ID và
//     văn bản kết quả game lưu trong StaticInformation.endOfGame (nhận từ GAMA).
//     Cung cấp nút reset để quay lại menu khởi động.
public class EndofGameController : MonoBehaviour
{
    // EN: UI text element for displaying the game result.
    // VI: Phần tử UI text để hiển thị kết quả game.
    TextMeshProUGUI textMP;

    void Start()
    {
        TextMeshProUGUI textPN = GameObject.FindGameObjectWithTag("textPN").GetComponent<TextMeshProUGUI>();
        textPN.text = "Player id: " + StaticInformation.getId();

        textMP = GameObject.FindGameObjectWithTag("textIP").GetComponent<TextMeshProUGUI>();
        textMP.text = StaticInformation.endOfGame;
       
    }

    // EN: Return to the startup menu scene.
    // VI: Quay lại scene menu khởi động.
    public void ResetBtn()
    {
        SceneManager.LoadScene("Startup Menu");
    }


}
