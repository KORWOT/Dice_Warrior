using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Editor
{
    // Migrates only MenuUI.prefab; every preparation panel remains editable in the original prefab.
    public static class LobbyPreparationAuthoring
    {
        public const string MenuPath = "Assets/_Project/Features/Run/Prefabs/MenuUI.prefab";
        const int Version = 1;
        static readonly Color Background = new Color(.018f, .026f, .039f);
        static readonly Color Panel = new Color(.035f, .052f, .073f);
        static readonly Color Line = new Color(.18f, .23f, .29f);
        static readonly Color Gold = new Color(.75f, .63f, .39f);
        static readonly Color Ink = new Color(.91f, .91f, .88f);
        static readonly Color Muted = new Color(.60f, .67f, .74f);

        [MenuItem("Fate Dice/로비 준비 화면 적용")]
        public static string Apply()
        {
            GuardEditor();
            var original = AssetDatabase.LoadAssetAtPath<MenuUI>(MenuPath);
            if (!original || !original.layout || !original.layout.header || !original.layout.header.font)
                throw new InvalidOperationException("기존 MenuUI와 연결된 한글 폰트가 필요합니다.");
            if (EditorUtility.IsDirty(original.gameObject))
                throw new InvalidOperationException("로비 원본의 편집 내용을 먼저 저장하거나 되돌려 주세요.");
            if (original.layoutVersion >= Version) return "로비 준비 화면 버전 1이 이미 적용되어 있습니다. 변경 없음.";

            var contents = PrefabUtility.LoadPrefabContents(MenuPath);
            try
            {
                var menu = contents.GetComponent<MenuUI>();
                if (!menu || !menu.layout || !menu.group)
                    throw new InvalidOperationException("MenuUI의 원본 view, layout, CanvasGroup 연결이 필요합니다.");
                var font = menu.layout.header.font;
                Author(menu, font);
                menu.layout.Validate();
                menu.layoutVersion = Version;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, MenuPath))
                    throw new InvalidOperationException("로비 준비 화면을 저장하지 못했습니다.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return "MenuUI 원본에 캐릭터·세팅·성장 안내와 고정 출전 버튼을 적용했습니다 (버전 1).";
        }

        static void GuardEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("플레이와 컴파일·가져오기를 마친 뒤 로비를 적용해 주세요.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null ||
                !StageUtility.GetCurrentStageHandle().Equals(StageUtility.GetMainStageHandle()))
                throw new InvalidOperationException("원본 편집과 미리보기를 닫고 제작 씬으로 돌아와 주세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("열린 씬을 먼저 저장하거나 직접 닫아 주세요.");
            }
        }

        static void Author(MenuUI menu, Font font)
        {
            var root = (RectTransform)menu.transform;
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            Fill(root);
            var background = root.GetComponent<Image>();
            if (!background) background = root.gameObject.AddComponent<Image>();
            background.color = Background; background.raycastTarget = true;
            var stack = root.GetComponent<VerticalLayoutGroup>();
            if (!stack) stack = root.gameObject.AddComponent<VerticalLayoutGroup>();
            Stack(stack, 12, 20);

            var header = Column("Lobby header", root, 2, 0);
            FixedHeight(header, 84);
            var eyebrow = Text("Preparation heading", header, font, 17, Gold, "출전 준비");
            MinimumHeight(eyebrow.rectTransform, 26);
            menu.layout.header = Text("Header", header, font, 30, Ink, "운명의 주사위  /  로비");
            MinimumHeight(menu.layout.header.rectTransform, 44);

            var tabs = Rect("Preparation tabs", root);
            FixedHeight(tabs, 68);
            menu.characterTab = Tab("Character tab", tabs, font, "캐릭터", 0, menu);
            menu.settingsTab = Tab("Settings tab", tabs, font, "세팅", 1, menu);
            menu.growthTab = Tab("Growth tab", tabs, font, "성장", 2, menu);

            var scrollRoot = Rect("Preparation content", root);
            var flex = scrollRoot.gameObject.AddComponent<LayoutElement>();
            flex.minHeight = 180; flex.flexibleHeight = 1;
            menu.layout.scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            menu.layout.scroll.horizontal = false; menu.layout.scroll.vertical = true;
            menu.layout.scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", scrollRoot); Fill(viewport);
            var touch = viewport.gameObject.AddComponent<Image>(); touch.color = Background;
            touch.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            menu.layout.body = Column("Content", viewport, 0, 0);
            TopContent(menu.layout.body);
            menu.layout.body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            menu.layout.scroll.viewport = viewport; menu.layout.scroll.content = menu.layout.body;

            menu.characterPanel = Card("Character panel", menu.layout.body, 10, 22);
            var caption = Text("Character caption", menu.characterPanel, font, 18, Muted, "함께할 모험가");
            MinimumHeight(caption.rectTransform, 28);
            caption.alignment = TextAnchor.MiddleCenter;
            Emblem(menu.characterPanel);
            menu.characterName = Text("Character name", menu.characterPanel, font, 38, Ink, "방랑자");
            MinimumHeight(menu.characterName.rectTransform, 54);
            menu.characterName.alignment = TextAnchor.MiddleCenter;
            var chosen = Text("Character selected", menu.characterPanel, font, 19, Gold, "선택됨");
            MinimumHeight(chosen.rectTransform, 30); chosen.alignment = TextAnchor.MiddleCenter;
            Divider("Character divider", menu.characterPanel, Gold);
            menu.characterDetails = Text("Character details", menu.characterPanel, font, 22, Ink);
            MinimumHeight(menu.characterDetails.rectTransform, 184);
            var characterHelp = Text("Character help", menu.characterPanel, font, 18, Muted,
                "와일드 카드와 탐험 등급 상한은 세팅에서 선택하세요.");
            MinimumHeight(characterHelp.rectTransform, 50);

            menu.settingsPanel = Card("Settings panel", menu.layout.body, 14, 22);
            var settingTitle = Text("Settings heading", menu.settingsPanel, font, 28, Ink, "출전 설정");
            MinimumHeight(settingTitle.rectTransform, 42);
            var settingHelp = Text("Settings help", menu.settingsPanel, font, 19, Muted,
                "새 여정에 사용할 구성을 선택하세요.\n저장된 여정은 시작 당시의 설정으로 이어집니다.");
            MinimumHeight(settingHelp.rectTransform, 66);
            menu.trialHeading = Text("Trial heading", menu.settingsPanel, font, 22, Gold, "와일드 카드");
            MinimumHeight(menu.trialHeading.rectTransform, 38);
            menu.trialChoices = Row("Wildcards", menu.settingsPanel, 112);
            menu.capHeading = Text("Grade cap heading", menu.settingsPanel, font, 22, Gold, "탐험 등급 상한");
            MinimumHeight(menu.capHeading.rectTransform, 40);
            menu.capChoices = Row("Grade cap", menu.settingsPanel, 112);
            var capHelp = Text("Grade cap help", menu.settingsPanel, font, 19, Muted,
                "선택한 상한은 탐험 카드에 적용됩니다.\n전투 카드의 등급에는 영향을 주지 않습니다.");
            MinimumHeight(capHelp.rectTransform, 68);

            menu.growthPanel = Card("Growth panel", menu.layout.body, 16, 24);
            var growthTitle = Text("Growth heading", menu.growthPanel, font, 28, Ink, "여정 속 성장");
            MinimumHeight(growthTitle.rectTransform, 44);
            menu.growthDetails = Text("Growth details", menu.growthPanel, font, 22, Ink);
            MinimumHeight(menu.growthDetails.rectTransform, 238);
            Divider("Growth divider", menu.growthPanel, Line);
            var future = Text("Permanent growth status", menu.growthPanel, font, 20, Gold,
                "영구 성장 기능은 아직 제공하지 않습니다.\n현재 보상과 성장은 해당 여정에만 적용됩니다.");
            MinimumHeight(future.rectTransform, 92);

            // This footer is outside the scrolling tab content, so departure/continue remain reachable.
            menu.layout.footer = Column("Departure footer", root, 8, 0);
            Divider("Departure divider", menu.layout.footer, Line);
            menu.error = Text("Save error", menu.layout.footer, font, 18, new Color(.91f, .56f, .49f));
            MinimumHeight(menu.error.rectTransform, 76); menu.error.gameObject.SetActive(false);
            menu.lastResult = Text("Last result", menu.layout.footer, font, 18, Muted);
            MinimumHeight(menu.lastResult.rectTransform, 56); menu.lastResult.gameObject.SetActive(false);
            menu.mainChoices = Column("Journey choices", menu.layout.footer, 8, 0);
            menu.layout.notice = Text("Notice", menu.layout.footer, font, 17, Muted);
            MinimumHeight(menu.layout.notice.rectTransform, 50);

            // Existing layout/seed serialized references stay valid, without showing in the preparation flow.
            var hidden = Rect("Compatibility fields (hidden)", root);
            menu.layout.stats = Text("Stats", hidden, font, 22, Ink);
            menu.layout.situation = Text("Situation", hidden, font, 22, Ink);
            menu.layout.fate = Text("Fate", hidden, font, 22, Ink);
            menu.layout.gear = Text("Gear summary", hidden, font, 22, Ink);
            menu.layout.diceRow = Rect("Dice (hidden)", hidden);
            menu.seedHeading = Text("Seed heading", hidden, font, 22, Ink, "시드");
            var seed = Rect("seed", hidden); seed.sizeDelta = new Vector2(400, 64);
            var seedBackground = seed.gameObject.AddComponent<Image>(); seedBackground.color = Panel;
            var seedLabel = Text("Value", seed, font, 22, Ink); Fill(seedLabel.rectTransform, 12);
            menu.seedInput = seed.gameObject.AddComponent<InputField>();
            menu.seedInput.textComponent = seedLabel; menu.seedInput.targetGraphic = seedBackground;
            menu.seedInput.contentType = InputField.ContentType.IntegerNumber;
            seed.gameObject.SetActive(false); menu.seedHeading.gameObject.SetActive(false); hidden.gameObject.SetActive(false);
            menu.characterPanel.gameObject.SetActive(true);
            menu.settingsPanel.gameObject.SetActive(false); menu.growthPanel.gameObject.SetActive(false);
        }

        static RectTransform Card(string name, Transform parent, float spacing, int padding)
        {
            var card = Column(name, parent, spacing, padding);
            var image = card.gameObject.AddComponent<Image>(); image.color = Panel; image.raycastTarget = false;
            var outline = card.gameObject.AddComponent<Outline>(); outline.effectColor = Line;
            outline.effectDistance = new Vector2(1, -1); outline.useGraphicAlpha = false;
            return card;
        }

        static void Emblem(Transform parent)
        {
            var band = Rect("Character emblem", parent); FixedHeight(band, 86);
            var diamond = Rect("Emblem rim", band);
            diamond.anchorMin = diamond.anchorMax = diamond.pivot = new Vector2(.5f, .5f);
            diamond.sizeDelta = new Vector2(48, 48); diamond.localRotation = Quaternion.Euler(0, 0, 45);
            var rim = diamond.gameObject.AddComponent<Image>(); rim.color = Gold; rim.raycastTarget = false;
            var inset = Rect("Emblem inset", diamond); Fill(inset, 2);
            var fill = inset.gameObject.AddComponent<Image>(); fill.color = Panel; fill.raycastTarget = false;
            var center = Rect("Emblem center", diamond); Fill(center, 17);
            var middle = center.gameObject.AddComponent<Image>(); middle.color = Gold; middle.raycastTarget = false;
        }

        static Button Tab(string name, Transform parent, Font font, string label, int index, MenuUI menu)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(index / 3f, 0); rect.anchorMax = new Vector2((index + 1) / 3f, 1);
            rect.offsetMin = new Vector2(index == 0 ? 0 : 4, 0); rect.offsetMax = new Vector2(index == 2 ? 0 : -4, 0);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white; image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = index == 0 ? menu.buttonSelected : menu.buttonNormal;
            colors.highlightedColor = colors.selectedColor = menu.buttonSelected;
            colors.pressedColor = menu.buttonPressed; colors.disabledColor = menu.buttonDisabled;
            button.colors = colors;
            var text = Text("Label", rect, font, 22, index == 0 ? menu.accent : menu.unselectedTabText, label);
            Fill(text.rectTransform, 8); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static RectTransform Row(string name, Transform parent, float height)
        {
            var row = Rect(name, parent); FixedHeight(row, height);
            var group = row.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 8;
            group.childControlHeight = group.childControlWidth = group.childForceExpandWidth = group.childForceExpandHeight = true;
            return row;
        }
        static RectTransform Column(string name, Transform parent, float spacing, int padding)
        {
            var rect = Rect(name, parent); Stack(rect.gameObject.AddComponent<VerticalLayoutGroup>(), spacing, padding); return rect;
        }
        static void Stack(VerticalLayoutGroup group, float spacing, int padding)
        {
            group.spacing = spacing; group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlHeight = group.childControlWidth = group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
        }
        static Text Text(string name, Transform parent, Font font, int size, Color color, string initial = "")
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color; text.text = initial;
            text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate; text.supportRichText = false; text.raycastTarget = false;
            return text;
        }
        static void Divider(string name, Transform parent, Color color)
        {
            var line = Rect(name, parent); FixedHeight(line, 1);
            var image = line.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent) rect.SetParent(parent, false); return rect;
        }
        static void Fill(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        }
        static void TopContent(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        static void FixedHeight(RectTransform rect, float value)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = value; element.flexibleHeight = 0; element.flexibleWidth = 1;
        }
        static void MinimumHeight(RectTransform rect, float value)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = value; element.flexibleHeight = 0; element.flexibleWidth = 1;
        }
    }
}
