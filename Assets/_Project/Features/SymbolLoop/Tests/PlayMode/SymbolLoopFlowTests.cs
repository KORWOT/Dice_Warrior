#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FateDice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class SymbolLoopFlowTests
    {
        GameObject root, appObject, eventObject, cameraObject;
        SymbolLoopApp app;
        UIManager ui;
        Camera camera;
        RenderTexture target;
        SymbolLoopConfig config;
        string directory, checkpointPath;
        readonly List<IDisposable> networkBackends=new List<IDisposable>();
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"DiceWarriorSymbolLoopUI",Guid.NewGuid().ToString("N"));
            checkpointPath=Path.Combine(directory,"player.json");
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));
            ui=root.GetComponentInChildren<UIManager>(true);
            ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<SymbolLoopScreen>("Assets/_Project/Features/SymbolLoop/Prefabs/SymbolLoopScreen.prefab")};
            config=Object.Instantiate(AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>("Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset"));
            cameraObject=new GameObject("Isolated capture camera"); camera=cameraObject.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera; ui.Root.canvas.worldCamera=camera; ui.Root.canvas.planeDistance=1;
            Resize(720,1280);
            eventObject=new GameObject("Isolated events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            appObject=new GameObject("Isolated symbol app"); appObject.SetActive(false);
            app=appObject.AddComponent<SymbolLoopApp>(); app.ui=ui; app.config=config;
            app.Open(checkpointPath,backend:LocalBackend());
            appObject.SetActive(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui) ui.CloseAll();
            if(appObject) Object.Destroy(appObject);
            if(root) Object.Destroy(root);
            if(eventObject) Object.Destroy(eventObject);
            if(cameraObject) Object.Destroy(cameraObject);
            if(config) Object.Destroy(config);
            if(target) { target.Release(); Object.Destroy(target); }
            yield return null;
            foreach(var backend in networkBackends) backend.Dispose(); networkBackends.Clear();
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorSymbolLoopUI"))+Path.DirectorySeparatorChar;
            if(directory!=null && Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory,true);
        }
        void Resize(int width,int height)
        {
            if(target) { target.Release(); Object.Destroy(target); }
            target=new RenderTexture(width,height,24); camera.targetTexture=target;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            ui.Root.canvas.scaleFactor=width/720f;
            Canvas.ForceUpdateCanvases();
        }
        void Click(string prefix)
        {
            var view=(SymbolLoopScreen)ui.ActiveScreen;
            var button=view.buttons.First(b=>b.gameObject.activeSelf && b.label.text.StartsWith(prefix,StringComparison.Ordinal));
            var rect=(RectTransform)button.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(eventObject.GetComponent<EventSystem>()) {position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); eventObject.GetComponent<EventSystem>().RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),prefix+" has no raycast hit");
            var selected=hits.Select(h=>h.gameObject.GetComponentInParent<Button>()).FirstOrDefault(b=>b);
            Assert.That(selected,Is.EqualTo(button.button),prefix+" is obstructed");
            ExecuteEvents.Execute(selected.gameObject,data,ExecuteEvents.pointerClickHandler);
            Canvas.ForceUpdateCanvases();
        }
        void Capture(string name)
        {
            Directory.CreateDirectory(Path.GetDirectoryName("Temp/symbol-loop-presentation/"+name+".png"));
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0); texture.Apply();
            File.WriteAllBytes("Temp/symbol-loop-presentation/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous; Object.Destroy(texture);
        }
        void AssertLayout()
        {
            var view=(SymbolLoopScreen)ui.ActiveScreen;
            foreach(var button in view.buttons.Where(b=>b.gameObject.activeSelf))
            {
                var corners=new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();
                Assert.That(points.Min(p=>p.x),Is.GreaterThanOrEqualTo(0));
                Assert.That(points.Max(p=>p.x),Is.LessThanOrEqualTo(target.width+1));
                Assert.That(points.Min(p=>p.y),Is.GreaterThanOrEqualTo(0));
                Assert.That(points.Max(p=>p.y),Is.LessThanOrEqualTo(target.height+1));
            }
        }
        static void AssertRendered(Graphic graphic)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(graphic.GetComponent<CanvasRenderer>(),Is.Not.Null,graphic.name+" requires an authored renderer");
            var mesh=graphic.canvasRenderer.GetMesh();
            Assert.That(mesh,Is.Not.Null,graphic.name+" mesh");
            Assert.That(mesh.vertexCount,Is.GreaterThan(0),graphic.name+" has no rendered vertices");
        }
        [UnityTest] public IEnumerator PointerFlowCompletesCampaignAndRestoresSavedTurnWithoutExecutingAgain()
        {
            yield return Reopen(NetworkBackend()); yield return WaitPlayback();
            Assert.That(app.Backend.ProfileReady,Is.True,"The standalone test server must be running with seed 33.");
            AssertLayout(); Capture("lobby-720x1280");
            Click("새 여정"); yield return WaitPlayback();
            bool capturedBoard=false,capturedCombat=false;
            for(int i=0;i<160;i++)
            {
                var run=app.Session.Snapshot.run;
                if(run.phase==Phase.Result) break;
                switch(run.phase)
                {
                    case Phase.Map:
                        if(!capturedBoard) { Capture("board-720x1280"); capturedBoard=true; }
                        Click("이동 주사위"); break;
                    case Phase.Combat:
                        if(!capturedCombat) { Capture("combat-720x1280"); capturedCombat=true; }
                        Click("기호 주사위");
                        yield return WaitPlayback();
                        string saved=LoopJson.Encode(app.Session.Snapshot);
                        app.Session.Reload(); app.Render();
                        Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved)); break;
                    case Phase.Rest:Click("휴식");break;
                    case Phase.Event:Click("조사");break;
                    case Phase.Shop:Click("떠나기");break;
                    case Phase.Treasure:Click("수호 갑옷");break;
                    case Phase.LapReward:
                        if(run.region<3) Click("확정 보상"); else Click("성채 갑옷");
                        break;
                }
                yield return WaitPlayback();
            }
            Assert.That(app.Session.Snapshot.run.won,Is.True);
            Click("기록 검증"); yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.profile.currency,Is.EqualTo(70));
            Assert.That(app.Session.Snapshot.submission.status,Is.EqualTo(BackendStatus.Approved));
            Capture("result-720x1280");
            Click("로비로"); Assert.That(((SymbolLoopScreen)ui.ActiveScreen).title.text,Is.EqualTo("DICE WARRIOR"));
            yield return null; Click("영구 HP"); yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.profile.currency,Is.EqualTo(50));
            Click("새 여정"); yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.run.maxHp,Is.EqualTo(65));
        }
        IEnumerator WaitPlayback(Action observe=null)
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(app.IsPresenting)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Presentation stuck");
                observe?.Invoke(); yield return null;
            }
            Assert.That(ui.Root.inputGroup.interactable,Is.True);
            // Newly activated Graphics need a render frame before the next pointer event (absoluteDepth registration).
            yield return null;
        }
        void Scenario(Action<Run> configure)
        {
            var saved=app.Session.Snapshot; configure(saved.run); long previous=saved.revision; saved.revision++;
            new LoopStore(checkpointPath).Commit(saved,previous,saved.generation);
            app.Session.Reload(); app.Render();
        }
        [UnityTest] public IEnumerator PawnVisitsEachCellAndInterruptedRollRestoresCommittedLanding()
        {
            Click("새 여정"); var view=(SymbolLoopScreen)ui.ActiveScreen;
            var play=view.playback; int start=app.Session.Snapshot.run.position;
            AssertRendered(play.numericDie); AssertRendered(play.pawn.GetComponent<LoopGlyphGraphic>());
            Assert.That(play.cells[1].anchoredPosition.y,Is.GreaterThan(play.cells[0].anchoredPosition.y),"Clockwise route starts upwards on the left edge");
            Capture("board-720x1280");
            var originalButton=view.buttons.First(b=>b.label.text.StartsWith("이동 주사위"));
            Click("이동 주사위"); string committed=LoopJson.Encode(app.Session.Snapshot);
            Assert.That(app.IsPresenting,Is.True); Assert.That(ui.Root.inputGroup.interactable,Is.False);
            originalButton.button.onClick.Invoke(); Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));
            int roll=app.Session.Snapshot.run.lastTravel;
            var visited=new List<int>(); float nextCapture=0; int frame=0; bool rotating=false;
            yield return WaitPlayback(()=>{
                rotating|=play.DiceRolling && play.numericDie.RollProgress>0 && play.numericDie.RollProgress<1;
                if(play.VisiblePosition!=start && (visited.Count==0 || visited.Last()!=play.VisiblePosition)) visited.Add(play.VisiblePosition);
                if(Time.realtimeSinceStartup>=nextCapture) { Capture("travel-frames/"+(frame++).ToString("000")); nextCapture=Time.realtimeSinceStartup+.08f; }
            });
            CollectionAssert.AreEqual(Enumerable.Range(1,Math.Min(roll,12-start)).Select(i=>(start+i)%12),visited);
            Assert.That(rotating,Is.True); Assert.That(play.numericDie.Result,Is.EqualTo(roll));
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));
            Scenario(r=>{r.phase=Phase.Map;r.position=11;});
            Click("이동 주사위"); committed=LoopJson.Encode(app.Session.Snapshot);
            yield return WaitPlayback(); Assert.That(play.VisiblePosition,Is.Zero); Assert.That(app.Session.Snapshot.run.phase,Is.EqualTo(Phase.LapReward));
            Scenario(r=>{r.phase=Phase.Map;r.position=0;});
            Click("이동 주사위"); committed=LoopJson.Encode(app.Session.Snapshot);
            yield return new WaitForSecondsRealtime(.2f);
            app.SendMessage("OnApplicationPause",true);
            Assert.That(app.IsPresenting,Is.False); Assert.That(ui.Root.inputGroup.interactable,Is.True);
            Assert.That(play.VisiblePosition,Is.EqualTo(app.Session.Snapshot.run.position));
            app.Session.Reload(); app.Render(); Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));
            // Closing the screen during a roll must release the same lock without replaying on reopening.
            Scenario(r=>{r.phase=Phase.Map;r.position=0;}); Click("이동 주사위"); committed=LoopJson.Encode(app.Session.Snapshot);
            ui.CloseAll(); Assert.That(app.IsPresenting,Is.False); app.Render();
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));
        }
        [UnityTest] public IEnumerator SymbolDiceAndEffectsFollowCommittedComboFallbackAndKillLogs()
        {
            Click("새 여정");
            Symbol[][] cases={ new[]{Symbol.Sword},new[]{Symbol.Sword,Symbol.Staff},new[]{Symbol.Sword,Symbol.Staff,Symbol.Fire},
                new[]{Symbol.Sword,Symbol.Fire,Symbol.Shield},new[]{Symbol.Sword,Symbol.Fire,Symbol.Shield} };
            for(int scenario=0;scenario<cases.Length;scenario++)
            {
                var faces=cases[scenario]; int count=faces.Length;
                Scenario(r=>{
                    r.region=count;r.completedRegions=count-1;r.claimed=Enumerable.Range(1,count-1).Select(n=>r.id+":"+n+":lap").ToList();
                    r.dice.Clear();r.lastTurn=null;r.phase=Phase.Combat;r.hp=r.maxHp;
                    for(int i=0;i<count;i++) { r.rules.dice[i].faces=Enumerable.Repeat(faces[i],6).ToArray();r.dice.Add(new DieSlot{instanceId="visual-"+i,definitionId=r.rules.dice[i].id}); }
                    int hp=scenario==4 ? 1 : 150;
                    r.enemy=new Enemy{name="연출 검증 수호자",hp=hp,maxHp=hp,attack=10,intent=Intent.Attack};
                });
                var play=((SymbolLoopScreen)ui.ActiveScreen).playback;
                for(int i=0;i<count;i++) AssertRendered(play.symbolDice[i]);
                Click("기호 주사위"); var committed=app.Session.Snapshot; var log=committed.run.lastTurn;
                string saved=LoopJson.Encode(committed); var skills=new List<string>(); bool retaliation=false,spinning=false;
                float nextCapture=0;int frame=0;
                yield return WaitPlayback(()=>{
                    spinning|=play.DiceRolling;
                    if(play.VisibleSkill!=null && (skills.Count==0 || skills.Last()!=play.VisibleSkill)) skills.Add(play.VisibleSkill);
                    retaliation|=play.EnemyActing;
                    if(!play.DiceRolling) for(int i=0;i<count;i++) Assert.That(play.symbolDice[i].Result,Is.EqualTo((int)log.faces[i]));
                    if(scenario==2 && Time.realtimeSinceStartup>=nextCapture) { Capture("combat-frames/"+(frame++).ToString("000")); nextCapture=Time.realtimeSinceStartup+.08f; }
                });
                Assert.That(spinning,Is.True); Assert.That(retaliation,Is.EqualTo(log.retaliation));
                CollectionAssert.AreEqual(log.skills.Where(s=>s.status=="applied").Select(s=>s.skillId),skills);
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
                if(scenario<4) { Assert.That(play.DisplayedEnemyHp,Is.EqualTo(log.enemyHpAfter)); Assert.That(play.DisplayedPlayerHp,Is.EqualTo(log.playerHpAfter)); }
                Capture("symbols-"+scenario);
            }
        }
        [UnityTest] public IEnumerator TallPortraitShowsChoicesWithoutClipping()
        {
            Resize(720,1600); yield return null; Canvas.ForceUpdateCanvases(); AssertLayout();
            Capture("lobby-720x1600"); Click("새 여정"); AssertLayout();
            Capture("board-720x1600");
            ui.Root.safeArea.anchorMin=new Vector2(.05f,.08f);
            ui.Root.safeArea.anchorMax=new Vector2(.95f,.95f);
            Canvas.ForceUpdateCanvases(); AssertLayout();
            Capture("board-safe-area-720x1600");
            Click("여정 종료"); yield return null; Click("여정 종료 확정"); yield return WaitPlayback();
            var result=((SymbolLoopScreen)ui.ActiveScreen).playback;
            Assert.That(result.tray.activeSelf,Is.False);
            Assert.That(result.cue.text,Is.EqualTo("이번 여정이 끝났습니다"));
            Capture("result-final");
            yield return null;
        }
        IEnumerator Reopen(IGameBackend backend=null)
        {
            Object.Destroy(appObject); yield return null;
            appObject=new GameObject("Reopened symbol app"); appObject.SetActive(false);
            app=appObject.AddComponent<SymbolLoopApp>(); app.ui=ui; app.config=config;
            app.Open(checkpointPath,backend:backend ?? LocalBackend());
            appObject.SetActive(true); yield return null; Canvas.ForceUpdateCanvases();
        }
        IGameBackend LocalBackend()=>new MockGameBackend(new MockAuthorityStore(Path.Combine(directory,"authority.json")),()=>33);
        HttpGameBackend NetworkBackend(HttpMessageHandler handler=null)
        {
            string endpoint=Environment.GetEnvironmentVariable("DICE_WARRIOR_HTTP_TEST_URL");
            Assert.That(string.IsNullOrWhiteSpace(endpoint),Is.False,
                "DICE_WARRIOR_HTTP_TEST_URL must point to an isolated running standalone test host (seed 33, test faults enabled). No local fallback is allowed.");
            var settings=new HttpBackendSettings {endpoint=endpoint,userId="unity-"+Guid.NewGuid().ToString("N"),timeoutSeconds=10};
            checkpointPath=settings.CheckpointPath(directory);
            var backend=new HttpGameBackend(settings,true,handler);networkBackends.Add(backend);return backend;
        }
        [UnityTest] public IEnumerator CorruptCheckpointRecoveryPreservesUnknownStartRequest()
        {
            app.Session.Execute(app.Session.Prepare("loadout","die:arcane"));
            var command=app.Session.Prepare("start"); var disk=new LoopStore(checkpointPath);
            disk.WritePending(command);
            var backend=new MockGameBackend(new MockAuthorityStore(Path.Combine(directory,"authority.json")),()=>33);
            var approved=backend.StartRunAsync(command.backend,CancellationToken.None).GetAwaiter().GetResult();
            File.WriteAllText(checkpointPath,"corrupt checkpoint");
            yield return Reopen(); Assert.That(app.Session,Is.Null);
            Click("백업 내용 확인"); yield return null; Click("이 백업으로 복원"); yield return null;
            Assert.That(disk.ReadPending().id,Is.EqualTo(command.id));
            Click("같은 입력 재시도"); yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.run.id,Is.EqualTo(approved.run.serverRunId));
            Assert.That(app.Session.Snapshot.run.dice[0].definitionId,Is.EqualTo("arcane"));
            Assert.That(disk.ReadPending(),Is.Null);
        }
        sealed class DelayedBackend : IGameBackend
        {
            readonly IGameBackend inner;
            public readonly TaskCompletionSource<bool> release=new TaskCompletionSource<bool>();
            public int starts;
            public DelayedBackend(IGameBackend inner) { this.inner=inner; }
            public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>inner.GetProfileAsync(ct);
            public Task<BackendReply> GetRunStatusAsync(string id,CancellationToken ct)=>inner.GetRunStatusAsync(id,ct);
            public async Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct)
            { starts++; await release.Task; return await inner.StartRunAsync(request,ct); }
            public Task<BackendReply> SubmitRunAsync(BackendRequest r,CancellationToken ct)=>inner.SubmitRunAsync(r,ct);
            public Task<BackendReply> UpgradeAsync(BackendRequest r,CancellationToken ct)=>inner.UpgradeAsync(r,ct);
            public Task<BackendReply> GetOperationStatusAsync(string id,CancellationToken ct)=>inner.GetOperationStatusAsync(id,ct);
        }
        [UnityTest] public IEnumerator DelayedApprovalSurvivesPauseDisableAndBlocksDuplicateInput()
        {
            var backend=new DelayedBackend(new MockGameBackend(new MockAuthorityStore(Path.Combine(directory,"authority.json")),()=>33));
            yield return Reopen(backend);
            var button=((SymbolLoopScreen)ui.ActiveScreen).buttons.First(b=>b.label.text.StartsWith("새 여정"));
            Click("새 여정"); Assert.That(app.IsPresenting,Is.True);
            button.button.onClick.Invoke(); Assert.That(backend.starts,Is.EqualTo(1));
            app.SendMessage("OnApplicationPause",true); appObject.SetActive(false);
            backend.release.SetResult(true);
            float deadline=Time.realtimeSinceStartup+5;
            while(app.Backend.Busy) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline)); yield return null; }
            Assert.That(app.Session.Snapshot.run.seed,Is.EqualTo(33));
            Assert.That(new LoopStore(checkpointPath).ReadPending(),Is.Null);
            appObject.SetActive(true); app.SendMessage("OnApplicationPause",false); yield return null;
            Assert.That(app.IsPresenting,Is.False); Assert.That(backend.starts,Is.EqualTo(1));
            Assert.That(((SymbolLoopScreen)ui.ActiveScreen).title.text,Does.StartWith("REGION"));
            Capture("backend-approved-after-pause");
        }

        sealed class LostSettlementResponse : DelegatingHandler
        {
            bool sent;
            public LostSettlementResponse():base(new HttpClientHandler {AllowAutoRedirect=false,UseCookies=false,UseProxy=false}) { }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
            {
                if(!sent && request.Method==HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/submit",StringComparison.Ordinal))
                { sent=true;request.Headers.Add("X-DiceWarrior-Test-Fault","after-abort"); }
                return base.SendAsync(request,ct);
            }
        }
        [UnityTest] public IEnumerator LostSettlementReplyShowsPendingAndRecoversAfterPauseAndReopen()
        {
            var backend=NetworkBackend(new LostSettlementResponse());
            yield return Reopen(backend);yield return WaitPlayback();
            Assert.That(app.Backend.ProfileReady,Is.True,"The standalone test server must be available.");
            Click("새 여정");yield return WaitPlayback();
            // Resolve the ordinary seed33 commands directly; the separate pointer campaign test covers every turn.
            for(int i=0;app.Session.Snapshot.run.phase!=Phase.Result && i<200;i++)
            {
                var run=app.Session.Snapshot.run;string kind="choose",arg="";
                switch(run.phase)
                {
                    case Phase.Map:kind="travel";break;
                    case Phase.Combat:kind="combat";break;
                    case Phase.LapReward:kind="lap";arg="aegis";break;
                    case Phase.Rest:arg="heal";break;
                    case Phase.Event:arg="study";break;
                    case Phase.Treasure:arg="gear:mail";break;
                    case Phase.Shop:arg="leave";break;
                }
                app.Session.Execute(app.Session.Prepare(kind,arg));
            }
            Assert.That(app.Session.Snapshot.run.won,Is.True);app.Render();yield return null;
            Click("기록 검증");
            var disk=new LoopStore(checkpointPath);string requestId=disk.ReadPending().id;
            app.SendMessage("OnApplicationPause",true);appObject.SetActive(false);
            float deadline=Time.realtimeSinceStartup+15;
            while(app.Backend.Busy) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null; }
            appObject.SetActive(true);app.SendMessage("OnApplicationPause",false);yield return null;
            Assert.That(((SymbolLoopScreen)ui.ActiveScreen).detail.text,Does.Contain("검증 대기"));
            Assert.That(app.Session.Snapshot.run.settled,Is.False);Assert.That(disk.ReadPending().id,Is.EqualTo(requestId));Capture("settlement-pending");
            yield return Reopen(backend);yield return null;Click("같은 입력 재시도");yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.run.settled,Is.True);Assert.That(app.Session.Snapshot.profile.currency,Is.EqualTo(70));
            Assert.That(app.Session.Snapshot.submission.requestId,Is.EqualTo(requestId));Assert.That(disk.ReadPending(),Is.Null);
            Assert.That(((SymbolLoopScreen)ui.ActiveScreen).detail.text,Does.Contain("50 지급"));Capture("settlement-recovered");
        }
    }
}
#endif
