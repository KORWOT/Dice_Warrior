using System;

namespace FateDice
{
    // Explicit field copies preserve null, empty collections and stable IDs without serialization.
    public static class RulesCopy
    {
        static T[] Values<T>(T[] source) => source == null ? null : (T[])source.Clone();

        static T[] Entries<T>(T[] source, Func<T, T> copy)
        {
            if (source == null) return null;
            var result = new T[source.Length];
            for (int i = 0; i < source.Length; i++) result[i] = copy(source[i]);
            return result;
        }

        public static HandDefinition Hand(HandDefinition source) => source == null ? null : new HandDefinition
        {
            kind = source.kind, label = source.label, priority = source.priority, fatePower = source.fatePower
        };

        public static DieDefinition Die(DieDefinition source) => source == null ? null : new DieDefinition
        {
            id = source.id, label = source.label, values = Values(source.values), weights = Values(source.weights)
        };

        public static GradeRow GradeRow(GradeRow source) => source == null ? null : new GradeRow
        {
            minimumPower = source.minimumPower, weights = Values(source.weights)
        };

        public static DiceSettings Dice(DiceSettings source) => source == null ? null : new DiceSettings
        {
            basicDieId = source.basicDieId, replacementDieId = source.replacementDieId,
            dice = Entries(source.dice, Die), hands = Entries(source.hands, Hand),
            minimumFatePower = source.minimumFatePower, maximumFatePower = source.maximumFatePower
        };

        public static FateSettings Fate(FateSettings source) => source == null ? null : new FateSettings
        {
            exploration = Entries(source.exploration, GradeRow), combat = Entries(source.combat, GradeRow),
            gradeMultipliers = Values(source.gradeMultipliers), nodeWeights = Values(source.nodeWeights),
            selectedTypeBias = source.selectedTypeBias, capRewardMultipliers = Values(source.capRewardMultipliers)
        };

        public static ActionEffectDefinition Effect(ActionEffectDefinition source) => source == null ? null : new ActionEffectDefinition
        { kind = source.kind, coefficient = source.coefficient };

        public static ActionDefinition Action(ActionDefinition source) => source == null ? null : new ActionDefinition
        {
            id = source.id, label = source.label, grade = source.grade,
            damageCoefficient = source.damageCoefficient, blockCoefficient = source.blockCoefficient,
            tags = Values(source.tags), effects = Entries(source.effects, Effect)
        };

        public static EnemyIntent Intent(EnemyIntent source) => source == null ? null : new EnemyIntent
        {
            id = source.id, label = source.label, kind = source.kind,
            weight = source.weight, coefficient = source.coefficient
        };

        public static EnemyDefinition Enemy(EnemyDefinition source) => source == null ? null : new EnemyDefinition
        {
            id = source.id, label = source.label, maxHp = source.maxHp,
            power = source.power, guard = source.guard, intents = Entries(source.intents, Intent)
        };

        public static CombatSettings Combat(CombatSettings source) => source == null ? null : new CombatSettings
        {
            actions = Entries(source.actions, Action), startingActionIds = Values(source.startingActionIds),
            trialActionIds = Values(source.trialActionIds), enemies = Entries(source.enemies, Enemy),
            bossEnemyId = source.bossEnemyId, minimumEffect = source.minimumEffect
        };

        public static TagDefinition Tag(TagDefinition source) => source == null ? null : new TagDefinition
        {
            id = source.id, label = source.label
        };

        public static TagModifier Modifier(TagModifier source) => source == null ? null : new TagModifier
        {
            match = source.match, source = source.source, value = source.value,
            requiredTags = Values(source.requiredTags), bonus = source.bonus
        };

        public static EquipmentDefinition Equipment(EquipmentDefinition source) => source == null ? null : new EquipmentDefinition
        {
            id = source.id, label = source.label, slot = source.slot,
            maxHp = source.maxHp, power = source.power, guard = source.guard,
            tags = Values(source.tags), modifiers = Entries(source.modifiers, Modifier)
        };

        public static LevelStep Level(LevelStep source) => source == null ? null : new LevelStep
        {
            xpRequired = source.xpRequired, maxHp = source.maxHp, power = source.power, guard = source.guard
        };

        public static RewardDefinition Reward(RewardDefinition source) => source == null ? null : new RewardDefinition
        {
            gold = source.gold, xp = source.xp, health = source.health, equipmentId = source.equipmentId,
            rerollCharges = source.rerollCharges, dieId = source.dieId, addActionId = source.addActionId
        };

        public static EventDefinition Event(EventDefinition source) => source == null ? null : new EventDefinition
        {
            id = source.id, label = source.label, description = source.description,
            type = source.type, grade = source.grade, enemyId = source.enemyId, reward = Reward(source.reward)
        };

        public static ShopProduct Product(ShopProduct source) => source == null ? null : new ShopProduct
        {
            id = source.id, label = source.label, price = source.price, reward = Reward(source.reward)
        };

        public static GrowthSettings Growth(GrowthSettings source) => source == null ? null : new GrowthSettings
        {
            characterId = source.characterId, characterName = source.characterName,
            startingMaxHp = source.startingMaxHp, startingPower = source.startingPower,
            startingGuard = source.startingGuard, startingGold = source.startingGold,
            levels = Entries(source.levels, Level), tags = Entries(source.tags, Tag),
            equipment = Entries(source.equipment, Equipment), rerollCost = source.rerollCost
        };

        public static WorldSettings World(WorldSettings source) => source == null ? null : new WorldSettings
        {
            eventsToBoss = source.eventsToBoss, offeredCards = source.offeredCards,
            previewDepth = source.previewDepth, branchCount = source.branchCount,
            events = Entries(source.events, Event), bossReward = Reward(source.bossReward),
            shop = Entries(source.shop, Product), shopPriceMultipliers = Values(source.shopPriceMultipliers), restTraining = Reward(source.restTraining)
        };
    }
}
