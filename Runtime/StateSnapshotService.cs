using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xprees.Core
{
    // TODO later consider adding limits to not blow up memory - probably not an issue
    // TODO consider using OdinSerializer for more robust serialization in future
    /// Core engine service for zero-boilerplate capturing and in-place restoration
    /// of ScriptableObject serialized state between scenario runs.
    public static class StateSnapshotService
    {
        private readonly static Dictionary<int, string> snapshots = new();
        private readonly static HashSet<int> restoringEntities = new();
        private readonly static ConcurrentDictionary<Type, FieldInfo[]> snapshotIgnoreFieldsCache = new();
#if UNITY_EDITOR
        private readonly static Dictionary<int, ScriptableObject> trackedTargets = new();
#endif

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
#if UNITY_EDITOR
                trackedTargets[entityId] = target;
#endif
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

            // Re-entrancy guard to prevent infinite mutual recursion (e.g. scenario resetting itself)
            if (!restoringEntities.Add(entityId)) return false;

            try
            {
                RestoreFromJson(json, target);

                if (target is IRuntimeStateOwner stateOwner)
                {
                    stateOwner.ClearTransientState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateSnapshotService] Failed to restore snapshot for '{target.name}' ({target.GetType().Name}): {ex.Message}",
                    target);
                return false;
            }
            finally
            {
                restoringEntities.Remove(entityId);
            }
        }

        /// Restores all currently tracked ScriptableObjects to their baseline state.
        public static int RestoreAll()
        {
            var restoredCount = 0;
#if UNITY_EDITOR
            foreach (var kvp in trackedTargets)
            {
                var target = kvp.Value;
                if (!target)
                {
                    target = EditorUtility.EntityIdToObject(kvp.Key) as ScriptableObject;
                }

                if (!target || !snapshots.TryGetValue(kvp.Key, out var json)) continue;
                if (!restoringEntities.Add(kvp.Key)) continue;

                try
                {
                    RestoreFromJson(json, target);

                    if (target is IRuntimeStateOwner stateOwner)
                    {
                        stateOwner.ClearTransientState();
                    }

                    EditorUtility.ClearDirty(target);
                    restoredCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[StateSnapshotService] Failed to restore target '{target.name}': {ex.Message}", target);
                }
                finally
                {
                    restoringEntities.Remove(kvp.Key);
                }
            }
#endif
            return restoredCount;
        }

        /// Restores serialized fields from JSON while preserving fields marked with [SnapshotIgnore].
        private static void RestoreFromJson(string json, ScriptableObject target)
        {
            var ignoredFields = GetSnapshotIgnoreFields(target.GetType());
            object[] preservedValues = null;
            if (ignoredFields.Length > 0)
            {
                preservedValues = new object[ignoredFields.Length];
                for (var i = 0; i < ignoredFields.Length; i++)
                {
                    preservedValues[i] = ignoredFields[i].GetValue(target);
                }
            }

            JsonUtility.FromJsonOverwrite(json, target);
            if (ignoredFields.Length <= 0 || preservedValues == null) return;

            for (var i = 0; i < ignoredFields.Length; i++)
            {
                ignoredFields[i].SetValue(target, preservedValues[i]);
            }
        }

        private static FieldInfo[] GetSnapshotIgnoreFields(Type type)
        {
            if (snapshotIgnoreFieldsCache.TryGetValue(type, out var fields))
            {
                return fields;
            }

            var list = new List<FieldInfo>();
            var current = type;
            while (current != null && current != typeof(ScriptableObject) && current != typeof(Object) && current != typeof(object))
            {
                var currentFields =
                    current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in currentFields)
                {
                    if (Attribute.IsDefined(field, typeof(SnapshotIgnoreAttribute)))
                    {
                        list.Add(field);
                    }
                }

                current = current.BaseType;
            }

            var result = list.Count > 0 ? list.ToArray() : Array.Empty<FieldInfo>();
            snapshotIgnoreFieldsCache[type] = result;
            return result;
        }

        /// Checks whether a baseline snapshot currently exists for the specified ScriptableObject.
        public static bool HasSnapshot(ScriptableObject target) => target && snapshots.ContainsKey(target.GetEntityId());

        /// Gets the raw JSON snapshot string for debugging / state inspector inspection.
        public static string GetSnapshotJson(ScriptableObject target)
        {
            if (target && snapshots.TryGetValue(target.GetEntityId(), out var json))
            {
                return json;
            }

            return null;
        }

        /// Evicts a specific ScriptableObject snapshot from memory.
        public static void Evict(ScriptableObject target)
        {
            if (target == null) return;
            var entityId = target.GetEntityId();
            snapshots.Remove(entityId);
            restoringEntities.Remove(entityId);
#if UNITY_EDITOR
            trackedTargets.Remove(entityId);
#endif
        }

        /// Clears all stored state snapshots.
        public static void ClearAll()
        {
            snapshots.Clear();
            restoringEntities.Clear();
#if UNITY_EDITOR
            trackedTargets.Clear();
#endif
        }

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
            // When exiting Play Mode, restore before scene objects begin tearing down
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreAll();
                return;
            }

            // Once fully transitioned back to Edit Mode, all scene unload and OnDisable/OnDestroy
            // lifecycle calls have finished. Re-run RestoreAll() to guarantee any mutations occurring
            // during teardown are cleanly reverted, then clear the snapshot tracking.
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                var count = RestoreAll();
                if (count > 0)
                {
                    Debug.Log($"[StateSnapshotService] Restored {count} stateful ScriptableObjects back to pre-play baseline.");
                }

                ClearAll();
            }
        }
#endif
    }
}