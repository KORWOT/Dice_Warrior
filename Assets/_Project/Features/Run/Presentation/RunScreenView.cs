using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public interface IRunScreenView { FateDiceWidgets Widgets { get; } }

    public abstract class RunScreenView<TData> : BaseUI<TData>, IRunScreenView where TData : RunUIData
    {
        public RunScreenLayout layout;
        public FateDiceWidgets Widgets { get; private set; }
        protected virtual bool PreserveMapOnRebind => false;

        protected sealed override void OnBind(TData data)
        {
            if (data == null || data.context == null || data.hud == null)
                throw new InvalidOperationException(GetType().Name + " requires display context and HUD data.");
            if (!layout) throw new InvalidOperationException(GetType().Name + ".layout is missing.");
            layout.Validate();
            if (Widgets == null) Widgets = new FateDiceWidgets(layout, data.context);
            else Widgets.BindContext(data.context);
            Widgets.Clear(PreserveMapOnRebind);
            BindHUD(data);
            BindScreen(data);
        }

        protected virtual void BindHUD(TData data)
        {
            SetText(layout.header, data.hud.header);
            SetText(layout.stats, data.hud.stats);
            SetText(layout.situation, data.hud.situation);
            SetText(layout.fate, data.hud.fate);
            SetText(layout.notice, data.hud.notice);
            SetText(layout.gear, data.hud.gear);
            Widgets.ShowDice(data.hud.dice, data.hud.dieClicked);
            layout.footer.gameObject.SetActive(data.hud.menu != null);
            Widgets.Choice(layout.footer, data.hud.menu);
        }

        protected abstract void BindScreen(TData data);
        protected virtual void UnbindScreen() { }

        protected sealed override void OnUnbind()
        {
            try { UnbindScreen(); }
            finally
            {
                Widgets?.Clear();
                if (layout)
                {
                    ClearText(layout.header); ClearText(layout.stats); ClearText(layout.situation);
                    ClearText(layout.fate); ClearText(layout.notice); ClearText(layout.gear);
                }
            }
        }

        // Content visibility changes only: authored typography, size, anchors and layout stay untouched.
        protected static void SetText(Text field, string content)
        {
            if (!field) throw new InvalidOperationException("A screen's authored text reference is missing.");
            field.text = content ?? "";
            field.gameObject.SetActive(!string.IsNullOrEmpty(field.text));
        }
        protected static void ClearText(Text field)
        {
            if (field) { field.text = ""; field.gameObject.SetActive(false); }
        }

        protected static void SetArtwork(RectTransform root, Image image, Text fallback, VisualArtwork visual)
        {
            if (!root || !image || !fallback)
                throw new InvalidOperationException("A screen's authored artwork root/image/fallback reference is missing.");
            image.overrideSprite = null;
            image.sprite = visual == null ? null : (visual.artwork ? visual.artwork : visual.icon);
            image.preserveAspect = true;
            image.enabled = image.sprite != null;
            fallback.text = visual != null && !image.sprite ? visual.fallbackGlyph ?? "" : "";
            fallback.gameObject.SetActive(!string.IsNullOrEmpty(fallback.text));
            if (visual != null) { image.color = visual.tint; fallback.color = visual.tint; }
            root.gameObject.SetActive(visual != null);
        }
    }
}
