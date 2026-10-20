using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

// Bake collision into the build copy so Quest does not depend on readable FBX
// meshes or cook terrain at startup. The source scene is never saved/modified.
public sealed class BeeMapSurfaceBuild : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Unity also processes the transient scene copy before entering Play.
        // Do this before static batching replaces each MeshFilter's source mesh.
        if (scene.name == "SCN_LetItBee") BeeMapSurfaces.Install(scene);
    }
}
