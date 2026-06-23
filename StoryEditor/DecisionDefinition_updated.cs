using System.Collections.Generic;

namespace StoryEngine.Models
{
    /// <summary>
    /// A choice the player can make from a story block.
    /// </summary>
    public class DecisionDefinition
    {
        /// <summary>Button label shown to the player</summary>
        public string Text { get; set; }

        /// <summary>Id of the block this decision leads to</summary>
        public string TargetBlock { get; set; }

        /// <summary>Optional icon filename (e.g. "sword.png")</summary>
        public string Icon { get; set; }

        /// <summary>Optional narrative text shown to the player after choosing this decision</summary>
        public string ResultText { get; set; }

        /// <summary>Optional narrative text shown after this decision is chosen (before moving to TargetBlock)</summary>
        public string ResultText { get; set; }

        /// <summary>If non-null, the decision is only shown when this condition is true</summary>
        public ConditionDefinition Condition { get; set; }

        /// <summary>Side-effects applied when this decision is chosen</summary>
        public List<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();

        public override string ToString()
        {
            string target = string.IsNullOrEmpty(TargetBlock) ? "(fără țintă)" : $"→ {TargetBlock}";
            string cond = Condition != null ? "  [condiționat]" : "";
            return $"{Text}  {target}{cond}";
        }
    }
}
