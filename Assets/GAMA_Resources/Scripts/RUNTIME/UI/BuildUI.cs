using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class BuildUI : MonoBehaviour
{
    [SerializeField] private BuildSystemManager buildManager;
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI currentBuildInfo;
    [SerializeField] private List<TextMeshProUGUI> currentQuantities;
    [SerializeField] private List<Image> imageCooldownList;

    [SerializeField] private GameObject removeConstructionRay;

    public List<Image> ImageCooldownList
    {
        get { return imageCooldownList; }
    }
    void Start()
    {
        //content.SetActive(false);
    }

    private void Update()
    {
        int count = Mathf.Min(currentQuantities.Count, buildManager.Constructions.Count);
        for(int i = 0; i < count; i++)
        {
            if (currentQuantities[i] == null || imageCooldownList[i] == null) continue;
            currentQuantities[i].text = buildManager.Constructions[i].CurrentQuantity.ToString();
            if(imageCooldownList[i].fillAmount != 0)
            {
                imageCooldownList[i].fillAmount -= 1.0f / buildManager.Constructions[i].cooldownTime * Time.deltaTime;
            }
        }
    }

    public void ChoseConstruction(int constructionIndex)
    {
        buildManager.StartBuilding(constructionIndex);
        if (buildManager.IsBuilding) ToggleMenu();
    }

    public void ToggleRemoveConstruction(bool toggle)
    {
        removeConstructionRay.SetActive(toggle);
    }

    public void ToggleMenu()
    {
        content.SetActive(!content.activeSelf);
    }

    public void OnHover(int constructionIndex)
    {
        currentBuildInfo.text = buildManager.GetConstructionInfo(constructionIndex);
    }

    public void ExitHover()
    {
        currentBuildInfo.text = "";
    }
}
