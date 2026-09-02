using System.Reflection;
using UnityEngine;
using Xprees.Core.DescriptionAttribute;

namespace Xprees.Core
{
    /// Base class for ScriptableObjects with a public description field visible only in Editor.
    /// Supports lifecycle-scoped state management.
    public class DescriptionBaseSO : ScriptableObject, IResettable
    {
#if UNITY_EDITOR
        [Tooltip("Description of the ScriptableObject. Editor only.")]
        [DescriptionTextArea]
        public string description;
#endif

        [Tooltip("Defines how long this ScriptableObject's runtime state persists before being automatically restored.")]
        [SerializeField] private StateLifetime lifetime = StateLifetime.Scenario;

        public StateLifetime Lifetime
        {
            get
            {
                var attr = GetType().GetCustomAttribute<StatefulLifetimeAttribute>(true);
                return attr?.Lifetime ?? lifetime;
            }
            set => lifetime = value;
        }

        public virtual void ResetState()
        {
        }
    }
}