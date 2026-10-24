using System;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    public sealed class CompetitionScreenData : UIData
    {
        public string title, subtitle, status, detail, history;
        public string growthMode,growthTitle,growthSummary,growthBody;
        public string[] statValues=Array.Empty<string>(),statSources=Array.Empty<string>(),adventurerCards=Array.Empty<string>();
        public CompetitionState state;
        public string[] labels=Array.Empty<string>();
        public Action[] actions=Array.Empty<Action>();
        public bool[] enabled=Array.Empty<bool>();
        public Action onClose;
        public bool v5Combat;
        public string v5Notice;
        public Action<string,string> v5Command;
        public Action v5DiceInfo,v5CombatInfo;
        public V5DiceNetData v5NetData;
    }
    public sealed class CompetitionScreen : BaseUI<CompetitionScreenData>
    {
        public RectTransform content;
        public Text title,subtitle,status,detail,history,center;
        public Text[] places,roster;
        public RectTransform[] cells,pawns;
        public Image[] tileFrames;
        public GameObject board;
        public CommonButtonView[] buttons;
        public CompetitionPlayback playback;
        public GameObject growthPanel,statRows,adventurerCards;
        public Text growthTitle,growthSummary,growthBody,skillDetails;
        public Text[] statValues,statSources,cardTexts;
        public GameObject attributeRows;
        public Text attributeBody;
        public Text[] attributeValues,attributeSources;
        public V5DiceAreaView v5Area;
        public V5DiceNetView v5Net;
        public static Color ParticipantColor(int index)=>new[]{new Color(1,.84f,.43f),new Color(1,.47f,.47f),new Color(.47f,.76f,1),new Color(.72f,.59f,1)}[index];
        void OnRectTransformDimensionsChange()
        {
            if(!content) return;
            Vector2 area=((RectTransform)transform).rect.size;
            content.localScale=Vector3.one*Mathf.Max(.01f,Mathf.Min(area.x/720,area.y/1280));
        }
        public Vector2 PawnPosition(int participant,int position)
        {
            var cell=cells[position];
            return cell.anchoredPosition+new Vector2(cell.sizeDelta.x-43+(participant%2)*23,-20-(participant/2)*31);
        }
        protected override void OnBind(CompetitionScreenData data)
        {
            OnRectTransformDimensionsChange();
            if((data.v5Combat && !v5Area) || (data.v5NetData!=null && !v5Net))throw new InvalidOperationException("V5 화면의 authored 참조를 연결해야 합니다.");
            content.gameObject.SetActive(!data.v5Combat);
            if(v5Area) v5Area.gameObject.SetActive(data.v5Combat);
            if(v5Net) {v5Net.gameObject.SetActive(data.v5NetData!=null);if(data.v5NetData!=null)v5Net.Bind(data.v5NetData);else v5Net.Unbind();}
            title.text=data.title;subtitle.text=data.subtitle;status.text=data.status;detail.text=data.detail;history.text=data.history;
            var state=data.state;
            bool combat=state!=null && state.participants[0].phase==CompetitionPhase.Combat && !state.finished;
            board.SetActive(!combat);
            center.text=state==null?"한 세계 · 네 모험가\n고정 지명 / 개인 사건":"세계 라운드 "+Mathf.Min(state.worldRound,state.definition.maxRounds)+" / "+state.definition.maxRounds+"\n"+state.definition.world.name;
            for(int i=0;i<places.Length;i++)
            {
                places[i].text=state==null?"":state.definition.world.places[i].name;
                tileFrames[i].color=state!=null && state.participants[0].position==i?new Color(.16f,.37f,.36f):new Color(.1f,.18f,.25f);
            }
            for(int i=0;i<roster.Length;i++)
            {
                roster[i].color=ParticipantColor(i);
                if(state==null) { roster[i].text=(i==0?"1 당신":" "+(i+1)+" AI 모험가")+" · 같은 계정 성장 보정";continue; }
                var p=state.participants[i];
                string condition=p.eliminated?"탈락":p.finished?"완료":state.definition.world.places[p.position].name;
                roster[i].text=$"{i+1} {p.name} · {p.rank}위  HP {p.hp}/{p.maxHp}  완주 {p.completedLaps}  명예 {p.honor}  {condition}";
            }
            playback.Snap(state);
            bool growth=!string.IsNullOrEmpty(data.growthMode);
            if(growthPanel) growthPanel.SetActive(growth);
            if(growth)
            {
                if(!growthPanel) throw new InvalidOperationException("모험가 상세 화면의 authored 연결이 없습니다.");
                board.SetActive(false);playback.human.combatStage.SetActive(false);playback.human.tray.SetActive(false);
                growthTitle.text=data.growthTitle;growthSummary.text=data.growthSummary;
                bool cards=data.growthMode=="adventurers",stats=data.growthMode=="stats",attributes=data.growthMode=="attributes";
                if(attributes && (!attributeRows || !attributeBody || attributeValues==null || attributeValues.Length!=6 || attributeSources==null || attributeSources.Length!=6))
                    throw new InvalidOperationException("6스탯 화면의 authored 연결이 없습니다.");
                if(attributeRows) attributeRows.SetActive(attributes);
                if(attributeBody) {attributeBody.gameObject.SetActive(attributes);attributeBody.text=data.growthBody;}
                adventurerCards.SetActive(cards);statRows.SetActive(stats);growthBody.gameObject.SetActive(stats);skillDetails.gameObject.SetActive(!cards && !stats && !attributes);
                growthBody.text=data.growthBody;skillDetails.text=data.growthBody;
                for(int i=0;i<statValues.Length;i++) {statValues[i].text=i<data.statValues.Length?data.statValues[i]:"";statSources[i].text=i<data.statSources.Length?data.statSources[i]:"";}
                if(attributes) for(int i=0;i<6;i++) {attributeValues[i].text=data.statValues[i];attributeSources[i].text=data.statSources[i];}
                for(int i=0;i<cardTexts.Length;i++) cardTexts[i].text=i<data.adventurerCards.Length?data.adventurerCards[i]:"";
            }
            playback.human.cue.gameObject.SetActive(!growth);
            if(data.v5Combat)v5Area.Bind(data.state,data.v5Command,data.v5DiceInfo,data.v5CombatInfo,data.v5Notice);
            for(int i=0;i<buttons.Length;i++)
            {
                bool used=i<data.labels.Length;buttons[i].gameObject.SetActive(used);
                if(!used) { buttons[i].Unbind();continue; }
                int index=i;bool allowed=i>=data.enabled.Length || data.enabled[i];
                buttons[i].Bind(i.ToString(),data.labels[i],null,"",new ButtonAppearance {
                    normal=new Color(.14f,.29f,.36f),selected=new Color(.22f,.48f,.52f),pressed=new Color(.12f,.62f,.56f),disabled=new Color(.13f,.16f,.19f)
                },allowed,false,_=>data.actions[index]());
            }
        }
        protected override void OnUnbind() { Data?.onClose?.Invoke();foreach(var button in buttons) button.Unbind();if(v5Area)v5Area.Unbind();if(v5Net)v5Net.Unbind(); }
    }
}
