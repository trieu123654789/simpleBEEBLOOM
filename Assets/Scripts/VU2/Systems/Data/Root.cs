// =============================================================================
// Root - Top-level JSON data structure containing all language data.
// Root - Cấu trúc dữ liệu JSON cấp cao nhất chứa tất cả dữ liệu ngôn ngữ.
// 
// This is the root object parsed from Resources/data.json.
// It contains separate Lang objects for each supported language.
// 
// Đây là object gốc được parse từ Resources/data.json.
// Nó chứa các object Lang riêng biệt cho mỗi ngôn ngữ được hỗ trợ.
// 
// Supported languages:
// - vi: Vietnamese (Tiếng Việt) - primary/default
// - en: English (Tiếng Anh)
// - fr: French (Tiếng Pháp)
// - th: Thai (Tiếng Thái)
// =============================================================================
[System.Serializable]
public class Root
{
    // Vietnamese language data (primary).
    // Dữ liệu tiếng Việt (chính).
    public Lang vi;
    
    // English language data.
    // Dữ liệu tiếng Anh.
    public Lang en; 
    
    // French language data.
    // Dữ liệu tiếng Pháp.
    public Lang fr; 
    
    // Thai language data.
    // Dữ liệu tiếng Thái.
    public Lang th;
}