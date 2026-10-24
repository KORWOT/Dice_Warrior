using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Threading;
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

// V5 UI authoring/capture tool. Only the named prefab may be saved. Fixtures never touch user saves.
public static class V5UiImprovementCapture
{
    const string Output="Docs/UI/V5Improved";
    const string Evidence="artifacts/v5-ui-improvement-11/run-01";
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
            PrefabUtility.SaveAsPrefabAsset(root,path);
            if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Prefab GUID changed");
            return new {path,guid,shell=screen.v5Shell!=null};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static async Task<object> Capture()
    {
        var rows=new List<object>();
        using(var c=new Context())
        {
            c.OpenAccount();rows.Add(await c.Capture("01-lobby","로비","isolated memory backend profile; real prefab"));
            c.Click("모험가 선택");rows.Add(await c.Capture("02-adventurers","모험가 선택","real menu callback"));c.app.HandleBack();
            try{c.Click("출전 선택");rows.Add(await c.Capture("03-loadout","출전 구성","real menu callback"));}
            catch(Exception e){rows.Add(new {id="03-loadout",status="BLOCKED",error=e.GetBaseException().Message});}c.app.HandleBack();
            c.Click("출전 능력치");rows.Add(await c.Capture("09-stats","출전 능력치","real menu callback; next match values"));c.app.HandleBack();
            c.Click("계정 관리");rows.Add(await c.Capture("10-account","계정과 영구 성장","memory backend approved profile, no live server"));c.app.HandleBack();
            c.Click("새 모험 시작");rows.Add(await c.Capture("04-board","공동 모험 보드","real new-match callback"));
            c.Cmd("travel");rows.Add(await c.Capture("05-encounter","도착 사건","real travel command and offer"));
            c.SetCombat(3);c.Cmd("combat_roll");c.SetFaces("arcane","arcane","sword");
            c.Cmd("combat_hold",c.app.Session.Snapshot.state.participants[0].dice[0].instanceId);
            rows.Add(await c.Capture("06-combat","굴림과 보관·기술 예상","isolated physical results; production plan resolver and real hold command"));
            c.Tap(c.Screen.v5Area.diceInfo);rows.Add(await c.Capture("07-dice-net","주사위 6면 조회","real modal callback"));c.Tap(c.Screen.v5Net.close);
            c.Tap(c.Screen.v5Area.combatInfo);rows.Add(await c.Capture("08-combat-info","전투 정보와 모험 메뉴","real modal callback"));c.Tap(c.Screen.v5Net.close);
        }
        using(var c=new Context())
        {
            c.OpenLocal();c.Click("새 모험 시작");
            // Natural command path to first reward; no completed-lap counter injection.
            for(int guard=0;guard<300 && c.app.Session.Snapshot.state.participants[0].phase!=CompetitionPhase.LapReward;guard++)
            {
                var p=c.app.Session.Snapshot.state.participants[0];
                if(p.phase==CompetitionPhase.Map)c.Cmd("travel");
                else if(p.phase==CompetitionPhase.Encounter)c.Cmd("choose",p.offer.choices.Where(x=>x.available).OrderBy(x=>x.kind==EncounterKind.Combat?1:0).First().id);
                else if(p.phase==CompetitionPhase.Combat)c.Cmd(p.v5.selection.phase==V5SelectionPhase.AwaitRoll?"combat_roll":"combat_execute");
                else throw new InvalidOperationException("Capture fixture did not reach reward");
            }
            if(c.app.Session.Snapshot.state.participants[0].phase!=CompetitionPhase.LapReward)throw new InvalidOperationException("Reward capture guard exceeded");
            rows.Add(await c.Capture("11-reward","완주 보상과 선택 확정","real commands from start; isolated legal growth20 profile"));
            c.Cmd("abandon");for(int guard=0;guard<40&&!c.app.Session.Snapshot.state.finished;guard++)c.Cmd("advance");
            if(!c.app.Session.Snapshot.state.finished)throw new InvalidOperationException("Result capture guard exceeded");
            rows.Add(await c.Capture("12-results","최종 결과","real abandon then advance commands until finish; no rank injection"));
        }
        File.WriteAllText(Path.Combine(Output,"manifest.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
        return new {count=rows.Count,path=Output+"/manifest.json"};
    }
    public static object BoundaryAudit()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);checks.Add(name);}
        using(var c=new Context())
        {
            c.OpenLocal();c.Click("새 모험 시작");c.Cmd("travel");c.app.Close();
            string saved=LoopJson.Encode(c.app.Session.Snapshot),raw=File.ReadAllText(c.path);
            c.Click("경기 백업 확인");Check(c.Screen.v5Shell.Data.mode=="confirm","QA22 backup has explicit confirmation");c.app.HandleBack();
            Check(LoopJson.Encode(c.app.Session.Snapshot)==saved && File.ReadAllText(c.path)==raw,"QA22 inspection and Back do not restore");
            c.Click("새 모험 시작");c.app.HandleBack();Check(File.ReadAllText(c.path)==raw,"QA26 cancel preserves match");
            c.Click("새 모험 시작");c.Click("새 모험 시작 확정");
            Check(c.app.Session.Snapshot.state.startingProfile.growth==20 && c.app.Session.Snapshot.state.startingProfile.currency==120,"QA26 confirmed new match preserves account values");
            Check(Directory.GetFiles(c.directory,"checkpoint.json.restarted-*").Any(x=>File.ReadAllText(x)==raw),"QA26 previous raw match preserved");
            c.Cmd("abandon");c.Edit(d=>d.state.worldRound=29);c.Cmd("advance");
            Check(c.app.Session.Snapshot.state.worldRound==30 && !c.app.Session.Snapshot.state.finished,"QA20 round29 completes AI and advances30");
            c.Cmd("advance");Check(c.app.Session.Snapshot.state.finished && c.app.Session.Snapshot.state.endReason=="round-limit","QA20 final round AI precedes limit result");
            Check(c.Screen.v5Shell.Data.mode=="results" && c.Screen.v5Shell.summary.text.Contains("30라운드"),"QA20 result shows translated round reason");
        }
        foreach(bool kill in new[]{false,true})using(var c=new Context())
        {
            c.OpenLocal();c.Click("새 모험 시작");c.SetCombat(1);
            for(int i=0;i<19;i++){c.Cmd("combat_roll");c.SetFaces("blank");c.Cmd("combat_execute");}
            Check(c.app.Session.Snapshot.state.participants[0].encounterTurns==19,"QA14 nineteen real blank actions "+kill);
            c.Cmd("combat_roll");c.SetFaces(kill?"sword":"blank");
            if(kill)c.Edit(d=>d.state.participants[0].enemy.hp=1);
            Check(c.Screen.v5Area.heading.text.Contains("20/20") && c.Screen.v5Area.notice.text.Contains("마지막 행동턴"),"QA14 last action visible "+kill);
            string saved=LoopJson.Encode(c.app.Session.Snapshot);c.Tap(c.Screen.v5Area.combatInfo);c.Tap(c.Screen.v5Net.stats);c.app.HandleBack();
            Check(c.Screen.v5Net.gameObject.activeInHierarchy && c.Screen.v5Net.description.text.Contains("20번째"),"QA15 details return to combat information "+kill);
            c.Tap(c.Screen.v5Net.close);Check(LoopJson.Encode(c.app.Session.Snapshot)==saved,"QA15 nested queries preserve exact saved state "+kill);
            c.Cmd("combat_execute");var p=c.app.Session.Snapshot.state.participants[0];
            Check(kill?!p.eliminated && !p.lastTurn.retaliation:p.eliminated && p.phase==CompetitionPhase.Complete,"QA14 kill precedes turn limit or surviving enemy eliminates "+kill);
        }
        Directory.CreateDirectory(Evidence);File.WriteAllText(Path.Combine(Evidence,"boundary-audit.json"),JsonConvert.SerializeObject(checks,Formatting.Indented));
        return new {passed=checks.Count,checks};
    }
    public static object LapRoundBoundary()
    {
        var checks=new List<string>();void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks.Add(label);}
        using(var c=new Context())
        {
            c.OpenLocal();c.Click("새 모험 시작");c.SetLap(2);c.Edit(d=>d.state.worldRound=30);
            Check(c.Screen.v5Shell.Data.mode=="reward" && c.Screen.v5Shell.Data.choices.Length==5,"QA20 round30 third reward shows all five candidates");
            c.Click(c.definition.combat.gear[0].name+" · 수령 확정");var state=c.app.Session.Snapshot.state;
            Check(state.worldRound==30 && !state.finished && state.participants[0].phase==CompetitionPhase.Combat && state.participants[0].enemy.boss,"QA20 third reward enters trial before round limit");
            c.Cmd("combat_roll");c.SetFaces("sword","sword","sword");c.Edit(d=>d.state.participants[0].enemy.hp=1);c.Cmd("combat_execute");state=c.app.Session.Snapshot.state;
            Check(state.participants[0].trialSucceeded && state.finished && state.endReason=="round-limit","QA20 trial victory retained before round limit result");
            Check(state.lastRound.Count==3,"QA20 remaining three AIs finish their final action");
            Check(c.Screen.v5Shell.Data.mode=="results" && c.Screen.v5Shell.summary.text.Contains("30라운드") && c.Screen.v5Shell.body.text.Contains("시련 성공"),"QA20 final UI shows trial success and round-limit reason");
        }
        File.WriteAllText(Path.Combine(Evidence,"lap-round-boundary.json"),JsonConvert.SerializeObject(checks,Formatting.Indented));return new {passed=checks.Count,checks};
    }
    sealed class Context:IDisposable
    {
        readonly Scene preview;
        readonly GameObject root,appObject,cameraObject;
        readonly Camera camera;
        readonly RenderTexture target;
        public readonly UIManager ui;
        public readonly CompetitionApp app;
        public readonly CompetitionDefinition definition;
        public readonly string directory,path,accountPath;
        public CompetitionScreen Screen=>(CompetitionScreen)ui.ActiveScreen;
        public Context(int version=5)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Capture requires stopped Editor; do not interrupt user Play.");
            Directory.CreateDirectory(Output);
            directory=Path.GetFullPath(Path.Combine(Evidence,"fixtures",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(directory);path=Path.Combine(directory,"checkpoint.json");accountPath=Path.Combine(directory,"account.json");
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-"+(version==2?1:version)+".json").text);
            if(version<=2)definition.rulesVersion="shared-world-preview-"+version;
            preview=EditorSceneManager.NewPreviewScene();
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));SceneManager.MoveGameObjectToScene(root,preview);
            ui=root.GetComponentInChildren<UIManager>(true);ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>("Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab")};
            cameraObject=new GameObject("Documentation capture camera");SceneManager.MoveGameObjectToScene(cameraObject,preview);camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            target=new RenderTexture(720,1280,24);camera.targetTexture=target;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=1;
            appObject=new GameObject("Documentation isolated app");SceneManager.MoveGameObjectToScene(appObject,preview);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
        }
        public void OpenLocal(){app.Open(path,definition,new Profile{growth=20,currency=120},()=>33);app.Render();}
        public void OpenAccount(FixtureBackend backend=null){app.OpenAccount(accountPath,path,definition,backend??new FixtureBackend(),()=>33);app.Render();}
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
        public async Task<object> Capture(string id,string title,string setup,bool render=true)
        {
            if(render)app.Render();
            ui.Root.safeArea.anchorMin=Vector2.zero;ui.Root.safeArea.anchorMax=Vector2.one;ui.Root.safeArea.offsetMin=ui.Root.safeArea.offsetMax=Vector2.zero;
            EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(100);Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,id+".png"),texture.EncodeToPNG());}
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
    sealed class FixtureBackend:IGameBackend
    {
        public BackendProfile Current=new BackendProfile{revision=1,values=new Profile{currency=120,growth=2}};
        public RunTicket ActiveRun;
        public bool Fail;
        public TaskCompletionSource<BackendReply> Held;
        BackendReply Reply()=>new BackendReply{status=BackendStatus.Approved,profile=LoopJson.Copy(Current),run=LoopJson.Copy(ActiveRun)};
        public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>Held!=null?Held.Task:Fail?Task.FromException<BackendReply>(new TimeoutException("캡처용 격리 서버 응답 없음")):Task.FromResult(Reply());
        public Task<BackendReply> UpgradeAsync(BackendRequest request,CancellationToken ct){Current.values.currency-=20*(Current.values.growth+1);Current.values.growth++;Current.revision++;return Task.FromResult(Reply());}
        static Task<BackendReply> Unsupported()=>Task.FromException<BackendReply>(new InvalidOperationException("Documentation fixture does not submit solo runs"));
        public Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct)=>Unsupported();
        public Task<BackendReply> SubmitRunAsync(BackendRequest request,CancellationToken ct)=>Unsupported();
        public Task<BackendReply> GetOperationStatusAsync(string requestId,CancellationToken ct)=>Task.FromResult(new BackendReply{status=BackendStatus.Rejected,code="OPERATION_NOT_FOUND"});
        public Task<BackendReply> GetRunStatusAsync(string serverRunId,CancellationToken ct)=>Unsupported();
    }
}
