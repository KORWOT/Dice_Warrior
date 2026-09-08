using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FateDice
{
    public enum Grade { Common, Uncommon, Rare, Epic, Legendary }
    public enum NodeType { Combat, Event, Treasure, Shop, Rest, Boss }
    public enum HandKind { Pair, TwoPairs, Triple, ThreePairs, FullHouse, FourKind, Straight, FullStraight, FiveKind, SixKind }
    public enum EquipmentSlot { Weapon, Armor, Accessory }
    public enum MatchMode { Any, All, None }
    public enum TagSource { Card, Equipment }
    public enum ModifiedValue { Damage, Block }
    public enum IntentKind { Attack, Defend, Heavy }
    public enum RunPhase { Map, ExplorationRoll, ExplorationCards, CombatRoll, CombatCards, Encounter, Shop, Reward, EquipmentChoice, Result }

    [Serializable] public sealed class HandDefinition
    {
        [Tooltip("Stable rule ID; each of the ten kinds exactly once.")] public HandKind kind;
        [Tooltip("Display text only; not a save ID.")] public string label;
        [Tooltip("Greater wins. Unique nonnegative priority, separate from fate power.")] public int priority;
        [Tooltip("Base fate power, clamped to the configured table range after bonus.")] public int fatePower;
    }
    [Serializable] public sealed class DieDefinition
    {
        public string id;
        public string label;
        [Tooltip("Exactly six face entries. Values 1..6, duplicates allowed.")] public int[] values;
        [Tooltip("Relative nonnegative face weights; positive finite sum.")] public float[] weights;
    }
    [Serializable] public sealed class GradeRow
    {
        [Tooltip("Minimum inclusive fate power for this row. Rows must be ascending.")] public int minimumPower;
        [Tooltip("Common, Uncommon, Rare, Epic, Legendary relative weights.")] public float[] weights;
    }
    [Serializable] public sealed class DiceSettings
    {
        public string basicDieId;
        public string replacementDieId;
        public DieDefinition[] dice;
        public HandDefinition[] hands;
        [Tooltip("Inclusive power range of both tables.")] public int minimumFatePower, maximumFatePower;
    }
    [Serializable] public sealed class FateSettings
    {
        public GradeRow[] exploration;
        public GradeRow[] combat;
        [Tooltip("Exactly five grade multipliers. Empty action grades scale numeric effects by offered/base ratio.")] public float[] gradeMultipliers;
        [Tooltip("Relative nonnegative weights: Combat, Event, Treasure, Shop, Rest.")] public float[] nodeWeights;
        [Tooltip("Multiply the chosen node weight for each non-guaranteed slot.")] public float selectedTypeBias;
        [Tooltip("Extra reward multiplier by selected exploration cap. Does not affect combat grades.")] public float[] capRewardMultipliers;
    }
    [Serializable] public sealed class ActionDefinition
    {
        public string id, label;
        public Grade grade;
        [Tooltip("Damage = Power * coefficient * grade ratio * explicit tag modifiers. Zero means no damage.")] public float damageCoefficient;
        [Tooltip("Shield = Guard * coefficient * grade ratio * explicit tag modifiers. Zero means no shield.")] public float blockCoefficient;
        public string[] tags;
    }
    [Serializable] public sealed class EnemyIntent
    {
        public string id, label;
        public IntentKind kind;
        [Tooltip("Relative selection weight; finite, nonnegative, sum positive.")] public float weight;
        [Tooltip("Attack uses enemy Power; Defend uses enemy Guard.")] public float coefficient;
    }
    [Serializable] public sealed class EnemyDefinition
    {
        public string id, label;
        [Min(1)] public int maxHp;
        [Min(0)] public int power, guard;
        public EnemyIntent[] intents;
    }
    [Serializable] public sealed class CombatSettings
    {
        public ActionDefinition[] actions;
        public string[] startingActionIds;
        [Tooltip("Choose one owned trial action at start; no draw preference. Rare or lower.")] public string[] trialActionIds;
        public EnemyDefinition[] enemies;
        public string bossEnemyId;
        [Tooltip("Minimum positive damage/shield after final rounding. No effect if coefficient is zero.")] [Min(0)] public int minimumEffect;
    }
    [Serializable] public sealed class TagDefinition { public string id, label; }
    [Serializable] public sealed class TagModifier
    {
        public MatchMode match;
        public TagSource source;
        public ModifiedValue value;
        public string[] requiredTags;
        [Tooltip("Additive fractional modifier; 0.2 = +20%. Sum clamped at zero.")] public float bonus;
    }
    [Serializable] public sealed class EquipmentDefinition
    {
        public string id, label;
        public EquipmentSlot slot;
        [Min(0)] public int maxHp, power, guard;
        public string[] tags;
        public TagModifier[] modifiers;
    }
    [Serializable] public sealed class LevelStep
    {
        [Tooltip("XP cost from current to next level. Last row is the final advance, then capped.")] [Min(1)] public int xpRequired;
        [Min(0)] public int maxHp, power, guard;
    }
    [Serializable] public sealed class RewardDefinition
    {
        [Min(0)] public int gold, xp;
        [Tooltip("HP delta: positive heal, negative hazard. Final HP clamped to max.")] public int health;
        [Tooltip("Empty means no equipment.")] public string equipmentId;
        [Tooltip("Earns reroll permission and these charges; zero grants nothing.")] [Min(0)] public int rerollCharges;
        [Tooltip("Empty means no die replacement. The player selects one of six dice.")] public string dieId;
    }
    [Serializable] public sealed class EventDefinition
    {
        public string id, label;
        [TextArea] public string description;
        public NodeType type;
        public Grade grade;
        [Tooltip("Required for combat events; ignored for other events.")] public string enemyId;
        public RewardDefinition reward;
    }
    [Serializable] public sealed class ShopProduct
    {
        public string id, label;
        [Min(0)] public int price;
        public RewardDefinition reward;
    }
    [Serializable] public sealed class GrowthSettings
    {
        public string characterId, characterName;
        [Min(1)] public int startingMaxHp;
        [Min(0)] public int startingPower, startingGuard, startingGold;
        public LevelStep[] levels;
        public TagDefinition[] tags;
        public EquipmentDefinition[] equipment;
        [Tooltip("Charges consumed per one-die reroll. Permission must first be earned.")] [Min(1)] public int rerollCost;
    }
    [Serializable] public sealed class WorldSettings
    {
        [Range(1,100)] public int eventsToBoss;
        [Range(1,5)] public int offeredCards;
        [Range(1,3)] public int previewDepth;
        [Range(2,3)] public int branchCount;
        public EventDefinition[] events;
        public RewardDefinition bossReward;
        public ShopProduct[] shop;
        public RewardDefinition restTraining;
    }
    [Serializable] public sealed class PresentationSettings
    {
        [Tooltip("Cosmetic input lock duration, seconds. Does not consume rules RNG.")] [Range(0,2)] public float actionSeconds;
        [Tooltip("Dice reveal duration, seconds. Result is already decided and checkpointed.")] [Range(0,2)] public float rollSeconds;
        [Tooltip("Portrait logical canvas dimensions in pixels.")] public Vector2 referenceResolution;
        [Tooltip("Main text pixel sizes at reference resolution.")] [Range(16,64)] public int bodyFontSize, titleFontSize;
        [Tooltip("Button height in reference pixels.")] [Range(48,180)] public float buttonHeight;
        public Color background, panel, accent, text, danger;
        [Tooltip("One color per grade, alongside grade text.")] public Color[] gradeColors;
    }
    [Serializable] public sealed class GameConfigData
    {
        [Tooltip("Snapshot schema identity; not the only restored data.")] public string version;
        public DiceSettings dice;
        public FateSettings fate;
        public CombatSettings combat;
        public GrowthSettings growth;
        public WorldSettings world;
        public PresentationSettings presentation;

        public GameConfigData DeepCopy() => JsonUtility.FromJson<GameConfigData>(JsonUtility.ToJson(this));
        public string[] Validate()
        {
            var errors = new List<string>();
            bool Check(bool ok,string path) { if(!ok)errors.Add(path);return ok; }
            bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
            bool Nonnegative(float x)=>Finite(x)&&x>=0;
            bool Positive(float x)=>Finite(x)&&x>0;
            void Weights(float[] xs,int count,string path)
            {
                if(Check(xs!=null&&xs.Length==count,path+": wrong weight count"))
                    Check(xs.All(Nonnegative)&&xs.Sum(x=>(double)x)>0,path+": finite nonnegative weights with positive sum required");
            }
            HashSet<string> Ids<T>(T[] xs,Func<T,string> id,string path) where T:class
            {
                var ids=new HashSet<string>(StringComparer.Ordinal);
                if(!Check(xs!=null&&xs.Length>0,path+": empty required pool"))return ids;
                foreach(var x in xs)if(Check(x!=null,path+": null entry"))
                    Check(!string.IsNullOrWhiteSpace(id(x))&&ids.Add(id(x)),path+": missing/duplicate ID "+id(x));
                return ids;
            }
            Check(!string.IsNullOrWhiteSpace(version),"version: required");
            Check(dice!=null,"dice: required");Check(fate!=null,"fate: required");Check(combat!=null,"combat: required");
            Check(growth!=null,"growth: required");Check(world!=null,"world: required");Check(presentation!=null,"presentation: required");
            if(dice==null||fate==null||combat==null||growth==null||world==null||presentation==null)return errors.ToArray();
            var dieIds=Ids(dice.dice,x=>x.id,"dice.dice");
            Check(dieIds.Contains(dice.basicDieId),"dice.basicDieId: missing reference");
            Check(dieIds.Contains(dice.replacementDieId),"dice.replacementDieId: missing reference");
            foreach(var die in dice.dice??Array.Empty<DieDefinition>())if(die!=null)
            {
                Check(!string.IsNullOrWhiteSpace(die.label),"dice."+die.id+": label required");
                Check(die.values!=null&&die.values.Length==6&&die.values.All(x=>x>=1&&x<=6),"dice."+die.id+": six values in 1..6 required");
                Weights(die.weights,6,"dice."+die.id);
            }
            Check(dice.minimumFatePower>=1&&dice.maximumFatePower>=dice.minimumFatePower,"dice.fateRange: invalid 1..N range");
            Check(dice.hands!=null&&dice.hands.Length==10,"dice.hands: exactly ten required");
            var kinds=new HashSet<HandKind>();var priorities=new HashSet<int>();
            foreach(var hand in dice.hands??Array.Empty<HandDefinition>())if(Check(hand!=null,"dice.hands: null rule"))
            {
                Check(Enum.IsDefined(typeof(HandKind),hand.kind)&&kinds.Add(hand.kind),"dice.hands: invalid/duplicate kind");
                Check(hand.priority>=0&&priorities.Add(hand.priority),"dice.hands: unique nonnegative priority required");
                Check(hand.fatePower>=dice.minimumFatePower&&hand.fatePower<=dice.maximumFatePower,"dice.hands: power outside range");
                Check(!string.IsNullOrWhiteSpace(hand.label),"dice.hands: label required");
            }
            void Rows(GradeRow[] rows,string path)
            {
                if(!Check(rows!=null&&rows.Length>0,path+": empty table"))return;
                var previous=int.MinValue;
                for(var i=0;i<rows.Length;i++)
                {
                    var row=rows[i];if(!Check(row!=null,path+": null row"))continue;
                    Check(row.minimumPower>previous&&row.minimumPower>=dice.minimumFatePower&&row.minimumPower<=dice.maximumFatePower,path+": ascending powers inside range required");
                    if(i==0)Check(row.minimumPower==dice.minimumFatePower,path+": first row must cover minimum");
                    previous=row.minimumPower;Weights(row.weights,5,path+"["+i+"]");
                }
            }
            Rows(fate.exploration,"fate.exploration");Rows(fate.combat,"fate.combat");
            Check(fate.gradeMultipliers!=null&&fate.gradeMultipliers.Length==5&&fate.gradeMultipliers.All(Positive),"fate.gradeMultipliers: five positive finite ratios required");
            Check(fate.capRewardMultipliers!=null&&fate.capRewardMultipliers.Length==5&&fate.capRewardMultipliers.All(Positive),"fate.capRewardMultipliers: five positive finite ratios required");
            Weights(fate.nodeWeights,5,"fate.nodeWeights");Check(Nonnegative(fate.selectedTypeBias),"fate.selectedTypeBias: finite nonnegative required");
            if(fate.nodeWeights!=null&&fate.nodeWeights.Length==5)
                for(var i=0;i<5;i++)Check(fate.nodeWeights.Select((w,j)=>(double)w*(i==j?fate.selectedTypeBias:1)).Sum()>0,"fate.nodeWeights: biased pool is empty for type "+i);
            var tagIds=Ids(growth.tags,x=>x.id,"growth.tags");var itemIds=Ids(growth.equipment,x=>x.id,"growth.equipment");
            var actionIds=Ids(combat.actions,x=>x.id,"combat.actions");var enemyIds=Ids(combat.enemies,x=>x.id,"combat.enemies");
            void Tags(string[] tags,string path)=>Check(tags!=null&&tags.All(x=>x!=null&&tagIds.Contains(x))&&tags.Distinct().Count()==tags.Length,path+": invalid/duplicate/missing tags");
            foreach(var tag in growth.tags??Array.Empty<TagDefinition>())if(tag!=null)Check(!string.IsNullOrWhiteSpace(tag.label),"growth.tags: label required");
            foreach(var action in combat.actions??Array.Empty<ActionDefinition>())if(action!=null)
            {
                Check(!string.IsNullOrWhiteSpace(action.label)&&Enum.IsDefined(typeof(Grade),action.grade),"combat.actions."+action.id+": invalid label/grade");
                Check(Nonnegative(action.damageCoefficient)&&Nonnegative(action.blockCoefficient)&&(action.damageCoefficient>0||action.blockCoefficient>0),"combat.actions."+action.id+": finite nonnegative coefficients with an effect required");
                Tags(action.tags,"combat.actions."+action.id);
            }
            void Actions(string[] refs,string path)=>Check(refs!=null&&refs.Length>0&&refs.All(x=>x!=null&&actionIds.Contains(x))&&refs.Distinct().Count()==refs.Length,path+": unique existing action IDs required");
            Actions(combat.startingActionIds,"combat.startingActionIds");Actions(combat.trialActionIds,"combat.trialActionIds");
            if(combat.trialActionIds!=null&&combat.actions!=null)foreach(var id in combat.trialActionIds)
            {
                var action=combat.actions.FirstOrDefault(x=>x!=null&&x.id==id);
                if(action!=null)Check(action.grade<=Grade.Rare,"combat.trialActionIds: Rare or lower required");
            }
            Check(combat.minimumEffect>=0,"combat.minimumEffect: nonnegative required");Check(enemyIds.Contains(combat.bossEnemyId),"combat.bossEnemyId: missing reference");
            foreach(var enemy in combat.enemies??Array.Empty<EnemyDefinition>())if(enemy!=null)
            {
                Check(!string.IsNullOrWhiteSpace(enemy.label)&&enemy.maxHp>0&&enemy.power>=0&&enemy.guard>=0,"combat.enemies."+enemy.id+": invalid label/HP/stats");
                Ids(enemy.intents,x=>x.id,"combat.enemies."+enemy.id+".intents");
                if(enemy.intents==null||enemy.intents.Any(x=>x==null))continue;
                Weights(enemy.intents.Select(x=>x.weight).ToArray(),enemy.intents.Length,"combat.enemies."+enemy.id+".intents");
                foreach(var intent in enemy.intents)Check(Enum.IsDefined(typeof(IntentKind),intent.kind)&&!string.IsNullOrWhiteSpace(intent.label)&&Positive(intent.coefficient),"combat.enemies: invalid intent kind/label/coefficient");
            }
            Check(!string.IsNullOrWhiteSpace(growth.characterId)&&!string.IsNullOrWhiteSpace(growth.characterName),"growth.character: ID/label required");
            Check(growth.startingMaxHp>0&&growth.startingPower>=0&&growth.startingGuard>=0&&growth.startingGold>=0,"growth.startingStats: invalid");
            Check(growth.rerollCost>0,"growth.rerollCost: positive required");
            Check(growth.levels!=null&&growth.levels.Length>0,"growth.levels: required");
            foreach(var level in growth.levels??Array.Empty<LevelStep>())Check(level!=null&&level.xpRequired>0&&level.maxHp>=0&&level.power>=0&&level.guard>=0,"growth.levels: positive cost and nonnegative growth required");
            foreach(var item in growth.equipment??Array.Empty<EquipmentDefinition>())if(item!=null)
            {
                Check(Enum.IsDefined(typeof(EquipmentSlot),item.slot)&&!string.IsNullOrWhiteSpace(item.label),"growth.equipment."+item.id+": invalid slot/label");
                Check(item.maxHp>=0&&item.power>=0&&item.guard>=0,"growth.equipment."+item.id+": nonnegative stats required");
                Tags(item.tags,"growth.equipment."+item.id);
                Check(item.modifiers!=null,"growth.equipment."+item.id+": modifier list required (may be empty)");
                foreach(var modifier in item.modifiers??Array.Empty<TagModifier>())if(Check(modifier!=null,"growth.equipment: null modifier"))
                {
                    Check(Enum.IsDefined(typeof(MatchMode),modifier.match)&&Enum.IsDefined(typeof(TagSource),modifier.source)&&Enum.IsDefined(typeof(ModifiedValue),modifier.value)&&Finite(modifier.bonus),"growth.equipment: invalid explicit modifier");
                    Tags(modifier.requiredTags,"growth.equipment."+item.id+".requiredTags");
                    Check(modifier.requiredTags!=null&&modifier.requiredTags.Length>0,"growth.equipment: nonempty condition tags required");
                }
            }
            void Reward(RewardDefinition reward,string path)
            {
                if(!Check(reward!=null,path+": required"))return;
                Check(reward.gold>=0&&reward.xp>=0&&reward.rerollCharges>=0,path+": nonnegative gold/XP/charges required");
                Check(string.IsNullOrEmpty(reward.equipmentId)||itemIds.Contains(reward.equipmentId),path+": missing equipment reference");
                Check(string.IsNullOrEmpty(reward.dieId)||dieIds.Contains(reward.dieId),path+": missing die reference");
            }
            Check(world.eventsToBoss>=1&&world.eventsToBoss<=100,"world.eventsToBoss: 1..100");
            Check(world.offeredCards>=1&&world.offeredCards<=5,"world.offeredCards: 1..5");
            Check(world.previewDepth>=1&&world.previewDepth<=3&&world.branchCount>=2&&world.branchCount<=3,"world.tree: depth1..3 branches2..3");
            Ids(world.events,x=>x.id,"world.events");
            foreach(var encounter in world.events??Array.Empty<EventDefinition>())if(encounter!=null)
            {
                Check(encounter.type>=NodeType.Combat&&encounter.type<=NodeType.Rest&&Enum.IsDefined(typeof(Grade),encounter.grade),"world.events."+encounter.id+": invalid normal type/grade");
                Check(!string.IsNullOrWhiteSpace(encounter.label)&&!string.IsNullOrWhiteSpace(encounter.description),"world.events."+encounter.id+": label/description required");
                if(encounter.type==NodeType.Combat)Check(enemyIds.Contains(encounter.enemyId),"world.events."+encounter.id+": missing enemy reference");
                Reward(encounter.reward,"world.events."+encounter.id+".reward");
            }
            for(var type=0;type<5;type++)for(var grade=0;grade<5;grade++)
                Check(world.events!=null&&world.events.Any(x=>x!=null&&(int)x.type==type&&(int)x.grade==grade),"world.events: missing type "+type+" grade "+grade+" pool");
            Ids(world.shop,x=>x.id,"world.shop");
            foreach(var product in world.shop??Array.Empty<ShopProduct>())if(product!=null)
            {
                Check(product.price>=0&&!string.IsNullOrWhiteSpace(product.label),"world.shop."+product.id+": invalid price/label");Reward(product.reward,"world.shop."+product.id+".reward");
            }
            Reward(world.bossReward,"world.bossReward");Reward(world.restTraining,"world.restTraining");
            Check(Nonnegative(presentation.actionSeconds)&&presentation.actionSeconds<=2&&Nonnegative(presentation.rollSeconds)&&presentation.rollSeconds<=2,"presentation.timings: 0..2 seconds");
            Check(Positive(presentation.referenceResolution.x)&&Positive(presentation.referenceResolution.y)&&presentation.referenceResolution.y>presentation.referenceResolution.x,"presentation.referenceResolution: positive portrait dimensions required");
            Check(presentation.bodyFontSize>=16&&presentation.bodyFontSize<=64&&presentation.titleFontSize>=16&&presentation.titleFontSize<=64,"presentation.fonts: 16..64");
            Check(Finite(presentation.buttonHeight)&&presentation.buttonHeight>=48&&presentation.buttonHeight<=180,"presentation.buttonHeight: 48..180");
            Check(presentation.gradeColors!=null&&presentation.gradeColors.Length==5,"presentation.gradeColors: five required");
            return errors.ToArray();
        }
        public ActionDefinition Action(string id) => combat.actions.Single(x => x.id == id);
        public EnemyDefinition Enemy(string id) => combat.enemies.Single(x => x.id == id);
        public EquipmentDefinition Equipment(string id) => growth.equipment.Single(x => x.id == id);
        public DieDefinition Die(string id) => dice.dice.Single(x => x.id == id);
        public EventDefinition Event(string id) => world.events.Single(x => x.id == id);
    }
    [CreateAssetMenu(menuName="Fate Dice/Prototype Config", fileName="DefaultFateDice")]
    public sealed class FateDiceConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset";
        public GameConfigData data;
        public GameConfigData Snapshot()
        {
            if (data == null) throw new InvalidOperationException("Missing config data: " + DefaultAssetPath);
            var errors = data.Validate();
            if (errors.Length != 0) throw new InvalidOperationException(DefaultAssetPath + ": " + string.Join("; ", errors));
            return data.DeepCopy();
        }
    }
}
