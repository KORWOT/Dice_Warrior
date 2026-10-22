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
    public sealed class CompetitionFlowTests
    {
        const string PrefabPath="Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab";
        GameObject root,appObject,eventObject,cameraObject;
        UIManager ui;
        CompetitionApp app;
        Camera camera;
        RenderTexture target;
        CompetitionDefinition definition;
        string directory,path;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"DiceWarriorCompetitionUI",Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"checkpoint.json");
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-1.json").text);
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));
            ui=root.GetComponentInChildren<UIManager>(true);ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath),AssetDatabase.LoadAssetAtPath<SymbolLoopScreen>("Assets/_Project/Features/SymbolLoop/Prefabs/SymbolLoopScreen.prefab")};
            cameraObject=new GameObject("Competition capture camera");camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;Resize(720,1280);
            eventObject=new GameObject("Competition test events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            appObject=new GameObject("Competition test app");appObject.SetActive(false);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
            app.Open(path,definition,new Profile {growth=2},()=>33);appObject.SetActive(true);
            yield return null;Canvas.ForceUpdateCanvases();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui) ui.CloseAll();if(appObject) Object.Destroy(appObject);if(root) Object.Destroy(root);if(eventObject) Object.Destroy(eventObject);if(cameraObject) Object.Destroy(cameraObject);
            if(target) { target.Release();Object.Destroy(target); }
            yield return null;
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorCompetitionUI"))+Path.DirectorySeparatorChar;
            if(directory!=null && Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        [Test] public void AuthoredCompetitionScreenHasFourDistinctPawnSlots()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab,Is.Not.Null,"The approved new competition screen must be authored.");
            var screen=prefab.GetComponent("CompetitionScreen");
            Assert.That(screen,Is.Not.Null);
            var data=new SerializedObject(screen);
            Assert.That(data.FindProperty("pawns").arraySize,Is.EqualTo(4));
            for(int i=0;i<4;i++) Assert.That(data.FindProperty("pawns").GetArrayElementAtIndex(i).objectReferenceValue,Is.Not.Null);
        }
        void Resize(int width,int height)
        {
            if(target) { target.Release();Object.Destroy(target); }
            target=new RenderTexture(width,height,24);camera.targetTexture=target;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=width/720f;Canvas.ForceUpdateCanvases();
        }
        CompetitionScreen Screen=>(CompetitionScreen)ui.ActiveScreen;
        void Click(string prefix)
        {
            var button=Screen.buttons.First(b=>b.gameObject.activeSelf && b.label.text.StartsWith(prefix,StringComparison.Ordinal));
            ClickButton(button);
        }
        void ClickButton(CommonButtonView button)
        {
            var rect=(RectTransform)button.transform;var input=new PointerEventData(eventObject.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();eventObject.GetComponent<EventSystem>().RaycastAll(input,hits);
            Assert.That(hits.Select(h=>h.gameObject.GetComponentInParent<Button>()).FirstOrDefault(b=>b),Is.EqualTo(button.button),button.label.text+" is obstructed");
            ExecuteEvents.Execute(button.button.gameObject,input,ExecuteEvents.pointerClickHandler);Canvas.ForceUpdateCanvases();
        }
        IEnumerator WaitPlayback(Action observe=null)
        {
            float deadline=Time.realtimeSinceStartup+25;
            while(app.IsPresenting) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Presentation stuck");observe?.Invoke();yield return null; }
            Canvas.ForceUpdateCanvases();
        }
        void Capture(string name)
        {
            string destination="Temp/shared-world-competition/"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(destination));
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(destination,texture.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(texture);
        }
        void AssertLayout()
        {
            foreach(var graphic in Screen.buttons.Where(b=>b.gameObject.activeSelf).Select(b=>(RectTransform)b.transform).Concat(Screen.roster.Select(t=>t.rectTransform)))
            {
                var corners=new Vector3[4];graphic.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();
                Assert.That(points.Min(p=>p.x),Is.GreaterThanOrEqualTo(-1));Assert.That(points.Max(p=>p.x),Is.LessThanOrEqualTo(target.width+1));
                Assert.That(points.Min(p=>p.y),Is.GreaterThanOrEqualTo(-1));Assert.That(points.Max(p=>p.y),Is.LessThanOrEqualTo(target.height+1));
            }
            foreach(var place in Screen.places) Assert.That(place.preferredHeight,Is.LessThanOrEqualTo(place.rectTransform.rect.height+1),place.text+" is clipped");
        }
        [UnityTest] public IEnumerator PointerRoundShowsDiceSkillsAndAllFourParticipants()
        {
            Click("시험 경기 시작");yield return WaitPlayback();
            Assert.That(Screen.pawns.Select(p=>p.anchoredPosition).Distinct().Count(),Is.EqualTo(4),"Same-cell participants need separate slots");
            Assert.That(app.Session.Snapshot.state.participants.Select(p=>p.maxHp).Distinct().Count(),Is.EqualTo(1));
            AssertLayout();Capture("board-720x1280");bool numberRoll=false,symbolRoll=false,skill=false;
            for(int step=0;step<40;step++)
            {
                var state=app.Session.Snapshot.state;var human=state.participants[0];
                if(state.worldRound>=2 && symbolRoll && skill) break;
                switch(human.phase)
                {
                    case CompetitionPhase.Map:Click("이동 주사위");break;
                    case CompetitionPhase.Encounter:
                        var choice=human.offer.choices.FirstOrDefault(c=>c.available && c.kind==EncounterKind.Combat) ?? human.offer.choices.First(c=>c.available);Click(choice.label);break;
                    case CompetitionPhase.Combat:Capture("combat-720x1280");Click("기호 주사위");break;
                    case CompetitionPhase.LapReward:Click(human.completedLaps<2?definition.combat.dice[0].name+" 주사위 추가":definition.combat.gear[0].name);break;
                    case CompetitionPhase.Complete:Click("다음 AI");break;
                }
                yield return WaitPlayback(()=>{numberRoll|=Screen.playback.MovementRolling;symbolRoll|=Screen.playback.human.DiceRolling;skill|=Screen.playback.human.VisibleSkill!=null;});
            }
            var result=app.Session.Snapshot;
            Assert.That(result.state.worldRound,Is.GreaterThanOrEqualTo(2));Assert.That(numberRoll && symbolRoll && skill,Is.True,"Movement, symbols and an applied skill must render");
            Assert.That(result.state.participants.Skip(1).All(p=>p.lastTravel>0),Is.True);Assert.That(Screen.history.text,Is.Not.Empty);Capture("round-summary-720x1280");
            string saved=LoopJson.Encode(result);app.Session.Reload();app.Render();Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
        }
        [UnityTest] public IEnumerator InterruptedMovementReopensSavedStateWithoutRepeatingAi()
        {
            Click("시험 경기 시작");yield return WaitPlayback();Click("이동 주사위");yield return null;
            Assert.That(app.IsPresenting,Is.True);string committed=LoopJson.Encode(app.Session.Snapshot);
            app.Close();app.Open(path,definition,new Profile {growth=2},()=>33);yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));Assert.That(app.IsPresenting,Is.False);
            Assert.That(Screen.playback.VisiblePosition,Is.EqualTo(app.Session.Snapshot.state.participants[0].position));
        }
        [UnityTest] public IEnumerator PortraitAndInsetSafeAreaKeepPlacesParticipantsAndActionsVisible()
        {
            Click("시험 경기 시작");yield return WaitPlayback();
            foreach(int height in new[]{1280,1600})
            {
                Resize(720,height);yield return null;ui.Root.safeArea.anchorMin=new Vector2(.025f,.035f);ui.Root.safeArea.anchorMax=new Vector2(.975f,.955f);
                Canvas.ForceUpdateCanvases();app.Render();yield return null;AssertLayout();Capture("safe-area-720x"+height);
                foreach(var pawn in Screen.pawns)
                {
                    var graphic=pawn.GetComponent<LoopGlyphGraphic>();var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                }
            }
        }
        [UnityTest] public IEnumerator LegacyLobbyEntryRefreshesProfileAndKeepsModeOwnershipAcrossDelayedResponse()
        {
            var legacyObject=new GameObject("Isolated legacy mode entry");legacyObject.SetActive(false);
            var legacy=legacyObject.AddComponent<SymbolLoopApp>();var backend=new ProfileBackend();
            try
            {
                app.Close();appObject.SetActive(false);
                legacy.ui=ui;legacy.config=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>("Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset");
                app.legacy=legacy;app.combatConfig=legacy.config;app.content=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-1.json");
                appObject.SetActive(true);
                legacy.Open(Path.Combine(directory,"legacy","checkpoint.json"),backend:backend);legacyObject.SetActive(true);
                yield return null;
                Assert.That(legacy.Backend.ProfileReady,Is.True);Assert.That(ui.ActiveScreen,Is.TypeOf<SymbolLoopScreen>());
                backend.HoldNextProfile();
                var entry=((SymbolLoopScreen)ui.ActiveScreen).buttons.Single(b=>b.gameObject.activeSelf && b.label.text=="공동 세계 시험");
                ClickButton(entry);yield return null;
                Assert.That(ui.ActiveScreen,Is.TypeOf<CompetitionScreen>());Assert.That(app.IsPresenting,Is.True);
                var pendingScreen=ui.ActiveScreen;long binding=pendingScreen.BindingRevision;
                appObject.SetActive(false);
                legacy.Render();legacy.enabled=false;legacy.enabled=true;
                legacy.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);
                legacy.SendMessage("OnApplicationPause",false,SendMessageOptions.RequireReceiver);
                Assert.That(ui.ActiveScreen,Is.SameAs(pendingScreen),"Legacy lifecycle must not take ownership of the new mode.");
                backend.Current=new BackendProfile {revision=2,values=new Profile {growth=4,currency=20}};backend.ReleaseProfile();
                yield return WaitPlayback();
                Assert.That(ui.ActiveScreen,Is.SameAs(pendingScreen));
                Assert.That(pendingScreen.BindingRevision,Is.EqualTo(binding),"A delayed profile response must not rebind a disabled competition app.");
                appObject.SetActive(true);yield return null;
                Assert.That(ui.ActiveScreen,Is.TypeOf<CompetitionScreen>());Assert.That(app.Session.Snapshot.state,Is.Null);
                backend.Current=new BackendProfile {revision=3,values=new Profile {growth=5,currency=20}};
                Click("시험 경기 시작");yield return WaitPlayback();
                var state=app.Session.Snapshot.state;
                Assert.That(backend.ProfileReads,Is.EqualTo(3),"Lobby load, mode entry and match start each confirm the profile.");
                Assert.That(state.startingProfile.growth,Is.EqualTo(5));
                Assert.That(state.participants.All(p=>p.maxHp==definition.combat.hp+25),Is.True);
                Assert.That(legacy.Session.Snapshot.run,Is.Null);Assert.That(legacy.Session.Snapshot.profile.currency,Is.EqualTo(20));
                string saved=LoopJson.Encode(app.Session.Snapshot);legacy.Render();Assert.That(ui.ActiveScreen,Is.TypeOf<CompetitionScreen>());
                Click("기존 모험으로");yield return null;
                Assert.That(ui.ActiveScreen,Is.TypeOf<SymbolLoopScreen>());Assert.That(app.IsOpen,Is.False);
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
                Assert.That(((SymbolLoopScreen)ui.ActiveScreen).buttons.Any(b=>b.gameObject.activeSelf && b.label.text=="공동 세계 시험"),Is.True);
            }
            finally { appObject.SetActive(false);app.legacy=null;Object.Destroy(legacyObject); }
        }
        sealed class ProfileBackend : IGameBackend
        {
            public BackendProfile Current=new BackendProfile {revision=1,values=new Profile {growth=3,currency=20}};
            public int ProfileReads;
            TaskCompletionSource<BackendReply> held;
            public void HoldNextProfile()=>held=new TaskCompletionSource<BackendReply>();
            public void ReleaseProfile() { var pending=held;held=null;pending.SetResult(Reply()); }
            BackendReply Reply()=>new BackendReply {status=BackendStatus.Approved,profile=LoopJson.Copy(Current)};
            public Task<BackendReply> GetProfileAsync(CancellationToken ct) { ++ProfileReads;return held!=null?held.Task:Task.FromResult(Reply()); }
            static Task<BackendReply> Unexpected()=>Task.FromException<BackendReply>(new InvalidOperationException("This fixture only serves profile reads."));
            public Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> SubmitRunAsync(BackendRequest request,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> UpgradeAsync(BackendRequest request,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> GetOperationStatusAsync(string requestId,CancellationToken ct)=>Unexpected();
            public Task<BackendReply> GetRunStatusAsync(string serverRunId,CancellationToken ct)=>Unexpected();
        }
    }
}
#endif
