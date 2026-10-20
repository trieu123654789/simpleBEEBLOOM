using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BeeBloom.HoneyPilot
{
    // One full, replaceable observation snapshot. Gameplay state stays in GAMA.
    public sealed class BeeWorldSync
    {
        public const string Schema = "beebloom-world-v1";
        public const int MaxEntities = 256;
        public string Source { get; } = Guid.NewGuid().ToString("N");
        public long Sequence { get; private set; }
        string acknowledged;
        JObject pending;
        public JObject Acknowledgement { get; private set; }

        public static JObject Entity(string id, string kind, string label, Vector3 position, string species = "")
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
                throw new InvalidOperationException("World entity has an invalid position: " + id);
            return new JObject { ["id"] = id, ["kind"] = kind, ["label"] = label,
                ["species"] = species, ["position"] = BeeHoneyPilot.ToGama(position) };
        }
        public JArray Capture(BeeHoneyPilot pilot)
        {
            var entities = new List<JObject> {
                Entity("player-bee","bee","Player",pilot.InteractionPosition),
                Entity("flower-a","flower",pilot.flowerAHead.name,pilot.flowerAHead.position,pilot.speciesId),
                Entity("flower-b","flower",pilot.flowerBHead.name,pilot.flowerBHead.position,pilot.speciesId),
                Entity("slot-a","hive","Hive",pilot.slot.position)
            };
            foreach (var item in BeeWorldEntity.Active.ToArray()) {
                if (item == null || !item.isActiveAndEnabled || item.gameObject.scene != pilot.gameObject.scene) continue;
                // Marking an existing pilot object must not count it twice.
                Transform at = item.anchor != null ? item.anchor : item.transform;
                if (Contains(item.transform,pilot.beeRoot) || Contains(item.transform,pilot.flowerAHead)
                    || Contains(item.transform,pilot.flowerBHead) || Contains(item.transform,pilot.slotMount)
                    || at == pilot.slot) continue;
                entities.Add(Entity(item.Id,item.kind.ToString().ToLowerInvariant(),item.name,item.Position,item.speciesId));
            }
            return new JArray(entities.OrderBy(x=>(string)x["id"],StringComparer.Ordinal));
        }
        static bool Contains(Transform owner, Transform target) => target != null && (owner == target || target.IsChildOf(owner));

        public JObject Prepare(JArray entities, bool force = false)
        {
            // Retries send exactly the same sequence/content until acknowledged.
            if (pending != null) return (JObject)pending.DeepClone();
            if (entities.Count > MaxEntities) throw new InvalidOperationException("World sync supports up to " + MaxEntities + " entities in this first version.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (JObject entity in entities) {
                string id = (string)entity["id"], kind = (string)entity["kind"];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id) || (kind != "bee" && kind != "flower" && kind != "hive"))
                    throw new InvalidOperationException("Invalid or duplicate world entity: " + id);
            }
            string content = entities.ToString(Formatting.None);
            if (!force && content == acknowledged) return null;
            pending = new JObject { ["schema"] = Schema, ["source"] = Source,
                ["sequence"] = ++Sequence, ["entities"] = entities.DeepClone() };
            return (JObject)pending.DeepClone();
        }
        public void Accept(JObject reply)
        {
            if (pending == null || reply == null || (bool?)reply["accepted"] != true
                || (string)reply["source"] != Source || (long?)reply["sequence"] != Sequence)
                throw new InvalidOperationException("World sync acknowledgement does not match the current snapshot.");
            acknowledged = pending["entities"].ToString(Formatting.None);
            Acknowledgement = (JObject)reply.DeepClone(); pending = null;
        }
        public void Reconnect() { pending = null; acknowledged = null; }
    }
}
