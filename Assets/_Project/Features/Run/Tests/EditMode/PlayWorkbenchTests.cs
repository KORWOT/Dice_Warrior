using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FateDice.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class PlayWorkbenchTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string FontPath = "Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf";
        const string ScreenFolder = "Assets/_Project/Features/Run/Prefabs/";
        static readonly WorkbenchStartPoint[] Points = (WorkbenchStartPoint[])Enum.GetValues(typeof(WorkbenchStartPoint));
        static readonly string[] GradeNames = { "일반", "고급", "희귀", "영웅", "전설" };
        static readonly string[] NodeNames = { "전투", "사건", "보물", "상점", "휴식", "대운명" };
        GameApplication prefab;
        string directory, originalConfig, defaultPath, previousStorePath, previousError;
        string[] previousWorkbenchFiles;
        byte[] originalSave;
        Dictionary<string, byte[]> sourceBytes;
        Dictionary<Object, bool> sourceDirty;

        [SetUp]
        public void SetUp()
        {
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            Assert.That(StageUtility.GetCurrentStageHandle(), Is.EqualTo(StageUtility.GetMainStageHandle()),
                "Run these Editor tests from the main stage, after closing any user-owned prefab/preview stage.");
            Assert.That(PlayWorkbenchSession.IsPending, Is.False, "An isolated Play request leaked into a normal Editor session.");
            Assert.That(GameApplication.Current, Is.Null);
            prefab = AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
            Assert.That(prefab, Is.Not.Null);
            originalConfig = EditorJsonUtility.ToJson(prefab.controller.config);
            directory = Path.Combine(Path.GetTempPath(), "FateDiceWorkbenchTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            // Observe the real file only to prove preservation; never pass this path to a store or bootstrap.
            defaultPath = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            originalSave = BytesIfPresent(defaultPath);
            previousStorePath = PlayWorkbenchSession.LastStorePath;
            previousError = PlayWorkbenchSession.LastError;
            previousWorkbenchFiles = WorkbenchFiles();
            var paths = AssetDatabase.GetDependencies(AppPath, true)
                .Where(path => path.EndsWith(".prefab", StringComparison.Ordinal) || path.EndsWith(".asset", StringComparison.Ordinal))
                .Concat(new[] { "Title", "Lobby", "InGame" }.Select(name => "Assets/_Project/Scenes/" + name + ".unity"))
                .Distinct().ToArray();
            sourceBytes = paths.SelectMany(path => new[] { path, path + ".meta" })
                .Where(File.Exists).ToDictionary(path => path, File.ReadAllBytes);
            sourceDirty = paths.Select(AssetDatabase.LoadMainAssetAtPath).Where(asset => asset)
                .Distinct().ToDictionary(asset => asset, EditorUtility.IsDirty);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (StageUtility.GetCurrentStage() is UIWorkbenchPreview) StageUtility.GoToMainStage();
                if (prefab)
                {
                    Assert.That(EditorJsonUtility.ToJson(prefab.controller.config), Is.EqualTo(originalConfig));
                    Assert.That(PlayWorkbenchSession.IsPending, Is.False);
                    Assert.That(PlayWorkbenchSession.LastStorePath, Is.EqualTo(previousStorePath));
                    Assert.That(PlayWorkbenchSession.LastError, Is.EqualTo(previousError));
                    Assert.That(WorkbenchFiles(), Is.EqualTo(previousWorkbenchFiles), "Build/preview must not create a Play save/request.");
                    Assert.That(BytesIfPresent(defaultPath), Is.EqualTo(originalSave), "The user's default save changed.");
                    Assert.That(GameApplication.Current, Is.Null, "A preview created a persistent gameplay owner.");
                    foreach (var source in sourceBytes)
                        Assert.That(BytesIfPresent(source.Key), Is.EqualTo(source.Value), "Authored source changed: " + source.Key);
                    foreach (var source in sourceDirty)
                        Assert.That(EditorUtility.IsDirty(source.Key), Is.EqualTo(source.Value), "Authored asset became dirty: " + source.Key.name);
                }
            }
            finally { if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        WorkbenchOptions Options(WorkbenchStartPoint point, uint seed = 33) => new WorkbenchOptions
        {
            application = prefab, seed = seed, trialId = prefab.controller.config.data.combat.trialActionIds[0],
            cap = Grade.Legendary, startPoint = point
        };

        [Test]
        public void EveryPresetIsAValidResumablePhaseWithoutChangingTheConfigAsset()
        {
            foreach (var point in Points)
            {
                var options = Options(point);
                var state = PlayWorkbenchSession.Build(options);
                Assert.That(state.phase, Is.EqualTo(ExpectedPhase(point)), point.ToString());
                Assert.That(state.initialSeed, Is.EqualTo(options.seed));
                Assert.That(state.rngState, Is.Not.Zero);
                Assert.That(state.explorationCap, Is.EqualTo(options.cap));
                Assert.That(state.actionIds, Does.Contain(options.trialId));
                Assert.That(state.config.Validate(), Is.Empty);
                Assert.That(state.config, Is.Not.SameAs(prefab.controller.config.data));
                Assert.That(state.playedSeconds, Is.Zero);
                string json = JsonUtility.ToJson(state);
                var store = new LocalRunStore(Path.Combine(directory, point + ".json"));
                Assert.DoesNotThrow(() => store.Save(state), "The production save validator rejected " + point);
                var loaded = store.Load();
                Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(json), point + " lost choices/config/RNG on resume.");
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(json), "Validation mutated " + point);
                Assert.That(EditorJsonUtility.ToJson(prefab.controller.config), Is.EqualTo(originalConfig));
            }
        }

        [Test]
        public void RebuildingTheSameSeedPreservesEveryChoiceAndOwnsIndependentSnapshots()
        {
            foreach (var point in Points)
            {
                var options = Options(point, 987654);
                var first = PlayWorkbenchSession.Build(options);
                var second = PlayWorkbenchSession.Build(options);
                Assert.That(Normalized(first), Is.EqualTo(Normalized(second)), point + " consumed RNG or chose content differently.");
                Assert.That(first.config, Is.Not.SameAs(second.config));
                Assert.That(first.config.world, Is.Not.SameAs(second.config.world));
                string secondBefore = JsonUtility.ToJson(second);
                first.config.growth.characterName = "Only this disposable snapshot";
                first.config.combat.actions[0].label = "Only this disposable action";
                Assert.That(JsonUtility.ToJson(second), Is.EqualTo(secondBefore));
                Assert.That(EditorJsonUtility.ToJson(prefab.controller.config), Is.EqualTo(originalConfig));
            }
        }

        [Test]
        public void InvalidOptionsCannotQueuePlayOrCreateAStore()
        {
            Assert.Throws<ArgumentNullException>(() => PlayWorkbenchSession.Build(null));
            var options = Options(WorkbenchStartPoint.Combat);
            options.application = null;
            Assert.Throws<ArgumentException>(() => PlayWorkbenchSession.Build(options));
            options = Options(WorkbenchStartPoint.Combat, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayWorkbenchSession.Build(options));
            options = Options(WorkbenchStartPoint.Combat);
            options.trialId = "__missing_workbench_trial__";
            Assert.Throws<ArgumentException>(() => PlayWorkbenchSession.Build(options));
            options.trialId = prefab.controller.config.data.combat.actions
                .Select(action => action.id).Except(prefab.controller.config.data.combat.trialActionIds).First();
            Assert.Throws<ArgumentException>(() => PlayWorkbenchSession.Build(options), "An existing non-trial action is still an invalid trial.");
            options = Options(WorkbenchStartPoint.Combat);
            options.cap = (Grade)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayWorkbenchSession.Build(options));
            options = Options((WorkbenchStartPoint)99);
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayWorkbenchSession.Build(options));
            Assert.That(PlayWorkbenchSession.IsPending, Is.False);
            Assert.That(WorkbenchFiles(), Is.EqualTo(previousWorkbenchFiles));
        }

        [UnityTest]
        public IEnumerator EveryScreenRendersItsAuthoredKoreanTextInBothPortraitPreviews()
        {
            var seen = new HashSet<Type>();
            foreach (int height in new[] { 1280, 1600 })
            foreach (var point in Points)
            {
                var preview = UIWorkbenchPreview.Open(Options(point), height);
                yield return Settle();
                var manager = AssertPreview(preview, point, height);
                seen.Add(manager.ActiveScreen.GetType());
                AssertRenderedText(manager.ActiveScreen, manager.Root.canvas);
                var root = preview.PreviewRoot;
                var scene = root.scene;
                StageUtility.GoToMainStage();
                StageUtility.GoToMainStage();
                Assert.That(root == null, Is.True, "Closing the stage left its cloned UI alive.");
                Assert.That(scene.IsValid(), Is.False, "Closing the stage left its preview scene loaded.");
            }
            Assert.That(seen.Count, Is.EqualTo(8), "All eight concrete authored screens must be represented.");
        }

        [UnityTest]
        public IEnumerator OpeningAnotherPreviewDisposesThePreviousRootAndClosingTwiceIsSafe()
        {
            var first = UIWorkbenchPreview.Open(Options(WorkbenchStartPoint.Title), 1280);
            yield return Settle();
            var oldRoot = first.PreviewRoot;
            var oldScene = oldRoot.scene;
            var second = UIWorkbenchPreview.Open(Options(WorkbenchStartPoint.Combat), 1600);
            yield return Settle();
            Assert.That(oldRoot == null, Is.True, "Refresh retained the first bound UI hierarchy.");
            Assert.That(oldScene.IsValid(), Is.False);
            var manager = AssertPreview(second, WorkbenchStartPoint.Combat, 1600);
            Assert.That(manager.CachedCount, Is.EqualTo(1), "A preview should bind just the selected screen.");
            var root = second.PreviewRoot;
            var scene = root.scene;
            StageUtility.GoToMainStage();
            Assert.DoesNotThrow(StageUtility.GoToMainStage);
            Assert.That(root == null, Is.True);
            Assert.That(scene.IsValid(), Is.False);
            Assert.That(StageUtility.GetCurrentStageHandle(), Is.EqualTo(StageUtility.GetMainStageHandle()));
        }

        [UnityTest]
        public IEnumerator CombatAndExplorationPreviewsDisplayRuleResultsWithoutInventingChoices()
        {
            var options = Options(WorkbenchStartPoint.Combat, 2468);
            var expected = PlayWorkbenchSession.Build(options);
            string unchanged = JsonUtility.ToJson(expected);
            var preview = UIWorkbenchPreview.Open(options, 1280);
            yield return Settle();
            var combat = (CombatUI)AssertPreview(preview, WorkbenchStartPoint.Combat, 1280).ActiveScreen;
            var enemy = expected.config.Enemy(expected.activeEnemyId);
            var stats = GrowthRules.Stats(expected);
            Assert.That(combat.enemyHealth.text, Is.EqualTo(expected.enemyHp + " / " + enemy.maxHp));
            Assert.That(combat.layout.stats.text, Is.EqualTo("체력 " + expected.hp + "/" + stats.maxHp));
            Assert.That(combat.intentValue.text, Is.EqualTo(CombatRules.IntentAmount(expected).ToString()));
            Assert.That(combat.enemyHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)expected.enemyHp / enemy.maxHp).Within(.0001f));
            Assert.That(combat.playerHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)expected.hp / stats.maxHp).Within(.0001f));
            var actions = combat.actionChoices.GetComponentsInChildren<ActionCardView>();
            Assert.That(actions.Length, Is.EqualTo(expected.cards.Count));
            Assert.That(actions.Select(card => card.OfferedId).Distinct().Count(), Is.EqualTo(actions.Length));
            for (int i = 0; i < actions.Length; i++)
            {
                var offered = expected.cards[i];
                var effect = CombatRules.Evaluate(expected, offered);
                Assert.That(actions[i].OriginalId, Is.EqualTo(offered.contentId));
                Assert.That(actions[i].gradeLabel.text, Is.EqualTo(GradeNames[(int)offered.grade]));
                Assert.That(actions[i].effectLabel.text, Is.EqualTo("피해 " + effect.damage + " / 수호 " + effect.block));
            }
            Assert.That(JsonUtility.ToJson(expected), Is.EqualTo(unchanged));
            options = Options(WorkbenchStartPoint.ExplorationCards, 2468);
            expected = PlayWorkbenchSession.Build(options);
            preview = UIWorkbenchPreview.Open(options, 1600);
            yield return Settle();
            var exploration = (ExplorationUI)AssertPreview(preview, WorkbenchStartPoint.ExplorationCards, 1600).ActiveScreen;
            var fates = exploration.fateChoices.GetComponentsInChildren<FateCardView>();
            Assert.That(fates.Length, Is.EqualTo(expected.cards.Count));
            for (int i = 0; i < fates.Length; i++)
                Assert.That(fates[i].frame.label.text,
                    Is.EqualTo(NodeNames[(int)expected.cards[i].type] + "  /  " + GradeNames[(int)expected.cards[i].grade]));
        }

        static UIManager AssertPreview(UIWorkbenchPreview preview, WorkbenchStartPoint point, int height)
        {
            Assert.That(preview, Is.Not.Null);
            Assert.That(StageUtility.GetCurrentStage(), Is.SameAs(preview));
            var root = preview.PreviewRoot;
            Assert.That(root, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(root), Is.False);
            Assert.That(EditorSceneManager.IsPreviewScene(root.scene), Is.True);
            var manager = root.GetComponent<UIManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.Root.canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            var canvasRect = manager.Root.canvas.GetComponent<RectTransform>();
            Assert.That(canvasRect.rect.width, Is.EqualTo(720).Within(.1f));
            Assert.That(canvasRect.rect.height, Is.EqualTo(height).Within(.1f));
            Assert.That(manager.ActiveScreen, Is.TypeOf(ExpectedScreen(point)));
            Assert.That(manager.ActiveScreen.IsOpen && manager.ActiveScreen.gameObject.activeInHierarchy, Is.True);
            Assert.That(preview.SourcePrefabPath, Is.EqualTo(ScreenFolder + ExpectedScreen(point).Name + ".prefab"));
            Assert.That(root.GetComponentsInChildren<BaseUI>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<GameApplication>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<RunUIController>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(manager.Root.inputGroup.interactable, Is.False);
            Assert.That(manager.Root.inputGroup.blocksRaycasts, Is.False);
            foreach (var selectable in root.GetComponentsInChildren<Selectable>())
                Assert.That(selectable.IsInteractable(), Is.False, "Preview control accepts gameplay input: " + selectable.name);
            return manager;
        }

        static void AssertRenderedText(BaseUI screen, Canvas canvas)
        {
            Canvas.ForceUpdateCanvases();
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(font, Is.Not.Null);
            var texts = screen.GetComponentsInChildren<Text>().Where(text => text.isActiveAndEnabled && !string.IsNullOrWhiteSpace(text.text)).ToArray();
            Assert.That(texts.Length, Is.GreaterThan(1));
            string hangul = new string(string.Concat(texts.Select(text => text.text)).Where(ch => ch >= '가' && ch <= '힣').Distinct().ToArray());
            Assert.That(hangul, Is.Not.Empty, screen.GetType().Name + " has no Korean content.");
            font.RequestCharactersInTexture(hangul, 25, FontStyle.Normal);
            foreach (char glyph in hangul) Assert.That(font.HasCharacter(glyph), Is.True, "Missing Hangul glyph: " + glyph);
            int visible = 0;
            foreach (var text in texts)
            {
                Assert.That(text.font, Is.SameAs(font), text.name);
                if (!FullyVisible(text.rectTransform, canvas.GetComponent<RectTransform>())) continue;
                visible++;
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + .1f), "Truncated preview text: " + text.text);
                var mesh = text.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null, "Missing text mesh: " + text.text);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), "Invisible preview glyphs: " + text.text);
            }
            Assert.That(visible, Is.GreaterThan(1), "The canvas has no inspectable text.");
        }

        static bool FullyVisible(RectTransform text, RectTransform canvas)
        {
            if (!Contains(canvas, text)) return false;
            foreach (var mask in text.GetComponentsInParent<RectMask2D>())
                if (mask.isActiveAndEnabled && !Contains(mask.rectTransform, text)) return false;
            return true;
        }
        static bool Contains(RectTransform outer, RectTransform inner)
        {
            var corners = new Vector3[4];
            inner.GetWorldCorners(corners);
            var bounds = outer.rect;
            foreach (var corner in corners)
            {
                Vector3 local = outer.InverseTransformPoint(corner);
                if (local.x < bounds.xMin - .1f || local.x > bounds.xMax + .1f ||
                    local.y < bounds.yMin - .1f || local.y > bounds.yMax + .1f) return false;
            }
            return true;
        }
        static IEnumerator Settle()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }
        static byte[] BytesIfPresent(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;
        static string[] WorkbenchFiles() => Directory.Exists("Library/FateDiceWorkbench")
            ? Directory.GetFiles("Library/FateDiceWorkbench", "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();
        static string Normalized(RunState state) => JsonUtility.ToJson(state).Replace(state.runId, "<run-id>");
        static RunPhase ExpectedPhase(WorkbenchStartPoint point)
        {
            switch (point)
            {
                case WorkbenchStartPoint.ExplorationCards: return RunPhase.ExplorationCards;
                case WorkbenchStartPoint.Combat: return RunPhase.CombatCards;
                case WorkbenchStartPoint.Shop: return RunPhase.Shop;
                case WorkbenchStartPoint.Reward: return RunPhase.Reward;
                case WorkbenchStartPoint.Equipment: return RunPhase.EquipmentChoice;
                case WorkbenchStartPoint.Result: return RunPhase.Result;
                default: return RunPhase.Map;
            }
        }
        static Type ExpectedScreen(WorkbenchStartPoint point)
        {
            switch (point)
            {
                case WorkbenchStartPoint.Title: return typeof(TitleUI);
                case WorkbenchStartPoint.Lobby: return typeof(MenuUI);
                case WorkbenchStartPoint.Combat: return typeof(CombatUI);
                case WorkbenchStartPoint.Shop: return typeof(EncounterUI);
                case WorkbenchStartPoint.Reward: return typeof(RewardUI);
                case WorkbenchStartPoint.Equipment: return typeof(EquipmentUI);
                case WorkbenchStartPoint.Result: return typeof(ResultUI);
                default: return typeof(ExplorationUI);
            }
        }
    }
}
