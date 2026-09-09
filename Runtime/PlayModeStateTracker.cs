#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xprees.Core
{
    /// Thread-safe and serialization-safe tracker for play mode state.
    /// Accessing Unity's Application.isPlaying during ISerializationCallbackReceiver throws UnityException.
    /// This pure C# static field can be safely read from any thread or serialization callback.
    public static class PlayModeStateTracker
    {
#if UNITY_EDITOR
        public static bool IsPlaying { get; private set; }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            IsPlaying = EditorApplication.isPlaying;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            IsPlaying = change is
                PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.ExitingEditMode;
        }
#else
        // We must have runtime fallback to code to compile
        public static bool IsPlaying => true;
#endif
    }
}