using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// EN: Serializable representation of a Unity GameObject’s mesh geometry,
//     converted to GAMA coordinate system. Used to EXPORT geometry FROM Unity
//     TO GAMA (the reverse of WorldJSONInfo which imports FROM GAMA).
//     Recursively traverses child objects, extracting all triangle vertices.
// VI: Biểu diễn serializable của hình học mesh của GameObject Unity,
//     chuyển đổi sang hệ tọa độ GAMA. Dùng để XUẤT hình học TỪ Unity
//     SANG GAMA (ngược lại với WorldJSONInfo là nhập TỪ GAMA).
//     Duyệt đệ quy các đối tượng con, trích xuất toàn bộ đỉnh tam giác.
[System.Serializable]
public class UnityGeometry 
{
    // EN: All triangle vertices converted to GAMA CRS.
    // VI: Toàn bộ đỉnh tam giác đã chuyển sang CRS GAMA.
    public List<UnityPoint> points;
    // EN: Bounding-box height of each sub-mesh.
    // VI: Chiều cao bounding-box của mỗi sub-mesh.
    public List<int> heights;
    // EN: Object name for each triangle (tracks which mesh the vertex belongs to).
    // VI: Tên đối tượng cho mỗi tam giác (theo dõi đỉnh thuộc mesh nào).
    public List<string> names;
     
    public UnityGeometry(GameObject obj, CoordinateConverter converter)
    {
         
        points = new List<UnityPoint>();
        heights = new List<int>();
        names = new List<string>();
        addObject(obj, converter);
    }

   private void addObject(GameObject obj, CoordinateConverter converter)
    {
        MeshFilter mf = null;
        float yV = obj.transform.localScale.y;
        Vector3 v = obj.transform.localScale;
        v.y = 0.0f;
        obj.transform.localScale = v;
        Mesh mesh = obj.GetComponent<Mesh>();
       
         if (mesh == null)
         {
             mf = obj.GetComponent<MeshFilter>();
             if (mf != null)
             {
                 mesh = mf.sharedMesh;
             }

         }
        
        if (mesh != null)
        {
            

           
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                for (int i = 0; i < mesh.GetTriangles(index).Length; i++)
                {
                    Debug.Log("Triangles: " + i);
                    names.Add(obj.name);
                    heights.Add((int)mesh.bounds.size.y);

                    Vector3 wv = mesh.vertices[mesh.GetTriangles(index)[i]];
                    if (mf != null)
                        wv = mf.transform.TransformPoint(wv);


                    UnityPoint pt = new UnityPoint(wv, converter);
                    points.Add(pt);

                }
            }
            
          
            points.Add(new UnityPoint());
        }
        v.y = yV;
        obj.transform.localScale = v;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            addObject(obj.transform.GetChild(i).gameObject, converter);
            
        }
       

    }


    public string ToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}

// EN: A single point in GAMA CRS, constructed from a Unity Vector3.
//     The empty constructor creates a sentinel/separator point.
// VI: Một điểm đơn trong CRS GAMA, được tạo từ Vector3 Unity.
//     Constructor rỗng tạo điểm sentinel/phân cách.
[System.Serializable]
public class UnityPoint
{
    // EN: Coordinate values in GAMA CRS [x, y] or [x, y, z].
    // VI: Giá trị tọa độ trong CRS GAMA [x, y] hoặc [x, y, z].
    public List<int> c;

    public UnityPoint()
    {
        c = new List<int>();
    }
    public UnityPoint(Vector3 vect, CoordinateConverter converter)
    {
       c = converter.toGAMACRS(vect);
    }   
}
