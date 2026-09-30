# BÁO CÁO CHI TIẾT: Thiết kế hệ thống đa ngôn ngữ và giao diện người dùng thích ứng trong ứng dụng VR giáo dục xuyên quốc gia

## Dự án: SIMPLE VU2 — Simulation for Interactive Modeling and Participatory Learning Environment (Version 2)

**Đối tác:** IRD (Pháp) – Đại học Cần Thơ (Việt Nam)  
**Nền tảng:** Unity 2022.3.5f1 LTS | URP 14.0.8 | XR Interaction Toolkit 2.5.2 | Meta Quest VR  
**Ngày báo cáo:** Tháng 7/2025

---

## MỤC LỤC

1. [Tổng quan hệ thống](#1-tổng-quan-hệ-thống)
2. [Kiến trúc đa ngôn ngữ hai lớp (Dual Localization Architecture)](#2-kiến-trúc-đa-ngôn-ngữ-hai-lớp)
3. [Hệ thống CSV — LocalizationManager](#3-hệ-thống-csv--localizationmanager)
4. [Hệ thống JSON — Thuan_23127_JsonReader](#4-hệ-thống-json--thuan_23127_jsonreader)
5. [Mô hình dữ liệu đa ngôn ngữ (Data Model Hierarchy)](#5-mô-hình-dữ-liệu-đa-ngôn-ngữ)
6. [Cơ chế chuyển đổi ngôn ngữ và Fallback Chain](#6-cơ-chế-chuyển-đổi-ngôn-ngữ-và-fallback-chain)
7. [Hệ thống UI thích ứng cho VR (Adaptive VR UI)](#7-hệ-thống-ui-thích-ứng-cho-vr)
8. [UIStructureFixer — Giải pháp XR Ray Compatibility](#8-uistructurefixer--giải-pháp-xr-ray-compatibility)
9. [Giao diện HUD nông trại — AreaHUD](#9-giao-diện-hud-nông-trại--areahud)
10. [Hệ thống tooltip đa ngôn ngữ — PlantHoverHandler](#10-hệ-thống-tooltip-đa-ngôn-ngữ--planthoverhandler)
11. [Hệ thống hội thoại NPC](#11-hệ-thống-hội-thoại-npc)
12. [Đồng hồ triều — TidalClockUI](#12-đồng-hồ-triều--tidalclockui)
13. [GameUI — Giao diện trạng thái trò chơi](#13-gameui--giao-diện-trạng-thái-trò-chơi)
14. [BuildUI — Giao diện xây dựng](#14-buildui--giao-diện-xây-dựng)
15. [Hệ thống VU1 (Legacy) và quá trình tiến hóa](#15-hệ-thống-vu1-legacy-và-quá-trình-tiến-hóa)
16. [Phân tích kiến trúc tổng thể](#16-phân-tích-kiến-trúc-tổng-thể)
17. [Sơ đồ luồng dữ liệu](#17-sơ-đồ-luồng-dữ-liệu)
18. [Đánh giá và khuyến nghị](#18-đánh-giá-và-khuyến-nghị)

---

## 1. Tổng quan hệ thống

### 1.1. Bối cảnh

SIMPLE VU2 là ứng dụng VR giáo dục xuyên quốc gia, nhằm mô phỏng quản lý tài nguyên nông nghiệp tại Đồng bằng sông Cửu Long. Dự án phục vụ người dùng từ nhiều quốc gia với ngôn ngữ và văn hóa khác nhau, đòi hỏi:

- **Hỗ trợ đa ngôn ngữ (4 ngôn ngữ):** Tiếng Việt (vi), Tiếng Anh (en), Tiếng Pháp (fr), Tiếng Thái (th)
- **Giao diện thích ứng cho VR:** Tương thích XR ray-based interaction, World Space Canvas
- **Nội dung giáo dục bản địa hóa:** Dữ liệu cây trồng, vật nuôi, thủy sản với tên gọi và thông tin đặc thù từng ngôn ngữ
- **Hệ thống hội thoại NPC:** Các nhân vật nông dân hướng dẫn bằng ngôn ngữ người dùng

### 1.2. Phạm vi ngôn ngữ

| Mã | Ngôn ngữ    | Vai trò               | Đối tác     |
|----|-------------|----------------------|-------------|
| vi | Tiếng Việt  | Ngôn ngữ chính (primary/default) | ĐH Cần Thơ |
| en | Tiếng Anh   | Ngôn ngữ quốc tế   | IRD (Pháp)  |
| fr | Tiếng Pháp  | Ngôn ngữ đối tác gốc | IRD (Pháp)  |
| th | Tiếng Thái  | Mở rộng khu vực    | Đối tác ASEAN |

### 1.3. Danh sách file nguồn

| File | Đường dẫn | Chức năng |
|------|-----------|-----------|
| `LocalizationManager.cs` | `Assets/Scripts/VU2/Others/Localization/` | Singleton quản lý CSV, event-driven |
| `LocalizedKey.cs` | `Assets/Scripts/VU2/Others/Localization/` | Component gắn TextMeshPro, auto-update |
| `Thuan_23127_JsonReader.cs` | `Assets/Scripts/VU2/InternCode/Thuan_23127/` | JSON 4-ngôn ngữ, tra cứu dữ liệu |
| `Root.cs` | `Assets/Scripts/VU2/Systems/Data/` | Cấu trúc gốc JSON (vi/en/fr/th) |
| `Lang.cs` | `Assets/Scripts/VU2/Systems/Data/` | Container ngôn ngữ đơn |
| `Labels.cs` | `Assets/Scripts/VU2/Systems/Data/` | Nhãn UI (13 chuỗi) |
| `Plant.cs` | `Assets/Scripts/VU2/Systems/Data/` | Mô hình cây trồng |
| `Animal.cs` | `Assets/Scripts/VU2/Systems/Data/` | Mô hình vật nuôi |
| `Fish.cs` | `Assets/Scripts/VU2/Systems/Data/` | Mô hình thủy sản |
| `NPCDialogue.cs` | `Assets/Scripts/VU2/Systems/Data/` | Mô hình hội thoại NPC |
| `Gameplay.cs` | `Assets/Scripts/VU2/Systems/Data/` | Thông tin gameplay |
| `Interpretation.cs` | `Assets/Scripts/VU2/Systems/Data/` | Dữ liệu diễn giải |
| `Fields.cs` | `Assets/Scripts/VU2/Systems/Data/` | Nhãn trường dữ liệu |
| `Units.cs` | `Assets/Scripts/VU2/Systems/Data/` | Đơn vị đo lường |
| `StatusText.cs` | `Assets/Scripts/VU2/Systems/Data/` | Text trạng thái (khỏe/bệnh/chết) |
| `Templates.cs` | `Assets/Scripts/VU2/Systems/Data/` | Template mô tả sức khỏe |
| `ConversationUIController.cs` | `Assets/Scripts/VU2/InternCode/LinhH_B2110085/` | Hội thoại NPC typewriter |
| `NPC.cs` | `Assets/Scripts/VU2/InternCode/LinhH_B2110085/` | Tải dữ liệu hội thoại |
| `Thuan_23127_AreaHUD.cs` | `Assets/Scripts/VU2/InternCode/Thuan_23127/` | HUD nông trại |
| `Thuan_23127_PlantHoverHandler.cs` | `Assets/Scripts/VU2/InternCode/Thuan_23127/` | Tooltip hover đa ngôn ngữ |
| `UIStructureFixer.cs` | `Assets/Scripts/VU2/User_Interface/` | Sửa camera stack cho XR |
| `TidalClockUI.cs` | `Assets/Scripts/VU2/Managers/` | Đồng hồ triều trực quan |
| `GameUI.cs` | `Assets/GAMA_Resources/Scripts/RUNTIME/UI/` | UI chính (start/win/lose) |
| `BuildUI.cs` | `Assets/GAMA_Resources/Scripts/RUNTIME/UI/` | UI xây dựng công trình |
| `BaseUI.cs` | `Assets/GAMA_Resources/Scripts/RUNTIME/UI/` | UI billboard cơ sở cho VR |
| `LocalizationData.csv` | `Assets/Resources/Localization/` | File CSV ngôn ngữ |
| `data.json` | `Assets/Resources/` | File JSON dữ liệu đa ngôn ngữ |

---

## 2. Kiến trúc đa ngôn ngữ hai lớp

### 2.1. Thiết kế tổng thể

SIMPLE VU2 sử dụng kiến trúc đa ngôn ngữ **hai lớp song song** (Dual Localization Architecture), mỗi lớp phục vụ một phân khúc nội dung khác nhau:

```
┌──────────────────────────────────────────────────────────────────┐
│                    DUAL LOCALIZATION ARCHITECTURE                │
├──────────────────────┬───────────────────────────────────────────┤
│  LỚP 1: CSV System  │  LỚP 2: JSON System                      │
│  (UI Labels)         │  (Game Content)                           │
├──────────────────────┼───────────────────────────────────────────┤
│ LocalizationManager  │ Thuan_23127_JsonReader                    │
│ LocalizedKey         │ Root → Lang → Labels/Plants/Animals/...   │
├──────────────────────┼───────────────────────────────────────────┤
│ Nguồn: CSV file      │ Nguồn: JSON file                         │
│ Ngôn ngữ: en, vi     │ Ngôn ngữ: vi, en, fr, th                 │
│ Cơ chế: Event-driven │ Cơ chế: Direct reference + callback      │
│ Render: TextMeshPro  │ Render: UnityEngine.UI.Text + TMP        │
│ Singleton + DDoL     │ MonoBehaviour instance                    │
└──────────────────────┴───────────────────────────────────────────┘
```

### 2.2. Phân tách trách nhiệm

| Tiêu chí | CSV System (Lớp 1) | JSON System (Lớp 2) |
|----------|--------------------|--------------------|
| **Phạm vi** | Nhãn UI tĩnh (nút, tiêu đề) | Nội dung game (cây trồng, NPC, diễn giải) |
| **Số ngôn ngữ** | 2 (English, Vietnamese) | 4 (vi, en, fr, th) |
| **Cách tải** | `Resources.Load<TextAsset>` | `Resources.Load<TextAsset>` + `JsonUtility.FromJson<Root>` |
| **Cập nhật UI** | Event delegate `OnLanguageChanged` | Gọi trực tiếp `ApplyLanguage()` |
| **Component** | `LocalizedKey` (auto-subscribe) | Không có component tương đương |
| **Persistence** | `DontDestroyOnLoad` | Tồn tại theo Scene |

### 2.3. Lý do kiến trúc song song

1. **Lịch sử phát triển:** Dự án phát triển bởi nhiều nhóm sinh viên qua nhiều đợt thực tập (intern code). CSV system là framework chung, JSON system do nhóm Thuan_23127 phát triển chuyên biệt cho nội dung nông nghiệp.
2. **Độ phức tạp dữ liệu:** CSV phù hợp cho key-value đơn giản (nhãn UI); JSON cần thiết cho cấu trúc lồng nhau (cây trồng có nhiều thuộc tính, NPC có danh sách hội thoại).
3. **Mở rộng ngôn ngữ:** JSON system hỗ trợ 4 ngôn ngữ với fallback chain — phù hợp cho hợp tác quốc tế.

---

## 3. Hệ thống CSV — LocalizationManager

### 3.1. Kiến trúc Singleton

```csharp
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    
    private const string CsvFilePath = "Localization/LocalizationData";
    private Dictionary<string, Dictionary<string, string>> localizedData;
    private string currentLanguage = "English";
    
    public delegate void LanguageChanged();
    public static event LanguageChanged OnLanguageChanged;
}
```

**Đặc điểm:**
- **Singleton pattern** với `DontDestroyOnLoad` — tồn tại xuyên Scene
- **Event delegate** `OnLanguageChanged` — tất cả component đăng ký sẽ tự cập nhật khi đổi ngôn ngữ
- **Cấu trúc dữ liệu 2 lớp:** `Dictionary<language, Dictionary<key, value>>` cho tra cứu O(1)

### 3.2. CSV Parser thủ công

```csharp
private List<string> ParseCsvLine(string line)
{
    var fields = new List<string>();
    var currentField = new StringBuilder();
    bool inQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (inQuotes)
        {
            if (c == '"')
            {
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip escaped quote
                }
                else { inQuotes = false; }
            }
            else { currentField.Append(c); }
        }
        else
        {
            if (c == '"') { inQuotes = true; }
            else if (c == ',') { fields.Add(currentField.ToString()); currentField.Clear(); }
            else { currentField.Append(c); }
        }
    }
    fields.Add(currentField.ToString());
    return fields;
}
```

**Phân tích kỹ thuật:**
- Parser hỗ trợ **quoted fields** — xử lý đúng chuỗi có dấu phẩy bên trong ngoặc kép
- Hỗ trợ **escaped quotes** (`""` → `"`) — chuẩn RFC 4180
- Sử dụng `StringBuilder` thay vì string concatenation cho hiệu năng
- **Lưu ý:** Chưa xử lý line breaks bên trong quoted fields (giới hạn do `string.Split(new[] { '\r', '\n' })` tại bước đầu)

### 3.3. Event-Driven Language Switching

```csharp
public void SetLanguage(string languageName)
{
    if (localizedData.ContainsKey(languageName))
    {
        currentLanguage = languageName;
        OnLanguageChanged?.Invoke();  // Broadcast to all subscribers
    }
}
```

**Luồng hoạt động:**
```
User chọn ngôn ngữ
    ↓
SetLanguage("Vietnamese")
    ↓
OnLanguageChanged?.Invoke()
    ↓ (broadcast)
┌───────────────────────────────────────┐
│ LocalizedKey_1.UpdateText()           │
│ LocalizedKey_2.UpdateText()           │
│ LocalizedKey_N.UpdateText()           │
│ (Tất cả component đã subscribe)      │
└───────────────────────────────────────┘
```

### 3.4. LocalizedKey Component

```csharp
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
        LocalizationManager.OnLanguageChanged -= UpdateText;  // Prevent memory leak
    }

    public void UpdateText()
    {
        string localizedText = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
        if (!string.IsNullOrEmpty(localizedText))
            textComponent.text = localizedText;
    }
}
```

**Thiết kế Observer Pattern:**
- Mỗi `LocalizedKey` tự đăng ký vào event `OnLanguageChanged` khi `Start()`
- Tự hủy đăng ký khi `OnDestroy()` — ngăn memory leak
- Workflow cho Designer: kéo thả component vào TextMeshPro, nhập key → tự động hoạt động

### 3.5. Dữ liệu CSV hiện tại

```csv
Key,English,Vietnamese
test.hello,Hello,Xin Chao
```

**Nhận xét:** File CSV hiện chỉ có 1 entry test. Hệ thống CSV đã sẵn sàng về mặt kỹ thuật nhưng nội dung chưa được mở rộng — phần lớn bản địa hóa thực tế được xử lý bởi hệ thống JSON.

---

## 4. Hệ thống JSON — Thuan_23127_JsonReader

### 4.1. Kiến trúc tổng thể

```csharp
public class Thuan_23127_JsonReader : MonoBehaviour
{
    // UI References (13 Text elements)
    public Text nameText, levelText, scoreText, infoText, 
                scoreTextEndGame, playAgainText, settingText,
                salinityText, scoreTextDetails;

    // Configuration
    public string fileName = "data";
    public string currentLang = "vi";
    public Root root;
    private string _jsonString;
}
```

**Đặc điểm:**
- **MonoBehaviour** (không phải Singleton) — tồn tại trong Scene, có thể có nhiều instance
- Tham chiếu trực tiếp 9 UI Text elements qua Inspector
- Sử dụng `JsonUtility.FromJson<Root>()` — bộ deserializer built-in của Unity (nhanh, zero-allocation)

### 4.2. Quy trình tải dữ liệu

```csharp
protected virtual void Start()
{
    // 1. Load JSON từ Resources
    var resourceName = Path.GetFileNameWithoutExtension(fileName);
    var jsonFile = Resources.Load<TextAsset>(resourceName);
    _jsonString = jsonFile.text;
    
    // 2. Parse JSON → Root object
    root = JsonUtility.FromJson<Root>(_jsonString);

    // 3. Đăng ký với GameManager
    var gm = Thuan_23127_GameManager.Instance;
    if (gm) gm.jsonReader = this;

    // 4. Áp dụng ngôn ngữ ban đầu
    ApplyLanguage();
}
```

### 4.3. Chuyển đổi ngôn ngữ qua Dropdown

```csharp
public void SetLanguageByIndex(int index)
{
    switch (index)
    {
        case 0: currentLang = "vi"; break;  // Tiếng Việt
        case 1: currentLang = "en"; break;  // English
        case 2: currentLang = "fr"; break;  // Français
        case 3: currentLang = "th"; break;  // ภาษาไทย
        default: currentLang = "vi"; break; // Fallback
    }
    ApplyLanguage();
}
```

**Mapping UI Dropdown → Language Code:**
```
Dropdown Index 0 ──→ "vi" (Tiếng Việt)
Dropdown Index 1 ──→ "en" (English)
Dropdown Index 2 ──→ "fr" (Français)
Dropdown Index 3 ──→ "th" (ภาษาไทย)
Default          ──→ "vi" (Fallback)
```

### 4.4. Hàm ApplyLanguage — Cập nhật toàn bộ UI

```csharp
private void ApplyLanguage()
{
    if (root == null) return;
    var l = GetCurrentLangData();
    if (l == null) return;

    // Cập nhật nhãn cố định
    if (infoText)      infoText.text      = l.labels?.info      ?? "INFO";
    if (nameText)      nameText.text      = $"{l.labels?.name ?? "Name"}: {l.gameplay?.name}";
    if (levelText)     levelText.text     = $"{l.labels?.level ?? "Level"}: {l.gameplay?.level}";
    if (settingText)   settingText.text   = l.labels?.setting   ?? "Setting";
    if (playAgainText) playAgainText.text = l.labels?.playagain ?? "Play Again";

    // Cập nhật độ mặn
    var gm = Thuan_23127_GameManager.Instance;
    if (gm) UpdateSalinityUI(gm.GetSeasonSalinity());
    
    // Cập nhật điểm
    var label = l.labels?.score ?? "Score";
    var currentScore = gm ? gm.Score : 0;
    if (scoreText)        scoreText.text        = $"{label}: {currentScore}";
    if (scoreTextEndGame) scoreTextEndGame.text  = $"{label}: {currentScore}";
    if (scoreTextDetails) scoreTextDetails.text  = $"{label}: {currentScore}";
}
```

### 4.5. Tra cứu dữ liệu theo ID

```csharp
// Tra cứu cây trồng (Plant ID → Plant data)
public Plant  GetPlantById(int id)     => GetCurrentLangPlants()?.FirstOrDefault(p => p.id == id);

// Tra cứu vật nuôi (Animal ID → Animal data)
public Animal GetLivestockById(int id) => GetCurrentLangAnimals()?.FirstOrDefault(a => a.id == id);

// Tra cứu thủy sản (Fish ID → Fish data)
public Fish   GetFishById(int id)      => GetCurrentLangFish()?.FirstOrDefault(f => f.id == id);
```

**Sử dụng bởi:**
- `PlantGrowth.Init()` — lấy thông tin cây khi khởi tạo
- `PlantHoverHandler.OnPointerEnter()` — hiển thị tooltip khi hover
- Hệ thống tính điểm — lấy `economic_benefits` và `salinity_threshold`

### 4.6. Cập nhật độ mặn đa ngôn ngữ

```csharp
public void UpdateSalinityUI(float salinity)
{
    var l = GetCurrentLangData();
    string label = l?.labels?.salinity ?? "Salinity";
    if (salinityText) salinityText.text = $"{label}: {salinity:0.00}";
}
```

**Ví dụ output theo ngôn ngữ:**
- vi: `"Độ mặn: 0.85"`
- en: `"Salinity: 0.85"`
- fr: `"Salinité: 0.85"`
- th: `"ความเค็ม: 0.85"`

---

## 5. Mô hình dữ liệu đa ngôn ngữ

### 5.1. Cây phân cấp (Data Model Hierarchy)

```
Root                           ← Gốc JSON ({vi:{...}, en:{...}, ...})
├── vi: Lang                   ← Dữ liệu Tiếng Việt
├── en: Lang                   ← Dữ liệu Tiếng Anh
├── fr: Lang                   ← Dữ liệu Tiếng Pháp
└── th: Lang                   ← Dữ liệu Tiếng Thái
    │
    Lang                       ← Container cho 1 ngôn ngữ
    ├── labels: Labels         ← 13 nhãn UI
    ├── gameplay: Gameplay     ← Tên game, level, score
    ├── interpretation: InterpretationData  ← Diễn giải sức khỏe
    │   ├── fields: Fields     ← 16 tên trường (đa ngôn ngữ)
    │   ├── units: Units       ← Đơn vị đo lường
    │   ├── status_values: string[]  ← ["Tốt", "Bệnh", "Chết"]
    │   ├── status_text: StatusText  ← {healthy, diseased, dead}
    │   └── templates: Templates     ← Template mô tả động
    ├── plants: List<Plant>          ← 13 loại cây trồng
    ├── livestock: List<Animal>      ← 6 loại vật nuôi
    ├── fish: List<Fish>             ← 6 loại thủy sản
    └── npcDialogues: List<NPCDialogue>  ← Hội thoại NPC
```

### 5.2. Chi tiết từng class dữ liệu

#### Root — Cấu trúc gốc
```csharp
[System.Serializable]
public class Root
{
    public Lang vi;  // Vietnamese (primary)
    public Lang en;  // English
    public Lang fr;  // French
    public Lang th;  // Thai
}
```

#### Labels — 13 nhãn UI cốt lõi
```csharp
[System.Serializable]
public class Labels
{
    // Nhóm chính
    public string info;           // "THÔNG TIN" / "INFO"
    public string name;           // "Tên" / "Name"
    public string level;          // "Cấp độ" / "Level"
    public string score;          // "Sản lượng" / "Score"
    public string playagain;      // "Chơi lại" / "Play Again"
    public string language;       // "Ngôn ngữ" / "Language"
    public string setting;        // "Cài đặt" / "Settings"
    public string salinity;       // "Độ mặn" / "Salinity"
    
    // Nhóm SeasonHUD (David's additions)
    public string water_level;    // "Mực nước sông" / "River Level"
    public string full;           // "Đầy" / "Full"
    public string low;            // "Thấp" / "Low"
    public string season_rainy;   // "Mùa mưa" / "Rainy Season"
    public string season_dry;     // "Mùa khô" / "Dry Season"
}
```

#### Plant — Mô hình cây trồng (13 loại)
```csharp
[System.Serializable]
public class Plant
{
    public int id;                    // 1-13
    public string tag_name;           // "Cây sầu riêng" / "Durian"
    public int growth_time;           // Thời gian sinh trưởng (ngày)
    public string[] status;           // ["Tốt", "Bệnh", "Chết"]
    public int economic_benefits;     // Điểm kinh tế (2-5)
    public string information;        // Mô tả chi tiết
    public float salinity_threshold;  // Ngưỡng chịu mặn (‰)
    public int harvest_time;          // Thời gian thu hoạch (giây)
}
```

#### InterpretationData — Hệ thống diễn giải
```csharp
[System.Serializable]
public class InterpretationData
{
    public Fields fields;          // 16 tên trường đa ngôn ngữ
    public Units units;            // Đơn vị đo lường
    public string[] status_values; // Mảng giá trị trạng thái
    public StatusText status_text; // Healthy/Diseased/Dead text
    public Templates templates;    // Template mô tả động
}

[System.Serializable]
public class Fields
{
    public string id, tag_name, growth_time, status, economic_benefits,
        information, salinity, status_good, status_sick, status_dead,
        productivity, base_yield, adjusted_yield, threshold_label,
        current_salinity_label, unit_ppt;
}

[System.Serializable]
public class Templates
{
    public string healthy_desc;   
    // vi: "{tag} đang {status}. {currentLabel}: {current}{unit} | {thresholdLabel}: {threshold}{unit}."
    public string diseased_desc;  
    // vi: "{tag} đang {status} do mặn vượt ngưỡng. {currentLabel}: {current}{unit} > {thresholdLabel}: {threshold}{unit}."
}
```

### 5.3. Dữ liệu mẫu từ data.json

**Cây trồng (vi):**
| ID  | Tên         | Kinh tế | Ngưỡng mặn (‰) |
|-----|-------------|---------|-----------------|
| 1   | Cây sầu riêng | 4     | 0.80            |
| 2   | Chuối       | 5       | 0.80            |
| 3   | Bắp cải     | 2       | 1.15            |
| 4   | Bắp         | 3       | 1.09            |
| 5   | Thanh long   | 2       | 2.00            |
| 6   | Ổi          | 3       | 3.01            |
| 7   | Lúa         | 4       | 1.92            |
| 8   | Mía         | 3       | 12.00           |
| 9   | Cam         | 3       | 0.83            |
| 10  | Dừa         | 3       | 0.80            |

### 5.4. NPCDialogue — Hội thoại đa ngôn ngữ

```csharp
[System.Serializable]
public class NPCDialogue
{
    public string npcId;              // "farmer", "linhh", "engineer_linh"
    public string npcName;            // "Nông dân" / "Farmer" / "Agriculteur"
    public List<string> dialogues;    // Danh sách câu hội thoại theo thứ tự
}
```

---

## 6. Cơ chế chuyển đổi ngôn ngữ và Fallback Chain

### 6.1. Fallback Chain Pattern

```csharp
public Lang GetCurrentLangData()
{
    if (root == null) return null;

    var code = string.IsNullOrEmpty(currentLang) ? "vi" : currentLang.ToLowerInvariant();
    
    // Thử lấy ngôn ngữ được yêu cầu
    var pick = code switch
    {
        "vi" => root.vi,
        "en" => root.en,
        "fr" => root.fr,
        "th" => root.th,
        _    => null
    };
    
    if (pick != null) return pick;
    
    // Chuỗi fallback: vi → en → fr → th
    if (root.vi != null) return root.vi;
    if (root.en != null) return root.en;
    if (root.fr != null) return root.fr;
    if (root.th != null) return root.th;
    return null;
}
```

**Sơ đồ Fallback:**
```
User chọn "th" (Thai)
    ↓
root.th != null?
    ├── CÓ → Trả về root.th ✓
    └── KHÔNG ↓
        root.vi != null?
            ├── CÓ → Trả về root.vi (fallback 1) ✓
            └── KHÔNG ↓
                root.en != null?
                    ├── CÓ → Trả về root.en (fallback 2) ✓
                    └── KHÔNG ↓
                        root.fr != null?
                            ├── CÓ → Trả về root.fr (fallback 3) ✓
                            └── KHÔNG → return null
```

**Ý nghĩa thiết kế:**
- Tiếng Việt là fallback ưu tiên cao nhất (người dùng chính tại ĐBSCL)
- Đảm bảo ứng dụng không crash dù thiếu dữ liệu ngôn ngữ
- Thứ tự ưu tiên phản ánh mức độ hoàn thiện nội dung

### 6.2. So sánh cơ chế chuyển đổi hai hệ thống

| Tiêu chí | CSV System | JSON System |
|----------|-----------|-------------|
| **Trigger** | `SetLanguage(string name)` | `SetLanguageByIndex(int index)` |
| **Input** | Tên đầy đủ ("English", "Vietnamese") | Index dropdown (0-3) |
| **Broadcast** | Event delegate to N subscribers | Gọi trực tiếp `ApplyLanguage()` |
| **Fallback** | Không có (log warning) | Chain: vi → en → fr → th |
| **Null-safe** | `ContainsKey` check | Null-conditional `?.` |

---

## 7. Hệ thống UI thích ứng cho VR

### 7.1. Thách thức thiết kế UI trong VR

```
┌─────────────────────────────────────────────────────────┐
│                  THÁCH THỨC VR UI                       │
├─────────────────────────────────────────────────────────┤
│ 1. XR Ray bị chặn bởi Overlay Camera Canvas            │
│ 2. UI phải follow đầu người dùng (billboard effect)     │
│ 3. World Space Canvas cần tôn trọng khoảng cách 3D     │
│ 4. Input từ XR Controller (không phải mouse/touch)      │
│ 5. TextMeshPro phải đọc được ở khoảng cách VR           │
│ 6. Multiple Canvas layers phải ray-aware                 │
└─────────────────────────────────────────────────────────┘
```

### 7.2. BaseUI — Billboard Pattern cho VR

```csharp
public class BaseUI : MonoBehaviour
{
    [SerializeField] private Transform head;       // Camera/head transform
    [SerializeField] private float spawnDistance;   // Khoảng cách spawn

    void Start()
    {
        // Đặt UI phía trước người dùng
        transform.position = head.position 
            + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;
    }

    void Update()
    {
        // Billboard: UI luôn quay mặt về phía người dùng
        transform.LookAt(new Vector3(head.position.x, transform.position.y, head.position.z));
        transform.forward *= -1;  // Lật 180° (LookAt mặc định quay lưng)
    }
}
```

**Kỹ thuật Billboard:**
- `LookAt()` trên trục Y cố định — UI không bị xoay nghiêng theo đầu
- `transform.forward *= -1` — bù cho hành vi mặc định của `LookAt()` (hướng về camera = hướng lưng)
- `spawnDistance` điều chỉnh — khoảng cách thoải mái cho đọc text trong VR

### 7.3. Kiến trúc tầng UI

```
XR Origin (VR Headset)
├── Main Camera (URP, Base)
│   └── UI (reparented by UIStructureFixer)
│       └── UIGAMEMENU (World Space Canvas)
│           ├── StartContent (Menu bắt đầu)
│           ├── FinalContent_Win (Màn hình thắng)
│           └── FinalContent_Lose (Màn hình thua)
├── UI Camera (Overlay) — DISABLED at runtime
├── FarmArea_1 → AreaHUD_1 (World Space)
├── FarmArea_2 → AreaHUD_2 (World Space)
├── TidalClockUI (World Space)
├── BuildUI (World Space)
└── NPC Dialogue Panel (Screen Space → World Space)
```

---

## 8. UIStructureFixer — Giải pháp XR Ray Compatibility

### 8.1. Vấn đề

Trong cấu trúc URP mặc định, "UI Camera" (Overlay mode) nằm trong camera stack của Main Camera:

```
Main Camera (Base)
└── Camera Stack
    └── UI Camera (Overlay) ← CHẶN MỌI XR RAY
        └── UI (World Space Canvas)
            └── GraphicRaycaster ← XRUIInputModule query canvas này
```

**Hậu quả:** `XRUIInputModule` truy vấn `GraphicRaycaster` trên mọi Canvas → ray bị chặn bởi UI dù UI ở xa trong không gian 3D.

### 8.2. Giải pháp runtime (7 bước)

```csharp
public class UIStructureFixer : MonoBehaviour
{
    public string uiCameraName = "UI Camera";
    public string uiRootName = "UI";

    private void Awake()
    {
        FixUIStructure();
    }

    public void FixUIStructure()
    {
        // Bước 1: Tìm Main Camera
        Camera mainCam = Camera.main;
        
        // Bước 2: Tìm UI Camera
        GameObject uiCamObj = /* tìm camera tên "UI Camera" */;
        Camera uiCam = uiCamObj.GetComponent<Camera>();

        // Bước 3: Xóa UI Camera khỏi stack
        var mainCamData = mainCam.GetUniversalAdditionalCameraData();
        mainCamData.cameraStack.Remove(uiCam);

        // Bước 4: Tìm object "UI" (con của UI Camera)
        Transform uiRoot = uiCamObj.transform.Find(uiRootName);

        // Bước 5: Chuyển "UI" sang con Main Camera
        uiRoot.SetParent(mainCam.transform, true);  // worldPositionStays = true

        // Bước 6: Thêm Layer 5 (UI) vào cullingMask
        mainCam.cullingMask |= (1 << 5);

        // Bước 7: Tắt UI Camera
        uiCam.enabled = false;
        uiCamObj.SetActive(false);
    }
}
```

### 8.3. Kết quả sau khi sửa

```
Main Camera (Base, cullingMask += UI layer)
├── UI (reparented, World Space)  ← XR ray tôn trọng khoảng cách 3D
│   └── UIGAMEMENU
│       └── Canvas (World Space) → GraphicRaycaster hoạt động đúng
└── [UI Camera: DISABLED]
```

**Lợi ích:**
- XR ray chỉ tương tác với UI khi ray thực sự chỉ vào UI element
- Canvas World Space tôn trọng khoảng cách 3D (gần/xa)
- `CanvasFollower` tho targetCamera = Main Camera tiếp tục hoạt động bình thường

---

## 9. Giao diện HUD nông trại — AreaHUD

### 9.1. Tổng quan

Mỗi `FarmArea` trong trò chơi có một `Thuan_23127_AreaHUD` hiển thị trực tiếp trên bảng thông tin 3D tại khu vực nông trại.

### 9.2. Các thành phần UI

```
┌─────────────────────────────────────────┐
│              AREA HUD                   │
├─────────────────────────────────────────┤
│  [Icon cây] [████████░░ 75%]            │  ← Progress bar + Subject
│  Độ mặn: 0.85 / 1.20                   │  ← Salinity (localized label)
│  Mô tả sức khỏe: Đang tốt              │  ← Description text
│                                         │
│  ┌─────────┬──────────┐                │
│  │ Mùa Mưa │ Mùa Khô  │                │  ← Season score columns
│  │  [🌾] 5  │  [🌴] 3   │                │
│  └─────────┴──────────┘                │
│                                         │
│  [Xem chi tiết độ mặn ▶]               │  ← Button → Popup
└─────────────────────────────────────────┘
```

### 9.3. Hệ thống Popup chia sẻ

```csharp
// Static reference: chỉ 1 HUD sở hữu popup tại một thời điểm
private static Thuan_23127_AreaHUD _currentOwner;

public void OnClickShowInformationSalinity()
{
    bool visible = showUIInformationSalinity.activeSelf;
    if (_popupCg != null) visible = visible && _popupCg.alpha > 0.01f;

    // Nếu popup đang mở và thuộc về HUD này → đóng
    if (visible && _currentOwner == this)
    {
        HideInformationSalinity();
        return;
    }

    // Mở popup với dữ liệu của HUD này
    if (popupHeadText)
    {
        if (salinityTextPro != null && !string.IsNullOrEmpty(salinityTextPro.text))
            popupHeadText.text = salinityTextPro.text;
        else if (salinityText != null)
            popupHeadText.text = salinityText.text;
    }
    if (popupBodyText && descriptionText)
        popupBodyText.text = descriptionText.text ?? string.Empty;

    showUIInformationSalinity.SetActive(true);
    if (_popupCg != null)
    {
        _popupCg.alpha = 1f;
        _popupCg.blocksRaycasts = false;  // Không chặn XR ray
    }
    _currentOwner = this;
}
```

**Thiết kế đáng chú ý:**
- **Static ownership pattern:** `_currentOwner` đảm bảo chỉ 1 popup hiển thị tại một thời điểm
- **CanvasGroup alpha:** Sử dụng alpha thay vì SetActive cho fade animation
- `blocksRaycasts = false` — popup không chặn XR ray (chỉ hiển thị thông tin)

### 9.4. Tích hợp đa ngôn ngữ cho độ mặn

```csharp
public void SetSalinity(float current, float threshold)
{
    string salinityLabel = "Salinity";
    if (jsonReader != null)
    {
        var langData = jsonReader.GetCurrentLangData();
        if (langData?.interpretation?.fields != null)
        {
            salinityLabel = langData.interpretation.fields.salinity ?? "Salinity";
        }
    }
    string formattedText = $"{salinityLabel} : {current:0.00} / {threshold:0.00}";
    if (salinityText)    salinityText.text    = formattedText;
    if (salinityTextPro) salinityTextPro.text = formattedText;
}
```

**Dual text support:** Hỗ trợ cả `UnityEngine.UI.Text` và `TextMeshProUGUI` — backward compatible với UI cũ.

### 9.5. Hệ thống điểm theo mùa

```csharp
public enum SeasonPhase { Rainy1 = 0, Dry = 1, Rainy2 = 2 }

[Serializable]
public class SeasonUI
{
    public Text  scoreText;       // Text hiển thị điểm
    public Image iconImage;       // Icon sản phẩm thu hoạch
    public Sprite defaultIcon;    // Icon mặc định
}

// 2 cột: Mùa Mưa và Mùa Khô
public SeasonUI rainy;
public SeasonUI dry;
private readonly int[] _phaseScores = new int[2];  // [0]=Rainy, [1]=Dry
```

**Luồng cập nhật điểm:**
```
FarmArea thu hoạch cây
    ↓
AreaHUD.AddSeasonPointsPhase(SeasonPhase.Rainy1, delta: +4, iconOverride: durian_sprite)
    ↓
_phaseScores[0] += 4
rainy.scoreText.text = "4"
rainy.iconImage.sprite = durian_sprite
```

---

## 10. Hệ thống tooltip đa ngôn ngữ — PlantHoverHandler

### 10.1. Kiến trúc

```csharp
public enum EntityType
{
    Auto = 0,       // Tự dò cả 3 loại
    Plant = 1,      // Cây trồng
    Livestock = 2,  // Vật nuôi
    Fish = 3        // Thủy sản
}

public class Thuan_23127_PlantHoverHandler : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler
{
    public int id;                         // ID thực thể
    public EntityType type = EntityType.Auto;
    public GameObject scrollInfoPanel;     // Panel tooltip
    public Text headText;                  // Tiêu đề
    public Text infoText;                  // Nội dung
}
```

### 10.2. Logic hover đa ngôn ngữ

```csharp
public void OnPointerEnter(PointerEventData eventData)
{
    // Ngăn tooltip mở khi đang click/drag
    if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || eventData.eligibleForClick) return;

    var lang = jsonReader.GetCurrentLangData();
    var fields = lang.interpretation.fields;
    var units  = lang.interpretation.units;
    var langCode = jsonReader.GetCurrentLangCode();

    void AppendBlock(string tag_name, int growth_time, int economic_benefits, string information)
    {
        sbBody.AppendLine($"- {fields.growth_time}: {growth_time} {units.growth_time}")
              .AppendLine($"- {fields.economic_benefits}: {economic_benefits}")
              .Append($"- {fields.information}: {information}");
    }

    // Auto scan: thử tìm ở cả 3 type
    if (type is EntityType.Plant or EntityType.Auto)
    {
        var p = jsonReader.GetPlantById(id);
        if (p != null) AppendBlock(p.tag_name, p.growth_time, p.economic_benefits, p.information);
    }
    if (type is EntityType.Livestock or EntityType.Auto)
    {
        var a = jsonReader.GetLivestockById(id);
        if (a != null) AppendBlock(a.tag_name, a.growth_time, a.economic_benefits, a.information);
    }
    if (type is EntityType.Fish or EntityType.Auto)
    {
        var f = jsonReader.GetFishById(id);
        if (f != null) AppendBlock(f.tag_name, f.growth_time, f.economic_benefits, f.information);
    }
}
```

### 10.3. Bản địa hóa nhãn nhóm (Hardcoded fallback)

```csharp
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
```

**Nhận xét:** Hàm này sử dụng fallback hardcoded (chỉ vi/en) thay vì đọc từ JSON — có thể cải thiện để hỗ trợ đầy đủ 4 ngôn ngữ.

### 10.4. Xử lý XR Raycast trên tooltip

```csharp
private void Start()
{
    if (scrollInfoPanel)
    {
        _panelCg = scrollInfoPanel.GetComponent<CanvasGroup>();
        if (!_panelCg) _panelCg = scrollInfoPanel.AddComponent<CanvasGroup>();
        _panelCg.interactable = false;
        _panelCg.blocksRaycasts = false;

        // Tắt raycast cho tất cả Graphic con
        foreach (var g in scrollInfoPanel.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
    }
}
```

**Mục đích:** Tooltip không "nuốt" XR ray — người dùng có thể tương tác với thế giới 3D phía sau tooltip.

---

## 11. Hệ thống hội thoại NPC

### 11.1. Kiến trúc MVC

```
┌──────────┐    ┌───────────────────────────┐    ┌──────────────────────────┐
│   NPC    │───→│ ConversationUIController  │───→│   Dialogue Panel (UI)    │
│ (Model)  │    │      (Controller)          │    │   _npcNameText           │
│          │    │                             │    │   _dialogueText          │
│ _npcId   │    │ StartConversation()         │    │   _dialoguePanel         │
│ _root    │    │ PlayDialogue() [Coroutine]  │    │                          │
│          │    │ PlayNextLine()              │    │                          │
│          │    │ EndConversation()           │    │                          │
└──────────┘    └───────────────────────────┘    └──────────────────────────┘
```

### 11.2. NPC — Tải hội thoại đa ngôn ngữ

```csharp
public class NPC : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private ConversationUIController _conversationController;
    [SerializeField] private Thuan_23127_JsonReader _jsonReader;

    public void Talk()
    {
        GetDialoguesFromData();
        if (_npcDialogues != null)
            _conversationController.StartConversation(_npcDialogues.npcName, _npcDialogues.dialogues);
    }

    private void GetDialoguesFromData()
    {
        // Load JSON
        TextAsset jsonFile = Resources.Load<TextAsset>("data");
        _root = JsonUtility.FromJson<Root>(jsonFile.text);

        // Lấy dữ liệu ngôn ngữ hiện tại từ JsonReader
        var lang = _jsonReader.GetCurrentLangData();

        // Tìm hội thoại theo npcId
        _npcDialogues = lang?.npcDialogues.Find(npc => npc.npcId == _npcId);
    }
}
```

**Luồng đa ngôn ngữ:**
```
NPC._npcId = "farmer"
    ↓
_jsonReader.GetCurrentLangData() → root.vi  (nếu currentLang = "vi")
    ↓
lang.npcDialogues.Find(npc => npc.npcId == "farmer")
    ↓
NPCDialogue {
    npcId: "farmer",
    npcName: "Nông dân",
    dialogues: ["Xin chào! Tôi là nông dân...", "Hãy cẩn thận với độ mặn...", ...]
}
```

### 11.3. ConversationUIController — Typewriter Effect

```csharp
public class ConversationUIController : MonoBehaviour
{
    [SerializeField] private float _typeSpeed;
    [SerializeField] private Text _dialogueText;
    [SerializeField] private Text _npcNameText;

    private StringBuilder stringBuilder = new StringBuilder();
    private bool _isTyping = false;
    private int _currentLine;
    private List<string> _npcDialogues;

    private IEnumerator PlayDialogue()
    {
        _isTyping = true;
        _dialogueText.text = "";
        stringBuilder.Clear();

        // Hiệu ứng typewriter: thêm từng ký tự
        foreach (var letter in _npcDialogues[_currentLine])
        {
            stringBuilder.Append(letter);
            _dialogueText.text = stringBuilder.ToString();
            yield return new WaitForSeconds(_typeSpeed);
        }
        _isTyping = false;
    }
}
```

### 11.4. Cơ chế Skip Typing

```csharp
public void PlayNextLine()
{
    bool skipTyping = false;
    
    // Nếu đang typing → hiển thị toàn bộ text (skip)
    if (_isTyping)
    {
        StopAllCoroutines();
        _dialogueText.text = _npcDialogues[_currentLine];
        skipTyping = true;
        _isTyping = false;
    }

    if (skipTyping) return;  // Lần click đầu = skip, lần click 2 = next line

    // Chuyển sang dòng tiếp theo
    if (++_currentLine < _npcDialogues.Count)
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogue());
    }
    else
    {
        EndConversation();
    }
}
```

**Trải nghiệm người dùng:**
```
Click 1: Bắt đầu typing "Xin chào! Tôi là n..."
Click 2: Skip → Hiển thị đầy đủ "Xin chào! Tôi là nông dân ở ĐBSCL."
Click 3: Chuyển sang câu tiếp "Hãy cẩn thận với độ mặn..."
...
Click cuối: EndConversation() → Ẩn dialogue panel
```

---

## 12. Đồng hồ triều — TidalClockUI

### 12.1. Thiết kế trực quan

```
               12h (Trăng khuyết, VT4)
                    │
                    ●
                   / \
                  /   \
     9h ●───────●─────●───────● 3h
   (VT1)      [🌍]        (VT3)
  Không trăng  Trái đất   Trăng tròn
                  \   /
                   \ /
                    ●
                    │
               6h (Trăng khuyết, VT2)

   ← ← ← Tia sáng Mặt trời → → →

   Trạng thái: [Triều Cường] / [Triều Kém]
   Cường độ: [████████░░░] 0.78
```

### 12.2. Cấu trúc component

```csharp
public class TidalClockUI : MonoBehaviour
{
    // Quỹ đạo Mặt trăng
    public RectTransform moonIcon;
    public RectTransform clockCenter;
    public float orbitRadius = 60f;

    // 4 pha Mặt trăng
    public Sprite[] moonPhaseSprites = new Sprite[4];
    public Image moonImage;

    // 4 marker vị trí
    public Image[] positionMarkers = new Image[4];
    public Color activeMarkerColor = Color.yellow;
    public Color inactiveMarkerColor = new Color(1f, 1f, 1f, 0.4f);

    // Text labels
    public TextMeshProUGUI tideStateText;
    public TextMeshProUGUI moonPhaseText;

    // Chỉ báo cường độ
    public Image tidalIntensityFill;
    public Gradient tidalIntensityGradient;
    public GameObject springTideWarningIcon;
}
```

### 12.3. Animation quỹ đạo Mặt trăng

```csharp
private void UpdateMoonPosition()
{
    // Chuyển pha (0.0-1.0) thành góc: bắt đầu từ TRÁI (180°), quay kim đồng hồ
    float angleRad = (Mathf.PI - _manager.MoonPhaseNormalized * 2f * Mathf.PI);
    float x = Mathf.Cos(angleRad) * orbitRadius;
    float y = Mathf.Sin(angleRad) * orbitRadius;
    moonIcon.anchoredPosition = clockCenter.anchoredPosition + new Vector2(x, y);
}
```

**Ánh xạ vị trí:**
| Pha (normalized) | Vị trí | Góc | Tên Mặt trăng |
|------------------|--------|------|---------------|
| 0.00 | Trái (9h) | 180° | Không trăng |
| 0.25 | Dưới (6h) | 270° | Trăng khuyết |
| 0.50 | Phải (3h) | 0° | Trăng tròn |
| 0.75 | Trên (12h) | 90° | Trăng khuyết |

### 12.4. Chuỗi hiển thị tiếng Việt

```csharp
private readonly string[] _phaseNames = new string[]
{
    "Không trăng",      // NewMoon
    "Trăng khuyết",     // FirstQuarter
    "Trăng tròn",       // FullMoon
    "Trăng khuyết"      // LastQuarter
};

private const string SPRING_TIDE_TEXT = "Triều Cường";
private const string NEAP_TIDE_TEXT   = "Triều Kém";
```

**Nhận xét:** TidalClockUI hiện tại sử dụng **chuỗi hardcoded tiếng Việt** — chưa tích hợp với hệ thống đa ngôn ngữ. Đây là điểm cần cải thiện cho phiên bản xuyên quốc gia.

### 12.5. Hệ thống sự kiện

```csharp
// Subscribe event từ TidalClockManager
TidalClockManager.OnTidalPhaseChanged += OnPhaseChanged;
TidalClockManager.OnTidalStateChanged += OnTideStateChanged;

private void OnTideStateChanged(TidalState state)
{
    if (tideStateText)
    {
        tideStateText.text = state == TidalState.SpringTide ? SPRING_TIDE_TEXT : NEAP_TIDE_TEXT;
        tideStateText.color = state == TidalState.SpringTide
            ? new Color(0.9f, 0.2f, 0.2f)   // Đỏ = Triều Cường (cảnh báo)
            : new Color(0.2f, 0.6f, 1f);     // Xanh = Triều Kém (an toàn)
    }
    if (springTideWarningIcon)
        springTideWarningIcon.SetActive(state == TidalState.SpringTide);
}
```

---

## 13. GameUI — Giao diện trạng thái trò chơi

### 13.1. Kiến trúc Singleton

```csharp
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    // References
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SimulationManager simulationManager;
    [SerializeField] private Transform head;               // VR head transform
    [SerializeField] private float spawnDistance;           // Khoảng cách UI

    // UI States
    [SerializeField] private GameObject startContent;      // Menu bắt đầu
    [SerializeField] private GameObject finalContent;      // Kết thúc (legacy)
    [SerializeField] private GameObject finalContent_Win;  // Màn thắng
    [SerializeField] private GameObject finalContent_Lose; // Màn thua
    
    // Report: Win
    [SerializeField] private TextMeshProUGUI win_reportLivingTreesNumber;
    [SerializeField] private TextMeshProUGUI win_reportDeadTreesNumber;
    [SerializeField] private TextMeshProUGUI win_reportPumpNumber;
    [SerializeField] private TextMeshProUGUI win_reportEnemiesNumber;
    [SerializeField] private TextMeshProUGUI win_reportSubsidenceScore;

    // Report: Lose (mirror fields)
    [SerializeField] private TextMeshProUGUI lose_reportLivingTreesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportDeadTreesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportPumpNumber;
    [SerializeField] private TextMeshProUGUI lose_reportEnemiesNumber;
    [SerializeField] private TextMeshProUGUI lose_reportSubsidenceScore;
}
```

### 13.2. Hệ thống tính điểm

```csharp
public void computeScore()
{
    SubsidenceScore = subsidenceManager.SubsidenceScore;
    LiveTreeNumber = playerResourcesManager.CurrentRefillSources;
    DeadTreeNumber = playerResourcesManager.TotalTree - playerResourcesManager.CurrentRefillSources;
    NumberPumper = StatisticsManager.Instance.WaterPumpCount;
    TotalNeutralWater = StatisticsManager.Instance.EnemyKillCount;
    TotalMiningWater = 100 - subsidenceManager.RemainingWaterLevelLocal;

    // Công thức điểm tổng hợp
    ScoreGame = (
        (1 - (SubsidenceScore / 10)) +           // Sụt lún (thấp = tốt)
        ((LiveTreeNumber+1) / (TotalTree+1)) +    // Tỷ lệ cây sống
        (1 - ((NumberPumper+1) / 10)) +           // Giếng bơm (ít = tốt)
        (((TotalNeutralWater+1)/200+1) / (TotalMiningWater+1))  // Nước
    ) * 100;
    
    ScoreGame = Mathf.Round(ScoreGame);
}
```

### 13.3. Điều kiện Win/Lose

```csharp
if (LiveTreeNumber > 0 && SubsidenceScore < 1.25f)
    finalContent_Win.SetActive(true);    // THẮNG: Còn cây sống VÀ sụt lún thấp
else
    finalContent_Lose.SetActive(true);   // THUA: Hết cây HOẶC sụt lún nghiêm trọng
```

### 13.4. Billboard UI cho VR

```csharp
void Start()
{
    // Spawn UI phía trước người dùng trong VR
    transform.position = head.position 
        + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;
    startContent.SetActive(true);
    finalContent.SetActive(false);
}
```

---

## 14. BuildUI — Giao diện xây dựng

### 14.1. Cấu trúc

```csharp
public class BuildUI : MonoBehaviour
{
    [SerializeField] private BuildSystemManager buildManager;
    [SerializeField] private GameObject content;
    [SerializeField] private TextMeshProUGUI currentBuildInfo;
    [SerializeField] private List<TextMeshProUGUI> currentQuantities;
    [SerializeField] private List<Image> imageCooldownList;
    [SerializeField] private GameObject removeConstructionRay;
}
```

### 14.2. Real-time Update

```csharp
private void Update()
{
    int count = Mathf.Min(currentQuantities.Count, buildManager.Constructions.Count);
    for(int i = 0; i < count; i++)
    {
        // Cập nhật số lượng khả dụng
        currentQuantities[i].text = buildManager.Constructions[i].CurrentQuantity.ToString();
        
        // Cập nhật thanh cooldown
        if(imageCooldownList[i].fillAmount != 0)
            imageCooldownList[i].fillAmount -= 1.0f / buildManager.Constructions[i].cooldownTime * Time.deltaTime;
    }
}
```

**Các loại công trình:**
- Hồ nước (Lake)
- Máy bơm nước (Water Pump)
- Cống nước (Sluice Gate)

---

## 15. Hệ thống VU1 (Legacy) và quá trình tiến hóa

### 15.1. Kiến trúc VU1

```csharp
// VU1: Dùng Newtonsoft.Json + StreamingAssets
public class LocalizationManager_old : MonoBehaviour
{
    private Dictionary<string, string> localizedText;
    private Dictionary<string, Dictionary<string, string>> allLanguages;

    void LoadLocalizedText()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "languages.json");
        string json = File.ReadAllText(filePath);
        allLanguages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
    }
}

// VU1: Component text
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText_old : MonoBehaviour
{
    public string key;
    public void UpdateText()
    {
        textComponent.text = LocalizationManager_old.Instance.GetText(key);
    }
}

// VU1: Dropdown controller
public class LanguageDropdown : MonoBehaviour
{
    void OnLanguageChanged(int index)
    {
        string langCode = (index == 0) ? "en" : "vi";
        LocalizationManager_old.Instance.SetLanguage(langCode);
        foreach (var t in FindObjectsOfType<LocalizedText_old>())
            t.UpdateText();  // Brute-force update tất cả
    }
}
```

### 15.2. So sánh tiến hóa VU1 → VU2

| Tiêu chí | VU1 (Legacy) | VU2 (Hiện tại) |
|----------|-------------|-----------------|
| **Nguồn dữ liệu** | `StreamingAssets/languages.json` | `Resources/data.json` + `Resources/Localization/CSV` |
| **Parser** | `Newtonsoft.Json` (external dependency) | `JsonUtility` (built-in) + Custom CSV parser |
| **Ngôn ngữ** | 2 (en, vi) | 4 (vi, en, fr, th) |
| **Cập nhật** | `FindObjectsOfType<>()` brute-force | Event delegate O(1) broadcast |
| **Text component** | `TextMeshProUGUI` only | `UnityEngine.UI.Text` + `TextMeshProUGUI` dual |
| **Cấu trúc dữ liệu** | Flat key-value | Hierarchical (Root → Lang → Labels/Plants/...) |
| **Fallback** | Trả `#key` khi thiếu | Chain: vi → en → fr → th |
| **VR support** | Chưa rõ | UIStructureFixer, BaseUI billboard, CanvasGroup |

### 15.3. Các cải tiến chính

1. **Loại bỏ Newtonsoft.Json:** Chuyển sang `JsonUtility` built-in — giảm dependency, tăng tốc deserialization
2. **Event-driven pattern:** Từ `FindObjectsOfType` (O(n) mỗi component) sang delegate event (O(1) invoke)
3. **Mở rộng ngôn ngữ:** Từ 2 → 4 ngôn ngữ với fallback chain
4. **Dữ liệu phức tạp:** Từ flat string → hierarchical object với plant/animal/fish/NPC dialogue
5. **VR Adaptation:** Thêm UIStructureFixer, billboard UI, CanvasGroup ray management

---

## 16. Phân tích kiến trúc tổng thể

### 16.1. Sơ đồ component

```
┌────────────────────────────────────────────────────────────────────┐
│                        RUNTIME DATA FLOW                          │
│                                                                    │
│  ┌─────────────────┐    ┌────────────────────┐                    │
│  │ LocalizationData │    │    data.json        │                    │
│  │     .csv         │    │ (vi/en/fr/th)       │                    │
│  └────────┬────────┘    └────────┬───────────┘                    │
│           │ Resources.Load       │ Resources.Load                  │
│           ▼                      ▼                                 │
│  ┌─────────────────┐    ┌─────────────────────┐                   │
│  │ Localization     │    │ Thuan_23127_        │                   │
│  │ Manager          │    │ JsonReader           │                   │
│  │ (CSV Singleton)  │    │ (JSON MonoBehaviour) │                   │
│  │                  │    │                      │                   │
│  │ OnLanguageChanged│    │ GetCurrentLangData() │                   │
│  │ (event)          │    │ GetPlantById()       │                   │
│  │                  │    │ ApplyLanguage()      │                   │
│  └──────┬───────────┘    └──────┬──────────────┘                   │
│         │                       │                                   │
│         ▼                       ▼                                   │
│  ┌──────────────┐    ┌─────────────────────────────────────────┐   │
│  │ LocalizedKey  │    │ UI Components                           │   │
│  │ (N instances) │    │                                         │   │
│  │ auto-update   │    │ AreaHUD ──── SetSalinity(localized)     │   │
│  └──────────────┘    │ PlantHover ─ tooltip(fields, units)      │   │
│                      │ NPC ──────── dialogues[currentLang]      │   │
│                      │ GameUI ───── score(labels.score)         │   │
│                      │ TidalClock ─ (hardcoded vi) ⚠            │   │
│                      └─────────────────────────────────────────┘   │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ VR LAYER                                                     │   │
│  │                                                               │   │
│  │ UIStructureFixer ── Camera stack fix at Awake()               │   │
│  │ BaseUI ──────────── Billboard (LookAt head, Y-axis lock)     │   │
│  │ GameUI ──────────── Spawn at head.forward * distance          │   │
│  │ CanvasGroup ─────── blocksRaycasts = false (popups/tooltips) │   │
│  └─────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────┘
```

### 16.2. Điểm mạnh

| # | Điểm mạnh | Chi tiết |
|---|-----------|----------|
| 1 | **Tách biệt data/logic/view** | JSON chứa data, JsonReader chứa logic, UI components chứa view |
| 2 | **Fallback chain robust** | 4 cấp fallback (vi→en→fr→th) đảm bảo không crash |
| 3 | **Event-driven updates** | CSV system dùng delegate → clean, decoupled |
| 4 | **Null-safe operators** | Sử dụng `?.` và `??` xuyên suốt — an toàn runtime |
| 5 | **XR ray management** | CanvasGroup.blocksRaycasts, Graphic.raycastTarget, UIStructureFixer |
| 6 | **Dual text support** | Hỗ trợ cả UnityEngine.UI.Text và TextMeshProUGUI |
| 7 | **Template-based descriptions** | `Templates.healthy_desc` / `diseased_desc` cho localized dynamic text |
| 8 | **Billboard pattern** | UI tự quay về phía người dùng trong VR |

### 16.3. Điểm cần cải thiện

| # | Vấn đề | Nghiêm trọng | Chi tiết |
|---|--------|:------------:|----------|
| 1 | **Hai hệ thống song song** | Trung bình | CSV và JSON chưa thống nhất — dễ gây confused |
| 2 | **TidalClockUI hardcoded vi** | Cao | Chưa tích hợp đa ngôn ngữ |
| 3 | **PlantHoverHandler hardcoded vi/en** | Trung bình | `LocalizeGroupLabelHardcoded()` chỉ hỗ trợ 2 ngôn ngữ |
| 4 | **CSV chỉ có 1 entry** | Thấp | Hệ thống sẵn sàng nhưng nội dung chưa điền |
| 5 | **NPC reload JSON mỗi lần Talk()** | Trung bình | Nên cache `Root` thay vì parse lại |
| 6 | **GameUI report text hardcoded** | Trung bình | Các nhãn report (Living Trees, Dead Trees...) chưa localized |
| 7 | **Không có language persistence** | Thấp | Chưa lưu ngôn ngữ đã chọn vào PlayerPrefs |

---

## 17. Sơ đồ luồng dữ liệu

### 17.1. Luồng chuyển đổi ngôn ngữ (JSON System)

```
User chọn ngôn ngữ "en" từ Dropdown
    │
    ▼ (Dropdown.onValueChanged → index=1)
Thuan_23127_JsonReader.SetLanguageByIndex(1)
    │
    ├── currentLang = "en"
    │
    ▼
ApplyLanguage()
    │
    ├── GetCurrentLangData() → root.en
    │   │
    │   ├── labels: { info: "INFO", name: "Name", salinity: "Salinity", ... }
    │   ├── gameplay: { name: "SIMPLE VU2", level: 1, score: 0 }
    │   └── interpretation: { fields: { growth_time: "Growth time", ... }, ... }
    │
    ├── infoText.text = "INFO"
    ├── nameText.text = "Name: SIMPLE VU2"
    ├── levelText.text = "Level: 1"
    ├── settingText.text = "Settings"
    ├── playAgainText.text = "Play Again"
    ├── salinityText.text = "Salinity: 0.85"
    └── scoreText.text = "Score: 0"

    (Đồng thời, các component tham chiếu JsonReader cũng tự cập nhật)
    │
    ├── AreaHUD.SetSalinity() → đọc interpretation.fields.salinity → "Salinity"
    ├── PlantHoverHandler.OnPointerEnter() → đọc fields + units → hiển thị en
    └── NPC.Talk() → đọc npcDialogues → hiển thị dialogue tiếng Anh
```

### 17.2. Luồng hiển thị tooltip đa ngôn ngữ

```
User hover XR ray lên cây trồng (ID=1)
    │
    ▼
PlantHoverHandler.OnPointerEnter()
    │
    ├── jsonReader.GetCurrentLangData() → lang (en)
    ├── fields = lang.interpretation.fields
    ├── units = lang.interpretation.units
    │
    ├── type = Auto → Thử cả 3:
    │   ├── GetPlantById(1) → Plant { tag_name: "Durian", growth_time: 0, ... }
    │   ├── GetLivestockById(1) → null
    │   └── GetFishById(1) → null
    │
    ├── AppendBlock("Durian", 0, 4, "Information about durian")
    │   → "- Growth time: 0 days"
    │   → "- Economic benefits: 4"
    │   → "- Information: Information about durian"
    │
    ▼
    scrollInfoPanel.SetActive(true)
    headText.text = "Durian"
    infoText.text = [formatted block]
```

---

## 18. Đánh giá và khuyến nghị

### 18.1. Đánh giá tổng thể

| Tiêu chí | Điểm (1-10) | Nhận xét |
|----------|:-----------:|----------|
| Mức độ hoàn thiện đa ngôn ngữ | 7/10 | 4 ngôn ngữ với data model tốt, nhưng còn hardcoded text |
| Kiến trúc kỹ thuật | 7/10 | Hai hệ thống song song hợp lý nhưng cần thống nhất |
| VR UI Adaptation | 8/10 | UIStructureFixer giải quyết tốt vấn đề XR ray |
| Trải nghiệm người dùng | 7/10 | Typewriter NPC, tooltip, HUD tốt; chưa persist language |
| Khả năng mở rộng | 8/10 | JSON structure dễ thêm ngôn ngữ mới; Data model clean |
| Hiệu năng | 7/10 | StringBuilder tốt; NPC reload JSON mỗi lần Talk() cần fix |

### 18.2. Khuyến nghị cải thiện

#### Ưu tiên cao
1. **Tích hợp đa ngôn ngữ cho TidalClockUI:** Di chuyển `_phaseNames`, `SPRING_TIDE_TEXT`, `NEAP_TIDE_TEXT` vào `data.json` và đọc qua `JsonReader`
2. **Cache JSON trong NPC:** Thay vì `Resources.Load<TextAsset>` + `JsonUtility.FromJson` mỗi lần `Talk()`, nên dùng `Root` đã parse sẵn từ `JsonReader`
3. **Localize GameUI reports:** Các nhãn "Living Trees", "Dead Trees", "Score" trong màn hình win/lose cần đa ngôn ngữ

#### Ưu tiên trung bình
4. **Mở rộng PlantHoverHandler localization:** Hàm `LocalizeGroupLabelHardcoded()` nên đọc từ `data.json` thay vì hardcode vi/en
5. **Lưu ngôn ngữ đã chọn:** Sử dụng `PlayerPrefs.SetString("Lang", currentLang)` để persist qua sessions
6. **Thống nhất hai hệ thống:** Xem xét merge CSV system vào JSON system hoặc ngược lại — giảm maintenance cost

#### Ưu tiên thấp
7. **Mở rộng CSV content:** Nếu giữ CSV system, cần thêm nội dung cho tất cả UI labels thực tế
8. **Thêm RTL support:** Nếu mở rộng sang Arabic/Hebrew trong tương lai
9. **Font management:** Đảm bảo font hỗ trợ Thai characters (ภาษาไทย) đầy đủ

### 18.3. Kết luận

Hệ thống đa ngôn ngữ và giao diện người dùng thích ứng của SIMPLE VU2 thể hiện một kiến trúc **thực dụng và đang phát triển**, phù hợp cho ứng dụng VR giáo dục xuyên quốc gia. Kiến trúc hai lớp (CSV + JSON) phản ánh sự phát triển theo từng đợt intern, với hệ thống JSON hiện đang là backbone chính xử lý đa ngôn ngữ cho toàn bộ nội dung game.

Điểm nổi bật nhất là:
- **UIStructureFixer** — giải pháp sáng tạo giải quyết xung đột URP Camera Stack với XR ray interaction
- **Mô hình dữ liệu phân cấp** — `Root → Lang → Labels/Plants/Animals/Fish/NPC` cho phép mở rộng ngôn ngữ chỉ bằng cách thêm node JSON mới
- **Fallback chain** — đảm bảo robustness khi thiếu dữ liệu ngôn ngữ
- **Event-driven localization** — kiến trúc observer pattern cho cập nhật UI clean và decoupled

Hệ thống sẵn sàng cho mở rộng thêm ngôn ngữ và nội dung mới, với điều kiện các vấn đề hardcoded text được giải quyết trong các phiên bản tiếp theo.

---

*Báo cáo được tạo từ phân tích mã nguồn dự án SIMPLE VU2 — Tháng 7/2025*
