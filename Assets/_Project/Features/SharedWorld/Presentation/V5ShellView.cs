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
        public bool showBoard;
        public V5ShellAction[] choices=Array.Empty<V5ShellAction>(),tools=Array.Empty<V5ShellAction>(),tabs=Array.Empty<V5ShellAction>();
        public Action back;
    }

    // Authored v5 presentation only: local pagination/scroll never executes game commands.
    public sealed class V5ShellView:MonoBehaviour
    {
        public Text heading,summary,body,notice,pageLabel;
        public Text[] choiceDetails;
        public CommonButtonView[] choices,tools,tabs;
        public CommonButtonView back,previous,next;
        public RectTransform viewport,scrollContent;
        public ScrollRect scroll;
        public GameObject legacyFooter;
        V5ShellData data;
        int page;
        string mode;
        static readonly ButtonAppearance ChoiceStyle=new ButtonAppearance {
            normal=new Color(.10f,.20f,.27f),selected=new Color(.16f,.40f,.39f),pressed=new Color(.10f,.48f,.43f),disabled=new Color(.12f,.15f,.20f)
        };
        static readonly ButtonAppearance ToolStyle=new ButtonAppearance {
            normal=new Color(.13f,.17f,.23f),selected=new Color(.22f,.40f,.47f),pressed=new Color(.12f,.44f,.48f),disabled=new Color(.10f,.12f,.15f)
        };

        public int ChoicePage=>page;
        public V5ShellData Data=>data;
        public void Bind(V5ShellData value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));
            if(choices==null || choices.Length!=6 || choiceDetails==null || choiceDetails.Length!=6 || tools==null || tools.Length!=4 || tabs==null || tabs.Length!=5 || !scroll || !viewport || !scrollContent)
                throw new InvalidOperationException("V5 주요 화면의 authored 연결을 확인해 주세요.");
            if(value.tools.Length>tools.Length || value.tabs.Length>tabs.Length)
                throw new InvalidOperationException("V5 공통 도구/탭의 표시 계약을 초과했습니다.");
            bool changed=mode!=value.mode;float scrollPosition=changed?1f:scroll.verticalNormalizedPosition;
            data=value;mode=value.mode;if(changed)page=0;
            int pages=Math.Max(1,(value.choices.Length+choices.Length-1)/choices.Length);
            page=Math.Max(0,Math.Min(page,pages-1));
            heading.text=value.title??"";summary.text=value.summary??"";notice.text=value.notice??"";
            SetRect(heading.rectTransform,30,20,660,52);
            float summaryHeight=value.showBoard?40:Mathf.Max(42,summary.preferredHeight+8);
            SetRect(summary.rectTransform,30,78,660,summaryHeight);
            float top=value.showBoard?870:90+summaryHeight;
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
            int toolRows=(value.tools.Length+1)/2;
            float footerHeight=toolRows*106+(value.back!=null?106:0);
            float footerTop=1260-footerHeight;
            for(int i=0;i<tools.Length;i++)
            {
                bool shown=i<value.tools.Length;tools[i].gameObject.SetActive(shown);
                if(!shown){tools[i].Unbind();continue;}
                int countInRow=Math.Min(2,value.tools.Length-(i/2)*2);
                SetRect((RectTransform)tools[i].transform,30+(i%2)*338,footerTop+(i/2)*106,countInRow==1?660:322,96);
                BindButton(tools[i],value.tools[i],ToolStyle);
            }
            back.gameObject.SetActive(value.back!=null);
            if(value.back!=null)
            {
                SetRect((RectTransform)back.transform,30,footerTop+toolRows*106,660,96);
                BindButton(back,new V5ShellAction{id="shell-back",label="돌아가기",invoke=value.back},ToolStyle);
            }
            else back.Unbind();
            float noticeHeight=string.IsNullOrEmpty(value.notice)?0:Mathf.Max(32,notice.preferredHeight+8);
            notice.gameObject.SetActive(noticeHeight>0);
            SetRect(notice.rectTransform,30,footerTop-noticeHeight-12,660,noticeHeight);
            SetRect(viewport,30,top,660,Mathf.Max(48,footerTop-top-noticeHeight-24));
            SetRect(scrollContent,0,0,660,viewport.rect.height);
            float y=8;
            body.text=value.body??"";body.gameObject.SetActive(!string.IsNullOrEmpty(body.text));
            if(body.gameObject.activeSelf)
            {
                float h=Mathf.Max(26,body.preferredHeight+8);SetRect(body.rectTransform,12,y,636,h);y+=h+18;
            }
            for(int i=0;i<choices.Length;i++)
            {
                int index=page*choices.Length+i;bool shown=index<value.choices.Length;
                choices[i].gameObject.SetActive(shown);
                if(!shown){choices[i].Unbind();continue;}
                var choice=value.choices[index];BindButton(choices[i],choice,ChoiceStyle);
                var title=choices[i].label;var description=choiceDetails[i];description.text=choice.detail??"";
                float titleHeight=Mathf.Max(30,title.preferredHeight+4),descriptionHeight=string.IsNullOrEmpty(description.text)?0:description.preferredHeight+4;
                float height=Mathf.Max(104,titleHeight+descriptionHeight+32);
                SetRect((RectTransform)choices[i].transform,4,y,652,height);
                SetRect(title.rectTransform,18,12,616,titleHeight);
                description.gameObject.SetActive(descriptionHeight>0);SetRect(description.rectTransform,18,16+titleHeight,616,descriptionHeight);
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
        void ChangePage(int delta)
        {if(data==null)return;page+=delta;Bind(data);scroll.verticalNormalizedPosition=1;}
        static void BindButton(CommonButtonView button,V5ShellAction action,ButtonAppearance style)
        {
            button.Bind(action.id, (action.selected?"[선택] ":"")+(action.label??""),null,"",style,action.enabled,action.selected,_=>action.invoke?.Invoke());
        }
        public void Unbind()
        {
            data=null;mode=null;page=0;
            if(choices!=null)foreach(var button in choices)if(button)button.Unbind();
            if(tools!=null)foreach(var button in tools)if(button)button.Unbind();
            if(tabs!=null)foreach(var button in tabs)if(button)button.Unbind();
            if(back)back.Unbind();if(previous)previous.Unbind();if(next)next.Unbind();
        }
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
