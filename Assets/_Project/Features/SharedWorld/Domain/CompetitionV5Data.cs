using System;
using Newtonsoft.Json;

namespace DiceWarrior.SharedWorld
{
    public enum V5FaceKind { Invalid = -1, Common, Class, Blank }
    public enum V5SelectionPhase { Invalid = -1, AwaitRoll, Selection }

    [Serializable] public sealed class V5Definition
    {
        [JsonProperty(Required = Required.Always)] public V5FaceDefinition[] faces = Array.Empty<V5FaceDefinition>();
        [JsonProperty(Required = Required.Always)] public V5DieLayout[] diceLayouts = Array.Empty<V5DieLayout>();
        [JsonProperty(Required = Required.Always)] public V5SkillDefinition[] skills = Array.Empty<V5SkillDefinition>();
        [JsonProperty(Required = Required.Always)] public int baseRerollsPerTurn = 1;
        [JsonProperty(Required = Required.Always)] public int maxHoldChangesPerTurn = 12;
        [JsonProperty(Required = Required.Always)] public string aiPolicyVersion = "v5-keep-nonblank-1";
    }
    [Serializable] public sealed class V5FaceDefinition
    {
        [JsonProperty(Required = Required.Always)] public string id, name, iconId;
        [JsonProperty(Required = Required.Always)] public V5FaceKind kind = V5FaceKind.Invalid;
        [JsonProperty(Required = Required.AllowNull)] public string adventurerId;
    }
    [Serializable] public sealed class V5DieLayout
    {
        [JsonProperty(Required = Required.Always)] public string dieId;
        [JsonProperty(Required = Required.Always)] public string[] faceIds = Array.Empty<string>();
    }
    [Serializable] public sealed class V5SkillDefinition
    {
        [JsonProperty(Required = Required.Always)] public string id, name, iconId;
        [JsonProperty(Required = Required.AllowNull)] public string ownerAdventurerId;
        [JsonProperty(Required = Required.Always)] public string[] faceIds = Array.Empty<string>();
        [JsonProperty(Required = Required.Always)] public int priority, damage, block;
        [JsonProperty(Required = Required.Always)] public V4SkillDefinition effect;
    }
    [Serializable] public sealed class V5ParticipantState
    {
        [JsonProperty(Required = Required.AllowNull)] public V5SelectionState selection;
        [JsonProperty(Required = Required.AllowNull)] public V5CombatLog lastTurn;
    }
    [Serializable] public sealed class V5SelectionState
    {
        [JsonProperty(Required = Required.Always)] public V5SelectionPhase phase = V5SelectionPhase.Invalid;
        [JsonProperty(Required = Required.Always)] public int combatTurnSequence, rerollsLeft, rollSequence, holdChanges;
        [JsonProperty(Required = Required.Always)] public V5FaceResult[] results = Array.Empty<V5FaceResult>();
    }
    [Serializable] public sealed class V5FaceResult
    {
        [JsonProperty(Required = Required.Always)] public string dieInstanceId, faceId;
        [JsonProperty(Required = Required.Always)] public int slotIndex = -1, faceIndex = -1;
        [JsonProperty(Required = Required.Always)] public bool held;
    }
    [Serializable] public sealed class V5CombatLog
    {
        [JsonProperty(Required = Required.Always)] public V5FaceResult[] results = Array.Empty<V5FaceResult>();
        [JsonProperty(Required = Required.Always)] public string[] plannedSkillIds = Array.Empty<string>();
        [JsonProperty(Required = Required.Always)] public bool noOp;
    }
    public sealed class V5CombatPreview
    {
        public V5PreviewSkill[] skills = Array.Empty<V5PreviewSkill>();
        public bool noOp;
        public string status;
    }
    public sealed class V5PlannedSkill
    {
        public V5SkillDefinition skill;
        public int[] dieSlotIndices = Array.Empty<int>();
    }
    public sealed class V5PreviewSkill
    {
        public string skillId, name, status;
        public int[] dieSlotIndices = Array.Empty<int>();
        public DamageType damageType;
        public int damageApplied, shieldAbsorbed, block;
    }
}
