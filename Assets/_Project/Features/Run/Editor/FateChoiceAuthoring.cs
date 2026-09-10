using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    public static class FateChoiceAuthoring
    {
        public const string PopupPath = "Assets/_Project/Features/Fate/Prefabs/FateChoiceUI.prefab";
        const string CardPath = "Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab";
        const string RootPath = "Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab";
        const string ButtonPath = "Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";

        [MenuItem("Fate Dice/운명 선택 팝업 원본 적용")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("플레이와 미리보기를 닫고 가져오기가 끝난 뒤 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 먼저 저장하세요.");
            var common = AssetDatabase.LoadAssetAtPath<CommonButtonView>(ButtonPath);
            var card = AssetDatabase.LoadAssetAtPath<FateCardView>(CardPath);
            if (!common || !common.label || !common.label.font || !card || !card.frame || !card.frame.label)
                throw new InvalidOperationException("기존 공통 버튼과 운명 카드, 한글 폰트가 필요합니다.");
            int changed = 0;
            if (card.choicePresentationVersion < 1) { AuthorCard(); changed++; }
            if (AssetDatabase.LoadAssetAtPath<FateCardView>(CardPath).choicePresentationVersion < 2) { AuthorCardOutline(); changed++; }
            var popup = AssetDatabase.LoadAssetAtPath<FateChoiceUI>(PopupPath);
            if (!popup) { CreatePopup(common); changed++; popup = AssetDatabase.LoadAssetAtPath<FateChoiceUI>(PopupPath); }
            var root = PrefabUtility.LoadPrefabContents(RootPath);
            try
            {
                var manager = root.GetComponent<UIManager>();
                if (!manager || manager.prefabs == null) throw new InvalidOperationException("UIRoot의 명시적 prefab 등록이 필요합니다.");
                if (!manager.prefabs.Any(value => value is FateChoiceUI))
                {
                    manager.prefabs = manager.prefabs.Concat(new BaseUI[] { popup }).ToArray();
                    Save(root, RootPath); changed++;
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return changed == 0 ? "운명 팝업 원본 적용됨. 사용자 편집을 유지합니다." : "운명 카드·선택 팝업·등록 적용: " + changed;
        }

        static void AuthorCard()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                var card = root.GetComponent<FateCardView>();
                var frame = card.frame;
                if (!frame.GetComponent<SelectionFeedback>())
                    throw new InvalidOperationException("기존 운명 카드의 FEEL SelectionFeedback 연결을 먼저 확인하세요.");
                var font = frame.label.font;
                Place((RectTransform)root.transform, Vector2.zero, new Vector2(190, 430));
                var element = root.GetComponent<LayoutElement>();
                if (!element) element = root.AddComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = 190;
                element.minHeight = element.preferredHeight = 430;
                element.flexibleWidth = element.flexibleHeight = 0;
                frame.background.color = new Color(.022f, .026f, .039f, 1);
                frame.icon.transform.parent.gameObject.SetActive(false);
                frame.label.gameObject.SetActive(false);
                card.typeGlow = Rect("유형의 빛", root.transform, Vector2.zero, new Vector2(182, 418)).gameObject.AddComponent<Image>();
                card.typeGlow.color = new Color(.35f,.55f,1,.15f); card.typeGlow.raycastTarget = false;
                card.typeGlow.transform.SetSiblingIndex(1);
                ReparentPlace(card.artwork.rectTransform, root.transform, new Vector2(0, 16), new Vector2(170, 218));
                card.artwork.preserveAspect = true; card.artwork.raycastTarget = false;
                ReparentPlace(card.artworkFallback.rectTransform, root.transform, new Vector2(0, 16), new Vector2(170, 218));
                card.artworkFallback.fontSize = 64; card.artworkFallback.alignment = TextAnchor.MiddleCenter;
                ReparentPlace(card.gradeBadge.rectTransform, root.transform, new Vector2(65, 148), new Vector2(28, 28));
                card.typeLabel = Label("운명 유형", root.transform, font, 27, new Vector2(0, 176), new Vector2(174, 44));
                card.gradeLabel = Label("운명 등급", root.transform, font, 22, new Vector2(0, 134), new Vector2(160, 34));
                card.description = Label("공개 유형 설명", root.transform, font, 20, new Vector2(0, -146), new Vector2(168, 98));
                card.description.color = new Color(.77f,.79f,.83f);
                card.selectionBorder = Rect("금색 선택 테두리", root.transform, Vector2.zero, new Vector2(190,430)).gameObject.AddComponent<Image>();
                card.selectionBorder.sprite = frame.border.sprite;
                card.selectionBorder.type = Image.Type.Sliced;
                card.selectionBorder.color = new Color(1,.81f,.34f);
                card.selectionBorder.raycastTarget = false;
                if (!card.selectionBorder.sprite)
                {
                    card.selectionBorder.color = new Color(1,.81f,.34f,.045f);
                    var line = card.selectionBorder.gameObject.AddComponent<Outline>();
                    line.effectColor = new Color(1,.81f,.34f); line.effectDistance = new Vector2(2,-2); line.useGraphicAlpha = false;
                }
                card.selectionBorder.gameObject.SetActive(false);
                card.choicePresentationVersion = 1;
                Save(root, CardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AuthorCardOutline()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                var card = root.GetComponent<FateCardView>();
                // A sliced border must leave its center transparent, or it hides the selected card's text.
                card.selectionBorder.fillCenter = false;
                if (!card.typeSymbol)
                    card.typeSymbol = Rect("운명 유형 도형", root.transform, new Vector2(0,16), new Vector2(156,156))
                        .gameObject.AddComponent<MapNodeGraphic>();
                card.typeSymbol.raycastTarget = false;
                card.typeSymbol.transform.SetSiblingIndex(card.selectionBorder.transform.GetSiblingIndex());
                card.choicePresentationVersion = 2;
                Save(root, CardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void CreatePopup(CommonButtonView common)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject go = null;
            try
            {
                go = new GameObject("FateChoiceUI", typeof(RectTransform), typeof(CanvasGroup));
                SceneManager.MoveGameObjectToScene(go, scene);
                Fill((RectTransform)go.transform);
                go.AddComponent<Image>().color = new Color(0,0,0,.9f);
                var view = go.AddComponent<FateChoiceUI>();
                view.group = go.GetComponent<CanvasGroup>(); view.layoutVersion = 1;
                view.cardPrefab = AssetDatabase.LoadAssetAtPath<FateCardView>(CardPath);
                view.panel = Rect("운명 선택 창", go.transform, Vector2.zero, new Vector2(664,1040));
                view.panel.gameObject.AddComponent<Image>().color = new Color(.013f,.017f,.026f,.98f);
                var edge = view.panel.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.48f,.39f,.21f); edge.effectDistance = new Vector2(2,-2);
                var font = common.label.font;
                view.title = Label("제목", view.panel, font, 39, new Vector2(-12,439), new Vector2(520,70));
                view.title.text = "운명을 선택하세요"; view.title.color = new Color(1,.87f,.6f);
                view.status = Label("운명 상태", view.panel, font, 23, new Vector2(0,370), new Vector2(600,58));
                view.status.text = "카드를 선택한 뒤 아래 버튼으로 진행하세요";
                var scrollRoot = Rect("운명 카드 목록", view.panel, new Vector2(0,75), new Vector2(628,476));
                view.cardScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
                var viewport = Rect("Viewport", scrollRoot, Vector2.zero, new Vector2(628,476));
                viewport.gameObject.AddComponent<RectMask2D>();
                view.cardContent = Rect("Cards", viewport, Vector2.zero, new Vector2(0,456));
                view.cardContent.anchorMin = view.cardContent.anchorMax = new Vector2(0,.5f);
                view.cardContent.pivot = new Vector2(0,.5f);
                var row = view.cardContent.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.spacing = 16; row.padding = new RectOffset(10,10,12,12); row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;
                var fit = view.cardContent.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                view.cardScroll.viewport = viewport; view.cardScroll.content = view.cardContent;
                view.cardScroll.horizontal = true; view.cardScroll.vertical = false;
                view.cardScroll.movementType = ScrollRect.MovementType.Clamped;
                view.instructions = Label("선택 안내", view.panel, font, 23, new Vector2(0,-203), new Vector2(596,66));
                view.instructions.text = "카드를 살펴보고 하나를 선택하세요";
                view.diceRow = Rect("주사위 재굴림", view.panel, new Vector2(0,-313), new Vector2(568,106));
                Label("재굴림 안내", view.diceRow, font, 20, new Vector2(0,64), new Vector2(560,32)).text = "주사위를 눌러 하나만 다시 굴릴 수 있습니다";
                view.rerollButtons = new CommonButtonView[6]; view.rerollFaces = new DiceFaceView[6];
                for (int i = 0; i < 6; i++)
                {
                    var button = Button(common, view.diceRow, "주사위 " + (i + 1), new Vector2((i-2.5f)*92,0), new Vector2(80,80));
                    view.rerollButtons[i] = button;
                    var face = Rect("확정 눈", button.transform, Vector2.zero, new Vector2(70,70)).gameObject.AddComponent<DiceFaceView>();
                    if (!face.GetComponent<CanvasRenderer>()) face.gameObject.AddComponent<CanvasRenderer>();
                    face.raycastTarget = false; view.rerollFaces[i] = face;
                    button.label.gameObject.SetActive(false);
                }
                view.confirmButton = Button(common, view.panel, "선택 실행", new Vector2(0,-435), new Vector2(590,82));
                view.confirmButton.label.text = "선택한 운명으로 진행";
                view.closeButton = Button(common, view.panel, "닫기", new Vector2(280,470), new Vector2(56,56));
                view.closeButton.label.text = "×";
                Save(go, PopupPath);
            }
            finally
            {
                if (go) UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static CommonButtonView Button(CommonButtonView source, Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source.gameObject, parent);
            go.name = name;
            var view = go.GetComponent<CommonButtonView>();
            Place((RectTransform)go.transform, position, size);
            var element = go.GetComponent<LayoutElement>(); if (element) element.ignoreLayout = true;
            Fill(view.label.rectTransform); view.label.rectTransform.offsetMin = new Vector2(8,4); view.label.rectTransform.offsetMax = new Vector2(-8,-4);
            view.label.alignment = TextAnchor.MiddleCenter; view.label.fontSize = size.x < 90 ? 28 : 27;
            view.icon.transform.parent.gameObject.SetActive(false);
            return view;
        }
        static Text Label(string name, Transform parent, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            var text = Rect(name,parent,position,dimensions).gameObject.AddComponent<Text>();
            text.font=font; text.fontSize=size; text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(.93f,.93f,.95f); text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); Place(rect,position,size); return rect;
        }
        static void ReparentPlace(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
        { rect.SetParent(parent,false); Place(rect,position,size); }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=position; rect.sizeDelta=size; }
        static void Fill(RectTransform rect)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        static void Save(GameObject root,string path)
        { if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new InvalidOperationException("운명 원본 저장 실패: "+path); }
    }
}
