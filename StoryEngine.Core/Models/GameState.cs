using System.Collections.Generic;

namespace StoryEngine.Models
{
    /// <summary>
    /// Mutable runtime state. Kept separate from StoryDefinition so saves are small.
    /// </summary>
    public class GameState
    {
        /// <summary>Current values keyed by property Key (e.g. { "health": 75, "food": 30 })</summary>
        public Dictionary<string, int> Properties { get; set; } = new Dictionary<string, int>();

        /// <summary>Id of the block currently displayed</summary>
        public string CurrentBlock { get; set; }

        /// <summary>How many days (turns) have elapsed</summary>
        public int Day { get; set; }

        /// <summary>True once IsFinal block has been reached</summary>
        public bool IsGameOver { get; set; }

        /// <summary>
        /// Author-written outcome text of the last decision (e.g. "You ate a
        /// proper meal and feel better."). Null if the decision had no
        /// ResultText, or right after StartNewGame/restart.
        /// </summary>
        public string LastResultText { get; set; }

        /// <summary>
        /// Auto-generated summary of the effects applied by the last decision
        /// (e.g. "Health -5,  Food +10,  + Lantern"). Always computed when
        /// effects exist, independent of LastResultText. Null if the decision
        /// had no effects (or all were clamped to no-op), or right after
        /// StartNewGame/restart.
        /// </summary>
        public string LastEffectsSummary { get; set; }

        /// <summary>
        /// Creates a fresh GameState from a StoryDefinition (used on New Game / Restart).
        /// </summary>
        public static GameState CreateNew(StoryDefinition story)
        {
            var state = new GameState
            {
                CurrentBlock = story.StartBlock,
                Day = 1,
                IsGameOver = false,
                LastResultText = null,
                LastEffectsSummary = null
            };

            foreach (var prop in story.Properties)
                state.Properties[prop.Key] = prop.Initial;

            return state;
        }
    }
}