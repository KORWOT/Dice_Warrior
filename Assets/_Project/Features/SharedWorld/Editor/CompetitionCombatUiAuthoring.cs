using System;
using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;
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
            var font=view.heading.font;view.GetComponent<Image>().color=new Color(.063f,.106f,.176f);
            Rect(view.heading.rectTransform,30,12,470,36);Style(view.heading,20);
            Rect(view.enemy.rectTransform,30,58,455,40);Style(view.enemy,21);Health(view.enemyFill,30,108,660);
            Rect(view.intent.rectTransform,30,127,660,28);Style(view.intent,18);
            Button(view.combatInfo,510,6,180,96,18);
            view.battleStage=Panel(view.layout,"V5 battle stage",30,164,660,192,new Color(.08f,.13f,.21f));
            view.heroGlyph=Glyph(view.battleStage,"Adventurer",LoopGlyph.Pawn,130,84,100,112,new Color(.32f,.84f,.8f));
            view.enemyGlyph=Glyph(view.battleStage,"Enemy",LoopGlyph.Enemy,530,84,112,112,new Color(.94f,.48f,.38f));
            view.effectGlyph=Glyph(view.battleStage,"Skill effect",LoopGlyph.Sword,330,84,72,72,new Color(.95f,.74f,.36f));view.effectGlyph.gameObject.SetActive(false);
            view.stageHeroName=Text(view.battleStage,"Adventurer name",font,17,20,157,190,30);view.stageHeroName.alignment=TextAnchor.MiddleCenter;
            view.stageEnemyName=Text(view.battleStage,"Enemy name",font,17,450,157,190,30);view.stageEnemyName.alignment=TextAnchor.MiddleCenter;
            view.stageAction=Text(view.battleStage,"Current skill",font,18,188,8,284,32);view.stageAction.alignment=TextAnchor.MiddleCenter;
            view.impact=Text(view.battleStage,"Actual impact",font,16,215,140,230,44);view.impact.alignment=TextAnchor.MiddleCenter;
            Rect(view.player.rectTransform,30,370,660,36);Style(view.player,21);Health(view.playerFill,30,414,660);
            Rect(view.rollZone,30,436,660,352);view.rollZone.GetComponent<Image>().color=new Color(.11f,.17f,.26f);
            view.holdHint.transform.SetParent(view.rollZone,false);view.holdZone.gameObject.SetActive(false);
            Rect(view.rollHint.rectTransform,18,12,624,34);Rect(view.holdHint.rectTransform,18,49,624,26);Style(view.rollHint,21);Style(view.holdHint,16);
            view.cubes=new V5DieGraphic[3];
            for(int i=0;i<view.dice.Length;i++)
            {
                var button=view.dice[i];var rect=(RectTransform)button.transform;rect.SetParent(view.rollZone,false);
                Rect(rect,0,0,200,248);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=V5DiceAreaView.SlotPosition(i,3);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
                view.faces[i].gameObject.SetActive(false);view.faces[i].raycastTarget=false;
                var existing=rect.Find("Physical six face cube");var go=existing?existing.gameObject:new GameObject("Physical six face cube",typeof(RectTransform),typeof(CanvasRenderer),typeof(V5DieGraphic));
                if(!existing)go.transform.SetParent(rect,false);view.cubes[i]=go.GetComponent<V5DieGraphic>();Rect(view.cubes[i].rectTransform,20,0,160,180);view.cubes[i].raycastTarget=false;
                Rect(button.label.rectTransform,5,176,190,68);Style(button.label,16);button.label.alignment=TextAnchor.MiddleCenter;
                RecordButton(button);Record(view.faces[i]);Record(view.cubes[i]);
            }
            Rect(view.preview.rectTransform,30,808,660,176);Style(view.preview,18);view.preview.color=new Color(.95f,.74f,.36f);
            Button(view.diceInfo,30,994,200,96,18);
            view.feedbackScroll=Scroll(view.layout,"Combat feedback scroll",view.notice,246,994,444,130);Style(view.notice,16);
            view.roll.transform.SetParent(view.footer,false);Button(view.roll,30,0,660,96,24);
            Button(view.reroll,30,0,242,96,21);Button(view.execute,290,0,400,96,24);
            foreach(var value in new UnityEngine.Object[]{view.stageHeroName,view.stageEnemyName,view.stageAction,view.impact,view.rollZone.GetComponent<Image>(),view.GetComponent<Image>()})Record(value);
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
            Button(view.stats,30,888,320,96,20);
            if(!view.motion)
            {
                var existing=view.layout.Find("Reduce motion");
                if(existing)view.motion=existing.GetComponent<CommonButtonView>();
                else
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab");
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,view.layout);go.name="Reduce motion";view.motion=go.GetComponent<CommonButtonView>();view.motion.label.font=view.title.font;
                }
            }
            Button(view.motion,370,888,320,96,20);view.motion.gameObject.SetActive(false);
            view.managementHeading=Text(view.layout,"Adventure menu heading",view.title.font,18,30,995,660,30);
            view.managementHeading.text="모험 메뉴 · 진행 유지 또는 내 참가자 포기";
            Button(view.leave,30,1032,320,96,20);Button(view.abandon,370,1032,320,96,20);
            Button(view.close,30,1160,660,96,23);
            view.blocker.color=new Color(.063f,.106f,.176f,1);view.blocker.raycastTarget=true;Record(view.blocker);Record(view);
        }
        static RectTransform Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var existing=parent.Find(name);var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            if(!existing)go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Rect(rect,x,y,width,height);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;Record(image);return rect;
        }
        static LoopGlyphGraphic Glyph(Transform parent,string name,LoopGlyph glyph,float x,float y,float width,float height,Color color)
        {
            var existing=parent.Find(name);var go=existing?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(LoopGlyphGraphic));
            if(!existing)go.transform.SetParent(parent,false);var graphic=go.GetComponent<LoopGlyphGraphic>();Rect(graphic.rectTransform,x,y,width,height);graphic.rectTransform.pivot=new Vector2(.5f,.5f);graphic.glyph=glyph;graphic.color=color;graphic.raycastTarget=false;Record(graphic);return graphic;
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
        {text.fontSize=size;text.resizeTextForBestFit=false;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;text.color=new Color(.957f,.933f,.875f);Record(text);Record(text.rectTransform);}
        static void Rect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);Record(rect);}
        static void RecordButton(CommonButtonView button)
        {Record(button);Record(button.transform);Record(button.label);Record(button.label.rectTransform);Record(button.background);}
        static void Record(UnityEngine.Object value)
        {if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);EditorUtility.SetDirty(value);}
    }
}
