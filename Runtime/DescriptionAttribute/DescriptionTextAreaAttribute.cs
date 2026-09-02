using UnityEngine;

namespace Xprees.Core.DescriptionAttribute
{
    /// Marks a multiline string field to be drawn as a collapsible foldout in the Inspector,
    /// showing a single-line preview when collapsed and a full text area when expanded.
    public class DescriptionTextAreaAttribute : PropertyAttribute
    {
        public readonly int expandedMinLines;
        public readonly int expandedMaxLines;

        public DescriptionTextAreaAttribute(int expandedMinLines = 3, int expandedMaxLines = 10)
        {
            this.expandedMinLines = expandedMinLines;
            this.expandedMaxLines = expandedMaxLines;
        }
    }
}