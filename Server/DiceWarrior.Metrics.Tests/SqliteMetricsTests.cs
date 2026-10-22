using DiceWarrior.SymbolLoop;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.Metrics.Tests;

public sealed class SqliteMetricsTests
{
    string directory=null!,path=null!;
    [SetUp] public void Setup() { directory=Path.Combine(Path.GetTempPath(),"DiceWarriorSqlMetrics",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path=Path.Combine(directory,"authority.db"); }
    [TearDown] public void Cleanup() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
    sealed class Sink : IAuthorityTelemetry
    {
        public readonly List<AuthorityDiagnostic> values=new();
        public void Record(AuthorityDiagnostic value) { lock(values) values.Add(value); }
        public long Count(string key) { lock(values) return values.Where(v=>v.kind==key).Sum(v=>v.count); }
    }
    sealed class ThrowingSink : IAuthorityTelemetry { public void Record(AuthorityDiagnostic value)=>throw new IOException("injected metrics failure"); }
    [Test] public void ActualReadMeasuresThreeSelectsAndReturnedRowsIncludingEmptyTables()
    {
        var sink=new Sink(); var repository=new SqliteAuthorityRepository(path,"alice",sink); sink.values.Clear();
        var reply=repository.Execute(d=>new MockMutation {reply=new BackendReply {profile=d.profile}},default);
        Assert.That(reply.profile.values.currency,Is.EqualTo(20)); Assert.That(sink.Count("sql-select"),Is.EqualTo(3));
        Assert.That(sink.Count("sql-rows"),Is.EqualTo(1)); Assert.That(sink.Count("transaction-begin"),Is.EqualTo(1));
        Assert.That(sink.Count("transaction-commit"),Is.EqualTo(1)); Assert.That(sink.Count("transaction-rollback"),Is.Zero);
        Assert.That(sink.values.All(v=>v.milliseconds>=0),Is.True);
    }
    [Test] public async Task ThrowingObserverDoesNotChangePurchaseOrDuplicateReceipt()
    {
        var backend=new MockGameBackend(new SqliteAuthorityRepository(path,"alice",new ThrowingSink()),()=>33);
        var request=new BackendRequest {requestId="buy",kind="grow"}; var bought=await backend.UpgradeAsync(request,default);
        var duplicate=await backend.UpgradeAsync(request,default); Assert.That(bought.status,Is.EqualTo(BackendStatus.Approved));
        Assert.That(LoopJson.Encode(duplicate),Is.EqualTo(LoopJson.Encode(bought))); Assert.That((await backend.GetProfileAsync(default)).profile.values.currency,Is.Zero);
    }
    [Test] public async Task ThrowingObserverStillPaysVerifiedVictoryExactlyOnce()
    {
        var observer=new ThrowingSink();
        var backend=new MockGameBackend(new SqliteAuthorityRepository(path,"alice",observer),()=>33,telemetry:observer);
        var start=await backend.StartRunAsync(new BackendRequest {requestId="start",kind="start"},default);
        var golden=JObject.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"ReplayGoldenVectors.json")));
        var vector=golden["vectors"]!.Single(v=>(string?)v["name"]=="victory");
        var commands=LoopJson.Decode<List<RunCommandEntry>>(vector["commands"]!.ToString());
        var request=new BackendRequest {requestId="settle",kind="settle",serverRunId=start.run.serverRunId,expectedRevision=1,commands=commands};
        var settled=await backend.SubmitRunAsync(request,default); var duplicate=await backend.SubmitRunAsync(request,default);
        request.requestId="settle-new-id"; var sameRun=await backend.SubmitRunAsync(request,default);
        Assert.That(settled.status,Is.EqualTo(BackendStatus.Approved)); Assert.That(settled.submission.appliedAmount,Is.EqualTo(50));
        Assert.That(duplicate.submission.receiptId,Is.EqualTo(settled.submission.receiptId)); Assert.That(sameRun.submission.receiptId,Is.EqualTo(settled.submission.receiptId));
        Assert.That((await backend.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(70));
    }
    [Test] public async Task FailedWriteRecordsRollbackAndPreservesProfile()
    {
        var sink=new Sink(); var backend=new MockGameBackend(new SqliteAuthorityRepository(path,"alice",sink),()=>33);
        using(var connection=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=path,Pooling=false}.ToString()))
        { connection.Open(); using var command=connection.CreateCommand(); command.CommandText="CREATE TRIGGER fail_operation BEFORE INSERT ON operations BEGIN SELECT RAISE(ABORT,'injected'); END;"; command.ExecuteNonQuery(); }
        sink.values.Clear(); var reply=await backend.UpgradeAsync(new BackendRequest {requestId="buy",kind="grow"},default);
        Assert.That(reply.status,Is.EqualTo(BackendStatus.TemporaryFailure)); Assert.That(sink.Count("transaction-rollback"),Is.EqualTo(1));
        Assert.That(sink.Count("sql-update"),Is.EqualTo(1)); Assert.That(sink.Count("sql-insert"),Is.EqualTo(1));
        Assert.That((await backend.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(20));
    }
    [Test] public async Task CasRetryReportsOneDiscardedTransactionAndPreservesConcurrentSpend()
    {
        var sink=new Sink(); var repository=new SqliteAuthorityRepository(path,"alice",sink); var other=new MockGameBackend(new SqliteAuthorityRepository(path,"alice"),()=>33);
        using var entered=new ManualResetEventSlim(); using var release=new ManualResetEventSlim(); int calls=0;
        var slow=Task.Run(()=>repository.Execute(d=>{if(Interlocked.Increment(ref calls)==1){entered.Set(); if(!release.Wait(10000))throw new TimeoutException();}
            d.profile.values.currency++; d.profile.revision++; return new MockMutation {changed=true,reply=new BackendReply {profile=d.profile}};},default));
        try { Assert.That(entered.Wait(10000),Is.True); Assert.That((await other.UpgradeAsync(new BackendRequest {requestId="buy",kind="grow"},default)).status,Is.EqualTo(BackendStatus.Approved)); }
        finally { release.Set(); await slow; }
        Assert.That(calls,Is.EqualTo(2)); Assert.That(sink.Count("cas-retry"),Is.EqualTo(1)); Assert.That(sink.Count("transaction-rollback"),Is.EqualTo(1));
        Assert.That((await other.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(1));
    }
}
