using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class EncounterUI : RunScreenView<EncounterUIData>
    {
        public RectTransform artworkRoot, choices;
        public Image artwork;
        public Text artworkFallback, description, outcome;
        protected override void BindScreen(EncounterUIData data)
        {
            SetArtwork(artworkRoot, artwork, artworkFallback, data.revealed);
            SetText(description, data.description); SetText(outcome, data.outcome);
            Widgets.Choices(choices, data.choices);
        }
        protected override void UnbindScreen()
        {
            ClearText(description); ClearText(outcome);
            if (artworkRoot && artwork && artworkFallback) SetArtwork(artworkRoot, artwork, artworkFallback, null);
        }
    }
}
