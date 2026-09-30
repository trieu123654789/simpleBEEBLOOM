using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

public enum EntityType
{
    Auto = 0,     // Tự dò cả 3
    Plant = 1,    // Cây trồng
    Livestock = 2,// Vật nuôi
    Fish = 3      // Thủy sản
}

public class Thuan_23127_PlantHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Config")]
    public int id;                       // ID dùng chung cho mọi loại
    public EntityType type = EntityType.Auto; // 0=Auto, 1=Plant, 2=Livestock, 3=Fish

    [Header("UI")]
    public GameObject scrollInfoPanel;   // Panel hiển thị
    public Text headText;                // Tiêu đề
    public Text infoText;                // Nội dung

    private Thuan_23127_JsonReader jsonReader;  // Reader lấy dữ liệu từ JSON
    private CanvasGroup _panelCg;

    private void Start()
    {
        // Đảm bảo tìm lại jsonReader nếu bị null
        if (jsonReader == null)
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        
        if (scrollInfoPanel)
        {
            _panelCg = scrollInfoPanel.GetComponent<CanvasGroup>();
            if (!_panelCg) _panelCg = scrollInfoPanel.AddComponent<CanvasGroup>();
            _panelCg.interactable = false;
            _panelCg.blocksRaycasts = false;

            //tắt Raycast Target cho toàn bộ đồ hoạ con để chắc chắn không nuốt input
            foreach (var g in scrollInfoPanel.GetComponentsInChildren<Graphic>(true))
            {
                g.raycastTarget = false;
            }

            scrollInfoPanel.SetActive(false);
        }
        else
        {
            scrollInfoPanel?.SetActive(false);
        }
    }
    
    private void OnDisable()
    {
        // Hide tooltip when this object is disabled
        ForceHideTooltip();
    }

    public void ForceHideTooltip()
    {
        if (scrollInfoPanel != null)
        {
            scrollInfoPanel.SetActive(false);
        }
    }

    private string LocalizeGroupLabelHardcoded(string langCode, EntityType t)
    {
        bool vi = (langCode == "vi");
        switch (t)
        {
            case EntityType.Plant:     return vi ? "Cây"       : "Plant";
            case EntityType.Livestock: return vi ? "Vật nuôi"  : "Livestock";
            case EntityType.Fish:      return vi ? "Thủy sản"  : "Fish";
            default:                   return vi ? "Thông tin" : "Info";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Đảm bảo jsonReader không null
        if (jsonReader == null)
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        
        if (jsonReader == null || scrollInfoPanel == null || infoText == null) return;

        // nếu đang giữ chuột (click/drag) thì KHÔNG mở tooltip (tránh "click treo" gây trồng)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || eventData.eligibleForClick) return;

        var lang = jsonReader.GetCurrentLangData();
        if (lang == null || lang.interpretation == null) return;

        var fields = lang.interpretation.fields;
        var units  = lang.interpretation.units;
        var langCode = jsonReader.GetCurrentLangCode();

        // Gom nhiều kết quả nếu trùng ID ở nhiều type
        var sbBody = new StringBuilder();
        var sbHead = new StringBuilder();

        void AppendBlock(string tag_name, int growth_time, int economic_benefits, string information)
        {
            if (sbHead.Length > 0) sbHead.Append(" | ");
            sbHead.Append($"{tag_name}");

            if (sbBody.Length > 0) sbBody.AppendLine().AppendLine("----------------");

            sbBody.AppendLine($"- {fields.growth_time}: {growth_time} {units.growth_time}")
                 .AppendLine($"- {fields.economic_benefits}: {economic_benefits}")
                 .Append($"- {fields.information}: {information}");
        }

        bool foundAny = false;

        // Theo "type" Auto scan tất cả
        if (type is EntityType.Plant or EntityType.Auto)
        {
            var p = jsonReader.GetPlantById(id);
            if (p != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Plant);
                AppendBlock(p.tag_name, p.growth_time, p.economic_benefits, p.information);
                foundAny = true;
            }
        }

        if (type is EntityType.Livestock or EntityType.Auto)
        {
            var a = jsonReader.GetLivestockById(id);
            if (a != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Livestock);
                AppendBlock(a.tag_name, a.growth_time, a.economic_benefits, a.information);
                foundAny = true;
            }
        }

        if (type is EntityType.Fish or EntityType.Auto)
        {
            var f = jsonReader.GetFishById(id);
            if (f != null)
            {
                var label = LocalizeGroupLabelHardcoded(langCode, EntityType.Fish);
                AppendBlock(f.tag_name, f.growth_time, f.economic_benefits, f.information);
                foundAny = true;
            }
        }

        if (foundAny)
        {
            scrollInfoPanel.SetActive(true);
            if (headText) headText.text = sbHead.ToString();
            infoText.text = sbBody.ToString();
        }
        else
        {
            scrollInfoPanel.SetActive(false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scrollInfoPanel?.SetActive(false);
    }
}
