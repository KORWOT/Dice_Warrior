using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    public static class CampaignMapAuthoring
    {
        public const string ExplorationPath = "Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab";
        public const string NodePath = "Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab";
        const int Version = 1;
        static readonly Color Ink = new Color(.91f, .94f, .94f);
        static readonly Color Muted = new Color(.55f, .65f, .67f);
        static readonly Color Accent = new Color(.34f, .83f, .78f);

        [MenuItem("Fate Dice/Apply campaign map layout (once)")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Stop Play Mode and wait for imports before authoring the campaign map.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before authoring the campaign map.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null ||
                StageUtility.GetCurrentStageHandle() != StageUtility.GetMainStageHandle())
                throw new InvalidOperationException("Close Prefab Mode and preview stages before authoring the campaign map.");

            var existing = AssetDatabase.LoadAssetAtPath<ExplorationUI>(ExplorationPath);
            var node = AssetDatabase.LoadAssetAtPath<ExplorationNodeView>(NodePath);
            if (!existing || !node || !node.frame || !node.frame.label || !node.frame.label.font)
                throw new InvalidOperationException("Existing ExplorationUI and its node original/font are required.");
            if (existing.layoutVersion >= Version)
                return "Campaign map layout version 1 already authored; no assets changed.";

            AuthorNode();
            var contents = PrefabUtility.LoadPrefabContents(ExplorationPath);
            try
            {
                var view = contents.GetComponent<ExplorationUI>();
                if (!view || !view.layout || !view.group || !view.mapContainer)
                    throw new InvalidOperationException("ExplorationUI must keep its authored root components and map container.");
                AuthorScreen(view, node.frame.label.font);
                view.layoutVersion = Version;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, ExplorationPath))
                    throw new InvalidOperationException("Could not save " + ExplorationPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return "Campaign map layers and original node touch layout authored (version 1).";
        }

        static void AuthorNode()
        {
            var contents = PrefabUtility.LoadPrefabContents(NodePath);
            try
            {
                var node = contents.GetComponent<ExplorationNodeView>();
                if (!node || !node.frame) throw new InvalidOperationException("The node original requires its CommonButtonView frame.");
                var frame = node.frame;
                var rect = (RectTransform)contents.transform;
                rect.sizeDelta = new Vector2(116, 106);
                Height(rect, 106);
                // Preserve the original CommonButtonView hierarchy, sprite references, font and fontStyle inheritance.
                var iconArea = (RectTransform)frame.icon.transform.parent;
                iconArea.anchorMin = iconArea.anchorMax = new Vector2(.5f, 1);
                iconArea.pivot = new Vector2(.5f, .5f);
                iconArea.sizeDelta = new Vector2(36, 36); iconArea.anchoredPosition = new Vector2(0, -27);
                iconArea.gameObject.SetActive(true);
                Fill(frame.icon.rectTransform);
                Fill(frame.iconFallback.rectTransform);
                Format(frame.iconFallback, 28, TextAnchor.MiddleCenter);
                frame.label.rectTransform.anchorMin = Vector2.zero;
                frame.label.rectTransform.anchorMax = new Vector2(1, 0);
                frame.label.rectTransform.offsetMin = new Vector2(3, 5);
                frame.label.rectTransform.offsetMax = new Vector2(-3, 36);
                Format(frame.label, 19, TextAnchor.MiddleCenter);
                frame.label.resizeTextForBestFit = false;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, NodePath))
                    throw new InvalidOperationException("Could not save " + NodePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static void AuthorScreen(ExplorationUI view, Font font)
        {
            var root = (RectTransform)view.transform;
            var layout = view.layout;
            var stack = root.GetComponent<VerticalLayoutGroup>();
            if (!stack || !layout.scroll || !layout.body || !view.instructions || !view.rollChoices || !view.fateChoices)
                throw new InvalidOperationException("ExplorationUI requires its existing editable layout and one viewport.");
            stack.padding = new RectOffset(16, 16, 14, 14); stack.spacing = 8;
            stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            Height(layout.header.rectTransform, 44); Format(layout.header, 28, TextAnchor.MiddleLeft);
            Height(layout.stats.rectTransform, 72); Format(layout.stats, 18, TextAnchor.UpperLeft);
            Height(layout.situation.rectTransform, 54); Format(layout.situation, 22, TextAnchor.MiddleLeft);
            Height(layout.notice.rectTransform, 64); Format(layout.notice, 18, TextAnchor.UpperLeft);
            Height(layout.fate.rectTransform, 74); Format(layout.fate, 21, TextAnchor.MiddleLeft);
            Height(layout.diceRow, 104); Height(layout.footer, 108);
            layout.diceRow.gameObject.SetActive(false);
            layout.gear.gameObject.SetActive(false);
            layout.fate.gameObject.SetActive(false);
            var scrollHeight = layout.scroll.GetComponent<LayoutElement>();
            if (!scrollHeight) scrollHeight = layout.scroll.gameObject.AddComponent<LayoutElement>();
            scrollHeight.minHeight = 240; scrollHeight.preferredHeight = -1; scrollHeight.flexibleHeight = 1;
            layout.scroll.horizontal = false; layout.scroll.vertical = true;
            layout.scroll.scrollSensitivity = 28;
            layout.scroll.movementType = ScrollRect.MovementType.Clamped;
            var bodyStack = layout.body.GetComponent<VerticalLayoutGroup>();
            if (bodyStack) { bodyStack.spacing = 12; bodyStack.padding = new RectOffset(0, 0, 0, 12); }
            Height(view.instructions.rectTransform, 66); Format(view.instructions, 19, TextAnchor.MiddleLeft);
            view.instructions.text = "위로 이어진 길 중 하나를 선택하세요.\n밝은 테두리는 지금 이동할 수 있는 곳입니다.";
            var map = view.mapContainer;
            var oldStack = map.GetComponent<VerticalLayoutGroup>();
            if (oldStack) UnityEngine.Object.DestroyImmediate(oldStack);
            var oldFitter = map.GetComponent<ContentSizeFitter>();
            if (oldFitter) UnityEngine.Object.DestroyImmediate(oldFitter);
            Height(map, 470);
            var graphic = map.GetComponent<Image>();
            if (!graphic) graphic = map.gameObject.AddComponent<Image>();
            graphic.color = new Color(.025f, .047f, .057f); graphic.raycastTarget = false;
            view.campaignMap = map.GetComponent<CampaignMapView>();
            if (!view.campaignMap) view.campaignMap = map.gameObject.AddComponent<CampaignMapView>();
            view.campaignMap.rowSpacing = 174;
            view.campaignMap.edgeLayer = Layer("Path connections", map);
            view.campaignMap.nodeLayer = Layer("Path nodes", map);
            var label = Label("Current position", map, font, 18, Muted);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 0);
            label.rectTransform.sizeDelta = new Vector2(650, 42); label.rectTransform.anchoredPosition = new Vector2(0, 26);
            label.text = "현재 위치  ·  연결된 길을 선택하세요";
            view.campaignMap.currentLocation = label;
            var direction = Label("Map direction", map, font, 17, Muted);
            direction.text = "↑ 공개된 다음 경로"; direction.alignment = TextAnchor.MiddleCenter;
            direction.rectTransform.anchorMin = new Vector2(0, 1); direction.rectTransform.anchorMax = Vector2.one;
            direction.rectTransform.offsetMin = new Vector2(10, -36); direction.rectTransform.offsetMax = new Vector2(-10, -4);
            var marker = Rect("Player marker", map);
            marker.anchorMin = marker.anchorMax = new Vector2(.5f, 0);
            marker.sizeDelta = new Vector2(40, 40); marker.anchoredPosition = new Vector2(0, 74);
            var markerImage = marker.gameObject.AddComponent<Image>();
            markerImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            markerImage.type = Image.Type.Sliced; markerImage.color = Accent; markerImage.raycastTarget = false;
            var mark = Label("Player glyph", marker, font, 20, new Color(.02f, .10f, .12f));
            Fill(mark.rectTransform); mark.alignment = TextAnchor.MiddleCenter; mark.text = "나";
            view.campaignMap.playerMarker = marker;
            view.campaignMap.edgeLayer.SetAsFirstSibling();
            view.campaignMap.nodeLayer.SetSiblingIndex(1);
            marker.SetAsLastSibling();
            view.mapContainer.SetAsFirstSibling();
        }

        static RectTransform Layer(string name, Transform parent)
        {
            var rect = Rect(name, parent); Fill(rect); return rect;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        static Text Label(string name, Transform parent, Font font, int size, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = rect.GetComponent<Text>(); text.font = font; text.color = color; text.supportRichText = false;
            Format(text, size, TextAnchor.MiddleLeft); return text;
        }
        static void Format(Text text, int size, TextAnchor alignment)
        {
            text.fontSize = size; text.alignment = alignment; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        }
        static void Height(RectTransform rect, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (!element) element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height; element.flexibleHeight = 0; element.flexibleWidth = 1;
        }
        static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
