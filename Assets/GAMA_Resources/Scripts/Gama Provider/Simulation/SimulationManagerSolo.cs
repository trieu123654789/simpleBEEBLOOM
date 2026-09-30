using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.InputSystem;

// EN: Single-player specialization of SimulationManager.
//     Adds day/night toggle, hotspot highlighting, and hover color feedback.
//     Used when the game runs with one player directly connected to GAMA.
// VI: Bản chuyên biệt hóa một người chơi của SimulationManager.
//     Thêm chuyển đổi ngày/đêm, làm nổi bật hotspot và phản hồi màu khi hover.
//     Dùng khi game chạy với một người chơi kết nối trực tiếp với GAMA.
public class SimulationManagerSolo : SimulationManager
{
    // EN: Current lighting state — toggled by the main controller button.
    // VI: Trạng thái ánh sáng hiện tại — chuyển đổi bằng nút chính của controller.
    protected bool isNight = false;

    

    // EN: Toggle all scene lights on/off (day/night cycle) on main button press.
    // VI: Bật/tắt toàn bộ đèn trong scene (chu kỳ ngày/đêm) khi nhấn nút chính.
    protected override void TriggerMainButton()
    {
        isNight = !isNight;
        Light[] lights = FindObjectsOfType(typeof(Light)) as Light[];
        foreach (Light light in lights)
        {
            light.intensity = isNight ? 0 : 1.0f;
        }
    }

    // EN: After geometry is loaded, highlight objects whose names match the hotspot list.
    // VI: Sau khi geometry được tải, làm nổi bật các đối tượng có tên nằm trong danh sách hotspot.
    protected override void AdditionalInitAfterGeomLoading()
    {
        if (parameters.hotspots != null && parameters.hotspots.Count > 0)
        {
            GameObject[] blocks = GameObject.FindGameObjectsWithTag("selectable");

            foreach (GameObject gameObj in blocks)
            {
                if (parameters.hotspots.Contains(gameObj.name))
                {
                    SelectedObjects.Add(gameObj);
                    SimulationManagerSolo.ChangeColor(gameObj, Color.red);
                }
            }
        }
    }



  
    protected override void OtherUpdate()
    { 
        // if (message != null)
        // {
        //     // Debug.Log("received from GAMA: subside " + message.subside);
        //     if(message.subside){
        //             SubsidenceManager    subsidenceManager = FindObjectOfType<SubsidenceManager>();

        //         subsidenceManager.Flooded();
        //     }
        //     message = null;
        // }


    }

    // GAMAMessage message = null;
    protected override void ManageOtherMessages(string content)
    {
                // message = GAMAMessage.CreateFromJSON(content);

    }
    // EN: Change object color to blue when the XR ray hovers over selectable/car/moto.
    // VI: Đổi màu đối tượng sang xanh khi tia XR di qua selectable/car/moto.
    protected override void HoverEnterInteraction(HoverEnterEventArgs ev)
    {

        GameObject obj = ev.interactableObject.transform.gameObject;
        if (obj.tag.Equals("selectable") || obj.tag.Equals("car") || obj.tag.Equals("moto"))
            SimulationManagerSolo.ChangeColor(obj, Color.blue);
    }

    // EN: Restore color when the XR ray leaves: red if selected, gray/white otherwise.
    // VI: Khôi phục màu khi tia XR rời đi: đỏ nếu được chọn, xám/trắng nếu không.
    protected override void HoverExitInteraction(HoverExitEventArgs ev)
    {
        GameObject obj = ev.interactableObject.transform.gameObject;
        if (obj.tag.Equals("selectable"))
        {
            bool isSelected = SelectedObjects.Contains(obj);

            SimulationManagerSolo.ChangeColor(obj, isSelected ? Color.red : Color.gray);
        }
        else if (obj.tag.Equals("car") || obj.tag.Equals("moto"))
        {
            SimulationManagerSolo.ChangeColor(obj, Color.white);
        }


    }

   

}