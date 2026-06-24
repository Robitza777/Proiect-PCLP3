using System.Collections.Generic;
using System.IO;
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

            if (story == null)
            {
                result.Errors.Add("Story is null.");
                return result;
            }

            ValidateMetadata(story, result);
            ValidateProperties(story, result);
            ValidateStartBlock(story, result);
            ValidateBlocks(story, result);
            ValidateMap(story, result);

            return result;
        }

        // ------------------------------------------------------------------ //

        private void ValidateMetadata(StoryDefinition story, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(story.Title))
                result.Warnings.Add("Story title is empty.");
        }

        private void ValidateProperties(StoryDefinition story, ValidationResult result)
        {
            if (story.Properties == null)
            {
                result.Errors.Add("Story Properties list is null.");
                return;
            }

            var keys = new HashSet<string>();

            foreach (var prop in story.Properties)
            {
                if (string.IsNullOrWhiteSpace(prop.Key))
                    result.Errors.Add("A property is missing its Key.");

                if (!string.IsNullOrWhiteSpace(prop.Key) && !keys.Add(prop.Key))
                    result.Errors.Add($"Duplicate property key: '{prop.Key}'.");

                if (prop.Min > prop.Max)
                    result.Errors.Add($"Property '{prop.Key}': Min ({prop.Min}) > Max ({prop.Max}).");

                if (prop.Initial < prop.Min || prop.Initial > prop.Max)
                    result.Errors.Add($"Property '{prop.Key}': Initial value {prop.Initial} is outside [{prop.Min}, {prop.Max}].");

                if (!string.IsNullOrEmpty(prop.HudIcon) && !IsValidImageExtension(prop.HudIcon))
                    result.Warnings.Add($"Property '{prop.Key}': HudIcon '{prop.HudIcon}' does not have a supported image extension.");
            }
        }

        private void ValidateStartBlock(StoryDefinition story, ValidationResult result)
        {
            if (story.Blocks == null)
            {
                result.Errors.Add("Story Blocks list is null.");
                return;
            }

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
            if (story.Blocks == null)
                return;

            var blockIds = new HashSet<string>(story.Blocks
                .Where(b => !string.IsNullOrWhiteSpace(b.Id))
                .Select(b => b.Id));

            var propertyKeys = new HashSet<string>();

            if (story.Properties != null)
            {
                foreach (var prop in story.Properties)
                    if (!string.IsNullOrWhiteSpace(prop.Key))
                        propertyKeys.Add(prop.Key);
            }

            var seenIds = new HashSet<string>();

            foreach (var block in story.Blocks)
            {
                if (string.IsNullOrWhiteSpace(block.Id))
                {
                    result.Errors.Add("A block is missing its Id.");
                    continue;
                }

                if (!seenIds.Add(block.Id))
                    result.Errors.Add($"Duplicate block Id: '{block.Id}'.");

                if (string.IsNullOrWhiteSpace(block.Text))
                    result.Warnings.Add($"Block '{block.Id}' has empty text.");

                if (!string.IsNullOrEmpty(block.BackgroundImage) && !IsValidImageExtension(block.BackgroundImage))
                    result.Warnings.Add($"Block '{block.Id}': BackgroundImage '{block.BackgroundImage}' does not have a supported image extension.");

                if (block.Decisions == null)
                {
                    result.Errors.Add($"Block '{block.Id}' has null Decisions list.");
                    continue;
                }

                if (block.IsFinal && block.Decisions.Count > 0)
                    result.Warnings.Add($"Block '{block.Id}' is marked Final but has {block.Decisions.Count} decision(s) (they will be ignored).");

                if (!block.IsFinal && string.IsNullOrEmpty(block.EventCategory) && block.Decisions.Count == 0)
                    result.Warnings.Add($"Block '{block.Id}' has no decisions and is not Final. Players will be stuck.");

                foreach (var decision in block.Decisions)
                    ValidateDecision(decision, block.Id, blockIds, propertyKeys, result);
            }

            if (story.Properties != null)
            {
                foreach (var prop in story.Properties)
                {
                    if (!string.IsNullOrEmpty(prop.OnMinBlock) && !blockIds.Contains(prop.OnMinBlock))
                        result.Errors.Add($"Property '{prop.Key}' OnMinBlock '{prop.OnMinBlock}' does not exist.");

                    if (!string.IsNullOrEmpty(prop.OnMaxBlock) && !blockIds.Contains(prop.OnMaxBlock))
                        result.Errors.Add($"Property '{prop.Key}' OnMaxBlock '{prop.OnMaxBlock}' does not exist.");
                }
            }
        }

        private void ValidateDecision(DecisionDefinition decision, string blockId,
            HashSet<string> blockIds, HashSet<string> propertyKeys, ValidationResult result)
        {
            if (decision == null)
            {
                result.Errors.Add($"Block '{blockId}': A decision is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(decision.Text))
                result.Errors.Add($"Block '{blockId}': A decision is missing its Text.");

            if (string.IsNullOrWhiteSpace(decision.TargetBlock))
                result.Errors.Add($"Block '{blockId}': Decision '{decision.Text}' has no TargetBlock.");
            else if (!blockIds.Contains(decision.TargetBlock))
                result.Errors.Add($"Block '{blockId}': Decision '{decision.Text}' targets unknown block '{decision.TargetBlock}'.");

            if (!string.IsNullOrEmpty(decision.Icon) && !IsValidImageExtension(decision.Icon))
                result.Warnings.Add($"Block '{blockId}', Decision '{decision.Text}': Icon '{decision.Icon}' does not have a supported image extension.");

            if (decision.Effects != null)
            {
                foreach (var effect in decision.Effects)
                {
                    if (effect == null)
                    {
                        result.Errors.Add($"Block '{blockId}', Decision '{decision.Text}': A null effect was found.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(effect.Property))
                        result.Errors.Add($"Block '{blockId}', Decision '{decision.Text}': Effect is missing Property.");
                    else if (!propertyKeys.Contains(effect.Property))
                        result.Errors.Add($"Block '{blockId}', Decision '{decision.Text}': Effect references unknown property '{effect.Property}'.");
                }
            }

            if (decision.Condition != null)
                ValidateCondition(decision.Condition, blockId, decision.Text, propertyKeys, result);
        }

        private void ValidateMap(StoryDefinition story, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(story.MapBackground) && !IsValidImageExtension(story.MapBackground))
                result.Warnings.Add($"MapBackground '{story.MapBackground}' does not have a supported image extension.");

            if (story.MapLocations == null)
                return;

            var blockIds = new HashSet<string>();

            if (story.Blocks != null)
            {
                foreach (var block in story.Blocks)
                    if (!string.IsNullOrWhiteSpace(block.Id))
                        blockIds.Add(block.Id);
            }

            var propertyKeys = new HashSet<string>();

            if (story.Properties != null)
            {
                foreach (var prop in story.Properties)
                    if (!string.IsNullOrWhiteSpace(prop.Key))
                        propertyKeys.Add(prop.Key);
            }

            var locationIds = new HashSet<string>();

            foreach (var location in story.MapLocations)
            {
                if (location == null)
                {
                    result.Errors.Add("A map location is null.");
                    continue;
                }

                string locationLabel = string.IsNullOrWhiteSpace(location.Id)
                    ? "(missing id)"
                    : location.Id;

                if (string.IsNullOrWhiteSpace(location.Id))
                    result.Errors.Add("A map location is missing its Id.");
                else if (!locationIds.Add(location.Id))
                    result.Errors.Add($"Duplicate map location Id: '{location.Id}'.");

                if (string.IsNullOrWhiteSpace(location.Name))
                    result.Errors.Add($"Map location '{locationLabel}' is missing its Name.");

                if (string.IsNullOrWhiteSpace(location.TargetBlock))
                    result.Errors.Add($"Map location '{locationLabel}' has no TargetBlock.");
                else if (!blockIds.Contains(location.TargetBlock))
                    result.Errors.Add($"Map location '{locationLabel}' targets unknown block '{location.TargetBlock}'.");

                if (location.X < 0 || location.Y < 0)
                    result.Errors.Add($"Map location '{locationLabel}' has invalid coordinates ({location.X}, {location.Y}).");

                if (!string.IsNullOrEmpty(location.Icon) && !IsValidImageExtension(location.Icon))
                    result.Warnings.Add($"Map location '{locationLabel}': Icon '{location.Icon}' does not have a supported image extension.");

                if (location.Condition != null)
                    ValidateCondition(location.Condition, "Map", locationLabel, propertyKeys, result);
            }
        }

        private void ValidateCondition(ConditionDefinition cond, string blockId, string decisionText,
            HashSet<string> propertyKeys, ValidationResult result)
        {
            if (cond == null)
                return;

            switch (cond.Type)
            {
                case "COMPARISON":
                    if (string.IsNullOrWhiteSpace(cond.Property))
                    {
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Condition is missing Property.");
                    }
                    else if (cond.Property != "day" && !propertyKeys.Contains(cond.Property))
                    {
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Condition references unknown property '{cond.Property}'.");
                    }

                    if (!IsValidOperator(cond.Operator))
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Condition has unknown operator '{cond.Operator}'.");
                    break;

                case "AND":
                case "OR":
                    if (cond.Operands == null || cond.Operands.Count == 0)
                    {
                        result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': {cond.Type} condition has no operands.");
                    }
                    else
                    {
                        foreach (var child in cond.Operands)
                            ValidateCondition(child, blockId, decisionText, propertyKeys, result);
                    }
                    break;

                default:
                    result.Errors.Add($"Block '{blockId}', Decision '{decisionText}': Unknown condition type '{cond.Type}'.");
                    break;
            }
        }

        private bool IsValidOperator(string op)
        {
            return op == "<" || op == "<=" || op == ">" || op == ">=" || op == "==" || op == "!=";
        }

        private bool IsValidImageExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return true;

            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext == ".png"
                || ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".bmp"
                || ext == ".gif";
        }
    }
}