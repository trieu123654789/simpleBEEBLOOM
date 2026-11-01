#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LetItBee.UI;

public static class BeeMainMenuInstaller
{
    public static string InstallMusic()
    {
        const string audioPath = "Assets/LetItBee/UI/Audio/LetItBee_MenuTheme.wav";
        var importer = (AudioImporter)AssetImporter.GetAtPath(audioPath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        const string path = "Assets/Prefabs/LetItBee/UI/PF_MainMenu.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            var menu = root.GetComponent<BeeMainMenu>();
            var audio = root.GetComponent<AudioSource>();
            if (audio == null) audio = root.AddComponent<AudioSource>();
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
            if (audio.clip == null) throw new InvalidOperationException("Menu music import failed.");
            audio.playOnAwake = false; audio.loop = true; audio.spatialBlend = 0f;
            audio.volume = 0.35f; audio.dopplerLevel = 0f;
            menu.menuMusic = audio;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return "Menu music installed: " + audio.clip.length + " seconds";
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    [MenuItem("Let It Bee/Menu/Install Main Menu")]
    public static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/VU2/SCN_LetItBee.unity") throw new InvalidOperationException("Open the saved Let It Bee scene in Edit Mode.");
        if (scene.isDirty) throw new InvalidOperationException("Save your current scene edits before installing the menu.");
        if (UnityEngine.Object.FindFirstObjectByType<BeeMainMenu>(FindObjectsInactive.Include) != null) throw new InvalidOperationException("A menu is already installed. Edit its prefab instead.");
        var before = new Dictionary<Component,string>();
        foreach(var root in scene.GetRootGameObjects()) foreach(var c in root.GetComponentsInChildren<Component>(true))
            if(c != null) before[c] = EditorJsonUtility.ToJson(c);
        LetItBee.UIEditor.LetItBeeMenuBuilder.Create();
        var rootMenu = Selection.activeGameObject;
        var menu = rootMenu.AddComponent<BeeMainMenu>();
        menu.cards = rootMenu.GetComponent<LetItBeeMenuController>();
        foreach(var ray in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None)) {
            if(ray.name == "Left Controller") menu.leftRay = ray;
            if(ray.name == "Right Controller") menu.rightRay = ray;
        }
        menu.statusText = rootMenu.transform.Find("Hint").GetComponent<TMP_Text>();
        menu.statusText.fontSize = 23;
        menu.statusText.enableAutoSizing = true; menu.statusText.fontSizeMin = 15; menu.statusText.fontSizeMax = 23;
        menu.statusText.rectTransform.sizeDelta = new Vector2(940,60);
        menu.statusText.rectTransform.anchoredPosition = new Vector2(0,-246);
        rootMenu.transform.Find("Subtitle").GetComponent<TMP_Text>().text = "FLOWERS  ·  POLLINATORS  ·  NATURE";
        var panel = Rect("Details",rootMenu.transform,Vector2.zero,new Vector2(940,350));
        menu.detailsPanel = panel.gameObject;
        menu.detailsText = Text("Description",panel,new Vector2(0,20),new Vector2(880,210),27);
        menu.backButton = Button("Back",panel,new Vector2(-320,-160),new Vector2(160,52),"Back");
        menu.retryButton = Button("Retry",panel,new Vector2(320,-160),new Vector2(160,52),"Retry");
        menu.volumeDownButton = Button("VolumeDown",panel,new Vector2(-185,-110),new Vector2(60,48),"−");
        menu.volumeUpButton = Button("VolumeUp",panel,new Vector2(185,-110),new Vector2(60,48),"+");
        menu.volumeText = Text("Volume",panel,new Vector2(0,-110),new Vector2(290,48),25);
        panel.gameObject.SetActive(false);
        foreach(var card in rootMenu.GetComponentsInChildren<LetItBeeMenuCard>(true)) {
            var button=card.GetComponent<Button>(); button.transition=Selectable.Transition.None;
            button.navigation=new Navigation { mode=Navigation.Mode.None };
        }
        var raycaster=rootMenu.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        raycaster.checkFor3DOcclusion=false; raycaster.checkFor2DOcclusion=false;
        foreach(var pair in before) if(pair.Key == null || EditorJsonUtility.ToJson(pair.Key)!=pair.Value)
            throw new InvalidOperationException("Existing scene component changed unexpectedly; scene was not saved.");
        const string folder="Assets/Prefabs/LetItBee/UI";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prefabs/LetItBee","UI");
        // Prefab must not retain a reference to the scene camera.
        rootMenu.GetComponent<Canvas>().worldCamera=null;
        var left = menu.leftRay; var right = menu.rightRay;
        menu.leftRay = menu.rightRay = null;
        PrefabUtility.SaveAsPrefabAssetAndConnect(rootMenu,folder+"/PF_MainMenu.prefab",InteractionMode.AutomatedAction);
        menu.leftRay = left; menu.rightRay = right;
        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
        if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Debug.Log("[Let It Bee] Main menu installed. Existing scene components verified unchanged: "+before.Count);
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*0.5f;rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
    }
    static TMP_Text Text(string name,Transform parent,Vector2 position,Vector2 size,float fontSize)
    {
        var t=Rect(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();
        t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;t.color=new Color32(74,46,0,255);t.raycastTarget=false;return t;
    }
    static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string caption)
    {
        var rt=Rect(name,parent,position,size);var image=rt.gameObject.AddComponent<Image>();image.color=new Color32(255,211,100,255);
        var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=image;
        Text("Label",rt,Vector2.zero,size,25).text=caption;return b;
    }
}
#endif