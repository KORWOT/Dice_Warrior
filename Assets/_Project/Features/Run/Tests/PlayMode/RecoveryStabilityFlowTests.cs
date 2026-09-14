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
    [Category("RecoveryStability")] public sealed class RecoveryStabilityFlowTests
    {
        GameApplication app;
        MetaProgressionConfig meta;
        GameConfigData rules;
        LocalPlayerDataStore disk;
        LocalMetaProgressionService service;
        ProbeStore probe;
        sealed class ProbeStore : IPlayerDataStore, IPlayerDataRecoveryStore
        {
            readonly IPlayerDataStore inner;
            public bool failAfter, failBefore, failRead;
            public ProbeStore(IPlayerDataStore inner) { this.inner = inner; }
            public string ProfileId => inner.ProfileId;
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
            directory = Path.Combine(Path.GetTempPath(), "FateDiceRecoveryFlow", Guid.NewGuid().ToString("N"));
            userRun = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            userMeta = LocalPlayerDataStore.PlayerPath(Path.Combine(Application.persistentDataPath, "FateDiceMeta"), "local-development");
            userRunBytes = File.Exists(userRun) ? File.ReadAllBytes(userRun) : null;
            userMetaBytes = File.Exists(userMeta) ? File.ReadAllBytes(userMeta) : null;
            meta = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            meta.startingEquipmentIds = new[] { "ember_blade" }; meta.extraDieIds = new[] { "ember" };
            rules = Prefab().controller.config.Snapshot(); rules.world.eventsToBoss = 3; rules.world.mapGenerationVersion = 0;
            rules.growth.startingPower = 500; rules.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 };
            disk = new LocalPlayerDataStore(Path.Combine(directory, "player.json"), "ui-player");
            probe = new ProbeStore(disk);
            service = new LocalMetaProgressionService(probe, () => rules.DeepCopy(), meta, new FixedSeedSource(33));
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            if (meta) Object.Destroy(meta);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            Preserve(userRun, userRunBytes); Preserve(userMeta, userMetaBytes);
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceRecoveryFlow")) + Path.DirectorySeparatorChar;
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
            app = GameApplication.Bootstrap(Prefab(), metaService: service);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/" + scene + ".unity");
            yield return Until(() => !C.Busy && (scene == "Lobby" ? C.UI.ActiveScreen is MenuUI : C.Session != null));
            yield return Layout();
        }
        // RV-01: resolving a stale encounter exposes recovery instead of stranding the UI.
        [UnityTest] public IEnumerator StaleCheckpointOffersExplicitReloadWithoutSavingTheOldSession()
        {
            var initial = service.StartRunAsync(new StartRunRequest { requestId = "rv-start" }).GetAwaiter().GetResult();
            var setup = new RunSession(initial, service.RunStore);
            Assert.That(setup.ChooseNode(setup.ReadSnapshot().availableNodeIds[0]), Is.True);
            Assert.That(setup.Roll(), Is.True);
            Assert.That(setup.ChooseFate(setup.ReadSnapshot().cards[0].id), Is.True);
            Assert.That(setup.Phase, Is.EqualTo(RunPhase.Encounter));
            yield return Open(720, 1280, "InGame");
            var oldSession = C.Session;
            var data = (EncounterUIData)typeof(BaseUI<EncounterUIData>).GetProperty("Data",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(C.UI.ActiveScreen);
            var oldResolve = data.choices[0].clicked;
            var oldMenu = data.hud.menu.clicked;
            oldSession.RecordElapsed(5);
            int sequence = oldSession.ReadSnapshot().sequence;
            var other = new RunSession(service.RunStore.Load(), service.RunStore);
            Assert.That(other.ResolveEncounter(false), Is.True);
            Assert.That(other.Phase, Is.EqualTo(RunPhase.Reward));
            Assert.That(other.ClaimReward(), Is.True);
            var latest = disk.Read();
            Assert.That(latest.run.sequence, Is.EqualTo(sequence + 2));
            var bytes = File.ReadAllBytes(disk.Path);
            LogAssert.Expect(LogType.Warning, "시작 기록과 다른 여정 또는 오래된 체크포인트입니다.");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.RecoveryFailure, Is.EqualTo(MetaFailureKind.Stale));
            oldResolve(); yield return Until(() => !C.Busy); yield return Layout();
            oldMenu(); yield return Layout();
            Assert.That(C.RefreshView(), Is.True); yield return Layout();
            Assert.That(C.Session, Is.SameAs(oldSession));
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(sequence));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
            Assert.That(C.Widgets.Buttons.ContainsKey("recovery-reload"), Is.True,
                "A stale command, autosave and RefreshView cannot reload; an explicit safe recovery action is required.");
            Click(Button("recovery-reload")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session, Is.Not.SameAs(oldSession));
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(latest.run.sequence));
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(latest.run.runId));
            CollectionAssert.AreEqual(latest.run.dice, C.Session.ReadSnapshot().dice);
            Assert.That(MetaJson.Encode(C.Session.ReadSnapshot().cards), Is.EqualTo(MetaJson.Encode(latest.run.cards)));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path), "Reload must not replay dice, rewards or settlement.");
            oldResolve(); oldMenu(); yield return Layout();
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(latest.run.sequence));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        RunState PrepareEncounter()
        {
            var initial = service.StartRunAsync(new StartRunRequest { requestId = "rv-start" }).GetAwaiter().GetResult();
            var run = new RunSession(initial, service.RunStore);
            Assert.That(run.ChooseNode(run.ReadSnapshot().availableNodeIds[0]), Is.True);
            Assert.That(run.Roll(), Is.True);
            Assert.That(run.ChooseFate(run.ReadSnapshot().cards[0].id), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Encounter));
            return run.ReadSnapshot();
        }
        void AdvanceTwo()
        {
            var b = new RunSession(service.RunStore.Load(), service.RunStore);
            Assert.That(b.ResolveEncounter(false), Is.True);
            Assert.That(b.ClaimReward(), Is.True);
        }
        IEnumerator TriggerConflict()
        {
            C.Session.RecordElapsed(5);
            LogAssert.Expect(LogType.Warning, "시작 기록과 다른 여정 또는 오래된 체크포인트입니다.");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.RecoveryFailure, Is.EqualTo(MetaFailureKind.Stale));
        }
        [UnityTest] public IEnumerator ReloadReadFailureKeepsBothCopiesAndAllowsRetry()
        {
            PrepareEncounter(); yield return Open(1080, 2400, "InGame");
            var a = C.Session; AdvanceTwo(); yield return TriggerConflict();
            var oldMemory = MetaJson.Encode(a.ReadSnapshot()); var bytes = File.ReadAllBytes(disk.Path);
            probe.failRead = true; LogAssert.Expect(LogType.Warning, "recovery read unavailable");
            Click(Button("recovery-reload")); yield return Layout();
            Assert.That(C.Session, Is.SameAs(a)); Assert.That(MetaJson.Encode(a.ReadSnapshot()), Is.EqualTo(oldMemory));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
            Assert.That(C.RecoveryFailure, Is.EqualTo(MetaFailureKind.TemporaryIO));
            probe.failRead = false; Click(Button("recovery-reload")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session, Is.Not.SameAs(a)); Assert.That(C.RecoveryFailure, Is.Null);
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(disk.Read().run.sequence));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [UnityTest] public IEnumerator ReloadAdoptsTheOtherDepartureWithoutReplayingTheOldRun()
        {
            var first = PrepareEncounter(); yield return Open(720, 1280, "InGame");
            var second = service.StartRunAsync(new StartRunRequest { requestId = "replace-run",
                expectedRevision = service.Profile.revision, abandonActiveRun = true }).GetAwaiter().GetResult();
            Assert.That(second.runId, Is.Not.EqualTo(first.runId));
            yield return TriggerConflict(); var bytes = File.ReadAllBytes(disk.Path);
            Click(Button("recovery-reload")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(second.runId));
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(second.sequence));
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<ExplorationUI>());
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [UnityTest] public IEnumerator AlreadySettledRecoveryReturnsToLobbyWithoutPayingAgain()
        {
            PrepareEncounter(); yield return Open(1080, 2400, "InGame");
            var other = new RunSession(service.RunStore.Load(), service.RunStore);
            Complete(other);
            service.SettleAsync(new SettlementRequest { requestId = "other-settle", runId = other.ReadSnapshot().runId,
                expectedRevision = service.Profile.revision }).GetAwaiter().GetResult();
            var paid = disk.Read(); var bytes = File.ReadAllBytes(disk.Path);
            yield return TriggerConflict();
            Click(Button("recovery-reload")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            Assert.That(C.Session, Is.Null); Assert.That(C.RecoveryFailure, Is.Null);
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(paid.profile.growthCurrency));
            Assert.That(disk.Read().settlements.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [UnityTest] public IEnumerator TemporaryWriteFailurePreservesStateAndRetriesTheSameAction()
        {
            var before = PrepareEncounter(); yield return Open(720, 1280, "InGame");
            var a = C.Session; var bytes = File.ReadAllBytes(disk.Path);
            probe.failBefore = true; LogAssert.Expect(LogType.Warning, "recovery write unavailable");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session, Is.SameAs(a)); Assert.That(a.Phase, Is.EqualTo(RunPhase.Encounter));
            Assert.That(a.ReadSnapshot().sequence, Is.EqualTo(before.sequence)); Assert.That(C.RecoveryFailure, Is.Null);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
            probe.failBefore = false; Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(a.Phase, Is.EqualTo(RunPhase.Reward)); Assert.That(a.ReadSnapshot().sequence, Is.EqualTo(before.sequence + 1));
        }
        IEnumerator CorruptAfterOpeningEncounter()
        {
            PrepareEncounter(); yield return Open(720, 1280, "InGame");
            C.Session.RecordElapsed(2); Assert.That(C.Session.SaveCheckpoint(), Is.True);
            File.WriteAllText(disk.Path, "{damaged ui profile");
            LogAssert.Expect(LogType.Warning, "프로필 JSON을 읽지 못했습니다.");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.RecoveryFailure, Is.EqualTo(MetaFailureKind.Corrupt));
        }
        [UnityTest] public IEnumerator BackupRestoreRequiresVisibleConfirmationAndCancellingDoesNotWrite()
        {
            PrepareEncounter(); yield return Open(1080, 2400, "InGame");
            var a = C.Session; a.RecordElapsed(2); Assert.That(a.SaveCheckpoint(), Is.True);
            var data = (EncounterUIData)typeof(BaseUI<EncounterUIData>).GetProperty("Data",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(C.UI.ActiveScreen);
            var oldChoice = data.choices[0].clicked;
            var saved = new LocalPlayerDataStore(disk.BackupPath, disk.ProfileId).Read();
            File.WriteAllText(disk.Path, "{damaged ui profile"); var damaged = File.ReadAllBytes(disk.Path);
            var backup = File.ReadAllBytes(disk.BackupPath);
            LogAssert.Expect(LogType.Warning, "프로필 JSON을 읽지 못했습니다.");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Click(Button("recovery-backup")); yield return Layout();
            Assert.That(((MenuUI)C.UI.ActiveScreen).characterDetails.text, Does.Contain("이 시점 이후"));
            Assert.That(((MenuUI)C.UI.ActiveScreen).characterDetails.text, Does.Contain("백업 보관 시각"));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            Assert.That(Directory.Exists(disk.RecoveryArchiveDirectory), Is.False);
            Click(Button("recovery-cancel")); yield return Layout();
            Assert.That(C.Session, Is.SameAs(a)); CollectionAssert.AreEqual(damaged, File.ReadAllBytes(disk.Path));
            Click(Button("recovery-backup")); yield return Layout();
            Click(Button("recovery-confirm")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session, Is.Not.SameAs(a)); Assert.That(C.RecoveryFailure, Is.Null);
            Assert.That(C.Session.ReadSnapshot().sequence, Is.EqualTo(saved.run.sequence));
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<EncounterUI>());
            Assert.That(C.Session.ReadSnapshot().recoveryGeneration, Is.Not.EqualTo(a.ReadSnapshot().recoveryGeneration));
            var restored = File.ReadAllBytes(disk.Path);
            oldChoice(); yield return Layout(); CollectionAssert.AreEqual(restored, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(Directory.GetFiles(disk.RecoveryArchiveDirectory)[0]));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Assert.That(C.Session.Phase, Is.EqualTo(RunPhase.Reward), "The restored generation must still accept a new real command.");
        }
        [UnityTest] public IEnumerator MissingBackupKeepsReadRetryAndNeverOffersRestoreConfirmation()
        {
            yield return CorruptAfterOpeningEncounter();
            File.Move(disk.BackupPath, Path.Combine(directory, "preserved-backup.json"));
            var damaged = File.ReadAllBytes(disk.Path); var a = C.Session;
            Click(Button("recovery-backup")); yield return Layout();
            Assert.That(C.Widgets.Buttons.ContainsKey("recovery-confirm"), Is.False);
            Assert.That(C.Widgets.Buttons.ContainsKey("recovery-reload"), Is.True);
            Assert.That(((MenuUI)C.UI.ActiveScreen).layout.notice.text, Does.Contain("백업이 없습니다"));
            Assert.That(C.Session, Is.SameAs(a)); CollectionAssert.AreEqual(damaged, File.ReadAllBytes(disk.Path));
        }
        [UnityTest] public IEnumerator InvalidBackupDoesNotEnableConfirmationOrOverwriteEitherFile()
        {
            yield return CorruptAfterOpeningEncounter();
            File.WriteAllText(disk.BackupPath, "{bad backup");
            var damaged = File.ReadAllBytes(disk.Path); var backup = File.ReadAllBytes(disk.BackupPath);
            LogAssert.Expect(LogType.Warning, "프로필 JSON을 읽지 못했습니다.");
            Click(Button("recovery-backup")); yield return Layout();
            Assert.That(C.Widgets.Buttons.ContainsKey("recovery-confirm"), Is.False);
            Assert.That(C.Widgets.Buttons.ContainsKey("recovery-reload"), Is.True);
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
        }
        [UnityTest] public IEnumerator RestoringPaidBackupRestoresMatchingGrowthAndReceiptsWithoutDoubleSettlement()
        {
            PrepareEncounter(); yield return Open(720, 1280, "InGame");
            var other = new RunSession(service.RunStore.Load(), service.RunStore); Complete(other);
            string runId = other.ReadSnapshot().runId;
            service.SettleAsync(new SettlementRequest { requestId = "fixture-paid", runId = runId,
                expectedRevision = service.Profile.revision }).GetAwaiter().GetResult();
            var paid = disk.Read();
            service.GrowAsync(new GrowthRequest { requestId = "growth-after-backup", expectedRevision = paid.profile.revision,
                characterId = paid.profile.loadout.characterId }).GetAwaiter().GetResult();
            Assert.That(service.Profile.growthCurrency, Is.LessThan(paid.profile.growthCurrency));
            Assert.That(service.Profile.characters[0].rank, Is.EqualTo(1));
            File.WriteAllText(disk.Path, "{damaged after growth");
            LogAssert.Expect(LogType.Warning, "프로필 JSON을 읽지 못했습니다.");
            Click(Button("resolve")); yield return Until(() => !C.Busy); yield return Layout();
            Click(Button("recovery-backup")); yield return Layout();
            Click(Button("recovery-confirm")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            var restored = disk.Read();
            Assert.That(restored.profile.growthCurrency, Is.EqualTo(paid.profile.growthCurrency));
            Assert.That(restored.profile.characters[0].rank, Is.EqualTo(paid.profile.characters[0].rank));
            Assert.That(MetaJson.Encode(restored.settlements), Is.EqualTo(MetaJson.Encode(paid.settlements)));
            Assert.That(MetaJson.Encode(restored.operations), Is.EqualTo(MetaJson.Encode(paid.operations)));
            Assert.That(MetaJson.Encode(restored.start), Is.EqualTo(MetaJson.Encode(paid.start)));
            Assert.That(restored.run.rngState, Is.EqualTo(paid.run.rngState));
            Click(Button("continue")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is ResultUI); yield return Layout();
            Click(Button("restart")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(paid.profile.growthCurrency));
            Assert.That(disk.Read().settlements.Count, Is.EqualTo(1));
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
