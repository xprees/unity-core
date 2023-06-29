using UnityEngine;

namespace Xprees.Core
{
    /// Base class for ScriptableObjects with a public description field visible only in Editor.
    public class DescriptionBaseSO : ScriptableObject, IResettable
    {
#if UNITY_EDITOR
        [Tooltip("Description of the ScriptableObject. Editor only.")]
        [TextArea]
        public string description;
#endif

        public virtual void ResetState()
        {
        }
    }
}