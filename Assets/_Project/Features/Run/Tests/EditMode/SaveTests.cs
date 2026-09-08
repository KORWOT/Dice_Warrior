using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using FateDice.Editor;

namespace FateDice.Tests
{
    public sealed class SaveTests
    {
        string directory;
        LocalRunStore store;
        readonly HashSet<RunPhase> boundaries=new HashSet<RunPhase>();
        [SetUp] public void SetUp()
        {
            directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"FateDiceSaveTests",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store=new LocalRunStore(System.IO.Path.Combine(directory,"run.json"));
            boundaries.Clear();
        }
        [TearDown] public void TearDown()
        {
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        static GameConfigData Config(NodeType type,int threshold=1)
        {
            var data=PrototypeAuthoring.CreateDefaults();
            data.world.eventsToBoss=threshold;
            data.fate.nodeWeights=new float[5];data.fate.nodeWeights[(int)type]=1;
            data.growth.startingPower=100;data.growth.startingGold=100;
            foreach(var treasure in data.world.events.Where(x=>x.type==NodeType.Treasure))treasure.reward.dieId="ember";
            return data;
        }
        RunSession Start(NodeType type,int threshold=1)
        {
            var run=RunSession.New(Config(type,threshold),88,"fireball",Grade.Common);
            return RoundTrip(run);
        }
        RunSession RoundTrip(RunSession run)
        {
            var json=JsonUtility.ToJson(run.State);var rng=run.State.rngState;
            store.Save(run.State);Assert.That(store.Exists,Is.True);
            var loaded=store.Load();
            Assert.That(JsonUtility.ToJson(loaded),Is.EqualTo(json),"A checkpoint must retain the complete snapshot and pending choices.");
            Assert.That(loaded.rngState,Is.EqualTo(rng));
            Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(json),"Save must not mutate its input.");
            boundaries.Add(loaded.phase);
            return new RunSession(loaded){Checkpoint=store.Save};
        }
        void Step(ref RunSession run,Func<RunSession,bool> command)
        {
            var continuous=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));
            Assert.That(command(continuous),Is.True);
            Assert.That(command(run),Is.True);
            Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(JsonUtility.ToJson(continuous.State)),
                "Resuming may not change the next command, choices, rewards, or RNG.");
            run=RoundTrip(run);
        }
        void Enter(ref RunSession run)
        {
            var node=run.State.availableNodeIds[0];Step(ref run,r=>r.ChooseNode(node));
            Step(ref run,r=>r.Roll());var fate=run.State.cards[0].id;Step(ref run,r=>r.ChooseFate(fate));
        }
        void Fight(ref RunSession run)
        {
            for(var turn=0;turn<80&&run.State.phase==RunPhase.CombatRoll;turn++)
            {
                Step(ref run,r=>r.Roll());
                var current=run.State;var card=current.cards.OrderByDescending(x=>CombatRules.Evaluate(current,x).damage).First().id;
                Step(ref run,r=>r.ChooseAction(card));
                Assert.That(run.ChooseAction(card),Is.False,"The consumed offer cannot be replayed after loading.");
            }
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Reward));
        }
        void Claim(ref RunSession run,bool accept)
        {
            Step(ref run,r=>r.ClaimReward());
            Assert.That(run.ClaimReward(),Is.False,"The restored reward ID cannot be claimed twice.");
            for(var guard=0;guard<3&&run.State.phase==RunPhase.EquipmentChoice;guard++)
            {
                if(!string.IsNullOrEmpty(run.State.pendingEquipmentId))Step(ref run,r=>r.Equip(accept));
                else Step(ref run,r=>r.ReplaceDie(accept?2:-1));
            }
        }
        [TestCase(NodeType.Combat)] [TestCase(NodeType.Event)] [TestCase(NodeType.Treasure)]
        [TestCase(NodeType.Shop)] [TestCase(NodeType.Rest)]
        public void EveryCommandBoundaryResumesExactStateThroughNormalEventAndBoss(NodeType type)
        {
            var run=Start(type);Enter(ref run);
            if(type==NodeType.Combat){Fight(ref run);Claim(ref run,true);}
            else if(type==NodeType.Shop)
            {
                foreach(var product in new[]{"potion","blade","die"})
                {
                    var id=product;Step(ref run,r=>r.Buy(id));Claim(ref run,true);
                    Assert.That(run.State.phase,Is.EqualTo(RunPhase.Shop));
                    Assert.That(run.Buy(id),Is.False);
                }
                Step(ref run,r=>r.LeaveShop());
            }
            else{Step(ref run,r=>r.ResolveEncounter(false));Claim(ref run,true);}
            Assert.That(run.State.eventsResolved,Is.EqualTo(1));
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Map));
            var boss=run.State.availableNodeIds[0];Step(ref run,r=>r.ChooseNode(boss));
            Fight(ref run);Claim(ref run,true);
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Result));Assert.That(run.State.won,Is.True);
            Assert.That(run.State.eventsResolved,Is.EqualTo(1));
            Assert.That(boundaries,Does.Contain(RunPhase.ExplorationCards));
            Assert.That(boundaries,Does.Contain(RunPhase.CombatCards));
            Assert.That(boundaries,Does.Contain(RunPhase.Reward));
            Assert.That(boundaries,Does.Contain(RunPhase.Result));
            if(type==NodeType.Treasure||type==NodeType.Shop)Assert.That(boundaries,Does.Contain(RunPhase.EquipmentChoice));
        }
        [Test] public void DeclinedEquipmentAndDieRemainDeclinedAfterResume()
        {
            var run=Start(NodeType.Treasure);Enter(ref run);Step(ref run,r=>r.ResolveEncounter(false));
            Claim(ref run,false);
            Assert.That(run.State.equipmentIds.All(string.IsNullOrEmpty),Is.True);
            Assert.That(run.State.dieIds.All(x=>x=="plain"),Is.True);
            Assert.That(run.Equip(true),Is.False);Assert.That(run.ReplaceDie(0),Is.False);
            Assert.That(run.State.eventsResolved,Is.EqualTo(1));
        }
        [Test] public void DefeatPersistsWithoutReplayingAnEnemyTurnOrGrantingReward()
        {
            var data=Config(NodeType.Combat);data.growth.startingMaxHp=1;data.growth.startingPower=1;
            foreach(var enemy in data.combat.enemies)
            {
                enemy.maxHp=9999;
                foreach(var intent in enemy.intents)intent.weight=intent.kind==IntentKind.Attack?1:0;
            }
            var run=RoundTrip(RunSession.New(data,88,"fireball",Grade.Common));Enter(ref run);
            for(var turn=0;turn<80&&run.State.phase==RunPhase.CombatRoll;turn++)
            {
                Step(ref run,r=>r.Roll());
                var id=run.State.cards.OrderByDescending(x=>CombatRules.Evaluate(run.State,x).damage).First().id;
                Step(ref run,r=>r.ChooseAction(id));
            }
            Assert.That(run.State.phase,Is.EqualTo(RunPhase.Result));Assert.That(run.State.won,Is.False);
            Assert.That(run.State.hp,Is.Zero);Assert.That(run.State.eventsResolved,Is.Zero);
            Assert.That(run.ClaimReward(),Is.False);
        }
        [Test] public void StoredSnapshotSurvivesSourceChangesAndRepeatedLoadDoesNotReroll()
        {
            var source=Config(NodeType.Rest,10);var run=RunSession.New(source,88,"fireball",Grade.Rare);
            run=RoundTrip(run);var node=run.State.availableNodeIds[0];Step(ref run,r=>r.ChooseNode(node));Step(ref run,r=>r.Roll());
            var expected=JsonUtility.ToJson(run.State);source.combat.enemies[0].maxHp+=700;
            source.fate.exploration[0].weights=new float[]{0,0,0,0,1};source.world.eventsToBoss=2;
            for(var i=0;i<4;i++)Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(expected));
            Assert.That(store.Load().config.world.eventsToBoss,Is.EqualTo(10));
            var fresh=RunSession.New(source,88,"fireball",Grade.Rare);
            Assert.That(fresh.State.config.world.eventsToBoss,Is.EqualTo(2));
            Assert.That(fresh.State.config.combat.enemies[0].maxHp,Is.Not.EqualTo(run.State.config.combat.enemies[0].maxHp));
            Assert.That(run.Roll(),Is.False);
        }
        [TestCase("checksum")] [TestCase("envelope-schema")] [TestCase("state-schema")]
        [TestCase("config")] [TestCase("action")] [TestCase("rng")] [TestCase("phase")]
        [TestCase("node")] [TestCase("die")] [TestCase("pending")] [TestCase("phase-state")]
        public void InvalidSaveIsExplainedAndOriginalBytesSurviveLoadAndSave(string defect)
        {
            var state=RunSession.New(Config(NodeType.Rest),88,"fireball",Grade.Common).State;
            var envelope=Envelope(state);
            switch(defect)
            {
                case "checksum":envelope.checksum="00";break;
                case "envelope-schema":envelope.schema=999;break;
                case "state-schema":state.schema=999;break;
                case "config":state.config.world.eventsToBoss=0;break;
                case "action":state.actionIds[0]="missing-action";break;
                case "rng":state.rngState=0;break;
                case "phase":state.phase=(RunPhase)999;break;
                case "node":state.availableNodeIds[0]="missing-node";break;
                case "die":state.dieIds[0]="missing-die";break;
                case "pending":state.pendingEquipmentId="missing-equipment";break;
                case "phase-state":state.phase=RunPhase.ExplorationCards;break;
            }
            if(defect!="checksum"&&defect!="envelope-schema")envelope=Envelope(state);
            File.WriteAllText(store.Path,JsonUtility.ToJson(envelope),new UTF8Encoding(false));
            var original=File.ReadAllBytes(store.Path);
            var error=Assert.Throws<InvalidDataException>(()=>store.Load());
            Assert.That(error.Message,Is.Not.Empty);Assert.That(error.Message,Does.Contain(store.Path));
            CollectionAssert.AreEqual(original,File.ReadAllBytes(store.Path));
            var fresh=RunSession.New(Config(NodeType.Rest),88,"fireball",Grade.Common).State;
            Assert.Throws<InvalidDataException>(()=>store.Save(fresh));
            CollectionAssert.AreEqual(original,File.ReadAllBytes(store.Path),"A damaged slot must be explicitly archived before replacement.");
        }
        [TestCase("not json")] [TestCase("{}")] [TestCase("{\"schema\":1,\"payload\":null}")]
        public void MalformedEnvelopeIsPreserved(string content)
        {
            File.WriteAllText(store.Path,content);var bytes=File.ReadAllBytes(store.Path);
            Assert.Throws<InvalidDataException>(()=>store.Load());CollectionAssert.AreEqual(bytes,File.ReadAllBytes(store.Path));
        }
        [Test] public void InvalidCandidateCannotOverwriteExistingValidCheckpoint()
        {
            var run=Start(NodeType.Rest);var bytes=File.ReadAllBytes(store.Path);
            var broken=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State));broken.dieIds=new string[]{"plain"};
            Assert.Throws<InvalidDataException>(()=>store.Save(broken));
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(store.Path));
        }
        [Test] public void SaveTwiceAtomicallyReplacesAndLeavesNoTemporaryFiles()
        {
            Assert.That(store.Exists,Is.False);var run=Start(NodeType.Rest);
            var first=File.ReadAllText(store.Path);var id=run.State.availableNodeIds[0];Step(ref run,r=>r.ChooseNode(id));
            Assert.That(File.ReadAllText(store.Path),Is.Not.EqualTo(first));
            Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
            Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(JsonUtility.ToJson(run.State)));
        }
        [Test] public void ExplicitArchivePreservesDamagedSourceAndAllowsFreshRun()
        {
            File.WriteAllText(store.Path,"damaged save");var bytes=File.ReadAllBytes(store.Path);
            var archive=store.Archive();
            Assert.That(System.IO.Path.GetDirectoryName(archive),Is.EqualTo(directory));
            Assert.That(archive,Does.EndWith(".bak"));Assert.That(store.Exists,Is.False);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(archive));
            Start(NodeType.Rest);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(archive));
            var second=store.Archive();Assert.That(second,Is.Not.EqualTo(archive));
            Assert.That(File.Exists(second),Is.True);
        }
        [Test] public void AbsentSaveHasReadableFailureAndIsNotCreatedByLoad()
        {
            Assert.That(store.Exists,Is.False);
            var error=Assert.Throws<FileNotFoundException>(()=>store.Load());
            Assert.That(error.Message,Does.Contain(store.Path));Assert.That(store.Exists,Is.False);
            Assert.Throws<FileNotFoundException>(()=>store.Archive());
        }
        [TestCase("node-type")] [TestCase("node-children")] [TestCase("reward-values")]
        public void AbsentInlineDtosRejectUnexpectedValues(string defect)
        {
            var state=RunSession.New(Config(NodeType.Rest),88,"fireball",Grade.Common).State;
            state=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            if(defect=="node-type")state.selectedNode.type=NodeType.Boss;
            else if(defect=="node-children")state.selectedNode.childIds.Add(state.availableNodeIds[0]);
            else state.pendingReward.health=1;
            Assert.Throws<InvalidDataException>(()=>store.Save(state));
            Assert.That(store.Exists,Is.False);
        }
        [Test] public void LockedDestinationKeepsOriginalCheckpointOnActualReplaceFailure()
        {
            var run=Start(NodeType.Rest);var original=File.ReadAllBytes(store.Path);
            var candidate=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(run.State)));
            Assert.That(candidate.ChooseNode(candidate.State.availableNodeIds[0]),Is.True);
            using(var locked=new FileStream(store.Path,FileMode.Open,FileAccess.Read,FileShare.Read))
                Assert.Throws<IOException>(()=>store.Save(candidate.State));
            CollectionAssert.AreEqual(original,File.ReadAllBytes(store.Path));
            Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
            Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(JsonUtility.ToJson(run.State)));
        }

        [TestCase(false)] [TestCase(true)]
        public void EarnedRerollPersistsOneDieChoicesAndCostOrRollsBackWhenPriorSaveIsDamaged(bool combat)
        {
            var data=Config(combat?NodeType.Combat:NodeType.Event,10);
            foreach(var encounter in data.world.events)encounter.reward.rerollCharges=data.growth.rerollCost*2;
            var run=RoundTrip(RunSession.New(data,88,"fireball",Grade.Common));Enter(ref run);
            if(combat)Fight(ref run);else Step(ref run,r=>r.ResolveEncounter(false));
            Claim(ref run,false);
            Assert.That(run.State.rerollUnlocked,Is.True);
            var next=run.State.availableNodeIds[0];Step(ref run,r=>r.ChooseNode(next));Step(ref run,r=>r.Roll());
            if(combat)
            {
                var fate=run.State.cards[0].id;Step(ref run,r=>r.ChooseFate(fate));Step(ref run,r=>r.Roll());
            }
            Assert.That(run.State.phase,Is.EqualTo(combat?RunPhase.CombatCards:RunPhase.ExplorationCards));
            var before=JsonUtility.ToJson(run.State);var dice=(int[])run.State.dice.Clone();
            var oldCards=run.State.cards.Select(x=>x.id).ToArray();
            var charges=run.State.rerollCharges;var rng=run.State.rngState;
            var events=run.State.eventsResolved;var turns=run.State.combatTurns;
            File.WriteAllText(store.Path,"controlled damaged slot");var damaged=File.ReadAllBytes(store.Path);
            Assert.Throws<InvalidDataException>(()=>run.Reroll(2));
            Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(before),"Rejected persistence cannot consume a charge, RNG sample, face, or offer.");
            CollectionAssert.AreEqual(damaged,File.ReadAllBytes(store.Path));
            var archived=store.Archive();store.Save(run.State);
            CollectionAssert.AreEqual(damaged,File.ReadAllBytes(archived));
            Step(ref run,r=>r.Reroll(2));
            Assert.That(run.State.rerollCharges,Is.EqualTo(charges-data.growth.rerollCost));
            Assert.That(run.State.rngState,Is.Not.EqualTo(rng));
            for(var index=0;index<6;index++)if(index!=2)Assert.That(run.State.dice[index],Is.EqualTo(dice[index]),"Only the selected die may be rerolled.");
            Assert.That(run.State.cards.All(x=>!oldCards.Contains(x.id)),Is.True);
            Assert.That(run.State.eventsResolved,Is.EqualTo(events));Assert.That(run.State.combatTurns,Is.EqualTo(turns));
            var fixedResult=JsonUtility.ToJson(run.State);
            for(var load=0;load<3;load++)Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(fixedResult),"Repeated load cannot refund the resource or regenerate choices.");
            Step(ref run,r=>r.Reroll(2));Assert.That(run.State.rerollCharges,Is.Zero);
            var exhausted=JsonUtility.ToJson(run.State);
            Assert.That(run.Reroll(2),Is.False);Assert.That(JsonUtility.ToJson(run.State),Is.EqualTo(exhausted));
            Assert.That(JsonUtility.ToJson(store.Load()),Is.EqualTo(exhausted));
        }

        static LocalSaveEnvelope Envelope(RunState state)
        {
            var payload=JsonUtility.ToJson(state);string checksum;
            using(var sha=SHA256.Create())checksum=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
            return new LocalSaveEnvelope{schema=RunState.CurrentSchema,payload=payload,checksum=checksum};
        }
    }
}
