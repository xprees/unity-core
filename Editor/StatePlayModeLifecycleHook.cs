using System;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xprees.Core.Editor
{
    /// Centralized Editor runner that coordinates Play Mode entry and exit transitions for stateful ScriptableObjects.
    /// Resets objects marked with [ResetOnPlayMode] and restores mutated assets upon exiting Play Mode.
    /// Can be migrated to future Unity lifecycle hooks in future versions.
    [InitializeOnLoad]
    public static class StatePlayModeLifecycleHook
    {
        private readonly static ConcurrentDictionary<Type, PlayModeResetTiming> timingCache = new();

        static StatePlayModeLifecycleHook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    ResetPlayModeObjects(PlayModeResetTiming.EnterPlayMode);
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // Automatically revert all mutated ScriptableObjects to baseline before returning to Edit Mode
                    StateSnapshotService.RestoreAll();
                    ResetPlayModeObjects(PlayModeResetTiming.ExitPlayMode);
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    StateSnapshotService.ClearAll();
                    break;
            }
        }

        /// Resets loaded ScriptableObjects decorated with [ResetOnPlayMode] matching the specified timing.
        /// Persistent objects keep serialized values across sessions, but their transient state (e.g. event listeners)
        /// is cleared upon exiting Play Mode.
        public static void ResetPlayModeObjects(PlayModeResetTiming timing)
        {
            var loadedObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            foreach (var so in loadedObjects)
            {
                if (!so || so.IsStateless()) continue;

                var isPersistent = so.GetStateLifetime() == StateLifetime.Persistent;
                if (isPersistent && (timing & PlayModeResetTiming.ExitPlayMode) == 0) continue;

                var type = so.GetType();
                var configuredTiming = GetPlayModeTiming(type);
                if ((configuredTiming & timing) == 0) continue;

                var resetMethod = type.GetMethod("ResetToDefault", BindingFlags.Public | BindingFlags.Instance);
                if (resetMethod != null)
                {
                    if (!isPersistent) resetMethod.Invoke(so, null);
                    return;
                }

                if (so is IRuntimeStateOwner stateOwner)
                {
                    stateOwner.ClearTransientState();
                }
            }
        }

        public static PlayModeResetTiming GetPlayModeTiming(Type type)
        {
            if (timingCache.TryGetValue(type, out var timing)) return timing;

            var attr = type.GetCustomAttribute<ResetOnPlayModeAttribute>(true);
            timing = attr?.Timing ?? PlayModeResetTiming.None;
            timingCache[type] = timing;
            return timing;
        }
    }
}