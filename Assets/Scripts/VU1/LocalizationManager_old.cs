using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class LocalizationManager_old : MonoBehaviour
{
    public static LocalizationManager_old Instance;

    private Dictionary<string, string> localizedText;
    private string currentLanguage = "en";

    private Dictionary<string, Dictionary<string, string>> allLanguages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalizedText();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadLocalizedText()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "languages.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            allLanguages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
            SetLanguage(currentLanguage);
        }
        else
        {
            Debug.LogError("Không tìm thấy file languages.json tại: " + filePath);
        }
    }

    public void SetLanguage(string langCode)
    {
        currentLanguage = langCode;
        if (allLanguages.ContainsKey(langCode))
            localizedText = allLanguages[langCode];
        else
            Debug.LogWarning("Ngôn ngữ không tồn tại: " + langCode);
    }

    public string GetText(string key)
    {
        if (localizedText != null && localizedText.ContainsKey(key))
            return localizedText[key];
        return $"#{key}";
    }

    public string CurrentLanguage => currentLanguage;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
