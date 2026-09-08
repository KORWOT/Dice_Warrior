using System;
using System.Collections;
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
    public sealed class SceneStructureTests
    {
        const string Folder = "Assets/_Project/Scenes/";
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        GameApplication app;
        LocalRunStore store;
        string directory;
        RunUIController Controller => app.controller;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its persistent app.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceSceneTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Fate Dice scene flow");
#endif
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        GameApplication Prefab()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
#else
            throw new InvalidOperationException("These authoring/production scene integration tests run in the Editor.");
#endif
        }
        void Bootstrap()
        {
            var prefab = Prefab(); Assert.That(prefab, Is.Not.Null);
            app = GameApplication.Bootstrap(prefab, store);
            Assert.That(app.controller.Store, Is.SameAs(store));
        }
        IEnumerator Load(string scene, string accepted = null)
        {
            var load = SceneManager.LoadSceneAsync(Folder + scene + ".unity", LoadSceneMode.Single);
            yield return load;
            yield return WaitScene(accepted ?? scene);
        }
        IEnumerator WaitScene(string scene)
        {
            var deadline = Time.realtimeSinceStartup + 12;
            while (SceneManager.GetActiveScene().path != Folder + scene + ".unity" || Controller.Busy ||
                   app.sceneFlow.CurrentRole.ToString() != scene)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Scene transition did not settle: " + scene);
                yield return null;
            }
            yield return null;
            AssertSingleRoot();
        }
        void AssertSingleRoot()
        {
            Assert.That(Object.FindObjectsByType<GameApplication>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Controller.UI.Root.inputGroup.interactable, Is.True);
            Assert.That(Controller.UI.Root.inputGroup.blocksRaycasts, Is.True);
        }
        void Press(string key)
        {
            if ((key.StartsWith("trial-") || key.StartsWith("cap-")) && Controller.UI.ActiveScreen is MenuUI menu)
                menu.settingsTab.onClick.Invoke();
            // These navigation fixtures explicitly dismiss the modal before returning to Lobby.
            if (key == "menu" && Controller.UI.Popups.LastOrDefault() is DiceRollUI) Controller.UI.CloseTopPopup();
            var popup = key == "roll" ? Controller.UI.Popups.LastOrDefault() as DiceRollUI : null;
            if (popup) { popup.rollButton.button.onClick.Invoke(); return; }
            Assert.That(Controller.Widgets.Buttons.ContainsKey(key), Is.True, key);
            Controller.Widgets.Buttons[key].onClick.Invoke();
        }
        IEnumerator Unlocked()
        {
            var deadline = Time.realtimeSinceStartup + 12;
            while (Controller.Busy) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            yield return null;
        }
        static string StableState(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0; return JsonUtility.ToJson(copy);
        }
        void AssertStoredState(RunState expected)
        {
            Assert.That(StableState(Controller.Session.State), Is.EqualTo(StableState(expected)), "Scene travel changed run state beyond elapsed time.");
            Assert.That(StableState(store.Load()), Is.EqualTo(StableState(expected)));
        }

        [Test] public void ProductionScenesAndAppRootAreAuthored()
        {
#if UNITY_EDITOR
            foreach (var scene in new[] { "Title", "Lobby", "InGame" })
                Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(Folder + scene + ".unity"), Is.Not.Null, scene);
            var prefab = Prefab(); Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.gameObject.activeSelf, Is.False, "Inject store before first initialization.");
            Assert.That(prefab.controller.initializeOnAwake, Is.False);
            Assert.That(prefab.sceneFlow, Is.Not.Null);
            var manager = prefab.controller.uiRootPrefab.GetComponent<UIManager>();
            Assert.That(manager.prefabs.Select(p => p.GetType()).Distinct().Count(), Is.EqualTo(9));
            Assert.That(manager.prefabs.OfType<TitleUI>().Single().enterButton, Is.Not.Null);
            Assert.That(manager.prefabs.OfType<DiceRollUI>().Single().dice, Has.Length.EqualTo(6));
#endif
        }
        [Test] public void ProductionBuildStartsAtTitle()
        {
#if UNITY_EDITOR
            var enabled = UnityEditor.EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Assert.That(enabled.Take(3), Is.EqualTo(new[] { Folder + "Title.unity", Folder + "Lobby.unity", Folder + "InGame.unity" }));
            Assert.That(enabled, Does.Not.Contain("Assets/Scenes/SampleScene.unity"));
            Assert.That(enabled, Does.Not.Contain(Folder + "FateDicePrototype.unity"));
#endif
        }
        [UnityTest] public IEnumerator TitleLobbyNewRunAndRepeatedContinueKeepOneOwner()
        {
            Bootstrap(); var owner = app; var ui = Controller.UI;
            yield return Load("Title");
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<TitleUI>());
            Assert.That(store.Exists, Is.False);
            var enter = ((TitleUI)Controller.UI.ActiveScreen).enterButton.button;
            enter.onClick.Invoke(); enter.onClick.Invoke();
            Assert.That(Controller.Busy, Is.True);
            Assert.That(Controller.UI.Root.inputGroup.interactable, Is.False);
            Assert.That(app.sceneFlow.RequestLobby(), Is.False, "A duplicate load cannot be accepted.");
            yield return WaitScene("Lobby");
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            Assert.That(store.Exists, Is.False);
            Controller.Seed = 88; Press("trial-fireball"); Press("cap-Common");
            Press("new"); yield return WaitScene("InGame");
            Assert.That(Controller.Session.State.initialSeed, Is.EqualTo(88));
            Assert.That(Controller.Session.State.explorationCap, Is.EqualTo(Grade.Common));
            var saved = store.Load();
            for (var i = 0; i < 3; i++)
            {
                Press("menu"); yield return WaitScene("Lobby");
                Assert.That(GameApplication.Bootstrap(Prefab()), Is.SameAs(owner));
                Assert.That(app.controller.UI, Is.SameAs(ui));
                AssertStoredState(saved);
                Press("continue"); yield return WaitScene("InGame"); AssertStoredState(saved);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DirectLobbyAndMissingInGameNeverCreateASave()
        {
            Bootstrap(); yield return Load("Lobby");
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            Assert.That(store.Exists, Is.False);
            Object.Destroy(app.gameObject); yield return null;
            Bootstrap(); yield return Load("InGame", "Lobby");
            Assert.That(Controller.Session, Is.Null); Assert.That(store.Exists, Is.False);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DirectInGameRestoresExactSavedOffersAndPaidReroll()
        {
            var config = Prefab().controller.config.Snapshot();
            var run = RunSession.New(config, 33, "fireball", Grade.Legendary);
            var node = run.State.nodes.Single(n => run.State.availableNodeIds.Contains(n.id) && n.type == NodeType.Event);
            Assert.That(run.ChooseNode(node.id) && run.Roll() && run.ChooseFate(run.State.cards[0].id), Is.True);
            Assert.That(run.ResolveEncounter(false) && run.ClaimReward(), Is.True);
            Assert.That(run.State.rerollUnlocked, Is.True);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll() && run.Reroll(3), Is.True);
            store.Save(run.State); var bytes = File.ReadAllBytes(Path.Combine(directory, "run.json"));
            Bootstrap(); yield return Load("InGame"); AssertStoredState(run.State);
            Assert.That(File.ReadAllBytes(Path.Combine(directory, "run.json")), Is.EqualTo(bytes), "Loading alone is read-only.");
            Press("menu"); yield return WaitScene("Lobby"); Press("continue"); yield return WaitScene("InGame"); AssertStoredState(run.State);
            var expected = new RunSession(store.Load());
            Assert.That(expected.Reroll(2), Is.True);
            Press("die-2"); Press("die-2"); yield return Unlocked(); AssertStoredState(expected.State);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DamagedDirectInGameFallsBackAndPreservesBytesUntilArchive()
        {
            var path = Path.Combine(directory, "run.json"); File.WriteAllText(path, "broken-scene-save");
            string diagnostic = Assert.Throws<InvalidDataException>(() => store.Load()).Message;
            for (int i = 0; i < 3; i++) LogAssert.Expect(LogType.Warning, diagnostic);
            Bootstrap(); yield return Load("InGame", "Lobby");
            Assert.That(Controller.Session, Is.Null);
            Assert.That(Controller.Widgets.Buttons["new"].interactable, Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo("broken-scene-save"));
            Press("new"); Assert.That(File.ReadAllText(path), Is.EqualTo("broken-scene-save"));
            Press("archive"); yield return Unlocked();
            Assert.That(File.Exists(path), Is.False);
            Assert.That(File.ReadAllText(Directory.GetFiles(directory, "*.bak").Single()), Is.EqualTo("broken-scene-save"));
            Press("new"); yield return WaitScene("InGame"); Assert.That(store.Exists, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator InvalidDestinationLeavesCurrentViewUsableAndStoreUntouched()
        {
            Bootstrap(); yield return Load("Title");
            var current = Controller.UI.ActiveScreen; var original = app.sceneFlow.lobbyScenePath;
            var priorStatus = ((TitleUI)current).status.text;
            app.sceneFlow.lobbyScenePath = "Assets/_Project/Scenes/NotAuthored.unity";
            LogAssert.Expect(LogType.Warning, "Scene is unavailable in the build: " + app.sceneFlow.lobbyScenePath);
            Assert.That(app.sceneFlow.RequestLobby(), Is.False);
            Assert.That(Controller.Busy, Is.False);
            Assert.That(Controller.UI.ActiveScreen, Is.SameAs(current));
            Assert.That(((TitleUI)current).status.text, Is.Not.EqualTo(priorStatus));
            Assert.That(store.Exists, Is.False); AssertSingleRoot();
            app.sceneFlow.lobbyScenePath = original;
            ((TitleUI)current).enterButton.button.onClick.Invoke(); yield return WaitScene("Lobby");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DuplicateBootstrapCannotReplaceStoreOrReinitializeUI()
        {
            Bootstrap(); yield return Load("Lobby");
            var ui = Controller.UI;
            Assert.That(GameApplication.Bootstrap(Prefab(), store), Is.SameAs(app));
            Assert.Throws<InvalidOperationException>(() => GameApplication.Bootstrap(Prefab(), new LocalRunStore(Path.Combine(directory, "other.json"))));
            Assert.That(Controller.UI, Is.SameAs(ui)); Assert.That(Controller.Store, Is.SameAs(store));
            Assert.That(store.Exists, Is.False); AssertSingleRoot();
        }
        [UnityTest] public IEnumerator ProductionJourneyUsesRealViewsThroughResultAndReturnsToLobby()
        {
            Bootstrap(); yield return Load("Lobby"); Press("new"); yield return WaitScene("InGame");
            var steps = 0; var combatSeen = false; var rewardSeen = false; var equipmentSeen = false;
            while (Controller.Session.State.phase != RunPhase.Result)
            {
                Assert.That(steps++, Is.LessThan(400));
                var state = Controller.Session.State; string key;
                switch (state.phase)
                {
                    case RunPhase.Map: key = "node-" + state.nodes.Where(n => state.availableNodeIds.Contains(n.id)).OrderBy(n => Preference(n.type)).First().id; break;
                    case RunPhase.ExplorationRoll: case RunPhase.CombatRoll: key = "roll"; break;
                    case RunPhase.ExplorationCards: key = "fate-" + state.cards.OrderBy(c => Preference(c.type)).First().id; break;
                    case RunPhase.CombatCards: combatSeen = true; key = "card-" + state.cards.OrderByDescending(c => CombatRules.Evaluate(state, c).damage).First().id; break;
                    case RunPhase.Encounter: key = "resolve"; break;
                    case RunPhase.Shop: key = "leave"; break;
                    case RunPhase.Reward: rewardSeen = true; key = "claim"; break;
                    case RunPhase.EquipmentChoice: equipmentSeen = true; key = string.IsNullOrEmpty(state.pendingEquipmentId) ? "die-0" : "equip-accept"; break;
                    default: throw new InvalidOperationException(state.phase.ToString());
                }
                Press(key); yield return Unlocked();
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Folder + "InGame.unity"));
            }
            Assert.That(combatSeen && rewardSeen && equipmentSeen, Is.True);
            Assert.That(Controller.Session.State.won, Is.True);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<ResultUI>());
            var completed = store.Load(); Press("restart"); yield return WaitScene("Lobby"); AssertStoredState(completed);
            Press("new"); yield return WaitScene("InGame");
            Assert.That(Controller.Session.State.eventsResolved, Is.Zero);
            Assert.That(Controller.Session.State.lastResult.runId, Is.EqualTo(completed.lastResult.runId));
            Assert.That(Controller.Session.State.lastResult.won, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        static int Preference(NodeType type)
        {
            switch (type) { case NodeType.Rest: return 0; case NodeType.Treasure: return 1; case NodeType.Event: return 2; case NodeType.Shop: return 3; default: return 4; }
        }
    }
}
