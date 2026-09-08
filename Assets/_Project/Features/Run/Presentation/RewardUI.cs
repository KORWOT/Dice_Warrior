using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class RewardUI : RunScreenView<RewardUIData>
    {
        public RectTransform artworkRoot, choices;
        public Image artwork;
        public Text artworkFallback, description, instructions;
        protected override void BindScreen(RewardUIData data)
        {
            SetArtwork(artworkRoot, artwork, artworkFallback, data.revealed);
            SetText(description, data.description); SetText(instructions, data.instructions);
            Widgets.Choices(choices, new[] { data.claim });
        }
        protected override void UnbindScreen()
        {
            ClearText(description); ClearText(instructions);
            if (artworkRoot && artwork && artworkFallback) SetArtwork(artworkRoot, artwork, artworkFallback, null);
        }
    }
}
