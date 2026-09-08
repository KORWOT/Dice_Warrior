using System;
using System.Collections;
using UnityEngine;

namespace FateDice
{
    public sealed class ExplorationNodeView : MonoBehaviour
    {
        public CommonButtonView frame;
        public string NodeId { get; private set; }
        public NodeType Type { get; private set; }
        public bool IsFading { get; private set; }
        Coroutine fade;
        int generation;
        public void Bind(string nodeId,NodeType type,string label,VisualArtwork visual,ButtonAppearance style,bool selectable,bool selected,Action<string> selectedCallback)
        {
            if(!frame)throw new InvalidOperationException("ExplorationNodeView requires its common frame.");
            if(visual==null)throw new ArgumentNullException(nameof(visual));
            Unbind();enabled=true;gameObject.SetActive(true);
            frame.Bind(nodeId,label,visual.icon?visual.icon:visual.artwork,visual.fallbackGlyph,style,selectable,selected,selectedCallback);
            frame.icon.color=visual.tint;frame.iconFallback.color=visual.tint;
            NodeId=nodeId;Type=type;
        }
        public void Unbind()
        {
            CancelFade();NodeId=null;Type=default;
            if(frame)frame.Unbind();
        }
        public void FadeOut(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(string.IsNullOrEmpty(NodeId)||!frame)return;
            CancelFade();
            frame.button.interactable=false;frame.group.interactable=false;frame.group.blocksRaycasts=false;
            if(seconds==0||!isActiveAndEnabled)
            {
                frame.group.alpha=0;gameObject.SetActive(false);return;
            }
            IsFading=true;
            fade=StartCoroutine(Fade(seconds,generation));
        }
        IEnumerator Fade(float seconds,int version)
        {
            var initial=frame.group.alpha;float elapsed=0;
            while(elapsed<seconds)
            {
                if(version!=generation||!frame)yield break;
                elapsed+=Time.unscaledDeltaTime;
                frame.group.alpha=Mathf.Lerp(initial,0,Mathf.Clamp01(elapsed/seconds));
                yield return null;
            }
            if(version!=generation||!frame)yield break;
            fade=null;IsFading=false;frame.group.alpha=0;gameObject.SetActive(false);
        }
        void CancelFade()
        {
            generation++;
            if(fade!=null){StopCoroutine(fade);fade=null;}
            IsFading=false;
        }
        void OnDisable()=>CancelFade();
        void OnDestroy()=>Unbind();
    }
}
