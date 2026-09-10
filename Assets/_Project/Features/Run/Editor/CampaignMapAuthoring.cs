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

        [MenuItem("Fate Dice/Apply direct campaign travel (once)")]
        public static string ApplyDirectTravel()
        {
            RequireStoppedAuthoring();
            var existing = AssetDatabase.LoadAssetAtPath<ExplorationUI>(ExplorationPath);
            if (!existing || existing.layoutVersion < 2 || !existing.moveButton || !existing.selectedDetails)
                throw new InvalidOperationException("The procedural ExplorationUI original must be authored before direct travel.");
            if (existing.layoutVersion >= 3)
                return "Direct campaign travel version 3 already authored; no assets changed.";
            var contents = PrefabUtility.LoadPrefabContents(ExplorationPath);
            try
            {
                var view = contents.GetComponent<ExplorationUI>();
                // Preserve the button and its references/GUID. An inactive child releases its space in the existing stack.
                view.moveButton.interactable = false;
                view.moveButton.gameObject.SetActive(false);
                view.selectedDetails.text = "빛나는 경로를 누르면 바로 이동합니다.\n가까운 두 층의 유형을 확인할 수 있습니다.";
                view.layoutVersion = 3;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, ExplorationPath))
                    throw new InvalidOperationException("Could not save " + ExplorationPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return "Direct campaign travel authored on ExplorationUI (version 3).";
        }

        [MenuItem("Fate Dice/Apply procedural campaign layout (once)")]
        public static string ApplyProcedural()
        {
            RequireStoppedAuthoring();
            var existing = AssetDatabase.LoadAssetAtPath<ExplorationUI>(ExplorationPath);
            var original = AssetDatabase.LoadAssetAtPath<ExplorationNodeView>(NodePath);
            if (!existing || !original || !original.frame || !original.frame.label || !original.frame.label.font)
                throw new InvalidOperationException("The existing map/node originals and their font are required.");
            bool nodeNeeded = original.layoutVersion < 2, screenNeeded = existing.layoutVersion < 2;
            if (!nodeNeeded && !screenNeeded) return "Procedural campaign layout version 2 already authored; no assets changed.";
            Font font = original.frame.label.font;
            if (nodeNeeded) AuthorProceduralNode();
            if (screenNeeded)
            {
                var contents = PrefabUtility.LoadPrefabContents(ExplorationPath);
                try
                {
                    var view = contents.GetComponent<ExplorationUI>();
                    if (view.layoutVersion < 1) AuthorScreen(view, font);
                    AuthorProceduralScreen(view, font);
                    view.layoutVersion = 2;
                    if (!PrefabUtility.SaveAsPrefabAsset(contents, ExplorationPath))
                        throw new InvalidOperationException("Could not save " + ExplorationPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            return "Procedural map originals authored: screen=" + screenNeeded + ", node=" + nodeNeeded + ".";
        }

        static void RequireStoppedAuthoring()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Stop Play Mode and wait for imports before authoring the campaign map.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before authoring the campaign map.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null ||
                StageUtility.GetCurrentStageHandle() != StageUtility.GetMainStageHandle())
                throw new InvalidOperationException("Close Prefab Mode and preview stages before authoring the campaign map.");
        }

        static void AuthorProceduralNode()
        {
            var contents = PrefabUtility.LoadPrefabContents(NodePath);
            try
            {
                var node = contents.GetComponent<ExplorationNodeView>();
                if (!node || !node.frame) throw new InvalidOperationException("The node original requires its existing frame.");
                var frame = node.frame;
                var rect = (RectTransform)contents.transform;
                rect.sizeDelta = new Vector2(100, 112); Height(rect, 112);
                // Keep the inherited images/font/feedback components. A transparent hit area retains the Button contract.
                frame.background.color = Color.clear;
                frame.border.gameObject.SetActive(false);
                var symbol = Child("Map node symbol", rect);
                symbol.anchorMin = new Vector2(0, .27f); symbol.anchorMax = new Vector2(1, 1);
                symbol.offsetMin = new Vector2(2, 0); symbol.offsetMax = new Vector2(-2, 0);
                node.mapGraphic = symbol.GetComponent<MapNodeGraphic>();
                if (!node.mapGraphic) node.mapGraphic = symbol.gameObject.AddComponent<MapNodeGraphic>();
                node.mapGraphic.raycastTarget = false;
                symbol.SetAsFirstSibling();
                var iconArea = (RectTransform)frame.icon.transform.parent;
                iconArea.anchorMin = iconArea.anchorMax = new Vector2(.5f, .635f);
                iconArea.pivot = new Vector2(.5f, .5f);
                iconArea.sizeDelta = new Vector2(32, 32); iconArea.anchoredPosition = Vector2.zero;
                iconArea.gameObject.SetActive(true); Fill(frame.icon.rectTransform); Fill(frame.iconFallback.rectTransform);
                frame.label.rectTransform.anchorMin = new Vector2(0, 0); frame.label.rectTransform.anchorMax = new Vector2(1, .25f);
                frame.label.rectTransform.offsetMin = new Vector2(1, 1); frame.label.rectTransform.offsetMax = new Vector2(-1, -1);
                Format(frame.label, 17, TextAnchor.MiddleCenter); frame.label.resizeTextForBestFit = false;
                node.layoutVersion = 2;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, NodePath)) throw new InvalidOperationException("Could not save " + NodePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static void AuthorProceduralScreen(ExplorationUI view, Font font)
        {
            if (!view || !view.layout || !view.campaignMap || !view.mapContainer)
                throw new InvalidOperationException("The existing campaign map layout must be authored first.");
            var root = (RectTransform)view.transform;
            var layout = view.layout;
            var stack = root.GetComponent<VerticalLayoutGroup>();
            if (!stack || !layout.scroll || !layout.body) throw new InvalidOperationException("The map requires its original editable stack and viewport.");
            stack.padding = new RectOffset(18, 18, 14, 12); stack.spacing = 6;
            stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var background = root.GetComponent<Image>();
            if (background) background.color = new Color(.025f, .031f, .041f);
            Height(layout.header.rectTransform, 42); Format(layout.header, 27, TextAnchor.MiddleLeft);
            layout.header.color = new Color(.86f, .80f, .65f);
            layout.stats.gameObject.SetActive(false); layout.situation.gameObject.SetActive(false);
            layout.gear.gameObject.SetActive(false); layout.diceRow.gameObject.SetActive(false);

            var top = Child("Campaign progress and gold", root); Horizontal(top, 16); Height(top, 38);
            view.progressLabel = ChildLabel("Campaign progress", top, font, 18, new Color(.73f,.73f,.72f));
            view.goldLabel = ChildLabel("Campaign gold", top, font, 19, new Color(.88f,.74f,.39f));
            Flexible(view.progressLabel.rectTransform); Flexible(view.goldLabel.rectTransform);
            view.goldLabel.alignment = TextAnchor.MiddleRight;
            view.progressLabel.text = "여정 0 / 10"; view.goldLabel.text = "골드 0";

            view.legendToggle = AuthoredButton("Map legend toggle", root, font, "지도 범례  ▾", out var legendLabel);
            view.legendLabel = legendLabel; Height((RectTransform)view.legendToggle.transform, 34); Format(legendLabel, 16, TextAnchor.MiddleRight);
            view.legendToggle.image.color = new Color(.04f,.05f,.063f);
            view.legendBody = Child("Map legend", root); Height(view.legendBody, 94);
            var legend = ChildLabel("Map legend entries", view.legendBody, font, 17, new Color(.65f,.68f,.71f));
            Fill(legend.rectTransform); legend.alignment = TextAnchor.MiddleCenter;
            legend.text = "전투 · 사건 · 보물 · 상점 · 휴식 · 대운명\n발광 링  이동 가능    ✓  완료    자물쇠  접근 불가\n파란 표식  현재 위치    점선 빈 원  미발견";
            view.legendBody.gameObject.SetActive(false);

            Height(layout.notice.rectTransform, 44); Format(layout.notice, 16, TextAnchor.MiddleLeft); layout.notice.color = Muted;
            Height(layout.fate.rectTransform, 34); Format(layout.fate, 16, TextAnchor.MiddleLeft); layout.fate.color = new Color(.65f,.61f,.75f);
            var scrollHeight = layout.scroll.GetComponent<LayoutElement>();
            if (!scrollHeight) scrollHeight = layout.scroll.gameObject.AddComponent<LayoutElement>();
            scrollHeight.minHeight = 240; scrollHeight.preferredHeight = -1; scrollHeight.flexibleHeight = 1;
            layout.scroll.horizontal = false; layout.scroll.vertical = true;
            layout.scroll.scrollSensitivity = 34; layout.scroll.movementType = ScrollRect.MovementType.Clamped;
            var bodyStack = layout.body.GetComponent<VerticalLayoutGroup>();
            if (bodyStack) { bodyStack.spacing = 0; bodyStack.padding = new RectOffset(0,0,0,0); }
            view.campaignMap.rowSpacing = 160;
            var mapSurface = view.mapContainer.GetComponent<Image>();
            if (mapSurface) { mapSurface.color = new Color(.027f,.034f,.044f); mapSurface.raycastTarget = false; }
            var direction = view.mapContainer.Find("Map direction");
            if (direction && direction.TryGetComponent<Text>(out var directionLabel))
            { directionLabel.text = "↑  대운명을 향하여"; directionLabel.color = new Color(.48f,.48f,.51f); }
            var markerImage = view.campaignMap.playerMarker.GetComponent<Image>();
            if (markerImage) markerImage.color = new Color(.20f,.56f,1);
            view.campaignMap.arrivedColor = new Color(.25f,.60f,1);
            var playerText = view.campaignMap.playerMarker.GetComponentInChildren<Text>(true);
            if (playerText) { playerText.text = "나"; playerText.color = new Color(.88f,.95f,1); }

            var health = Child("Campaign health and ward", root); Horizontal(health, 18); Height(health, 38);
            view.healthLabel = ChildLabel("Campaign health", health, font, 21, new Color(.89f,.51f,.47f));
            view.wardLabel = ChildLabel("Campaign ward", health, font, 20, new Color(.53f,.72f,.88f));
            Flexible(view.healthLabel.rectTransform); Flexible(view.wardLabel.rectTransform);
            view.wardLabel.alignment = TextAnchor.MiddleRight;
            view.healthLabel.text = "체력 —"; view.wardLabel.text = "수호 —";
            view.selectedDetails = ChildLabel("Selected route details", root, font, 19, new Color(.82f,.82f,.79f));
            Height(view.selectedDetails.rectTransform, 78); view.selectedDetails.alignment = TextAnchor.MiddleLeft;
            view.selectedDetails.text = "빛나는 경로를 눌러 살펴보세요.\n가까운 두 층의 유형을 확인할 수 있습니다.";
            view.moveButton = AuthoredButton("Travel to selected node", root, font, "이동할 경로를 선택하세요", out var moveLabel);
            view.moveLabel = moveLabel; Height((RectTransform)view.moveButton.transform, 64);
            view.moveButton.image.color = new Color(.26f,.24f,.17f); view.moveButton.interactable = false;

            view.instructions.transform.SetParent(root, false); Height(view.instructions.rectTransform, 44); Format(view.instructions, 17, TextAnchor.MiddleLeft);
            view.rollChoices.SetParent(root, false); Height(view.rollChoices, 108);
            view.fateChoices.gameObject.SetActive(false);
            Height(layout.footer, 108);
            int order = 0;
            layout.header.transform.SetSiblingIndex(order++); top.SetSiblingIndex(order++);
            view.legendToggle.transform.SetSiblingIndex(order++); view.legendBody.SetSiblingIndex(order++);
            layout.scroll.transform.SetSiblingIndex(order++); health.SetSiblingIndex(order++);
            view.selectedDetails.transform.SetSiblingIndex(order++); view.moveButton.transform.SetSiblingIndex(order++);
            view.instructions.transform.SetSiblingIndex(order++); view.rollChoices.SetSiblingIndex(order++);
            layout.fate.transform.SetSiblingIndex(order++); layout.notice.transform.SetSiblingIndex(order++); layout.footer.SetSiblingIndex(order);
        }

        static RectTransform Child(string name, Transform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            return existing ? existing : Rect(name, parent);
        }
        static Text ChildLabel(string name, Transform parent, Font font, int size, Color color)
        {
            var existing = parent.Find(name);
            if (existing && existing.TryGetComponent<Text>(out var text)) { Format(text, size, TextAnchor.MiddleLeft); return text; }
            return Label(name, parent, font, size, color);
        }
        static void Horizontal(RectTransform rect, float spacing)
        {
            var row = rect.GetComponent<HorizontalLayoutGroup>();
            if (!row) row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing; row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true; row.childForceExpandHeight = true;
        }
        static void Flexible(RectTransform rect)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (!element) element = rect.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 0; element.preferredWidth = 0; element.flexibleWidth = 1;
        }
        static Button AuthoredButton(string name, Transform parent, Font font, string caption, out Text label)
        {
            var rect = Child(name, parent);
            var image = rect.GetComponent<Image>();
            if (!image) image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.14f,.16f,.18f); image.raycastTarget = true;
            var button = rect.GetComponent<Button>();
            if (!button) button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f,1.10f,.95f); colors.pressedColor = new Color(.75f,.75f,.68f);
            colors.disabledColor = new Color(.44f,.46f,.49f); button.colors = colors;
            label = ChildLabel("Label", rect, font, 21, new Color(.94f,.89f,.75f));
            Fill(label.rectTransform); label.alignment = TextAnchor.MiddleCenter; label.text = caption;
            return button;
        }

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
