using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionV5Skills
    {
        public static V5SkillDefinition[] Plan(CompetitionDefinition d, ParticipantState p, V5FaceResult[] results)
            => PlanSteps(d, p, results).Select(step => step.skill).ToArray();

        public static V5PlannedSkill[] PlanSteps(CompetitionDefinition d, ParticipantState p, V5FaceResult[] results)
        {
            if (!CompetitionV5Content.IsEnabled(d) || p == null) throw new ArgumentException("Expected v5 planner state");
            CompetitionV5Content.ValidateResults(d, p, results);
            var dice = results.Where(r => r.faceId != CompetitionV5Content.BlankFaceId).ToArray();
            var faces = dice.Select(r => r.faceId).ToArray();
            if (faces.Length == 0) return Array.Empty<V5PlannedSkill>();
            var skills = d.v5.skills.Where(s => s.ownerAdventurerId == null || s.ownerAdventurerId == p.adventurerId)
                .OrderByDescending(s => s.priority).ThenBy(s => s.id, StringComparer.Ordinal).ToArray();
            if (faces.Length == 3)
            {
                string signature = CompetitionV5Content.Signature(faces);
                var triple = skills.FirstOrDefault(s => s.faceIds.Length == 3 && CompetitionV5Content.Signature(s.faceIds) == signature);
                if (triple != null) return new[] { Step(triple, dice) };
            }
            foreach (var pair in skills.Where(s => s.faceIds.Length == 2))
            {
                var used = new bool[faces.Length];
                bool matches = true;
                foreach (string required in pair.faceIds)
                {
                    int index = -1;
                    for (int i = 0; i < faces.Length; ++i) if (!used[i] && faces[i] == required) { index = i; break; }
                    if (index < 0) { matches = false; break; }
                    used[index] = true;
                }
                if (matches) return new[] { Step(pair, dice.Where((die, index) => used[index]).ToArray()) }
                    .Concat(dice.Where((die, index) => !used[index]).Select(die => Step(
                        skills.Single(s => s.faceIds.Length == 1 && s.faceIds[0] == die.faceId), die))).ToArray();
            }
            return dice.Select(die => Step(skills.Single(s => s.faceIds.Length == 1 && s.faceIds[0] == die.faceId), die)).ToArray();
        }

        static V5PlannedSkill Step(V5SkillDefinition skill, params V5FaceResult[] dice) => new V5PlannedSkill
        { skill = skill, dieSlotIndices = dice.Select(die => die.slotIndex).OrderBy(index => index).ToArray() };

        public static V5CombatPreview Preview(CompetitionDefinition d, ParticipantState p)
        {
            if (!CompetitionV5Content.IsEnabled(d) || p?.v5 == null) throw new ArgumentException("Missing v5 preview state");
            if (p.v5.selection == null || p.v5.selection.phase == V5SelectionPhase.AwaitRoll)
                return new V5CombatPreview { status = "await-roll" };
            if (p.v5.selection.phase != V5SelectionPhase.Selection) throw new ArgumentException("Invalid v5 preview phase");
            var steps = PlanSteps(d, p, p.v5.selection.results);
            var plan = steps.Select(step => step.skill).ToArray();
            // The same deterministic resolver evaluates a private copy. No RNG, rewards or command dispatch occurs.
            var copy = LoopJson.Copy(p);
            var log = CompetitionElements.ResolvePlanned(d, copy, plan);
            return new V5CombatPreview { noOp = plan.Length == 0, status = plan.Length == 0 ? "no-op" : "ready",
                skills = plan.Select((skill, i) => new V5PreviewSkill { skillId = skill.id, name = skill.name,
                    dieSlotIndices = steps[i].dieSlotIndices,
                    damageType = copy.v4.lastTurn.skills[i].damageType, damageApplied = copy.v4.lastTurn.skills[i].damageApplied,
                    shieldAbsorbed = log.skills[i].enemyShieldBefore - log.skills[i].enemyShieldAfter,
                    block = log.skills[i].block, status = log.skills[i].status }).ToArray() };
        }

        internal static void Execute(CompetitionDefinition d, ParticipantState p)
        {
            var results = p.v5.selection.results;
            var plan = Plan(d, p, results);
            CompetitionElements.ResolvePlanned(d, p, plan);
            p.v5.lastTurn = new V5CombatLog { results = LoopJson.Copy(results), plannedSkillIds = plan.Select(s => s.id).ToArray(), noOp = plan.Length == 0 };
        }

        internal static int MatchupScore(CompetitionDefinition d, ParticipantState p, string encounterId)
        {
            var enemy = d.v4.enemies.Single(e => e.encounterId == encounterId);
            int total = 0, count = 0;
            foreach (var face in p.dice.SelectMany(die => CompetitionV5Content.Faces(d, p.adventurerId, die.definitionId)))
            {
                if (face.kind == V5FaceKind.Blank) continue;
                var single = d.v5.skills.Single(s => s.faceIds.Length == 1 && s.faceIds[0] == face.id);
                if (single.damage == 0) continue;
                total += CompetitionElements.SkillPreview(d, p, single, enemy).affinityBasisPoints - 10000; ++count;
            }
            return count == 0 ? 0 : total / (count * 1000);
        }
    }
}
