#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using LetItBee.UI;
using LetItBee.HoneyPilot;

public static class BeeMainMenuChecks
{
    [MenuItem("Let It Bee/Checks/Main Menu")]
    public static void Run()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/LetItBee/UI/PF_MainMenu.prefab");
        float volume=AudioListener.volume;
        try {
            var menu=root.GetComponent<BeeMainMenu>();
            var card=menu.cards.storyCard;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var hover=typeof(LetItBeeMenuCard).GetField("_glowTarget",flags);
            var left=new PointerEventData(null){pointerId=10};var right=new PointerEventData(null){pointerId=11};
            card.OnPointerEnter(left);card.OnPointerEnter(right);card.OnPointerExit(left);
            check((float)hover.GetValue(card)==1f,"Second pointer must retain hover.");
            card.OnPointerExit(right);check((float)hover.GetValue(card)==0f,"Last pointer clears hover.");
            int clicks=0;card.onClick.AddListener(()=>clicks++);
            card.OnPointerClick(left);card.OnPointerClick(right);check(clicks==1,"Simultaneous clicks must be deduplicated.");
            card.GetComponent<UnityEngine.UI.Button>().interactable=false;
            card.OnPointerClick(left);check(clicks==1,"Disabled card must not click.");
            card.GetComponent<UnityEngine.UI.Button>().interactable=true;
            menu.ShowHome();check(!menu.detailsPanel.activeSelf,"Home hides details.");
            menu.SelectStudy();check(menu.State==BeeMainMenu.MenuState.Study && menu.detailsText.text.Contains("Coming soon"),"Study must be explicit placeholder.");
            menu.SelectSettings();check(menu.volumeUpButton.gameObject.activeSelf,"Settings exposes volume.");
            menu.ShowHome();menu.SelectStory();check(menu.State==BeeMainMenu.MenuState.Connecting,"Story waits for server.");
            typeof(BeeMainMenu).GetField("canvas",flags).SetValue(menu,root.GetComponent<Canvas>());
            typeof(BeeMainMenu).GetField("deadline",flags).SetValue(menu,-1f);
            typeof(BeeMainMenu).GetMethod("Update",flags).Invoke(menu,null);
            check(menu.State==BeeMainMenu.MenuState.ConnectionFailed,"No server must show failure, not enter gameplay.");
            check(menu.retryButton.gameObject.activeSelf,"Failure exposes Retry.");
            menu.SelectStory();check(menu.State==BeeMainMenu.MenuState.Connecting,"Retry restarts waiting.");
            menu.ShowHome();check(menu.State==BeeMainMenu.MenuState.Home,"Back cancels pending entry.");
            check(root.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace,"VR canvas must be world space.");
            check(root.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>()!=null,"XR raycaster missing.");
            check(menu.cards.bobHeight==0f,"Whole menu must remain stable.");
            check(root.GetComponentsInChildren<Camera>(true).Length==0,"Menu must not add a camera.");
            var pilot=root.AddComponent<BeeHoneyPilot>();
            typeof(BeeMainMenu).GetField("pilot",flags).SetValue(menu,pilot);
            typeof(BeeHoneyPilot).GetProperty("Live").SetValue(pilot,true);
            typeof(BeeHoneyPilot).GetProperty("State").SetValue(pilot,new Newtonsoft.Json.Linq.JObject());
            menu.SelectStory();typeof(BeeMainMenu).GetMethod("Update",flags).Invoke(menu,null);
            check(menu.State==BeeMainMenu.MenuState.Releasing,"Server readiness must still wait for trigger release.");
            typeof(BeeHoneyPilot).GetProperty("Live").SetValue(pilot,false);
            typeof(BeeMainMenu).GetMethod("Update",flags).Invoke(menu,null);
            check(menu.State==BeeMainMenu.MenuState.Connecting,"Lost connection during release must return to waiting.");
                        var leftObject=new GameObject("Left ray check");leftObject.transform.SetParent(root.transform);
            var rightObject=new GameObject("Right ray check");rightObject.transform.SetParent(root.transform);
            var leftRay=leftObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            var rightRay=rightObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            var originalLeft=leftRay.uiPressInput;var originalRight=rightRay.uiPressInput;
            var bind=typeof(BeeMainMenu).GetMethod("BindRay",flags);
            bind.Invoke(menu,new object[]{leftRay,"LeftHand"});bind.Invoke(menu,new object[]{rightRay,"RightHand"});
            check(leftRay.uiPressInput.inputActionPerformed.bindings[0].path=="<XRController>{LeftHand}/trigger","Left binding must use left trigger.");
            check(rightRay.uiPressInput.inputActionPerformed.bindings[0].path=="<XRController>{RightHand}/trigger","Right binding must use right trigger.");
            check(leftRay.uiPressInput.inputActionPerformed.enabled && rightRay.uiPressInput.inputActionPerformed.enabled,"UI input actions must be enabled.");
            typeof(BeeMainMenu).GetMethod("RestoreGameplay",flags).Invoke(menu,null);
            check(ReferenceEquals(originalLeft,leftRay.uiPressInput)&&ReferenceEquals(originalRight,rightRay.uiPressInput),"Gameplay must restore both original readers.");
            Debug.Log("[Let It Bee] Main menu checks passed: "+count);
        } finally {AudioListener.volume=volume;PrefabUtility.UnloadPrefabContents(root);}
    }
    public static string Render()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/LetItBee/UI/PF_MainMenu.prefab");
        var cameraGo=new GameObject("Menu preview camera",typeof(Camera));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,root.scene);
        var camera=cameraGo.GetComponent<Camera>(); camera.scene=root.scene;
        var rt=new RenderTexture(1200,800,24);var tex=new Texture2D(1200,800,TextureFormat.RGB24,false);
        var old=RenderTexture.active;
        try {
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one*0.0012f;
            camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;camera.orthographicSize=0.49f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.12f,0.2f,0.16f);
            camera.targetTexture=rt;root.GetComponent<Canvas>().worldCamera=camera;
            Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>())t.ForceMeshUpdate();
            camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,800),0,0);tex.Apply();
            return Convert.ToBase64String(tex.EncodeToPNG());
        } finally {RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif