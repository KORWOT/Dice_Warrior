using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // One-time authoring only. Existing originals and display mappings are never reset by this command.
    public static class UiPrototypeAuthoring
    {
        public const string CommonPath="Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";
        public const string NodePath="Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab";
        public const string ActionPath="Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab";
        public const string FatePath="Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab";
        public const string VisualPath="Assets/_Project/Features/Run/Configs/DefaultFateDiceVisuals.asset";
        public const string ProbePath="Assets/_Project/Shared/UI/Art/UiMappingProbe.asset";

        [MenuItem("Fate Dice/Create and connect reusable UI (new assets only)")]
        public static string CreateAndConnect()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring.");
            if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("An open scene is dirty. Save your scene before authoring.");
            var config=AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            var data=config.Snapshot();var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var common=AssetDatabase.LoadAssetAtPath<CommonButtonView>(CommonPath);
            if(!common)common=CreateCommon(font,data.presentation);
            if(!AssetDatabase.LoadAssetAtPath<ExplorationNodeView>(NodePath))CreateNode(common);
            if(!AssetDatabase.LoadAssetAtPath<ActionCardView>(ActionPath))CreateAction(common,font,data.presentation);
            if(!AssetDatabase.LoadAssetAtPath<FateCardView>(FatePath))CreateFate(common,font,data.presentation);
            var catalog=AssetDatabase.LoadAssetAtPath<FateDiceVisualCatalog>(VisualPath);
            if(!catalog)
            {
                Folder(VisualPath);catalog=ScriptableObject.CreateInstance<FateDiceVisualCatalog>();
                catalog.actions=data.combat.actions.Select(x=>new ActionVisualEntry{contentId=x.id,visual=new VisualArtwork{fallbackGlyph=x.label.Substring(0,1),tint=data.presentation.accent}}).ToArray();
                catalog.events=data.world.events.Select(x=>new EventVisualEntry{contentId=x.id,visual=new VisualArtwork{fallbackGlyph=x.type.ToString().Substring(0,1),tint=data.presentation.accent}}).ToArray();
                catalog.nodes=Enum.GetValues(typeof(NodeType)).Cast<NodeType>().Select(x=>new NodeVisualEntry{type=x,visual=new VisualArtwork{fallbackGlyph=Glyph(x),tint=data.presentation.accent}}).ToArray();
                catalog.fates=Enum.GetValues(typeof(NodeType)).Cast<NodeType>().Select(x=>new NodeVisualEntry{type=x,visual=new VisualArtwork{fallbackGlyph=Glyph(x),tint=Color.white}}).ToArray();
                catalog.grades=Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(x=>new GradeVisualEntry{grade=x}).ToArray();
                catalog.buttons=Enum.GetValues(typeof(ButtonPurpose)).Cast<ButtonPurpose>().Select(x=>new ButtonAppearance{
                    purpose=x,normal=data.presentation.panel,selected=new Color(.12f,.3f,.3f),pressed=new Color(.18f,.4f,.42f),disabled=new Color(.07f,.09f,.13f)}).ToArray();
                catalog.Validate(data);AssetDatabase.CreateAsset(catalog,VisualPath);
            }
            catalog.Validate(data);
            var scene=EditorSceneManager.OpenScene(PrototypeAuthoring.ScenePath,OpenSceneMode.Single);
            var screen=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<FateDiceScreen>(true)).Single();
            screen.visuals=catalog;screen.uiPrefabs=LoadReferences();
            EditorUtility.SetDirty(screen);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Four reusable originals and visual catalog connected to "+scene.path;
        }
        public static UiPrefabReferences LoadReferences()=>new UiPrefabReferences{
            commonButton=AssetDatabase.LoadAssetAtPath<CommonButtonView>(CommonPath),
            explorationNode=AssetDatabase.LoadAssetAtPath<ExplorationNodeView>(NodePath),
            actionCard=AssetDatabase.LoadAssetAtPath<ActionCardView>(ActionPath),
            fateCard=AssetDatabase.LoadAssetAtPath<FateCardView>(FatePath)};

        private static CommonButtonView CreateCommon(Font font,PresentationSettings style)
        {
            var root=Rect("CommonButtonView",null);Height(root,style.buttonHeight);
            try
            {
                var view=root.gameObject.AddComponent<CommonButtonView>();
                view.group=root.gameObject.AddComponent<CanvasGroup>();view.button=root.gameObject.AddComponent<Button>();
                view.border=Picture("Border",root);Fill(view.border.rectTransform);
                view.border.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");view.border.type=Image.Type.Sliced;
                view.border.color=Color.clear;
                view.background=Picture("Background",root);Fill(view.background.rectTransform,3);view.background.color=Color.white;
                view.background.raycastTarget=true;view.button.targetGraphic=view.background;
                view.label=Text("Label",root,font,style.bodyFontSize,style.text);Fill(view.label.rectTransform);
                view.label.rectTransform.offsetMin=new Vector2(70,6);view.label.rectTransform.offsetMax=new Vector2(-16,-6);
                var compact=Rect("Compact icon area",root);Left(compact,10,48,10);
                view.icon=Picture("Icon",compact);Fill(view.icon.rectTransform);view.icon.preserveAspect=true;view.icon.enabled=false;
                view.iconFallback=Text("Icon fallback",compact,font,27,style.accent);Fill(view.iconFallback.rectTransform);
                view.iconFallback.alignment=TextAnchor.MiddleCenter;
                Folder(CommonPath);return PrefabUtility.SaveAsPrefabAsset(root.gameObject,CommonPath).GetComponent<CommonButtonView>();
            }
            finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        }
        private static void CreateNode(CommonButtonView common)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(common.gameObject);
            try
            {
                root.name="ExplorationNodeView";var view=root.AddComponent<ExplorationNodeView>();view.frame=root.GetComponent<CommonButtonView>();
                Folder(NodePath);PrefabUtility.SaveAsPrefabAsset(root,NodePath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void CreateAction(CommonButtonView common,Font font,PresentationSettings style)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(common.gameObject);
            try
            {
                root.name="ActionCardView";Height((RectTransform)root.transform,168);
                var view=root.AddComponent<ActionCardView>();view.frame=root.GetComponent<CommonButtonView>();
                CardFrame(view.frame);
                view.artwork=Picture("Artwork",root.transform);Left(view.artwork.rectTransform,12,106,22);view.artwork.preserveAspect=true;
                view.artworkFallback=Text("Artwork fallback",root.transform,font,38,style.accent);Left(view.artworkFallback.rectTransform,12,106,22);view.artworkFallback.alignment=TextAnchor.MiddleCenter;
                view.gradeBadge=Picture("Grade badge",root.transform);TopRight(view.gradeBadge.rectTransform,12,12,28,28);view.gradeBadge.preserveAspect=true;
                view.gradeLabel=Text("Offered grade",root.transform,font,21,style.text);Band(view.gradeLabel.rectTransform,130,44,28,16);
                view.effectLabel=Text("Actual effect",root.transform,font,22,style.text);Band(view.effectLabel.rectTransform,130,77,49,16);
                view.tagsLabel=Text("Tags",root.transform,font,19,style.accent);Band(view.tagsLabel.rectTransform,130,132,28,16);
                Folder(ActionPath);PrefabUtility.SaveAsPrefabAsset(root,ActionPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void CreateFate(CommonButtonView common,Font font,PresentationSettings style)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(common.gameObject);
            try
            {
                root.name="FateCardView";Height((RectTransform)root.transform,120);
                var view=root.AddComponent<FateCardView>();view.frame=root.GetComponent<CommonButtonView>();
                CardFrame(view.frame);Fill(view.frame.label.rectTransform);
                view.frame.label.rectTransform.offsetMin=new Vector2(130,8);view.frame.label.rectTransform.offsetMax=new Vector2(-48,-8);
                view.artwork=Picture("Public artwork",root.transform);Left(view.artwork.rectTransform,12,106,12);view.artwork.preserveAspect=true;
                view.artworkFallback=Text("Public artwork fallback",root.transform,font,38,style.accent);Left(view.artworkFallback.rectTransform,12,106,12);view.artworkFallback.alignment=TextAnchor.MiddleCenter;
                view.gradeBadge=Picture("Grade badge",root.transform);TopRight(view.gradeBadge.rectTransform,12,12,28,28);view.gradeBadge.preserveAspect=true;
                Folder(FatePath);PrefabUtility.SaveAsPrefabAsset(root,FatePath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void CardFrame(CommonButtonView frame)
        {
            frame.icon.transform.parent.gameObject.SetActive(false);
            Band(frame.label.rectTransform,130,10,32,48);
        }

        // Reuses an existing project image without changing its importer or original bytes.
        public static string CreateMappingProbe()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Create probe outside Play.");
            if(AssetDatabase.LoadMainAssetAtPath(ProbePath))return ProbePath;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TutorialInfo/Icons/URP.png");
            if(!texture)throw new InvalidOperationException("The existing temporary URP image is unavailable.");
            Folder(ProbePath);
            var wide=Sprite.Create(texture,new Rect(0,0,texture.width,Mathf.Max(1,texture.height/2)),new Vector2(.5f,.5f));
            wide.name="Existing image wide probe";AssetDatabase.CreateAsset(wide,ProbePath);
            var tall=Sprite.Create(texture,new Rect(0,0,Mathf.Max(1,texture.width/2),texture.height),new Vector2(.5f,.5f));
            tall.name="Existing image tall probe";AssetDatabase.AddObjectToAsset(tall,ProbePath);
            EditorUtility.SetDirty(wide);AssetDatabase.SaveAssetIfDirty(wide);AssetDatabase.ImportAsset(ProbePath);
            return ProbePath;
        }
        private static string Glyph(NodeType type)
        {
            switch(type){case NodeType.Combat:return "X";case NodeType.Event:return "?";case NodeType.Treasure:return "$";case NodeType.Shop:return "+";case NodeType.Rest:return "Z";default:return "*";}
        }
        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            if(parent)rect.SetParent(parent,false);return rect;
        }
        private static Image Picture(string name,Transform parent){var image=Rect(name,parent).gameObject.AddComponent<Image>();image.raycastTarget=false;return image;}
        private static Text Text(string name,Transform parent,Font font,int size,Color color)
        {
            var text=Rect(name,parent).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.color=color;
            text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.supportRichText=false;text.raycastTarget=false;return text;
        }
        private static void Fill(RectTransform rect,float inset=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*inset;rect.offsetMax=-Vector2.one*inset;}
        private static void Left(RectTransform rect,float left,float width,float inset)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(0,1);rect.offsetMin=new Vector2(left,inset);rect.offsetMax=new Vector2(left+width,-inset);
        }
        private static void Band(RectTransform rect,float left,float top,float height,float right)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,-top-height);rect.offsetMax=new Vector2(-right,-top);
        }
        private static void TopRight(RectTransform rect,float right,float top,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(-right-width,-top-height);rect.offsetMax=new Vector2(-right,-top);
        }
        private static void Height(RectTransform rect,float height)
        {
            var layout=rect.GetComponent<LayoutElement>()??rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight=layout.minHeight=height;layout.flexibleHeight=0;layout.flexibleWidth=1;
        }
        private static void Folder(string assetPath)
        {
            var parts=assetPath.Split('/');var current=parts[0];
            for(var i=1;i<parts.Length-1;i++){var next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}
        }
    }
}
