using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace LetItBee.HoneyPilot
{
    // Presentation only: never owns inventory, tracking, colliders or flight.
    // Mesh/line buffers are reused; no per-frame GameObjects or particle allocations.
    public sealed class BeeHoneyFeedback : IDisposable
    {
        const int Capacity = 160;
        static readonly Color Nectar = new Color(1f, 0.64f, 0.08f);
        static readonly Color Pollen = new Color(1f, 0.83f, 0.24f);
        static readonly Color Cream = new Color(1f, 0.94f, 0.63f);
        readonly GameObject root;
        readonly Transform flowerA, flowerB, slot;
        readonly Transform visualParent;
        Renderer socketArt;
        public void SetSocket(Renderer value) { socketArt = value; }
        readonly Bounds nectarBounds;
        const float AlignDuration = 0.36f, InsertDuration = 0.6f;
        const float SeatTime = AlignDuration + InsertDuration;
        readonly BeeFlowerGeometry[] flowers;
        readonly Camera camera;
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Material dotsMaterial, ribbonMaterial;
        public Material NectarMaterial { get; }
        readonly Vector3[] vertices = new Vector3[Capacity * 4];
        readonly Color[] colors = new Color[Capacity * 4];
        readonly LineRenderer[] flowerRings = new LineRenderer[2], streams = new LineRenderer[3];
        readonly LineRenderer hiveOutline, hiveWave;
        readonly LineRenderer pickupThread, hiveSeal;
        readonly LineRenderer honeyHover, gatherProgress;
        readonly LineRenderer[] waxEchoes = new LineRenderer[6];
        readonly Transform[] liquidDrops = new Transform[3];
        readonly Mesh dropletMesh;
        readonly HashSet<string> receipts = new HashSet<string>();
        readonly AudioSource notes, gathering;
        readonly AudioClip pickupSound, pollenSound, snapSound, landingSound, gatheringSound, stretchSound;
        readonly List<AudioClip> generatedSounds = new List<AudioClip>();
        readonly BeeInteractionAudio audioProfile;
        readonly float carriedScale;
        readonly AudioSource[] eventVoices = new AudioSource[6];
        int nextVoice;
        int count, previousCount, sourceFlower = -1, transferFlower = -1, collectionFlower = -1, landedFlower = -1;
        float clock, amount, intensity = 1f, volume = 0.45f, haptics = 0.6f, flowTime, nextHaptic;
        float sourceAt = -100f, transferAt = -100f, collectAt = -100f, depositAt = -100f, leaveAt = -100f, pickupAt = -100f;
        int releasedFlower = -1;
        Vector3 carrier, transferStart, depositStart;
        Quaternion depositRotation;
        Vector3 pickupStart;
        Quaternion pickupRotation;
        bool depositChimed, pickupChimed;
        bool depositSoundStarted;
        public bool Ready => dotsMaterial != null;
        public int VisibleMotes => count;
        public float Formation => amount;
        public bool DepositAnimating => clock - depositAt < SeatTime;
        // The saved slot is the interaction point in front of the opening. The
        // hidden original honey mesh supplies the actual seated centre and fit.
        public Vector3 SocketCenter => socketArt != null ? socketArt.transform.TransformPoint(socketArt.localBounds.center) : slot.position;
        public Quaternion SocketRotation => socketArt != null ? socketArt.transform.rotation : slot.rotation;
        public Vector3 SocketScale {
            get {
                if (socketArt == null) return Vector3.one;
                Vector3 size = socketArt.localBounds.size, artScale = socketArt.transform.lossyScale;
                Vector3 parentScale = visualParent != null ? visualParent.lossyScale : Vector3.one;
                return new Vector3(Fit(size.x,artScale.x,nectarBounds.size.x,parentScale.x),
                    Fit(size.y,artScale.y,nectarBounds.size.y,parentScale.y),Fit(size.z,artScale.z,nectarBounds.size.z,parentScale.z));
            }
        }
        static float Fit(float size, float artScale, float meshSize, float parentScale)
            => Mathf.Abs(size*artScale)/Mathf.Max(0.0001f,meshSize*Mathf.Abs(parentScale));
        Vector3 SocketRimPoint(float angle, float radius = 1f, float outset = 0.008f)
        {
            Vector3 e = socketArt != null ? Vector3.Scale(socketArt.localBounds.extents,socketArt.transform.lossyScale) : nectarBounds.extents;
            return SocketCenter + SocketRotation*new Vector3(Mathf.Cos(angle)*Mathf.Abs(e.x)*1.04f*radius,
                Mathf.Sin(angle)*Mathf.Abs(e.y)*1.04f*radius,Mathf.Abs(e.z)+outset);
        }
        float FormationDuration => audioProfile != null ? Mathf.Max(0.38f, audioProfile.formationImpactSeconds) : 0.38f;
        float PickupDuration => audioProfile != null ? Mathf.Max(0.34f, audioProfile.pickupImpactSeconds) : 0.34f;
        Vector3 landingPosition; float landingAt = -100f;
        public void SetLandingSurface(Vector3 position) { landingPosition = position; }
        public bool CollectionAnimating => clock - collectAt < FormationDuration + 0.02f;
        public bool PickupAnimating => clock - pickupAt < PickupDuration;
        public bool PickupThreadVisible => pickupThread != null && pickupThread.enabled;
        public float CollectionScale {
            get { float t = Mathf.Clamp01((clock - collectAt) / FormationDuration)-1f; return 1f + 2.1f*t*t*t + 1.1f*t*t; }
        }
        Transform Flower(int i) => i == 0 ? flowerA : flowerB;
        // Calibrated against the existing Daisy rig: Head is below the corolla.
        // Follow its saved scale without moving the bone or the GAMA interaction point.
        Vector3 FlowerSurface(int i) => flowers[i].Surface;
        public Vector3 NectarRestPosition(int flower) => flowers[flower].Nectar;

        public BeeHoneyFeedback(Transform parent, Camera view, Transform a, Transform b, Transform hive, Material nectarSource = null,
            Renderer socketGeometry = null, Mesh nectarMesh = null, BeeInteractionAudio audioSettings = null, float miniatureScale = 1f)
        {
            camera = view; flowerA = a; flowerB = b; slot = hive;
            visualParent = parent; socketArt = socketGeometry;
            nectarBounds = nectarMesh != null ? nectarMesh.bounds : new Bounds(Vector3.zero,new Vector3(0.16f,0.16f,0.07f));
            flowers = new[] { new BeeFlowerGeometry(a), new BeeFlowerGeometry(b) };
            root = new GameObject("Honey feedback (runtime)"); root.layer = 2;
            root.transform.SetParent(parent, false);
            var shader = Resources.Load<Shader>("HoneyFeedback");
            if (shader == null) { Debug.LogWarning("Bee Bloom: honey feedback shader missing; using basic cues."); return; }
            dotsMaterial = new Material(shader) { name = "Honey motes (runtime)", hideFlags = HideFlags.DontSave };
            ribbonMaterial = new Material(shader) { name = "Honey ribbons (runtime)", hideFlags = HideFlags.DontSave };
            ribbonMaterial.SetFloat("_Ribbon", 1f);
            var nectarShader = Resources.Load<Shader>("HoneyNectar");
            if (nectarShader != null) {
                NectarMaterial = new Material(nectarShader) {name="Nectar glaze (runtime)",hideFlags=HideFlags.DontSave};
                if (nectarSource != null) {
                    if (nectarSource.HasProperty("_BaseColor")) NectarMaterial.SetColor("_BaseColor",nectarSource.GetColor("_BaseColor"));
                    if (nectarSource.HasProperty("_BaseMap") && nectarSource.GetTexture("_BaseMap") != null) {
                        NectarMaterial.SetTexture("_BaseMap",nectarSource.GetTexture("_BaseMap"));
                        NectarMaterial.SetTextureScale("_BaseMap",nectarSource.GetTextureScale("_BaseMap"));
                        NectarMaterial.SetTextureOffset("_BaseMap",nectarSource.GetTextureOffset("_BaseMap"));
                    }
                }
                dropletMesh = DropletMesh();
                for (int i = 0; i < liquidDrops.Length; i++) {
                    var drop = new GameObject("Nectar liquid preview " + i); drop.layer = 2; drop.transform.SetParent(root.transform,false);
                    drop.AddComponent<MeshFilter>().sharedMesh = dropletMesh;
                    var draw = drop.AddComponent<MeshRenderer>(); draw.sharedMaterial = NectarMaterial;
                    draw.shadowCastingMode = ShadowCastingMode.Off; draw.receiveShadows = false;
                    liquidDrops[i] = drop.transform; drop.SetActive(false);
                }
            }
            mesh = new Mesh { name = "Honey feedback motes", hideFlags = HideFlags.DontSave }; mesh.MarkDynamic();
            var uv = new Vector2[Capacity * 4]; var triangles = new int[Capacity * 6];
            for (int i = 0; i < Capacity; i++) {
                int v = i * 4, t = i * 6;
                uv[v] = Vector2.zero; uv[v+1] = Vector2.right; uv[v+2] = Vector2.one; uv[v+3] = Vector2.up;
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
                triangles[t+3] = v; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.triangles = triangles;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = dotsMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            for (int i = 0; i < 2; i++) flowerRings[i] = Line("Flower warmth " + i, 41, true);
            for (int i = 0; i < 3; i++) streams[i] = Line("Nectar filament " + i, 20, false);
            hiveOutline = Line("Hive socket rim", 7, true); hiveWave = Line("Hive confirmation wave", 7, true);
            pickupThread = Line("Viscous nectar lift",25,false); hiveSeal = Line("Wax rim sealing",49,false);
            honeyHover = Line("Honey grab target",33,true);
            gatherProgress = Line("Nectar hold progress",41,false);
            for (int i = 0; i < waxEchoes.Length; i++) waxEchoes[i] = Line("Wax warmth " + i,7,true);
            carriedScale = Mathf.Clamp(miniatureScale, 0.05f, 1f);
            notes = Audio("Honey event audio"); gathering = Audio("Nectar gathering audio");
            audioProfile = audioSettings != null ? audioSettings : Resources.Load<BeeInteractionAudio>("BeeAudio/InteractionAudio");
            pickupSound = Clip(audioProfile != null ? audioProfile.honeyFormation : null, 0, 0.28f);
            pollenSound = Clip(audioProfile != null ? audioProfile.pollenPop : null, 1, 0.65f);
            snapSound = Clip(audioProfile != null ? audioProfile.hiveDeposit : null, 2, 0.46f);
            landingSound = Clip(audioProfile != null ? audioProfile.landingClip : null, 3, 0.16f);
            gatheringSound = Clip(audioProfile != null ? audioProfile.nectarSip : null, 4, 1.6f);
            stretchSound = Clip(audioProfile != null ? audioProfile.honeyPickup : null, 5, 0.32f);
            gathering.clip = gatheringSound; gathering.loop = true;
            for (int i = 0; i < eventVoices.Length; i++) eventVoices[i] = Audio("Spatial event voice " + i);
        }

        LineRenderer Line(string name, int points, bool loop)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = ribbonMaterial;
            line.useWorldSpace = true; line.positionCount = points; line.loop = loop;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch; line.enabled = false;
            return line;
        }
        AudioSource Audio(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            var audio = go.AddComponent<AudioSource>(); audio.playOnAwake = false;
            audio.spatialBlend = 1f; audio.dopplerLevel = 0; audio.minDistance = 0.25f; audio.maxDistance = 5f;
            audio.rolloffMode = AudioRolloffMode.Linear; return audio;
        }
        public void Configure(float visualIntensity, float audioVolume, float hapticStrength)
        { intensity = Mathf.Clamp(visualIntensity, 0f, 1.5f); volume = Mathf.Clamp01(audioVolume); haptics = Mathf.Clamp01(hapticStrength); }
        public void SetActive(bool active) { if (root != null) root.SetActive(active); }
        public void HoverHoney() { Pulse(XRNode.RightHand, 0.035f, 0.025f); }
        public void SetHoneyHover(bool ready, bool pending, Vector3 position)
        {
            if (!Ready || honeyHover == null) return;
            Quaternion facing = camera != null ? Quaternion.LookRotation(position - camera.transform.position) : Quaternion.identity;
            Ring(honeyHover, position, facing, pending ? 0.065f : 0.055f, 0.002f,
                ready ? Cream : Nectar, pending ? 0.4f : ready ? 0.8f : 0f);
        }

        // Only call this from an accepted transaction, never from button-down or proximity.
        public bool Confirm(string eventId, string kind, int flower, bool pollinated, Vector3 chunkPosition, Quaternion chunkRotation)
        {
            if (!Ready || string.IsNullOrEmpty(eventId) || !receipts.Add(eventId)) return false;
            if (kind == "visit" && flower >= 0) {
                sourceFlower = flower; sourceAt = clock + (pollinated ? 0.65f : 0f);
                if (pollinated) {
                    transferFlower = flower; transferAt = clock; transferStart = carrier;
                    Play(pollenSound, Flower(flower).position, 0.7f); Pulse(XRNode.RightHand, 0.12f, 0.1f);
                } else { Play(pollenSound, Flower(flower).position, 0.35f); Pulse(XRNode.RightHand, 0.045f, 0.06f); }
            } else if (kind == "collect") {
                collectionFlower = flower; collectAt = clock; amount = 0;
                Play(pickupSound, flower >= 0 ? NectarRestPosition(flower) : chunkPosition, 0.8f); Pulse(XRNode.RightHand, 0.15f, 0.08f);
            } else if (kind == "deposit") {
                pickupAt = -100f;
                depositAt = clock; depositStart = chunkPosition; depositRotation = chunkRotation;
                depositChimed = false;
                depositSoundStarted = false;
            } else if (kind == "pick") {
                pickupAt = clock; pickupStart = chunkPosition; pickupRotation = chunkRotation; pickupChimed = false;
                Play(stretchSound,chunkPosition,0.65f); Pulse(XRNode.RightHand,0.045f,0.06f);
            } else if (kind == "drop") pickupAt = -100f;
            return true;
        }

        public void DepositPose(out Vector3 position, out Quaternion rotation)
        {
            float age = Mathf.Max(0f,clock - depositAt);
            Quaternion seated = SocketRotation;
            Vector3 normal = seated*Vector3.forward;
            float halfDepth = socketArt != null ? Mathf.Abs(socketArt.localBounds.extents.z*socketArt.transform.lossyScale.z) : nectarBounds.extents.z;
            Vector3 mouth = SocketCenter + normal*Mathf.Max(0.16f,halfDepth+0.08f);
            if (age < AlignDuration) {
                float p = Mathf.SmoothStep(0f,1f,age/AlignDuration);
                position = Vector3.Lerp(depositStart,mouth,p); rotation = Quaternion.Slerp(depositRotation,seated,p);
            } else {
                float p = Mathf.SmoothStep(0f,1f,(age-AlignDuration)/InsertDuration);
                position = Vector3.Lerp(mouth,SocketCenter,p); rotation = seated;
            }
        }
        public void PickupPose(Transform hand, out Vector3 position, out Quaternion rotation)
        {
            float t = Mathf.Clamp01((clock-pickupAt)/PickupDuration), ease = 1f-Mathf.Pow(1f-t,3f);
            Vector3 target = hand.TransformPoint(new Vector3(0f,0.015f,0.12f));
            position = Vector3.Lerp(pickupStart,target,ease) + Vector3.up*(0.024f*Mathf.Sin(t*Mathf.PI));
            rotation = Quaternion.Slerp(pickupRotation,hand.rotation,ease);
        }
        public static void FollowHandPose(Vector3 current, Quaternion currentRotation, Transform hand, float dt,
            out Vector3 position, out Quaternion rotation)
        {
            Vector3 target = hand.TransformPoint(new Vector3(0f,0.015f,0.12f));
            Vector3 smoothed = Vector3.Lerp(current,target,1f-Mathf.Exp(-14f*Mathf.Max(0f,dt)));
            position = target + Vector3.ClampMagnitude(smoothed-target,0.035f);
            rotation = Quaternion.RotateTowards(hand.rotation,Quaternion.Slerp(currentRotation,hand.rotation,1f-Mathf.Exp(-25f*Mathf.Max(0f,dt))),10f);
        }
        public Vector3 NectarScale(string state)
        {
            if (state == "ready" && CollectionAnimating) return Vector3.one*carriedScale*Mathf.Max(0.05f,CollectionScale);
            if (state == "held" && PickupAnimating) {
                float stretch = Mathf.Sin(Mathf.Clamp01((clock-pickupAt)/PickupDuration)*Mathf.PI);
                return new Vector3(1f-stretch*0.055f,1f-stretch*0.055f,1f+stretch*0.14f)*carriedScale;
            }
            float seat = clock-depositAt;
            if (state == "stored") {
                Vector3 fit = Vector3.Lerp(Vector3.one*carriedScale,SocketScale,Mathf.SmoothStep(0f,1f,seat/AlignDuration));
                float squeeze = seat >= SeatTime && seat < SeatTime+0.25f ? Mathf.Sin((seat-SeatTime)/0.25f*Mathf.PI) : 0f;
                return Vector3.Scale(fit,new Vector3(1f+squeeze*0.015f,1f+squeeze*0.015f,1f-squeeze*0.035f));
            }
            return Vector3.one*carriedScale;
        }
        public float FlowerBloom(int flower)
        {
            float t = clock - transferAt - 0.48f;
            return flower == transferFlower && t >= 0f && t < 1.2f ? Mathf.Sin(t / 1.2f * Mathf.PI) : 0f;
        }
        public float FlowerRebound(int flower)
        {
            float t = clock - leaveAt;
            return flower == releasedFlower && t < 0.8f ? Mathf.Sin(t * 11f) * Mathf.Exp(-t * 5f) : 0f;
        }

        public void Step(float dt, bool live, int contact, float charge, bool waitingCollect, bool waitingVisit,
            int perched, bool settled, int pollenSource, int pollinatedMask, string chunkState,
            Vector3 chunkPosition, Transform hand, float snapRadius)
        {
            if (!Ready) return;
            dt = Mathf.Max(0f, dt); clock += dt; count = 0;
            float target = charge > 0f ? charge : waitingCollect ? 0.94f : 0f;
            amount = Mathf.MoveTowards(amount, target, dt * (target > amount ? 3.5f : 2.4f));
            flowTime += dt * (target > 0f ? 0.8f : -0.6f);
            carrier = chunkState == "held" ? chunkPosition - hand.right * 0.105f : hand.TransformPoint(new Vector3(-0.075f, 0.015f, 0.1f));
            int nowLanded = settled ? perched : -1;
            if (nowLanded != landedFlower) {
                if (landedFlower >= 0) { releasedFlower = landedFlower; leaveAt = clock; }
                if (nowLanded >= 0) { landingAt = clock; Play(landingSound, landingPosition, 0.55f); Pulse(XRNode.LeftHand, audioProfile != null ? audioProfile.landingHapticIntensity : 0.07f, 0.07f); }
                landedFlower = nowLanded;
            }
            for (int i = 0; i < 2; i++) {
                Vector3 centre = FlowerSurface(i);
                float near = live && contact == i
                    ? Mathf.Clamp01(1f - Vector3.Distance(camera != null ? camera.transform.position : hand.position, centre) / 2.8f) : 0f;
                float bloom = FlowerBloom(i), warmth = contact == i ? amount : 0f;
                Ring(flowerRings[i], centre + Vector3.up * 0.012f, Quaternion.Euler(90f,0f,0f),
                    0.075f + bloom * 0.055f, 0.0025f + bloom * 0.002f, Nectar, near * (0.13f + warmth * 0.55f + bloom * 0.5f));
                if ((pollinatedMask & (1 << i)) != 0) {
                    for (int j = 0; j < 3; j++) Dot(centre + Orbit(j*2.094f, 0.045f, 0.02f), 0.007f, Pollen, near * 0.6f);
                }
                if (live && near > 0f) for (int j = 0; j < 5; j++) {
                    float phase = clock * 0.45f + j * 1.2566f;
                    Dot(centre + Orbit(phase, 0.12f, 0.025f + 0.02f * Mathf.Sin(phase*1.7f)), 0.007f, Pollen, near * 0.6f);
                }
            }
            int gatheringFlower = contact >= 0 ? contact : collectionFlower;
            bool flowing = amount > 0.001f && gatheringFlower >= 0;
            gatherProgress.enabled = flowing && intensity > 0f;
            if (flowing) {
                Vector3 centre = FlowerSurface(gatheringFlower) + Vector3.up * 0.015f;
                for (int i = 0; i < gatherProgress.positionCount; i++) {
                    float angle = i / (float)(gatherProgress.positionCount - 1) * Mathf.PI * 2f * Mathf.Clamp01(waitingCollect ? 0.98f : charge);
                    gatherProgress.SetPosition(i, centre + new Vector3(Mathf.Cos(angle),0f,Mathf.Sin(angle)) * 0.095f);
                }
                Style(gatherProgress,0.004f,Cream,0.9f);
            }
            for (int i = 0; i < 3; i++) {
                var line = streams[i]; line.enabled = flowing && intensity > 0f;
                if (!flowing) continue;
                Vector3 centre = FlowerSurface(gatheringFlower);
                for (int j = 0; j < line.positionCount; j++) line.SetPosition(j, Stream(centre, i*2.094f, (float)j/(line.positionCount-1)));
                Style(line, 0.004f, Nectar, amount * 0.55f);
                for (int j = 0; j < 4; j++) {
                    float t = Mathf.Repeat(flowTime + j * 0.25f + i * 0.11f, 1f);
                    Dot(Stream(centre,i*2.094f,t), 0.012f + 0.007f*t, Cream, amount * Mathf.Sin(t*Mathf.PI));
                }
            }
            for (int i = 0; i < liquidDrops.Length; i++) {
                if (liquidDrops[i] == null) continue;
                bool showing = flowing && (chunkState == "none" || chunkState == "stored") && amount > 0.08f && intensity > 0f;
                liquidDrops[i].gameObject.SetActive(showing);
                if (!showing) continue;
                float angle = i*2.094f+flowTime;
                float merge = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0.35f,0.94f,amount));
                liquidDrops[i].position = NectarRestPosition(gatheringFlower) + Orbit(angle,0.045f*(1f-merge),0.012f*Mathf.Sin(angle*1.7f)*(1f-merge));
                liquidDrops[i].localScale = Vector3.one*carriedScale*Mathf.Lerp(0.1f,0.85f,amount);
            }
            if (gathering != null) {
                if (gatheringFlower >= 0) gathering.transform.position = Flower(gatheringFlower).position;
                float audioTarget = live && charge > 0f && !waitingCollect ? volume * (audioProfile != null ? audioProfile.sipVolume : 0.28f) : 0f;
                gathering.volume = Mathf.MoveTowards(gathering.volume, audioTarget, dt / Mathf.Max(0.025f, audioProfile != null ? audioProfile.fadeSeconds : 0.12f));
                gathering.pitch = Mathf.Lerp(0.96f, 1.04f, charge);
                if (audioTarget > 0.001f && !gathering.isPlaying && Application.isPlaying) gathering.Play();
                if (audioTarget == 0f && gathering.volume <= 0.001f && gathering.isPlaying) gathering.Stop();
            }
            if (live && charge > 0f && clock >= nextHaptic) { nextHaptic = clock + 0.18f; Pulse(XRNode.RightHand, 0.025f + charge*0.04f, 0.035f); }
            if (pollenSource >= 0 && pollenSource < 2) {
                for (int i = 0; i < 3; i++) {
                    float a = i * 2.39996f + clock * 0.65f;
                    Dot(carrier + Orbit(a, 0.015f + (i%3)*0.009f, 0.012f*Mathf.Sin(a*1.3f)), 0.006f + (i%3)*0.0015f, Pollen, 0.8f);
                }
            }
            // Anticipation motes stay at the flower. Actual transfer starts only after ACK.
            if (waitingVisit && contact >= 0) Dot(FlowerSurface(contact) + Vector3.up*0.04f, 0.018f, Cream, 0.35f);
            FlyPollen(sourceAt, sourceFlower, false);
            FlyPollen(transferAt, transferFlower, true);
            float landingAge = clock - landingAt;
            if (landingAge >= 0f && landingAge < 0.45f) {
                int particles = audioProfile != null ? audioProfile.pollenParticleCount : 8;
                for (int i = 0; i < particles; i++) {
                    float t = landingAge / 0.45f, angle = i * Mathf.PI * 2f / Mathf.Max(1, particles);
                    Dot(landingPosition + Orbit(angle, 0.015f + t * 0.055f, 0.01f + 0.035f * Mathf.Sin(t * Mathf.PI)), 0.004f * (1f - t), Nectar, 1f - t);
                }
            }
            float pop = clock - collectAt;
            if (collectionFlower >= 0 && pop >= 0f && pop < 0.55f) {
                for (int i = 0; i < 12; i++) {
                    float t = pop/0.55f, a = i * Mathf.PI/6f;
                    Dot(chunkPosition + Orbit(a, 0.025f + t*0.1f, 0.015f + 0.04f*Mathf.Sin(t*Mathf.PI)), 0.01f*(1f-t), Cream, 1f-t);
                }
            }
            float lift = clock-pickupAt;
            float threadDistance = Vector3.Distance(pickupStart,chunkPosition);
            bool stretching = chunkState == "held" && lift >= 0f && lift < PickupDuration && threadDistance < 0.55f;
            pickupThread.enabled = stretching && intensity > 0f;
            if (stretching) {
                for (int i = 0; i < pickupThread.positionCount; i++) {
                    float t = (float)i/(pickupThread.positionCount-1);
                    pickupThread.SetPosition(i,Vector3.Lerp(pickupStart,chunkPosition,t)-Vector3.up*(0.026f*Mathf.Sin(t*Mathf.PI)));
                }
                Style(pickupThread,Mathf.Lerp(0.009f,0.002f,lift/PickupDuration),Nectar,0.8f);
                pickupThread.endWidth *= 0.45f;
            }
            if (!pickupChimed && lift >= 0.28f && lift < 0.7f && chunkState == "held") {
                pickupChimed = true; Pulse(XRNode.RightHand,0.08f,0.045f);
            }
            if (chunkState == "held" && lift >= 0.3f && lift < 0.65f && threadDistance < 0.65f) {
                float t = (lift-0.3f)/0.35f;
                for (int i = 0; i < 4; i++) Dot(pickupStart+Orbit(i*1.57f,0.012f+t*0.028f,-t*t*0.045f),0.008f*(1f-t),Nectar,1f-t);
            }
            if (chunkState == "ready" || chunkState == "loose" || chunkState == "held") {
                float glimmer = Mathf.Pow(Mathf.Max(0f,Mathf.Sin(clock*2.5f)),8f);
                Vector3 toward = camera != null ? (camera.transform.position-chunkPosition).normalized : Vector3.back;
                Spark(chunkPosition + toward*0.055f + Vector3.up*0.045f,0.012f,Cream,glimmer*0.85f);
            }
            bool socketReady = live && chunkState == "held"
                && Vector3.Distance(chunkPosition, slot.position) <= snapRadius
                && (camera == null || Vector3.Distance(camera.transform.position,slot.position) <= 2f
                    && Vector3.Dot(camera.transform.position-slot.position,slot.forward) >= -0.05f);
            float snap = clock - depositAt;
            if (audioProfile != null && !depositSoundStarted && snap >= Mathf.Max(0f, SeatTime - audioProfile.depositImpactSeconds)
                && snap < SeatTime + 0.85f) {
                depositSoundStarted = true; Play(snapSound, slot.position, 0.85f);
            }
            if (!depositChimed && snap >= SeatTime && snap < SeatTime+0.85f) {
                depositChimed = true; if (audioProfile == null) Play(snapSound,slot.position,0.85f); Pulse(XRNode.RightHand,0.2f,0.1f);
            }
            if (NectarMaterial != null) {
                NectarMaterial.SetFloat("_Warmth",intensity*Mathf.Max(Mathf.Clamp01(1f-pop/0.55f),Mathf.Clamp01(1f-snap/1.2f)));
                NectarMaterial.SetFloat("_FlowTime",clock);
            }
            bool socketVisible = (live && chunkState == "held") || snap < SeatTime+0.85f;
            Style(hiveOutline, socketReady ? 0.009f : 0.004f, socketReady ? Cream : Nectar, socketVisible ? (socketReady ? 0.95f : 0.25f) : 0f);
            hiveOutline.enabled = socketVisible && intensity > 0f;
            for (int i = 0; i < 7; i++) hiveOutline.SetPosition(i,SocketRimPoint(i*Mathf.PI/3f+Mathf.PI/6f));
            Ring(hiveWave, SocketRimPoint(0f,0f), SocketRotation, 0.125f + Mathf.Clamp01((snap-SeatTime)/0.85f)*0.025f,
                0.006f, Cream, snap >= SeatTime && snap < SeatTime+0.85f ? (1f-(snap-SeatTime)/0.85f)*0.85f : 0f);
            hiveSeal.enabled = snap >= SeatTime && snap < SeatTime+0.95f && intensity > 0f;
            if (hiveSeal.enabled) {
                float fill = Mathf.Clamp01((snap-SeatTime)/0.5f);
                Style(hiveSeal,0.008f,Cream,Mathf.Clamp01((SeatTime+0.95f-snap)/0.35f)*0.85f);
                for (int i = 0; i < hiveSeal.positionCount; i++) {
                    float edge = Mathf.Min((float)i/(hiveSeal.positionCount-1),fill)*6f;
                    int segment = Mathf.Min(5,Mathf.FloorToInt(edge)); float blend = edge-segment;
                    Vector3 from = SocketRimPoint(segment*Mathf.PI/3f+Mathf.PI/6f);
                    Vector3 to = SocketRimPoint((segment+1)*Mathf.PI/3f+Mathf.PI/6f);
                    hiveSeal.SetPosition(i,Vector3.Lerp(from,to,blend));
                }
            }
            for (int i = 0; i < waxEchoes.Length; i++) {
                float t = (snap-SeatTime-0.05f-i*0.045f)/0.75f;
                float a = i*Mathf.PI/3f;
                Vector3 centre = SocketRimPoint(a+Mathf.PI/6f,1.3f);
                Ring(waxEchoes[i],centre,SocketRotation,0.025f,0.003f,Nectar,t > 0f && t < 1f ? Mathf.Sin(t*Mathf.PI)*0.4f : 0f);
            }
            if (snap >= SeatTime && snap < SeatTime+0.65f) for (int i = 0; i < 6; i++) {
                float a = i*Mathf.PI/3f;
                Dot(SocketRimPoint(a+Mathf.PI/6f),0.012f,Cream,(SeatTime+0.65f-snap)/0.65f);
            }
            if (socketReady) for (int i = 0; i < 6; i++) {
                float a = i*Mathf.PI/3f + clock*0.2f;
                Dot(SocketRimPoint(a,1.08f), 0.008f, Cream, 0.7f);
            }
            Flush();
        }

        Vector3 Stream(Vector3 centre, float angle, float t)
        {
            float radius = Mathf.Lerp(0.12f,0.012f,t);
            return centre + Orbit(angle + t*2.2f + flowTime*0.35f, radius, 0.014f + Mathf.SmoothStep(0f,1f,t)*0.1f*amount);
        }
        void FlyPollen(float started, int flower, bool transfer)
        {
            float age = clock - started;
            if (flower < 0 || age < 0f || age > 1.5f) return;
            Vector3 centre = FlowerSurface(flower) + Vector3.up*0.03f;
            for (int i = 0; i < 14; i++) {
                float t = Mathf.Clamp01((age - i*0.012f)/0.55f);
                if (t <= 0f || t >= 1f) continue;
                Vector3 start = transfer ? transferStart : centre, end = transfer ? centre : carrier;
                Vector3 position = Vector3.Lerp(start,end,t) + Vector3.up*(0.08f*Mathf.Sin(t*Mathf.PI));
                position += Orbit(i*2.39996f, 0.025f*Mathf.Sin(t*Mathf.PI), 0f);
                Dot(position, 0.009f, Pollen, Mathf.Sin(t*Mathf.PI));
            }
            float bloom = age - 0.5f;
            if (transfer && bloom >= 0f && bloom < 0.85f) for (int i = 0; i < 12; i++) {
                float t = bloom/0.85f, a = i*2.39996f;
                Dot(centre + Orbit(a,0.025f+t*0.13f,0.06f*Mathf.Sin(t*Mathf.PI)), 0.008f*(1f-t*0.5f), Pollen, 1f-t);
            }
        }
        static Vector3 Orbit(float angle, float radius, float height) => new Vector3(Mathf.Cos(angle)*radius,height,Mathf.Sin(angle)*radius);
        void Ring(LineRenderer line, Vector3 centre, Quaternion rotation, float radius, float width, Color color, float alpha)
        {
            line.enabled = alpha * intensity > 0.001f;
            if (!line.enabled) return;
            Style(line,width,color,alpha);
            for (int i = 0; i < line.positionCount; i++) {
                float a = i*Mathf.PI*2f/(line.positionCount-1);
                line.SetPosition(i,centre + rotation * new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));
            }
        }
        void Style(LineRenderer line, float width, Color color, float alpha)
        { line.startWidth = line.endWidth = width; color.a = Mathf.Clamp01(alpha*intensity); line.startColor = line.endColor = color; }
        void Dot(Vector3 position, float size, Color color, float alpha)
            => Sprite(position,size,size,color,alpha);
        void Spark(Vector3 position, float size, Color color, float alpha)
        { Sprite(position,size*0.15f,size*1.7f,color,alpha); Sprite(position,size*1.3f,size*0.15f,color,alpha); Dot(position,size*0.45f,color,alpha); }
        void Sprite(Vector3 position, float width, float height, Color color, float alpha)
        {
            if (count >= Capacity || alpha * intensity < 0.001f || width <= 0f || height <= 0f) return;
            if (camera != null && ((position-camera.transform.position).sqrMagnitude < 0.0625f || (position-camera.transform.position).sqrMagnitude > 36f)) return;
            Vector3 right = (camera != null ? camera.transform.right : Vector3.right)*width;
            Vector3 up = (camera != null ? camera.transform.up : Vector3.up)*height;
            int v = count++*4; color.a = Mathf.Clamp01(alpha*intensity);
            vertices[v] = root.transform.InverseTransformPoint(position-right-up); vertices[v+1] = root.transform.InverseTransformPoint(position+right-up);
            vertices[v+2] = root.transform.InverseTransformPoint(position+right+up); vertices[v+3] = root.transform.InverseTransformPoint(position-right+up);
            colors[v] = colors[v+1] = colors[v+2] = colors[v+3] = color;
        }
        void Flush()
        {
            for (int i = count*4; i < previousCount*4; i++) { colors[i] = Color.clear; vertices[i] = Vector3.zero; }
            previousCount = count; renderer.enabled = count > 0;
            mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
        }
        void Play(AudioClip clip, Vector3 position, float gain)
        {
            if (notes == null || !Application.isPlaying || clip == null) return;
            if (audioProfile != null) gain = clip == pollenSound ? audioProfile.pollenVolume
                : clip == pickupSound ? audioProfile.formationVolume : clip == snapSound ? audioProfile.depositVolume
                : clip == stretchSound ? audioProfile.pickupVolume : clip == landingSound ? audioProfile.landingVolume : gain;
            var voice = eventVoices[nextVoice++ % eventVoices.Length];
            if (voice == null) voice = notes;
            voice.Stop(); voice.transform.position = position; voice.clip = clip; voice.volume = volume * gain; voice.Play();
        }
        AudioClip Clip(AudioClip asset, int kind, float duration)
        {
            if (asset != null) return asset;
            var generated = Sound(kind, duration); generatedSounds.Add(generated); return generated;
        }
        void Pulse(XRNode node, float amplitude, float duration)
        {
            if (!Application.isPlaying || haptics <= 0f) return;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0,amplitude*haptics,duration);
        }
        static AudioClip Sound(int kind, float duration)
        {
            const int rate = 22050; var samples = new float[(int)(rate*duration)];
            for (int i = 0; i < samples.Length; i++) {
                float t = (float)i/rate, value = 0f;
                if (kind == 4) {
                    // Periodic low, rounded water-like bed; no sudden loop edge.
                    float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI*t/duration),2f);
                    value = (Mathf.Sin(t*2f*Mathf.PI*180f)*0.12f + Mathf.Sin(t*2f*Mathf.PI*360f)*0.04f)*envelope;
                } else {
                    int notes = kind == 1 ? 3 : kind == 2 ? 2 : 1;
                    for (int n = 0; n < notes; n++) {
                        float local = t-n*(kind == 1 ? 0.095f : 0.09f);
                        if (local < 0) continue;
                        float frequency = kind == 0 ? 620f : kind == 1 ? 660f*Mathf.Pow(1.25f,n) : kind == 2 ? (n == 0 ? 185f : 555f) : kind == 5 ? 150f+local*380f : 130f;
                        float envelope = (1f-Mathf.Exp(-local*160f))*Mathf.Exp(-local*(kind == 1 ? 9f : 22f));
                        value += (Mathf.Sin(local*frequency*2f*Mathf.PI)+0.2f*Mathf.Sin(local*frequency*4f*Mathf.PI))*envelope*0.25f;
                    }
                    value *= Mathf.Clamp01((duration-t)/0.025f);
                }
                samples[i] = Mathf.Clamp(value,-0.8f,0.8f);
            }
            var clip = AudioClip.Create("Honey feedback " + kind,samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
        static void Destroy(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        public void Dispose()
        {
            Destroy(root); Destroy(mesh); Destroy(dotsMaterial); Destroy(ribbonMaterial); Destroy(NectarMaterial); Destroy(dropletMesh);
            foreach (var generated in generatedSounds) Destroy(generated);
        }
        static Mesh DropletMesh()
        {
            const int sides = 12, rings = 8;
            var points = new Vector3[(sides+1)*(rings+1)]; var normals = new Vector3[points.Length]; var uv = new Vector2[points.Length];
            var indices = new int[sides*rings*6]; int index = 0;
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++) {
                float latitude = y*Mathf.PI/rings, longitude = x*Mathf.PI*2f/sides;
                int v = y*(sides+1)+x;
                normals[v] = new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude));
                points[v] = normals[v]*0.04f; uv[v] = new Vector2((float)x/sides,(float)y/rings);
                if (y == rings || x == sides) continue;
                int next = v+sides+1;
                indices[index++] = v; indices[index++] = v+1; indices[index++] = next;
                indices[index++] = v+1; indices[index++] = next+1; indices[index++] = next;
            }
            var result = new Mesh {name="Honey liquid droplet (runtime)",hideFlags=HideFlags.DontSave};
            result.vertices = points; result.normals = normals; result.uv = uv; result.triangles = indices; result.RecalculateBounds(); return result;
        }
    }
}
