using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class BackendFoundationTests
    {
        string directory;
        string AuthorityPath => Path.Combine(directory,"authority.json");
        string LocalPath => Path.Combine(directory,"checkpoint.json");
        static readonly CancellationToken None=CancellationToken.None;
        [SetUp] public void Setup() => directory=Path.Combine(Path.GetTempPath(),"DiceWarriorBackend",Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorBackend"))+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        MockGameBackend Backend(uint seed=33) => new MockGameBackend(new MockAuthorityStore(AuthorityPath),()=>seed);
        BackendRequest Request(string kind,long revision=0,string run=null) => new BackendRequest {
            kind=kind,requestId=Guid.NewGuid().ToString("N"),expectedRevision=revision,serverRunId=run};
        async Task<GameBackendCoordinator> Open(IGameBackend backend,LoopSession session,LoopStore store)
        {
            var coordinator=new GameBackendCoordinator(session,store,backend); await coordinator.InitializeAsync(); return coordinator;
        }
        [Test] public async Task MockGrantIsOnceAndReturnedOrLocalProfileCannotChangeAuthority()
        {
            var backend=Backend(); var profile=await backend.GetProfileAsync(None);
            Assert.That(profile.profile.values.currency,Is.EqualTo(20));
            profile.profile.values.currency=999999; profile.profile.values.growth=100;
            var store=new LoopStore(LocalPath); var session=new LoopSession(store,new Rules());
            var coordinator=await Open(backend,session,store);
            var forged=session.Snapshot; forged.profile.currency=999999; forged.profile.growth=100;
            forged.authority.values.currency=999999; forged.revision++;
            store.Commit(forged,forged.revision-1,forged.generation); session.Reload();
            await coordinator.ExecuteAsync(session.Prepare("start",seed:999));
            Assert.That(session.Snapshot.run.seed,Is.EqualTo(33));
            Assert.That(session.Snapshot.run.maxHp,Is.EqualTo(60));
            Assert.That(session.Snapshot.run.rules.power,Is.EqualTo(5));
            Assert.That((await Backend().GetProfileAsync(None)).profile.values.currency,Is.EqualTo(20));
        }
        [Test] public async Task UpgradeChecksCostRevisionAndReplayFingerprint()
        {
            var backend=Backend(); var request=Request("grow");
            var first=await backend.UpgradeAsync(request,None);
            Assert.That(first.status,Is.EqualTo(BackendStatus.Approved));
            Assert.That(first.profile.values.currency,Is.Zero); Assert.That(first.profile.values.growth,Is.EqualTo(1));
            Assert.That(LoopJson.Encode(await Backend().UpgradeAsync(request,None)),Is.EqualTo(LoopJson.Encode(first)));
            var conflict=await backend.UpgradeAsync(Request("grow"),None);
            Assert.That(conflict.status,Is.EqualTo(BackendStatus.Conflict));
            request.upgrade="forged";
            Assert.That((await backend.UpgradeAsync(request,None)).code,Is.EqualTo("REQUEST_ID_REUSED"));
            Assert.That((await backend.UpgradeAsync(Request("grow",1),None)).code,Is.EqualTo("INSUFFICIENT_CURRENCY"));
        }
        [Test] public async Task ConcurrentAdaptersSpendTheInitialBalanceOnlyOnce()
        {
            var a=Backend(); var b=Backend();
            var replies=await Task.WhenAll(Task.Run(()=>a.UpgradeAsync(Request("grow"),None)),Task.Run(()=>b.UpgradeAsync(Request("grow"),None)));
            Assert.That(replies.Count(r=>r.status==BackendStatus.Approved),Is.EqualTo(1));
            Assert.That(replies.Count(r=>r.status==BackendStatus.Conflict),Is.EqualTo(1));
            Assert.That((await a.GetProfileAsync(None)).profile.values.growth,Is.EqualTo(1));
        }
        [Test] public async Task SubmissionIsVerifiedWithSingleRewardAndNextRunIsPossible()
        {
            var store=new LoopStore(LocalPath); var session=new LoopSession(store,new Rules()); var backend=Backend();
            var coordinator=await Open(backend,session,store);
            await coordinator.ExecuteAsync(session.Prepare("start"));
            SymbolLoopTests.Finish(session);
            Assert.That(session.Snapshot.run.won,Is.True);
            Assert.That(session.Snapshot.commands.Count,Is.GreaterThan(10));
            var submit=session.Prepare("settle");
            for(int i=0;i<10;i++) await coordinator.ExecuteAsync(submit);
            Assert.That(session.Snapshot.submission.status,Is.EqualTo(BackendStatus.Approved));
            Assert.That(session.Snapshot.run.payout,Is.EqualTo(50));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(70));
            string id=session.Snapshot.run.id;
            await coordinator.ExecuteAsync(session.Prepare("start"));
            Assert.That(session.Snapshot.run.id,Is.Not.EqualTo(id)); Assert.That(session.Snapshot.commands,Is.Empty);
            Assert.That((await backend.GetOperationStatusAsync(submit.id,None)).submission.serverRunId,Is.EqualTo(id));
        }
        [Test] public async Task InvalidVersionLoadoutOwnershipAndOversizedLogAreExplicitFailures()
        {
            var backend=Backend(); var request=Request("start"); request.rulesVersion="future";
            Assert.That((await backend.StartRunAsync(request,None)).code,Is.EqualTo("VERSION_MISMATCH"));
            request=Request("start"); request.startingDie="unowned";
            Assert.That((await backend.StartRunAsync(request,None)).code,Is.EqualTo("INVALID_LOADOUT"));
            Assert.That((await backend.SubmitRunAsync(Request("settle",0,"other-run"),None)).code,Is.EqualTo("RUN_NOT_FOUND"));
            request=Request("settle"); request.commands=Enumerable.Range(0,2049).Select(i=>new RunCommandEntry()).ToList();
            Assert.That((await backend.SubmitRunAsync(request,None)).code,Is.EqualTo("COMMAND_LIMIT"));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(20));
        }
        [Test] public async Task UnsupportedFirebaseDoesNotCreateMockAuthority()
        {
            var backend=GameBackendFactory.Create(BackendMode.Firebase,AuthorityPath);
            Assert.That((await backend.GetProfileAsync(None)).status,Is.EqualTo(BackendStatus.Unsupported));
            Assert.That((await backend.StartRunAsync(Request("start"),None)).status,Is.EqualTo(BackendStatus.Unsupported));
            Assert.That(File.Exists(AuthorityPath),Is.False);
        }
        [Test] public async Task MissingOrCorruptAuthorityNeverRestoresOldCurrencyOrRegrants()
        {
            var backend=Backend(); await backend.GetProfileAsync(None);
            await backend.UpgradeAsync(Request("grow"),None);
            byte[] original=File.ReadAllBytes(AuthorityPath); File.WriteAllText(AuthorityPath,"corrupt");
            Assert.That((await Backend().GetProfileAsync(None)).status,Is.EqualTo(BackendStatus.TemporaryFailure));
            Assert.That(File.ReadAllText(AuthorityPath),Is.EqualTo("corrupt"));
            File.Delete(AuthorityPath);
            Assert.That((await Backend().GetProfileAsync(None)).status,Is.EqualTo(BackendStatus.TemporaryFailure));
            Assert.That(File.Exists(AuthorityPath),Is.False);
            File.WriteAllBytes(AuthorityPath,original);
            Assert.That((await Backend().GetProfileAsync(None)).profile.values.currency,Is.Zero);
        }
        sealed class FaultStore : ILoopStore
        {
            public readonly LoopStore disk;
            public bool fail;
            public FaultStore(LoopStore disk) { this.disk=disk; }
            public PlayerDocument Read()=>disk.Read();
            public void Commit(PlayerDocument value,long revision,int generation)
            { if(fail) throw new IOException("Injected checkpoint failure"); disk.Commit(value,revision,generation); }
        }
        [Test] public async Task ApprovedUpgradeSurvivesLocalSaveFailureAndRestart()
        {
            var disk=new LoopStore(LocalPath); var fault=new FaultStore(disk); var session=new LoopSession(fault,new Rules());
            var coordinator=await Open(Backend(),session,disk); var command=session.Prepare("grow"); fault.fail=true;
            Assert.ThrowsAsync<IOException>(async()=>await coordinator.ExecuteAsync(command));
            Assert.That(session.Snapshot.profile.growth,Is.Zero); Assert.That(disk.ReadPending().id,Is.EqualTo(command.id));
            var restarted=new LoopSession(disk,new Rules()); var recovery=await Open(Backend(),restarted,disk);
            await recovery.ExecuteAsync(disk.ReadPending());
            Assert.That(restarted.Snapshot.profile.growth,Is.EqualTo(1)); Assert.That(restarted.Snapshot.profile.currency,Is.Zero);
            Assert.That(disk.ReadPending(),Is.Null);
        }
        sealed class LostReplyBackend : IGameBackend
        {
            readonly IGameBackend inner;
            public LostReplyBackend(IGameBackend inner) { this.inner=inner; }
            public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>inner.GetProfileAsync(ct);
            public Task<BackendReply> GetRunStatusAsync(string id,CancellationToken ct)=>inner.GetRunStatusAsync(id,ct);
            public async Task<BackendReply> StartRunAsync(BackendRequest r,CancellationToken ct) { await inner.StartRunAsync(r,ct); throw new TimeoutException(); }
            public Task<BackendReply> SubmitRunAsync(BackendRequest r,CancellationToken ct)=>inner.SubmitRunAsync(r,ct);
            public Task<BackendReply> UpgradeAsync(BackendRequest r,CancellationToken ct)=>inner.UpgradeAsync(r,ct);
            public Task<BackendReply> GetOperationStatusAsync(string id,CancellationToken ct)=>inner.GetOperationStatusAsync(id,ct);
        }
        [Test] public async Task LostStartResponseResumesSameServerRunAndCannotReplayChangedBody()
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules()); var backend=Backend();
            var coordinator=await Open(new LostReplyBackend(backend),session,disk); var command=session.Prepare("start");
            await coordinator.ExecuteAsync(command);
            Assert.That(coordinator.LastReply.status,Is.EqualTo(BackendStatus.UnknownAfterTimeout));
            Assert.That(session.Snapshot.run,Is.Null); Assert.That(disk.ReadPending().id,Is.EqualTo(command.id));
            var started=await backend.GetOperationStatusAsync(command.id,None);
            coordinator=new GameBackendCoordinator(session,disk,backend); await coordinator.ExecuteAsync(disk.ReadPending());
            Assert.That(session.Snapshot.run.id,Is.EqualTo(started.run.serverRunId)); Assert.That(disk.ReadPending(),Is.Null);
            command.backend.startingDie="arcane";
            await coordinator.ExecuteAsync(command);
            Assert.That(coordinator.LastReply.code,Is.EqualTo("REQUEST_ID_REUSED"));
            Assert.That(session.Snapshot.run.id,Is.EqualTo(started.run.serverRunId));
        }
        [Test] public async Task RestoredCheckpointRefreshesFromAuthorityWithoutRollingItBack()
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules()); var backend=Backend();
            var coordinator=await Open(backend,session,disk);
            await coordinator.ExecuteAsync(session.Prepare("grow"));
            disk.RestoreBackup(); session.Reload(); Assert.That(session.Snapshot.profile.currency,Is.EqualTo(20));
            await coordinator.InitializeAsync();
            Assert.That(session.Snapshot.profile.currency,Is.Zero); Assert.That(session.Snapshot.profile.growth,Is.EqualTo(1));
            Assert.That((await Backend().GetProfileAsync(None)).profile.values.currency,Is.Zero);
        }
        [Test] public async Task LocalSaveFailureDoesNotAppendCommandsOrAdvanceRandomness()
        {
            var disk=new LoopStore(LocalPath); var fault=new FaultStore(disk); var session=new LoopSession(fault,new Rules());
            var coordinator=await Open(Backend(),session,disk); await coordinator.ExecuteAsync(session.Prepare("start"));
            string before=LoopJson.Encode(session.Snapshot); var travel=session.Prepare("travel"); fault.fail=true;
            Assert.ThrowsAsync<IOException>(async()=>await coordinator.ExecuteAsync(travel));
            Assert.That(LoopJson.Encode(session.Snapshot),Is.EqualTo(before));
            fault.fail=false; await coordinator.ExecuteAsync(disk.ReadPending());
            Assert.That(session.Snapshot.commands.Count,Is.EqualTo(1));
            await coordinator.ExecuteAsync(travel); Assert.That(session.Snapshot.commands.Count,Is.EqualTo(1));
        }
        [Test] public void DirectLocalPermanentMutationsAreRefused()
        {
            var session=new LoopSession(new LoopStore(LocalPath),new Rules());
            foreach(string kind in new[]{"start","grow","settle"})
                Assert.Throws<InvalidOperationException>(()=>session.Execute(session.Prepare(kind)));
        }
        [Test] public async Task LobbyBackupRecoversActiveTicketAndMatchingProgressIsNeverRestarted()
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules()); var backend=Backend();
            var coordinator=await Open(backend,session,disk); await coordinator.ExecuteAsync(session.Prepare("start"));
            string id=session.Snapshot.run.id;
            disk.RestoreBackup(); session.Reload(); Assert.That(session.Snapshot.run,Is.Null);
            await coordinator.InitializeAsync(); Assert.That(session.Snapshot.run.id,Is.EqualTo(id));
            await coordinator.ExecuteAsync(session.Prepare("travel"));
            string progressed=LoopJson.Encode(session.Snapshot);
            await coordinator.InitializeAsync(); Assert.That(LoopJson.Encode(session.Snapshot),Is.EqualTo(progressed));
        }
        [Test] public async Task StaleUndispatchedBackendInputDoesNotTrapPendingOrChangeAuthority()
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules()); var backend=Backend();
            var coordinator=await Open(backend,session,disk); var stale=session.Prepare("grow");
            session.Execute(session.Prepare("loadout","die:arcane"));
            await coordinator.ExecuteAsync(stale);
            Assert.That(coordinator.LastReply.code,Is.EqualTo("LOCAL_REQUEST_STALE"));
            Assert.That(disk.ReadPending(),Is.Null);
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(20));
        }
    }
}
