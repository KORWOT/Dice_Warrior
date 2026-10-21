using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class SymbolLoopTests
    {
        string directory;
        [SetUp] public void Setup() { directory=Path.Combine(Path.GetTempPath(),"DiceWarriorSymbolLoop",Guid.NewGuid().ToString("N")); }
        [TearDown] public void Cleanup()
        {
            string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorSymbolLoop"))+Path.DirectorySeparatorChar;
            if(directory!=null && Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory,true);
        }
        LoopStore Disk() => new LoopStore(Path.Combine(directory,"player.json"));
        void Execute(LoopSession session,Command command,Rules rules=null)
        {
            var backend=new MockGameBackend(new MockAuthorityStore(Path.Combine(directory,"authority.json")),()=>command.seed==0?33:command.seed,rules);
            BackendReply reply;
            switch(command.kind)
            {
                case "start": reply=backend.StartRunAsync(command.backend,CancellationToken.None).GetAwaiter().GetResult(); break;
                case "grow": reply=backend.UpgradeAsync(command.backend,CancellationToken.None).GetAwaiter().GetResult(); break;
                case "settle": reply=backend.SubmitRunAsync(command.backend,CancellationToken.None).GetAwaiter().GetResult(); break;
                default: session.Execute(command); return;
            }
            Assert.That(reply.IsConfirmed,Is.True,reply.message);
            session.ApplyBackend(command,reply);
        }
        static Run NewRun(Rules rules=null) => LoopRules.Start(rules??new Rules(),new Profile(),33,"test-run");
        sealed class MemoryStore : ILoopStore
        {
            public PlayerDocument document=new PlayerDocument();
            public bool fail;
            public PlayerDocument Read() => LoopJson.Copy(document);
            public void Commit(PlayerDocument candidate,long revision,int generation)
            {
                if(fail) throw new IOException("Injected failure");
                Assert.That(document.revision,Is.EqualTo(revision)); Assert.That(document.generation,Is.EqualTo(generation));
                document=LoopJson.Copy(candidate);
            }
        }
        [Test] public void All84OrderedResultsUseOnlyExactWholeCombosOrAllBasics()
        {
            var rules=new Rules(); int checkedCount=0;
            for(int count=1;count<=3;count++)
            for(int value=0;value<(1<<(2*count));value++)
            {
                var faces=Enumerable.Range(0,count).Select(i=>(Symbol)((value>>(2*i))&3)).ToArray();
                string sorted=string.Concat(faces.OrderBy(s=>(int)s).Select(s=>((int)s).ToString()));
                string combo=sorted=="01"?"arc-slash":sorted=="12"?"fire-bolt":sorted=="03"?"guard-slash":
                    sorted=="012"?"flame-arc":sorted=="033"?"citadel":null;
                var expected=combo!=null?new[]{combo}:faces.Select(s=>new[]{"slash","bolt","flame","guard"}[(int)s]).ToArray();
                Assert.That(SymbolCombat.Plan(rules,faces).Select(s=>s.id),Is.EqualTo(expected),sorted); checkedCount++;
            }
            Assert.That(checkedCount,Is.EqualTo(84));
        }
        [Test] public void KillSkipsPlannedLaterSkillsAndRetaliation()
        {
            var run=NewRun(); run.enemy=new Enemy {hp=1,maxHp=1,attack=8,intent=Intent.Attack};
            var log=SymbolCombat.Resolve(run,new[]{Symbol.Sword,Symbol.Staff,Symbol.Shield});
            Assert.That(log.skills.Select(s=>s.status),Is.EqualTo(new[]{"applied","enemy-defeated","enemy-defeated"}));
            Assert.That(log.retaliation,Is.False); Assert.That(run.hp,Is.EqualTo(60)); Assert.That(log.playerBlock,Is.Zero);
        }
        [Test] public void EnemyShieldSpansWholeGroupAndPlayerBlockExpires()
        {
            var run=NewRun(); run.power=run.guard=0; run.equipment=new string[3];
            run.rules.basics[0].damage=5; run.rules.basics[1].damage=4; run.rules.basics[3].block=5;
            run.enemy=new Enemy {hp=100,maxHp=100,shield=7,attack=8,intent=Intent.Attack};
            var log=SymbolCombat.Resolve(run,new[]{Symbol.Shield,Symbol.Sword,Symbol.Staff});
            Assert.That(run.enemy.hp,Is.EqualTo(98)); Assert.That(run.enemy.shield,Is.Zero);
            Assert.That(log.playerBlock,Is.EqualTo(5)); Assert.That(run.hp,Is.EqualTo(57));
            SymbolCombat.Resolve(run,new[]{Symbol.Staff});
            Assert.That(run.hp,Is.EqualTo(49),"Previous block must not persist");
        }
        [Test] public void GearRequiresSkillTagAndGuardDoesNotReduceIncomingAttack()
        {
            var run=NewRun(); run.guard=100; run.equipment[0]="wand";
            run.enemy=new Enemy {hp=100,maxHp=100,attack=8,intent=Intent.Attack};
            var log=SymbolCombat.Resolve(run,new[]{Symbol.Sword});
            Assert.That(log.skills[0].damage,Is.EqualTo(7)); Assert.That(run.hp,Is.EqualTo(52));
            log=SymbolCombat.Resolve(run,new[]{Symbol.Fire}); Assert.That(log.skills[0].damage,Is.EqualTo(11));
        }
        [TestCase(1)] [TestCase(6)]
        public void TravelOnlyResolvesLanding(int roll)
        {
            var run=NewRun(); run.board[roll]=Tile.Rest;
            var combatRng=run.combatRng;
            LoopRules.Travel(run,roll);
            Assert.That(run.position,Is.EqualTo(roll)); Assert.That(run.phase,Is.EqualTo(Phase.Rest));
            Assert.That(run.combatRng,Is.EqualTo(combatRng)); Assert.That(run.gold,Is.Zero);
        }
        [Test] public void CrossingEachLapGrowsDiceAndThirdRewardPrecedesBoss()
        {
            var run=NewRun();
            for(int region=1;region<=3;region++)
            {
                run.position=9; run.phase=Phase.Map; LoopRules.Travel(run,5);
                Assert.That(run.phase,Is.EqualTo(Phase.LapReward)); Assert.That(run.position,Is.Zero);
                Assert.That(run.completedRegions,Is.EqualTo(region-1));
                LoopRules.ClaimLap(run,"aegis");
                Assert.That(run.completedRegions,Is.EqualTo(region)); Assert.That(run.dice.Count,Is.EqualTo(Math.Min(region+1,3)));
                Assert.Throws<InvalidOperationException>(()=>LoopRules.ClaimLap(run,"aegis"));
            }
            Assert.That(run.phase,Is.EqualTo(Phase.Combat)); Assert.That(run.enemy.boss,Is.True);
            Assert.That(run.equipment[1],Is.EqualTo("aegis")); Assert.That(run.claimed.Distinct().Count(),Is.EqualTo(3));
        }
        [Test] public void BoardsAreStableWithAllFiveTypesAndIndependentOfCombat()
        {
            for(uint seed=1;seed<=30;seed++)
            for(int region=1;region<=3;region++)
            {
                var board=LoopRules.Board(seed,region);
                Assert.That(board.Length,Is.EqualTo(12)); Assert.That(board[0],Is.EqualTo(Tile.Start));
                Assert.That(board.Skip(1).Distinct().Count(),Is.EqualTo(5)); Assert.That(LoopRules.Board(seed,region),Is.EqualTo(board));
            }
        }
        [Test] public void FailedSaveDoesNotPublishStateRandomnessOrReceipt()
        {
            var store=new MemoryStore(); var session=new LoopSession(store,new Rules());
            Execute(session,session.Prepare("start",seed:33)); string before=LoopJson.Encode(session.Snapshot);
            var request=session.Prepare("travel"); store.fail=true;
            Assert.Throws<IOException>(()=>session.Execute(request));
            Assert.That(LoopJson.Encode(session.Snapshot),Is.EqualTo(before));
            Assert.That(LoopJson.Encode(store.document),Is.EqualTo(before));
            store.fail=false; session.Execute(request);
            string after=LoopJson.Encode(session.Snapshot); session.Execute(request);
            Assert.That(LoopJson.Encode(session.Snapshot),Is.EqualTo(after));
        }
        [Test] public void DiskResumeKeepsNextResultAndReturnedSnapshotIsIsolated()
        {
            var disk=Disk(); var session=new LoopSession(disk,new Rules());
            Execute(session,session.Prepare("start",seed:33));
            var snapshot=session.Snapshot; snapshot.run.hp=1; snapshot.run.rules.dice[0].faces[0]=Symbol.Fire;
            var resumed=new LoopSession(disk,new Rules()); Assert.That(resumed.Snapshot.run.hp,Is.EqualTo(60));
            var request=session.Prepare("travel"); session.Execute(request); resumed.Execute(request);
            Assert.That(LoopJson.Encode(session.Snapshot),Is.EqualTo(LoopJson.Encode(resumed.Snapshot)));
        }
        [Test] public void RestTreasureShopAndDiceReplacementHaveActualCostsAndChoices()
        {
            var run=NewRun(); run.phase=Phase.Rest; run.hp=20; LoopRules.Choose(run,"heal");
            Assert.That(run.hp,Is.EqualTo(42)); Assert.That(run.phase,Is.EqualTo(Phase.Map));
            run.phase=Phase.Event; LoopRules.Choose(run,"risk"); Assert.That(run.gold,Is.EqualTo(25)); Assert.That(run.hp,Is.EqualTo(34));
            run.phase=Phase.Shop; LoopRules.Choose(run,"gear:mail");
            Assert.That(run.gold,Is.EqualTo(5)); Assert.That(run.equipment[1],Is.EqualTo("mail"));
            Assert.Throws<InvalidOperationException>(()=>LoopRules.Choose(run,"heal"));
            run.phase=Phase.Treasure; LoopRules.Choose(run,"die:0:arcane");
            Assert.That(run.dice[0].definitionId,Is.EqualTo("arcane")); Assert.That(run.phase,Is.EqualTo(Phase.Map));
        }
        [Test] public void SavedRunUsesFrozenRulesAfterConfigChanges()
        {
            var disk=Disk(); var session=new LoopSession(disk,new Rules());
            Execute(session,session.Prepare("start",seed:33));
            var changed=new Rules {power=99}; var resumed=new LoopSession(disk,changed);
            Assert.That(resumed.Snapshot.run.power,Is.EqualTo(5)); Assert.That(resumed.Snapshot.run.rules.power,Is.EqualTo(5));
        }
        [Test] public void CompleteCampaignVerifiesAndSettlesOnceAndGrowthAppliesToNextRun()
        {
            var store=new MemoryStore(); var rules=new Rules {hp=500,power=30};
            var session=new LoopSession(store,rules); Execute(session,session.Prepare("start",seed:42),rules);
            int commands=Finish(session);
            Assert.That(commands,Is.LessThan(100)); Assert.That(session.Snapshot.run.won,Is.True);
            var settle=session.Prepare("settle"); Execute(session,settle); Execute(session,settle); Execute(session,session.Prepare("settle"));
            Assert.That(session.Snapshot.profile.currency,Is.EqualTo(70));
            Assert.That(session.Snapshot.submission.status,Is.EqualTo(BackendStatus.Approved));
            Execute(session,session.Prepare("grow")); Assert.That(session.Snapshot.profile.currency,Is.EqualTo(50));
            Execute(session,session.Prepare("start",seed:44),rules); Assert.That(session.Snapshot.run.maxHp,Is.EqualTo(505));
        }
        [Test] public void DefaultRulesFinishWithoutFixturesAndSeed33Wins()
        {
            var session=new LoopSession(new MemoryStore(),new Rules());
            Execute(session,session.Prepare("start",seed:33));
            Assert.That(Finish(session),Is.LessThan(100));
            Assert.That(session.Snapshot.run.won,Is.True);
        }
        [Test] public void LossAndAbandonDoNotGrantUnverifiedCurrency()
        {
            var store=new MemoryStore(); var session=new LoopSession(store,new Rules());
            Execute(session,session.Prepare("start",seed:33)); session.Execute(session.Prepare("abandon"));
            Execute(session,session.Prepare("settle")); Assert.That(session.Snapshot.profile.currency,Is.EqualTo(20));
            var run=NewRun(); run.hp=1; run.enemy=new Enemy {hp=999,maxHp=999,attack=999,intent=Intent.Attack}; run.phase=Phase.Combat;
            LoopRules.RollCombat(run); Assert.That(run.phase,Is.EqualTo(Phase.Result)); Assert.That(run.won,Is.False);
        }
        [Test] public void BackupRestorationPreservesCorruptFileAndFencesOldRequests()
        {
            var disk=Disk(); var session=new LoopSession(disk,new Rules());
            Execute(session,session.Prepare("start",seed:33)); var old=session.Prepare("travel");
            session.Execute(session.Prepare("abandon"));
            File.WriteAllText(Path.Combine(directory,"player.json"),"damaged");
            Assert.Throws<IOException>(()=>disk.Read());
            Assert.That(disk.InspectBackup().run.phase,Is.EqualTo(Phase.Map));
            disk.RestoreBackup(); session.Reload();
            Assert.Throws<InvalidOperationException>(()=>session.Execute(old));
            Assert.That(Directory.GetFiles(directory,"*.damaged-*").Length,Is.EqualTo(1));
            int generation=session.Snapshot.generation; disk.RestoreBackup();
            Assert.That(disk.Read().generation,Is.GreaterThan(generation));
        }
        [Test] public void PendingRequestSurvivesRestartAndCommitReceiptMakesRetryIdempotent()
        {
            var disk=Disk(); var session=new LoopSession(disk,new Rules()); var start=session.Prepare("start",seed:33);
            disk.WritePending(start); Execute(session,start);
            var restarted=new LoopSession(Disk(),new Rules());
            Execute(restarted,disk.ReadPending());
            Assert.That(restarted.Snapshot.revision,Is.EqualTo(1)); Assert.That(restarted.Snapshot.run.id,Is.EqualTo(session.Snapshot.run.id));
            disk.ClearPending(start.id); Assert.That(disk.ReadPending(),Is.Null);
        }
        [Test] public void ExplicitPendingCancelPreservesMalformedRequestAndAllowsNewInput()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"player.json.pending"),"damaged pending");
            var disk=Disk(); Assert.Catch(()=>disk.ReadPending());
            disk.DiscardPending();
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"*.pending-discarded-*").Single()),Is.EqualTo("damaged pending"));
            var session=new LoopSession(disk,new Rules()); Execute(session,session.Prepare("start",seed:33));
            Assert.That(session.Snapshot.run.hp,Is.EqualTo(60));
        }
        [Test] public void WrongFingerprintAndStaleVersionCannotExecute()
        {
            var disk=Disk(); var session=new LoopSession(disk,new Rules()); var start=session.Prepare("start",seed:33);
            Execute(session,start); start.seed=34; Assert.Throws<InvalidOperationException>(()=>Execute(session,start));
            var a=session.Prepare("travel"); session.Execute(session.Prepare("abandon"));
            Assert.Throws<InvalidOperationException>(()=>session.Execute(a));
        }
        [Test] public void InvalidTablesAndMalformedStateAreRejected()
        {
            var rules=new Rules(); rules.combos=new[]{rules.combos[0],rules.combos[0]};
            Assert.Throws<ArgumentException>(()=>LoopRules.ValidateRules(rules));
            var document=new PlayerDocument {run=NewRun()}; document.run.board[1]=(Tile)99;
            Assert.Throws<ArgumentException>(()=>LoopRules.Validate(document));
        }
        [Test] public void AuthoredPrefabHasAllLocalBindingsAndNoLegacyDependencies()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<SymbolLoopScreen>(Editor.SymbolLoopAuthoring.PrefabPath);
            Assert.That(prefab,Is.Not.Null); Assert.That(prefab.buttons.Length,Is.EqualTo(8));
            Assert.That(prefab.tiles.Length,Is.EqualTo(12)); Assert.That(prefab.dice.Length,Is.EqualTo(3));
            Assert.That(prefab.focus,Is.Not.Null);
            Assert.That(prefab.content,Is.Not.Null);
            foreach(var button in prefab.buttons)
                Assert.That(button.button && button.label && button.background && button.border && button.icon && button.iconFallback && button.group,Is.True);
            var deps=AssetDatabase.GetDependencies(Editor.SymbolLoopAuthoring.ScenePath,true);
            Assert.That(deps.Any(p=>p.Contains("/Features/Run/")||p.Contains("/Features/Fate/")||p.Contains("/Features/Combat/")),Is.False);
            var scene=EditorSceneManager.OpenPreviewScene(Editor.SymbolLoopAuthoring.ScenePath);
            try
            {
                var objects=scene.GetRootGameObjects();
                Assert.That(objects.SelectMany(o=>o.GetComponentsInChildren<Transform>(true))
                    .Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),Is.Zero);
                var app=objects.SelectMany(o=>o.GetComponentsInChildren<SymbolLoopApp>(true)).Single();
                Assert.That(app.config,Is.Not.Null); Assert.That(app.ui,Is.Not.Null);
                Assert.That(app.ui.prefabs.Single(),Is.EqualTo(prefab));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        public static int Finish(LoopSession session)
        {
            for(int i=0;i<200;i++)
            {
                var run=session.Snapshot.run;
                if(run.phase==Phase.Result) return i;
                string kind, arg="";
                switch(run.phase)
                {
                    case Phase.Map:kind="travel";break;
                    case Phase.Combat:kind="combat";break;
                    case Phase.LapReward:kind="lap";arg="aegis";break;
                    case Phase.Rest:kind="choose";arg="heal";break;
                    case Phase.Event:kind="choose";arg="study";break;
                    case Phase.Treasure:kind="choose";arg="gear:mail";break;
                    default:kind="choose";arg="leave";break;
                }
                session.Execute(session.Prepare(kind,arg));
            }
            Assert.Fail("Campaign did not end"); return 200;
        }
    }
}
