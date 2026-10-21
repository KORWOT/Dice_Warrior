using UnityEngine;

namespace DiceWarrior.SymbolLoop
{
    [CreateAssetMenu(menuName="Dice Warrior/Symbol Loop Rules")]
    public sealed class SymbolLoopConfig : ScriptableObject
    {
        public Rules rules = new Rules();
        public Rules CreateRules()
        {
            LoopRules.ValidateRules(rules);
            return LoopJson.Copy(rules);
        }
    }
}
