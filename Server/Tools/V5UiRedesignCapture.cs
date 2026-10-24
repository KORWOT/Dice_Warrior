using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Newtonsoft.Json;
using Object=UnityEngine.Object;

// UI12 evidence uses the real prefab with isolated legal fixtures, never a user's checkpoint.
public static class V5UiRedesignCapture
{
    const string Output="Docs/UI/V5Redesign";
    const string Evidence="artifacts/v5-ui-redesign-12/run-01";
    const string LayoutOutput="Docs/UI/V5Layout";
    const string LayoutEvidence="artifacts/v5-ui-layout-13/run-01";
    public static object Upgrade()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required");
        const string path="Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab";
        string guid=AssetDatabase.AssetPathToGUID(path);var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var screen=root.GetComponent<CompetitionScreen>();
            DiceWarrior.SharedWorld.Editor.V5ShellAuthoring.Upgrade(screen);
            DiceWarrior.SharedWorld.Editor.CompetitionCombatUiAuthoring.Upgrade(screen);
            DiceWarrior.SharedWorld.Editor.V5RedesignAuthoring.Upgrade(screen);
            PrefabUtility.SaveAsPrefabAsset(root,path);
            if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Prefab GUID changed");
            return new {path,guid,cubes=screen.v5Area.cubes.Length};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static async Task<object> Capture()
    {
        var rows=new List<object>();
        using(var c=new Context())
        {
            c.OpenLocal();rows.Add(await c.Capture("01-lobby","로비 · 주사위 출전 구성","isolated legal local profile; actual prefab"));
            c.Click("새 모험 시작");rows.Add(await c.Capture("02-board","12장소 보드 · 이동 준비","actual new match and fixed board CTA"));
            var before=c.app.Session.Snapshot.state;c.app.Session.Execute(c.app.Session.Prepare("travel",""));var after=c.app.Session.Snapshot.state;
            int phase=0;
            await Sample(c.Screen.playback.Play("travel",before,after),async t=>
            {
                if(t!=.5f)return;
                if(phase++==0)rows.Add(await c.Capture("03-board-rolling","이동 D6 회전","actual saved travel; sampled production animation callback",false));
                else if(!c.Screen.playback.MovementRolling && c.Screen.v5Shell.boardPrimary.label.text.Contains("으로 이동"))
                    rows.Add(await c.Capture("04-board-arrival","확정 눈 · 실제 경로 · 도착","actual saved travel; arrival frame before automatic event view",false));
            });c.app.Render();
            rows.Add(await c.Capture("05-encounter","도착 사건","same travel result after animation"));
            c.SetCombat(3);rows.Add(await c.Capture("06-combat-ready","전투 첫 굴림 준비","legal three dice fixture; actual enemy and dice data"));
            before=c.app.Session.Snapshot.state;c.app.Session.Execute(c.app.Session.Prepare("combat_roll",""));after=c.app.Session.Snapshot.state;
            bool captured=false;
            await Sample(c.Screen.playback.Play("combat_roll",before,after),async t=>
            {if(!captured && t==.5f && c.Screen.v5Area.DiceRolling){captured=true;rows.Add(await c.Capture("07-combat-rolling","전투 주사위3개 회전","actual saved roll; sampled production animation callback",false));}});c.app.Render();
            c.SetFaces("arcane","arcane","sword");c.Cmd("combat_hold",c.app.Session.Snapshot.state.participants[0].dice[0].instanceId);
            rows.Add(await c.Capture("08-combat-hold","같은 자리의 보관 · 기술 예상","explicit physical face fixture; actual hold command and planner"));
            before=c.app.Session.Snapshot.state;c.app.Session.Execute(c.app.Session.Prepare("combat_reroll",""));after=c.app.Session.Snapshot.state;captured=false;
            await Sample(c.Screen.playback.Play("combat_reroll",before,after),async t=>
            {if(!captured && t==.5f && c.Screen.v5Area.DiceRolling){captured=true;rows.Add(await c.Capture("09-combat-reroll","보관1개 고정 · 나머지2개 리롤","actual saved partial reroll; sampled production animation callback",false));}});c.app.Render();
            c.SetFaces("arcane","arcane","sword");before=c.app.Session.Snapshot.state;c.app.Session.Execute(c.app.Session.Prepare("combat_execute",""));after=c.app.Session.Snapshot.state;captured=false;
            await Sample(c.Screen.playback.Play("combat_execute",before,after),async t=>
            {if(!captured && t==.5f && c.Screen.v5Area.preview.text.StartsWith("기술 ",StringComparison.Ordinal)){captured=true;rows.Add(await c.Capture("10-combat-execute","확정 결과 · 기술 실행","actual saved skill log and consumed dice; sampled production animation callback",false));}});c.app.Render();
            c.Tap(c.Screen.v5Area.combatInfo);rows.Add(await c.Capture("11-combat-info","전투 정보 · 동작 줄이기","actual modal callback; no game mutation"));c.Tap(c.Screen.v5Net.close);
            c.SetLap(1);before=c.app.Session.Snapshot.state;c.app.Session.Execute(c.app.Session.Prepare("lap",c.definition.combat.dice[0].id));after=c.app.Session.Snapshot.state;captured=false;
            await Sample(c.Screen.playback.Play("lap",before,after),async t=>
            {if(!captured && t==1f && c.Screen.v5Shell.rewardGrowth.activeSelf){captured=true;rows.Add(await c.Capture("12-dice-growth","전투 주사위2→3개 합류","legal lap fixture then real reward command; display cubes are not rolled results",false));}});c.app.Render();
        }
        File.WriteAllText(Path.Combine(Output,"manifest.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
        return new {count=rows.Count,path=Output+"/manifest.json"};
    }
    public static async Task<object> RatioAudit()
    {
        var checks=new List<string>();
        foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640)})
        using(var c=new Context(5,size.x,size.y,true))
        {
            c.OpenLocal();c.Click("새 모험 시작");await c.Capture("board-"+size.x+"x"+size.y,"비율 검수","synthetic safe area",true,Evidence);
            var screen=c.Screen;var safe=c.ui.Root.safeArea;
            var required=new[]{(RectTransform)screen.board.transform,(RectTransform)screen.playback.human.tray.transform,(RectTransform)screen.v5Shell.boardPrimary.transform};
            foreach(var rect in required)if(!Contains(safe.rect,Bounds(rect,safe)))throw new InvalidOperationException("Safe area overflow: "+rect.name);
            foreach(var text in screen.places.Concat(screen.roster).Concat(new[]{screen.center,screen.v5Shell.heading,screen.v5Shell.summary}))
                if(text.preferredHeight>text.rectTransform.rect.height+1)throw new InvalidOperationException("Board text overflow: "+text.name+" "+text.preferredHeight+"/"+text.rectTransform.rect.height);
            var die=screen.playback.human.numericDie;var tray=(RectTransform)screen.playback.human.tray.transform;var label=Bounds(screen.playback.human.rollLabel.rectTransform,tray);
            for(int sample=0;sample<=40;sample++)
            {
                die.Pose(sample/40f,0);Canvas.ForceUpdateCanvases();var mesh=die.canvasRenderer.GetMesh();
                if(!mesh || mesh.vertexCount==0)throw new InvalidOperationException("Numeric die mesh missing");
                var vertices=mesh.vertices.Select(v=>(Vector2)tray.InverseTransformPoint(die.transform.TransformPoint(v))).ToArray();
                var visible=Rect.MinMaxRect(vertices.Min(v=>v.x),vertices.Min(v=>v.y),vertices.Max(v=>v.x),vertices.Max(v=>v.y));
                if(!Contains(tray.rect,visible) || visible.Overlaps(label))throw new InvalidOperationException("Numeric die bounds at "+sample+": "+visible+" label="+label+" tray="+tray.rect);
            }
            checks.Add(size.x+"x"+size.y+" safe area, board text, 41 numeric cube frames");
            c.SetCombat(3);await c.Capture("combat-"+size.x+"x"+size.y,"비율 검수","synthetic safe area",true,Evidence);
        }
        File.WriteAllText(Path.Combine(Evidence,"ratio-audit.json"),JsonConvert.SerializeObject(checks,Formatting.Indented));return new {checks};
    }
    public static async Task<object> LayoutCapture()
    {
        var rows=new List<object>();
        using(var c=new Context(evidence:LayoutEvidence))
        {
            c.OpenLocal();rows.Add(await c.Capture("01-lobby","새 모험 준비 · 고정 시작 버튼","isolated profile; actual authored layout",true,LayoutOutput));
            c.Click("모험가 선택");rows.Add(await c.Capture("03-adventurers","다음 모험가 선택","read-only opening; selection is still saved immediately",true,LayoutOutput));c.app.HandleBack();
            c.Click("출전 선택");rows.Add(await c.Capture("04-loadout","주사위·무기 출전 구성","read-only opening; current saved match is separate",true,LayoutOutput));c.app.HandleBack();
            c.Click("새 모험 시작");rows.Add(await c.Capture("05-board","현재 위치와 고정 이동 행동","actual new match",true,LayoutOutput));
            c.app.Close();rows.Add(await c.Capture("02-resume","진행 보존 · 이어하기 우선","same saved match; Close preserves progress",true,LayoutOutput));c.Click("경기 이어하기");
            c.Cmd("travel");rows.Add(await c.Capture("06-encounter","사건 · 비용과 보상 비교","actual saved travel result",true,LayoutOutput));
            c.SetCombat(3);rows.Add(await c.Capture("07-combat-ready","전투 · 첫 굴림 준비","legal three dice combat fixture",true,LayoutOutput));
            c.Cmd("combat_roll");c.SetFaces("arcane","arcane","sword");c.Cmd("combat_hold",c.app.Session.Snapshot.state.participants[0].dice[0].instanceId);
            rows.Add(await c.Capture("08-combat-selection","보관 · 기술별 예상 결과","physical face fixture then actual hold and shared planner",true,LayoutOutput));
            c.Tap(c.Screen.v5Area.combatInfo);rows.Add(await c.Capture("09-combat-info","전투 정보 · 조회와 관리 분리","actual read-only information callback",true,LayoutOutput));
            c.Tap(c.Screen.v5Net.menu);rows.Add(await c.Capture("10-management","모험 관리 · 진행 보존과 포기 구분","actual management route from combat information",true,LayoutOutput));
            c.app.HandleBack();c.Tap(c.Screen.v5Net.close);
            c.SetLap(2);rows.Add(await c.Capture("11-reward","완주 보상 · 실제 교체와 지급 비교","legal third-lap fixture; reward not yet claimed",true,LayoutOutput));
            c.Cmd("lap",c.definition.combat.gear[0].id);c.Cmd("abandon");int guard=0;
            while(!c.app.Session.Snapshot.state.finished && guard++<32)c.Cmd("advance");
            if(!c.app.Session.Snapshot.state.finished)throw new InvalidOperationException("Result capture did not complete within the legal round limit");
            rows.Add(await c.Capture("12-results","결과 · 순위와 고정 로비 복귀","actual abandon then AI advance commands; fixture journey",true,LayoutOutput));
        }
        File.WriteAllText(Path.Combine(LayoutOutput,"manifest.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
        return new {count=rows.Count,path=LayoutOutput+"/manifest.json"};
    }
    public static async Task<object> LayoutRatioAudit()
    {
        var checks=new List<string>();
        foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640)})
        using(var c=new Context(5,size.x,size.y,true,LayoutEvidence))
        {
            c.OpenLocal();await c.Capture("lobby-"+size.x+"x"+size.y,"로비 비율 검수","synthetic safe area",true,LayoutEvidence);
            CheckBounds(c.Screen.v5Shell.boardPrimary.transform,c.ui.Root.safeArea,"lobby primary");
            CheckText(c.Screen.v5Shell.heading);CheckText(c.Screen.v5Shell.summary);
            c.Click("새 모험 시작");c.SetCombat(3);c.Cmd("combat_roll");c.SetFaces("arcane","arcane","sword");
            await c.Capture("combat-"+size.x+"x"+size.y,"전투 비율 검수","synthetic safe area; legal physical face fixture",true,LayoutEvidence);
            CheckBounds(c.Screen.v5Area.footer,c.ui.Root.safeArea,"combat actions");
            foreach(var row in c.Screen.v5Area.previewRows)if(row.gameObject.activeInHierarchy){CheckText(row);CheckBounds(row.transform,c.ui.Root.safeArea,"skill preview");}
            c.Tap(c.Screen.v5Area.combatInfo);await c.Capture("info-"+size.x+"x"+size.y,"정보 비율 검수","synthetic safe area; actual information view",true,LayoutEvidence);
            CheckBounds(c.Screen.v5Net.close.transform,c.ui.Root.safeArea,"information close");CheckBounds(c.Screen.v5Net.menu.transform,c.ui.Root.safeArea,"information menu");
            checks.Add(size.x+"x"+size.y+" lobby primary, combat skill rows/actions, information close/menu");
        }
        File.WriteAllText(Path.Combine(LayoutEvidence,"ratio-audit.json"),JsonConvert.SerializeObject(checks,Formatting.Indented));return new {checks};
    }
    static void CheckBounds(Transform target,RectTransform parent,string label)
    {if(!Contains(parent.rect,Bounds((RectTransform)target,parent)))throw new InvalidOperationException("Safe area overflow: "+label);}
    static void CheckText(Text text)
    {if(text.preferredHeight>text.rectTransform.rect.height+1)throw new InvalidOperationException("Text overflow: "+text.name);}
    static bool Contains(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-.5f&&inner.yMin>=outer.yMin-.5f&&inner.xMax<=outer.xMax+.5f&&inner.yMax<=outer.yMax+.5f;
    static Rect Bounds(RectTransform child,RectTransform parent)
    {var corners=new Vector3[4];child.GetWorldCorners(corners);var p=corners.Select(v=>(Vector2)parent.InverseTransformPoint(v)).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
    // Sample the real animation frame callback at mid-flight and landing, without wall-clock waiting in EditMode.
    // Actual coroutine timing, input and interruption are checked by the PlayMode suite.
    static async Task Sample(IEnumerator sequence,Func<float,Task> capture)
    {
        var stack=new Stack<IEnumerator>();stack.Push(sequence);int guard=0;
        while(stack.Count>0)
        {
            if(++guard>500)throw new InvalidOperationException("Animation sampling guard exceeded");
            var current=stack.Peek();
            var frame=current.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Select(f=>f.GetValue(current)).OfType<Action<float>>().FirstOrDefault();
            if(frame!=null){frame(.5f);Canvas.ForceUpdateCanvases();await capture(.5f);frame(1);Canvas.ForceUpdateCanvases();await capture(1);stack.Pop();continue;}
            if(!current.MoveNext()){stack.Pop();continue;}
            if(current.Current is IEnumerator nested)stack.Push(nested);
        }
    }
    sealed class Context:IDisposable
    {
        readonly Scene preview;
        readonly GameObject root,appObject,cameraObject;
        readonly Camera camera;
        readonly RenderTexture target;
        readonly bool inset;
        public readonly UIManager ui;
        public readonly CompetitionApp app;
        public readonly CompetitionDefinition definition;
        public readonly string directory,path,accountPath;
        public CompetitionScreen Screen=>(CompetitionScreen)ui.ActiveScreen;
        public Context(int version=5,int width=720,int height=1280,bool inset=false,string evidence=null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Capture requires stopped Editor; do not interrupt user Play.");
            Directory.CreateDirectory(Output);this.inset=inset;
            directory=Path.GetFullPath(Path.Combine(evidence??Evidence,"fixtures",Guid.NewGuid().ToString("N").Substring(0,8)));Directory.CreateDirectory(directory);path=Path.Combine(directory,"checkpoint.json");accountPath=Path.Combine(directory,"account.json");
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-"+(version==2?1:version)+".json").text);
            if(version<=2)definition.rulesVersion="shared-world-preview-"+version;
            preview=EditorSceneManager.NewPreviewScene();
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));SceneManager.MoveGameObjectToScene(root,preview);
            ui=root.GetComponentInChildren<UIManager>(true);ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>("Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab")};
            cameraObject=new GameObject("Documentation capture camera");SceneManager.MoveGameObjectToScene(cameraObject,preview);camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            target=new RenderTexture(width,height,24);camera.targetTexture=target;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=1;
            appObject=new GameObject("Documentation isolated app");SceneManager.MoveGameObjectToScene(appObject,preview);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
        }
        public void OpenLocal(){app.Open(path,definition,new Profile{growth=20,currency=120},()=>33);app.Render();}
        public bool Has(string prefix,bool enabled=false)=>Screen.buttons.Any(b=>b.gameObject.activeInHierarchy&&b.label.text.StartsWith(prefix,StringComparison.Ordinal)&&(!enabled||b.button.interactable));
        public void Click(string prefix)=>Tap(Screen.GetComponentsInChildren<CommonButtonView>().First(b=>b.gameObject.activeInHierarchy&&b.label.text.StartsWith(prefix,StringComparison.Ordinal)));
        public void Tap(CommonButtonView button)
        {
            if(!button.button.interactable)throw new InvalidOperationException("Disabled button: "+button.label.text);
            app.enabled=false;try{button.button.onClick.Invoke();}finally{app.enabled=true;}app.Render();
        }
        public void Cmd(string kind,string arg=""){app.Session.Execute(app.Session.Prepare(kind,arg));app.Render();}
        public void Edit(Action<CompetitionDocument> change,bool render=true)
        {var original=app.Session.Snapshot;var candidate=LoopJson.Copy(original);change(candidate);candidate.revision++;new CompetitionStore(path).Commit(candidate,original.revision,original.generation);app.Session.Reload();if(render)app.Render();}
        public void SetLap(int completed)
        {
            Edit(d=>{var p=d.state.participants[0];p.completedLaps=completed;p.claimed=Enumerable.Range(1,completed).Select(i=>d.state.matchId+":"+p.id+":lap:"+i).ToList();p.dice=definition.combat.dice.Take(Math.Min(completed+1,3)).Select((x,i)=>new DieSlot{instanceId="catalog-die-"+i,definitionId=x.id}).ToList();p.phase=CompetitionPhase.LapReward;p.position=0;p.offer=null;p.enemy=null;p.encounterTurns=0;p.lastTurn=null;if(p.v4!=null){p.v4.enemyId=null;p.v4.lastTurn=null;}if(p.v5!=null){p.v5.selection=null;p.v5.lastTurn=null;}});
        }
        public void SetCombat(int count)
        {
            Edit(d=>{
                var p=d.state.participants[0];p.completedLaps=count-1;p.claimed=Enumerable.Range(1,count-1).Select(i=>d.state.matchId+":"+p.id+":lap:"+i).ToList();p.dice=definition.combat.dice.Take(count).Select((x,i)=>new DieSlot{instanceId="catalog-die-"+i,definitionId=x.id}).ToList();p.lastTurn=null;p.enemy=null;p.encounterTurns=0;p.turn=0;p.offer=null;
                if(p.v4!=null){p.v4.enemyId=null;p.v4.lastTurn=null;}if(p.v5!=null){p.v5.selection=null;p.v5.lastTurn=null;}
                var enemy=definition.encounters.First(e=>e.kind==EncounterKind.Combat);var other=definition.encounters.First(e=>e.kind==EncounterKind.Study);
                p.phase=CompetitionPhase.Encounter;p.offer=new EncounterOffer{id="catalog-offer-"+count,placeId=definition.world.places[p.position].id,stage=count-1,visit=1,choices=new[]{new EncounterChoice{id=enemy.id,label=enemy.label,description=enemy.description,kind=EncounterKind.Combat,available=true,enemyHp=500,enemyAttack=2},new EncounterChoice{id=other.id,label=other.label,description=other.description,kind=EncounterKind.Study,available=true}}};
            });
            Cmd("choose",app.Session.Snapshot.state.participants[0].offer.choices[0].id);
        }
        public void SetFaces(params string[] kinds)
        {
            Edit(d=>{var p=d.state.participants[0];for(int i=0;i<kinds.Length;i++)
            {var faces=CompetitionV5Content.Faces(d.state.definition,p.adventurerId,p.dice[i].definitionId);var key=kinds[i];int index=Array.FindIndex(faces,f=>key=="blank"?f.kind==V5FaceKind.Blank:key=="arcane"?f.name=="비전":f.name=="검격");if(index<0)throw new InvalidOperationException("Missing physical face "+key+" in "+string.Join(",",faces.Select(f=>f.id)));p.v5.selection.results[i].faceIndex=index;p.v5.selection.results[i].faceId=faces[index].id;}});
        }
        public async Task<object> Capture(string id,string title,string setup,bool render=true,string destination=null)
        {
            ui.Root.safeArea.anchorMin=inset?new Vector2(.03f,.04f):Vector2.zero;ui.Root.safeArea.anchorMax=inset?new Vector2(.97f,.96f):Vector2.one;ui.Root.safeArea.offsetMin=ui.Root.safeArea.offsetMax=Vector2.zero;
            // PreviewScene does not automatically refresh the safe-area rect before binding.
            Canvas.ForceUpdateCanvases();
            if(render)app.Render();
            Canvas.ForceUpdateCanvases();
            EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(100);Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();Directory.CreateDirectory(destination??Output);File.WriteAllBytes(Path.Combine(destination??Output,id+".png"),texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(texture);}
            var foreground=Screen.v5Net.gameObject.activeInHierarchy?Screen.v5Net.transform:Screen.transform;
            return new {id,title,setup,width=target.width,height=target.height,file=id+".png",modal=Screen.v5Net.gameObject.activeInHierarchy,buttons=foreground.GetComponentsInChildren<CommonButtonView>().Where(b=>b.gameObject.activeInHierarchy).Select(b=>new {text=b.label.text,enabled=b.button.interactable}).ToArray(),text=foreground.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)).Select(t=>t.text).ToArray()};
        }
        public void Dispose()
        {
            if(appObject)Object.DestroyImmediate(appObject);
            if(root)Object.DestroyImmediate(root);
            if(cameraObject)Object.DestroyImmediate(cameraObject);
            target.Release();Object.DestroyImmediate(target);
            if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
