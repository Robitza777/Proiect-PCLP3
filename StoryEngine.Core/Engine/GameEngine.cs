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
            EnsureRuntimeCollections();
        }

        public void LoadGame(GameState savedState)
        {
            State = savedState;
            EnsureRuntimeCollections();
        }

        private void EnsureRuntimeCollections()
        {
            if (State.Journal == null)
                State.Journal = new List<string>();

            if (State.RecentEventHistory == null)
                State.RecentEventHistory = new Dictionary<string, List<string>>();

            if (State.VisitedMapLocations == null)
                State.VisitedMapLocations = new List<string>();

            if (State.RecentDecisionHistory == null)
                State.RecentDecisionHistory = new Dictionary<string, List<string>>();

            if (State.CurrentChoiceKeys == null)
                State.CurrentChoiceKeys = new List<string>();
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

        public List<DecisionDefinition> GetVisibleDecisionsForCurrentBlock()
        {
            EnsureRuntimeCollections();

            var block = GetCurrentBlock();

            if (block.Decisions == null || block.Decisions.Count == 0)
                return new List<DecisionDefinition>();

            if (block.RandomDecisionCount <= 0 || string.IsNullOrWhiteSpace(block.DecisionPoolCategory))
                return block.Decisions;

            var available = block.Decisions
                .Where(d => d.Condition == null || EvaluateCondition(d.Condition))
                .ToList();

            int count = Math.Min(block.RandomDecisionCount, available.Count);

            if (count <= 0)
                return new List<DecisionDefinition>();

            string cacheKey = block.Id + "|" + block.DecisionPoolCategory;

            if (State.CurrentChoiceBlock == cacheKey
                && State.CurrentChoiceDay == State.Day
                && State.CurrentChoiceKeys.Count > 0)
            {
                var cached = available
                    .Where(d => State.CurrentChoiceKeys.Contains(GetDecisionKey(d)))
                    .ToList();

                if (cached.Count > 0)
                    return cached;
            }

            if (!State.RecentDecisionHistory.TryGetValue(block.DecisionPoolCategory, out var history))
            {
                history = new List<string>();
                State.RecentDecisionHistory[block.DecisionPoolCategory] = history;
            }

            int avoidCount = Math.Min(available.Count - count, 6);

            var recent = history
                .Skip(Math.Max(0, history.Count - avoidCount))
                .ToHashSet();

            var filtered = available
                .Where(d => !recent.Contains(GetDecisionKey(d)))
                .ToList();

            if (filtered.Count < count)
                filtered = available;

            var chosen = filtered
                .OrderBy(d => _random.Next())
                .Take(count)
                .ToList();

            State.CurrentChoiceBlock = cacheKey;
            State.CurrentChoiceDay = State.Day;
            State.CurrentChoiceKeys = chosen.Select(GetDecisionKey).ToList();

            foreach (var decision in chosen)
                history.Add(GetDecisionKey(decision));

            if (history.Count > 40)
                history.RemoveRange(0, history.Count - 40);

            return chosen;
        }

        private string GetDecisionKey(DecisionDefinition decision)
        {
            return (decision.Text ?? "") + "->" + (decision.TargetBlock ?? "");
        }

        // ------------------------------------------------------------------ //
        //  Player action: choose a decision
        // ------------------------------------------------------------------ //

        public StoryBlock ChooseDecision(DecisionDefinition decision)
        {
            EnsureRuntimeCollections();

            string fromBlock = State.CurrentBlock;
            int currentDay = State.Day;

            var before = new Dictionary<string, int>(State.Properties);

            ApplyEffects(decision.Effects);

            State.LastResultText = string.IsNullOrWhiteSpace(decision.ResultText) ? null : decision.ResultText;
            State.LastEffectsSummary = BuildEffectsSummary(decision, before, State.Properties);

            State.CurrentBlock = ResolveTargetBlock(decision.TargetBlock);
            State.Day++;

            string redirect = GetPropertyRedirect();
            if (redirect != null)
                State.CurrentBlock = redirect;

            if (State.CurrentBlock == _story.StartBlock || State.CurrentBlock.StartsWith("hub."))
                State.ActiveExpedition = false;

            var block = GetCurrentBlock();

            if (block.IsFinal)
                State.IsGameOver = true;

            State.Journal.Add($"Ziua {currentDay}: {fromBlock} -> {decision.Text} -> {State.CurrentBlock}");

            return block;
        }

        // ------------------------------------------------------------------ //
        //  Map navigation
        // ------------------------------------------------------------------ //

        public StoryBlock GoToBlock(string blockId)
        {
            EnsureRuntimeCollections();

            if (!_blockMap.ContainsKey(blockId))
                throw new InvalidOperationException($"Block '{blockId}' not found in story.");

            string fromBlock = State.CurrentBlock;
            int currentDay = State.Day;

            State.CurrentBlock = blockId;
            State.Day++;
            State.LastResultText = null;
            State.LastEffectsSummary = null;

            var block = GetCurrentBlock();

            if (block.IsFinal)
                State.IsGameOver = true;

            State.Journal.Add($"Ziua {currentDay}: {fromBlock} -> Expediție pe hartă -> {State.CurrentBlock}");

            return block;
        }

        public StoryBlock GoToMapLocation(MapLocationDefinition location)
        {
            EnsureRuntimeCollections();

            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (State.ActiveExpedition)
                throw new InvalidOperationException("Ești deja într-o expediție. Întoarce-te mai întâi la adăpost.");

            if (State.LastExpeditionDay == State.Day)
                throw new InvalidOperationException("Ai făcut deja o expediție astăzi.");

            if (location.Condition != null && !EvaluateCondition(location.Condition))
                throw new InvalidOperationException("Locația nu este disponibilă momentan.");

            if (location.OneTimeOnly && State.VisitedMapLocations.Contains(location.Id))
                throw new InvalidOperationException("Această locație a fost deja explorată.");

            string fromBlock = State.CurrentBlock;
            int currentDay = State.Day;

            ApplyEffects(location.TravelEffects);

            string targetBlock = !string.IsNullOrWhiteSpace(location.RandomEventCategory)
                ? ResolveTargetBlock("random:" + location.RandomEventCategory)
                : location.TargetBlock;

            State.CurrentBlock = targetBlock;
            State.Day++;
            State.LastExpeditionDay = currentDay;
            State.ActiveExpedition = true;
            State.LastResultText = null;
            State.LastEffectsSummary = null;

            if (!State.VisitedMapLocations.Contains(location.Id))
                State.VisitedMapLocations.Add(location.Id);

            var block = GetCurrentBlock();

            if (block.IsFinal)
                State.IsGameOver = true;

            State.Journal.Add($"Ziua {currentDay}: {fromBlock} -> Expediție: {location.Name} -> {State.CurrentBlock}");

            return block;
        }

        // ------------------------------------------------------------------ //
        //  Random events
        // ------------------------------------------------------------------ //

        public StoryBlock PickRandomEvent(string category)
        {
            EnsureRuntimeCollections();

            var candidates = _story.Blocks
                .Where(b => b.EventCategory == category)
                .Where(b => b.Decisions == null
                    || b.Decisions.Count == 0
                    || b.Decisions.Any(d => d.Condition == null || EvaluateCondition(d.Condition)))
                .ToList();

            if (candidates.Count == 0)
                return null;

            if (!State.RecentEventHistory.TryGetValue(category, out var history))
            {
                history = new List<string>();
                State.RecentEventHistory[category] = history;
            }

            int avoidCount = Math.Min(candidates.Count - 1, 2);

            var recent = history
                .Skip(Math.Max(0, history.Count - avoidCount))
                .ToHashSet();

            var filtered = candidates
                .Where(b => !recent.Contains(b.Id))
                .ToList();

            if (filtered.Count == 0)
                filtered = candidates;

            var chosen = filtered[_random.Next(filtered.Count)];

            history.Add(chosen.Id);

            if (history.Count > 12)
                history.RemoveAt(0);

            return chosen;
        }

        private string ResolveTargetBlock(string targetBlock)
        {
            if (string.IsNullOrWhiteSpace(targetBlock))
                throw new InvalidOperationException("TargetBlock is empty.");

            if (targetBlock.StartsWith("random:"))
            {
                string category = targetBlock.Substring("random:".Length);
                var randomBlock = PickRandomEvent(category);

                if (randomBlock == null)
                    throw new InvalidOperationException($"No random events available for category '{category}'.");

                return randomBlock.Id;
            }

            return targetBlock;
        }

        // ------------------------------------------------------------------ //
        //  Auto-generated effects summary
        // ------------------------------------------------------------------ //

        private string BuildEffectsSummary(DecisionDefinition decision,
            Dictionary<string, int> before, Dictionary<string, int> after)
        {
            if (decision.Effects == null || decision.Effects.Count == 0)
                return null;

            var parts = new List<string>();

            foreach (var effect in decision.Effects)
            {
                if (!_propMap.TryGetValue(effect.Property, out var propDef))
                    continue;

                int beforeVal = before.TryGetValue(effect.Property, out var b) ? b : 0;
                int afterVal = after.TryGetValue(effect.Property, out var a) ? a : 0;
                int delta = afterVal - beforeVal;

                if (delta == 0)
                    continue;

                bool isItem = propDef.Key.StartsWith("item.");
                string label = propDef.DisplayName ?? effect.Property;

                if (isItem && delta > 0)
                {
                    parts.Add($"+ {label}");
                }
                else if (isItem && delta < 0)
                {
                    parts.Add($"- {label}");
                }
                else
                {
                    string sign = delta > 0 ? "+" : "";
                    parts.Add($"{label} {sign}{delta}");
                }
            }

            if (parts.Count == 0)
                return null;

            return string.Join(",  ", parts);
        }

        // ------------------------------------------------------------------ //
        //  Condition evaluation
        // ------------------------------------------------------------------ //

        public bool EvaluateCondition(ConditionDefinition condition)
        {
            if (condition == null)
                return true;

            switch (condition.Type)
            {
                case "AND":
                    return condition.Operands != null && condition.Operands.All(EvaluateCondition);

                case "OR":
                    return condition.Operands != null && condition.Operands.Any(EvaluateCondition);

                case "COMPARISON":
                    return EvaluateComparison(condition);

                default:
                    throw new ArgumentException($"Unknown condition type: '{condition.Type}'");
            }
        }

        private bool EvaluateComparison(ConditionDefinition c)
        {
            int current;

            if (c.Property == "day")
            {
                current = State.Day;
            }
            else if (!State.Properties.TryGetValue(c.Property, out current))
            {
                throw new ArgumentException($"Unknown property '{c.Property}' in condition.");
            }

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
            if (effects == null)
                return;

            foreach (var effect in effects)
                ApplyEffect(effect);
        }

        private void ApplyEffect(EffectDefinition effect)
        {
            if (effect == null)
                return;

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