using System;
using FateDice;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SymbolLoop
{
    public sealed class LoopScreenData : UIData
    {
        public string title, subtitle, stats, center, detail, history;
        public string[] tiles = new string[12], dice = new string[3], labels=Array.Empty<string>();
        public Action[] actions=Array.Empty<Action>();
        public int position=-1;
        public bool showBoard;
        public Run run;
        public Action onClose;
    }
    public sealed class SymbolLoopScreen : BaseUI<LoopScreenData>
    {
        public Text title, subtitle, stats, center, detail, history, focus;
        public Text[] tiles, dice;
        public Image[] tileFrames;
        public GameObject board;
        public RectTransform content;
        public CommonButtonView[] buttons;
        public SymbolLoopPlayback playback;
        public LoopGlyphGraphic[] tileIcons;
        void OnRectTransformDimensionsChange()
        {
            if(!content) return;
            var area=((RectTransform)transform).rect.size;
            float scale=Mathf.Min(area.x/720f,area.y/1280f);
            content.localScale=Vector3.one*Mathf.Max(.01f,scale);
        }
        protected override void OnBind(LoopScreenData data)
        {
            OnRectTransformDimensionsChange();
            title.text=data.title; subtitle.text=data.subtitle; stats.text=data.stats;
            center.text=data.center; detail.text=data.detail; history.text=data.history;
            focus.text=data.center; focus.gameObject.SetActive(!data.showBoard && (data.run==null || data.run.phase!=Phase.Combat));
            board.SetActive(data.showBoard);
            for(int i=0;i<tiles.Length;i++)
            {
                tiles[i].text=data.tiles[i] ?? "";
                tileFrames[i].color=i==data.position ? new Color(.17f,.69f,.62f) : new Color(.13f,.20f,.28f);
                if(data.run!=null)
                {
                    var glyphs=new[]{LoopGlyph.Star,LoopGlyph.Sword,LoopGlyph.Staff,LoopGlyph.Gem,LoopGlyph.Shop,LoopGlyph.Camp};
                    tileIcons[i].glyph=glyphs[(int)data.run.board[i]];
                    tileIcons[i].color=LoopGlyphGraphic.Tint(tileIcons[i].glyph); tileIcons[i].SetVerticesDirty();
                }
            }
            for(int i=0;i<dice.Length;i++) dice[i].text=data.dice[i] ?? "—";
            playback.Snap(data.run,data.showBoard);
            for(int i=0;i<buttons.Length;i++)
            {
                bool used=i<data.labels.Length;
                buttons[i].gameObject.SetActive(used);
                if(!used) { buttons[i].Unbind(); continue; }
                int index=i;
                buttons[i].Bind(i.ToString(),data.labels[i],null,"",new ButtonAppearance {
                    normal=new Color(.14f,.29f,.36f),selected=new Color(.22f,.48f,.52f),
                    pressed=new Color(.12f,.62f,.56f),disabled=new Color(.15f,.18f,.22f)
                },true,false,_=>data.actions[index]());
            }
        }
        protected override void OnUnbind() { Data?.onClose?.Invoke(); foreach(var button in buttons) button.Unbind(); }
    }
}
