// =============================================================================
// BeeCameraRig - FIRST-PERSON VR rig cho vai con ong (giống ảnh mẫu: Bee POV).
// BeeCameraRig - Rig VR goc nhin thu nhat cho nhan vat ong.
//
// VI SAO KHONG DUNG VAO POSE DA TRACK CUA CAMERA
//   Camera trong XR Origin duoc XR dat vao moi frame tu head tracking.
//   Neu ta gan thang position/rotation cho camera thi head tracking bi pha vo
//   (chong mat, troi khoi dau). Cach DUNG la:
//     -> DICH CHUYEN XR Origin (player rig) sao cho camera toi dung vi tri
//        "mat" cua con ong. Camera giu nguyen local pose da track.
//     -> Xoay XR Origin theo yaw/pitch/bank cua con ong.
//   Ket qua: nguoi choi van nhin quanh dau tu do, ma the gioi van bay theo ong.
//
// HIERARCHY KHUYEN NGHI
//   XR Origin                  <- playerRig (gan vao day) + BeeController
//   |-- Camera Offset
//   |   `-- Main Camera        <- trackedCamera (KHONG bi script nay set transform)
//   `-- BeeVisual              <- visualRoot (Body + Wing_L + Wing_R)
//
//   HOAC gan BeeController + script nay len chinh XR Origin: script tu phat hien
//   (playerRig == transform) va khong dich chuyen gi them -> tranh vong lap.
//
// VE VIEC TAT XRI LOCOMOTION
//   ActionBasedContinuousMoveProvider / SnapTurn / Teleportation se giat nguoi
//   choi nguoc huong bay. Script tu tim va tat cac provider do, cung
//   CharacterController (dang bay thi khong can capsule).
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BeeCameraRig : MonoBehaviour
{
    /// <summary>Che do goc nhin. / Camera view mode.</summary>
    public enum ViewMode
    {
        /// <summary>Nguoi choi chinh la con ong (bee POV).</summary>
        FirstPersonBee = 0,

        /// <summary>Camera sau lung ong, dung de debug.</summary>
        ThirdPersonDrone = 1
    }

    // =========================================================================
    // VIEW MODE / CHE DO GOC NHIN
    // =========================================================================
    [Header("View Mode / Che do goc nhin")]
    [Tooltip("FirstPersonBee = mat trong than ong (nhu anh mau).")]
    public ViewMode viewMode = ViewMode.FirstPersonBee;

    [Tooltip("FOV cua camera khi choi. Rong (~82) cho cam giac nho be nhu ong.")]
    [Range(50f, 110f)]
    public float targetFov = 82f;


    // =========================================================================
    // EYE OFFSET / VI TRI MAT CUA CON ONG
    // =========================================================================
    [Header("Eye Offset / Vi tri mat con ong")]
    [Tooltip("Vi tri 'mat' so voi BeeRoot (local). Cao hon than, hoi truoc mui.")]
    public Vector3 eyeOffset = new Vector3(0f, 0.08f, 0.16f);

    [Tooltip("Offset goc nhin (do) so voi huong bay. / Look angle offset.")]
    public Vector3 eyeEulerOffset = Vector3.zero;

    // =========================================================================
    // RIG MOTION / CACH XR ORIGIN THEO CON ONG
    // =========================================================================
    [Header("Rig Motion / Chuyen dong rig")]
    [Tooltip("Ap yaw cua ong len XR Origin. Luon nen bat.")]
    public bool applyYawToRig = true;

    [Tooltip("Ap pitch len XR Origin (the gioi nghieng khi ong len/xuong). " +
             "Tat neu thay chong mat - khi do chi nhin bang dau.")]
    public bool applyPitchToRig = false;

    [Tooltip("He so nhan pitch (0-1) de giam say khi bat ap pitch.")]
    [Range(0f, 1f)]
    public float pitchInfluence = 0.4f;

    [Tooltip("Ap bank (nghieng khi re) len XR Origin. Rat nang cao cam giac re.")]
    public bool applyBankToRig = true;

    [Range(0f, 1f)]
    public float bankInfluence = 0.55f;

    [Tooltip("Cui nhe xuong khi bay nhanh -> cam giac luot sat mat nuoc nhu anh mau.")]
    public bool applySpeedPitchDown = true;

    [Tooltip("Do cui xuong khi bay het toc (do). Nen <= 12 de khong gay say.")]
    [Range(0f, 25f)]
    public float speedPitchDown = 10f;

    [Tooltip("Bam mem vi tri rig. Tat = dich chuyen cung (khuyen nghi cho VR).")]
    public bool smoothRigPosition = false;

    [Range(1f, 60f)]
    public float rigSmoothSpeed = 40f;

    // =========================================================================
    // BODY BOB / LAC NHE THEO NHIP CANH
    // =========================================================================
    [Header("Body Bob / Lac nhe theo nhip canh")]
    [Tooltip("Bien do rung nhe khi dap canh (m). Rat nho la du tao cam giac.")]
    [Range(0f, 0.05f)]
    public float bobAmplitude = 0.008f;

    [Tooltip("Tan so rung (Hz) khi bay het toc.")]
    [Range(1f, 30f)]
    public float bobFrequency = 12f;

    [Tooltip("Them chut xoay lac theo canh de ong co cam giac 'thi giac'.")]
    [Range(0f, 5f)]
    public float bobRollAmplitude = 0.8f;

    // =========================================================================
    // FIRST PERSON VISUALS / HINH ANH GOC NHIN THU NHAT
    // =========================================================================
    [Header("First Person Visuals / Hinh anh goc nhin thu nhat")]
    [Tooltip("An than ong (chi hien canh o ria man hinh) nhu anh mau.")]
    public bool hideBodyInFirstPerson = true;

    [Tooltip("Giu canh hien o goc nhin. / Keep wings visible at screen edges.")]
    public bool showWingsInFirstPerson = true;

    [Tooltip("Offset vi tri than/canh so voi camera (local).")]
    public Vector3 visualOffset = new Vector3(0f, -0.13f, 0.30f);

    [Tooltip("Offset goc cua than so voi camera (do).")]
    public Vector3 visualEulerOffset = new Vector3(6f, 0f, 0f);

    [Tooltip("Tong do lech giua visualRoot va BeeRoot (m). Offset rig de bu chenh lech.")]
    [Range(0f, 2f)]
    public float visualFollowLag = 0.05f;

    [Tooltip("Ti le cua visualRoot bay theo rig (0 = dung hinh, 1 = bam chuyen dong).")]
    [Range(0f, 1f)]
    public float visualFollowScale = 1f;

    [Tooltip("Cho phep visualRoot bam rig ngay lap (khong noi suy).")]
    public bool visualInstantFollow = true;

    [Tooltip("Toc do noi suy FOV khi doi che do. / FOV lerp speed.")]
    [Range(1f, 20f)]
    public float fovSmoothSpeed = 6f;

    [Tooltip("FOM them khi bay het toc (mo rong gon nhin) -> cam giac toc do.")]
    [Range(0f, 25f)]
    public float fovSpeedBoost = 6f;

    // =========================================================================
    // REFERENCES / THAM CHIEU
    // =========================================================================
    [Header("References / Tham chieu")]
    [Tooltip("XR Origin (player rig). De trong se tu tim. / The XR Origin root.")]
    public Transform playerRig;

    [Tooltip("Main Camera da duoc XR track. Script KHONG ghi de transform cua no.")]
    public Camera trackedCamera;

    [Tooltip("BeeController dung de doc yaw/pitch/bank. De trong se tu tim.")]
    public BeeController bee;

    [Tooltip("Than/canh ong hien thi o goc nhin thu nhat. De trong se tu tim.")]
    public Transform visualRoot;

    // =========================================================================
    // INTERNAL STATE / TRANG THAI NOI BO
    // =========================================================================
    private readonly List<Behaviour> _disabledLocomotion = new List<Behaviour>();
    private Vector3 _cameraLocalOffset;
    private Quaternion _cameraLocalRotation;
    private Transform _bodyVisual;
    private Renderer[] _bodyRenderers;
    private Renderer[] _wingRenderers;
    private float _bobPhase;
    private Vector3 _bobOffset;
    private float _bobRollOffset;
    private float _currentFov;
    private Vector3 _rigVelocity;
    private bool _resolved;

    private void Awake()
    {
        ResolveReferences();
        _currentFov = trackedCamera != null ? trackedCamera.fieldOfView : targetFov;
    }

    private void OnEnable()
    {
        if (playerRig != null && playerRig != transform)
        {
            DisableLocomotion();
        }
    }

    // =========================================================================
    // PUBLIC API / HAM CONG KHAI
    // =========================================================================

    /// <summary>Dat nguoi choi (XR Origin) ve vi tri con ong.</summary>
    public void SnapToBee()
    {
        ResolveReferences();
        if (trackedCamera == null || playerRig == null) return;

        // Dùng offset đã chot ở ResolveReferences để camera về đúng "mắt" ong
        // mà vẫn giữ nguyên khoảng cách head-tracking mà người chơi đang có.
        playerRig.position = GetEyeWorldPosition() - playerRig.rotation * _cameraLocalOffset;
        trackedCamera.transform.rotation = playerRig.rotation * _cameraLocalRotation;
    }

    /// <summary>Chuyen doi nhanh giua first-person va third-person.</summary>
    public void SetViewMode(ViewMode mode)
    {
        viewMode = mode;
        ApplyBodyVisibility();
    }


    // =========================================================================
    // ResolveReferences - Tu tim moi thu thieu trong scene de test nhanh.
    // =============================================================================
    private void ResolveReferences()
    {
        if (bee == null) bee = GetComponent<BeeController>();

        if (trackedCamera == null)
        {
            trackedCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        if (playerRig == null && trackedCamera != null)
        {
            playerRig = FindPlayerRig(trackedCamera.transform);
        }

        if (visualRoot == null) visualRoot = transform.Find("BeeVisual");

        // Cache body/wings de dung lai moi frame (tranh Find() lap lai).
        _bodyVisual = visualRoot != null
            ? (visualRoot.Find("Body") != null ? visualRoot.Find("Body") : visualRoot)
            : null;
        if (_bodyVisual == null) _bodyVisual = transform.Find("Body");

        _bodyRenderers = _bodyVisual != null ? _bodyVisual.GetComponentsInChildren<Renderer>() : null;

        // CANH: chi lay renderer cua 2 pivot canh. Khong lay tat ca renderer
        // trong visualRoot, neu khong se bao gio cung bat lai than ong dang bi an.
        _wingRenderers = CollectWingRenderers();

        // Loai renderer cua canh ra khoi danh sach THAN. Canh nam duoi Body nen
        // GetComponentsInChildren o tren da gom luon canh -> neu khong loai, than
        // bi an se keo canh an theo.
        if (_bodyRenderers != null && _wingRenderers != null && _wingRenderers.Length > 0)
        {
            var keep = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in _bodyRenderers)
            {
                bool isWing = false;
                foreach (Renderer w in _wingRenderers)
                {
                    if (r == w) { isWing = true; break; }
                }
                if (!isWing) keep.Add(r);
            }
            _bodyRenderers = keep.ToArray();
        }

        if (trackedCamera != null)
        {
            // Chot offset local cua camera MOT LAN. Giữ nguyên offset này về sau
            // để không triệt tiêu room-scale (người chơi nghiêng người vẫn dịch được).
            _cameraLocalOffset = playerRig != null
                ? playerRig.InverseTransformPoint(trackedCamera.transform.position)
                : trackedCamera.transform.localPosition;
            _cameraLocalRotation = playerRig != null
                ? Quaternion.Inverse(playerRig.rotation) * trackedCamera.transform.rotation
                : trackedCamera.transform.localRotation;
        }

        _currentFov = trackedCamera != null ? trackedCamera.fieldOfView : targetFov;
        _resolved = true;

        ApplyBodyVisibility();
    }

    /// <summary>Gom renderer cua rieng hai pivot canh (Wing_L / Wing_R).</summary>
    private Renderer[] CollectWingRenderers()
    {
        if (visualRoot == null) return null;

        // Phai tim SAU moi cap: canh thuong nam duoi Body (BeeRoot/Body/Wing_L),
        // khong phai con truc tiep cua visualRoot.
        Transform wingL = FindDeep(visualRoot, "Wing_L");
        Transform wingR = FindDeep(visualRoot, "Wing_R");
        if (wingL == null && wingR == null) return null;

        var list = new System.Collections.Generic.List<Renderer>();
        if (wingL != null) list.AddRange(wingL.GetComponentsInChildren<Renderer>(true));
        if (wingR != null) list.AddRange(wingR.GetComponentsInChildren<Renderer>(true));
        return list.ToArray();
    }

    /// <summary>Tim child theo ten o moi cap (khong de quy vo han).</summary>
    private static Transform FindDeep(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;

            Transform deep = FindDeep(child, childName);
            if (deep != null) return deep;
        }
        return null;
    }

    /// <summary>Leo len cha de tim XR Origin. / Walk up to find the XR Origin.</summary>
    private static Transform FindPlayerRig(Transform camera)
    {
        Transform current = camera;
        while (current.parent != null)
        {
            current = current.parent;
            if (current.name.Contains("XR Origin") || current.name.Contains("XROrigin"))
            {
                return current;
            }
        }
        return camera.root;
    }

    /// <summary>Bat/tat hien thi than ong (dung khi vao menu).</summary>
    public void SetBodyVisible(bool visible)
    {
        if (_bodyRenderers == null) return;
        foreach (Renderer r in _bodyRenderers) r.enabled = visible;
    }

    private void LateUpdate()
    {
        if (!_resolved) ResolveReferences();
        if (trackedCamera == null || playerRig == null) return;

        // Bob phải chạy TRUOC vi GetTargetRigRotation dung _bobRollOffset.
        UpdateBob();
        UpdateRigPose();
        UpdateFov();
        UpdateFirstPersonVisual();
    }

    // =========================================================================
    // UpdateRigPose - Dua XR Origin sao cho CAMERA (khong phai transform cua
    // camera) den dung diem "mat" cua con ong.
    //
    //   rig.position = eyeWorld - rig.rotation * cameraLocalOffset
    //
    // Nho giu nguyen cameraLocalOffset da chot o ResolveReferences nen viec
    // nguoi choi nheo nguoi trong phong van duoc giu nguyen (room-scale).
    // =============================================================================
    private void UpdateRigPose()
    {
        // Neu script nam tren chinh XR Origin thi BeeController da lo het.
        if (playerRig == transform) return;

        Quaternion targetRotation = GetTargetRigRotation();
        playerRig.rotation = targetRotation;

        Vector3 targetPosition = GetEyeWorldPosition() - targetRotation * _cameraLocalOffset;

        if (smoothRigPosition)
        {
            float t = 1f - Mathf.Exp(-rigSmoothSpeed * Time.deltaTime);
            targetPosition = Vector3.Lerp(playerRig.position, targetPosition, t);
        }

        // _rigVelocity giu lai de tien cho hieu ung rut lien tuong lai
        // (tuy chon cho sau; hien tai chi de debug).
        _rigVelocity = (targetPosition - playerRig.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        playerRig.position = targetPosition;
    }

    /// <summary>Goc quay cua XR Origin: yaw cua ong + pitch/bank tuy chon.</summary>
    private Quaternion GetTargetRigRotation()
    {
        float yaw = bee != null ? bee.Yaw : transform.eulerAngles.y;
        float pitch = bee != null ? bee.Pitch : 0f;
        float bank = bee != null ? bee.Bank : 0f;

        if (!applyYawToRig) yaw = transform.eulerAngles.y;
        if (!applyPitchToRig) pitch = 0f;
        if (!applyBankToRig) bank = 0f;

        // DAU CUA PITCH / PITCH SIGN
        //   BeeController.Pitch duong = BAY LEN, nhung Quaternion.Euler(x duong)
        //   trong Unity lai la CUI XUONG. Vi vay phai dao dau truoc khi ap len rig,
        //   neu khong nguoi choi se thay the gioi nghieng nguoc huong bay.
        pitch = -pitch * pitchInfluence;

        // Cui them khi bay nhanh (huong duong = cui xuong) -> cam giac luot
        // sat mat nuoc / lao xuong nhu anh mau.
        if (applySpeedPitchDown && bee != null)
        {
            pitch += bee.SpeedNormalized * speedPitchDown;
        }

        bank *= bankInfluence;
        bank += _bobRollOffset;

        return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, bank);
    }

    // =========================================================================
    // GetEyeWorldPosition - Diem "mat" cua con ong trong world space.
    //   Gom ca rung nhe theo nhip canh (_bobOffset) de tao cam giac "thinh giac".
    // =============================================================================
    private Vector3 GetEyeWorldPosition()
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        eye += _bobOffset;
        return eye;
    }

    // =========================================================================
    // UpdateBob - Rung nhe theo nhip canh (doc theo pha cua BeeFlapping).
    //   Bien do cực nho (<= 2cm) nen khong gay say nhung lai "nhip thoi".
    // =============================================================================
    private void UpdateBob()
    {
        bool flapping = bee != null && bee.IsFlapping;
        float speedNorm = bee != null ? bee.SpeedNormalized : 0f;

        // Dung tan so goc voi BeeFlapping de hai hieu ung khop pha.
        float frequency = bobFrequency * Mathf.Lerp(0.55f, 1f, speedNorm);
        if (flapping)
        {
            _bobPhase += frequency * Time.deltaTime * Mathf.PI * 2f;
            if (_bobPhase > Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;
        }

        float amplitude = flapping ? bobAmplitude : bobAmplitude * 0.25f;
        float wave = Mathf.Sin(_bobPhase);

        // Trục lên/down lấy theo hướng nhìn để lắc đúng chiều khi nghiêng đầu.
        Vector3 up = trackedCamera.transform.up;
        _bobOffset = up * (wave * amplitude);
        _bobRollOffset = wave * bobRollAmplitude;
    }

    // =========================================================================
    // UpdateFov - FOV rong hon khi bay nhanh, tạo cảm giác tốc độ.
    // =============================================================================
    private void UpdateFov()
    {
        float speedNorm = bee != null ? bee.SpeedNormalized : 0f;
        float desired = targetFov + speedNorm * fovSpeedBoost;

        _currentFov = Mathf.Lerp(_currentFov, desired,
            1f - Mathf.Exp(-fovSmoothSpeed * Time.deltaTime));

        if (!Mathf.Approximately(trackedCamera.fieldOfView, _currentFov))
        {
            trackedCamera.fieldOfView = _currentFov;
        }
    }

    // =========================================================================
    // UpdateFirstPersonVisual - Dam than/canh ong o ria man hinh nhu anh mau.
    //   visualRoot duoc dat truoc + thap hon camera, nen nguoi choi thay
    //   khoi hai canh dap o goc nhin ma van thay duoc phan chan trung tinh.
    // =============================================================================
    private void UpdateFirstPersonVisual()
    {
        if (visualRoot == null) return;

        if (viewMode == ViewMode.ThirdPersonDrone)
        {
            // Third person: visualRoot di chuyen nhu mot object binh thuong.
            visualRoot.SetPositionAndRotation(transform.position, transform.rotation);
            return;
        }

        // First person: gan visualRoot theo he toa do CAMERA de no luon
        // nằm đúng chỗ trên màn hình dù người chơi có nghiêng đầu.
        Vector3 targetPosition = trackedCamera.transform.TransformPoint(visualOffset);

        if (visualInstantFollow)
        {
            visualRoot.position = targetPosition;
        }
        else
        {
            float t = visualFollowLag > 0.0001f
                ? 1f - Mathf.Exp(-1f / visualFollowLag * Time.deltaTime)
                : 1f;
            visualRoot.position = Vector3.Lerp(visualRoot.position, targetPosition,
                t * visualFollowScale);
        }

        // Goc: tron giua huong bay cua ong va huong camera de canh khong "dinh".
        Quaternion cameraRotation = trackedCamera.transform.rotation * Quaternion.Euler(visualEulerOffset);
        visualRoot.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, 0.5f);
    }

    // =========================================================================
    // ApplyBodyVisibility - First person: an than, chi giu canh o ria man hinh.
    // =============================================================================
    private void ApplyBodyVisibility()
    {
        bool firstPerson = viewMode == ViewMode.FirstPersonBee;

        if (_bodyRenderers != null)
        {
            bool visible = !firstPerson || !hideBodyInFirstPerson;
            foreach (Renderer r in _bodyRenderers) r.enabled = visible;
        }

        if (_wingRenderers != null)
        {
            bool visible = !firstPerson || showWingsInFirstPerson;
            foreach (Renderer r in _wingRenderers) r.enabled = visible;
        }
    }

    // =========================================================================
    // DisableLocomotion - Tat locomotion cua XRI de khong keo nguoi choi
    // nguoc huong bay (ContinuousMove / SnapTurn / Teleportation), va tat
    // CharacterController vi dang bay khong can capsule.
    // =============================================================================
    private void DisableLocomotion()
    {
        if (playerRig == null) return;

        MonoBehaviour[] behaviours = playerRig.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            if (behaviour is BeeController || behaviour is BeeFlapping) continue;

            string typeName = behaviour.GetType().Name;
            bool isLocomotion =
                typeName.Contains("MoveProvider") ||
                typeName.Contains("TurnProvider") ||
                typeName.Contains("TeleportationProvider") ||
                typeName.Contains("NavigationProvider") ||
                typeName.Contains("CharacterControllerDriver");

            if (isLocomotion)
            {
                behaviour.enabled = false;
                _disabledLocomotion.Add(behaviour);
            }
        }

        // Dang bay: bo capsule de tranh vam vao vat the.
        CharacterController controller = playerRig.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }

    // =========================================================================
    // ApplyBeePovPreset - Dat lai thong so cho "goc nhin con ong nhu anh mau".
    //   Anh mau: FOV rong, mat nuoc ngay duoi tam nhin, ong luot sat mat nuoc,
    //   nghien nhe khi re, cam giac toc do ro nhung KHONG gay say.
    //
    //   Goi tu menu VU2/Bee/Apply Bee POV Preset, hoac tu BeeFlightSceneSetup.
    // =============================================================================
    public void ApplyBeePovPreset()
    {
        // --- Che do nhin: chinh nguoi choi la con ong ---
        viewMode = ViewMode.FirstPersonBee;

        // --- Khung nhin: rong de thay ca mat nuoc + hoa sen hai ben nhu anh ---
        targetFov = 82f;
        fovSpeedBoost = 6f;
        fovSmoothSpeed = 6f;

        // --- Rig theo ong: uu tien chong say ---
        applyYawToRig = true;
        applyPitchToRig = false;   // chi nhin bang dau -> an toan nhat trong VR
        pitchInfluence = 0.35f;
        applyBankToRig = true;
        bankInfluence = 0.5f;
        applySpeedPitchDown = true;
        speedPitchDown = 10f;
        smoothRigPosition = false;

        // --- Rung nhe khi dap canh (nho la du tao cam giac "nhip thoi") ---
        bobAmplitude = 0.008f;
        bobFrequency = 12f;
        bobRollAmplitude = 0.8f;

        // --- An than, giu 2 canh o day khung nhin nhu anh mau ---
        hideBodyInFirstPerson = true;
        showWingsInFirstPerson = true;
        visualOffset = new Vector3(0f, -0.12f, 0.34f);
        visualEulerOffset = new Vector3(8f, 0f, 0f);
        visualFollowLag = 0.05f;
        visualFollowScale = 1f;
        visualInstantFollow = true;

        // --- Mat ong: hoi cao va truoc mui mot chut ---
        eyeOffset = new Vector3(0f, 0.06f, 0.16f);
        eyeEulerOffset = Vector3.zero;

        // --- Chinh BeeController: re nhe hon, giu cam giac "em" ---
        if (bee == null) bee = GetComponent<BeeController>();
        if (bee != null)
        {
            bee.maxBankAngle = 18f;
            bee.speedTurnPenalty = 0.55f;
        }
    }

    // =========================================================================
    // OnDestroy - Tra lai locomotion neu bi tat nham.
    // =============================================================================
    private void OnDestroy()
    {
        foreach (Behaviour behaviour in _disabledLocomotion)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        _disabledLocomotion.Clear();
    }
}
