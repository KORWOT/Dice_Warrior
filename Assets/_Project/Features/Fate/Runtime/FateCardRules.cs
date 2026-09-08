using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    /// <summary>Generates resolved, stable-ID offers. Presentation owns what is revealed.</summary>
    public static class FateCardRules
    {
        public static Grade DrawGrade(GameConfigData config, int power, bool combat, Grade cap, ref uint state)
        {
            if (config == null || config.dice == null || config.fate == null)
                throw new ArgumentException("Missing fate configuration.", nameof(config));
            if (!combat && ((int)cap < 0 || (int)cap > 4)) throw new ArgumentOutOfRangeException(nameof(cap));
            var table = combat ? config.fate.combat : config.fate.exploration;
            if (table == null || table.Length == 0) throw new InvalidOperationException("The selected grade table is empty.");
            int boundedPower = Math.Max(config.dice.minimumFatePower, Math.Min(config.dice.maximumFatePower, power));
            GradeRow row = null;
            foreach (var candidate in table)
                if (candidate != null && candidate.minimumPower <= boundedPower)
                    row = candidate;
            if (row == null || row.weights == null || row.weights.Length != 5)
                throw new InvalidOperationException("No valid five-grade row covers fate power " + boundedPower + ".");

            // Double precision avoids overflow when several finite float weights aggregate into the cap.
            // Normalization only rescales all weights equally; overflow mass stays in the cap.
            var mass = new double[5];
            for (int grade = 0; grade < mass.Length; grade++)
            {
                float weight = row.weights[grade];
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0)
                    throw new ArgumentException("Grade weights must be finite and nonnegative.", nameof(config));
                int destination = combat ? grade : Math.Min(grade, (int)cap);
                mass[destination] += weight;
            }
            double largest = mass.Max();
            if (largest <= 0) throw new ArgumentException("Grade weight sum must be positive.", nameof(config));
            var weights = mass.Select(weight => (float)(weight / largest)).ToArray();
            return (Grade)DiceRules.WeightedIndex(weights, ref state);
        }

        public static List<OfferedCard> GenerateExploration(RunState state)
        {
            CheckState(state);
            if (state.selectedNode == null || (int)state.selectedNode.type < 0 || (int)state.selectedNode.type >= 5)
                throw new InvalidOperationException("Exploration cards require a selected normal node.");
            if (state.config.world.events == null) throw new InvalidOperationException("Exploration content is missing.");
            var sourceWeights = state.config.fate.nodeWeights;
            if (sourceWeights == null || sourceWeights.Length != 5)
                throw new InvalidOperationException("Five normal node type weights are required.");
            float bias = state.config.fate.selectedTypeBias;
            if (float.IsNaN(bias) || float.IsInfinity(bias) || bias < 0)
                throw new ArgumentException("Selected node bias must be finite and nonnegative.", nameof(state));
            // Finite relative weights may overflow float when multiplied by a finite bias.
            // Multiply in double, then rescale the entire pool equally before float sampling.
            var mass = new double[5];
            for (int type = 0; type < mass.Length; type++)
            {
                float weight = sourceWeights[type];
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0)
                    throw new ArgumentException("Node weights must be finite and nonnegative.", nameof(state));
                mass[type] = (double)weight * (type == (int)state.selectedNode.type ? bias : 1);
            }
            double largest = mass.Max();
            if (largest <= 0) throw new ArgumentException("Biased node weight sum must be positive.", nameof(state));
            var weights = mass.Select(weight => (float)(weight / largest)).ToArray();
            uint rng = state.rngState;
            var result = new List<OfferedCard>(state.config.world.offeredCards);
            for (int slot = 0; slot < state.config.world.offeredCards; slot++)
            {
                // Only this type slot is guaranteed; each slot has its own grade and content samples.
                var type = slot == 0 ? state.selectedNode.type : (NodeType)DiceRules.WeightedIndex(weights, ref rng);
                Grade grade = DrawGrade(state.config, state.fatePower, false, state.explorationCap, ref rng);
                var pool = state.config.world.events.Where(x => x != null && x.type == type && x.grade == grade).ToArray();
                if (pool.Length == 0) throw new InvalidOperationException("Missing exploration pool: " + type + "/" + grade + ".");
                var content = pool[UniformIndex(pool.Length, ref rng)];
                result.Add(Offer(state, slot, type, grade, content.id));
            }
            state.rngState = rng;
            return result;
        }

        public static List<OfferedCard> GenerateActions(RunState state)
        {
            CheckState(state);
            if (state.actionIds == null || state.actionIds.Count == 0)
                throw new InvalidOperationException("The run must own at least one action.");
            // Ownership is a set of stable IDs, so choosing an already-owned trial card cannot add draw bias.
            var owned = state.actionIds.Distinct().Select(state.config.Action).ToArray();
            uint rng = state.rngState;
            var result = new List<OfferedCard>(state.config.world.offeredCards);
            for (int slot = 0; slot < state.config.world.offeredCards; slot++)
            {
                Grade grade = DrawGrade(state.config, state.fatePower, true, state.explorationCap, ref rng);
                var exact = owned.Where(x => x.grade == grade).ToArray();
                var pool = exact.Length > 0 ? exact : owned;
                var action = pool[UniformIndex(pool.Length, ref rng)];
                // Offered grade is separate from original grade; CombatRules applies only a numeric ratio.
                result.Add(Offer(state, slot, NodeType.Combat, grade, action.id));
            }
            state.rngState = rng;
            return result;
        }

        static int UniformIndex(int count, ref uint state)
        {
            var weights = new float[count];
            for (int i = 0; i < count; i++) weights[i] = 1;
            return DiceRules.WeightedIndex(weights, ref state);
        }

        static OfferedCard Offer(RunState state, int slot, NodeType type, Grade grade, string contentId) =>
            new OfferedCard { id = state.runId + ":card:" + state.sequence + ":" + slot, type = type, grade = grade, contentId = contentId };

        static void CheckState(RunState state)
        {
            if (state == null || state.config == null || state.config.world == null || state.config.fate == null)
                throw new ArgumentException("Card generation requires a configured run.", nameof(state));
            if (state.config.world.offeredCards < 1 || state.config.world.offeredCards > 5)
                throw new ArgumentException("The configured card count must be 1..5.", nameof(state));
        }
    }
}
