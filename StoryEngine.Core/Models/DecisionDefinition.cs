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

        /// <summary>
        /// Optional text describing the outcome of this decision, shown at the
        /// TOP of the next block once the player arrives there (e.g. "You ate
        /// a proper meal and feel better."). If null/empty, the engine falls
        /// back to an auto-generated summary of the Effects.
        /// </summary>
        public string ResultText { get; set; }

        /// <summary>Id of the block this decision leads to</summary>
        public string TargetBlock { get; set; }

        /// <summary>Optional icon filename (e.g. "sword.png")</summary>
        public string Icon { get; set; }

        /// <summary>If non-null, the decision is only shown when this condition is true</summary>
        public ConditionDefinition Condition { get; set; }

        /// <summary>Side-effects applied when this decision is chosen</summary>
        public List<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();
    }
}
