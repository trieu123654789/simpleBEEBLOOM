using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BeeHoneyPilot))]
public sealed class BeeHoneyPilotInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var pilot = (BeeHoneyPilot)target;
        if (Application.isPlaying) {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(pilot.ConnectionStatus, pilot.Live ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.LabelField("Transaction", pilot.Busy ? "Waiting for GAMA" : pilot.LastReason);
            EditorGUILayout.LabelField("Nectar chunk", pilot.ChunkState);
            EditorGUILayout.LabelField("Slot", pilot.Stored + " / 1");
            EditorGUILayout.LabelField("Pollinated plants", pilot.ConfirmedPollinations + " / 2");
            EditorGUILayout.LabelField("Active SIMPLE endpoint", pilot.EffectiveSimpleEndpoint);
            Repaint();
        } else {
            EditorGUILayout.HelpBox(pilot.useSimpleToolchain
                ? "Start SIMPLE WebPlatform, launch LetItBeeVR, then Play. IP can be overridden by letitbee-connection.json without editing this scene. Left trigger holds; right trigger collects; right grip picks/releases. Desktop: F / G."
                : "Legacy direct test: Start-GamaServer.ps1, then Play. Left trigger holds; right trigger collects; right grip picks/releases. Desktop: F / G.", MessageType.Info);
        }
        EditorGUILayout.LabelField("External connection file", BeeConnectionProfile.ConfigPath);
    }
}
