#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class CompetitionV5FlowTests
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
        ParticipantState Human=>app.Session.Snapshot.state.participants[0];
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.GetFullPath(Path.Combine("artifacts/v5-dice-combat-09/playmode",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(directory);path=Path.Combine(directory,"checkpoint.json");
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-5.json").text);
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));ui=root.GetComponentInChildren<UIManager>(true);
            ui.prefabs=new BaseUI[]{AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath)};
            cameraObject=new GameObject("V5 isolated capture camera");camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            ui.Root.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.Root.canvas.worldCamera=camera;ui.Root.canvas.planeDistance=1;Resize(1280);
            eventObject=new GameObject("V5 isolated events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            appObject=new GameObject("V5 isolated app");appObject.SetActive(false);app=appObject.AddComponent<CompetitionApp>();app.ui=ui;
            app.Open(path,definition,new Profile {growth=2},()=>33);appObject.SetActive(true);yield return null;Canvas.ForceUpdateCanvases();
            foreach(var face in Screen.v5Area.faces.Concat(Screen.v5Net.faces))Assert.That(face.GetComponent<CanvasRenderer>(),Is.Not.Null,"Instance renderer required: "+HierarchyPath(face.transform));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui)ui.CloseAll();if(appObject)Object.Destroy(appObject);if(root)Object.Destroy(root);if(eventObject)Object.Destroy(eventObject);if(cameraObject)Object.Destroy(cameraObject);
            if(target){target.Release();Object.Destroy(target);}yield return null;
            // Isolated checkpoints and captures are run evidence, retained under ignored artifacts.
        }
        [Test] public void AuthoredV5HasIndependentAreasAndSixFaceNet()
        {
            var screen=AssetDatabase.LoadAssetAtPath<CompetitionScreen>(PrefabPath);
            Assert.That(screen,Is.Not.Null);
            var serialized=new SerializedObject(screen);
            foreach(string field in new[]{"v5Area","v5Net"})
            {
                var binding=serialized.FindProperty(field);
                Assert.That(binding,Is.Not.Null,"V5 requires authored "+field);
                Assert.That(binding.objectReferenceValue,Is.Not.Null,"Run the approved UpgradeV5 authoring step: "+field);
            }
            Assert.That(screen.v5Area.rollZone,Is.Not.SameAs(screen.v5Area.holdZone));Assert.That(screen.v5Area.dice.Length,Is.EqualTo(3));Assert.That(screen.v5Net.cells.Length,Is.EqualTo(6));
            foreach(var face in screen.v5Area.faces.Concat(screen.v5Net.faces))Assert.That(face.GetComponent<CanvasRenderer>(),Is.Not.Null,"Authored renderer required: "+HierarchyPath(face.transform));
            Assert.That(screen.buttons.Length,Is.EqualTo(8));Assert.That(screen.v5Net.blocker.raycastTarget,Is.True);
            Assert.That(screen.v5Net.blocker.color.a,Is.EqualTo(1f),"Information text requires an opaque backdrop.");
            for(int i=0;i<6;i++)
            {
                var cell=V5DiceNetView.Cell(i);var rect=(RectTransform)screen.v5Net.cells[i].transform;
                Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(cell.x*158,-cell.y*158)));Assert.That(rect.sizeDelta.x,Is.EqualTo(rect.sizeDelta.y));
            }
        }
        [UnityTest] public IEnumerator PointerSelectionSupportsOneTwoAndThreeDiceAndKeepsHeldDieStill()
        {
            yield return Click("새 게임");yield return Wait();
            for(int count=1;count<=3;count++)
            {
                yield return Combat(count);var area=Screen.v5Area;Assert.That(area.gameObject.activeInHierarchy,Is.True);Assert.That(Screen.content.gameObject.activeSelf,Is.False);
                Assert.That(area.reroll.button.interactable,Is.False);Assert.That(area.execute.button.interactable,Is.False);
                yield return Tap(area.roll);Assert.That(app.IsPresenting,Is.True);Assert.That(area.DiceRolling,Is.True);yield return Wait();
                Assert.That(Human.v5.selection.results.Length,Is.EqualTo(count));Assert.That(area.dice.Count(d=>d.gameObject.activeInHierarchy),Is.EqualTo(count));
                uint rng=Human.combatRng;yield return Tap(area.dice[0]);yield return Wait();Assert.That(area.dice[0].transform.parent,Is.EqualTo(area.holdZone));Assert.That(Human.combatRng,Is.EqualTo(rng));
                yield return Tap(area.dice[0]);yield return Wait();Assert.That(area.dice[0].transform.parent,Is.EqualTo(area.rollZone));
                for(int i=0;i<count;i++){yield return Tap(area.dice[i]);yield return Wait();}
                Assert.That(area.reroll.button.interactable,Is.False);Assert.That(area.execute.button.interactable,Is.True);
                yield return Tap(area.dice[count-1]);yield return Wait();var held=Human.v5.selection.results.Where(r=>r.held).ToArray();
                var heldPositions=held.Select(r=>area.dice[r.slotIndex].transform.position).ToArray();yield return Tap(area.reroll);yield return null;
                for(int i=0;i<held.Length;i++)
                {Assert.That(area.dice[held[i].slotIndex].transform.position,Is.EqualTo(heldPositions[i]));Assert.That(area.dice[held[i].slotIndex].transform.localRotation,Is.EqualTo(Quaternion.identity));}
                yield return Wait();Assert.That(Human.v5.selection.rerollsLeft,Is.Zero);
                foreach(var result in held)Assert.That(LoopJson.Encode(Human.v5.selection.results[result.slotIndex]),Is.EqualTo(LoopJson.Encode(result)));
                int turn=Human.encounterTurns;yield return Tap(area.execute);Assert.That(area.DiceRolling,Is.False);yield return Wait();Assert.That(Human.encounterTurns,Is.EqualTo(turn+1));
                Assert.That(Human.v5.selection.phase,Is.EqualTo(V5SelectionPhase.AwaitRoll));yield return Capture("dice-"+count);
            }
        }
        [UnityTest] public IEnumerator SixFaceModalIsReadOnlyAndBlocksTheBattleBehindIt()
        {
            yield return Click("출전 주사위 6면");yield return null;Assert.That(Screen.v5Net.gameObject.activeInHierarchy,Is.True);yield return Tap(Screen.v5Net.close);
            yield return Click("새 게임");yield return Wait();yield return Combat(3);yield return Tap(Screen.v5Area.roll);yield return Wait();
            string saved=LoopJson.Encode(app.Session.Snapshot),raw=File.ReadAllText(path);yield return Tap(Screen.v5Area.diceInfo);yield return null;
            var net=Screen.v5Net;Assert.That(net.cells.Count(c=>c.gameObject.activeInHierarchy),Is.EqualTo(6));
            var result=Human.v5.selection.results[0];Assert.That(net.faces.Count(f=>f.current),Is.EqualTo(1));Assert.That(net.faces[result.faceIndex].current,Is.True);
            for(int slot=0;slot<3;slot++)
            {
                yield return Tap(net.tabs[slot]);var actual=CompetitionV5Content.Faces(definition,Human.adventurerId,Human.dice[slot].definitionId);
                Assert.That(net.faces.Count(f=>f.current),Is.EqualTo(1));Assert.That(net.faces[Human.v5.selection.results[slot].faceIndex].current,Is.True);
                for(int i=0;i<6;i++){yield return Tap(net.cells[i]);Assert.That(net.cells[i].label.text,Does.Contain(actual[i].name));Assert.That(net.description.text,Does.Contain(actual[i].name));}
                if(slot==1){Assert.That(actual[1].id,Is.EqualTo(actual[2].id));Assert.That(net.cells[1],Is.Not.SameAs(net.cells[2]));}
            }
            var hits=Hits(Screen.v5Area.execute);Assert.That(hits.First().gameObject.transform.IsChildOf(net.transform),Is.True,"Modal must intercept the first raycast");
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllText(path),Is.EqualTo(raw));yield return Capture("six-face-net");yield return Tap(net.close);
            yield return Tap(Screen.v5Area.combatInfo);Assert.That(net.description.text,Does.Contain("발동 예상"));yield return Tap(net.close);Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
        }
        [UnityTest] public IEnumerator BlankExecuteAndCancellationRestoreCommittedStateWithoutAnotherCommand()
        {
            yield return Click("새 게임");yield return Wait();yield return Combat(3);yield return Tap(Screen.v5Area.roll);
            string saved=LoopJson.Encode(app.Session.Snapshot);appObject.SetActive(false);yield return null;appObject.SetActive(true);yield return null;
            Assert.That(app.IsPresenting,Is.False);Assert.That(Screen.v5Area.DiceRolling,Is.False);Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));
            yield return Tap(Screen.v5Area.dice[0]);saved=LoopJson.Encode(app.Session.Snapshot);Screen.v5Area.dice[0].button.onClick.Invoke();
            appObject.SendMessage("OnApplicationPause",true);yield return null;appObject.SendMessage("OnApplicationPause",false);yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(Screen.v5Area.dice[0].transform.parent,Is.EqualTo(Screen.v5Area.holdZone));
            yield return Tap(Screen.v5Area.reroll);saved=LoopJson.Encode(app.Session.Snapshot);appObject.SetActive(false);yield return null;appObject.SetActive(true);yield return null;
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(Screen.v5Area.DiceRolling,Is.False);
            SetFaces(V5FaceKind.Blank);Assert.That(Screen.v5Area.preview.text,Does.Contain("NO_OP").And.Contain("적이 행동"));
            int turn=Human.encounterTurns;yield return Tap(Screen.v5Area.execute);saved=LoopJson.Encode(app.Session.Snapshot);
            appObject.SendMessage("OnApplicationPause",true);yield return null;appObject.SendMessage("OnApplicationPause",false);yield return null;
            Assert.That(app.IsPresenting,Is.False);Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(Human.v5.lastTurn.noOp,Is.True);
            Assert.That(Human.encounterTurns,Is.EqualTo(turn+1));Assert.That(Human.lastTurn.retaliation,Is.True);Assert.That(Screen.v5Area.roll.button.interactable,Is.True);yield return Capture("blank-after-cancel");
        }
        [UnityTest] public IEnumerator HoldBudgetAndWriteFailureHaveVisibleFeedbackAndKeepExecutionAvailable()
        {
            yield return Click("새 게임");yield return Wait();yield return Combat(1);yield return Tap(Screen.v5Area.roll);yield return Wait();
            Edit(snapshot=>snapshot.state.participants[0].v5.selection.holdChanges=12);
            Assert.That(Screen.v5Area.notice.text,Does.Contain("12"));Assert.That(Screen.v5Area.dice[0].button.interactable,Is.False);
            Assert.That(Screen.v5Area.execute.button.interactable,Is.True);Assert.That(Screen.v5Area.reroll.button.interactable,Is.True);
            string saved=LoopJson.Encode(app.Session.Snapshot);
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))yield return Tap(Screen.v5Area.reroll);
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(app.IsPresenting,Is.False);
            Assert.That(Screen.v5Area.notice.text,Is.Not.Empty);Assert.That(Screen.v5Area.execute.button.interactable,Is.True);yield return Capture("save-failure");
        }
        [UnityTest] public IEnumerator BottomControlsAndAllTextFitBothSafeAreaRatios()
        {
            yield return Click("새 게임");yield return Wait();yield return Combat(3);yield return Tap(Screen.v5Area.roll);yield return Wait();SetFaces(V5FaceKind.Class);
            foreach(int height in new[]{1280,1600})
            {
                Resize(height);yield return null;ui.Root.safeArea.anchorMin=new Vector2(.03f,.04f);ui.Root.safeArea.anchorMax=new Vector2(.97f,.96f);Canvas.ForceUpdateCanvases();app.Render();yield return null;
                var area=Screen.v5Area;AssertWithinSafeArea(area.reroll);AssertWithinSafeArea(area.execute);
                Assert.That(((RectTransform)area.reroll.transform).rect.height*area.footer.localScale.y,Is.GreaterThanOrEqualTo(44));
                var safe=Bounds(ui.Root.safeArea);var control=Bounds((RectTransform)area.execute.transform);Assert.That(control.yMin-safe.yMin,Is.InRange(0,70));
                AssertTextFits(area.transform);yield return Capture("combat-safe-720x"+height);yield return Tap(area.diceInfo);AssertTextFits(Screen.v5Net.transform);yield return Capture("net-safe-720x"+height);yield return Tap(Screen.v5Net.close);
            }
        }
        [UnityTest] public IEnumerator RewardCandidateEntryAndLegacyResumeUseTheirOwnDefinitions()
        {
            yield return Click("새 게임");yield return Wait();Edit(snapshot=>{var p=snapshot.state.participants[0];p.phase=CompetitionPhase.LapReward;p.position=0;p.lastTravel=1;});
            yield return Click("주사위 상세 보기");Assert.That(Screen.v5Net.tabs.Count(t=>t.gameObject.activeInHierarchy),Is.EqualTo(3));yield return Tap(Screen.v5Net.close);
            Assert.That(Human.phase,Is.EqualTo(CompetitionPhase.LapReward));
            app.Close();var legacy=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-4.json").text);
            string legacyPath=Path.Combine(directory,"legacy.json");app.Open(legacyPath,legacy,new Profile {growth=2},()=>33);yield return Click("새 게임");yield return Wait();
            string legacySaved=LoopJson.Encode(app.Session.Snapshot);app.Close();app.Open(legacyPath,definition,new Profile {growth=2},()=>33);yield return Click("경기 이어하기");yield return null;
            Assert.That(app.Session.Snapshot.state.definition.rulesVersion,Is.EqualTo(legacy.rulesVersion));Assert.That(Screen.v5Area.gameObject.activeSelf,Is.False);
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(legacySaved));yield return Click("능력치 상세");Assert.That(Screen.attributeRows.activeInHierarchy,Is.True);
        }
        [UnityTest] public IEnumerator V5DetailsExposeClassCalculationEquipmentEffectsAndEnemyTagsWithoutMutatingState()
        {
            yield return Click("출전 능력치");string lobbySaved=LoopJson.Encode(app.Session.Snapshot);
            string detailText=null;yield return ReadDetailPages("주사위·스킬",value=>detailText=value);Assert.That(detailText,Does.Contain("단독 예상").And.Contain("최종 반올림"));
            yield return ReadDetailPages("효과 출처",value=>detailText=value);Assert.That(detailText,Does.Contain("장착 장비").And.Contain("고유 능력"));
            yield return ReadDetailPages("태그 조회",value=>detailText=value);Assert.That(detailText,Does.Contain("출처:"));Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(lobbySaved));yield return Click("돌아가기");
            yield return Click("새 게임");yield return Wait();yield return Combat(3);yield return Tap(Screen.v5Area.roll);yield return Wait();SetFaces(V5FaceKind.Class);yield return Tap(Screen.v5Area.execute);yield return Wait();
            var p=Human;string saved=LoopJson.Encode(app.Session.Snapshot),raw=File.ReadAllText(path);yield return Tap(Screen.v5Area.combatInfo);yield return Tap(Screen.v5Net.stats);
            string skills=null;yield return ReadDetailPages("주사위·스킬",value=>skills=value);foreach(var skill in definition.v5.skills.Where(s=>s.ownerAdventurerId==p.adventurerId))Assert.That(skills,Does.Contain(skill.name));
            Assert.That(skills,Does.Contain("현재 능력치").And.Contain("보호막 전"));
            string elements=null;yield return ReadDetailPages("속성·상성",value=>elements=value);var enemy=CompetitionElements.Enemy(definition,p);
            Assert.That(elements,Does.Contain($"HP {p.enemy.hp}/{p.enemy.maxHp}").And.Contain($"보호 {p.enemy.shield}").And.Contain($"기준 공격 {p.enemy.attack}").And.Contain("약점").And.Contain("저항"));
            foreach(string tag in enemy.identityTags)Assert.That(elements,Does.Contain(tag));
            yield return ReadDetailPages("태그 조회",value=>detailText=value);Assert.That(detailText,Does.Contain("대상").And.Contain("출처:"));
            yield return ReadDetailPages("효과 출처",value=>detailText=value);Assert.That(detailText,Does.Contain("규칙:"));
            Assert.That(LoopJson.Encode(app.Session.Snapshot),Is.EqualTo(saved));Assert.That(File.ReadAllText(path),Is.EqualTo(raw));yield return Capture("v5-restored-details");
        }
        [UnityTest] public IEnumerator ExecuteHealthBarsNumbersAndProtectionFollowEachSavedStep()
        {
            yield return Click("새 게임");yield return Wait();yield return Combat(3);yield return Tap(Screen.v5Area.roll);yield return Wait();
            Edit(snapshot=>
            {
                var p=snapshot.state.participants[0];p.enemy.shield=7;p.enemy.attack=50;p.enemy.intent=Intent.Attack;
                string[] icons={"shield","staff","sword"};
                foreach(var result in p.v5.selection.results){var faces=CompetitionV5Content.Faces(snapshot.state.definition,p.adventurerId,p.dice[result.slotIndex].definitionId);result.faceIndex=Array.FindIndex(faces,f=>f.iconId==icons[result.slotIndex]);result.faceId=faces[result.faceIndex].id;}
            });
            var before=Human;var area=Screen.v5Area;yield return Tap(area.execute);var log=Human.lastTurn;Assert.That(log.skills.Count,Is.GreaterThanOrEqualTo(2));
            AssertHealth(before.hp,before.maxHp,0,before.enemy.hp,before.enemy.maxHp,before.enemy.shield);
            var observed=new HashSet<int>();bool enemyAction=false;float deadline=Time.realtimeSinceStartup+10;
            while(app.IsPresenting)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                if(area.notice.text.StartsWith("적 행동 종료",StringComparison.Ordinal))
                {enemyAction=true;AssertHealth(log.playerHpAfter,before.maxHp,0,log.enemyHpAfter,before.enemy.maxHp,0);}
                else for(int i=0;i<log.skills.Count;i++)if(area.preview.text.StartsWith(log.skills[i].name+"\n",StringComparison.Ordinal))
                {observed.Add(i);AssertHealth(log.playerHpBefore,before.maxHp,log.skills.Take(i+1).Sum(s=>s.block),log.skills[i].enemyHpAfter,before.enemy.maxHp,log.skills[i].enemyShieldAfter);}
                yield return null;
            }
            Assert.That(observed.Count,Is.EqualTo(log.skills.Count));Assert.That(enemyAction,Is.True);Assert.That(area.player.text,Does.Contain("현재 보호 0").And.Contain("턴 초기화"));
            Assert.That(area.playerFill.fillAmount,Is.EqualTo((float)Human.hp/Human.maxHp).Within(.0001f));yield return Capture("saved-health-step-order");
        }
        IEnumerator ReadDetailPages(string tab,Action<string> complete)
        {
            yield return Click(tab);var pages=new List<string>();int count=0;
            while(true)
            {
                yield return null;Canvas.ForceUpdateCanvases();Assert.That(++count,Is.LessThanOrEqualTo(100));pages.Add(Screen.skillDetails.text);AssertTextFits(Screen.growthPanel.transform);
                var next=Screen.buttons.FirstOrDefault(b=>b.gameObject.activeInHierarchy && b.label.text=="다음 페이지");if(next==null || !next.button.interactable)break;yield return Tap(next);
            }
            complete(string.Join("\n",pages));
        }
        void AssertHealth(int hp,int maxHp,int block,int enemyHp,int enemyMax,int enemyShield)
        {
            var area=Screen.v5Area;Assert.That(area.player.text,Does.Contain($"내 HP {hp}/{maxHp}").And.Contain($"현재 보호 {block}"));
            Assert.That(area.enemy.text,Does.Contain($"HP {enemyHp}/{enemyMax}").And.Contain($"보호 {enemyShield}"));
            Assert.That(area.playerFill.fillAmount,Is.EqualTo((float)hp/maxHp).Within(.0001f));Assert.That(area.enemyFill.fillAmount,Is.EqualTo((float)enemyHp/enemyMax).Within(.0001f));
        }
        IEnumerator Combat(int count)
        {
            Edit(snapshot=>
            {
                var state=snapshot.state;var p=state.participants[0];p.completedLaps=count-1;p.claimed=Enumerable.Range(1,count-1).Select(i=>state.matchId+":"+p.id+":lap:"+i).ToList();
                p.dice=state.definition.combat.dice.Take(count).Select((d,i)=>new DieSlot {instanceId="v5-ui-"+i,definitionId=d.id}).ToList();p.lastTurn=null;p.v4.lastTurn=null;p.v5.lastTurn=null;p.v5.selection=null;p.encounterTurns=0;p.turn=0;p.enemy=null;p.v4.enemyId=null;
                string enemy=state.definition.v4.enemies.First(e=>e.encounterId!="final").encounterId;
                p.phase=CompetitionPhase.Encounter;p.offer=new EncounterOffer {id="v5-ui-offer-"+count,placeId=state.definition.world.places[p.position].id,stage=count-1,visit=1,
                    choices=new[]{new EncounterChoice {id=enemy,label="V5 검증 전투",description="격리 검증",kind=EncounterKind.Combat,available=true,enemyHp=500,enemyAttack=1},
                    new EncounterChoice {id="harbor-work",label="항구의 짐 운반",description="격리 대안",kind=EncounterKind.Study,available=true}}};
            });yield return Click("V5 검증 전투");yield return Wait();
        }
        void SetFaces(V5FaceKind kind)
        {
            Edit(snapshot=>{var p=snapshot.state.participants[0];foreach(var result in p.v5.selection.results){var faces=CompetitionV5Content.Faces(snapshot.state.definition,p.adventurerId,p.dice[result.slotIndex].definitionId);result.faceIndex=Array.FindIndex(faces,f=>f.kind==kind);result.faceId=faces[result.faceIndex].id;}});
        }
        void Edit(Action<CompetitionDocument> change)
        {
            var original=app.Session.Snapshot;var candidate=LoopJson.Copy(original);change(candidate);candidate.revision++;
            new CompetitionStore(path).Commit(candidate,original.revision,original.generation);app.Session.Reload();app.Render();Canvas.ForceUpdateCanvases();
        }
        IEnumerator Click(string prefix)
        {
            yield return null;Canvas.ForceUpdateCanvases();
            TapNow(Screen.buttons.First(b=>b.gameObject.activeInHierarchy && b.label.text.StartsWith(prefix,StringComparison.Ordinal)));
        }
        List<RaycastResult> Hits(CommonButtonView button)
        {
            var rect=(RectTransform)button.transform;var pointer=new PointerEventData(eventObject.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();eventObject.GetComponent<EventSystem>().RaycastAll(pointer,hits);return hits;
        }
        IEnumerator Tap(CommonButtonView button)
        {
            // A human can only tap after newly activated graphics have reached a rendered frame.
            yield return null;Canvas.ForceUpdateCanvases();TapNow(button);
        }
        void TapNow(CommonButtonView button)
        {
            Assert.That(button.button.interactable,Is.True,button.name);var hits=Hits(button);Assert.That(hits,Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button.button),RaycastDiagnostic(button,hits));
            var rect=(RectTransform)button.transform;var pointer=new PointerEventData(eventObject.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Canvas.ForceUpdateCanvases();
        }
        string RaycastDiagnostic(CommonButtonView button,List<RaycastResult> hits)
        {
            string GraphicState(Graphic graphic)
            {
                var renderer=graphic.GetComponent<CanvasRenderer>();
                return HierarchyPath(graphic.transform)+$" active={graphic.isActiveAndEnabled} raycast={graphic.raycastTarget} renderer={(renderer?"yes":"missing")} depth={(renderer?renderer.absoluteDepth:-1)} cull={(renderer?renderer.cull:false)}";
            }
            return "Top raycast must reach "+HierarchyPath(button.transform)+$"; presenting={app.IsPresenting}; input={ui.Root.inputGroup.interactable}/{ui.Root.inputGroup.blocksRaycasts}; popupBlocker={ui.Root.popupBlocker.gameObject.activeInHierarchy}; net={Screen.v5Net.gameObject.activeInHierarchy}\n"+
                "Hits: "+string.Join(" | ",hits.Select(h=>HierarchyPath(h.gameObject.transform)+$" depth={h.depth} order={h.sortingOrder}"))+"\n"+
                "Graphics: "+string.Join(" | ",button.GetComponentsInChildren<Graphic>(true).Select(GraphicState))+"\n"+
                "Groups: "+string.Join(" | ",button.GetComponentsInParent<CanvasGroup>(true).Select(g=>HierarchyPath(g.transform)+$" active={g.gameObject.activeInHierarchy} alpha={g.alpha} interactable={g.interactable} blocks={g.blocksRaycasts}"));
        }
        static string HierarchyPath(Transform item)
        {var names=new List<string>();for(var current=item;current;current=current.parent)names.Add(current.name);names.Reverse();return string.Join("/",names);}
        IEnumerator Wait()
        {float until=Time.realtimeSinceStartup+20;while(app.IsPresenting){Assert.That(Time.realtimeSinceStartup,Is.LessThan(until));yield return null;}Canvas.ForceUpdateCanvases();}
        void Resize(int height)
        {if(target){target.Release();Object.Destroy(target);}target=new RenderTexture(720,height,24);camera.targetTexture=target;ui.Root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.Root.canvas.scaleFactor=1;Canvas.ForceUpdateCanvases();}
        Rect Bounds(RectTransform rect)
        {var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
        void AssertWithinSafeArea(CommonButtonView button)
        {var safe=Bounds(ui.Root.safeArea);var bounds=Bounds((RectTransform)button.transform);Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(safe.xMin-1));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(safe.xMax+1));Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(safe.yMin-1));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(safe.yMax+1));}
        static void AssertTextFits(Transform parent)
        {foreach(var text in parent.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)))Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name+": "+text.text);}
        IEnumerator Capture(string name)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.Destroy(texture);}
            if(Screen.v5Area.gameObject.activeInHierarchy)foreach(var face in Screen.v5Area.faces.Where(f=>f.gameObject.activeInHierarchy))AssertGraphicReady(face);
            if(Screen.v5Net.gameObject.activeInHierarchy)
            {
                AssertGraphicReady(Screen.v5Net.blocker);
                foreach(var face in Screen.v5Net.faces.Where(f=>f.gameObject.activeInHierarchy))AssertGraphicReady(face);
            }
        }
        static void AssertGraphicReady(Graphic graphic)
        {
            // Avoid Graphic.canvasRenderer: its getter can add a missing component and hide an authoring defect.
            var renderer=graphic.GetComponent<CanvasRenderer>();string context=HierarchyPath(graphic.transform);
            Assert.That(renderer,Is.Not.Null,context+" has no authored CanvasRenderer");
            Assert.That(graphic.isActiveAndEnabled,Is.True,context);Assert.That(renderer.absoluteDepth,Is.GreaterThanOrEqualTo(0),context+" has not reached the rendered canvas");
            Assert.That(renderer.cull,Is.False,context+" is culled");Assert.That(renderer.materialCount,Is.GreaterThan(0),context+" has no material");
            var mesh=renderer.GetMesh();
            Assert.That(mesh,Is.Not.Null,context+" has no rendered mesh");
            Assert.That(mesh.vertexCount,Is.GreaterThan(0),context+" has no rendered mesh");
        }
    }
}
#endif
