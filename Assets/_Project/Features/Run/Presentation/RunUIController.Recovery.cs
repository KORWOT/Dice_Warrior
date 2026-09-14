using System;
using System.Linq;

namespace FateDice
{
    public partial class RunUIController
    {
        public MetaFailureKind? RecoveryFailure { get; private set; }
        bool NeedsRecovery => RecoveryFailure.HasValue;
        long commandViewVersion;
        ProfileBackupPreview backupPreview;

        Action GuardInput(Action action, bool recovery = false)
        {
            long version = commandViewVersion;
            return () => { if (this && isActiveAndEnabled && version == commandViewVersion && (recovery || !NeedsRecovery && !MetaCommandsBlocked)) action?.Invoke(); };
        }
        Action<T> GuardInput<T>(Action<T> action)
        {
            long version = commandViewVersion;
            return value => { if (this && isActiveAndEnabled && version == commandViewVersion && !NeedsRecovery && !MetaCommandsBlocked) action(value); };
        }
        void RequireRecovery(MetaFailureKind kind)
        {
            if (!NeedsRecovery) commandViewVersion++;
            RecoveryFailure = kind;
        }
        void RenderRecovery()
        {
            string detail = MetaFailure.Message(RecoveryFailure.Value) + "\n\n현재 화면의 진행을 덮어쓰지 않고 저장을 먼저 읽습니다.\n불러오기에 실패하면 현재 상태와 원본 파일을 유지합니다.";
            var choices = new System.Collections.Generic.List<UIChoiceData>();
            if (backupPreview == null)
            {
                choices.Add(Choice("recovery-reload", "저장된 여정 다시 불러오기", ReloadSavedJourney, recovery: true));
                if (RecoveryFailure != MetaFailureKind.Stale)
                    choices.Add(Choice("recovery-backup", "검증된 백업 확인", InspectRecoveryBackup, recovery: true));
            }
            else
            {
                detail = "백업 보관 시각: " + backupPreview.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") +
                    "\n해결한 사건: " + backupPreview.EventsResolved + "개\n\n이 시점 이후의 여정과 성장은 사라질 수 있습니다." +
                    "\n프로필·여정·정산 기록을 이 백업 시점으로 함께 되돌립니다.\n손상 원본과 정상 백업은 별도로 보존합니다.";
                choices.Add(Choice("recovery-confirm", "진행 손실을 이해했으며 백업 복원", ConfirmRecoveryBackup, recovery: true));
                choices.Add(Choice("recovery-cancel", "복원하지 않고 돌아가기", () => { commandViewVersion++; backupPreview = null; RenderRecovery(); }, recovery: true));
            }
            Show<MenuUI>(new MenuUIData
            {
                context = Context(PreviewConfig.presentation), metaEnabled = true, recoveryMode = true,
                hud = new RunHUDData { header = "저장 확인", notice = error },
                characterName = RecoveryFailure == MetaFailureKind.Stale ? "저장된 진행과 현재 화면이 다릅니다" : "저장 확인이 필요합니다",
                characterDetails = detail,
                recoveryChoices = choices.ToArray()
            }, false);
        }
        void InspectRecoveryBackup()
        {
            if (Busy || Meta == null || !NeedsRecovery) return;
            commandViewVersion++; backupPreview = null;
            try
            {
                backupPreview = Meta.InspectBackup();
                error = backupPreview == null ? "복구할 정상 백업이 없습니다. 원본을 보존하고 다시 읽기를 시도해 주세요." : null;
            }
            catch (Exception e) { error = "백업을 사용하지 못했습니다. " + PlayerError(e); }
            RenderRecovery();
        }
        void ConfirmRecoveryBackup()
        {
            if (Busy || Meta == null || !NeedsRecovery || backupPreview == null) return;
            var confirmed = backupPreview;
            commandViewVersion++; backupPreview = null;
            try
            {
                Meta.RestoreBackup(confirmed);
                // The restored generation rejects every pre-restore checkpoint/request, even after an ABA revision rollback.
                ReloadSavedJourney();
            }
            catch (Exception e) { error = "백업 복원을 완료하지 못했습니다. " + PlayerError(e); RenderRecovery(); }
        }
        void ReloadSavedJourney()
        {
            if (Busy || Meta == null || !NeedsRecovery) return;
            PlayerSaveDocument snapshot;
            RunSession candidate;
            bool settled;
            try
            {
                // Read the run and settlement from the same validated document, without autosave.
                snapshot = Meta.ReadRecoverySnapshot();
                snapshot.Validate(snapshot.profile.profileId);
                settled = snapshot.run != null && snapshot.settlements.Any(x => x.runId == snapshot.run.runId);
                candidate = snapshot.run == null || settled ? null : new RunSession(snapshot.run, Store);
            }
            catch (Exception e)
            {
                RequireRecovery(MetaFailure.Classify(e)); error = PlayerError(e); RenderRecovery(); return;
            }

            // All reads and candidate validation succeeded. Old UI delegates cannot command this session.
            commandViewVersion++; metaViewVersion++;
            Playback.Cancel(); StopAllCoroutines(); UI.CloseAll();
            presentingFate = shownFate = null; presentingEntry = shownCombat = null;
            actionBusy = false;
            Session = candidate; savedPreview = snapshot.run?.DeepCopy();
            RecoveryFailure = null; backupPreview = null; invalidSave = false; error = null;
            atTitle = false; menu = candidate == null;
            if (candidate == null)
            {
                if (navigation == null || !navigation.RequestLobby()) Render();
            }
            else if (navigation == null || !navigation.RequestInGame()) Render();
            SyncInputLock();
        }
    }
}
