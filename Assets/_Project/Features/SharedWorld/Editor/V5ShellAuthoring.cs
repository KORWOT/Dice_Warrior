using System;
using FateDice;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld.Editor
{
    // The caller owns prefab loading/saving. This authoring method never opens or saves a scene.
    public static class V5ShellAuthoring
    {
        const string FontPath="Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf";
        const string ButtonPath="Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";
        public static void Upgrade(CompetitionScreen screen)
        {
            if(!screen || !screen.content)throw new InvalidOperationException("Expected authored CompetitionScreen content");
            if(screen.v5Shell)return;
            if(screen.content.Find("V5 Main Shell"))throw new InvalidOperationException("Unbound V5 Main Shell; inspect before authoring");
            var font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if(!font || !prefab)throw new InvalidOperationException("Missing shared UI authoring input");
            var root=Rect(screen.content,"V5 Main Shell",0,0,720,1280);
            var view=root.gameObject.AddComponent<V5ShellView>();screen.v5Shell=view;
            view.heading=Label(root,"Screen heading",font,27,30,20,660,52);view.heading.color=new Color(.98f,.84f,.55f);
            view.summary=Label(root,"Current context",font,19,30,78,660,60);
            view.notice=Label(root,"State feedback",font,18,30,1000,660,50);view.notice.color=new Color(.99f,.76f,.44f);
            var scrollRoot=Rect(root,"Main content scroll",30,160,660,840);view.viewport=scrollRoot;
            var viewportImage=scrollRoot.gameObject.AddComponent<Image>();viewportImage.color=new Color(.025f,.055f,.085f,.82f);viewportImage.raycastTarget=true;
            scrollRoot.gameObject.AddComponent<RectMask2D>();view.scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();
            view.scrollContent=Rect(scrollRoot,"Scrollable content",0,0,660,840);
            view.scroll.viewport=scrollRoot;view.scroll.content=view.scrollContent;view.scroll.horizontal=false;view.scroll.vertical=true;
            view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.inertia=false;view.scroll.scrollSensitivity=42;
            view.body=Label(view.scrollContent,"Current explanation",font,20,12,8,636,100);
            view.choices=new CommonButtonView[6];view.choiceDetails=new Text[6];
            for(int i=0;i<6;i++)
            {
                view.choices[i]=Button(view.scrollContent,"Choice card "+i,font,prefab,4,120+i*132,652,120);
                view.choices[i].label.fontSize=23;view.choices[i].label.alignment=TextAnchor.UpperLeft;SetRect(view.choices[i].label.rectTransform,18,12,616,32);
                view.choiceDetails[i]=Label(view.choices[i].transform,"Choice explanation",font,19,18,48,616,60);view.choiceDetails[i].color=new Color(.75f,.84f,.88f);
                Record(view.choices[i]);
            }
            view.pageLabel=Label(view.scrollContent,"Choice page",font,19,12,940,636,32);
            view.previous=Button(view.scrollContent,"Previous choices",font,prefab,4,980,318,96);
            view.next=Button(view.scrollContent,"Next choices",font,prefab,338,980,318,96);
            view.tools=new CommonButtonView[4];for(int i=0;i<4;i++)view.tools[i]=Button(root,"Information or menu "+i,font,prefab,30+(i%2)*338,1048+(i/2)*106,322,96);
            view.tabs=new CommonButtonView[5];for(int i=0;i<5;i++)view.tabs[i]=Button(root,"Information tab "+i,font,prefab,30+(i%3)*224,160+(i/3)*106,212,96);
            view.back=Button(root,"Return to previous screen",font,prefab,30,1164,660,96);
            var footer=screen.content.Find("Preview footer");view.legacyFooter=footer?footer.gameObject:null;
            root.gameObject.SetActive(false);
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;SetRect(rect,x,y,width,height);return rect;}
        static Text Label(Transform parent,string name,Font font,int size,float x,float y,float width,float height)
        {
            var rect=Rect(parent,name,x,y,width,height);rect.gameObject.AddComponent<CanvasRenderer>();var text=rect.gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=size;text.color=new Color(.89f,.94f,.96f);text.alignment=TextAnchor.UpperLeft;text.raycastTarget=false;
            text.supportRichText=false;text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        static CommonButtonView Button(Transform parent,string name,Font font,GameObject prefab,float x,float y,float width,float height)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=name;SetRect((RectTransform)go.transform,x,y,width,height);
            var view=go.GetComponent<CommonButtonView>();view.label.font=font;view.label.fontSize=21;view.label.alignment=TextAnchor.MiddleCenter;
            view.label.resizeTextForBestFit=false;view.label.supportRichText=false;view.label.raycastTarget=false;view.background.raycastTarget=true;
            view.label.rectTransform.anchorMin=Vector2.zero;view.label.rectTransform.anchorMax=Vector2.one;view.label.rectTransform.offsetMin=new Vector2(12,8);view.label.rectTransform.offsetMax=new Vector2(-12,-8);
            Record(view);return view;
        }
        static void Record(CommonButtonView view)
        {PrefabUtility.RecordPrefabInstancePropertyModifications(view.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label.rectTransform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.background);}
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
