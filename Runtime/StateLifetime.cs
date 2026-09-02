using System;

namespace Xprees.Core
{
    /// Defines the retention lifecycle and reset scope of a ScriptableObject's runtime state.
    public enum StateLifetime
    {
        /// Scoped strictly to the active Scenario.
        /// Automatically captured and restored to its pristine baseline every time a Scenario starts or restarts.
        /// Example: Scenario-specific variables, emails, dialog progression, and interactables.
        Scenario = 0,

        /// Persists across multiple scenarios during the entire game execution session,
        /// restored only when quitting the application or starting a new game session from the Main Menu.
        /// Example: global player profile, per session settings etc.
        Session = 1,

        /// Completely ignored by the runtime state snapshot system.
        /// Represents purely static/constant authored content or data persisted externally (e.g., disk save files / cloud saves).
        /// Example: Audio clips, localization tables, immutable item database definitions.
        Persistent = 2,
    }

    /// Explicitly declares the state lifetime for an entire ScriptableObject type.
    /// Overrides per-instance serialized lifetime fields.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class StatefulLifetimeAttribute : Attribute
    {
        public StateLifetime Lifetime { get; }

        public StatefulLifetimeAttribute(StateLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }

    /// Marks a field on a ScriptableObject to be excluded from automated state snapshots.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SnapshotIgnoreAttribute : Attribute
    {
    }

    /// Marks a ScriptableObject type as entirely stateless/immutable, skipping it during reachability walks.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class StatelessAssetAttribute : Attribute
    {
    }
}