using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class ExplorationUI : RunScreenView<ExplorationUIData>
    {
        public Text instructions;
        public RectTransform mapContainer, rollChoices, fateChoices;
        public CampaignMapView campaignMap;
        public int layoutVersion;
        protected override bool PreserveMapOnRebind => true;

        protected override void BindHUD(ExplorationUIData data)
        {
            bool cards = data.fates != null && data.fates.Length > 0;
            SetText(layout.header, data.hud.header);
            SetText(layout.stats, data.hud.stats);
            SetText(layout.situation, data.hud.situation);
            SetText(layout.notice, data.hud.notice);
            SetText(layout.fate, cards ? data.hud.fate : null);
            SetText(layout.gear, null);
            layout.diceRow.gameObject.SetActive(cards);
            if (cards) Widgets.ShowDice(data.hud.dice, data.hud.dieClicked);
            layout.footer.gameObject.SetActive(data.hud.menu != null || data.cap != null);
            Widgets.Choice(layout.footer, data.hud.menu);
        }

        protected override void BindScreen(ExplorationUIData data)
        {
            if (!mapContainer || !campaignMap || campaignMap.transform != mapContainer || !rollChoices || !fateChoices)
                throw new InvalidOperationException("ExplorationUI requires its authored map and choice containers.");
            bool cards = data.fates != null && data.fates.Length > 0;
            mapContainer.gameObject.SetActive(!cards);
            SetText(instructions, !string.IsNullOrEmpty(data.instructions) ? data.instructions : cards
                ? "운명 카드 한 장을 선택하세요.\n유형과 등급을 비교하고 다음 사건을 정합니다."
                : "위로 이어진 길 중 하나를 선택하세요.\n밝은 테두리는 지금 이동할 수 있는 곳입니다.");
            Widgets.ShowMap(mapContainer, data.nodes ?? Array.Empty<NodeState>(),
                data.available ?? Array.Empty<string>(), data.selected, data.chooseNode, data.completedNodes);
            Widgets.Choices(rollChoices, new[] { data.roll });
            fateChoices.gameObject.SetActive(cards);
            if (data.fates != null)
                foreach (var offer in data.fates) Widgets.FateCard(fateChoices, offer);
            Widgets.Choice(layout.footer, data.cap);
            // Keep one authored viewport. The map owns focusing the current position after history grows.
            if (cards) layout.scroll.verticalNormalizedPosition = 1;
            if (cards)
            {
                layout.scroll.horizontal = false;
                var size = layout.body.sizeDelta; size.x = 0; layout.body.sizeDelta = size;
            }
        }
        protected override void UnbindScreen() => ClearText(instructions);
    }
}
