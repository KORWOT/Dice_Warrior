using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class KoreanUiTests
    {
        const string FontPath = "Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf";
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string SceneFolder = "Assets/_Project/Scenes/";
        static readonly string[] Grades = { "일반", "고급", "희귀", "영웅", "전설" };
        static readonly Dictionary<string, string> ContentGolden = new Dictionary<string, string>
        {
            { "Road bandit", "길목 도적" }, { "Stone sentry", "돌 파수꾼" }, { "Fate keeper", "운명의 수호자" },
            { "Strike", "공격" }, { "Guard", "방어" }, { "Heavy strike", "강공격" }, { "Fireball", "화염구" },
            { "Bastion", "철벽" }, { "Attack", "공격" }, { "Defend", "방어" }, { "Heavy attack", "강공격" }
        };
        GameApplication app;
        LocalRunStore store;
        string directory;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.State;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its persistent application.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceKoreanUiTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            SetResolution(1280);
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            yield return CloseOwner();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        IEnumerator CloseOwner()
        {
            if (app) Object.Destroy(app.gameObject);
            app = null;
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
        }
        static GameApplication Prefab()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
#else
            throw new InvalidOperationException("Production font and prefab integration tests run in the Editor.");
#endif
        }
        static Font KoreanFont()
        {
#if UNITY_EDITOR
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(font, Is.Not.Null);
            return font;
#else
            throw new InvalidOperationException("The Korean font asset must be inspected in the Editor.");
#endif
        }
        static void SetResolution(uint height)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, height, "Fate Dice Korean UI");
#endif
        }
        IEnumerator Open(GameSceneRole role)
        {
            Assert.That(app, Is.Null);
            app = GameApplication.Bootstrap(Prefab(), store);
            Assert.That(Controller.Store, Is.SameAs(store), "The isolated store must precede controller initialization.");
            yield return SceneManager.LoadSceneAsync(SceneFolder + role + ".unity", LoadSceneMode.Single);
            yield return WaitScene(role);
        }
        IEnumerator WaitScene(GameSceneRole role)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (Controller.Busy || app.sceneFlow.CurrentRole != role ||
                   SceneManager.GetActiveScene().path != SceneFolder + role + ".unity")
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Production scene did not settle.");
                yield return null;
            }
            yield return Settled();
        }
        IEnumerator Unlocked()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 8;
            while (Controller.Busy)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Input lock did not recover.");
                yield return null;
            }
            yield return Settled();
        }
        static IEnumerator Settled()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }
        IEnumerator Display(RunState state)
        {
            yield return CloseOwner();
            store.Save(state);
            byte[] bytes = File.ReadAllBytes(store.Path);
            yield return Open(GameSceneRole.InGame);
            AssertUnchanged(state, bytes);
        }

        [UnityTest] public IEnumerator AuthoredApplicationUsesPretendardFont()
        {
#if UNITY_EDITOR
            var prefab = Prefab();
            Assert.That(prefab, Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(prefab.controller.uiFont), Is.EqualTo(FontPath),
                "The application must use the authored Korean font before any screen opens.");
            var font = KoreanFont();
            var importer = (UnityEditor.TrueTypeFontImporter)UnityEditor.AssetImporter.GetAtPath(FontPath);
            Assert.That(importer.includeFontData, Is.True, "The build cannot depend on an installed OS font.");
            Assert.That(font.dynamic, Is.True, "The unmodified static TTF must provide dynamic runtime glyphs.");
            var manager = prefab.controller.uiRootPrefab.GetComponent<UIManager>();
            var originals = manager.prefabs.Select(p => p.gameObject).Concat(new[] {
                prefab.controller.uiPrefabs.commonButton.gameObject, prefab.controller.uiPrefabs.explorationNode.gameObject,
                prefab.controller.uiPrefabs.actionCard.gameObject, prefab.controller.uiPrefabs.fateCard.gameObject,
                manager.prefabs.OfType<CombatUI>().Single().diePrefab.gameObject
            }).Distinct().ToArray();
            var texts = originals.SelectMany(p => p.GetComponentsInChildren<Text>(true)).ToArray();
            Assert.That(texts.Length, Is.GreaterThan(30), "The check must cover actual authored screens and reusable originals.");
            foreach (Text text in texts) Assert.That(text.font, Is.SameAs(font), text.name);
#endif
            yield return Open(GameSceneRole.Title);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<TitleUI>());
            Assert.That(((TitleUI)Controller.UI.ActiveScreen).title.text, Is.EqualTo("운명의 주사위"));
            AssertKoreanSurface();
            AssertGlyphs(KoreanFont(), "운명의 주사위 체력 위력 방어력 수호 재굴림 여정 완료");
            Assert.That(store.Exists, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TitleLobbyExplorationAndRecoverableErrorsUseKoreanWithStableCommands()
        {
            yield return Open(GameSceneRole.Title);
            var title = (TitleUI)Controller.UI.ActiveScreen;
            string status = title.status.text;
            string lobby = app.sceneFlow.lobbyScenePath;
            app.sceneFlow.lobbyScenePath = "Assets/_Project/Scenes/NotAuthored.unity";
            LogAssert.Expect(LogType.Warning, "Scene is unavailable in the build: " + app.sceneFlow.lobbyScenePath);
            Assert.That(app.sceneFlow.RequestLobby(), Is.False);
            Assert.That(title.status.text, Is.Not.EqualTo(status));
            AssertKoreanSurface();
            app.sceneFlow.lobbyScenePath = lobby;
            yield return PressButton(title.enterButton.button);
            yield return WaitScene(GameSceneRole.Lobby);
            AssertKoreanSurface();
            Assert.That(store.Exists, Is.False);

            File.WriteAllText(store.Path, "legacy damaged save evidence");
            LogAssert.Expect(LogType.Warning, Assert.Throws<InvalidDataException>(() => store.Load()).Message);
            Controller.EnterLobby();
            yield return Settled();
            Assert.That(Controller.Widgets.Buttons["new"].interactable, Is.False);
            Assert.That(AllText(), Does.Contain("저장"));
            Assert.That(AllText(), Does.Contain("읽"));
            AssertKoreanSurface();
            Assert.That(File.ReadAllText(store.Path), Is.EqualTo("legacy damaged save evidence"));
            yield return Press("archive");
            yield return Unlocked();
            Assert.That(File.ReadAllText(Directory.GetFiles(directory, "*.bak").Single()), Is.EqualTo("legacy damaged save evidence"));
            yield return Press("cap-Common");
            yield return Press("new");
            yield return WaitScene(GameSceneRole.InGame);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<ExplorationUI>());
            AssertKoreanSurface();
            var oracle = new RunSession(store.Load());
            string node = State.nodes.First(n => State.availableNodeIds.Contains(n.id) && n.type == NodeType.Combat).id;
            Assert.That(oracle.ChooseNode(node), Is.True);
            yield return Press("node-" + node);
            yield return Unlocked();
            AssertState(oracle.State);
            AssertKoreanSurface();
            Assert.That(oracle.Roll(), Is.True);
            yield return Press("roll");
            yield return Unlocked();
            AssertState(oracle.State);
            Assert.That(Controller.Widgets.Buttons["fate-" + State.cards[0].id].GetComponent<FateCardView>().frame.label.text,
                Is.EqualTo("전투  /  일반"));
            AssertKoreanSurface();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LegacyEnglishCombatSaveTranslatesOnlyPresentationAcrossRebindAndResize()
        {
            var run = Encounter(NodeType.Combat);
            Assert.That(run.Roll(), Is.True);
            Assert.That(run.State.message, Does.Contain("Choose one action"));
            Assert.That(run.State.config.Enemy(run.State.activeEnemyId).label, Is.EqualTo("Road bandit"));
            yield return Display(run.State);
            byte[] bytes = File.ReadAllBytes(store.Path);
            string config = JsonUtility.ToJson(State.config);
            var view = (CombatUI)Controller.UI.ActiveScreen;
            Assert.That(view.enemyName.text, Is.EqualTo("길목 도적"));
            var intent = State.config.Enemy(State.activeEnemyId).intents.Single(i => i.id == State.activeIntentId);
            Assert.That(view.layout.situation.text, Is.EqualTo("다음 행동: " + ContentGolden[intent.label]));
            Assert.That(view.layout.stats.text, Does.Contain("체력 " + State.hp + "/"));
            foreach (var card in State.cards)
            {
                var action = State.config.Action(card.contentId);
                var actionView = Controller.Widgets.Buttons["card-" + card.id].GetComponent<ActionCardView>();
                Assert.That(actionView.frame.label.text, Is.EqualTo(ContentGolden[action.label]));
                Assert.That(actionView.gradeLabel.text, Is.EqualTo(Grades[(int)card.grade]));
                var effect = CombatRules.Evaluate(State, card);
                Assert.That(actionView.effectLabel.text, Is.EqualTo("피해 " + effect.damage + " / 수호 " + effect.block));
                Assert.That(actionView.OfferedId, Is.EqualTo(card.id));
                Assert.That(actionView.OriginalId, Is.EqualTo(card.contentId));
            }
            AssertKoreanSurface();
            SetResolution(1600);
            yield return Settled();
            Assert.That(Controller.TryEnterInGame(), Is.True);
            yield return Settled();
            AssertKoreanSurface();
            Assert.That(JsonUtility.ToJson(State.config), Is.EqualTo(config));
            AssertUnchanged(run.State, bytes);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator EncounterShopRewardAndEquipmentBoundariesDisplayKoreanWithoutImplicitRewards()
        {
            foreach (NodeType type in new[] { NodeType.Event, NodeType.Shop, NodeType.Rest, NodeType.Treasure })
            {
                var run = Encounter(type);
                yield return Display(run.State);
                Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<EncounterUI>());
                AssertKoreanSurface();
                if (type == NodeType.Shop)
                {
                    Assert.That(AllText(), Does.Contain("회복 물약"));
                    Assert.That(Controller.Widgets.Buttons.ContainsKey("leave"), Is.True);
                    continue;
                }
                Assert.That(run.ResolveEncounter(false), Is.True);
                yield return Display(run.State);
                Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<RewardUI>());
                AssertKoreanSurface();
                Assert.That(Controller.Widgets.Buttons["claim"].interactable, Is.True);
                if (type == NodeType.Treasure)
                {
                    Assert.That(run.ClaimReward(), Is.True);
                    Assert.That(run.State.phase, Is.EqualTo(RunPhase.EquipmentChoice));
                    yield return Display(run.State);
                    Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<EquipmentUI>());
                    Assert.That(AllText(), Does.Contain("불씨 검"));
                    AssertKoreanSurface();
                    Assert.That(Controller.Widgets.Buttons.ContainsKey("equip-accept"), Is.True);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CompletedWinAndLossSummariesStayKoreanAndLeaveTheSavedResultsIntact()
        {
            foreach (bool won in new[] { true, false })
            {
                var run = Completed(won);
                Assert.That(run.State.phase, Is.EqualTo(RunPhase.Result));
                Assert.That(run.State.won, Is.EqualTo(won));
                yield return Display(run.State);
                byte[] bytes = File.ReadAllBytes(store.Path);
                Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<ResultUI>());
                Assert.That(Controller.Widgets.Situation.text, Is.EqualTo(won ? "여정 완료" : "여정 종료"));
                AssertKoreanSurface();
                yield return Settled();
                AssertUnchanged(run.State, bytes);
                yield return Press("restart");
                yield return WaitScene(GameSceneRole.Lobby);
                Assert.That(State.lastResult.runId, Is.EqualTo(run.State.runId));
                AssertKoreanSurface();
                AssertState(run.State);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CustomLabelsAndDescriptionsRemainVerbatimWhileDefaultChromeIsKorean()
        {
            const string customName = "The wishing stone / 사용자 명칭";
            const string customDescription = "A player's own tale / 사용자 설명";
            var encounter = Encounter(NodeType.Event, config =>
            {
                foreach (var definition in config.world.events.Where(e => e.type == NodeType.Event))
                { definition.label = customName; definition.description = customDescription; }
            });
            yield return Display(encounter.State);
            byte[] encounterBytes = File.ReadAllBytes(store.Path);
            Assert.That(((EncounterUI)Controller.UI.ActiveScreen).description.text, Is.EqualTo(customDescription));
            Assert.That(Controller.Widgets.Situation.text, Does.Contain(customName));
            AssertKoreanSurface(customName, customDescription);
            AssertUnchanged(encounter.State, encounterBytes);

            const string customEnemy = "Captain Ember";
            const string customPlayer = "Mira";
            var names = new Dictionary<string, string> {
                { "strike", "Strike / 사용자 검" }, { "guard", "Azure Ward" },
                { "heavy", "Titan Cut" }, { "fireball", "Nova Arc" }, { "bastion", "Custom Bastion" }
            };
            var combat = Encounter(NodeType.Combat, config =>
            {
                config.growth.characterName = customPlayer;
                config.Enemy("road_bandit").label = customEnemy;
                foreach (var action in config.combat.actions) action.label = names[action.id];
            });
            Assert.That(combat.Roll(), Is.True);
            yield return Display(combat.State);
            byte[] bytes = File.ReadAllBytes(store.Path);
            var view = (CombatUI)Controller.UI.ActiveScreen;
            Assert.That(view.enemyName.text, Is.EqualTo(customEnemy));
            Assert.That(view.playerName.text, Is.EqualTo(customPlayer));
            foreach (var card in State.cards)
                Assert.That(Controller.Widgets.Buttons["card-" + card.id].GetComponent<ActionCardView>().frame.label.text,
                    Is.EqualTo(names[card.contentId]));
            AssertKoreanSurface(names.Values.Concat(new[] { customEnemy, customPlayer }).ToArray());
            AssertUnchanged(combat.State, bytes);
            LogAssert.NoUnexpectedReceived();
        }

        static RunSession Encounter(NodeType type, Action<GameConfigData> customize = null)
        {
            var config = Prefab().controller.config.Snapshot();
            config.fate.nodeWeights = Enumerable.Range(0, 5).Select(i => i == (int)type ? 1f : 0f).ToArray();
            customize?.Invoke(config);
            var run = RunSession.New(config, 33, "fireball", Grade.Common);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll(), Is.True);
            Assert.That(run.ChooseFate(run.State.cards[0].id), Is.True);
            return run;
        }
        static RunSession Completed(bool victory)
        {
            var run = Encounter(victory ? NodeType.Rest : NodeType.Combat, config =>
            {
                config.world.eventsToBoss = 1;
                config.growth.startingMaxHp = victory ? 200 : 1;
                config.growth.startingPower = victory ? 500 : 1;
                config.growth.startingGuard = 0;
                config.combat.startingActionIds = new[] { "strike" };
            });
            if (victory)
            {
                Assert.That(run.ResolveEncounter(false) && run.ClaimReward(), Is.True);
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            }
            for (int turns = 0; run.State.phase == RunPhase.CombatRoll && turns < 40; turns++)
            {
                Assert.That(run.Roll(), Is.True);
                var state = run.State;
                string card = state.cards.OrderByDescending(c => CombatRules.Evaluate(state, c).damage).First().id;
                Assert.That(run.ChooseAction(card), Is.True);
            }
            if (run.State.phase == RunPhase.Reward) Assert.That(run.ClaimReward(), Is.True);
            return run;
        }

        string AllText() => string.Join("\n", Controller.UI.ActiveScreen.GetComponentsInChildren<Text>()
            .Where(t => t.isActiveAndEnabled).Select(t => t.text));
        void AssertKoreanSurface(params string[] customText)
        {
            Canvas.ForceUpdateCanvases();
            var texts = Controller.UI.ActiveScreen.GetComponentsInChildren<Text>().Where(t => t.isActiveAndEnabled &&
                !string.IsNullOrWhiteSpace(t.text)).ToArray();
            Assert.That(texts.Length, Is.GreaterThan(1));
            var font = KoreanFont();
            string combined = string.Join("\n", texts.Select(t => t.text));
            Assert.That(Regex.IsMatch(combined, "[가-힣]"), Is.True, "The actual screen has no Korean text.");
            foreach (Text text in texts)
            {
                Assert.That(text.font, Is.SameAs(font), text.name);
                string builtIn = text.text;
                foreach (string custom in customText.OrderByDescending(s => s.Length)) builtIn = builtIn.Replace(custom, "");
                builtIn = Regex.Replace(builtIn, "<[^>]+>", "");
                Assert.That(Regex.IsMatch(builtIn, "[A-Za-z]"), Is.False, "Untranslated default player text: " + text.text);
                if (FullyVisible(text.rectTransform))
                {
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + .1f),
                        "Visible Korean text overflows its authored height: " + text.text);
                    var rendered = text.canvasRenderer.GetMesh();
                    Assert.That(rendered, Is.Not.Null, "A nonempty visible text field has no rendering mesh: " + text.text);
                    Assert.That(rendered.vertexCount, Is.GreaterThan(0),
                        "A nonempty visible text field produced no rendered glyphs: " + text.text);
                }
            }
            AssertGlyphs(font, combined);
        }
        static void AssertGlyphs(Font font, string text)
        {
            string hangul = new string(text.Where(c => c >= '가' && c <= '힣').Distinct().ToArray());
            Assert.That(hangul.Length, Is.GreaterThan(0));
            font.RequestCharactersInTexture(hangul, 25, FontStyle.Normal);
            foreach (char glyph in hangul)
            {
                Assert.That(font.HasCharacter(glyph), Is.True, "The bundled font is missing " + glyph);
                Assert.That(font.GetCharacterInfo(glyph, out var info, 25, FontStyle.Normal), Is.True, "No rendered glyph for " + glyph);
                Assert.That(info.advance, Is.GreaterThan(0));
            }
        }
        static bool FullyVisible(RectTransform rect)
        {
            Rect screen = ScreenRect(rect);
            if (!Contains(Screen.safeArea, screen)) return false;
            foreach (ScrollRect scroll in rect.GetComponentsInParent<ScrollRect>())
                if (scroll.viewport && !Contains(ScreenRect(scroll.viewport), screen)) return false;
            return true;
        }
        IEnumerator Press(string key)
        {
            if ((key.StartsWith("trial-") || key.StartsWith("cap-")) && Controller.UI.ActiveScreen is MenuUI menu && !menu.settingsPanel.gameObject.activeSelf)
                yield return PressButton(menu.settingsTab);
            if (key == "menu" && Controller.UI.Popups.LastOrDefault() is DiceRollUI) Controller.UI.CloseTopPopup();
            if (key == "roll" && Controller.UI.Popups.LastOrDefault() is DiceRollUI popup)
            {
                yield return PressButton(popup.rollButton.button);
                yield break;
            }
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key, out var button), Is.True, key);
            yield return PressButton(button);
        }
        IEnumerator PressButton(Button button)
        {
            Assert.That(Controller.Busy, Is.False);
            Assert.That(button.IsInteractable(), Is.True);
            var rect = (RectTransform)button.transform;
            foreach (ScrollRect scroll in rect.GetComponentsInParent<ScrollRect>())
            {
                scroll.StopMovement();
                for (int step = 0; !Contains(ScreenRect(scroll.viewport), ScreenRect(rect)) && step <= 40; step++)
                {
                    if (scroll.horizontal) scroll.horizontalNormalizedPosition = step / 40f;
                    else scroll.verticalNormalizedPosition = 1 - step / 40f;
                    Canvas.ForceUpdateCanvases();
                }
                Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect(rect)), Is.True, "The command is clipped.");
            }
            yield return null;
            Assert.That(FullyVisible(rect), Is.True);
            var pointer = new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left, position = ScreenRect(rect).center
            };
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var target = hits.Select(h => ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject)).FirstOrDefault(h => h != null);
            Assert.That(target, Is.SameAs(button.gameObject), "The visible pointer must reach the existing command.");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 2 && inner.xMax <= outer.xMax + 2 &&
            inner.yMin >= outer.yMin - 2 && inner.yMax <= outer.yMax + 2;
        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
        void AssertState(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected)), "Presentation changed state, RNG, fixed IDs or pending rewards.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)));
        }
        void AssertUnchanged(RunState expected, byte[] bytes)
        {
            AssertState(expected);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Presentation must leave the legacy save byte-identical.");
        }
    }
}
