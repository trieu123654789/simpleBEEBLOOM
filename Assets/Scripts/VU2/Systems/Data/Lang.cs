using System.Collections.Generic;

// =============================================================================
// Lang - Data container for a single language's content.
// Lang - Container dữ liệu cho nội dung của một ngôn ngữ.
// 
// Each Lang object contains all localized data for one language:
// - UI labels (buttons, headers, etc.)
// - Gameplay info (player name, level)
// - Plant/animal/fish data with localized names and descriptions
// - Interpretation data for health status messages
// 
// Mỗi object Lang chứa tất cả dữ liệu đã bản địa hóa cho một ngôn ngữ:
// - Nhãn UI (nút, tiêu đề, v.v.)
// - Thông tin gameplay (tên người chơi, cấp độ)
// - Dữ liệu cây/động vật/cá với tên và mô tả đã bản địa hóa
// - Dữ liệu interpretation cho thông báo trạng thái sức khỏe
// =============================================================================
[System.Serializable]
public class Lang
{
    // UI text labels (score, name, settings, etc.).
    // Các nhãn text UI (điểm, tên, cài đặt, v.v.).
    public Labels labels;
    
    // Gameplay configuration (player info).
    // Cấu hình gameplay (thông tin người chơi).
    public Gameplay gameplay;
    
    // Interpretation templates for health status messages.
    // Các template interpretation cho thông báo trạng thái sức khỏe.
    public InterpretationData interpretation;
    
    // List of plant data with localized names/descriptions.
    // Danh sách dữ liệu cây với tên/mô tả đã bản địa hóa.
    public List<Plant> plants;
    
    // List of livestock data with localized names/descriptions.
    // Danh sách dữ liệu vật nuôi với tên/mô tả đã bản địa hóa.
    public List<Animal> livestock;
    
    // List of fish/aquatic data with localized names/descriptions.
    // Danh sách dữ liệu cá/thủy sản với tên/mô tả đã bản địa hóa.
    public List<Fish> fish;  
    
    // NPC dialogue data for in-game conversations.
    // Dữ liệu hội thoại NPC cho các cuộc trò chuyện trong game.
    public List<NPCDialogue> npcDialogues;
}