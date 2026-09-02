using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xprees.Core
{
    /// Core engine service for zero-boilerplate capturing and in-place restoration
    /// of ScriptableObject serialized state between scenario runs.
    public static class StateSnapshotService
    {
        private readonly static Dictionary<int, string> snapshots = new();

        /// Total number of currently tracked baseline snapshots in memory.
        public static int SnapshotCount => snapshots.Count;

        /// Captures the baseline-serialized state of the ScriptableObject if not already captured.
        public static bool EnsureCaptured(ScriptableObject target)
        {
            if (!target || target.IsStateless() || target.GetStateLifetime() == StateLifetime.Persistent)
            {
                return false;
            }

            var entityId = target.GetEntityId();
            if (snapshots.ContainsKey(entityId))
            {
                return false;
            }

            try
            {
                var json = JsonUtility.ToJson(target);
                snapshots[entityId] = json;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateSnapshotService] Failed to capture snapshot for '{target.name}' ({target.GetType().Name}): {ex.Message}",
                    target);
                return false;
            }
        }

        /// Restores the ScriptableObject to its captured baseline state in-place.
        /// If no baseline has been captured yet, the current state is captured as baseline.
        public static bool Restore(ScriptableObject target)
        {
            if (!target || target.IsStateless() || target.GetStateLifetime() == StateLifetime.Persistent)
            {
                return false;
            }

            var entityId = target.GetEntityId();
            if (!snapshots.TryGetValue(entityId, out var json))
            {
                EnsureCaptured(target);
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, target);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateSnapshotService] Failed to restore snapshot for '{target.name}' ({target.GetType().Name}): {ex.Message}",
                    target);
                return false;
            }
        }

        /// Checks whether a baseline snapshot currently exists for the specified ScriptableObject.
        public static bool HasSnapshot(ScriptableObject target) => target && snapshots.ContainsKey(target.GetEntityId());

        /// Gets the raw JSON snapshot string for debugging / state inspector inspection.
        public static string GetSnapshotJson(ScriptableObject target)
        {
            if (target != null && snapshots.TryGetValue(target.GetEntityId(), out var json))
            {
                return json;
            }

            return null;
        }

        /// Evicts a specific ScriptableObject snapshot from memory (e.g. on Addressables unload).
        public static void Evict(ScriptableObject target)
        {
            if (target == null) return;
            snapshots.Remove(target.GetEntityId());
        }

        /// Clears all stored state snapshots.
        public static void ClearAll() => snapshots.Clear();

        /// Alias for ClearAll to provide clear naming.
        public static void ClearAllSnapshots() => ClearAll();

        /// Automatically called by Unity when Enter Play Mode Options (Disable Domain Reload) is active,
        /// or when the game initializes.
        /// SubsystemRegistration executes before any Awake/OnEnable calls when entering Play Mode,
        /// ensuring static state dictionaries from previous play sessions or editor runs are completely purged.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ClearAll();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitEditorLifecycle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // When exiting Play Mode back to Edit Mode, purge all baseline snapshots
            // so any subsequent Inspector edits in Edit Mode immediately become the new baseline.
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                ClearAll();
            }
        }
#endif
    }

    /// Utility extensions for resolving lifecycle and stateless status of ScriptableObjects.
    public static class StateLifetimeExtensions
    {
        public static StateLifetime GetStateLifetime(this ScriptableObject so)
        {
            if (!so) return StateLifetime.Scenario;

            var attr = so.GetType().GetCustomAttribute<StatefulLifetimeAttribute>(true);
            if (attr != null) return attr.Lifetime;

            if (so is DescriptionBaseSO descSo) return descSo.Lifetime;

            return StateLifetime.Scenario;
        }

        public static bool IsStateless(this ScriptableObject so)
        {
            if (!so) return true;
            return so.GetType().GetCustomAttribute<StatelessAssetAttribute>(true) != null;
        }
    }
}