using System;

namespace Xprees.Core
{
    [Flags]
    public enum PlayModeResetTiming
    {
        /// No reset will occur on Play Mode transitions
        None = 0,

        /// Reset state when entering Play Mode
        EnterPlayMode = 1 << 0,

        /// Reset state when exiting Play Mode
        ExitPlayMode = 1 << 1,

        /// Reset state on both entering and exiting Play Mode
        Both = EnterPlayMode | ExitPlayMode,
    }

    /// Controls whether and when a stateful ScriptableObject (such as a Variable) resets to its authored default value during Editor Play Mode transitions.
    /// Hooked and executed centrally by StateSnapshotService.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ResetOnPlayModeAttribute : Attribute
    {
        public PlayModeResetTiming Timing { get; }

        public ResetOnPlayModeAttribute(PlayModeResetTiming timing = PlayModeResetTiming.EnterPlayMode)
        {
            Timing = timing;
        }
    }
}