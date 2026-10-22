using System;
using System.Collections.Generic;

namespace DiceWarrior.SharedWorld
{
    public enum AttributeStat { Strength, Dexterity, Constitution, Intelligence, Will, Luck }
    public enum DamageType { Physical, Fire, Ice, Lightning }
    public enum ModifierOperation { Flat, PercentAdd, FinalMultiply }
    public enum ModifierStack { Stackable, UniqueBySource, HighestWins }

    [Serializable] public sealed class AttributeValues
    {
        public int strength, dexterity, constitution, intelligence, will, luck;
        public int Get(AttributeStat stat)
        {
            switch (stat)
            {
                case AttributeStat.Strength: return strength;
                case AttributeStat.Dexterity: return dexterity;
                case AttributeStat.Constitution: return constitution;
                case AttributeStat.Intelligence: return intelligence;
                case AttributeStat.Will: return will;
                case AttributeStat.Luck: return luck;
                default: throw new ArgumentOutOfRangeException(nameof(stat));
            }
        }
        public void Set(AttributeStat stat, int value)
        {
            switch (stat)
            {
                case AttributeStat.Strength: strength = value; break;
                case AttributeStat.Dexterity: dexterity = value; break;
                case AttributeStat.Constitution: constitution = value; break;
                case AttributeStat.Intelligence: intelligence = value; break;
                case AttributeStat.Will: will = value; break;
                case AttributeStat.Luck: luck = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(stat));
            }
        }
    }
    [Serializable] public sealed class CompetitionV4Definition
    {
        public int baseHp = 30, hpPerConstitution = 6, attributePerLevel = 1, weaknessBasisPoints = 2000, resistanceBasisPoints = 1500;
        public TagDefinition[] tags = Array.Empty<TagDefinition>();
        public TagConflictGroup[] conflicts = Array.Empty<TagConflictGroup>();
        public V4AdventurerDefinition[] adventurers = Array.Empty<V4AdventurerDefinition>();
        public V4SkillDefinition[] skills = Array.Empty<V4SkillDefinition>();
        public V4GearDefinition[] gear = Array.Empty<V4GearDefinition>();
        public V4EnemyDefinition[] enemies = Array.Empty<V4EnemyDefinition>();
        public string[] treasureEncounterIds = Array.Empty<string>();
    }
    [Serializable] public sealed class V4AdventurerDefinition
    {
        public string id, passiveName, passiveDescription;
        public AttributeValues attributes = new AttributeValues();
        public string[] identityTags = Array.Empty<string>();
        public V4Binding[] bindings = Array.Empty<V4Binding>();
    }
    [Serializable] public sealed class V4SkillDefinition
    {
        public string skillId;
        public string[] identityTags = Array.Empty<string>(), grantedActionTags = Array.Empty<string>();
        public DamageType damageType = (DamageType)(-1);
        public AttributeValues damageScaling = new AttributeValues(), blockScaling = new AttributeValues();
    }
    [Serializable] public sealed class V4GearDefinition
    {
        public string gearId;
        public string[] identityTags = Array.Empty<string>(), grantedActionTags = Array.Empty<string>();
        public V4Binding[] bindings = Array.Empty<V4Binding>();
        public DamageType? conversion;
    }
    [Serializable] public sealed class V4Binding
    {
        public string id, target, stackGroup;
        public TagQuery[] anyOf = Array.Empty<TagQuery>();
        public ModifierOperation operation;
        public ModifierStack stack = ModifierStack.UniqueBySource;
        public int amount;
    }
    [Serializable] public sealed class V4Modifier
    {
        public string id, sourceKind, sourceId, target, stackGroup;
        public ModifierOperation operation;
        public ModifierStack stack;
        public int amount;
    }
    [Serializable] public sealed class V4EnemyDefinition
    {
        public string encounterId;
        public string[] identityTags = Array.Empty<string>();
        public DamageType attackType = (DamageType)(-1);
        public DamageType? weakness, resistance;
    }
    [Serializable] public sealed class V4ParticipantState
    {
        public int accountHp;
        public string enemyId;
        public V4CombatLog lastTurn;
    }
    [Serializable] public sealed class V4CombatLog
    {
        public List<V4SkillResult> skills = new List<V4SkillResult>();
        public DamageType retaliationType;
        public int retaliationBeforeBlock;
    }
    [Serializable] public sealed class V4SkillResult
    {
        public string skillId, status = "applied";
        public DamageType originalType, damageType;
        public int damageBeforeShield, damageApplied, block, affinityBasisPoints = 10000;
        public string[] tags = Array.Empty<string>();
        public V4Modifier[] modifiers = Array.Empty<V4Modifier>();
    }
    public sealed class V4StatBreakdown
    {
        public string name, passiveName, passiveDescription;
        public AttributeValues baseStats, attributes;
        public int level, levelBonus, baseHp, constitutionHp, accountHp, hp, maxHp;
        public V4Modifier[] modifiers;
    }
}
