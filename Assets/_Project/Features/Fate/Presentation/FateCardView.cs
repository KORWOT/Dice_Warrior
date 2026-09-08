using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class FateCardView : MonoBehaviour
    {
        public CommonButtonView frame;
        public Image artwork, gradeBadge;
        public Text artworkFallback;
        public string OfferedId { get; private set; }
        public void Bind(string offeredId,NodeType type,Grade grade,VisualArtwork publicVisual,GradeVisualEntry gradeVisual,Color gradeColor,ButtonAppearance style,Action<string> selected)
        {
            if(publicVisual==null)throw new ArgumentNullException(nameof(publicVisual));
            if(gradeVisual==null)throw new ArgumentNullException(nameof(gradeVisual));
            if(!frame||!artwork||!gradeBadge||!artworkFallback)
                throw new InvalidOperationException("FateCardView requires its common frame, public artwork, badge, and fallback references.");
            Unbind();enabled=true;gameObject.SetActive(true);
            frame.Bind(offeredId,KoreanText.Node(type)+"  /  "+KoreanText.Grade(grade),publicVisual.icon,publicVisual.fallbackGlyph,style,true,false,selected);
            frame.label.color=gradeColor;frame.icon.color=publicVisual.tint;frame.iconFallback.color=publicVisual.tint;
            frame.SetBorder(gradeVisual.border,gradeColor);OfferedId=offeredId;
            var picture=publicVisual.artwork?publicVisual.artwork:publicVisual.icon;
            ShowImage(artwork,picture,publicVisual.tint);
            artworkFallback.text=picture?"":publicVisual.fallbackGlyph??"";
            artworkFallback.color=publicVisual.tint;
            artworkFallback.gameObject.SetActive(!picture&&!string.IsNullOrEmpty(artworkFallback.text));
            ShowImage(gradeBadge,gradeVisual.badge,gradeColor);
        }
        public void Unbind()
        {
            OfferedId=null;
            if(frame)frame.Unbind();
            ShowImage(artwork,null,Color.white);ShowImage(gradeBadge,null,Color.white);
            if(artworkFallback){artworkFallback.text="";artworkFallback.color=Color.white;artworkFallback.gameObject.SetActive(false);}
        }
        static void ShowImage(Image image,Sprite sprite,Color color)
        {
            if(!image)return;
            image.overrideSprite=null;image.sprite=sprite;image.color=color;image.preserveAspect=true;
            image.enabled=sprite;image.gameObject.SetActive(sprite);
        }
        void OnDestroy()=>Unbind();
    }
}
