using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class changeScene : MonoBehaviour
{
    // public Button StartGame_1;
    // public Button StartGame_2;
    // Start is called before the first frame update
    void Start()
    {
        // if (StartGame_1 != null)
        // {
        //     StartGame_1.onClick.AddListener(OnStartGame1Clicked);
        // }
        // if (StartGame_2 != null)
        // {
        //     StartGame_2.onClick.AddListener(OnStartGame2Clicked);
        // }
    }

    public void OnStartGame1Clicked()
    {
        SceneManager.LoadScene("SCN_VU2_Level1_New"); // Thay "YourSceneName" bằng tên scene bạn muốn chuyển đến
    }

    public void OnStartGame2Clicked()
    {
        SceneManager.LoadScene("SCN_VU2_Level2_New"); // Thay "YourSceneName" bằng tên scene bạn muốn chuyển đến
    }

    public void OnStartGameDemoClicked()
    {
        SceneManager.LoadScene("DEMO"); // Thay "YourSceneName" bằng tên scene bạn muốn chuyển đến
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
