namespace StoryEngine.Models
{
    /// <summary>
    /// Defines a single tracked resource (e.g. food, water, health, morale).
    /// </summary>
    public class StatePropertyDefinition
    {
        /// <summary>Unique identifier used in conditions and effects (e.g. "health")</summary>
        public string Key { get; set; }

        /// <summary>Human-readable label shown in the HUD</summary>
        public string DisplayName { get; set; }

        public int Min { get; set; }
        public int Max { get; set; }
        public int Initial { get; set; }

        // HUD display settings
        public bool ShowInHud { get; set; } = true;
        /// <summary>Display order in the HUD (lower = leftmost)</summary>
        public int HudOrder { get; set; }
        /// <summary>Optional icon filename for the HUD</summary>
        public string HudIcon { get; set; }

        // Automatic redirect blocks when the property hits its boundary
        /// <summary>Block to jump to when value reaches Min (e.g. death block when health = 0)</summary>
        public string OnMinBlock { get; set; }

        /// <summary>Block to jump to when value reaches Max (optional)</summary>
        public string OnMaxBlock { get; set; }
    }
}
