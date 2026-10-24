using System;
using DiceWarrior.SharedWorld;
using FateDice;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld.Editor
{
    // Receives the caller's open prefab contents. The caller alone owns saving and scene state.
    public static class CompetitionCombatUiAuthoring
    {
        public static void Upgrade(CompetitionScreen screen)
        {
            if(!screen || !screen.v5Area || !screen.v5Net)throw new InvalidOperationException("Existing authored v5 Area/Net required");
            if(!screen.v5Area.transform.IsChildOf(screen.transform) || !screen.v5Net.transform.IsChildOf(screen.transform))
                throw new InvalidOperationException("Combat UI must belong to the supplied prefab");
            Area(screen.v5Area);Net(screen.v5Net);
            Record(screen.v5Area);Record(screen.v5Net);
        }
        static void Area(V5DiceAreaView view)
        {
            Rect(view.heading.rectTransform,30,12,660,36);Style(view.heading,20);
            Rect(view.player.rectTransform,30,56,660,44);Style(view.player,23);
            Health(view.playerFill,30,104,660);
            Rect(view.enemy.rectTransform,30,124,660,44);Style(view.enemy,23);
            Health(view.enemyFill,30,174,660);
            Rect(view.intent.rectTransform,30,192,660,32);Style(view.intent,18);
            Button(view.diceInfo,30,228,320,96,21);Button(view.combatInfo,370,228,320,96,21);
            Rect(view.rollZone,30,338,660,230);Rect(view.holdZone,30,590,660,230);
            Rect(view.rollHint.rectTransform,18,10,624,38);Rect(view.holdHint.rectTransform,18,10,624,38);Style(view.rollHint,17);Style(view.holdHint,17);
            for(int i=0;i<view.dice.Length;i++)
            {
                var button=view.dice[i];var rect=(RectTransform)button.transform;rect.SetParent(view.rollZone,false);
                Rect(rect,0,0,180,148);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=V5DiceAreaView.CardPosition(i);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
                Rect(view.faces[i].rectTransform,66,5,48,48);view.faces[i].raycastTarget=false;
                Rect(button.label.rectTransform,6,57,168,86);Style(button.label,15);button.label.alignment=TextAnchor.MiddleCenter;
                RecordButton(button);Record(view.faces[i]);
            }
            Button(view.roll,180,406,360,96,23);
            Rect(view.preview.rectTransform,30,842,660,158);Style(view.preview,18);
            view.feedbackScroll=Scroll(view.layout,"Combat feedback scroll",view.notice,30,1012,660,112);Style(view.notice,17);
            Button(view.reroll,30,0,320,96,22);Button(view.execute,370,0,320,96,23);
            Record(view.feedbackScroll);Record(view);
        }
        static void Net(V5DiceNetView view)
        {
            Rect(view.title.rectTransform,30,24,660,72);Style(view.title,26);
            for(int i=0;i<view.tabs.Length;i++)Button(view.tabs[i],30+i*224,112,212,96,18);
            Rect(view.net,40,224,640,474);
            for(int i=0;i<view.cells.Length;i++)
            {
                var at=V5DiceNetView.Cell(i);Button(view.cells[i],at.x*158,at.y*158,154,154,15);
                Rect(view.faces[i].rectTransform,50,4,54,54);
                Rect(view.cells[i].label.rectTransform,4,61,146,89);Style(view.cells[i].label,15);view.cells[i].label.alignment=TextAnchor.MiddleCenter;
                RecordButton(view.cells[i]);Record(view.faces[i]);
            }
            view.detailsScroll=Scroll(view.layout,"Information details scroll",view.description,30,714,660,308);Style(view.description,18);
            Button(view.previous,30,1040,320,96,21);Button(view.next,370,1040,320,96,21);
            Button(view.stats,30,888,660,96,21);
            view.managementHeading=Text(view.layout,"Adventure menu heading",view.title.font,18,30,995,660,30);
            view.managementHeading.text="모험 메뉴 · 진행 유지 또는 내 참가자 포기";
            Button(view.leave,30,1032,320,96,20);Button(view.abandon,370,1032,320,96,20);
            Button(view.close,30,1160,660,96,23);
            view.blocker.color=new Color(.015f,.025f,.045f,1);view.blocker.raycastTarget=true;Record(view.blocker);Record(view);
        }
        static void Health(Image fill,float x,float y,float width)
        {
            var track=(RectTransform)fill.transform.parent;Rect(track,x,y,width,10);
            var rect=fill.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;Record(rect);Record(fill);
        }
        static void Button(CommonButtonView button,float x,float y,float width,float height,int fontSize)
        {
            Rect((RectTransform)button.transform,x,y,width,height);var label=button.label.rectTransform;
            label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(8,4);label.offsetMax=new Vector2(-8,-4);
            Style(button.label,fontSize);button.label.alignment=TextAnchor.MiddleCenter;RecordButton(button);
        }
        static ScrollRect Scroll(Transform parent,string name,Text content,float x,float y,float width,float height)
        {
            var child=parent.Find(name);GameObject go=child?child.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            if(!child)go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Rect(rect,x,y,width,height);
            var image=go.GetComponent<Image>();image.color=new Color(.01f,.02f,.035f,.1f);image.raycastTarget=true;
            var scroll=go.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=false;scroll.scrollSensitivity=26;
            content.transform.SetParent(rect,false);Rect(content.rectTransform,0,0,width,height);content.raycastTarget=false;
            scroll.viewport=rect;scroll.content=content.rectTransform;Record(scroll);Record(content);Record(image);return scroll;
        }
        static Text Text(Transform parent,string name,Font font,int size,float x,float y,float width,float height)
        {
            var existing=parent.Find(name);var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            if(!existing)go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=font;text.color=new Color(.7f,.84f,.88f);Rect(text.rectTransform,x,y,width,height);Style(text,size);return text;
        }
        static void Style(Text text,int size)
        {text.fontSize=size;text.resizeTextForBestFit=false;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;Record(text);Record(text.rectTransform);}
        static void Rect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);Record(rect);}
        static void RecordButton(CommonButtonView button)
        {Record(button);Record(button.transform);Record(button.label);Record(button.label.rectTransform);Record(button.background);}
        static void Record(UnityEngine.Object value)
        {if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);EditorUtility.SetDirty(value);}
    }
}
