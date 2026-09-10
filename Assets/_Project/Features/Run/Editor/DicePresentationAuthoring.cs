using System;
using System.Linq;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FateDice.Editor
{
    public static class DicePresentationAuthoring
    {
        const string DicePath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        const string CatalogPath = "Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset";
        const string MaterialPath = "Assets/_Project/Features/Run/Materials/ComboRibbon.mat";
        const string FontPath = "Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Effects SDF.asset";

        [MenuItem("Fate Dice/등급별 연출 시간 원본 적용")]
        public static string ApplyTimelines()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("플레이와 미리보기를 닫고 가져오기가 끝난 뒤 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 먼저 저장하세요.");
            var catalog = AssetDatabase.LoadAssetAtPath<DiceFeedbackCatalog>(CatalogPath);
            if (!catalog) throw new InvalidOperationException("조합 연출 설정 원본이 필요합니다.");
            if (catalog.timelineVersion >= 1) return "등급별 연출 시간은 이미 적용되어 있습니다. 사용자 편집을 유지합니다.";
            if (catalog.tiers == null || catalog.tiers.Length == 0)
                catalog.tiers = new[]
                {
                    Timeline("일반 · 짧은 점등", .223f, DiceEffectComplexity.Simple, .16f, 0, .35f, .12f),
                    Timeline("중급 · 룬 점등", .556f, DiceEffectComplexity.Runic, .32f, .28f, .55f, .18f),
                    Timeline("상급 · 회전과 파동", .889f, DiceEffectComplexity.Grand, .5f, .65f, .8f, .25f),
                    Timeline("최상급 · 다중 파동", 1, DiceEffectComplexity.Legendary, .7f, 1.3f, 1.1f, .4f)
                };
            catalog.ResolveTimeline(1);
            // Save only the explicit originals, preserving every existing pose, color and FEEL track.
            foreach (string path in new[] { DicePath, "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab",
                "Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab", "Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab",
                "Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Save(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            catalog.timelineVersion = 1;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return "등급별 등장·강조·읽기·퇴장 시간과 원본 필드 적용 완료.";
        }

        static DiceEffectTimeline Timeline(string label, float threshold, DiceEffectComplexity complexity,
            float entrance, float flourish, float read, float exit) => new DiceEffectTimeline
        { label = label, maximumStrength = threshold, complexity = complexity, entranceSeconds = entrance,
            flourishSeconds = flourish, readSeconds = read, exitSeconds = exit };

        [MenuItem("Fate Dice/주사위 오라 연출 원본 적용")]
        public static string ApplyAuras()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("플레이와 미리보기를 닫고 가져오기가 끝난 뒤 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 먼저 저장하세요.");
            var catalog = AssetDatabase.LoadAssetAtPath<DiceFeedbackCatalog>(CatalogPath);
            if (!catalog) throw new InvalidOperationException("기존 조합 연출 원본이 필요합니다.");
            if (catalog.auraVersion < 1)
            {
                // Replace only untouched v1 defaults; keep any colors edited since the first migration.
                string[] previous = { "C8D0DD", "8DD6A3", "6BAEFF", "62E2D2", "B599FF", "E591F1", "F2D685", "FFA65E", "FF797C", "FFF0B5" };
                string[] colors = { "9AC8BD", "3D9DFF", "58E8CC", "FFD064", "C590FF", "EE83E0", "77D6FF", "FFE39A", "FF8D47", "FF5530" };
                string[] accents = { "D3EFE6", "8CDEFF", "A1FFDE", "C572FF", "F2BCFF", "FFC5F5", "E1F4FF", "FFFFFF", "FFE3A2", "FFD76A" };
                foreach (var style in catalog.styles)
                {
                    int i = (int)style.hand;
                    ColorUtility.TryParseHtmlString("#" + previous[i], out var old);
                    ColorUtility.TryParseHtmlString("#" + colors[i], out var next);
                    ColorUtility.TryParseHtmlString("#" + accents[i], out var accent);
                    style.color = AuraDefaultColor(style.color, old, next);
                    style.accentColor = accent;
                    style.alternateGroups = style.hand == HandKind.ThreePairs || style.hand == HandKind.FullHouse;
                }
                catalog.auraVersion = 1;
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
            var root = PrefabUtility.LoadPrefabContents(DicePath);
            try
            {
                var view = root.GetComponent<DiceRollUI>();
                if (view.presentationVersion >= 4)
                {
                    bool repaired = false;
                    foreach (var aura in view.GetComponentsInChildren<DiceAuraGraphic>(true))
                        if (!aura.GetComponent<CanvasRenderer>()) { aura.gameObject.AddComponent<CanvasRenderer>(); repaired = true; }
                    if (repaired) Save(root, DicePath);
                    return repaired ? "누락된 오라 CanvasRenderer 연결 복구 완료." : "오라 원본은 이미 적용되어 있습니다. 사용자 편집을 유지합니다.";
                }
                var feedback = view.resultFeedback;
                if (!feedback) throw new InvalidOperationException("기존 조합 배너가 필요합니다.");
                if (view.presentationVersion >= 2)
                {
                    FinishAuraAppearance(view);
                    view.presentationVersion = 4;
                    Save(root, DicePath);
                    return "오라 원본의 렌더러 직렬화와 투명 배경 적용 완료.";
                }
                var panel = (RectTransform)view.title.transform.parent;
                panel.sizeDelta = new Vector2(640, 750);
                var panelImage = panel.GetComponent<Image>();
                if (panelImage) panelImage.color = new Color(.012f, .018f, .028f, .94f);
                Place(view.title.rectTransform, new Vector2(0, 315), new Vector2(570, 56));
                Place(view.detail.rectTransform, new Vector2(0, 252), new Vector2(564, 64));
                var row = (RectTransform)view.dice[0].transform.parent;
                Place(row, new Vector2(0, -75), new Vector2(600, 180));
                Place((RectTransform)feedback.transform, new Vector2(0, 115), new Vector2(580, 105));
                Place(feedback.comboName.rectTransform, Vector2.zero, new Vector2(568, 100));
                feedback.comboName.fontSize = 62; feedback.comboName.fontSizeMax = 62; feedback.comboName.fontSizeMin = 34;
                Place(feedback.ribbon.rectTransform, new Vector2(0, -58), new Vector2(440, 2));
                Place(view.result.rectTransform, new Vector2(0, -212), new Vector2(558, 72));
                view.result.fontSize = 23;
                Place((RectTransform)view.rollButton.transform, new Vector2(0, -306), new Vector2(520, 80));
                var crest = Rect("조합 빛 문양", panel, new Vector2(0, 5), new Vector2(620, 500)).gameObject.AddComponent<DiceAuraGraphic>();
                crest.shape = DiceAuraGraphic.AuraShape.Crest; crest.raycastTarget = false;
                crest.transform.SetAsFirstSibling();
                feedback.crest = crest;
                feedback.dieAuras = new DiceAuraGraphic[6];
                for (int i = 0; i < 6; i++)
                {
                    Place(view.dice[i].rectTransform, new Vector2((i - 2.5f) * 93, 0), new Vector2(80, 80));
                    var aura = Rect("주사위 " + (i + 1) + " 오라", row, view.dice[i].rectTransform.anchoredPosition,
                        new Vector2(180, 180)).gameObject.AddComponent<DiceAuraGraphic>();
                    aura.raycastTarget = false; aura.variation = i;
                    aura.transform.SetAsFirstSibling();
                    feedback.dieAuras[i] = aura;
                }
                FinishAuraAppearance(view);
                view.presentationVersion = 4;
                Save(root, DicePath);
                return "주사위 오라·중앙 문양·조합 이름 배치 적용 완료 (버전 4).";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Color AuraDefaultColor(Color authored, Color previous, Color next)
        {
            return Mathf.Abs(authored.r - previous.r) + Mathf.Abs(authored.g - previous.g) +
                Mathf.Abs(authored.b - previous.b) + Mathf.Abs(authored.a - previous.a) < .001f ? next : authored;
        }

        static void FinishAuraAppearance(DiceRollUI view)
        {
            var panel = view.title.transform.parent;
            var background = panel.GetComponent<Image>();
            if (background) background.color = Color.clear;
            var outline = panel.GetComponent<Outline>();
            if (outline) outline.enabled = false;
            var overlay = view.GetComponent<Image>();
            if (overlay) overlay.color = new Color(.004f, .008f, .016f, .93f);
            Place(view.result.rectTransform, new Vector2(0, -238), new Vector2(558, 72));
            foreach (var die in view.dice)
            {
                die.faceColor = new Color(.64f, .70f, .78f);
                die.edgeColor = new Color(.91f, .96f, 1);
                die.pipColor = new Color(.018f, .028f, .045f);
            }
            // Import can synthesize RequireComponent in memory. The caller must save once.
            foreach (var aura in view.GetComponentsInChildren<DiceAuraGraphic>(true))
                if (!aura.GetComponent<CanvasRenderer>()) aura.gameObject.AddComponent<CanvasRenderer>();
        }

        [MenuItem("Fate Dice/주사위 조합 연출 원본 적용")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("플레이와 가져오기가 끝난 뒤 적용하세요.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("미리보기와 프리팹 편집을 먼저 닫으세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 먼저 저장하세요.");
            var catalog = CreateCatalog();
            var font = CreateFont();
            var material = CreateRibbon();
            int changed = AuthorDice(catalog, font, material) ? 1 : 0;
            if (AuthorCard("Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab")) changed++;
            if (AuthorCard("Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab")) changed++;
            AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(font);
            AssetDatabase.SaveAssetIfDirty(material);
            return "조합 연출 적용: " + changed + "개 원본. 기존 적용본의 사용자 편집은 보존했습니다.";
        }

        static DiceFeedbackCatalog CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DiceFeedbackCatalog>(CatalogPath);
            if (catalog) return catalog;
            catalog = ScriptableObject.CreateInstance<DiceFeedbackCatalog>();
            string[] colors = { "C8D0DD", "8DD6A3", "6BAEFF", "62E2D2", "B599FF", "E591F1", "F2D685", "FFA65E", "FF797C", "FFF0B5" };
            var kinds = (HandKind[])Enum.GetValues(typeof(HandKind));
            catalog.styles = kinds.Select((hand, i) => {
                ColorUtility.TryParseHtmlString("#" + colors[i], out var color);
                return new DiceHandFeedbackStyle { hand = hand, color = color, textEffect = i < 4 ? "wave" : "bounce", punchScale = .07f + i * .008f };
            }).ToArray();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        static TMP_FontAsset CreateFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf");
            if (!source) throw new InvalidOperationException("기존 Pretendard 한글 글꼴이 필요합니다.");
            var font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Pretendard-Effects SDF";
            font.atlasTextures[0].name = "Pretendard-Effects Atlas";
            font.material.name = "Pretendard-Effects Material";
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            var rules = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            string glyphs = "검증복원조합 다음굴림 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz·!?" + string.Concat(rules.data.dice.hands.Select(hand => KoreanText.Content(hand.label)));
            font.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("한글 글꼴에 없는 문자: " + missing);
            EditorUtility.SetDirty(font);
            return font;
        }

        static Material CreateRibbon()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material) return material;
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShaderUiMask");
            if (!shader) throw new InvalidOperationException("All In 1 UI Mask shader가 필요합니다.");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Features/Run/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project/Features/Run", "Materials");
            material = new Material(shader) { name = "ComboRibbon" };
            material.EnableKeyword("SHINE_ON");
            material.SetFloat("_ShineLocation", 1);
            material.SetFloat("_ShineWidth", .16f);
            material.SetFloat("_ShineGlow", .7f);
            material.SetFloat("_ShineRotate", .15f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        static bool AuthorDice(DiceFeedbackCatalog catalog, TMP_FontAsset font, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(DicePath);
            try
            {
                var view = root.GetComponent<DiceRollUI>();
                if (view.presentationVersion >= 1) return false;
                var panel = (RectTransform)view.title.transform.parent;
                panel.sizeDelta = new Vector2(620, 640);
                Place(view.title.rectTransform, new Vector2(0, 257), new Vector2(560, 60));
                Place(view.detail.rectTransform, new Vector2(0, 188), new Vector2(552, 82));
                var row = (RectTransform)view.dice[0].transform.parent;
                Place(row, new Vector2(0, 70), new Vector2(566, 118));
                for (int i = 0; i < 6; i++)
                    Place(view.dice[i].rectTransform, new Vector2((i - 2.5f) * 93, 0), new Vector2(80, 80));
                Place(view.result.rectTransform, new Vector2(0, -146), new Vector2(550, 82));
                view.result.fontSize = 23;
                Place((RectTransform)view.rollButton.transform, new Vector2(0, -250), new Vector2(538, 82));
                var banner = Rect("조합 연출", panel, new Vector2(0, -51), new Vector2(548, 90));
                var feedback = banner.gameObject.AddComponent<DiceResultFeedback>();
                var ribbon = Rect("조합 색상 띠", banner, Vector2.zero, new Vector2(548, 82)).gameObject.AddComponent<Image>();
                ribbon.color = new Color(1, 1, 1, 0); ribbon.raycastTarget = false; ribbon.material = material;
                var text = Rect("조합 이름", banner, Vector2.zero, new Vector2(518, 76)).gameObject.AddComponent<TextMeshProUGUI>();
                text.font = font; text.fontSize = 46; text.fontStyle = FontStyles.Bold;
                text.enableAutoSizing = true; text.fontSizeMin = 30; text.fontSizeMax = 46;
                text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.text = "";
                var animator = text.gameObject.AddComponent<TextAnimator_TMP>();
                animator.sharedSettings = null;
                animator.localSettings.timeScale = Febucci.TextAnimatorCore.Time.TimeScale.Unscaled;
                animator.SetBehaviorsActive(false); animator.SetAppearancesActive(false);
                feedback.comboName = text; feedback.textAnimator = animator; feedback.ribbon = ribbon; feedback.catalog = catalog;
                feedback.reveal = CreatePlayer(banner.gameObject, banner, .1f);
                view.resultFeedback = feedback;
                view.presentationVersion = 1;
                Save(root, DicePath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static bool AuthorCard(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var frame = root.GetComponent<CommonButtonView>();
                if (!frame) throw new InvalidOperationException("카드 공통 프레임이 필요합니다: " + path);
                if (frame.GetComponent<SelectionFeedback>()) return false;
                var feedback = frame.gameObject.AddComponent<SelectionFeedback>();
                feedback.surface = frame.background;
                feedback.player = CreatePlayer(frame.gameObject, frame.transform, .09f);
                var graphic = (MMF_Graphic)feedback.player.AddFeedback(typeof(MMF_Graphic));
                graphic.TargetGraphic = frame.background; graphic.Mode = MMF_Graphic.Modes.OverTime;
                graphic.Duration = .32f; graphic.ModifyColor = true; graphic.StartsOff = false;
                graphic.DisableOnStop = false; graphic.AllowAdditivePlays = false;
                Save(root, path);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static MMF_Player CreatePlayer(GameObject owner, Transform target, float amount)
        {
            var player = owner.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            player.PlayerTimescaleMode = TimescaleModes.Unscaled; player.ForceTimescaleMode = true;
            player.ForcedTimescaleMode = TimescaleModes.Unscaled;
            player.CanPlayWhileAlreadyPlaying = false; player.StopFeedbacksOnDisable = true; player.RestoreInitialValuesOnDisable = true;
            var scale = (MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
            scale.AnimateScaleTarget = target; scale.Mode = MMF_Scale.Modes.Additive;
            scale.MovementMode = MMF_Scale.MovementModes.Duration; scale.AnimateScaleDuration = .32f;
            scale.RemapCurveZero = 0; scale.RemapCurveOne = amount;
            scale.AnimateX = true; scale.AnimateY = true; scale.AnimateZ = false; scale.UniformScaling = false;
            scale.AllowAdditivePlays = false; scale.DetermineScaleOnPlay = false;
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.28f, 1), new Keyframe(1, 0));
            scale.AnimateScaleTweenX = new MMTweenType(curve); scale.AnimateScaleTweenY = new MMTweenType(curve);
            return player;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Place(rect, position, size); return rect;
        }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        static void Save(GameObject root, string path)
        {
            if (!PrefabUtility.SaveAsPrefabAsset(root, path)) throw new InvalidOperationException("저장 실패: " + path);
        }
    }
}
