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
                    CaptureBaselines();
                    ResetPlayModeObjects(PlayModeResetTiming.EnterPlayMode);
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // Automatically revert all mutated ScriptableObjects to baseline before returning to Edit Mode
                    StateSnapshotService.RestoreAll();
                    ClearTransientStateOfLoadedObjects();
                    ResetPlayModeObjects(PlayModeResetTiming.ExitPlayMode);
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    StateSnapshotService.ClearAll();
                    break;
            }
        }

        /// Captures the baseline of every loaded stateful asset when play mode starts.
        /// Scenario starts capture the assets they reach, but test scenes (a computer opened without a scenario) mutate serialized state
        /// (lists of emails, chats, apps...) that nothing captured, so it was never restored on exit. Capturing is idempotent and skips
        /// Persistent/stateless assets. Graphs are excluded: their serialized content is authored only and they reset through ClearTransientState.
        public static void CaptureBaselines()
        {
            foreach (var so in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                if (so is DescriptionBaseSO) StateSnapshotService.EnsureCaptured(so);
            }
        }

        /// With domain reload disabled, non-serialized runtime state (parsers, cancellation sources, caches...) of an asset survives into the next play session.
        /// Snapshots only cover assets that were captured during the session (usually through a scenario start), so assets used outside a scenario
        /// (test scenes, manually opened apps) would never be cleared. This clears every loaded stateful asset, it's idempotent for already restored ones.
        /// Persistent assets are skipped: their transient state (e.g. event listeners) is wired in OnEnable and must survive.
        public static void ClearTransientStateOfLoadedObjects()
        {
            foreach (var so in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                if (!so || so is not IRuntimeStateOwner stateOwner) continue;
                if (so.IsStateless() || so.GetStateLifetime() == StateLifetime.Persistent) continue;

                try
                {
                    stateOwner.ClearTransientState();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, so);
                }
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
                    continue;
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