using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FateDice
{
    // Owns the application state and commands. Views receive display snapshots only.
    public class RunUIController : MonoBehaviour
    {
        [Tooltip("New runs take a validated snapshot of this config.")] public FateDiceConfig config;
        [Tooltip("Font used when authoring the screen prefabs.")] public Font uiFont;
        public FateDiceVisualCatalog visuals;
        public UiPrefabReferences uiPrefabs;
        [Tooltip("Authored Canvas, safe area, layers and explicit screen registry.")] public UIRoot uiRootPrefab;
        [Tooltip("Standalone prototype initializes on Awake; GameApplication initializes explicitly.")] public bool initializeOnAwake = true;
        public UIManager UI { get; private set; }
        public FateDiceWidgets Widgets => (UI?.ActiveScreen as IRunScreenView)?.Widgets;
        public GameConfigData PreviewConfig { get; private set; }
        public RunSession Session { get; private set; }
        public bool Busy => actionBusy || (navigation?.IsTransitioning ?? false);
        public LocalRunStore Store { get; private set; }
        public uint Seed { get; set; } = 33;
        private string trialId;
        private Grade cap = Grade.Legendary;
        private bool menu = true;
        private string error;
        private RunState savedPreview;
        private bool invalidSave;
        private bool actionBusy;
        private bool atTitle;
        private Action enterLobby;
        private IRunSceneNavigation navigation;

        protected virtual void Awake()
        {
            if (!initializeOnAwake) return;
            try { Initialize(new LocalRunStore(System.IO.Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json"))); }
            catch (Exception e) { Debug.LogError(e.Message, this); enabled = false; }
        }

        public void Initialize(LocalRunStore store, IRunSceneNavigation sceneNavigation = null)
        {
            if (UI) throw new InvalidOperationException("RunUIController has already been initialized.");
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (!config || !uiFont || !uiRootPrefab || !visuals)
                throw new InvalidOperationException("Fate Dice requires its config, font, visual catalog and UIRoot prefab.");
            PreviewConfig = config.Snapshot();
            trialId = PreviewConfig.combat.trialActionIds[0];
            visuals.Validate(PreviewConfig);
            uiPrefabs.Validate();
            var root = Instantiate(uiRootPrefab, transform, false);
            UI = root.GetComponent<UIManager>();
            if (!UI) throw new InvalidOperationException("UIRoot prefab requires UIManager.");
            navigation = sceneNavigation;
            UseStore(store);
        }

        public void UseStore(LocalRunStore store)
        {
            if (Busy) throw new InvalidOperationException("Cannot switch saves during an action.");
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Session = null; menu = true; atTitle = false; ReadSavedPreview(); Render();
        }
        public void EnterTitle(Action continueToLobby)
        {
            enterLobby = continueToLobby ?? throw new ArgumentNullException(nameof(continueToLobby));
            menu = true; atTitle = true; error = null; Render();
        }
        public void EnterLobby()
        {
            menu = true; atTitle = false; PreviewConfig = config.Snapshot(); ReadSavedPreview(); Render();
        }
        public bool TryEnterInGame()
        {
            if (Session == null)
            {
                if (!Store.Exists) return false;
                try { Session = new RunSession(Store.Load()) { Checkpoint = Store.Save }; }
                catch (Exception e) { invalidSave = true; error = PlayerError(e); return false; }
            }
            menu = false; atTitle = false; error = null; Render(); return true;
        }
        public void SyncInputLock() { if (UI) UI.SetInputLocked(Busy); }
        public void ReportNavigationError(string message)
        {
            Debug.LogWarning(message, this);
            error = "화면을 불러오지 못했습니다. 메뉴에서 다시 시도해 주세요.";
            if (atTitle) Render();
            else if (Widgets != null) Widgets.Notice.text = error;
            SyncInputLock();
        }
        private void ReadSavedPreview()
        {
            savedPreview = null; invalidSave = false; error = null;
            if (Store == null || !Store.Exists) return;
            try { savedPreview = Store.Load(); }
            catch (Exception e) { invalidSave = true; error = PlayerError(e); }
        }
        private void StartNewJourney()
        {
            if (Busy || invalidSave) return;
            try
            {
                var next = RunSession.New(config.Snapshot(), Seed, trialId, cap);
                next.State.lastResult = (Session?.State ?? savedPreview)?.lastResult;
                Store.Save(next.State); next.Checkpoint = Store.Save;
                Session = next; savedPreview = next.State; error = null;
                if (navigation != null) navigation.RequestInGame();
                else { menu = false; Render(); }
            }
            catch (Exception e) { error = PlayerError(e); Widgets.Notice.text = "시작하지 못했습니다: " + error; }
        }
        private void ContinueJourney()
        {
            if (Busy || invalidSave) return;
            try
            {
                Session = new RunSession(Store.Load()) { Checkpoint = Store.Save };
                error = null;
                if (navigation != null) navigation.RequestInGame();
                else { menu = false; Render(); }
            }
            catch (Exception e) { invalidSave = true; error = PlayerError(e); menu = true; Render(); }
        }
        private void ArchiveDamagedSave()
        {
            if (Busy) return;
            try { Store.Archive(); Session = null; ReadSavedPreview(); Render(); }
            catch (Exception e) { error = PlayerError(e); Render(); }
        }
        protected virtual void OnApplicationPause(bool paused) { if (paused && !menu) SaveElapsedTime(); }
        protected virtual void OnApplicationQuit() { if (!menu) SaveElapsedTime(); }
        protected virtual void OnDestroy() { if (UI) UI.CloseAll(); }
        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            Widgets?.ResetSelectionFeedback();
            if (UI && UI.ActiveScreen is CombatUI combat) combat.ResetFeedback();
            if (UI && UI.Popups.LastOrDefault() is DiceRollUI) UI.CloseTopPopup();
            actionBusy = false; SyncInputLock();
        }
        protected virtual void OnEnable() { if (UI) Render(); }
        private bool SaveElapsedTime()
        {
            if (Session == null || Store == null) return true;
            try { Store.Save(Session.State); return true; }
            catch (Exception e) { error = PlayerError(e); if (Widgets != null) Widgets.Notice.text = "자동 저장 실패: " + error; return false; }
        }
        protected virtual void Update()
        {
            if (UI) UI.Root.ApplySafeArea();
            if (Session != null && !menu && !(navigation?.IsTransitioning ?? false) && Session.State.phase != RunPhase.Result)
                Session.State.playedSeconds += Time.unscaledDeltaTime;
            if (!Busy && UI && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (UI.Popups.Count > 0) UI.CloseTopPopup();
                else if (!menu) ShowMenu();
            }
        }

        private RunUIContext Context(PresentationSettings presentation) => new RunUIContext
        { manager = UI, prefabs = uiPrefabs, visuals = visuals, presentation = presentation };
        private static UIChoiceData Choice(string key, string text, Action clicked, bool interactable = true,
            ButtonPurpose purpose = ButtonPurpose.Primary, bool selected = false) => new UIChoiceData
        { key = key, text = text, clicked = clicked, interactable = interactable, purpose = purpose, selected = selected };
        private void Show<T>(RunUIData data) where T : BaseUI
        {
            UI.Show<T>(data);
            if (!menu && Session != null && (Session.State.phase == RunPhase.ExplorationRoll || Session.State.phase == RunPhase.CombatRoll))
                ShowDiceWindow(false);
            UI.SetInputLocked(Busy);
        }

        private void Render()
        {
            if (atTitle)
            {
                UI.Show<TitleUI>(new TitleUIData { title = "운명의 주사위", subtitle = "한 명의 모험가, 여섯 개의 주사위, 당신의 여정.",
                    status = error ?? "여정을 시작할 준비가 되었습니다", appearance = visuals.ResolveButton(ButtonPurpose.Primary),
                    enterLobby = () => { if (!Busy) enterLobby(); } });
                SyncInputLock(); return;
            }
            if (menu) { RenderMenu(); return; }
            var state = Session.State;
            var rules = state.config;
            var stats = GrowthRules.Stats(state);
            var xpTarget = state.level <= rules.growth.levels.Length ? rules.growth.levels[state.level - 1].xpRequired.ToString() : "최대";
            var rerollPhase = state.phase == RunPhase.ExplorationCards || state.phase == RunPhase.CombatCards;
            var hud = new RunHUDData
            {
                header = "운명의 주사위  /  " + (state.boss ? "대운명" : "여정"),
                stats = KoreanText.Content(rules.growth.characterName) + "  체력 " + state.hp + "/" + stats.maxHp + "   수호 " + state.shield +
                    "\n위력 " + stats.power + "   방어력 " + stats.guard + "   레벨 " + state.level + "\n경험치 " + state.xp + "/" + xpTarget + "   골드 " + state.gold,
                notice = error ?? KoreanText.Notice(state.message),
                situation = "여정 " + state.eventsResolved + "/" + rules.world.eventsToBoss,
                dice = state.dice == null ? null : (int[])state.dice.Clone(),
                fate = state.dice == null ? "대운명 " + state.eventsResolved + "/" + rules.world.eventsToBoss + "  •  탐험 등급 상한 " + KoreanText.Grade(state.explorationCap) :
                    KoreanText.HandSummary(state),
                gear = state.phase == RunPhase.Result ? null : GearSummary(state),
                menu = Choice("menu", "메뉴", ShowMenu, purpose: ButtonPurpose.Navigation)
            };
            if (rerollPhase && state.rerollUnlocked)
            {
                hud.fate += "\n재굴림: " + state.rerollCharges + "  /  주사위 선택 (" + rules.growth.rerollCost + "회 소모)";
                if (state.rerollCharges >= rules.growth.rerollCost) hud.dieClicked = index => Command(() => Session.Reroll(index), true);
            }
            var context = Context(rules.presentation);
            var showPaths = state.phase == RunPhase.Map || state.phase == RunPhase.ExplorationRoll || state.phase == RunPhase.ExplorationCards;
            // Resolve private event art only after the fate has been selected.
            var revealed = !showPaths && state.phase != RunPhase.Result && !string.IsNullOrEmpty(state.activeEventId)
                ? (state.boss ? visuals.ResolveNode(NodeType.Boss) : visuals.ResolveEvent(rules, state.activeEventId)) : null;
            switch (state.phase)
            {
                case RunPhase.Map:
                case RunPhase.ExplorationRoll:
                case RunPhase.ExplorationCards:
                    var exploration = new ExplorationUIData
                    {
                        context = context, hud = hud,
                        nodes = state.nodes.Select(n => new NodeState { id = n.id, type = n.type, childIds = n.childIds.ToList() }).ToArray(),
                        completedNodes = state.resolvedEventIds.Select(id => state.nodeHistory.FirstOrDefault(n => n.id == id))
                            .Where(n => n != null).Select(n => new NodeState { id = n.id, type = n.type, childIds = n.childIds.ToList() }).ToArray(),
                        available = state.availableNodeIds.ToArray(), selected = state.selectedNode?.id,
                        chooseNode = TravelToNode,
                        fates = Array.Empty<FateOfferUIData>()
                    };
                    if (state.phase == RunPhase.Map)
                    {
                        hud.situation = state.eventsResolved >= rules.world.eventsToBoss ? "다음  /  대운명" :
                            "갈림길 선택  /  " + (rules.world.eventsToBoss - state.eventsResolved) + "번의 사건 후 대운명";
                        exploration.cap = Choice("cap-cycle", "등급 상한: " + KoreanText.Grade(state.explorationCap),
                            () => Command(() => Session.SetExplorationCap((Grade)(((int)Session.State.explorationCap + 1) % 5))), purpose: ButtonPurpose.Navigation);
                    }
                    else if (state.phase == RunPhase.ExplorationRoll)
                    {
                        hud.situation = "선택한 길  /  " + KoreanText.Node(state.selectedNode.type);
                        exploration.instructions = "주사위를 굴려 " + rules.world.offeredCards + "장의 운명 카드를 확인하세요.\n한 장은 선택한 길의 유형으로 제시됩니다.";
                        exploration.roll = RollChoice();
                    }
                    else
                    {
                        hud.situation = "운명 선택  /  " + KoreanText.Node(state.selectedNode.type) + " 경로";
                        exploration.fates = state.cards.Select(c => new FateOfferUIData
                        { id = c.id, type = c.type, grade = c.grade, clicked = id => Command(() => Session.ChooseFate(id), selectionKey: "fate-" + id) }).ToArray();
                    }
                    Show<ExplorationUI>(exploration); break;
                case RunPhase.CombatRoll:
                case RunPhase.CombatCards:
                    var enemy = rules.Enemy(state.activeEnemyId);
                    var intent = enemy.intents.Single(x => x.id == state.activeIntentId);
                    hud.situation = "[적] " + KoreanText.Content(enemy.label) + "  체력 " + state.enemyHp + "/" + enemy.maxHp + "  수호 " + state.enemyShield +
                        "\n다음 행동: " + KoreanText.Content(intent.label) + " " + CombatRules.IntentAmount(state);
                    var combat = new CombatUIData
                    {
                        context = context, hud = hud, revealed = revealed, actions = Array.Empty<ActionOfferUIData>(),
                        battleLabel = state.boss ? "대운명" : "전투  /  사건 " + (state.eventsResolved + 1),
                        turnLabel = "누적 턴 " + (state.combatTurns + 1),
                        enemyName = KoreanText.Content(enemy.label), enemyHealth = state.enemyHp + " / " + enemy.maxHp,
                        enemyHealth01 = Mathf.Clamp01((float)state.enemyHp / enemy.maxHp),
                        enemyShield = state.enemyShield > 0 ? "수호 " + state.enemyShield : null,
                        intentLabel = "다음 행동: " + KoreanText.Content(intent.label),
                        intentValue = CombatRules.IntentAmount(state).ToString(),
                        playerName = KoreanText.Content(rules.growth.characterName), playerHealth = "체력 " + state.hp + "/" + stats.maxHp,
                        playerHealth01 = Mathf.Clamp01((float)state.hp / stats.maxHp), playerShield = "수호 " + state.shield,
                        playerAttributes = "위력 " + stats.power + "  /  방어력 " + stats.guard,
                        handLabel = state.phase == RunPhase.CombatCards ? KoreanText.HandStage(state) : "주사위를 굴려 확인",
                        fatePowerLabel = state.phase == RunPhase.CombatCards ? "운명력  " + state.fatePower : "운명력  —",
                        rerollLabel = state.rerollUnlocked ? "재굴림 " + state.rerollCharges : null
                    };
                    if (state.phase == RunPhase.CombatRoll) combat.roll = RollChoice();
                    else combat.actions = state.cards.Select(c =>
                    {
                        var action = rules.Action(c.contentId);
                        var effect = CombatRules.Evaluate(state, c);
                        return new ActionOfferUIData { id = c.id, originalId = c.contentId, label = KoreanText.Content(action.label), grade = c.grade,
                            effect = "피해 " + effect.damage + " / 수호 " + effect.block, tags = (string[])action.tags.Clone(),
                            visual = visuals.ResolveAction(rules, c.contentId), clicked = id => Command(() => Session.ChooseAction(id), selectionKey: "card-" + id, actionId: id) };
                    }).ToArray();
                    Show<CombatUI>(combat); break;
                case RunPhase.Encounter:
                    var encounter = rules.Event(state.activeEventId);
                    hud.situation = KoreanText.Grade(state.activeGrade) + "  /  " + KoreanText.Content(encounter.label);
                    var choices = new System.Collections.Generic.List<UIChoiceData>
                    { Choice("resolve", encounter.type == NodeType.Rest ? "휴식하고 회복" : "결과 받아들이기", () => Command(() => Session.ResolveEncounter(false))) };
                    if (encounter.type == NodeType.Rest) choices.Add(Choice("train", "대신 훈련하기\n" + RewardText(Session.RestTrainingReward()), () => Command(() => Session.ResolveEncounter(true))));
                    Show<EncounterUI>(new EncounterUIData { context = context, hud = hud, revealed = revealed,
                        description = KoreanText.Content(encounter.description), outcome = "결과\n" + RewardText(state.pendingReward), choices = choices.ToArray() }); break;
                case RunPhase.Shop:
                    hud.situation = "여행자의 상점  /  " + state.gold + " 골드";
                    var products = rules.world.shop.Select(goods => Choice("buy-" + goods.id,
                        KoreanText.Content(goods.label) + "  /  " + goods.price + " 골드\n" + (state.purchasedIds.Contains(goods.id) ? "구매 완료" : RewardText(goods.reward)),
                        () => Command(() => Session.Buy(goods.id)), !state.purchasedIds.Contains(goods.id) && state.gold >= goods.price, ButtonPurpose.Purchase)).ToList();
                    products.Add(Choice("leave", "상점 나가기", () => Command(() => Session.LeaveShop())));
                    Show<EncounterUI>(new EncounterUIData { context = context, hud = hud, revealed = revealed, choices = products.ToArray() }); break;
                case RunPhase.Reward:
                    hud.situation = !string.IsNullOrEmpty(state.activeEnemyId) && state.enemyHp == 0 ? "승리  /  보상" : "결과  /  보상";
                    Show<RewardUI>(new RewardUIData { context = context, hud = hud, revealed = revealed, description = RewardText(state.pendingReward),
                        instructions = "보상을 받은 뒤 장비 선택을 마치면 여정이 이어집니다.",
                        claim = Choice("claim", "보상 받고 계속하기", () => Command(() => Session.ClaimReward())) }); break;
                case RunPhase.EquipmentChoice:
                    var equipment = new EquipmentUIData { context = context, hud = hud, revealed = revealed };
                    hud.situation = "발견한 장비";
                    if (!string.IsNullOrEmpty(state.pendingEquipmentId))
                    {
                        var item = rules.Equipment(state.pendingEquipmentId);
                        var oldId = state.equipmentIds[(int)item.slot];
                        equipment.details = KoreanText.Slot(item.slot) + "  /  " + KoreanText.Content(item.label) + "\n" + EquipmentText(item);
                        equipment.current = "현재 장비: " + (string.IsNullOrEmpty(oldId) ? "빈 슬롯" : KoreanText.Content(rules.Equipment(oldId).label) + "\n" + EquipmentText(rules.Equipment(oldId)));
                        equipment.choices = new[] { Choice("equip-accept", "장비 교체", () => Command(() => Session.Equip(true))), Choice("equip-decline", "두고 가기", () => Command(() => Session.Equip(false))) };
                    }
                    else
                    {
                        var die = rules.Die(state.pendingDieId);
                        hud.situation = "발견한 주사위  /  " + KoreanText.Content(die.label);
                        hud.dieClicked = index => Command(() => Session.ReplaceDie(index));
                        equipment.details = "위의 주사위 여섯 개 중 교체할 하나를 고르세요.\n눈: " + string.Join(", ", die.values) + "\n등장 가중치: " + string.Join(", ", die.weights);
                        equipment.current = "현재 주사위\n" + string.Join(" / ", state.dieIds.Select(id => KoreanText.Content(rules.Die(id).label)));
                        equipment.choices = new[] { Choice("die-skip", "기존 주사위 유지", () => Command(() => Session.ReplaceDie(-1))) };
                    }
                    Show<EquipmentUI>(equipment); break;
                case RunPhase.Result:
                    hud.situation = state.won ? "여정 완료" : "여정 종료";
                    Show<ResultUI>(new ResultUIData { context = context, hud = hud,
                        summary = "사건 " + state.eventsResolved + "   전투 턴 " + state.combatTurns + "\n시드 " + state.initialSeed + "   시간 " + TimeSpan.FromSeconds(state.playedSeconds).ToString(@"mm\:ss"),
                        grades = "선택한 등급 (운명 + 행동)\n" + string.Join(" / ", state.selectedGrades.Select((count, index) => ShortGrade((Grade)index) + ": " + count)),
                        restart = Choice("restart", "준비 로비로 돌아가기", ShowMenu) }); break;
                default: throw new InvalidOperationException("No UI registered for phase " + state.phase);
            }
        }
        private UIChoiceData RollChoice() => Choice("open-dice", "주사위 창 열기", () => { if (!Busy) ShowDiceWindow(false); });

        private void ShowDiceWindow(bool rolling)
        {
            var state = Session.State;
            bool combat = state.phase == RunPhase.CombatRoll || state.phase == RunPhase.CombatCards;
            float duration = Mathf.Max(.65f, state.config.presentation.rollSeconds);
            UI.ShowPopup<DiceRollUI>(new DiceRollUIData
            {
                title = combat ? "전투 주사위" : "운명의 주사위",
                detail = error ?? (rolling ? "여섯 개의 결과로 선택지가 정해집니다." :
                    combat ? "주사위를 굴려 이번 턴의 행동을 확인하세요." :
                    KoreanText.Node(state.selectedNode.type) + " 노드에 도착했습니다.\n굴리기를 눌러 운명을 확인하세요."),
                values = rolling ? (int[])state.dice.Clone() : null,
                result = rolling ? KoreanText.HandSummary(state, true) : "6개의 주사위가 준비되었습니다",
                rolling = rolling, duration = duration,
                roll = rolling ? null : Choice("roll", "주사위 6개 굴리기", () => Command(() => Session.Roll(), true)),
                appearance = visuals.ResolveButton(ButtonPurpose.Primary)
            });
            SyncInputLock();
        }

        private void TravelToNode(string id)
        {
            if (Busy) return;
            StartCoroutine(ArriveAtNode(id));
        }

        private IEnumerator ArriveAtNode(string id)
        {
            actionBusy = true; SyncInputLock(); error = null;
            bool moved = false;
            try { moved = Session.ChooseNode(id); }
            catch (Exception e) { error = "이동을 저장하지 못했습니다. " + PlayerError(e); }
            if (moved && Widgets != null) yield return Widgets.AnimateNodeArrival(id, .35f);
            Render();
            actionBusy = false; SyncInputLock();
        }
        private void RenderMenu()
        {
            var rules = PreviewConfig;
            var record = savedPreview?.lastResult;
            Show<MenuUI>(new MenuUIData
            {
                context = Context(rules.presentation),
                characterName = KoreanText.Content(rules.growth.characterName),
                characterDetails = "선택한 모험가\n\n체력  " + rules.growth.startingMaxHp + "     위력  " + rules.growth.startingPower + "     방어력  " + rules.growth.startingGuard +
                    "\n\n기본 행동\n" + string.Join(" · ", rules.combat.startingActionIds.Select(id => KoreanText.Content(rules.Action(id).label))) +
                    "\n\n기본 주사위  6개\n" + KoreanText.Content(rules.Die(rules.dice.basicDieId).label),
                growthDetails = "여정에서 성장하기\n\n경험치를 얻으면 레벨이 오릅니다. 장비와 주사위는 여정 중 보상으로 획득합니다.\n\n" +
                    string.Join("\n", rules.growth.levels.Select((step, index) => "레벨 " + (index + 2) + "  ·  경험치 " + step.xpRequired + "\n체력 +" + step.maxHp + " / 위력 +" + step.power + " / 방어력 +" + step.guard)) +
                    "\n\n이 성장은 현재 여정에 적용됩니다.\n영구 성장 기능은 추후 제공됩니다.",
                hud = new RunHUDData
                {
                    header = "탐험 준비", stats = KoreanText.Content(rules.growth.characterName) + "  •  모험 정보\n체력 " + rules.growth.startingMaxHp + "   위력 " + rules.growth.startingPower + "   방어력 " + rules.growth.startingGuard,
                    situation = "모험가와 출전 세팅을 확인하고 여정을 시작하세요.",
                    fate = "와일드 카드와 탐험 카드의 등급 상한을 선택하세요.",
                    notice = invalidSave ? "저장 파일을 확인해 주세요" : error ?? "자동 저장 • 보상은 현재 여정에만 적용됩니다"
                },
                trials = rules.combat.trialActionIds.Select(id => Choice("trial-" + id,
                    (trialId == id ? "[선택됨] " : "") + KoreanText.Content(rules.Action(id).label) + "\n" + KoreanText.Grade(rules.Action(id).grade),
                    () => { if (Busy) return; trialId = id; Render(); }, purpose: ButtonPurpose.Trial, selected: trialId == id)).ToArray(),
                caps = Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(grade => Choice("cap-" + grade,
                    (cap == grade ? "●\n" : "") + ShortGrade(grade),
                    () => { if (Busy) return; cap = grade; Render(); }, purpose: ButtonPurpose.Grade, selected: cap == grade)).ToArray(),
                seed = Seed.ToString(), seedChanged = text =>
                {
                    if (uint.TryParse(text, out var value) && value != 0) { Seed = value; error = null; }
                    else { error = "시드는 1~4294967295 사이의 숫자로 입력해 주세요."; Widgets.Notice.text = error; }
                },
                start = Choice("new", Store.Exists ? "새 여정 (기존 저장 덮어쓰기)" : "새 여정", StartNewJourney, !invalidSave),
                resume = Store.Exists ? Choice("continue", "저장된 여정 이어하기", ContinueJourney, !invalidSave) : null,
                archive = invalidSave ? Choice("archive", "손상 파일 보관 후 저장 슬롯 비우기", ArchiveDamagedSave) : null,
                error = invalidSave ? error : null,
                lastResult = record != null && !string.IsNullOrEmpty(record.runId) ? "지난 결과  /  " + (record.won ? "완료" : "패배") + "\n사건 " + record.events + "  턴 " + record.combatTurns + "  시드 " + record.seed : null
            });
        }
        private void ShowMenu()
        {
            if (Busy) return;
            if (!SaveElapsedTime()) return;
            if (navigation != null) navigation.RequestLobby();
            else EnterLobby();
        }
        private void Command(Func<bool> command, bool roll = false, string selectionKey = null, string actionId = null)
        {
            if (Busy) return;
            StartCoroutine(Perform(command, roll, selectionKey, actionId));
        }
        private IEnumerator Perform(Func<bool> command, bool roll, string selectionKey, string actionId)
        {
            actionBusy = true; SyncInputLock(); error = null;
            bool changed = false;
            var before = Session.State;
            var combatView = UI.ActiveScreen as CombatUI;
            CombatFeedbackData feedback = null;
            try
            {
                changed = command();
                if (changed && actionId != null) feedback = CaptureFeedback(before, Session.State, actionId);
            }
            catch (Exception e) { error = "행동을 저장하지 못했습니다. " + PlayerError(e); }
            var style = Session.State.config.presentation;
            try
            {
                if (roll && changed)
                {
                    // Results and cards are already checkpointed. Only presentation is delayed.
                    ShowDiceWindow(true);
                    var popup = UI.Popups.LastOrDefault() as DiceRollUI;
                    yield return new WaitForSecondsRealtime(Mathf.Max(.65f, style.rollSeconds));
                    yield return new WaitForSecondsRealtime(popup ? Mathf.Clamp(popup.resultHoldSeconds, 0, 3) : .9f);
                    if (UI.Popups.LastOrDefault() == popup) UI.CloseTopPopup();
                }
                if (changed && selectionKey != null)
                {
                    if (Widgets != null) yield return Widgets.AnimateCardSelection(selectionKey, .22f);
                    if (feedback != null && combatView) yield return combatView.PlayFeedback(feedback);
                    else yield return new WaitForSecondsRealtime(Mathf.Max(.18f, style.actionSeconds));
                }
                Render();
                if (!roll && selectionKey == null) yield return new WaitForSecondsRealtime(style.actionSeconds);
            }
            finally
            {
                Widgets?.ResetSelectionFeedback();
                if (combatView) combatView.ResetFeedback();
                actionBusy = false; SyncInputLock();
            }
        }
        private static CombatFeedbackData CaptureFeedback(RunState before, RunState after, string id)
        {
            var card = before.cards.Single(c => c.id == id);
            var effect = CombatRules.Evaluate(before, card);
            var enemy = before.config.Enemy(before.activeEnemyId);
            var intent = enemy.intents.Single(i => i.id == before.activeIntentId);
            int incoming = CombatRules.IntentAmount(before);
            bool retaliates = after.enemyHp > 0;
            return new CombatFeedbackData
            {
                actionLabel = KoreanText.Content(before.config.Action(card.contentId).label), grade = card.grade,
                attack = effect.damage, blocked = Math.Min(before.enemyShield, effect.damage), blockGained = effect.block,
                enemyHpLost = Math.Max(0, before.enemyHp - after.enemyHp), playerHpLost = Math.Max(0, before.hp - after.hp),
                enemyHpAfter = after.enemyHp, playerHpAfter = after.hp,
                enemyMaxHp = enemy.maxHp, playerMaxHp = GrowthRules.Stats(before).maxHp,
                retaliates = retaliates, enemyDefends = intent.kind == IntentKind.Defend,
                enemyActionLabel = KoreanText.Content(intent.label), incoming = incoming,
                enemyBlockGained = retaliates && intent.kind == IntentKind.Defend ? after.enemyShield : 0,
                absorbed = retaliates && intent.kind != IntentKind.Defend ? (int)Math.Min(incoming, Math.Min(int.MaxValue, (long)before.shield + effect.block)) : 0
            };
        }
        private static string EquipmentText(EquipmentDefinition item)
        {
            var text = "체력 +" + item.maxHp + "  위력 +" + item.power + "  방어력 +" + item.guard + "\n태그: " + string.Join(", ", item.tags.Select(KoreanText.Tag));
            foreach (var modifier in item.modifiers)
                text += "\n" + KoreanText.Match(modifier.match) + " " + KoreanText.Source(modifier.source) + " [" + string.Join(", ", modifier.requiredTags.Select(KoreanText.Tag)) + "]: " + KoreanText.Value(modifier.value) + " " +
                    (modifier.bonus >= 0 ? "+" : "") + (modifier.bonus * 100).ToString("0.#") + "%";
            return text;
        }
        private static string GearSummary(RunState state)
        {
            var lines = new System.Collections.Generic.List<string> { "장비  /  " + (state.rerollUnlocked ? state.rerollCharges + "회 재굴림 가능" : "아직 재굴림을 획득하지 않았습니다") };
            for (var i = 0; i < 3; i++)
            {
                var id = state.equipmentIds[i];
                lines.Add(KoreanText.Slot((EquipmentSlot)i) + ": " + (string.IsNullOrEmpty(id) ? "비어 있음" : KoreanText.Content(state.config.Equipment(id).label) + " [" + string.Join(", ", state.config.Equipment(id).tags.Select(KoreanText.Tag)) + "]"));
            }
            return string.Join("\n", lines);
        }
        private string RewardText(RewardDefinition reward)
        {
            if (reward == null) return "받을 보상이 없습니다";
            var parts = new System.Collections.Generic.List<string>();
            if (reward.gold != 0) parts.Add(reward.gold + " 골드");
            if (reward.xp != 0) parts.Add(reward.xp + " 경험치");
            if (reward.health != 0) parts.Add("체력 " + (reward.health > 0 ? "+" : "") + reward.health);
            if (reward.rerollCharges > 0) parts.Add(reward.rerollCharges + "회 재굴림");
            if (!string.IsNullOrEmpty(reward.equipmentId)) parts.Add("장비: " + KoreanText.Content((Session?.State.config ?? PreviewConfig).Equipment(reward.equipmentId).label));
            if (!string.IsNullOrEmpty(reward.dieId)) parts.Add("주사위: " + KoreanText.Content((Session?.State.config ?? PreviewConfig).Die(reward.dieId).label));
            return parts.Count == 0 ? "아이템 보상 없음" : string.Join("  •  ", parts);
        }
        private static string PlayerError(Exception exception)
        {
            Debug.LogWarning(exception.Message);
            if (exception is System.IO.InvalidDataException)
                return "저장 파일을 읽을 수 없습니다. 손상 파일을 보관한 뒤 새 여정을 시작해 주세요.";
            if (exception is UnauthorizedAccessException || exception is System.IO.IOException)
                return "저장 공간과 파일 접근 권한을 확인한 뒤 다시 시도해 주세요.";
            return "요청을 처리하지 못했습니다. 저장 상태를 확인한 뒤 다시 시도해 주세요.";
        }
        private static string ShortGrade(Grade grade)
        {
            return KoreanText.Grade(grade);
        }
    }
}
