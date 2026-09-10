using System;

namespace FateDice
{
    public sealed class RunUIContext
    {
        public UIManager manager;
        public UiPrefabReferences prefabs;
        public FateDiceVisualCatalog visuals;
        public PresentationSettings presentation;
    }
    public sealed class RunHUDData
    {
        public string header, stats, situation, fate, notice, gear;
        public int[] dice;
        public Action<int> dieClicked;
        public UIChoiceData menu;
    }
    public sealed class UIChoiceData
    {
        public string key, text;
        public Action clicked;
        public bool interactable = true, selected;
        public ButtonPurpose purpose = ButtonPurpose.Primary;
    }
    public sealed class ActionOfferUIData
    {
        public string id, originalId, label, effect;
        public string[] tags;
        public Grade grade;
        public VisualArtwork visual;
        public Action<string> clicked;
    }
    // Public pre-choice data intentionally has no actual event/content identity.
    public sealed class FateOfferUIData
    {
        public string id;
        public NodeType type;
        public Grade grade;
        public Action<string> clicked;
    }
    public abstract class RunUIData : UIData
    {
        public RunUIContext context;
        public RunHUDData hud;
    }
    public sealed class MenuUIData : RunUIData
    {
        public string characterName, characterDetails, growthDetails;
        public UIChoiceData[] trials, caps;
        public string seed;
        public Action<string> seedChanged;
        public UIChoiceData start, resume, archive;
        public string error, lastResult;
    }
    public sealed class DiceRollUIData : UIData
    {
        public string title, detail, result;
        public string comboName;
        public HandKind hand;
        public float comboStrength, holdSeconds;
        public int[] values;
        public bool rolling;
        public float duration;
        public UIChoiceData roll;
        public ButtonAppearance appearance;
    }
    public sealed class ExplorationUIData : RunUIData
    {
        public CampaignNodeUIData[] campaignNodes;
        public string currentNodeId;
        public int mapFloors;
        public string mapProgressLabel, mapGoldLabel, mapHealthLabel, mapWardLabel;
        public NodeState[] nodes;
        public NodeState[] completedNodes;
        public string[] available;
        public string selected, instructions;
        public Action<string> chooseNode;
        public UIChoiceData roll, cap;
        public FateOfferUIData[] fates;
    }
    // Distant types are omitted before reaching the view, rather than merely dimmed by it.
    public sealed class CampaignNodeUIData
    {
        public string id;
        public NodeType? type;
        public int floor, lane;
        public string[] childIds;
        public bool revealed, available, completed, unreachable;
    }
    public sealed class CombatUIData : RunUIData
    {
        public string battleLabel, turnLabel, enemyName, enemyHealth, enemyShield, intentLabel, intentValue;
        public string playerName, playerHealth, playerShield, playerAttributes, handLabel, fatePowerLabel, rerollLabel;
        public float enemyHealth01, playerHealth01;
        public VisualArtwork revealed;
        public UIChoiceData roll;
        public ActionOfferUIData[] actions;
    }
    // A committed action's display snapshot; views never resolve combat or inspect a saved run.
    public sealed class CombatFeedbackData
    {
        public string actionLabel, enemyActionLabel;
        public Grade grade;
        public int attack, blocked, blockGained, enemyHpLost, playerHpLost;
        public int incoming, absorbed, enemyBlockGained, enemyHpAfter, playerHpAfter;
        public int enemyMaxHp, playerMaxHp;
        public bool retaliates, enemyDefends;
    }
    public sealed class EncounterUIData : RunUIData
    {
        public VisualArtwork revealed;
        public string description, outcome;
        public UIChoiceData[] choices;
    }
    public sealed class RewardUIData : RunUIData
    {
        public VisualArtwork revealed;
        public string description, instructions;
        public UIChoiceData claim;
    }
    public sealed class EquipmentUIData : RunUIData
    {
        public VisualArtwork revealed;
        public string details, current;
        public UIChoiceData[] choices;
    }
    public sealed class ResultUIData : RunUIData
    {
        public string summary, grades;
        public UIChoiceData restart;
    }
}
