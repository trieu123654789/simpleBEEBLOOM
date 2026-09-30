using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanguageSwitcher : MonoBehaviour
{
    public void SetLanguage(string languageCode)
    {
        LocalizationManager_old.Instance.SetLanguage(languageCode);

        // Cập nhật lại toàn bộ text
        foreach (var text in FindObjectsOfType<LocalizedText_old>())
        {
            text.UpdateText();
        }

        Debug.Log("Đã đổi ngôn ngữ sang: " + languageCode);
    }


}
