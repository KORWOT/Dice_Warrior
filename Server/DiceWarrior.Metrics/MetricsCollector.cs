using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.Metrics;

public sealed class MetricsCollector
{
    const int MaxRecordBytes=8192;
    static readonly JsonSerializerOptions Json=new() {IncludeFields=true,MaxDepth=8,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    internal static readonly HashSet<string> Kinds=new("sql-select sql-insert sql-update sql-delete sql-other sql-rows transaction-begin transaction-commit transaction-rollback cas-retry sql-busy replay duplicate-operation duplicate-submission".Split(' '),StringComparer.Ordinal);
    static readonly HashSet<string> Endpoints=new("profile start settle grow operation run unknown".Split(' '),StringComparer.Ordinal);
    static readonly HashSet<string> Outcomes=new("approved rejected conflict temporary-failure transport-error invalid-request".Split(' '),StringComparer.Ordinal);
    static readonly HashSet<string> AuthorityOutcomes=new("none approved rejected conflict temporary-failure".Split(' '),StringComparer.Ordinal);
    static readonly HashSet<string> Codes=new(("PROFILE OPERATION_NOT_FOUND RUN_NOT_FOUND RUN_ACTIVE RUN_RECEIPT INVALID_REQUEST_ID COMMAND_LIMIT REQUEST_TOO_LARGE REQUEST_ID_REUSED OPERATION_LIMIT VERSION_MISMATCH WRONG_OPERATION RUN_LIMIT INVALID_LOADOUT RUN_STARTED INVALID_UPGRADE GROWTH_LIMIT INSUFFICIENT_CURRENCY UPGRADED RUN_VERIFIED PENDING_VERIFICATION RUN_NOT_ACTIVE INVALID_COMMANDS ABANDON_RATE_LIMIT CURRENCY_LIMIT START_RATE_LIMIT SUBMIT_RATE_LIMIT REVISION_CONFLICT REVISION_LIMIT AUTHORITY_STORAGE_UNAVAILABLE REPLAY_TICKET_INVALID REPLAY_RULES_UNSUPPORTED REPLAY_LOG_EMPTY REPLAY_LIMIT_EXCEEDED REPLAY_COMMAND_INVALID REPLAY_SEQUENCE_INVALID REPLAY_COMMAND_ID_DUPLICATE REPLAY_ALREADY_FINISHED REPLAY_TARGET_MISMATCH REPLAY_STATE_INVALID REPLAY_INCOMPLETE REPLAY_VERIFIED DEVELOPMENT_IDENTITY_REQUIRED INVALID_ROUTE_ID JSON_REQUIRED INVALID_JSON AUTHORITY_UNAVAILABLE").Split(' '),StringComparer.Ordinal);
    readonly string directory,authorityPath,identity=Guid.NewGuid().ToString("N");
    readonly MetricsOptions options;
    readonly object gate=new();
    readonly TimeSpan initialCpu;
    byte[]? secret;
    string? currentJournal;
    long failedCompletions,failedDrops;
    int active,peak;
    bool available;

    public MetricsCollector(string directory,string authorityDatabasePath,MetricsOptions? options=null)
    {
        this.directory=directory; authorityPath=authorityDatabasePath;
        var requested=options??new(); this.options=new MetricsOptions {retentionDays=Math.Clamp(requested.retentionDays,1,7),
            maxRecords=Math.Clamp(requested.maxRecords,1,10000),maxJournalFiles=Math.Clamp(requested.maxJournalFiles,1,16),maxJournalBytes=Math.Clamp(requested.maxJournalBytes,256,4*1024*1024)};
        try { using var process=Process.GetCurrentProcess(); initialCpu=process.TotalProcessorTime; } catch { }
        try { Directory.CreateDirectory(directory); using var lease=Acquire(); secret=LoadSecret(); available=true; }
        catch(Exception) { available=false; }
    }

    public RequestMeasurementScope Begin(string endpoint)
    {
        int count=Interlocked.Increment(ref active),prior;
        do {prior=Volatile.Read(ref peak); if(prior>=count)break;} while(Interlocked.CompareExchange(ref peak,count,prior)!=prior);
        return new RequestMeasurementScope(this,Endpoints.Contains(endpoint)?endpoint:"unknown");
    }

    internal void Identify(RequestMeasurement measurement,string? user,string? run)
    {
        if(secret==null)return;
        if(!string.IsNullOrEmpty(user)) measurement.userKey=Opaque("user\n"+user);
        // A run pseudonym is scoped to its owner; equal run IDs in different users cannot merge.
        if(!string.IsNullOrEmpty(user)&&!string.IsNullOrEmpty(run)) measurement.runKey=Opaque("run\n"+user+"\n"+run);
    }
    string Opaque(string value) => Convert.ToHexString(HMACSHA256.HashData(secret!,Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal void Complete(RequestMeasurement measurement)
    {
        Interlocked.Decrement(ref active);
        bool appended=false;
        try
        {
            lock(gate)
            {
                Directory.CreateDirectory(directory); using var lease=Acquire(); secret??=LoadSecret();
                var state=ReadState(); state.completed++; Sanitize(measurement);
                var bytes=JsonSerializer.SerializeToUtf8Bytes(measurement,Json);
                Trim(state);
                if(measurement.startedUtc<DateTimeOffset.UtcNow.AddDays(-options.retentionDays)||bytes.Length+1>Math.Min(MaxRecordBytes,options.maxJournalBytes)) state.dropped++;
                else
                {
                    if(currentJournal==null||!File.Exists(currentJournal)||new FileInfo(currentJournal).Length+bytes.Length+1>options.maxJournalBytes)
                        currentJournal=Path.Combine(directory,"m-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")+"-"+identity+"-"+Guid.NewGuid().ToString("N")+".jsonl");
                    using(var stream=new FileStream(currentJournal,FileMode.Append,FileAccess.Write,FileShare.Read)) {stream.Write(bytes); stream.WriteByte((byte)'\n'); stream.Flush(true);}
                    appended=true;
                    Trim(state);
                }
                SaveState(state); available=true;
            }
        }
        catch(Exception) { if(!appended) {Interlocked.Increment(ref failedCompletions);Interlocked.Increment(ref failedDrops);} available=false; }
    }

    public MetricsSnapshot Snapshot()
    {
        var snapshot=new MetricsSnapshot {activeRequests=Volatile.Read(ref active),peakRequests=Volatile.Read(ref peak),retentionDays=options.retentionDays,
            maxRecords=options.maxRecords,maxJournalFiles=options.maxJournalFiles,maxJournalBytes=options.maxJournalBytes};
        try { using var p=Process.GetCurrentProcess(); snapshot.processMemoryBytes=p.WorkingSet64; snapshot.processCpuMilliseconds=Math.Max(0,(p.TotalProcessorTime-initialCpu).TotalMilliseconds); } catch(Exception) { }
        snapshot.authorityDatabaseBytes=FileSize(authorityPath)+FileSize(authorityPath+"-wal")+FileSize(authorityPath+"-shm");
        try
        {
            lock(gate)
            {
                using var lease=Acquire(); secret??=LoadSecret(); var state=ReadState(); Trim(state); SaveState(state);
                snapshot.completedRequests=state.completed; snapshot.droppedRecords=state.dropped; snapshot.invalidRecords=state.invalid;
                snapshot.truncated=state.truncated||state.dropped>0||state.invalid>0;
                var newest=new PriorityQueue<RequestMeasurement,long>(); long retained=0;
                foreach(var path in Journals())
                {
                    foreach(var record in ReadJournal(path,()=>{snapshot.invalidRecords++;snapshot.truncated=true;}))
                    {
                        if(record.startedUtc<DateTimeOffset.UtcNow.AddDays(-options.retentionDays)) {snapshot.droppedRecords++;snapshot.truncated=true;continue;}
                        retained++; newest.Enqueue(record,record.startedUtc.UtcTicks);
                        if(newest.Count>options.maxRecords)newest.Dequeue();
                    }
                }
                snapshot.records=newest.UnorderedItems.Select(x=>x.Element).OrderBy(r=>r.startedUtc).ToList();
                snapshot.completedRequests=Math.Max(snapshot.completedRequests,retained+state.dropped);
                if(retained>options.maxRecords) {snapshot.droppedRecords+=retained-options.maxRecords;snapshot.truncated=true;}
                snapshot.metricsBytes=Journals().Sum(FileSize)+FileSize(Path.Combine(directory,"identity.key"))+FileSize(Path.Combine(directory,"retention.json"));
                available=true;
            }
        }
        catch(Exception) { available=false; snapshot.truncated=true; }
        snapshot.completedRequests+=Interlocked.Read(ref failedCompletions); snapshot.droppedRecords+=Interlocked.Read(ref failedDrops);
        snapshot.persistenceAvailable=available; snapshot.truncated|=snapshot.droppedRecords>0;
        return snapshot;
    }

    FileStream Acquire()
    {
        // A short bounded wait serializes independent processes without sharing authority locks.
        for(int i=0;;i++)
        {
            try {return new FileStream(Path.Combine(directory,"metrics.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(i<39) {Thread.Sleep(5);}
        }
    }
    byte[] LoadSecret()
    {
        var path=Path.Combine(directory,"identity.key");
        if(File.Exists(path)) {var bytes=ReadBounded(path,32);if(bytes.Length!=32)throw new IOException("Invalid metrics identity key");return bytes;}
        var value=RandomNumberGenerator.GetBytes(32);
        using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {file.Write(value);file.Flush(true);}
        return value;
    }
    sealed class RetentionState {public long completed,dropped,invalid;public bool truncated;}
    RetentionState ReadState()
    {
        var path=Path.Combine(directory,"retention.json"); if(!File.Exists(path))return new();
        try
        {
            if(FileSize(path)>1024)throw new IOException();
            var state=JsonSerializer.Deserialize<RetentionState>(ReadBounded(path,1024),Json)??throw new IOException();
            if(state.completed<0||state.dropped<0||state.invalid<0||state.completed>1_000_000_000_000||state.dropped>1_000_000_000_000||state.invalid>1_000_000_000_000)throw new IOException();
            return state;
        }
        catch(Exception) {return new RetentionState {invalid=1,truncated=true};}
    }
    void SaveState(RetentionState state)
    {
        string path=Path.Combine(directory,"retention.json"),temp=Path.Combine(directory,"retention-"+identity+".tmp");
        try {using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {stream.Write(JsonSerializer.SerializeToUtf8Bytes(state,Json));stream.Flush(true);} File.Move(temp,path,true);}
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    IEnumerable<string> Journals() => Directory.EnumerateFiles(directory,"m-*.jsonl",SearchOption.TopDirectoryOnly).OrderBy(p=>p,StringComparer.Ordinal);
    void Trim(RetentionState state)
    {
        var files=Journals().ToList(); var cutoff=DateTime.UtcNow.AddDays(-options.retentionDays);
        foreach(var path in files.ToArray())
        {
            if(File.GetCreationTimeUtc(path)>=cutoff&&FileSize(path)<=options.maxJournalBytes)continue;
            Remove(path,state); files.Remove(path);
        }
        while(files.Count>options.maxJournalFiles) {Remove(files[0],state);files.RemoveAt(0);}
    }
    void Remove(string path,RetentionState state)
    {
        state.dropped+=ReadJournal(path,()=>state.invalid++).LongCount(); state.truncated=true; File.Delete(path);
    }
    IEnumerable<RequestMeasurement> ReadJournal(string path,Action invalid)
    {
        if(FileSize(path)>options.maxJournalBytes) {invalid();yield break;}
        byte[] bytes;
        try {bytes=ReadBounded(path,(int)options.maxJournalBytes);}
        catch(Exception) {invalid();yield break;}
        int start=0;
        for(int end=0;end<bytes.Length;end++)
        {
            if(bytes[end]!='\n')continue;
            int length=end-start; RequestMeasurement? record=null;
            try {if(length<=0||length>MaxRecordBytes)throw new IOException();record=JsonSerializer.Deserialize<RequestMeasurement>(bytes.AsSpan(start,length),Json);if(record==null||!Valid(record))record=null;}
            catch(Exception) { }
            if(record==null)invalid();else yield return record; start=end+1;
        }
        if(start!=bytes.Length)invalid();
    }
    static long FileSize(string path) {try {return new FileInfo(path).Length;}catch(Exception){return 0;}}
    static byte[] ReadBounded(string path,int limit)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        using var output=new MemoryStream(); var buffer=new byte[Math.Min(8192,limit+1)];
        while(true) {int count=stream.Read(buffer,0,Math.Min(buffer.Length,limit+1-(int)output.Length));if(count==0)return output.ToArray();output.Write(buffer,0,count);if(output.Length>limit)throw new IOException("Metrics file exceeds its limit");}
    }
    static bool Key(string? value) => value==null||(value.Length==64&&value.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f'));
    static bool Finite(double value) => double.IsFinite(value)&&value>=0&&value<=1e12;
    static bool Valid(RequestMeasurement m) => Guid.TryParseExact(m.id,"N",out _)&&Endpoints.Contains(m.endpoint)&&Outcomes.Contains(m.outcome)&&AuthorityOutcomes.Contains(m.authorityOutcome)
        &&(m.domainCode==""||Codes.Contains(m.domainCode))&&Key(m.userKey)&&Key(m.runKey)&&Finite(m.durationMs)&&m.startedUtc>=new DateTimeOffset(2000,1,1,0,0,0,TimeSpan.Zero)&&m.startedUtc<=DateTimeOffset.UtcNow.AddDays(1)
        &&m.requestBytes is >=0 and <=1_000_000_000&&m.responseBytes is >=0 and <=1_000_000_000&&m.logBytes is >=0 and <=1_000_000_000&&m.commandCount is >=0 and <=1_000_000&&m.statusCode is >=0 and <=599
        &&m.diagnostics!=null&&m.diagnostics.Count<=Kinds.Count&&m.diagnostics.All(p=>Kinds.Contains(p.Key)&&p.Value!=null&&p.Value.count is >=0 and <=1_000_000_000&&Finite(p.Value.milliseconds));
    static void Sanitize(RequestMeasurement m)
    {
        if(!Guid.TryParseExact(m.id,"N",out _))m.id=Guid.NewGuid().ToString("N");
        if(!Endpoints.Contains(m.endpoint))m.endpoint="unknown"; if(!Outcomes.Contains(m.outcome))m.outcome="invalid-request";
        if(!AuthorityOutcomes.Contains(m.authorityOutcome))m.authorityOutcome="none"; if(m.domainCode==null||!Codes.Contains(m.domainCode))m.domainCode="";
        if(!Key(m.userKey))m.userKey=null;if(!Key(m.runKey))m.runKey=null;
        if(!Finite(m.durationMs))m.durationMs=0;
        m.requestBytes=Math.Clamp(m.requestBytes,0,1_000_000_000);m.responseBytes=Math.Clamp(m.responseBytes,0,1_000_000_000);m.logBytes=Math.Clamp(m.logBytes,0,1_000_000_000);
        m.commandCount=Math.Clamp(m.commandCount,0,1_000_000);m.statusCode=Math.Clamp(m.statusCode,0,599);
        m.diagnostics=(m.diagnostics??new()).Where(p=>Kinds.Contains(p.Key)&&p.Value!=null&&p.Value.count>=0&&Finite(p.Value.milliseconds)).ToDictionary(p=>p.Key,p=>new DiagnosticTotal {count=Math.Min(1_000_000_000,p.Value.count),milliseconds=p.Value.milliseconds},StringComparer.Ordinal);
    }
}

public sealed class RequestMeasurementScope : IAuthorityTelemetry,IDisposable
{
    readonly MetricsCollector collector;
    readonly Stopwatch stopwatch=Stopwatch.StartNew();
    int disposed;
    public RequestMeasurement Measurement {get;}
    internal RequestMeasurementScope(MetricsCollector collector,string endpoint) {this.collector=collector;Measurement=new RequestMeasurement {endpoint=endpoint};}
    public void Identify(string? userId,string? runId) {try {collector.Identify(Measurement,userId,runId);}catch(Exception){}}
    public void Record(AuthorityDiagnostic value)
    {
        if(value==null||!MetricsCollector.Kinds.Contains(value.kind)||value.count<0||!double.IsFinite(value.milliseconds)||value.milliseconds<0)return;
        lock(Measurement.diagnostics)
        {
            if(!Measurement.diagnostics.TryGetValue(value.kind,out var total))Measurement.diagnostics[value.kind]=total=new();
            total.count=Math.Min(1_000_000_000,total.count+Math.Min(1_000_000_000,value.count)); total.milliseconds=Math.Min(1e12,total.milliseconds+value.milliseconds);
        }
    }
    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        Measurement.durationMs=stopwatch.Elapsed.TotalMilliseconds;
        try {collector.Complete(Measurement);}catch(Exception) { }
    }
}
