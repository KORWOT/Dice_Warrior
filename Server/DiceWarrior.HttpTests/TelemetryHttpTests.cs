using System.Net;
using System.Text;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.HttpTests;

[TestFixture,NonParallelizable]
public sealed class TelemetryHttpTests
{
    string directory=null!;
    [SetUp] public void Setup()
    {directory=Path.Combine(Path.GetTempPath(),"DiceWarriorMetricsHttp",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
    [TearDown] public void Cleanup()
    {
        string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorMetricsHttp"))+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        Directory.Delete(directory,true);
    }
    static BackendRequest Request(string kind,long revision,string? run=null,List<RunCommandEntry>? commands=null)=>new() {
        requestId=Guid.NewGuid().ToString("N"),kind=kind,expectedRevision=revision,serverRunId=run,commands=commands??new()};
    static HttpGameBackend Client(ServerProcess server,string user)=>new(new HttpBackendSettings {endpoint=server.Url,userId=user},true);
    static async Task<JObject> Metrics(HttpClient http,string url)=>JObject.Parse(await http.GetStringAsync(url+"/dev/metrics"));

    [Test] public async Task DashboardAndExportsAreLocalAndDoNotCountTheirOwnRefreshes()
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var http=new HttpClient();
        var response=await http.GetAsync(server.Url+"/dev/dashboard");
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Contains("Content-Security-Policy"),Is.True);
        Assert.That(await response.Content.ReadAsStringAsync(),Does.Contain("Firebase 운영 실측 없음"));
        var first=await Metrics(http,server.Url);await Metrics(http,server.Url);
        Assert.That((long)(await Metrics(http,server.Url))["overview"]!["requests"]!,Is.EqualTo((long)first["overview"]!["requests"]!));
        Assert.That(await http.GetStringAsync(server.Url+"/dev/metrics/export?format=csv"),Does.StartWith("startedUtc,endpoint,"));
        var invalid=await http.GetAsync(server.Url+"/dev/metrics/export?format=xml");
        Assert.That(invalid.StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
        using var forbidden=new HttpRequestMessage(HttpMethod.Get,server.Url+"/dev/metrics");forbidden.Headers.Host="attacker.example";
        Assert.That((await http.SendAsync(forbidden)).StatusCode,Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test] public async Task CompletedRunDuplicateAndInvalidInputHaveSeparatePhysicalAndLogicalMeasurements()
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        const string user="private-account-never-export";
        using var client=Client(server,user);using var http=new HttpClient();
        var started=await client.StartRunAsync(Request("start",0),default);
        var golden=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"ReplayGoldenVectors.json")));
        var commands=golden["vectors"]![0]!["commands"]!.ToObject<List<RunCommandEntry>>()!;
        var input=Request("settle",started.profile.revision,started.run.serverRunId,commands);
        var paid=await client.SubmitRunAsync(input,default);await client.SubmitRunAsync(input,default);
        await client.SubmitRunAsync(Request("settle",started.profile.revision,started.run.serverRunId,commands),default);
        Assert.That(paid.submission.appliedAmount,Is.EqualTo(50));
        var bad=new HttpRequestMessage(HttpMethod.Post,server.Url+"/v1/runs") {Content=new StringContent("{invalid",Encoding.UTF8,"application/json")};
        bad.Headers.Add("X-DiceWarrior-Dev-User",user);Assert.That((await http.SendAsync(bad)).StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
        var result=await Metrics(http,server.Url);
        Assert.That((long)result["overview"]!["requests"]!,Is.EqualTo(5));
        Assert.That((long)result["overview"]!["duplicates"]!,Is.EqualTo(2));
        Assert.That((long)result["overview"]!["diagnostics"]!["sql-select"]!["count"]!,Is.GreaterThan(0));
        Assert.That((long)result["estimatedFirestore"]!["reads"]!,Is.GreaterThan(0));
        var groups=(JArray)result["byEndpoint"]!;
        Assert.That(groups.Sum(g=>(long)g["estimatedFirestore"]!["reads"]!),Is.EqualTo((long)result["estimatedFirestore"]!["reads"]!));
        var settlement=groups.Single(g=>(string?)g["key"]=="settle");
        Assert.That((decimal)settlement["estimatedFirestore"]!["estimatedDocumentOperationsUsd"]!,Is.GreaterThan(0));
        Assert.That((long)result["byRun"]![0]!["logBytes"]!,Is.GreaterThan(0));
        var sample=(JArray)result["sample"]!;
        Assert.That(sample.Count(t=>t["diagnostics"]?["replay"]!=null),Is.EqualTo(1));
        foreach(var format in new[]{"json","csv"})
        {
            string export=await http.GetStringAsync(server.Url+"/dev/metrics/export?format="+format);
            Assert.That(export,Does.Not.Contain(user).And.Not.Contain(started.run.serverRunId).And.Not.Contain(input.requestId));
            Assert.That(export,Does.Not.Contain("clientCommandId").And.Not.Contain("startingProfile"));
        }
        File.WriteAllText(Path.Combine(directory,"sample.json"),result.ToString());
    }

    [Test] public async Task BrokenMetricsDirectoryCannotStopAuthorityAndPriceValidationIsExplicit()
    {
        string denied=Path.Combine(directory,"not-a-directory");File.WriteAllText(denied,"fixture");
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"),extraArguments:new[]{"--Metrics:Directory",denied});
        using var client=Client(server,"safe-user");using var http=new HttpClient();
        Assert.That((await client.UpgradeAsync(Request("grow",0),default)).status,Is.EqualTo(BackendStatus.Approved));
        Assert.That((await client.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(0));
        var snapshot=await Metrics(http,server.Url);
        Assert.That((bool)snapshot["storage"]!["persistenceAvailable"]!,Is.False);
        var quote=await http.PostAsync(server.Url+"/dev/costs",new StringContent("{}",Encoding.UTF8,"application/json"));
        Assert.That(quote.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        var cost=JObject.Parse(await quote.Content.ReadAsStringAsync());
        Assert.That((bool)cost["isActualBilling"]!,Is.False);
        Assert.That((bool)cost["scenario"]!["applyFreeTier"]!,Is.False);
        var invalid=await http.PostAsync(server.Url+"/dev/costs",new StringContent("{\"region\":\"unsupported\"}",Encoding.UTF8,"application/json"));
        Assert.That(invalid.StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
    }

    sealed class FaultOnce(string fault) : DelegatingHandler(new HttpClientHandler {AllowAutoRedirect=false})
    {
        int used;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if(request.Method==HttpMethod.Post&&request.RequestUri!.AbsolutePath.EndsWith("/submit")&&Interlocked.Exchange(ref used,1)==0)
                request.Headers.Add("X-DiceWarrior-Test-Fault",fault);
            return base.SendAsync(request,ct);
        }
    }
    [Test] public async Task UnavailableAuthorityIsUnestimatedInJsonAndCsv()
    {
        string invalidDatabase=Path.Combine(directory,"directory-is-not-database");Directory.CreateDirectory(invalidDatabase);
        using var server=await ServerProcess.Start(invalidDatabase);using var http=new HttpClient();
        http.DefaultRequestHeaders.Add("X-DiceWarrior-Dev-User","failed-storage-account");
        Assert.That((await http.GetAsync(server.Url+"/v1/profile")).StatusCode,Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        var snapshot=await Metrics(http,server.Url);
        Assert.That((long)snapshot["estimatedFirestore"]!["unsupported"]!,Is.EqualTo(1));
        Assert.That((string?)snapshot["sample"]![0]!["authorityOutcome"],Is.EqualTo("temporary-failure"));
        Assert.That(snapshot["byEndpoint"]![0]!["estimatedFirestore"]!["estimatedDocumentOperationsUsd"]!.Type,Is.EqualTo(JTokenType.Null));
        string csv=await http.GetStringAsync(server.Url+"/dev/metrics/export?format=csv");
        Assert.That(csv,Does.Contain("estimateSupported,estimateClassification"));
        Assert.That(csv,Does.Contain(",False,unknown-authority-outcome,,,\r\n"));
    }
    [TestCase("before-abort")][TestCase("after-abort")][TestCase("after-delay")]
    public async Task ReplayRejectionAndLostResponseKeepAuthorityAndTransportOutcomesSeparate(string fault)
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var client=Client(server,"fault-account");using var http=new HttpClient();
        var started=await client.StartRunAsync(Request("start",0),default);
        var invalid=new List<RunCommandEntry>{new() {sequence=1,clientCommandId="invalid-turn",kind="lap"}};
        Assert.That((await client.SubmitRunAsync(Request("settle",started.profile.revision,started.run.serverRunId,invalid),default)).status,Is.EqualTo(BackendStatus.Rejected));
        var golden=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"ReplayGoldenVectors.json")));
        var commands=golden["vectors"]![0]!["commands"]!.ToObject<List<RunCommandEntry>>()!;
        var request=Request("settle",started.profile.revision,started.run.serverRunId,commands);
        using var faulty=new HttpGameBackend(new HttpBackendSettings {endpoint=server.Url,userId="fault-account"},true,new FaultOnce(fault));
        using var cancellation=new CancellationTokenSource();if(fault=="after-delay")cancellation.CancelAfter(300);
        // Transport propagates failures; the game Coordinator classifies the unresolved outcome.
        var transport=Assert.CatchAsync<Exception>(async()=>await faulty.SubmitRunAsync(request,cancellation.Token));
        Assert.That(transport,fault=="after-delay"?Is.InstanceOf<OperationCanceledException>():Is.InstanceOf<HttpRequestException>());
        var recovered=await client.SubmitRunAsync(request,default);
        Assert.That(recovered.submission.appliedAmount,Is.EqualTo(50));
        Assert.That((await client.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(70));
        JObject snapshot=await Metrics(http,server.Url);
        for(int i=0;i<20&&(long)snapshot["overview"]!["requests"]!<5;i++) {await Task.Delay(25);snapshot=await Metrics(http,server.Url);}
        var records=(JArray)snapshot["sample"]!;
        var lost=records.Single(r=>(string?)r["outcome"]=="transport-error");
        Assert.That((bool)lost["responseComplete"]!,Is.False);
        Assert.That((string?)lost["authorityOutcome"],Is.EqualTo(fault=="before-abort"?"none":"approved"));
        Assert.That(records.Count(r=>r["diagnostics"]?["replay"]!=null),Is.EqualTo(2));
        Assert.That(records.Any(r=>(string?)r["authorityOutcome"]=="rejected"&&r["diagnostics"]?["replay"]!=null),Is.True);
        Assert.That((long)snapshot["overview"]!["duplicates"]!,Is.EqualTo(fault=="before-abort"?0:1));
    }
}
