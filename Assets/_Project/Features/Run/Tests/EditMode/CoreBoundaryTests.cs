using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using FateDice.Editor;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class CoreBoundaryTests
    {
        [Test] public void ActualCoreAssemblyOwnsRulesAndCommandsWithoutEngineReferences()
        {
            var assembly=AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a=>a.GetName().Name=="FateDice.Core");
            Assert.That(assembly,Is.Not.Null,"Rules and application must compile in the actual FateDice.Core assembly.");
            foreach(var name in new[]{"RunApplication","CoreRunState","RunRulesCatalog","RunStateValidator","DiceRules","FateCardRules","CombatRules","GrowthRules","ExplorationRules"})
                Assert.That(assembly.GetType("FateDice."+name),Is.Not.Null,name);
            Assert.That(assembly.GetReferencedAssemblies().Select(a=>a.Name),Has.None.Matches<string>(n=>n.StartsWith("Unity",StringComparison.Ordinal)||n.StartsWith("FateDice.",StringComparison.Ordinal)||n.Contains("Firebase")));
            var path=Path.Combine(Application.dataPath,"_Project/Features/Run/Domain/FateDice.Core.asmdef");
            var definition=JObject.Parse(File.ReadAllText(path));
            Assert.That((bool)definition["noEngineReferences"],Is.True);
            Assert.That((bool)definition["overrideReferences"],Is.True);
            Assert.That((JArray)definition["references"],Is.Empty);
            Assert.That((JArray)definition["precompiledReferences"],Is.Empty);
        }

        static RunState Fixture() => RunSession.New(PrototypeAuthoring.CreateDefaults(),33,"fireball",Grade.Common).ReadSnapshot();
        static void AssertIndependentTree(object source,object copy,string path)
        {
            if(source==null){Assert.That(copy,Is.Null,path);return;}
            var type=source.GetType();
            Assert.That(copy,Is.Not.Null,path);
            Assert.That(copy.GetType(),Is.EqualTo(type),path);
            if(type.IsValueType||source is string){Assert.That(copy,Is.EqualTo(source),path);return;}
            Assert.That(ReferenceEquals(source,copy),Is.False,path+" aliases a mutable source object");
            if(source is IList sourceList)
            {
                var copyList=(IList)copy;
                Assert.That(copyList.Count,Is.EqualTo(sourceList.Count),path);
                for(int i=0;i<sourceList.Count;i++)AssertIndependentTree(sourceList[i],copyList[i],path+"["+i+"]");
                return;
            }
            foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))
                AssertIndependentTree(field.GetValue(source),field.GetValue(copy),path+"."+field.Name);
        }
        [Test] public void ExplicitCopiesKeepAllValuesAndSeparateEveryMutableObject()
        {
            var source=Fixture();
            source.lastResult=new RunRecord{runId="older",seed=27,selectedGrades=new[]{1,2,3,4,5},playedSeconds=4.5};
            source.pendingReward=new RewardDefinition{gold=7,equipmentId="ember_blade",dieId="ember"};
            source.cards.Add(new OfferedCard{id="fixture-offer",contentId="strike",grade=Grade.Epic});
            source.dice=new[]{1,2,3,4,5,6};
            AssertIndependentTree(source,source.DeepCopy(),"runtime");
            var core=source.ToCore();
            Assert.That(core.config.GetType(),Is.EqualTo(typeof(RunRulesCatalog)));
            Assert.That(core.config.GetType().GetField("presentation"),Is.Null);
            AssertIndependentTree(core,core.DeepCopy(),"core");
            var copiedCatalog=core.config.DeepCopy();
            AssertIndependentTree(core.config,copiedCatalog,"catalog");
            core.config.combat.actions[0].tags[0]="changed";
            core.config.growth.equipment[0].modifiers[0].requiredTags[0]="changed";
            core.nodes[0].childIds.Clear();
            Assert.That(source.config.combat.actions[0].tags[0],Is.Not.EqualTo("changed"));
            Assert.That(source.config.growth.equipment[0].modifiers[0].requiredTags[0],Is.Not.EqualTo("changed"));
            Assert.That(source.nodes[0].childIds,Is.Not.Empty);
        }
        [Test] public void ExplicitStateCopiesPreserveNullAndEmptyDtoMeaning()
        {
            var state=Fixture().ToCore();
            state.selectedNode=null;state.pendingReward=null;state.lastResult=null;state.dice=null;
            var absent=state.DeepCopy();
            Assert.That(absent.selectedNode,Is.Null);Assert.That(absent.pendingReward,Is.Null);
            Assert.That(absent.lastResult,Is.Null);Assert.That(absent.dice,Is.Null);
            state.selectedNode=new NodeState();state.pendingReward=new RewardDefinition();state.lastResult=new RunRecord();
            var empty=state.DeepCopy();
            Assert.That(empty.selectedNode,Is.Not.Null);Assert.That(empty.pendingReward,Is.Not.Null);Assert.That(empty.lastResult,Is.Not.Null);
            AssertIndependentTree(state,empty,"empty");
            Assert.DoesNotThrow(()=>RunStateValidator.Validate(absent));
            Assert.DoesNotThrow(()=>RunStateValidator.Validate(empty));
        }
        [Test] public void CoreApplicationOwnsCandidatesAndFailureDoesNotPublishThem()
        {
            var input=Fixture().ToCore();
            var run=new RunApplication(input);
            var baseline=run.ReadSnapshot();
            input.hp=1;input.config.combat.actions[0].damageCoefficient=90;
            run.Checkpoint=next=>throw new IOException("core checkpoint failure");
            run.RecordElapsed(.75);
            Assert.Throws<IOException>(()=>run.ChooseNode(baseline.availableNodeIds[0]));
            var unchanged=run.ReadSnapshot();
            Assert.That(unchanged.hp,Is.EqualTo(baseline.hp));
            Assert.That(unchanged.sequence,Is.EqualTo(baseline.sequence));
            Assert.That(unchanged.rngState,Is.EqualTo(baseline.rngState));
            Assert.That(unchanged.playedSeconds,Is.EqualTo(baseline.playedSeconds+.75));
            Assert.That(unchanged.config.combat.actions[0].damageCoefficient,Is.EqualTo(baseline.config.combat.actions[0].damageCoefficient));
            CoreRunState checkpoint=null;
            run.Checkpoint=next=>checkpoint=next;
            Assert.That(run.ChooseNode(baseline.availableNodeIds[0]),Is.True);
            AssertIndependentTree(checkpoint,run.ReadSnapshot(),"checkpoint");
        }
        [Test] public void SavedRulesAndIndependentStyleNeverReloadTheCurrentAsset()
        {
            var source=PrototypeAuthoring.CreateDefaults();
            source.presentation.rollSeconds=1.25f;source.presentation.explorationDice=null;
            source.presentation.combatDice=new RollPresentationSettings{rollSeconds=0,resultHoldSeconds=0};
            var run=RunSession.New(source,33,"fireball",Grade.Common);
            var checkpoint=run.ReadSnapshot();
            source.combat.actions[0].damageCoefficient=99;
            source.presentation.background=Color.magenta;
            var restored=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(checkpoint)));
            var snapshot=restored.ReadSnapshot();
            Assert.That(snapshot.config.combat.actions[0].damageCoefficient,Is.EqualTo(1.1f));
            Assert.That(snapshot.config.presentation.DiceTiming(false).rollSeconds,Is.EqualTo(1.25f));
            Assert.That(snapshot.config.presentation.DiceTiming(true).rollSeconds,Is.Zero);
            Assert.That(snapshot.config.presentation.DiceTiming(true).resultHoldSeconds,Is.Zero);
            Assert.That(snapshot.config.presentation.background,Is.EqualTo(checkpoint.config.presentation.background));
            snapshot.config.presentation.gradeColors[0]=Color.magenta;
            snapshot.config.combat.actions[0].damageCoefficient=50;
            Assert.That(restored.ReadSnapshot().config.presentation.gradeColors[0],Is.EqualTo(checkpoint.config.presentation.gradeColors[0]));
            Assert.That(restored.ReadSnapshot().config.combat.actions[0].damageCoefficient,Is.EqualTo(1.1f));
        }
        [Test] public void LocalCheckpointKeepsDurationBitsAcrossSaveAndResume()
        {
            double elapsed=0;for(int i=0;i<100;i++)elapsed+=.01;
            var source=Fixture();
            source.lastResult=new RunRecord{runId="precise-duration",seed=44,playedSeconds=elapsed};
            string directory=Path.Combine(Path.GetTempPath(),"FateDiceCoreDuration",Guid.NewGuid().ToString("N"));
            var store=new LocalRunStore(Path.Combine(directory,"run.json"));
            try
            {
                var run=new RunSession(source,store);
                for(int i=0;i<100;i++)run.RecordElapsed(.01);
                run.SaveCheckpoint();
                var committed=run.ReadSnapshot();
                for(int i=0;i<3;i++)
                {
                    var loaded=store.Load();
                    Assert.That(BitConverter.DoubleToInt64Bits(loaded.playedSeconds),Is.EqualTo(BitConverter.DoubleToInt64Bits(committed.playedSeconds)),"Current run duration bits");
                    Assert.That(BitConverter.DoubleToInt64Bits(loaded.lastResult.playedSeconds),Is.EqualTo(BitConverter.DoubleToInt64Bits(committed.lastResult.playedSeconds)),"Result duration bits");
                    Assert.That(JsonUtility.ToJson(loaded),Is.EqualTo(JsonUtility.ToJson(committed)));
                }
            }
            finally
            {
                var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceCoreDuration"))+Path.DirectorySeparatorChar;
                if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
    }
}
