using UnityEngine;
using Xprees.Core.DescriptionAttribute;

namespace Xprees.Core
{
    /// Base class for ScriptableObjects with a public description field visible only in Editor.
    /// Supports lifecycle-scoped state management.
    public class DescriptionBaseSO : ScriptableObject
    {
#if UNITY_EDITOR
        [SnapshotIgnore]
        [Tooltip("Description of the ScriptableObject. Editor only.")]
        [DescriptionTextArea]
        public string description;
#endif

        [Tooltip("Defines how long this ScriptableObject's runtime state persists before being automatically restored.")]
        [SerializeField] private StateLifetime lifetime = StateLifetime.Scenario;

        public StateLifetime ConfiguredLifetime => lifetime;

        public StateLifetime Lifetime
        {
            get => this.GetStateLifetime();
            set => lifetime = value;
        }
    }
}