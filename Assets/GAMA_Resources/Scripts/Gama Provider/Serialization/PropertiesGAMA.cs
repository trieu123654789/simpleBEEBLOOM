using System.Collections.Generic;
using UnityEngine;

// EN: Container for the full list of GAMA object property definitions.
//     Received via key "properties" in HandleServerMessageReceived.
// VI: Container cho toàn bộ danh sách định nghĩa thuộc tính đối tượng GAMA.
//     Nhận qua khóa "properties" trong HandleServerMessageReceived.
[System.Serializable]
public class AllProperties
{
   public List<PropertiesGAMA> properties;

    public static AllProperties CreateFromJSON(string jsonString)
    {
       return JsonUtility.FromJson<AllProperties>(jsonString);
    }
}

// EN: Property definition for a single GAMA geometry/agent type.
//     Controls how the corresponding Unity GameObject is instantiated:
//     prefab, collider, interaction, visual appearance, etc.
// VI: Định nghĩa thuộc tính cho một loại hình học/agent GAMA đơn lẻ.
//     Điều khiển cách GameObject Unity tương ứng được khởi tạo:
//     prefab, collider, tương tác, hiển thị, v.v.
[System.Serializable]
public class PropertiesGAMA
{
    // EN: Unique type ID matching propertyID in WorldJSONInfo.
    // VI: ID kiểu duy nhất khớp với propertyID trong WorldJSONInfo.
    public string id;
    // EN: Whether to add a Collider component.
    // VI: Có thêm component Collider không.
    public bool hasCollider;
    // EN: Unity tag to assign to the GameObject.
    // VI: Tag Unity gán cho GameObject.
    public string tag;
    // EN: Rigidbody axis constraints [posX, posY, posZ, rotX, rotY, rotZ].
    // VI: Ràng buộc trục Rigidbody [posX, posY, posZ, rotX, rotY, rotZ].
    public List<bool> constraints;
  
    // EN: Whether the object supports XR interaction (hover/select events).
    // VI: Đối tượng có hỗ trợ tương tác XR (sự kiện hover/select) không.
    public bool isInteractable;
    // EN: Whether the object can be grabbed (XRGrabInteractable vs XRSimpleInteractable).
    // VI: Đối tượng có thể nắm/nhặt được không (XRGrabInteractable vs XRSimpleInteractable).
    public bool isGrabable;

    // EN: Whether to instantiate from a Resources prefab (true) or generate polygon mesh (false).
    // VI: Khởi tạo từ prefab Resources (true) hay tạo mesh polygon (false).
    public bool hasPrefab;
    // EN: Resources path for the prefab (e.g. "Prefabs/Tree").
    // VI: Đường dẫn Resources cho prefab (vd: "Prefabs/Tree").
    public string prefab;

    // EN: Scale factor (precision-scaled integer) for prefab instantiation.
    // VI: Hệ số scale (số nguyên scale theo precision) cho khởi tạo prefab.
    public int size;
    // EN: Y-axis offset (precision-scaled) applied after placement.
    // VI: Offset trục Y (scale theo precision) áp dụng sau khi đặt.
    public int yOffset;
    // EN: Rotation multiplier (precision-scaled).
    // VI: Hệ số xoay (scale theo precision).
    public int rotationCoeff;
    // EN: Rotation offset (precision-scaled).
    // VI: Offset góc xoay (scale theo precision).
    public int rotationOffset;
    // EN: Whether the mesh renderers should be visible.
    // VI: Các mesh renderer có hiển thị không.
    public bool visible = true;

    // EN: Float versions of the above (computed in loadPrefab after dividing by precision).
    // VI: Phiên bản float của các giá trị trên (tính trong loadPrefab sau khi chia cho precision).
    public float yOffsetF;
    public float rotationCoeffF;
    public float rotationOffsetF;

    // EN: Extrusion height for 3D polygon generation (precision-scaled).
    // VI: Chiều cao đùn cho tạo polygon 3D (scale theo precision).
    public int height;
    // EN: Whether to generate 3D extruded geometry (true) or flat 2D (false).
    // VI: Tạo hình học đùn 3D (true) hay phẳng 2D (false).
    public bool is3D;

    // EN: Material resource path for polygon rendering.
    // VI: Đường dẫn tài nguyên material cho render polygon.
    public string material;

    // EN: RGBA color components for polygon coloring.
    // VI: Thành phần màu RGBA cho tô màu polygon.
    public int red;
    public int green;
    public int blue;
    public int alpha;

    // EN: Whether this object’s position should be tracked and sent back to GAMA.
    // VI: Vị trí đối tượng này có được theo dõi và gửi lại GAMA không.
    public bool toFollow;
    // EN: Cached prefab instance loaded from Resources.
    // VI: Instance prefab đã cache từ Resources.
    public GameObject prefabObj = null;

    // EN: Load the prefab from Resources and compute float-precision offsets.
    // VI: Tải prefab từ Resources và tính toán các offset dạng float.
    public void loadPrefab(int precision)
    {
        if (prefab != null && !prefab.Equals(""))
        {
            prefabObj = Resources.Load(prefab) as GameObject;
            yOffsetF = (0.0f + yOffset)/precision  ;
            rotationCoeffF = (0.0f + rotationCoeff) / precision;
            rotationOffsetF = (0.0f + rotationOffset) / precision;

        }
    }

    

}
