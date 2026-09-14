using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    [Category("RecoveryAsync")] public sealed class RecoveryStabilityAsyncTests
    {
        GameApplication app;
        MetaProgressionConfig meta;
        GameConfigData rules;
        LocalPlayerDataStore disk;
        LocalMetaProgressionService service; ControlledMetaService controlled;
        ProbeStore probe;
        sealed class ProbeStore : IPlayerDataStore, IPlayerDataRecoveryStore
        {
            readonly IPlayerDataStore inner;
            public bool failAfter, failBefore, failRead;
            public ProbeStore(IPlayerDataStore inner) { this.inner = inner; }
            public string ProfileId => inner.ProfileId;
            public IMetaRequestJournal RequestJournal => inner.RequestJournal;
            public bool Exists => inner.Exists;
            public PlayerSaveDocument Read() => failRead ? throw new IOException("recovery read unavailable") : inner.Read();
            public ProfileBackupPreview InspectBackup() => ((IPlayerDataRecoveryStore)inner).InspectBackup();
            public PlayerSaveDocument RestoreBackup(ProfileBackupPreview preview) => ((IPlayerDataRecoveryStore)inner).RestoreBackup(preview);
            public void Write(long revision, PlayerSaveDocument candidate)
            { if (failBefore) throw new IOException("recovery write unavailable"); inner.Write(revision, candidate); if (failAfter) throw new IOException("UI meta response lost"); }
        }
        string directory, userRun, userMeta;
        byte[] userRunBytes, userMetaBytes;
        RunUIController C => app.controller;
        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null);
            directory = Path.Combine(Path.GetTempPath(), "FateDiceRecoveryAsync", Guid.NewGuid().ToString("N"));
            userRun = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            userMeta = LocalPlayerDataStore.PlayerPath(Path.Combine(Application.persistentDataPath, "FateDiceMeta"), "local-development");
            userRunBytes = File.Exists(userRun) ? File.ReadAllBytes(userRun) : null;
            userMetaBytes = File.Exists(userMeta) ? File.ReadAllBytes(userMeta) : null;
            meta = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            meta.startingEquipmentIds = new[] { "ember_blade" }; meta.extraDieIds = new[] { "ember" };
            rules = Prefab().controller.config.Snapshot(); rules.world.eventsToBoss = 1; rules.world.mapGenerationVersion = 0;
            rules.growth.startingPower = 500; rules.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 };
            disk = new LocalPlayerDataStore(Path.Combine(directory, "player.json"), "ui-player");
            probe = new ProbeStore(disk);
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(33));
            controlled = new ControlledMetaService(service);
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            if (meta) Object.Destroy(meta);
            yield return null;
            controlled?.Cancel(); yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            Preserve(userRun, userRunBytes); Preserve(userMeta, userMetaBytes);
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceRecoveryAsync")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
            LogAssert.NoUnexpectedReceived();
        }
        static void Preserve(string path, byte[] before)
        {
            Assert.That(File.Exists(path), Is.EqualTo(before != null), path);
            if (before != null) CollectionAssert.AreEqual(before, File.ReadAllBytes(path), path);
        }
        static GameApplication Prefab()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>("Assets/_Project/Features/Run/Prefabs/GameApplication.prefab");
#else
            throw new InvalidOperationException("Editor-only authoring integration test.");
#endif
        }
        IEnumerator Open(int width, int height, string scene = "Lobby")
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "Meta progression");
#endif
            app = GameApplication.Bootstrap(Prefab(), metaService: controlled);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/" + scene + ".unity");
            yield return Until(() => !C.Busy && (scene == "Title" ? C.UI.ActiveScreen is TitleUI : scene == "Lobby" ? C.UI.ActiveScreen is MenuUI : C.Session != null));
            yield return Layout();
        }
        sealed class ControlledMetaService : IMetaProgressionService
        {
            public sealed class Call
            {
                public MetaPendingRequest request;
                public System.Threading.Tasks.Task task;
                public Action accept, complete, cancel;
                public Action<Exception> fail;
            }
            readonly IMetaProgressionService inner;
            public readonly List<Call> calls = new List<Call>();
            public ControlledMetaService(IMetaProgressionService inner) { this.inner = inner; }
            public PlayerProfileData Profile => inner.Profile;
            public MetaPolicy Policy => inner.Policy;
            public IMetaRequestJournal RequestJournal => inner.RequestJournal;
            public double ResponseWaitSeconds => inner.ResponseWaitSeconds;
            public System.Threading.Tasks.Task<MetaRequestResolution> QueryRequestAsync(MetaPendingRequest value) => inner.QueryRequestAsync(value);
            public IRunStore RunStore => inner.RunStore;
            public bool NeedsSettlement => inner.NeedsSettlement;
            public SettlementReceipt Settlement(string runId) => inner.Settlement(runId);
            public PlayerSaveDocument ReadRecoverySnapshot() => inner.ReadRecoverySnapshot();
            public ProfileBackupPreview InspectBackup() => inner.InspectBackup();
            public PlayerSaveDocument RestoreBackup(ProfileBackupPreview preview) => inner.RestoreBackup(preview);
            public System.Threading.Tasks.Task<PlayerProfileData> SaveLoadoutAsync(LoadoutRequest value) => Hold(value, r => inner.SaveLoadoutAsync((LoadoutRequest)r));
            public System.Threading.Tasks.Task<PlayerProfileData> GrowAsync(GrowthRequest value) => Hold(value, r => inner.GrowAsync((GrowthRequest)r));
            public System.Threading.Tasks.Task<SettlementReceipt> SettleAsync(SettlementRequest value) => Hold(value, r => inner.SettleAsync((SettlementRequest)r));
            public System.Threading.Tasks.Task<RunState> StartRunAsync(StartRunRequest value) => Hold(value, r => inner.StartRunAsync((StartRunRequest)r));
            System.Threading.Tasks.Task<T> Hold<T>(MetaRequest value, Func<MetaRequest, System.Threading.Tasks.Task<T>> commit)
            {
                var input = MetaPendingRequest.Create(RequestJournal.ProfileId, value);
                var source = new System.Threading.Tasks.TaskCompletionSource<T>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
                bool accepted = false; T result = default;
                // This delegate invokes only the synchronous local test authority. The returned UI Task remains independently controlled.
                Action accept = () => { if (!accepted) { result = commit(input.Request()).GetAwaiter().GetResult(); accepted = true; } };
                calls.Add(new Call { request = input.Copy(), task = source.Task, accept = accept,
                    complete = () => { try { accept(); source.TrySetResult(result); } catch (Exception e) { source.TrySetException(e); } },
                    fail = e => source.TrySetException(e), cancel = () => source.TrySetCanceled() });
                return source.Task;
            }
            public void CompleteStart() => calls[0].complete();
            public void Cancel() { foreach (var call in calls) call.cancel(); }
        }
        [UnityTest] public IEnumerator WaitingCanCloseAndReopenWhileOldSuccessCannotNavigateTheNewBinding()
        {
            yield return Open(720, 1280);
            Click(Button("new")); yield return Layout();
            Assert.That(controlled.calls.Count, Is.EqualTo(1)); Assert.That(controlled.calls[0].task.IsCompleted, Is.False);
            Assert.That(disk.Exists, Is.False, "The delayed service has not accepted a departure yet.");
            Assert.That(C.Widgets.Buttons.ContainsKey("meta-request-close"), Is.True,
                "Waiting must expose an operable close/recovery path instead of locking the whole UI.");
            Click(Button("meta-request-close")); yield return Layout();
            C.enabled = false; yield return null; C.enabled = true; yield return Layout();
            var view = C.UI.ActiveScreen;
            controlled.CompleteStart(); yield return Layout(); yield return Layout();
            Assert.That(C.Session, Is.Null, "An old success cannot bind its session in a reopened view.");
            Assert.That(C.UI.ActiveScreen, Is.SameAs(view)); Assert.That(C.Busy, Is.False);
            Click(Button("meta-request-status")); yield return Layout();
            Click(Button("meta-request-check")); yield return Until(() => !C.Busy && C.Session != null); yield return Layout();
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(service.RunStore.Load().runId));
        }
        [UnityTest] public IEnumerator NormalDelayedSuccessWaitsAndAppliesOneDeparture()
        {
            yield return Open(1080, 2400); Click(Button("new")); yield return Layout();
            var original = service.RequestJournal.Read();
            Assert.That(original.kind, Is.EqualTo("start")); Assert.That(disk.Exists, Is.False); Assert.That(C.Session, Is.Null);
            Assert.That(Button("meta-request-check").interactable, Is.False);
            controlled.calls[0].complete(); yield return Until(() => C.Session != null && !C.Busy); yield return Layout();
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<ExplorationUI>());
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1)); Assert.That(service.RequestJournal.Read(), Is.Null);
        }
        [UnityTest] public IEnumerator AcceptedDepartureWithLostResponseQueriesWithoutRollingOrStartingAgain()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            controlled.calls[0].accept(); var accepted = disk.Read(); var primary = File.ReadAllBytes(disk.Path);
            LogAssert.Expect(LogType.Warning, "response lost"); controlled.calls[0].fail(new IOException("response lost"));
            yield return Until(() => Button("meta-request-check").interactable); yield return Layout();
            Assert.That(C.Session, Is.Null); Assert.That(service.RequestJournal.Read(), Is.Not.Null);
            Click(Button("meta-request-check")); yield return Until(() => C.Session != null && !C.Busy); yield return Layout();
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(accepted.run.runId));
            Assert.That(C.Session.ReadSnapshot().rngState, Is.EqualTo(accepted.run.rngState));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(disk.Path)); Assert.That(controlled.calls.Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator TimeoutKeepsExactRequestAndOldFailureCannotReleaseNewAttempt()
        {
            meta.responseWaitSeconds = .1f;
            yield return Open(720, 1280); Click(Button("new"));
            yield return Until(() => Button("meta-request-retry").interactable); yield return Layout();
            var first = service.RequestJournal.Read(); var bytes = File.ReadAllBytes(disk.Path + ".pending");
            Assert.That(disk.Exists, Is.False); Assert.That(controlled.calls[0].task.IsCompleted, Is.False);
            meta.responseWaitSeconds = 30;
            Click(Button("meta-request-retry")); yield return Layout();
            Assert.That(controlled.calls.Count, Is.EqualTo(2));
            Assert.That(MetaJson.Encode(controlled.calls[1].request), Is.EqualTo(MetaJson.Encode(first)));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path + ".pending"));
            var view = C.UI.ActiveScreen; long binding = view.BindingRevision;
            controlled.calls[0].fail(new IOException("old attempt failure")); yield return Layout(); yield return Layout();
            Assert.That(C.UI.ActiveScreen, Is.SameAs(view)); Assert.That(view.BindingRevision, Is.EqualTo(binding));
            Assert.That(Button("meta-request-retry").interactable, Is.False); Assert.That(C.Session, Is.Null);
            controlled.calls[1].complete(); yield return Until(() => C.Session != null && !C.Busy);
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1)); Assert.That(service.RequestJournal.Read(), Is.Null);
        }
        [UnityTest] public IEnumerator BeforeCommitFailureRetriesTheOriginalIdAndInput()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            var original = service.RequestJournal.Read();
            LogAssert.Expect(LogType.Warning, "offline before commit"); controlled.calls[0].fail(new IOException("offline before commit"));
            yield return Until(() => Button("meta-request-retry").interactable); yield return Layout();
            Assert.That(disk.Exists, Is.False); Assert.That(C.Session, Is.Null);
            Click(Button("meta-request-retry")); yield return Layout();
            Assert.That(MetaJson.Encode(controlled.calls[1].request), Is.EqualTo(MetaJson.Encode(original)));
            controlled.calls[1].complete(); yield return Until(() => C.Session != null && !C.Busy);
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator LateOldResponseCannotChangeANewGrowthRequestsWaitingBinding()
        {
            meta.startingCurrency = 100; meta.responseWaitSeconds = .1f;
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(33)); controlled = new ControlledMetaService(service);
            yield return Open(720, 1280); Click(Button("new")); controlled.calls[0].accept();
            yield return Until(() => Button("meta-request-check").interactable); yield return Layout();
            Click(Button("meta-request-check")); yield return Until(() => C.Session != null && !C.Busy);
            yield return Layout();
            Click(Button("menu")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            meta.responseWaitSeconds = 30;
            Click(((MenuUI)C.UI.ActiveScreen).growthTab); yield return Layout(); yield return Reveal(Button("meta-grow"));
            Click(Button("meta-grow")); yield return Layout();
            Assert.That(controlled.calls.Count, Is.EqualTo(2));
            Assert.That(controlled.calls[1].request.Request().requestId, Is.Not.EqualTo(controlled.calls[0].request.Request().requestId));
            var view = C.UI.ActiveScreen; long binding = view.BindingRevision;
            controlled.calls[0].fail(new IOException("old departure fault after a new growth request")); yield return Layout(); yield return Layout();
            Assert.That(view.BindingRevision, Is.EqualTo(binding)); Assert.That(Button("meta-request-check").interactable, Is.False);
            Assert.That(controlled.calls[1].task.IsCompleted, Is.False); Assert.That(service.Profile.characters[0].rank, Is.Zero);
            controlled.calls[1].complete(); yield return Until(() => !C.Busy && service.RequestJournal.Read() == null);
            Assert.That(service.Profile.characters[0].rank, Is.EqualTo(1)); Assert.That(disk.Read().operations.Count, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator ApplicationRestartLoadsPendingRequestAndQueriesCommittedDeparture()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            var original = service.RequestJournal.Read(); controlled.calls[0].accept(); var id = disk.Read().run.runId;
            var previous = controlled;
            Object.Destroy(app.gameObject); yield return null; Assert.That(GameApplication.Current, Is.Null);
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(66));
            controlled = new ControlledMetaService(service);
            yield return Open(1080, 2400);
            Assert.That(C.Session, Is.Null); Assert.That(controlled.calls, Is.Empty);
            Assert.That(MetaJson.Encode(service.RequestJournal.Read()), Is.EqualTo(MetaJson.Encode(original)));
            previous.calls[0].complete(); yield return Layout(); Assert.That(C.Session, Is.Null);
            Click(Button("meta-request-status")); yield return Layout();
            Click(Button("meta-request-check")); yield return Until(() => C.Session != null && !C.Busy);
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(id)); Assert.That(disk.Read().operations.Count, Is.EqualTo(1));
        }
        void RebindAndLock(string notice)
        {
            var data = (MenuUIData)typeof(BaseUI<MenuUIData>).GetProperty("Data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(C.UI.ActiveScreen);
            C.UI.Show<MenuUI>(new MenuUIData { context = data.context, metaEnabled = true, recoveryMode = true,
                characterName = "새 바인딩", characterDetails = notice, hud = new RunHUDData { notice = notice }, recoveryChoices = Array.Empty<UIChoiceData>() });
            C.UI.SetInputLocked(true);
        }
        [UnityTest] public IEnumerator RestartBeforeAcceptanceCanLeaveTitleAndRetryTheSameStoredIntent()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            var original = service.RequestJournal.Read(); var previous = controlled;
            Object.Destroy(app.gameObject); yield return null; previous.Cancel(); yield return null;
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(66)); controlled = new ControlledMetaService(service);
            yield return Open(720, 1280, "Title");
            Assert.That(disk.Exists, Is.False); Assert.That(C.Session, Is.Null);
            Click(((TitleUI)C.UI.ActiveScreen).enterButton.GetComponent<Button>());
            yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            Click(Button("meta-request-status")); yield return Layout(); Click(Button("meta-request-retry")); yield return Layout();
            Assert.That(MetaJson.Encode(controlled.calls[0].request), Is.EqualTo(MetaJson.Encode(original)));
            controlled.calls[0].complete(); yield return Until(() => C.Session != null && !C.Busy);
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1)); Assert.That(service.RequestJournal.Read(), Is.Null);
        }
        [UnityTest] public IEnumerator LateFailureCannotWriteToOrUnlockExternallyReboundSameView()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            RebindAndLock("다른 화면이 소유한 안내"); var view = C.UI.ActiveScreen; long binding = view.BindingRevision;
            controlled.calls[0].fail(new IOException("late failure must stay out of this view")); yield return Layout(); yield return Layout();
            Assert.That(C.Session, Is.Null); Assert.That(view.BindingRevision, Is.EqualTo(binding)); Assert.That(view.group.interactable, Is.False);
            Assert.That(((MenuUI)view).characterDetails.text, Is.EqualTo("다른 화면이 소유한 안내"));
            Assert.That(service.RequestJournal.Read(), Is.Not.Null); C.UI.SetInputLocked(false);
        }
        [UnityTest] public IEnumerator LateSuccessCannotReplaceOrUnlockExternallyReboundSameView()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            RebindAndLock("새 화면 유지"); var view = C.UI.ActiveScreen; long binding = view.BindingRevision;
            controlled.calls[0].complete(); yield return Layout(); yield return Layout();
            Assert.That(C.Session, Is.Null); Assert.That(view.BindingRevision, Is.EqualTo(binding)); Assert.That(view.group.interactable, Is.False);
            Assert.That(((MenuUI)view).characterDetails.text, Is.EqualTo("새 화면 유지"));
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1)); C.UI.SetInputLocked(false);
        }
        [UnityTest] public IEnumerator LostGrowthResponseAndSameRequestRetrySpendOnlyOnce()
        {
            // Recreate the local authority before any profile write so this is a valid owned starting balance.
            meta.startingCurrency = 100;
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(33)); controlled = new ControlledMetaService(service);
            yield return Open(1080, 2400); Click(((MenuUI)C.UI.ActiveScreen).growthTab); yield return Layout();
            yield return Reveal(Button("meta-grow")); Click(Button("meta-grow")); yield return Layout();
            controlled.calls[0].accept(); var paid = service.Profile;
            LogAssert.Expect(LogType.Warning, "growth response lost"); controlled.calls[0].fail(new IOException("growth response lost"));
            yield return Until(() => Button("meta-request-retry").interactable); yield return Layout();
            Click(Button("meta-request-retry")); yield return Layout();
            Assert.That(MetaJson.Encode(controlled.calls[1].request), Is.EqualTo(MetaJson.Encode(controlled.calls[0].request)));
            controlled.calls[1].complete(); yield return Until(() => service.RequestJournal.Read() == null); yield return Layout();
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(paid.growthCurrency)); Assert.That(service.Profile.characters[0].rank, Is.EqualTo(1));
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator JournalWriteFailurePreventsDispatchAndPreservesProfile()
        {
            yield return Open(720, 1280);
            Directory.CreateDirectory(disk.Path + ".pending"); Click(Button("new")); yield return Layout();
            Assert.That(controlled.calls, Is.Empty); Assert.That(disk.Exists, Is.False); Assert.That(C.Session, Is.Null);
            Assert.That(Directory.Exists(disk.Path + ".pending"), Is.True);
        }
        [UnityTest] public IEnumerator LostSettlementResponseAndRetryGrantOnlyOneReceiptAndReward()
        {
            var first = service.StartRunAsync(new StartRunRequest { requestId = "fixture" }).GetAwaiter().GetResult();
            var run = new RunSession(first, service.RunStore); Complete(run);
            yield return Open(1080, 2400, "InGame"); Click(Button("restart")); yield return Layout();
            controlled.calls[0].accept(); var paid = disk.Read();
            LogAssert.Expect(LogType.Warning, "settlement response lost"); controlled.calls[0].fail(new IOException("settlement response lost"));
            yield return Until(() => Button("meta-request-retry").interactable); yield return Layout();
            Click(Button("meta-request-retry")); yield return Layout();
            Assert.That(MetaJson.Encode(controlled.calls[1].request), Is.EqualTo(MetaJson.Encode(controlled.calls[0].request)));
            controlled.calls[1].complete(); yield return Until(() => !C.Busy && service.RequestJournal.Read() == null); yield return Layout();
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<MenuUI>()); Assert.That(service.Profile.growthCurrency, Is.EqualTo(paid.profile.growthCurrency));
            Assert.That(disk.Read().settlements.Count, Is.EqualTo(1)); Assert.That(disk.Read().operations.Count, Is.EqualTo(paid.operations.Count));
        }
        [UnityTest] public IEnumerator RestoredProfileRejectsLateOldRequestAndUiQueriesItsDisposition()
        {
            var initial = service.StartRunAsync(new StartRunRequest { requestId = "fixture" }).GetAwaiter().GetResult();
            yield return Open(720, 1280); Click(((MenuUI)C.UI.ActiveScreen).settingsTab); yield return Layout();
            yield return Reveal(Button("trial-bastion")); Click(Button("trial-bastion")); yield return Layout();
            var pending = service.RequestJournal.Read();
            var session = new RunSession(initial, service.RunStore); session.RecordElapsed(1); session.SaveCheckpoint();
            var backup = disk.InspectBackup(); File.WriteAllText(disk.Path, "{corrupt while pending"); var restored = disk.RestoreBackup(backup);
            var bytes = File.ReadAllBytes(disk.Path);
            LogAssert.Expect(LogType.Warning, "복구 이전의 요청입니다. 저장된 진행을 확인해 주세요."); controlled.calls[0].complete();
            yield return Until(() => Button("meta-request-check").interactable); yield return Layout();
            Assert.That(MetaJson.Encode(service.RequestJournal.Read()), Is.EqualTo(MetaJson.Encode(pending)));
            Click(Button("meta-request-check")); yield return Until(() => !C.Busy && service.RequestJournal.Read() == null); yield return Layout();
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<MenuUI>()); Assert.That(C.Session, Is.Null);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
            Assert.That(service.Profile.loadout.wildcardId, Is.EqualTo(restored.profile.loadout.wildcardId)); Assert.That(controlled.calls.Count, Is.EqualTo(1));
        }
        static void Complete(RunSession run)
        {
            for (int i = 0; i < 300 && run.Phase != RunPhase.Result; i++)
            {
                var s = run.ReadSnapshot();
                switch (s.phase)
                {
                    case RunPhase.Map: Assert.That(run.ChooseNode(s.availableNodeIds[0]), Is.True); break;
                    case RunPhase.ExplorationRoll: case RunPhase.CombatRoll: Assert.That(run.Roll(), Is.True); break;
                    case RunPhase.ExplorationCards: Assert.That(run.ChooseFate(s.cards[0].id), Is.True); break;
                    case RunPhase.CombatCards: Assert.That(run.ChooseAction(s.cards.OrderByDescending(x => CombatRules.Evaluate(s, x).damage).First().id), Is.True); break;
                    case RunPhase.Encounter: Assert.That(run.ResolveEncounter(false), Is.True); break;
                    case RunPhase.Reward: Assert.That(run.ClaimReward(), Is.True); break;
                    case RunPhase.EquipmentChoice:
                        if (!string.IsNullOrEmpty(s.pendingEquipmentId)) run.Equip(false); else run.ReplaceDie(-1); break;
                    case RunPhase.Shop: run.LeaveShop(); break;
                }
            }
            Assert.That(run.ReadSnapshot().won, Is.True);
        }
        [UnityTest] public IEnumerator FailedJournalCleanupKeepsResultQueryableWithoutPayingAgain()
        {
            yield return Open(720, 1280); Click(Button("new")); yield return Layout();
            using (var held = new FileStream(disk.Path + ".pending", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                controlled.calls[0].complete(); yield return Until(() => controlled.calls[0].task.IsCompleted); yield return Layout(); yield return Layout();
                Assert.That(C.Session, Is.Null); Assert.That(service.RequestJournal.Read(), Is.Not.Null);
                Assert.That(Button("meta-request-check").interactable, Is.True);
            }
            Click(Button("meta-request-check")); yield return Until(() => C.Session != null && !C.Busy);
            Assert.That(disk.Read().operations.Count, Is.EqualTo(1)); Assert.That(service.RequestJournal.Read(), Is.Null);
        }
        Button Button(string key)
        { Assert.That(C.Widgets.Buttons.ContainsKey(key), Is.True, key); return C.Widgets.Buttons[key]; }
        IEnumerator Reveal(Button button)
        {
            yield return Layout();
            var scroll = ((MenuUI)C.UI.ActiveScreen).layout.scroll;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, button.transform);
            var position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + scroll.viewport.rect.center.y - bounds.center.y, 0,
                Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
            scroll.StopMovement(); scroll.content.anchoredPosition = position;
            yield return Layout();
        }
        void Click(Button button)
        {
            Assert.That(C.Busy, Is.False); Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = (RectTransformUtility.WorldToScreenPoint(null, corners[0]) + RectTransformUtility.WorldToScreenPoint(null, corners[2])) * .5f };
            var hits = new List<RaycastResult>(); C.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var target = hits.Select(h => ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject)).FirstOrDefault(x => x != null);
            Assert.That(target, Is.SameAs(button.gameObject), "The authored UI raycast must reach " + button.name);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
        static IEnumerator Layout() { yield return null; Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases(); }
        static IEnumerator Until(Func<bool> condition)
        { float limit = Time.realtimeSinceStartup + 15; while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(limit)); yield return null; } }
    }
}
