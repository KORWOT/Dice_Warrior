using DiceWarrior.Metrics;
using NUnit.Framework;

namespace DiceWarrior.Metrics.Tests;

public sealed class CostEstimatorTests
{
    [Test]
    public void LogicalOperationModelSeparatesSqlAndDuplicateOutcomes()
    {
        var fresh = new RequestMeasurement { endpoint="settle",authorityOutcome="approved",domainCode="RUN_VERIFIED",logBytes=100,responseBytes=200 };
        fresh.diagnostics["sql-select"]=new(){count=999};
        var original=FirebaseOperationModel.Describe(fresh);
        Assert.That(original.reads,Is.EqualTo(4)); Assert.That(original.writes,Is.EqualTo(4));
        fresh.diagnostics["cas-retry"]=new(){count=20};
        Assert.That(FirebaseOperationModel.Describe(fresh).reads,Is.EqualTo(4));
        fresh.outcome="transport-error";
        Assert.That(FirebaseOperationModel.Describe(fresh).writes,Is.EqualTo(4),"Committed response loss still has logical writes");
        fresh.diagnostics["duplicate-operation"]=new(){count=1};
        var duplicate=FirebaseOperationModel.Describe(fresh);
        Assert.That(duplicate.reads,Is.EqualTo(1)); Assert.That(duplicate.writes,Is.Zero); Assert.That(duplicate.storageBytes,Is.Zero);
        fresh.diagnostics.Remove("duplicate-operation");fresh.diagnostics["duplicate-submission"]=new(){count=1};
        Assert.That(FirebaseOperationModel.Describe(fresh).writes,Is.EqualTo(1));
    }

    [TestCase("profile",1)] [TestCase("operation",1)] [TestCase("run",2)]
    public void LookupsAndNotFoundAreChargedLogicalReads(string endpoint,long reads)
    {
        var result=FirebaseOperationModel.Describe(new(){endpoint=endpoint,authorityOutcome="rejected",domainCode="RUN_NOT_FOUND"});
        Assert.That(result.reads,Is.EqualTo(reads));Assert.That(result.writes,Is.Zero);
    }

    [Test]
    public void BeforeAuthorityErrorAndValidationRejectionDiffer()
    {
        Assert.That(FirebaseOperationModel.Describe(new(){endpoint="settle",statusCode=400}).reads,Is.Zero);
        var result=FirebaseOperationModel.Describe(new(){endpoint="settle",authorityOutcome="rejected",domainCode="INVALID_COMMANDS"});
        Assert.That(result.reads,Is.EqualTo(4));Assert.That(result.writes,Is.EqualTo(1));
        Assert.That(FirebaseOperationModel.Describe(new(){endpoint="settle",authorityOutcome="temporary-failure"}).supported,Is.False);
    }

    [Test]
    public void SimpleWorkloadMatchesHandCalculatedOperationsAndUnits()
    {
        var scenario=Simple();
        var result=CostEstimator.Estimate(scenario,PricingCatalog.LoadDefault()).basis;
        // 100 users * 2 runs * 30 days: (start2+settle4) reads and (start3+settle4) writes.
        Assert.That(result.usage.reads,Is.EqualTo(36000));Assert.That(result.usage.writes,Is.EqualTo(42000));
        Assert.That(Item(result,"firestore-reads").costUsd,Is.EqualTo(.01368m));
        Assert.That(Item(result,"firestore-writes").costUsd,Is.EqualTo(.0483m));
        Assert.That(result.usage.requests,Is.EqualTo(12000));
        Assert.That(result.usage.cpuSeconds,Is.EqualTo(1200));Assert.That(result.usage.memoryGiBSeconds,Is.EqualTo(600));
        Assert.That(Item(result,"cloud-run-cpu").costUsd,Is.EqualTo(.04032m));
        Assert.That(Item(result,"cloud-run-memory").costUsd,Is.EqualTo(.0021m));
        Assert.That(result.complete,Is.True);
    }

    [Test]
    public void FreeQuotaIsDailyAndCloudRunCreditUsesTierOneMoney()
    {
        var scenario=Simple();scenario.runsPerUserPerDay=0;scenario.dailyActiveUsers=1;
        scenario.profileReadsPerUserPerDay=60000;scenario.applyFreeTier=true;
        var result=CostEstimator.Estimate(scenario,PricingCatalog.LoadDefault()).basis;
        Assert.That(Item(result,"firestore-reads").billableQuantity,Is.EqualTo(300000));
        Assert.That(Item(result,"cloud-run-cpu").costUsd,Is.EqualTo(1.728m)); //180k *.0000336 - 180k *.000024
        Assert.That(Item(result,"cloud-run-requests").costUsd,Is.Zero);
        scenario.profileReadsPerUserPerDay=50000;
        Assert.That(Item(CostEstimator.Estimate(scenario,PricingCatalog.LoadDefault()).basis,"firestore-reads").costUsd,Is.Zero);
    }

    [Test]
    public void StorageIsDailyAverageRetentionAndNotTotalIngestedBytes()
    {
        var scenario=Simple();scenario.dailyActiveUsers=1;scenario.runsPerUserPerDay=1;scenario.days=2;scenario.retentionDays=1;
        scenario.profileBytes=0;scenario.runBytes=0;scenario.receiptBytes=0;scenario.operationBytes=0;scenario.logBytesPerRun=1073741824;
        scenario.indexOverheadFactor=0;
        var usage=CostEstimator.Estimate(scenario,PricingCatalog.LoadDefault()).basis.usage;
        Assert.That(usage.ingestedStorageBytes,Is.EqualTo(2m*1073741824));
        Assert.That(usage.averageStoredGiB,Is.EqualTo(1));Assert.That(usage.storageGiBMonths,Is.EqualTo(2m/30m));
        Assert.That(usage.deletes,Is.EqualTo(4)); // previous day's run, receipt, start and settle operation documents
    }

    [Test]
    public void RetryAddsReadsButNotCommittedWritesAndBandsAreOrdered()
    {
        var s=Simple();var clean=CostEstimator.Estimate(s,PricingCatalog.LoadDefault());
        s.transactionRetryRate=.5m;s.duplicateRate=.5m;s.errorRate=.5m;
        var result=CostEstimator.Estimate(s,PricingCatalog.LoadDefault());
        Assert.That(result.basis.usage.reads,Is.GreaterThan(clean.basis.usage.reads));
        Assert.That(result.minimum.knownSubtotalUsd,Is.LessThanOrEqualTo(result.basis.knownSubtotalUsd));
        Assert.That(result.worst.knownSubtotalUsd,Is.GreaterThanOrEqualTo(result.basis.knownSubtotalUsd));
    }

    [Test]
    public void UnsupportedRegionAndBoundedInputsFailBeforeCalculation()
    {
        var s=Simple();s.region="us-central1";Assert.Throws<ArgumentException>(()=>CostEstimator.Estimate(s,PricingCatalog.LoadDefault()));
        s=Simple();s.days=32;Assert.Throws<ArgumentOutOfRangeException>(()=>CostEstimator.Estimate(s,PricingCatalog.LoadDefault()));
        s=Simple();s.runsPerUserPerDay=decimal.MaxValue;Assert.Throws<ArgumentOutOfRangeException>(()=>CostEstimator.Estimate(s,PricingCatalog.LoadDefault()));
        s=Simple();s.logBytesPerRun=-1;Assert.Throws<ArgumentOutOfRangeException>(()=>CostEstimator.Estimate(s,PricingCatalog.LoadDefault()));
    }

    [Test]
    public void RateFileRejectsBadUnitsAndMissingRateIsUnestimated()
    {
        string source=File.ReadAllText(PricingCatalog.DefaultPath);
        string directory=Path.Combine(Path.GetTempPath(),"dice-pricing-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            string path=Path.Combine(directory,"rates.json");
            File.WriteAllText(path,source.Replace("per-100000-documents","per-million-documents"));
            Assert.Throws<InvalidDataException>(()=>PricingCatalog.Load(path));
            var json=Newtonsoft.Json.Linq.JObject.Parse(source);
            json["rates"]!["firestore-reads"]!.Parent!.Remove();File.WriteAllText(path,json.ToString());
            var estimate=CostEstimator.Estimate(Simple(),PricingCatalog.Load(path));
            Assert.That(estimate.basis.complete,Is.False);Assert.That(estimate.basis.totalUsd,Is.Null);
            Assert.That(Item(estimate.basis,"firestore-reads").status,Is.EqualTo("unestimated"));
        }
        finally { Directory.Delete(directory,true); }
    }

    [Test]
    public void MonthlyStorageAndKoreaEgressUseDifferentFreeAllowances()
    {
        var s=Simple();s.dailyActiveUsers=1;s.runsPerUserPerDay=0;s.days=30;s.profileBytes=0;s.initialStoredGiB=2;s.applyFreeTier=true;
        var stored=CostEstimator.Estimate(s,PricingCatalog.LoadDefault()).basis;
        Assert.That(Item(stored,"firestore-storage").quantity,Is.EqualTo(2));
        Assert.That(Item(stored,"firestore-storage").billableQuantity,Is.EqualTo(1));
        Assert.That(Item(stored,"firestore-storage").costUsd,Is.EqualTo(.192m));
        s.days=1;s.dailyActiveUsers=10241;s.profileReadsPerUserPerDay=1;s.responseBytesPerRequest=1073741824;
        var egress=CostEstimator.Estimate(s,PricingCatalog.LoadDefault()).basis;
        Assert.That(egress.usage.egressGiB,Is.EqualTo(10241));
        Assert.That(Item(egress,"cloud-run-egress").costUsd,Is.EqualTo(1945.75m)); //10240*.19+1*.15; no Firestore10GiB deduction
        Assert.That(Item(egress,"firestore-same-region-egress").costUsd,Is.Zero);
    }

    [Test]
    public void ZeroUsersAndMaximumBoundsRemainFiniteAndInputIsNotMutated()
    {
        var s=Simple();s.dailyActiveUsers=0;
        Assert.That(CostEstimator.Estimate(s,PricingCatalog.LoadDefault()).basis.knownSubtotalUsd,Is.Zero);
        s.dailyActiveUsers=1000000;s.days=31;s.runsPerUserPerDay=100;s.profileReadsPerUserPerDay=100000;
        s.growthPerUserPerDay=100;s.operationReadsPerRun=100;s.runReadsPerRun=100;s.transactionRetryRate=5;
        s.duplicateRate=1;s.errorRate=1;s.indexOverheadFactor=10;s.initialStoredGiB=1000000000;
        s.profileBytes=s.runBytes=s.receiptBytes=s.operationBytes=s.logBytesPerRun=s.responseBytesPerRequest=1073741824;
        s.cpuSecondsPerRequest=3600;s.cpuCount=8;s.memoryGiB=32;s.retentionDays=365;
        var before=Newtonsoft.Json.JsonConvert.SerializeObject(s);
        var estimate=CostEstimator.Estimate(s,PricingCatalog.LoadDefault());
        Assert.That(estimate.worst.totalUsd,Is.GreaterThan(0));
        Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(estimate),Does.Not.Contain("NaN"));
        Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(s),Is.EqualTo(before));
        estimate.scenario.dailyActiveUsers=1;Assert.That(s.dailyActiveUsers,Is.EqualTo(1000000));
    }

    [TestCase("negative")][TestCase("duplicate")][TestCase("nan")][TestCase("unsupported")][TestCase("missing")]
    public void InvalidCatalogCannotSilentlyBecomeFree(string fault)
    {
        string source=File.ReadAllText(PricingCatalog.DefaultPath),path=Path.Combine(Path.GetTempPath(),"dice-bad-price-"+Guid.NewGuid().ToString("N")+".json");
        source=fault switch {
            "negative"=>source.Replace("\"usd\": 0.038","\"usd\": -0.038"),
            "duplicate"=>source.Replace("\"usd\": 0.038","\"usd\": 0.038, \"usd\": 0"),
            "nan"=>source.Replace("\"usd\": 0.038","\"usd\": NaN"),
            "unsupported"=>source.Replace("\"edition\": \"Standard\"","\"edition\": \"Enterprise\""),
            _=>source.Replace("\"usd\": 0.038,","")
        };
        try {File.WriteAllText(path,source);Assert.Throws<InvalidDataException>(()=>PricingCatalog.Load(path));}
        finally {File.Delete(path);}
    }

    [Test]
    public void PublishedRatesAreConfigurationAndNotBakedIntoArithmetic()
    {
        var source=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(PricingCatalog.DefaultPath));
        source["rates"]!["firestore-reads"]!["usd"]=1;
        string path=Path.Combine(Path.GetTempPath(),"dice-fixture-price-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            File.WriteAllText(path,source.ToString());
            Assert.That(Item(CostEstimator.Estimate(Simple(),PricingCatalog.Load(path)).basis,"firestore-reads").costUsd,Is.EqualTo(.36m));
        }
        finally {File.Delete(path);}
    }

    static CostScenario Simple()=>new(){dailyActiveUsers=100,runsPerUserPerDay=2,days=30,profileReadsPerUserPerDay=0,
        operationReadsPerRun=0,runReadsPerRun=0,growthPerUserPerDay=0,duplicateRate=0,errorRate=0,transactionRetryRate=0};
    static CostLineItem Item(CostVariant result,string key)=>result.lineItems.Single(item=>item.key==key);
}
