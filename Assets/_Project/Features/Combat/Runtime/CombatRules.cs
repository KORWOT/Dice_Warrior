using System;
using System.Linq;

namespace FateDice
{
    public struct ActionEffect { public int damage, block; }
    public static class CombatRules
    {
        public static ActionEffect Evaluate(RunStateData state,OfferedCard card) => EffectResolver.Evaluate(state,card);

        public static int Amount(int stat,double coefficient,int minimum)
        {
            if(coefficient<=0)return 0;
            return (int)Math.Min(int.MaxValue,Math.Max(minimum,Math.Round(stat*(double)coefficient,MidpointRounding.AwayFromZero)));
        }
        public static void Begin(RunStateData state,string enemyId)
        {
            var enemy=state.Rules.Enemy(enemyId);state.activeEnemyId=enemy.id;state.enemyHp=enemy.maxHp;
            state.enemyShield=0;state.shield=0;state.dice=null;state.cards.Clear();state.phase=RunPhase.CombatRoll;
            NextIntent(state);
        }
        public static int IntentAmount(RunStateData state)
        {
            var enemy=state.Rules.Enemy(state.activeEnemyId);
            var intent=enemy.intents.Single(x=>x.id==state.activeIntentId);
            return Amount(intent.kind==IntentKind.Defend?enemy.guard:enemy.power,intent.coefficient,state.Rules.combat.minimumEffect);
        }
        public static void Resolve(RunStateData state,OfferedCard card)
        {
            if(state.phase!=RunPhase.CombatCards||state.enemyHp<=0||state.hp<=0)throw new InvalidOperationException("Combat action is not available in this phase.");
            var effect=Evaluate(state,card);
            state.shield=(int)Math.Min(int.MaxValue,(long)state.shield+effect.block);
            var damage=Math.Max(0,effect.damage-state.enemyShield);
            state.enemyHp=Math.Max(0,state.enemyHp-damage);
            state.enemyShield=0; // A player action expires the enemy's remaining shield, even if it dealt no damage.
            state.combatTurns++;state.selectedGrades[(int)card.grade]++;
            state.cards.Clear();
            state.message=state.Rules.Action(card.contentId).label+": "+damage+" damage, "+effect.block+" shield.";
            if(state.enemyHp==0)
            {
                state.phase=RunPhase.Reward;state.shield=0;
                state.pendingRewardId=state.selectedNode.id+":reward";
                state.message+=" Victory! Claim the earned reward.";
                return; // Dead enemies never retaliate or consume another intent sample.
            }
            var enemy=state.Rules.Enemy(state.activeEnemyId);
            var intent=enemy.intents.Single(x=>x.id==state.activeIntentId);
            int amount=IntentAmount(state);
            if(intent.kind==IntentKind.Defend)
            {
                state.enemyShield=amount;state.message+=" Enemy gains "+amount+" shield.";
            }
            else
            {
                var incoming=Math.Max(0,amount-state.shield);
                state.hp=Math.Max(0,state.hp-incoming);state.message+=" Enemy deals "+incoming+" damage.";
            }
            state.shield=0; // Expires after the next opponent action, including their defense.
            if(state.hp==0){state.phase=RunPhase.Result;state.won=false;state.message+=" The journey ends.";return;}
            state.phase=RunPhase.CombatRoll;
            NextIntent(state);
        }
        private static void NextIntent(RunStateData state)
        {
            var intents=state.Rules.Enemy(state.activeEnemyId).intents;
            state.activeIntentId=intents[DiceRules.WeightedIndex(intents.Select(x=>x.weight).ToArray(),ref state.rngState)].id;
        }
    }
}
