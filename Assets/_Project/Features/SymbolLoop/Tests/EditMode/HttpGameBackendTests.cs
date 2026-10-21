using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class HttpGameBackendTests
    {
        string directory;
        [SetUp] public void Setup()=>directory=Path.Combine(Path.GetTempPath(),"DiceWarriorHttpTransport",Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorHttpTransport"))+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        sealed class Handler : HttpMessageHandler
        {
            readonly Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send;
            public Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send)=>this.send=send;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>send(request,ct);
        }
        static HttpResponseMessage Json(object reply,HttpStatusCode status=HttpStatusCode.OK)=>new HttpResponseMessage(status) {
            Content=new StringContent(LoopJson.Encode(reply),Encoding.UTF8,"application/json") };
        static HttpResponseMessage Raw(string json,HttpStatusCode status=HttpStatusCode.OK)=>new HttpResponseMessage(status) {
            Content=new StringContent(json,Encoding.UTF8,"application/json") };
        static BackendReply Rejected(string code="FIXTURE")=>new BackendReply {status=BackendStatus.Rejected,code=code,message="fixture"};
        static HttpBackendSettings Settings()=>new HttpBackendSettings {userId="transport-user",timeoutSeconds=1};

        [Test] public async Task SixRoutesPreserveProtocolBodyAndDevelopmentIdentity()
        {
            var routes=new List<string>();var bodies=new List<string>();
            using(var backend=new HttpGameBackend(Settings(),true,new Handler(async (request,ct)=>{
                routes.Add(request.Method+" "+request.RequestUri.AbsolutePath);
                Assert.That(request.Headers.GetValues("X-DiceWarrior-Dev-User").Single(),Is.EqualTo("transport-user"));
                Assert.That(request.Headers.Authorization,Is.Null);
                if(request.Content!=null) bodies.Add(await request.Content.ReadAsStringAsync());
                return Json(Rejected());
            })))
            {
                var request=new BackendRequest {requestId="request_1",kind="start",serverRunId="run_1"};
                string expected=LoopJson.Encode(request);
                await backend.GetProfileAsync(CancellationToken.None);
                await backend.StartRunAsync(request,CancellationToken.None);
                await backend.SubmitRunAsync(request,CancellationToken.None);
                await backend.UpgradeAsync(request,CancellationToken.None);
                await backend.GetOperationStatusAsync(request.requestId,CancellationToken.None);
                await backend.GetRunStatusAsync(request.serverRunId,CancellationToken.None);
                CollectionAssert.AreEqual(new[]{"GET /v1/profile","POST /v1/runs","POST /v1/runs/run_1/submit",
                    "POST /v1/profile/upgrades","GET /v1/operations/request_1","GET /v1/runs/run_1"},routes);
                Assert.That(bodies.All(body=>body==expected),Is.True);
            }
        }
        [Test] public async Task RequestIsSnapshottedBeforeAwaitAndReplyMayCarryDomainConflict()
        {
            var release=new TaskCompletionSource<bool>();string captured=null;
            using(var backend=new HttpGameBackend(Settings(),true,new Handler(async (request,ct)=>{
                await release.Task;captured=await request.Content.ReadAsStringAsync();
                return Json(new BackendReply {status=BackendStatus.Conflict,code="REVISION_CONFLICT"},HttpStatusCode.Conflict);
            })))
            {
                var request=new BackendRequest {requestId="original",kind="grow"};string original=LoopJson.Encode(request);
                var pending=backend.UpgradeAsync(request,CancellationToken.None);request.requestId="changed";request.expectedRevision=99;
                release.SetResult(true);var result=await pending;
                Assert.That(captured,Is.EqualTo(original));Assert.That(result.status,Is.EqualTo(BackendStatus.Conflict));
            }
        }
        [TestCase("{}")]
        [TestCase("{\"status\":99,\"code\":\"BAD\"}")]
        [TestCase("{\"status\":\"Approved\",\"code\":\"BAD\"}")]
        [TestCase("{\"status\":1,\"status\":0,\"code\":\"BAD\"}")]
        [TestCase("{\"status\":1,\"code\":\"BAD\",\"unknown\":1}")]
        [TestCase("null")]
        public void InvalidReplyIsTransportFailure(string json)
        {
            using(var backend=new HttpGameBackend(Settings(),true,new Handler((request,ct)=>Task.FromResult(Raw(json)))))
                Assert.ThrowsAsync<HttpRequestException>(async()=>await backend.GetProfileAsync(CancellationToken.None));
        }
        [TestCase(302)] [TestCase(500)] [TestCase(503)]
        public void RedirectOrServerErrorCannotBecomeAConfirmedDomainReply(int status)
        {
            using(var backend=new HttpGameBackend(Settings(),true,new Handler((request,ct)=>Task.FromResult(Json(
                new BackendReply {status=BackendStatus.Approved,code="DO_NOT_ACCEPT"},(HttpStatusCode)status)))))
                Assert.ThrowsAsync<HttpRequestException>(async()=>await backend.GetProfileAsync(CancellationToken.None));
        }
        [Test] public void ResponseLimitIsEnforcedBeforeParsing()
        {
            using(var backend=new HttpGameBackend(Settings(),true,new Handler((request,ct)=>{
                var response=Raw("{}");response.Content.Headers.ContentLength=HttpGameBackend.MaximumResponseBytes+1L;
                return Task.FromResult(response);
            }))) Assert.ThrowsAsync<HttpRequestException>(async()=>await backend.GetProfileAsync(CancellationToken.None));
        }
        [Test] public void CancellationAndTimeoutAreDistinctUnknownOutcomes()
        {
            using(var backend=new HttpGameBackend(Settings(),true,new Handler(async (request,ct)=>{
                // NUnit's synchronous ThrowsAsync must not wait on the blocked Unity main-thread context.
                await Task.Delay(Timeout.Infinite,ct).ConfigureAwait(false);return Json(Rejected());
            })))
            using(var caller=new CancellationTokenSource())
            {
                caller.CancelAfter(30);
                Assert.CatchAsync<OperationCanceledException>(()=>backend.GetProfileAsync(caller.Token));
                Assert.ThrowsAsync<TimeoutException>(()=>backend.GetProfileAsync(CancellationToken.None));
            }
        }
        [TestCase("server-error")] [TestCase("invalid-json")]
        public async Task UnknownHttpResultKeepsTheSameDurableRequest(string fault)
        {
            using(var backend=new HttpGameBackend(Settings(),true,new Handler((request,ct)=>{
                if(request.Method==HttpMethod.Get)
                    return Task.FromResult(request.RequestUri.AbsolutePath=="/v1/profile" ? Json(new BackendReply {
                        status=BackendStatus.Approved,code="PROFILE",profile=new BackendProfile {values=new Profile {currency=20}}
                    }):Json(Rejected("OPERATION_NOT_FOUND")));
                return Task.FromResult(fault=="server-error" ? Json(Rejected(),HttpStatusCode.ServiceUnavailable):Raw("{}"));
            })))
            {
                var store=new LoopStore(Path.Combine(directory,"checkpoint.json"));var session=new LoopSession(store,new Rules());
                var coordinator=new GameBackendCoordinator(session,store,backend);await coordinator.InitializeAsync();
                var command=session.Prepare("start");string expected=LoopJson.Encode(command);
                await coordinator.ExecuteAsync(command);
                Assert.That(coordinator.LastReply.status,Is.EqualTo(BackendStatus.UnknownAfterTimeout));
                Assert.That(LoopJson.Encode(store.ReadPending()),Is.EqualTo(expected));Assert.That(session.Snapshot.run,Is.Null);
                await coordinator.ExecuteAsync(store.ReadPending());
                Assert.That(LoopJson.Encode(store.ReadPending()),Is.EqualTo(expected));
            }
        }
        [Test] public void SettingsKeepAccountsAndEndpointsSeparateAndDenyRemoteOrReleaseUse()
        {
            var settings=Settings();string first=settings.CheckpointPath(directory);
            settings.endpoint+="/";Assert.That(settings.CheckpointPath(directory),Is.EqualTo(first));
            settings.userId="another";Assert.That(settings.CheckpointPath(directory),Is.Not.EqualTo(first));
            settings.userId="transport-user";settings.endpoint="http://127.0.0.1:5088";
            Assert.That(settings.CheckpointPath(directory),Is.Not.EqualTo(first));
            Assert.Throws<InvalidOperationException>(()=>settings.Validate(false));
            settings.endpoint="http://example.com";Assert.Throws<InvalidOperationException>(()=>settings.Validate(true));
            settings.endpoint="http://name:secret@127.0.0.1:5087";Assert.Throws<InvalidOperationException>(()=>settings.Validate(true));
            settings.endpoint="http://127.0.0.1:5087/path";Assert.Throws<InvalidOperationException>(()=>settings.Validate(true));
            settings.endpoint="http://127.0.0.1:5087";settings.userId="id\r\nHeader: value";
            Assert.Throws<ArgumentException>(()=>settings.Validate(true));
        }
        [Test] public void FactoryHasNoLocalAuthorityFallbackAndSettingsDoNotTouchLegacyStorage()
        {
            string settings=Path.Combine(directory,"http-settings.json");
            Assert.Throws<InvalidOperationException>(()=>GameBackendFactory.Create(BackendMode.Mock,settings));
            Assert.Throws<InvalidOperationException>(()=>GameBackendFactory.Create(BackendMode.HttpMock,settings));
            Assert.Throws<InvalidOperationException>(()=>GameBackendFactory.Create(BackendMode.HttpMock,settings,()=>33,true));
            using(var backend=(HttpGameBackend)GameBackendFactory.Create(BackendMode.HttpMock,settings,developmentAllowed:true))
                Assert.That(Directory.Exists(directory),Is.False);
        }
    }
}
