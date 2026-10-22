using System.Globalization;
using System.Net;
using System.Text;
using DiceWarrior.Metrics;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiceWarrior.MockServer;

public static class MetricsEndpoints
{
    public static void Map(WebApplication app,MetricsCollector collector)
    {
        app.MapGet("/dev/dashboard",c=>Static(c,"dashboard.html","text/html; charset=utf-8"));
        app.MapGet("/dev/dashboard.js",c=>Static(c,"dashboard.js","application/javascript; charset=utf-8"));
        app.MapGet("/dev/dashboard.css",c=>Static(c,"dashboard.css","text/css; charset=utf-8"));
        app.MapGet("/dev/metrics",c=>Metrics(c,collector,false));
        app.MapGet("/dev/metrics/export",c=>Metrics(c,collector,true));
        app.MapPost("/dev/costs",Costs);
        app.MapGet("/dev/pricing",async c=> {
            if(!await Guard(c)) return;
            try { await Json(c,JObject.Parse(File.ReadAllText(PricingCatalog.DefaultPath))); }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException)
            {await Failure(c,503,"PRICING_UNAVAILABLE");}
        });
    }
    static async Task<bool> Guard(HttpContext c)
    {
        c.Response.Headers.CacheControl="no-store";
        c.Response.Headers["X-Content-Type-Options"]="nosniff";
        c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        var host=c.Request.Host.Host.Trim('[',']');
        bool localHost=host.Equals("localhost",StringComparison.OrdinalIgnoreCase)||(IPAddress.TryParse(host,out var ip)&&IPAddress.IsLoopback(ip));
        bool localPeer=c.Connection.RemoteIpAddress is { } peer&&IPAddress.IsLoopback(peer);
        string origin=c.Request.Headers.Origin.ToString();
        bool sameOrigin=origin.Length==0||origin==$"{c.Request.Scheme}://{c.Request.Host}";
        if(localHost&&localPeer&&sameOrigin) return true;
        await Failure(c,403,"LOCAL_ACCESS_ONLY");return false;
    }
    static async Task Static(HttpContext c,string name,string type)
    {
        if(!await Guard(c)) return;
        c.Response.ContentType=type;
        await c.Response.SendFileAsync(Path.Combine(AppContext.BaseDirectory,"wwwroot",name),c.RequestAborted);
    }
    static List<RequestMeasurement> Filter(HttpContext c,MetricsSnapshot snapshot)
    {
        DateTimeOffset? Parse(string key)
        {
            string value=c.Request.Query[key].ToString();if(value.Length==0)return null;
            if(!DateTimeOffset.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var result))
                throw new ArgumentException("Invalid UTC date.");
            return result;
        }
        var from=Parse("from");var to=Parse("to");
        if(from>to)throw new ArgumentException("Invalid date range.");
        return snapshot.records.Where(r=>(from==null||r.startedUtc>=from)&&(to==null||r.startedUtc<to.Value.AddDays(1))).ToList();
    }
    static async Task Metrics(HttpContext c,MetricsCollector collector,bool export)
    {
        if(!await Guard(c))return;
        var snapshot=collector.Snapshot();List<RequestMeasurement> records;
        try {records=Filter(c,snapshot);}catch(ArgumentException){await Failure(c,400,"INVALID_DATE_RANGE");return;}
        var estimates=records.Select(FirebaseOperationModel.Describe).ToList();
        PricingCatalog? pricing=null;
        try {pricing=PricingCatalog.LoadDefault();} catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { }
        JObject Group(MeasurementAggregate aggregate,IEnumerable<RequestMeasurement> rows)
        {
            var plans=rows.Select(FirebaseOperationModel.Describe).ToArray();
            long reads=plans.Sum(x=>x.reads),writes=plans.Sum(x=>x.writes),deletes=plans.Sum(x=>x.deletes);
            decimal? Price(string key,long quantity)=>pricing!=null&&pricing.rates.TryGetValue(key,out var rate)&&rate.usd is decimal usd?quantity/100000m*usd:null;
            decimal? subtotal=Price("firestore-reads",reads)+Price("firestore-writes",writes)+Price("firestore-deletes",deletes);
            var row=JObject.FromObject(aggregate);
            row["estimatedFirestore"]=JObject.FromObject(new {reads,writes,deletes,
                unsupported=plans.Count(x=>!x.supported),estimatedDocumentOperationsUsd=plans.All(x=>x.supported)?subtotal:null,
                knownDocumentOperationsSubtotalUsd=subtotal,
                costScope="hypothetical document operations only; excludes storage, egress, Cloud Run, free quotas and tax"});
            return row;
        }
        var modeled=new {isActualFirestoreUsage=false,modelVersion=FirebaseOperationModel.Version,
            reads=estimates.Sum(x=>x.reads),writes=estimates.Sum(x=>x.writes),deletes=estimates.Sum(x=>x.deletes),
            indexEntryReads=estimates.Sum(x=>x.indexEntryReads),queryMinimumReads=estimates.Sum(x=>x.queryMinimumReads),
            transactionAttempts=estimates.Sum(x=>x.transactionAttempts),storageBytes=estimates.Sum(x=>x.storageBytes),egressBytes=estimates.Sum(x=>x.egressBytes),
            unsupported=estimates.Count(x=>!x.supported),assumptions=estimates.SelectMany(x=>x.assumptions).Distinct().ToArray()};
        var result=new {schemaVersion=1,snapshot.generatedUtc,snapshot.measurementScope,snapshot.privacy,
            byteScope="HTTP entity bytes read/written; excludes headers, compression, TLS and confirmed delivery",
            storage=new {snapshot.persistenceAvailable,snapshot.truncated,snapshot.droppedRecords,snapshot.invalidRecords,
                snapshot.retentionDays,snapshot.maxRecords,snapshot.maxJournalFiles,snapshot.maxJournalBytes,snapshot.authorityDatabaseBytes,snapshot.metricsBytes},
            process=new {snapshot.activeRequests,snapshot.peakRequests,snapshot.completedRequests,snapshot.processCpuMilliseconds,snapshot.processMemoryBytes,snapshot.cpuScope},
            overview=MetricsAggregation.Overall(records),byDay=MetricsAggregation.ByDay(records).Select(g=>Group(g,records.Where(r=>r.startedUtc.UtcDateTime.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)==g.key))).ToArray(),
            byEndpoint=MetricsAggregation.ByEndpoint(records).Select(g=>Group(g,records.Where(r=>r.endpoint==g.key))).ToArray(),
            byRun=MetricsAggregation.ByRun(records).TakeLast(200).Select(g=>Group(g,records.Where(r=>r.runKey==g.key))).ToArray(),estimatedFirestore=modeled,
            sample=export?records:records.TakeLast(200).ToList(),sampleLimited=!export&&records.Count>200};
        if(!export){await Json(c,result);return;}
        string format=c.Request.Query["format"].ToString();
        if(format!="json"&&format!="csv"){await Failure(c,400,"INVALID_EXPORT_FORMAT");return;}
        c.Response.Headers.ContentDisposition=$"attachment; filename=mock-metrics.{format}";
        if(format=="json"){await Json(c,result);return;}
        c.Response.ContentType="text/csv; charset=utf-8";
        var csv=new StringBuilder("startedUtc,endpoint,outcome,authorityOutcome,statusCode,responseComplete,userKey,runKey,durationMs,requestBytes,responseBytes,commandCount,logBytes,sqlCommands,sqlRows,commitMs,casRetries,replayMs,estimateSupported,estimateClassification,estimatedReads,estimatedWrites,estimatedDeletes\r\n");
        for(int i=0;i<records.Count;i++)
        {
            var r=records[i];var e=estimates[i];
            long Count(string key)=>r.diagnostics.TryGetValue(key,out var d)?d.count:0;
            double Ms(string key)=>r.diagnostics.TryGetValue(key,out var d)?d.milliseconds:0;
            object?[] row={r.startedUtc.ToString("O"),r.endpoint,r.outcome,r.authorityOutcome,r.statusCode,r.responseComplete,r.userKey,r.runKey,
                r.durationMs,r.requestBytes,r.responseBytes,r.commandCount,r.logBytes,
                Count("sql-select")+Count("sql-insert")+Count("sql-update")+Count("sql-delete")+Count("sql-other"),Count("sql-rows"),Ms("transaction-commit"),Count("cas-retry"),Ms("replay"),e.supported,e.classification,e.supported?e.reads:null,e.supported?e.writes:null,e.supported?e.deletes:null};
            csv.AppendJoin(',',row.Select(Csv)).Append("\r\n");
        }
        await c.Response.WriteAsync(csv.ToString(),c.RequestAborted);
    }
    static string Csv(object? value)
    {
        string s=Convert.ToString(value,CultureInfo.InvariantCulture)??"";
        if(s.Length>0&&"=+@-".Contains(s[0]))s="'"+s;
        return s.IndexOfAny(new[]{',','"','\r','\n'})>=0?"\""+s.Replace("\"","\"\"")+"\"":s;
    }
    static async Task Costs(HttpContext c)
    {
        if(!await Guard(c))return;
        if(c.Request.ContentType?.Split(';')[0].Trim()!="application/json"){await Failure(c,415,"JSON_REQUIRED");return;}
        try
        {
            using var data=new MemoryStream();var buffer=new byte[4096];int read;
            while((read=await c.Request.Body.ReadAsync(buffer,c.RequestAborted))>0)
            {if(data.Length+read>16384){await Failure(c,413,"SCENARIO_TOO_LARGE");return;}data.Write(buffer,0,read);}
            string text=new UTF8Encoding(false,true).GetString(data.ToArray());
            using var reader=new JsonTextReader(new StringReader(text)){MaxDepth=8};
            JObject.Load(reader,new JsonLoadSettings {DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(reader.Read())throw new JsonReaderException();
            var scenario=LoopJson.Decode<CostScenario>(text)??throw new ArgumentException();
            PricingCatalog catalog;
            try {catalog=PricingCatalog.LoadDefault();}
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException)
            {await Failure(c,503,"PRICING_UNAVAILABLE");return;}
            await Json(c,CostEstimator.Estimate(scenario,catalog));
        }
        catch(Exception e) when(e is JsonException or ArgumentException or OverflowException or DecoderFallbackException)
        {await Failure(c,400,"INVALID_SCENARIO");}
    }
    static Task Failure(HttpContext c,int status,string code){c.Response.StatusCode=status;return Json(c,new {code});}
    static Task Json(HttpContext c,object value){c.Response.ContentType="application/json; charset=utf-8";return c.Response.WriteAsync(LoopJson.Encode(value),c.RequestAborted);}
}
