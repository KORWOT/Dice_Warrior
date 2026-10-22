namespace DiceWarrior.Metrics;

public sealed class CostScenario
{
    public string region="asia-northeast3",edition="Standard",currency="USD";
    public bool applyFreeTier;
    public int dailyActiveUsers=1000,days=30,retentionDays=30;
    public decimal runsPerUserPerDay=3,profileReadsPerUserPerDay=2,operationReadsPerRun=.1m,runReadsPerRun=.1m,growthPerUserPerDay=.1m;
    public decimal duplicateRate=.05m,errorRate=.02m,transactionRetryRate=.05m;
    public decimal preAuthorityErrorFraction=.5m,duplicateSubmissionFraction=.25m;
    public long profileBytes=2048,runBytes=2048,receiptBytes=1024,operationBytes=1024,logBytesPerRun=8192,responseBytesPerRequest=4096;
    public decimal indexOverheadFactor=.5m,initialStoredGiB,cpuSecondsPerRequest=.1m,memoryGiB=.5m,cpuCount=1;
}

public sealed class CostUsage
{
    public decimal reads,writes,deletes,indexEntryReads,queryMinimumReads,transactionAttempts,requests,runs;
    public decimal ingestedStorageBytes,averageStoredGiB,storageGiBMonths,egressGiB,cpuSeconds,memoryGiBSeconds;
}

public sealed class CostLineItem
{
    public string key="",unit="",status="estimated",note="";
    public decimal quantity,billableQuantity,freeCreditUsd;
    public decimal? costUsd;
}

public sealed class CostVariant
{
    public string name="";
    public CostUsage usage=new();
    public List<CostLineItem> lineItems=new();
    public decimal knownSubtotalUsd;
    public decimal? totalUsd;
    public bool complete;
}

public sealed class CostEstimate
{
    public CostScenario scenario=new();
    public string modelVersion=FirebaseOperationModel.Version,catalogVersion="";
    public bool isActualBilling;
    public CostVariant minimum=new(),basis=new(),worst=new();
    public string[] assumptions=Array.Empty<string>(),excludedCosts=Array.Empty<string>();
}

public static class CostEstimator
{
    const decimal GiB=1073741824m;

    public static CostEstimate Estimate(CostScenario scenario,PricingCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(scenario);ArgumentNullException.ThrowIfNull(catalog);
        Validate(scenario,catalog);
        return new CostEstimate {
            scenario=Newtonsoft.Json.JsonConvert.DeserializeObject<CostScenario>(Newtonsoft.Json.JsonConvert.SerializeObject(scenario))!,
            catalogVersion=catalog.version,
            minimum=Calculate(scenario,catalog,"minimum",0,.5m),basis=Calculate(scenario,catalog,"basis",1,1),worst=Calculate(scenario,catalog,"worst",2,2),
            assumptions=new[] {
                "Offline scenario, not actual Firebase billing or a cloud performance benchmark. DAU and traffic are constant across each requested day.",
                "Minimum removes duplicate/error/retry traffic and halves payload/compute assumptions. Basis uses input. Worst doubles those assumptions (error/duplicate rates cap at 1). These are planning bands, not statistical confidence intervals.",
                "Each completed run starts once and submits once. Direct reads, growth, duplicate requests and rejected requests are additive. Operation model assumes existing profiles and no authentication billable operations.",
                "A same-ID duplicate reads one operation receipt. The duplicateSubmissionFraction of duplicate settle calls uses a new ID, reads three documents and writes one operation receipt. Errors split by preAuthorityErrorFraction; only authority errors read documents and write rejection receipts.",
                "TransactionRetryRate is assumed additional attempts per transaction: reads and compute amplify; committed writes do not. It is not derived from SQLite CAS or SQL counts.",
                "Command logs are embedded in run documents. Retention assumes explicit deletion of expired run/receipt/operation documents after retentionDays, not Firestore TTL. Profile count assumes fixed DAU cohort; newly retained bytes exclude overwritten profiles.",
                "Storage sums daily retained bytes including index overhead and divides by the catalog planning month length. initialStoredGiB remains throughout the period; it is already billed-size data. Daily accumulation begins from an empty run/operation history.",
                "Cloud Run request billing assumes one request per instance at a time, no minimum instances; cpuSecondsPerRequest is assumed active wall time, rounded up to 100 ms. Memory is allocated GiB multiplied by active seconds. Local process CPU is not substituted.",
                "Egress is Cloud Run Premium Seoul to Korea, once per response. Firestore-to-server stays within the same region. HTTP headers/TLS and compression are excluded from the payload estimate.",
                "Optional free Firestore read/write/delete quotas apply separately each day, storage daily. Cloud Run monthly discounts use Tier 1 USD spending credits. No other projects consume these assumed unused allowances; the scenario fits within one billing month. Firestore's 10 GiB allowance does not discount Cloud Run internet traffic."
            },
            excludedCosts=new[] {"Firebase Authentication and phone/SMS fees","Cloud Logging/Monitoring ingestion and retention","Cloud Build, Artifact Registry and deployment artifacts",
                "Backups, PITR, restore, clones and TTL deletes","Cloud Storage object storage/operations","Security Rules reads, queries/listeners/index-entry scans",
                "Cold starts, idle/minimum instances, network headers, load balancers and VPC connectors","Taxes, exchange rates, commitments and negotiated discounts"}
        };
    }

    static CostVariant Calculate(CostScenario s,PricingCatalog p,string name,decimal riskMultiplier,decimal sizeMultiplier)
    {
        decimal runs=s.dailyActiveUsers*s.runsPerUserPerDay,growth=s.dailyActiveUsers*s.growthPerUserPerDay,mutations=2*runs+growth;
        decimal duplicateRate=Math.Min(1,s.duplicateRate*riskMultiplier),errorRate=Math.Min(1,s.errorRate*riskMultiplier),retry=s.transactionRetryRate*riskMultiplier;
        decimal duplicateSubmission=runs*duplicateRate*s.duplicateSubmissionFraction;
        decimal duplicateOperation=mutations*duplicateRate-duplicateSubmission;
        decimal errors=mutations*errorRate,authorityErrors=errors*(1-s.preAuthorityErrorFraction);
        decimal transactionReads=6*runs+2*growth;
        decimal errorReads=mutations==0?0:authorityErrors*transactionReads/mutations;
        decimal directRequests=s.dailyActiveUsers*s.profileReadsPerUserPerDay+runs*(s.operationReadsPerRun+s.runReadsPerRun);
        decimal directReads=directRequests+runs*s.runReadsPerRun;
        decimal dailyReads=directReads+duplicateOperation+(transactionReads+3*duplicateSubmission+errorReads)*(1+retry);
        decimal dailyWrites=7*runs+2*growth+duplicateSubmission+authorityErrors;
        decimal dailyRequests=mutations+directRequests+duplicateOperation+duplicateSubmission+errors;
        decimal newOperations=mutations+duplicateSubmission+authorityErrors;
        decimal dailyDocuments=2*runs+newOperations;
        decimal dailyPayload=(runs*(s.runBytes+(decimal)s.receiptBytes+s.logBytesPerRun)+newOperations*s.operationBytes)*sizeMultiplier;
        decimal indexFactor=1+s.indexOverheadFactor*sizeMultiplier;
        decimal persistentGiB=s.initialStoredGiB+s.dailyActiveUsers*s.profileBytes*sizeMultiplier*indexFactor/GiB;
        decimal storedGiBDays=0,billableStoredGiBDays=0;
        var free=p.freeQuota;
        for(int day=1;day<=s.days;day++)
        {
            decimal stored=persistentGiB+dailyPayload*Math.Min(day,s.retentionDays)*indexFactor/GiB;
            storedGiBDays+=stored;billableStoredGiBDays+=Math.Max(0,stored-(s.applyFreeTier?free.firestoreStorageGiB:0));
        }
        decimal cleanupDays=Math.Max(0,s.days-s.retentionDays),deletes=dailyDocuments*cleanupDays;
        decimal requestActiveSeconds=decimal.Ceiling(s.cpuSecondsPerRequest*sizeMultiplier*10)/10;
        // Retried authority transactions consume additional active time, not extra HTTP request charges.
        decimal activeSeconds=requestActiveSeconds*(dailyRequests+(mutations+duplicateSubmission+authorityErrors)*retry)*s.days;
        var result=new CostVariant {name=name,usage=new() {
            reads=dailyReads*s.days,writes=dailyWrites*s.days,deletes=deletes,requests=dailyRequests*s.days,runs=runs*s.days,
            transactionAttempts=(mutations+duplicateSubmission+authorityErrors)*(1+retry)*s.days,
            ingestedStorageBytes=dailyPayload*s.days,averageStoredGiB=storedGiBDays/s.days,storageGiBMonths=storedGiBDays/p.storageMonthDays,
            egressGiB=dailyRequests*s.days*s.responseBytesPerRequest*sizeMultiplier/GiB,cpuSeconds=activeSeconds*s.cpuCount,memoryGiBSeconds=activeSeconds*s.memoryGiB
        }};
        var u=result.usage;
        Add(result,p,"firestore-reads",u.reads,Math.Max(0,dailyReads-(s.applyFreeTier?free.firestoreReadsPerDay:0))*s.days,100000);
        Add(result,p,"firestore-writes",u.writes,Math.Max(0,dailyWrites-(s.applyFreeTier?free.firestoreWritesPerDay:0))*s.days,100000);
        Add(result,p,"firestore-deletes",u.deletes,Math.Max(0,dailyDocuments-(s.applyFreeTier?free.firestoreDeletesPerDay:0))*cleanupDays,100000);
        Add(result,p,"firestore-storage",u.storageGiBMonths,billableStoredGiBDays/p.storageMonthDays,1);
        Add(result,p,"firestore-same-region-egress",u.egressGiB,u.egressGiB,1,note:"Same-region server transfer assumption; response size is a planning proxy, not measured Firestore bytes.");
        Add(result,p,"cloud-run-cpu",u.cpuSeconds,u.cpuSeconds,1,s.applyFreeTier?free.cloudRunCpuCreditUsdPerMonth:0);
        Add(result,p,"cloud-run-memory",u.memoryGiBSeconds,u.memoryGiBSeconds,1,s.applyFreeTier?free.cloudRunMemoryCreditUsdPerMonth:0);
        Add(result,p,"cloud-run-requests",u.requests,u.requests,1000000,s.applyFreeTier?free.cloudRunRequestCreditUsdPerMonth:0);
        Add(result,p,"cloud-run-egress",u.egressGiB,u.egressGiB,1,note:"Seoul to Korea Premium internet transfer; no Firestore allowance applied.");
        result.knownSubtotalUsd=result.lineItems.Sum(item=>item.costUsd??0);result.complete=result.lineItems.All(item=>item.costUsd.HasValue);
        result.totalUsd=result.complete?result.knownSubtotalUsd:null;return result;
    }

    static void Add(CostVariant result,PricingCatalog catalog,string key,decimal quantity,decimal billable,decimal denominator,decimal credit=0,string note="")
    {
        var item=new CostLineItem {key=key,quantity=quantity,billableQuantity=billable,note=note};
        if(!catalog.rates.TryGetValue(key,out var price)) {item.status="unestimated";item.note="Missing pricing rate; not zero cost.";result.lineItems.Add(item);return;}
        item.unit=price.unit;
        decimal gross=0;
        if(price.usd.HasValue) gross=billable/denominator*price.usd.Value;
        else
        {
            decimal previous=0;
            foreach(var tier in price.tiers)
            {
                decimal amount=Math.Max(0,Math.Min(billable,tier.upTo??billable)-previous);
                gross+=amount*tier.usd;previous=tier.upTo??billable;
                if(previous>=billable) break;
            }
        }
        item.freeCreditUsd=Math.Min(gross,credit);item.costUsd=gross-item.freeCreditUsd;result.lineItems.Add(item);
    }

    static void Validate(CostScenario s,PricingCatalog p)
    {
        if(s.region!=p.region || s.edition!=p.edition || s.currency!=p.currency) throw new ArgumentException("Unsupported region, edition or currency; no fallback tariff.");
        Bound(s.dailyActiveUsers,0,1000000,nameof(s.dailyActiveUsers));Bound(s.days,1,31,nameof(s.days));Bound(s.retentionDays,1,365,nameof(s.retentionDays));
        Bound(s.runsPerUserPerDay,0,100,nameof(s.runsPerUserPerDay));Bound(s.growthPerUserPerDay,0,100,nameof(s.growthPerUserPerDay));
        Bound(s.profileReadsPerUserPerDay,0,100000,nameof(s.profileReadsPerUserPerDay));Bound(s.operationReadsPerRun,0,100,nameof(s.operationReadsPerRun));Bound(s.runReadsPerRun,0,100,nameof(s.runReadsPerRun));
        Bound(s.duplicateRate,0,1,nameof(s.duplicateRate));Bound(s.errorRate,0,1,nameof(s.errorRate));Bound(s.transactionRetryRate,0,5,nameof(s.transactionRetryRate));
        Bound(s.preAuthorityErrorFraction,0,1,nameof(s.preAuthorityErrorFraction));Bound(s.duplicateSubmissionFraction,0,1,nameof(s.duplicateSubmissionFraction));
        foreach(var bytes in new[]{s.profileBytes,s.runBytes,s.receiptBytes,s.operationBytes,s.logBytesPerRun,s.responseBytesPerRequest}) Bound(bytes,0,1073741824,"byte assumption");
        Bound(s.indexOverheadFactor,0,10,nameof(s.indexOverheadFactor));Bound(s.initialStoredGiB,0,1000000000,nameof(s.initialStoredGiB));
        Bound(s.cpuSecondsPerRequest,0,3600,nameof(s.cpuSecondsPerRequest));Bound(s.memoryGiB,.125m,32,nameof(s.memoryGiB));Bound(s.cpuCount,.08m,8,nameof(s.cpuCount));
    }
    static void Bound(decimal value,decimal min,decimal max,string name) {if(value<min || value>max) throw new ArgumentOutOfRangeException(name,$"Expected {min}..{max}.");}
}
