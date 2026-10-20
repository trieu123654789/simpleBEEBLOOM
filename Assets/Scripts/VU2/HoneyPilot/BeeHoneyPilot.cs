using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace BeeBloom.HoneyPilot
{
    [DisallowMultipleComponent, DefaultExecutionOrder(150)]
    public sealed class BeeHoneyPilot : MonoBehaviour
    {
        [Header("Scene references — flight/HUD settings are never changed")]
        public Transform beeRoot, rightHand, flowerAHead, flowerBHead, slot;
        public Animator flowerAAnimator, flowerBAnimator;
        public RuntimeAnimatorController pilotFlowerIdle;
        public Mesh honeyMesh;
        public Material honeyMaterial, ghostMaterial, cueMaterial;
        public Font labelFont;
        public Camera trackedCamera;
        public Renderer slotDecoration;
        public Transform slotMount;
        public Vector3 slotMountLocalPoint;

        [Header("GAMA server on the PC")]
        public bool useSimpleToolchain = true;
        [Tooltip("SIMPLE headset endpoint. On Quest, use the PC LAN address, not localhost.")]
        public string simpleEndpoint = "ws://127.0.0.1:8080";
        public string endpoint = "ws://127.0.0.1:6873";
        public string modelOnServer;
        public string checkpointDirectoryOnServer;
        public string sessionId;
        public string speciesId = "chrysanthemum-pilot";
        [Tooltip("Symbolic demo species ID, not a verified scientific identification.")]
        public string plantAId = "plant-a", plantBId = "plant-b";

        [Header("Interaction (metres / seconds)")]
        [Min(0.1f)] public float contactRadius = 1f;
        [Min(0.1f)] public float grabRadius = 1.2f;
        [Min(0.05f)] public float snapRadius = 0.35f;
        [Min(0.2f)] public float collectDuration = 1.2f;
        [Range(0.05f, 0.4f)] public float aimRadius = 0.22f;
        [Range(0.1f, 2f)] public float labelScale = 0.35f;
        public bool enableDesktopKeys = true;
        [Tooltip("Require left-trigger hold for flower contact and nectar collection. Grip pickup/release remains available while carrying or flying.")]
        public bool requirePositionHoldInVr = true;

        [Header("Flower landing — hold left trigger, release to leave")]
        public bool enableFlowerPerching = true;
        [Range(0.2f, 0.9f)] public float perchApproachRadius = 0.65f;
        [Tooltip("Eye height above the branch/leaf perch, in metres.")]
        [Min(0.1f)] public float perchHeight = 0.36f;
        [Range(0.05f, 0.5f)] public float perchSettleSpeed = 0.25f;
        [Min(0.1f)] public float perchDwell = 0.4f;
        [Tooltip("Optional branch/leaf anchors. When empty, use the nearest leaf tip in the flower rig.")]
        public Transform flowerAPerch, flowerBPerch;
        [Tooltip("Preferred minimum viewing distance in metres. Larger blossoms automatically add clearance without changing this saved value.")]
        [Min(0.2f)] public float perchViewDistance = 0.75f;
        [Header("Honey presentation — runtime only, no bee body parts")]
        public bool enhancedFeedback = true;
        [Range(0f, 1.5f)] public float feedbackIntensity = 1f;
        [Range(0f, 1f)] public float feedbackVolume = 0.45f;
        [Range(0f, 1f)] public float feedbackHaptics = 0.6f;
        public int PerchedFlower { get; private set; } = -1;
        public bool IsFlowerSettled => PerchedFlower >= 0 && Vector3.Distance(beeRoot.position, perchAnchor) < 0.04f;
        // A separate XR rig can place the tracked view away from BeeRoot. Use
        // the perceived player position for both local interaction and GAMA.
        public Vector3 InteractionPosition {
            get {
                var rig = beeRoot != null ? beeRoot.GetComponent<BeeCameraRig>() : null;
                return trackedCamera != null && (rig == null || rig.viewMode == BeeCameraRig.ViewMode.FirstPersonBee)
                    ? trackedCamera.transform.position : beeRoot.position;
            }
        }

        public bool Live { get; private set; }
        public bool Busy => pendingRequest != null;
        public string ConnectionStatus { get; private set; } = "Waiting for GAMA";
        public JObject State { get; private set; }
        public string LastReason { get; private set; } = "";
        public string EffectiveSimpleEndpoint { get; private set; } = "";
        public float Charge01 { get; private set; }
        public int ConfirmedPollinations => State == null ? 0 : ((bool)State["pollinated"][0] ? 1 : 0) + ((bool)State["pollinated"][1] ? 1 : 0);
        public int Stored => (int?)State?["stored"] ?? 0;
        public string ChunkState => (string)State?["chunk_state"] ?? "none";

        IHoneyClient client;
        CancellationTokenSource lifetime;
        JObject configuration, pendingRequest;
        RuntimeAnimatorController originalA, originalB;
        InputAction rightTrigger, rightGrip;
        readonly BeeHoneyGrip gripIntent = new BeeHoneyGrip();
        Vector3 releaseHand, releaseBee, releaseChunk;
        int contact = -1, visitedContact = -1, chargingFlower = -1;
        float contactDwell, collectTime;
        float touchDwell, retryAfter, failureUntil, nextHoverHaptic;
        string failureKind, failureHint;
        string lastInteractionTrace;
        string lastGripDeviceTrace, lastTriggerDeviceTrace;
        readonly System.Collections.Generic.List<UnityEngine.XR.InputDevice> rightInputDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        bool grabReady;
        readonly BeeFlowerGeometry[] flowerGeometry = new BeeFlowerGeometry[2];
        public const string InteractionVersion = "Honey interaction 3.2";
        BeeFlowerGeometry Geometry(int flower) => flowerGeometry[flower] ?? (flowerGeometry[flower] = new BeeFlowerGeometry(Head(flower)));
        Transform chunk, chargeVisual, ghost, pollen;
        LineRenderer tether;
        TextMesh labelA, labelB, labelSlot, labelChunk;
        AudioSource feedback;
        AudioClip collectSound, pollinateSound, depositSound;
        Vector3 lastChunkPosition;
        float pulseUntil;
        bool decorationWasEnabled;
        ParticleSystem pollenBurst;
        int reportedPollinationMask;
        Vector3 threadSource;
        float threadUntil;
        bool originalsCaptured;
        BeeController beeController;
        Vector3 perchAnchor;
        float perchTime, perchBlockedTime, previousPerchDistance;
        bool perchArmed = true;
        readonly Transform[][] perchSupports = new Transform[2][];
        string lastLoggedConnectionError;
        FlowerResponse responseA, responseB;
        LineRenderer landingRing;
        BeeHoneyFeedback honeyFeedback;
        bool pendingVisitPollinates;
        bool Enhanced => enhancedFeedback && honeyFeedback != null && honeyFeedback.Ready;
        readonly BeeWorldSync worldSync = new BeeWorldSync();
        public JObject WorldSyncStatus => worldSync.Acknowledgement;

        void OnEnable()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("BeeBloom.Simple.IsolatedChecks", false)) {
                UnityEditor.SessionState.EraseBool("BeeBloom.Simple.IsolatedChecks");
                ConnectionStatus = "Temporarily disabled for isolated SIMPLE checks";
                enabled = false;
                return;
            }
#endif
            if (beeRoot == null || rightHand == null || flowerAHead == null || flowerBHead == null || slot == null) {
                ConnectionStatus = "Missing pilot scene references"; enabled = false; return;
            }
            originalA = flowerAAnimator != null ? flowerAAnimator.runtimeAnimatorController : null;
            beeController = beeRoot.GetComponent<BeeController>();
            gripIntent.Reset();
            if (beeController != null) beeController.RightHandInteractionOwnsInput = true;
            responseA = new FlowerResponse(flowerAHead);
            responseB = new FlowerResponse(flowerBHead);
            originalB = flowerBAnimator != null ? flowerBAnimator.runtimeAnimatorController : null;
            originalsCaptured = true;
            if (slotDecoration != null) { decorationWasEnabled = slotDecoration.enabled; slotDecoration.enabled = false; }
            // Runtime only; originals and their saved Animator settings remain intact.
            if (pilotFlowerIdle != null) {
                if (flowerAAnimator != null) flowerAAnimator.runtimeAnimatorController = pilotFlowerIdle;
                if (flowerBAnimator != null) flowerBAnimator.runtimeAnimatorController = pilotFlowerIdle;
            }
            rightTrigger = new InputAction("HoneyPilotTrigger", InputActionType.Value, "<XRController>{RightHand}/trigger");
            rightGrip = new InputAction("HoneyPilotGrip", InputActionType.Value, "<XRController>{RightHand}/grip");
            rightGrip.AddBinding("<XRController>{RightHand}/gripPressed");
            rightTrigger.Enable(); rightGrip.Enable();
            BuildViews();
            Debug.Log("[BeeBloom] " + InteractionVersion + "; hold grip to carry, release to place");
            UpdateSlotPose();
            try { configuration = BuildConfiguration(); }
            catch (Exception ex) { ConnectionStatus = ex.Message; enabled = false; return; }
            lifetime = new CancellationTokenSource();
            _ = Pump(lifetime.Token);
        }

        public JObject BuildConfiguration()
        {
            if (!Guid.TryParse(sessionId, out _)) throw new InvalidOperationException("Pilot session ID is missing or invalid");
            string checkpoint = checkpointDirectoryOnServer.TrimEnd('/', '\\') + "/" + sessionId + ".json";
            return new JObject {
                ["session"] = sessionId, ["checkpoint"] = checkpoint.Replace('\\', '/'), ["world_source"] = worldSync.Source,
                ["flowers"] = new JArray(FlowerConfig("flower-a", plantAId, flowerAHead.position), FlowerConfig("flower-b", plantBId, flowerBHead.position)),
                ["slot"] = new JObject { ["id"] = "slot-a", ["position"] = ToGama(slot.position) },
                ["contact_radius"] = contactRadius, ["grab_radius"] = grabRadius, ["snap_radius"] = snapRadius
            };
        }
        JObject FlowerConfig(string id, string plant, Vector3 position) => new JObject {
            ["id"] = id, ["plant_id"] = plant, ["species"] = speciesId, ["position"] = ToGama(position)
        };
        public static JArray ToGama(Vector3 value) => new JArray(Math.Round(value.x, 4), Math.Round(value.z, 4), Math.Round(value.y, 4));
        public static Vector3 FromGama(JToken value) => new((float)value[0], (float)value[2], (float)value[1]);

        async Task Pump(CancellationToken token)
        {
            DateTime profileCheck = DateTime.MinValue;
            DateTime nextWorldSync = DateTime.MinValue;
            while (!token.IsCancellationRequested) {
                try {
                    if (useSimpleToolchain && DateTime.UtcNow >= profileCheck) {
                        string resolved = BeeConnectionProfile.Resolve(simpleEndpoint);
                        profileCheck = DateTime.UtcNow.AddSeconds(1);
                        if (client != null && resolved != EffectiveSimpleEndpoint) {
                            Live = false;
                            client.Dispose(); client = null;
                        }
                        EffectiveSimpleEndpoint = resolved;
                    }
                    if (client == null) {
                        ConnectionStatus = useSimpleToolchain ? "Connecting through SIMPLE" : "Connecting to GAMA (legacy direct test)";
                        client = useSimpleToolchain ? new HoneySimpleClient(transform) : new HoneyGamaClient();
                        JObject first = await client.ConnectAsync(useSimpleToolchain ? EffectiveSimpleEndpoint : endpoint, modelOnServer, configuration);
                        if (token.IsCancellationRequested) break;
                        State = (JObject)first["state"]; Live = true; ConnectionStatus = useSimpleToolchain ? "LIVE · SIMPLE → LinkToUnity → GAMA" : "LIVE · direct test";
                        lastLoggedConnectionError = null;
                        Debug.Log("[BeeBloom] GAMA ready; session " + sessionId);
                        reportedPollinationMask = PollinationMask();
                        worldSync.Reconnect(); nextWorldSync = DateTime.MinValue;
                    }
                    await client.AskAsync("set_pose", new JObject { ["pose_json"] = new JObject {
                        ["bee"] = ToGama(InteractionPosition), ["hand"] = ToGama(rightHand.position),
                        ["flowers"] = new JArray(ToGama(flowerAHead.position), ToGama(flowerBHead.position)),
                        ["nectar_positions"] = new JArray(ToGama(NectarPosition(0)), ToGama(NectarPosition(1))),
                        ["flower_surfaces"] = new JArray(ToGama(Geometry(0).Surface), ToGama(Geometry(1).Surface)),
                        ["flower_radii"] = new JArray(Geometry(0).Radius, Geometry(1).Radius),
                        ["slot"] = ToGama(slot.position)
                    }.ToString(Newtonsoft.Json.Formatting.None) });
                    if (DateTime.UtcNow >= nextWorldSync) {
                        var world = worldSync.Prepare(worldSync.Capture(this));
                        if (world != null) {
                            var rawWorldReply = await client.AskAsync("sync_world",new JObject {
                                ["world_json"] = world.ToString(Newtonsoft.Json.Formatting.None) });
                            var worldReply = rawWorldReply as JObject ?? await client.SnapshotAsync();
                            worldSync.Accept((JObject)worldReply["world_sync"]);
                        }
                        nextWorldSync = DateTime.UtcNow.AddMilliseconds(250);
                    }
                    JObject request = pendingRequest;
                    if (request != null) await client.AskAsync("transact", new JObject { ["request_json"] = request.ToString(Newtonsoft.Json.Formatting.None) });
                    JObject snapshot = await client.SnapshotAsync();
                    if (token.IsCancellationRequested) break;
                    State = (JObject)snapshot["state"];
                    if (request != null && (string)snapshot["response"]?["event_id"] == (string)request["event_id"]) {
                        CompleteRequest(request,(JObject)snapshot["response"]);
                    }
                    await Task.Delay(100, token);
                } catch (OperationCanceledException) { break; }
                catch (Exception ex) {
                    if (token.IsCancellationRequested) break;
                    Live = false; ConnectionStatus = ex.Message;
                    if (lastLoggedConnectionError != ex.Message) {
                        lastLoggedConnectionError = ex.Message;
                        Debug.LogWarning("[BeeBloom] " + ConnectionStatus);
                    }
                    client?.Dispose(); client = null;
                    // Keep both the visible confirmed state and the SAME transaction ID.
                    // Reconnect reloads the GAMA checkpoint, then retries idempotently.
                    try { await Task.Delay(2000, token); } catch (OperationCanceledException) { break; }
                }
            }
            client?.Dispose(); client = null;
        }

        void CompleteRequest(JObject request, JObject response)
        {
            LastReason = (string)response["reason"];
            gripIntent.Complete((string)request["kind"], (bool)response["accepted"]);
            Debug.Log("[BeeBloom interaction] " + request["kind"] + " -> " + LastReason + "; chunk=" + ChunkState);
            if ((bool)response["accepted"]) Accepted((string)request["kind"], (int)request["flower"]);
            // A restored checkpoint can already contain this contact. Re-arming
            // that benign visit every frame prevents collection from getting a turn.
            if ((string)request["kind"] == "visit") {
                if (((bool)response["accepted"] || LastReason == "same_visit") && (int)request["flower"] == contact)
                    visitedContact = contact;
                else if (LastReason != "same_visit") { visitedContact = -1; retryAfter = Time.time + 0.5f; }
            }
            if (!(bool)response["accepted"] && LastReason != "same_visit") {
                failureKind = (string)request["kind"]; failureUntil = Time.time + 3f;
                failureHint = LastReason == "carrying_or_empty" ? "DA CO MAT - TIM CUC MAT DANG SANG"
                    : LastReason == "too_far_from_flower" ? "TOI GAN HOA HON ROI THU LAI"
                    : failureKind == "pick" ? "DUA TAY GAN MAT\nBOP LAI NUT BEN HONG PHAI"
                    : failureKind == "deposit" || failureKind == "drop" ? "CHUA DAT\nBOP LAI NUT BEN HONG PHAI"
                    : "CHUA NHAN THAO TAC - THU LAI";
            } else if (LastReason != "same_visit") failureUntil = 0f;
            pendingRequest = null;
        }

        void Update()
        {
            if (chunk == null) return;
            UpdateSlotPose();
            float trigger = ReadRightControl(false, rightTrigger?.ReadValue<float>() ?? 0f);
            float grip = ReadRightControl(true, rightGrip?.ReadValue<float>() ?? 0f);
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool desktopInteraction = false;
            if (enableDesktopKeys && Keyboard.current != null) {
                if (Keyboard.current.fKey.isPressed) { trigger = 1f; desktopInteraction = true; }
                if (Keyboard.current.gKey.isPressed) { grip = 1f; desktopInteraction = true; }
            }
            bool xrActive = XRSettings.isDeviceActive || device.isValid || rightTrigger?.activeControl != null;
            bool stable = !requirePositionHoldInVr || !xrActive || desktopInteraction || (beeController != null && beeController.IsPositionHolding);
            StepInteraction(trigger, grip, stable, (beeController != null && beeController.IsPositionHolding)
                || (enableDesktopKeys && Keyboard.current != null && Keyboard.current.hKey.isPressed), Time.deltaTime);
        }

        // OpenXR may expose squeeze as a digital button, an axis, or a separate
        // right-hand controller device. Never borrow input from the left hand.
        float ReadRightControl(bool isGrip, float actionValue)
        {
            float xrAxis = 0f, xrButton = 0f, systemAxis = 0f, systemButton = 0f;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightInputDevices);
            if (rightInputDevices.Count == 0) InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightInputDevices);
            foreach (var xr in rightInputDevices) {
                if (xr.TryGetFeatureValue(isGrip ? CommonUsages.grip : CommonUsages.trigger, out float axis)) xrAxis = Mathf.Max(xrAxis,axis);
                if (xr.TryGetFeatureValue(isGrip ? CommonUsages.gripButton : CommonUsages.triggerButton, out bool pressed) && pressed) xrButton = 1f;
            }
            foreach (var input in InputSystem.devices) {
                bool right = false;
                foreach (var usage in input.usages) if (usage.ToString() == "RightHand") right = true;
                if (!right) continue;
                var axis = input.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>(isGrip ? "grip" : "trigger");
                if (axis != null) systemAxis = Mathf.Max(systemAxis,axis.ReadValue());
                foreach (var control in input.allControls) {
                    bool matches = isGrip ? control.name == "gripPressed" || control.name == "gripButton" || control.name == "squeeze"
                        : control.name == "triggerPressed" || control.name == "triggerButton";
                    if (!matches) continue;
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed) systemButton = 1f;
                    else if (control is UnityEngine.InputSystem.Controls.AxisControl squeeze) systemAxis = Mathf.Max(systemAxis,squeeze.ReadValue());
                }
            }
            float value = Mathf.Clamp01(Mathf.Max(actionValue,Mathf.Max(Mathf.Max(xrAxis,xrButton),Mathf.Max(systemAxis,systemButton))));
            if (Debug.isDebugBuild) {
                string trace = "action=" + Mathf.RoundToInt(actionValue*10f) + "; XRaxis=" + Mathf.RoundToInt(xrAxis*10f)
                    + "; XRbutton=" + xrButton + "; ISaxis=" + Mathf.RoundToInt(systemAxis*10f) + "; ISbutton=" + systemButton;
                string previous = isGrip ? lastGripDeviceTrace : lastTriggerDeviceTrace;
                if (trace != previous) {
                    if (isGrip) lastGripDeviceTrace = trace; else lastTriggerDeviceTrace = trace;
                    Debug.Log("[BeeBloom input] " + (isGrip ? "grip " : "trigger ") + trace);
                }
            }
            return value;
        }

        void StepInteraction(float trigger, float grip, bool stable, bool holding, float dt)
        {
            if (gripIntent.Sample(grip)) {
                releaseHand = rightHand.position; releaseBee = InteractionPosition;
                // A release before pickup ACK refers to the intended hand pose.
                // Once carried, use the visible centre, not the controller pivot.
                releaseChunk = ChunkState == "held" && (!Enhanced || !honeyFeedback.PickupAnimating)
                    ? chunk.position : HeldPoint;
            }
            int near = NearestContact();
            if (near != contact) { contact = near; visitedContact = -1; contactDwell = touchDwell = 0f; }
            contactDwell += dt;
            StepFlowerPerching(dt, holding);
            bool deliberateContact = contact >= 0 && Geometry(contact).Distance(InteractionPosition) <= contactRadius && stable && ((IsFlowerSettled && PerchedFlower == contact)
                || Vector3.Distance(rightHand.position, NectarPosition(contact)) <= Mathf.Min(0.25f, grabRadius)
                || trigger >= 0.65f && AimedAt(NectarPosition(contact)));
            touchDwell = deliberateContact ? touchDwell + dt : 0f;
            grabReady = CanPickChunk();
            if (grabReady && Time.time >= nextHoverHaptic) {
                nextHoverHaptic = Time.time + 1.2f;
                if (Enhanced) honeyFeedback.HoverHoney();
            }
            // Release/pick input has priority over automatic visits and collection.
            string gripCommand = gripIntent.Choose(Live, Busy, ChunkState, grabReady,
                Vector3.Distance(releaseChunk, slot.position) <= snapRadius);
            if (gripCommand == "pick") Request("pick");
            else if (gripCommand != null) RequestAt(gripCommand, -1, releaseBee, releaseHand, releaseChunk);
            if (Live && !Busy && contact >= 0 && visitedContact != contact && touchDwell >= 0.4f && Time.time >= retryAfter)
                Request("visit", contact);
            bool canCollect = deliberateContact && visitedContact == contact && Live && !Busy && State != null && ChunkState == "none" && Stored == 0
                && (int)State["nectar"][contact] > 0;
            if (trigger >= 0.65f && canCollect) {
                if (chargingFlower != contact) { collectTime = 0f; chargingFlower = contact; }
                collectTime += dt;
                Charge01 = Mathf.Clamp01(collectTime / Mathf.Max(0.2f, collectDuration));
                if (Charge01 >= 1f) { Request("collect", contact); collectTime = 0f; Charge01 = 0f; }
            } else { collectTime = 0f; Charge01 = 0f; chargingFlower = -1; }
            string trace = "flower=" + contact + "; stable=" + stable + "; perched=" + IsFlowerSettled
                + "; touched=" + deliberateContact + "; grab=" + grabReady + "; chunk=" + ChunkState
                + "; grip=" + gripIntent.Pressed + "; trigger=" + (trigger >= 0.65f)
                + "; pending=" + ((string)pendingRequest?["kind"] ?? "none");
            if (trace != lastInteractionTrace) { lastInteractionTrace = trace; Debug.Log("[BeeBloom interaction] " + trace); }
            UpdateViews();
        }

        Vector3 HeldPoint => rightHand.TransformPoint(new Vector3(0f, 0.015f, 0.12f));
        bool CanPickChunk() => Live && (ChunkState == "ready" || ChunkState == "loose")
            && Vector3.Distance(rightHand.position, chunk.position) <= grabRadius
            && Vector3.Distance(InteractionPosition, chunk.position) <= 2f
            && (Vector3.Distance(rightHand.position, chunk.position) <= Mathf.Min(0.35f, grabRadius) || AimedAt(chunk.position))
            && (!Enhanced || !honeyFeedback.CollectionAnimating);

        void StepFlowerPerching(float dt, bool holding)
        {
            if (!holding) perchArmed = true;
            if (PerchedFlower >= 0) {
                if (!enableFlowerPerching || !holding || beeController == null || beeController.ExternalMovementLocked
                    || Vector3.Distance(beeRoot.position, perchAnchor) > Mathf.Max(1f, contactRadius + 0.2f)) {
                    LeaveFlower(); return;
                }
                Vector3 next = Vector3.MoveTowards(beeRoot.position, perchAnchor, Mathf.Max(0.05f, perchSettleSpeed) * dt);
                if (beeController.groundCollision) next = beeController.ResolveGroundCollision(next, Vector3.zero);
                if (beeController.maxFlightAltitude > 0f) next.y = Mathf.Min(next.y, beeController.maxFlightAltitude);
                beeRoot.position = next;
                float distance = Vector3.Distance(next, perchAnchor);
                perchBlockedTime = distance > 0.04f && previousPerchDistance - distance < 0.0001f ? perchBlockedTime + dt : 0f;
                previousPerchDistance = distance;
                if (perchBlockedTime > 0.5f) LeaveFlower();
                return;
            }
            if (!enableFlowerPerching || !perchArmed || !holding || contact < 0 || beeController == null
                || beeController.ExternalMovementLocked || beeController.IsPositionBraking || beeController.Speed > 0.25f
                || contactDwell < perchDwell) return;
            if (!TryGetPerchView(contact, out Vector3 support, out Vector3 eyeTarget)) return;
            if (Mathf.Min(Vector3.Distance(InteractionPosition, eyeTarget),
                Vector3.Distance(InteractionPosition, support)) > perchApproachRadius) return;
            Vector3 target = beeRoot.position + eyeTarget - InteractionPosition;
            if (beeController.groundCollision) {
                Vector3 safe = beeController.ResolveGroundCollision(target, Vector3.zero);
                // Allow the ground to raise the resting point, without pulling
                // through a wall or claiming to land outside the flower's range.
                if (Vector3.ProjectOnPlane(safe - target, Vector3.up).magnitude > 0.04f || safe.y < target.y - 0.04f) return;
                // A standing XR user can have a low body origin. If the ground
                // raises it, shorten the horizontal offset rather than losing
                // the perch or putting the tracked view outside server reach.
                Vector3 safeView = InteractionPosition + safe - beeRoot.position;
                Vector3 fromHead = safeView - Head(contact).position;
                float reach = (contactRadius + Geometry(contact).Radius) * 0.9f;
                if (Mathf.Abs(fromHead.y) >= reach) return;
                Vector3 horizontal = Vector3.ProjectOnPlane(fromHead, Vector3.up);
                float available = Mathf.Sqrt(reach * reach - fromHead.y * fromHead.y);
                if (horizontal.magnitude > available) {
                    if (available < Mathf.Min(0.4f, perchViewDistance)) return;
                    Vector3 adjusted = safe + horizontal.normalized * (available - horizontal.magnitude);
                    safe = beeController.ResolveGroundCollision(adjusted, Vector3.zero);
                    if (Vector3.ProjectOnPlane(safe - adjusted, Vector3.up).magnitude > 0.04f || safe.y < adjusted.y - 0.04f) return;
                }
                if (Geometry(contact).Distance(InteractionPosition + safe - beeRoot.position) > contactRadius * 0.95f) return;
                target = safe;
            }
            if (beeController.maxFlightAltitude > 0f && target.y > beeController.maxFlightAltitude) return;
            PerchedFlower = contact;
            perchArmed = false;
            perchAnchor = target; // Stable in world space; flower animation never drives the camera.
            previousPerchDistance = Vector3.Distance(beeRoot.position, target);
            perchBlockedTime = 0f;
            beeController.SetFlowerPerched(true);
            if (!Enhanced) PlayFeedback(collectSound, 0.08f);
        }

        // Only move the bee root. Freeze this world-space pose when landing begins;
        // an animated leaf or a physical head movement must never drag the camera.
        public bool TryGetPerchView(int flower, out Vector3 support, out Vector3 eyeTarget)
        {
            Transform head = Head(flower);
            support = eyeTarget = head.position;
            Transform anchor = flower == 0 ? flowerAPerch : flowerBPerch;
            if (anchor == null) {
                if (perchSupports[flower] == null) {
                    Transform rig = head;
                    while (rig.parent != null && rig.GetComponent<Animator>() == null) rig = rig.parent;
                    var tips = new System.Collections.Generic.List<Transform>();
                    foreach (var bone in rig.GetComponentsInChildren<Transform>(true))
                        if (bone.name.StartsWith("Leaf_") && bone.name.EndsWith("_end")) tips.Add(bone);
                    perchSupports[flower] = tips.ToArray();
                }
                float nearest = float.PositiveInfinity;
                foreach (var tip in perchSupports[flower]) {
                    if (tip == null) continue;
                    float distance = (tip.position - InteractionPosition).sqrMagnitude;
                    if (distance < nearest) { anchor = tip; nearest = distance; }
                }
            }
            support = anchor != null ? anchor.position : head.position;
            eyeTarget = support + Vector3.up * Mathf.Max(0.1f, perchHeight);
            var geometry = Geometry(flower);
            if (geometry.Radius > 0f) eyeTarget.y = Mathf.Max(eyeTarget.y, geometry.Nectar.y + 0.16f);
            float vertical = eyeTarget.y - head.position.y;
            float reach = (contactRadius + geometry.Radius) * 0.9f;
            if (Mathf.Abs(vertical) >= reach) return false;
            Vector3 outward = Vector3.ProjectOnPlane(support - head.position, Vector3.up);
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector3.ProjectOnPlane(InteractionPosition - head.position, Vector3.up);
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector3.ProjectOnPlane(-(trackedCamera != null ? trackedCamera.transform.forward : beeRoot.forward), Vector3.up);
            if (outward.sqrMagnitude < 0.0001f) outward = Vector3.back;
            float horizontalReach = Mathf.Sqrt(reach * reach - vertical * vertical);
            float distanceFromHead = Mathf.Min(Mathf.Max(geometry.ViewDistance(perchViewDistance), outward.magnitude), horizontalReach);
            // Do not shorten the view until the flower intersects the player.
            if (geometry.Radius > 0f && distanceFromHead < geometry.Radius + 0.25f) return false;
            eyeTarget = new Vector3(head.position.x, eyeTarget.y, head.position.z) + outward.normalized * distanceFromHead;
            return true;
        }

        void DrawPerchGizmos()
        {
            if (beeRoot == null || flowerAHead == null || flowerBHead == null) return;
            for (int i = 0; i < 2; i++) {
                if (!TryGetPerchView(i, out Vector3 support, out Vector3 view)) continue;
                Gizmos.color = Color.green; Gizmos.DrawWireSphere(support, 0.06f);
                Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(view, 0.08f);
                Gizmos.DrawLine(support, view); Gizmos.DrawLine(view, Head(i).position);
            }
        }

        void LeaveFlower()
        {
            if (PerchedFlower >= 0 && beeController != null) beeController.SetFlowerPerched(false);
            PerchedFlower = -1;
            perchBlockedTime = 0f;
        }

        void LateUpdate()
        {
            if (enhancedFeedback && honeyFeedback == null && chunk != null)
                honeyFeedback = new BeeHoneyFeedback(transform, trackedCamera, flowerAHead, flowerBHead, slot, honeyMaterial, slotDecoration, honeyMesh);
            honeyFeedback?.SetActive(enhancedFeedback);
            if (Enhanced) {
                honeyFeedback.Configure(feedbackIntensity, feedbackVolume, feedbackHaptics);
                bool waitingCollect = (string)pendingRequest?["kind"] == "collect";
                bool waitingVisit = (string)pendingRequest?["kind"] == "visit";
                int visualContact = waitingCollect ? (int)pendingRequest["flower"] : contact;
                honeyFeedback.Step(Time.deltaTime, Live, visualContact, Charge01, waitingCollect, waitingVisit,
                    PerchedFlower, IsFlowerSettled, (int?)State?["pollen_source"] ?? -1, PollinationMask(),
                    ChunkState, chunk.position, rightHand, snapRadius);
            }
            perchTime = Mathf.MoveTowards(perchTime, IsFlowerSettled ? 1f : 0f, Time.deltaTime * 3f);
            responseA?.Apply(PerchedFlower == 0 ? perchTime : 0f, contact == 0 ? Charge01 : 0f,
                Enhanced ? honeyFeedback.FlowerBloom(0) : 0f, Enhanced ? honeyFeedback.FlowerRebound(0) : 0f);
            responseB?.Apply(PerchedFlower == 1 ? perchTime : 0f, contact == 1 ? Charge01 : 0f,
                Enhanced ? honeyFeedback.FlowerBloom(1) : 0f, Enhanced ? honeyFeedback.FlowerRebound(1) : 0f);
            if (landingRing == null) return;
            landingRing.enabled = enableFlowerPerching && contact >= 0 && ChunkState == "none" && PerchedFlower < 0
                && beeController != null && beeController.IsPositionHolding;
            if (!landingRing.enabled) return;
            if (!TryGetPerchView(contact, out Vector3 support, out _)) { landingRing.enabled = false; return; }
            Vector3 centre = support + Vector3.up * 0.025f;
            float radius = 0.08f + 0.004f * Mathf.Sin(Time.time * 3f);
            for (int i = 0; i < landingRing.positionCount; i++) {
                float angle = i * Mathf.PI * 2f / landingRing.positionCount;
                landingRing.SetPosition(i, centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        sealed class FlowerResponse
        {
            readonly Transform head;
            Vector3 basePosition, appliedPosition;
            Quaternion baseRotation, appliedRotation;
            readonly Transform[] petals;
            readonly Quaternion[] petalBase, petalApplied;
            bool applied;
            public FlowerResponse(Transform value)
            {
                head = value;
                var bones = new System.Collections.Generic.List<Transform>();
                foreach (var bone in head.GetComponentsInChildren<Transform>(true))
                    if (bone.name.StartsWith("Petal_") && bone.name.EndsWith("_0")) bones.Add(bone);
                petals = bones.ToArray(); petalBase = new Quaternion[petals.Length]; petalApplied = new Quaternion[petals.Length];
            }
            public void Apply(float pressure, float charge, float bloom = 0f, float rebound = 0f)
            {
                if (head == null) return;
                // Keep the Animator's current pose, including after culling/resuming.
                if (!applied || (head.localPosition - appliedPosition).sqrMagnitude > 0.00000001f) basePosition = head.localPosition;
                if (!applied || Quaternion.Angle(head.localRotation, appliedRotation) > 0.001f) baseRotation = head.localRotation;
                Vector3 worldDip = Vector3.up * (-0.008f * pressure + 0.007f * rebound);
                appliedPosition = basePosition + (head.parent != null ? head.parent.InverseTransformVector(worldDip) : worldDip);
                appliedRotation = baseRotation * Quaternion.Euler(pressure * 1.5f, 0f, charge * Mathf.Sin(Time.time * 9f) * 1.2f);
                head.localPosition = appliedPosition;
                head.localRotation = appliedRotation;
                for (int i = 0; i < petals.Length; i++) {
                    var bone = petals[i];
                    if (!applied || Quaternion.Angle(bone.localRotation, petalApplied[i]) > 0.001f) petalBase[i] = bone.localRotation;
                    Vector3 radial = Vector3.ProjectOnPlane(bone.position - head.position, head.up);
                    if (radial.sqrMagnitude < 0.000001f) radial = head.TransformDirection(Quaternion.Euler(0f,i*360f/Mathf.Max(1,petals.Length),0f)*Vector3.right);
                    Vector3 axis = Vector3.Cross(head.up, radial.normalized);
                    if (bone.parent != null) axis = bone.parent.InverseTransformDirection(axis);
                    petalApplied[i] = Quaternion.AngleAxis(pressure*0.7f - bloom*3f,axis) * petalBase[i];
                    bone.localRotation = petalApplied[i];
                }
                applied = true;
            }
            public void Restore()
            {
                if (!applied || head == null) return;
                if ((head.localPosition - appliedPosition).sqrMagnitude < 0.00000001f) head.localPosition = basePosition;
                if (Quaternion.Angle(head.localRotation, appliedRotation) < 0.001f) head.localRotation = baseRotation;
                for (int i = 0; i < petals.Length; i++)
                    if (petals[i] != null && Quaternion.Angle(petals[i].localRotation,petalApplied[i]) < 0.001f) petals[i].localRotation = petalBase[i];
                applied = false;
            }
        }

        void UpdateSlotPose()
        {
            if (slotMount == null || slot == null) return;
            slot.position = slotMount.TransformPoint(slotMountLocalPoint);
            slot.rotation = Quaternion.LookRotation(slotMount.forward, Vector3.up);
            if (ghost != null) { ghost.position = slot.position; ghost.rotation = slot.rotation; }
        }

        int NearestContact()
        {
            float a = Geometry(0).Distance(InteractionPosition), b = Geometry(1).Distance(InteractionPosition);
            if (contact >= 0) {
                float current = contact == 0 ? a : b, other = contact == 0 ? b : a;
                if (current <= contactRadius + 0.08f && (PerchedFlower == contact || current <= other + 0.12f)) return contact;
            }
            if (a > contactRadius && b > contactRadius) return -1;
            return a < b ? 0 : 1;
        }
        Transform Head(int flower) => flower == 0 ? flowerAHead : flowerBHead;
        Vector3 NectarPosition(int flower) => Geometry(flower).Nectar;
        bool AimedAt(Vector3 point)
        {
            Vector3 origin = rightHand.position, direction = rightHand.forward;
            var tip = rightHand.Find("RayTip");
            if (tip != null) { origin = tip.position; direction = tip.forward; }
            else { origin = rightHand.TransformPoint(new Vector3(0f, 0.01f, 0.04f)); }
            var swipe = beeRoot.GetComponent<BeeSwipeFlap>();
            if (swipe != null) direction = Quaternion.AngleAxis(swipe.rayPitchOffset, tip != null ? tip.right : rightHand.right) * direction;
            float projection = Vector3.Dot(point - origin, direction);
            return projection >= -aimRadius && projection <= grabRadius + aimRadius
                && Vector3.Distance(origin + direction * Mathf.Max(0f, projection), point) <= aimRadius;
        }
        void Request(string kind, int flower = -1)
        {
            RequestAt(kind, flower, InteractionPosition, rightHand.position, rightHand.TransformPoint(new Vector3(0f, 0.015f, 0.12f)));
        }
        void RequestAt(string kind, int flower, Vector3 bee, Vector3 hand, Vector3 heldChunk)
        {
            if (!Live || Busy) return;
            if (kind == "visit") pendingVisitPollinates = flower >= 0 && (int?)State?["pollen_source"] >= 0
                && (int?)State?["pollen_source"] != flower && (PollinationMask() & (1 << flower)) == 0;
            pendingRequest = new JObject { ["event_id"] = Guid.NewGuid().ToString("N"), ["session"] = sessionId,
                ["kind"] = kind, ["flower"] = flower, ["chunk_id"] = (string)State?["chunk_id"] ?? "", ["slot_id"] = "slot-a",
                ["bee"] = ToGama(bee), ["hand"] = ToGama(hand) };
            if (kind == "deposit" || kind == "drop") pendingRequest["held_chunk"] = ToGama(heldChunk);
        }
        void Accepted(string kind, int flower)
        {
            if (Enhanced) {
                int mask = PollinationMask();
                bool newPollination = flower >= 0 && ((mask & ~reportedPollinationMask) != 0
                    || (pendingVisitPollinates && (mask & (1 << flower)) != 0));
                honeyFeedback.Confirm((string)pendingRequest?["event_id"], kind, flower, newPollination, chunk.position, chunk.rotation);
                if (kind == "visit") reportedPollinationMask = mask;
                return;
            }
            if (kind == "visit") {
                int mask = PollinationMask();
                if ((mask & ~reportedPollinationMask) != 0 && flower >= 0) {
                    PlayFeedback(pollinateSound, 0.25f); pulseUntil = Time.time + 0.7f; EmitAt(Head(flower).position);
                } else PlayFeedback(collectSound, 0.08f);
                reportedPollinationMask = mask;
            } else if (kind == "collect") PlayFeedback(collectSound, 0.2f);
            else if (kind == "deposit") { PlayFeedback(depositSound, 0.45f); pulseUntil = Time.time + 1f; EmitAt(slot.position); }
            else if (kind == "pick") { PlayFeedback(collectSound, 0.12f); threadSource = chunk.position; threadUntil = Time.time + 0.25f; }
        }
        int PollinationMask() => State == null ? 0 : ((bool)State["pollinated"][0] ? 1 : 0) | ((bool)State["pollinated"][1] ? 2 : 0);
        void EmitAt(Vector3 point) { if (pollenBurst != null) { pollenBurst.transform.position = point; pollenBurst.Emit(18); } }

        Transform Visual(string name, Material material)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = honeyMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }
        TextMesh Label(string name)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMesh>(); text.font = labelFont; text.fontSize = 64;
            text.characterSize = 0.035f * labelScale; text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.87f, 0.3f); if (labelFont != null) go.GetComponent<MeshRenderer>().sharedMaterial = labelFont.material;
            return text;
        }
        void BuildViews()
        {
            chunk = Visual("Pilot nectar chunk", honeyMaterial); chunk.gameObject.SetActive(false);
            chargeVisual = Visual("Nectar charge preview", ghostMaterial);
            chargeVisual.gameObject.SetActive(false);
            ghost = Visual("Empty hive slot", ghostMaterial); ghost.position = slot.position; ghost.rotation = slot.rotation;
            pollen = Visual("Carried pollen cue", cueMaterial); pollen.localScale = Vector3.one * 0.16f;
            labelA = Label("Flower A cue"); labelB = Label("Flower B cue"); labelSlot = Label("Hive slot cue");
            labelChunk = Label("Honey grip cue");
            var line = new GameObject("Nectar thread"); line.layer = 2; line.transform.SetParent(transform, false);
            tether = line.AddComponent<LineRenderer>(); tether.sharedMaterial = cueMaterial; tether.positionCount = 2;
            tether.startWidth = 0.012f; tether.endWidth = 0.004f; tether.enabled = false;
            var ring = new GameObject("Flower landing cue"); ring.layer = 2; ring.transform.SetParent(transform, false);
            landingRing = ring.AddComponent<LineRenderer>(); landingRing.sharedMaterial = cueMaterial;
            landingRing.positionCount = 40; landingRing.loop = true; landingRing.useWorldSpace = true;
            landingRing.startWidth = landingRing.endWidth = 0.008f; landingRing.enabled = false;
            var particles = new GameObject("Pollen transfer feedback"); particles.layer = 2; particles.transform.SetParent(transform, false);
            pollenBurst = particles.AddComponent<ParticleSystem>(); pollenBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = pollenBurst.main; main.loop = false; main.playOnAwake = false; main.startLifetime = 0.55f;
            main.startSpeed = 0.25f; main.startSize = 0.016f; main.startColor = new Color(1f, 0.82f, 0.1f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = pollenBurst.emission; emission.rateOverTime = 0;
            var shape = pollenBurst.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.06f;
            pollenBurst.GetComponent<ParticleSystemRenderer>().sharedMaterial = cueMaterial;
            feedback = gameObject.AddComponent<AudioSource>(); feedback.playOnAwake = false; feedback.spatialBlend = 0.7f; feedback.volume = 0.22f;
            collectSound = Tone("Nectar pickup", 650, 0.1f); pollinateSound = Tone("Pollen transfer", 950, 0.18f); depositSound = Tone("Hive snap", 480, 0.22f);
            if (enhancedFeedback) {
                honeyFeedback = new BeeHoneyFeedback(transform, trackedCamera, flowerAHead, flowerBHead, slot, honeyMaterial, slotDecoration, honeyMesh);
                honeyFeedback.Configure(feedbackIntensity,feedbackVolume,feedbackHaptics);
            }
        }
        void UpdateViews()
        {
            bool exists = ChunkState != "none";
            var chunkRenderer = chunk.GetComponent<MeshRenderer>();
            var chunkMaterial = Enhanced && honeyFeedback.NectarMaterial != null ? honeyFeedback.NectarMaterial : honeyMaterial;
            if (chunkRenderer.sharedMaterial != chunkMaterial) chunkRenderer.sharedMaterial = chunkMaterial;
            bool wasVisible = chunk.gameObject.activeSelf;
            chunk.gameObject.SetActive(exists);
            chunk.localScale = Enhanced ? honeyFeedback.NectarScale(ChunkState) : Vector3.one;
            if (exists && State != null) {
                Vector3 target = FromGama(State["chunk_pos"]);
                Quaternion rotation = Quaternion.identity;
                if (ChunkState == "held") { target = rightHand.TransformPoint(new Vector3(0f, 0.015f, 0.12f)); rotation = rightHand.rotation; }
                if (ChunkState == "stored") { target = Enhanced ? honeyFeedback.SocketCenter : slot.position; rotation = Enhanced ? honeyFeedback.SocketRotation : slot.rotation; }
                if (ChunkState == "ready" || ChunkState == "loose") {
                    int origin = (int?)State["chunk_flower"] ?? -1;
                    if (Enhanced && ChunkState == "ready" && origin >= 0 && origin < 2) target = honeyFeedback.NectarRestPosition(origin);
                    else if (ChunkState == "ready") target += Vector3.up * 0.06f;
                    target += Vector3.up * (0.008f * Mathf.Sin(Time.time * 3f));
                }
                bool releasing = ChunkState == "held" && ((string)pendingRequest?["kind"] == "deposit" || (string)pendingRequest?["kind"] == "drop");
                if (releasing) {
                    chunk.position = releaseChunk;
                } else if (Enhanced && ChunkState == "held") {
                    Vector3 lifted; Quaternion liftedRotation;
                    if (honeyFeedback.PickupAnimating) honeyFeedback.PickupPose(rightHand,out lifted,out liftedRotation);
                    else BeeHoneyFeedback.FollowHandPose(chunk.position,chunk.rotation,rightHand,Time.deltaTime,out lifted,out liftedRotation);
                    chunk.SetPositionAndRotation(lifted,liftedRotation);
                } else if (Enhanced && ChunkState == "stored" && honeyFeedback.DepositAnimating) {
                    honeyFeedback.DepositPose(out Vector3 snapped, out Quaternion snappedRotation);
                    chunk.SetPositionAndRotation(snapped,snappedRotation);
                } else {
                    if (!wasVisible || lastChunkPosition == Vector3.zero) chunk.position = target;
                    else chunk.position = Vector3.Lerp(chunk.position,target,1f-Mathf.Exp(-(Enhanced && ChunkState == "held" ? 14f : 25f)*Time.deltaTime));
                    chunk.rotation = Quaternion.Slerp(chunk.rotation,rotation,1f-Mathf.Exp(-25f*Time.deltaTime));
                }
                lastChunkPosition = chunk.position;
            }
            bool collecting = Charge01 > 0f && contact >= 0;
            chargeVisual.gameObject.SetActive(collecting && !Enhanced);
            if (collecting) { chargeVisual.position = Head(contact).position + Vector3.up * 0.06f; chargeVisual.localScale = Vector3.one * Mathf.Lerp(0.12f, 1f, Charge01); }
            tether.enabled = !Enhanced && exists && ChunkState == "ready" && Vector3.Distance(chunk.position, FromGama(State["chunk_pos"])) < 0.3f;
            if (tether.enabled) { tether.SetPosition(0, FromGama(State["chunk_pos"])); tether.SetPosition(1, chunk.position); }
            if (ChunkState == "held" && Time.time < threadUntil && Vector3.Distance(threadSource, chunk.position) < 0.5f) {
                tether.enabled = true; tether.SetPosition(0, threadSource); tether.SetPosition(1, chunk.position);
            }
            ghost.gameObject.SetActive(Stored == 0 && !Enhanced);
            ghost.localScale = Vector3.one * ((ChunkState == "held" && Vector3.Distance(chunk.position, slot.position) <= snapRadius) ? 1.12f : 1f);
            pollen.gameObject.SetActive(!Enhanced && State != null && (int)State["pollen_source"] >= 0);
            pollen.position = rightHand.TransformPoint(new Vector3(-0.07f, 0.015f, 0.02f)); pollen.rotation = rightHand.rotation;
            labelA.text = FlowerHint(0);
            labelB.text = FlowerHint(1);
            string networkHint = client is HoneySimpleClient simple && simple.WaitingForAdmission
                ? "CHO THEM KINH VAO SIMPLE" : "CHO SIMPLE/GAMA TREN PC";
            bool socketReady = ChunkState == "held" && Vector3.Distance(chunk.position, slot.position) <= snapRadius
                && Vector3.Distance(InteractionPosition, slot.position) <= 2f;
            labelSlot.text = Stored > 0 ? Enhanced && honeyFeedback.DepositAnimating ? "DANG LAP MAT..." : "DA GAN MAT  1/1" : !Live ? networkHint
                : ChunkState == "held" ? Busy ? "DANG XAC NHAN..." : socketReady ? "DA KHOP O\nTHA NUT BEN HONG DE GAN" : "DUA CUC MAT VAO O TO" : "";
            labelChunk.text = ChunkState == "ready" || ChunkState == "loose"
                ? Busy ? "DANG XAC NHAN CAM..." : grabReady ? "GIU NUT BEN HONG TAY PHAI\nDE CAM MAT" : "MAT O DAY\nDUA TAY GAN HOAC NGAM VAO MAT"
                : ChunkState == "held" && !socketReady ? "GIU NUT BEN HONG\nMANG MAT VE TO" : "";
            if (Time.time < failureUntil) {
                if (failureKind == "pick") labelChunk.text = failureHint;
                else if (failureKind == "deposit" || failureKind == "drop") labelSlot.text = failureHint;
                else if (contact == 0) labelA.text = failureHint;
                else if (contact == 1) labelB.text = failureHint;
            }
            PlaceLabel(labelA, Geometry(0).Surface + Vector3.up * 0.25f);
            PlaceLabel(labelB, Geometry(1).Surface + Vector3.up * 0.25f);
            PlaceLabel(labelSlot, slot.position + Vector3.up * 0.18f);
            PlaceLabel(labelChunk, chunk.position + Vector3.up * 0.13f);
            if (Enhanced) honeyFeedback.SetHoneyHover(grabReady, Busy && (string)pendingRequest?["kind"] == "pick", chunk.position);
            if (Time.time < pulseUntil) ghost.localScale *= 1f + 0.04f * Mathf.Sin(Time.time * 24f);
        }
        string FlowerHint(int index)
        {
            string name = index == 0 ? "A" : "B";
            if (contact != index) return "";
            if (!Live) return "CHO KET NOI SIMPLE/GAMA";
            if (ChunkState == "ready" || ChunkState == "loose") return "";
            if (ChunkState == "held") return !(bool)State["pollinated"][index] && visitedContact != index ? "DAU HOAC CHAM NHUY\nDE CHUYEN PHAN" : "";
            if (Busy && (string)pendingRequest?["kind"] == "visit") return "DANG NHAN / CHUYEN PHAN...";
            if (Busy && (string)pendingRequest?["kind"] == "collect") return "DANG TAO CUC MAT...";
            if (Charge01 > 0f) return name + " - LAY MAT " + Mathf.RoundToInt(Charge01 * 100f) + "%";
            if (Stored > 0) return "DA HOAN THANH  1/1";
            if ((int)State["nectar"][index] == 0) return "HOA DA LAY MAT";
            if (PerchedFlower == index) return IsFlowerSettled
                ? visitedContact == index ? "DA DAU CANH\nGIU TRIGGER PHAI DE LAY MAT" : "GIU YEN DE NHAN / CHUYEN PHAN"
                : "DANG DAU LEN CANH...";
            return "GIU TRIGGER TRAI DE ON DINH\nDAU CANH HOAC CHAM NHUY";
        }
        void PlaceLabel(TextMesh label, Vector3 position)
        {
            label.transform.position = position;
            if (trackedCamera == null) return;
            float distance = Vector3.Distance(trackedCamera.transform.position, position);
            label.gameObject.SetActive(!string.IsNullOrEmpty(label.text) && distance < 2.8f && distance > 0.15f);
            label.transform.rotation = Quaternion.LookRotation(position - trackedCamera.transform.position, Vector3.up);
            label.transform.localScale = Vector3.one;
            if (!label.gameObject.activeSelf) return;
            var size = label.GetComponent<Renderer>().bounds.size;
            float width = Mathf.Max(size.x, size.z);
            float scale = Mathf.Min(1f, Mathf.Min(Mathf.Min(0.6f, distance * 0.42f) / Mathf.Max(0.001f, width),
                Mathf.Min(0.11f, distance * 0.12f) / Mathf.Max(0.001f, size.y)));
            label.transform.localScale = Vector3.one * scale;
        }
        void PlayFeedback(AudioClip sound, float amplitude)
        {
            if (feedback != null) feedback.PlayOneShot(sound);
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse) device.SendHapticImpulse(0, amplitude, 0.08f);
        }
        static AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 22050; var samples = new float[(int)(duration * rate)];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * 0.2f * (1f - (float)i / samples.Length);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDisable()
        {
            LeaveFlower();
            if (beeController != null) beeController.RightHandInteractionOwnsInput = false;
            gripIntent.Reset();
            honeyFeedback?.Dispose(); honeyFeedback = null;
            responseA?.Restore(); responseB?.Restore();
            lifetime?.Cancel(); client?.Dispose(); Live = false;
            rightTrigger?.Dispose(); rightGrip?.Dispose();
            if (originalsCaptured) {
                if (flowerAAnimator != null) flowerAAnimator.runtimeAnimatorController = originalA;
                if (flowerBAnimator != null) flowerBAnimator.runtimeAnimatorController = originalB;
                if (slotDecoration != null) slotDecoration.enabled = decorationWasEnabled;
                originalsCaptured = false;
            }
            foreach (var t in new Transform[] { chunk, chargeVisual, ghost, pollen, tether != null ? tether.transform : null,
                labelA != null ? labelA.transform : null, labelB != null ? labelB.transform : null, labelSlot != null ? labelSlot.transform : null, labelChunk != null ? labelChunk.transform : null,
                landingRing != null ? landingRing.transform : null }) if (t != null) ReleaseRuntime(t.gameObject);
            if (feedback != null) ReleaseRuntime(feedback);
            if (pollenBurst != null) ReleaseRuntime(pollenBurst.gameObject);
            foreach (var sound in new[] { collectSound, pollinateSound, depositSound }) if (sound != null) ReleaseRuntime(sound);
        }
        static void ReleaseRuntime(UnityEngine.Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnDrawGizmosSelected()
        {
            DrawPerchGizmos();
            Gizmos.color = Color.yellow;
            if (flowerAHead != null) Gizmos.DrawWireSphere(flowerAHead.position, contactRadius);
            if (flowerBHead != null) Gizmos.DrawWireSphere(flowerBHead.position, contactRadius);
            Gizmos.color = Color.cyan; if (slot != null) Gizmos.DrawWireSphere(slot.position, snapRadius);
        }
    }
}
