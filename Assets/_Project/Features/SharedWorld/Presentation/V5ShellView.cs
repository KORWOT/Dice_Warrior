using System;
using FateDice;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    public sealed class V5ShellAction
    {
        public string id,label,detail;
        public bool enabled=true,selected;
        public Action invoke;
    }
    public sealed class V5ShellData
    {
        public string mode,title,summary,body,notice;
        public bool showBoard,compactChoices;
        public V5ShellAction primary;
        public V5ShellAction[] choices=Array.Empty<V5ShellAction>(),tools=Array.Empty<V5ShellAction>(),tabs=Array.Empty<V5ShellAction>();
        public Action back;
    }

    // Authored v5 presentation only: local pagination/scroll never executes game commands.
    public sealed class V5ShellView:MonoBehaviour
    {
        public Text heading,summary,body,notice,pageLabel;
        public Text[] choiceDetails;
        public CommonButtonView[] choices,tools,tabs;
        public CommonButtonView back,previous,next,boardPrimary;
        public RectTransform viewport,scrollContent;
        public ScrollRect scroll;
        public Image contextBackground;
        public GameObject legacyFooter;
        public GameObject rewardGrowth;
        public Text rewardGrowthLabel;
        public V5DieGraphic[] rewardDice;
        V5ShellData data;
        int page;
        string mode;
        static readonly Color Ink=new Color32(0x10,0x1B,0x2D,0xFF),Slate=new Color32(0x1C,0x2B,0x42,0xFF),Ivory=new Color32(0xF4,0xEE,0xDF,0xFF),Muted=new Color32(0xB5,0xC1,0xD0,0xFF),Amber=new Color32(0xF3,0xBD,0x5C,0xFF),Teal=new Color32(0x52,0xD6,0xCD,0xFF);
        static readonly ButtonAppearance ChoiceStyle=new ButtonAppearance {
            normal=Slate,selected=Teal,pressed=new Color(.18f,.55f,.55f),disabled=new Color(.12f,.15f,.20f)
        };
        static readonly ButtonAppearance ToolStyle=new ButtonAppearance {
            normal=Slate,selected=Teal,pressed=new Color(.18f,.55f,.55f),disabled=new Color(.10f,.12f,.15f)
        };
        static readonly ButtonAppearance PrimaryStyle=new ButtonAppearance {normal=Amber,selected=Amber,pressed=new Color(.85f,.61f,.23f),disabled=Slate};

        public int ChoicePage=>page;
        public V5ShellData Data=>data;
        public void Bind(V5ShellData value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));
            if(rewardGrowth)rewardGrowth.SetActive(false);
            if(choices==null || choices.Length!=6 || choiceDetails==null || choiceDetails.Length!=6 || tools==null || tools.Length!=4 || tabs==null || tabs.Length!=5 || !scroll || !viewport || !scrollContent)
                throw new InvalidOperationException("V5 주요 화면의 authored 연결을 확인해 주세요.");
            if(value.tools.Length>tools.Length || value.tabs.Length>tabs.Length)
                throw new InvalidOperationException("V5 공통 도구/탭의 표시 계약을 초과했습니다.");
            bool fixedPrimary=value.primary!=null;
            if(fixedPrimary && (value.showBoard || value.back!=null || value.tools.Length>2 || Array.Exists(value.choices,x=>x.id==value.primary.id)))
                throw new InvalidOperationException("고정 진행 행동은 보드·뒤로·중복 선택 없이 헤더 도구 최대 2개와 함께 사용합니다.");
            if(value.compactChoices && value.mode!="lobby")throw new InvalidOperationException("두 열 선택은 로비의 보조 행동에만 사용합니다.");
            bool changed=mode!=value.mode;float scrollPosition=changed?1f:scroll.verticalNormalizedPosition;
            data=value;mode=value.mode;if(changed)page=0;
            int pages=Math.Max(1,(value.choices.Length+choices.Length-1)/choices.Length);
            page=Math.Max(0,Math.Min(page,pages-1));
            heading.text=value.title??"";summary.text=value.summary??"";notice.text=value.notice??"";
            heading.fontSize=29;summary.fontSize=19;
            heading.color=Ivory;summary.color=Muted;body.color=Ivory;notice.color=Amber;pageLabel.color=Muted;
            if(viewport.TryGetComponent<Image>(out var backdrop))backdrop.color=Ink;
            if(value.showBoard){BindBoard(value);return;}
            if(boardPrimary){boardPrimary.Unbind();boardPrimary.gameObject.SetActive(false);}
            if(!contextBackground)throw new InvalidOperationException("V5 본문 요약 패널을 authoring으로 연결해 주세요.");
            viewport.gameObject.SetActive(true);
            float headerWidth=fixedPrimary?440:660;
            SetRect(heading.rectTransform,30,20,headerWidth,52);
            while(heading.preferredHeight>52 && heading.fontSize>24)--heading.fontSize;
            SetRect(summary.rectTransform,30,78,headerWidth,42);
            float summaryHeight=Mathf.Max(42,summary.preferredHeight+8);
            SetRect(summary.rectTransform,30,78,headerWidth,summaryHeight);
            float top=90+summaryHeight;
            if(value.tabs.Length>0)
            {
                for(int i=0;i<tabs.Length;i++)
                {
                    bool shown=i<value.tabs.Length;tabs[i].gameObject.SetActive(shown);
                    if(!shown){tabs[i].Unbind();continue;}
                    SetRect((RectTransform)tabs[i].transform,30+(i%3)*224,top+(i/3)*106,212,96);
                    BindButton(tabs[i],value.tabs[i],ToolStyle);
                }
                top+=Mathf.Ceil(value.tabs.Length/3f)*106+10;
            }
            else foreach(var tab in tabs){tab.Unbind();tab.gameObject.SetActive(false);}
            int toolRows=fixedPrimary?0:(value.tools.Length+1)/2;
            float footerHeight=toolRows*106+(value.back!=null?106:0);
            float footerTop=fixedPrimary?1160:1260-footerHeight;
            for(int i=0;i<tools.Length;i++)
            {
                bool shown=i<value.tools.Length;tools[i].gameObject.SetActive(shown);
                if(!shown){tools[i].Unbind();continue;}
                tools[i].label.fontSize=21;
                int countInRow=Math.Min(2,value.tools.Length-(i/2)*2);
                if(fixedPrimary)SetRect((RectTransform)tools[i].transform,482+i*108,20,100,96);
                else SetRect((RectTransform)tools[i].transform,30+(i%2)*338,footerTop+(i/2)*106,countInRow==1?660:322,96);
                BindButton(tools[i],value.tools[i],ToolStyle);
            }
            back.gameObject.SetActive(value.back!=null);
            if(value.back!=null)
            {
                SetRect((RectTransform)back.transform,30,footerTop+toolRows*106,660,96);
                BindButton(back,new V5ShellAction{id="shell-back",label="돌아가기",invoke=value.back},ToolStyle);
            }
            else back.Unbind();
            if(fixedPrimary)
            {
                if(!boardPrimary)throw new InvalidOperationException("V5 고정 진행 버튼을 authoring으로 연결해 주세요.");
                SetRect((RectTransform)boardPrimary.transform,30,1160,660,96);boardPrimary.gameObject.SetActive(true);
                boardPrimary.label.fontSize=28;BindButton(boardPrimary,value.primary,PrimaryStyle);boardPrimary.label.color=value.primary.enabled?Ink:Muted;
                if(string.IsNullOrEmpty(notice.text))notice.text=value.primary.detail??"";
            }
            SetRect(notice.rectTransform,30,0,660,32);
            float noticeHeight=string.IsNullOrEmpty(notice.text)?0:Mathf.Max(32,notice.preferredHeight+8);
            notice.gameObject.SetActive(noticeHeight>0);
            SetRect(notice.rectTransform,30,footerTop-noticeHeight-12,660,noticeHeight);
            SetRect(viewport,30,top,660,Mathf.Max(48,footerTop-top-noticeHeight-24));
            SetRect(scrollContent,0,0,660,viewport.rect.height);
            float y=8;
            body.text=value.body??"";body.gameObject.SetActive(!string.IsNullOrEmpty(body.text));
            body.fontSize=20;SetRect(body.rectTransform,20,y+16,620,26);
            contextBackground.color=new Color(Slate.r,Slate.g,Slate.b,.55f);
            contextBackground.gameObject.SetActive(body.gameObject.activeSelf);
            if(body.gameObject.activeSelf)
            {
                float h=Mathf.Max(26,body.preferredHeight+8);SetRect(body.rectTransform,20,y+16,620,h);
                SetRect(contextBackground.rectTransform,4,y,652,h+32);y+=h+50;
            }
            int columns=value.compactChoices?2:1;float cardWidth=value.compactChoices?319:652,textWidth=cardWidth-36;
            var heights=new float[choices.Length];
            for(int i=0;i<choices.Length;i++)
            {
                int index=page*choices.Length+i;bool shown=index<value.choices.Length;
                choices[i].gameObject.SetActive(shown);
                if(!shown){choices[i].Unbind();continue;}
                var choice=value.choices[index];BindButton(choices[i],choice,ChoiceStyle);
                var title=choices[i].label;var description=choiceDetails[i];description.text=choice.detail??"";description.color=choice.selected?Ink:Muted;
                title.fontSize=value.compactChoices?22:23;description.fontSize=value.compactChoices?18:19;
                SetRect(title.rectTransform,18,12,textWidth,30);SetRect(description.rectTransform,18,46,textWidth,26);
                float titleHeight=Mathf.Max(30,title.preferredHeight+4),descriptionHeight=string.IsNullOrEmpty(description.text)?0:description.preferredHeight+4;
                heights[i]=Mathf.Max(104,titleHeight+descriptionHeight+32);
                SetRect(title.rectTransform,18,12,textWidth,titleHeight);
                description.gameObject.SetActive(descriptionHeight>0);SetRect(description.rectTransform,18,16+titleHeight,textWidth,descriptionHeight);
            }
            for(int row=0;row<choices.Length;row+=columns)
            {
                float height=0;for(int column=0;column<columns && row+column<heights.Length;column++)height=Mathf.Max(height,heights[row+column]);
                if(height<=0)continue;
                for(int column=0;column<columns && row+column<heights.Length;column++)if(heights[row+column]>0)
                    SetRect((RectTransform)choices[row+column].transform,4+column*(cardWidth+14),y,cardWidth,height);
                y+=height+14;
            }
            bool paged=pages>1;pageLabel.gameObject.SetActive(paged);previous.gameObject.SetActive(paged);next.gameObject.SetActive(paged);
            if(paged)
            {
                pageLabel.text=$"선택 목록 {page+1}/{pages}";SetRect(pageLabel.rectTransform,12,y,636,32);y+=40;
                SetRect((RectTransform)previous.transform,4,y,318,96);SetRect((RectTransform)next.transform,338,y,318,96);y+=110;
                BindButton(previous,new V5ShellAction{id="shell-previous",label="이전 목록",enabled=page>0,invoke=()=>ChangePage(-1)},ToolStyle);
                BindButton(next,new V5ShellAction{id="shell-next",label="다음 목록",enabled=page<pages-1,invoke=()=>ChangePage(1)},ToolStyle);
            }
            else{previous.Unbind();next.Unbind();}
            scrollContent.sizeDelta=new Vector2(660,Mathf.Max(viewport.rect.height,y+8));
            scroll.StopMovement();scroll.verticalNormalizedPosition=scrollPosition;
        }
        void BindBoard(V5ShellData value)
        {
            if(!boardPrimary)throw new InvalidOperationException("V5 보드의 고정 이동 버튼을 authoring으로 연결해 주세요.");
            if(value.choices.Length!=1 || value.tools.Length>2 || value.tabs.Length!=0 || value.back!=null)
                throw new InvalidOperationException("V5 보드는 진행 행동 1개와 헤더 도구 최대 2개를 사용합니다.");
            viewport.gameObject.SetActive(false);scroll.StopMovement();
            foreach(var choice in choices){choice.Unbind();choice.gameObject.SetActive(false);}
            foreach(var tab in tabs){tab.Unbind();tab.gameObject.SetActive(false);}
            previous.Unbind();next.Unbind();back.Unbind();previous.gameObject.SetActive(false);next.gameObject.SetActive(false);back.gameObject.SetActive(false);pageLabel.gameObject.SetActive(false);
            SetRect(heading.rectTransform,30,20,440,44);SetRect(summary.rectTransform,30,68,440,48);
            while(heading.preferredHeight>44 && heading.fontSize>24)--heading.fontSize;
            for(int i=0;i<tools.Length;i++)
            {
                bool shown=i<value.tools.Length;tools[i].gameObject.SetActive(shown);
                if(!shown){tools[i].Unbind();continue;}
                SetRect((RectTransform)tools[i].transform,482+i*108,20,100,96);BindButton(tools[i],value.tools[i],ToolStyle);
            }
            SetRect(notice.rectTransform,30,1100,660,48);notice.gameObject.SetActive(true);notice.color=Ivory;
            SetRect((RectTransform)boardPrimary.transform,30,1160,660,96);boardPrimary.gameObject.SetActive(true);
            BindButton(boardPrimary,value.choices[0],PrimaryStyle);boardPrimary.label.color=Ink;
            notice.text=string.IsNullOrEmpty(value.notice)?value.choices[0].detail??"":value.notice;
        }
        // Playback updates only presentation. The original action/ID remains the App-owned command path.
        public void SetBoardPresentation(string stage,string detail,bool busy)
        {
            if(data==null || !data.showBoard || !boardPrimary)return;
            var action=data.choices[0];
            BindButton(boardPrimary,new V5ShellAction {id=action.id,label=string.IsNullOrEmpty(stage)?action.label:stage,enabled=action.enabled&&!busy,invoke=action.invoke},PrimaryStyle);
            boardPrimary.label.color=busy?Muted:Ink;
            notice.text=detail??"";notice.gameObject.SetActive(true);notice.color=Ivory;
            foreach(var tool in tools)if(tool.gameObject.activeSelf)
            {tool.button.interactable=!busy&&Array.Exists(data.tools,x=>x.id==tool.BoundId&&x.enabled);tool.group.interactable=tool.button.interactable;tool.group.blocksRaycasts=tool.button.interactable;}
        }
        public void ShowDiceGrowth(CompetitionState state,int previousCount,float progress,bool reducedMotion)
        {
            if(state==null || !CompetitionV5Content.IsEnabled(state.definition))return;
            if(!rewardGrowth || !rewardGrowthLabel || rewardDice==null || rewardDice.Length!=3)
                throw new InvalidOperationException("V5 주사위 획득 표시를 authoring으로 연결해 주세요.");
            var player=state.participants[0];int count=player.dice.Count;
            if(count!=previousCount+1 || previousCount<1 || count>3)throw new ArgumentException("Expected one newly acquired combat die.");
            heading.text="완주 보상 수령 완료";summary.text="확정 저장 완료 · 다음 전투의 주사위 구성";
            viewport.gameObject.SetActive(false);foreach(var tool in tools)tool.gameObject.SetActive(false);foreach(var tab in tabs)tab.gameObject.SetActive(false);
            back.gameObject.SetActive(false);if(boardPrimary)boardPrimary.gameObject.SetActive(false);notice.gameObject.SetActive(false);
            rewardGrowth.SetActive(true);progress=Mathf.Clamp01(progress);
            for(int i=0;i<rewardDice.Length;i++)
            {
                var cube=rewardDice[i];cube.gameObject.SetActive(i<count);if(i>=count)continue;
                var faces=CompetitionV5Content.Faces(state.definition,player.adventurerId,player.dice[i].definitionId);
                cube.Configure(faces,0);cube.Pose(1,i);cube.RevealResult=false;
                float center=360+(i-(count-1)*.5f)*215;
                var rect=cube.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(center,-455);rect.sizeDelta=new Vector2(176,190);
                bool added=i==count-1;cube.GetComponent<CanvasGroup>().alpha=added?progress:1;
                cube.transform.localScale=Vector3.one*(added&&!reducedMotion?Mathf.Lerp(.82f,1,progress):1);
            }
            string dieId=player.dice[count-1].definitionId;string name=Array.Find(state.definition.combat.dice,x=>x.id==dieId).name;
            rewardGrowthLabel.text=$"전투 주사위 {previousCount} → {count}개\n{name} 주사위가 내 구성에 추가되었습니다.\n\n다음 전투부터 함께 굴립니다.\n획득 안내 · 굴린 결과가 아닙니다.";
        }
        void ChangePage(int delta)
        {if(data==null)return;page+=delta;Bind(data);scroll.verticalNormalizedPosition=1;}
        static void BindButton(CommonButtonView button,V5ShellAction action,ButtonAppearance style)
        {
            button.Bind(action.id, (action.selected?"[선택] ":"")+(action.label??""),null,"",style,action.enabled,action.selected,_=>action.invoke?.Invoke());
            button.label.color=action.selected?Ink:Ivory;
        }
        public void Unbind()
        {
            data=null;mode=null;page=0;
            if(rewardGrowth)rewardGrowth.SetActive(false);
            if(choices!=null)foreach(var button in choices)if(button)button.Unbind();
            if(tools!=null)foreach(var button in tools)if(button)button.Unbind();
            if(tabs!=null)foreach(var button in tabs)if(button)button.Unbind();
            if(back)back.Unbind();if(previous)previous.Unbind();if(next)next.Unbind();if(boardPrimary)boardPrimary.Unbind();
        }
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
