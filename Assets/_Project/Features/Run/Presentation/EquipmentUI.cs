using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class EquipmentUI : RunScreenView<EquipmentUIData>
    {
        public RectTransform artworkRoot, choices;
        public Image artwork;
        public Text artworkFallback, details, current;
        protected override void BindScreen(EquipmentUIData data)
        {
            SetArtwork(artworkRoot, artwork, artworkFallback, data.revealed);
            SetText(details, data.details); SetText(current, data.current);
            Widgets.Choices(choices, data.choices);
        }
        protected override void UnbindScreen()
        {
            ClearText(details); ClearText(current);
            if (artworkRoot && artwork && artworkFallback) SetArtwork(artworkRoot, artwork, artworkFallback, null);
        }
    }
}
