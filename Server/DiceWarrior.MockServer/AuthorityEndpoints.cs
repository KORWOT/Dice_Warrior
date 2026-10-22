using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DiceWarrior.SymbolLoop;
using DiceWarrior.Metrics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiceWarrior.MockServer;

public static class AuthorityEndpoints
{
    static readonly Regex ValidId=new("\\A[a-zA-Z0-9._:-]{1,128}\\z",RegexOptions.CultureInvariant);
    public static void Map(WebApplication app,MockServerSettings settings,MetricsCollector metrics)
    {
        var identity=new DevelopmentIdentityResolver();
        app.MapGet("/healthz",()=>Results.Json(new {status="ok",mode="development-mock",protocol=1,
            rulesVersion=BackendProtocol.RulesVersion,rulesSha256=MockServerSettings.RulesSha256}));
        app.MapGet("/v1/profile",context=>Handle(context,"profile",settings,identity,metrics));
        app.MapPost("/v1/runs",context=>Handle(context,"start",settings,identity,metrics));
        app.MapPost("/v1/runs/{runId}/submit",context=>Handle(context,"settle",settings,identity,metrics));
        app.MapPost("/v1/profile/upgrades",context=>Handle(context,"grow",settings,identity,metrics));
        app.MapGet("/v1/operations/{requestId}",context=>Handle(context,"operation",settings,identity,metrics));
        app.MapGet("/v1/runs/{runId}",context=>Handle(context,"run",settings,identity,metrics));
    }

    static async Task Handle(HttpContext context,string kind,MockServerSettings settings,IDevelopmentIdentityResolver identity,MetricsCollector metrics)
    {
        using var scope=metrics.Begin(kind);
        var requestBody=context.Request.Body;var responseBody=context.Response.Body;
        var request=new MeasuredHttpStream(requestBody);var response=new MeasuredHttpStream(responseBody);
        context.Request.Body=request;context.Response.Body=response;
        try {await HandleCore(context,kind,settings,identity,scope);}
        finally
        {
            scope.Measurement.requestBytes=request.ReadBytes;scope.Measurement.responseBytes=response.WrittenBytes;
            scope.Measurement.statusCode=context.Response.StatusCode;
            if(context.RequestAborted.IsCancellationRequested) {scope.Measurement.outcome="transport-error";scope.Measurement.responseComplete=false;}
            context.Request.Body=requestBody;context.Response.Body=responseBody;
        }
    }
    static async Task HandleCore(HttpContext context,string kind,MockServerSettings settings,IDevelopmentIdentityResolver identity,RequestMeasurementScope scope)
    {
        context.Response.Headers.CacheControl="no-store";
        var user=identity.Resolve(context);
        if(user==null) { await Error(context,scope,401,"DEVELOPMENT_IDENTITY_REQUIRED"); return; }
        string? runId=context.Request.RouteValues["runId"]?.ToString();
        string? requestId=context.Request.RouteValues["requestId"]?.ToString();
        scope.Identify(user,runId);
        if((runId!=null&&!ValidId.IsMatch(runId)) || (requestId!=null&&!ValidId.IsMatch(requestId)))
        { await Error(context,scope,400,"INVALID_ROUTE_ID"); return; }
        try
        {
            BackendRequest? input=null;
            if(context.Request.Method=="POST")
            {
                if(context.Request.ContentType?.Split(';')[0].Trim()!="application/json")
                { await Error(context,scope,415,"JSON_REQUIRED"); return; }
                input=await ReadRequest(context.Request,context.RequestAborted);
                if(input==null || input.requestId==null || !ValidId.IsMatch(input.requestId))
                { await Error(context,scope,400,"INVALID_REQUEST_ID"); return; }
                if(input==null || input.kind!=kind || (kind=="settle"&&input.serverRunId!=runId))
                { await Error(context,scope,400,"WRONG_OPERATION"); return; }
                if(kind=="settle")
                {
                    scope.Measurement.commandCount=input.commands?.Count??0;
                    scope.Measurement.logBytes=Encoding.UTF8.GetByteCount(LoopJson.Encode(input.commands));
                }
            }
            var fault=settings.EnableFaults?context.Request.Headers["X-DiceWarrior-Test-Fault"].ToString():"";
            if(fault=="before-abort") {scope.Measurement.outcome="transport-error";context.Abort();return;}
            // From this point a failed/cancelled call can have accessed authority storage.
            // Until a reply confirms its outcome, zero Firestore operations would be misleading.
            scope.Measurement.authorityOutcome="temporary-failure";
            var repository=new SqliteAuthorityRepository(settings.DatabasePath,user,scope);
            var backend=new MockGameBackend(repository,settings.Seed.HasValue?()=>settings.Seed.Value:null,settings.Rules,telemetry:scope);
            var ct=context.RequestAborted;
            BackendReply reply=kind switch {
                "profile"=>await backend.GetProfileAsync(ct),
                "start"=>await backend.StartRunAsync(input!,ct),
                "settle"=>await backend.SubmitRunAsync(input!,ct),
                "grow"=>await backend.UpgradeAsync(input!,ct),
                "operation"=>await backend.GetOperationStatusAsync(requestId!,ct),
                "run"=>await backend.GetRunStatusAsync(runId!,ct),
                _=>throw new InvalidOperationException("Unknown endpoint.")
            };
            scope.Measurement.authorityOutcome=Outcome(reply.status);scope.Measurement.outcome=Outcome(reply.status);
            scope.Measurement.domainCode=reply.code??"";
            scope.Identify(user,reply.run?.serverRunId??reply.submission?.serverRunId??runId);
            if(fault=="after-abort") {scope.Measurement.outcome="transport-error";context.Abort();return;}
            if(fault=="after-delay") await Task.Delay(2000,ct);
            await Write(context,scope,reply,200);
        }
        catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested) {scope.Measurement.outcome="transport-error";context.Abort();}
        catch(BadHttpRequestException error) { await Error(context,scope,error.StatusCode,"REQUEST_TOO_LARGE"); }
        catch(JsonException) { await Error(context,scope,400,"INVALID_JSON"); }
        catch(DecoderFallbackException) { await Error(context,scope,400,"INVALID_JSON"); }
        catch(Exception error)
        {
            // Report only the type; exception messages may contain database paths or payload data.
            appError(context,error.GetType().Name);
            scope.Measurement.outcome="temporary-failure";
            if(!context.Response.HasStarted) await Write(context,scope,new BackendReply {status=BackendStatus.TemporaryFailure,
                code="AUTHORITY_UNAVAILABLE",message="권한 서버에서 결과를 확정하지 못했습니다. 같은 요청으로 다시 확인하세요."},503);
            else {scope.Measurement.outcome="transport-error";context.Abort();}
        }
    }
    static void appError(HttpContext context,string type)=>context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Authority").LogWarning("Authority request unavailable ({ExceptionType}).",type);

    static async Task<BackendRequest?> ReadRequest(HttpRequest request,CancellationToken ct)
    {
        const int limit=512*1024;
        using var data=new MemoryStream();
        var buffer=new byte[8192];
        int count;
        while((count=await request.Body.ReadAsync(buffer.AsMemory(),ct))>0)
        {
            if(data.Length+count>limit) throw new BadHttpRequestException("Request body limit.",413);
            data.Write(buffer,0,count);
        }
        var json=new UTF8Encoding(false,true).GetString(data.ToArray());
        using var reader=new JsonTextReader(new StringReader(json)) {MaxDepth=64,DateParseHandling=DateParseHandling.None};
        JObject.Load(reader,new JsonLoadSettings {DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
        if(reader.Read()) throw new JsonReaderException("Only one JSON object is allowed.");
        return LoopJson.Decode<BackendRequest>(json);
    }
    static Task Error(HttpContext context,RequestMeasurementScope scope,int httpStatus,string code)=>Write(context,scope,new BackendReply {
        status=BackendStatus.Rejected,code=code,message="요청 형식 또는 개발 신원을 확인하세요."},httpStatus);
    static string Outcome(BackendStatus status)=>status switch {
        BackendStatus.Approved=>"approved",BackendStatus.Rejected=>"rejected",BackendStatus.Conflict=>"conflict",
        BackendStatus.TemporaryFailure=>"temporary-failure",_=>"transport-error"};
    static async Task Write(HttpContext context,RequestMeasurementScope scope,BackendReply reply,int status)
    {
        context.Response.StatusCode=status; context.Response.ContentType="application/json; charset=utf-8";
        scope.Measurement.domainCode=reply.code??"";
        await context.Response.WriteAsync(LoopJson.Encode(reply),context.RequestAborted);
        scope.Measurement.responseComplete=true;
    }
}
