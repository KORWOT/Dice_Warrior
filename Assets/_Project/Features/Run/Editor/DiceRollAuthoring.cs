using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    public static class DiceRollAuthoring
    {
        public const string PopupPath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        const string RootPath = "Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab";
        const string CommonPath = "Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";

        [MenuItem("Fate Dice/주사위 창 원본 만들기")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Stop Play and wait for compilation before authoring.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null || StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("Close the preview or prefab stage first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");

            var popup = AssetDatabase.LoadAssetAtPath<DiceRollUI>(PopupPath);
            if (!popup) popup = CreatePopup();
            // Unity can synthesize required components on import without saving them to disk.
            // Load and save the contents even when the imported object already exposes them.
            {
                var contents = PrefabUtility.LoadPrefabContents(PopupPath);
                try
                {
                    foreach (var face in contents.GetComponent<DiceRollUI>().dice)
                        if (!face.GetComponent<CanvasRenderer>()) face.gameObject.AddComponent<CanvasRenderer>();
                    if (!PrefabUtility.SaveAsPrefabAsset(contents, PopupPath))
                        throw new InvalidOperationException("Could not repair the six dice renderers.");
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                popup = AssetDatabase.LoadAssetAtPath<DiceRollUI>(PopupPath);
            }
            var root = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                var manager = root.GetComponent<UIManager>();
                if (!manager) throw new InvalidOperationException("UIRoot requires UIManager.");
                if (!manager.prefabs.Any(x => x is DiceRollUI))
                {
                    manager.prefabs = manager.prefabs.Concat(new BaseUI[] { popup }).ToArray();
                    if (!PrefabUtility.SaveAsPrefabAsset(root, RootPath)) throw new InvalidOperationException("Could not register DiceRollUI.");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "Six-dice popup authored and registered; existing screen registrations retained.";
        }

        static DiceRollUI CreatePopup()
        {
            var common = AssetDatabase.LoadAssetAtPath<CommonButtonView>(CommonPath);
            if (!common || !common.label || !common.label.font) throw new InvalidOperationException("The existing button and Korean font are required.");
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject go = null;
            try
            {
                go = new GameObject("DiceRollUI", typeof(RectTransform), typeof(CanvasGroup));
                SceneManager.MoveGameObjectToScene(go, scene);
                var root = (RectTransform)go.transform;
                root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.sizeDelta = Vector2.zero;
                go.AddComponent<Image>().color = new Color(.006f, .01f, .018f, .94f);
                var view = go.AddComponent<DiceRollUI>();
                view.group = go.GetComponent<CanvasGroup>();
                var panel = Rect("주사위 창", root, Vector2.zero, new Vector2(620, 730));
                panel.gameObject.AddComponent<Image>().color = new Color(.035f, .048f, .068f);
                var outline = panel.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.52f, .42f, .25f); outline.effectDistance = new Vector2(2, -2);
                view.title = Label("제목", panel, common.label.font, 38, new Vector2(0, 302), new Vector2(560, 64));
                view.title.text = "운명의 주사위";
                view.detail = Label("안내", panel, common.label.font, 25, new Vector2(0, 233), new Vector2(552, 86));
                view.detail.text = "노드에 도착했습니다.\n굴리기를 눌러 운명을 확인하세요.";
                view.detail.color = new Color(.68f, .72f, .79f);
                var diceArea = Rect("주사위 6개", panel, Vector2.zero, new Vector2(540, 366));
                view.dice = new DiceFaceView[6];
                for (int i = 0; i < 6; i++)
                {
                    var face = Rect("주사위 " + (i + 1), diceArea, new Vector2((i % 3 - 1) * 174, 87 - (i / 3) * 180), new Vector2(148, 148));
                    face.gameObject.AddComponent<CanvasRenderer>();
                    view.dice[i] = face.gameObject.AddComponent<DiceFaceView>();
                    view.dice[i].raycastTarget = false;
                }
                view.result = Label("확정 결과", panel, common.label.font, 26, new Vector2(0, -223), new Vector2(550, 58));
                view.result.text = "6개의 주사위가 준비되었습니다";
                var buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(common.gameObject, panel);
                buttonObject.name = "굴리기 버튼";
                view.rollButton = buttonObject.GetComponent<CommonButtonView>();
                var buttonRect = (RectTransform)buttonObject.transform;
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, .5f);
                buttonRect.pivot = new Vector2(.5f, .5f);
                buttonRect.anchoredPosition = new Vector2(0, -300);
                buttonRect.sizeDelta = new Vector2(538, 82);
                var element = buttonObject.GetComponent<LayoutElement>();
                if (element) element.ignoreLayout = true;
                view.rollButton.label.fontSize = 28;
                view.rollButton.label.text = "주사위 6개 굴리기";
                view.rollButton.label.alignment = TextAnchor.MiddleCenter;
                view.rollButton.label.rectTransform.anchorMin = Vector2.zero;
                view.rollButton.label.rectTransform.anchorMax = Vector2.one;
                view.rollButton.label.rectTransform.offsetMin = new Vector2(12, 4);
                view.rollButton.label.rectTransform.offsetMax = new Vector2(-12, -4);
                var saved = PrefabUtility.SaveAsPrefabAsset(go, PopupPath);
                if (!saved) throw new InvalidOperationException("Could not save DiceRollUI prefab.");
                return saved.GetComponent<DiceRollUI>();
            }
            finally
            {
                if (go) UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        static Text Label(string name, Transform parent, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            var text = Rect(name, parent, position, dimensions).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.94f, .91f, .84f); text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
