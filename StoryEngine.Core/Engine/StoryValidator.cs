using System.Collections.Generic;
using System.Linq;
using StoryEngine.Models;

namespace StoryEngine.Engine
{
    /// <summary>
    /// Validates a StoryDefinition before saving or running.
    /// The editor calls this; errors are shown to the author.
    /// </summary>
    public class StoryValidator
    {
        public class ValidationResult
        {
            public bool IsValid => Errors.Count == 0;
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
        }

        public ValidationResult Validate(StoryDefinition story)
        {
            var result = new ValidationResult();

            ValidateProperties(story, result);
            ValidateStartBlock(story, result);
            ValidateBlocks(story, result);

            return result;
        }

        // ------------------------------------------------------------------ //

        private void ValidateProperties(StoryDefinition story, ValidationResult result)
        {
            var keys = new HashSet<string>();
            foreach (var prop in story.Properties)
            {
                if (string.IsNullOrWhiteSpace(prop.Key))
                    result.Errors.Add("A property is missing its Key.");

                if (!keys.Add(prop.Key))
                    result.Errors.Add($"Duplicate property key: '{prop.Key}'.");

                if (prop.Min > prop.Max)
                    result.Errors.Add($"Property '{prop.Key}': Min ({prop.Min}) > Max ({prop.Max}).");

                if (prop.Initial < prop.Min || prop.Initial > prop.Max)
                    result.Errors.Add($"Property '{prop.Key}': Initial value {prop.Initial} is outside [{prop.Min}, {prop.Max}].");
            }
        }

        private void ValidateStartBlock(StoryDefinition story, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(story.StartBlock))
            {
                result.Errors.Add("No StartBlock defined.");
                return;
            }

            if (!story.Blocks.Any(b => b.Id == story.StartBlock))
                result.Errors.Add($"StartBlock '{story.StartBlock}' does not exist in Blocks.");
        }

        private void ValidateBlocks(StoryDefinition story, ValidationResult result)
        {
            var blockIds = new HashSet<string>(story.Blocks.Select(b => b.Id));
            var propertyKeys = new HashSet<string>(story.Properties.Select(p => p.Key));
            var seenIds = new HashSet<string>();

            foreach (var block in story.Blocks)
            {
                // Unique IDs
                if (string.IsNullOrWhiteSpace(block.Id))
                {
                    result.Errors.Add("A block is missing its Id.");
                    continue;
                }

                if (!seenIds.Add(block.Id))
                    result.Errors.Add($"Duplicate block Id: '{block.Id}'.");

                // Final blocks should have no decisions
                if (block.IsFinal && block.Decisions.Count > 0)
                    result.Warnings.Add($"Block '{block.Id}' is marked Final but has {block.Decisions.Count} decision(s) (they will be ignored).");

                // Non-final, non-category blocks should have at least one decision
                if (!block.IsFinal && string.IsNullOrEmpty(block.EventCategory) && block.Decisions.Count == 0)
                    result.Warnings.Add($"Block '{block.Id}' has no decisions and is not Final. Players will be stuck.");

                foreach (var decision in block.Decisions)
                    ValidateDecision(decision, block.Id, blockIds, propertyKeys, result);

                // Validate OnMinBlock / OnMaxBlock references (checked via property definitions)
            }

            // Validate property redirect targets
            foreach (var prop in story.Properties)
            {
                if (!string.IsNullOrEmpty(prop.OnMinBlock) && !blockIds.Contains(prop.OnMinBlock))
                    result.Errors.Add($"Property '{prop.Key}' OnMinBlock '{prop.OnMinBlock}' does not exist.");

                if (!string.IsNullOrEmpty(prop.OnMaxBlock) && !blockIds.Contains(prop.OnMaxBlock))
                    result.Errors.Add($"Property '{prop.Key}' OnMaxBlock '{prop.OnMaxBlock}' does not exist.");
            }
        }

        private void ValidateDecision(DecisionDefinition decision, string blockId,
            HashSet<string> blockIds, HashSet<string> propertyKeys, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(decision.Text))
                result.Errors.Add($"Block '{blockId}': A decision is missing its Text.");

            if (string.IsNullOrWhiteSpace(decision.TargetBlock))
                result.Errors.Add($"Block '{blockId}': Decision '{decision.Text}' has no TargetBlock.");
            else if (!blockIds.Contains(decision.TargetBlock))
                result.Errors.Add($"Block '{blockId}': Decision '{decision.Text}' targets unknown block '{decision.TargetBlock}'.");

            foreach (var effect in decision.Effects)
            {
                if (!propertyKeys.Contains(effect.Property))
                    result.Errors.Add($"Block '{blockId}', Decision '{decision.Text}': Effect references unknown property '{effect.Property}'.");
            }

            if (decision.Condition != null)
                ValidateCondition(decision.Condition, blockId, decision.Text, propertyKeys, result);
        }

        private void ValidateCondition(ConditionDefinition cond, string blockId, string decisionText,
            HashSet<string> propertyKeys, ValidationResult result)
        {
            switch (cond.Type)
            {
                case "COMPARISON":
                    if (!propertyKeys.Contains(cond.Property))
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Condition references unknown property '{cond.Property}'.");
                    break;
                case "AND":
                case "OR":
                    if (cond.Operands == null || cond.Operands.Count == 0)
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': {cond.Type} condition has no operands.");
                    else
                        foreach (var child in cond.Operands)
                            ValidateCondition(child, blockId, decisionText, propertyKeys, result);
                    break;
                default:
                    result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Unknown condition type '{cond.Type}'.");
                    break;
            }
        }
    }
}
