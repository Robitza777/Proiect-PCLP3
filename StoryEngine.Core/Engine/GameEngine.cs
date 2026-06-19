using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private readonly Dictionary<string, StoryBlock> _blockMap;
        private readonly Dictionary<string, StatePropertyDefinition> _propMap;

        public GameState State { get; private set; }

        public GameEngine(StoryDefinition story)
        {
            _story = story;
            _blockMap = story.Blocks.ToDictionary(b => b.Id);
            _propMap = story.Properties.ToDictionary(p => p.Key);
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
        /// Applies the effects of a decision, computes the result text (fixed
        /// or auto-generated), and navigates to its target block.
        /// State.LastResultText is set so the UI can show it at the top of
        /// the next block's text. Returns the block now active (may differ
        /// from decision.TargetBlock after a property-boundary redirect).
        /// </summary>
        public StoryBlock ChooseDecision(DecisionDefinition decision)
        {
            // Snapshot values BEFORE effects, so we can describe deltas accurately
            var before = new Dictionary<string, int>(State.Properties);

            ApplyEffects(decision.Effects);

            State.LastResultText = BuildResultText(decision, before, State.Properties);

            State.CurrentBlock = decision.TargetBlock;
            State.Day++;

            string redirect = GetPropertyRedirect();
            if (redirect != null)
                State.CurrentBlock = redirect;

            var block = GetCurrentBlock();
            if (block.IsFinal)
                State.IsGameOver = true;

            return block;
        }

        // ------------------------------------------------------------------ //
        //  Result text (fixed or auto-generated from effects)
        // ------------------------------------------------------------------ //

        private string BuildResultText(DecisionDefinition decision,
            Dictionary<string, int> before, Dictionary<string, int> after)
        {
            if (!string.IsNullOrWhiteSpace(decision.ResultText))
                return decision.ResultText;

            if (decision.Effects == null || decision.Effects.Count == 0)
                return null; // nimic de raportat

            var parts = new List<string>();
            foreach (var effect in decision.Effects)
            {
                if (!_propMap.TryGetValue(effect.Property, out var propDef))
                    continue;

                int beforeVal = before.TryGetValue(effect.Property, out var b) ? b : 0;
                int afterVal = after.TryGetValue(effect.Property, out var a) ? a : 0;
                int delta = afterVal - beforeVal;

                if (delta == 0) continue; // clamped la aceeași valoare — nu raportăm

                string label = propDef.DisplayName ?? effect.Property;
                string sign = delta > 0 ? "+" : "";
                parts.Add($"{label} {sign}{delta}");
            }

            if (parts.Count == 0) return null;

            return "Rezultat: " + string.Join(", ", parts) + ".";
        }

        // ------------------------------------------------------------------ //
        //  Random event selection (Milestone 2 feature)
        // ------------------------------------------------------------------ //

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
                case "<": return current < c.Value;
                case "<=": return current <= c.Value;
                case ">": return current > c.Value;
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

            State.Properties[effect.Property] = Math.Max(propDef.Min, Math.Min(propDef.Max, raw));
        }

        // ------------------------------------------------------------------ //
        //  Automatic redirects triggered by property boundaries
        // ------------------------------------------------------------------ //

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
