using System;
using System.Linq;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionPartialComboTests
    {
        static CompetitionState Fight(Symbol[] faces, string version = null)
        {
            var definition = CompetitionRulesTests.Definition();
            if (version != null) definition.rulesVersion = version;
            for (int i = 0; i < faces.Length; ++i)
                definition.combat.dice[i].faces = Enumerable.Repeat(faces[i], 6).ToArray();
            var state = CompetitionRules.Start(definition, new Profile(), 123, "partial-combo-match");
            var p = state.participants[0];
            p.completedLaps = 2;
            p.claimed.Add(state.matchId + ":human:lap:1"); p.claimed.Add(state.matchId + ":human:lap:2");
            p.dice = definition.combat.dice.Take(3).Select((d, i) => new DieSlot { instanceId = "partial-die-" + i, definitionId = d.id }).ToList();
            p.position = 2; p.offer = CompetitionEncounterRules.CreateOffer(state, p); p.phase = CompetitionPhase.Encounter;
            CompetitionCommandRules.Apply(state, "choose", "forest-corrupt");
            p.enemy.hp = p.enemy.maxHp = 500;
            return state;
        }

        [Test] public void NewMatchStaffStaffSwordUsesRemainingBasicThenArcSlash()
        {
            var state = Fight(new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword });
            CompetitionCommandRules.Apply(state, "combat", "");
            Assert.That(state.participants[0].lastTurn.skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "arc-slash" }));
        }

        [Test] public void LegacySavedMatchKeepsThreeBasicsForTheSameFaces()
        {
            var state = Fight(new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword }, "shared-world-preview-1");
            var saved = LoopJson.Copy(state);
            CompetitionCommandRules.Apply(saved, "combat", "");
            Assert.That(saved.participants[0].lastTurn.skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "bolt", "slash" }));
        }

        [Test] public void NewContentVersionIsTwoAndUnknownVersionIsRejected()
        {
            var definition = CompetitionRulesTests.Definition();
            Assert.That(definition.rulesVersion, Is.EqualTo("shared-world-preview-2"));
            definition.rulesVersion = "shared-world-preview-unknown";
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
        }

        [TestCase(Symbol.Staff, Symbol.Staff, Symbol.Fire, "bolt", "fire-bolt")]
        [TestCase(Symbol.Sword, Symbol.Sword, Symbol.Shield, "slash", "guard-slash")]
        [TestCase(Symbol.Staff, Symbol.Fire, Symbol.Shield, "guard", "fire-bolt")]
        [TestCase(Symbol.Sword, Symbol.Staff, Symbol.Shield, "guard", "arc-slash")]
        public void ThreeFacesConsumeOnePairAndLeaveExactlyOneBasic(Symbol first, Symbol second, Symbol third, string basic, string combo)
        {
            Assert.That(SymbolCombat.Plan(new Rules(), new[] { first, second, third }, true).Select(s => s.id), Is.EqualTo(new[] { basic, combo }));
        }

        [Test] public void ExactThreeFaceComboWinsAndNoPairKeepsOriginalBasicOrder()
        {
            var rules = new Rules();
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Fire, Symbol.Staff, Symbol.Sword }, true).Select(s => s.id), Is.EqualTo(new[] { "flame-arc" }));
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Shield, Symbol.Sword, Symbol.Shield }, true).Select(s => s.id), Is.EqualTo(new[] { "citadel" }));
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Shield, Symbol.Fire, Symbol.Fire }, true).Select(s => s.id), Is.EqualTo(new[] { "guard", "flame", "flame" }));
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Staff, Symbol.Staff, Symbol.Staff }, true).Select(s => s.id), Is.EqualTo(new[] { "bolt", "bolt", "bolt" }));
        }

        [Test] public void OverlappingPairsFollowContentOrderAndDuplicateSymbolsNeedDistinctFaces()
        {
            var rules = new Rules();
            var pair = rules.combos.Single(s => s.id == "guard-slash");
            rules.combos = new[] { pair }.Concat(rules.combos.Where(s => s != pair)).ToArray();
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Sword, Symbol.Staff, Symbol.Shield }, true).Select(s => s.id), Is.EqualTo(new[] { "bolt", "guard-slash" }));
            rules.combos = new[] { new Skill { id = "double-staff", tag = "arcane", symbols = new[] { Symbol.Staff, Symbol.Staff }, damage = 8 } };
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Staff, Symbol.Sword, Symbol.Fire }, true).Select(s => s.id), Is.EqualTo(new[] { "bolt", "slash", "flame" }));
            Assert.That(SymbolCombat.Plan(rules, new[] { Symbol.Staff, Symbol.Sword, Symbol.Staff }, true).Select(s => s.id), Is.EqualTo(new[] { "slash", "double-staff" }));
        }

        static Run EnemyRun(int enemyHp = 100) => new Run { rules = new Rules(), hp = 60, power = 5, guard = 4,
            equipment = new[] { "blade", "mail", "charm" }, enemy = new Enemy { hp = enemyHp, maxHp = enemyHp, shield = 3, attack = 8, intent = Intent.Heavy } };

        [Test] public void MixedSkillsPreserveGearShieldAndSingleRetaliation()
        {
            var run = EnemyRun();
            var log = SymbolCombat.Resolve(run, new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword }, true);
            Assert.That(log.skills.Select(s => s.damage), Is.EqualTo(new[] { 6, 18 }));
            Assert.That(run.enemy.hp, Is.EqualTo(76)); Assert.That(run.enemy.shield, Is.Zero);
            Assert.That(log.retaliation, Is.True); Assert.That(log.retaliationDamage, Is.EqualTo(12)); Assert.That(run.hp, Is.EqualTo(48));

            run = EnemyRun(); run.equipment[0] = "wand";
            log = SymbolCombat.Resolve(run, new[] { Symbol.Shield, Symbol.Staff, Symbol.Fire }, true);
            Assert.That(log.skills.Select(s => s.skillId), Is.EqualTo(new[] { "guard", "fire-bolt" }));
            Assert.That(log.playerBlock, Is.EqualTo(10)); Assert.That(log.skills[1].damage, Is.EqualTo(18));
            Assert.That(log.retaliationDamage, Is.EqualTo(2)); Assert.That(run.hp, Is.EqualTo(58));
        }

        [Test] public void KillingBasicKeepsComboLogAsSkippedWithoutRetaliation()
        {
            var run = EnemyRun(1); run.enemy.shield = 0;
            var log = SymbolCombat.Resolve(run, new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword }, true);
            Assert.That(log.skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "arc-slash" }));
            Assert.That(log.skills.Select(s => s.status), Is.EqualTo(new[] { "applied", "enemy-defeated" }));
            Assert.That(log.skills[1].damage, Is.Zero); Assert.That(log.retaliation, Is.False); Assert.That(run.hp, Is.EqualTo(60));
        }

        [Test] public void LegacyTwoParameterApisStillUseExactWholeCombinationOnly()
        {
            var faces = new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword };
            Assert.That(SymbolCombat.Plan(new Rules(), faces).Select(s => s.id), Is.EqualTo(new[] { "bolt", "bolt", "slash" }));
            Assert.That(SymbolCombat.Resolve(EnemyRun(), faces).skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "bolt", "slash" }));
            Assert.That(typeof(SymbolCombat).GetMethod("Plan", new[] { typeof(Rules), typeof(Symbol[]) }), Is.Not.Null);
            Assert.That(typeof(SymbolCombat).GetMethod("Resolve", new[] { typeof(Run), typeof(Symbol[]) }), Is.Not.Null);
        }

        [Test] public void VersionTwoHumanCommandsReplayAllAiAfterSerializationResume()
        {
            var definition = CompetitionRulesTests.Definition();
            Assert.That(definition.rulesVersion, Is.EqualTo("shared-world-preview-2"));
            var current = CompetitionRules.Start(definition, new Profile { growth = 2 }, 456, "v2-replay");
            var replay = LoopJson.Copy(current);
            for (int i = 0; i < 2048 && !current.finished; ++i)
            {
                var command = CompetitionRulesTests.NextCommand(current);
                Assert.That(CompetitionCommandRules.Target(replay), Is.EqualTo(command.targetId));
                CompetitionCommandRules.Apply(current, command.kind, command.arg);
                CompetitionCommandRules.Apply(replay, command.kind, command.arg);
                if (i == 12) replay = LoopJson.Copy(replay);
                Assert.That(LoopJson.Encode(replay), Is.EqualTo(LoopJson.Encode(current)), "command " + command.sequence);
            }
            Assert.That(current.finished, Is.True); Assert.That(current.definition.rulesVersion, Is.EqualTo("shared-world-preview-2"));
        }
    }
}
