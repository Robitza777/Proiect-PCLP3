using System;
using System.Collections.Generic;
using System.Linq;
using StoryEngine.Models;

namespace StoryEngine.Engine
{
    /// <summary>
    /// Core game engine: evaluates conditions, applies effects, and drives state transitions.
    /// This class is intentionally UI-agnostic — no WinForms references.
    /// </summary>
    public class GameEngine
    {
        private readonly StoryDefinition _story;
        private readonly Random _random = new Random();

        // Quick lookup: blockId -> StoryBlock
        private readonly Dictionary<string, StoryBlock> _blockMap;

        // Quick lookup: propertyKey -> StatePropertyDefinition
        private readonly Dictionary<string, StatePropertyDefinition> _propMap;

        public GameState State { get; private set; }

        public GameEngine(StoryDefinition story)
        {
            _story = story;
            _blockMap = story.Blocks.ToDictionary(b => b.Id);
            _propMap  = story.Properties.ToDictionary(p => p.Key);
        }

        // ------------------------------------------------------------------ //
        //  New game / restart
        // ------------------------------------------------------------------ //

        public void StartNewGame()
        {
            State = GameState.CreateNew(_story);
        }

        public void LoadGame(GameState savedState)
        {
            State = savedState;
        }

        // ------------------------------------------------------------------ //
        //  Current block
        // ------------------------------------------------------------------ //

        public StoryBlock GetCurrentBlock()
        {
            if (!_blockMap.TryGetValue(State.CurrentBlock, out var block))
                throw new InvalidOperationException($"Block '{State.CurrentBlock}' not found in story.");
            return block;
        }

        /// <summary>
        /// Returns only the decisions whose conditions are satisfied in the current state.
        /// </summary>
        public List<DecisionDefinition> GetAvailableDecisions()
        {
            var block = GetCurrentBlock();
            return block.Decisions
                .Where(d => d.Condition == null || EvaluateCondition(d.Condition))
                .ToList();
        }

        // ------------------------------------------------------------------ //
        //  Player action: choose a decision
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Applies the effects of a decision and navigates to its target block.
        /// Returns the block now active (may differ from decision.TargetBlock after redirects).
        /// </summary>
        public StoryBlock ChooseDecision(DecisionDefinition decision)
        {
            ApplyEffects(decision.Effects);
            State.CurrentBlock = decision.TargetBlock;
            State.Day++;

            // Redirect check: properties that have hit min/max
            string redirect = GetPropertyRedirect();
            if (redirect != null)
                State.CurrentBlock = redirect;

            var block = GetCurrentBlock();
            if (block.IsFinal)
                State.IsGameOver = true;

            return block;
        }

        // ------------------------------------------------------------------ //
        //  Random event selection (Milestone 2 feature)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Picks a random block from the given event category whose conditions are satisfied.
        /// Returns null if no valid block is found.
        /// </summary>
        public StoryBlock PickRandomEvent(string category)
        {
            var candidates = _story.Blocks
                .Where(b => b.EventCategory == category)
                .Where(b => b.Decisions.Any(d => d.Condition == null || EvaluateCondition(d.Condition)))
                .ToList();

            if (candidates.Count == 0) return null;
            return candidates[_random.Next(candidates.Count)];
        }

        // ------------------------------------------------------------------ //
        //  Condition evaluation
        // ------------------------------------------------------------------ //

        public bool EvaluateCondition(ConditionDefinition condition)
        {
            switch (condition.Type)
            {
                case "AND":
                    return condition.Operands.All(EvaluateCondition);

                case "OR":
                    return condition.Operands.Any(EvaluateCondition);

                case "COMPARISON":
                    return EvaluateComparison(condition);

                default:
                    throw new ArgumentException($"Unknown condition type: '{condition.Type}'");
            }
        }

        private bool EvaluateComparison(ConditionDefinition c)
        {
            if (!State.Properties.TryGetValue(c.Property, out int current))
                throw new ArgumentException($"Unknown property '{c.Property}' in condition.");

            switch (c.Operator)
            {
                case "<":  return current <  c.Value;
                case "<=": return current <= c.Value;
                case ">":  return current >  c.Value;
                case ">=": return current >= c.Value;
                case "==": return current == c.Value;
                case "!=": return current != c.Value;
                default:
                    throw new ArgumentException($"Unknown operator '{c.Operator}'.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Effect application
        // ------------------------------------------------------------------ //

        public void ApplyEffects(IEnumerable<EffectDefinition> effects)
        {
            foreach (var effect in effects)
                ApplyEffect(effect);
        }

        private void ApplyEffect(EffectDefinition effect)
        {
            if (!State.Properties.ContainsKey(effect.Property))
                throw new ArgumentException($"Unknown property '{effect.Property}' in effect.");

            if (!_propMap.TryGetValue(effect.Property, out var propDef))
                throw new ArgumentException($"No definition for property '{effect.Property}'.");

            int current = State.Properties[effect.Property];
            int raw = effect.Type == EffectType.ADD
                ? current + effect.Value
                : effect.Value;

            // Clamp to [min, max]
            State.Properties[effect.Property] = Math.Max(propDef.Min, Math.Min(propDef.Max, raw));
        }

        // ------------------------------------------------------------------ //
        //  Automatic redirects triggered by property boundaries
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Checks all properties for boundary violations after effects are applied.
        /// Returns the redirect block id, or null if no redirect is needed.
        /// First found wins (check min before max for each property, in definition order).
        /// </summary>
        private string GetPropertyRedirect()
        {
            foreach (var propDef in _story.Properties)
            {
                int value = State.Properties[propDef.Key];

                if (value <= propDef.Min && !string.IsNullOrEmpty(propDef.OnMinBlock))
                    return propDef.OnMinBlock;

                if (value >= propDef.Max && !string.IsNullOrEmpty(propDef.OnMaxBlock))
                    return propDef.OnMaxBlock;
            }
            return null;
        }
    }
}
