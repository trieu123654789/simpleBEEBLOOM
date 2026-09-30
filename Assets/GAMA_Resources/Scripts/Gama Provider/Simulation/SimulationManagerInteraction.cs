using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.InputSystem;

// EN: Template / skeleton subclass of SimulationManager for custom XR interaction
//     scenarios. Override the virtual methods below to define game-specific behavior.
//     This class ships with empty overrides as a starting point for new interaction modes.
// VI: Lớp con mẫu / khung xương của SimulationManager cho các kịch bản tương tác XR
//     tùy chỉnh. Override các phương thức ảo bên dưới để định nghĩa hành vi cụ thể cho game.
//     Lớp này cung cấp các override rỗng làm điểm khởi đầu cho chế độ tương tác mới.
public class SimulationManagerInteraction : SimulationManager
{

    // EN: Called when the XR ray enters an interactable object.
    // VI: Được gọi khi tia XR đi vào đối tượng tương tác.
    protected override void HoverEnterInteraction(HoverEnterEventArgs ev)
    {
         GameObject obj = ev.interactableObject.transform.gameObject;
    }


    // EN: Called when the XR ray leaves an interactable object.
    // VI: Được gọi khi tia XR rời khỏi đối tượng tương tác.
    protected override void HoverExitInteraction(HoverExitEventArgs ev)
    {
        GameObject obj = ev.interactableObject.transform.gameObject;
    }



    // EN: Called when the main button (right controller) is triggered.
    // VI: Được gọi khi nút chính (controller phải) được nhấn.
    protected override void TriggerMainButton()
    {
       
    }

    // EN: Called when the inbound message key doesn’t match any known case.
    //     Use this for game-specific custom GAMA messages.
    // VI: Được gọi khi khóa message đến không khớp case nào đã biết.
    //     Dùng để xử lý message GAMA tùy chỉnh cho game cụ thể.
    protected override void ManageOtherMessages(string content)
    {

    }

  

    // EN: Called every frame after the base Update logic.
    //     Use for game-specific per-frame logic.
    // VI: Được gọi mỗi frame sau logic Update cơ sở.
    //     Dùng cho logic theo frame riêng của game.
    protected override void OtherUpdate()
    {

    }

   
}