using System.Collections.Generic;

namespace StoryEngine.Models
{
    /// <summary>
    /// AST-style condition node. Can be a leaf COMPARISON or a compound AND/OR.
    /// </summary>
    public class ConditionDefinition
    {
        /// <summary>COMPARISON | AND | OR</summary>
        public string Type { get; set; }

        // --- COMPARISON fields ---
        /// <summary>Key of the state property to compare (e.g. "health")</summary>
        public string Property { get; set; }

        /// <summary>Operator: &lt; | &lt;= | &gt; | &gt;= | == | !=</summary>
        public string Operator { get; set; }

        /// <summary>Right-hand side value</summary>
        public int Value { get; set; }

        // --- AND / OR fields ---
        /// <summary>Child conditions (used when Type is AND or OR)</summary>
        public List<ConditionDefinition> Operands { get; set; }
    }
}
