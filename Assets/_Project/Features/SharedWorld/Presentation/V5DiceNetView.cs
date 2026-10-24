using System;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;
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
    }
    public sealed class V5DiceNetView : MonoBehaviour
    {
        public RectTransform layout,net;
        public Image blocker;
        public Text title,description;
        public CommonButtonView[] tabs,cells;
        public V5FaceGraphic[] faces;
        public CommonButtonView close,previous,next,leave,abandon,stats;
        public int SelectedSlot { get; private set; }
        public int SelectedFace { get; private set; }
        public static Vector2Int Cell(int index)
        {if(index<0 || index>5)throw new ArgumentOutOfRangeException(nameof(index));return index<4?new Vector2Int(index,1):index==4?new Vector2Int(1,0):new Vector2Int(1,2);}
        V5DiceNetData data;
        int page;
        void OnRectTransformDimensionsChange()
        {if(layout){var size=((RectTransform)transform).rect.size;layout.localScale=Vector3.one*Mathf.Max(.01f,Mathf.Min(size.x/720,size.y/1280));}}
        public void Bind(V5DiceNetData value)
        {
            if(!layout || !net || !blocker || tabs==null || tabs.Length!=3 || cells==null || cells.Length!=6 || faces==null || faces.Length!=6)
                throw new InvalidOperationException("V5 주사위 상세의 authored 연결이 없습니다.");
            if(!ReferenceEquals(data,value)){SelectedSlot=SelectedFace=page=0;}data=value;OnRectTransformDimensionsChange();
            title.text=value.title;blocker.raycastTarget=true;
            close.Bind("close","닫기",null,"",V5DiceAreaView.Appearance,true,false,_=>data.close?.Invoke());
            BindOptional(leave,"leave","모험 종료 · 로비",value.leave);BindOptional(abandon,"abandon","모험 포기",value.abandon);BindOptional(stats,"stats","능력치 상세",value.stats);
            for(int i=0;i<tabs.Length;i++)
            {
                int slot=i;tabs[i].gameObject.SetActive(i<value.dice.Length);
                if(i<value.dice.Length)tabs[i].Bind(i.ToString(),$"{i+1}. "+value.definition.combat.dice.Single(d=>d.id==value.dice[i].definitionId).name,null,"",V5DiceAreaView.Appearance,true,i==SelectedSlot,_=>{SelectedSlot=slot;SelectedFace=page=0;Bind(data);});
                else tabs[i].Unbind();
            }
            net.gameObject.SetActive(value.dice.Length>0);
            if(value.dice.Length==0)
            {
                previous.gameObject.SetActive(false);next.gameObject.SetActive(false);description.text=value.information;
                description.rectTransform.anchoredPosition=new Vector2(30,-140);description.rectTransform.sizeDelta=new Vector2(660,790);return;
            }
            description.rectTransform.anchoredPosition=new Vector2(30,-680);description.rectTransform.sizeDelta=new Vector2(660,330);
            var die=value.dice[SelectedSlot];var layoutFaces=CompetitionV5Content.Faces(value.definition,value.adventurerId,die.definitionId);
            var result=value.results.FirstOrDefault(r=>r.dieInstanceId==die.instanceId);
            for(int i=0;i<6;i++)
            {
                int index=i;var face=layoutFaces[i];bool current=result?.faceIndex==i;
                faces[i].Configure(face,current);
                cells[i].Bind(i.ToString(),$"{i+1} · {face.name}\n{V5DiceAreaView.KindLabel(face.kind)}"+(current?" · 현재":""),null,"",V5DiceAreaView.Appearance,true,i==SelectedFace,_=>{SelectedFace=index;page=0;Bind(data);});
            }
            var selected=layoutFaces[SelectedFace];var skills=value.definition.v5.skills.Where(s=>s.faceIds.Contains(selected.id) && (s.ownerAdventurerId==null || s.ownerAdventurerId==value.adventurerId)).ToArray();
            int pages=Math.Max(1,skills.Length);page=Math.Max(0,Math.Min(page,pages-1));
            string restriction=selected.kind==V5FaceKind.Class?"이 면의 소유 모험가 전용 · 타 직업 기술로 사용 불가":selected.kind==V5FaceKind.Blank?"꽝은 기술 조합에 기여하지 않습니다.":"모든 모험가의 공용 면";
            description.text=$"면 {SelectedFace+1}/6 · {selected.name} · {V5DiceAreaView.KindLabel(selected.kind)}\n{restriction}\n"+
                (selected.kind==V5FaceKind.Blank?"모두 꽝이어도 실행 가능 · 적은 행동합니다.":string.Join("\n\n",skills.Skip(page).Take(1).Select(s=>SkillText(value.definition,s))))+$"\n\n상세 {page+1}/{pages} · 중복 면도 각각 표시합니다.";
            previous.Bind("previous","이전 상세",null,"",V5DiceAreaView.Appearance,page>0,false,_=>{page--;Bind(data);});
            next.Bind("next","다음 상세",null,"",V5DiceAreaView.Appearance,page<pages-1,false,_=>{page++;Bind(data);});
            previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);
        }
        static string SkillText(CompetitionDefinition d,V5SkillDefinition skill)
        {
            string text=skill.name+" · "+(skill.faceIds.Length==1?"단독":"조합")+" · "+CompetitionElements.TypeName(skill.effect.damageType)+"\n"+string.Join(" + ",skill.faceIds.Select(id=>CompetitionV5Content.Face(d,id).name));
            if(skill.damage>0)text+=$"\n피해 {skill.damage} + {CompetitionElements.ScalingText(skill.effect.damageScaling)}";
            if(skill.block>0)text+=$"\n보호 {skill.block} + {CompetitionElements.ScalingText(skill.effect.blockScaling)}";
            return text+"\n태그: "+string.Join(", ",skill.effect.identityTags.Concat(skill.effect.grantedActionTags).Distinct());
        }
        static void BindOptional(CommonButtonView button,string id,string label,Action action)
        {button.gameObject.SetActive(action!=null);if(action!=null)button.Bind(id,label,null,"",V5DiceAreaView.Appearance,true,false,_=>action());else button.Unbind();}
        public void Unbind()
        {foreach(var button in tabs.Concat(cells).Concat(new[]{close,previous,next,leave,abandon,stats}))if(button)button.Unbind();data=null;}
    }
}
