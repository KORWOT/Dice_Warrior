using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // Migrates only the three approved combat prefabs. Runtime views bind the authored layout.
    public static class CombatLayoutAuthoring
    {
        public const string CombatPath = "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab";
        public const string ActionPath = "Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab";
        public const string DiePath = "Assets/_Project/Features/Combat/Prefabs/CombatDie.prefab";
        private const int Version = 1;
        private const float CardHeight = 312;
        private static readonly Color Background = new Color(.025f, .027f, .032f);
        private static readonly Color Panel = new Color(.055f, .059f, .069f);
        private static readonly Color Line = new Color(.23f, .24f, .27f);
        private static readonly Color Ink = new Color(.91f, .90f, .87f);
        private static readonly Color Muted = new Color(.56f, .58f, .62f);
        private static readonly Color Red = new Color(.76f, .18f, .22f);
        private static readonly Color Blue = new Color(.35f, .63f, .91f);

        [MenuItem("Fate Dice/Apply combat layout (once)")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring combat layout.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save all open scenes before authoring combat layout.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before authoring combat layout.");

            var existing = AssetDatabase.LoadAssetAtPath<CombatUI>(CombatPath);
            if (!existing) throw new InvalidOperationException("The existing CombatUI prefab is required.");
            if (existing.layoutVersion >= Version) return "Combat layout version 1 already authored; no assets changed.";
            var common = AssetDatabase.LoadAssetAtPath<CommonButtonView>(UiPrototypeAuthoring.CommonPath);
            if (!common || !common.label || !common.label.font)
                throw new InvalidOperationException("The existing CommonButtonView prefab and its font are required.");
            if (!AssetDatabase.LoadAssetAtPath<ActionCardView>(ActionPath))
                throw new InvalidOperationException("The existing ActionCardView prefab is required.");

            // Prefab contents provide isolated scenes, without an unsaved additive authoring scene.
            var contents = PrefabUtility.LoadPrefabContents(CombatPath);
            try
            {
                var view = contents.GetComponent<CombatUI>();
                if (!view || !view.layout || !view.group)
                    throw new InvalidOperationException("CombatUI must retain its root view, layout and CanvasGroup.");
                var die = CreateDie(common, contents.scene);
                AuthorAction();
                AuthorCombat(view, common, die, common.label.font);
                view.layoutVersion = Version;
                if (!PrefabUtility.SaveAsPrefabAsset(contents, CombatPath))
                    throw new InvalidOperationException("Could not save " + CombatPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return "CombatUI portrait layout, original action cards and CombatDie variant authored (version 1).";
        }

        private static CommonButtonView CreateDie(CommonButtonView common, Scene scene)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(DiePath);
            if (asset)
            {
                var existing = AssetDatabase.LoadAssetAtPath<CommonButtonView>(DiePath);
                if (!existing) throw new InvalidOperationException(DiePath + " exists without CommonButtonView.");
                return existing;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(common.gameObject, scene);
            try
            {
                instance.name = "CombatDie";
                var view = instance.GetComponent<CommonButtonView>();
                Height((RectTransform)instance.transform, 102);
                Fill(view.label.rectTransform, 8);
                view.label.alignment = TextAnchor.MiddleCenter;
                view.label.fontSize = 22;
                view.label.horizontalOverflow = HorizontalWrapMode.Wrap;
                view.label.verticalOverflow = VerticalWrapMode.Truncate;
                view.icon.transform.parent.gameObject.SetActive(false);
                var saved = PrefabUtility.SaveAsPrefabAsset(instance, DiePath);
                if (!saved) throw new InvalidOperationException("Could not save " + DiePath);
                return saved.GetComponent<CommonButtonView>();
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void AuthorAction()
        {
            var contents = PrefabUtility.LoadPrefabContents(ActionPath);
            try
            {
                var view = contents.GetComponent<ActionCardView>();
                if (!view || !view.frame || !view.frame.label || !view.artwork || !view.artworkFallback ||
                    !view.gradeBadge || !view.gradeLabel || !view.effectLabel || !view.tagsLabel)
                    throw new InvalidOperationException("ActionCardView must retain its original frame and image/text slots.");
                var root = (RectTransform)contents.transform;
                Height(root, CardHeight);
                root.sizeDelta = new Vector2(220, CardHeight);
                // Keep the inherited CommonButtonView objects and fontStyle; change only their layout.
                view.frame.icon.transform.parent.gameObject.SetActive(false);
                Band(view.frame.label.rectTransform, 12, 106, 50, 12);
                Format(view.frame.label, 21, TextAnchor.MiddleCenter);
                Band(view.artwork.rectTransform, 12, 10, 90, 12);
                view.artwork.preserveAspect = true;
                view.artwork.raycastTarget = false;
                Band(view.artworkFallback.rectTransform, 12, 10, 90, 12);
                Format(view.artworkFallback, 38, TextAnchor.MiddleCenter);
                Corner(view.gradeBadge.rectTransform, true, true, 14, 14, 24, 24);
                Band(view.gradeLabel.rectTransform, 12, 160, 24, 12);
                Format(view.gradeLabel, 18, TextAnchor.MiddleCenter);
                Band(view.effectLabel.rectTransform, 12, 190, 66, 12);
                Format(view.effectLabel, 19, TextAnchor.UpperLeft);
                Band(view.tagsLabel.rectTransform, 12, 260, 44, 12);
                Format(view.tagsLabel, 18, TextAnchor.UpperLeft);

                var backing = NamedImage("Portrait artwork backing", root, new Color(.025f, .027f, .035f, .72f));
                Band(backing.rectTransform, 9, 8, 94, 9);
                backing.transform.SetSiblingIndex(view.frame.background.transform.GetSiblingIndex() + 1);
                var divider = NamedImage("Portrait effect divider", root, Line);
                Band(divider.rectTransform, 12, 185, 1, 12);
                if (!PrefabUtility.SaveAsPrefabAsset(contents, ActionPath))
                    throw new InvalidOperationException("Could not save " + ActionPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void AuthorCombat(CombatUI view, CommonButtonView common, CommonButtonView die, Font font)
        {
            var root = (RectTransform)view.transform;
            for (var i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            Fill(root);
            var background = root.GetComponent<Image>();
            if (!background) background = root.gameObject.AddComponent<Image>();
            background.color = Background;
            background.raycastTarget = false;
            var stack = root.GetComponent<VerticalLayoutGroup>();
            if (!stack) stack = root.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(16, 16, 16, 16);
            stack.spacing = 8;
            stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            stack.childAlignment = TextAnchor.UpperCenter;
            var layout = view.layout;

            var header = Rect("Battle Header", root);
            Height(header, 64);
            var menuSocket = Rect("Menu socket", header);
            Corner(menuSocket, true, true, 0, 0, 64, 64);
            var menuObject = (GameObject)PrefabUtility.InstantiatePrefab(common.gameObject, menuSocket);
            menuObject.name = "Battle menu";
            view.menuButton = menuObject.GetComponent<CommonButtonView>();
            Fill((RectTransform)menuObject.transform);
            Height((RectTransform)menuObject.transform, 64);
            view.menuButton.icon.transform.parent.gameObject.SetActive(false);
            Fill(view.menuButton.label.rectTransform, 4);
            Format(view.menuButton.label, 17, TextAnchor.MiddleCenter);
            view.menuButton.label.text = "MENU";
            layout.footer = menuSocket;
            layout.header = Label("Battle label", header, font, 25, Ink, TextAnchor.MiddleLeft);
            Band(layout.header.rectTransform, 0, 0, 64, 230);
            view.turn = Label("Run turn", header, font, 18, Muted, TextAnchor.MiddleRight);
            Corner(view.turn.rectTransform, true, true, 76, 0, 145, 64);
            Rule("Header divider", header, true);

            var enemy = Rect("Enemy Health", root);
            Height(enemy, 76);
            view.enemyName = Label("Enemy name", enemy, font, 23, Ink, TextAnchor.MiddleCenter);
            Band(view.enemyName.rectTransform, 100, 0, 30, 100);
            view.enemyShield = Label("Enemy shield", enemy, font, 18, Blue, TextAnchor.MiddleRight);
            Corner(view.enemyShield.rectTransform, true, true, 0, 0, 170, 30);
            var enemyBar = Rect("Enemy HP bar", enemy);
            Band(enemyBar, 100, 38, 30, 100);
            view.enemyHealthFill = HealthBar(enemyBar, font, out view.enemyHealth, 20);

            view.arena = Rect("Arena", root);
            var arenaLayout = view.arena.gameObject.AddComponent<LayoutElement>();
            arenaLayout.minHeight = 250;
            arenaLayout.flexibleHeight = 1;
            var stage = Rect("Combat stage", view.arena);
            Fill(stage);
            view.stageGraphic = stage.gameObject.AddComponent<CombatStageGraphic>();
            view.stageGraphic.raycastTarget = false;
            view.stageGraphic.showFigures = true;

            view.artworkRoot = Rect("Revealed event artwork", view.arena);
            Anchor(view.artworkRoot, new Vector2(.22f, .35f), new Vector2(.70f, .85f));
            view.artwork = Picture("Artwork", view.artworkRoot, Color.white);
            Fill(view.artwork.rectTransform);
            view.artwork.preserveAspect = true;
            view.artworkFallback = Label("Artwork fallback", view.artworkRoot, font, 40, Muted, TextAnchor.MiddleCenter);
            Fill(view.artworkFallback.rectTransform);

            var intent = Rect("Enemy intent", view.arena);
            Corner(intent, true, true, 12, 12, 190, 112);
            var intentBackground = intent.gameObject.AddComponent<Image>();
            intentBackground.color = new Color(.055f, .036f, .043f, .93f);
            intentBackground.raycastTarget = false;
            Rule("Intent top border", intent, false);
            layout.situation = Label("Intent label", intent, font, 19, Muted, TextAnchor.UpperRight);
            Band(layout.situation.rectTransform, 12, 10, 36, 12);
            view.intentValue = Label("Intent value", intent, font, 34, Red, TextAnchor.MiddleRight);
            Band(view.intentValue.rectTransform, 12, 48, 52, 12);

            var player = Rect("Player status", view.arena);
            Corner(player, false, false, 12, 12, 260, 152);
            view.playerName = Label("Player name", player, font, 22, Ink, TextAnchor.MiddleLeft);
            Band(view.playerName.rectTransform, 0, 0, 28, 0);
            var playerBar = Rect("Player HP bar", player);
            Band(playerBar, 0, 34, 28, 0);
            view.playerHealthFill = HealthBar(playerBar, font, out layout.stats, 19);
            view.playerShield = Label("Player shield", player, font, 22, Blue, TextAnchor.MiddleLeft);
            Band(view.playerShield.rectTransform, 0, 67, 28, 0);
            view.playerAttributes = Label("Player attributes", player, font, 18, Muted, TextAnchor.UpperLeft);
            Band(view.playerAttributes.rectTransform, 0, 103, 44, 0);
            layout.gear = Label("Unused gear summary", view.arena, font, 18, Muted, TextAnchor.MiddleLeft);
            layout.gear.gameObject.SetActive(false);

            var dice = Rect("Dice and fate", root);
            Height(dice, 146);
            layout.fate = Label("Hand", dice, font, 20, Ink, TextAnchor.MiddleLeft);
            layout.fate.rectTransform.anchorMin = new Vector2(0, 1);
            layout.fate.rectTransform.anchorMax = new Vector2(.47f, 1);
            layout.fate.rectTransform.offsetMin = new Vector2(0, -36);
            layout.fate.rectTransform.offsetMax = Vector2.zero;
            view.fatePower = Label("Fate power", dice, font, 19, Muted, TextAnchor.MiddleCenter);
            view.fatePower.rectTransform.anchorMin = new Vector2(.47f, 1);
            view.fatePower.rectTransform.anchorMax = new Vector2(.73f, 1);
            view.fatePower.rectTransform.offsetMin = new Vector2(0, -36);
            view.fatePower.rectTransform.offsetMax = Vector2.zero;
            view.rerolls = Label("Rerolls", dice, font, 19, Blue, TextAnchor.MiddleRight);
            view.rerolls.rectTransform.anchorMin = new Vector2(.73f, 1);
            view.rerolls.rectTransform.anchorMax = Vector2.one;
            view.rerolls.rectTransform.offsetMin = new Vector2(0, -36);
            view.rerolls.rectTransform.offsetMax = Vector2.zero;
            layout.diceRow = Rect("Dice", dice);
            Band(layout.diceRow, 0, 44, 102, 0);
            Height(layout.diceRow, 102);
            var diceLayout = layout.diceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            diceLayout.spacing = 8;
            diceLayout.childControlWidth = diceLayout.childControlHeight = true;
            diceLayout.childForceExpandWidth = diceLayout.childForceExpandHeight = true;
            view.diePrefab = die;

            var actions = Rect("Actions", root);
            Height(actions, CardHeight);
            var scrollRoot = Rect("Action scroll", actions);
            Fill(scrollRoot);
            view.actionScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            view.actionScroll.horizontal = true;
            view.actionScroll.vertical = false;
            view.actionScroll.movementType = ScrollRect.MovementType.Clamped;
            view.actionScroll.scrollSensitivity = 30;
            var viewport = Rect("Viewport", scrollRoot);
            Fill(viewport);
            var surface = viewport.gameObject.AddComponent<Image>();
            surface.color = Panel;
            surface.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            view.actionChoices = Rect("Action cards", viewport);
            view.actionChoices.anchorMin = Vector2.zero;
            view.actionChoices.anchorMax = new Vector2(0, 1);
            view.actionChoices.pivot = new Vector2(0, 1);
            view.actionChoices.offsetMin = view.actionChoices.offsetMax = Vector2.zero;
            view.actionGrid = view.actionChoices.gameObject.AddComponent<GridLayoutGroup>();
            view.actionGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            view.actionGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            view.actionGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            view.actionGrid.constraintCount = 1;
            view.actionGrid.cellSize = new Vector2(220, CardHeight);
            view.actionGrid.spacing = new Vector2(10, 0);
            view.actionGrid.childAlignment = TextAnchor.UpperLeft;
            var fitter = view.actionChoices.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            view.actionScroll.viewport = viewport;
            view.actionScroll.content = view.actionChoices;
            layout.scroll = view.actionScroll;
            layout.body = view.actionChoices;
            view.rollChoices = Rect("Roll choices", actions);
            Band(view.rollChoices, 0, 105, 102, 0);
            var rollLayout = view.rollChoices.gameObject.AddComponent<VerticalLayoutGroup>();
            rollLayout.childControlWidth = rollLayout.childControlHeight = rollLayout.childForceExpandWidth = true;
            rollLayout.childForceExpandHeight = false;

            layout.notice = Label("Notice", root, font, 19, Muted, TextAnchor.UpperLeft);
            Height(layout.notice.rectTransform, 64);
        }

        private static Image HealthBar(RectTransform root, Font font, out Text value, int fontSize)
        {
            var edge = root.gameObject.AddComponent<Image>();
            edge.color = Line; edge.raycastTarget = false;
            var track = Picture("Track", root, new Color(.17f, .045f, .055f));
            Fill(track.rectTransform, 2);
            var fill = Picture("Fill", track.transform, Red);
            Fill(fill.rectTransform);
            value = Label("Value", root, font, fontSize, Ink, TextAnchor.MiddleCenter);
            Fill(value.rectTransform, 3);
            return fill;
        }

        private static void Rule(string name, Transform parent, bool bottom)
        {
            var rule = Picture(name, parent, Line);
            if (bottom)
            {
                rule.rectTransform.anchorMin = Vector2.zero;
                rule.rectTransform.anchorMax = new Vector2(1, 0);
                rule.rectTransform.offsetMin = Vector2.zero;
                rule.rectTransform.offsetMax = new Vector2(0, 1);
            }
            else Band(rule.rectTransform, 0, 0, 1, 0);
        }

        private static Text Label(string name, Transform parent, Font font, int size, Color color, TextAnchor alignment)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.color = color;
            Format(text, size, alignment);
            text.supportRichText = false;
            return text;
        }

        private static void Format(Text text, int size, TextAnchor alignment)
        {
            text.fontSize = size;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static Image NamedImage(string name, RectTransform parent, Color color)
        {
            var existing = parent.Find(name);
            var image = existing ? existing.GetComponent<Image>() : Picture(name, parent, color);
            if (!image) throw new InvalidOperationException(name + " exists without its expected Image.");
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private static Image Picture(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Height(RectTransform rect, float height)
        {
            var layout = rect.GetComponent<LayoutElement>();
            if (!layout) layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
            layout.flexibleHeight = 0;
            layout.flexibleWidth = 1;
        }

        private static void Fill(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
        }

        private static void Anchor(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Band(RectTransform rect, float left, float top, float height, float right)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Corner(RectTransform rect, bool right, bool top, float x, float y, float width, float height)
        {
            var point = new Vector2(right ? 1 : 0, top ? 1 : 0);
            rect.anchorMin = rect.anchorMax = point;
            rect.pivot = point;
            rect.anchoredPosition = new Vector2(right ? -x : x, top ? -y : y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
