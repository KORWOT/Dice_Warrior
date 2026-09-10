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
        public Text typeLabel, gradeLabel, description;
        public Image typeGlow, selectionBorder;
        public MapNodeGraphic typeSymbol;
        [HideInInspector] public int choicePresentationVersion;
        public bool IsSelected { get; private set; }
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
            if(typeSymbol)
            {
                typeSymbol.gameObject.SetActive(!picture);
                typeSymbol.Bind(type,TypeColor(type),true,false,false,false,false,true);
                artworkFallback.gameObject.SetActive(false);
            }
            ShowImage(gradeBadge,gradeVisual.badge,gradeColor);
            if(typeLabel) { typeLabel.text=KoreanText.Node(type); typeLabel.color=TypeColor(type); }
            if(gradeLabel) { gradeLabel.text=KoreanText.Grade(grade); gradeLabel.color=gradeColor; }
            if(description) description.text=PublicDescription(type);
            if(typeGlow) { var color=TypeColor(type); color.a=.15f; typeGlow.color=color; }
            if(typeLabel&&gradeLabel) frame.label.gameObject.SetActive(false);
            SetSelected(false);
        }
        public void SetSelected(bool selected)
        {
            IsSelected=selected;
            if(selectionBorder) selectionBorder.gameObject.SetActive(selected);
        }
        public void Unbind()
        {
            OfferedId=null;
            SetSelected(false);
            if(frame)frame.Unbind();
            ShowImage(artwork,null,Color.white);ShowImage(gradeBadge,null,Color.white);
            if(artworkFallback){artworkFallback.text="";artworkFallback.color=Color.white;artworkFallback.gameObject.SetActive(false);}
            if(typeLabel)typeLabel.text="";
            if(gradeLabel)gradeLabel.text="";
            if(description)description.text="";
            if(typeSymbol)typeSymbol.gameObject.SetActive(false);
        }
        static string PublicDescription(NodeType type)
        {
            switch(type)
            {
                case NodeType.Combat:return "적과 맞서는\n전투의 운명";
                case NodeType.Event:return "뜻밖의 만남과\n선택의 운명";
                case NodeType.Treasure:return "발견과 획득의\n운명";
                case NodeType.Shop:return "물품을 살펴보는\n거래의 운명";
                case NodeType.Rest:return "잠시 쉬어 가는\n휴식의 운명";
                case NodeType.Boss:return "마지막 적과 맞서는\n결전의 운명";
                default:return "운명을 선택하세요";
            }
        }
        static Color TypeColor(NodeType type)
        {
            switch(type)
            {
                case NodeType.Combat:return new Color(1,.43f,.35f);
                case NodeType.Event:return new Color(.77f,.57f,1);
                case NodeType.Treasure:return new Color(1,.82f,.34f);
                case NodeType.Shop:return new Color(.37f,.81f,1);
                case NodeType.Rest:return new Color(.42f,.92f,.64f);
                default:return new Color(1,.38f,.57f);
            }
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
