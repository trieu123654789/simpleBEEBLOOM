// using UnityEngine;

// public class PlantUIManager : MonoBehaviour
// {
//     public static PlantUIManager Instance;

//     [Header("GroupUI")]
//     public GameObject[] PlantPanels;
//     public GameObject[] FishPanels;
//     public GameObject[] AnimalPanels;

//     private GameObject currentPanel;

//     void Awake()
//     {
//         Instance = this;
//         HideAllUI();
//     }

//     public void ShowGroup(PlotType type, int index)
//     {
//         // Nếu panel đang mở là đúng nhóm + đúng index -> tắt đi

//         if (!RulesoftheGame_VU2_1.GameActive) return;
//         if (currentPanel != null && currentPanel.activeSelf)
//         {
//             switch (type)
//             {
//                 case PlotType.Plant:
//                     if (index >= 0 && index < PlantPanels.Length && currentPanel == PlantPanels[index])
//                     {
//                         HideAllUI();
//                         return;
//                     }
//                     break;
//                 case PlotType.Fish:
//                     if (index >= 0 && index < FishPanels.Length && currentPanel == FishPanels[index])
//                     {
//                         HideAllUI();
//                         return;
//                     }
//                     break;
//                 case PlotType.Animal:
//                     if (index >= 0 && index < AnimalPanels.Length && currentPanel == AnimalPanels[index])
//                     {
//                         HideAllUI();
//                         return;
//                     }
//                     break;
//             }
//         }

//         // Ẩn hết trước
//         HideAllUI();

//         // Mở panel mới
//         GameObject[] panels = null;
//         switch (type)
//         {
//             case PlotType.Plant: panels = PlantPanels; break;
//             case PlotType.Fish: panels = FishPanels; break;
//             case PlotType.Animal: panels = AnimalPanels; break;
//         }

//         if (panels != null && index >= 0 && index < panels.Length)
//         {
//             GameObject panel = panels[index];
//             panel.SetActive(true);
//             currentPanel = panel;

//             // --- Đặt UI trước mặt player ---
//             Transform cam = Camera.main.transform;
//             float distance = 2f; // khoảng cách trước mặt
//             Vector3 targetPos = cam.position + cam.forward * distance;
//             targetPos.y = cam.position.y; // ngang tầm mắt

//             panel.transform.position = targetPos;
//             panel.transform.LookAt(cam);
//             panel.transform.Rotate(0, 180f, 0); // lật lại vì canvas mặc định ngược
//         }
//     }

//     public void HideAllUI()
//     {
//         // Hide all UI panels
//         foreach (var p in PlantPanels)
//         {
//             if (p != null)
//             {
//                 // Notify hover handlers before hiding
//                 var hoverHandlers = p.GetComponentsInChildren<Thuan_23127_PlantHoverHandler>(true);
//                 foreach (var handler in hoverHandlers)
//                 {
//                     handler.ForceHideTooltip();
//                 }
//                 p.SetActive(false);
//             }
//         }
//         foreach (var p in FishPanels)
//         {
//             if (p != null)
//             {
//                 var hoverHandlers = p.GetComponentsInChildren<Thuan_23127_PlantHoverHandler>(true);
//                 foreach (var handler in hoverHandlers)
//                 {
//                     handler.ForceHideTooltip();
//                 }
//                 p.SetActive(false);
//             }
//         }
//         foreach (var p in AnimalPanels)
//         {
//             if (p != null)
//             {
//                 var hoverHandlers = p.GetComponentsInChildren<Thuan_23127_PlantHoverHandler>(true);
//                 foreach (var handler in hoverHandlers)
//                 {
//                     handler.ForceHideTooltip();
//                 }
//                 p.SetActive(false);
//             }
//         }
//         currentPanel = null;
//     }
// }