using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionGrowthTests
    {
        public static CompetitionDefinition Definition()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-3.json");
            if (!File.Exists(path)) path = Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Content", "shared-world-preview-3.json");
            return LoopJson.Decode<CompetitionDefinition>(File.ReadAllText(path));
        }

        [TestCase("wanderer", 6, 4, 4)]
        [TestCase("scholar", 4, 4, 6)]
        [TestCase("vanguard", 4, 6, 4)]
        public void SelectedAdventurerAndAiReceiveApprovedStatsAndSameAccountHp(string id, int strength, int guard, int arcane)
        {
            var profile = new Profile { growth = 2, startingDie = "arcane", startingWeapon = "wand" };
            var definition = Definition();
            Assert.That(CompetitionStats.IsEnabled(definition), Is.True);
            var preview = CompetitionStats.Preview(definition, profile, id);
            Assert.That(preview, Is.Not.Null, "v3 must expose the selected adventurer's real starting stats.");
            Assert.That(new[] { preview.hp, preview.maxHp, preview.baseHp, preview.accountHp, preview.levelHp, preview.levelBonus },
                Is.EqualTo(new[] { 70, 70, 60, 10, 0, 0 }));
            Assert.That(new[] { preview.strength, preview.guard, preview.arcane }, Is.EqualTo(new[] { strength, guard, arcane }));
            var match = CompetitionRules.Start(definition, profile, 123, "growth-start", id);
            Assert.That(match.participants.Select(p => p.adventurerId), Is.EqualTo(new[] { id, "wanderer", "scholar", "vanguard" }));
            Assert.That(match.participants.All(p => p.hp == 70 && p.maxHp == 70 && p.dice.Single().definitionId == "arcane" && p.equipment[0] == "wand"), Is.True);
            Assert.That(CompetitionStats.Describe(match.definition, match.participants[0]).arcane, Is.EqualTo(arcane));
            preview.strength = 999;
            Assert.That(CompetitionStats.Preview(definition, profile, id).strength, Is.EqualTo(strength), "Display snapshots must not mutate rules.");
            profile.growth = 999;
            Assert.That(match.startingProfile.growth, Is.EqualTo(2));
            CompetitionRules.Validate(match);
        }

        // Literal outcomes include the original blade tag bonus; each row distinguishes stat and passive routes.
        [TestCase("slash", 11, 8, 8, 0, 0, 0)]
        [TestCase("bolt", 5, 8, 5, 0, 0, 0)]
        [TestCase("flame", 7, 10, 7, 0, 0, 0)]
        [TestCase("guard", 0, 0, 0, 7, 7, 10)]
        [TestCase("arc-slash", 20, 17, 17, 0, 0, 0)]
        [TestCase("fire-bolt", 17, 20, 17, 0, 0, 0)]
        [TestCase("guard-slash", 12, 10, 10, 9, 9, 12)]
        [TestCase("flame-arc", 28, 29, 27, 0, 0, 0)]
        [TestCase("citadel", 15, 13, 13, 18, 18, 21)]
        public void SkillUsesItsStatTagGearAndAdventurerPassive(string skillId, int swordDamage, int runeDamage, int guardianDamage,
            int swordBlock, int runeBlock, int guardianBlock)
        {
            var definition = Definition();
            var skills = definition.combat.basics.Concat(definition.combat.combos);
            var skill = skills.Single(s => s.id == skillId);
            var ids = new[] { "wanderer", "scholar", "vanguard" };
            var damage = new[] { swordDamage, runeDamage, guardianDamage }; var block = new[] { swordBlock, runeBlock, guardianBlock };
            for (int i = 0; i < ids.Length; ++i)
            {
                var match = CompetitionRules.Start(definition, new Profile(), 123, "skill-" + i, ids[i]);
                var p = match.participants[0]; p.enemy = Enemy(1000, 40);
                var log = CompetitionCombat.Resolve(match.definition, p, skill.symbols);
                Assert.That(log.skills.Single().skillId, Is.EqualTo(skillId));
                Assert.That(log.skills[0].damage, Is.EqualTo(damage[i]), ids[i]);
                Assert.That(log.skills[0].block, Is.EqualTo(block[i]), ids[i]);
                Assert.That(log.retaliationDamage, Is.EqualTo(40 - block[i]));
                Assert.That(log.retaliation, Is.True);
            }
        }

        [Test] public void MixedCoefficientFloorsOnceThenAppliesFireGearAndPassive()
        {
            var definition = Definition();
            var stats = definition.adventurers.Single(a => a.id == "scholar").stats;
            stats.strength = 5; stats.arcane = 7;
            var match = CompetitionRules.Start(definition, new Profile { startingWeapon = "wand" }, 123, "mixed", "scholar");
            var p = match.participants[0]; p.enemy = Enemy(100, 5);
            var log = CompetitionCombat.Resolve(match.definition, p, new[] { Symbol.Sword, Symbol.Staff, Symbol.Fire });
            Assert.That(log.skills.Single().damage, Is.EqualTo(33), "23 + floor((5+7)/2) + fire gear 3 + passive 1; flooring terms separately would give 32.");
            Assert.That(CompetitionStats.ScalingText(definition, "flame-arc"), Does.Contain("무력").And.Contain("비전").And.Contain("내림"));
        }

        [Test] public void PartialSkillsShareShieldSkipAfterKillAndCannotCreateZeroEffects()
        {
            var match = CompetitionRules.Start(Definition(), new Profile(), 123, "partial", "scholar");
            var p = match.participants[0]; p.enemy = Enemy(6, 40); p.enemy.shield = 2;
            var log = CompetitionCombat.Resolve(match.definition, p, new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword });
            Assert.That(log.skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "arc-slash" }));
            Assert.That(log.skills[0].damage, Is.EqualTo(6));
            Assert.That(log.skills[1].status, Is.EqualTo("enemy-defeated"));
            Assert.That(log.retaliation, Is.False); Assert.That(p.hp, Is.EqualTo(60));
            p.enemy = Enemy(100, 40); p.equipment[1] = "mail";
            log = CompetitionCombat.Resolve(match.definition, p, new[] { Symbol.Shield });
            Assert.That(log.skills.Single().damage, Is.Zero);
            Assert.That(log.playerBlock, Is.EqualTo(10), "3 base + 4 GRD + matching mail 3; scholar adds no block passive.");
        }

        [Test] public void EncounterXpRaisesAllThreeStatsAndHpWithoutChangingAccountAndNewMatchResetsRun()
        {
            var profile = new Profile { growth = 2 };
            var match = CompetitionRules.Start(Definition(), profile, 123, "run-growth", "scholar");
            var p = match.participants[0]; p.hp = 30; p.xp = 7;
            p.position = 4; p.offer = CompetitionEncounterRules.CreateOffer(match, p); p.phase = CompetitionPhase.Encounter;
            CompetitionCommandRules.Apply(match, "choose", "well-train");
            var detail = CompetitionStats.Describe(match.definition, p);
            Assert.That(new[] { p.level, p.xp, p.hp, p.maxHp }, Is.EqualTo(new[] { 2, 7, 36, 76 }));
            Assert.That(new[] { detail.strength, detail.guard, detail.arcane, detail.levelBonus, detail.levelHp, detail.accountHp },
                Is.EqualTo(new[] { 5, 5, 7, 1, 6, 10 }));
            Assert.That(profile.growth, Is.EqualTo(2)); Assert.That(match.startingProfile.growth, Is.EqualTo(2));
            var restarted = CompetitionRules.Start(match.definition, profile, 456, "new-run", "scholar");
            var fresh = CompetitionStats.Describe(restarted.definition, restarted.participants[0]);
            Assert.That(new[] { restarted.participants[0].level, restarted.participants[0].xp, fresh.hp, fresh.strength, fresh.guard, fresh.arcane },
                Is.EqualTo(new[] { 1, 0, 70, 4, 4, 6 }));
        }

        [Test] public void LegacyVersionsOmitNewNestedFieldsAndKeepTheirUniformPower()
        {
            foreach (var version in new[] { CompetitionContent.LegacyVersion, CompetitionContent.RulesVersion })
            {
                var definition = CompetitionRulesTests.Definition(); definition.rulesVersion = version;
                string encodedDefinition = LoopJson.Encode(definition);
                var match = CompetitionRules.Start(definition, new Profile { growth = 2 }, 123, "legacy");
                Assert.That(CompetitionStats.IsEnabled(definition), Is.False);
                Assert.That(CompetitionStats.Preview(definition, new Profile(), "wanderer"), Is.Null);
                Assert.That(CompetitionStats.Describe(definition, match.participants[0]), Is.Null);
                Assert.That(encodedDefinition, Does.Not.Contain("\"growth\"").And.Not.Contain("\"stats\""));
                Assert.That(LoopJson.Encode(match.participants[0]), Does.Not.Contain("\"growth\""));
                Assert.That(match.participants.All(p => p.power == 5 && p.guard == 4 && p.hp == 70), Is.True);
                Assert.That(LoopJson.Encode(LoopJson.Copy(match)), Is.EqualTo(LoopJson.Encode(match)));
            }
        }

        [Test] public void V3HumanCommandsReplayAllAiStateAndResumeAcrossSerialization()
        {
            var definition = Definition(); var profile = new Profile { growth = 2 };
            var match = CompetitionRules.Start(definition, profile, 123, "v3-replay", "vanguard");
            var entries = new List<CompetitionCommandEntry>(); var expected = new List<string>();
            for (int i = 0; !match.finished && i < 300; ++i)
            {
                var command = CompetitionRulesTests.NextCommand(match);
                entries.Add(LoopJson.Copy(command));
                CompetitionCommandRules.Apply(match, command.kind, command.arg);
                CompetitionRules.Validate(match); expected.Add(LoopJson.Encode(match));
            }
            Assert.That(match.finished, Is.True); Assert.That(match.participants.Any(p => p.level > 1), Is.True);
            var replay = CompetitionRules.Start(definition, profile, 123, "v3-replay", "vanguard");
            for (int i = 0; i < entries.Count; ++i)
            {
                if (i == entries.Count / 2) replay = LoopJson.Copy(replay);
                Assert.That(CompetitionCommandRules.Target(replay), Is.EqualTo(entries[i].targetId));
                CompetitionCommandRules.Apply(replay, entries[i].kind, entries[i].arg);
                Assert.That(LoopJson.Encode(replay), Is.EqualTo(expected[i]), "command " + (i + 1));
            }
        }

        [Test] public void InvalidStatsScalingAndVersionPayloadsAreRejected()
        {
            var definition = Definition(); definition.growth.scaling[0].divisor = 0;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
            definition = Definition(); definition.adventurers[0].stats = null;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
            definition = Definition(); definition.rulesVersion = CompetitionContent.RulesVersion;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(definition));
            var match = CompetitionRules.Start(Definition(), new Profile(), 123, "invalid-state");
            match.participants[0].growth.accountHp = 5;
            Assert.Throws<ArgumentException>(() => CompetitionRules.Validate(match));
        }

        static Enemy Enemy(int hp, int attack) => new Enemy { hp = hp, maxHp = hp, attack = attack, intent = Intent.Attack };
    }
}
