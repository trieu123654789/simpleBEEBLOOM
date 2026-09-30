using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText_old : MonoBehaviour
{
    public string key; // Khóa của câu muốn hiển thị
    private TextMeshProUGUI textComponent;

    // Start is called before the first frame update
    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void UpdateText()
    {
        textComponent.text = LocalizationManager_old.Instance.GetText(key);
    }

    void OnEnable()
    {
        if (LocalizationManager_old.Instance != null)
            UpdateText();
    }
}
