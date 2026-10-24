using System;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    public sealed class V5DiceNetData
    {
        public CompetitionDefinition definition;
        public string adventurerId,title,information;
        public DieSlot[] dice=Array.Empty<DieSlot>();
        public V5FaceResult[] results=Array.Empty<V5FaceResult>();
        public Action close,leave,abandon,stats;
        // Only view-local state. Retaining this data while opening another detail view restores the query.
        public int selectedSlot,selectedFace=-1,detailPage;
        public float scrollPosition=1;
    }
    public sealed class V5DiceNetView : MonoBehaviour
    {
        public RectTransform layout,net;
        public Image blocker;
        public Text title,description,managementHeading;
        public ScrollRect detailsScroll;
        public CommonButtonView[] tabs,cells;
        public V5FaceGraphic[] faces;
        public CommonButtonView close,previous,next,leave,abandon,stats;
        public int SelectedSlot { get; private set; }
        public int SelectedFace { get; private set; }
        public static Vector2Int Cell(int index)
        {if(index<0 || index>5)throw new ArgumentOutOfRangeException(nameof(index));return index<4?new Vector2Int(index,1):index==4?new Vector2Int(1,0):new Vector2Int(1,2);}
        V5DiceNetData data;
        int page;
        UnityAction<Vector2> scrollChanged;
        void OnRectTransformDimensionsChange()
        {if(layout){var size=((RectTransform)transform).rect.size;layout.localScale=Vector3.one*Mathf.Max(.01f,Mathf.Min(size.x/720,size.y/1280));}}
        public void Bind(V5DiceNetData value)
        {
            if(!layout || !net || !blocker || tabs==null || tabs.Length!=3 || cells==null || cells.Length!=6 || faces==null || faces.Length!=6)
                throw new InvalidOperationException("V5 주사위 상세의 authored 연결이 없습니다.");
            data=value;OnRectTransformDimensionsChange();
            SelectedSlot=Math.Max(0,Math.Min(value.selectedSlot,Math.Max(0,value.dice.Length-1)));page=Math.Max(0,value.detailPage);
            var initialResult=value.dice.Length==0?null:value.results.FirstOrDefault(r=>r.dieInstanceId==value.dice[SelectedSlot].instanceId);
            SelectedFace=Math.Max(0,Math.Min(value.selectedFace<0?initialResult?.faceIndex ?? 0:value.selectedFace,5));
            title.text=value.title;blocker.raycastTarget=true;
            close.Bind("close","닫기",null,"",V5DiceAreaView.Appearance,true,false,_=>data.close?.Invoke());
            BindOptional(leave,"leave","로비로 · 진행 유지",value.leave);BindOptional(abandon,"abandon","내 모험 포기",value.abandon);BindOptional(stats,"stats","현재 경기 능력치",value.stats);
            if(managementHeading)managementHeading.gameObject.SetActive(value.leave!=null || value.abandon!=null);
            for(int i=0;i<tabs.Length;i++)
            {
                int slot=i;tabs[i].gameObject.SetActive(i<value.dice.Length);
                if(i<value.dice.Length)tabs[i].Bind(i.ToString(),V5DiceAreaView.DieLabel(value.definition,value.dice[i],i)+(i==SelectedSlot?"\n조회 중":""),null,"",V5DiceAreaView.Appearance,true,i==SelectedSlot,_=>{data.selectedSlot=slot;data.selectedFace=-1;data.detailPage=0;data.scrollPosition=1;Bind(data);});
                else tabs[i].Unbind();
            }
            net.gameObject.SetActive(value.dice.Length>0);
            if(value.dice.Length==0)
            {
                previous.gameObject.SetActive(false);next.gameObject.SetActive(false);description.text=value.information;
                LayoutDescription(true);SaveSelection();return;
            }
            var die=value.dice[SelectedSlot];var layoutFaces=CompetitionV5Content.Faces(value.definition,value.adventurerId,die.definitionId);
            var result=value.results.FirstOrDefault(r=>r.dieInstanceId==die.instanceId);
            for(int i=0;i<6;i++)
            {
                int index=i;var face=layoutFaces[i];bool current=result?.faceIndex==i;
                faces[i].Configure(face,current);
                cells[i].Bind(i.ToString(),$"{i+1}번 면 · {face.name}\n{V5DiceAreaView.KindLabel(face.kind)}"+(current?" · 이번 굴림":"")+(i==SelectedFace?"\n조회 중":""),null,"",V5DiceAreaView.Appearance,true,i==SelectedFace,_=>{data.selectedFace=index;data.detailPage=0;data.scrollPosition=1;Bind(data);});
            }
            var selected=layoutFaces[SelectedFace];var skills=value.definition.v5.skills.Where(s=>s.faceIds.Contains(selected.id) && (s.ownerAdventurerId==null || s.ownerAdventurerId==value.adventurerId)).ToArray();
            int pages=Math.Max(1,skills.Length);page=Math.Max(0,Math.Min(page,pages-1));
            string restriction=selected.kind==V5FaceKind.Class?"이 면의 소유 모험가 전용 · 타 직업 기술로 사용 불가":selected.kind==V5FaceKind.Blank?"꽝은 기술 조합에 기여하지 않습니다.":"모든 모험가의 공용 면";
            description.text=$"조회 중: {SelectedFace+1}번 면 · {selected.name} · {V5DiceAreaView.KindLabel(selected.kind)}\n"+
                (result==null?"이번 굴림: 없음 · 구성 조회":$"이번 굴림: {result.faceIndex+1}번 면 · {layoutFaces[result.faceIndex].name}")+"\n"+restriction+"\n"+
                (selected.kind==V5FaceKind.Blank?"모두 꽝이어도 실행 가능 · 적은 행동합니다.":string.Join("\n\n",skills.Skip(page).Take(1).Select(s=>SkillText(value.definition,s))))+$"\n\n상세 {page+1}/{pages} · 중복 면도 각각 표시합니다.";
            previous.Bind("previous","이전 상세",null,"",V5DiceAreaView.Appearance,page>0,false,_=>{data.detailPage=page-1;data.scrollPosition=1;Bind(data);});
            next.Bind("next","다음 상세",null,"",V5DiceAreaView.Appearance,page<pages-1,false,_=>{data.detailPage=page+1;data.scrollPosition=1;Bind(data);});
            previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);
            LayoutDescription(false);SaveSelection();
        }
        void SaveSelection(){data.selectedSlot=SelectedSlot;data.selectedFace=SelectedFace;data.detailPage=page;}
        void LayoutDescription(bool information)
        {
            if(!detailsScroll)throw new InvalidOperationException("V5 상세 스크롤의 authored 연결이 없습니다.");
            if(scrollChanged!=null)detailsScroll.onValueChanged.RemoveListener(scrollChanged);
            var viewport=detailsScroll.viewport;viewport.anchoredPosition=new Vector2(30,information?-130:-714);viewport.sizeDelta=new Vector2(660,information?740:308);
            var rect=description.rectTransform;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(660,viewport.sizeDelta.y);
            rect.sizeDelta=new Vector2(660,Mathf.Max(viewport.sizeDelta.y,description.preferredHeight));
            detailsScroll.StopMovement();Canvas.ForceUpdateCanvases();detailsScroll.verticalNormalizedPosition=data.scrollPosition;
            scrollChanged=position=>{if(data!=null)data.scrollPosition=position.y;};detailsScroll.onValueChanged.AddListener(scrollChanged);
        }
        static string SkillText(CompetitionDefinition d,V5SkillDefinition skill)
        {
            string text=skill.name+" · "+(skill.faceIds.Length==1?"단독":"조합")+" · "+CompetitionElements.TypeName(skill.effect.damageType)+"\n"+string.Join(" + ",skill.faceIds.Select(id=>CompetitionV5Content.Face(d,id).name));
            if(skill.damage>0)text+=$"\n피해 {skill.damage} + {CompetitionElements.ScalingText(skill.effect.damageScaling)}";
            if(skill.block>0)text+=$"\n보호 {skill.block} + {CompetitionElements.ScalingText(skill.effect.blockScaling)}";
            return text+"\n분류: "+CompetitionV5UiText.Friendly(d,string.Join(", ",skill.effect.identityTags.Concat(skill.effect.grantedActionTags).Distinct()));
        }
        static void BindOptional(CommonButtonView button,string id,string label,Action action)
        {button.gameObject.SetActive(action!=null);if(action!=null)button.Bind(id,label,null,"",V5DiceAreaView.Appearance,true,false,_=>action());else button.Unbind();}
        public void Unbind()
        {
            if(detailsScroll && scrollChanged!=null)detailsScroll.onValueChanged.RemoveListener(scrollChanged);scrollChanged=null;
            foreach(var button in tabs.Concat(cells).Concat(new[]{close,previous,next,leave,abandon,stats}))if(button)button.Unbind();data=null;
        }
    }
}
