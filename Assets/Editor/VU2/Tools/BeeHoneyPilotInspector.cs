using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BeeHoneyPilot))]
public sealed class BeeHoneyPilotInspector : Editor
{
    public override void OnInspectorGUI()
    {
        HoneyInspectorPreview.Draw((Component)target);
        DrawDefaultInspector();
        var pilot = (BeeHoneyPilot)target;
        if (Application.isPlaying) {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(pilot.ConnectionStatus, pilot.Live ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.LabelField("Transaction", pilot.Busy ? "Waiting for GAMA" : pilot.LastReason);
            EditorGUILayout.LabelField("Nectar chunk", pilot.ChunkState);
            EditorGUILayout.LabelField("Slot", pilot.Stored + " / " + pilot.HiveCapacity);
            EditorGUILayout.LabelField("Pollinated plants", pilot.ConfirmedPollinations + " / 2");
            EditorGUILayout.LabelField("Active SIMPLE endpoint", pilot.EffectiveSimpleEndpoint);
            Repaint();
        } else {
            EditorGUILayout.HelpBox(pilot.useSimpleToolchain
                ? "Start SIMPLE WebPlatform, launch LetItBeeVR, then Play. IP can be overridden by letitbee-connection.json without editing this scene. Left trigger holds; right trigger collects; right grip picks/releases. Desktop: F / G."
                : "Legacy direct test: Start-GamaServer.ps1, then Play. Left trigger holds; right trigger collects; right grip picks/releases. Desktop: F / G.", MessageType.Info);
        }
        EditorGUILayout.LabelField("External connection file", BeeConnectionProfile.ConfigPath);
        if (!Application.isPlaying && GUILayout.Button("Validate hive cells (no scene changes)")) {
            var cells = new BeeHiveLayout(pilot.slotDecoration,pilot.slotMount,pilot.slotMountLocalPoint,pilot.slot,pilot.hiveCells);
            string message = cells.Count + " hive cells. Existing progress will be kept.\n";
            var bee = pilot.beeRoot != null ? pilot.beeRoot.GetComponent<BeeController>() : null;
            for (int i=0;i<cells.Count;i++) {
                message += cells.Id(i) + " " + cells.Position(i).ToString("F2");
                if (bee != null && bee.maxFlightAltitude>0f && cells.Position(i).y>bee.maxFlightAltitude) message += " ABOVE FLIGHT LIMIT";
                message += "\n";
            }
            Debug.Log(message,pilot);
        }
        if (!Application.isPlaying && GUILayout.Button("New test round (keep tuning and old checkpoint)")) {
            Undo.RecordObject(pilot,"New full hive round"); pilot.sessionId = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(pilot); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pilot.gameObject.scene);
        }
    }
}
