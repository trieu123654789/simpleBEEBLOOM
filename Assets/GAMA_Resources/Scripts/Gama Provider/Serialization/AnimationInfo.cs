using System.Collections.Generic;
using UnityEngine;

// EN: Animation command from GAMA. Specifies which named objects to animate,
//     which Animator parameters to set, and which triggers to fire.
//     Received via key "triggers" in HandleServerMessageReceived.
// VI: Lệnh animation từ GAMA. Chỉ định đối tượng nào cần animate,
//     tham số Animator nào cần set, và trigger nào cần kích hoạt.
//     Nhận qua khóa "triggers" trong HandleServerMessageReceived.
[System.Serializable]
public class AnimationInfo
{
    // EN: List of geometry names (keys in SimulationManager.geometryMap) to animate.
    // VI: Danh sách tên geometry (khóa trong SimulationManager.geometryMap) cần animate.
    public List<string> names;

    // EN: Animator trigger names to fire on the target objects.
    // VI: Tên các trigger Animator cần kích hoạt trên đối tượng đích.
    public List<string> triggers;
    // EN: Animator parameter values (int/float/bool) to set before firing triggers.
    // VI: Giá trị tham số Animator (int/float/bool) cần set trước khi kích trigger.
    public List<ParameterVal> parameters;

    public static AnimationInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<AnimationInfo>(jsonString);
    } 

}


// EN: A single Animator parameter with its typed value.
//     GAMA specifies the type as a string ("int"/"float"/"bool")
//     and provides the corresponding value field.
// VI: Một tham số Animator đơn lẻ với giá trị theo kiểu.
//     GAMA chỉ định kiểu bằng chuỗi ("int"/"float"/"bool")
//     và cung cấp trường giá trị tương ứng.
[System.Serializable]
public class ParameterVal
{
    // EN: Parameter name matching the Animator Controller parameter.
    // VI: Tên tham số khớp với tham số trong Animator Controller.
    public string key;
    public float floatVal;
    public int intVal;
    public bool boolVal;
    // EN: Type discriminator: "int", "float", or "bool".
    // VI: Kiểu phân biệt: "int", "float", hoặc "bool".
    public string type;

    public object getVal()
    {
        if ("int".Equals(type))
            return intVal;
        else if ("float".Equals(type))
            return intVal;
        return boolVal;
    }
        
}


