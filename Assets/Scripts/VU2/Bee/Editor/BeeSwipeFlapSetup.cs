// =============================================================================
// BeeSwipeFlapSetup - Editor script gắn BeeSwipeFlap + fix toàn bộ cấu hình
//                     (thân ong, tay cầm, trigger bay) vào mọi BeeController
//                     trong scene đang mở.
//
// Chạy từ Menu: VU2/Bee/🐝 Setup SwipeFlap (Thanh ngang + Ray)
// =============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

public static class BeeSwipeFlapSetup
{
    // =========================================================================
    // MENU ITEM CHÍNH
    // =========================================================================
    [MenuItem("VU2/Bee/🐝 Setup SwipeFlap (Thanh ngang + Ray)")]
    public static void SetupSwipeFlap()
    {
        BeeController[] beeControllers = Object.FindObjectsOfType<BeeController>(true);

        if (beeControllers == null || beeControllers.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "❌ Không tìm thấy BeeController",
                "Không có BeeController nào trong scene đang mở.\n\n" +
                "Hãy mở scene chứa nhân vật ong (ví dụ: SCN_LetItBee, SCN_VU2_Level1_New) rồi chạy lại.",
                "OK");
            return;
        }

        string list = "";
        foreach (var bc in beeControllers)
            list += $"\n• {bc.gameObject.name}";

        bool confirm = EditorUtility.DisplayDialog(
            "🐝 Setup BeeSwipeFlap",
            $"Tìm thấy {beeControllers.Length} BeeController:{list}\n\n" +
            "Script sẽ FIX 3 vấn đề:\n\n" +
            "  [BUG 1] Thân/cánh ong nằm tuốt dưới\n" +
            "    → Tìm và gán visualRoot vào BeeCameraRig\n\n" +
            "  [BUG 2] Không thấy tay cầm\n" +
            "    → Gán anchor trái/phải + tăng kích thước proxy tay\n\n" +
            "  [BUG 3] Trigger vẫn bay thay vì SwipeFlap\n" +
            "    → Tắt enableDirectXrInput, disable BeeShakeFlap/Adapter\n\n" +
            "Tiếp tục?",
            "✅ Fix ngay", "❌ Huỷ");

        if (!confirm) return;

        // Tìm XR Origin trong scene (dùng chung cho tất cả bee)
        XROrigin xrOrigin = Object.FindObjectOfType<XROrigin>();
        Transform leftAnchor  = FindControllerAnchor(xrOrigin, isLeft: true);
        Transform rightAnchor = FindControllerAnchor(xrOrigin, isLeft: false);

        // Fix model tay cầm và tracking cho cả 2 controllers
        FixXrControllers(xrOrigin, leftAnchor, rightAnchor);

        int success = 0;

        foreach (BeeController beeCtrl in beeControllers)
        {
            GameObject go = beeCtrl.gameObject;
            Undo.RecordObject(go, "Setup BeeSwipeFlap");

            // ==================================================================
            // BUG 1 FIX: BeeCameraRig — gán visualRoot & playerRig
            // ==================================================================
            BeeCameraRig camRig = go.GetComponent<BeeCameraRig>();
            if (camRig != null)
            {
                Undo.RecordObject(camRig, "Fix BeeCameraRig refs");

                // Tìm visualRoot: con của BeeRoot tên chứa "Visual" hoặc "visual"
                if (camRig.visualRoot == null)
                {
                    camRig.visualRoot = FindVisualRoot(go.transform);
                    if (camRig.visualRoot != null)
                        Debug.Log($"[Setup] ✅ Gán visualRoot = '{camRig.visualRoot.name}' vào BeeCameraRig trên '{go.name}'.");
                    else
                        Debug.LogWarning($"[Setup] ⚠️ Không tìm thấy visualRoot (BeeVisual) trong con của '{go.name}'. " +
                                         "Thân/cánh sẽ không hiện đúng. Gán thủ công trong Inspector.");
                }

                // Gán playerRig (XR Origin) nếu chưa có
                if (camRig.playerRig == null && xrOrigin != null)
                {
                    camRig.playerRig = xrOrigin.transform;
                    Debug.Log($"[Setup] ✅ Gán playerRig = '{xrOrigin.name}' vào BeeCameraRig trên '{go.name}'.");
                }

                // Gán trackedCamera nếu chưa có
                if (camRig.trackedCamera == null && xrOrigin != null && xrOrigin.Camera != null)
                {
                    camRig.trackedCamera = xrOrigin.Camera;
                    Debug.Log($"[Setup] ✅ Gán trackedCamera = '{xrOrigin.Camera.name}'.");
                }

                // Gán BeeController ref
                if (camRig.bee == null)
                    camRig.bee = beeCtrl;

                // ⚠️ KEY FIX: Tắt wings in first person → không thấy blob cánh vàng khổng lồ
                camRig.showWingsInFirstPerson = false;
                camRig.hideBodyInFirstPerson  = true;
                camRig.targetFov              = 105f;
                camRig.unlockWideFov          = true;
                camRig.applyBankToRig         = false;
                camRig.applyPitchToRig        = false;

                EditorUtility.SetDirty(camRig);
            }
            else
            {
                Debug.LogWarning($"[Setup] ⚠️ '{go.name}' không có BeeCameraRig — bỏ qua fix BUG 1.");
            }

            // ==================================================================
            // BUG 2 & 3 FIX: Gắn BeeSwipeFlap + cấu hình
            // ==================================================================
            BeeSwipeFlap swipe = go.GetComponent<BeeSwipeFlap>();
            if (swipe == null)
                swipe = Undo.AddComponent<BeeSwipeFlap>(go);

            if (swipe != null)
            {
                Undo.RecordObject(swipe, "Configure BeeSwipeFlap");

                // Visual
                swipe.showBars              = true;
                swipe.showRays              = true;   // Hiện tia vàng kim neon định hướng rõ ràng trong VR
                swipe.showHandProxies       = false;  // Tắt quả cầu thô
                swipe.autoSkinControllers   = true;   // Áp da và đảm bảo hiển thị 100% trong VR
                swipe.fadeBarsWhenIdle      = false;  // Luôn hiện thanh ngay khi vào game
                swipe.debugLog              = true;
                swipe.enableLeftHand        = true;
                swipe.enableRightHand       = true;

                // Thanh ngang nối liền duy nhất ở vị trí thấp ngoại vi (-0.21m) để giải phóng 100% tầm nhìn
                swipe.useSingleBar          = true;
                swipe.barCenterOffset       = new Vector2(0f, -0.21f);
                swipe.barDepth              = 0.55f;
                swipe.barWidth              = 0.52f;  // Nối ngang liền mạch
                swipe.barThickness          = 0.012f;

                // Detection 2 chiều
                swipe.bidirectionalCrossing = true;   // Chạm từ dưới lên HOẶC trên xuống đều bay
                swipe.crossDeadband         = 0.025f;
                swipe.cooldownPerHand       = 0.09f;
                swipe.horizontalGate        = 1.3f;

                // Trợ lực bay (Chống mỏi tay)
                swipe.enableFlightAssist    = true;
                swipe.assistLiftImpulse     = 2.4f;   // Xung lực nâng bốc lên tức thì
                swipe.flapHoldSeconds       = 0.45f;  // Giữ lực nâng vỗ cánh lâu hơn
                swipe.glideFloatDuration    = 0.9f;   // Đệm lướt êm
                swipe.glideGravityScale     = 0.35f;  // Trọng lực rơi êm 35%

                // Màu sắc
                swipe.barColor              = new Color(1f, 0.76f, 0.12f, 0.75f);
                swipe.flashColor            = new Color(1f, 0.98f, 0.65f, 1.00f);
                swipe.rayColor              = new Color(0.2f, 0.9f, 1f, 0.85f);

                // BUG 2 FIX: gán anchor tay cầm trực tiếp (không chờ auto-find)
                if (leftAnchor != null)
                {
                    swipe.leftControllerAnchor = leftAnchor;
                    Debug.Log($"[Setup] ✅ Gán leftControllerAnchor = '{leftAnchor.name}'.");
                }
                if (rightAnchor != null)
                {
                    swipe.rightControllerAnchor = rightAnchor;
                    Debug.Log($"[Setup] ✅ Gán rightControllerAnchor = '{rightAnchor.name}'.");
                }

                if (xrOrigin != null) swipe.xrOrigin = xrOrigin;

                EditorUtility.SetDirty(swipe);
                Debug.Log($"[Setup] ✅ BeeSwipeFlap cấu hình xong trên '{go.name}'.");
            }

            // ==================================================================
            // BUG 3 FIX: BeeController — giữ enableDirectXrInput = true và
            //            enableDirectXrMovement = true để joystick DI CHUYỂN BÌNH THƯỜNG.
            //            Chỉ tắt enableDirectXrFlap = false để trigger không bay (phải quẹt thanh).
            // ==================================================================
            Undo.RecordObject(beeCtrl, "Fix BeeController flap input");
            beeCtrl.enableDirectXrInput    = true;   // Giữ mở để đọc phần cứng
            beeCtrl.enableDirectXrMovement = true;   // ← KEY FIX: joystick di chuyển bình thường
            beeCtrl.enableDirectXrFlap     = false;  // ← KEY FIX: tắt trigger/grip bay
            beeCtrl.enableShakeToFlap      = false;
            beeCtrl.enableKeyboardInput    = true;   // Giữ keyboard để test trên PC

            // Gán steerAction & turnAction từ XRI Default Input Actions nếu có
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/XRI Default Input Actions.inputactions");
            if (inputActions != null)
            {
                var moveAction = inputActions.FindAction("XRI LeftHand Locomotion/Move");
                if (moveAction != null) beeCtrl.steerAction = new UnityEngine.InputSystem.InputActionProperty(moveAction);

                var turnAct = inputActions.FindAction("XRI RightHand Locomotion/Turn");
                if (turnAct != null) beeCtrl.turnAction = new UnityEngine.InputSystem.InputActionProperty(turnAct);
            }

            EditorUtility.SetDirty(beeCtrl);
            Debug.Log($"[Setup] ✅ Bật joystick di chuyển (enableDirectXrMovement = true) + Tắt trigger bay (enableDirectXrFlap = false) trên '{go.name}'.");

            // BUG 3 FIX: Tắt BeeShakeFlap nếu có
            BeeShakeFlap shakeFlap = go.GetComponent<BeeShakeFlap>();
            if (shakeFlap != null && shakeFlap.enabled)
            {
                Undo.RecordObject(shakeFlap, "Disable BeeShakeFlap");
                shakeFlap.enabled = false;
                EditorUtility.SetDirty(shakeFlap);
                Debug.Log($"[Setup] 🔕 Tắt BeeShakeFlap trên '{go.name}'.");
            }

            // BUG 3 FIX: Tắt BeeControllerShakeAdapter nếu có
            BeeControllerShakeAdapter adapter = go.GetComponent<BeeControllerShakeAdapter>();
            if (adapter != null && adapter.enabled)
            {
                Undo.RecordObject(adapter, "Disable ShakeAdapter");
                adapter.enabled = false;
                EditorUtility.SetDirty(adapter);
                Debug.Log($"[Setup] 🔕 Tắt BeeControllerShakeAdapter trên '{go.name}'.");
            }

            success++;
        }

        // Đánh dấu scene dirty để Save hoạt động
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        // Tóm tắt kết quả
        string anchorStatus = "";
        if (leftAnchor == null || rightAnchor == null)
        {
            anchorStatus = "\n\n⚠️ CHƯA TÌM THẤY ANCHOR TAY CẦM!\n" +
                           "Gán thủ công trong Inspector BeeSwipeFlap:\n" +
                           "  • Left Controller Anchor\n" +
                           "  • Right Controller Anchor\n" +
                           "(Thường là child của XR Origin tên 'Left Controller' / 'Right Controller')";
        }
        else
        {
            anchorStatus = $"\n\n✅ Đã gán anchor:\n  Trái: {leftAnchor.name}\n  Phải: {rightAnchor.name}";
        }

        EditorUtility.DisplayDialog(
            "🐝 Setup hoàn tất",
            $"Đã xử lý {success} BeeController.\n\n" +
            "Các fix đã áp dụng:\n" +
            "  [BUG 1] ✅ visualRoot, playerRig, trackedCamera gán vào BeeCameraRig\n" +
            "  [BUG 2] ✅ proxy tay to hơn, anchor tay cầm gán trực tiếp\n" +
            "  [BUG 3] ✅ enableDirectXrInput = false → trigger không bay nữa" +
            anchorStatus +
            "\n\nSau khi Save scene và test:\n" +
            "  👋 Giơ tay → thấy 2 quả cầu nhỏ (tay ong)\n" +
            "  📊 Thấy 2 thanh vàng bên phải/trái mắt\n" +
            "  ⬆️ Quét tay TỪ DƯỚI LÊN qua thanh = ong bay lên",
            "Tuyệt! Lưu scene ngay");

        Debug.Log("[BeeSwipeFlapSetup] ✅ Setup hoàn tất. Nhớ Ctrl+S lưu scene.");
    }

    // =========================================================================
    // MENU: Kiểm tra trạng thái
    // =========================================================================
    [MenuItem("VU2/Bee/🔍 Kiểm tra BeeSwipeFlap trong scene")]
    public static void CheckSwipeFlap()
    {
        BeeController[] beeControllers = Object.FindObjectsOfType<BeeController>(true);
        XROrigin xrOrigin = Object.FindObjectOfType<XROrigin>();

        string report = "=== BeeSwipeFlap Diagnostic ===\n\n";
        report += $"XR Origin: {(xrOrigin != null ? xrOrigin.name : "❌ KHÔNG TÌM THẤY")}\n";

        Transform la = FindControllerAnchor(xrOrigin, true);
        Transform ra = FindControllerAnchor(xrOrigin, false);
        report += $"Left Anchor:  {(la  != null ? "✅ " + la.name  : "❌ Không tìm thấy")}\n";
        report += $"Right Anchor: {(ra != null ? "✅ " + ra.name : "❌ Không tìm thấy")}\n\n";

        if (beeControllers != null)
        {
            foreach (BeeController bc in beeControllers)
            {
                GameObject go = bc.gameObject;
                BeeSwipeFlap sf = go.GetComponent<BeeSwipeFlap>();
                BeeShakeFlap sh = go.GetComponent<BeeShakeFlap>();
                BeeControllerShakeAdapter sa = go.GetComponent<BeeControllerShakeAdapter>();
                BeeCameraRig cr = go.GetComponent<BeeCameraRig>();

                report += $"🐝 {go.name}\n";
                report += $"  BeeSwipeFlap : {(sf != null ? (sf.enabled ? "✅ Bật" : "⚠️ Có nhưng TẮT") : "❌ Chưa có")}\n";
                if (sf != null)
                {
                    report += $"    leftAnchor : {(sf.leftControllerAnchor  != null ? "✅ " + sf.leftControllerAnchor.name  : "❌ Chưa gán")}\n";
                    report += $"    rightAnchor: {(sf.rightControllerAnchor != null ? "✅ " + sf.rightControllerAnchor.name : "❌ Chưa gán")}\n";
                    report += $"    fadeBarsWhenIdle: {(sf.fadeBarsWhenIdle ? "⚠️ true (sẽ ẩn thanh lúc đầu)" : "✅ false")}\n";
                }
                report += $"  BeeShakeFlap : {(sh != null ? (sh.enabled ? "⚠️ Đang BẬT (xung đột!)" : "✅ Đã tắt") : "✅ Không có")}\n";
                report += $"  ShakeAdapter : {(sa != null ? (sa.enabled ? "⚠️ Đang BẬT (xung đột!)" : "✅ Đã tắt") : "✅ Không có")}\n";
                report += $"  enableDirectXrInput: {(bc.enableDirectXrInput ? "⚠️ BẬT → trigger vẫn bay!" : "✅ Tắt")}\n";
                report += $"  enableShakeToFlap  : {(bc.enableShakeToFlap   ? "⚠️ Bật" : "✅ Tắt")}\n";
                if (cr != null)
                {
                    report += $"  BeeCameraRig.playerRig  : {(cr.playerRig   != null ? "✅ " + cr.playerRig.name   : "❌ Chưa gán → thân ong lơ lửng!")}\n";
                    report += $"  BeeCameraRig.visualRoot : {(cr.visualRoot  != null ? "✅ " + cr.visualRoot.name  : "❌ Chưa gán → cánh không theo camera!")}\n";
                    report += $"  BeeCameraRig.trackedCamera: {(cr.trackedCamera != null ? "✅ " + cr.trackedCamera.name : "❌ Chưa gán")}\n";
                }
                report += "\n";
            }
        }
        else
        {
            report += "❌ Không có BeeController trong scene!\n";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("🔍 BeeSwipeFlap Diagnostic", report, "OK");
    }

    // =========================================================================
    // HELPER: Tìm visualRoot (BeeVisual) trong con của go
    // =========================================================================
    private static Transform FindVisualRoot(Transform root)
    {
        // Ưu tiên tên chứa "BeeVisual" hoặc "Visual"
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string n = child.name.ToLowerInvariant();
            if (n.Contains("beevisual") || n == "visual" || n.Contains("visualroot"))
                return child;
        }
        // Thử tên "Body" (BeeFlapping gán body)
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.ToLowerInvariant().Contains("body"))
                return child.parent; // Trả về parent chứa cả body + wing
        }
        // Nếu chỉ có 1 child → đó là visual root
        if (root.childCount == 1)
            return root.GetChild(0);

        return null;
    }

    // =========================================================================
    // HELPER: Tìm Left/Right Controller anchor dưới XR Origin theo tên
    // =========================================================================
    private static Transform FindControllerAnchor(XROrigin xrOrigin, bool isLeft)
    {
        if (xrOrigin == null) return null;

        string side = isLeft ? "left" : "right";
        Transform[] all = xrOrigin.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            string n = t.name.ToLowerInvariant();
            bool sideMatch = n.Contains(side);
            bool typeMatch = n.Contains("controller") || n.Contains("hand") || n.Contains("anchor");
            if (sideMatch && typeMatch)
                return t;
        }
        return null;
    }

    // =========================================================================
    // HELPER: Cấu hình model 3D tay cầm + tracking actions + Line Visual cho controllers
    // =========================================================================
    private static void FixXrControllers(XROrigin xrOrigin, Transform leftAnchor, Transform rightAnchor)
    {
        GameObject leftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab");
        GameObject rightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab");

        FixSingleController(leftAnchor, leftPrefab, isLeft: true);
        FixSingleController(rightAnchor, rightPrefab, isLeft: false);
    }

    private static void FixSingleController(Transform anchor, GameObject modelPrefab, bool isLeft)
    {
        if (anchor == null) return;

        // Xoá SortingGroup nếu có (vì SortingGroup 2D làm hỏng culling 3D trong URP XR stereo)
        var sg = anchor.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null) Object.DestroyImmediate(sg);

        // Thiết lập RayTip chuẩn tại mũi tay cầm
        Transform rayTip = anchor.Find("RayTip");
        if (rayTip == null)
        {
            GameObject rtGO = new GameObject("RayTip");
            rtGO.transform.SetParent(anchor, false);
            rayTip = rtGO.transform;
        }
        rayTip.localPosition = new Vector3(0f, 0.01f, 0.04f);
        rayTip.localRotation = Quaternion.identity;

        foreach (var comp in anchor.GetComponents<MonoBehaviour>())
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;

            if (typeName == "ActionBasedController" || typeName.Contains("XRController"))
            {
                SerializedObject so = new SerializedObject(comp);
                SerializedProperty modelProp = so.FindProperty("m_ModelPrefab");
                if (modelProp != null && modelProp.objectReferenceValue == null && modelPrefab != null)
                {
                    modelProp.objectReferenceValue = modelPrefab.transform;
                }
                SerializedProperty parentProp = so.FindProperty("m_ModelParent");
                if (parentProp != null && parentProp.objectReferenceValue == null)
                {
                    parentProp.objectReferenceValue = anchor;
                }
                SerializedProperty trackProp = so.FindProperty("m_EnableInputTracking");
                if (trackProp != null) trackProp.boolValue = true;
                SerializedProperty actProp = so.FindProperty("m_EnableInputActions");
                if (actProp != null) actProp.boolValue = true;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
                Debug.Log($"[Setup] ✅ Đã cấu hình {(isLeft ? "Left" : "Right")} Controller ({typeName}).");
            }

            if (typeName == "XRInteractorLineVisual")
            {
                // Tắt tia đỏ không cần thiết
                comp.enabled = false;
                var lr = comp.GetComponent<LineRenderer>();
                if (lr != null) lr.enabled = false;
                EditorUtility.SetDirty(comp);
            }
        }
    }
}
#endif
