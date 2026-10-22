namespace DiceWarrior.Metrics;

public sealed class OperationEstimate
{
    public string modelVersion=FirebaseOperationModel.Version,classification="";
    public bool supported=true,isActualFirestoreUsage;
    public long reads,writes,deletes,indexEntryReads,queryMinimumReads,transactionAttempts,storageBytes,egressBytes;
    public string[] assumptions=Array.Empty<string>();
}

public static class FirebaseOperationModel
{
    public const string Version="firestore-logical-plan-1";
    public static OperationEstimate Describe(RequestMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        var result=new OperationEstimate {egressBytes=Math.Max(0,measurement.responseBytes),classification="before-authority"};
        var assumptions=new List<string> {
            "Hypothetical document plan, not Firebase usage. SQL statements and SQLite CAS retries do not become document reads.",
            "Direct document keys; no queries, listeners, index-entry scans or Security Rules dependent reads. Profile already provisioned.",
            "storageBytes estimates newly retained payload bytes, excluding indexes; egressBytes is observed HTTP body output, not billed network bytes."
        };
        if(measurement.authorityOutcome is "none" or "") return Finish(result,assumptions);
        if(measurement.authorityOutcome is "temporary-failure" or "unknown")
        {
            result.supported=false;result.classification="unknown-authority-outcome";
            assumptions.Add("Authority failed without a known commit outcome. Zero counters are placeholders; Firestore use is unestimated.");
            return Finish(result,assumptions);
        }
        if(measurement.endpoint is "profile" or "operation" or "run")
        {
            result.classification="document-lookup";result.reads=measurement.endpoint=="run"?2:1;
            assumptions.Add("Profile/operation: one document lookup. Run: run plus optional receipt lookup. Missing documents still cost reads.");
            return Finish(result,assumptions);
        }
        if(measurement.endpoint is not ("start" or "settle" or "grow"))
        { result.supported=false;result.classification="unsupported-endpoint";return Finish(result,assumptions); }
        if(measurement.domainCode is "INVALID_REQUEST_ID" or "COMMAND_LIMIT" or "REQUEST_TOO_LARGE")
        { result.classification="authority-input-rejection";return Finish(result,assumptions); }
        if(Has(measurement,"duplicate-operation") || measurement.domainCode=="REQUEST_ID_REUSED")
        {
            result.classification="duplicate-operation";result.reads=1;
            assumptions.Add("Read stored operation receipt only. No replay, reward or newly retained data.");
            return Finish(result,assumptions);
        }
        result.transactionAttempts=1;
        if(Has(measurement,"duplicate-submission"))
        {
            result.classification="duplicate-submission";result.reads=3;result.writes=1;result.storageBytes=1024;
            assumptions.Add("New request ID for an already submitted run: read operation/run/receipt, retain one new operation receipt; no reward write.");
            return Finish(result,assumptions);
        }
        if(measurement.domainCode=="OPERATION_LIMIT")
        { result.classification="operation-cap-rejection";result.reads=1;result.transactionAttempts=0;return Finish(result,assumptions); }
        result.reads=measurement.endpoint=="settle"?4:2;
        if(measurement.authorityOutcome=="approved")
        {
            result.classification="new-approved";
            result.writes=measurement.endpoint switch {"start"=>3,"settle"=>4,_=>2};
            result.storageBytes=measurement.endpoint switch {"start"=>3072,"settle"=>checked(2048+Math.Clamp(measurement.logBytes,0,long.MaxValue-2048)),_=>1024};
            assumptions.Add("Start writes profile/run/operation. Settle writes profile/run/receipt/operation; command log stored in run. Growth writes profile/operation.");
        }
        else if(measurement.authorityOutcome is "rejected" or "conflict")
        {
            result.classification="validation-rejection";result.writes=1;result.storageBytes=1024;
            assumptions.Add("Failed validation/revision retains a single operation receipt, without profile reward mutation.");
        }
        else { result.supported=false;result.classification="unknown-authority-outcome"; }
        if(Has(measurement,"cas-retry")) assumptions.Add("Observed SQLite CAS retries are not applied here. Cost scenarios separately assume Firestore transaction read amplification.");
        return Finish(result,assumptions);
    }
    static bool Has(RequestMeasurement measurement,string key)=>measurement.diagnostics.TryGetValue(key,out var value) && value.count>0;
    static OperationEstimate Finish(OperationEstimate result,List<string> assumptions) {result.assumptions=assumptions.ToArray();return result;}
}
