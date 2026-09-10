using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    // The sole conversion from committed graph/history to public map information.
    public static class CampaignMapProjection
    {
        public static void Apply(ExplorationUIData target, RunState state)
        {
            if (target == null || state == null) throw new ArgumentNullException();
            var world = state.config.world;
            var stats = GrowthRules.Stats(state);
            target.mapFloors = world.eventsToBoss;
            target.currentNodeId = HasSelectedNode(state) ? state.selectedNode.id : state.resolvedEventIds.LastOrDefault();
            target.mapProgressLabel = "탐험  " + state.eventsResolved + " / " + world.eventsToBoss +
                "   ·   대운명까지 " + Math.Max(0, world.eventsToBoss - state.eventsResolved) + "층";
            target.mapGoldLabel = "골드  " + state.gold.ToString("N0");
            target.mapHealthLabel = KoreanText.Content(state.config.growth.characterName) + "   체력 " + state.hp + " / " + stats.maxHp;
            target.mapWardLabel = "수호 " + state.shield + "   ·   레벨 " + state.level;
            target.campaignNodes = Build(state);
            if (target.campaignNodes != null)
            {
                // Legacy fields must not carry the unmasked graph alongside the public projection.
                target.nodes = Array.Empty<NodeState>();
                target.completedNodes = Array.Empty<NodeState>();
            }
        }

        public static CampaignNodeUIData[] Build(RunState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.config.world.mapGenerationVersion == 0) return null;
            var active = new HashSet<string>(state.nodes.Select(node => node.id));
            var available = new HashSet<string>(state.availableNodeIds);
            var completed = new HashSet<string>(state.resolvedEventIds);
            int currentFloor = HasSelectedNode(state) ? state.selectedNode.floor : state.eventsResolved;
            int revealedThrough = currentFloor + state.config.world.previewDepth;
            return state.nodes.Concat(state.nodeHistory ?? Enumerable.Empty<NodeState>())
                .OrderBy(node => node.floor).ThenBy(node => node.lane).ThenBy(node => node.id, StringComparer.Ordinal)
                .Select(node =>
                {
                    bool revealed = node.type == NodeType.Boss || node.floor <= revealedThrough;
                    return new CampaignNodeUIData
                    {
                        id = node.id, floor = node.floor, lane = node.lane,
                        childIds = node.childIds.ToArray(), revealed = revealed,
                        type = revealed ? (NodeType?)node.type : null,
                        available = available.Contains(node.id), completed = completed.Contains(node.id),
                        unreachable = !active.Contains(node.id) && !completed.Contains(node.id)
                    };
                }).ToArray();
        }

        // JsonUtility may restore an absent class field as an empty instance in a schema-1 save.
        static bool HasSelectedNode(RunState state) => !string.IsNullOrEmpty(state.selectedNode?.id);
    }
}
