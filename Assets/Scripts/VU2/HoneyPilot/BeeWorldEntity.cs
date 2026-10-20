using System;
using System.Collections.Generic;
using UnityEngine;

namespace LetItBee.HoneyPilot
{
    // Explicit opt-in: decorative meshes are never mistaken for simulation actors.
    [ExecuteAlways, DisallowMultipleComponent, AddComponentMenu("Bee Bloom/World Entity")]
    public sealed class BeeWorldEntity : MonoBehaviour
    {
        public enum EntityKind { Bee, Flower, Hive }
        public EntityKind kind = EntityKind.Flower;
        [Tooltip("Optional point to track, e.g. a daisy's Head bone. Defaults to this object.")]
        public Transform anchor;
        [Tooltip("Logical species identifier; presentation names are not scientific identification.")]
        public string speciesId = "";
        [SerializeField, HideInInspector] string entityId;
        static readonly HashSet<BeeWorldEntity> active = new();
        public static IEnumerable<BeeWorldEntity> Active => active;
        public string Id => entityId;
        public Vector3 Position => anchor != null ? anchor.position : transform.position;
        public void RenewIdentity() => entityId = "entity-" + Guid.NewGuid().ToString("N");
        void Reset() { RenewIdentity(); }
        void OnValidate() { if (string.IsNullOrEmpty(entityId)) RenewIdentity(); }
        void OnEnable()
        {
            if (string.IsNullOrEmpty(entityId)) RenewIdentity();
            // Instantiating a prefab at runtime copies serialized IDs. Give only
            // the new instance its own ID, retained through disable/re-enable.
            foreach (var other in active)
                if (other != null && other != this && other.Id == Id) { RenewIdentity(); break; }
            active.Add(this);
        }
        void OnDisable() { active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearRegistry() { active.Clear(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RestoreRegistry()
        {
            // Also support Enter Play Mode with domain/scene reload disabled.
            foreach (var entity in FindObjectsByType<BeeWorldEntity>(FindObjectsSortMode.None))
                if (entity.isActiveAndEnabled) entity.OnEnable();
        }
    }
}
