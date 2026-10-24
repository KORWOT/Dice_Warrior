using System;
using System.Collections.Generic;
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
        public bool v5Combat,reducedMotion;
        public string v5Notice;
        public Action<string,string> v5Command;
        public Action v5DiceInfo,v5CombatInfo;
        public V5DiceNetData v5NetData;
        public V5ShellData shell;
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
        public V5ShellView v5Shell;
        public Image[] v5RosterCards;
        readonly List<LegacyRect> legacyRects=new List<LegacyRect>();
        readonly List<LegacyText> legacyTexts=new List<LegacyText>();
        readonly List<LegacyImage> legacyImages=new List<LegacyImage>();
        bool geometryCaptured,v5BoardLayout;
        LegacyRect numericHome;
        Text[] indices,pawnIds;
        RectTransform rosterRoot,worldCenter;
        static readonly Color Ink=new Color32(0x10,0x1B,0x2D,0xFF),Slate=new Color32(0x1C,0x2B,0x42,0xFF),Ivory=new Color32(0xF4,0xEE,0xDF,0xFF),Muted=new Color32(0xB5,0xC1,0xD0,0xFF),Amber=new Color32(0xF3,0xBD,0x5C,0xFF),Teal=new Color32(0x52,0xD6,0xCD,0xFF);
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
            if(v5BoardLayout)return cell.anchoredPosition+new Vector2(cell.rect.width*(.16f+participant*.226f),-cell.rect.height*.82f);
            return cell.anchoredPosition+new Vector2(cell.sizeDelta.x-43+(participant%2)*23,-20-(participant/2)*31);
        }
        protected override void OnBind(CompetitionScreenData data)
        {
            OnRectTransformDimensionsChange();
            if((data.v5Combat && !v5Area) || (data.v5NetData!=null && !v5Net))throw new InvalidOperationException("V5 화면의 authored 참조를 연결해야 합니다.");
            bool shell=data.shell!=null;
            if(shell && !v5Shell)throw new InvalidOperationException("V5 주요 화면의 authored 참조를 연결해야 합니다.");
            CaptureLegacyGeometry();RestoreLegacyGeometry();v5BoardLayout=shell&&data.shell.showBoard;
            if(v5BoardLayout)ApplyV5BoardGeometry();
            if(shell || data.v5Combat || data.v5NetData!=null)GetComponent<Image>().color=Ink;
            playback.ReducedMotion=data.reducedMotion;if(v5Area)v5Area.ReducedMotion=data.reducedMotion;
            content.gameObject.SetActive(!data.v5Combat);
            if(v5Shell){v5Shell.gameObject.SetActive(shell);if(!shell)v5Shell.Unbind();if(v5Shell.legacyFooter)v5Shell.legacyFooter.SetActive(!shell);}
            if(v5Area) v5Area.gameObject.SetActive(data.v5Combat);
            if(v5Net) {v5Net.gameObject.SetActive(data.v5NetData!=null);if(data.v5NetData!=null)v5Net.Bind(data.v5NetData);else v5Net.Unbind();}
            title.text=data.title;subtitle.text=data.subtitle;status.text=data.status;detail.text=data.detail;history.text=data.history;
            foreach(var text in new[]{title,subtitle,status,detail,history})text.gameObject.SetActive(!shell);
            var state=data.state;
            bool combat=state!=null && state.participants[0].phase==CompetitionPhase.Combat && !state.finished;
            board.SetActive(shell?data.shell.showBoard:!combat);
            if(roster.Length>0)roster[0].transform.parent.gameObject.SetActive(!shell || data.shell.showBoard);
            center.text=state==null?"한 세계 · 네 모험가\n고정 지명 / 개인 사건":"세계 라운드 "+Mathf.Min(state.worldRound,state.definition.maxRounds)+" / "+state.definition.maxRounds+"\n"+state.definition.world.name;
            for(int i=0;i<places.Length;i++)
            {
                places[i].text=state==null?"":state.definition.world.places[i].name;
                tileFrames[i].color=v5BoardLayout?(state!=null&&state.participants[0].position==i?new Color(.15f,.39f,.42f):Slate):
                    state!=null && state.participants[0].position==i?new Color(.16f,.37f,.36f):new Color(.1f,.18f,.25f);
            }
            for(int i=0;i<roster.Length;i++)
            {
                roster[i].color=ParticipantColor(i);
                roster[i].fontSize=v5BoardLayout?18:shell?14:16;
                if(state==null) { roster[i].text=(i==0?"1 당신":" "+(i+1)+" AI 모험가")+" · 같은 계정 성장 보정";continue; }
                var p=state.participants[i];
                string condition=p.eliminated?"탈락":p.finished?"완료":state.definition.world.places[p.position].name;
                roster[i].text=v5BoardLayout?$"{i+1} {(i==0?"나":"AI")} · {p.rank}위"+(p.eliminated?" · 탈락":p.finished?" · 완료":"")+$"\nHP {p.hp}/{p.maxHp}\n완주{p.completedLaps}/{state.definition.lapsToFinish} 명예{p.honor}":shell?$"{i+1}번 {(i==0?"나":"AI")} {p.name} · {p.rank}위  HP {p.hp}/{p.maxHp}  {p.completedLaps}/{state.definition.lapsToFinish}바퀴 · 명예 {p.honor}"+(p.eliminated?" · 탈락":p.finished?" · 완료":""):
                    $"{i+1} {p.name} · {p.rank}위  HP {p.hp}/{p.maxHp}  완주 {p.completedLaps}  명예 {p.honor}  {condition}";
            }
            playback.Snap(state);
            // SymbolLoopPlayback remembers its first numeric home; each mode restores its own authored geometry afterwards.
            if(v5BoardLayout)SetRect(playback.human.numericDie.rectTransform,260,62,140,140);else numericHome.Restore();
            bool growth=!shell && !string.IsNullOrEmpty(data.growthMode);
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
                bool used=!shell && i<data.labels.Length;buttons[i].gameObject.SetActive(used);
                if(!used) { buttons[i].Unbind();continue; }
                int index=i;bool allowed=i>=data.enabled.Length || data.enabled[i];
                buttons[i].Bind(i.ToString(),data.labels[i],null,"",new ButtonAppearance {
                    normal=new Color(.14f,.29f,.36f),selected=new Color(.22f,.48f,.52f),pressed=new Color(.12f,.62f,.56f),disabled=new Color(.13f,.16f,.19f)
                },allowed,false,_=>data.actions[index]());
            }
            if(shell)
            {
                v5Shell.Bind(data.shell);
                bool showBoard=data.shell.showBoard;board.SetActive(showBoard);playback.human.cue.gameObject.SetActive(false);
                if(!showBoard){playback.human.combatStage.SetActive(false);playback.human.tray.SetActive(false);}
                else if(state!=null)
                {
                    var p=state.participants[0];
                    playback.human.rollLabel.text="이동 주사위 · D6";
                    center.text=state.definition.world.name+$"\n내 완주 {p.completedLaps}/{state.definition.lapsToFinish}\n"+(p.finished || p.eliminated?"남은 AI 경기 진행":$"다음: {Math.Min(p.completedLaps+1,state.definition.lapsToFinish)}바퀴 완주")+"\n00 출발 · 시계 방향";
                }
            }
        }
        void CaptureLegacyGeometry()
        {
            if(geometryCaptured)return;
            var recorded=new HashSet<RectTransform>();
            void Capture(Transform root)
            {
                foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(recorded.Add(rect))legacyRects.Add(new LegacyRect(rect));
                foreach(var text in root.GetComponentsInChildren<Text>(true))legacyTexts.Add(new LegacyText(text));
                foreach(var image in root.GetComponentsInChildren<Image>(true))legacyImages.Add(new LegacyImage(image));
            }
            rosterRoot=(RectTransform)roster[0].transform.parent;worldCenter=(RectTransform)board.transform.Find("World center");
            Capture(board.transform);Capture(rosterRoot);Capture(playback.human.tray.transform);Capture(playback.human.cue.transform);
            legacyImages.Add(new LegacyImage(GetComponent<Image>()));numericHome=new LegacyRect(playback.human.numericDie.rectTransform);
            indices=new Text[cells.Length];for(int i=0;i<cells.Length;i++)indices[i]=cells[i].Find("Index").GetComponent<Text>();
            pawnIds=new Text[pawns.Length];for(int i=0;i<pawns.Length;i++)pawnIds[i]=pawns[i].Find("Number").GetComponent<Text>();
            geometryCaptured=true;
        }
        void RestoreLegacyGeometry()
        {
            foreach(var value in legacyRects)value.Restore();foreach(var value in legacyTexts)value.Restore();foreach(var value in legacyImages)value.Restore();
            if(v5RosterCards!=null)foreach(var image in v5RosterCards)if(image)image.gameObject.SetActive(false);
        }
        void ApplyV5BoardGeometry()
        {
            if(v5RosterCards==null || v5RosterCards.Length!=4)throw new InvalidOperationException("V5 참가자 카드 4개를 authoring으로 연결해 주세요.");
            SetRect(rosterRoot,30,126,660,92);rosterRoot.GetComponent<Image>().color=Ink;
            for(int i=0;i<roster.Length;i++)
            {
                v5RosterCards[i].gameObject.SetActive(true);SetRect(v5RosterCards[i].rectTransform,i*167.5f,0,157.5f,92);
                v5RosterCards[i].color=i==0?new Color(.12f,.29f,.33f):Slate;
                SetRect(roster[i].rectTransform,i*167.5f+8,8,141.5f,76);roster[i].alignment=TextAnchor.UpperLeft;roster[i].fontSize=18;
            }
            SetRect((RectTransform)board.transform,50,230,620,620);board.GetComponent<Image>().color=Ink;
            SetRect(worldCenter,164,164,292,292);worldCenter.GetComponent<Image>().color=Slate;
            SetRect(center.rectTransform,178,178,264,264);center.fontSize=23;center.color=Ivory;
            int[] columns={0,0,0,0,1,2,3,3,3,3,2,1},rows={3,2,1,0,0,0,0,1,2,3,3,3};
            for(int i=0;i<cells.Length;i++)
            {
                SetRect(cells[i],12+columns[i]*152,12+rows[i]*152,140,140);
                SetRect(indices[i].rectTransform,10,7,120,26);indices[i].fontSize=20;indices[i].color=Muted;
                SetRect(places[i].rectTransform,10,38,120,58);places[i].fontSize=22;places[i].color=Ivory;
            }
            for(int i=0;i<pawns.Length;i++)
            {
                pawns[i].sizeDelta=new Vector2(28,34);SetRect(pawnIds[i].rectTransform,0,0,28,22);pawnIds[i].fontSize=18;pawnIds[i].text=i==0?"나":(i+1).ToString();
            }
            SetRect((RectTransform)playback.human.tray.transform,30,866,660,224);playback.human.tray.GetComponent<Image>().color=Slate;
            SetRect(playback.human.rollLabel.rectTransform,20,8,620,30);playback.human.rollLabel.fontSize=20;playback.human.rollLabel.color=Ivory;
            SetRect(playback.human.numericDie.rectTransform,260,62,140,140);
            SetRect(playback.human.cue.rectTransform,30,1100,660,48);playback.human.cue.color=Amber;
        }
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        sealed class LegacyRect
        {
            readonly RectTransform rect;readonly Vector2 min,max,pivot,position,size;readonly Vector3 scale;readonly Quaternion rotation;
            public LegacyRect(RectTransform value){rect=value;min=value.anchorMin;max=value.anchorMax;pivot=value.pivot;position=value.anchoredPosition;size=value.sizeDelta;scale=value.localScale;rotation=value.localRotation;}
            public void Restore(){rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=scale;rect.localRotation=rotation;}
        }
        sealed class LegacyText
        {
            readonly Text text;readonly int size;readonly Color color;readonly TextAnchor alignment;readonly string value;
            public LegacyText(Text item){text=item;size=item.fontSize;color=item.color;alignment=item.alignment;value=item.text;}
            public void Restore(){text.fontSize=size;text.color=color;text.alignment=alignment;text.text=value;}
        }
        sealed class LegacyImage
        {
            readonly Image image;readonly Color color;
            public LegacyImage(Image value){image=value;color=value.color;}
            public void Restore(){image.color=color;}
        }
        protected override void OnUnbind() { Data?.onClose?.Invoke();foreach(var button in buttons) button.Unbind();if(v5Area)v5Area.Unbind();if(v5Net)v5Net.Unbind();if(v5Shell)v5Shell.Unbind(); }
    }
}
