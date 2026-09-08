using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FateDice
{
    public enum ButtonPurpose { Primary, Secondary, Navigation, Purchase, Die, Trial, Grade }
    [Serializable] public sealed class ButtonAppearance
    {
        public ButtonPurpose purpose;
        public Sprite icon;
        public Color normal, selected, pressed, disabled;
    }
    public sealed class CommonButtonView : MonoBehaviour
    {
        public Button button;
        public Image background, border, icon;
        public Text label, iconFallback;
        public CanvasGroup group;
        public string BoundId { get; private set; }
        UnityAction ownedListener;
        Action<string> callback;
        bool captured;
        Color labelColor, glyphColor, iconColor, borderColor, backgroundColor;
        ColorBlock authoredColors;
        Sprite authoredBorder;

        public void Bind(string id,string text,Sprite icon,string fallbackGlyph,ButtonAppearance style,bool interactable,bool selected,Action<string> clicked)
        {
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("A button binding ID is required.",nameof(id));
            if(style==null)throw new ArgumentNullException(nameof(style));
            if(!button||!background||!border||!this.icon||!label||!iconFallback||!group)
                throw new InvalidOperationException("CommonButtonView requires its local Button, images, text, and CanvasGroup references.");
            CaptureDefaults();Unbind();
            enabled=true;gameObject.SetActive(true);
            BoundId=id;callback=clicked;
            SetBorder(null,borderColor);
            label.text=text??"";label.gameObject.SetActive(true);
            var picture=icon?icon:style.icon;
            this.icon.overrideSprite=null;this.icon.sprite=picture;this.icon.preserveAspect=true;
            this.icon.enabled=picture;this.icon.gameObject.SetActive(picture);
            iconFallback.text=picture?"":fallbackGlyph??"";
            iconFallback.gameObject.SetActive(!picture&&!string.IsNullOrEmpty(iconFallback.text));
            var colors=authoredColors;
            colors.normalColor=selected?style.selected:style.normal;
            colors.highlightedColor=style.selected;colors.selectedColor=style.selected;
            colors.pressedColor=style.pressed;colors.disabledColor=style.disabled;
            button.targetGraphic=background;button.transition=Selectable.Transition.ColorTint;button.colors=colors;
            group.alpha=1;group.interactable=interactable;group.blocksRaycasts=interactable;
            button.interactable=interactable;button.enabled=true;
            ownedListener=()=>{
                if(!isActiveAndEnabled||!button||!button.isActiveAndEnabled||!button.IsInteractable()||
                    !group||!group.interactable||!group.blocksRaycasts||callback==null)return;
                callback(BoundId);
            };
            button.onClick.AddListener(ownedListener);
        }
        public void Unbind()
        {
            CaptureDefaults();Detach();
            BoundId=null;callback=null;
            if(button)
            {
                button.interactable=false;
                button.enabled=false; // Selectable.OnDisable clears any old pressed, hover, or keyboard-selection state.
                if(captured)button.colors=authoredColors;
            }
            if(group){group.alpha=1;group.interactable=false;group.blocksRaycasts=false;}
            if(label){label.text="";if(captured)label.color=labelColor;}
            if(iconFallback){iconFallback.text="";if(captured)iconFallback.color=glyphColor;iconFallback.gameObject.SetActive(false);}
            ClearImage(icon,captured?iconColor:Color.white);
            ClearImage(border,captured?borderColor:Color.white);
            if(background&&captured)background.color=backgroundColor;
        }
        public void SetBorder(Sprite sprite,Color color)
        {
            if(!border)throw new InvalidOperationException("CommonButtonView requires its border Image reference.");
            CaptureDefaults();var picture=sprite?sprite:authoredBorder;
            border.overrideSprite=null;border.sprite=picture;border.color=color;border.preserveAspect=true;
            border.enabled=picture;border.gameObject.SetActive(picture);
        }
        void CaptureDefaults()
        {
            if(captured||!button||!background||!border||!icon||!label||!iconFallback)return;
            authoredColors=button.colors;backgroundColor=background.color;authoredBorder=border.sprite;
            labelColor=label.color;glyphColor=iconFallback.color;iconColor=icon.color;borderColor=border.color;captured=true;
        }
        static void ClearImage(Image image,Color color)
        {
            if(!image)return;
            image.overrideSprite=null;image.sprite=null;image.color=color;image.preserveAspect=true;
            image.enabled=false;image.gameObject.SetActive(false);
        }
        void Detach()
        {
            if(button&&ownedListener!=null)button.onClick.RemoveListener(ownedListener);
            ownedListener=null;
        }
        void OnDestroy(){Detach();callback=null;BoundId=null;}
    }
}
