#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LetItBee.UI;

namespace LetItBee.UIEditor
{
    /// <summary>
    /// Menu: Let It Bee > Create VR Main Menu
    /// Builds a World Space canvas with the panel, title and three cards.
    /// </summary>
    public static class LetItBeeMenuBuilder
    {
        const string SpriteDir = "Assets/LetItBee/UI/Sprites/";
        const float PanelW = 1024f, PanelH = 592f;
        const float WorldScale = 0.0012f;      // 1024 units -> ~1.23 m wide
        const float DistanceFromHead = 2f;     // meters

        [MenuItem("Let It Bee/Create VR Main Menu")]
        public static void Create()
        {
            Sprite panel = LoadSprite("Menu_Panel.png", Vector4.zero);
            Sprite story = LoadSprite("Card_Story.png", Vector4.zero);
            Sprite study = LoadSprite("Card_Study.png", Vector4.zero);
            Sprite settings = LoadSprite("Card_Settings.png", Vector4.zero);
            Sprite border = LoadSprite("Card_Border.png", new Vector4(32, 32, 32, 32));
            Sprite glow = LoadSprite("Card_Glow.png", new Vector4(48, 48, 48, 48));
            Sprite logo = LoadSprite("Logo_LetItBee.png", Vector4.zero);
            Sprite bee = LoadSprite("Bee_Decor.png", Vector4.zero);
            Sprite trail = LoadSprite("Trail_Dashes.png", Vector4.zero);

            var root = new GameObject("LetItBee_MainMenu",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Create Let It Bee Menu");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var cam = Camera.main;
            if (cam != null) canvas.worldCamera = cam;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PanelW, PanelH);
            rt.localScale = Vector3.one * WorldScale;
            if (cam != null)
            {
                Vector3 fwd = cam.transform.forward;
                fwd.y = 0f;
                fwd = fwd.sqrMagnitude < 0.01f ? Vector3.forward : fwd.normalized;
                rt.position = cam.transform.position + fwd * DistanceFromHead;
                rt.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else
            {
                rt.position = new Vector3(0f, 1.5f, DistanceFromHead);
            }

            SetupRaycaster(root);
            SetupEventSystem();

            // Panel + texts
            NewImage("Panel", root.transform, panel, Vector2.zero, new Vector2(PanelW, PanelH), false);

            // Logo (with the bee inside the word), flight trail and decorative bees
            var logoImg = NewImage("Logo", root.transform, logo, new Vector2(-110, 215), new Vector2(540, 125), false);
            logoImg.preserveAspect = true;
            NewImage("Trail", root.transform, trail, new Vector2(216, 228), new Vector2(360, 110), false);

            var smallBee = NewImage("Bee_Small", root.transform, bee, new Vector2(-420, 188), new Vector2(66, 54), false);
            smallBee.preserveAspect = true;
            AddFloat(smallBee.gameObject, 5f, 1.4f, 5f, 0f);

            var heroBee = NewImage("Bee_Hero", root.transform, bee, new Vector2(472, 296), new Vector2(170, 138), false);
            heroBee.preserveAspect = true;
            AddFloat(heroBee.gameObject, 7f, 1.0f, 3f, 1.7f);

            var brown = new Color32(74, 46, 0, 255);
            NewText("Subtitle", root.transform, "SALTWATER  \u00B7  FLOWERS  \u00B7  POLLINATORS", 28,
                new Vector2(-110, 138), new Vector2(800, 44)).color = brown;
            NewText("Hint", root.transform, "Point the ray at a card, then press the trigger", 28,
                new Vector2(0, -235), new Vector2(900, 44)).color = brown;

            // Cards
            var storyCard = NewCard("Card_Story", root.transform, story, border, glow, new Vector2(-310, -40));
            var studyCard = NewCard("Card_Study", root.transform, study, border, glow, new Vector2(0, -40));
            var settingsCard = NewCard("Card_Settings", root.transform, settings, border, glow, new Vector2(310, -40));

            var ctrl = root.AddComponent<LetItBeeMenuController>();
            ctrl.bobHeight = 0f;
            ctrl.storyCard = storyCard;
            ctrl.studyCard = studyCard;
            ctrl.settingsCard = settingsCard;

            Selection.activeGameObject = root;
            Debug.Log("[Let It Bee] Menu created. Wire Story/Study/Settings in LetItBeeMenuController > Events.");
        }

        // ---------- helpers ----------

        static void AddFloat(GameObject go, float amp, float speed, float tilt, float phase)
        {
            var f = go.AddComponent<LetItBeeFloatDecor>();
            f.amplitude = amp;
            f.speed = speed;
            f.tiltDegrees = tilt;
            f.phase = phase;
        }

        static LetItBeeMenuCard NewCard(string name, Transform parent, Sprite art, Sprite border, Sprite glow, Vector2 pos)
        {
            Vector2 size = new Vector2(280, 260);
            var rect = NewRect(name, parent, pos, size);
            var hit = rect.gameObject.AddComponent<Image>();   // invisible hit area for the ray
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            var glowImg = NewImage("Glow", rect, glow, Vector2.zero, size + new Vector2(80, 80), false);
            glowImg.type = Image.Type.Sliced;
            glowImg.color = new Color(1f, 0.82f, 0.25f, 0f);
            NewImage("Art", rect, art, Vector2.zero, size, false);
            var borderImg = NewImage("Border", rect, border, Vector2.zero, size, false);
            borderImg.type = Image.Type.Sliced;

            var card = rect.gameObject.AddComponent<LetItBeeMenuCard>();
            card.glow = glowImg;
            card.border = borderImg;
            return card;
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, bool raycast)
        {
            var r = NewRect(name, parent, pos, size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 box)
        {
            var r = NewRect(name, parent, pos, box);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Sprite LoadSprite(string file, Vector4 border)
        {
            string path = SpriteDir + file;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                bool dirty = imp.textureType != TextureImporterType.Sprite
                             || imp.spriteImportMode != SpriteImportMode.Single
                             || imp.mipmapEnabled
                             || imp.spriteBorder != border;
                if (dirty)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;
                    imp.spriteBorder = border;
                    imp.SaveAndReimport();
                }
            }
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning("[Let It Bee] Missing sprite: " + path);
            return s;
        }

        // XR Interaction Toolkit support (found by name so the package still compiles without XRI)
        static void SetupRaycaster(GameObject root)
        {
            Type tracked = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (tracked != null)
            {
                var gr = root.GetComponent<GraphicRaycaster>();
                if (gr != null) UnityEngine.Object.DestroyImmediate(gr);
                root.AddComponent(tracked);
            }
            else
            {
                Debug.LogWarning("[Let It Bee] XR Interaction Toolkit not found: using GraphicRaycaster. " +
                                 "Install XRI and re-run to get VR controller-ray clicking.");
            }
        }

        static void SetupEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Type module =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? typeof(StandaloneInputModule);
            go.AddComponent(module);
        }
    }
}
#endif
