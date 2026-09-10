using System;
using System.Collections.Generic;
namespace FateDice
{
    [Serializable] public sealed class NodeState
    {
        public string id;
        public NodeType type;
        public List<string> childIds = new List<string>();
        // Version1 map positions are immutable for the run; zero fields preserve legacy saves.
        public int floor;
        public int lane;
    }
    [Serializable] public sealed class OfferedCard
    {
        public string id;
        public NodeType type;
        public Grade grade;
        public string contentId;
    }
    [Serializable] public sealed class RunRecord
    {
        public string runId;
        public uint seed;
        public bool won;
        public int events, combatTurns;
        public int[] selectedGrades = new int[5];
        public double playedSeconds;
    }
    [Serializable] public abstract class RunStateData
    {
        public const int CurrentSchema = 1;
        public int schema = CurrentSchema;
        public string profileId = "local-test";
        public string runId;
        public abstract RunRulesCatalog Rules { get; }
        public uint rngState;
        public uint initialSeed;
        public RunPhase phase;
        public Grade explorationCap;
        public int hp, shield, level, xp, gold;
        public int baseMaxHp, basePower, baseGuard;
        public int eventsResolved, combatTurns, sequence;
        public List<string> actionIds = new List<string>();
        public string[] equipmentIds = new string[3];
        public string[] dieIds = new string[6];
        public bool rerollUnlocked;
        public int rerollCharges;
        public List<NodeState> nodes = new List<NodeState>();
        public List<NodeState> nodeHistory = new List<NodeState>();
        public List<string> availableNodeIds = new List<string>();
        public int nextNodeId;
        public NodeState selectedNode;
        public List<OfferedCard> cards = new List<OfferedCard>();
        public int[] dice;
        public HandKind hand;
        public int fatePower;
        public string activeEventId;
        public Grade activeGrade;
        public string activeEnemyId, activeIntentId;
        public int enemyHp, enemyShield;
        public bool boss;
        public RewardDefinition pendingReward;
        public string pendingRewardId;
        public string pendingEquipmentId, pendingDieId;
        public RunPhase rewardReturnPhase;
        public List<ShopOffer> shopOffers = new List<ShopOffer>();
        public List<string> purchasedIds = new List<string>();
        public List<string> processedRewardIds = new List<string>();
        public List<string> resolvedEventIds = new List<string>();
        public RunRecord lastResult;
        public string message;
        public int[] selectedGrades = new int[5];
        public double playedSeconds;
        public bool won;
    }
}
