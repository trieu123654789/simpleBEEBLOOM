using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private PlayerResourcesManager playerResourcesManager;
    [SerializeField] private LevelManager levelManager;

    [SerializeField] private Image timeBar;
    [SerializeField] private TextMeshProUGUI time;
    [SerializeField] private TextMeshProUGUI wave;
    [SerializeField] private TextMeshProUGUI step;
    [SerializeField] private TextMeshProUGUI resoucre;

    [SerializeField] private string waveText;
    [SerializeField] private string[] waveStepTexts;

    
    // Update is called once per frame
    void Update()
    {
        UpdateLevelUI();
        UpdateResourcesUI();
    }

    void UpdateLevelUI()
    {
        if (!levelManager) return;

        if (levelManager.Finished)
        {
            if (time) time.text = "FINISHED";
        }
        else
        {
            if (timeBar)
                timeBar.fillAmount = (levelManager.CurrentWaveStepTime - levelManager.CurrentTime) / levelManager.CurrentWaveStepTime;

            int minutes = Mathf.FloorToInt(levelManager.CurrentTime / 60F);
            int seconds = Mathf.FloorToInt(levelManager.CurrentTime - minutes * 60);
            string niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);
            if (time) time.text = niceTime;
        }
        if (wave) wave.text = waveText + levelManager.CurrentWave + "/" + levelManager.MaxWave;

        string connId = ConnectionManager.Instance != null
            ? ConnectionManager.Instance.GetConnectionId()
            : "";

        if (step)
        {
            switch (levelManager.CurrentWaveStep)
            {
                case WaveStep.Preparation:
                    step.text = connId + " " + waveStepTexts[0];
                    break;
                case WaveStep.Defense:
                    step.text = connId + " " + waveStepTexts[1];
                    break;
            }
        }
    }

    void UpdateResourcesUI()
    {
        if (playerResourcesManager)
            resoucre.text = playerResourcesManager.CurrentAmount.ToString();
    }
}
