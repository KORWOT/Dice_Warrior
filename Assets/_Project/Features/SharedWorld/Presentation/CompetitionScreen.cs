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
        public CompetitionState state;
        public string[] labels=Array.Empty<string>();
        public Action[] actions=Array.Empty<Action>();
        public bool[] enabled=Array.Empty<bool>();
        public Action onClose;
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
                if(state==null) { roster[i].text=(i==0?"1 당신":" "+(i+1)+" AI 모험가")+" · 같은 시작 전력";continue; }
                var p=state.participants[i];
                string condition=p.eliminated?"탈락":p.finished?"완료":state.definition.world.places[p.position].name;
                roster[i].text=$"{i+1} {p.name} · {p.rank}위  HP {p.hp}/{p.maxHp}  완주 {p.completedLaps}  명예 {p.honor}  {condition}";
            }
            playback.Snap(state);
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
        protected override void OnUnbind() { Data?.onClose?.Invoke();foreach(var button in buttons) button.Unbind(); }
    }
}
