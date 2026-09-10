using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using FateDice.Editor;

namespace FateDice.Tests
{
    public sealed class RunTests
    {
        static RunState Battle()
        {
            var data=PrototypeAuthoring.CreateDefaults();
            return new RunState{runId="battle-fixture",config=data,rngState=33,initialSeed=33,phase=RunPhase.CombatCards,
                hp=90,baseMaxHp=90,basePower=12,baseGuard=10,level=1,activeEnemyId="road_bandit",enemyHp=26,activeIntentId="attack",
                activeEventId="combat_0",selectedNode=new NodeState{id="node-1",type=NodeType.Combat},pendingReward=new RewardDefinition{gold=8,xp=8},
                actionIds=data.combat.startingActionIds.Concat(new[]{"fireball"}).ToList(),dieIds=Enumerable.Repeat("plain",6).ToArray()};
        }
        // Pure combat oracles above intentionally use a minimal state. Session commands require a valid checkpoint graph.
        static RunState CommandBattle()
        {
            var state=Battle();state.phase=RunPhase.CombatRoll;
            state.nodes.Add(state.selectedNode);state.nextNodeId=2;
            state.pendingRewardId=state.selectedNode.id+":reward";
            return state;
        }
        static OfferedCard Card(string id="strike",Grade grade=Grade.Common)=>new OfferedCard{id="offer-1",contentId=id,grade=grade,type=NodeType.Combat};
        [Test] public void NewRunOwnsValidatedDeepSnapshotAndNoRerollPermission()
        {
            var data=PrototypeAuthoring.CreateDefaults();var run=RunSession.New(data,33,"fireball",Grade.Rare);
            data.growth.startingMaxHp=1;
            Assert.That(run.State.config.growth.startingMaxHp,Is.EqualTo(90));
            Assert.That(run.State.rerollUnlocked,Is.False);Assert.That(run.State.rerollCharges,Is.Zero);
            CollectionAssert.AreEquivalent(new[]{"strike","guard","heavy","fireball"},run.State.actionIds);
            Assert.Throws<ArgumentOutOfRangeException>(()=>RunSession.New(data,0,"fireball",Grade.Rare));
        }
        [Test] public void KillingBlowPreventsEnemyRetaliationAndDefersReward()
        {
            var state=Battle();state.enemyHp=1;uint rng=state.rngState;
            CombatRules.Resolve(state,Card());
            Assert.That(state.enemyHp,Is.Zero);Assert.That(state.hp,Is.EqualTo(90));
            Assert.That(state.phase,Is.EqualTo(RunPhase.Reward));Assert.That(state.rngState,Is.EqualTo(rng));
            Assert.That(state.eventsResolved,Is.Zero);Assert.That(state.gold,Is.Zero);
        }
        [Test] public void GuardOnlyCreatesShieldAndRemainingShieldExpiresAfterEnemyAction()
        {
            var state=Battle();CombatRules.Resolve(state,Card("guard"));
            Assert.That(state.hp,Is.EqualTo(90));Assert.That(state.shield,Is.Zero);Assert.That(state.enemyHp,Is.EqualTo(26));
            state=Battle();state.baseGuard=999;CombatRules.Resolve(state,Card());
            Assert.That(state.hp,Is.EqualTo(84),"Guard must not passively mitigate six incoming damage.");
        }
        [Test] public void EnemyDefenseSurvivesUntilNextPlayerActionThenExpires()
        {
            var state=Battle();state.activeIntentId="defend";
            CombatRules.Resolve(state,Card("guard"));
            Assert.That(state.enemyShield,Is.EqualTo(8));Assert.That(state.shield,Is.Zero);
            state.activeIntentId="attack";state.phase=RunPhase.CombatCards;
            CombatRules.Resolve(state,Card("guard"));Assert.That(state.enemyShield,Is.Zero);
        }
        [Test] public void NonlethalActionThenEnemyAttackCanEndRunInDefeat()
        {
            var state=Battle();state.hp=1;CombatRules.Resolve(state,Card());
            Assert.That(state.enemyHp,Is.EqualTo(13));Assert.That(state.hp,Is.Zero);
            Assert.That(state.phase,Is.EqualTo(RunPhase.Result));Assert.That(state.won,Is.False);
            Assert.That(state.combatTurns,Is.EqualTo(1));Assert.That(state.eventsResolved,Is.Zero);
        }
        [Test] public void EmptyGradeUsesNumericRatioWithoutChangingOriginalCard()
        {
            var state=Battle();var json=JsonUtility.ToJson(state.config);
            var effect=CombatRules.Evaluate(state,Card("strike",Grade.Legendary));
            Assert.That(effect.damage,Is.EqualTo(32));Assert.That(effect.block,Is.Zero);
            Assert.That(JsonUtility.ToJson(state.config),Is.EqualTo(json));
        }
        [Test] public void RollOffersThreeUsableActionsAndDuplicateInputDoesNothing()
        {
            var state=CommandBattle();var run=new RunSession(state);int checkpoints=0;
            run.Checkpoint=_=>checkpoints++;
            Assert.That(run.Roll(),Is.True);Assert.That(run.State.dice.Length,Is.EqualTo(6));Assert.That(run.State.cards.Count,Is.EqualTo(3));
            var id=run.State.cards[0].id;
            Assert.That(run.ChooseAction(id),Is.True);var after=JsonUtility.ToJson(run.State);
            Assert.That(run.ChooseAction(id),Is.False);Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(after));
            Assert.That(checkpoints,Is.EqualTo(2));Assert.That(run.State.eventsResolved,Is.Zero);
        }
        [Test] public void EveryOfferedCardIsExecutableWithoutResources()
        {
            var source=CommandBattle();var initial=new RunSession(source);initial.Roll();
            foreach(var offer in initial.State.cards)
            {
                var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(initial.State));
                var run=new RunSession(copy);Assert.That(run.ChooseAction(offer.id),Is.True,offer.contentId);
            }
        }
        [Test] public void FailedCheckpointPreservesPriorStateAndRandomStream()
        {
            var state=CommandBattle();var run=new RunSession(state);var before=JsonUtility.ToJson(run.State);
            run.Checkpoint=_=>throw new IOException("controlled disk failure");
            Assert.Throws<IOException>(()=>run.Roll());Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(before));
        }

        [Test] public void RestTrainingPreviewMatchesCapAdjustedCommittedReward()
        {
            var run=Journey(NodeType.Rest);EnterGuaranteedEvent(run);
            var preview=run.RestTrainingReward();
            Assert.That(preview.xp,Is.EqualTo(16),"Common cap must show the actual rounded training XP.");
            run.ResolveEncounter(true);
            Assert.That(JsonUtility.ToJson(run.State.pendingReward),Is.EqualTo(JsonUtility.ToJson(preview)));
            Assert.That(run.State.config.world.restTraining.xp,Is.EqualTo(12),"Config source must remain unchanged.");
        }

        [Test] public void GrowthConsumesConfiguredXpStepsAndHealsOnlyMaximumIncrease()
        {
            var state=Battle();state.hp=40;
            GrowthRules.Grant(state,new RewardDefinition{xp=35});
            Assert.That(state.level,Is.EqualTo(3));Assert.That(state.xp,Is.EqualTo(5));
            Assert.That(state.baseMaxHp,Is.EqualTo(106));Assert.That(state.basePower,Is.EqualTo(16));
            Assert.That(state.baseGuard,Is.EqualTo(12));Assert.That(state.hp,Is.EqualTo(56));
            GrowthRules.Equip(state,"traveler_charm");
            Assert.That(GrowthRules.Stats(state).maxHp,Is.EqualTo(111));Assert.That(state.hp,Is.EqualTo(61));
        }
        [Test] public void EquipmentReplacementRecalculatesStatsAndDeclineKeepsCurrentItem()
        {
            var state=Battle();var extra=new EquipmentDefinition{id="plain_blade",label="Plain blade",slot=EquipmentSlot.Weapon,
                power=1,tags=new[]{"physical"},modifiers=new TagModifier[0]};
            state.config.growth.equipment=state.config.growth.equipment.Concat(new[]{extra}).ToArray();
            GrowthRules.Equip(state,"ember_blade");Assert.That(GrowthRules.Stats(state).power,Is.EqualTo(15));
            GrowthRules.Equip(state,"plain_blade");Assert.That(GrowthRules.Stats(state).power,Is.EqualTo(13));
            Assert.That(state.basePower,Is.EqualTo(12));
            var prepared=Journey(NodeType.Treasure).State;prepared.equipmentIds[0]="plain_blade";
            prepared.config.growth.equipment=state.config.growth.equipment;
            var run=new RunSession(prepared);
            EnterGuaranteedEvent(run);run.ResolveEncounter(false);run.ClaimReward();
            Assert.That(run.Equip(false),Is.True);Assert.That(run.State.equipmentIds[0],Is.EqualTo("plain_blade"));
            Assert.That(GrowthRules.Stats(run.State).power,Is.EqualTo(13));
        }
        [TestCase(MatchMode.Any,"fireball",1.5)] [TestCase(MatchMode.All,"fireball",1.5)]
        [TestCase(MatchMode.None,"fireball",1.0)] [TestCase(MatchMode.Any,"guard",1.0)]
        [TestCase(MatchMode.All,"guard",1.0)] [TestCase(MatchMode.None,"guard",1.5)]
        public void ExplicitAnyAllNoneMatchCardTags(MatchMode match,string action,double expected)
        {
            var state=Battle();state.equipmentIds[0]="ember_blade";
            state.config.Equipment("ember_blade").modifiers=new[]{new TagModifier{match=match,source=TagSource.Card,
                value=ModifiedValue.Damage,requiredTags=new[]{"fire","magic"},bonus=.5f}};
            Assert.That(GrowthRules.Multiplier(state,state.config.Action(action),ModifiedValue.Damage),Is.EqualTo(expected).Within(.00001));
            Assert.That(GrowthRules.Multiplier(state,state.config.Action(action),ModifiedValue.Block),Is.EqualTo(1));
        }
        [Test] public void EquipmentAndCardTagSourcesStaySeparate()
        {
            var state=Battle();state.equipmentIds[0]="ember_blade";
            var modifier=new TagModifier{match=MatchMode.All,source=TagSource.Equipment,value=ModifiedValue.Damage,requiredTags=new[]{"fire"},bonus=.5f};
            state.config.Equipment("ember_blade").modifiers=new[]{modifier};
            Assert.That(CombatRules.Evaluate(state,Card()).damage,Is.EqualTo(25));
            modifier.source=TagSource.Card;
            Assert.That(CombatRules.Evaluate(state,Card()).damage,Is.EqualTo(17),"Equipment fire does not transfer to Strike.");
        }
        [Test] public void ExplicitModifiersChangeOnlyTheirDeclaredNumericEffect()
        {
            var state=Battle();GrowthRules.Equip(state,"ember_blade");
            Assert.That(CombatRules.Evaluate(state,Card()).damage,Is.EqualTo(17));
            Assert.That(CombatRules.Evaluate(state,Card("fireball",Grade.Rare)).damage,Is.EqualTo(43));
            Assert.That(CombatRules.Evaluate(state,Card("guard")).block,Is.EqualTo(15));
            GrowthRules.Equip(state,"guard_plate");
            Assert.That(CombatRules.Evaluate(state,Card("guard")).block,Is.EqualTo(23));
            GrowthRules.Equip(state,"traveler_charm");
            Assert.That(CombatRules.Evaluate(state,Card("guard")).block,Is.EqualTo(25));
        }
        [Test] public void ActualEventRewardsGrantLevelsAndRerollPermission()
        {
            var run=Journey(NodeType.Event);
            for(var i=0;i<2;i++){EnterGuaranteedEvent(run);run.ResolveEncounter(false);ClaimAndDeclineExtras(run);}
            Assert.That(run.State.level,Is.EqualTo(2));Assert.That(run.State.xp,Is.EqualTo(10));
            Assert.That(run.State.baseMaxHp,Is.EqualTo(98));Assert.That(run.State.rerollUnlocked,Is.True);
            Assert.That(run.State.rerollCharges,Is.EqualTo(4));
        }
        [Test] public void RerollRequiresEarnedPermissionResourcesAndCurrentOffer()
        {
            var run=Journey(NodeType.Event);run.ChooseNode(run.State.availableNodeIds[0]);run.Roll();
            var before=JsonUtility.ToJson(run.State);
            Assert.That(run.Reroll(2),Is.False);Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(before));
            var prepared=run.State;prepared.rerollUnlocked=true;run=new RunSession(prepared);
            Assert.That(run.Reroll(2),Is.False);
            prepared=run.State;prepared.rerollCharges=1;run=new RunSession(prepared);
            Assert.That(run.Reroll(-1),Is.False);Assert.That(run.Reroll(6),Is.False);
            prepared=Journey(NodeType.Event).State;prepared.rerollUnlocked=true;prepared.rerollCharges=1;
            run=new RunSession(prepared);Assert.That(run.Reroll(2),Is.False);
        }
        [Test] public void EarnedRerollReplacesOnlyOneFaceAndCommitsChargeChoicesAndRngTogether()
        {
            var run=Journey(NodeType.Event);EnterGuaranteedEvent(run);run.ResolveEncounter(false);run.ClaimReward();
            run.ChooseNode(run.State.availableNodeIds[0]);run.Roll();
            var before=(int[])run.State.dice.Clone();var rng=run.State.rngState;var oldIds=run.State.cards.Select(x=>x.id).ToArray();
            var progress=run.State.eventsResolved;int saves=0;run.Checkpoint=_=>saves++;
            Assert.That(run.Reroll(2),Is.True);
            for(var i=0;i<6;i++)if(i!=2)Assert.That(run.State.dice[i],Is.EqualTo(before[i]));
            Assert.That(run.State.rngState,Is.Not.EqualTo(rng));Assert.That(run.State.rerollCharges,Is.EqualTo(1));
            Assert.That(run.State.cards.Any(x=>oldIds.Contains(x.id)),Is.False);
            Assert.That(run.State.eventsResolved,Is.EqualTo(progress));Assert.That(saves,Is.EqualTo(1));
            var resumed=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));
            Assert.That(resumed.Reroll(2),Is.True);Assert.That(resumed.State.rerollCharges,Is.Zero);
            before=(int[])resumed.State.dice.Clone();rng=resumed.State.rngState;
            Assert.That(resumed.Reroll(2),Is.False);CollectionAssert.AreEqual(before,resumed.State.dice);Assert.That(resumed.State.rngState,Is.EqualTo(rng));
        }
        [Test] public void FailedRerollCheckpointDoesNotConsumeTheEarnedResource()
        {
            var run=Journey(NodeType.Event);run.ChooseNode(run.State.availableNodeIds[0]);run.Roll();
            var prepared=run.State;prepared.rerollUnlocked=true;prepared.rerollCharges=1;run=new RunSession(prepared);var before=JsonUtility.ToJson(run.State);
            run.Checkpoint=_=>throw new IOException("controlled reroll save failure");
            Assert.Throws<IOException>(()=>run.Reroll(4));Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(before));
        }
        [Test] public void EarnedDieReplacementUsesTheSelectedWeightedDieOnTheNextRoll()
        {
            var data=PrototypeAuthoring.CreateDefaults();data.fate.nodeWeights=new float[]{0,0,1,0,0};
            foreach(var e in data.world.events.Where(x=>x.type==NodeType.Treasure))e.reward.dieId="ember";
            data.Die("ember").values=Enumerable.Repeat(6,6).ToArray();
            var run=RunSession.New(data,88,"fireball",Grade.Common);EnterGuaranteedEvent(run);run.ResolveEncounter(false);run.ClaimReward();run.Equip(false);
            Assert.That(run.ReplaceDie(4),Is.True);
            for(var i=0;i<6;i++)Assert.That(run.State.dieIds[i],Is.EqualTo(i==4?"ember":"plain"));
            run.ChooseNode(run.State.availableNodeIds[0]);run.Roll();Assert.That(run.State.dice[4],Is.EqualTo(6));
        }

        [Test] public void PartialTagIntersectionSeparatesAnyAllAndNone()
        {
            var state=Battle();state.equipmentIds[0]="ember_blade";
            var modifier=new TagModifier{match=MatchMode.Any,source=TagSource.Card,value=ModifiedValue.Damage,
                requiredTags=new[]{"fire","physical"},bonus=.25f};
            state.config.Equipment("ember_blade").modifiers=new[]{modifier};
            var fireball=state.config.Action("fireball");
            Assert.That(GrowthRules.Multiplier(state,fireball,ModifiedValue.Damage),Is.EqualTo(1.25));
            modifier.match=MatchMode.All;Assert.That(GrowthRules.Multiplier(state,fireball,ModifiedValue.Damage),Is.EqualTo(1));
            modifier.match=MatchMode.None;Assert.That(GrowthRules.Multiplier(state,fireball,ModifiedValue.Damage),Is.EqualTo(1));
        }


        static RunSession MergingJourney()
        {
            var data=PrototypeAuthoring.CreateDefaults();
            data.world.branchCount=2;data.world.previewDepth=1;
            data.fate.nodeWeights=new float[]{0,0,0,0,1};
            var prepared=RunSession.New(data,33,"fireball",Grade.Common).State;
            prepared.nodes=new System.Collections.Generic.List<NodeState>{
                new NodeState{id="A",type=NodeType.Rest,childIds=new System.Collections.Generic.List<string>{"C","D"}},
                new NodeState{id="B",type=NodeType.Rest,childIds=new System.Collections.Generic.List<string>{"C","E"}},
                new NodeState{id="C",type=NodeType.Rest},new NodeState{id="D",type=NodeType.Rest},new NodeState{id="E",type=NodeType.Rest}};
            prepared.availableNodeIds=new System.Collections.Generic.List<string>{"A","B"};
            return new RunSession(prepared);
        }
        static System.Collections.Generic.List<NodeState> NodeHistory(RunState state)
        {
            var field=typeof(RunState).GetField("nodeHistory");
            Assert.That(field,Is.Not.Null,"Pruned path records need a persisted nodeHistory.");
            return (System.Collections.Generic.List<NodeState>)field.GetValue(state);
        }
        [Test] public void DiscardedNodesAreArchivedWhileTheSharedFutureAndRngSurvive()
        {
            var run=MergingJourney();var rng=run.State.rngState;var sequence=run.State.sequence;var nextId=run.State.nextNodeId;
            Assert.That(run.ChooseNode("A"),Is.True);
            CollectionAssert.AreEquivalent(new[]{"A","C","D"},run.State.nodes.Select(x=>x.id));
            CollectionAssert.AreEquivalent(new[]{"B","E"},NodeHistory(run.State).Select(x=>x.id));
            CollectionAssert.AreEqual(new[]{"C","E"},NodeHistory(run.State).Single(x=>x.id=="B").childIds);
            Assert.That(run.State.availableNodeIds,Is.Empty,"Reachable future nodes are not yet selectable.");
            Assert.That(run.State.rngState,Is.EqualTo(rng));Assert.That(run.State.eventsResolved,Is.Zero);
            Assert.That(run.State.sequence,Is.EqualTo(sequence+1));Assert.That(run.State.nextNodeId,Is.EqualTo(nextId));
            var exported=run.State;ExplorationRules.PruneTo(exported,new[]{"A"});
            Assert.That(NodeHistory(exported).Count,Is.EqualTo(2),"Repeated visual/path cleanup must not duplicate history.");
        }
        [Test] public void MergedGraphAndArchivedCrossReferencesRoundTripAtEveryPathBoundary()
        {
            var directory=Path.Combine(Path.GetTempPath(),"FateDicePathTests",Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);var store=new LocalRunStore(Path.Combine(directory,"run.json"));
                var run=MergingJourney();store.Save(run.State);run=new RunSession(store.Load()){Checkpoint=store.Save};
                Assert.That(run.ChooseNode("A"),Is.True);
                Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(JsonUtility.ToJson(run.State)));
                run.Roll();run.ChooseFate(run.State.cards[0].id);run.ResolveEncounter(false);run.ClaimReward();
                CollectionAssert.AreEquivalent(new[]{"C","D"},run.State.nodes.Select(x=>x.id));
                CollectionAssert.AreEquivalent(new[]{"B","E","A"},NodeHistory(run.State).Select(x=>x.id));
                CollectionAssert.AreEqual(new[]{"C","D"},NodeHistory(run.State).Single(x=>x.id=="A").childIds);
                CollectionAssert.AreEquivalent(new[]{"C","D"},run.State.availableNodeIds);
                Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(JsonUtility.ToJson(run.State)));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test] public void ARealCycleStillCannotBeSavedAsAMergedGraph()
        {
            var invalid=MergingJourney().State;invalid.nodes.Single(x=>x.id=="C").childIds.Add("A");
            var path=Path.Combine(Path.GetTempPath(),"FateDicePathTests",Guid.NewGuid().ToString("N"),"cycle.json");
            Assert.Throws<InvalidDataException>(()=>new LocalRunStore(path).Save(invalid));
            Assert.That(File.Exists(path),Is.False);
        }
        [Test] public void ArchivedIdsCannotCollideOrReferenceMissingNodes()
        {
            var run=MergingJourney();run.ChooseNode("A");
            var invalid=run.State;var history=NodeHistory(invalid);history.Add(new NodeState{id="A",type=NodeType.Rest});
            var path=Path.Combine(Path.GetTempPath(),"FateDicePathTests",Guid.NewGuid().ToString("N"),"bad.json");
            Assert.Throws<InvalidDataException>(()=>new LocalRunStore(path).Save(invalid));
            history.RemoveAt(history.Count-1);history[0].childIds.Add("missing");
            Assert.Throws<InvalidDataException>(()=>new LocalRunStore(path).Save(invalid));
            Assert.That(File.Exists(path),Is.False);
        }
        [Test] public void SchemaOneWithoutHistoryCanContinueAndSave()
        {
            var directory=Path.Combine(Path.GetTempPath(),"FateDicePathTests",Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);var path=Path.Combine(directory,"legacy.json");
                var initial=RunSession.New(PrototypeAuthoring.CreateDefaults(),33,"fireball",Grade.Common);
                var payload=JsonUtility.ToJson(initial.State).Replace("\"nodeHistory\":[],","");
                Assert.That(payload,Does.Not.Contain("\"nodeHistory\""));
                string checksum;using(var sha=System.Security.Cryptography.SHA256.Create())
                    checksum=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
                File.WriteAllText(path,JsonUtility.ToJson(new LocalSaveEnvelope{schema=1,payload=payload,checksum=checksum}));
                var store=new LocalRunStore(path);var run=new RunSession(store.Load()){Checkpoint=store.Save};
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]),Is.True);
                Assert.That(NodeHistory(store.Load()).Count,Is.GreaterThan(0));
                Assert.That(store.Load().schema,Is.EqualTo(1));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        static RunSession Journey(NodeType type,int threshold=10)
        {
            var data=PrototypeAuthoring.CreateDefaults();data.world.eventsToBoss=threshold;
            data.fate.nodeWeights=new float[5];data.fate.nodeWeights[(int)type]=1;
            return RunSession.New(data,88,"fireball",Grade.Common);
        }
        static void EnterGuaranteedEvent(RunSession run)
        {
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]),Is.True);
            Assert.That(run.Roll(),Is.True);
            Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);
        }
        static void ClaimAndDeclineExtras(RunSession run)
        {
            Assert.That(run.ClaimReward(),Is.True);
            for(var i=0;i<3&&run.State.phase==RunPhase.EquipmentChoice;i++)
            {
                if(!string.IsNullOrEmpty(run.State.pendingEquipmentId))Assert.That(run.Equip(false),Is.True);
                else Assert.That(run.ReplaceDie(-1),Is.True);
            }
        }
        [Test] public void BranchMapPreviewsTwoLevelsAndPrunesUnchosenPaths()
        {
            var run=Journey(NodeType.Rest);
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Map));
            Assert.That(run.State.availableNodeIds.Count,Is.EqualTo(3));
            Assert.That(run.State.nodes.Count,Is.EqualTo(12));
            var chosen=run.State.nodes.Single(x=>x.id==run.State.availableNodeIds[0]);
            var discarded=run.State.availableNodeIds.Skip(1).ToArray();
            Assert.That(run.ChooseNode(chosen.id),Is.True);
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.ExplorationRoll));
            Assert.That(run.State.nodes.Any(x=>discarded.Contains(x.id)),Is.False);
            CollectionAssert.AreEqual(chosen.childIds,run.State.selectedNode.childIds);
            Assert.That(run.ChooseNode(chosen.id),Is.False);
        }
        [TestCase(NodeType.Combat)] [TestCase(NodeType.Event)] [TestCase(NodeType.Treasure)]
        [TestCase(NodeType.Shop)] [TestCase(NodeType.Rest)]
        public void FiveEventTypesHaveRealEffectsAndAdvanceExactlyOnce(NodeType type)
        {
            var run=Journey(type);var beforeGold=run.State.gold;EnterGuaranteedEvent(run);
            if(type==NodeType.Combat)
            {
                for(var turn=0;turn<80&&run.State.phase==RunPhase.CombatRoll;turn++)
                {
                    run.Roll();var card=run.State.cards.OrderByDescending(x=>CombatRules.Evaluate(run.State,x).damage).First();
                    run.ChooseAction(card.id);
                }
                Assert.That(run.State.phase,Is.EqualTo(RunPhase.Reward));ClaimAndDeclineExtras(run);
            }
            else if(type==NodeType.Shop)
            {
                Assert.That(run.Buy("potion"),Is.True);ClaimAndDeclineExtras(run);
                Assert.That(run.State.phase,Is.EqualTo(RunPhase.Shop));Assert.That(run.LeaveShop(),Is.True);
                Assert.That(run.State.gold,Is.LessThan(beforeGold));
            }
            else
            {
                Assert.That(run.ResolveEncounter(false),Is.True);ClaimAndDeclineExtras(run);
                if(type==NodeType.Event)Assert.That(run.State.hp,Is.LessThan(90));
            }
            Assert.That(run.State.eventsResolved,Is.EqualTo(1));Assert.That(run.State.phase,Is.EqualTo(RunPhase.Map));
            Assert.That(run.ClaimReward(),Is.False);Assert.That(run.State.eventsResolved,Is.EqualTo(1));
            if(type!=NodeType.Shop)Assert.That(run.State.xp,Is.GreaterThan(0));
        }
        [Test] public void ThresholdTransformsNextChoicesOnlyAndBossEndsSingleSection()
        {
            var data=PrototypeAuthoring.CreateDefaults();data.world.eventsToBoss=2;data.growth.startingPower=100;
            data.fate.nodeWeights=new float[]{0,0,0,0,1};var run=RunSession.New(data,88,"fireball",Grade.Common);
            for(var i=0;i<2;i++){EnterGuaranteedEvent(run);run.ResolveEncounter(false);Assert.That(run.State.eventsResolved,Is.EqualTo(i));ClaimAndDeclineExtras(run);}
            Assert.That(run.State.nodes.Where(x=>run.State.availableNodeIds.Contains(x.id)).All(x=>x.type==NodeType.Boss),Is.True);
            Assert.That(run.State.eventsResolved,Is.EqualTo(2));
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]),Is.True);
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.CombatRoll));Assert.That(run.State.boss,Is.True);
            for(var i=0;i<20&&run.State.phase==RunPhase.CombatRoll;i++){run.Roll();run.ChooseAction(run.State.cards.OrderByDescending(x=>CombatRules.Evaluate(run.State,x).damage).First().id);}
            ClaimAndDeclineExtras(run);
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Result));Assert.That(run.State.won,Is.True);
            Assert.That(run.State.eventsResolved,Is.EqualTo(2),"Boss is not an extra normal event.");
        }
        [Test] public void RewardClaimAndEquipmentReentryCannotDuplicateRewardsOrProgress()
        {
            var run=Journey(NodeType.Treasure);EnterGuaranteedEvent(run);run.ResolveEncounter(false);
            var before=run.State.gold;var expected=run.State.pendingReward.gold;run.ClaimReward();
            Assert.That(run.State.gold,Is.EqualTo(before+expected));Assert.That(run.State.eventsResolved,Is.Zero);
            var restored=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));
            Assert.That(restored.ClaimReward(),Is.False);restored.Equip(false);
            Assert.That(restored.State.gold,Is.EqualTo(before+expected));Assert.That(restored.State.eventsResolved,Is.EqualTo(1));
            Assert.That(restored.Equip(false),Is.False);Assert.That(restored.State.eventsResolved,Is.EqualTo(1));
        }
    }
}
