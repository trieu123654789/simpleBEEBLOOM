// =============================================================================
// BeeDesktopTestMode - Cho phep TEST game Bay ong tren PC, KHONG can kinh VR
// va khong can tay cam.
//
// VAN DE GIAI QUYET
//   BeeController doc input tuong doi doc lap voi XR (chi doc InputAction),
//   nhung BeeCameraRig lai day camera cua XR Origin va BeeAltitudeHold giu ong
//   o cao dinh -> khi khong co kinh VR thi camera khong di chuyen theo ong.
//
// CACH HOAT DONG
//   1. Tim BeeController + Camera (tu tao neu scene chua co camera).
//   2. Tam tat BeeCameraRig va BeeAltitudeHold de khong "giet" camera.
//   3. Tat TrackedPoseDriver trong XR Origin (khong co head tracking khi test).
//   4. Tu tao InputAction ao (WASD + Space) neu scene chua co BeeFlightInput.
//   5. Tu dieu khien camera theo ong (goc ninh ong hoac quanh sau lung).
//
// PHIM
//   W / S        - nhung xuong (pitch)
//   A / D        - re trai / phai (yaw)
//   Space        - dap canh (bay len)
//   Shift (giu)  - dap canh lien tuc (bay nhanh)
//   Chuot        - nhin quanh
//   F            - doi giua goc ninh ong / sau lung ong
//   R            - dua ong ve vi tri bat dau
//   Esc          - tho che do test (tra ve input VR)
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BeeDesktopTestMode : MonoBehaviour
{
    [Header("References / Tham chieu")]
    public BeeController bee;
    public Camera cam;

    [Header("View / Goc nhin")]
    public bool startThirdPerson = true;
    public float thirdPersonDistance = 4.5f;
    public float thirdPersonHeight = 1.2f;
    public float mouseSensitivity = 3f;
    [Tooltip("Khoa con tro chuot khi bat dau (Esc de tha).")]
    public bool lockCursor = true;

    public enum ControlScheme
    {
        /// <summary>W/S nhung - A/D re. Ong tu bay theo huong mui (VR).</summary>
        BeeFly = 0,

        /// <summary>W/S tien-lui - A/D di ngang - Q/E len-xuong (giong XR Device Simulator).</summary>
        DeviceSimulator = 1
    }

    [Header("Control scheme / Kieu dieu khien")]
    [Tooltip("Mac dinh: DeviceSimulator (W,S,A,D + Q,E nhu XR Device Simulator).")]
    public ControlScheme scheme = ControlScheme.DeviceSimulator;

    [Header("Simulator movement / Dieu khien simulator")]
    [Tooltip("Toc do toi da tieng/lui toi da (m/s).")]
    public float horizontalMaxSpeed = 9f;

    [Tooltip("Toc do leo toi da (m/s).")]
    public float verticalMaxSpeed = 6f;

    [Tooltip("Gia toc ngang (m/s^2) - giu phim de tang toc dan, khong 'nhay' nhu thang may.")]
    public float moveAccel = 12f;

    [Tooltip("Gia toc doc (m/s^2) khi leo/xuong.")]
    public float verticalAccel = 8f;

    [Tooltip("Toc do ha toc ngang khi khong phim (ma sat khong khi).")]
    public float horizontalDrag = 6f;

    [Tooltip("Trong luoc: tha phim -> ong roi toc do va TE mũi xuong.")]
    public float diveGravity = 9f;

    [Tooltip("Luc day khi dap canh (Space/Shift) - giu ong khong bi roi.")]
    public float flapLift = 7f;

    [Tooltip("Luc day ngang khi dap canh.")]
    public float flapForward = 5f;

    [Header("Nose / Mui ong")]
    [Tooltip("Do nghieng mui cho moi 1 m/s leo (do) - ong nhung mui khi leo, cụi mui khi lao xuong.")]
    public float noseAnglePerSpeed = 4.5f;

    [Tooltip("Do muot cua chuyen dong mui.")]
    public float noseSmooth = 5f;

    [Header("Vertical / Leo - xuong bang Q-E")]
    [Tooltip("Luc day thang truc Y khi giu E (m/s^2).")]
    public float verticalThrust = 7f;

    [Tooltip("Toc do nghieng mui khi giu Q/E (do/giay).")]
    public float verticalPitchPerSecond = 40f;

    [Header("Test options / Tuy chon test")]
    public bool disableAltitudeHold = true;
    public bool disableCameraRig = true;
    public bool showHelpOverlay = true;

    // Vi tri "mat" cua ong khi dung goc ninh ong (local).
    private static readonly Vector3 EyeOffset = new Vector3(0f, 0.08f, 0.16f);

    private readonly List<Behaviour> _disabledBehaviours = new List<Behaviour>();
    private Vector3 _vel;              // van toc ong (m/s) - tao cam giac co quan tinh
    private float _nosePitch;        // goc nhung mui (do), smooth
    private Renderer[] _bodyRenderers;
    private InputAction _steer, _flap;
    private bool _installedActions;
    private bool _thirdPerson;
    private float _orbitYaw, _orbitPitch;
    private bool _helpShown;

    private void Awake()
    {
        if (bee == null) bee = FindObjectOfType<BeeController>();
        if (cam == null) cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (cam == null) cam = CreateCamera();
        startPos = bee != null ? bee.transform.position : Vector3.zero;
        _thirdPerson = startThirdPerson;
    }

    private void OnEnable()
    {
        // 1. Tat rig / altitude hold + tracked pose driver
        if (disableCameraRig) Disable("BeeCameraRig");
        if (disableAltitudeHold) Disable("BeeAltitudeHold");
        DisableTrackedPoseDrivers();

        // 2. Tao input ao neu chua co BeeFlightInput trong scene
        InstallKeyboardInputIfMissing();

        // 3. Che do simulator: ong do script nay dieu khien hoan toan.
        if (bee != null) bee.ExternalMovementLocked = (scheme == ControlScheme.DeviceSimulator);

        // 4. Reset van toc (on gia trang co quang tinh, khong "bich" khi bat len).
        _vel = Vector3.zero;
        _nosePitch = 0f;

        // 3. Khoa chuot + reset goc nhin quanh sau lung
        _orbitYaw = 0f; _orbitPitch = 12f;
        if (lockCursor && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ShowHelpOnce();
    }

    private void OnDisable()
    {
        Restore();
    }

    // =====================================================================
    // LateUpdate - Dieu khien camera theo ong.
    // =====================================================================
    private void LateUpdate()
    {
        if (bee == null || cam == null) return;

        ReadHotkeys();
        ReadMouseLook();

        if (scheme == ControlScheme.DeviceSimulator) UpdateSimulatorMovement();

        Vector3 beePos = bee.transform.position;
        Quaternion beeRot = bee.transform.rotation;

        if (!_thirdPerson)
        {
            cam.transform.SetPositionAndRotation(bee.transform.TransformPoint(EyeOffset), beeRot);
        }
        else
        {
            Quaternion orbit = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 pos = beePos - (orbit * Vector3.forward) * thirdPersonDistance
                                   + Vector3.up * thirdPersonHeight;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation((beePos - pos).normalized, Vector3.up);
        }

        // Góc nhìn ong: an than de khong bi thân che mat; sau lung: hien tat ca.
        if (bee != null) SetBodyVisible(_thirdPerson);
    }

    private void SetBodyVisible(bool visible)
    {
        if (_bodyRenderers == null || _bodyRenderers.Length == 0)
        {
            Transform body = bee != null ? FindDeep(bee.transform, "Body") : null;
            if (body == null) return;
            _bodyRenderers = body.GetComponentsInChildren<Renderer>(true);
        }
        foreach (Renderer r in _bodyRenderers)
            if (r != null && r.enabled != visible) r.enabled = visible;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).name == name) return root.GetChild(i);
            Transform d = FindDeep(root.GetChild(i), name);
            if (d != null) return d;
        }
        return null;
    }

    // =====================================================================
    // Input
    // =====================================================================
    private void ReadHotkeys()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        // Che do simulator: W/S/A/D/Q/E + Space/Shift xu ly trong UpdateSimulatorMovement().
        if (scheme == ControlScheme.BeeFly)
        {
            // Shift giu: dap canh lien tuc.
            bee.ExternalFlapOverride = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;

            // Q / E: leo len - lao xuong truc tiep (khong can nghieng mui).
            bool up = k.eKey.isPressed;
            bool down = k.qKey.isPressed;
            bee.ExternalVerticalThrust = (up ? verticalThrust : 0f) - (down ? verticalThrust : 0f);

            if (up) bee.AddPitch(verticalPitchPerSecond * Time.deltaTime);
            else if (down) bee.AddPitch(-verticalPitchPerSecond * Time.deltaTime);
        }
        else
        {
            bee.ExternalVerticalThrust = 0f;
        }

        // F: doi goc ninh ong / sau lung ong
        if (k.fKey.wasPressedThisFrame) _thirdPerson = !_thirdPerson;

        // R: dua ong ve diem bat dau
        if (k.rKey.wasPressedThisFrame)
        {
            _vel = Vector3.zero;
            _nosePitch = 0f;
            bee.Teleport(startPos, 0f);
            Debug.Log("[BeeDesktopTestMode] Da dua ong ve " + startPos);
        }
    }

    private Vector3 startPos;

    private void ReadMouseLook()
    {
        Mouse m = Mouse.current;
        if (m == null) return;

        Vector2 d = m.delta.ReadValue();
        _orbitYaw += d.x * mouseSensitivity * 0.05f;
        _orbitPitch = Mathf.Clamp(_orbitPitch - d.y * mouseSensitivity * 0.05f, -70f, 80f);
    }

    // =====================================================================
    // DEVICE SIMULATOR MOVEMENT
    //   W/S tien-lui, A/D di ngang, Q/E len-xuong (theo goc nhin camera).
    // =====================================================================
    private void UpdateSimulatorMovement()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
        float z = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
        float up = (k.eKey.isPressed ? 1f : 0f) - (k.qKey.isPressed ? 1f : 0f);
        bool flapping = k.spaceKey.isPressed || k.leftShiftKey.isPressed;

        float dt = Time.deltaTime;
        float camYaw = cam.transform.eulerAngles.y;
        Vector3 want = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(x, 0f, z);

        // ---- 1. CHUYEN DONG NGANG: tang toc dan, tha phim thi truot ----
        Vector3 hv = new Vector3(_vel.x, 0f, _vel.z);
        if (want.sqrMagnitude > 0.001f)
        {
            hv = Vector3.MoveTowards(hv, want.normalized * horizontalMaxSpeed, moveAccel * dt);
        }
        else if (flapping)
        {
            hv = Vector3.MoveTowards(hv, hv.normalized * horizontalMaxSpeed, flapForward * dt);
        }
        else
        {
            hv = Vector3.MoveTowards(hv, Vector3.zero, horizontalDrag * dt);
        }

        // ---- 2. CHUYEN DONG DOC: leo cham, tha phim thi TE (roi nhanh) ----
        float vy = _vel.y;
        if (up > 0.01f)
        {
            vy = Mathf.Min(vy + verticalAccel * dt, verticalMaxSpeed);   // leo
        }
        else if (up < -0.01f)
        {
            vy = Mathf.Max(vy - verticalAccel * dt, -verticalMaxSpeed);  // nhin xuong
        }
        else if (flapping)
        {
            // Dang dap canh: luc day manh gap trong luoc -> ong treo lai / leo duoc,
            // day toi phia truoc nhu ong that.
            vy += flapLift * dt;
            hv = Vector3.MoveTowards(hv,
                (hv.sqrMagnitude > 0.01f ? hv.normalized : Quaternion.Euler(0f, camYaw, 0f) * Vector3.forward)
                * horizontalMaxSpeed,
                flapForward * dt);
        }
        else
        {
            // THA PHIM -> ong nhua roi roi nhanh hon roi: THE MUI + LAO XUONG.
            vy -= diveGravity * dt;
        }

        _vel = new Vector3(hv.x, vy, hv.z);
        bee.transform.position += _vel * dt;

        // ---- 3. MUI ONG: nhung khi leo, cui mui khi lao xuong ----
        Vector3 lookDir = new Vector3(hv.x, 0f, hv.z);
        if (lookDir.sqrMagnitude < 0.01f) lookDir = bee.transform.forward;
        float yaw = Quaternion.LookRotation(lookDir.normalized, Vector3.up).eulerAngles.y;

        float targetNose = Mathf.Clamp(vy * noseAnglePerSpeed, -60f, 45f);
        _nosePitch = Mathf.Lerp(_nosePitch, targetNose, 1f - Mathf.Exp(-noseSmooth * dt));
        bee.SetOrientation(yaw, _nosePitch);

        // ---- 4. CANH: Space/Shift = CÚ BÁM MẠNH (vỗ nhanh, biên độ lớn).
        // Di chuyển bình thường thì ong chỉ vỗ nhẹ -> nhìn ra sự khác biệt khi bấm.
        bool moving = want.sqrMagnitude > 0.001f || Mathf.Abs(up) > 0.001f;
        bee.ExternalFlapOverride = flapping;

        BeeFlapping flappingFx = bee.GetComponent<BeeFlapping>();
        if (flappingFx != null)
        {
            flappingFx.ExternalSpeedRatio = _vel.magnitude / Mathf.Max(0.01f, horizontalMaxSpeed);
            // Dam manh = 1 khi bam; 0.4 khi dang bay/chuyen dong; 0 khi dung.
            flappingFx.ExternalFlapBoost = flapping ? 1f : (moving ? 0.4f : 0f);
        }
    }
    // =====================================================================
    // HELPERS
    // =====================================================================

    private static Camera CreateCamera()
    {
        GameObject go = new GameObject("DesktopTestCamera");
        go.tag = "MainCamera";
        Camera c = go.AddComponent<Camera>();
        c.nearClipPlane = 0.02f;
        go.AddComponent<AudioListener>();
        Debug.Log("[BeeDesktopTestMode] Scene chua co camera -> da tao 'DesktopTestCamera'.");
        return c;
    }

    /// <summary>Tat component theo ten lop (khong can reference vi kieu du lieu).</summary>
    private void Disable(string typeName)
    {
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour b in all)
        {
            if (b == null || b == this) continue;
            if (b.GetType().Name != typeName) continue;
            if (!_disabledBehaviours.Contains(b))
            {
                _disabledBehaviours.Add(b);
                b.enabled = false;
                Debug.Log("[BeeDesktopTestMode] Da tam tat: " + typeName + " (" + b.gameObject.name + ")");
            }
        }
    }

    /// <summary>Tat TrackedPoseDriver (XRI) tren camera va cha cua no - khong co kinh thi vo ich.</summary>
    private void DisableTrackedPoseDrivers()
    {
        if (cam == null) return;
        Transform t = cam.transform;
        while (t != null)
        {
            MonoBehaviour[] bs = t.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour b in bs)
            {
                if (b == null) continue;
                if (!b.GetType().Name.Contains("TrackedPoseDriver")) continue;
                if (!_disabledBehaviours.Contains(b))
                {
                    _disabledBehaviours.Add(b);
                    b.enabled = false;
                }
            }
            t = t.parent;
        }
    }

    /// <summary>
    /// Neu scene khong co BeeFlightInput HOAC BeeController chua gan input
    /// (Steer/Flap rong) thi tu tao input ao cho editor.
    /// Truong hop rong xay ra khi keo prefab Bee_Placeholder vao scene ma
    /// chua chay menu 'VU2/Bee/Setup Bee Flight Scene'.
    /// </summary>
    private void InstallKeyboardInputIfMissing()
    {
        if (bee == null) return;
        if (_installedActions) return;

        bool hasBeeFlightInput = FindObjectOfType<BeeFlightInput>() != null;
        bool actionsEmpty = bee.flapAction.action == null || bee.steerAction.action == null;

        if (hasBeeFlightInput && !actionsEmpty) return;   // da co input day du
        if (hasBeeFlightInput && actionsEmpty)
            Debug.LogWarning("[BeeDesktopTestMode] BeeFlightInput co san nhung Steer/Flap dang RONG " +
                             "-> dung input ao WASD/Space thay the.");

        _steer = new InputAction("BeeSteerDesktop", InputActionType.Value, "<Keyboard>/a,d,w,s");
        _flap = new InputAction("BeeFlapDesktop", InputActionType.Button, "<Keyboard>/space");
        _steer.Enable();
        _flap.Enable();

        bee.steerAction = new InputActionProperty(_steer);
        bee.flapAction = new InputActionProperty(_flap);
        _installedActions = true;
        Debug.Log("[BeeDesktopTestMode] Da bat input ao: WASD + Space (Space = dap canh).");
    }

    private void Restore()
    {
        foreach (Behaviour b in _disabledBehaviours)
            if (b != null) b.enabled = true;
        _disabledBehaviours.Clear();

        if (_installedActions)
        {
            _steer?.Disable(); _flap?.Disable();
            _steer?.Dispose(); _flap?.Dispose();
            _steer = null; _flap = null;
            _installedActions = false;
        }

        if (bee != null)
        {
            bee.ExternalFlapOverride = false;
            bee.ExternalVerticalThrust = 0f;
            bee.ExternalMovementLocked = false;
        }

        // Hien lai than ong (neu da an khi dung goc nhin ong).
        if (_bodyRenderers != null)
            foreach (Renderer r in _bodyRenderers)
                if (r != null) r.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowHelpOnce()
    {
        if (_helpShown) return;
        _helpShown = true;
        if (scheme == ControlScheme.DeviceSimulator)
        {
            Debug.Log("[BeeDesktopTestMode] ===== CHE DO XR DEVICE SIMULATOR =====\n" +
                      "  W/S : tien / lui      A/D : di ngang trai-phai\n" +
                      "  Q   : xuong            E   : len\n" +
                      "  Chuot: nhin quanh      F : doi goc ninh ong / sau lung\n" +
                      "  R : dua ong ve diem bat dau   Esc : tho che do test");
        }
        else
        {
            Debug.Log("[BeeDesktopTestMode] ===== CHE DO BAY ONG (VR) =====\n" +
                      "  W/S : nhung xuong      A/D : re trai-phai\n" +
                      "  E : leo len            Q : lao xuong\n" +
                      "  Space : dap canh       Shift : dap canh lien tuc\n" +
                      "  Chuot: nhin quanh      F : doi goc ninh ong / sau lung\n" +
                      "  R : dua ong ve diem bat dau   Esc : tho che do test");
        }
    }

    private void OnGUI()
    {
        if (!showHelpOverlay) return;
        GUI.Label(new Rect(10, 10, 620, 20),
            (scheme == ControlScheme.DeviceSimulator)
                ? "SIMULATOR - W/S: tien-lui | A/D: di ngang | Q: xuong | E: len | Chuot: nhin"
                : "BEE FLY - W/S: pitch | A/D: yaw | E: len | Q: xuong | Space: flap");
        GUI.Label(new Rect(10, 28, 620, 20),
            "Chuot: nhin quanh | F: doi goc nhin | R: reset | Esc: tho che do test");
    }
}