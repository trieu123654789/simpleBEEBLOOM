using System.Collections.Generic;
using System.Linq;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BeeWorldEntity)), CanEditMultipleObjects]
public sealed class BeeWorldEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("World sync sends this active object's type and position to GAMA. Additional flowers are observation only in the current pilot.",MessageType.Info);
        if (targets.Length == 1) EditorGUILayout.LabelField("Entity ID",((BeeWorldEntity)target).Id);
    }
    [MenuItem("Let It Bee/World Sync/Track selected as Bee")]
    static void Bee() => Mark(BeeWorldEntity.EntityKind.Bee);
    [MenuItem("Let It Bee/World Sync/Track selected as Flower")]
    static void Flower() => Mark(BeeWorldEntity.EntityKind.Flower);
    [MenuItem("Let It Bee/World Sync/Track selected as Hive")]
    static void Hive() => Mark(BeeWorldEntity.EntityKind.Hive);
    static void Mark(BeeWorldEntity.EntityKind kind)
    {
        foreach (var go in Selection.gameObjects.Where(x=>x.scene.IsValid())) {
            var entity = go.GetComponent<BeeWorldEntity>();
            if (entity == null) entity = Undo.AddComponent<BeeWorldEntity>(go);
            Undo.RecordObject(entity,"Track Let It Bee object"); entity.kind = kind;
            PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
        }
    }
}

// Duplicating an opted-in object also duplicates its saved ID. Repair only this
// component's internal ID, with Undo; never save scenes or change other settings.
[InitializeOnLoad]
public static class BeeWorldEntityIdentities
{
    static readonly Dictionary<string,BeeWorldEntity> owners = new();
    static bool queued;
    static BeeWorldEntityIdentities() { EditorApplication.hierarchyChanged += Schedule; }
    static void Schedule()
    {
        if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
        queued = true; EditorApplication.delayCall += Repair;
    }
    public static void Repair()
    {
        queued = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var entities = Resources.FindObjectsOfTypeAll<BeeWorldEntity>()
            .Where(x=>x.gameObject.scene.IsValid() && x.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(x)
                && (x.gameObject.hideFlags & HideFlags.DontSave) == 0).ToArray();
        foreach (var key in owners.Keys.ToArray())
            if (owners[key] == null || !entities.Contains(owners[key]) || owners[key].Id != key) owners.Remove(key);
        foreach (var entity in entities) {
            if (string.IsNullOrEmpty(entity.Id) || (owners.TryGetValue(entity.Id,out var owner) && owner != entity)) {
                Undo.RecordObject(entity,"Assign copied world object a unique ID"); entity.RenewIdentity();
                EditorUtility.SetDirty(entity); PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
            }
            owners[entity.Id] = entity;
        }
    }
}
