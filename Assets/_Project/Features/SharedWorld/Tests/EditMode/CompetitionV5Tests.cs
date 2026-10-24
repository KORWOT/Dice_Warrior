using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionV5Tests
    {
        public static CompetitionDefinition Definition()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-5.json");
            if (!File.Exists(path)) path = Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Content", "shared-world-preview-5.json");
            return CompetitionContent.Load(File.ReadAllText(path));
        }

        [Test] public void NewMatchUsesV5AndEachStarterHasFourCommonOneOwnedAndOneBlankPhysicalFace()
        {
            var state = CompetitionRules.Start(Definition(), new Profile(), 123, "v5-content");
            Assert.That(state.definition.rulesVersion, Is.EqualTo("shared-world-preview-5"));
            Assert.That(state.participants.All(p => p.dice.Count == 1 && p.v5 != null), Is.True);
            foreach (var p in state.participants)
            foreach (string die in new[] { "balanced", "arcane", "steel" })
            {
                var faces = CompetitionV5Content.Faces(state.definition, p.adventurerId, die);
                Assert.That(faces.Length, Is.EqualTo(6));
                Assert.That(faces.Count(f => f.kind == V5FaceKind.Common), Is.EqualTo(4));
                Assert.That(faces.Count(f => f.kind == V5FaceKind.Class && f.adventurerId == p.adventurerId), Is.EqualTo(1));
                Assert.That(faces.Count(f => f.kind == V5FaceKind.Blank), Is.EqualTo(1));
                Assert.That(faces.Any(f => f.id == "$class"), Is.False, "Template IDs must resolve before display and rolling.");
            }
        }

        [Test] public void FirstRollStoresOnePhysicalResultWithoutConsumingTurnOrEnemyAction()
        {
            var state = Combat(); var p = state.participants[0];
            int hp = p.hp, enemyHp = p.enemy.hp, turn = p.turn;
            uint rng = p.combatRng;
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            var selection = p.v5.selection;
            Assert.That(selection.phase, Is.EqualTo(V5SelectionPhase.Selection));
            Assert.That(selection.rerollsLeft, Is.EqualTo(1));
            Assert.That(selection.results.Length, Is.EqualTo(1));
            Assert.That(selection.results[0].dieInstanceId, Is.EqualTo(p.dice[0].instanceId));
            Assert.That(selection.results[0].slotIndex, Is.Zero);
            Assert.That(selection.results[0].faceIndex, Is.InRange(0, 5));
            Assert.That(selection.results[0].held, Is.False);
            Assert.That(selection.results[0].faceId, Is.EqualTo(CompetitionV5Content.Faces(state.definition, p.adventurerId, p.dice[0].definitionId)[selection.results[0].faceIndex].id));
            Assert.That(p.combatRng, Is.Not.EqualTo(rng));
            Assert.That(new[] { p.hp, p.enemy.hp, p.turn }, Is.EqualTo(new[] { hp, enemyHp, turn }));
        }

        [Test] public void HoldAndReleasePreservePhysicalFaceRngTurnAndRerollAllowance()
        {
            var state = Combat(); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            var original = LoopJson.Copy(p.v5.selection.results[0]);
            uint rng = p.combatRng; int hp = p.hp, turn = p.turn;
            CompetitionCommandRules.Apply(state, "combat_hold", original.dieInstanceId);
            Assert.That(p.v5.selection.results[0].held, Is.True);
            CompetitionCommandRules.Apply(state, "combat_release", original.dieInstanceId);
            Assert.That(LoopJson.Encode(p.v5.selection.results[0]), Is.EqualTo(LoopJson.Encode(original)));
            Assert.That(p.combatRng, Is.EqualTo(rng));
            Assert.That(new[] { p.hp, p.turn, p.v5.selection.rerollsLeft, p.v5.selection.holdChanges }, Is.EqualTo(new[] { hp, turn, 1, 2 }));
        }

        [TestCase(false)] [TestCase(true)]
        public void RerollConsumesExactlyOneDrawPerUnheldDieInOriginalSlotOrder(bool holdMiddle)
        {
            var state = Combat(3); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            if (holdMiddle) CompetitionCommandRules.Apply(state, "combat_hold", p.dice[1].instanceId);
            var before = LoopJson.Copy(p.v5.selection.results);
            uint expectedRng = p.combatRng;
            int[] expectedIndices = before.Select(r => r.held ? r.faceIndex : CompetitionRandom.Next(ref expectedRng, 6)).ToArray();
            CompetitionCommandRules.Apply(state, "combat_reroll", "");
            Assert.That(p.combatRng, Is.EqualTo(expectedRng));
            Assert.That(p.v5.selection.results.Select(r => r.faceIndex), Is.EqualTo(expectedIndices));
            Assert.That(p.v5.selection.results.Select(r => r.dieInstanceId), Is.EqualTo(p.dice.Select(d => d.instanceId)));
            Assert.That(p.v5.selection.results.Select(r => r.slotIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(p.v5.selection.rerollsLeft, Is.Zero);
            if (holdMiddle) Assert.That(LoopJson.Encode(p.v5.selection.results[1]), Is.EqualTo(LoopJson.Encode(before[1])));
        }

        [Test] public void AllHeldRejectsRerollButAllowsExecutionAndUnusedRerollDoesNotCarry()
        {
            var state = Combat(3); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            foreach (var die in p.dice) CompetitionCommandRules.Apply(state, "combat_hold", die.instanceId);
            var finalFaces = LoopJson.Copy(p.v5.selection.results);
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_reroll", ""));
            int turn = p.turn;
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(LoopJson.Encode(p.v5.lastTurn.results), Is.EqualTo(LoopJson.Encode(finalFaces)));
            Assert.That(p.turn, Is.EqualTo(turn + 1));
            Assert.That(p.encounterTurns, Is.EqualTo(1));
            Assert.That(p.v5.selection.phase, Is.EqualTo(V5SelectionPhase.AwaitRoll));
            Assert.That(p.v5.selection.results, Is.Empty);
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            Assert.That(p.v5.selection.rerollsLeft, Is.EqualTo(1));
            Assert.That(p.v5.selection.results.All(r => !r.held), Is.True);
        }

        [Test] public void SecondRerollIsRejectedAndExecuteStillWorks()
        {
            var state = Combat(); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            CompetitionCommandRules.Apply(state, "combat_reroll", "");
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_reroll", ""));
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(p.encounterTurns, Is.EqualTo(1));
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            Assert.That(p.v5.selection.rerollsLeft, Is.EqualTo(1));
        }

        [Test] public void CommandsRejectWrongStageForeignInstanceExtraArgumentsAndLegacyCombat()
        {
            var state = Combat(); var p = state.participants[0];
            foreach (string kind in new[] { "combat_execute", "combat_reroll", "combat_hold", "combat_release", "combat" })
                RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, kind, ""));
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_roll", "injected-result"));
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_roll", ""));
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_hold", "foreign-instance"));
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_release", p.dice[0].instanceId));
            CompetitionCommandRules.Apply(state, "combat_hold", p.dice[0].instanceId);
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_hold", p.dice[0].instanceId));
            foreach (string kind in new[] { "combat_execute", "combat_reroll" })
                RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, kind, "forged"));
            var outside = CompetitionRules.Start(Definition(), new Profile(), 123, "outside");
            foreach (string kind in new[] { "combat_roll", "combat_hold", "combat_release", "combat_reroll", "combat_execute" })
                RejectWithoutMutation(outside, () => CompetitionCommandRules.Apply(outside, kind, ""));
        }

        [Test] public void BlankOnlyExecutionHasNoPlayerEffectAndExactlyOneEnemyAction()
        {
            var state = Selected("face.blank"); var p = state.participants[0];
            int hp = p.hp, enemyHp = p.enemy.hp, turn = p.turn;
            var preview = CompetitionV5Skills.Preview(state.definition, p);
            Assert.That(preview.noOp, Is.True); Assert.That(preview.skills, Is.Empty);
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(p.enemy.hp, Is.EqualTo(enemyHp));
            Assert.That(p.hp, Is.EqualTo(hp - 1));
            Assert.That(p.turn, Is.EqualTo(turn + 1));
            Assert.That(p.encounterTurns, Is.EqualTo(1));
            Assert.That(p.lastTurn.skills, Is.Empty);
            Assert.That(p.lastTurn.retaliation, Is.True);
            Assert.That(p.lastTurn.retaliationDamage, Is.EqualTo(1));
            Assert.That(p.v5.lastTurn.noOp, Is.True);
            Assert.That(p.v5.lastTurn.plannedSkillIds, Is.Empty);
            Assert.That(p.v5.selection.phase, Is.EqualTo(V5SelectionPhase.AwaitRoll));
        }

        [TestCase("face.common.sword", "face.common.staff", "face.blank", "arc-slash")]
        [TestCase("face.common.sword", "face.common.staff", "face.common.fire", "flame-arc")]
        [TestCase("face.common.sword", "face.common.shield", "face.common.shield", "citadel")]
        public void PlannerUsesOneLargestRecipeAndDoesNotActivateBlank(string a, string b, string c, string expected)
        {
            var state = Selected(a, b, c); var p = state.participants[0];
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, p.v5.selection.results).Select(s => s.id), Is.EqualTo(new[] { expected }));
        }

        [Test] public void PairPrecedesRemainingSinglesAndTiesUseOrdinalSkillId()
        {
            var state = Selected("face.common.staff", "face.common.staff", "face.common.sword"); var p = state.participants[0];
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, p.v5.selection.results).Select(s => s.id), Is.EqualTo(new[] { "arc-slash", "bolt" }));
            state = Selected("face.common.sword", "face.common.staff", "face.common.shield"); p = state.participants[0];
            state.definition.v5.skills.Single(s => s.id == "guard-slash").priority = 100;
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, p.v5.selection.results).Select(s => s.id), Is.EqualTo(new[] { "arc-slash", "guard" }));
            state.definition.v5.skills.Single(s => s.id == "guard-slash").priority = 101;
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, p.v5.selection.results).Select(s => s.id), Is.EqualTo(new[] { "guard-slash", "bolt" }));
        }

        [Test] public void PreviewIsPureMatchesShieldDamageBlockAndSkipsSkillsAfterKill()
        {
            foreach (bool lethal in new[] { false, true })
            {
                var state = Selected("face.common.sword", "face.common.staff", "face.common.shield"); var p = state.participants[0];
                p.enemy.shield = 2; p.enemy.hp = lethal ? 1 : 1000;
                string before = LoopJson.Encode(state);
                var preview = CompetitionV5Skills.Preview(state.definition, p);
                Assert.That(LoopJson.Encode(state), Is.EqualTo(before), "Preview must not change HP, RNG, result, target, or command count.");
                Assert.That(preview.skills.Select(s => s.skillId), Is.EqualTo(new[] { "arc-slash", "guard" }));
                Assert.That(preview.skills[0].shieldAbsorbed, Is.EqualTo(2));
                if (lethal) { Assert.That(preview.skills[0].damageApplied, Is.EqualTo(1)); Assert.That(preview.skills[1].status, Is.EqualTo("enemy-defeated")); }
                CompetitionCommandRules.Apply(state, "combat_execute", "");
                Assert.That(preview.skills.Select(s => s.skillId), Is.EqualTo(p.v4.lastTurn.skills.Select(s => s.skillId)));
                Assert.That(preview.skills.Select(s => s.damageApplied), Is.EqualTo(p.v4.lastTurn.skills.Select(s => s.damageApplied)));
                Assert.That(preview.skills.Select(s => s.block), Is.EqualTo(p.v4.lastTurn.skills.Select(s => s.block)));
                Assert.That(preview.skills.Select(s => s.status), Is.EqualTo(p.v4.lastTurn.skills.Select(s => s.status)));
                if (lethal) { Assert.That(p.lastTurn.retaliation, Is.False); Assert.That(p.v5.selection, Is.Null); }
            }
        }

        [Test] public void HoldOnlyDoesNotChangePreviewAndRerollRecomputesFromCommittedFaces()
        {
            var state = Combat(3); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            string originalPreview = LoopJson.Encode(CompetitionV5Skills.Preview(state.definition, p));
            CompetitionCommandRules.Apply(state, "combat_hold", p.dice[1].instanceId);
            Assert.That(LoopJson.Encode(CompetitionV5Skills.Preview(state.definition, p)), Is.EqualTo(originalPreview));
            CompetitionCommandRules.Apply(state, "combat_reroll", "");
            var preview = CompetitionV5Skills.Preview(state.definition, p);
            string before = LoopJson.Encode(state);
            for (int i = 0; i < 5; ++i) Assert.That(LoopJson.Encode(CompetitionV5Skills.Preview(state.definition, p)), Is.EqualTo(LoopJson.Encode(preview)));
            Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(p.v5.lastTurn.plannedSkillIds, Is.EqualTo(preview.skills.Select(s => s.skillId)));
        }

        [Test] public void DuplicatePhysicalFacesStayDistinctBySlotInstanceAndFaceIndex()
        {
            var state = Combat(3); var p = state.participants[0];
            foreach (var die in p.dice) die.definitionId = "steel";
            var faces = CompetitionV5Content.Faces(state.definition, p.adventurerId, "steel");
            Assert.That(faces.Length, Is.EqualTo(6));
            Assert.That(faces.Take(2).Select(f => f.id), Is.EqualTo(new[] { "face.common.sword", "face.common.sword" }));
            p.v5.selection = new V5SelectionState { phase = V5SelectionPhase.Selection, combatTurnSequence = p.encounterTurns + 1, rerollsLeft = 1, rollSequence = 1,
                results = new[] { Result(p, 0, 0, "face.common.sword"), Result(p, 1, 1, "face.common.sword"), Result(p, 2, 0, "face.common.sword") } };
            CompetitionRules.Validate(state);
            CompetitionCommandRules.Apply(state, "combat_hold", p.dice[1].instanceId);
            uint rng = p.combatRng; CompetitionRandom.Next(ref rng, 6); CompetitionRandom.Next(ref rng, 6);
            CompetitionCommandRules.Apply(state, "combat_reroll", "");
            Assert.That(p.combatRng, Is.EqualTo(rng));
            Assert.That(p.v5.selection.results[1].faceIndex, Is.EqualTo(1));
            Assert.That(p.v5.selection.results[1].held, Is.True);
            Assert.That(p.v5.selection.results.Select(r => r.dieInstanceId).Distinct().Count(), Is.EqualTo(3));
        }

        [TestCase("wanderer", "face.class.swordsman.signature", "oath", "twin-blade", "face.common.sword")]
        [TestCase("scholar", "face.class.runecaster.signature", "rune-pulse", "rune-burst", "face.common.staff")]
        [TestCase("vanguard", "face.class.guardian.signature", "fortress-stance", "wall-ward", "face.common.shield")]
        public void OwnedFaceAndPairAreAvailableOnlyToTheirAdventurer(string hero, string face, string single, string pair, string common)
        {
            var state = Combat(1, hero); var p = state.participants[0];
            var own = new[] { Result(p, 0, 4, face) };
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, own).Select(s => s.id), Is.EqualTo(new[] { single }));
            state = Combat(2, hero); p = state.participants[0]; own = new[] { Result(p, 0, 4, face) };
            Assert.That(CompetitionV5Skills.Plan(state.definition, p, new[] { own[0], Result(p, 1, Array.FindIndex(CompetitionV5Content.Faces(state.definition, hero, "balanced"), f => f.id == common), common) }).Select(s => s.id), Is.EqualTo(new[] { pair }));
            var foreign = Combat(1, hero == "wanderer" ? "scholar" : "wanderer"); var other = foreign.participants[0];
            Assert.Throws<ArgumentException>(() => CompetitionV5Skills.Plan(foreign.definition, other, new[] { Result(other, 0, 4, face) }));
            var selection = new V5SelectionState { phase = V5SelectionPhase.Selection, combatTurnSequence = other.encounterTurns + 1, rerollsLeft = 1, rollSequence = 1, results = new[] { Result(other, 0, 4, face) } };
            other.v5.selection = selection;
            Assert.Throws<ArgumentException>(() => CompetitionRules.Validate(foreign));
        }

        [TestCase("missing-face")] [TestCase("kind")] [TestCase("owner")] [TestCase("skill-owner")] [TestCase("damage-type")]
        [TestCase("duplicate-recipe")] [TestCase("five-faces")]
        public void InvalidCatalogCannotLoadWithFallbackFacesTypesOrOwners(string fault)
        {
            var d = Definition();
            if (fault == "missing-face") d.v5.diceLayouts[0].faceIds[0] = "face.unknown";
            else if (fault == "kind") d.v5.faces[0].kind = V5FaceKind.Invalid;
            else if (fault == "owner") d.v5.faces.Single(f => f.id == "face.class.swordsman.signature").adventurerId = "scholar";
            else if (fault == "skill-owner") d.v5.skills.Single(s => s.id == "oath").ownerAdventurerId = "scholar";
            else if (fault == "damage-type") d.v5.skills[0].effect.damageType = (DamageType)(-1);
            else if (fault == "five-faces") d.v5.diceLayouts[0].faceIds = d.v5.diceLayouts[0].faceIds.Take(5).ToArray();
            else { var duplicate = LoopJson.Copy(d.v5.skills[0]); duplicate.id = "duplicate-recipe"; duplicate.effect.skillId = duplicate.id; d.v5.skills = d.v5.skills.Concat(new[] { duplicate }).ToArray(); }
            Assert.Throws<ArgumentException>(() => CompetitionContent.Validate(d));
        }

        [TestCase("v5.faces[0].kind")] [TestCase("v5.faces[0].adventurerId")] [TestCase("v5.faces[0].id")]
        [TestCase("v5.skills[0].ownerAdventurerId")] [TestCase("v5.skills[0].effect.damageType")]
        [TestCase("v5.skills[0].faceIds")] [TestCase("v5.baseRerollsPerTurn")]
        public void OmittedRequiredCatalogFieldsAreRejected(string path)
        {
            var json = JObject.Parse(LoopJson.Encode(Definition()));
            ((JProperty)json.SelectToken(path).Parent).Remove();
            Assert.Catch(() => CompetitionContent.Load(json.ToString()), "Required v5 fields cannot become Common/Physical/zero.");
        }

        [TestCase("faceIndex")] [TestCase("slotIndex")] [TestCase("dieInstanceId")] [TestCase("faceId")]
        public void ForgedSelectionCannotBecomeCurrentState(string fault)
        {
            var state = Selected("face.common.sword"); var result = state.participants[0].v5.selection.results[0];
            if (fault == "faceIndex") result.faceIndex = 6;
            else if (fault == "slotIndex") result.slotIndex = 1;
            else if (fault == "dieInstanceId") result.dieInstanceId = "foreign";
            else result.faceId = "face.common.fire";
            Assert.Throws<ArgumentException>(() => CompetitionRules.Validate(state));
        }

        [TestCase("participants[0].v5")]
        [TestCase("participants[0].v5.selection.phase")]
        [TestCase("participants[0].v5.selection.rerollsLeft")]
        [TestCase("participants[0].v5.selection.results[0].held")]
        [TestCase("participants[0].v5.selection.results[0].faceIndex")]
        public void OmittedCurrentSelectionFieldsNeverRegenerateOrDefaultOnResume(string path)
        {
            var json = JObject.Parse(LoopJson.Encode(Selected("face.common.sword")));
            ((JProperty)json.SelectToken(path).Parent).Remove();
            Assert.Catch(() => CompetitionRules.Validate(LoopJson.Decode<CompetitionState>(json.ToString())));
        }

        [Test] public void MissingAndDuplicatePhysicalResultsCannotBeAcceptedAsCurrentSelection()
        {
            var state = Selected("face.common.sword", "face.common.staff"); var p = state.participants[0];
            p.v5.selection.results = p.v5.selection.results.Take(1).ToArray();
            Assert.Throws<ArgumentException>(() => CompetitionRules.Validate(state));
            state = Selected("face.common.sword", "face.common.staff"); p = state.participants[0];
            p.v5.selection.results[1] = LoopJson.Copy(p.v5.selection.results[0]);
            Assert.Throws<ArgumentException>(() => CompetitionRules.Validate(state));
        }

        [Test] public void TwelfthHoldChangeIsAllowedThirteenthRejectedAndExecutionRemainsAvailable()
        {
            var state = Selected("face.common.sword"); var p = state.participants[0];
            for (int i = 0; i < 12; ++i) CompetitionCommandRules.Apply(state, i % 2 == 0 ? "combat_hold" : "combat_release", p.dice[0].instanceId);
            Assert.That(p.v5.selection.holdChanges, Is.EqualTo(12));
            Assert.That(CompetitionV5Dice.CanHold(state, p), Is.False);
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_hold", p.dice[0].instanceId));
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(p.encounterTurns, Is.EqualTo(1));
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            Assert.That(p.v5.selection.holdChanges, Is.Zero);
            Assert.That(CompetitionV5Dice.CanHold(state, p), Is.True);
        }

        [Test] public void ReservationRejectsOnlyOptionalToggleAndKeepsLastTurnRollRerollExecuteSpace()
        {
            var state = Combat(); var p = state.participants[0];
            state.worldRound = 30; p.encounterTurns = 19; p.turn = 19; p.v5.selection.combatTurnSequence = 20;
            state.actionNumber = 2045;
            Assert.That(CompetitionV5Dice.RemainingRequiredCommands(state), Is.EqualTo(3));
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            Assert.That(CompetitionV5Dice.RemainingRequiredCommands(state), Is.EqualTo(2));
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_hold", p.dice[0].instanceId));
            CompetitionCommandRules.Apply(state, "combat_reroll", "");
            Assert.That(CompetitionV5Dice.RemainingRequiredCommands(state), Is.EqualTo(1));
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(state.actionNumber, Is.EqualTo(2048));
            Assert.That(state.finished, Is.True);
            Assert.That(p.encounterTurns, Is.EqualTo(20));
            Assert.That(p.eliminated, Is.True);
            Assert.That(p.v5.selection, Is.Null);
        }

        [Test] public void OptionalToggleConsumesOnlySlackBeyondAllThirtyRoundsOfRequiredProgress()
        {
            var initial = CompetitionRules.Start(Definition(), new Profile(), 123, "v5-reserve");
            Assert.That(CompetitionV5Dice.RemainingRequiredCommands(initial), Is.EqualTo(1860), "30 rounds times travel/choice plus 20 roll/reroll/execute turns.");
            var state = Selected("face.common.sword"); var p = state.participants[0];
            int required = CompetitionV5Dice.RemainingRequiredCommands(state);
            state.actionNumber = 2048 - required - 1;
            Assert.That(CompetitionV5Dice.CanHold(state, p), Is.True);
            CompetitionCommandRules.Apply(state, "combat_hold", p.dice[0].instanceId);
            Assert.That(CompetitionV5Dice.RemainingRequiredCommands(state), Is.LessThanOrEqualTo(required));
            // A held single die no longer needs its current reroll. Recompute the exact reservation boundary.
            state.actionNumber = 2048 - CompetitionV5Dice.RemainingRequiredCommands(state);
            RejectWithoutMutation(state, () => CompetitionCommandRules.Apply(state, "combat_release", p.dice[0].instanceId));
            CompetitionCommandRules.Apply(state, "combat_execute", "");
            Assert.That(p.encounterTurns, Is.EqualTo(1));
        }

        [Test] public void AiPolicyKeepsOnlyNonblankInSlotOrderAndDoesNotReadFutureRng()
        {
            var state = Selected("face.blank", "face.common.sword", "face.common.shield"); var p = state.participants[0];
            var method = typeof(CompetitionV5Dice).GetMethod("AiCommand", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            foreach (uint rng in new[] { 1u, 123u, uint.MaxValue })
            {
                var copy = LoopJson.Copy(p); copy.combatRng = rng; string before = LoopJson.Encode(copy);
                object[] args = { copy, null };
                Assert.That(method.Invoke(null, args), Is.EqualTo("combat_hold"));
                Assert.That(args[1], Is.EqualTo(p.dice[1].instanceId));
                Assert.That(LoopJson.Encode(copy), Is.EqualTo(before));
            }
            string[] expected = { "combat_hold", "combat_hold", "combat_reroll", "combat_execute" };
            foreach (string kind in expected)
            {
                object[] args = { p, null }; Assert.That(method.Invoke(null, args), Is.EqualTo(kind));
                CompetitionCommandRules.Apply(state, kind, (string)args[1]);
            }
            Assert.That(p.encounterTurns, Is.EqualTo(1));
        }

        [TestCase("wanderer", 123u)] [TestCase("scholar", 456u)] [TestCase("vanguard", 789u)]
        public void EntireHumanHistoryReplaysEveryParticipantRngTargetAndHashAcrossJsonResume(string hero, uint seed)
        {
            var profile = new Profile { growth = 2 };
            var state = CompetitionRules.Start(Definition(), profile, seed, "v5-replay", hero);
            var replay = LoopJson.Copy(state); var history = new List<CompetitionCommandEntry>(); var hashes = new List<string>();
            for (int i = 0; i < 2048 && !state.finished; ++i)
            {
                var command = NextCommand(state); history.Add(command);
                CompetitionCommandRules.Apply(state, command.kind, command.arg);
                hashes.Add(LoopJson.Hash(LoopJson.Encode(state)));
            }
            Assert.That(state.finished, Is.True); Assert.That(history.Count, Is.LessThanOrEqualTo(2048));
            Assert.That(state.participants.Skip(1).Any(p => p.turn > 0), Is.True, "Replay must include real AI combat.");
            for (int i = 0; i < history.Count; ++i)
            {
                if (i == history.Count / 2) replay = LoopJson.Decode<CompetitionState>(LoopJson.Encode(replay));
                Assert.That(CompetitionCommandRules.Target(replay), Is.EqualTo(history[i].targetId));
                CompetitionCommandRules.Apply(replay, history[i].kind, history[i].arg);
                Assert.That(LoopJson.Hash(LoopJson.Encode(replay)), Is.EqualTo(hashes[i]), "All participants and RNG at command " + i);
            }
            Assert.That(LoopJson.Encode(replay), Is.EqualTo(LoopJson.Encode(state)));
            TestContext.WriteLine(hero + " replay commands=" + history.Count + "; stateHash=" + hashes.Last() + "; commandHash=" + LoopJson.Hash(LoopJson.Encode(history)));
        }

        public static CompetitionState Combat(int dice = 1, string adventurer = "wanderer", uint seed = 123)
        {
            var state = CompetitionRules.Start(Definition(), new Profile(), seed, "v5-fixture", adventurer);
            var p = state.participants[0];
            p.completedLaps = dice - 1;
            for (int lap = 1; lap < dice; ++lap)
            {
                string id = state.matchId + ":" + p.id + ":lap:" + lap;
                p.claimed.Add(id); p.dice.Add(new DieSlot { instanceId = id + ":die", definitionId = "balanced" });
            }
            p.phase = CompetitionPhase.Combat;
            p.position = 2;
            p.offer = new EncounterOffer { id = "v5-fixture-offer", placeId = state.definition.world.places[2].id, stage = dice - 1, visit = 1,
                selectedChoiceId = "forest-wolf", choices = new[] { new EncounterChoice { id = "forest-wolf", kind = EncounterKind.Combat, available = true } } };
            p.enemy = new Enemy { hp = 1000, maxHp = 1000, attack = 1, intent = Intent.Attack };
            p.v4.enemyId = "forest-wolf";
            p.v5.selection = new V5SelectionState { phase = V5SelectionPhase.AwaitRoll, combatTurnSequence = p.encounterTurns + 1, rerollsLeft = 1 };
            CompetitionRules.Validate(state);
            return state;
        }

        public static CompetitionState Selected(params string[] faceIds)
        {
            var state = Combat(faceIds.Length); var p = state.participants[0];
            CompetitionCommandRules.Apply(state, "combat_roll", "");
            p.v5.selection.results = faceIds.Select((face, slot) => Result(p, slot,
                Array.FindIndex(CompetitionV5Content.Faces(state.definition, p.adventurerId, p.dice[slot].definitionId), f => f.id == face), face)).ToArray();
            CompetitionRules.Validate(state);
            return state;
        }

        static V5FaceResult Result(ParticipantState p, int slot, int faceIndex, string face) => new V5FaceResult {
            dieInstanceId = p.dice[slot].instanceId, slotIndex = slot, faceIndex = faceIndex, faceId = face };

        static void RejectWithoutMutation(CompetitionState state, TestDelegate action)
        {
            string before = LoopJson.Encode(state); var error = Assert.Catch(action);
            Assert.That(error, Is.TypeOf<ArgumentException>().Or.TypeOf<InvalidOperationException>());
            Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
        }

        public static CompetitionCommandEntry NextCommand(CompetitionState state)
        {
            var p = CompetitionRules.Human(state);
            if (p.phase != CompetitionPhase.Combat || !CompetitionV5Content.IsEnabled(state.definition)) return CompetitionRulesTests.NextCommand(state);
            var selection = p.v5.selection; string kind = "combat_execute", arg = "";
            if (selection.phase == V5SelectionPhase.AwaitRoll) kind = "combat_roll";
            else if (selection.holdChanges == 0 && CompetitionV5Dice.CanHold(state, p)) { kind = "combat_hold"; arg = p.dice[0].instanceId; }
            else if (selection.holdChanges == 1 && CompetitionV5Dice.CanHold(state, p)) { kind = "combat_release"; arg = p.dice[0].instanceId; }
            else if (selection.rerollsLeft > 0 && selection.results.Any(r => !r.held)) kind = "combat_reroll";
            return new CompetitionCommandEntry { kind = kind, arg = arg, targetId = CompetitionCommandRules.Target(state), sequence = state.actionNumber + 1 };
        }
    }
}
