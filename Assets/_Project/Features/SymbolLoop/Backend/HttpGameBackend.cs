using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiceWarrior.SymbolLoop
{
    // This transport has no Unity dependency. It never creates or changes authority state locally.
    public sealed class HttpGameBackend : IGameBackend, IDisposable
    {
        public const int MaximumResponseBytes = 8*1024*1024;
        public const int MaximumRequestBytes = 2*1024*1024;
        readonly HttpClient client;
        readonly string userId;
        readonly TimeSpan timeout;

        public HttpGameBackend(HttpBackendSettings settings,bool developmentAllowed,HttpMessageHandler handler=null)
        {
            if(settings==null) throw new ArgumentNullException(nameof(settings));
            var uri=settings.Validate(developmentAllowed);
            userId=settings.userId; timeout=TimeSpan.FromSeconds(settings.timeoutSeconds);
            client=new HttpClient(handler ?? new HttpClientHandler {
                AllowAutoRedirect=false,UseCookies=false,UseProxy=false,UseDefaultCredentials=false,Credentials=null
            },true) { BaseAddress=uri,Timeout=Timeout.InfiniteTimeSpan };
        }
        public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>Send(HttpMethod.Get,"v1/profile",null,ct);
        public Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct)=>Post("v1/runs",request,ct);
        public Task<BackendReply> SubmitRunAsync(BackendRequest request,CancellationToken ct)
        {
            if(request==null) throw new ArgumentNullException(nameof(request));
            return Post("v1/runs/"+Id(request.serverRunId)+"/submit",request,ct);
        }
        public Task<BackendReply> UpgradeAsync(BackendRequest request,CancellationToken ct)=>Post("v1/profile/upgrades",request,ct);
        public Task<BackendReply> GetOperationStatusAsync(string requestId,CancellationToken ct)=>Send(HttpMethod.Get,"v1/operations/"+Id(requestId),null,ct);
        public Task<BackendReply> GetRunStatusAsync(string serverRunId,CancellationToken ct)=>Send(HttpMethod.Get,"v1/runs/"+Id(serverRunId),null,ct);
        static string Id(string value) { HttpBackendSettings.ValidateId(value,nameof(value)); return Uri.EscapeDataString(value); }
        Task<BackendReply> Post(string route,BackendRequest request,CancellationToken ct)
        {
            if(request==null) throw new ArgumentNullException(nameof(request));
            HttpBackendSettings.ValidateId(request.requestId,nameof(request.requestId));
            // Serialize before the first await: caller mutation cannot alter a durable retry body.
            string body=LoopJson.Encode(request);
            if(Encoding.UTF8.GetByteCount(body)>MaximumRequestBytes) throw new ArgumentException("서버 요청 크기 제한을 초과했습니다.",nameof(request));
            return Send(HttpMethod.Post,route,body,ct);
        }
        async Task<BackendReply> Send(HttpMethod method,string route,string body,CancellationToken ct)
        {
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct))
            using(var request=new HttpRequestMessage(method,route))
            {
                deadline.CancelAfter(timeout);
                request.Headers.Add("X-DiceWarrior-Dev-User",userId);
                request.Headers.Accept.ParseAdd("application/json");
                if(body!=null) request.Content=new StringContent(body,Encoding.UTF8,"application/json");
                try
                {
                    using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false))
                    {
                        int status=(int)response.StatusCode;
                        if(status>=500 || (status>=300 && status<400))
                            throw new HttpRequestException("HTTP Mock 응답을 확인하지 못했습니다. 서버 상태 코드: "+status+".");
                        if(response.Content==null || response.Content.Headers.ContentLength>MaximumResponseBytes)
                            throw new HttpRequestException("HTTP Mock 응답이 없거나 크기 제한을 초과했습니다.");
                        if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"application/json",StringComparison.OrdinalIgnoreCase))
                            throw new HttpRequestException("HTTP Mock 서버가 JSON 응답을 반환하지 않았습니다.");
                        using(var stream=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using(var buffer=new MemoryStream())
                        {
                            var chunk=new byte[8192]; int count;
                            while((count=await stream.ReadAsync(chunk,0,chunk.Length,deadline.Token).ConfigureAwait(false))>0)
                            {
                                if(buffer.Length+count>MaximumResponseBytes) throw new HttpRequestException("HTTP Mock 응답 크기 제한을 초과했습니다.");
                                buffer.Write(chunk,0,count);
                            }
                            deadline.Token.ThrowIfCancellationRequested();
                            string json=new UTF8Encoding(false,true).GetString(buffer.ToArray());
                            BackendReply reply;
                            try
                            {
                                var parsed=JObject.Parse(json,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                                if(parsed["status"]?.Type!=JTokenType.Integer || parsed["code"]?.Type!=JTokenType.String)
                                    throw new JsonSerializationException("Missing response status or code.");
                                reply=LoopJson.Decode<BackendReply>(json);
                                if(reply==null || !Enum.IsDefined(typeof(BackendStatus),reply.status) || string.IsNullOrWhiteSpace(reply.code) ||
                                    (!response.IsSuccessStatusCode && reply.IsConfirmed))
                                    throw new JsonSerializationException("Invalid response status.");
                            }
                            catch(Exception error) when(error is JsonException || error is ArgumentException || error is OverflowException)
                            { throw new HttpRequestException("HTTP Mock 응답 형식이 올바르지 않습니다. 원 요청을 보존합니다.",error); }
                            return reply;
                        }
                    }
                }
                catch(OperationCanceledException error) when(!ct.IsCancellationRequested)
                { throw new TimeoutException("HTTP Mock 응답 제한 시간을 초과했습니다. 같은 요청으로 결과를 확인하세요.",error); }
                catch(Exception error) when(error is IOException || error is DecoderFallbackException)
                { throw new HttpRequestException("HTTP Mock 연결이 끊겼거나 응답을 읽을 수 없습니다. 원 요청을 보존합니다.",error); }
            }
        }
        public void Dispose()=>client.Dispose();
    }
}
