namespace StoryEngine.Models
{
    /// <summary>
    /// A clickable location displayed on the expedition map.
    /// </summary>
    public class MapLocationDefinition
    {
        /// <summary>Unique id of the map location.</summary>
        public string Id { get; set; }

        /// <summary>Display name shown to the player.</summary>
        public string Name { get; set; }

        /// <summary>Story block where the player is sent after choosing this location.</summary>
        public string TargetBlock { get; set; }

        /// <summary>Short description shown in the map tooltip/details.</summary>
        public string Description { get; set; }

        /// <summary>X position on the map image.</summary>
        public int X { get; set; }

        /// <summary>Y position on the map image.</summary>
        public int Y { get; set; }

        /// <summary>Optional icon stored in the ZIP, for example images/icon_radio.png.</summary>
        public string Icon { get; set; }

        /// <summary>Optional condition required to unlock this location.</summary>
        public ConditionDefinition Condition { get; set; }
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name)
                ? Id
                : $"{Name}  ->  {TargetBlock}";
        }
        public List<EffectDefinition> TravelEffects { get; set; } = new List<EffectDefinition>();

        public string RandomEventCategory { get; set; }

        public bool OneTimeOnly { get; set; }

        public bool HideWhenLocked { get; set; }
    }
}