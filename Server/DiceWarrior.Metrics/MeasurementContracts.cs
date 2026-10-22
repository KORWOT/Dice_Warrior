namespace DiceWarrior.Metrics;

public sealed class DiagnosticTotal
{
    public long count;
    public double milliseconds;
}

public sealed class RequestMeasurement
{
    public string id=Guid.NewGuid().ToString("N");
    public string endpoint="unknown";
    public DateTimeOffset startedUtc=DateTimeOffset.UtcNow;
    public double durationMs;
    public long requestBytes,responseBytes,logBytes;
    public int statusCode,commandCount;
    public string outcome="invalid-request",authorityOutcome="none",domainCode="";
    public bool responseComplete;
    public string? userKey,runKey;
    public Dictionary<string,DiagnosticTotal> diagnostics=new(StringComparer.Ordinal);
}

public sealed class MetricsOptions
{
    public int retentionDays=7,maxRecords=10000,maxJournalFiles=16;
    public long maxJournalBytes=4*1024*1024;
}

public sealed class MetricsSnapshot
{
    public DateTimeOffset generatedUtc=DateTimeOffset.UtcNow;
    public string measurementScope="retained local journals; process gauges are current host only";
    public string privacy="local HMAC pseudonyms; no raw identity or request payload";
    public string cpuScope="process CPU since collector start; not isolated replay CPU";
    public List<RequestMeasurement> records=new();
    public long completedRequests,droppedRecords,invalidRecords;
    public int activeRequests,peakRequests;
    public bool truncated,persistenceAvailable;
    public int retentionDays,maxRecords,maxJournalFiles;
    public long maxJournalBytes,authorityDatabaseBytes,metricsBytes,processMemoryBytes;
    public double processCpuMilliseconds;
}

public sealed class MeasurementAggregate
{
    public string key="";
    public long requests,failures,duplicates,requestBytes,responseBytes,commandCount,logBytes;
    public int activeUsers;
    public double averageMs,p50Ms,p95Ms,p99Ms,maxMs;
    public Dictionary<string,DiagnosticTotal> diagnostics=new(StringComparer.Ordinal);
}
