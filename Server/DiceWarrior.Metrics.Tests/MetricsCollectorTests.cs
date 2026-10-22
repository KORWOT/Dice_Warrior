using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.Metrics.Tests;

public sealed class MetricsCollectorTests
{
    string root = null!, directory = null!;
    [SetUp] public void Setup() { root=Path.Combine(Path.GetTempPath(),"DiceWarriorMetricsTests",Guid.NewGuid().ToString("N")); directory=Path.Combine(root,"metrics"); Directory.CreateDirectory(root); }
    [TearDown] public void Cleanup() { if(Directory.Exists(root)) Directory.Delete(root,true); }
    MetricsCollector Collector(MetricsOptions? options=null) => new(directory,Path.Combine(root,"authority.db"),options);
    static void Complete(MetricsCollector collector,string endpoint="profile",string user="private-user",string run="private-run")
    {
        using var scope=collector.Begin(endpoint); scope.Identify(user,run); scope.Measurement.outcome="approved";
        scope.Measurement.authorityOutcome="approved"; scope.Measurement.statusCode=200; scope.Measurement.domainCode="PROFILE";
        scope.Measurement.responseComplete=true; scope.Record(new AuthorityDiagnostic("sql-select",3,1.5));
    }
    [Test] public void ReloadPreservesOpaqueKeysActualZeroBytesAndExactlyOneCompletion()
    {
        var collector=Collector(); var scope=collector.Begin("profile"); scope.Identify("secret-user","secret-run"); scope.Dispose(); scope.Dispose();
        Complete(collector,"run","secret-user","secret-run"); var snapshot=Collector().Snapshot();
        Assert.That(snapshot.completedRequests,Is.EqualTo(2)); Assert.That(snapshot.records.Count,Is.EqualTo(2));
        Assert.That(snapshot.records.Select(r=>r.userKey).Distinct().Count(),Is.EqualTo(1));
        Assert.That(snapshot.records.Select(r=>r.runKey).Distinct().Count(),Is.EqualTo(1));
        Assert.That(snapshot.records.All(r=>r.requestBytes==0&&r.responseBytes==0&&r.logBytes==0),Is.True);
        var text=string.Join("",Directory.GetFiles(directory,"*.json*").Select(File.ReadAllText));
        Assert.That(text,Does.Not.Contain("secret-user").And.Not.Contain("secret-run"));
    }
    [Test] public void ConcurrentColdStartsShareKeyAndDoNotLoseJournalRecords()
    {
        Parallel.For(0,12,_=>Complete(Collector())); var snapshot=Collector().Snapshot();
        Assert.That(snapshot.records.Count,Is.EqualTo(12)); Assert.That(snapshot.records.Select(r=>r.userKey).Distinct().Count(),Is.EqualTo(1));
        Assert.That(snapshot.records.Select(r=>r.id).Distinct().Count(),Is.EqualTo(12));
    }
    [Test] public void FileAndSnapshotCapsReportOmissionWithoutTreatingZeroBytesAsMissing()
    {
        var options=new MetricsOptions {maxRecords=2,maxJournalFiles=2,maxJournalBytes=1200}; var collector=Collector(options);
        for(int i=0;i<12;i++) Complete(collector); var snapshot=collector.Snapshot();
        Assert.That(snapshot.records.Count,Is.LessThanOrEqualTo(2)); Assert.That(snapshot.completedRequests,Is.EqualTo(12));
        Assert.That(snapshot.truncated,Is.True); Assert.That(snapshot.droppedRecords,Is.GreaterThan(0));
        Assert.That(Directory.GetFiles(directory,"*.jsonl").Length,Is.LessThanOrEqualTo(2));
        Assert.That(Directory.GetFiles(directory,"*.jsonl").All(p=>new FileInfo(p).Length<=1200),Is.True);
    }
    [Test] public void RetentionRemovesExpiredSamplesAndCountsThemAsOmitted()
    {
        var collector=Collector(); using(var scope=collector.Begin("profile")) scope.Measurement.startedUtc=DateTimeOffset.UtcNow.AddDays(-8);
        Complete(collector); var snapshot=collector.Snapshot();
        Assert.That(snapshot.records.Count,Is.EqualTo(1)); Assert.That(snapshot.droppedRecords,Is.EqualTo(1)); Assert.That(snapshot.truncated,Is.True);
    }
    [Test] public void UntrustedJournalStringsAndOversizeInputCannotReachSnapshot()
    {
        var collector=Collector(); Complete(collector); var journal=Directory.GetFiles(directory,"*.jsonl").Single();
        File.AppendAllText(journal,"{\"id\":\"raw-secret\",\"endpoint\":\"raw-secret\",\"domainCode\":\"raw-secret\",\"userKey\":\"raw-secret\",\"diagnostics\":{\"raw-secret\":{\"count\":1}}}\n");
        File.WriteAllText(Path.Combine(directory,"m-malicious.jsonl"),new string('x',5*1024*1024));
        var snapshot=collector.Snapshot(); var json=LoopJson.Encode(snapshot);
        Assert.That(json,Does.Not.Contain("raw-secret")); Assert.That(snapshot.invalidRecords,Is.GreaterThan(0)); Assert.That(snapshot.truncated,Is.True);
    }
    [Test] public void FailureToCreateMetricsDirectoryNeverAffectsRequestOrLeaksIdentities()
    {
        File.WriteAllText(directory,"occupied"); var collector=Collector(); Assert.DoesNotThrow(()=>Complete(collector));
        var snapshot=collector.Snapshot(); Assert.That(snapshot.persistenceAvailable,Is.False); Assert.That(snapshot.droppedRecords,Is.EqualTo(1));
        Assert.That(snapshot.completedRequests,Is.EqualTo(1)); Assert.That(snapshot.records,Is.Empty);
    }
    [Test] public void UnknownLiveStringsAreReplacedAndActivePeakCountsReturnToZero()
    {
        var collector=Collector(); var first=collector.Begin("raw-secret"); var second=collector.Begin("profile");
        first.Measurement.domainCode="raw-secret"; first.Measurement.outcome="raw-secret";
        first.Record(new AuthorityDiagnostic("raw-secret",100)); first.Dispose(); second.Dispose();
        var snapshot=collector.Snapshot(); Assert.That(snapshot.activeRequests,Is.Zero); Assert.That(snapshot.peakRequests,Is.EqualTo(2));
        Assert.That(LoopJson.Encode(snapshot),Does.Not.Contain("raw-secret"));
    }
    [Test] public void AggregatesUseNearestRankPercentilesUtcDaysAndDistinctOpaqueUsers()
    {
        var records=Enumerable.Range(1,100).Select(i=>new RequestMeasurement {endpoint=i%2==0?"profile":"run",durationMs=i,
            startedUtc=new DateTimeOffset(2026,10,22,0,30,0,TimeSpan.FromHours(9)),userKey=i%3==0?"a":"b",runKey="r",requestBytes=2,
            responseBytes=3,commandCount=1,logBytes=4,outcome=i==100?"rejected":"approved",diagnostics=new(){["sql-select"]=new(){count=2,milliseconds=1}}}).ToList();
        records[0].diagnostics["duplicate-operation"]=new(){count=2}; records[0].diagnostics["duplicate-submission"]=new(){count=3};
        var total=MetricsAggregation.Overall(records); Assert.That(total.requests,Is.EqualTo(100)); Assert.That(total.failures,Is.EqualTo(1)); Assert.That(total.duplicates,Is.EqualTo(1));
        Assert.That(total.averageMs,Is.EqualTo(50.5)); Assert.That(total.p50Ms,Is.EqualTo(50)); Assert.That(total.p95Ms,Is.EqualTo(95)); Assert.That(total.p99Ms,Is.EqualTo(99));
        Assert.That(total.maxMs,Is.EqualTo(100)); Assert.That(total.activeUsers,Is.EqualTo(2)); Assert.That(total.diagnostics["sql-select"].count,Is.EqualTo(200));
        Assert.That(MetricsAggregation.ByEndpoint(records).Count,Is.EqualTo(2)); Assert.That(MetricsAggregation.ByRun(records).Single().requests,Is.EqualTo(100));
        Assert.That(MetricsAggregation.ByDay(records).Single().key,Is.EqualTo("2026-10-21"));
    }
}
