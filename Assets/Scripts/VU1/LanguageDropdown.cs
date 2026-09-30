using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LanguageDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        dropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    void OnLanguageChanged(int index)
    {
        string langCode = (index == 0) ? "en" : "vi"; // hoặc gắn theo list
        LocalizationManager_old.Instance.SetLanguage(langCode);

        foreach (var t in FindObjectsOfType<LocalizedText_old>())
        {
            t.UpdateText();
        }
    }
}
