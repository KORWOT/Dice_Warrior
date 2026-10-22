using System;
using System.IO;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld.Editor
{
    public static class CompetitionAuthoring
    {
        public const string ScenePath="Assets/_Project/Scenes/Title.unity";
        public const string PrefabPath="Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab";
        public const string ContentPath="Assets/_Project/Features/SharedWorld/Content/shared-world-preview-1.json";
        static Font font;
        [MenuItem("Dice Warrior/Build Shared World Main")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play first");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene");
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf");
            var content=AssetDatabase.LoadAssetAtPath<TextAsset>(ContentPath);
            if(!font || !content) throw new InvalidOperationException("Missing authored font or competition content");
            CompetitionContent.Load(content.text);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));AssetDatabase.Refresh();
            var go=new GameObject("Shared World Screen",typeof(RectTransform),typeof(CanvasGroup),typeof(Image),typeof(CompetitionScreen));
            try
            {
                Stretch((RectTransform)go.transform);go.GetComponent<Image>().color=new Color(.035f,.065f,.1f);
                var screen=go.GetComponent<CompetitionScreen>();screen.group=go.GetComponent<CanvasGroup>();
                var frame=new GameObject("Portrait frame",typeof(RectTransform)).GetComponent<RectTransform>();frame.SetParent(go.transform,false);
                frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.sizeDelta=new Vector2(720,1280);screen.content=frame;
                screen.title=Text(frame,"Title","DICE WARRIOR · 공동 모험",29,30,18,660,44);screen.title.color=new Color(.95f,.83f,.55f);
                screen.subtitle=Text(frame,"Subtitle","개발 시험 · 영구 재화 지급 없음",17,30,65,660,28);
                screen.status=Text(frame,"Round status","",18,30,94,660,30);
                var roster=Panel(frame,"Participants",30,126,660,112,new Color(.07f,.12f,.18f));screen.roster=new Text[4];
                for(int i=0;i<4;i++) screen.roster[i]=Text(roster,"Participant "+i,"",16,10,i*27,640,27);
                var board=Panel(frame,"Shared world",30,246,660,374,new Color(.045f,.09f,.13f));screen.board=board.gameObject;
                Panel(board,"World center",174,104,312,166,new Color(.06f,.13f,.17f));
                screen.center=Text(board,"World name","",24,184,112,292,136,TextAnchor.MiddleCenter);
                screen.places=new Text[12];screen.cells=new RectTransform[12];screen.tileFrames=new Image[12];
                int[] columns={0,0,0,0,1,2,3,3,3,3,2,1}, rows={3,2,1,0,0,0,0,1,2,3,3,3};
                for(int i=0;i<12;i++)
                {
                    var tile=Panel(board,"Place "+i,10+columns[i]*162,10+rows[i]*89,154,80,new Color(.1f,.18f,.25f));
                    screen.cells[i]=tile;screen.tileFrames[i]=tile.GetComponent<Image>();
                    Text(tile,"Index",i.ToString("00"),13,8,3,90,20).color=new Color(.45f,.68f,.73f);
                    screen.places[i]=Text(tile,"Place name","",15,8,25,96,50);
                }
                screen.pawns=new RectTransform[4];
                for(int i=0;i<4;i++)
                {
                    var pawn=Glyph(board,"Adventurer "+i,LoopGlyph.Pawn,0,0,22,29);pawn.color=CompetitionScreen.ParticipantColor(i);
                    pawn.rectTransform.pivot=new Vector2(.5f,.5f);screen.pawns[i]=pawn.rectTransform;
                    var number=Text(pawn.transform,"Number",(i+1).ToString(),11,0,-4,22,15,TextAnchor.MiddleCenter);number.color=Color.white;
                }
                var human=go.AddComponent<SymbolLoopPlayback>();human.pawn=screen.pawns[0];human.cells=screen.cells;
                var playback=go.AddComponent<CompetitionPlayback>();playback.screen=screen;playback.human=human;screen.playback=playback;
                var stage=Panel(frame,"Personal combat",30,246,660,374,new Color(.075f,.09f,.15f));human.combatStage=stage.gameObject;
                Text(stage,"Battle heading","당신의 전투 · 다른 모험가의 사건과 독립",17,20,8,620,28,TextAnchor.MiddleCenter);
                human.playerHp=Text(stage,"Player HP","",17,24,42,270,28);
                human.enemyHp=Text(stage,"Enemy HP","",17,366,42,270,28,TextAnchor.MiddleRight);
                human.playerFill=Health(stage,"Player bar",24,74,new Color(.25f,.85f,.68f));human.enemyFill=Health(stage,"Enemy bar",366,74,new Color(.94f,.4f,.42f));
                human.playerShield=Text(stage,"Player shield","",16,24,91,270,26);
                human.enemyShield=Text(stage,"Enemy shield","",16,366,91,270,26,TextAnchor.MiddleRight);
                human.hero=Glyph(stage,"Human adventurer",LoopGlyph.Pawn,76,159,128,140).rectTransform;
                human.foe=Glyph(stage,"Personal enemy",LoopGlyph.Enemy,466,159,128,140).rectTransform;
                human.foe.GetComponent<LoopGlyphGraphic>().color=new Color(.96f,.45f,.5f);
                human.effectGlyph=Glyph(stage,"Applied skill",LoopGlyph.Sword,270,169,120,120);human.effect=human.effectGlyph.rectTransform;human.effect.gameObject.SetActive(false);
                human.skillName=Text(stage,"Skill name","",24,20,116,620,38,TextAnchor.MiddleCenter);
                human.impact=Text(stage,"Combat impact","",26,20,313,620,51,TextAnchor.MiddleCenter);
                human.cue=Text(frame,"Playback cue","",20,30,633,660,38,TextAnchor.MiddleCenter);human.cue.color=new Color(1,.82f,.43f);
                var tray=Panel(frame,"Dice tray",30,681,660,176,new Color(.065f,.12f,.18f));human.tray=tray.gameObject;
                human.rollLabel=Text(tray,"Tray label","",14,20,5,620,26,TextAnchor.MiddleCenter);
                human.numericDie=Die(tray,"Travel D6",272,30);human.symbolDice=new LoopDieGraphic[3];human.dieLabels=new Text[3];
                for(int i=0;i<3;i++) { human.symbolDice[i]=Die(tray,"Symbol die "+i,100+i*172,30);human.dieLabels[i]=Text(tray,"Die caption "+i,"",16,80+i*172,141,156,28,TextAnchor.MiddleCenter); }
                screen.detail=Text(frame,"Current encounter","",17,32,871,656,72);
                screen.history=Text(frame,"AI round summary","",15,32,945,656,68);screen.history.color=new Color(.61f,.76f,.78f);
                var buttons=Panel(frame,"Actions",30,1016,660,224,Color.clear);
                var buttonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab");
                screen.buttons=new CommonButtonView[8];
                for(int i=0;i<8;i++)
                {
                    var button=(GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab,buttons);button.name="Action "+i;
                    SetRect((RectTransform)button.transform,(i%2)*338,(i/2)*56,322,50);
                    var view=button.GetComponent<CommonButtonView>();view.label.font=font;view.label.fontSize=17;
                    view.label.resizeTextForBestFit=true;view.label.resizeTextMinSize=14;view.label.resizeTextMaxSize=17;view.label.alignment=TextAnchor.MiddleCenter;
                    Stretch(view.label.rectTransform);view.label.rectTransform.offsetMin=new Vector2(10,4);view.label.rectTransform.offsetMax=new Vector2(-10,-4);screen.buttons[i]=view;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view.label);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label.rectTransform);
                }
                Text(frame,"Preview footer","명예·순위는 경기 기록 / 영구 성장 재화 지급 0",14,30,1248,660,24,TextAnchor.MiddleCenter).color=new Color(.5f,.64f,.68f);
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var roots=scene.GetRootGameObjects();
            var manager=roots.SelectMany(r=>r.GetComponentsInChildren<UIManager>(true)).Single();
            var app=roots.SelectMany(r=>r.GetComponentsInChildren<CompetitionApp>(true)).SingleOrDefault();
            if(!app) { var obj=new GameObject("Shared World Application");SceneManager.MoveGameObjectToScene(obj,scene);app=obj.AddComponent<CompetitionApp>(); }
            app.ui=manager;app.combatConfig=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>("Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset");app.content=content;
            if(!app.combatConfig) throw new InvalidOperationException("Missing shared combat configuration");
            var prefab=AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath);
            manager.prefabs=new BaseUI[]{prefab};
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            EditorUtility.SetDirty(app);EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Scene save failed");
        }
        static Text Text(Transform parent,string name,string value,int size,float x,float y,float width,float height,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);SetRect((RectTransform)go.transform,x,y,width,height);
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.alignment=alignment;text.color=new Color(.88f,.92f,.93f);text.raycastTarget=false;text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        static RectTransform Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;SetRect(rect,x,y,width,height);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        static LoopGlyphGraphic Glyph(Transform parent,string name,LoopGlyph glyph,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(LoopGlyphGraphic));go.transform.SetParent(parent,false);SetRect((RectTransform)go.transform,x,y,width,height);
            var graphic=go.GetComponent<LoopGlyphGraphic>();graphic.glyph=glyph;graphic.color=LoopGlyphGraphic.Tint(glyph);graphic.raycastTarget=false;return graphic;
        }
        static LoopDieGraphic Die(Transform parent,string name,float x,float y)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(LoopDieGraphic));go.transform.SetParent(parent,false);SetRect((RectTransform)go.transform,x,y,116,116);
            var die=go.GetComponent<LoopDieGraphic>();die.raycastTarget=false;return die;
        }
        static Image Health(Transform parent,string name,float x,float y,Color color)
        {
            var back=Panel(parent,name+" track",x,y,270,10,new Color(.03f,.04f,.08f));var fill=Panel(back,name,0,0,270,10,color).GetComponent<Image>();
            fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;return fill;
        }
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
        static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
    }
}
