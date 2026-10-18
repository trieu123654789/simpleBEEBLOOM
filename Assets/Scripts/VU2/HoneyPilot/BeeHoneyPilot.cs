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
        [Tooltip("Use the existing left-trigger hold for precise VR interaction; prevents legacy trigger/grip flight input from lifting the bee during collection or release.")]
        public bool requirePositionHoldInVr = true;

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
        bool gripWasPressed;
        int contact = -1, visitedContact = -1, chargingFlower = -1;
        float contactDwell, collectTime;
        Transform chunk, chargeVisual, ghost, pollen;
        LineRenderer tether;
        TextMesh labelA, labelB, labelSlot;
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
            rightTrigger.Enable(); rightGrip.Enable();
            BuildViews();
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
                ["session"] = sessionId, ["checkpoint"] = checkpoint.Replace('\\', '/'),
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
                        reportedPollinationMask = PollinationMask();
                    }
                    await client.AskAsync("set_pose", new JObject { ["pose_json"] = new JObject {
                        ["bee"] = ToGama(beeRoot.position), ["hand"] = ToGama(rightHand.position),
                        ["flowers"] = new JArray(ToGama(flowerAHead.position), ToGama(flowerBHead.position)),
                        ["slot"] = ToGama(slot.position)
                    }.ToString(Newtonsoft.Json.Formatting.None) });
                    JObject request = pendingRequest;
                    if (request != null) await client.AskAsync("transact", new JObject { ["request_json"] = request.ToString(Newtonsoft.Json.Formatting.None) });
                    JObject snapshot = await client.SnapshotAsync();
                    if (token.IsCancellationRequested) break;
                    State = (JObject)snapshot["state"];
                    if (request != null && (string)snapshot["response"]?["event_id"] == (string)request["event_id"]) {
                        LastReason = (string)snapshot["response"]["reason"];
                        if ((bool)snapshot["response"]["accepted"]) Accepted((string)request["kind"], (int)request["flower"]);
                        else if ((string)request["kind"] == "visit") visitedContact = -1;
                        pendingRequest = null;
                    }
                    await Task.Delay(100, token);
                } catch (OperationCanceledException) { break; }
                catch (Exception ex) {
                    if (token.IsCancellationRequested) break;
                    Live = false; ConnectionStatus = ex.Message;
                    client?.Dispose(); client = null;
                    // Keep both the visible confirmed state and the SAME transaction ID.
                    // Reconnect reloads the GAMA checkpoint, then retries idempotently.
                    try { await Task.Delay(2000, token); } catch (OperationCanceledException) { break; }
                }
            }
            client?.Dispose(); client = null;
        }

        void Update()
        {
            if (chunk == null) return;
            UpdateSlotPose();
            float trigger = rightTrigger?.ReadValue<float>() ?? 0f;
            float grip = rightGrip?.ReadValue<float>() ?? 0f;
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.TryGetFeatureValue(CommonUsages.trigger, out float physicalTrigger)) trigger = Mathf.Max(trigger, physicalTrigger);
            if (device.TryGetFeatureValue(CommonUsages.grip, out float physicalGrip)) grip = Mathf.Max(grip, physicalGrip);
            bool desktopInteraction = false;
            if (enableDesktopKeys && Keyboard.current != null) {
                if (Keyboard.current.fKey.isPressed) { trigger = 1f; desktopInteraction = true; }
                if (Keyboard.current.gKey.isPressed) { grip = 1f; desktopInteraction = true; }
            }
            bool xrActive = XRSettings.isDeviceActive || device.isValid || rightTrigger?.activeControl != null;
            bool stable = !requirePositionHoldInVr || !xrActive || desktopInteraction || (beeController != null && beeController.IsPositionHolding);
            bool gripDown = grip >= 0.65f && !gripWasPressed;
            gripWasPressed = grip >= 0.45f;
            int near = NearestContact();
            if (near != contact) { contact = near; visitedContact = -1; contactDwell = 0f; }
            contactDwell += Time.deltaTime;
            if (Live && !Busy && contact >= 0 && visitedContact != contact && contactDwell >= 0.25f) {
                visitedContact = contact; Request("visit", contact);
            }
            bool canCollect = stable && Live && !Busy && State != null && ChunkState == "none" && Stored == 0
                && contact >= 0 && (int)State["nectar"][contact] > 0 && AimedAt(Head(contact).position);
            if (trigger >= 0.65f && canCollect) {
                if (chargingFlower != contact) { collectTime = 0f; chargingFlower = contact; }
                collectTime += Time.deltaTime;
                Charge01 = Mathf.Clamp01(collectTime / Mathf.Max(0.2f, collectDuration));
                if (Charge01 >= 1f) { Request("collect", contact); collectTime = 0f; Charge01 = 0f; }
            } else { collectTime = 0f; Charge01 = 0f; chargingFlower = -1; }
            if (gripDown && stable && Live && !Busy) {
                if (ChunkState == "held") Request(Vector3.Distance(rightHand.position, slot.position) <= snapRadius ? "deposit" : "drop");
                else if ((ChunkState == "ready" || ChunkState == "loose") && Vector3.Distance(rightHand.position, chunk.position) <= grabRadius
                    && (Vector3.Distance(rightHand.position, chunk.position) < 0.2f || AimedAt(chunk.position))) Request("pick");
            }
            UpdateViews();
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
            float a = Vector3.Distance(beeRoot.position, flowerAHead.position), b = Vector3.Distance(beeRoot.position, flowerBHead.position);
            if (a > contactRadius && b > contactRadius) return -1;
            return a < b ? 0 : 1;
        }
        Transform Head(int flower) => flower == 0 ? flowerAHead : flowerBHead;
        bool AimedAt(Vector3 point)
        {
            Vector3 origin = rightHand.position, direction = rightHand.forward;
            var tip = rightHand.Find("RayTip");
            if (tip != null) { origin = tip.position; direction = tip.forward; }
            else { origin = rightHand.TransformPoint(new Vector3(0f, 0.01f, 0.04f)); }
            var swipe = beeRoot.GetComponent<BeeSwipeFlap>();
            if (swipe != null) direction = Quaternion.AngleAxis(swipe.rayPitchOffset, rightHand.right) * direction;
            float projection = Vector3.Dot(point - origin, direction);
            return projection >= -aimRadius && projection <= grabRadius + aimRadius
                && Vector3.Distance(origin + direction * Mathf.Max(0f, projection), point) <= aimRadius;
        }
        void Request(string kind, int flower = -1)
        {
            if (!Live || Busy) return;
            pendingRequest = new JObject { ["event_id"] = Guid.NewGuid().ToString("N"), ["session"] = sessionId,
                ["kind"] = kind, ["flower"] = flower, ["chunk_id"] = (string)State?["chunk_id"] ?? "", ["slot_id"] = "slot-a",
                ["bee"] = ToGama(beeRoot.position), ["hand"] = ToGama(rightHand.position) };
        }
        void Accepted(string kind, int flower)
        {
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
            var line = new GameObject("Nectar thread"); line.layer = 2; line.transform.SetParent(transform, false);
            tether = line.AddComponent<LineRenderer>(); tether.sharedMaterial = cueMaterial; tether.positionCount = 2;
            tether.startWidth = 0.012f; tether.endWidth = 0.004f; tether.enabled = false;
            var particles = new GameObject("Pollen transfer feedback"); particles.layer = 2; particles.transform.SetParent(transform, false);
            pollenBurst = particles.AddComponent<ParticleSystem>(); pollenBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = pollenBurst.main; main.loop = false; main.playOnAwake = false; main.startLifetime = 0.55f;
            main.startSpeed = 0.25f; main.startSize = 0.016f; main.startColor = new Color(1f, 0.82f, 0.1f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = pollenBurst.emission; emission.rateOverTime = 0;
            var shape = pollenBurst.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.06f;
            pollenBurst.GetComponent<ParticleSystemRenderer>().sharedMaterial = cueMaterial;
            feedback = gameObject.AddComponent<AudioSource>(); feedback.playOnAwake = false; feedback.spatialBlend = 0.7f; feedback.volume = 0.22f;
            collectSound = Tone("Nectar pickup", 650, 0.1f); pollinateSound = Tone("Pollen transfer", 950, 0.18f); depositSound = Tone("Hive snap", 480, 0.22f);
        }
        void UpdateViews()
        {
            bool exists = ChunkState != "none";
            chunk.gameObject.SetActive(exists);
            if (exists && State != null) {
                Vector3 target = FromGama(State["chunk_pos"]);
                Quaternion rotation = Quaternion.identity;
                if (ChunkState == "held") { target = rightHand.TransformPoint(new Vector3(0f, 0.015f, 0.12f)); rotation = rightHand.rotation; }
                if (ChunkState == "stored") { target = slot.position; rotation = slot.rotation; }
                if (ChunkState == "ready" || ChunkState == "loose") target += Vector3.up * (0.06f + 0.008f * Mathf.Sin(Time.time * 3f));
                if (!chunk.gameObject.activeSelf || lastChunkPosition == Vector3.zero) chunk.position = target;
                else chunk.position = Vector3.Lerp(chunk.position, target, 1f - Mathf.Exp(-25f * Time.deltaTime));
                chunk.rotation = Quaternion.Slerp(chunk.rotation, rotation, 1f - Mathf.Exp(-25f * Time.deltaTime)); lastChunkPosition = chunk.position;
            }
            bool collecting = Charge01 > 0f && contact >= 0;
            chargeVisual.gameObject.SetActive(collecting);
            if (collecting) { chargeVisual.position = Head(contact).position + Vector3.up * 0.06f; chargeVisual.localScale = Vector3.one * Mathf.Lerp(0.12f, 1f, Charge01); }
            tether.enabled = exists && ChunkState == "ready" && Vector3.Distance(chunk.position, FromGama(State["chunk_pos"])) < 0.3f;
            if (tether.enabled) { tether.SetPosition(0, FromGama(State["chunk_pos"])); tether.SetPosition(1, chunk.position); }
            if (ChunkState == "held" && Time.time < threadUntil && Vector3.Distance(threadSource, chunk.position) < 0.5f) {
                tether.enabled = true; tether.SetPosition(0, threadSource); tether.SetPosition(1, chunk.position);
            }
            ghost.gameObject.SetActive(Stored == 0);
            ghost.localScale = Vector3.one * ((ChunkState == "held" && Vector3.Distance(rightHand.position, slot.position) <= snapRadius) ? 1.12f : 1f);
            pollen.gameObject.SetActive(State != null && (int)State["pollen_source"] >= 0);
            pollen.position = rightHand.TransformPoint(new Vector3(-0.07f, 0.015f, 0.02f)); pollen.rotation = rightHand.rotation;
            labelA.text = State == null ? "A" : ((bool)State["pollinated"][0] ? "A - DA NHAN PHAN" : "A - LAY MAT / PHAN");
            labelB.text = State == null ? "B" : ((bool)State["pollinated"][1] ? "B - DA NHAN PHAN" : "B - THU PHAN");
            labelSlot.text = Stored > 0 ? "DA GAN MAT  1/1" : (!Live ? "DANG KET NOI..." : (Busy ? "DANG XU LY..." : "O TO  0/1"));
            PlaceLabel(labelA, flowerAHead.position + Vector3.up * 0.3f);
            PlaceLabel(labelB, flowerBHead.position + Vector3.up * 0.3f);
            PlaceLabel(labelSlot, slot.position + Vector3.up * 0.35f);
            if (Time.time < pulseUntil) ghost.localScale *= 1f + 0.04f * Mathf.Sin(Time.time * 24f);
        }
        void PlaceLabel(TextMesh label, Vector3 position)
        {
            label.transform.position = position;
            if (trackedCamera == null) return;
            label.gameObject.SetActive(Vector3.Distance(trackedCamera.transform.position, position) < 8f);
            label.transform.rotation = Quaternion.LookRotation(position - trackedCamera.transform.position, Vector3.up);
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
            lifetime?.Cancel(); client?.Dispose(); Live = false;
            rightTrigger?.Dispose(); rightGrip?.Dispose();
            if (originalsCaptured) {
                if (flowerAAnimator != null) flowerAAnimator.runtimeAnimatorController = originalA;
                if (flowerBAnimator != null) flowerBAnimator.runtimeAnimatorController = originalB;
                if (slotDecoration != null) slotDecoration.enabled = decorationWasEnabled;
                originalsCaptured = false;
            }
            foreach (var t in new Transform[] { chunk, chargeVisual, ghost, pollen, tether != null ? tether.transform : null,
                labelA != null ? labelA.transform : null, labelB != null ? labelB.transform : null, labelSlot != null ? labelSlot.transform : null }) if (t != null) Destroy(t.gameObject);
            if (feedback != null) Destroy(feedback);
            if (pollenBurst != null) Destroy(pollenBurst.gameObject);
            foreach (var sound in new[] { collectSound, pollinateSound, depositSound }) if (sound != null) Destroy(sound);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (flowerAHead != null) Gizmos.DrawWireSphere(flowerAHead.position, contactRadius);
            if (flowerBHead != null) Gizmos.DrawWireSphere(flowerBHead.position, contactRadius);
            Gizmos.color = Color.cyan; if (slot != null) Gizmos.DrawWireSphere(slot.position, snapRadius);
        }
    }
}
