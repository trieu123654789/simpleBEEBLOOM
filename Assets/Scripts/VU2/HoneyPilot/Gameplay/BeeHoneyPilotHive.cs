using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyPilot
    {
        [Header("Full hive - runtime progression")]
        public bool fillWholeHive = true;
        [Tooltip("Optional ordered inserts. Empty discovers the seven inserts of the current coconut asset.")]
        public Renderer[] hiveCells;
        [Min(1f)] public float nectarRefillSeconds = 8f;
        [Min(0f)] public float gatherGraceSeconds = 0.18f;
        public bool requirePollinationForCompletion = true;
        public int HiveCapacity => (int?)State?["hive_capacity"] ?? (hive != null ? hive.Count : 1);
        public bool HiveFull => Stored >= HiveCapacity;
        public bool RoundComplete => HiveFull && (!requirePollinationForCompletion || ConfirmedPollinations >= 2);
        public string WorldSyncWarning { get; private set; } = "";
        public string PerchHint { get; private set; } = "";
        BeeHiveLayout hive;
        Transform savedSlot;
        int selectedCell;
        string releaseSlotId = "slot-a";
        float gatherInterrupted;
        bool collectNeedsRelease;
        // The legacy model accepts configure but silently ignores hive_slots.
        // Never advertise LIVE or play a collection against that incompatible state.
        void ValidateServerHive(JObject configuration, JObject state)
        {
            var requested = configuration["hive_slots"] as JArray;
            if (requested == null || requested.Count < 2) return;
            var confirmed = state?["hive_slots"] as JArray;
            if ((int?)state?["hive_capacity"] != requested.Count || confirmed == null
                || confirmed.Count != requested.Count || !(state["filled_slots"] is JArray))
                throw new InvalidOperationException("GAMA_MODEL_OUTDATED: restart the SIMPLE/GAMA experiment to load the full-hive model. Saved progress is kept.");
            for (int i = 0; i < requested.Count; i++)
                if ((string)requested[i]["id"] != (string)confirmed[i]["id"])
                    throw new InvalidOperationException("GAMA hive geometry does not match the requested cells.");
        }
        bool hivePresentationActive;
        void BeginHive()
        {
            if (!fillWholeHive || slotDecoration == null) return;
            hive = new BeeHiveLayout(slotDecoration,slotMount,slotMountLocalPoint,slot,hiveCells);
            if (hive.Count < 2) { hive.Dispose(); hive = null; return; }
            savedSlot = slot;
            slot = new GameObject("Runtime hive interaction target").transform;
            slot.SetParent(transform,false); selectedCell = 0;
            slot.SetPositionAndRotation(hive.Position(0),hive.Rotation(0));
            hive.Render(null,-1); hivePresentationActive = true;
        }
        void EndHive()
        {
            hive?.Dispose(); hive = null;
            if (savedSlot != null) { if (slot != null) ReleaseRuntime(slot.gameObject); slot = savedSlot; savedSlot = null; }
            hivePresentationActive = false;
        }
        string CurrentSlotId => hive != null ? hive.Id(selectedCell) : "slot-a";
        void StepHive()
        {
            if (hive == null) return;
            bool pin = Busy && ((string)pendingRequest["kind"] == "deposit" || (string)pendingRequest["kind"] == "drop");
            int seated = hive.Index((string)State?["deposited_slot"] ?? "slot-a");
            if (ChunkState == "stored" && seated >= 0) selectedCell = seated;
            else if (!pin) {
                int next = hive.Select(State, chunk != null && ChunkState == "held" ? chunk.position : rightHand.position,selectedCell);
                if (next >= 0) selectedCell = next;
            }
            slot.SetPositionAndRotation(hive.Position(selectedCell),hive.Rotation(selectedCell));
            honeyFeedback?.SetSocket(hive.Cells[selectedCell]);
            hive.Render(State,ChunkState == "stored" ? selectedCell : -1);
        }
        bool SocketReachable(Vector3 piece, Vector3 player, int index)
        {
            Vector3 target = hive != null ? hive.Position(index) : slot.position;
            Vector3 forward = hive != null ? hive.Rotation(index) * Vector3.forward : slot.forward;
            return Vector3.Distance(piece,target) <= snapRadius && Vector3.Distance(player,target) <= 2f
                && (hive == null || Vector3.Dot(player-target,forward) >= -0.05f);
        }
        bool HasLineOfSight(Vector3 from, Vector3 target)
        {
            Vector3 delta = target-from;
            if (delta.magnitude < 0.08f) return true;
            foreach (var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude-0.06f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
                Transform t = hit.transform;
                if (t.IsChildOf(beeRoot) || t == beeRoot || t.IsChildOf(rightHand)) continue;
                if (trackedCamera != null && t.IsChildOf(trackedCamera.transform)) continue;
                return false;
            }
            return true;
        }
        string renderedChunkId;
        async Task SyncWorld(IHoneyClient source, JObject world, CancellationToken token)
        {
            if (world == null) return;
            try {
                var reply = await source.AskAsync("sync_world", new JObject { ["world_json"] = world.ToString(Newtonsoft.Json.Formatting.None) });
                if (token.IsCancellationRequested || source != client) return;
                worldSync.Accept((JObject)reply["world_sync"]); WorldSyncWarning = "";
            } catch (Exception ex) { if (!token.IsCancellationRequested && source == client) WorldSyncWarning = ex.Message; }
        }
        bool TryDropSurface(Vector3 point, out Vector3 landing)
        {
            landing = point;
            var hits = Physics.RaycastAll(point,Vector3.down,2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach (var hit in hits) {
                if (hit.transform.IsChildOf(beeRoot) || hit.normal.y < 0.55f) continue;
                if (hit.transform.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0) return false;
                landing = hit.point + Vector3.up*0.06f; return true;
            }
            return false;
        }
        public string ObjectiveHint => !Live ? "Đang nối SIMPLE/GAMA"
            : RoundComplete ? "Tổ đầy • Thụ phấn hoàn tất"
            : HiveFull ? "Tổ đầy • Thụ phấn " + ConfirmedPollinations + "/2"
            : ChunkState == "held" ? "Đưa mật vào ô trống • Thụ phấn " + ConfirmedPollinations + "/2"
            : ChunkState == "ready" || ChunkState == "loose" ? "Giữ grip phải để cầm mật"
            : (int?)State?["pollen_source"] >= 0 && ConfirmedPollinations < 2 ? "Mang phấn sang hoa khác"
            : "Đậu hoa • Lấy mật • Lấp tổ";
        string ConnectionHint => ConnectionStatus.Contains("GAMA_MODEL_OUTDATED") ? "GAMA DANG DUNG MODEL CU\nKHOI DONG LAI EXPERIMENT SIMPLE/GAMA"
            : ConnectionStatus.Contains("geometry") ? "BO TRI DA DOI - KIEM TRA LUOT CHOI"
            : client is HoneySimpleClient simple && simple.WaitingForAdmission ? "CHO THEM KINH VAO SIMPLE"
            : "DANG NOI LAI SIMPLE/GAMA";
    }
}
