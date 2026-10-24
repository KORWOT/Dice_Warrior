using System;
using FateDice;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld.Editor
{
    // Adds v5-only bindings to the caller's open prefab contents. Never saves a prefab or a scene.
    public static class V5RedesignAuthoring
    {
        const string FontPath="Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf";
        const string ButtonPath="Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";
        static readonly Color Ink=new Color32(0x10,0x1B,0x2D,0xFF),Slate=new Color32(0x1C,0x2B,0x42,0xFF),Ivory=new Color32(0xF4,0xEE,0xDF,0xFF),Muted=new Color32(0xB5,0xC1,0xD0,0xFF);
        public static void Upgrade(CompetitionScreen screen)
        {
            if(!screen || !screen.content || !screen.v5Shell || screen.roster==null || screen.roster.Length!=4)
                throw new InvalidOperationException("Expected an authored UI11 CompetitionScreen with four participants.");
            var font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if(!font || !prefab)throw new InvalidOperationException("Missing shared UI authoring input.");
            var shell=screen.v5Shell;
            // Existing board/roster/tray transforms and colours stay untouched: runtime captures their legacy baseline once.
            if(screen.v5RosterCards==null || screen.v5RosterCards.Length==0)
            {
                var parent=screen.roster[0].transform.parent;screen.v5RosterCards=new Image[4];
                for(int i=0;i<4;i++)
                {
                    string name="V5 participant card "+i;if(parent.Find(name))throw new InvalidOperationException("Unbound "+name);
                    var rect=Rect(parent,name,i*167.5f,0,157.5f,92);rect.SetAsFirstSibling();
                    var panel=rect.gameObject.AddComponent<Image>();panel.color=Slate;panel.raycastTarget=false;screen.v5RosterCards[i]=panel;rect.gameObject.SetActive(false);
                }
            }
            else if(screen.v5RosterCards.Length!=4 || Array.Exists(screen.v5RosterCards,x=>!x))throw new InvalidOperationException("Incomplete V5 participant card bindings.");
            if(!shell.boardPrimary)
            {
                const string name="Fixed board advance";if(shell.transform.Find(name))throw new InvalidOperationException("Unbound "+name);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,shell.transform);go.name=name;SetRect((RectTransform)go.transform,30,1160,660,96);
                shell.boardPrimary=go.GetComponent<CommonButtonView>();var button=shell.boardPrimary;
                button.label.font=font;button.label.fontSize=28;button.label.alignment=TextAnchor.MiddleCenter;button.label.color=Ink;button.label.supportRichText=false;button.label.resizeTextForBestFit=false;
                button.label.raycastTarget=false;button.background.raycastTarget=true;
                button.label.rectTransform.anchorMin=Vector2.zero;button.label.rectTransform.anchorMax=Vector2.one;button.label.rectTransform.offsetMin=new Vector2(16,8);button.label.rectTransform.offsetMax=new Vector2(-16,-8);
                Record(button);go.SetActive(false);
            }
            if(!shell.rewardGrowth)
            {
                const string name="V5 new die joins collection";if(shell.transform.Find(name))throw new InvalidOperationException("Unbound "+name);
                var growth=Rect(shell.transform,name,0,0,720,1280);shell.rewardGrowth=growth.gameObject;
                var panel=Rect(growth,"Collection display panel",30,280,660,660).gameObject.AddComponent<Image>();panel.color=Slate;panel.raycastTarget=false;
                shell.rewardDice=new V5DieGraphic[3];
                for(int i=0;i<3;i++)
                {
                    var rect=Rect(growth,"Collection die "+(i+1),57+i*215,360,176,190);rect.gameObject.AddComponent<CanvasRenderer>();
                    var group=rect.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
                    shell.rewardDice[i]=rect.gameObject.AddComponent<V5DieGraphic>();shell.rewardDice[i].raycastTarget=false;
                }
                shell.rewardGrowthLabel=Label(growth,"Collection increase explanation",font,27,54,590,612,280);shell.rewardGrowthLabel.alignment=TextAnchor.UpperCenter;
                growth.gameObject.SetActive(false);
            }
            else if(!shell.rewardGrowthLabel || shell.rewardDice==null || shell.rewardDice.Length!=3 || Array.Exists(shell.rewardDice,x=>!x))
                throw new InvalidOperationException("Incomplete V5 collection display bindings.");
            shell.heading.color=Ivory;shell.heading.fontSize=29;shell.summary.color=Muted;shell.body.color=Ivory;shell.notice.color=Ivory;
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;SetRect(rect,x,y,width,height);return rect;}
        static Text Label(Transform parent,string name,Font font,int size,float x,float y,float width,float height)
        {
            var rect=Rect(parent,name,x,y,width,height);rect.gameObject.AddComponent<CanvasRenderer>();var text=rect.gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=size;text.color=Ivory;text.raycastTarget=false;text.supportRichText=false;text.resizeTextForBestFit=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        static void Record(CommonButtonView view)
        {PrefabUtility.RecordPrefabInstancePropertyModifications(view.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label);PrefabUtility.RecordPrefabInstancePropertyModifications(view.label.rectTransform);PrefabUtility.RecordPrefabInstancePropertyModifications(view.background);}
        static void SetRect(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
