using System;
using System.IO;
using FateDice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceWarrior.SymbolLoop.Editor
{
    public static class SymbolLoopAuthoring
    {
        public const string ScenePath="Assets/_Project/Scenes/Title.unity";
        public const string PrefabPath="Assets/_Project/Features/SymbolLoop/Prefabs/SymbolLoopScreen.prefab";
        public const string ConfigPath="Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset";
        static Font font;
        [MenuItem("Dice Warrior/Build Symbol Loop")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play first");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf");
            if(!font) throw new InvalidOperationException("Missing authored Korean font");
            var config=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>(ConfigPath);
            if(!config) { config=ScriptableObject.CreateInstance<SymbolLoopConfig>(); AssetDatabase.CreateAsset(config,ConfigPath); }
            LoopRules.ValidateRules(config.rules);
            var go=new GameObject("Symbol Loop Screen",typeof(RectTransform),typeof(CanvasGroup),typeof(Image),typeof(SymbolLoopScreen));
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color=new Color(.035f,.065f,.10f);
            var screen=go.GetComponent<SymbolLoopScreen>(); screen.group=go.GetComponent<CanvasGroup>();
            var frame=new GameObject("Portrait frame",typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(go.transform,false); frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);
            frame.sizeDelta=new Vector2(720,1280); screen.content=frame;
            screen.title=Text(frame,"Title","DICE WARRIOR",32,30,24,660,48,TextAnchor.MiddleLeft);
            screen.title.color=new Color(.88f,.78f,.52f);
            screen.subtitle=Text(frame,"Subtitle","",20,30,78,660,32,TextAnchor.MiddleLeft);
            var statPanel=Panel(frame,"Stats Panel",30,112,660,72,new Color(.075f,.13f,.19f));
            screen.stats=Text(statPanel,"Stats","",20,18,4,624,64,TextAnchor.MiddleLeft);
            screen.focus=Text(frame,"Focus","",38,70,240,580,300,TextAnchor.MiddleCenter);
            screen.focus.color=new Color(.66f,.83f,.81f);
            var playback=go.AddComponent<SymbolLoopPlayback>(); screen.playback=playback;
            var board=Panel(frame,"Loop Board",30,200,660,420,new Color(.045f,.09f,.13f)); screen.board=board.gameObject;
            Panel(board,"Board inlay",174,112,312,196,new Color(.06f,.13f,.17f));
            screen.center=Text(board,"Region","",29,188,160,284,100,TextAnchor.MiddleCenter);
            screen.center.color=new Color(.9f,.78f,.5f);
            Text(board,"Route","시계 방향 · 출발점에서 완주",13,176,280,308,24,TextAnchor.MiddleCenter);
            screen.tiles=new Text[12]; screen.tileFrames=new Image[12]; screen.tileIcons=new LoopGlyphGraphic[12];
            playback.cells=new RectTransform[12];
            int[] columns={0,0,0,0,1,2,3,3,3,3,2,1};
            int[] rows={3,2,1,0,0,0,0,1,2,3,3,3};
            for(int i=0;i<12;i++)
            {
                var tile=Panel(board,"Tile "+i,10+columns[i]*162,10+rows[i]*102,154,90,new Color(.13f,.20f,.28f));
                playback.cells[i]=tile; screen.tileFrames[i]=tile.GetComponent<Image>();
                screen.tileIcons[i]=Glyph(tile,"Tile icon",LoopGlyph.Gem,12,9,36,36);
                screen.tiles[i]=Text(tile,"Label",i.ToString(),17,12,45,130,42,TextAnchor.MiddleLeft);
            }
            var shadow=Panel(board,"Pawn shadow",0,0,44,12,new Color(0,0,0,.3f));
            shadow.gameObject.SetActive(false);
            var piece=Glyph(board,"Warrior pawn",LoopGlyph.Pawn,0,0,48,60); piece.color=new Color(1,.84f,.43f);
            piece.rectTransform.pivot=new Vector2(.5f,.5f); playback.pawn=piece.rectTransform;
            var stage=Panel(frame,"Combat stage",30,200,660,420,new Color(.075f,.09f,.15f)); playback.combatStage=stage.gameObject;
            Text(stage,"Battle heading","전사  /  수호자",17,20,10,620,30,TextAnchor.MiddleCenter);
            playback.playerHp=Text(stage,"Player HP","",17,24,44,270,30,TextAnchor.MiddleLeft);
            playback.enemyHp=Text(stage,"Enemy HP","",17,366,44,270,30,TextAnchor.MiddleRight);
            playback.playerFill=HealthBar(stage,"Player bar",24,77,new Color(.25f,.85f,.68f));
            playback.enemyFill=HealthBar(stage,"Enemy bar",366,77,new Color(.94f,.4f,.42f));
            playback.playerShield=Text(stage,"Player shield","",16,24,95,270,28,TextAnchor.MiddleLeft);
            playback.enemyShield=Text(stage,"Enemy shield","",16,366,95,270,28,TextAnchor.MiddleRight);
            Panel(stage,"Player platform",52,314,176,7,new Color(.15f,.4f,.42f));
            Panel(stage,"Enemy platform",432,314,176,7,new Color(.43f,.23f,.33f));
            playback.hero=Glyph(stage,"Warrior",LoopGlyph.Pawn,76,176,128,140).rectTransform;
            playback.foe=Glyph(stage,"Guardian",LoopGlyph.Enemy,466,176,128,140).rectTransform;
            playback.foe.GetComponent<LoopGlyphGraphic>().color=new Color(.96f,.45f,.5f);
            playback.effectGlyph=Glyph(stage,"Skill effect",LoopGlyph.Sword,270,186,120,120);
            playback.effect=playback.effectGlyph.rectTransform; playback.effect.gameObject.SetActive(false);
            playback.skillName=Text(stage,"Skill name","",24,20,128,620,40,TextAnchor.MiddleCenter);
            playback.impact=Text(stage,"Impact","",28,20,346,620,54,TextAnchor.MiddleCenter);
            playback.cue=Text(frame,"Playback cue","",21,30,633,660,38,TextAnchor.MiddleCenter);
            playback.cue.color=new Color(1,.82f,.43f);
            var tray=Panel(frame,"Dice tray",30,681,660,176,new Color(.065f,.12f,.18f)); playback.tray=tray.gameObject;
            playback.rollLabel=Text(tray,"Tray label","",14,20,5,620,26,TextAnchor.MiddleCenter);
            playback.numericDie=Die(tray,"Travel D6",272,30);
            playback.symbolDice=new LoopDieGraphic[3]; screen.dice=new Text[3];
            for(int i=0;i<3;i++)
            {
                playback.symbolDice[i]=Die(tray,"Symbol die "+i,100+i*172,30);
                screen.dice[i]=Text(tray,"Die caption "+i,"",16,80+i*172,141,156,28,TextAnchor.MiddleCenter);
            }
            playback.dieLabels=screen.dice;
            screen.detail=Text(frame,"Details","",17,32,871,656,72,TextAnchor.UpperLeft);
            screen.history=Text(frame,"Resolved history","",15,32,945,656,68,TextAnchor.UpperLeft);
            screen.history.color=new Color(.61f,.76f,.78f);
            var actions=new GameObject("Actions",typeof(RectTransform)).GetComponent<RectTransform>();
            actions.SetParent(frame,false);
            actions.anchorMin=actions.anchorMax=new Vector2(.5f,0); actions.pivot=new Vector2(.5f,0);
            actions.anchoredPosition=new Vector2(0,40); actions.sizeDelta=new Vector2(660,224);
            var buttonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab");
            screen.buttons=new CommonButtonView[8];
            for(int i=0;i<8;i++)
            {
                var button=(GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab,actions);
                button.name="Action "+i;
                SetRect((RectTransform)button.transform,(i%2)*338,(i/2)*56,322,50);
                var view=button.GetComponent<CommonButtonView>(); view.label.font=font; view.label.fontSize=17;
                view.label.resizeTextForBestFit=true; view.label.resizeTextMinSize=15; view.label.resizeTextMaxSize=17;
                view.label.alignment=TextAnchor.MiddleCenter;
                Stretch(view.label.rectTransform); view.label.rectTransform.offsetMin=new Vector2(10,4); view.label.rectTransform.offsetMax=new Vector2(-10,-4);
                screen.buttons[i]=view;
            }
            var footer=Text(frame,"Footer","전체 조합 1개 · 없으면 눈마다 기본기",15,30,1248,660,24,TextAnchor.MiddleCenter);
            footer.rectTransform.anchorMin=footer.rectTransform.anchorMax=new Vector2(0,0);
            footer.rectTransform.anchoredPosition=new Vector2(30,30);
            footer.color=new Color(.45f,.58f,.62f);
            PrefabUtility.SaveAsPrefabAsset(go,PrefabPath); UnityEngine.Object.DestroyImmediate(go);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var rootPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(rootPrefab,scene);
            var manager=root.GetComponentInChildren<UIManager>(true);
            manager.prefabs=new BaseUI[] {AssetDatabase.LoadAssetAtPath<SymbolLoopScreen>(PrefabPath)};
            var app=new GameObject("Symbol Loop Application").AddComponent<SymbolLoopApp>(); app.ui=manager; app.config=config;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Scene save failed");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssetIfDirty(config);
        }
        static LoopGlyphGraphic Glyph(Transform parent,string name,LoopGlyph glyph,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(LoopGlyphGraphic)); go.transform.SetParent(parent,false);
            SetRect((RectTransform)go.transform,x,y,width,height);
            var graphic=go.GetComponent<LoopGlyphGraphic>(); graphic.glyph=glyph; graphic.color=LoopGlyphGraphic.Tint(glyph); graphic.raycastTarget=false;
            return graphic;
        }
        static LoopDieGraphic Die(Transform parent,string name,float x,float y)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(LoopDieGraphic)); go.transform.SetParent(parent,false);
            SetRect((RectTransform)go.transform,x,y,116,116); var die=go.GetComponent<LoopDieGraphic>(); die.raycastTarget=false; return die;
        }
        static Image HealthBar(Transform parent,string name,float x,float y,Color color)
        {
            var back=Panel(parent,name+" track",x,y,270,10,new Color(.03f,.04f,.08f));
            var fill=Panel(back,name,0,0,270,10,color).GetComponent<Image>();
            fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillOrigin=0; return fill;
        }
        static RectTransform Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(parent,false); var rect=(RectTransform)go.transform; SetRect(rect,x,y,width,height);
            var image=go.GetComponent<Image>(); image.color=color; image.raycastTarget=false; return rect;
        }
        static Text Text(Transform parent,string name,string value,int size,float x,float y,float width,float height,TextAnchor alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            SetRect((RectTransform)go.transform,x,y,width,height);
            var text=go.GetComponent<Text>(); text.font=font; text.text=value; text.fontSize=size; text.alignment=alignment;
            text.color=new Color(.88f,.92f,.93f); text.raycastTarget=false; text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
