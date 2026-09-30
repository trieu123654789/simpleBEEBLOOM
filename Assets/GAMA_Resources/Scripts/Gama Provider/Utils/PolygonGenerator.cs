using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// EN: Singleton utility that converts GAMA polygon vertex data (integer coordinates)
//     into extruded 3D meshes in Unity. Used by SimulationManager to create
//     teleportation areas, walls, and other GAMA-defined geometries at runtime.
//     Delegates actual mesh extrusion to the PolyExtruder component.
// VI: Tiện ích singleton chuyển đổi dữ liệu đỉnh polygon GAMA (tọa độ số nguyên)
//     thành mesh 3D đùn trong Unity. Được SimulationManager dùng để tạo
//     vùng teleport, tường và các hình học GAMA định nghĩa khác lúc runtime.
//     Ủy thác việc đùn mesh thực tế cho component PolyExtruder.
public class PolygonGenerator
{
    // EN: Reference to the coordinate converter for GAMA→Unity transformations.
    // VI: Tham chiếu đến bộ chuyển đổi tọa độ GAMA→Unity.
    CoordinateConverter converter;

    // EN: Y offset applied to all generated background geometries.
    // VI: Offset Y áp dụng cho tất cả hình học nền được tạo.
    float offsetYBackgroundGeom;

    // EN: Singleton instance.
    // VI: Instance singleton.
    private static PolygonGenerator instance;

    // EN: Cached meshes from the last GeneratePolygons call (side/bottom/top).
    //     Used by callers to attach MeshColliders to specific faces.
    // VI: Các mesh được cache từ lần gọi GeneratePolygons cuối cùng (bên/đáy/nóc).
    //     Người gọi dùng để gắn MeshCollider vào các mặt cụ thể.
    public Mesh surroundMesh;
    public Mesh bottomMesh;
    public Mesh topMesh;



    public PolygonGenerator() { }

    public void Init(CoordinateConverter c)
    {
        converter = c;
    }

    public static PolygonGenerator GetInstance()
    {
        if (instance == null)
        {
            instance = new PolygonGenerator();
        }
        return instance;
    }

    public static void DestroyInstance()
    {
        instance = null;
    }



    // EN: Main entry point: convert a flat list of GAMA integer coordinates
    //     into a textured/colored 3D extruded polygon GameObject.
    //     Steps: decode pairs → convert to Unity 2D → determine color/material → extrude.
    // VI: Điểm vào chính: chuyển danh sách tọa độ số nguyên GAMA phẳng
    //     thành GameObject polygon 3D đùn có texture/màu.
    //     Các bước: giải mã cặp → chuyển sang Unity 2D → xác định màu/material → đùn.
    public GameObject GeneratePolygons(bool editMode, String name, List<int> points, PropertiesGAMA prop, int precision)
    {
   
        List<Vector2> pts = new List<Vector2>();
        for (int i = 0; i < points.Count - 1; i = i+2)
        {
            Vector2 p = converter.fromGAMACRS2D(points[i], points[i + 1]);
            pts.Add(p);
        }
        Vector2[] MeshDataPoints = pts.ToArray();
        //Color32 col = new Color32(BitConverter.GetBytes(prop.color[0])[0], BitConverter.GetBytes(prop.color[1])[0],
        //          BitConverter.GetBytes(prop.color[2])[0], BitConverter.GetBytes(prop.color[3])[0]);

       Color32 col = Color.black;
       Material mat = null;
        if (prop.visible)
        {
            if (prop.material != null && prop.material != "")
            {
                mat = Resources.Load<Material>(prop.material);
            }
            col = new Color32(BitConverter.GetBytes(prop.red)[0], BitConverter.GetBytes(prop.green)[0],
                    BitConverter.GetBytes(prop.blue)[0], BitConverter.GetBytes(prop.alpha)[0]);
        }
        GameObject obj = GeneratePolygon(editMode, name, MeshDataPoints, ((float)prop.height) / precision, mat, col);
        
        if (!prop.visible)
        {
            MeshRenderer r =  obj.GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false;
            foreach (MeshRenderer rr in obj.GetComponentsInChildren<MeshRenderer>())
            {
                if (rr != null) rr.enabled = false;

            }
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            if (lr != null)
                lr.enabled = false;
        }
        return obj;

    }


    // EN: Internal method: create a PolyExtruder component and generate
    //     the actual 3D mesh from 2D polygon points.
    // VI: Phương thức nội bộ: tạo component PolyExtruder và sinh
    //     mesh 3D thực tế từ các điểm polygon 2D.
    GameObject GeneratePolygon(bool editMode, String name, Vector2[] MeshDataPoints, float extrusionHeight, Material mat, Color32 color)
    {
        bool isUsingBottomMeshIn3D = false;
        bool isOutlineRendered = true;
        bool is3D = extrusionHeight != 0.0;

       
        // create new GameObject (as a child)
        GameObject polyExtruderGO = new GameObject();
       

        // reference to setup example poly extruder 
        PolyExtruder polyExtruder;

        
        // add PolyExtruder script to newly created GameObject and keep track of its reference
        polyExtruder = polyExtruderGO.AddComponent<PolyExtruder>();
       
        // global PolyExtruder configurations
        polyExtruder.isOutlineRendered = isOutlineRendered;
        Vector3 pos = polyExtruderGO.transform.position;
        pos.y += offsetYBackgroundGeom;
        polyExtruderGO.transform.position = pos;
        polyExtruder.createPrism(editMode, name, extrusionHeight, MeshDataPoints, color, mat, is3D, isUsingBottomMeshIn3D);
        surroundMesh = polyExtruder.surroundMesh;
        bottomMesh = polyExtruder.bottomMesh;
        topMesh = polyExtruder.topMesh;
        polyExtruderGO.name = name;
        return polyExtruderGO;
    }

  
}


