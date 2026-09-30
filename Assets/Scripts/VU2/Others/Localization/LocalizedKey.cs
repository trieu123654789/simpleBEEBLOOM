using UnityEngine;
using TMPro;

public class LocalizedKey : MonoBehaviour
{
    public string localizationKey;
    public TextMeshProUGUI textComponent;

    private void Start()
    {
        if (textComponent == null) GetComponent<TextMeshProUGUI>();

        UpdateText();
        LocalizationManager.OnLanguageChanged += UpdateText;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;

        if (textComponent != null)
        {
            string localizedText = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
            if (!string.IsNullOrEmpty(localizedText))
            {
                textComponent.text = localizedText;
            }
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component not found on " + gameObject.name);
        }
    }
}