using System;
using System.Collections.Generic;

namespace FateDice
{
    public static class RunStateCopy
    {
        static T[] Values<T>(T[] source) => source == null ? null : (T[])source.Clone();
        static List<string> Ids(List<string> source) => source == null ? null : new List<string>(source);

        static List<T> Entries<T>(List<T> source, Func<T, T> copy)
        {
            if (source == null) return null;
            var result = new List<T>(source.Count);
            foreach (var entry in source) result.Add(copy(entry));
            return result;
        }

        public static NodeState Node(NodeState source) => source == null ? null : new NodeState
        {
            id = source.id, type = source.type, childIds = Ids(source.childIds), floor = source.floor, lane = source.lane
        };

        public static OfferedCard Card(OfferedCard source) => source == null ? null : new OfferedCard
        {
            id = source.id, type = source.type, grade = source.grade, contentId = source.contentId
        };

        public static ShopOffer Offer(ShopOffer source) => source == null ? null : new ShopOffer
        { productId = source.productId, price = source.price };

        public static RunRecord Record(RunRecord source) => source == null ? null : new RunRecord
        {
            runId = source.runId, seed = source.seed, won = source.won,
            events = source.events, combatTurns = source.combatTurns,
            selectedGrades = Values(source.selectedGrades), playedSeconds = source.playedSeconds
        };

        public static CoreRunState ToCore(RunStateData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var target = new CoreRunState { config = source.Rules?.DeepCopy() };
            CopyFields(source, target);
            return target;
        }

        // Copy only mutable run fields. Each adapter supplies its own rules/display configuration.
        public static void CopyFields(RunStateData source, RunStateData target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.schema = source.schema;
            target.profileId = source.profileId;
            target.runId = source.runId;
            target.rngState = source.rngState;
            target.initialSeed = source.initialSeed;
            target.phase = source.phase;
            target.explorationCap = source.explorationCap;
            target.hp = source.hp;
            target.shield = source.shield;
            target.level = source.level;
            target.xp = source.xp;
            target.gold = source.gold;
            target.baseMaxHp = source.baseMaxHp;
            target.basePower = source.basePower;
            target.baseGuard = source.baseGuard;
            target.eventsResolved = source.eventsResolved;
            target.combatTurns = source.combatTurns;
            target.sequence = source.sequence;
            target.actionIds = Ids(source.actionIds);
            target.equipmentIds = Values(source.equipmentIds);
            target.dieIds = Values(source.dieIds);
            target.rerollUnlocked = source.rerollUnlocked;
            target.rerollCharges = source.rerollCharges;
            target.nodes = Entries(source.nodes, Node);
            target.nodeHistory = Entries(source.nodeHistory, Node);
            target.availableNodeIds = Ids(source.availableNodeIds);
            target.nextNodeId = source.nextNodeId;
            target.selectedNode = Node(source.selectedNode);
            target.cards = Entries(source.cards, Card);
            target.dice = Values(source.dice);
            target.hand = source.hand;
            target.fatePower = source.fatePower;
            target.activeEventId = source.activeEventId;
            target.activeGrade = source.activeGrade;
            target.activeEnemyId = source.activeEnemyId;
            target.activeIntentId = source.activeIntentId;
            target.enemyHp = source.enemyHp;
            target.enemyShield = source.enemyShield;
            target.boss = source.boss;
            target.pendingReward = RulesCopy.Reward(source.pendingReward);
            target.pendingRewardId = source.pendingRewardId;
            target.pendingEquipmentId = source.pendingEquipmentId;
            target.pendingDieId = source.pendingDieId;
            target.rewardReturnPhase = source.rewardReturnPhase;
            target.shopOffers = Entries(source.shopOffers, Offer);
            target.purchasedIds = Ids(source.purchasedIds);
            target.processedRewardIds = Ids(source.processedRewardIds);
            target.resolvedEventIds = Ids(source.resolvedEventIds);
            target.lastResult = Record(source.lastResult);
            target.message = source.message;
            target.selectedGrades = Values(source.selectedGrades);
            target.playedSeconds = source.playedSeconds;
            target.won = source.won;
        }
    }
}
