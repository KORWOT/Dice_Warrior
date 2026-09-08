using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // Versioned presentation-only migration; never rewrites run rules or saved data.
    public static class CombatFeedbackAuthoring
    {
        public const string MenuPath = "Assets/_Project/Features/Run/Prefabs/MenuUI.prefab";
        public const string CombatPath = "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab";
        public const string DicePath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        const int Version = 1;

        [MenuItem("Fate Dice/전투 피드백 원본 적용")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("플레이를 중지하고 컴파일과 가져오기가 끝난 뒤 적용하세요.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("미리보기와 프리팹 편집을 닫은 뒤 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬의 변경 사항을 먼저 저장하세요.");
            if (!AssetDatabase.LoadAssetAtPath<MenuUI>(MenuPath) ||
                !AssetDatabase.LoadAssetAtPath<CombatUI>(CombatPath) ||
                !AssetDatabase.LoadAssetAtPath<DiceRollUI>(DicePath))
                throw new InvalidOperationException("기존 로비, 전투, 주사위 창 원본이 필요합니다.");

            int changed = 0;
            if (AuthorMenu()) changed++;
            if (AuthorCombat()) changed++;
            if (AuthorDice()) changed++;
            return "전투 피드백 원본 적용: " + changed + "개 프리팹 변경. 적용된 원본의 사용자 편집은 유지합니다.";
        }

        static bool AuthorMenu()
        {
            var contents = PrefabUtility.LoadPrefabContents(MenuPath);
            try
            {
                bool changed = false;
                foreach (var text in contents.GetComponentsInChildren<Text>(true))
                {
                    string replacement = null;
                    switch (text.text)
                    {
                        case "시련 카드":
                        case "TRIAL WILDCARD": replacement = "와일드 카드"; break;
                        case "시련 카드와 탐험 등급 상한은 세팅에서 선택하세요.":
                            replacement = "와일드 카드와 탐험 등급 상한은 세팅에서 선택하세요."; break;
                        case "시련 카드와 탐험 카드의 등급 상한을 선택하세요.":
                            replacement = "와일드 카드와 탐험 카드의 등급 상한을 선택하세요."; break;
                    }
                    if (replacement == null) continue;
                    text.text = replacement;
                    changed = true;
                }
                if (changed) Save(contents, MenuPath);
                return changed;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static bool AuthorCombat()
        {
            var contents = PrefabUtility.LoadPrefabContents(CombatPath);
            try
            {
                var view = contents.GetComponent<CombatUI>();
                if (view.feedbackVersion >= Version) return false;
                if (!view.arena || !view.layout || !view.layout.header || !view.layout.header.font ||
                    !view.layout.fate || !view.layout.diceRow || !view.fatePower || !view.rerolls)
                    throw new InvalidOperationException("전투 원본의 무대, 한글 글꼴, 주사위 표시 연결이 필요합니다.");
                var font = view.layout.header.font;
                var flash = Rect("Hit flash", view.arena);
                Stretch(flash);
                view.hitFlash = flash.gameObject.AddComponent<Image>();
                view.hitFlash.color = new Color(1, .18f, .12f, 0);
                view.hitFlash.raycastTarget = false;

                view.actionFeedback = Label("Action feedback", view.arena, font, 27,
                    new Color(.98f, .87f, .65f), "강공격 · 희귀");
                Region(view.actionFeedback.rectTransform, new Vector2(.10f, .70f), new Vector2(.70f, .87f));
                view.damageFeedback = Label("Damage feedback", view.arena, font, 30,
                    new Color(1, .78f, .68f), "적 체력 -24\n막힘 3");
                Region(view.damageFeedback.rectTransform, new Vector2(.40f, .32f), new Vector2(.96f, .63f));
                view.actionFeedbackSeconds = .38f;
                view.shakePixels = 9;
                view.feedbackHoldSeconds = .25f;

                // Keep the complete hand stage readable above six dice at portrait widths.
                var hand = view.layout.fate;
                hand.fontSize = 18;
                hand.horizontalOverflow = HorizontalWrapMode.Wrap;
                hand.verticalOverflow = VerticalWrapMode.Truncate;
                TopBand(hand.rectTransform, 0, .65f, 48);
                view.fatePower.fontSize = 17;
                TopBand(view.fatePower.rectTransform, .65f, .82f, 48);
                view.rerolls.fontSize = 17;
                TopBand(view.rerolls.rectTransform, .82f, 1, 48);
                var row = view.layout.diceRow;
                row.anchorMin = new Vector2(0, 1); row.anchorMax = Vector2.one;
                row.offsetMin = new Vector2(0, -154); row.offsetMax = new Vector2(0, -52);
                var diceArea = row.parent.GetComponent<LayoutElement>();
                if (!diceArea) throw new InvalidOperationException("전투 주사위 영역의 LayoutElement가 필요합니다.");
                diceArea.minHeight = diceArea.preferredHeight = 154;

                view.feedbackVersion = Version;
                Save(contents, CombatPath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static bool AuthorDice()
        {
            var contents = PrefabUtility.LoadPrefabContents(DicePath);
            try
            {
                var view = contents.GetComponent<DiceRollUI>();
                if (view.feedbackVersion >= Version) return false;
                if (!view.result || !view.rollButton)
                    throw new InvalidOperationException("주사위 창의 결과와 굴리기 버튼 연결이 필요합니다.");
                var result = view.result.rectTransform;
                result.anchorMin = result.anchorMax = new Vector2(.5f, .5f);
                result.pivot = new Vector2(.5f, .5f);
                result.anchoredPosition = new Vector2(0, -205);
                result.sizeDelta = new Vector2(550, 90);
                view.result.fontSize = 24;
                view.result.horizontalOverflow = HorizontalWrapMode.Wrap;
                view.result.verticalOverflow = VerticalWrapMode.Truncate;
                view.result.raycastTarget = false;
                view.resultHoldSeconds = .9f;
                view.feedbackVersion = Version;
                Save(contents, DicePath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static Text Label(string name, Transform parent, Font font, int size, Color color, string example)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = example;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.01f, .015f, .02f, .95f);
            outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect) => Region(rect, Vector2.zero, Vector2.one);

        static void Region(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void TopBand(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(left, 1); rect.anchorMax = new Vector2(right, 1);
            rect.offsetMin = new Vector2(0, -height); rect.offsetMax = Vector2.zero;
        }

        static void Save(GameObject contents, string path)
        {
            if (!PrefabUtility.SaveAsPrefabAsset(contents, path))
                throw new InvalidOperationException("프리팹을 저장하지 못했습니다: " + path);
        }
    }
}
