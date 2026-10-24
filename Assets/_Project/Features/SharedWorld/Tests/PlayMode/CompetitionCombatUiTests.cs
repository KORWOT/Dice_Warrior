#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    [Category("V5UiRedesign12")]
    public sealed class CompetitionCombatUiTests
    {
        GameObject root,cameraObject,eventObject;
        UIRoot ui;
        CompetitionScreen screen;
        Camera camera;
        RenderTexture target;
        CompetitionDefinition definition;
        readonly float[] samples={0,.05f,.1f,.15f,.2f,.25f,.3f,.35f,.4f,.45f,.5f,.55f,.6f,.65f,.7f,.75f,.8f,.85f,.9f,.95f,1};
        [UnitySetUp] public IEnumerator Setup()
        {
            definition=CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-5.json").text);
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));ui=root.GetComponentInChildren<UIRoot>(true);
            cameraObject=new GameObject("UI11 isolated combat camera");camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=1;ui.popupBlocker.gameObject.SetActive(false);
            ui.inputGroup.interactable=ui.inputGroup.blocksRaycasts=true;ui.inputGroup.alpha=1;ui.cacheLayer.gameObject.SetActive(false);
            screen=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CompetitionScreen>("Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab"),ui.screenLayer);
            var rect=(RectTransform)screen.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            screen.content.gameObject.SetActive(false);if(screen.v5Shell)screen.v5Shell.gameObject.SetActive(false);
            screen.v5Net.gameObject.SetActive(false);screen.v5Area.gameObject.SetActive(true);
            eventObject=new GameObject("UI11 isolated combat events",typeof(EventSystem),typeof(InputSystemUIInputModule));Resize(720,1280);
            yield return null;Canvas.ForceUpdateCanvases();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(root)Object.Destroy(root);if(cameraObject)Object.Destroy(cameraObject);if(eventObject)Object.Destroy(eventObject);if(target){target.Release();Object.Destroy(target);}yield return null;}

        [UnityTest] public IEnumerator AllDiceCountsAndHoldMasksKeepFixedSlotsAndProjectedMeshesInsideTheTray()
        {
            foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(720,1600),new Vector2Int(540,960)})
            {
                Resize(size.x,size.y);yield return null;
                for(int count=1;count<=3;count++)
                {
                    var before=Combat(count);var rolled=LoopJson.Copy(before);CompetitionCommandRules.Apply(rolled,"combat_roll","");
                    screen.v5Area.Bind(before,null,null,null,"");yield return null;
                    yield return Sample("combat_roll",before,rolled);
                    for(int mask=0;mask<(1<<count);mask++)
                    {
                        var state=LoopJson.Copy(rolled);
                        foreach(var result in state.participants[0].v5.selection.results)
                            if((mask&(1<<result.slotIndex))!=0)
                                CompetitionCommandRules.Apply(state,"combat_hold",result.dieInstanceId);
                        screen.v5Area.Snap(state);Canvas.ForceUpdateCanvases();AssertGeometry();
                        if(mask!=(1<<count)-1)
                        {
                            var rerolled=LoopJson.Copy(state);CompetitionCommandRules.Apply(rerolled,"combat_reroll","");yield return Sample("combat_reroll",state,rerolled);
                        }
                        var changed=LoopJson.Copy(state);var die=changed.participants[0].v5.selection.results[0];string command=die.held?"combat_release":"combat_hold";
                        CompetitionCommandRules.Apply(changed,command,die.dieInstanceId);yield return Sample(command,state,changed);
                    }
                }
            }
        }

        [UnityTest] public IEnumerator EveryPhysicalFaceUsesItsActualLayoutAndSettlesFacingThePlayer()
        {
            foreach(string hero in new[]{"wanderer","scholar","vanguard"})
            {
                var state=Combat(3,hero);CompetitionCommandRules.Apply(state,"combat_roll","");
                for(int slot=0;slot<3;slot++)for(int index=0;index<6;index++)
                {
                    var p=state.participants[0];var definitions=CompetitionV5Content.Faces(state.definition,hero,p.dice[slot].definitionId);
                    var result=p.v5.selection.results[slot];result.faceIndex=index;result.faceId=definitions[index].id;
                    screen.v5Area.Snap(state);yield return null;Canvas.ForceUpdateCanvases();
                    var cube=screen.v5Area.cubes[slot];Assert.That(cube.ResultFaceIndex,Is.EqualTo(index));Assert.That(cube.FrontFaceIndex,Is.EqualTo(index));
                    Assert.That(cube.RevealResult,Is.True);Assert.That(cube.FaceId(index),Is.EqualTo(result.faceId));
                    for(int face=0;face<6;face++)Assert.That(cube.FaceId(face),Is.EqualTo(definitions[face].id));
                    Assert.That(screen.v5Area.dice[slot].label.text,Does.Contain(definitions[index].name));AssertGeometry();
                }
            }
        }

        [UnityTest] public IEnumerator CardsUseStableIdentityAndPreviewGroupsComeFromTheActualPlanner()
        {
            var state=Combat(3);CompetitionCommandRules.Apply(state,"combat_roll","");SetFaces(state,"비전","비전","검격");
            string saved=LoopJson.Encode(state);string clicked=null;
            screen.v5Area.Bind(state,(kind,id)=>clicked=kind+":"+id,()=>{},()=>{},"");yield return null;
            var planned=CompetitionV5Skills.PlanSteps(state.definition,state.participants[0],state.participants[0].v5.selection.results);
            var preview=CompetitionV5Skills.Preview(state.definition,state.participants[0]);
            Assert.That(preview.skills.Length,Is.EqualTo(planned.Length));Assert.That(screen.v5Area.preview.text,Does.Contain("발동 예상 "+planned.Length+"개"));
            for(int i=0;i<planned.Length;i++)
            {
                Assert.That(preview.skills[i].dieSlotIndices,Is.EqualTo(planned[i].dieSlotIndices));
                Assert.That(screen.v5Area.preview.text,Does.Contain(planned[i].skill.name));
                foreach(int slot in planned[i].dieSlotIndices)Assert.That(screen.v5Area.dice[slot].label.text,Does.Contain("기술 "+(i+1)));
            }
            foreach(var card in screen.v5Area.dice)Assert.That(card.label.text,Does.Contain("주사위"));
            yield return Tap(screen.v5Area.dice[1]);Assert.That(clicked,Is.EqualTo("combat_hold:"+state.participants[0].dice[1].instanceId));
            Assert.That(LoopJson.Encode(state),Is.EqualTo(saved));
            SetFaces(state,"검격","검격","검격");screen.v5Area.Snap(state);
            Assert.That(CompetitionV5Skills.PlanSteps(state.definition,state.participants[0],state.participants[0].v5.selection.results),Has.Length.EqualTo(3));
            Assert.That(screen.v5Area.preview.text,Does.Contain("발동 예상 3개").And.Contain("기술 3"));
            SetFaces(state,"꽝","꽝","꽝");screen.v5Area.Snap(state);Assert.That(screen.v5Area.preview.text,Does.Contain("발동 기술 없음"));Assert.That(screen.v5Area.execute.button.interactable,Is.True);
        }

        [UnityTest] public IEnumerator DiceDetailsDistinguishRolledAndInspectedFacesAndRetainTheirLocalState()
        {
            foreach(string hero in new[]{"wanderer","scholar","vanguard"})
            {
                var state=Combat(3,hero);CompetitionCommandRules.Apply(state,"combat_roll","");var p=state.participants[0];
                var data=new V5DiceNetData{definition=state.definition,adventurerId=hero,title="주사위 상세",dice=p.dice.ToArray(),results=p.v5.selection.results,close=()=>{}};
                string saved=LoopJson.Encode(state);screen.v5Net.gameObject.SetActive(true);screen.v5Net.Bind(data);yield return null;
                Assert.That(screen.v5Net.SelectedFace,Is.EqualTo(data.results[0].faceIndex));
                for(int slot=0;slot<3;slot++)
                {
                    yield return Tap(screen.v5Net.tabs[slot]);Assert.That(screen.v5Net.SelectedFace,Is.EqualTo(data.results[slot].faceIndex));
                    var faces=CompetitionV5Content.Faces(state.definition,hero,p.dice[slot].definitionId);
                    for(int face=0;face<6;face++)
                    {
                        yield return Tap(screen.v5Net.cells[face]);
                        Assert.That(screen.v5Net.cells[face].label.text,Does.Contain("조회 중"));Assert.That(screen.v5Net.description.text,Does.Contain(faces[face].name));
                        Assert.That(screen.v5Net.cells[data.results[slot].faceIndex].label.text,Does.Contain("이번 굴림"));
                        Assert.That(screen.v5Net.description.text,Does.Not.Contain("family."));
                    }
                }
                int selectedSlot=screen.v5Net.SelectedSlot,selectedFace=screen.v5Net.SelectedFace;
                screen.v5Net.Unbind();screen.v5Net.Bind(new V5DiceNetData{definition=definition,title="전투 상세",information="임시 다른 조회",close=()=>{}});
                screen.v5Net.Bind(data);Assert.That(screen.v5Net.SelectedSlot,Is.EqualTo(selectedSlot));Assert.That(screen.v5Net.SelectedFace,Is.EqualTo(selectedFace));
                Assert.That(LoopJson.Encode(state),Is.EqualTo(saved));screen.v5Net.gameObject.SetActive(false);
            }
        }

        IEnumerator Sample(string kind,CompetitionState before,CompetitionState after)
        {
            var area=screen.v5Area;area.Snap(before);string original=LoopJson.Encode(after);
            var positions=area.dice.Select(d=>((RectTransform)d.transform).anchoredPosition).ToArray();
            var sequence=area.Play(kind,before,after);
            while(sequence.MoveNext())
            {
                if(!(sequence.Current is IEnumerator animation))continue;
                var frame=animation.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Select(f=>f.GetValue(animation)).OfType<Action<float>>().First();
                foreach(float t in samples)
                {
                    frame(t);Canvas.ForceUpdateCanvases();AssertGeometry();
                    for(int slot=0;slot<before.participants[0].dice.Count;slot++)
                    {
                        Assert.That(((RectTransform)area.dice[slot].transform).anchoredPosition,Is.EqualTo(positions[slot]));
                        var result=before.participants[0].v5.selection.results.FirstOrDefault(r=>r.slotIndex==slot);
                        if(kind=="combat_reroll" && result?.held==true)
                        {Assert.That(area.cubes[slot].RollProgress,Is.EqualTo(1));Assert.That(area.cubes[slot].ResultFaceIndex,Is.EqualTo(result.faceIndex));Assert.That(area.cubes[slot].RevealResult,Is.True);}
                    }
                    if(area.DiceRolling && area.cubes.Any(c=>c.gameObject.activeInHierarchy && !c.RevealResult))
                    {Assert.That(area.preview.text,Does.Contain("멈추면"));Assert.That(area.execute.button.interactable,Is.False);}
                    yield return null;
                }
            }
            area.Snap(after);AssertGeometry();Assert.That(LoopJson.Encode(after),Is.EqualTo(original));
        }
        void AssertGeometry()
        {
            var area=screen.v5Area;var visible=area.dice.Where(d=>d.gameObject.activeInHierarchy).ToArray();
            foreach(var card in visible)
            {
                var rect=(RectTransform)card.transform;var lane=area.rollZone;var bounds=Bounds(rect,lane);
                Assert.That(rect.parent,Is.SameAs(lane));Assert.That(area.holdZone.gameObject.activeSelf,Is.False);
                Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(lane.rect.xMin-1));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(lane.rect.xMax+1));
                Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(lane.rect.yMin-1));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(lane.rect.yMax+1));
                Assert.That(bounds.Overlaps(Bounds(area.rollHint.rectTransform,lane)),Is.False,"Slot must not cover tray guidance");
                Assert.That(bounds.Overlaps(Bounds(area.holdHint.rectTransform,lane)),Is.False,"Slot must not cover hold count");
                int slot=Array.IndexOf(area.dice,card);Assert.That(rect.anchoredPosition,Is.EqualTo(V5DiceAreaView.SlotPosition(slot,visible.Length)));
                var cube=area.cubes[slot];var mesh=cube.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                var projected=mesh.vertices.Select(v=>(Vector2)lane.InverseTransformPoint(cube.transform.TransformPoint(v))).ToArray();
                var projectedBounds=Rect.MinMaxRect(projected.Min(p=>p.x),projected.Min(p=>p.y),projected.Max(p=>p.x),projected.Max(p=>p.y));
                Assert.That(projectedBounds.xMin,Is.GreaterThanOrEqualTo(bounds.xMin));Assert.That(projectedBounds.xMax,Is.LessThanOrEqualTo(bounds.xMax));
                Assert.That(projectedBounds.yMax,Is.LessThanOrEqualTo(bounds.yMax));Assert.That(projectedBounds.yMin,Is.GreaterThanOrEqualTo(bounds.yMin));
                Assert.That(projectedBounds.Overlaps(Bounds(card.label.rectTransform,lane)),Is.False,"Cube, shadow and lock must stay above the result label");
                var safe=Bounds(rect,ui.safeArea);Assert.That(safe.xMin,Is.GreaterThanOrEqualTo(ui.safeArea.rect.xMin-1));Assert.That(safe.xMax,Is.LessThanOrEqualTo(ui.safeArea.rect.xMax+1));
                Assert.That(safe.yMin,Is.GreaterThanOrEqualTo(ui.safeArea.rect.yMin-1));Assert.That(safe.yMax,Is.LessThanOrEqualTo(ui.safeArea.rect.yMax+1));
            }
            for(int i=0;i<visible.Length;i++)for(int j=i+1;j<visible.Length;j++)
                Assert.That(Bounds((RectTransform)visible[i].transform,area.layout).Overlaps(Bounds((RectTransform)visible[j].transform,area.layout)),Is.False,"Dice cards must not overlap");
        }
        static Rect Bounds(RectTransform child,RectTransform reference)
        {var corners=new Vector3[4];child.GetWorldCorners(corners);var p=corners.Select(c=>(Vector2)reference.InverseTransformPoint(c)).ToArray();return Rect.MinMaxRect(p.Min(x=>x.x),p.Min(x=>x.y),p.Max(x=>x.x),p.Max(x=>x.y));}
        CompetitionState Combat(int count,string hero="wanderer")
        {
            var state=CompetitionRules.Start(definition,new Profile{growth=2},33,"ui11-combat",hero);var p=state.participants[0];
            p.completedLaps=count-1;p.claimed=Enumerable.Range(1,count-1).Select(i=>state.matchId+":"+p.id+":lap:"+i).ToList();
            p.dice=definition.combat.dice.Take(count).Select((d,i)=>new DieSlot{instanceId="ui11-die-"+i,definitionId=d.id}).ToList();
            string enemy=definition.v4.enemies.First(e=>e.encounterId!="final").encounterId;p.phase=CompetitionPhase.Encounter;
            p.offer=new EncounterOffer{id="ui11-offer",placeId=definition.world.places[0].id,stage=count-1,visit=1,choices=new[]{new EncounterChoice{id=enemy,label="격리 전투",description="UI 검사",kind=EncounterKind.Combat,available=true,enemyHp=500,enemyAttack=1},new EncounterChoice{id="harbor-work",label="대안",description="UI 검사",kind=EncounterKind.Study,available=true}}};
            CompetitionCommandRules.Apply(state,"choose",enemy);return state;
        }
        static void SetFaces(CompetitionState state,params string[] names)
        {var p=state.participants[0];foreach(var r in p.v5.selection.results){var faces=CompetitionV5Content.Faces(state.definition,p.adventurerId,p.dice[r.slotIndex].definitionId);r.faceIndex=Array.FindIndex(faces,f=>f.name==names[r.slotIndex]);r.faceId=faces[r.faceIndex].id;}}
        void Resize(int width,int height)
        {if(target){target.Release();Object.Destroy(target);}target=new RenderTexture(width,height,24);camera.targetTexture=target;ui.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ui.canvas.scaleFactor=1;ui.safeArea.anchorMin=new Vector2(.03f,.04f);ui.safeArea.anchorMax=new Vector2(.97f,.96f);ui.safeArea.offsetMin=ui.safeArea.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();}
        IEnumerator Tap(CommonButtonView view)
        {
            yield return null;Canvas.ForceUpdateCanvases();var rect=(RectTransform)view.transform;
            var pointer=new PointerEventData(eventObject.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();eventObject.GetComponent<EventSystem>().RaycastAll(pointer,hits);
            Assert.That(view.button.interactable,Is.True);Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(view.button));
            ExecuteEvents.Execute(view.button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Canvas.ForceUpdateCanvases();
        }
    }
}
#endif
