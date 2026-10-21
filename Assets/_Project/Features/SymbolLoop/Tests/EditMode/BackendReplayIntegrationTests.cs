using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class BackendReplayIntegrationTests
    {
        string directory;
        string AuthorityPath=>Path.Combine(directory,"authority.json");
        string LocalPath=>Path.Combine(directory,"checkpoint.json");
        static readonly CancellationToken None=CancellationToken.None;
        [SetUp] public void Setup()=>directory=Path.Combine(Path.GetTempPath(),"DiceWarriorReplay",Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorReplay"))+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        MockGameBackend Backend(Func<long> clock=null,Rules rules=null)=>new MockGameBackend(new MockAuthorityStore(AuthorityPath),()=>33,rules,clock);
        static BackendRequest Request(string kind,long revision=0,string run=null)=>new BackendRequest {
            requestId=Guid.NewGuid().ToString("N"),kind=kind,expectedRevision=revision,serverRunId=run};
        async Task<LoopSession> Finished(IGameBackend backend)
        {
            var disk=new LoopStore(LocalPath); var session=new LoopSession(disk,new Rules());
            var coordinator=new GameBackendCoordinator(session,disk,backend); await coordinator.InitializeAsync();
            await coordinator.ExecuteAsync(session.Prepare("start")); SymbolLoopTests.Finish(session); return session;
        }
        [Test] public async Task InvalidLogsAndClientResultClaimsCannotGrantOrCloseTheActiveRun()
        {
            var backend=Backend(); var session=await Finished(backend); var valid=session.Prepare("settle").backend;
            var wrong=LoopJson.Copy(valid); wrong.commands[0].arg="6";
            var denied=await backend.SubmitRunAsync(wrong,None);
            Assert.That(denied.status,Is.EqualTo(BackendStatus.Rejected));
            var profile=await backend.GetProfileAsync(None);
            Assert.That(profile.profile.values.currency,Is.EqualTo(20)); Assert.That(profile.run.serverRunId,Is.EqualTo(valid.serverRunId));
            wrong=LoopJson.Copy(valid); wrong.requestId=Guid.NewGuid().ToString("N"); wrong.commands.RemoveAt(0);
            Assert.That((await backend.SubmitRunAsync(wrong,None)).status,Is.EqualTo(BackendStatus.Rejected));
            string json=LoopJson.Encode(valid); json=json.Substring(0,json.Length-1)+",\"won\":true,\"claimedCurrency\":999999}";
            Assert.Throws<JsonSerializationException>(()=>LoopJson.Decode<BackendRequest>(json));
            valid.requestId=Guid.NewGuid().ToString("N");
            var good=await backend.SubmitRunAsync(valid,None);
            Assert.That(good.status,Is.EqualTo(BackendStatus.Approved)); Assert.That(good.submission.appliedAmount,Is.EqualTo(50));
            Assert.That(good.profile.values.currency,Is.EqualTo(70));
        }
        [Test] public async Task ServerTicketRulesWinOverLaterConfigurationAndDuplicateSubmissionsPayOnce()
        {
            var session=await Finished(Backend()); var request=session.Prepare("settle").backend;
            var backend=Backend(rules:new Rules {regionCurrency=999,winCurrency=888,power=99});
            var first=await backend.SubmitRunAsync(request,None);
            Assert.That(first.submission.appliedAmount,Is.EqualTo(50)); Assert.That(first.submission.won,Is.True);
            for(int i=0;i<10;i++)
            {
                request.requestId=Guid.NewGuid().ToString("N");
                var repeated=await backend.SubmitRunAsync(request,None);
                Assert.That(repeated.submission.receiptId,Is.EqualTo(first.submission.receiptId));
                Assert.That(repeated.profile.values.currency,Is.EqualTo(70));
            }
        }
        [Test] public async Task LegacyPendingRecordAndItsReceiptRemainUnpaid()
        {
            var backend=Backend(); var session=await Finished(backend); var request=session.Prepare("settle").backend;
            // Isolated P1-format fixture; never read or mutate a real account/save.
            var envelope=JObject.Parse(File.ReadAllText(AuthorityPath)); var document=JObject.Parse((string)envelope["payload"]);
            var run=(JObject)document["runs"][0]; run["commands"]=JToken.Parse(LoopJson.Encode(request.commands));
            var oldReceipt=new JObject { ["requestId"]=request.requestId,["serverRunId"]=request.serverRunId,
                ["receiptId"]="p1-unverified-receipt",["status"]=(int)BackendStatus.PendingVerification,["appliedAmount"]=0 };
            run["submission"]=oldReceipt; document["activeRunId"]=null;
            document["profile"]["revision"]=(long)document["profile"]["revision"]+1;
            foreach(string field in new[]{"rateWindowOpen","rateWindowStartMs","startsInWindow","submitsInWindow","abandonsInWindow"}) document.Remove(field);
            string payload=document.ToString(Formatting.None); envelope["payload"]=payload; envelope["checksum"]=LoopJson.Hash(payload);
            File.WriteAllText(AuthorityPath,envelope.ToString(Formatting.None));
            string before=File.ReadAllText(AuthorityPath);
            Assert.That((await Backend().GetProfileAsync(None)).profile.values.currency,Is.EqualTo(20));
            Assert.That(File.ReadAllText(AuthorityPath),Is.EqualTo(before),"A read must not migrate or pay pending records");
            var replay=await Backend().SubmitRunAsync(request,None);
            Assert.That(replay.status,Is.EqualTo(BackendStatus.PendingVerification)); Assert.That(replay.submission.appliedAmount,Is.Zero);
            Assert.That(replay.submission.receiptId,Is.EqualTo("p1-unverified-receipt"));
            Assert.That(replay.profile.values.currency,Is.EqualTo(20));
        }
        sealed class FailingStore : ILoopStore
        {
            public bool fail;
            readonly LoopStore disk;
            public FailingStore(LoopStore disk) { this.disk=disk; }
            public PlayerDocument Read()=>disk.Read();
            public void Commit(PlayerDocument doc,long revision,int generation)
            { if(fail) throw new IOException("Injected save failure after payout"); disk.Commit(doc,revision,generation); }
        }
        sealed class LostSubmissionReply : IGameBackend
        {
            readonly IGameBackend inner;
            public LostSubmissionReply(IGameBackend inner) { this.inner=inner; }
            public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>inner.GetProfileAsync(ct);
            public Task<BackendReply> GetRunStatusAsync(string id,CancellationToken ct)=>inner.GetRunStatusAsync(id,ct);
            public Task<BackendReply> StartRunAsync(BackendRequest r,CancellationToken ct)=>inner.StartRunAsync(r,ct);
            public Task<BackendReply> UpgradeAsync(BackendRequest r,CancellationToken ct)=>inner.UpgradeAsync(r,ct);
            public Task<BackendReply> GetOperationStatusAsync(string id,CancellationToken ct)=>inner.GetOperationStatusAsync(id,ct);
            public async Task<BackendReply> SubmitRunAsync(BackendRequest r,CancellationToken ct)
            { await inner.SubmitRunAsync(r,ct); throw new TimeoutException(); }
        }
        [Test] public async Task LostPayoutReplyThenFailedLocalSaveRecoverTheSameReceiptOnce()
        {
            var backend=Backend(); await Finished(backend); var disk=new LoopStore(LocalPath);
            var fault=new FailingStore(disk); var session=new LoopSession(fault,new Rules());
            var coordinator=new GameBackendCoordinator(session,disk,new LostSubmissionReply(backend));
            var command=session.Prepare("settle"); await coordinator.ExecuteAsync(command);
            Assert.That(coordinator.LastReply.status,Is.EqualTo(BackendStatus.UnknownAfterTimeout));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(70));
            fault.fail=true; coordinator=new GameBackendCoordinator(session,disk,backend);
            Assert.ThrowsAsync<IOException>(async()=>await coordinator.ExecuteAsync(disk.ReadPending()));
            Assert.That(disk.ReadPending().id,Is.EqualTo(command.id));
            var resumed=new LoopSession(disk,new Rules()); coordinator=new GameBackendCoordinator(resumed,disk,Backend());
            await coordinator.ExecuteAsync(disk.ReadPending());
            Assert.That(resumed.Snapshot.run.payout,Is.EqualTo(50)); Assert.That(resumed.Snapshot.profile.currency,Is.EqualTo(70));
            Assert.That(disk.ReadPending(),Is.Null);
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(70));
        }
        static BackendRequest Abandon(RunTicket ticket,long revision)
        {
            var run=LoopRules.Start(LoopJson.Copy(ticket.rules),ticket.startingProfile.values,ticket.seed,ticket.serverRunId);
            var request=Request("settle",revision,ticket.serverRunId);
            request.commands.Add(new RunCommandEntry {sequence=1,kind="abandon",arg="",targetId=RunCommandRules.Target(run),clientCommandId=Guid.NewGuid().ToString("N")});
            return request;
        }
        [Test] public async Task DurableServerRateLimitsIgnoreExactRetriesAndClockRollback()
        {
            long now=100000; var backend=Backend(()=>now); long revision=0;
            for(int i=0;i<10;i++)
            {
                var request=Request("start",revision); var started=await backend.StartRunAsync(request,None);
                Assert.That(started.status,Is.EqualTo(BackendStatus.Approved));
                for(int retry=0;retry<3;retry++) Assert.That((await backend.StartRunAsync(request,None)).run.serverRunId,Is.EqualTo(started.run.serverRunId));
                var end=await backend.SubmitRunAsync(Abandon(started.run,started.profile.revision),None);
                Assert.That(end.status,Is.EqualTo(BackendStatus.Approved)); revision=end.profile.revision;
            }
            Assert.That((await Backend(()=>now).StartRunAsync(Request("start",revision),None)).code,Is.EqualTo("START_RATE_LIMIT"));
            now=90000;
            Assert.That((await backend.StartRunAsync(Request("start",revision),None)).code,Is.EqualTo("START_RATE_LIMIT"));
            now=160000;
            var next=await backend.StartRunAsync(Request("start",revision),None); Assert.That(next.status,Is.EqualTo(BackendStatus.Approved));
            var invalid=Request("settle",next.profile.revision,next.run.serverRunId);
            for(int i=0;i<20;i++) { invalid.requestId=Guid.NewGuid().ToString("N"); Assert.That((await backend.SubmitRunAsync(invalid,None)).status,Is.EqualTo(BackendStatus.Rejected)); }
            invalid.requestId=Guid.NewGuid().ToString("N");
            Assert.That((await backend.SubmitRunAsync(invalid,None)).code,Is.EqualTo("SUBMIT_RATE_LIMIT"));
            Assert.That((await backend.GetProfileAsync(None)).profile.values.currency,Is.EqualTo(20));
        }
    }
}
