using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using LetItBee.HoneyPilot;

namespace LetItBee.UI
{
    [DefaultExecutionOrder(-300)]
    public sealed class BeeMainMenu : MonoBehaviour
    {
        [Header("View (physical meters)")]
        [Min(0.5f)] public float viewingDistance = 1.5f;
        [Min(0.5f)] public float panelWidth = 1.5f;
        [Header("Menu Music")]
        public AudioSource menuMusic;
        [Header("Connection")]
        [Min(5f)] public float connectionTimeout = 25f;
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRay, rightRay;
        public LetItBeeMenuController cards;
        public TMP_Text statusText;
        public GameObject detailsPanel;
        public TMP_Text detailsText;
        public Button backButton, retryButton, volumeDownButton, volumeUpButton;
        public TMP_Text volumeText;
        public enum MenuState { Home, Connecting, ConnectionFailed, Settings, Study, Releasing, Playing }
        public MenuState State { get; private set; }
        BeeHoneyPilot pilot;
        Canvas canvas;
        Camera head;
        float deadline, releasedFor;
        bool positionedForTracking;
        BeeSwipeFlap swipe;
        bool previousBars;
        readonly Dictionary<Canvas, bool> hudCanvases = new Dictionary<Canvas, bool>();
        readonly Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader> originalReaders = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader>();

        void Awake() { BeeMenuInput.Blocked = true; }
        IEnumerator Start()
        {
            canvas = GetComponent<Canvas>(); head = Camera.main;
            pilot = FindFirstObjectByType<BeeHoneyPilot>();
            swipe = FindFirstObjectByType<BeeSwipeFlap>();
            if (swipe != null) { previousBars = swipe.showBars; swipe.showBars = false; }
            cards.onStoryMode.AddListener(SelectStory);
            cards.onStudyMode.AddListener(SelectStudy);
            cards.onSettings.AddListener(SelectSettings);
            backButton.onClick.AddListener(ShowHome);
            retryButton.onClick.AddListener(SelectStory);
            volumeDownButton.onClick.AddListener(() => ChangeVolume(-0.1f));
            volumeUpButton.onClick.AddListener(() => ChangeVolume(0.1f));
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("LetItBee.MasterVolume", AudioListener.volume));
            if (menuMusic != null && menuMusic.clip != null) menuMusic.Play();
            BindRay(leftRay, "LeftHand"); BindRay(rightRay, "RightHand");
            ShowHome();
            yield return null; // Let miniature rig scaling and HUD construction finish.
            PlaceInFront();
        }
        public void PlaceInFront()
        {
            if (head == null) head = Camera.main;
            if (head == null) return;
            canvas.worldCamera = head;
            float scale = Mathf.Max(0.01f, head.transform.lossyScale.x);
            Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            transform.SetPositionAndRotation(head.transform.position + forward * viewingDistance * scale, Quaternion.LookRotation(forward));
            transform.localScale = Vector3.one * (panelWidth * scale / 1024f);
        }
        void Update()
        {
            if (canvas == null || State == MenuState.Playing) return;
            if (!positionedForTracking) {
                var hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked) {
                    PlaceInFront(); positionedForTracking = true;
                }
            }
            if (State == MenuState.Connecting) {
                if (pilot != null && pilot.Live && pilot.State != null) { State = MenuState.Releasing; releasedFor = 0f; statusText.text = "Ready. Release both triggers and grips to start."; return; }
                else if (Time.unscaledTime >= deadline) {
                    State = MenuState.ConnectionFailed; retryButton.gameObject.SetActive(true);
                    detailsText.text = "Connection unavailable\nCheck that SIMPLE and GAMA are running on your PC and Quest shares the same network.\nYour progress has not been reset.";
                }
                statusText.text = pilot == null ? "Gameplay component is missing." : pilot.ConnectionStatus;
            }
            if (State == MenuState.Releasing) {
                if (pilot == null || !pilot.Live) { State = MenuState.Connecting; deadline = Time.unscaledTime + connectionTimeout; return; }
                statusText.text = "Ready. Release both triggers and grips to start.";
                releasedFor = ControlsReleased() ? releasedFor + Time.unscaledDeltaTime : 0f;
                if (releasedFor >= 0.25f) EnterGame();
            }
        }
        void LateUpdate()
        {
            if (!BeeMenuInput.Blocked) return;
            foreach (var hud in FindObjectsByType<BeeHud>(FindObjectsSortMode.None))
                foreach (var c in hud.GetComponentsInChildren<Canvas>(true)) {
                    if (!hudCanvases.ContainsKey(c)) hudCanvases.Add(c, c.enabled);
                    c.enabled = false;
                }
        }
        public static bool ControlsReleased()
        {
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices) {
                if (!(device is UnityEngine.InputSystem.XR.XRController)) continue;
                foreach (string control in new[] { "trigger", "grip", "triggerPressed", "gripPressed" }) {
                    var axis = device.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>(control);
                    if (axis != null && axis.ReadValue() > 0.2f) return false;
                }
            }
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand }) {
                var d = InputDevices.GetDeviceAtXRNode(node);
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float t) && t > 0.2f) return false;
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float g) && g > 0.2f) return false;
            }
            return Keyboard.current == null || !(Keyboard.current.fKey.isPressed || Keyboard.current.gKey.isPressed || Keyboard.current.hKey.isPressed);
        }
        void SetPage(MenuState state)
        {
            State = state;
            bool home = state == MenuState.Home;
            cards.storyCard.gameObject.SetActive(home); cards.studyCard.gameObject.SetActive(home); cards.settingsCard.gameObject.SetActive(home);
            detailsPanel.SetActive(!home); backButton.gameObject.SetActive(!home);
            retryButton.gameObject.SetActive(false);
            bool settings = state == MenuState.Settings;
            volumeDownButton.gameObject.SetActive(settings); volumeUpButton.gameObject.SetActive(settings); volumeText.gameObject.SetActive(settings);
        }
        public void ShowHome() { SetPage(MenuState.Home); statusText.text = "Point at a card and press the trigger."; }
        public void SelectStory()
        {
            if (State == MenuState.Connecting || State == MenuState.Releasing || State == MenuState.Playing) return;
            SetPage(MenuState.Connecting); deadline = Time.unscaledTime + connectionTimeout;
            detailsText.text = "Story prototype\nConnecting to SIMPLE / GAMA...\nExisting hive progress will be restored.";
        }
        public void SelectStudy() { SetPage(MenuState.Study); detailsText.text = "Study Mode\nComing soon\nEcology experiments are not available in this build."; statusText.text = "Return to choose Story Mode."; }
        public void SelectSettings()
        {
            SetPage(MenuState.Settings);
            detailsText.text = "Controls\nSwipe the flight bar to fly. Left stick: move. Right stick: turn.\nLeft trigger: land / take off. Right trigger: collect nectar.\nRight grip: carry nectar; release at the hive to deposit.";
            UpdateVolumeText(); statusText.text = "Settings are saved on this device.";
        }
        void ChangeVolume(float delta) { AudioListener.volume = Mathf.Clamp01(AudioListener.volume + delta); PlayerPrefs.SetFloat("LetItBee.MasterVolume", AudioListener.volume); PlayerPrefs.Save(); UpdateVolumeText(); }
        void UpdateVolumeText() { volumeText.text = "Master volume: " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%"; }
        void EnterGame()
        {
            State = MenuState.Playing; canvas.enabled = false;
            GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>().enabled = false;
            RestoreGameplay();
        }
        void BindRay(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor ray, string hand)
        {
            if (ray == null || originalReaders.ContainsKey(ray)) return;
            originalReaders.Add(ray, ray.uiPressInput);
            var reader = new UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader("Menu press", "Menu press value", false,
                UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.InputAction);
            reader.inputActionPerformed.AddBinding("<XRController>{" + hand + "}/trigger");
            reader.inputActionValue.AddBinding("<XRController>{" + hand + "}/trigger");
            ray.uiPressInput = reader;
            reader.EnableDirectActionIfModeUsed();
        }
        void RestoreGameplay()
        {
            BeeMenuInput.Blocked = false;
            if (menuMusic != null) menuMusic.Stop();
            foreach (var pair in originalReaders) {
                if (pair.Key == null) continue;
                var reader = pair.Key.uiPressInput;
                reader.DisableDirectActionIfModeUsed();
                pair.Key.uiPressInput = pair.Value;
                reader.inputActionPerformed.Dispose(); reader.inputActionValue.Dispose();
            }
            originalReaders.Clear();
            if (swipe != null) swipe.showBars = previousBars;
            foreach (var pair in hudCanvases) if (pair.Key != null) pair.Key.enabled = pair.Value;
            hudCanvases.Clear();
        }
        void OnDisable() { RestoreGameplay(); }
    }
}