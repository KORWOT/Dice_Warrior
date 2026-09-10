using System;
using System.IO;
using System.Linq;
using FateDice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class RunBoundaryTests
    {
        sealed class ProbeStore : IRunStore
        {
            RunState saved;
            public int Attempts;
            public bool Fail;
            public Action<RunState> OnSave;
            public bool Exists => saved != null;
            public RunState Load() => Copy(saved);
            public void Save(RunState state)
            {
                Attempts++;
                OnSave?.Invoke(state);
                if(Fail) throw new IOException("Controlled boundary checkpoint failure.");
                saved=Copy(state);
            }
            public string Archive() { saved=null;return "isolated-probe.bak"; }
        }
        static string Json(RunState state)=>JsonUtility.ToJson(state);
        static RunState Copy(RunState state)=>state?.DeepCopy();
        static RunSession Journey(NodeType type=NodeType.Combat)
        {
            var config=PrototypeAuthoring.CreateDefaults();
            config.fate.nodeWeights=new float[5];config.fate.nodeWeights[(int)type]=1;
            return RunSession.New(config,33,"fireball",Grade.Common);
        }
        static RunSession At(string boundary)
        {
            var type=boundary=="buy"?NodeType.Shop:boundary=="reward"?NodeType.Event:NodeType.Combat;
            var run=Journey(type);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0])&&run.Roll(),Is.True);
            if(boundary=="reroll")
            {
                var prepared=run.State;prepared.rerollUnlocked=true;prepared.rerollCharges=3;
                return new RunSession(prepared);
            }
            Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);
            if(boundary=="action")Assert.That(run.Roll(),Is.True);
            if(boundary=="reward")Assert.That(run.ResolveEncounter(false),Is.True);
            return run;
        }
        static Func<RunSession,RunCommandToken,bool> Command(string boundary,RunState state)
        {
            switch(boundary)
            {
                case "reroll":return (run,token)=>run.Reroll(2,token);
                case "action":var id=state.cards[0].id;return (run,token)=>run.ChooseAction(id,token);
                case "reward":return (run,token)=>run.ClaimReward(token);
                case "buy":return (run,token)=>run.Buy("potion",token);
                default:throw new ArgumentException(boundary);
            }
        }
        [Test] public void ConstructorOwnsItsInputAndStateGetterReturnsDetachedTrees()
        {
            var source=Journey().State;var run=new RunSession(source);var before=Json(run.State);
            source.hp=1;source.config.growth.startingPower=777;source.nodes[0].childIds.Clear();
            Assert.That(Json(run.State),Is.EqualTo(before),"Constructor input must not alias the owned run.");
            var display=run.State;
            display.hp=2;display.config.dice.dice[0].weights[0]=777;display.actionIds.Clear();
            display.nodes[0].childIds.Clear();display.availableNodeIds.Clear();display.equipmentIds[0]="ember_blade";
            display.dieIds[0]="ember";display.selectedGrades[0]=999;
            Assert.That(Json(run.ReadSnapshot()),Is.EqualTo(before),"Display trees must not alias the owned run.");
            Assert.That(run.Phase,Is.EqualTo(run.ReadSnapshot().phase));
        }
        [Test] public void CardsRewardsRecordsAndConfigSnapshotsCannotWriteBackThroughDisplay()
        {
            var prepared=At("action").State;
            prepared.lastResult=new RunRecord{runId="prior-run",seed=77,selectedGrades=new[]{1,2,3,4,5},playedSeconds=14};
            var run=new RunSession(prepared);var before=Json(run.State);
            prepared.pendingReward.gold=900;prepared.cards[0].contentId="guard";prepared.lastResult.selectedGrades[0]=100;
            var view=run.ReadSnapshot();view.pendingReward.gold=123;view.cards[0].id="forged";
            view.selectedNode.childIds.Clear();view.lastResult.selectedGrades[1]=777;view.config.combat.actions[0].tags[0]="forged";
            Assert.That(Json(run.State),Is.EqualTo(before));
            Assert.That(Json(run.ReadSnapshot()),Is.EqualTo(before));
        }
        [Test] public void NewSavesOnceAndOwnsBothConfigAndPreviousResult()
        {
            var config=PrototypeAuthoring.CreateDefaults();var store=new ProbeStore();
            var previous=new RunRecord{runId="old-result",seed=99,won=true,events=10,selectedGrades=new[]{1,2,3,4,5}};
            var run=RunSession.New(config,77,"fireball",Grade.Common,store,previous);
            Assert.That(store.Attempts,Is.EqualTo(1));Assert.That(store.Exists,Is.True);
            Assert.That(Json(store.Load()),Is.EqualTo(Json(run.State)));
            var before=Json(run.State);config.dice.dice[0].weights[0]=999;previous.selectedGrades[0]=888;
            Assert.That(Json(run.State),Is.EqualTo(before));Assert.That(run.State.lastResult.runId,Is.EqualTo("old-result"));
        }
        [Test] public void NewSaveFailureCannotReturnAnUnpersistedSession()
        {
            var store=new ProbeStore{Fail=true};
            Assert.Throws<IOException>(()=>RunSession.New(PrototypeAuthoring.CreateDefaults(),33,"fireball",Grade.Common,store));
            Assert.That(store.Attempts,Is.EqualTo(1));Assert.That(store.Exists,Is.False);
        }
        [TestCase("reroll")] [TestCase("action")] [TestCase("reward")] [TestCase("buy")]
        public void SaveFailurePreservesEverythingAndRetryCommitsExactlyOnce(string boundary)
        {
            var store=new ProbeStore();var prepared=At(boundary).State;store.Save(prepared);
            var persisted=Json(store.Load());store.Attempts=0;
            var run=new RunSession(prepared,store);run.RecordElapsed(.75);
            var before=Json(run.State);var token=run.CaptureCommandToken();var command=Command(boundary,run.State);
            var oracle=new RunSession(run.State);Assert.That(command(oracle,oracle.CaptureCommandToken()),Is.True);
            store.Fail=true;
            Assert.Throws<IOException>(()=>command(run,token));
            Assert.That(store.Attempts,Is.EqualTo(1));Assert.That(Json(run.State),Is.EqualTo(before),"HP, costs, offers, IDs, RNG and pending time all remain unchanged.");
            Assert.That(Json(store.Load()),Is.EqualTo(persisted));
            store.Fail=false;Assert.That(command(run,token),Is.True);
            Assert.That(Json(run.State),Is.EqualTo(Json(oracle.State)));Assert.That(Json(store.Load()),Is.EqualTo(Json(oracle.State)));
            Assert.That(store.Attempts,Is.EqualTo(2));Assert.That(command(run,token),Is.False);
            Assert.That(store.Attempts,Is.EqualTo(2));Assert.That(Json(run.State),Is.EqualTo(Json(oracle.State)));
        }
        [Test] public void CheckpointOnlySeesCandidateCopyAndOldStateRemainsVisibleUntilCommit()
        {
            var store=new ProbeStore();var run=new RunSession(Journey().State,store);
            var before=Json(run.State);RunState retained=null;string during=null;int expectedSequence=run.State.sequence+1;
            store.OnSave=candidate=>
            {
                during=Json(run.ReadSnapshot());retained=candidate;
                Assert.That(candidate.sequence,Is.EqualTo(expectedSequence));
                candidate.hp=1;candidate.config.growth.startingPower=1000;candidate.nodes[0].childIds.Clear();
            };
            Assert.That(run.SetExplorationCap(Grade.Rare),Is.True);
            Assert.That(during,Is.EqualTo(before));Assert.That(run.State.hp,Is.EqualTo(90));
            Assert.That(run.State.config.growth.startingPower,Is.EqualTo(12));
            var committed=Json(run.State);retained.selectedGrades[0]=100;retained.actionIds.Clear();
            Assert.That(Json(run.State),Is.EqualTo(committed),"A retained save callback payload is not the live candidate.");
        }
        [Test] public void CheckpointReentryIsRejectedAndOneOuterCommandAdvancesOnce()
        {
            var run=Journey();int writes=0;bool nested=true,flush=true;var token=run.CaptureCommandToken();
            run.Checkpoint=candidate=>{writes++;nested=run.SetExplorationCap(Grade.Epic,token);flush=run.SaveCheckpoint();};
            Assert.That(run.SetExplorationCap(Grade.Rare,token),Is.True);
            Assert.That(nested,Is.False);Assert.That(flush,Is.False);Assert.That(writes,Is.EqualTo(1));
            Assert.That(run.State.sequence,Is.EqualTo(token.Sequence+1));Assert.That(run.State.explorationCap,Is.EqualTo(Grade.Rare));
        }
        [Test] public void TokenRejectsOtherRunsAndStaleInputEvenWhenThePhaseStillMatches()
        {
            var run=Journey();int writes=0;run.Checkpoint=_=>writes++;
            var first=run.CaptureCommandToken();Assert.That(first.RunId,Is.EqualTo(run.State.runId));
            Assert.That(run.SetExplorationCap(Grade.Rare,new RunCommandToken("other-run",first.Sequence)),Is.False);
            Assert.That(writes,Is.Zero);Assert.That(run.SetExplorationCap(Grade.Rare,first),Is.True);
            var committed=Json(run.State);
            Assert.That(run.SetExplorationCap(Grade.Legendary,first),Is.False,"Map remains active, so phase guards alone cannot reject this stale input.");
            Assert.That(Json(run.State),Is.EqualTo(committed));Assert.That(writes,Is.EqualTo(1));
            Assert.That(run.SetExplorationCap(Grade.Legendary,run.CaptureCommandToken()),Is.True);Assert.That(writes,Is.EqualTo(2));
        }
        [Test] public void PendingElapsedTimeIsBatchedSavedOnceAndDoesNotConsumeRngOrSequence()
        {
            var store=new ProbeStore();var initial=Journey().State;store.Save(initial);store.Attempts=0;
            var run=new RunSession(initial,store);var token=run.CaptureCommandToken();uint rng=run.State.rngState;
            for(int i=0;i<100;i++)run.RecordElapsed(.01);
            Assert.That(run.State.playedSeconds,Is.EqualTo(initial.playedSeconds+1).Within(.000001));
            Assert.That(store.Attempts,Is.Zero);Assert.That(store.Load().playedSeconds,Is.EqualTo(initial.playedSeconds));
            Assert.That(run.State.sequence,Is.EqualTo(token.Sequence));Assert.That(run.State.rngState,Is.EqualTo(rng));
            Assert.That(run.SaveCheckpoint(),Is.True);Assert.That(store.Attempts,Is.EqualTo(1));
            Assert.That(Json(run.State),Is.EqualTo(Json(store.Load())));
            Assert.That(run.State.sequence,Is.EqualTo(token.Sequence));Assert.That(run.State.rngState,Is.EqualTo(rng));
            var saved=Json(run.State);run.SaveCheckpoint();Assert.That(store.Attempts,Is.EqualTo(1),"No dirty duration means no repeated disk write.");
            Assert.That(Json(run.State),Is.EqualTo(saved));
            Assert.That(run.SetExplorationCap(Grade.Rare,token),Is.True,"Saving time cannot invalidate an otherwise current input.");
        }
        [Test] public void ElapsedCheckpointFailureRetainsPendingDurationForRetry()
        {
            var store=new ProbeStore();var initial=Journey().State;store.Save(initial);store.Attempts=0;
            var run=new RunSession(initial,store);run.RecordElapsed(2.5);var before=Json(run.State);
            store.Fail=true;Assert.Throws<IOException>(()=>run.SaveCheckpoint());
            Assert.That(Json(run.State),Is.EqualTo(before));Assert.That(store.Load().playedSeconds,Is.EqualTo(initial.playedSeconds));
            store.Fail=false;Assert.That(run.SaveCheckpoint(),Is.True);
            Assert.That(Json(run.State),Is.EqualTo(before));Assert.That(Json(store.Load()),Is.EqualTo(before));Assert.That(store.Attempts,Is.EqualTo(2));
        }
        [TestCase(-.01)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void ElapsedRejectsInvalidInputWithoutMutatingTheRun(double seconds)
        {
            var run=Journey();var before=Json(run.State);
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.RecordElapsed(seconds));Assert.That(Json(run.State),Is.EqualTo(before));
        }
        [Test] public void ResultDoesNotAccumulateAdditionalElapsedTime()
        {
            var prepared=At("action").State;prepared.hp=0;prepared.phase=RunPhase.Result;prepared.cards.Clear();
            var run=new RunSession(prepared);var before=Json(run.State);run.RecordElapsed(20);
            Assert.That(Json(run.State),Is.EqualTo(before));
        }
        [Test] public void CandidateValidationPrecedesStoreEvenWithAnInMemoryAdapter()
        {
            var source=Journey().State;source.sequence=int.MaxValue;var store=new ProbeStore();var run=new RunSession(source,store);
            var before=Json(run.State);
            Assert.Throws<RunStateValidationException>(()=>run.SetExplorationCap(Grade.Rare));
            Assert.That(store.Attempts,Is.Zero);Assert.That(Json(run.State),Is.EqualTo(before));
        }
        [Test] public void ConstructorRejectsAnInvalidStateWithoutCallingTheStore()
        {
            var invalid=Journey().State;invalid.hp=-1;var store=new ProbeStore();
            Assert.Throws<RunStateValidationException>(()=>new RunSession(invalid,store));Assert.That(store.Attempts,Is.Zero);
        }
        [Test] public void RealDiskResumePreservesOffersAndReplaysTheNextCommandExactly()
        {
            string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceBoundaryTests"))+Path.DirectorySeparatorChar;
            string directory=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var store=new LocalRunStore(Path.Combine(directory,"run.json"));var source=At("reroll").State;store.Save(source);
                var bytes=File.ReadAllBytes(store.Path);var resumed=new RunSession(store.Load(),store);var oracle=new RunSession(source);
                Assert.That(Json(resumed.State),Is.EqualTo(Json(source)));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(store.Path));
                Assert.That(resumed.Reroll(4,resumed.CaptureCommandToken())&&oracle.Reroll(4),Is.True);
                Assert.That(Json(resumed.State),Is.EqualTo(Json(oracle.State)));Assert.That(Json(store.Load()),Is.EqualTo(Json(oracle.State)));
            }
            finally { if(Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(directory))Directory.Delete(directory,true); }
        }
    }
}
