using System;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;

namespace DiceWarrior.SharedWorld
{
    public enum CompetitionPhase { Map, Encounter, Combat, LapReward, Complete }
    public enum EncounterKind { Combat, Heal, Study, Risk, Gear, Die, Leave }

    [Serializable] public sealed class CompetitionDefinition
    {
        public string rulesVersion, aiVersion, scoreVersion;
        public int maxRounds = 30, maxCombatTurns = 64, lapsToFinish = 3, finalHonor = 5;
        public Rules combat = new Rules();
        public WorldDefinition world;
        public EncounterDefinition[] encounters;
        public AdventurerDefinition[] adventurers;
        public AiPolicyDefinition[] policies;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public CompetitionGrowthDefinition growth;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public CompetitionV4Definition v4;
    }
    [Serializable] public sealed class WorldDefinition
    {
        public string id, version, name;
        public PlaceDefinition[] places;
    }
    [Serializable] public sealed class PlaceDefinition
    {
        public string id, name, description;
        public string[] encounterIds;
    }
    [Serializable] public sealed class EncounterDefinition
    {
        public string id, label, description, rewardId;
        public EncounterKind kind;
        public int minLaps, maxLaps = 3, minHp, goldCost, hpCost;
        public int gold, xp, heal, honor, enemyHpBonus, enemyAttackBonus, weight = 1;
        public string requiredGearId;
    }
    [Serializable] public sealed class AdventurerDefinition
    {
        public string id, name;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public AdventurerStatsDefinition stats;
    }
    [Serializable] public sealed class AdventurerStatsDefinition
    {
        public int hp, strength, guard, arcane;
        public string passiveName, passiveDescription;
        public string[] damageTags;
        public int damageBonus, blockBonus;
    }
    [Serializable] public sealed class CompetitionGrowthDefinition
    {
        public int hpPerLevel, statPerLevel;
        public SkillScalingDefinition[] scaling;
    }
    [Serializable] public sealed class SkillScalingDefinition
    {
        public string skillId;
        public int strength, arcane, divisor;
    }
    [Serializable] public sealed class ParticipantGrowthState
    {
        public int accountHp;
    }
    [Serializable] public sealed class AiPolicyDefinition
    {
        public string id, sponsorName;
        public int combat, heal, study, risk, gear, die, leave, lowHpPercent = 40;
    }
    [Serializable] public sealed class EncounterChoice
    {
        public string id, label, description, rewardId, unavailableReason;
        public EncounterKind kind;
        public int goldCost, hpCost, gold, xp, heal, honor, enemyHp, enemyAttack;
        public bool available;
    }
    [Serializable] public sealed class EncounterOffer
    {
        public string id, placeId;
        public int visit, stage;
        public EncounterChoice[] choices;
        public string selectedChoiceId;
    }
    [Serializable] public sealed class ScoreRecord
    {
        public string id, reason;
        public int amount;
    }
    [Serializable] public sealed class ParticipantState
    {
        public string id, adventurerId, name, sponsorId, aiPolicyId;
        public bool isHuman, finished, eliminated, trialSucceeded;
        public int position, completedLaps, lastTravel, hp, maxHp, power, guard, gold, xp, level = 1;
        public int turn, encounterTurns, honor, rank;
        public uint travelRng, combatRng, eventRng, aiRng;
        public CompetitionPhase phase;
        public List<DieSlot> dice = new List<DieSlot>();
        public string[] equipment = new string[3];
        public int[] visits;
        public List<string> claimed = new List<string>();
        public List<ScoreRecord> scores = new List<ScoreRecord>();
        public EncounterOffer offer;
        public Enemy enemy;
        public TurnLog lastTurn;
        public string message;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ParticipantGrowthState growth;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public V4ParticipantState v4;
    }
    [Serializable] public sealed class ParticipantSummary
    {
        public string participantId, name, message;
        public int fromPosition, toPosition, travel, hpBefore, hpAfter, honorGained, completedLaps;
        public bool eliminated, finished;
    }
    [Serializable] public sealed class CompetitionState
    {
        public string matchId, endReason;
        public uint seed;
        public int worldRound = 1, actionNumber;
        public bool finished;
        public CompetitionDefinition definition;
        public Profile startingProfile;
        public ParticipantState[] participants;
        public List<ParticipantSummary> lastRound = new List<ParticipantSummary>();
    }
    [Serializable] public sealed class CompetitionCommandEntry
    {
        public int sequence;
        public string kind, arg, targetId, clientCommandId;
    }
}
