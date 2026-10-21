using System.Net;
using System.Text;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.HttpTests;

[TestFixture,NonParallelizable]
public sealed class HttpAuthorityTests
{
    string directory=null!;
    JObject golden=null!;
    [SetUp] public void SetUp()
    {
        directory=Path.Combine(Path.GetTempPath(),"DiceWarriorHttpTests",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        golden=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"ReplayGoldenVectors.json")));
    }
    [TearDown] public void TearDown()
    {
        string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorHttpTests"))+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Fixture path escaped.");
        Directory.Delete(directory,true);
    }
    HttpGameBackend Client(ServerProcess server,string user="test-user",HttpMessageHandler? handler=null)=>
        new(new HttpBackendSettings {endpoint=server.Url,userId=user,timeoutSeconds=15},true,handler);
    static BackendRequest Request(string kind,long revision,string? runId=null,List<RunCommandEntry>? commands=null)=>new() {
        requestId=Guid.NewGuid().ToString("N"),kind=kind,expectedRevision=revision,serverRunId=runId,commands=commands??new()};
    List<RunCommandEntry> Victory()=>golden["vectors"]![0]!["commands"]!.ToObject<List<RunCommandEntry>>()!;

    [TestCase(0u)] [TestCase(1u)] [TestCase(2u)] [TestCase(3u)]
    public async Task GoldenTicketsReplayAllStatesAcrossTheHttpProcess(uint index)
    {
        var vector=golden["vectors"]![(int)index]!;
        var expectedTicket=vector["ticket"]!.ToObject<RunTicket>()!;
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"),expectedTicket.seed);
        using var client=Client(server);
        var profile=await client.GetProfileAsync(default);
        Assert.That(profile.profile.values.currency,Is.EqualTo(20));
        var started=await client.StartRunAsync(Request("start",profile.profile.revision),default);
        Assert.That(started.status,Is.EqualTo(BackendStatus.Approved));
        Assert.That(LoopJson.Encode(started.run.rules),Is.EqualTo(LoopJson.Encode(expectedTicket.rules)));
        var run=LoopRules.Start(started.run.rules,started.run.startingProfile.values,started.run.seed,expectedTicket.serverRunId);
        var commands=vector["commands"]!.ToObject<List<RunCommandEntry>>()!;
        AssertState(run,vector["states"]![0]!);
        for(int i=0;i<commands.Count;i++)
        {RunCommandRules.Apply(run,commands[i].kind,commands[i].arg);AssertState(run,vector["states"]![i+1]!);}
        var request=Request("settle",started.profile.revision,started.run.serverRunId,commands);
        var paid=await client.SubmitRunAsync(request,default);
        Assert.That(paid.status,Is.EqualTo(BackendStatus.Approved),paid.code);
        Assert.That(paid.submission.appliedAmount,Is.EqualTo((int)vector["payout"]!));
        var receipt=await client.GetRunStatusAsync(started.run.serverRunId,default);
        Assert.That(LoopJson.Encode(receipt.commands),Is.EqualTo(LoopJson.Encode(commands)));
        Assert.That(receipt.submission.receiptId,Is.EqualTo(paid.submission.receiptId));
    }
    static void AssertState(Run run,JToken expected)
    {
        var actual=JObject.Parse(LoopJson.Encode(run));
        foreach(var property in ((JObject)expected).Properties())
            Assert.That(JToken.DeepEquals(property.Value,actual[property.Name]),Is.True,"state field "+property.Name);
    }

    [Test] public async Task TwoServerProcessesSettleOneRunOneHundredTimesAndSpendOnce()
    {
        string db=Path.Combine(directory,"authority.db");
        using var first=await ServerProcess.Start(db);
        using var second=await ServerProcess.Start(db);
        using var a=Client(first); using var b=Client(second);
        await a.GetProfileAsync(default);
        var started=await a.StartRunAsync(Request("start",0),default);
        var requests=Enumerable.Range(0,100).Select(_=>Request("settle",started.profile.revision,started.run.serverRunId,Victory())).ToArray();
        var replies=await Task.WhenAll(requests.Select((r,i)=>(i%2==0?a:b).SubmitRunAsync(r,default)));
        Assert.That(replies.All(r=>r.status==BackendStatus.Approved),Is.True,string.Join(",",replies.Select(r=>r.code).Distinct()));
        Assert.That(replies.Select(r=>r.submission.receiptId).Distinct().Count(),Is.EqualTo(1));
        var current=await b.GetProfileAsync(default);
        Assert.That(current.profile.values.currency,Is.EqualTo(70));
        var changed=LoopJson.Copy(requests[0]);changed.commands.RemoveAt(0);
        Assert.That((await a.SubmitRunAsync(changed,default)).code,Is.EqualTo("REQUEST_ID_REUSED"));
        var upgrades=await Task.WhenAll(Enumerable.Range(0,10).Select((_,i)=>(i%2==0?a:b).UpgradeAsync(Request("grow",current.profile.revision),default)));
        Assert.That(upgrades.Count(r=>r.status==BackendStatus.Approved),Is.EqualTo(1));
        Assert.That(upgrades.Count(r=>r.status==BackendStatus.Conflict),Is.EqualTo(9));
        Assert.That((await a.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(50));
    }

    sealed class FaultOnce(string fault) : DelegatingHandler(new HttpClientHandler {AllowAutoRedirect=false})
    {
        int used;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if(request.Method==HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/submit") && Interlocked.Exchange(ref used,1)==0)
                request.Headers.Add("X-DiceWarrior-Test-Fault",fault);
            return base.SendAsync(request,ct);
        }
    }
    [TestCase("before-abort")] [TestCase("after-abort")] [TestCase("after-delay")]
    public async Task ActualDisconnectOrCancellationAndCrashRecoverOriginalRequest(string fault)
    {
        string db=Path.Combine(directory,"authority.db"),local=Path.Combine(directory,"checkpoint.json");
        var server=await ServerProcess.Start(db);
        try
        {
            using(var client=Client(server,handler:new FaultOnce(fault)))
            {
                var store=new LoopStore(local);var session=new LoopSession(store,new Rules());
                var coordinator=new GameBackendCoordinator(session,store,client);
                await coordinator.InitializeAsync();await coordinator.ExecuteAsync(session.Prepare("start"));
                var old=File.ReadAllBytes(local);
                foreach(var entry in Victory()) session.Execute(session.Prepare(entry.kind,entry.arg));
                var command=session.Prepare("settle");
                using var cancellation=new CancellationTokenSource();
                if(fault=="after-delay") cancellation.CancelAfter(300);
                await coordinator.ExecuteAsync(command,cancellation.Token);
                Assert.That(coordinator.LastReply.status,Is.EqualTo(BackendStatus.UnknownAfterTimeout));
                Assert.That(store.ReadPending().id,Is.EqualTo(command.id));
                Assert.That(session.Snapshot.run.settled,Is.False);
                Assert.That(Directory.GetFiles(directory,"*.backend-error-*.json").Length,Is.GreaterThan(0));
                server.Dispose();server=await ServerProcess.Start(db);
                using var restarted=Client(server);
                var resumed=new LoopSession(store,new Rules());var recovery=new GameBackendCoordinator(resumed,store,restarted);
                await recovery.ExecuteAsync(store.ReadPending());
                Assert.That(store.ReadPending(),Is.Null);
                Assert.That(resumed.Snapshot.run.settled,Is.True);
                Assert.That(resumed.Snapshot.profile.currency,Is.EqualTo(70));
                Assert.That((await restarted.GetOperationStatusAsync(command.id,default)).submission.receiptId,Is.EqualTo(resumed.Snapshot.submission.receiptId));
                File.WriteAllBytes(local+".backup",old);File.WriteAllText(local,"damaged-fixture");store.RestoreBackup();
                var restored=new LoopSession(store,new Rules());
                await new GameBackendCoordinator(restored,store,restarted).InitializeAsync();
                Assert.That(restored.Snapshot.run.settled,Is.True);Assert.That(restored.Snapshot.profile.currency,Is.EqualTo(70));
                Assert.That(Directory.GetFiles(directory,"*.reconciled-*.json").Length,Is.GreaterThan(0));
            }
        }
        finally {server.Dispose();}
    }

    [Test] public async Task IdentitiesAreScopedAndForgedResultsOrLogsCannotPay()
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var a=Client(server,"user-a");using var b=Client(server,"user-b");
        var startRequest=Request("start",0);
        var start=await a.StartRunAsync(startRequest,default);
        Assert.That((await b.GetRunStatusAsync(start.run.serverRunId,default)).code,Is.EqualTo("RUN_NOT_FOUND"));
        Assert.That((await b.GetOperationStatusAsync(startRequest.requestId,default)).code,Is.EqualTo("OPERATION_NOT_FOUND"));
        var stolen=Request("settle",0,start.run.serverRunId,Victory());
        Assert.That((await b.SubmitRunAsync(stolen,default)).status,Is.EqualTo(BackendStatus.Rejected));
        var illegal=Victory();illegal.RemoveAt(0);
        Assert.That((await a.SubmitRunAsync(Request("settle",start.profile.revision,start.run.serverRunId,illegal),default)).status,Is.EqualTo(BackendStatus.Rejected));
        using var http=new HttpClient();
        Assert.That((await http.GetAsync(server.Url+"/v1/profile")).StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
        http.DefaultRequestHeaders.Add("X-DiceWarrior-Dev-User","user-a");
        var forged=JObject.Parse(LoopJson.Encode(Request("settle",start.profile.revision,start.run.serverRunId,Victory())));
        forged["currency"]=999999;forged["won"]=true;
        var response=await http.PostAsync(server.Url+"/v1/runs/"+start.run.serverRunId+"/submit",new StringContent(forged.ToString(),Encoding.UTF8,"application/json"));
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await a.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(20));
        Assert.That((await b.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(20));
    }

    [Test] public async Task FieldOrderDoesNotChangeThePersistedFingerprintAndFaultsAreOffByDefault()
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"),faults:false);
        using var client=Client(server);
        var input=Request("grow",0);
        var first=await client.UpgradeAsync(input,default);
        using var http=new HttpClient();http.DefaultRequestHeaders.Add("X-DiceWarrior-Dev-User","test-user");
        http.DefaultRequestHeaders.Add("X-DiceWarrior-Test-Fault","before-abort");
        var obj=JObject.Parse(LoopJson.Encode(input));var reversed=new JObject(obj.Properties().Reverse().Select(p=>new JProperty(p.Name,p.Value)));
        var response=await http.PostAsync(server.Url+"/v1/profile/upgrades",new StringContent(reversed.ToString(),Encoding.UTF8,"application/json"));
        var retry=LoopJson.Decode<BackendReply>(await response.Content.ReadAsStringAsync());
        Assert.That(LoopJson.Encode(retry),Is.EqualTo(LoopJson.Encode(first)));
        Assert.That((await client.GetProfileAsync(default)).profile.values.currency,Is.EqualTo(0));
    }

    [TestCase("_buy")] [TestCase("buy.v1")] [TestCase("buy:v1")] [TestCase("maximum")]
    public async Task EveryAcceptedEdgeRequestIdCanQueryItsOriginalReceipt(string id)
    {
        if(id=="maximum") id=new string('x',128);
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var client=Client(server);
        var request=Request("grow",0);request.requestId=id;
        var paid=await client.UpgradeAsync(request,default);
        Assert.That(paid.status,Is.EqualTo(BackendStatus.Approved));
        var queried=await client.GetOperationStatusAsync(id,default);
        Assert.That(LoopJson.Encode(queried),Is.EqualTo(LoopJson.Encode(paid)));
    }
    [TestCase("bad/id")] [TestCase("bad id")] [TestCase("too-long")]
    public async Task UnqueryableIdsAreRejectedBeforeAuthorityMutation(string id)
    {
        if(id=="too-long") id=new string('x',129);
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var client=Client(server);
        var request=Request("grow",0);request.requestId=id;
        Assert.Throws<ArgumentException>(()=>client.UpgradeAsync(request,default));
        using var http=new HttpClient();http.DefaultRequestHeaders.Add("X-DiceWarrior-Dev-User","test-user");
        var response=await http.PostAsync(server.Url+"/v1/profile/upgrades",new StringContent(LoopJson.Encode(request),Encoding.UTF8,"application/json"));
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
        var profile=await client.GetProfileAsync(default);
        Assert.That(profile.profile.values.currency,Is.EqualTo(20));Assert.That(profile.profile.revision,Is.EqualTo(0));
    }
    [Test] public async Task DevelopmentAccountLengthHasTheSameClientAndServerBoundary()
    {
        using var server=await ServerProcess.Start(Path.Combine(directory,"authority.db"));
        using var client=Client(server,new string('a',64));
        Assert.That((await client.GetProfileAsync(default)).status,Is.EqualTo(BackendStatus.Approved));
        Assert.Throws<ArgumentException>(()=>Client(server,new string('a',65)));
        using var http=new HttpClient();http.DefaultRequestHeaders.Add("X-DiceWarrior-Dev-User",new string('a',65));
        Assert.That((await http.GetAsync(server.Url+"/v1/profile")).StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
