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
        /// Outcome text of the last decision taken, to be shown at the top of
        /// CurrentBlock's text. Null on a fresh game (no decision taken yet)
        /// or right after StartNewGame/restart.
        /// </summary>
        public string LastResultText { get; set; }

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
                LastResultText = null
            };

            foreach (var prop in story.Properties)
                state.Properties[prop.Key] = prop.Initial;

            return state;
        }
    }
}
