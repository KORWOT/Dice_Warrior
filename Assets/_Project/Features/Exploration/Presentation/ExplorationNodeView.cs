using System;
using System.Collections;
using UnityEngine;

namespace FateDice
{
    public sealed class ExplorationNodeView : MonoBehaviour
    {
        public CommonButtonView frame;
        public MapNodeGraphic mapGraphic;
        public int layoutVersion;
        public string NodeId { get; private set; }
        public NodeType Type { get; private set; }
        public NodeType? PublicType { get; private set; }
        public bool Revealed { get; private set; }
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
            NodeId=nodeId;Type=type;PublicType=type;Revealed=true;
            ApplyMapAppearance(type,visual,selectable,false,false,selected,false);
        }
        public void BindCampaign(CampaignNodeUIData node, bool current, bool selected,
            VisualArtwork visual, ButtonAppearance style, Action<string> preview)
        {
            if(node==null)throw new ArgumentNullException(nameof(node));
            NodeType? shown = node.revealed ? node.type : null;
            var artwork = shown.HasValue ? visual : new VisualArtwork { tint = new Color(.43f,.47f,.52f), fallbackGlyph = "◇" };
            // Bind retains the reusable frame's listener/fade ownership. No private type is resolved here.
            Bind(node.id,shown.GetValueOrDefault(),shown.HasValue?KoreanText.Node(shown.Value):"미발견",
                artwork,style,node.available,selected,preview);
            PublicType=shown;Revealed=shown.HasValue;
            ApplyMapAppearance(shown,artwork,node.available,node.completed,node.unreachable,selected,current);
        }
        internal void ApplyMapAppearance(NodeType? type,VisualArtwork visual,bool available,bool completed,bool unreachable,bool selected,bool current)
        {
            if(!mapGraphic)return;
            var tone=unreachable?new Color(.32f,.35f,.39f):completed?new Color(.44f,.53f,.48f):
                available?visual.tint:new Color(.49f,.52f,.56f);
            if(!type.HasValue)tone=new Color(.36f,.39f,.44f);
            frame.border.gameObject.SetActive(false);
            bool hasPicture=type.HasValue&&(visual.icon||visual.artwork);
            frame.icon.gameObject.SetActive(hasPicture);frame.icon.enabled=hasPicture;
            frame.iconFallback.gameObject.SetActive(false);
            frame.label.color=unreachable?new Color(.39f,.42f,.46f):completed?new Color(.52f,.60f,.56f):
                available?new Color(.91f,.92f,.88f):new Color(.61f,.63f,.66f);
            frame.group.alpha = unreachable ? .42f : completed ? .72f : available || current || selected ? 1f : .68f;
            mapGraphic.Bind(type,tone,available,completed,unreachable,selected,current,!hasPicture);
        }
        public void Unbind()
        {
            CancelFade();NodeId=null;Type=default;PublicType=null;Revealed=false;
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
