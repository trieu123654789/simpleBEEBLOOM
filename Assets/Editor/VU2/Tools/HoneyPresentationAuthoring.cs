using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class HoneyPresentationAuthoring
{
    const string Root = "Assets/Prefabs/LetItBee/HoneyPilot/";
    const string Effects = Root + "Effects/";
    const string LibraryPath = "Assets/Resources/BeePresentation/PresentationLibrary.asset";

    [MenuItem("Let It Bee/Authoring/Create Honey Effect Prefabs")]
    public static void Menu() => Debug.Log(Build());

    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        Directory.CreateDirectory(Effects);
        Directory.CreateDirectory("Assets/Resources/BeePresentation");
        AssetDatabase.Refresh();
        var originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "PRB_NectarChunk.prefab");
        if (originalPrefab == null) throw new InvalidOperationException("The authored PRB_NectarChunk prefab is missing.");
        var prefab = PrefabUtility.LoadPrefabContents(Root + "PRB_NectarChunk.prefab");
        bool preserved;
        try {
            var before = prefab.GetComponentsInChildren<Component>(true).Where(c => c != null)
                .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
            var view = prefab.GetComponent<BeeNectarView>();
            if (view == null) view = prefab.AddComponent<BeeNectarView>();
            if (view.meshFilter == null) view.meshFilter = prefab.GetComponent<MeshFilter>();
            if (view.meshRenderer == null) view.meshRenderer = prefab.GetComponent<MeshRenderer>();
            if (view.gripAnchor == null) {
                var anchor = new GameObject("Grip Anchor").transform;
                anchor.SetParent(prefab.transform, false); view.gripAnchor = anchor;
            }
            preserved = before.All(x => EditorJsonUtility.ToJson(x.Key) == x.Value);
            if (!preserved) throw new Exception("Existing nectar prefab properties changed; refusing to save.");
            PrefabUtility.SaveAsPrefabAsset(prefab, Root + "PRB_NectarChunk.prefab");
        } finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var dots = Material("M_HoneyMotes", 0f);
        var ribbons = Material("M_HoneyRibbons", 1f);
        var droplet = AssetDatabase.LoadAssetAtPath<Mesh>(Effects + "NectarDroplet.asset");
        if (droplet == null) {
            droplet = (Mesh)typeof(BeeHoneyFeedback).GetMethod("DropletMesh", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            droplet.hideFlags = HideFlags.None; AssetDatabase.CreateAsset(droplet, Effects + "NectarDroplet.asset");
        }
        var flower = Make(BeeHoneyEffectKind.Flower, "PRB_FlowerNectarEffects", dots, ribbons, droplet);
        var carry = Make(BeeHoneyEffectKind.Carry, "PRB_NectarCarryEffects", dots, ribbons, droplet);
        var hive = Make(BeeHoneyEffectKind.Hive, "PRB_HiveSocketEffects", dots, ribbons, droplet);
        var library = AssetDatabase.LoadAssetAtPath<BeeHoneyPresentationLibrary>(LibraryPath);
        if (library == null) {
            library = ScriptableObject.CreateInstance<BeeHoneyPresentationLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.nectarPrefab = originalPrefab;
        library.flowerEffectsPrefab = flower; library.carryEffectsPrefab = carry; library.hiveEffectsPrefab = hive;
        library.dropletMesh = droplet;
        if (library.audioProfile == null) library.audioProfile = Resources.Load<BeeInteractionAudio>("BeeAudio/InteractionAudio");
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
        var report = new { prefabPropertiesPreserved = preserved, nectar = AssetDatabase.GetAssetPath(originalPrefab),
            effects = new[] { AssetDatabase.GetAssetPath(flower), AssetDatabase.GetAssetPath(carry), AssetDatabase.GetAssetPath(hive) },
            library = LibraryPath, runtimeShaders = new[] { "LetItBee/HoneyNectar", "LetItBee/HoneyFeedback" } };
        Directory.CreateDirectory("Library/HoneyAuthoring");
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Library/HoneyAuthoring/authoring.json", json);
        return json;
    }

    static Material Material(string name, float ribbon)
    {
        string path = Effects + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var material = new Material(Resources.Load<Shader>("HoneyFeedback")) { name = name };
        material.SetFloat("_Ribbon", ribbon); AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static BeeHoneyEffectRig Make(BeeHoneyEffectKind kind, string name, Material dots, Material ribbons, Mesh droplet)
    {
        string path = Effects + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<BeeHoneyEffectRig>();
        var root = new GameObject(name); root.layer = 2;
        try {
            var rig = root.AddComponent<BeeHoneyEffectRig>(); rig.effectKind = kind;
            var lines = new List<LineRenderer>();
            Action<string, int, bool> line = (label, points, loop) => {
                var go = new GameObject(label); go.layer = 2; go.transform.SetParent(root.transform, false);
                var draw = go.AddComponent<LineRenderer>(); draw.sharedMaterial = ribbons;
                draw.useWorldSpace = true; draw.positionCount = points; draw.loop = loop;
                draw.shadowCastingMode = ShadowCastingMode.Off; draw.receiveShadows = false;
                draw.textureMode = LineTextureMode.Stretch; draw.enabled = false; lines.Add(draw);
            };
            if (kind == BeeHoneyEffectKind.Flower) {
                for (int i = 0; i < 2; i++) line("Flower warmth " + i, 41, true);
                for (int i = 0; i < 3; i++) line("Nectar filament " + i, 20, false);
                line("Nectar hold progress", 41, false);
                var drops = new List<Transform>();
                for (int i = 0; i < 3; i++) {
                    var drop = new GameObject("Nectar liquid preview " + i); drop.layer = 2;
                    drop.transform.SetParent(root.transform, false); drop.AddComponent<MeshFilter>().sharedMesh = droplet;
                    var draw = drop.AddComponent<MeshRenderer>();
                    draw.shadowCastingMode = ShadowCastingMode.Off; draw.receiveShadows = false;
                    // The runtime binds its private nectar material; no shared material is animated.
                    drop.SetActive(false); drops.Add(drop.transform);
                }
                rig.droplets = drops.ToArray();
            } else if (kind == BeeHoneyEffectKind.Carry) {
                line("Viscous nectar lift", 25, false); line("Honey grab target", 33, true);
                rig.motesFilter = root.AddComponent<MeshFilter>(); rig.motesRenderer = root.AddComponent<MeshRenderer>();
                rig.motesRenderer.sharedMaterial = dots; rig.motesRenderer.shadowCastingMode = ShadowCastingMode.Off;
                rig.motesRenderer.receiveShadows = false; rig.motesRenderer.enabled = false;
                var sources = new List<AudioSource>();
                foreach (string label in new[] { "Honey event audio", "Nectar gathering audio", "Spatial event voice 0",
                    "Spatial event voice 1", "Spatial event voice 2", "Spatial event voice 3", "Spatial event voice 4", "Spatial event voice 5" }) {
                    var go = new GameObject(label); go.transform.SetParent(root.transform, false);
                    var audio = go.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 1f;
                    audio.dopplerLevel = 0f; audio.minDistance = .25f; audio.maxDistance = 5f;
                    audio.rolloffMode = AudioRolloffMode.Linear; sources.Add(audio);
                }
                rig.audioSources = sources.ToArray();
            } else {
                line("Hive socket rim", 7, true); line("Hive confirmation wave", 7, true);
                line("Wax rim sealing", 49, false);
                for (int i = 0; i < 6; i++) line("Wax warmth " + i, 7, true);
            }
            rig.lines = lines.ToArray();
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<BeeHoneyEffectRig>();
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
