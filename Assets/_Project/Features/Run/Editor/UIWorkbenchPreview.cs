using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // A disposable editor stage. It never initializes the application or a save store.
    public sealed class UIWorkbenchPreview : PreviewSceneStage
    {
        public string SourcePrefabPath { get; private set; }
        public GameObject PreviewRoot { get; private set; }
        private int previewHeight;
        private SceneView framedView;
        private bool previousGrid;

        public static UIWorkbenchPreview Open(WorkbenchOptions options, int height = 1280) => OpenPreview(options, height, false);

        public static UIWorkbenchPreview OpenDice(WorkbenchOptions options, int height = 1280) => OpenPreview(options, height, true);

        private static UIWorkbenchPreview OpenPreview(WorkbenchOptions options, int height, bool showDice)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("정지 상태에서 컴파일과 가져오기를 마친 뒤 미리보기를 여세요.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("원본 프리팹 편집을 닫은 뒤 미리보기를 여세요.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            // A dice preview uses a real arrival state or the selected combat state's fixed results.
            // All commands operate only on this disposable Build result; no store is created.
            var buildOptions = showDice && options != null && options.startPoint != WorkbenchStartPoint.Combat
                ? new WorkbenchOptions { application = options.application, seed = options.seed,
                    trialId = options.trialId, cap = options.cap, startPoint = WorkbenchStartPoint.Map }
                : options;
            var state = PlayWorkbenchSession.Build(buildOptions);
            if (showDice && state.phase == RunPhase.Map)
            {
                var run = new RunSession(state);
                if (!run.ChooseNode(state.availableNodeIds[0]))
                    throw new InvalidOperationException("주사위 미리보기의 도착 상태를 만들지 못했습니다.");
                state = run.State;
            }
            var source = options.application.controller;
            var rootPrefab = source.uiRootPrefab;
            if (!rootPrefab || !rootPrefab.GetComponent<UIManager>())
                throw new InvalidOperationException("미리보기에는 원본 UIRoot와 UIManager가 필요합니다.");
            if (rootPrefab.GetComponentsInChildren<GameApplication>(true).Length != 0 ||
                rootPrefab.GetComponentsInChildren<RunUIController>(true).Length != 0)
                throw new InvalidOperationException("UIRoot에는 앱이나 플레이 컨트롤러를 포함할 수 없습니다.");
            source.uiPrefabs.Validate();
            source.visuals.Validate(state.config);
            var screenType = ScreenType(buildOptions.startPoint, state.phase);
            var registered = rootPrefab.GetComponent<UIManager>().prefabs
                .SingleOrDefault(view => view && view.GetType() == screenType);
            if (!registered || !AssetDatabase.Contains(registered))
                throw new InvalidOperationException("선택한 화면의 원본 프리팹 등록을 확인해 주세요.");

            if (showDice)
            {
                registered = rootPrefab.GetComponent<UIManager>().prefabs.SingleOrDefault(view => view is DiceRollUI);
                if (!registered || !AssetDatabase.Contains(registered))
                    throw new InvalidOperationException("6주사위 창의 원본 프리팹 등록을 확인해 주세요.");
            }
            else if (state.phase == RunPhase.ExplorationCards)
            {
                registered = rootPrefab.GetComponent<UIManager>().prefabs.SingleOrDefault(view => view is FateChoiceUI);
                if (!registered || !AssetDatabase.Contains(registered))
                    throw new InvalidOperationException("운명 선택 창의 원본 프리팹 등록을 확인해 주세요.");
            }
            if (StageUtility.GetCurrentStage() is UIWorkbenchPreview) StageUtility.GoToMainStage();
            var stage = CreateInstance<UIWorkbenchPreview>();
            stage.SourcePrefabPath = AssetDatabase.GetAssetPath(registered);
            stage.previewHeight = height;
            try
            {
                StageUtility.GoToStage(stage, true);
                if (StageUtility.GetCurrentStage() != stage || !stage.scene.IsValid())
                    throw new InvalidOperationException("UI 미리보기 스테이지를 열지 못했습니다.");
                stage.CreatePreview(buildOptions, state, showDice);
                stage.FramePreview();
                return stage;
            }
            catch
            {
                if (StageUtility.GetCurrentStage() == stage) StageUtility.GoToMainStage();
                else if (stage) DestroyImmediate(stage);
                throw;
            }
        }

        protected override GUIContent CreateHeaderContent() =>
            new GUIContent("UI 미리보기 · 저장되지 않음");

        protected override Hash128 GetHashForStateStorage() =>
            Hash128.Compute("FateDice.UIWorkbenchPreview/" + SourcePrefabPath + "/" + previewHeight);

        protected override bool OnOpenStage() => base.OnOpenStage();

        protected override void OnCloseStage()
        {
            try
            {
                if (framedView) framedView.showGrid = previousGrid;
                // Do not call UIManager.CloseAll / BaseUI.CloseView: Widgets uses runtime Destroy.
                // Child view OnDestroy methods only detach local listeners/reset local visuals.
                if (PreviewRoot) DestroyImmediate(PreviewRoot);
                PreviewRoot = null;
            }
            finally { base.OnCloseStage(); }
        }

        private void CreatePreview(WorkbenchOptions options, RunState state, bool showDice)
        {
            var source = options.application.controller;
            var holder = new GameObject("미리보기 준비");
            holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, scene);
            try
            {
                // Keep the source EventSystem inactive, remove it, then activate the completed clone.
                PreviewRoot = Instantiate(source.uiRootPrefab.gameObject, holder.transform, false);
                PreviewRoot.name = "UI 미리보기 (저장되지 않음)";
                PreviewRoot.SetActive(false);
                foreach (var module in PreviewRoot.GetComponentsInChildren<BaseInputModule>(true))
                    DestroyImmediate(module);
                foreach (var input in PreviewRoot.GetComponentsInChildren<EventSystem>(true))
                    DestroyImmediate(input);
                var emptyInput = PreviewRoot.transform.Find("Input");
                if (emptyInput && emptyInput.GetComponents<Component>().Length == 1)
                    DestroyImmediate(emptyInput.gameObject);
                PreviewRoot.transform.SetParent(null, false);
                PreviewRoot.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
            finally { DestroyImmediate(holder); }

            var root = PreviewRoot.GetComponent<UIRoot>();
            var manager = PreviewRoot.GetComponent<UIManager>();
            foreach (var scaler in PreviewRoot.GetComponentsInChildren<CanvasScaler>(true)) scaler.enabled = false;
            foreach (var raycaster in PreviewRoot.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
            root.canvas.renderMode = RenderMode.WorldSpace;
            root.canvas.worldCamera = null;
            root.canvas.scaleFactor = 1;
            var canvasRect = (RectTransform)root.canvas.transform;
            canvasRect.anchorMin = canvasRect.anchorMax = new Vector2(.5f, .5f);
            canvasRect.pivot = new Vector2(.5f, .5f);
            canvasRect.sizeDelta = new Vector2(720, previewHeight);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;
            Stretch(root.safeArea);

            var context = new RunUIContext
            {
                manager = manager, prefabs = source.uiPrefabs,
                visuals = source.visuals, presentation = state.config.presentation
            };
            BindOnce(options, state, context);
            if (showDice)
            {
                bool combat = state.phase == RunPhase.CombatCards || state.phase == RunPhase.CombatRoll;
                var hand = state.dice == null ? null : state.config.dice.hands.Single(x => x.kind == state.hand);
                manager.ShowPopup<DiceRollUI>(new DiceRollUIData
                {
                    title = combat ? "전투 주사위" : "운명의 주사위",
                    detail = state.dice == null ? "노드에 도착했습니다.\n굴리기를 눌러 운명을 확인하세요." : "고정된 실제 결과 미리보기",
                    values = state.dice == null ? null : (int[])state.dice.Clone(),
                    result = KoreanText.HandSummary(state, true),
                    rolling = false, duration = state.config.presentation.DiceTiming(combat).rollSeconds,
                    comboName = hand == null ? "" : KoreanText.Content(hand.label), hand = state.hand,
                    comboStrength = hand == null ? 0 : state.config.dice.hands.Count(x => x.priority < hand.priority) / (float)Math.Max(1, state.config.dice.hands.Length - 1),
                    holdSeconds = 0,
                    roll = Choice("roll", "주사위 6개 굴리기"),
                    appearance = context.visuals.ResolveButton(ButtonPurpose.Primary)
                });
            }
            // UIManager initializes Screen.safeArea. A fixed editor canvas uses its whole exact rectangle.
            Stretch(root.safeArea);
            manager.SetInputLocked(true);
            foreach (var selectable in PreviewRoot.GetComponentsInChildren<Selectable>(true))
            {
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.None;
                selectable.navigation = navigation;
            }
            PreviewRoot.SetActive(true);
            RebuildLayout(root, manager);
            Selection.activeGameObject = manager.Popups.Count > 0 ? manager.Popups.Last().gameObject : manager.ActiveScreen.gameObject;
        }

        private static void RebuildLayout(UIRoot root, UIManager manager)
        {
            var canvasRect = (RectTransform)root.canvas.transform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            var combat = manager.ActiveScreen as CombatUI;
            if (combat && combat.actionChoices.gameObject.activeSelf)
            {
                int count = combat.actionChoices.GetComponentsInChildren<ActionCardView>(false).Length;
                if (count > 0)
                {
                    int visible = Math.Min(3, count);
                    float width = (combat.actionScroll.viewport.rect.width -
                        combat.actionGrid.spacing.x * (visible - 1) - combat.actionGrid.padding.horizontal) / visible;
                    combat.actionGrid.cellSize = new Vector2(width, combat.actionGrid.cellSize.y);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(combat.actionChoices);
                }
            }
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                if (scroll.horizontal) scroll.horizontalNormalizedPosition = 0;
                if (scroll.vertical) scroll.verticalNormalizedPosition = manager.ActiveScreen is ExplorationUI exploration && exploration.mapContainer.gameObject.activeSelf ? 0 : 1;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            Canvas.ForceUpdateCanvases();
            // Edit-mode previews do not receive the map's runtime LateUpdate.
            // Measure its node positions only after the authored viewport has its final width.
            foreach (var map in root.GetComponentsInChildren<CampaignMapView>(true)) map.RefreshLayout();
            foreach (var popup in root.GetComponentsInChildren<FateChoiceUI>(true)) popup.RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            Canvas.ForceUpdateCanvases();
        }

        private void FramePreview()
        {
            var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            view.in2DMode = true;
            view.sceneLighting = false;
            framedView = view;
            previousGrid = view.showGrid;
            view.showGrid = false;
            view.Frame(new Bounds(Vector3.zero, new Vector3(720, previewHeight, 1)), true);
            view.Repaint();
        }

        private static Type ScreenType(WorkbenchStartPoint point, RunPhase phase)
        {
            if (point == WorkbenchStartPoint.Title) return typeof(TitleUI);
            if (point == WorkbenchStartPoint.Lobby) return typeof(MenuUI);
            switch (phase)
            {
                case RunPhase.Map:
                case RunPhase.ExplorationRoll:
                case RunPhase.ExplorationCards: return typeof(ExplorationUI);
                case RunPhase.CombatRoll:
                case RunPhase.CombatCards: return typeof(CombatUI);
                case RunPhase.Encounter:
                case RunPhase.Shop: return typeof(EncounterUI);
                case RunPhase.Reward: return typeof(RewardUI);
                case RunPhase.EquipmentChoice: return typeof(EquipmentUI);
                case RunPhase.Result: return typeof(ResultUI);
                default: throw new InvalidOperationException("해당 상태의 미리보기 화면이 없습니다.");
            }
        }

        private static UIChoiceData Choice(string key, string text, ButtonPurpose purpose = ButtonPurpose.Primary,
            bool selected = false, bool interactable = true) =>
            new UIChoiceData { key = key, text = text, purpose = purpose, selected = selected, interactable = interactable };

        private static void BindOnce(WorkbenchOptions options, RunState state, RunUIContext context)
        {
            var manager = context.manager;
            var rules = state.config;
            if (options.startPoint == WorkbenchStartPoint.Title)
            {
                manager.Show<TitleUI>(new TitleUIData
                {
                    title = "운명의 주사위", subtitle = "길을 선택하고 운명을 바꾸세요.",
                    status = "정지 미리보기 · 원본 편집은 작업실의 프리팹 버튼을 사용하세요.",
                    appearance = context.visuals.ResolveButton(ButtonPurpose.Primary)
                });
                return;
            }
            var hud = HUD(state);
            if (options.startPoint == WorkbenchStartPoint.Lobby)
            {
                string trial = string.IsNullOrEmpty(options.trialId) ? rules.combat.trialActionIds[0] : options.trialId;
                hud.header = "탐험 준비";
                hud.situation = "모험가와 출전 세팅을 확인하고 여정을 시작하세요.";
                hud.fate = "와일드 카드와 탐험 카드의 등급 상한을 선택하세요.";
                hud.gear = null; hud.menu = null;
                manager.Show<MenuUI>(new MenuUIData
                {
                    context = context, hud = hud, seed = state.initialSeed.ToString(),
                    characterName = KoreanText.Content(rules.growth.characterName),
                    characterDetails = "선택한 모험가\n\n체력  " + rules.growth.startingMaxHp + "     위력  " + rules.growth.startingPower + "     방어력  " + rules.growth.startingGuard +
                        "\n\n기본 행동\n" + string.Join(" · ", rules.combat.startingActionIds.Select(id => KoreanText.Content(rules.Action(id).label))) +
                        "\n\n기본 주사위  6개\n" + KoreanText.Content(rules.Die(rules.dice.basicDieId).label),
                    growthDetails = "여정에서 성장하기\n\n경험치를 얻으면 레벨이 오릅니다. 장비와 주사위는 여정 중 보상으로 획득합니다.\n\n" +
                        string.Join("\n", rules.growth.levels.Select((step, index) => "레벨 " + (index + 2) + "  ·  경험치 " + step.xpRequired + "\n체력 +" + step.maxHp + " / 위력 +" + step.power + " / 방어력 +" + step.guard)) +
                        "\n\n이 성장은 현재 여정에 적용됩니다.\n영구 성장 기능은 추후 제공됩니다.",
                    trials = rules.combat.trialActionIds.Select(id => Choice("trial-" + id,
                        (trial == id ? "[선택됨] " : "") + KoreanText.Content(rules.Action(id).label) + "\n" +
                        KoreanText.Grade(rules.Action(id).grade), ButtonPurpose.Trial, trial == id)).ToArray(),
                    caps = Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(grade => Choice("cap-" + grade,
                        (state.explorationCap == grade ? "●\n" : "") + KoreanText.Grade(grade),
                        ButtonPurpose.Grade, state.explorationCap == grade)).ToArray(),
                    start = Choice("new", "새 여정")
                });
                return;
            }
            bool exploration = state.phase == RunPhase.Map || state.phase == RunPhase.ExplorationRoll ||
                state.phase == RunPhase.ExplorationCards;
            var revealed = !exploration && state.phase != RunPhase.Result && !string.IsNullOrEmpty(state.activeEventId)
                ? (state.boss ? context.visuals.ResolveNode(NodeType.Boss) : context.visuals.ResolveEvent(rules, state.activeEventId)) : null;
            switch (state.phase)
            {
                case RunPhase.Map:
                case RunPhase.ExplorationRoll:
                case RunPhase.ExplorationCards:
                    hud.situation = state.phase == RunPhase.Map ? "갈림길 선택" :
                        "선택한 길  /  " + KoreanText.Node(state.selectedNode.type);
                    var mapData = new ExplorationUIData
                    {
                        context = context, hud = hud,
                        nodes = state.nodes.Select(n => new NodeState { id = n.id, type = n.type, childIds = n.childIds.ToList() }).ToArray(),
                        available = state.availableNodeIds.ToArray(), selected = state.selectedNode?.id,
                        completedNodes = state.resolvedEventIds.Select(id => state.nodeHistory.FirstOrDefault(n => n.id == id))
                            .Where(n => n != null).Select(n => new NodeState { id = n.id, type = n.type, childIds = n.childIds.ToList() }).ToArray(),
                        instructions = state.phase == RunPhase.ExplorationRoll ?
                            "주사위를 굴려 " + rules.world.offeredCards + "장의 운명 카드를 확인하세요." : null,
                        roll = state.phase == RunPhase.ExplorationRoll ? Choice("roll", "주사위 여섯 개 굴리기") : null,
                        cap = state.phase == RunPhase.Map ? Choice("cap-cycle", "등급 상한: " +
                            KoreanText.Grade(state.explorationCap), ButtonPurpose.Navigation) : null,
                        fates = Array.Empty<FateOfferUIData>()
                    };
                    CampaignMapProjection.Apply(mapData, state);
                    manager.Show<ExplorationUI>(mapData);
                    if (state.phase == RunPhase.ExplorationCards)
                        manager.ShowPopup<FateChoiceUI>(new FateChoiceUIData
                        {
                            context = context, hud = hud,
                            offers = state.cards.Select(card => new FateOfferUIData
                            { id = card.id, type = card.type, grade = card.grade }).ToArray()
                        });
                    break;
                case RunPhase.CombatRoll:
                case RunPhase.CombatCards:
                    var stats = GrowthRules.Stats(state);
                    var enemy = rules.Enemy(state.activeEnemyId);
                    var intent = enemy.intents.Single(x => x.id == state.activeIntentId);
                    manager.Show<CombatUI>(new CombatUIData
                    {
                        context = context, hud = hud, revealed = revealed,
                        battleLabel = state.boss ? "대운명" : "전투  /  사건 " + (state.eventsResolved + 1),
                        turnLabel = "누적 턴 " + (state.combatTurns + 1),
                        enemyName = KoreanText.Content(enemy.label), enemyHealth = state.enemyHp + " / " + enemy.maxHp,
                        enemyHealth01 = Mathf.Clamp01((float)state.enemyHp / enemy.maxHp),
                        enemyShield = state.enemyShield > 0 ? "수호 " + state.enemyShield : null,
                        intentLabel = "다음 행동: " + KoreanText.Content(intent.label),
                        intentValue = CombatRules.IntentAmount(state).ToString(),
                        playerName = KoreanText.Content(rules.growth.characterName),
                        playerHealth = "체력 " + state.hp + "/" + stats.maxHp,
                        playerHealth01 = Mathf.Clamp01((float)state.hp / stats.maxHp),
                        playerShield = "수호 " + state.shield,
                        playerAttributes = "위력 " + stats.power + "  /  방어력 " + stats.guard,
                        handLabel = KoreanText.HandStage(state),
                        fatePowerLabel = state.dice == null ? "운명력  —" : "운명력  " + state.fatePower,
                        rerollLabel = state.rerollUnlocked ? "재굴림 " + state.rerollCharges : null,
                        roll = state.phase == RunPhase.CombatRoll ? Choice("roll", "주사위 여섯 개 굴리기") : null,
                        actions = state.phase == RunPhase.CombatCards ? state.cards.Select(card =>
                        {
                            var action = rules.Action(card.contentId);
                            var effect = CombatRules.Evaluate(state, card);
                            return new ActionOfferUIData
                            {
                                id = card.id, originalId = card.contentId, label = KoreanText.Content(action.label),
                                grade = card.grade, effect = "피해 " + effect.damage + " / 수호 " + effect.block,
                                tags = (string[])action.tags.Clone(), visual = context.visuals.ResolveAction(rules, card.contentId)
                            };
                        }).ToArray() : Array.Empty<ActionOfferUIData>()
                    });
                    break;
                case RunPhase.Shop:
                    hud.situation = "여행자의 상점  /  " + state.gold + " 골드";
                    var products = rules.world.shop.Select(goods => Choice("buy-" + goods.id,
                        KoreanText.Content(goods.label) + "  /  " + goods.price + " 골드\n" +
                        (state.purchasedIds.Contains(goods.id) ? "구매 완료" : Reward(state, goods.reward)),
                        ButtonPurpose.Purchase, interactable: !state.purchasedIds.Contains(goods.id) && state.gold >= goods.price)).ToList();
                    products.Add(Choice("leave", "상점 나가기"));
                    manager.Show<EncounterUI>(new EncounterUIData { context = context, hud = hud, revealed = revealed, choices = products.ToArray() });
                    break;
                case RunPhase.Encounter:
                    var encounter = rules.Event(state.activeEventId);
                    hud.situation = KoreanText.Grade(state.activeGrade) + "  /  " + KoreanText.Content(encounter.label);
                    manager.Show<EncounterUI>(new EncounterUIData
                    {
                        context = context, hud = hud, revealed = revealed,
                        description = KoreanText.Content(encounter.description), outcome = "결과\n" + Reward(state, state.pendingReward),
                        choices = new[] { Choice("resolve", encounter.type == NodeType.Rest ? "휴식하고 회복" : "결과 받아들이기") }
                    });
                    break;
                case RunPhase.Reward:
                    hud.situation = "결과  /  보상";
                    manager.Show<RewardUI>(new RewardUIData
                    {
                        context = context, hud = hud, revealed = revealed, description = Reward(state, state.pendingReward),
                        instructions = "보상을 받은 뒤 장비 선택을 마치면 여정이 이어집니다.",
                        claim = Choice("claim", "보상 받고 계속하기")
                    });
                    break;
                case RunPhase.EquipmentChoice:
                    hud.situation = "발견한 장비";
                    var data = new EquipmentUIData { context = context, hud = hud, revealed = revealed };
                    if (!string.IsNullOrEmpty(state.pendingEquipmentId))
                    {
                        var item = rules.Equipment(state.pendingEquipmentId);
                        var old = state.equipmentIds[(int)item.slot];
                        data.details = KoreanText.Slot(item.slot) + "  /  " + KoreanText.Content(item.label) + "\n" + Equipment(item);
                        data.current = "현재 장비: " + (string.IsNullOrEmpty(old) ? "빈 슬롯" :
                            KoreanText.Content(rules.Equipment(old).label) + "\n" + Equipment(rules.Equipment(old)));
                        data.choices = new[] { Choice("equip-accept", "장비 교체"), Choice("equip-decline", "두고 가기") };
                    }
                    else
                    {
                        var die = rules.Die(state.pendingDieId);
                        data.details = KoreanText.Content(die.label) + "\n눈: " + string.Join(", ", die.values) +
                            "\n등장 가중치: " + string.Join(", ", die.weights);
                        data.current = "현재 주사위\n" + string.Join(" / ", state.dieIds.Select(id => KoreanText.Content(rules.Die(id).label)));
                        data.choices = new[] { Choice("die-skip", "기존 주사위 유지") };
                    }
                    manager.Show<EquipmentUI>(data);
                    break;
                case RunPhase.Result:
                    hud.situation = state.won ? "여정 완료" : "여정 종료";
                    hud.gear = null;
                    manager.Show<ResultUI>(new ResultUIData
                    {
                        context = context, hud = hud,
                        summary = "사건 " + state.eventsResolved + "   전투 턴 " + state.combatTurns +
                            "\n시드 " + state.initialSeed + "   시간 " + TimeSpan.FromSeconds(state.playedSeconds).ToString(@"mm\:ss"),
                        grades = "선택한 등급 (운명 + 행동)\n" + string.Join(" / ", state.selectedGrades.Select((count, index) =>
                            KoreanText.Grade((Grade)index) + ": " + count)),
                        restart = Choice("restart", "새로운 여정 시작")
                    });
                    break;
                default: throw new InvalidOperationException("표시할 미리보기 상태가 없습니다.");
            }
        }

        private static RunHUDData HUD(RunState state)
        {
            var rules = state.config;
            var stats = GrowthRules.Stats(state);
            var xp = state.level <= rules.growth.levels.Length ? rules.growth.levels[state.level - 1].xpRequired.ToString() : "최대";
            return new RunHUDData
            {
                header = "운명의 주사위  /  " + (state.boss ? "대운명" : "여정"),
                stats = KoreanText.Content(rules.growth.characterName) + "  체력 " + state.hp + "/" + stats.maxHp +
                    "   수호 " + state.shield + "\n위력 " + stats.power + "   방어력 " + stats.guard +
                    "   레벨 " + state.level + "\n경험치 " + state.xp + "/" + xp + "   골드 " + state.gold,
                situation = "여정 " + state.eventsResolved + "/" + rules.world.eventsToBoss,
                dice = state.dice == null ? null : (int[])state.dice.Clone(),
                fate = state.dice == null ? "대운명 " + state.eventsResolved + "/" + rules.world.eventsToBoss +
                    "  •  탐험 등급 상한 " + KoreanText.Grade(state.explorationCap) : Hand(state),
                notice = KoreanText.Notice(state.message),
                gear = string.Join("\n", state.equipmentIds.Select((id, index) => KoreanText.Slot((EquipmentSlot)index) +
                    ": " + (string.IsNullOrEmpty(id) ? "비어 있음" : KoreanText.Content(rules.Equipment(id).label)))),
                menu = Choice("menu", "메뉴", ButtonPurpose.Navigation)
            };
        }

        private static string Hand(RunState state) => KoreanText.HandSummary(state);

        private static string Reward(RunState state, RewardDefinition reward)
        {
            if (reward == null) return "받을 보상이 없습니다";
            var parts = new List<string>();
            if (reward.gold != 0) parts.Add(reward.gold + " 골드");
            if (reward.xp != 0) parts.Add(reward.xp + " 경험치");
            if (reward.health != 0) parts.Add("체력 " + (reward.health > 0 ? "+" : "") + reward.health);
            if (reward.rerollCharges > 0) parts.Add(reward.rerollCharges + "회 재굴림");
            if (!string.IsNullOrEmpty(reward.equipmentId)) parts.Add("장비: " + KoreanText.Content(state.config.Equipment(reward.equipmentId).label));
            if (!string.IsNullOrEmpty(reward.dieId)) parts.Add("주사위: " + KoreanText.Content(state.config.Die(reward.dieId).label));
            return parts.Count == 0 ? "아이템 보상 없음" : string.Join("  •  ", parts);
        }

        private static string Equipment(EquipmentDefinition item)
        {
            var value = "체력 +" + item.maxHp + "  위력 +" + item.power + "  방어력 +" + item.guard +
                "\n태그: " + string.Join(", ", item.tags.Select(KoreanText.Tag));
            foreach (var modifier in item.modifiers)
                value += "\n" + KoreanText.Match(modifier.match) + " " + KoreanText.Source(modifier.source) +
                    " [" + string.Join(", ", modifier.requiredTags.Select(KoreanText.Tag)) + "]: " +
                    KoreanText.Value(modifier.value) + " " + (modifier.bonus >= 0 ? "+" : "") +
                    (modifier.bonus * 100).ToString("0.#") + "%";
            return value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
