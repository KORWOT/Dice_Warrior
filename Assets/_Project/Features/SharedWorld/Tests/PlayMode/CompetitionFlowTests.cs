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
            ui=root.GetComponentInChildren<UIManager>(true);ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath)};
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
            foreach(string field in new[]{"growthPanel","statRows","adventurerCards","growthTitle","growthSummary","growthBody","skillDetails"})
                Assert.That(data.FindProperty(field).objectReferenceValue,Is.Not.Null,field);
            foreach(string field in new[]{"statValues","statSources","cardTexts"})
            {
                var array=data.FindProperty(field);Assert.That(array.arraySize,Is.EqualTo(3));
                for(int i=0;i<3;i++) Assert.That(array.GetArrayElementAtIndex(i).objectReferenceValue,Is.Not.Null,field);
            }
        }
        [Test] public void AuthoredElementDetailsProvideSixReadableStatRows()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath);
            var data=new SerializedObject(prefab);
            foreach(string field in new[]{"attributeRows","attributeBody"})
            {
                var property=data.FindProperty(field);
                Assert.That(property,Is.Not.Null,"The six-stat view requires an authored "+field+" binding.");
                Assert.That(property.objectReferenceValue,Is.Not.Null,field);
            }
            foreach(string field in new[]{"attributeValues","attributeSources"})
            {
                var property=data.FindProperty(field);
                Assert.That(property,Is.Not.Null,"The six-stat view requires authored "+field+" bindings.");
                Assert.That(property.arraySize,Is.EqualTo(6));
                for(int i=0;i<6;i++) Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue,Is.Not.Null,field);
            }
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
            var hit=hits.Select(h=>h.gameObject.GetComponentInParent<Button>()).FirstOrDefault(b=>b);
            string diagnostic=button.label.text+" is obstructed; position="+input.position+"; active="+button.gameObject.activeInHierarchy+"; interactable="+button.button.interactable+
                "; graphics="+string.Join(" | ",button.GetComponentsInChildren<Graphic>(true).Select(g=>$"{g.name}: active={g.isActiveAndEnabled}, depth={g.depth}, absoluteDepth={g.canvasRenderer.absoluteDepth}, raycast={g.raycastTarget}"))+
                "; groups="+string.Join(" | ",button.GetComponentsInParent<CanvasGroup>(true).Select(g=>$"{g.name}: blocks={g.blocksRaycasts}, interactable={g.interactable}, alpha={g.alpha}"));
            if(hit!=button.button) Capture("pointer-failure-"+button.gameObject.name);
            Assert.That(hit,Is.EqualTo(button.button),diagnostic);
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
        void UseGrowth()
        {
            var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-3.json");
            Assert.That(source,Is.Not.Null,"New growth content must exist.");definition=CompetitionContent.Load(source.text);
            app.Open(path,definition,new Profile {growth=2},()=>33);
        }
        string VisibleText()=>string.Join("\n",Screen.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.text));
        void AssertGrowthLayout()
        {
            AssertLayout();
            Assert.That(Screen.growthPanel && Screen.growthPanel.activeInHierarchy,Is.True);
            foreach(var text in Screen.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy && t.transform.IsChildOf(Screen.transform.Find("Portrait frame/Growth details"))))
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name+" is clipped: "+text.text);
        }
        [UnityTest] public IEnumerator AdventurerSelectionChangesOnlyNextMatchAndPersistsAcrossReopen()
        {
            UseGrowth();yield return null;
            Assert.That(Screen.buttons.Any(b=>b.gameObject.activeSelf && b.label.text=="모험가 선택"),Is.True,"The growth lobby must expose adventurers.");
            Click("모험가 선택");yield return null;Assert.That(VisibleText(),Does.Contain("기본 제공"));AssertGrowthLayout();Capture("growth-adventurers");
            Click("룬술사 선택");yield return null;Click("출전 능력치");yield return null;
            Assert.That(VisibleText(),Does.Contain("룬술사").And.Contain("기존 계정 +10"));AssertGrowthLayout();Capture("growth-preview");
            Click("돌아가기");yield return null;Click("새 게임");yield return WaitPlayback();
            var human=app.Session.Snapshot.state.participants[0];Assert.That(human.adventurerId,Is.EqualTo("scholar"));Assert.That(human.maxHp,Is.EqualTo(70));Assert.That(human.power,Is.EqualTo(4));
            string saved=LoopJson.Encode(app.Session.Snapshot);Click("능력치 상세");yield return null;
            Assert.That(VisibleText(),Does.Contain("모험가 Lv.1").And.Contain("기존 계정 +10"));AssertGrowthLayout();Capture("growth-stats");
            Click("주사위·스킬");yield return null;Assert.That(VisibleText(),Does.Contain("현재 가능한 기술").And.Contain("비전"));AssertGrowthLayout();Capture("growth-skills");
            Resize(720,1600);yield return null;ui.Root.safeArea.anchorMin=new Vector2(.03f,.04f);ui.Root.safeArea.anchorMax=new Vector2(.97f,.96f);Canvas.ForceUpdateCanvases();app.Render();yield return null;AssertGrowthLayout();Capture("growth-safe-area");
            Click("돌아가기");yield return null;Click("모험 종료");yield return null;Click("모험가 선택");yield return null;Click("성채의 수호자 선택");yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            app.Close();UseGrowth();yield return null;Click("경기 이어하기");yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Click("모험 종료");yield return null;Click("새 게임");yield return null;Click("새 게임 시작 확정");yield return WaitPlayback();
            human=app.Session.Snapshot.state.participants[0];Assert.That(human.adventurerId,Is.EqualTo("vanguard"));Assert.That(human.guard,Is.EqualTo(6));Assert.That(human.level,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator LegacyMatchKeepsOldRulesUntilConfirmedGrowthRestart()
        {
            Click("새 게임");yield return WaitPlayback();string old=LoopJson.Encode(app.Session.Snapshot);
            app.Close();UseGrowth();yield return null;Click("경기 이어하기");yield return null;Click("능력치 상세");yield return null;
            Assert.That(VisibleText(),Does.Contain("기존 규칙").And.Contain("위력"));Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(old));
            Click("돌아가기");yield return null;Click("모험 종료");yield return null;Click("새 게임");yield return null;Click("새 게임 시작 확정");yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.state.definition.rulesVersion,Is.EqualTo("shared-world-preview-3"));
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"checkpoint.json.restarted-*").Single()),Does.Contain("shared-world-preview-2"));
        }
        [UnityTest] public IEnumerator CorruptAdventurerPreferenceKeepsCurrentAdventureAvailable()
        {
            UseGrowth();yield return null;Click("새 게임");yield return WaitPlayback();string saved=LoopJson.Encode(app.Session.Snapshot);
            app.Close();File.WriteAllText(Path.Combine(directory,"adventurer-selection.json"),"preserve-corrupt-selection");UseGrowth();yield return null;
            var start=Screen.buttons.Single(b=>b.gameObject.activeSelf && b.label.text=="새 게임");Assert.That(start.button.interactable,Is.False);
            Assert.That(VisibleText(),Does.Contain("모험가 선택"));
            Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Assert.That(File.ReadAllText(Path.Combine(directory,"adventurer-selection.json")),Is.EqualTo("preserve-corrupt-selection"));
        }
        [UnityTest] public IEnumerator GrownAdventurerShowsThreeDiceAllSkillsAndEquipmentWithoutChangingMatch()
        {
            UseGrowth();yield return null;Click("새 게임");yield return WaitPlayback();
            var original=app.Session.Snapshot;var fixture=LoopJson.Copy(original);var state=fixture.state;var p=state.participants[0];
            p.completedLaps=2;p.claimed=new List<string>{state.matchId+":"+p.id+":lap:1",state.matchId+":"+p.id+":lap:2"};
            p.dice=definition.combat.dice.Select((d,i)=>new DieSlot {instanceId="detail-"+i,definitionId=d.id}).ToList();
            p.level=3;p.hp=p.maxHp=82;p.power=8;p.guard=6;
            p.equipment=Enumerable.Range(0,3).Select(slot=>definition.combat.gear.First(g=>g.slot==slot).id).ToArray();
            fixture.revision++;new CompetitionStore(path).Commit(fixture,original.revision,original.generation);app.Session.Reload();app.Render();yield return null;
            string saved=LoopJson.Encode(app.Session.Snapshot);Click("능력치 상세");yield return null;
            Assert.That(VisibleText(),Does.Contain("모험가 Lv.3").And.Contain("런 레벨 2").And.Contain("피해 +"));AssertGrowthLayout();Capture("growth-level-equipment");
            Assert.That(Screen.growthBody.text,Does.Not.Contain("강철 피해 +2 · 강철 피해 +2"));
            Assert.That(Screen.statValues.Select(t=>(Color32)t.color),Is.EqualTo(new[]{new Color32(229,108,100,255),new Color32(82,185,135,255),new Color32(114,151,237,255)}));
            Click("주사위·스킬");yield return null;
            foreach(var skill in definition.combat.basics.Concat(definition.combat.combos)) Assert.That(VisibleText(),Does.Contain(skill.name));
            AssertGrowthLayout();Capture("growth-three-dice");
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved),"Detailed views must not consume RNG or mutate growth.");
        }
        void UseElements()
        {
            var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-4.json");
            Assert.That(source,Is.Not.Null,"The v4 content must be authored separately.");
            definition=CompetitionContent.Load(source.text);app.Open(path,definition,new Profile {growth=2},()=>33);
        }
        [UnityTest] public IEnumerator ElementSelectionShowsSixStatsAndKeepsJsonCombatDefinition()
        {
            UseElements();yield return null;
            var json=LoopJson.Copy(definition);json.combat.dice[0].name="JSON 정본 주사위";
            var asset=new TextAsset(LoopJson.Encode(json));
            try
            {
                app.content=asset;app.combatConfig=AssetDatabase.LoadAssetAtPath<SymbolLoopConfig>("Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.asset");
                var loader=typeof(CompetitionApp).GetMethod("LoadDefinition",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var loaded=(CompetitionDefinition)loader.Invoke(app,null);
                Assert.That(loaded.combat.dice[0].name,Is.EqualTo("JSON 정본 주사위"),"v4 must not replace frozen JSON rules with the legacy SO.");
            }
            finally {app.content=null;Object.Destroy(asset);}
            Click("모험가 선택");yield return null;AssertGrowthLayout();Capture("element-adventurers");
            Click("룬술사 선택");yield return null;Click("출전 능력치");yield return null;
            Assert.That(Screen.attributeValues.Select(t=>t.text),Is.EqualTo(new[]{"힘  STR  3","민첩  DEX  4","체질  CON  5","지능  INT  7","정신  WIL  6","행운  LUK  5"}));
            Assert.That(Screen.attributeBody.text,Does.Contain("기본 30 + 체질 30").And.Contain("기존 계정 10 = 70").And.Contain("회피 0%"));
            AssertGrowthLayout();Capture("element-preview");Click("돌아가기");yield return null;
            Click("새 게임");yield return WaitPlayback();Assert.That(app.Session.Snapshot.state.participants[0].adventurerId,Is.EqualTo("scholar"));
            string saved=LoopJson.Encode(app.Session.Snapshot);
            Click("모험 종료");yield return null;Click("모험가 선택");yield return null;Click("성채의 수호자 선택");yield return null;
            Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Click("모험 종료");yield return null;Click("새 게임");yield return null;Click("새 게임 시작 확정");yield return WaitPlayback();
            var next=app.Session.Snapshot.state.participants[0];Assert.That(next.adventurerId,Is.EqualTo("vanguard"));Assert.That(next.maxHp,Is.EqualTo(82));
        }
        [UnityTest] public IEnumerator RgbSavedMatchKeepsItsDetailsUntilConfirmedElementRestart()
        {
            UseGrowth();yield return null;Click("새 게임");yield return WaitPlayback();string saved=LoopJson.Encode(app.Session.Snapshot);
            app.Close();UseElements();yield return null;Click("경기 이어하기");yield return null;Click("능력치 상세");yield return null;
            Assert.That(Screen.statRows.activeInHierarchy,Is.True);Assert.That(Screen.attributeRows.activeInHierarchy,Is.False);
            Assert.That(VisibleText(),Does.Contain("무력").And.Contain("수호").And.Contain("비전"));Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Capture("element-legacy-rgb");Click("돌아가기");yield return null;Click("모험 종료");yield return null;
            Click("새 게임");yield return null;Click("취소");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Click("새 게임");yield return null;Click("새 게임 시작 확정");yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.state.definition.rulesVersion,Is.EqualTo("shared-world-preview-4"));
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"checkpoint.json.restarted-*").Single()),Does.Contain("shared-world-preview-3"));
        }
        [UnityTest] public IEnumerator ElementApplicationResumesEachLegacyVersionUntilExplicitRestart()
        {
            foreach(string version in new[]{"shared-world-preview-1","shared-world-preview-2"})
            {
                app.Close();path=Path.Combine(directory,version,"checkpoint.json");
                var old=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-1.json").text);
                old.rulesVersion=version;app.Open(path,old,new Profile {growth=2},()=>33);yield return null;
                Click("새 게임");yield return WaitPlayback();Click("이동 주사위");yield return WaitPlayback();
                string saved=LoopJson.Encode(app.Session.Snapshot),matchId=app.Session.Snapshot.state.matchId;
                byte[] raw=File.ReadAllBytes(path);
                app.Close();UseElements();yield return null;Click("경기 이어하기");yield return null;
                Assert.That(app.Session.Snapshot.state.definition.rulesVersion,Is.EqualTo(version));
                Click("능력치 상세");yield return null;
                Assert.That(Screen.statRows.activeInHierarchy,Is.False);Assert.That(Screen.attributeRows.activeInHierarchy,Is.False);
                Assert.That(VisibleText(),Does.Contain("기존 규칙 경기").And.Contain("HP 70/70").And.Contain("위력 5 · 수호 4"));
                AssertGrowthLayout();Click("주사위·스킬");yield return null;
                Assert.That(VisibleText(),Does.Contain("기존 위력/수호 규칙"));
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllBytes(path),Is.EqualTo(raw));
                Click("돌아가기");yield return null;Click("모험 종료");yield return null;Click("새 게임");yield return null;
                Assert.That(Screen.detail.text,Does.Contain("초기화"));Click("취소");yield return null;
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllBytes(path),Is.EqualTo(raw));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(path),"checkpoint.json.restarted-*"),Is.Empty);
                Click("새 게임");yield return null;Click("새 게임 시작 확정");yield return WaitPlayback();
                var replacement=app.Session.Snapshot;
                Assert.That(replacement.state.definition.rulesVersion,Is.EqualTo("shared-world-preview-4"));
                Assert.That(replacement.state.matchId,Is.Not.EqualTo(matchId));Assert.That(replacement.commands,Is.Empty);
                Assert.That(new[]{replacement.state.participants[0].maxHp,replacement.state.participants[0].power,replacement.state.participants[0].guard},Is.EqualTo(new[]{70,7,5}));
                Assert.That(File.ReadAllBytes(Directory.GetFiles(Path.GetDirectoryName(path),"checkpoint.json.restarted-*").Single()),Is.EqualTo(raw));
                Assert.That(File.ReadAllBytes(path+".backup"),Is.EqualTo(raw));
            }
        }
        [UnityTest] public IEnumerator ElementDetailPagesFitAllSkillsTagsAndSourcesWithoutChangingMatch()
        {
            UseElements();yield return null;Click("새 게임");yield return WaitPlayback();
            var original=app.Session.Snapshot;var fixture=LoopJson.Copy(original);var p=fixture.state.participants[0];
            p.completedLaps=2;p.claimed=new List<string>{fixture.state.matchId+":"+p.id+":lap:1",fixture.state.matchId+":"+p.id+":lap:2"};
            p.dice=fixture.state.definition.combat.dice.Select((d,i)=>new DieSlot {instanceId="element-detail-"+i,definitionId=d.id}).ToList();
            p.level=3;p.hp=p.maxHp=82;p.power=9;p.guard=7;
            p.equipment=Enumerable.Range(0,3).Select(slot=>definition.combat.gear.First(g=>g.slot==slot).id).ToArray();
            fixture.revision++;new CompetitionStore(path).Commit(fixture,original.revision,original.generation);app.Session.Reload();app.Render();yield return null;
            string saved=LoopJson.Encode(app.Session.Snapshot),raw=File.ReadAllText(path);Click("능력치 상세");yield return null;
            Assert.That(Screen.attributeValues.Select(t=>t.text),Is.EqualTo(new[]{"힘  STR  9","민첩  DEX  8","체질  CON  7","지능  INT  5","정신  WIL  7","행운  LUK  6"}));
            Assert.That(Screen.attributeBody.text,Does.Contain("기본 30 + 체질 42").And.Contain("기존 계정 10 = 82"));
            Assert.That(Screen.attributeValues.Select(t=>(Color32)t.color),Is.EqualTo(new[]{new Color32(229,115,115,255),new Color32(215,166,75,255),new Color32(85,186,139,255),new Color32(112,159,233,255),new Color32(161,138,224,255),new Color32(216,135,187,255)}));
            AssertGrowthLayout();Capture("element-six-stats");
            foreach(int height in new[]{1280,1600})
            {
                if(height==1600)
                {
                    Resize(720,height);yield return null;ui.Root.safeArea.anchorMin=new Vector2(.03f,.04f);ui.Root.safeArea.anchorMax=new Vector2(.97f,.96f);Canvas.ForceUpdateCanvases();
                }
                Click("능력치");yield return null;AssertGrowthLayout();
                if(height==1600) Capture("element-safe-area");
                string skills="";
                foreach(var tab in new[]{"주사위·스킬","속성·상성","태그 조회","효과 출처"})
                {
                    Click(tab);yield return null;int page=0;
                    while(true)
                    {
                        AssertGrowthLayout();Assert.That(Screen.buttons.Count(b=>b.gameObject.activeSelf),Is.LessThanOrEqualTo(8));
                        if(tab=="주사위·스킬") skills+=VisibleText();
                        if(page==0) Capture((tab=="주사위·스킬"?"element-skills":tab=="속성·상성"?"element-affinity":tab=="태그 조회"?"element-tags":"element-sources")+(height==1600?"-720x1600":""));
                        var next=Screen.buttons.FirstOrDefault(b=>b.gameObject.activeSelf && b.label.text=="다음 페이지");
                        if(next==null || !next.button.interactable) break;
                        Assert.That(++page,Is.LessThan(80),"The finite detail pages must end.");Click("다음 페이지");yield return null;
                    }
                }
                foreach(var name in new[]{"베기","마력탄","불꽃","방어","검기","화염탄","수호 베기","화염 검기","성채 베기"}) Assert.That(skills,Does.Contain(name),"Missing skill at 720x"+height);
                Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllText(path),Is.EqualTo(raw));
            }
            Click("돌아가기");yield return null;Click("모험 종료");yield return null;Click("모험가 선택");yield return null;
            Assert.That(Screen.cardTexts.Count(t=>t.gameObject.activeInHierarchy),Is.EqualTo(3));
            foreach(var name in new[]{"방랑 검사","룬술사","성채의 수호자"}) Assert.That(VisibleText(),Does.Contain(name));
            AssertGrowthLayout();Capture("element-adventurers-720x1600");
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllText(path),Is.EqualTo(raw));
        }
        [UnityTest] public IEnumerator ElementCombatDisplaysFrozenAffinityAndResumesWithoutRerolling()
        {
            UseElements();yield return null;Click("새 게임");yield return WaitPlayback();
            var original=app.Session.Snapshot;var fixture=LoopJson.Copy(original);var state=fixture.state;var p=state.participants[0];
            for(int i=0;i<3;i++) state.definition.combat.dice[i].faces=Enumerable.Repeat(i==2?Symbol.Sword:Symbol.Staff,6).ToArray();
            p.completedLaps=2;p.claimed=new List<string>{state.matchId+":"+p.id+":lap:1",state.matchId+":"+p.id+":lap:2"};
            p.dice=new List<DieSlot>{new DieSlot{instanceId="element-a",definitionId="balanced"},new DieSlot{instanceId="element-b",definitionId="arcane"},new DieSlot{instanceId="element-c",definitionId="steel"}};
            var enemy=state.definition.v4.enemies.First(e=>e.encounterId!="final");enemy.attackType=DamageType.Physical;enemy.weakness=DamageType.Ice;enemy.resistance=DamageType.Lightning;
            p.phase=CompetitionPhase.Encounter;p.offer=new EncounterOffer {id="element-offer",placeId=state.definition.world.places[p.position].id,stage=2,visit=1,
                choices=new[]{new EncounterChoice {id=enemy.encounterId,label="속성 연출 검증",description="속성 연출 검증",kind=EncounterKind.Combat,available=true,enemyHp=500,enemyAttack=1},
                    new EncounterChoice {id="harbor-work",label="항구의 짐 운반",description="항구의 짐 운반",kind=EncounterKind.Study,available=true}}};
            fixture.revision++;new CompetitionStore(path).Commit(fixture,original.revision,original.generation);app.Session.Reload();app.Render();yield return null;
            Assert.That(Screen.detail.text,Does.Contain("냉기 +20%").And.Contain("번개 −15%"));
            Assert.That(Screen.detail.preferredHeight,Is.LessThanOrEqualTo(Screen.detail.rectTransform.rect.height+1));Capture("element-encounter-affinity");
            Click("속성 연출 검증");yield return WaitPlayback();Assert.That(Screen.detail.text,Does.Contain("냉기 +20%").And.Contain("번개 −15%"));Capture("element-enemy-affinity");
            var observed=new List<string>();Click("기호 주사위");yield return WaitPlayback(()=>{var id=Screen.playback.human.VisibleSkill;if(id!=null && !observed.Contains(id)) observed.Add(id);});
            Assert.That(observed,Is.EqualTo(new[]{"bolt","arc-slash"}));var log=app.Session.Snapshot.state.participants[0].v4.lastTurn;
            Assert.That(log.skills.Select(s=>s.damageType),Is.EqualTo(new[]{DamageType.Lightning,DamageType.Ice}));
            Assert.That(log.skills.Select(s=>s.affinityBasisPoints),Is.EqualTo(new[]{8500,12000}));
            Assert.That(Screen.detail.text,Does.Contain("최근 피해: 번개").And.Contain("냉기"));Capture("element-combat-result");
            Click("기호 주사위");string saved=LoopJson.Encode(app.Session.Snapshot);appObject.SetActive(false);yield return null;appObject.SetActive(true);yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(app.IsPresenting,Is.False);
        }
        [UnityTest] public IEnumerator PointerRoundShowsDiceSkillsAndAllFourParticipants()
        {
            Click("새 게임");yield return WaitPlayback();
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
            Click("새 게임");yield return WaitPlayback();Click("이동 주사위");yield return null;
            Assert.That(app.IsPresenting,Is.True);string committed=LoopJson.Encode(app.Session.Snapshot);
            app.Close();app.Open(path,definition,new Profile {growth=2},()=>33);yield return null;Click("경기 이어하기");yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(committed));Assert.That(app.IsPresenting,Is.False);
            Assert.That(Screen.playback.VisiblePosition,Is.EqualTo(app.Session.Snapshot.state.participants[0].position));
        }
        [UnityTest] public IEnumerator MainLobbyOffersLoadoutAndReturnsToTheSameCompetition()
        {
            Assert.That(Screen.buttons.Any(b=>b.gameObject.activeSelf && b.label.text=="출전 선택"),Is.True,"The main lobby must expose loadout choices directly.");
            Click("출전 선택");yield return null;Click("비전 주사위");Click("출전 선택");yield return null;Click("불꽃 지팡이");
            Click("새 게임");yield return WaitPlayback();
            var state=app.Session.Snapshot.state;
            Assert.That(state.startingProfile.startingDie,Is.EqualTo("arcane"));Assert.That(state.startingProfile.startingWeapon,Is.EqualTo("wand"));
            string saved=LoopJson.Encode(app.Session.Snapshot);Click("모험 종료");yield return null;
            Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
        }
        [UnityTest] public IEnumerator ExitPreservesAdventureAndNewGameRequiresConfirmation()
        {
            Click("새 게임");yield return WaitPlayback();
            app.Session.Execute(app.Session.Prepare("travel"));app.Render();yield return null;
            string saved=LoopJson.Encode(app.Session.Snapshot),raw=File.ReadAllText(path);
            Assert.That(Screen.buttons.Any(b=>b.gameObject.activeSelf && b.label.text=="모험 종료"),Is.True);
            Click("모험 종료");yield return null;
            Assert.That(app.InLobby,Is.True);Assert.That(File.ReadAllText(path),Is.EqualTo(raw));
            Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Click("모험 종료");yield return null;Click("새 게임");yield return null;
            Assert.That(Screen.detail.text,Does.Contain("초기화"));
            Assert.That(File.ReadAllText(path),Is.EqualTo(raw));Click("취소");yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            Click("새 게임");yield return null;Capture("new-game-warning");Click("새 게임 시작 확정");yield return WaitPlayback();
            Assert.That(app.InLobby,Is.False);Assert.That(app.Session.Snapshot.commands,Is.Empty);
            Assert.That(app.Session.Snapshot.state.actionNumber,Is.Zero);
            Assert.That(app.Session.Snapshot.state.participants.All(p=>p.position==0 && p.dice.Count==1),Is.True);
            Assert.That(Directory.GetFiles(directory,"checkpoint.json.restarted-*").Select(File.ReadAllText),Does.Contain(raw));
            string replacement=LoopJson.Encode(app.Session.Snapshot);app.Session.Reload();
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(replacement));
        }
        [UnityTest] public IEnumerator ThreeDicePlayBasicThenPairComboWithDistinctSkillLabels()
        {
            Click("새 게임");yield return WaitPlayback();
            var original=app.Session.Snapshot;var fixture=LoopJson.Copy(original);var state=fixture.state;var p=state.participants[0];
            for(int i=0;i<3;i++) state.definition.combat.dice[i].faces=Enumerable.Repeat(i==2?Symbol.Sword:Symbol.Staff,6).ToArray();
            p.completedLaps=2;p.claimed=new List<string>{state.matchId+":"+p.id+":lap:1",state.matchId+":"+p.id+":lap:2"};
            p.dice=new List<DieSlot>{new DieSlot{instanceId="a",definitionId="balanced"},new DieSlot{instanceId="b",definitionId="arcane"},new DieSlot{instanceId="c",definitionId="steel"}};
            p.phase=CompetitionPhase.Combat;p.enemy=new Enemy{name="혼합 스킬 검증",hp=500,maxHp=500,attack=1,intent=Intent.Attack};
            fixture.revision++;
            new CompetitionStore(path).Commit(fixture,original.revision,original.generation);app.Session.Reload();app.Render();yield return null;
            var observed=new List<string>();bool basicLabel=false,comboLabel=false,mixedCue=false,captured=false;
            Click("기호 주사위");
            yield return WaitPlayback(()=>{
                var visual=Screen.playback.human;string id=visual.VisibleSkill;
                if(id!=null && !observed.Contains(id)) observed.Add(id);
                if(id=="bolt") basicLabel|=visual.skillName.text.StartsWith("SKILL");
                if(id=="arc-slash") { comboLabel|=visual.skillName.text.StartsWith("COMBO");if(comboLabel && !captured) {Capture("pair-combo");captured=true;} }
                mixedCue|=visual.cue.text.Contains("기본기 + 조합");
            });
            Assert.That(observed,Is.EqualTo(new[]{"bolt","arc-slash"}));Assert.That(basicLabel && comboLabel && mixedCue,Is.True);
            Assert.That(app.Session.Snapshot.state.participants[0].lastTurn.skills.Select(s=>s.skillId),Is.EqualTo(observed));
        }
        [UnityTest] public IEnumerator PortraitAndInsetSafeAreaKeepPlacesParticipantsAndActionsVisible()
        {
            Click("새 게임");yield return WaitPlayback();
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
        string AccountPath=>Path.Combine(directory,"account","checkpoint.json");
        CommonButtonView Button(string prefix)=>Screen.buttons.First(b=>b.gameObject.activeSelf && b.label.text.StartsWith(prefix,StringComparison.Ordinal));
        IEnumerator ReopenAccount(ProfileBackend backend,bool wait=true)
        {
            Object.Destroy(appObject);yield return null;
            appObject=new GameObject("Isolated main account app");appObject.SetActive(false);
            app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
            app.OpenAccount(AccountPath,path,definition,backend,()=>33);appObject.SetActive(true);yield return null;
            if(wait) yield return WaitPlayback();
        }
        static void AssertNoSoloRequests(ProfileBackend backend)
        { Assert.That(backend.StartCalls,Is.Zero);Assert.That(backend.SubmitCalls,Is.Zero); }
        [UnityTest] public IEnumerator DirectAccountEntryPreservesOwnershipAndFreezesLatestGrowth()
        {
            var backend=new ProfileBackend();backend.HoldNextProfile();yield return ReopenAccount(backend,false);
            Assert.That(ui.ActiveScreen,Is.TypeOf<CompetitionScreen>());Assert.That(app.InLobby,Is.True);Assert.That(app.IsPresenting,Is.True);
            var screen=Screen;long binding=screen.BindingRevision;
            appObject.SetActive(false);backend.ReleaseProfile();yield return WaitPlayback();
            Assert.That(screen.BindingRevision,Is.EqualTo(binding),"A delayed profile response must not rebind a disabled app.");
            appObject.SetActive(true);yield return null;Assert.That(app.Account.Ready,Is.True);
            Capture("main-lobby");Click("출전 선택");Capture("loadout");Click("비전 주사위");Click("출전 선택");Click("불꽃 지팡이");
            Click("영구 HP");yield return WaitPlayback();
            Assert.That(backend.UpgradeApplications,Is.EqualTo(1));Assert.That(app.Account.Profile.growth,Is.EqualTo(1));Assert.That(app.Account.Profile.currency,Is.EqualTo(80));
            backend.Current=new BackendProfile {revision=3,values=new Profile {growth=2,currency=80}};
            Click("새 게임");yield return WaitPlayback();
            var state=app.Session.Snapshot.state;
            Assert.That(state.startingProfile.growth,Is.EqualTo(2));Assert.That(state.startingProfile.startingDie,Is.EqualTo("arcane"));
            Assert.That(state.startingProfile.startingWeapon,Is.EqualTo("wand"));Assert.That(state.participants.All(p=>p.maxHp==definition.combat.hp+10),Is.True);
            string saved=LoopJson.Encode(app.Session.Snapshot);
            Click("모험 종료");Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            yield return ReopenAccount(backend);
            Assert.That(app.Account.Profile.startingDie,Is.EqualTo("arcane"));Assert.That(app.Account.Profile.startingWeapon,Is.EqualTo("wand"));
            Click("경기 이어하기");yield return null;Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator UnavailableServerKeepsLobbyAndAllowsExplicitProfileRetry()
        {
            var backend=new ProfileBackend {FailProfile=true};yield return ReopenAccount(backend);
            Assert.That(app.Account.Ready,Is.False);Assert.That(Button("새 게임").button.interactable,Is.False);Assert.That(app.Session.Snapshot.state,Is.Null);
            backend.FailProfile=false;Click("프로필 다시 확인");yield return WaitPlayback();
            Assert.That(app.Account.Ready,Is.True);Click("새 게임");yield return WaitPlayback();Assert.That(app.Session.Snapshot.state,Is.Not.Null);AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator NewGameProfileFailureKeepsSavedAdventureAvailableToResume()
        {
            var backend=new ProfileBackend();yield return ReopenAccount(backend);
            Click("새 게임");yield return WaitPlayback();Click("모험 종료");yield return null;
            string original=File.ReadAllText(path);string saved=LoopJson.Encode(app.Session.Snapshot);
            Click("새 게임");yield return null;backend.FailProfile=true;Click("새 게임 시작 확정");yield return WaitPlayback();
            Assert.That(app.InLobby,Is.True);Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory,"checkpoint.json.restarted-*"),Is.Empty);
            Click("경기 이어하기");yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator PendingGrowthReopensAndRetriesOnlyItsOriginalRequest()
        {
            var backend=new ProfileBackend {LoseUpgradeReply=true};yield return ReopenAccount(backend);
            Click("영구 HP");yield return WaitPlayback();
            var pending=app.Account.Pending;Assert.That(pending,Is.Not.Null);string original=LoopJson.Encode(pending);
            Assert.That(app.Account.Profile.growth,Is.Zero);Assert.That(backend.UpgradeApplications,Is.EqualTo(1));
            yield return ReopenAccount(backend);
            Assert.That(LoopJson.Encode(app.Account.Pending),Is.EqualTo(original));Assert.That(backend.UpgradeCalls,Is.EqualTo(1),"Opening the lobby must not automatically resubmit.");
            Assert.That(Button("영구 HP").button.interactable,Is.False);
            Click("계정 관리");Click("같은 요청 다시 확인");yield return WaitPlayback();
            Assert.That(app.Account.Pending,Is.Null);Assert.That(app.Account.Profile.growth,Is.EqualTo(1));Assert.That(app.Account.Profile.currency,Is.EqualTo(80));
            Assert.That(backend.UpgradeApplications,Is.EqualTo(1));Assert.That(backend.UpgradeRequests.Select(r=>r.requestId).Distinct().Single(),Is.EqualTo(pending.id));
            Assert.That(backend.UpgradeRequests.Select(LoopJson.Encode).Distinct().Count(),Is.EqualTo(1));AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator LocalAccountWriteFailureKeepsApprovedGrowthPendingAndRetryable()
        {
            var backend=new ProfileBackend();yield return ReopenAccount(backend);FileStream held=null;
            try
            {
                backend.OnApprovedUpgrade=()=>held=new FileStream(AccountPath,FileMode.Open,FileAccess.Read,FileShare.None);
                Click("영구 HP");yield return WaitPlayback();
                Assert.That(app.Account.Pending,Is.Not.Null,"A failed local apply must still expose its durable pending operation.");
                string id=app.Account.Pending.id;Assert.That(app.Account.Ready,Is.False);Assert.That(Button("영구 HP").button.interactable,Is.False);
                Assert.That(backend.UpgradeApplications,Is.EqualTo(1));held.Dispose();held=null;backend.OnApprovedUpgrade=null;
                Click("계정 관리");Click("같은 요청 다시 확인");yield return WaitPlayback();
                Assert.That(app.Account.Pending,Is.Null);Assert.That(app.Account.Profile.growth,Is.EqualTo(1));
                Assert.That(backend.UpgradeApplications,Is.EqualTo(1));Assert.That(backend.UpgradeRequests.All(r=>r.requestId==id),Is.True);AssertNoSoloRequests(backend);
            }
            finally { held?.Dispose(); }
        }
        [UnityTest] public IEnumerator OldActiveAdventureIsPreservedWhileNewLoadoutAndMatchRemainAvailable()
        {
            var backend=new ProfileBackend();
            backend.ActiveRun=new RunTicket {serverRunId="preserved-solo-run",seed=33,rules=LoopJson.Copy(definition.combat),startingProfile=LoopJson.Copy(backend.Current)};
            var accountStore=new LoopStore(AccountPath);var oldSession=new LoopSession(accountStore,definition.combat);
            oldSession.RestoreActiveRun(backend.ActiveRun,backend.Current);string original=File.ReadAllText(AccountPath);
            yield return ReopenAccount(backend);
            Assert.That(app.Account.HasLegacyRun,Is.True);Assert.That(Button("영구 성장").button.interactable,Is.False);
            Capture("legacy-growth-held");
            Click("출전 선택");Click("비전 주사위");Click("새 게임");yield return WaitPlayback();
            Assert.That(app.Session.Snapshot.state.startingProfile.startingDie,Is.EqualTo("arcane"));
            Assert.That(File.ReadAllText(AccountPath),Is.EqualTo(original),"New selection must never rewrite the old active adventure.");
            Assert.That(backend.UpgradeCalls,Is.Zero);AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator CorruptAccountAndCompetitionRemainUnchangedAndExposeRecovery()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AccountPath));Directory.CreateDirectory(directory);
            const string badAccount="broken-account-original",badCompetition="broken-competition-original";
            File.WriteAllText(AccountPath,badAccount);File.WriteAllText(path,badCompetition);
            var backend=new ProfileBackend();yield return ReopenAccount(backend);
            Assert.That(app.Account.StorageError,Is.Not.Empty);Assert.That(app.Session,Is.Null);
            Assert.That(File.ReadAllText(AccountPath),Is.EqualTo(badAccount));Assert.That(File.ReadAllText(path),Is.EqualTo(badCompetition));
            Assert.That(Button("경기 저장 다시 읽기"),Is.Not.Null);Click("계정 관리");Assert.That(Button("계정 저장 다시 읽기"),Is.Not.Null);
            Click("계정 저장 다시 읽기");yield return WaitPlayback();
            Assert.That(File.ReadAllText(AccountPath),Is.EqualTo(badAccount));Assert.That(File.ReadAllText(path),Is.EqualTo(badCompetition));
            Assert.That(backend.ProfileReads,Is.Zero);AssertNoSoloRequests(backend);
        }
        [UnityTest] public IEnumerator ExistingSoloPendingIsRetainedWithoutImplicitSubmission()
        {
            var backend=new ProfileBackend();var disk=new LoopStore(AccountPath);var oldSession=new LoopSession(disk,definition.combat);
            oldSession.RefreshProfile(backend.Current);var pending=oldSession.Prepare("start");disk.WritePending(pending);
            string original=LoopJson.Encode(pending);yield return ReopenAccount(backend);
            Assert.That(LoopJson.Encode(app.Account.Pending),Is.EqualTo(original));Assert.That(Button("새 게임").button.interactable,Is.False);
            Click("계정 관리");Assert.That(Button("같은 요청 다시 확인"),Is.Not.Null);
            Assert.That(backend.ProfileReads,Is.Zero);AssertNoSoloRequests(backend);
        }
        sealed class ProfileBackend : IGameBackend
        {
            public BackendProfile Current=new BackendProfile {revision=1,values=new Profile {currency=100}};
            public RunTicket ActiveRun;
            public int ProfileReads,StartCalls,SubmitCalls,UpgradeCalls,UpgradeApplications;
            public bool FailProfile,LoseUpgradeReply;
            public Action OnApprovedUpgrade;
            public readonly List<BackendRequest> UpgradeRequests=new List<BackendRequest>();
            readonly Dictionary<string,BackendReply> replies=new Dictionary<string,BackendReply>();
            TaskCompletionSource<BackendReply> held;
            public void HoldNextProfile()=>held=new TaskCompletionSource<BackendReply>();
            public void ReleaseProfile() { var pending=held;held=null;pending.SetResult(Reply()); }
            BackendReply Reply()=>new BackendReply {status=BackendStatus.Approved,profile=LoopJson.Copy(Current),run=LoopJson.Copy(ActiveRun)};
            public Task<BackendReply> GetProfileAsync(CancellationToken ct)
            { ++ProfileReads;return held!=null?held.Task:FailProfile?Task.FromException<BackendReply>(new TimeoutException("Isolated profile unavailable")):Task.FromResult(Reply()); }
            static Task<BackendReply> Unexpected()=>Task.FromException<BackendReply>(new InvalidOperationException("No new solo requests are allowed by this fixture."));
            public Task<BackendReply> StartRunAsync(BackendRequest request,CancellationToken ct) { ++StartCalls;return Unexpected(); }
            public Task<BackendReply> SubmitRunAsync(BackendRequest request,CancellationToken ct) { ++SubmitCalls;return Unexpected(); }
            public Task<BackendReply> UpgradeAsync(BackendRequest request,CancellationToken ct)
            {
                ++UpgradeCalls;UpgradeRequests.Add(LoopJson.Copy(request));
                if(replies.TryGetValue(request.requestId,out var recorded)) return Task.FromResult(LoopJson.Copy(recorded));
                Assert.That(request.kind,Is.EqualTo("grow"));Assert.That(request.expectedRevision,Is.EqualTo(Current.revision));Assert.That(ActiveRun,Is.Null);
                Current.values.currency-=20*(Current.values.growth+1);++Current.values.growth;++Current.revision;++UpgradeApplications;
                recorded=Reply();replies.Add(request.requestId,LoopJson.Copy(recorded));OnApprovedUpgrade?.Invoke();
                if(LoseUpgradeReply) { LoseUpgradeReply=false;return Task.FromException<BackendReply>(new TimeoutException("Approved reply lost")); }
                return Task.FromResult(recorded);
            }
            public Task<BackendReply> GetOperationStatusAsync(string requestId,CancellationToken ct)
                =>Task.FromResult(replies.TryGetValue(requestId,out var reply)?LoopJson.Copy(reply):new BackendReply {status=BackendStatus.Rejected,code="OPERATION_NOT_FOUND"});
            public Task<BackendReply> GetRunStatusAsync(string serverRunId,CancellationToken ct)=>Unexpected();
        }
    }
}
#endif
