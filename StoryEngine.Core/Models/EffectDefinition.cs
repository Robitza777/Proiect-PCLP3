namespace StoryEngine.Models
{
    /// <summary>
    /// Describes a single side-effect applied to a state property when a decision is taken.
    /// </summary>
    public class EffectDefinition
    {
        /// <summary>Key of the state property to modify (e.g. "food")</summary>
        public string Property { get; set; }

        /// <summary>ADD (relative) or SET (absolute)</summary>
        public EffectType Type { get; set; }

        /// <summary>Amount to add or the value to set</summary>
        public int Value { get; set; }
    }

    public enum EffectType
    {
        ADD,
        SET
    }
}
