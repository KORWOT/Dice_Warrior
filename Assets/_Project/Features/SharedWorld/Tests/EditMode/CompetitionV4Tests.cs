using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionV4Tests
    {
        [Test] public void V3InitialAndFinalStateHashesRemainExactlyThePreV4Baseline()
        {
            var rows = new[] {
                new[] { "wanderer", "368D89575E0BDF99C20E5FE36440EBF55546C02C40A4462D72EEA951D3393AEB", "B40A05D6D2308AE8D95D6B60377561041075F7462A689939A4233FA65C8BFE01" },
                new[] { "scholar", "EF7C6F0E05FC8758A6D6582761162CF608D99EF3B0D524D141458510FFB231C7", "06B2D29271BD5D8F4405D84AB76D6312B665185762419C88D3FDA76E9194FFC8" },
                new[] { "vanguard", "68F01858EEC661CC5D824171188CB8572707F43BCB8C0BCADBC31ABB919A3DCD", "FC2F99E4DA8AE7A9FF08FC20766DDF754998681E865BCE999210A4FD8C53F80A" } };
            foreach (var row in rows)
            {
                var state = CompetitionRules.Start(CompetitionGrowthTests.Definition(), new Profile { growth = 2 }, 123, "v3-frozen", row[0]);
                Assert.That(LoopJson.Hash(LoopJson.Encode(state)), Is.EqualTo(row[1]), row[0] + " initial");
                for (int n = 0; !state.finished && n < 300; n++)
                {
                    var c = CompetitionRulesTests.NextCommand(state);
                    CompetitionCommandRules.Apply(state, c.kind, c.arg);
                }
                Assert.That(state.finished, Is.True);
                Assert.That(LoopJson.Hash(LoopJson.Encode(state)), Is.EqualTo(row[2]), row[0] + " final");
            }
        }
        public static CompetitionDefinition Definition()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-4.json");
            if (!File.Exists(path)) path = Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Content", "shared-world-preview-4.json");
            return LoopJson.Decode<CompetitionDefinition>(File.ReadAllText(path));
        }

        [Test] public void NewVersionCanStartWhileV3KeepsItsOriginalSerialization()
        {
            var old = CompetitionGrowthTests.Definition();
            string before = LoopJson.Encode(CompetitionRules.Start(old, new Profile(), 123, "old"));
            Assert.That(File.Exists(Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Content", "shared-world-preview-4.json")) ||
                File.Exists(Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-4.json")), Is.True, "New rules need separate frozen v4 content.");
            var state = CompetitionRules.Start(Definition(), new Profile(), 123, "new");
            Assert.That(state.definition.rulesVersion, Is.EqualTo("shared-world-preview-4"));
            Assert.That(LoopJson.Encode(CompetitionRules.Start(old, new Profile(), 123, "old")), Is.EqualTo(before));
        }

        [TestCase("wanderer", 7, 6, 5, 3, 5, 4, 70)]
        [TestCase("scholar", 3, 4, 5, 7, 6, 5, 70)]
        [TestCase("vanguard", 6, 3, 7, 3, 7, 4, 82)]
        public void SixAttributesAndSameAccountHpReachHumanAiAndPreview(string id, int str, int dex, int con, int intel, int will, int luck, int hp)
        {
            var d = Definition(); var profile = new Profile { growth = 2 };
            var state = CompetitionRules.Start(d, profile, 123, "attributes", id);
            var detail = CompetitionAttributes.Describe(state.definition, state.participants[0]);
            Assert.That(CompetitionAttributes.All.Select(detail.attributes.Get), Is.EqualTo(new[] { str, dex, con, intel, will, luck }));
            Assert.That(new[] { detail.hp, detail.maxHp, detail.baseHp, detail.constitutionHp, detail.accountHp }, Is.EqualTo(new[] { hp, hp, 30, con * 6, 10 }));
            Assert.That(state.participants.Select(p => p.adventurerId), Is.EqualTo(new[] { id, "wanderer", "scholar", "vanguard" }));
            Assert.That(state.participants.Skip(1).Select(p => p.maxHp), Is.EqualTo(new[] { 70, 70, 82 }));
            Assert.That(state.participants.All(p => p.v4.accountHp == 10 && p.growth == null), Is.True);
            var preview = CompetitionAttributes.Preview(d, profile, id); Assert.That(preview.maxHp, Is.EqualTo(hp));
            preview.attributes.strength = 999; Assert.That(CompetitionAttributes.Describe(d, state.participants[0]).attributes.strength, Is.EqualTo(str));
        }

        [TestCase("slash", DamageType.Physical, 12, 7, 9, 0, 0, 0)]
        [TestCase("bolt", DamageType.Lightning, 5, 9, 6, 0, 0, 0)]
        [TestCase("flame", DamageType.Fire, 6, 11, 6, 0, 0, 0)]
        [TestCase("guard", DamageType.Physical, 0, 0, 0, 8, 8, 11)]
        [TestCase("arc-slash", DamageType.Ice, 20, 18, 17, 0, 0, 0)]
        [TestCase("fire-bolt", DamageType.Fire, 17, 21, 17, 0, 0, 0)]
        [TestCase("guard-slash", DamageType.Physical, 12, 11, 13, 10, 10, 13)]
        [TestCase("flame-arc", DamageType.Fire, 28, 29, 27, 0, 0, 0)]
        [TestCase("citadel", DamageType.Physical, 15, 14, 16, 19, 19, 22)]
        public void AllSkillsHaveApprovedTypesCoefficientsAndSingleGearPassiveApplication(string skillId, DamageType type,
            int sword, int rune, int guardian, int swordBlock, int runeBlock, int guardianBlock)
        {
            var damage = new[] { sword, rune, guardian }; var blocks = new[] { swordBlock, runeBlock, guardianBlock };
            int i = 0;
            foreach (var id in new[] { "wanderer", "scholar", "vanguard" })
            {
                var state = NeutralCombat(id); var p = state.participants[0];
                var skill = state.definition.combat.basics.Concat(state.definition.combat.combos).Single(s => s.id == skillId);
                string before = LoopJson.Encode(p);
                var preview = CompetitionElements.SkillPreview(state.definition, p, skillId);
                Assert.That(LoopJson.Encode(p), Is.EqualTo(before), "Preview cannot consume RNG or mutate state");
                var log = CompetitionCombat.Resolve(state.definition, p, skill.symbols);
                Assert.That(log.skills.Single().damage, Is.EqualTo(damage[i]), id + " damage");
                Assert.That(log.skills.Single().block, Is.EqualTo(blocks[i]), id + " block");
                Assert.That(p.v4.lastTurn.skills.Single().damageType, Is.EqualTo(type));
                Assert.That(preview.damageBeforeShield, Is.EqualTo(damage[i]));
                Assert.That(log.retaliationDamage, Is.EqualTo(40 - blocks[i++]));
            }
        }

        [Test] public void ConversionRebuildsElementTagsBeforeFireBindingsAndKeepsIdentitySeparate()
        {
            var d = Definition(); d.v4.gear.Single(g => g.gearId == "wand").conversion = DamageType.Ice;
            CompetitionContent.Validate(d);
            var state = CompetitionRules.Start(d, new Profile { startingWeapon = "wand" }, 123, "conversion", "scholar"); var p = state.participants[0];
            var step = CompetitionElements.SkillPreview(d, p, "flame");
            Assert.That(step.originalType, Is.EqualTo(DamageType.Fire)); Assert.That(step.damageType, Is.EqualTo(DamageType.Ice));
            Assert.That(step.damageBeforeShield, Is.EqualTo(10), "3 + INT7; fire wand +3 and fire passive +1 must both disappear");
            Assert.That(step.modifiers, Is.Empty);
            Assert.That(step.tags, Does.Contain("DamageComponent/damage:element.ice").And.Not.Contain("DamageComponent/damage:element.fire"));
            Assert.That(step.tags, Does.Contain("Skill/skill:family.fire"), "Identity is independent of converted type");
            p.equipment[0] = "blade";
            Assert.That(CompetitionElements.SkillPreview(d, p, "flame").damageBeforeShield, Is.EqualTo(11));
        }

        [Test] public void PartialComboUsesOneShieldAndSkipsAfterKillWithoutRetaliation()
        {
            var state = NeutralCombat("scholar"); var p = state.participants[0]; p.enemy.hp = 7; p.enemy.shield = 2;
            var log = CompetitionCombat.Resolve(state.definition, p, new[] { Symbol.Staff, Symbol.Staff, Symbol.Sword });
            Assert.That(log.skills.Select(s => s.skillId), Is.EqualTo(new[] { "bolt", "arc-slash" }));
            Assert.That(log.skills.Select(s => s.damage), Is.EqualTo(new[] { 7, 0 }));
            Assert.That(p.v4.lastTurn.skills[1].status, Is.EqualTo("enemy-defeated"));
            Assert.That(log.retaliation, Is.False); Assert.That(p.hp, Is.EqualTo(60));
        }

        [Test] public void ConstitutionGrowthAddsSixHpOnceAndTreasureLuckIsPaidOnce()
        {
            var state = CompetitionRules.Start(Definition(), new Profile { growth = 2 }, 123, "growth", "scholar"); var p = state.participants[0];
            p.hp = 30; p.xp = 7; p.position = 4; p.offer = CompetitionEncounterRules.CreateOffer(state, p); p.phase = CompetitionPhase.Encounter;
            CompetitionCommandRules.Apply(state, "choose", "well-train");
            Assert.That(new[] { p.level, p.xp, p.hp, p.maxHp }, Is.EqualTo(new[] { 2, 7, 36, 76 }));
            Assert.That(CompetitionAttributes.All.Select(CompetitionAttributes.Describe(state.definition, p).attributes.Get), Is.EqualTo(new[] { 4, 5, 6, 8, 7, 6 }));
            p.position = 6; p.offer = CompetitionEncounterRules.CreateOffer(state, p); p.phase = CompetitionPhase.Encounter;
            int gold = p.gold; CompetitionCommandRules.Apply(state, "choose", "citadel-curse");
            Assert.That(p.gold - gold, Is.EqualTo(31));
            string before = LoopJson.Encode(state);
            Assert.Throws<InvalidOperationException>(() => CompetitionCommandRules.Apply(state, "choose", "citadel-curse"));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
        }

        [Test] public void AffinityAiHeuristicAndViewsUseNoCombatOrEventDraws()
        {
            var state = CompetitionRules.Start(Definition(), new Profile(), 123, "read-only"); var p = state.participants[1];
            string before = LoopJson.Encode(state);
            foreach (var e in state.definition.v4.enemies)
                CompetitionElements.MatchupScore(state.definition, p, e.encounterId);
            foreach (var skill in state.definition.v4.skills)
            {
                CompetitionElements.SkillPreview(state.definition, p, skill.skillId);
                CompetitionElements.ActionTags(state.definition, p, skill.skillId, skill.damageType);
            }
            Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
            var fireWeak = state.definition.v4.enemies.Single(e => e.encounterId == "forest-wolf");
            var flame = CompetitionElements.SkillPreview(state.definition, p, "flame", fireWeak);
            Assert.That(flame.affinityBasisPoints, Is.EqualTo(12000)); Assert.That(flame.damageBeforeShield, Is.EqualTo(7));
            var ice = CompetitionElements.SkillPreview(state.definition, p, "arc-slash", fireWeak);
            Assert.That(ice.affinityBasisPoints, Is.EqualTo(8500)); Assert.That(ice.damageBeforeShield, Is.EqualTo(17));
        }

        [Test] public void V4HumanCommandsReplayAllAiAndResumeWithIdenticalTypedResults()
        {
            var state = CompetitionRules.Start(Definition(), new Profile { growth = 2 }, 123, "v4-replay", "vanguard");
            var replay = LoopJson.Copy(state); var history = new List<CompetitionCommandEntry>(); var hashes = new List<string>();
            for (int i = 0; !state.finished && i < 300; ++i)
            {
                var command = CompetitionRulesTests.NextCommand(state); history.Add(command);
                CompetitionCommandRules.Apply(state, command.kind, command.arg); hashes.Add(LoopJson.Hash(LoopJson.Encode(state)));
            }
            Assert.That(state.finished, Is.True); Assert.That(state.participants.Any(p => p.level > 1), Is.True);
            for (int i = 0; i < history.Count; ++i)
            {
                if (i == history.Count / 2) replay = LoopJson.Copy(replay);
                CompetitionCommandRules.Apply(replay, history[i].kind, history[i].arg);
                Assert.That(LoopJson.Hash(LoopJson.Encode(replay)), Is.EqualTo(hashes[i]), "command " + i);
            }
        }

        [Test] public void InvalidCoefficientsAffinitiesBindingsAndLegacyPayloadAreRejected()
        {
            var d = Definition(); d.v4.skills[0].damageScaling.dexterity = 4000;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d));
            d = Definition(); d.v4.enemies[0].weakness = d.v4.enemies[0].resistance;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d));
            d = Definition(); d.v4.gear[0].grantedActionTags = new[] { "element.fire" };
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d));
            d = Definition(); d.v4.adventurers[0].bindings[0].target = "strength";
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d), "A base attribute cannot depend on an action");
            d = Definition(); d.rulesVersion = CompetitionContent.StatsVersion;
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d));
            var state = NeutralCombat("wanderer"); var p = state.participants[0];
            state.definition.v4.skills[0].damageScaling.strength = -1;
            string before = LoopJson.Encode(p);
            Assert.Throws<ArgumentException>(() => CompetitionCombat.Resolve(state.definition, p, new[] { Symbol.Sword }));
            Assert.That(LoopJson.Encode(p), Is.EqualTo(before), "Failed calculation must not publish partial HP/shield/log changes");
        }

        [TestCase("query")] [TestCase("grant")] [TestCase("skill")] [TestCase("enemy")]
        public void OmittedOwnerOrDamageTypeCannotSilentlyDefaultToSourceOrPhysical(string field)
        {
            var d = Definition(); var p = CompetitionRules.Start(d, new Profile(), 123, "missing-type").participants[0];
            if (field == "query")
            {
                var q = LoopJson.Decode<TagQuery>("{\"ownerId\":\"self\",\"all\":[\"actor.adventurer\"]}");
                Assert.Throws<ArgumentException>(() => CompetitionV4Content.Tags(d, p).Matches(q));
            }
            else if (field == "grant")
            {
                var grant = LoopJson.Decode<TagGrant>("{\"ownerId\":\"self\",\"tagId\":\"actor.adventurer\",\"sourceKind\":\"adventurer\",\"sourceId\":\"wanderer\"}");
                Assert.Throws<ArgumentException>(() => CompetitionTags.Build(d.v4.tags, d.v4.conflicts, new[] { grant }, TagStage.FinalResult));
            }
            else
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(LoopJson.Encode(d));
                var target = (Newtonsoft.Json.Linq.JObject)json["v4"][field == "skill" ? "skills" : "enemies"][0];
                target.Property(field == "skill" ? "damageType" : "attackType").Remove();
                Assert.Throws<ArgumentException>(() => CompetitionContent.Load(json.ToString()));
            }
        }

        static CompetitionState NeutralCombat(string id)
        {
            var d = Definition(); foreach (var e in d.v4.enemies) { e.weakness = null; e.resistance = null; }
            var state = CompetitionRules.Start(d, new Profile(), 123, "skill-" + id, id);
            var p = state.participants[0]; p.enemy = new Enemy { hp = 1000, maxHp = 1000, attack = 40, intent = Intent.Attack };
            p.v4.enemyId = "forest-wolf"; return state;
        }
    }
}
