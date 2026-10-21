using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // Runtime view of the authored hive. Never changes a saved transform or material.
    public sealed class BeeHiveLayout : IDisposable
    {
        public readonly Renderer[] Cells;
        readonly bool[] enabledBefore;
        readonly Vector3 firstLocalPoint;
        readonly Transform originalMount;
        readonly Transform originalSlot;
        public int Count => Cells.Length;
        public BeeHiveLayout(Renderer first, Transform mount, Vector3 localPoint, Transform slot, Renderer[] authored)
        {
            originalMount = mount; firstLocalPoint = localPoint; originalSlot = slot;
            var cells = new List<Renderer>();
            if (first != null) cells.Add(first);
            if (authored != null && authored.Length > 0) cells.AddRange(authored.Where(r => r != null && r != first));
            else if (first != null && first.name == "Cylinder.001" && first.transform.parent != null) {
                // These are the seven nectar inserts in the current coconut asset;
                // Cylinder.010 is the shell and must never be hidden or filled.
                var names = new HashSet<string> { "Cylinder.003", "Cylinder.005", "Cylinder.008", "Cylinder.009", "Cylinder.011", "Cylinder.013" };
                cells.AddRange(first.transform.parent.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.transform.parent == first.transform.parent && names.Contains(r.name))
                    .OrderBy(r => r.name, StringComparer.Ordinal));
            }
            Cells = cells.Distinct().ToArray();
            enabledBefore = Cells.Select(r => r.enabled).ToArray();
        }
        public string Id(int i) => i == 0 ? "slot-a" : "slot-" + Cells[i].name;
        public Vector3 Position(int i) => i == 0
            ? originalMount != null ? originalMount.TransformPoint(firstLocalPoint) : originalSlot.position
            : Cells[i].transform.TransformPoint(Cells[i].localBounds.center) + Cells[i].transform.forward * 0.12f;
        public Quaternion Rotation(int i) => Cells[i].transform.rotation;
        public JArray Configuration() => new JArray(Enumerable.Range(0, Count).Select(i => new JObject {
            ["id"] = Id(i), ["position"] = BeeHoneyPilot.ToGama(Position(i)) }));
        public bool Filled(JObject state, int i) => state?["filled_slots"] is JArray ids
            ? ids.Any(x => (string)x == Id(i)) : i == 0 && (int?)state?["stored"] > 0;
        public int Index(string id) => Array.FindIndex(Cells, r => Id(Array.IndexOf(Cells,r)) == id);
        public int Select(JObject state, Vector3 hand, int previous)
        {
            int best = -1; float nearest = float.PositiveInfinity;
            for (int i = 0; i < Count; i++) {
                if (Filled(state,i)) continue;
                float d = Vector3.Distance(hand, Position(i));
                if (i == previous) d -= 0.04f;
                if (d < nearest) { best = i; nearest = d; }
            }
            return best;
        }
        public void Render(JObject state, int animatedCell)
        {
            for (int i = 0; i < Count; i++) Cells[i].enabled = Filled(state,i) && i != animatedCell;
        }
        public void Dispose() { for (int i = 0; i < Count; i++) if (Cells[i] != null) Cells[i].enabled = enabledBefore[i]; }
    }
}
