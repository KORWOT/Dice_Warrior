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
    public sealed class MetaProgressionFlowTests
    {
        GameApplication app;
        MetaProgressionConfig meta;
        GameConfigData rules;
        LocalPlayerDataStore disk;
        LocalMetaProgressionService service;
        ProbeStore probe;
        sealed class ProbeStore : IPlayerDataStore
        {
            readonly IPlayerDataStore inner;
            public bool failAfter;
            public ProbeStore(IPlayerDataStore inner) { this.inner = inner; }
            public string ProfileId => inner.ProfileId;
            public bool Exists => inner.Exists;
            public PlayerSaveDocument Read() => inner.Read();
            public void Write(long revision, PlayerSaveDocument candidate)
            { inner.Write(revision, candidate); if (failAfter) throw new IOException("UI meta response lost"); }
        }
        string directory, userRun, userMeta;
        byte[] userRunBytes, userMetaBytes;
        RunUIController C => app.controller;
        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null);
            directory = Path.Combine(Path.GetTempPath(), "FateDiceMetaFlow", Guid.NewGuid().ToString("N"));
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
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            if (meta) Object.Destroy(meta);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            Preserve(userRun, userRunBytes); Preserve(userMeta, userMetaBytes);
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceMetaFlow")) + Path.DirectorySeparatorChar;
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
        [UnityTest] public IEnumerator OwnedSettingsAreClickableAndTheRealDepartureUsesThem()
        {
            yield return Open(720, 1280);
            var menu = (MenuUI)C.UI.ActiveScreen;
            Assert.That(menu.metaLayoutVersion, Is.EqualTo(1)); Assert.That(Prefab().metaConfig, Is.Not.Null);
            Assert.That(disk.Exists, Is.False, "Simply opening the lobby must not create player data.");
            Click(menu.settingsTab); yield return Layout();
            yield return Reveal(Button("meta-gear-0")); Click(Button("meta-gear-0")); yield return Layout();
            Assert.That(service.Profile.loadout.equipment[0], Is.EqualTo("starter-gear-0"));
            yield return Reveal(Button("meta-die-0")); Click(Button("meta-die-0")); yield return Layout();
            Assert.That(service.Profile.loadout.dice[0], Is.EqualTo("starter-die-6"));
            yield return Reveal(Button("trial-bastion")); Click(Button("trial-bastion")); yield return Layout();
            Assert.That(service.Profile.loadout.wildcardId, Is.EqualTo("bastion"));
            Click(Button("new")); yield return Until(() => !C.Busy && C.Session != null && C.UI.ActiveScreen is ExplorationUI);
            Assert.That(C.Session.ReadSnapshot().equipmentIds[0], Is.EqualTo("ember_blade"));
            Assert.That(C.Session.ReadSnapshot().dieIds[0], Is.EqualTo("ember"));
            Assert.That(C.Session.ReadSnapshot().actionIds, Does.Contain("bastion"));
            Assert.That(C.Session.ReadSnapshot().profileId, Is.EqualTo("ui-player"));
        }
        [UnityTest] public IEnumerator ResultButtonSettlesOnceAndGrowthChangesTheNextDeparture()
        {
            var first = service.StartRunAsync(new StartRunRequest { requestId = "fixture-start" }).GetAwaiter().GetResult();
            var run = new RunSession(first, service.RunStore); Complete(run);
            int completedBaseHp = run.ReadSnapshot().baseMaxHp;
            yield return Open(1080, 2400, "InGame");
            Assert.That(C.UI.ActiveScreen, Is.TypeOf<ResultUI>()); Assert.That(service.Profile.growthCurrency, Is.Zero);
            C.RefreshView(); C.RefreshView(); yield return Layout(); Assert.That(service.Profile.growthCurrency, Is.Zero);
            Click(Button("restart")); yield return Until(() => !C.Busy && C.UI.ActiveScreen is MenuUI); yield return Layout();
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(11));
            var menu = (MenuUI)C.UI.ActiveScreen; Click(menu.growthTab); yield return Layout();
            yield return Reveal(Button("meta-grow")); Click(Button("meta-grow")); yield return Layout();
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(1)); Assert.That(service.Profile.characters[0].rank, Is.EqualTo(1));
            Assert.That(disk.Read().run.baseMaxHp, Is.EqualTo(completedBaseHp));
            Click(Button("new")); yield return Until(() => !C.Busy && C.Session != null && C.UI.ActiveScreen is ExplorationUI);
            Assert.That(C.Session.ReadSnapshot().baseMaxHp, Is.EqualTo(first.baseMaxHp + 2));
            Assert.That(disk.Read().settlements.Count, Is.EqualTo(1));
            var repeated = service.SettleAsync(new SettlementRequest { requestId = "repeat", runId = first.runId }).GetAwaiter().GetResult();
            Assert.That(repeated.currency, Is.EqualTo(11)); Assert.That(service.Profile.growthCurrency, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator LostDepartureResponseThenSettingsChangeStillOffersTheCommittedRun()
        {
            yield return Open(720, 1280);
            probe.failAfter = true; LogAssert.Expect(LogType.Warning, "UI meta response lost");
            Click(Button("new")); yield return Layout(); probe.failAfter = false;
            var savedId = service.RunStore.Load().runId;
            Click(((MenuUI)C.UI.ActiveScreen).settingsTab); yield return Layout();
            yield return Reveal(Button("trial-bastion")); Click(Button("trial-bastion")); yield return Layout();
            Assert.That(C.Session, Is.Null);
            Assert.That(Button("new").GetComponentInChildren<Text>().text, Does.Contain("포기"));
            Click(Button("continue")); yield return Until(() => !C.Busy && C.Session != null && C.UI.ActiveScreen is ExplorationUI);
            Assert.That(C.Session.ReadSnapshot().runId, Is.EqualTo(savedId));
            Assert.That(C.Session.ReadSnapshot().actionIds, Does.Contain("fireball"), "The committed departure keeps its original wildcard.");
            Assert.That(service.Profile.loadout.wildcardId, Is.EqualTo("bastion"));
            Assert.That(disk.Read().settlements, Is.Empty);
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
