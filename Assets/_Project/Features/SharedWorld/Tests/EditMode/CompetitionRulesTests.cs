using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionRulesTests
    {
        public static CompetitionDefinition Definition()
        {
            string source = Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-1.json");
            if (!File.Exists(source)) source = Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Content", "shared-world-preview-1.json");
            return LoopJson.Decode<CompetitionDefinition>(File.ReadAllText(source));
        }

        [Test] public void MatchStartsWithFixedWorldAndFourIndependentEqualGrowthAdventurers()
        {
            var definition = Definition();
            var match = CompetitionRules.Start(definition, new Profile { growth = 4 }, 123, "contract-match");
            Assert.That(match, Is.Not.Null, "A shared competition must exist independently of legacy Run.");
            Assert.That(match.participants.Length, Is.EqualTo(4));
            Assert.That(match.definition.world.places.Length, Is.EqualTo(12));
            Assert.That(match.participants.All(p => p.hp == 80 && p.maxHp == 80 && p.dice.Count == 1), Is.True);
            Assert.That(match.participants.Select(p => p.travelRng).Distinct().Count(), Is.EqualTo(4));
            Assert.That(match.participants.Select(p => p.combatRng).Distinct().Count(), Is.EqualTo(4));
            match.participants[0].equipment[0] = "wand";
            Assert.That(match.participants.Skip(1).All(p => p.equipment[0] == "blade"), Is.True);
            Assert.That(definition.world.places[0].name, Is.EqualTo("운명의 광장"));
            CompetitionRules.Validate(match);
        }

        static CompetitionState New(uint seed = 123) => CompetitionRules.Start(Definition(), new Profile(), seed, "contract-match");
        public static void Step(CompetitionState match)
        {
            var command = NextCommand(match);
            CompetitionCommandRules.Apply(match, command.kind, command.arg);
        }
        public static CompetitionCommandEntry NextCommand(CompetitionState match)
        {
            var p = CompetitionRules.Human(match);
            string kind = "advance", arg = "";
            switch (p.phase)
            {
                case CompetitionPhase.Map: kind = "travel"; break;
                case CompetitionPhase.Combat: kind = "combat"; break;
                case CompetitionPhase.LapReward: kind = "lap"; arg = p.completedLaps < 2 ? match.startingProfile.startingDie : "aegis"; break;
                case CompetitionPhase.Encounter:
                    var choices = CompetitionCommandRules.LegalChoices(match, p);
                    var choice = choices.OrderByDescending(c => c.kind == EncounterKind.Heal && p.hp * 2 < p.maxHp ? 100 : c.xp + c.heal + c.honor).First();
                    kind = "choose"; arg = choice.id; break;
            }
            return new CompetitionCommandEntry { sequence = match.actionNumber + 1, clientCommandId = "human-command-" + (match.actionNumber + 1),
                targetId = CompetitionCommandRules.Target(match), kind = kind, arg = arg };
        }
        static void DisableAi(CompetitionState match)
        {
            foreach (var p in match.participants.Skip(1)) { p.hp = 0; p.eliminated = true; p.phase = CompetitionPhase.Complete; }
        }

        [Test] public void HumanEncounterCompletionMovesEveryActiveAiExactlyOnceOnTheSameWorld()
        {
            var match = New();
            string world = LoopJson.Encode(match.definition.world);
            var positions = match.participants.Skip(1).Select(p => p.position).ToArray();
            CompetitionCommandRules.Apply(match, "travel", "");
            Assert.That(match.worldRound, Is.EqualTo(1));
            Assert.That(match.participants.Skip(1).Select(p => p.position), Is.EqualTo(positions));
            for (int commands = 0; commands < 70 && match.worldRound == 1 && !match.finished; ++commands) Step(match);
            Assert.That(match.worldRound, Is.EqualTo(2));
            Assert.That(match.lastRound.Count, Is.EqualTo(3));
            Assert.That(match.lastRound.All(s => s.travel >= 1 && s.travel <= 6), Is.True);
            Assert.That(LoopJson.Encode(match.definition.world), Is.EqualTo(world));
            CompetitionRules.Validate(match);
        }

        [Test] public void SavedOfferDoesNotRerollAndChoicesCannotBypassCosts()
        {
            var match = New(); DisableAi(match);
            var p = CompetitionRules.Human(match);
            // Seek a shop offer by selecting a seed, without injecting a die result into gameplay.
            for (uint seed = 1; seed < 200; ++seed)
            {
                match = New(seed); DisableAi(match); p = CompetitionRules.Human(match);
                CompetitionCommandRules.Apply(match, "travel", "");
                if (p.offer.choices.Any(c => c.goldCost > 0)) break;
            }
            string saved = LoopJson.Encode(match);
            var restored = LoopJson.Copy(match);
            Assert.That(LoopJson.Encode(restored), Is.EqualTo(saved));
            var forbidden = p.offer.choices.First(c => c.goldCost > 0);
            Assert.That(forbidden.available, Is.False);
            Assert.Throws<InvalidOperationException>(() => CompetitionCommandRules.Apply(restored, "choose", forbidden.id));
            Assert.That(LoopJson.Encode(restored), Is.EqualTo(saved));
            Assert.Throws<ArgumentException>(() => CompetitionCommandRules.Apply(restored, "choose", "forged-event"));
            Assert.That(LoopJson.Encode(p.offer), Is.EqualTo(LoopJson.Encode(restored.participants[0].offer)));
        }

        [Test] public void LapClaimsKeepGeographyAndGrowOnlyTheClaimingParticipant()
        {
            var match = New(); DisableAi(match); var p = match.participants[0];
            string world = LoopJson.Encode(match.definition.world);
            for (int lap = 1; lap <= 3; ++lap)
            {
                p.position = 11; p.phase = CompetitionPhase.Map;
                CompetitionCommandRules.Apply(match, "travel", "");
                Assert.That(p.position, Is.Zero); Assert.That(p.phase, Is.EqualTo(CompetitionPhase.LapReward));
                if (lap < 3) Assert.Throws<ArgumentException>(() => CompetitionCommandRules.Apply(match, "lap", "aegis"));
                CompetitionCommandRules.Apply(match, "lap", lap < 3 ? "arcane" : "aegis");
                Assert.That(p.completedLaps, Is.EqualTo(lap));
                Assert.That(p.dice.Count, Is.EqualTo(Math.Min(lap + 1, 3)));
                Assert.That(p.claimed.Count, Is.EqualTo(lap));
                Assert.Throws<InvalidOperationException>(() => CompetitionCommandRules.Apply(match, "lap", "aegis"));
            }
            Assert.That(p.phase, Is.EqualTo(CompetitionPhase.Combat)); Assert.That(p.enemy.boss, Is.True);
            Assert.That(match.participants.Skip(1).All(ai => ai.completedLaps == 0 && ai.dice.Count == 1), Is.True);
            Assert.That(LoopJson.Encode(match.definition.world), Is.EqualTo(world));
        }

        [Test] public void HumanLogReplaysAiAndEntireStateAcrossSaveResume()
        {
            var match = New(456); var replay = New(456);
            for (int count = 0; count < 1500 && !match.finished; ++count)
            {
                Assert.That(CompetitionCommandRules.Target(replay), Is.EqualTo(CompetitionCommandRules.Target(match)));
                Step(match); Step(replay);
                if (count == 12) replay = LoopJson.Copy(replay);
                Assert.That(LoopJson.Encode(replay), Is.EqualTo(LoopJson.Encode(match)), "command " + count);
            }
            Assert.That(match.finished, Is.True);
            Assert.That(match.worldRound, Is.LessThanOrEqualTo(30));
            Assert.That(match.participants.All(p => p.scores.Select(s => s.id).Distinct().Count() == p.scores.Count), Is.True);
        }

        [Test] public void AbandonedHumanCanSpectateAndRoundLimitEndsTheMatch()
        {
            var match = New();
            CompetitionCommandRules.Apply(match, "abandon", "");
            Assert.That(match.participants[0].eliminated, Is.True);
            while (!match.finished) CompetitionCommandRules.Apply(match, "advance", "");
            Assert.That(match.worldRound, Is.LessThanOrEqualTo(30));
            Assert.Throws<InvalidOperationException>(() => CompetitionCommandRules.Apply(match, "advance", ""));
            var limit = New(); limit.worldRound = 30;
            for (int i = 0; i < 70 && !limit.finished; ++i) Step(limit);
            Assert.That(limit.finished, Is.True); Assert.That(limit.endReason, Is.EqualTo("round-limit"));
        }

        [Test] public void CombatAdapterPreservesAll84ExactSymbolResults()
        {
            var rules = new Rules();
            for (int count = 1; count <= 3; ++count)
            for (int value = 0; value < 1 << (count * 2); ++value)
            {
                var faces = Enumerable.Range(0, count).Select(i => (Symbol)((value >> (2 * i)) & 3)).ToArray();
                var p = new ParticipantState { hp = 60, power = 5, guard = 4, equipment = new[] { "wand", "mail", "charm" },
                    enemy = new Enemy { hp = 22, maxHp = 22, shield = 3, attack = 8, intent = Intent.Heavy } };
                var run = new Run { rules = rules, hp = p.hp, power = p.power, guard = p.guard, equipment = p.equipment,
                    enemy = LoopJson.Copy(p.enemy), turn = p.turn };
                var expected = SymbolCombat.Resolve(run, faces);
                var actual = CompetitionCombat.Resolve(rules, p, faces);
                Assert.That(LoopJson.Encode(actual), Is.EqualTo(LoopJson.Encode(expected)));
                Assert.That(p.hp, Is.EqualTo(run.hp)); Assert.That(LoopJson.Encode(p.enemy), Is.EqualTo(LoopJson.Encode(run.enemy)));
            }
        }

        [Test] public void ContentRejectsMissingReferencesAndNoLegalFallback()
        {
            var definition = Definition(); definition.world.places[1].encounterIds = new[] { "missing" };
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
            definition = Definition(); definition.world.places[1].id = definition.world.places[2].id;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
        }

        [Test] public void SamePlaceUsesPersonalStageAndEventStreamsNeverChangeTravelOrCombat()
        {
            var match = New(); var p = match.participants[0]; p.position = 2;
            uint travel = p.travelRng, combat = p.combatRng, otherEvent = match.participants[1].eventRng;
            var first = CompetitionEncounterRules.CreateOffer(match, p);
            Assert.That(first.choices.Any(c => c.id == "forest-wolf"), Is.True);
            Assert.That(first.choices.Any(c => c.id == "forest-corrupt"), Is.False);
            var advanced = match.participants[1]; advanced.position = 2; advanced.completedLaps = 2;
            var later = CompetitionEncounterRules.CreateOffer(match, advanced);
            Assert.That(later.choices.Any(c => c.id == "forest-corrupt"), Is.True);
            Assert.That(later.choices.Any(c => c.id == "forest-wolf"), Is.False);
            Assert.That(later.choices.First(c => c.kind == EncounterKind.Combat).enemyHp,
                Is.GreaterThan(first.choices.First(c => c.kind == EncounterKind.Combat).enemyHp));
            Assert.That(p.travelRng, Is.EqualTo(travel)); Assert.That(p.combatRng, Is.EqualTo(combat));
            Assert.That(advanced.eventRng, Is.Not.EqualTo(otherEvent));
            Assert.That(p.completedLaps, Is.Zero); Assert.That(p.visits[2], Is.EqualTo(1));
        }

        [Test] public void CombatTurnCapEliminatesWithoutFakeVictoryOrHonor()
        {
            var match = New(); DisableAi(match); var p = match.participants[0];
            p.position = 2; p.offer = CompetitionEncounterRules.CreateOffer(match, p); p.phase = CompetitionPhase.Encounter;
            CompetitionCommandRules.Apply(match, "choose", "forest-wolf");
            p.enemy.hp = p.enemy.maxHp = 10000; p.enemy.attack = 1; p.encounterTurns = 63;
            CompetitionCommandRules.Apply(match, "combat", "");
            Assert.That(p.eliminated, Is.True); Assert.That(p.hp, Is.GreaterThan(0));
            Assert.That(p.trialSucceeded, Is.False); Assert.That(p.honor, Is.Zero); Assert.That(p.encounterTurns, Is.EqualTo(64));
            Assert.That(match.finished, Is.True);
        }

        [Test] public void IdenticalResultsShareRankWithoutParticipantIdBreakingTheTie()
        {
            var match = New();
            CompetitionCommandRules.Apply(match, "travel", "");
            Assert.That(match.participants.All(p => p.rank == 1), Is.True);
            match.participants[0].hp -= 1;
            CompetitionCommandRules.Apply(match, "choose", CompetitionCommandRules.LegalChoices(match, match.participants[0])[0].id);
            var order = CompetitionRules.Rankings(match);
            for (int i = 1; i < order.Length; ++i)
                if (order[i].trialSucceeded == order[i - 1].trialSucceeded && order[i].completedLaps == order[i - 1].completedLaps &&
                    order[i].honor == order[i - 1].honor && order[i].hp == order[i - 1].hp)
                    Assert.That(order[i].rank, Is.EqualTo(order[i - 1].rank));
        }
    }
}
