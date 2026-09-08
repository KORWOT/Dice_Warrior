using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // Explicit migration of existing UI assets; display strings and font references only.
    public static class KoreanUiAuthoring
    {
        public const string FontPath = "Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf";
        private const string CatalogPath = "Assets/_Project/Features/Run/Configs/DefaultFateDiceVisuals.asset";
        private const string LegacyScenePath = "Assets/_Project/Scenes/FateDicePrototype.unity";
        private static readonly string[] PrefabPaths =
        {
            // The base must be saved before any variant or nested use is loaded.
            "Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab",
            "Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab",
            "Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab",
            "Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab",
            "Assets/_Project/Features/Combat/Prefabs/CombatDie.prefab",
            "Assets/_Project/Features/Run/Prefabs/TitleUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/MenuUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/EncounterUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/RewardUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/EquipmentUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/ResultUI.prefab",
            "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab",
            "Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"
        };

        [MenuItem("Fate Dice/Apply Korean UI")]
        public static string Apply()
        {
            GuardEditor();
            foreach (var path in PrefabPaths)
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                    throw new InvalidOperationException("Required UI prefab is missing: " + path);
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyScenePath))
                throw new InvalidOperationException("The legacy prototype scene is required.");
            var configAsset = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            var catalog = AssetDatabase.LoadAssetAtPath<FateDiceVisualCatalog>(CatalogPath);
            if (!configAsset || !catalog)
                throw new InvalidOperationException("The existing config and visual catalog are required.");
            if (EditorUtility.IsDirty(catalog))
                throw new InvalidOperationException("Save or discard pending visual catalog edits before applying Korean UI.");
            var config = configAsset.Snapshot();
            catalog.Validate(config);
            var importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer == null)
                throw new InvalidOperationException("Import the approved Pretendard-Regular.ttf before applying Korean UI.");

            var counts = new ChangeCounts();
            if (importer.fontTextureCase != FontTextureCase.Dynamic || !importer.includeFontData)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.includeFontData = true;
                importer.SaveAndReimport();
                counts.fontImport = 1;
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!font) throw new InvalidOperationException("The approved font could not be loaded after import.");

            foreach (var path in PrefabPaths) UpdatePrefab(path, font, counts);
            UpdateCatalog(catalog, config, counts);
            UpdateLegacyFont(font, counts);
            return "Korean UI applied: prefab saves=" + counts.prefabSaves +
                ", Text fonts=" + counts.textFonts + ", controller fonts=" + counts.controllerFonts +
                ", fixed texts=" + counts.fixedTexts + ", text sizes=" + counts.textSizes + ", fallback glyphs=" + counts.glyphs +
                ", scene saves=" + counts.sceneSaves + ", font reimports=" + counts.fontImport + ".";
        }

        private sealed class ChangeCounts
        {
            public int prefabSaves, textFonts, controllerFonts, fixedTexts, textSizes, glyphs, sceneSaves, fontImport;
        }

        private static void GuardEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying Korean UI.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before applying Korean UI.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    throw new InvalidOperationException("Save all open scenes before applying Korean UI.");
                // Additive opening cannot coexist with an untitled unsaved scene.
                if (scene.isLoaded && string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("Save or close the untitled scene before applying Korean UI.");
            }
        }

        private static void UpdatePrefab(string path, Font font, ChangeCounts counts)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var text in contents.GetComponentsInChildren<Text>(true))
                {
                    // Freshly loaded inherited Common fonts already match; do not create an override.
                    if (text.font == font) continue;
                    text.font = font;
                    counts.textFonts++;
                    changed = true;
                }
                foreach (var controller in contents.GetComponentsInChildren<RunUIController>(true))
                {
                    if (controller.uiFont == font) continue;
                    controller.uiFont = font;
                    counts.controllerFonts++;
                    changed = true;
                }
                var menu = contents.GetComponent<MenuUI>();
                if (menu)
                {
                    changed |= Replace(menu.trialHeading, "TRIAL WILDCARD", "와일드 카드", counts);
                    changed |= Replace(menu.trialHeading, "시련 카드", "와일드 카드", counts);
                    changed |= Replace(menu.capHeading, "EXPLORATION GRADE CAP (combat is independent)",
                        "탐험 등급 상한 (전투 제외)", counts);
                    changed |= Replace(menu.seedHeading, "SEED (nonzero number, for repeatable runs)",
                        "시드 (같은 숫자로 같은 여정 재현)", counts);
                    // Current MenuUI has no placeholder. Translate only an existing default Text.
                    var placeholder = menu.seedInput ? menu.seedInput.placeholder as Text : null;
                    if (placeholder)
                    {
                        changed |= Replace(placeholder, "Enter text...", "시드 입력", counts);
                        changed |= Replace(placeholder, "Enter seed", "시드 입력", counts);
                    }
                }
                var title = contents.GetComponent<TitleUI>();
                if (title)
                {
                    changed |= Replace(title.title, "FATE DICE", "운명의 주사위", counts);
                    changed |= Replace(title.subtitle, "Choose your path. Shape your fate.",
                        "길을 선택하고 운명을 바꾸세요.", counts);
                    changed |= Replace(title.status, "Ready", "준비 완료", counts);
                    if (title.enterButton)
                        changed |= Replace(title.enterButton.label, "ENTER LOBBY", "로비 입장", counts);
                }
                var combat = contents.GetComponent<CombatUI>();
                if (combat && combat.menuButton)
                    changed |= Replace(combat.menuButton.label, "MENU", "메뉴", counts);
                // Pretendard at the old size needs 23px in the authored 22px HP value area.
                // Truncate drops the whole line, so fit that existing default without changing the bar.
                if (combat && combat.layout && combat.layout.stats && combat.layout.stats.fontSize == 19)
                {
                    combat.layout.stats.fontSize = 18;
                    counts.textSizes++;
                    changed = true;
                }

                if (!changed) return;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, path))
                    throw new InvalidOperationException("Could not save migrated UI prefab: " + path);
                counts.prefabSaves++;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static bool Replace(Text field, string original, string translated, ChangeCounts counts)
        {
            if (!field || !string.Equals(field.text, original, StringComparison.Ordinal) ||
                string.Equals(original, translated, StringComparison.Ordinal)) return false;
            field.text = translated;
            counts.fixedTexts++;
            return true;
        }

        private static void UpdateCatalog(FateDiceVisualCatalog catalog, GameConfigData config, ChangeCounts counts)
        {
            var actions = config.combat.actions.ToDictionary(x => x.id, StringComparer.Ordinal);
            var events = config.world.events.ToDictionary(x => x.id, StringComparer.Ordinal);
            int changed = 0;
            foreach (var row in catalog.actions)
            {
                var label = actions[row.contentId].label;
                var translated = KoreanText.Content(label);
                // Exact Content lookup passes custom labels through; their glyph remains untouched.
                if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(translated) ||
                    string.Equals(label, translated, StringComparison.Ordinal)) continue;
                if (ReplaceGlyph(row.visual, label.Substring(0, 1), translated.Substring(0, 1))) changed++;
            }
            foreach (var row in catalog.events)
            {
                var type = events[row.contentId].type;
                if (ReplaceGlyph(row.visual, type.ToString().Substring(0, 1), Initial(KoreanText.Node(type)))) changed++;
            }
            foreach (var row in catalog.nodes)
                if (ReplaceGlyph(row.visual, OriginalNodeGlyph(row.type), Initial(KoreanText.Node(row.type)))) changed++;
            foreach (var row in catalog.fates)
                if (ReplaceGlyph(row.visual, OriginalNodeGlyph(row.type), Initial(KoreanText.Node(row.type)))) changed++;
            // defaultAction/defaultEvent '?' remains a neutral unknown symbol. Custom glyphs/images/colors are untouched.
            if (changed == 0) return;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            counts.glyphs += changed;
        }

        private static bool ReplaceGlyph(VisualArtwork visual, string original, string translated)
        {
            if (visual == null || !string.Equals(visual.fallbackGlyph, original, StringComparison.Ordinal) ||
                string.Equals(original, translated, StringComparison.Ordinal)) return false;
            visual.fallbackGlyph = translated;
            return true;
        }

        private static string Initial(string value) => string.IsNullOrEmpty(value) ? value : value.Substring(0, 1);

        private static string OriginalNodeGlyph(NodeType type)
        {
            switch (type)
            {
                case NodeType.Combat: return "X";
                case NodeType.Event: return "?";
                case NodeType.Treasure: return "$";
                case NodeType.Shop: return "+";
                case NodeType.Rest: return "Z";
                default: return "*";
            }
        }

        private static void UpdateLegacyFont(Font font, ChangeCounts counts)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(LegacyScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Additive);
                var controllers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<RunUIController>(true)).ToArray();
                if (controllers.Length != 1)
                    throw new InvalidOperationException("The legacy prototype must contain exactly one RunUIController.");
                var controller = controllers[0];
                if (controller.uiFont == font) return;
                controller.uiFont = font;
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save the legacy prototype font reference.");
                counts.controllerFonts++;
                counts.sceneSaves++;
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (!SameSetup(setup, EditorSceneManager.GetSceneManagerSetup()))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static bool SameSetup(SceneSetup[] first, SceneSetup[] second)
        {
            if (first.Length != second.Length) return false;
            for (var i = 0; i < first.Length; i++)
                if (first[i].path != second[i].path || first[i].isLoaded != second[i].isLoaded ||
                    first[i].isActive != second[i].isActive) return false;
            return true;
        }
    }
}
