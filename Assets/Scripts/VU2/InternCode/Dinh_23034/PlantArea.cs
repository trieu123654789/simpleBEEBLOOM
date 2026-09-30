// using UnityEngine;
// using UnityEngine.XR;
// using UnityEngine.UI;

// public enum PlotType { Plant, Fish, Animal }

// public class PlantArea : MonoBehaviour
// {
//     [Header("Setup")]
//     public Transform plantPoint;
//     public int panelIndex;
//     public PlotType plotType;
//     private GameObject currentPlant;

//     private bool playerInside;
//     private bool isUIOpen;
//     public GameObject button;
//     private bool primaryButtonPrevState;
//     public static PlantArea currentActivePlot;

//     private void Start()
//     {
//         if (button != null)
//             button.SetActive(false);
//         isUIOpen = false;
//         playerInside = false;
//         primaryButtonPrevState = false;
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             playerInside = true;
//             currentActivePlot = this;
//             Debug.Log("Player vào plot: " + gameObject.name + " -> Panel " + panelIndex);

//             if (button != null)
//             {
//                 button.SetActive(true);
//                 Debug.Log("display button");
//             }
//         }
//     }

//     void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             playerInside = false;
//             if (currentActivePlot == this) currentActivePlot = null;
//             HideUI();
//             Debug.Log("Player rời plot: " + gameObject.name);

//             if (button != null)
//                 button.SetActive(false);
//         }
//     }

//     // Gọi từ nút UI (Ray bấm)
//     public void OnClickShowUI()
//     {
//         ToggleUI();
//     }

//     void Update()
//     {
//         if (!playerInside) return;

//         // Test bằng bàn phím
//         if (Input.GetKeyDown(KeyCode.X))
//         {
//             ToggleUI();
//         }

//         // VR controller
//         InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
//         if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonPressed))
//         {
//             if (primaryButtonPressed && !primaryButtonPrevState)
//             {
//                 ToggleUI();
//             }
//             primaryButtonPrevState = primaryButtonPressed;
//         }
//     }

//     private void ToggleUI()
//     {
//         if (!isUIOpen)
//         {
//             ShowUI();
//         }
//         else
//         {
//             HideUI();
//         }
//         isUIOpen = !isUIOpen;
//     }

//     private void ShowUI()
//     {
//         PlantUIManager.Instance.ShowGroup(plotType, panelIndex);
//         Debug.Log("Show UI cho plot: " + gameObject.name + " -> Panel " + panelIndex);
//     }

//     private void HideUI()
//     {
//         PlantUIManager.Instance.HideAllUI();
//         Debug.Log("Hide UI cho plot: " + gameObject.name);
//     }

//     // Gọi khi chọn cây
//     public void Plant(GameObject plantPrefab)
//     {
//         if (currentPlant != null) Destroy(currentPlant);
//         currentPlant = Instantiate(plantPrefab, plantPoint.position, plantPoint.rotation, plantPoint);
//         Debug.Log("Đã trồng " + plantPrefab.name + " vào plot " + gameObject.name);
//     }
// }
