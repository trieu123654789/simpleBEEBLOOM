using UnityEngine;
using TMPro;

// =============================================================================
// HarvestPromptUI - World Space UI for harvest zone prompts.
// HarvestPromptUI - UI World Space cho hiển thị gợi ý thu hoạch.
// =============================================================================
public class HarvestPromptUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text promptText;
    public GameObject iconCoconut;
    public GameObject iconDurian;
    public GameObject iconRice;
    public GameObject iconFish;

    [Header("Settings")]
    public bool lookAtCamera = true;
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private Camera _mainCamera;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (lookAtCamera && _mainCamera != null)
        {
            transform.LookAt(transform.position + _mainCamera.transform.forward);
        }
    }

    public void Show(string text, HarvestZoneType zoneType)
    {
        gameObject.SetActive(true);
        
        if (promptText != null)
        {
            promptText.text = text;
        }
        
        HideAllIcons();
        switch (zoneType)
        {
            case HarvestZoneType.CoconutTree:
                if (iconCoconut != null) iconCoconut.SetActive(true);
                break;
            case HarvestZoneType.DurianTree:
                if (iconDurian != null) iconDurian.SetActive(true);
                break;
            case HarvestZoneType.RiceField:
                if (iconRice != null) iconRice.SetActive(true);
                break;
            case HarvestZoneType.FishPond:
            case HarvestZoneType.ShrimpPond:
                if (iconFish != null) iconFish.SetActive(true);
                break;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void HideAllIcons()
    {
        if (iconCoconut != null) iconCoconut.SetActive(false);
        if (iconDurian != null) iconDurian.SetActive(false);
        if (iconRice != null) iconRice.SetActive(false);
        if (iconFish != null) iconFish.SetActive(false);
    }
}
