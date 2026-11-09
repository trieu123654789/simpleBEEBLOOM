using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BeeWaterGrid))]
public sealed class BeeWaterGridInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var grid=(BeeWaterGrid)target;
        EditorGUILayout.HelpBox("Diagnostic grid only. Prescribed flow must be reviewed; shader animation is not measured river velocity. Select this object with Gizmos enabled to see the grid.",MessageType.Info);
        if(Application.isPlaying) EditorGUILayout.HelpBox(grid.Status,grid.IsFresh?MessageType.Info:MessageType.Warning);
        else if(!grid.connectToGama) EditorGUILayout.HelpBox("GAMA water is OFF. This component does not change gameplay, meshes or water materials.",MessageType.Info);
    }
}
