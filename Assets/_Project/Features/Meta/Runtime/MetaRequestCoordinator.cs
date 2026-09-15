using System;
using System.Threading;
using System.Threading.Tasks;

namespace FateDice
{
    public enum MetaRequestState { Idle, Waiting, Unknown, Resolved }
    // No view references. A timed-out attempt can finish, but cannot replace a newer attempt's result.
    public sealed class MetaRequestCoordinator
    {
        readonly IMetaProgressionService service;
        readonly IMetaRequestJournal journal;
        MetaPendingRequest pending;
        long attempt;
        public MetaRequestState State { get; private set; }
        public MetaRequestResolution Resolution { get; private set; }
        public Exception Error { get; private set; }
        public MetaPendingRequest Pending => pending?.Copy();
        public bool HasPending => pending != null;
        public Task ActiveAttempt { get; private set; } = Task.CompletedTask;
        public MetaRequestCoordinator(IMetaProgressionService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            journal = service.RequestJournal ?? throw new ArgumentException("A persistent request journal is required.");
            pending = journal.Read(); State = pending == null ? MetaRequestState.Idle : MetaRequestState.Unknown;
        }
        public Task StartAsync(MetaRequest request)
        {
            ProfileRules.Require(!HasPending, "먼저 보관된 요청의 결과를 확인해 주세요.");
            var candidate = MetaPendingRequest.Create(journal.ProfileId, request);
            journal.Begin(candidate); pending = candidate;
            return ActiveAttempt = AttemptAsync(true);
        }
        public Task RetryAsync()
        {
            ProfileRules.Require(HasPending && State == MetaRequestState.Unknown, "결과가 불명확한 요청만 같은 내용으로 다시 시도할 수 있습니다.");
            journal.Begin(pending); return ActiveAttempt = AttemptAsync(true);
        }
        public Task CheckAsync()
        {
            ProfileRules.Require(HasPending && State != MetaRequestState.Waiting, "요청 확인이 진행 중입니다.");
            return ActiveAttempt = AttemptAsync(false);
        }
        async Task<MetaRequestResolution> DispatchAndCheck(MetaPendingRequest request, bool dispatch)
        {
            if (dispatch) await request.DispatchAsync(service);
            return await service.QueryRequestAsync(request.Copy());
        }
        async Task AttemptAsync(bool dispatch)
        {
            var owned = pending.Copy(); long version = ++attempt;
            var prior = Resolution;
            State = MetaRequestState.Waiting; Error = null; Resolution = null;
            using (var timer = new CancellationTokenSource())
            {
                var operation = DispatchAndCheck(owned, dispatch);
                try
                {
                    var delay = Task.Delay(TimeSpan.FromSeconds(service.ResponseWaitSeconds), timer.Token);
                    if (await Task.WhenAny(operation, delay) != operation)
                    {
                        // Observe a detached fault without touching coordinator or Unity state.
                        _ = operation.ContinueWith(t => { var ignored = t.Exception; }, CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        if (Owns(version, owned)) State = MetaRequestState.Unknown;
                        return;
                    }
                    var result = await operation;
                    if (!Owns(version, owned)) return;
                    Resolution = !dispatch && result.outcome == MetaRequestOutcome.Unknown && prior?.outcome == MetaRequestOutcome.Rejected ? prior : result;
                    State = Resolution.outcome == MetaRequestOutcome.Unknown ? MetaRequestState.Unknown : MetaRequestState.Resolved;
                }
                catch (Exception e)
                {
                    if (!Owns(version, owned)) return;
                    Error = e;
                    if (e is MetaCommandException rejection && rejection.Kind == MetaFailureKind.Rejected)
                    {
                        Resolution = new MetaRequestResolution { outcome = MetaRequestOutcome.Rejected, message = e.Message };
                        State = MetaRequestState.Resolved;
                    }
                    else State = MetaRequestState.Unknown;
                }
                finally { timer.Cancel(); }
            }
        }
        bool Owns(long version, MetaPendingRequest request) => version == attempt && pending?.fingerprint == request.fingerprint;
        public void Acknowledge(MetaPendingRequest request)
        {
            ProfileRules.Require(State == MetaRequestState.Resolved && pending?.fingerprint == request.fingerprint,
                "처리 결과를 확인한 요청만 완료할 수 있습니다.");
            journal.Clear(request); ++attempt; pending = null; State = MetaRequestState.Idle; Resolution = null; Error = null;
            ActiveAttempt = Task.CompletedTask;
        }
    }
}
