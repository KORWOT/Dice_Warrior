using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class BackendSettlementSafetyTests
    {
        string directory;
        string AuthorityPath=>Path.Combine(directory,"authority.json");
        string LocalPath=>Path.Combine(directory,"checkpoint.json");
        static readonly CancellationToken None=CancellationToken.None;
        [SetUp] public void Setup()=>directory=Path.Combine(Path.GetTempPath(),"DiceWarriorSettlement",Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorSettlement"))+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        MockGameBackend Backend()=>new MockGameBackend(new MockAuthorityStore(AuthorityPath),()=>33);
        static BackendRequest Request(string kind,long revision=0,string run=null)=>new BackendRequest {
            requestId=Guid.NewGuid().ToString("N"),kind=kind,expectedRevision=revision,serverRunId=run};
        async Task<LoopSession> Started(IGameBackend backend)
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules());
            var coordinator=new GameBackendCoordinator(session,disk,backend); await coordinator.InitializeAsync();
            await coordinator.ExecuteAsync(session.Prepare("start")); return session;
        }
        [Test] public async Task PastBackupOfSettledRunRecoversReceiptAndNewestProfile()
        {
            var backend=Backend(); var session=await Started(backend); string old=File.ReadAllText(LocalPath);
            SymbolLoopTests.Finish(session);
            var settled=await backend.SubmitRunAsync(session.Prepare("settle").backend,None);
            var grown=await backend.UpgradeAsync(Request("grow",settled.profile.revision),None);
            Assert.That(grown.profile.values.currency,Is.EqualTo(50));
            File.WriteAllText(LocalPath+".backup",old); File.WriteAllText(LocalPath,"damaged-test-fixture");
            var disk=new LoopStore(LocalPath); disk.RestoreBackup();
            var restored=new LoopSession(disk,new Rules()); var coordinator=new GameBackendCoordinator(restored,disk,Backend());
            await coordinator.InitializeAsync();
            Assert.That(restored.Snapshot.run.settled,Is.True,"Server settlement must close the older unfinished backup");
            Assert.That(restored.Snapshot.submission.receiptId,Is.EqualTo(settled.submission.receiptId));
            Assert.That(restored.Snapshot.profile.currency,Is.EqualTo(50)); Assert.That(restored.Snapshot.profile.growth,Is.EqualTo(1));
            Assert.That(Directory.GetFiles(directory,"checkpoint.json.reconciled-*.json"),Is.Not.Empty);
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"checkpoint.json.damaged-*").Single()),Is.EqualTo("damaged-test-fixture"));
        }

        [Test] public async Task SettledBackupIsArchivedBeforeRecoveringAnotherSessionsNewRun()
        {
            var backend=Backend(); var session=await Started(backend); string old=File.ReadAllText(LocalPath);
            SymbolLoopTests.Finish(session); var paid=await backend.SubmitRunAsync(session.Prepare("settle").backend,None);
            var next=await backend.StartRunAsync(Request("start",paid.profile.revision),None);
            File.WriteAllText(LocalPath+".backup",old); new LoopStore(LocalPath).RestoreBackup();
            var disk=new LoopStore(LocalPath); var restored=new LoopSession(disk,new Rules());
            await new GameBackendCoordinator(restored,disk,Backend()).InitializeAsync();
            Assert.That(restored.Snapshot.run.id,Is.EqualTo(next.run.serverRunId)); Assert.That(restored.Snapshot.run.settled,Is.False);
            Assert.That(restored.Snapshot.profile.currency,Is.EqualTo(70));
            Assert.That(Directory.GetFiles(directory,"checkpoint.json.reconciled-*.json").Length,Is.GreaterThanOrEqualTo(2));
        }
        [Test] public async Task ApprovedSettlementPendingSurvivesRestoreToBeforeTheResult()
        {
            var backend=Backend(); var session=await Started(backend); string old=File.ReadAllText(LocalPath);
            SymbolLoopTests.Finish(session); var pending=session.Prepare("settle"); var disk=new LoopStore(LocalPath); disk.WritePending(pending);
            var paid=await backend.SubmitRunAsync(pending.backend,None);
            await backend.UpgradeAsync(Request("grow",paid.profile.revision),None);
            File.WriteAllText(LocalPath+".backup",old); disk.RestoreBackup();
            var restored=new LoopSession(disk,new Rules()); var coordinator=new GameBackendCoordinator(restored,disk,Backend());
            await coordinator.InitializeAsync(); Assert.That(disk.ReadPending().id,Is.EqualTo(pending.id));
            await coordinator.ExecuteAsync(disk.ReadPending());
            Assert.That(restored.Snapshot.run.settled,Is.True); Assert.That(restored.Snapshot.run.won,Is.True);
            Assert.That(restored.Snapshot.profile.currency,Is.EqualTo(50)); Assert.That(restored.Snapshot.profile.growth,Is.EqualTo(1));
            Assert.That(restored.Snapshot.submission.receiptId,Is.EqualTo(paid.submission.receiptId)); Assert.That(disk.ReadPending(),Is.Null);
        }
        static async Task<BackendReply[]> Race(params Func<Task<BackendReply>>[] actions)
        {
            using(var barrier=new Barrier(actions.Length))
            {
                var tasks=actions.Select(action=>Task.Factory.StartNew(async()=>{
                    if(!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Race fixture barrier");
                    return await action();
                },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default).Unwrap()).ToArray();
                return await Task.WhenAll(tasks);
            }
        }
        [Test] public async Task TenSimultaneousSettlementsAndChangedBodyOnlyPayOnce()
        {
            var backend=Backend(); var session=await Started(backend); SymbolLoopTests.Finish(session);
            var original=session.Prepare("settle").backend;
            var requests=Enumerable.Range(0,10).Select(_=>{var r=LoopJson.Copy(original);r.requestId=Guid.NewGuid().ToString("N");return r;}).ToArray();
            var replies=await Race(requests.Select<BackendRequest,Func<Task<BackendReply>>>(r=>()=>Backend().SubmitRunAsync(r,None)).ToArray());
            Assert.That(replies.All(r=>r.status==BackendStatus.Approved),Is.True);
            Assert.That(replies.Select(r=>r.submission.receiptId).Distinct().Count(),Is.EqualTo(1));
            var profile=await backend.GetProfileAsync(None); Assert.That(profile.profile.values.currency,Is.EqualTo(70)); Assert.That(profile.profile.revision,Is.EqualTo(2));
            var changed=LoopJson.Copy(requests[0]); changed.commands[0].arg="6";
            Assert.That((await backend.SubmitRunAsync(changed,None)).code,Is.EqualTo("REQUEST_ID_REUSED"));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(70));
        }
        [Test] public async Task TwoSessionsBuyingAtSameRevisionSpendOnceAndLoserCanReload()
        {
            var backend=Backend(); var first=await Started(backend); SymbolLoopTests.Finish(first);
            await new GameBackendCoordinator(first,new LoopStore(LocalPath),backend).ExecuteAsync(first.Prepare("settle"));
            string other=Path.Combine(directory,"device-b.json"); File.Copy(LocalPath,other);
            var a=new LoopSession(new LoopStore(LocalPath),new Rules()); var b=new LoopSession(new LoopStore(other),new Rules());
            var ca=new GameBackendCoordinator(a,new LoopStore(LocalPath),Backend());var cb=new GameBackendCoordinator(b,new LoopStore(other),Backend());
            await ca.InitializeAsync(); await cb.InitializeAsync(); var ra=a.Prepare("grow");var rb=b.Prepare("grow");
            var replies=await Race(async()=>{await ca.ExecuteAsync(ra);return ca.LastReply;},async()=>{await cb.ExecuteAsync(rb);return cb.LastReply;});
            Assert.That(replies.Count(r=>r.status==BackendStatus.Approved),Is.EqualTo(1));
            Assert.That(replies.Count(r=>r.status==BackendStatus.Conflict),Is.EqualTo(1));
            var profile=await backend.GetProfileAsync(None);Assert.That(profile.profile.values.currency,Is.EqualTo(50));Assert.That(profile.profile.values.growth,Is.EqualTo(1));
            var loser=replies[0].status==BackendStatus.Conflict?ca:cb; var session=replies[0].status==BackendStatus.Conflict?a:b;
            await loser.InitializeAsync(); await loser.ExecuteAsync(session.Prepare("grow"));
            profile=await backend.GetProfileAsync(None);Assert.That(profile.profile.values.currency,Is.EqualTo(10));Assert.That(profile.profile.values.growth,Is.EqualTo(2));
        }
        [Test] public async Task SettlementAndGrowthAtTheSameRevisionCannotBothChangeTheProfile()
        {
            var backend=Backend();var session=await Started(backend);SymbolLoopTests.Finish(session);
            var submit=session.Prepare("settle").backend;var grow=Request("grow",submit.expectedRevision);
            var replies=await Race(()=>Backend().SubmitRunAsync(submit,None),()=>Backend().UpgradeAsync(grow,None));
            Assert.That(replies[0].status,Is.EqualTo(BackendStatus.Approved));Assert.That(replies[1].status,Is.EqualTo(BackendStatus.Conflict));
            var profile=await backend.GetProfileAsync(None);Assert.That(profile.profile.values.currency,Is.EqualTo(70));Assert.That(profile.profile.values.growth,Is.Zero);
        }
        sealed class InterruptedBackend : IGameBackend
        {
            readonly IGameBackend inner; readonly string mode;
            public readonly TaskCompletionSource<bool> entered=new TaskCompletionSource<bool>(), release=new TaskCompletionSource<bool>();
            public bool blockLookup;
            public BackendReply delayedProfile;
            public InterruptedBackend(IGameBackend inner,string mode) { this.inner=inner;this.mode=mode; }
            public async Task<BackendReply> GetProfileAsync(CancellationToken ct)
            {
                if(delayedProfile!=null) { entered.TrySetResult(true);await release.Task;return LoopJson.Copy(delayedProfile); }
                return await inner.GetProfileAsync(ct);
            }
            public Task<BackendReply> GetRunStatusAsync(string id,CancellationToken ct)=>inner.GetRunStatusAsync(id,ct);
            public Task<BackendReply> StartRunAsync(BackendRequest r,CancellationToken ct)=>inner.StartRunAsync(r,ct);
            public Task<BackendReply> UpgradeAsync(BackendRequest r,CancellationToken ct)=>inner.UpgradeAsync(r,ct);
            public async Task<BackendReply> GetOperationStatusAsync(string id,CancellationToken ct)
            { if(blockLookup) { entered.TrySetResult(true); await release.Task; } return await inner.GetOperationStatusAsync(id,ct); }
            public async Task<BackendReply> SubmitRunAsync(BackendRequest r,CancellationToken ct)
            {
                if(mode=="before") throw new System.Net.Http.HttpRequestException("fixture-before-send");
                var reply=await inner.SubmitRunAsync(r,ct);
                if(mode=="after") throw new System.Net.Http.HttpRequestException("fixture-after-pay");
                if(mode=="cancel") throw new OperationCanceledException("fixture-cancel-after-pay");
                return reply;
            }
        }
        [TestCase("before",20)] [TestCase("after",70)] [TestCase("cancel",70)]
        public async Task DisconnectionOrCancellationKeepsOriginalRequestAndRawDiagnostic(string mode,int balanceBeforeRetry)
        {
            var backend=Backend();var session=await Started(backend);SymbolLoopTests.Finish(session);var disk=new LoopStore(LocalPath);
            string checkpoint=File.ReadAllText(LocalPath);var request=session.Prepare("settle");
            var coordinator=new GameBackendCoordinator(session,disk,new InterruptedBackend(backend,mode));
            await coordinator.ExecuteAsync(request);
            Assert.That(coordinator.LastReply.status,Is.EqualTo(BackendStatus.UnknownAfterTimeout));
            Assert.That(disk.ReadPending().id,Is.EqualTo(request.id));Assert.That(File.ReadAllText(LocalPath),Is.EqualTo(checkpoint));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(balanceBeforeRetry));
            Assert.That(Directory.GetFiles(directory,"*.backend-error-*.json").Select(File.ReadAllText).Any(s=>s.Contains("fixture-")),Is.True);
            var resumed=new LoopSession(disk,new Rules());var retry=new GameBackendCoordinator(resumed,disk,Backend());
            await retry.ExecuteAsync(disk.ReadPending());Assert.That(resumed.Snapshot.run.payout,Is.EqualTo(50));
            Assert.That(resumed.Snapshot.profile.currency,Is.EqualTo(70));Assert.That(disk.ReadPending(),Is.Null);
        }
        [Test] public async Task CallerMutationDuringAwaitCannotChangeTheDurableRequest()
        {
            var backend=Backend();var session=await Started(backend);SymbolLoopTests.Finish(session);var disk=new LoopStore(LocalPath);
            var delayed=new InterruptedBackend(backend,"pass") {blockLookup=true};var request=session.Prepare("settle");string id=request.id;
            var coordinator=new GameBackendCoordinator(session,disk,delayed);var task=coordinator.ExecuteAsync(request);
            await delayed.entered.Task;
            try { request.id="changed"; request.backend.commands.Clear(); request.backend.requestId="changed"; }
            finally { delayed.release.TrySetResult(true); }
            await task;
            Assert.That(session.Snapshot.submission.requestId,Is.EqualTo(id));Assert.That(session.Snapshot.profile.currency,Is.EqualTo(70));Assert.That(disk.ReadPending(),Is.Null);
        }

        [Test] public async Task LateFirstRunProfileCannotOverwriteASecondSettledRun()
        {
            var backend=Backend();var first=await Started(backend);var disk=new LoopStore(LocalPath);
            var delayed=new InterruptedBackend(backend,"pass") {delayedProfile=await backend.GetProfileAsync(None)};
            var slow=new GameBackendCoordinator(new LoopSession(disk,new Rules()),disk,delayed);
            var late=slow.InitializeAsync();await delayed.entered.Task;
            string latest=null,secondId=null;
            try
            {
                var active=new GameBackendCoordinator(first,disk,backend);
                SymbolLoopTests.Finish(first);await active.ExecuteAsync(first.Prepare("settle"));
                await active.ExecuteAsync(first.Prepare("start"));secondId=first.Snapshot.run.id;
                SymbolLoopTests.Finish(first);await active.ExecuteAsync(first.Prepare("settle"));
                latest=File.ReadAllText(LocalPath);
            }
            finally {delayed.release.TrySetResult(true);}
            Assert.ThrowsAsync<InvalidOperationException>(async()=>await late);
            Assert.That(File.ReadAllText(LocalPath),Is.EqualTo(latest));
            Assert.That(disk.Read().run.id,Is.EqualTo(secondId));Assert.That(disk.Read().run.settled,Is.True);
            Assert.That(disk.Read().profile.currency,Is.EqualTo(120));Assert.That(slow.ProfileReady,Is.False);
        }
    }
}
