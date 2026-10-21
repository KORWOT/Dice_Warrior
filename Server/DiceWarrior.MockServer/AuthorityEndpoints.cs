using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiceWarrior.MockServer;

public static class AuthorityEndpoints
{
    static readonly Regex ValidId=new("\\A[a-zA-Z0-9._:-]{1,128}\\z",RegexOptions.CultureInvariant);
    public static void Map(WebApplication app,MockServerSettings settings)
    {
        var identity=new DevelopmentIdentityResolver();
        app.MapGet("/healthz",()=>Results.Json(new {status="ok",mode="development-mock",protocol=1,
            rulesVersion=BackendProtocol.RulesVersion,rulesSha256=MockServerSettings.RulesSha256}));
        app.MapGet("/v1/profile",context=>Handle(context,"profile",settings,identity));
        app.MapPost("/v1/runs",context=>Handle(context,"start",settings,identity));
        app.MapPost("/v1/runs/{runId}/submit",context=>Handle(context,"settle",settings,identity));
        app.MapPost("/v1/profile/upgrades",context=>Handle(context,"grow",settings,identity));
        app.MapGet("/v1/operations/{requestId}",context=>Handle(context,"operation",settings,identity));
        app.MapGet("/v1/runs/{runId}",context=>Handle(context,"run",settings,identity));
    }

    static async Task Handle(HttpContext context,string kind,MockServerSettings settings,IDevelopmentIdentityResolver identity)
    {
        context.Response.Headers.CacheControl="no-store";
        var user=identity.Resolve(context);
        if(user==null) { await Error(context,401,"DEVELOPMENT_IDENTITY_REQUIRED"); return; }
        string? runId=context.Request.RouteValues["runId"]?.ToString();
        string? requestId=context.Request.RouteValues["requestId"]?.ToString();
        if((runId!=null&&!ValidId.IsMatch(runId)) || (requestId!=null&&!ValidId.IsMatch(requestId)))
        { await Error(context,400,"INVALID_ROUTE_ID"); return; }
        try
        {
            BackendRequest? input=null;
            if(context.Request.Method=="POST")
            {
                if(context.Request.ContentType?.Split(';')[0].Trim()!="application/json")
                { await Error(context,415,"JSON_REQUIRED"); return; }
                input=await ReadRequest(context.Request,context.RequestAborted);
                if(input==null || input.requestId==null || !ValidId.IsMatch(input.requestId))
                { await Error(context,400,"INVALID_REQUEST_ID"); return; }
                if(input==null || input.kind!=kind || (kind=="settle"&&input.serverRunId!=runId))
                { await Error(context,400,"WRONG_OPERATION"); return; }
            }
            var fault=settings.EnableFaults?context.Request.Headers["X-DiceWarrior-Test-Fault"].ToString():"";
            if(fault=="before-abort") { context.Abort(); return; }
            var repository=new SqliteAuthorityRepository(settings.DatabasePath,user);
            var backend=new MockGameBackend(repository,settings.Seed.HasValue?()=>settings.Seed.Value:null,settings.Rules);
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
            if(fault=="after-abort") { context.Abort(); return; }
            if(fault=="after-delay") await Task.Delay(2000,ct);
            await Write(context,reply,200);
        }
        catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested) { context.Abort(); }
        catch(BadHttpRequestException error) { await Error(context,error.StatusCode,"REQUEST_TOO_LARGE"); }
        catch(JsonException) { await Error(context,400,"INVALID_JSON"); }
        catch(DecoderFallbackException) { await Error(context,400,"INVALID_JSON"); }
        catch(Exception error)
        {
            // Report only the type; exception messages may contain database paths or payload data.
            appError(context,error.GetType().Name);
            if(!context.Response.HasStarted) await Write(context,new BackendReply {status=BackendStatus.TemporaryFailure,
                code="AUTHORITY_UNAVAILABLE",message="권한 서버에서 결과를 확정하지 못했습니다. 같은 요청으로 다시 확인하세요."},503);
            else context.Abort();
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
    static Task Error(HttpContext context,int httpStatus,string code)=>Write(context,new BackendReply {
        status=BackendStatus.Rejected,code=code,message="요청 형식 또는 개발 신원을 확인하세요."},httpStatus);
    static Task Write(HttpContext context,BackendReply reply,int status)
    {
        context.Response.StatusCode=status; context.Response.ContentType="application/json; charset=utf-8";
        return context.Response.WriteAsync(LoopJson.Encode(reply),context.RequestAborted);
    }
}
