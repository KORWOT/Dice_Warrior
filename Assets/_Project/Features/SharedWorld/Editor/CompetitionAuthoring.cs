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
        public const string ContentPath="Assets/_Project/Features/SharedWorld/Content/shared-world-preview-4.json";
        public const string V5ContentPath="Assets/_Project/Features/SharedWorld/Content/shared-world-preview-5.json";
        static Font font;
        [MenuItem("Dice Warrior/Upgrade Shared World V5")]
        public static void UpgradeV5()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop play first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf");
            var content=AssetDatabase.LoadAssetAtPath<TextAsset>(V5ContentPath);
            if(!font || !content || !AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))throw new InvalidOperationException("Missing approved V5 authoring input");
            var definition=CompetitionContent.Load(content.text);
            if(!CompetitionV5Content.IsEnabled(definition))throw new InvalidOperationException("Expected validated V5 content");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var screen=root.GetComponent<CompetitionScreen>();
                if(!screen || !screen.content || screen.buttons.Length!=8)throw new InvalidOperationException("Legacy screen bindings must be preserved");
                if(!screen.v5Area)
                {
                    if(root.transform.Find("V5 Combat"))throw new InvalidOperationException("Unbound V5 Combat subtree; inspect before authoring");
                    screen.v5Area=BuildV5Area(root.transform);
                }
                if(!screen.v5Net)
                {
                    if(root.transform.Find("V5 Information"))throw new InvalidOperationException("Unbound V5 Information subtree; inspect before authoring");
                    screen.v5Net=BuildV5Net(root.transform);
                }
                var areaFaces=screen.v5Area.faces;var netFaces=screen.v5Net.faces;
                if(areaFaces==null || areaFaces.Length!=3 || netFaces==null || netFaces.Length!=6 ||
                    areaFaces.Any(face=>!face || !face.transform.IsChildOf(screen.v5Area.transform)) ||
                    netFaces.Any(face=>!face || !face.transform.IsChildOf(screen.v5Net.transform)) ||
                    areaFaces.Concat(netFaces).Distinct().Count()!=9)throw new InvalidOperationException("Expected nine owned V5 faces");
                foreach(var face in areaFaces.Concat(netFaces))
                    if(!face.GetComponent<CanvasRenderer>())face.gameObject.AddComponent<CanvasRenderer>();
                screen.v5Net.blocker.color=new Color(.015f,.025f,.045f,1f);
                screen.v5Area.gameObject.SetActive(false);screen.v5Net.gameObject.SetActive(false);
                if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new IOException("V5 prefab save failed");
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var app=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CompetitionApp>(true)).Single();
            app.content=content;PrefabUtility.RecordPrefabInstancePropertyModifications(app);EditorUtility.SetDirty(app);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("V5 Title reference save failed");
        }
        static V5DiceAreaView BuildV5Area(Transform parent)
        {
            var root=Panel(parent,"V5 Combat",0,0,720,1280,new Color(.025f,.055f,.085f));Stretch(root);
            var view=root.gameObject.AddComponent<V5DiceAreaView>();
            view.layout=Panel(root,"Combat layout",0,0,720,1140,Color.clear);TopCenter(view.layout,720,1140);
            var layout=view.layout;
            view.heading=Text(layout,"Combat heading","",23,30,16,660,40);
            view.enemy=Text(layout,"Enemy HP","",25,30,70,660,50);view.intent=Text(layout,"Enemy intent","",20,30,123,660,48);
            view.player=Text(layout,"Player HP","",24,30,180,660,42);
            view.enemyFill=Health(layout,"Enemy health",390,175,new Color(.94f,.4f,.42f));view.playerFill=Health(layout,"Player health",30,175,new Color(.25f,.85f,.68f));
            view.diceInfo=V5Button(layout,"Dice info",30,224,320,60);view.combatInfo=V5Button(layout,"Combat info",370,224,320,60);
            view.rollZone=Panel(layout,"Roll zone",30,300,660,240,new Color(.06f,.13f,.18f));
            view.holdZone=Panel(layout,"Hold zone",30,562,660,240,new Color(.12f,.13f,.18f));
            view.rollHint=Text(view.rollZone,"Roll guidance","굴림 영역",18,18,12,624,40);view.holdHint=Text(view.holdZone,"Hold guidance","보관 영역",18,18,12,624,40);
            view.dice=new CommonButtonView[3];view.faces=new V5FaceGraphic[3];
            for(int i=0;i<3;i++)
            {
                view.dice[i]=V5Button(view.rollZone,"Die slot "+i,20+i*210,58,200,170);
                view.faces[i]=FaceGraphic(view.dice[i].transform,"Face",60,12,80,80);
                SetRect(view.dice[i].label.rectTransform,4,103,192,63);view.dice[i].label.fontSize=17;RecordButton(view.dice[i]);
            }
            view.roll=V5Button(layout,"Initial roll",220,400,280,88);
            view.preview=Text(layout,"Skill preview","",22,30,824,660,133);view.preview.color=new Color(.97f,.86f,.61f);
            view.notice=Text(layout,"Selection feedback","",19,30,967,660,168);
            view.footer=Panel(root,"Safe area footer",0,0,720,112,Color.clear);
            view.footer.anchorMin=view.footer.anchorMax=new Vector2(.5f,0);view.footer.pivot=new Vector2(.5f,0);view.footer.anchoredPosition=new Vector2(0,12);
            view.reroll=V5Button(view.footer,"Reroll",30,0,320,88);view.execute=V5Button(view.footer,"Execute",370,0,320,88);
            return view;
        }
        static V5DiceNetView BuildV5Net(Transform parent)
        {
            var root=Panel(parent,"V5 Information",0,0,720,1280,new Color(.015f,.025f,.045f,1f));Stretch(root);
            var view=root.gameObject.AddComponent<V5DiceNetView>();view.blocker=root.GetComponent<Image>();view.blocker.raycastTarget=true;
            view.layout=Panel(root,"Information layout",0,0,720,1280,Color.clear);view.layout.anchorMin=view.layout.anchorMax=view.layout.pivot=new Vector2(.5f,.5f);view.layout.anchoredPosition=Vector2.zero;
            var layout=view.layout;view.title=Text(layout,"Information title","",27,30,26,660,70);
            view.tabs=new CommonButtonView[3];for(int i=0;i<3;i++)view.tabs[i]=V5Button(layout,"Die tab "+i,30+i*224,112,212,65);
            view.net=Panel(layout,"Connected six face net",40,190,640,474,Color.clear);view.cells=new CommonButtonView[6];view.faces=new V5FaceGraphic[6];
            for(int i=0;i<6;i++)
            {
                var cell=V5DiceNetView.Cell(i);view.cells[i]=V5Button(view.net,"Face index "+i,cell.x*158,cell.y*158,154,154);
                view.faces[i]=FaceGraphic(view.cells[i].transform,"Face identity",44,5,66,66);
                SetRect(view.cells[i].label.rectTransform,3,77,148,48);view.cells[i].label.fontSize=16;RecordButton(view.cells[i]);
            }
            view.description=Text(layout,"Face and skill details","",18,30,680,660,330);
            view.previous=V5Button(layout,"Previous details",30,1020,320,62);view.next=V5Button(layout,"Next details",370,1020,320,62);
            view.leave=V5Button(layout,"Leave combat",30,1100,212,62);view.abandon=V5Button(layout,"Abandon combat",254,1100,212,62);view.stats=V5Button(layout,"Combat stats",478,1100,212,62);
            view.close=V5Button(layout,"Close information",30,1190,660,66);
            return view;
        }
        static void TopCenter(RectTransform rect,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,height);}
        static CommonButtonView V5Button(Transform parent,string name,float x,float y,float width,float height)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab");
            if(!prefab)throw new InvalidOperationException("Missing CommonButtonView");
            var button=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);button.name=name;SetRect((RectTransform)button.transform,x,y,width,height);
            var view=button.GetComponent<CommonButtonView>();view.label.font=font;view.label.fontSize=21;view.label.alignment=TextAnchor.MiddleCenter;
            view.label.resizeTextForBestFit=false;view.label.raycastTarget=false;view.label.supportRichText=false;Stretch(view.label.rectTransform);
            view.label.rectTransform.offsetMin=new Vector2(8,4);view.label.rectTransform.offsetMax=new Vector2(-8,-4);view.background.raycastTarget=true;RecordButton(view);return view;
        }
        static void RecordButton(CommonButtonView view)
        {PrefabUtility.RecordPrefabInstancePropertyModifications(view.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label.rectTransform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.background);}
        static V5FaceGraphic FaceGraphic(Transform parent,string name,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(V5FaceGraphic));go.transform.SetParent(parent,false);SetRect((RectTransform)go.transform,x,y,width,height);var graphic=go.GetComponent<V5FaceGraphic>();graphic.raycastTarget=false;return graphic;}
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
                GrowthDetails(frame,screen);
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
        static void GrowthDetails(RectTransform frame,CompetitionScreen screen)
        {
            var panel=Panel(frame,"Growth details",30,246,660,610,new Color(.055f,.1f,.15f));screen.growthPanel=panel.gameObject;
            screen.growthTitle=Text(panel,"Details title","",24,20,16,620,36);
            screen.growthTitle.color=new Color(.95f,.83f,.55f);
            screen.growthSummary=Text(panel,"Details summary","",18,20,60,620,72);
            var rows=Panel(panel,"RGB stats",20,140,620,204,Color.clear);screen.statRows=rows.gameObject;
            screen.statValues=new Text[3];screen.statSources=new Text[3];
            var colors=new[]{new Color32(229,108,100,255),new Color32(82,185,135,255),new Color32(114,151,237,255)};
            var glyphs=new[]{LoopGlyph.Sword,LoopGlyph.Shield,LoopGlyph.Staff};
            for(int i=0;i<3;i++)
            {
                Glyph(rows,"Stat icon "+i,glyphs[i],0,i*68+6,38,38).color=colors[i];
                screen.statValues[i]=Text(rows,"Stat value "+i,"",24,52,i*68,568,34);screen.statValues[i].color=colors[i];
                screen.statSources[i]=Text(rows,"Stat sources "+i,"",17,52,i*68+35,568,29);
            }
            screen.growthBody=Text(panel,"Growth sources","",17,20,356,620,238);
            var attributes=Panel(panel,"Classic attributes",20,140,620,264,Color.clear);screen.attributeRows=attributes.gameObject;
            screen.attributeValues=new Text[6];screen.attributeSources=new Text[6];
            var attributeColors=new[]{new Color32(229,115,115,255),new Color32(215,166,75,255),new Color32(85,186,139,255),new Color32(112,159,233,255),new Color32(161,138,224,255),new Color32(216,135,187,255)};
            var attributeGlyphs=new[]{LoopGlyph.Sword,LoopGlyph.Star,LoopGlyph.Pawn,LoopGlyph.Staff,LoopGlyph.Shield,LoopGlyph.Gem};
            for(int i=0;i<6;i++)
            {
                Glyph(attributes,"Attribute icon "+i,attributeGlyphs[i],0,i*44+6,30,30).color=attributeColors[i];
                screen.attributeValues[i]=Text(attributes,"Attribute value "+i,"",21,42,i*44+4,220,34);screen.attributeValues[i].color=attributeColors[i];
                screen.attributeSources[i]=Text(attributes,"Attribute sources "+i,"",15,270,i*44+2,350,40);
            }
            screen.attributeBody=Text(panel,"Attribute HP and growth","",17,20,416,620,178);
            attributes.gameObject.SetActive(false);screen.attributeBody.gameObject.SetActive(false);
            screen.skillDetails=Text(panel,"Dice and skills","",17,20,140,620,454);
            var cards=Panel(panel,"Adventurer cards",20,140,620,454,Color.clear);screen.adventurerCards=cards.gameObject;screen.cardTexts=new Text[3];
            for(int i=0;i<3;i++)
            {
                var card=Panel(cards,"Adventurer card "+i,0,i*149,620,137,new Color(.085f,.15f,.21f));
                Panel(card,"Accent",0,0,5,137,colors[new[]{0,2,1}[i]]);
                screen.cardTexts[i]=Text(card,"Adventurer description","",19,18,12,584,113);
            }
            panel.gameObject.SetActive(false);
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
