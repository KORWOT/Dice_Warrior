using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class ActionCardView : MonoBehaviour
    {
        public CommonButtonView frame;
        public Image artwork, gradeBadge;
        public Text artworkFallback, gradeLabel, effectLabel, tagsLabel;
        public string OfferedId { get; private set; }
        public string OriginalId { get; private set; }
        public void Bind(string offeredId,string originalId,string name,Grade grade,string effects,string tags,VisualArtwork visual,GradeVisualEntry gradeVisual,Color gradeColor,ButtonAppearance style,Action<string> selected)
        {
            if(string.IsNullOrWhiteSpace(originalId))throw new ArgumentException("An original action ID is required.",nameof(originalId));
            if(visual==null)throw new ArgumentNullException(nameof(visual));
            if(gradeVisual==null)throw new ArgumentNullException(nameof(gradeVisual));
            if(!frame||!artwork||!gradeBadge||!artworkFallback||!gradeLabel||!effectLabel||!tagsLabel)
                throw new InvalidOperationException("ActionCardView requires its common frame and local artwork, grade, effect, and tag references.");
            Unbind();enabled=true;gameObject.SetActive(true);
            frame.Bind(offeredId,name,visual.icon,visual.fallbackGlyph,style,true,false,selected);
            frame.icon.color=visual.tint;frame.iconFallback.color=visual.tint;
            frame.SetBorder(gradeVisual.border,gradeColor);
            OfferedId=offeredId;OriginalId=originalId;
            gradeLabel.text=KoreanText.Grade(grade);gradeLabel.color=gradeColor;gradeLabel.gameObject.SetActive(true);
            effectLabel.text=effects??"";effectLabel.gameObject.SetActive(true);
            tagsLabel.text=tags??"";tagsLabel.gameObject.SetActive(true);
            var picture=visual.artwork?visual.artwork:visual.icon;
            ShowImage(artwork,picture,visual.tint);
            artworkFallback.text=picture?"":visual.fallbackGlyph??"";
            artworkFallback.color=visual.tint;
            artworkFallback.gameObject.SetActive(!picture&&!string.IsNullOrEmpty(artworkFallback.text));
            ShowImage(gradeBadge,gradeVisual.badge,gradeColor);
        }
        public void Unbind()
        {
            OfferedId=null;OriginalId=null;
            if(frame)frame.Unbind();
            ShowImage(artwork,null,Color.white);ShowImage(gradeBadge,null,Color.white);
            if(artworkFallback){artworkFallback.text="";artworkFallback.color=Color.white;artworkFallback.gameObject.SetActive(false);}
            if(gradeLabel){gradeLabel.text="";gradeLabel.color=Color.white;}
            if(effectLabel)effectLabel.text="";
            if(tagsLabel)tagsLabel.text="";
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
