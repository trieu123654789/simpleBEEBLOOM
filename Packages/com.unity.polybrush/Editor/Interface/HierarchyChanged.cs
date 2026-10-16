
using UnityEngine;
using UnityEngine.Polybrush;

namespace UnityEditor.Polybrush
{
    [InitializeOnLoad]
    static class HierarchyChanged
    {
        static HierarchyChanged()
        {
            EditorApplication.hierarchyChanged += () =>
            {
                try
                {
                    foreach (var gameObject in Selection.gameObjects)
                    {
                        var mesh = Util.GetMesh(gameObject);
                        var id = EditableObject.GetMeshId(mesh);

                        // if the mesh is an instance managed by polybrush check that it's not a duplicate.
                        if (id != -1)
                        {
                            if (id != EditableObject.GetObjectInstanceId(gameObject) && EditableObject.InstanceIdToObject(id) != null)
                            {
                                mesh = PolyMeshUtility.DeepCopy(mesh);
                                mesh.name = EditableObject.k_MeshInstancePrefix + EditableObject.GetObjectInstanceId(gameObject);

                                var mf = gameObject.GetComponent<MeshFilter>();
                                var sf = gameObject.GetComponent<SkinnedMeshRenderer>();
                                var polyMesh = gameObject.GetComponent<PolybrushMesh>();

                                if (polyMesh != null)
                                {
                                    polyMesh.SetMesh(mesh);
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(polyMesh);
                                }
                                else if (mf != null)
                                    mf.sharedMesh = mesh;
                                else if (sf != null)
                                    sf.sharedMesh = mesh;
                            }
                        }
                    }
                }
                catch
                {
                    // Suppress any unforeseen editor callback exceptions in modern Unity
                }
            };
        }
    }
}
