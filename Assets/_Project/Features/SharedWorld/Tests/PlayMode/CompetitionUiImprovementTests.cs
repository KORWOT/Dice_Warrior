#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiceWarrior.SymbolLoop;
using FateDice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DiceWarrior.SharedWorld.Tests
{
    [Category("V5UiImprovement11")]
    public sealed class CompetitionUiImprovementTests
    {
        const string PrefabPath="Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab";
        GameObject root,appObject,eventObject,cameraObject;
        UIManager ui;
        CompetitionApp app;
        Camera camera;
        RenderTexture target;
        CompetitionDefinition definition;
        string directory,path;
        CompetitionScreen Screen=>(CompetitionScreen)ui.ActiveScreen;
        V5ShellView Shell=>Screen.v5Shell;
        ParticipantState Human=>app.Session.Snapshot.state.participants[0];
        string AccountPath=>Path.Combine(directory,"account.json");

        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.GetFullPath(Path.Combine("artifacts/v5-ui-improvement-11/tests",Guid.NewGuid().ToString("N").Substring(0,8)));
            Directory.CreateDirectory(directory);path=Path.Combine(directory,"match.json");
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-5.json").text);
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));
            ui=root.GetComponentInChildren<UIManager>(true);ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath)};
            cameraObject=new GameObject("Isolated UI improvement camera");camera=cameraObject.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;Resize(720,1280);
            eventObject=new GameObject("Isolated UI improvement events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            appObject=new GameObject("Isolated UI improvement app");appObject.SetActive(false);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
            // Legal starting account growth, frozen equally for all four entrants. No match-state injection.
            app.Open(path,definition,new Profile {growth=20},()=>33);appObject.SetActive(true);
            yield return null;Canvas.ForceUpdateCanvases();Assert.That(Shell,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui)ui.CloseAll();if(appObject)Object.Destroy(appObject);if(root)Object.Destroy(root);
            if(eventObject)Object.Destroy(eventObject);if(cameraObject)Object.Destroy(cameraObject);
            if(target){target.Release();Object.Destroy(target);}yield return null;
            // Preserve isolated checkpoints/captures as evidence; never open actual account or HTTP storage.
        }

        [UnityTest] public IEnumerator ThreeRealLapsExposeEveryGearAndClaimExactlyOnceAfterReadOnlyNavigation()
        {
            yield return Click("new");yield return Wait();
            var visited=new HashSet<CompetitionPhase>();bool forcedReroll=false;int commands=0;
            while(!(Human.phase==CompetitionPhase.LapReward && Human.completedLaps==2))
            {
                Assert.That(++commands,Is.LessThan(400),"Real command journey must reach the third reward within a bounded run.");
                Assert.That(app.Session.Snapshot.state.finished || Human.eliminated,Is.False,"Journey ended before the third reward: "+Human.message);
                if(visited.Add(Human.phase))yield return ReadOnlyRoundTrip();
                FastStep(ref forcedReroll);
                yield return null;
            }
            app.Render();yield return null;
            Assert.That(Shell.Data.mode,Is.EqualTo("reward"));Assert.That(Shell.Data.choices.Length,Is.EqualTo(5));
            yield return Capture("third-lap-reward-top");
            Assert.That(Human.claimed.Count(x=>x.Contains(":lap:")),Is.EqualTo(2));
            var before=app.Session.Snapshot;string exact=LoopJson.Encode(before),disk=File.ReadAllText(path);
            foreach(var gear in definition.combat.gear)
            {
                var action=Shell.Data.choices.Single(x=>x.id=="lap:"+gear.id);
                Assert.That(action.label,Does.Contain(gear.name));Assert.That(action.detail,Is.Not.Empty);
                Assert.That(action.detail,Does.Contain("→"),"Every gear candidate must expose an actual comparison.");
                var button=Find(action.id);yield return Reveal(button);AssertPointer(button);AssertInside(Bounds((RectTransform)button.transform),Bounds(Shell.viewport),"Reward card");
            }
            yield return Capture("third-lap-reward-fifth-gear");
            yield return Click("net");Assert.That(Screen.v5Net.gameObject.activeInHierarchy,Is.True);
            Assert.That(app.HandleBack(),Is.True);yield return null;Assert.That(Shell.Data.mode,Is.EqualTo("reward"));
            yield return Click("details");Assert.That(Shell.Data.mode,Is.EqualTo("details"));
            foreach(string tab in new[]{"stats","skills","elements","tags","effects"})yield return Click(tab);
            Assert.That(app.HandleBack(),Is.True);yield return null;Assert.That(Shell.Data.mode,Is.EqualTo("reward"));
            yield return ReadOnlyRoundTrip();
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(exact));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
            var chosen=definition.combat.gear.Last();yield return Click("lap:"+chosen.id);yield return Wait();
            Assert.That(Human.completedLaps,Is.EqualTo(3));Assert.That(Human.phase,Is.EqualTo(CompetitionPhase.Combat));Assert.That(Human.enemy.boss,Is.True);
            yield return Capture("third-lap-final-trial");
            Assert.That(Human.equipment[chosen.slot],Is.EqualTo(chosen.id));
            string claim=before.state.matchId+":"+Human.id+":lap:3";
            Assert.That(Human.claimed.Count(x=>x==claim),Is.EqualTo(1));
            Assert.That(app.Session.Snapshot.commands.Count(x=>x.kind=="lap"),Is.EqualTo(3));
            Assert.That(app.Session.Snapshot.commands.Count,Is.EqualTo(before.commands.Count+1));
            string received=LoopJson.Encode(app.Session.Snapshot);yield return ReadOnlyRoundTrip();
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(received));Assert.That(Human.claimed.Count(x=>x==claim),Is.EqualTo(1));
            foreach(string kind in new[]{"travel","choose","combat_roll","combat_reroll","combat_execute","lap"})
                Assert.That(app.Session.Snapshot.commands.Any(x=>x.kind==kind),Is.True,"Missing real command: "+kind);
            Assert.That(visited,Does.Contain(CompetitionPhase.Map));Assert.That(visited,Does.Contain(CompetitionPhase.Encounter));Assert.That(visited,Does.Contain(CompetitionPhase.Combat));
        }

        [UnityTest] public IEnumerator ConsecutiveNextLoadoutSelectionsAndBackNeverReplaceOrAbandonTheSavedMatch()
        {
            var backend=new MemoryBackend();yield return OpenAccount(backend);
            yield return Click("new");yield return Wait();string original=LoopJson.Encode(app.Session.Snapshot),disk=File.ReadAllText(path);
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("abandon");
            Assert.That(Shell.Data.mode,Is.EqualTo("confirm"));Assert.That(app.HandleBack(),Is.True);yield return null;
            Assert.That(Shell.Data.mode,Is.EqualTo("menu"));yield return Click("leave");yield return Click("loadout");
            foreach(string id in new[]{"die:arcane","die:steel","weapon:wand","weapon:blade"})
            {
                yield return Click(id);Assert.That(Shell.Data.mode,Is.EqualTo("loadout"));
                Assert.That(Shell.Data.choices.Single(x=>x.id==id).selected,Is.True);
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(original));
                Assert.That(Shell.notice.text,Does.Contain("다음 새 모험"));
            }
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("heroes");
            foreach(var hero in definition.adventurers.Take(2))
            {yield return Click(hero.id);Assert.That(Shell.Data.mode,Is.EqualTo("adventurers"));Assert.That(Shell.Data.choices.Single(x=>x.id==hero.id).selected,Is.True);}
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("new");
            Assert.That(Shell.Data.mode,Is.EqualTo("confirm"));Assert.That(app.HandleBack(),Is.True);yield return null;
            Assert.That(Shell.Data.mode,Is.EqualTo("lobby"));Assert.That(app.HandleBack(),Is.False);
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(original));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
            Assert.That(Directory.GetFiles(directory,"match.json.restarted-*"),Is.Empty);
            yield return OpenAccount(backend);Assert.That(app.Account.Profile.startingDie,Is.EqualTo("steel"));Assert.That(app.Account.Profile.startingWeapon,Is.EqualTo("blade"));
            yield return Click("resume");Assert.That(app.InLobby,Is.False);Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(original));
            Assert.That(Human.eliminated,Is.False);Assert.That(app.Session.Snapshot.commands,Is.Empty);
        }

        [UnityTest] public IEnumerator AccountPurchaseShowsPendingThenApprovedAndExplainsInsufficientOrFailedRequests()
        {
            var backend=new MemoryBackend();yield return OpenAccount(backend);yield return Click("account");
            Assert.That(Shell.body.text,Does.Contain("100 → 80"));Assert.That(Shell.body.text,Does.Contain("승인 전"));
            backend.HoldUpgrade=true;yield return Click("grow");
            Assert.That(app.IsPresenting,Is.True);Assert.That(Find("grow").button.interactable,Is.False);
            Assert.That(app.Account.Profile.growth,Is.Zero);Assert.That(app.Account.Profile.currency,Is.EqualTo(100));
            Assert.That(Shell.notice.text,Does.Contain("처리 중"));Assert.That(app.HandleBack(),Is.False);
            backend.ReleaseUpgrade();yield return Wait();
            Assert.That(app.Account.Profile.growth,Is.EqualTo(1));Assert.That(app.Account.Profile.currency,Is.EqualTo(80));
            Assert.That(Shell.notice.text,Does.Contain("구매 완료"));Assert.That(Shell.notice.text,Does.Contain("100 → 80"));Assert.That(backend.Applications,Is.EqualTo(1));
            backend.Current=new BackendProfile {revision=3,values=new Profile {growth=1,currency=5}};
            yield return Click("refresh");yield return Wait();
            Assert.That(Find("grow").button.interactable,Is.False);Assert.That(Shell.body.text,Does.Contain("재화 부족"));Assert.That(Shell.body.text,Does.Contain("35"));
            backend.Current=new BackendProfile {revision=4,values=new Profile {growth=1,currency=100}};
            yield return Click("refresh");yield return Wait();backend.FailUpgrade=true;yield return Click("grow");yield return Wait();
            Assert.That(app.Account.Profile.growth,Is.EqualTo(1));Assert.That(app.Account.Profile.currency,Is.EqualTo(100));
            Assert.That(app.Account.Pending,Is.Not.Null);Assert.That(Find("grow").button.interactable,Is.False);
            Assert.That(Shell.notice.text,Does.Contain("미확정"));Assert.That(Shell.Data.choices.Any(x=>x.id=="retry"),Is.True);
            Assert.That(backend.Applications,Is.EqualTo(1));Assert.That(backend.SoloCalls,Is.Zero);
        }

        [UnityTest] public IEnumerator EncounterCompletionShowsAiProgressThenCommittedResultsAndAllowsSafeAbandon()
        {
            yield return Click("new");yield return Wait();yield return Click("travel:");yield return Wait();
            Assert.That(Human.phase,Is.EqualTo(CompetitionPhase.Encounter));
            var choice=Human.offer.choices.First(x=>x.available && x.kind!=EncounterKind.Combat);
            int round=app.Session.Snapshot.state.worldRound;yield return Click("choose:"+choice.id);
            Assert.That(app.IsPresenting,Is.True);Assert.That(Shell.notice.text,Does.Contain("AI 행동 결과"));
            yield return Wait();Assert.That(app.Session.Snapshot.state.worldRound,Is.EqualTo(round+1));
            Assert.That(Shell.Data.mode,Is.EqualTo("board"));Assert.That(Shell.body.text,Does.Contain("직전 AI 행동 결과"));
            foreach(var result in app.Session.Snapshot.state.lastRound)Assert.That(Shell.body.text,Does.Contain(result.name));
            Assert.That(Shell.notice.text,Does.Contain("사건 해결"));
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("abandon");yield return Click("abandon:");yield return Wait();
            Assert.That(Human.eliminated,Is.True);Assert.That(Shell.Data.mode,Is.EqualTo("spectator"));
            yield return ReadOnlyRoundTrip();Assert.That(Find("advance:").button.interactable,Is.True);
        }

        [UnityTest] public IEnumerator LegacySoloRecordHoldsGrowthWithReasonAndNeverDispatchesPurchase()
        {
            var backend=new MemoryBackend();backend.ActiveRun=new RunTicket {serverRunId="isolated-preserved-solo",seed=33,rules=LoopJson.Copy(definition.combat),startingProfile=LoopJson.Copy(backend.Current)};
            var store=new LoopStore(AccountPath);var legacy=new LoopSession(store,definition.combat);legacy.RestoreActiveRun(backend.ActiveRun,backend.Current);
            string original=File.ReadAllText(AccountPath);yield return OpenAccount(backend);yield return Click("account");
            Assert.That(app.Account.HasLegacyRun,Is.True);Assert.That(Find("grow").button.interactable,Is.False);
            Assert.That(Shell.body.text,Does.Contain("구매 보류"));Assert.That(Shell.body.text,Does.Contain("종료/정산 기능이 없습니다"));
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("new");yield return Wait();
            Assert.That(app.Session.Snapshot.state,Is.Not.Null);Assert.That(File.ReadAllText(AccountPath),Is.EqualTo(original));
            Assert.That(backend.UpgradeCalls,Is.Zero);Assert.That(backend.SoloCalls,Is.Zero);
        }

        [UnityTest] public IEnumerator LongKoreanCardsScrollAndReceiveRealPointerAtThreeSizesAndSyntheticSafeArea()
        {
            string checkpoint=LoopJson.Encode(app.Session.Snapshot);int selected=-1,toolClicks=0,backClicks=0;
            foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(720,1600),new Vector2Int(360,640)})
            {
                Resize(size.x,size.y);yield return null;
                ui.Root.safeArea.anchorMin=new Vector2(.03f,.04f);ui.Root.safeArea.anchorMax=new Vector2(.97f,.96f);Canvas.ForceUpdateCanvases();
                var choices=Enumerable.Range(0,8).Select(i=>new V5ShellAction {id="long-"+i,label=$"{i+1}번 모험가의 아주 긴 한국어 선택 문구와 보상 수령 확인",
                    detail="현재 능력치와 다음 모험 구성을 자세하게 비교합니다.\n선택 후의 효과와 적용 범위를 확인할 수 있습니다.",invoke=()=>selected=i}).ToArray();
                ui.Show<CompetitionScreen>(new CompetitionScreenData {shell=new V5ShellData {mode="geometry-"+size,title="화면 경계와 긴 문구 확인",summary="가상 안전영역 안의 세로 화면 검사",body="스크롤해서 모든 선택 카드를 확인합니다.",choices=choices,
                    tools=new[]{new V5ShellAction {id="tool",label="안내 보기",invoke=()=>toolClicks++}},back=()=>backClicks++}});
                yield return null;
                Assert.That(Screen.board.activeSelf,Is.False);Assert.That(Screen.v5Area.gameObject.activeSelf,Is.False);
                for(int i=0;i<choices.Length;i++)
                {
                    yield return Click("long-"+i);Assert.That(selected,Is.EqualTo(i));
                    var button=Find("long-"+i);AssertInside(Bounds((RectTransform)button.transform),Bounds(Shell.viewport),"Scrolled choice");
                    Assert.That(button.label.preferredHeight,Is.LessThanOrEqualTo(button.label.rectTransform.rect.height+1),"Long Korean title must fit its authored card.");
                    Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(96),"Design units only; this is not physical-device dp evidence.");
                }
                Assert.That(Shell.ChoicePage,Is.EqualTo(1));yield return Click("tool");yield return Tap(Shell.back);
                AssertInside(Bounds(Shell.viewport),Bounds(ui.Root.safeArea),"Viewport");
                AssertInside(Bounds((RectTransform)Shell.tools[0].transform),Bounds(ui.Root.safeArea),"Tool");
                AssertInside(Bounds((RectTransform)Shell.back.transform),Bounds(ui.Root.safeArea),"Back");
                yield return Capture("shell-"+size.x+"x"+size.y);
            }
            Assert.That(toolClicks,Is.EqualTo(3));Assert.That(backClicks,Is.EqualTo(3));Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(checkpoint));
        }

        void FastStep(ref bool forcedReroll)
        {
            var state=app.Session.Snapshot.state;var p=state.participants[0];string kind,arg="";
            switch(p.phase)
            {
                case CompetitionPhase.Map:kind="travel";break;
                case CompetitionPhase.Encounter:
                    kind="choose";var legal=CompetitionCommandRules.LegalChoices(state,p);
                    var choice=!forcedReroll?legal.FirstOrDefault(x=>x.kind==EncounterKind.Combat):null;
                    choice=choice??legal.OrderBy(x=>x.kind==EncounterKind.Combat?1:0).ThenByDescending(x=>x.heal).ThenBy(x=>x.hpCost).First();arg=choice.id;break;
                case CompetitionPhase.LapReward:kind="lap";arg=definition.combat.dice[0].id;break;
                case CompetitionPhase.Combat:
                    if(p.v5.selection.phase==V5SelectionPhase.AwaitRoll)kind="combat_roll";
                    else if(p.v5.selection.rerollsLeft>0 && (!forcedReroll || p.v5.selection.results.Any(x=>x.faceId==CompetitionV5Content.BlankFaceId)))
                    {kind="combat_reroll";forcedReroll=true;}
                    else kind="combat_execute";
                    break;
                default:throw new AssertionException("Unexpected journey phase: "+p.phase+" / "+p.message);
            }
            app.Session.Execute(app.Session.Prepare(kind,arg));
        }
        IEnumerator ReadOnlyRoundTrip()
        {
            app.Render();yield return null;string before=LoopJson.Encode(app.Session.Snapshot),disk=File.ReadAllText(path);var phase=Human.phase;
            Assert.That(app.HandleBack(),Is.True);yield return null;Assert.That(Shell.Data.mode,Is.EqualTo("menu"));
            yield return Click("details");Assert.That(Shell.Data.mode,Is.EqualTo("details"));
            Assert.That(app.HandleBack(),Is.True);yield return null;Assert.That(Shell.Data.mode,Is.EqualTo("menu"));
            Assert.That(app.HandleBack(),Is.True);yield return null;Assert.That(Human.phase,Is.EqualTo(phase));
            Assert.That(app.HandleBack(),Is.True);yield return null;yield return Click("leave");Assert.That(app.InLobby,Is.True);
            yield return Click("resume");Assert.That(app.InLobby,Is.False);Assert.That(Human.phase,Is.EqualTo(phase));
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
        }
        IEnumerator OpenAccount(MemoryBackend backend)
        {
            Object.Destroy(appObject);yield return null;
            appObject=new GameObject("Isolated UI account app");appObject.SetActive(false);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
            app.OpenAccount(AccountPath,path,definition,backend,()=>33);appObject.SetActive(true);yield return null;yield return Wait();
        }
        CommonButtonView Find(string id)=>Shell.choices.Concat(Shell.tools).Concat(Shell.tabs).Concat(new[]{Shell.previous,Shell.next,Shell.back})
            .Single(x=>x.gameObject.activeInHierarchy && x.BoundId==id);
        IEnumerator Click(string id)
        {
            int index=Array.FindIndex(Shell.Data.choices,x=>x.id==id);
            if(index>=0)
            {
                int page=index/Shell.choices.Length;
                while(Shell.ChoicePage<page)yield return Tap(Shell.next);
                while(Shell.ChoicePage>page)yield return Tap(Shell.previous);
            }
            yield return Tap(Find(id));
        }
        IEnumerator Reveal(CommonButtonView button)
        {
            Canvas.ForceUpdateCanvases();
            if(button.transform.IsChildOf(Shell.scrollContent))
            {
                var rect=(RectTransform)button.transform;float localY=Shell.scrollContent.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                float range=Shell.scrollContent.rect.height-Shell.viewport.rect.height;
                Shell.scroll.StopMovement();Shell.scroll.verticalNormalizedPosition=range<=0?1:1-Mathf.Clamp01((-localY-Shell.viewport.rect.height*.5f)/range);
            }
            yield return null;Canvas.ForceUpdateCanvases();
        }
        IEnumerator Tap(CommonButtonView button)
        {
            yield return Reveal(button);var pointer=AssertPointer(button);
            ExecuteEvents.Execute(button.button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Canvas.ForceUpdateCanvases();
        }
        PointerEventData AssertPointer(CommonButtonView button)
        {
            Assert.That(button.button.interactable,Is.True,button.BoundId);var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(eventObject.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();eventObject.GetComponent<EventSystem>().RaycastAll(pointer,hits);Assert.That(hits,Is.Not.Empty,button.BoundId);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button.button),"Top raycast must hit "+button.BoundId+"; hits: "+string.Join(" / ",hits.Select(x=>x.gameObject.name)));
            Assert.That(Bounds(ui.Root.safeArea).Contains(pointer.position),Is.True,"Pointer must be inside safe area: "+button.BoundId);
            if(button.transform.IsChildOf(Shell.scrollContent))Assert.That(Bounds(Shell.viewport).Contains(pointer.position),Is.True,"Scroll target center must be inside viewport.");
            return pointer;
        }
        IEnumerator Wait()
        {float deadline=Time.realtimeSinceStartup+20;while(app.IsPresenting){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}Canvas.ForceUpdateCanvases();}
        void Resize(int width,int height)
        {
            if(target){target.Release();Object.Destroy(target);}target=new RenderTexture(width,height,24);camera.targetTexture=target;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=1;Canvas.ForceUpdateCanvases();
        }
        Rect Bounds(RectTransform rect)
        {var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(x=>RectTransformUtility.WorldToScreenPoint(camera,x)).ToArray();return Rect.MinMaxRect(points.Min(x=>x.x),points.Min(x=>x.y),points.Max(x=>x.x),points.Max(x=>x.y));}
        static void AssertInside(Rect inner,Rect outer,string context)
        {
            Assert.That(inner.xMin,Is.GreaterThanOrEqualTo(outer.xMin-1),context);Assert.That(inner.xMax,Is.LessThanOrEqualTo(outer.xMax+1),context);
            Assert.That(inner.yMin,Is.GreaterThanOrEqualTo(outer.yMin-1),context);Assert.That(inner.yMax,Is.LessThanOrEqualTo(outer.yMax+1),context);
        }
        IEnumerator Capture(string name)
        {
            yield return null;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.Destroy(texture);}
        }
        sealed class MemoryBackend:IGameBackend
        {
            public BackendProfile Current=new BackendProfile {revision=1,values=new Profile {currency=100}};
            public RunTicket ActiveRun;
            public int UpgradeCalls,Applications,SoloCalls;
            public bool HoldUpgrade,FailUpgrade;
            TaskCompletionSource<BackendReply> held;
            BackendRequest waiting;
            readonly Dictionary<string,BackendReply> replies=new Dictionary<string,BackendReply>();
            BackendReply Reply()=>new BackendReply {status=BackendStatus.Approved,profile=LoopJson.Copy(Current),run=LoopJson.Copy(ActiveRun)};
            public Task<BackendReply> GetProfileAsync(CancellationToken ct)=>Task.FromResult(Reply());
            public Task<BackendReply> UpgradeAsync(BackendRequest request,CancellationToken ct)
            {
                ++UpgradeCalls;if(FailUpgrade)return Task.FromException<BackendReply>(new TimeoutException("시험 성장 요청 실패"));
                if(HoldUpgrade){waiting=LoopJson.Copy(request);held=new TaskCompletionSource<BackendReply>();return held.Task;}
                return Task.FromResult(Approve(request));
            }
            BackendReply Approve(BackendRequest request)
            {
                if(replies.TryGetValue(request.requestId,out var known))return LoopJson.Copy(known);
                Assert.That(request.kind,Is.EqualTo("grow"));Assert.That(request.expectedRevision,Is.EqualTo(Current.revision));Assert.That(ActiveRun,Is.Null);
                Current.values.currency-=20*(Current.values.growth+1);++Current.values.growth;++Current.revision;++Applications;
                var reply=Reply();replies.Add(request.requestId,LoopJson.Copy(reply));return reply;
            }
            public void ReleaseUpgrade(){Assert.That(held,Is.Not.Null);HoldUpgrade=false;var completion=held;held=null;completion.SetResult(Approve(waiting));}
            Task<BackendReply> Unexpected(){++SoloCalls;return Task.FromException<BackendReply>(new InvalidOperationException("No solo requests in UI fixture."));}
            public Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> SubmitRunAsync(BackendRequest request,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> GetRunStatusAsync(string id,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> GetOperationStatusAsync(string id,CancellationToken ct)=>Task.FromResult(replies.TryGetValue(id,out var reply)?LoopJson.Copy(reply):new BackendReply {status=BackendStatus.Rejected,code="OPERATION_NOT_FOUND"});
        }
    }
}
#endif
