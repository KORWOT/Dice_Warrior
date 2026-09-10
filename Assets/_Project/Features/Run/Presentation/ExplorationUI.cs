using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class ExplorationUI : RunScreenView<ExplorationUIData>
    {
        public Text instructions;
        public RectTransform mapContainer, rollChoices, fateChoices;
        public CampaignMapView campaignMap;
        public int layoutVersion;
        public Text progressLabel, goldLabel, healthLabel, wardLabel, selectedDetails, moveLabel, legendLabel;
        public Button moveButton, legendToggle;
        public RectTransform legendBody;
        public string SelectedNodeId { get; private set; }
        UnityAction legendListener;
        int bindingVersion;
        bool submitting, legendExpanded;
        protected override bool PreserveMapOnRebind => true;

        protected override void BindHUD(ExplorationUIData data)
        {
            SetText(layout.header, data.hud.header);
            SetText(layout.stats, data.hud.stats);
            SetText(layout.situation, data.hud.situation);
            SetText(layout.notice, data.hud.notice);
            SetText(layout.fate, data.hud.fate);
            SetText(layout.gear, null);
            layout.stats.gameObject.SetActive(false);
            layout.situation.gameObject.SetActive(false);
            layout.diceRow.gameObject.SetActive(false);
            if (progressLabel) SetText(progressLabel, data.mapProgressLabel ?? data.hud.situation);
            if (goldLabel) SetText(goldLabel, data.mapGoldLabel ?? "골드 —");
            if (healthLabel) SetText(healthLabel, data.mapHealthLabel ?? "체력 —");
            if (wardLabel) SetText(wardLabel, data.mapWardLabel ?? "수호 —");
            layout.footer.gameObject.SetActive(data.hud.menu != null || data.cap != null);
            Widgets.Choice(layout.footer, data.hud.menu);
        }

        protected override void BindScreen(ExplorationUIData data)
        {
            if (!mapContainer || !campaignMap || campaignMap.transform != mapContainer || !rollChoices || !fateChoices)
                throw new InvalidOperationException("ExplorationUI requires its authored map and choice containers.");
            if (!moveButton || !moveLabel || !selectedDetails || !legendToggle || !legendBody || !legendLabel)
                throw new InvalidOperationException("ExplorationUI requires its authored travel controls and collapsible legend.");
            Detach(); bindingVersion++; submitting = false;
            SelectedNodeId = null;
            mapContainer.gameObject.SetActive(true);
            SetText(instructions, data.instructions);
            BindMap(data);
            Widgets.Choices(rollChoices, new[] { data.roll });
            fateChoices.gameObject.SetActive(false);
            Widgets.Choice(layout.footer, data.cap);
            int version = bindingVersion;
            legendListener = () => { if (version != bindingVersion || !isActiveAndEnabled) return; legendExpanded = !legendExpanded; RefreshLegend(); };
            legendToggle.onClick.AddListener(legendListener);
            Widgets.Buttons["map-legend"] = legendToggle;
            RefreshLegend(); RefreshSelection(data);
        }

        void BindMap(ExplorationUIData data)
        {
            int version = bindingVersion;
            // Keep the original Button beneath ScrollRect. Its input module suppresses clicks after a drag.
            Action<string> travel = id => Travel(data, version, id);
            if (data.campaignNodes != null)
                Widgets.ShowCampaignMap(mapContainer, data.campaignNodes, data.currentNodeId, SelectedNodeId, travel);
            else
            {
                Widgets.ShowMap(mapContainer, data.nodes ?? Array.Empty<NodeState>(), data.available ?? Array.Empty<string>(),
                    data.selected, travel, data.completedNodes);
                foreach (var entry in campaignMap.Nodes)
                    if (entry.Value.mapGraphic) entry.Value.mapGraphic.SetSelected(entry.Key == SelectedNodeId);
            }
        }

        static bool CanTravel(ExplorationUIData data, string id)
        {
            if (data == null || string.IsNullOrEmpty(id) || data.chooseNode == null || !string.IsNullOrEmpty(data.selected)) return false;
            return data.campaignNodes != null ? data.campaignNodes.Any(node => node.id == id && node.available && !node.completed && !node.unreachable) :
                (data.available ?? Array.Empty<string>()).Contains(id);
        }

        void Travel(ExplorationUIData data, int version, string id)
        {
            if (version != bindingVersion || !IsOpen || !isActiveAndEnabled || submitting || !group ||
                !group.interactable || !group.blocksRaycasts || !CanTravel(data, id)) return;
            string previousSelection = SelectedNodeId;
            submitting = true; SelectedNodeId = id;
            RefreshSelection(data);
            try { data.chooseNode(id); }
            catch
            {
                // Success remains latched until the next binding. An old callback cannot edit a replacement screen.
                if (version == bindingVersion)
                {
                    submitting = false; SelectedNodeId = previousSelection; RefreshSelection(data);
                }
                throw;
            }
        }

        void RefreshSelection(ExplorationUIData data)
        {
            // Keep serialized references for existing originals; travel now belongs to the node tap.
            moveButton.interactable = false;
            moveButton.gameObject.SetActive(false);
            string id = SelectedNodeId ?? data.selected;
            NodeType? type = null;
            int floor = 0;
            if (data.campaignNodes != null)
            {
                var node = data.campaignNodes.FirstOrDefault(value => value.id == id);
                if (node != null) { type = node.revealed ? node.type : null; floor = node.floor; }
            }
            else type = data.nodes?.FirstOrDefault(node => node.id == id)?.type;
            SetText(selectedDetails, string.IsNullOrEmpty(id) ? "빛나는 경로를 누르면 바로 이동합니다.\n가까운 두 층의 유형을 확인할 수 있습니다." :
                (floor > 0 ? floor + "층 · " : "") + (type.HasValue ? KoreanText.Node(type.Value) : "미발견") + "\n" + Description(type, data.campaignNodes != null));
        }

        static string Description(NodeType? type, bool procedural)
        {
            if (!type.HasValue) return "다가가면 경로의 유형이 드러납니다.";
            if (!procedural && type.Value != NodeType.Boss) return "이 길의 성향과 운명 카드를 살펴보고 다음 사건을 정하세요.";
            switch (type.Value)
            {
                case NodeType.Combat: return "전투 운명이 한 장 포함됩니다. 카드를 보고 사건을 정하세요.";
                case NodeType.Event: return "사건 운명이 한 장 포함됩니다. 뜻밖의 만남을 살펴보세요.";
                case NodeType.Treasure: return "보물 운명이 한 장 포함됩니다. 발견의 기회를 살펴보세요.";
                case NodeType.Shop: return "상점 운명이 한 장 포함됩니다. 거래의 기회를 살펴보세요.";
                case NodeType.Rest: return "휴식 운명이 한 장 포함됩니다. 쉼의 기회를 살펴보세요.";
                case NodeType.Boss: return "여정의 마지막 대운명이 기다립니다.";
                default: return "운명 카드를 선택하면 사건이 드러납니다.";
            }
        }

        void RefreshLegend()
        {
            legendBody.gameObject.SetActive(legendExpanded);
            legendLabel.text = legendExpanded ? "지도 범례  ▴" : "지도 범례  ▾";
        }
        void Detach()
        {
            if (legendToggle && legendListener != null) legendToggle.onClick.RemoveListener(legendListener);
            legendListener = null;
        }
        protected override void UnbindScreen()
        {
            bindingVersion++; submitting = false; SelectedNodeId = null; Detach(); ClearText(instructions);
            ClearText(selectedDetails);
            if (moveButton) moveButton.interactable = false;
        }
    }
}
