using System.Collections.Generic;

namespace StoryEngine.Models
{
    /// <summary>
    /// A single node in the story graph. Can be a narrative event, a final ending, or a random-pool event.
    /// </summary>
    public class StoryBlock
    {
        /// <summary>Unique identifier for this block (e.g. "day1.intro", "ending.death")</summary>
        public string Id { get; set; }

        /// <summary>Narrative text shown to the player</summary>
        public string Text { get; set; }

        /// <summary>If true, reaching this block ends the game</summary>
        public bool IsFinal { get; set; }

        /// <summary>Optional background image filename</summary>
        public string BackgroundImage { get; set; }

        /// <summary>Available choices from this block (may be empty for final blocks)</summary>
        public List<DecisionDefinition> Decisions { get; set; } = new List<DecisionDefinition>();

        /// <summary>
        /// Optional category for random event pooling (e.g. "Bandits", "Disease", "Expedition").
        /// Null means the block is a fixed narrative node, not part of any random pool.
        /// </summary>
        public string EventCategory { get; set; }
    }
}
