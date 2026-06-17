using System.Collections.Generic;

namespace StoryEngine.Models
{
    /// <summary>
    /// Root object. This is exactly what gets serialized to JSON and zipped.
    /// </summary>
    public class StoryDefinition
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }

        /// <summary>Id of the block where the game starts</summary>
        public string StartBlock { get; set; }

        /// <summary>All tracked state properties (food, water, health, morale, ...)</summary>
        public List<StatePropertyDefinition> Properties { get; set; } = new List<StatePropertyDefinition>();

        /// <summary>All story blocks — the nodes of the directed graph</summary>
        public List<StoryBlock> Blocks { get; set; } = new List<StoryBlock>();
    }
}
