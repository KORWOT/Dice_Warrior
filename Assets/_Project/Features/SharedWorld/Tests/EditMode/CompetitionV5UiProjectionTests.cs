using System;
using System.Linq;
using System.Reflection;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionV5UiProjectionTests
    {
        static Array Steps(CompetitionState state)
        {
            var method = typeof(CompetitionV5Skills).GetMethod("PlanSteps", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "The UI must receive consumed slots from the shared planner.");
            return (Array)method.Invoke(null, new object[] { state.definition, state.participants[0], state.participants[0].v5.selection.results });
        }
        static int[] Slots(object step) => (int[])step.GetType().GetField("dieSlotIndices").GetValue(step);
        static V5SkillDefinition Skill(object step) => (V5SkillDefinition)step.GetType().GetField("skill").GetValue(step);
        static ParticipantState PreviewLap(CompetitionState state, string choice)
        {
            var method = typeof(CompetitionCommandRules).GetMethod("PreviewLapReward", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Reward preview must share the actual grant path without drawing an intent.");
            return (ParticipantState)method.Invoke(null, new object[] { state, choice });
        }

        [Test] public void PartialPairIdentifiesPhysicalSlotsWhenFacesRepeat()
        {
            var state = CompetitionV5Tests.Selected("face.common.staff", "face.common.staff", "face.common.sword");
            string original = LoopJson.Encode(state); var steps = Steps(state).Cast<object>().ToArray();
            Assert.That(steps.Select(s => Skill(s).id), Is.EqualTo(new[] { "arc-slash", "bolt" }));
            Assert.That(Slots(steps[0]), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(Slots(steps[1]), Is.EqualTo(new[] { 1 }));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(original));
        }

        [Test] public void BlankPhysicalSlotIsNotRenumberedOrAssignedToASkill()
        {
            var state = CompetitionV5Tests.Selected("face.blank", "face.common.staff", "face.common.sword");
            var steps = Steps(state).Cast<object>().ToArray();
            Assert.That(steps.Length, Is.EqualTo(1));
            Assert.That(Skill(steps[0]).id, Is.EqualTo("arc-slash"));
            Assert.That(Slots(steps[0]), Is.EqualTo(new[] { 1, 2 }));
        }

        [TestCase("wanderer")] [TestCase("scholar")] [TestCase("vanguard")]
        public void AllPhysicalResultsMapEachNonblankDieOnceAndMatchExecutedOrder(string hero)
        {
            var template = CompetitionV5Tests.Combat(3, hero);
            CompetitionCommandRules.Apply(template, "combat_roll", "");
            for (int a = 0; a < 6; a++) for (int b = 0; b < 6; b++) for (int c = 0; c < 6; c++)
            {
                var state = LoopJson.Copy(template); var p = state.participants[0]; int[] faces = { a, b, c };
                for (int slot = 0; slot < 3; slot++)
                {
                    var face = CompetitionV5Content.Faces(state.definition, hero, p.dice[slot].definitionId)[faces[slot]];
                    p.v5.selection.results[slot].faceIndex = faces[slot]; p.v5.selection.results[slot].faceId = face.id;
                }
                string original = LoopJson.Encode(state); var steps = Steps(state).Cast<object>().ToArray();
                var used = steps.SelectMany(Slots).OrderBy(i => i).ToArray();
                Assert.That(used, Is.EqualTo(p.v5.selection.results.Where(r => r.faceId != CompetitionV5Content.BlankFaceId).Select(r => r.slotIndex)));
                foreach (var step in steps)
                    Assert.That(Slots(step).Select(i => p.v5.selection.results[i].faceId).OrderBy(id => id, StringComparer.Ordinal),
                        Is.EqualTo(Skill(step).faceIds.OrderBy(id => id, StringComparer.Ordinal)));
                var preview = CompetitionV5Skills.Preview(state.definition, p);
                Assert.That(preview.skills.Select(s => s.skillId), Is.EqualTo(steps.Select(s => Skill(s).id)));
                for (int i = 0; i < steps.Length; i++) Assert.That(Slots(preview.skills[i]), Is.EqualTo(Slots(steps[i])));
                Assert.That(LoopJson.Encode(state), Is.EqualTo(original), "Queries cannot consume RNG or mutate any participant.");
                CompetitionCommandRules.Apply(state, "combat_execute", "");
                Assert.That(p.v5.lastTurn.plannedSkillIds, Is.EqualTo(steps.Select(s => Skill(s).id)));
            }
        }

        [TestCase(0, "balanced")] [TestCase(1, "arcane")]
        [TestCase(2, "blade")] [TestCase(2, "wand")] [TestCase(2, "mail")]
        [TestCase(2, "charm")] [TestCase(2, "aegis")]
        public void LapPreviewMatchesGrantIncludingLevelAndHpCapWithoutRngOrAi(int completed, string choice)
        {
            var state = LapState(completed); var p = state.participants[0];
            string original = LoopJson.Encode(state); uint rng = p.combatRng;
            var preview = PreviewLap(state, choice);
            Assert.That(LoopJson.Encode(state), Is.EqualTo(original));
            Assert.That(preview.combatRng, Is.EqualTo(rng), "Even the preview copy must not roll a future enemy intent.");
            Assert.That(preview.travelRng, Is.EqualTo(p.travelRng));
            Assert.That(preview.eventRng, Is.EqualTo(p.eventRng));
            Assert.That(preview.aiRng, Is.EqualTo(p.aiRng));
            var actual = LoopJson.Copy(state); CompetitionCommandRules.Apply(actual, "lap", choice); var awarded = actual.participants[0];
            Assert.That(new[] { preview.hp, preview.maxHp, preview.level, preview.xp, preview.gold, preview.completedLaps },
                Is.EqualTo(new[] { awarded.hp, awarded.maxHp, awarded.level, awarded.xp, awarded.gold, awarded.completedLaps }));
            Assert.That(preview.level, Is.EqualTo(2)); Assert.That(preview.hp, Is.EqualTo(preview.maxHp));
            Assert.That(preview.equipment, Is.EqualTo(awarded.equipment));
            Assert.That(LoopJson.Encode(preview.dice), Is.EqualTo(LoopJson.Encode(awarded.dice)));
            Assert.That(preview.claimed, Is.EqualTo(awarded.claimed));
            Assert.That(LoopJson.Encode(PreviewLap(state, choice)), Is.EqualTo(LoopJson.Encode(preview)), "Repeated lookup is deterministic.");
        }

        static CompetitionState LapState(int completed)
        {
            var state = CompetitionV5Tests.Combat(completed + 1); var p = state.participants[0];
            p.phase = CompetitionPhase.LapReward; p.position = 0; p.enemy = null; p.offer = null;
            p.v4.enemyId = null; p.v5.selection = null; p.hp = p.maxHp - 2; p.xp = 7;
            CompetitionRules.Validate(state); return state;
        }

        static string Text(string method, params object[] args)
        {
            var type = typeof(CompetitionV5UiText);
            Assert.That(type, Is.Not.Null, "Pure UI descriptions must be exercised without Unity or external state.");
            return (string)type.GetMethod(method).Invoke(null, args);
        }

        [Test] public void TextShowsActualSixFacesAndRegisteredNamesWithoutMutatingDefinition()
        {
            var d = CompetitionV5Tests.Definition(); string before = LoopJson.Encode(d);
            var faces = CompetitionV5Content.Faces(d, "scholar", "arcane");
            string text = Text("Die", d, "scholar", "arcane");
            for (int i = 0; i < 6; i++) Assert.That(text, Does.Contain((i + 1) + ": " + faces[i].name));
            var tag = d.v4.tags.First(t => t.id.StartsWith("family."));
            Assert.That(Text("Friendly", d, tag.id), Is.EqualTo(tag.name));
            Assert.That(LoopJson.Encode(d), Is.EqualTo(before));
        }

        [Test] public void TextExplainsHpCostBoundaryTreasureLuckAndScoutingRewards()
        {
            var state = CompetitionV5Tests.Combat(); var p = state.participants[0]; var d = state.definition;
            var e = d.encounters.First(x => x.hpCost > 0 && d.v4.treasureEncounterIds.Contains(x.id));
            p.hp = e.hpCost;
            var choice = new EncounterChoice { id = e.id, kind = e.kind, hpCost = e.hpCost, gold = e.gold, xp = e.xp,
                honor = e.honor, available = false, unavailableReason = "체력이 부족합니다." };
            string original = LoopJson.Encode(state); string text = Text("Encounter", d, p, choice);
            Assert.That(text, Does.Contain("HP " + e.hpCost + " 소모"));
            Assert.That(text, Does.Contain("최소 HP " + Math.Max(e.minHp, e.hpCost + 1)));
            Assert.That(text, Does.Contain("선택 불가: 체력이 부족합니다."));
            int bonus = CompetitionAttributes.Describe(d, p).attributes.luck / 5;
            Assert.That(text, Does.Contain("골드 +" + (e.gold + bonus)));
            Assert.That(text, Does.Contain("행운 +" + bonus));
            var scout = d.encounters.Single(x => x.id == "pass-scout");
            string scouting = Text("Encounter", d, p, new EncounterChoice { id = scout.id, kind = scout.kind,
                xp = scout.xp, honor = scout.honor, available = true });
            Assert.That(scouting, Does.Contain("경험치 +" + scout.xp));
            Assert.That(scouting, Does.Contain("명예 +" + scout.honor));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(original));
        }

        [Test] public void TextLapReportsProjectedLevelAndExactHpRatherThanUncappedHeal()
        {
            var state = LapState(2); var p = state.participants[0]; var preview = PreviewLap(state, "aegis");
            string original = LoopJson.Encode(state); string text = Text("Lap", state, "aegis");
            Assert.That(text, Does.Contain($"HP {p.hp}/{p.maxHp} → {preview.hp}/{preview.maxHp}"));
            Assert.That(text, Does.Contain($"레벨 {p.level} → {preview.level}"));
            Assert.That(text, Does.Contain("선택 즉시")); Assert.That(text, Does.Contain("최종 시련"));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(original));
        }

        [Test] public void TextGearComparesSameSlotAndResultsKeepSharedRanksAndReasons()
        {
            var state = CompetitionV5Tests.Combat(); var p = state.participants[0];
            p.equipment[1] = "mail";
            string text = Text("Gear", state.definition, p, "aegis");
            Assert.That(text, Does.Contain(state.definition.combat.gear.Single(g => g.id == "mail").name));
            Assert.That(text, Does.Contain(state.definition.combat.gear.Single(g => g.id == "aegis").name));
            for (int i = 0; i < 4; i++) { state.participants[i].name = "참가자 " + i; state.participants[i].rank = i < 2 ? 1 : i + 1; state.participants[i].hp = i < 2 ? 60 : 30 - i; }
            state.finished = true; state.endReason = "round-limit";
            p.eliminated = true; p.message = "전투 제한 턴에 도달해 탈락했습니다.";
            string original = LoopJson.Encode(state); string result = Text("Results", state);
            Assert.That(result, Does.Contain("공동 1위")); Assert.That(result, Does.Contain("라운드 제한"));
            Assert.That(result, Does.Contain(p.message)); Assert.That(result, Does.Contain("시련 성공 → 완주 → 명예 → 남은 HP"));
            var ranked = CompetitionRules.Rankings(state);
            for (int i = 1; i < ranked.Length; i++) Assert.That(result.IndexOf(ranked[i].name, StringComparison.Ordinal),
                Is.GreaterThan(result.IndexOf(ranked[i - 1].name, StringComparison.Ordinal)));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(original));
        }

        [TestCase("blade")] [TestCase("wand")]
        public void TextGearWithoutParticipantDescribesStartingWeaponWithoutInventingCurrentEquipment(string gearId)
        {
            var d = CompetitionV5Tests.Definition(); string original = LoopJson.Encode(d);
            var gear = d.combat.gear.Single(value => value.id == gearId);
            string text = Text("Gear", d, null, gearId);
            Assert.That(text, Does.Contain("다음 모험 시작 무기"));
            Assert.That(text, Does.Contain("후보: " + gear.name));
            Assert.That(text, Does.Contain(gearId == "blade" ? "강철" : "화염"));
            Assert.That(text, Does.Contain("피해 +" + d.v4.gear.Single(value => value.gearId == gearId).bindings.Single().amount));
            Assert.That(text, Does.Not.Contain("현재:"));
            Assert.That(text, Does.Not.Contain("현재 장착 중"));
            Assert.That(text, Does.Not.Contain("교체"));
            Assert.That(LoopJson.Encode(d), Is.EqualTo(original));
        }

        [TestCase("forest-risk")] [TestCase("pass-risk")] [TestCase("citadel-curse")]
        [TestCase("rift-risk")] [TestCase("hill-supply")]
        public void TreasureGoldProjectionMatchesActualRewardBeforeLevelUp(string encounterId)
        {
            var state = CompetitionRules.Start(CompetitionV5Tests.Definition(), new Profile(), 123, "ui-reward", "scholar");
            var p = state.participants[0]; var d = state.definition; var e = d.encounters.Single(value => value.id == encounterId);
            p.xp = 7; p.gold = 50; p.phase = CompetitionPhase.Encounter;
            p.position = Array.FindIndex(d.world.places, place => place.encounterIds.Contains(encounterId));
            var choice = new EncounterChoice { id = e.id, label = e.label, description = e.description, kind = e.kind,
                goldCost = e.goldCost, hpCost = e.hpCost, gold = e.gold, xp = e.xp, honor = e.honor, heal = e.heal, available = true };
            p.offer = new EncounterOffer { id = "ui-offer", placeId = d.world.places[p.position].id, choices = new[] { choice,
                new EncounterChoice { id = "pass-scout", kind = EncounterKind.Study, available = true } } };
            CompetitionRules.Validate(state); string before = LoopJson.Encode(state);
            int expected = CompetitionEncounterRules.RewardGold(d, p, choice);
            Assert.That(Text("Encounter", d, p, choice), Does.Contain("골드 +" + expected));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
            var actual = LoopJson.Copy(state); CompetitionCommandRules.Apply(actual, "choose", encounterId);
            Assert.That(actual.participants[0].gold, Is.EqualTo(p.gold - e.goldCost + expected));
            Assert.That(actual.participants[0].combatRng, Is.EqualTo(p.combatRng));
            Assert.That(actual.participants[0].eventRng, Is.EqualTo(p.eventRng));
        }
    }
}
