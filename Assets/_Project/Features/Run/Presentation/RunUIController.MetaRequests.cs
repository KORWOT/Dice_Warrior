using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FateDice
{
    public partial class RunUIController
    {
        MetaRequestCoordinator metaRequests;
        Exception metaJournalError;
        bool metaRequestVisible;
        bool MetaCommandsBlocked => metaRequests?.HasPending == true || metaJournalError != null;
        void InitializeMetaRequests()
        {
            if (Meta == null || metaRequests != null) return;
            try { metaRequests = new MetaRequestCoordinator(Meta); metaJournalError = null; }
            catch (Exception e) { metaJournalError = e; }
        }
        void OpenMetaRequest()
        {
            if (Busy) return;
            InitializeMetaRequests(); metaRequestVisible = true;
            // Reattach this binding to the same attempt; reopening never sends a new request.
            if (metaRequests?.State == MetaRequestState.Waiting)
            { ObserveMetaRequest(metaRequests.ActiveAttempt); return; }
            commandViewVersion++; metaViewVersion++; RenderMetaRequest();
        }
        void CloseMetaRequest()
        {
            commandViewVersion++; metaViewVersion++; metaRequestVisible = false;
            menu = true; atTitle = false; Render();
        }
        void RunMetaRequest(MetaRequest request)
        {
            if (Busy || MetaCommandsBlocked || NeedsRecovery) return;
            InitializeMetaRequests();
            if (metaJournalError != null) { OpenMetaRequest(); return; }
            try { ObserveMetaRequest(metaRequests.StartAsync(request)); }
            catch (Exception e)
            {
                // A failure before dispatch cannot be treated as a lost service response.
                // Re-read the journal in case an I/O acknowledgement was itself interrupted.
                metaRequests = null; InitializeMetaRequests(); error = e.Message;
                if (MetaCommandsBlocked) OpenMetaRequest(); else Render();
            }
        }
        void CheckMetaRequest(bool retry)
        {
            if (Busy) return;
            InitializeMetaRequests();
            if (metaJournalError != null || !metaRequests.HasPending) { Render(); return; }
            try { ObserveMetaRequest(retry ? metaRequests.RetryAsync() : metaRequests.CheckAsync()); }
            catch (Exception e) { error = e.Message; RenderMetaRequest(); }
        }
        async void ObserveMetaRequest(Task operation)
        {
            metaRequestVisible = !operation.IsCompleted; error = null; commandViewVersion++; metaViewVersion++;
            if (metaRequestVisible) RenderMetaRequest();
            int version = metaViewVersion; var view = UI.ActiveScreen; long binding = view.BindingRevision;
            var request = metaRequests.Pending;
            await operation;
            // All UI writes, including error/cleanup, are owned by this exact binding.
            if (!this || !isActiveAndEnabled || version != metaViewVersion || !UI || UI.ActiveScreen != view ||
                !view || !view.IsOpen || view.BindingRevision != binding || metaRequests.Pending?.fingerprint != request?.fingerprint) return;
            if (metaRequests.Error != null) { Debug.LogWarning(metaRequests.Error.Message); error = metaRequests.Error.Message; }
            if (metaRequests.State == MetaRequestState.Resolved)
            {
                try { ApplyMetaResolution(request); }
                catch (Exception e) { error = e.Message; RenderMetaRequest(); }
            }
            else RenderMetaRequest();
        }
        void ApplyMetaResolution(MetaPendingRequest request)
        {
            var result = metaRequests.Resolution; var snapshot = result.snapshot;
            RunSession candidate = null;
            if (snapshot != null)
            {
                snapshot.Validate(Meta.RequestJournal.ProfileId);
                if (result.outcome == MetaRequestOutcome.Committed && request.kind == "start" &&
                    snapshot.run?.runId == result.runId && !snapshot.settlements.Any(x => x.runId == result.runId))
                    candidate = new RunSession(snapshot.run, Store);
            }
            // Clear only this confirmed record; failed cleanup retains a visible retry/query path.
            metaRequests.Acknowledge(request);
            commandViewVersion++; metaViewVersion++; metaRequestVisible = false;
            if (snapshot != null)
            {
                if (request.kind == "start" || request.kind == "settle" || result.outcome == MetaRequestOutcome.SupersededByRecovery)
                { Playback.Cancel(); StopAllCoroutines(); UI.CloseAll(); }
                actionBusy = false; Session = candidate; savedPreview = snapshot.run?.DeepCopy();
                invalidSave = false; RecoveryFailure = null;
            }
            error = result.message; menu = candidate == null; atTitle = false;
            if (candidate != null)
            {
                selectedSeed = candidate.ReadSnapshot().initialSeed;
                if (navigation == null || !navigation.RequestInGame()) Render();
            }
            else if (navigation == null || !navigation.RequestLobby()) Render();
            SyncInputLock();
        }
        void RenderMetaRequest()
        {
            var choices = new List<UIChoiceData>();
            bool waiting = metaRequests?.State == MetaRequestState.Waiting;
            string detail;
            if (metaJournalError != null)
            {
                detail = "보관된 요청 기록을 읽지 못했습니다. 원본을 유지하며 새 요청을 중단합니다.\n" + metaJournalError.Message;
                choices.Add(Choice("meta-request-check", "요청 기록 다시 읽기", () => { InitializeMetaRequests(); Render(); }, recovery: true));
            }
            else
            {
                detail = waiting ? "요청을 처리하고 있습니다. 이 화면을 닫아도 요청은 유지됩니다." :
                    "처리 결과를 확인해야 합니다. 시간이 지났다는 이유로 실패하거나 취소된 것은 아닙니다.\n같은 요청의 결과를 확인하거나 원래 내용 그대로 다시 시도할 수 있습니다.";
                if (metaRequests?.Pending != null)
                {
                    var request = metaRequests.Pending;
                    string label = request.kind == "start" ? "새 여정" : request.kind == "grow" ? "캐릭터 성장" : request.kind == "settle" ? "여정 정산" : "출전 설정";
                    detail = label + "\n\n" + detail;
                    choices.Add(Choice("meta-request-check", "처리 결과 확인", () => CheckMetaRequest(false), !waiting, recovery: true));
                    choices.Add(Choice("meta-request-retry", "같은 요청 다시 시도", () => CheckMetaRequest(true),
                        !waiting && metaRequests.State != MetaRequestState.Resolved, recovery: true));
                }
            }
            choices.Add(Choice("meta-request-close", "닫고 준비 화면으로", CloseMetaRequest, recovery: true));
            Show<MenuUI>(new MenuUIData { context = Context(PreviewConfig.presentation), metaEnabled = true, recoveryMode = true,
                hud = new RunHUDData { header = "요청 확인", notice = error }, characterName = waiting ? "처리 중" : "처리 결과 확인 필요",
                characterDetails = detail, recoveryChoices = choices.ToArray() }, false);
        }
    }
}
