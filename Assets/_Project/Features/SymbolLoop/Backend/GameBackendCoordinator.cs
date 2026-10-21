using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DiceWarrior.SymbolLoop
{
    public sealed class GameBackendCoordinator
    {
        readonly LoopSession session;
        readonly LoopStore store;
        readonly IGameBackend backend;
        int busy;
        public bool Busy => Volatile.Read(ref busy)!=0;
        public bool ProfileReady { get; private set; }
        public BackendReply LastReply { get; private set; }

        public GameBackendCoordinator(LoopSession session,LoopStore store,IGameBackend backend)
        {
            this.session=session ?? throw new ArgumentNullException(nameof(session));
            this.store=store ?? throw new ArgumentNullException(nameof(store));
            this.backend=backend ?? throw new ArgumentNullException(nameof(backend));
        }
        void Enter()
        {
            if(Interlocked.CompareExchange(ref busy,1,0)!=0) throw new InvalidOperationException("백엔드 요청 처리 중입니다.");
        }
        public async Task InitializeAsync(CancellationToken ct=default)
        {
            Enter(); ProfileReady=false;
            try { await RefreshAuthorityAsync(ct); }
            catch(Exception error) when(IsTransport(error))
            {
                LastReply=new BackendReply {status=BackendStatus.TemporaryFailure,code="PROFILE_UNAVAILABLE",
                    message="권한 상태를 확인할 수 없습니다. 로컬 진행을 보존했습니다."};
                store.RecordBackendFailure(null,LastReply,error.ToString());
            }
            finally { Volatile.Write(ref busy,0); }
        }
        async Task RefreshAuthorityAsync(CancellationToken ct)
        {
            ProfileReady=false;
            if(store.ReadPending()!=null) return;
            var reply=await backend.GetProfileAsync(ct); LastReply=reply;
            if(reply.status!=BackendStatus.Approved || store.ReadPending()!=null) return;
            session.Reload(); var local=session.Snapshot.run;
            bool recovered=false;
            if(local!=null && !local.settled && local.id!=reply.run?.serverRunId)
            {
                var status=await backend.GetRunStatusAsync(local.id,ct); LastReply=status;
                if(!status.IsConfirmed || status.submission==null)
                {
                    if(status.IsConfirmed) LastReply=new BackendReply {status=BackendStatus.Conflict,code="AUTHORITY_CHANGED",
                        message="다른 세션에서 여정 상태가 바뀌었습니다. 다시 확인해 주세요."};
                    return;
                }
                if(store.ReadPending()!=null) return;
                store.PreserveCheckpoint(); session.ReconcileSubmission(status); recovered=true;
                reply=await backend.GetProfileAsync(ct); LastReply=reply;
                if(reply.status!=BackendStatus.Approved || store.ReadPending()!=null) return;
            }
            if(reply.run!=null && session.Snapshot.run?.id!=reply.run.serverRunId) store.PreserveCheckpoint();
            bool restored=session.RestoreActiveRun(reply.run,reply.profile);
            if(recovered) reply.message="서버 정산 기록과 최신 재화를 복구했습니다. 이전 로컬 진행은 보존했습니다.";
            else if(restored) reply.message="백업에 없던 승인 런을 시작 지점으로 복원했습니다. 이전 로컬 진행은 보존했습니다.";
            LastReply=reply; ProfileReady=true;
        }
        public async Task ExecuteAsync(Command command,CancellationToken ct=default)
        {
            Enter();
            try
            {
                // Do not let caller mutation change the persisted operation after an await.
                command=LoopJson.Copy(command);
                store.WritePending(command);
                if(!LoopSession.IsBackend(command.kind))
                {
                    session.Execute(command); store.ClearPending(command.id); return;
                }
                ProfileReady=false;
                if(command.backend==null || command.backend.requestId!=command.id || command.backend.kind!=command.kind)
                    throw new InvalidOperationException("미결 요청 형식이 맞지 않습니다. 원문을 보존했습니다.");
                var reply=await backend.GetOperationStatusAsync(command.id,ct);
                if(reply.code=="OPERATION_NOT_FOUND" || reply.IsConfirmed || reply.status==BackendStatus.Rejected || reply.status==BackendStatus.Conflict)
                {
                    if(reply.code=="OPERATION_NOT_FOUND")
                    {
                        try { session.ValidateBackendCommand(command); }
                        catch(Exception error) when(error is InvalidOperationException || error is ArgumentException)
                        {
                            LastReply=new BackendReply {status=BackendStatus.Rejected,code="LOCAL_REQUEST_STALE",message=error.Message};
                            store.RecordBackendFailure(command,LastReply,error.ToString()); store.DiscardPending(command.id); return;
                        }
                    }
                    // Even a confirmed lookup is re-sent with its original body to check the fingerprint.
                    switch(command.kind)
                    {
                        case "start": reply=await backend.StartRunAsync(command.backend,ct); break;
                        case "grow": reply=await backend.UpgradeAsync(command.backend,ct); break;
                        case "settle": reply=await backend.SubmitRunAsync(command.backend,ct); break;
                    }
                }
                LastReply=reply;
                if(reply.IsConfirmed)
                {
                    if(command.kind=="settle")
                    {
                        var status=await backend.GetRunStatusAsync(command.backend.serverRunId,ct);
                        if(!status.IsConfirmed || status.submission==null)
                        { LastReply=status; store.RecordBackendFailure(command,status); return; }
                        if(status.submission.receiptId!=reply.submission?.receiptId)
                            throw new InvalidOperationException("정산 영수증 확인 결과가 다릅니다. 원 요청을 보존했습니다.");
                        status.message=reply.message; reply=status;
                        if(session.Snapshot.run?.phase!=Phase.Result) store.PreserveCheckpoint();
                    }
                    session.ApplyBackend(command,reply);
                    store.ClearPending(command.id);
                    try { await RefreshAuthorityAsync(ct); }
                    catch(Exception error) when(IsTransport(error))
                    {
                        // This operation is already durable locally. Only the later profile refresh failed.
                        reply.message+=" · 최신 프로필 재조회가 필요합니다.";
                        LastReply=reply; ProfileReady=false; store.RecordBackendFailure(command,reply,error.ToString()); return;
                    }
                    if(ProfileReady) { reply.profile=session.Snapshot.authority; LastReply=reply; }
                }
                else if(reply.status==BackendStatus.Rejected || reply.status==BackendStatus.Conflict || reply.status==BackendStatus.Unsupported)
                {
                    store.RecordBackendFailure(command,reply); store.DiscardPending(command.id);
                    await RefreshAuthorityAsync(ct); LastReply=reply;
                }
                else store.RecordBackendFailure(command,reply);
            }
            catch(Exception error) when(IsTransport(error))
            {
                LastReply=new BackendReply {status=BackendStatus.UnknownAfterTimeout,
                    code=error is OperationCanceledException?"CANCELLED_UNKNOWN":error is TimeoutException?"TIMEOUT_UNKNOWN":"CONNECTION_UNKNOWN",
                    message="요청 결과를 확인하지 못했습니다. 미확정 보상은 검증 대기이며 같은 요청으로 다시 확인합니다."};
                store.RecordBackendFailure(command,LastReply,error.ToString());
            }
            catch(Exception error)
            {
                store.RecordBackendFailure(command,LastReply,error.ToString());
                throw;
            }
            finally { Volatile.Write(ref busy,0); }
        }
        static bool IsTransport(Exception error)=>error is OperationCanceledException || error is TimeoutException || error is HttpRequestException;
    }
}
