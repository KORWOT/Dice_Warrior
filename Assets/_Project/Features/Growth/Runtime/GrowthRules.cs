using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    public struct PlayerStats { public int maxHp,power,guard; }
    public static class GrowthRules
    {
        public static PlayerStats Stats(RunState state)
        {
            long hp=state.baseMaxHp,power=state.basePower,guard=state.baseGuard;
            foreach(var item in Equipped(state)){hp+=item.maxHp;power+=item.power;guard+=item.guard;}
            return new PlayerStats{maxHp=Limit(hp),power=Limit(power),guard=Limit(guard)};
        }
        public static void Grant(RunState state,RewardDefinition reward)
        {
            state.gold=Limit((long)state.gold+reward.gold);
            state.xp=Limit((long)state.xp+reward.xp);
            var previousMax=Stats(state).maxHp;
            while(state.level<=state.config.growth.levels.Length)
            {
                var step=state.config.growth.levels[state.level-1];
                if(state.xp<step.xpRequired)break;
                state.xp-=step.xpRequired;state.level++;
                state.baseMaxHp=Limit((long)state.baseMaxHp+step.maxHp);
                state.basePower=Limit((long)state.basePower+step.power);
                state.baseGuard=Limit((long)state.baseGuard+step.guard);
            }
            var maximum=Stats(state).maxHp;
            var growthHeal=state.hp>0?Math.Max(0,maximum-previousMax):0;
            state.hp=(int)Math.Max(0,Math.Min(maximum,(long)state.hp+growthHeal+reward.health));
            if(reward.rerollCharges>0)
            {
                state.rerollUnlocked=true;state.rerollCharges=Limit((long)state.rerollCharges+reward.rerollCharges);
            }
        }
        public static void Equip(RunState state,string id)
        {
            var previousMax=Stats(state).maxHp;var item=state.config.Equipment(id);
            state.equipmentIds[(int)item.slot]=id;
            var maximum=Stats(state).maxHp;
            state.hp=(int)Math.Min(maximum,(long)state.hp+(state.hp>0?Math.Max(0,maximum-previousMax):0));
        }
        public static double Multiplier(RunState state,ActionDefinition action,ModifiedValue value)
        {
            var equipment=Equipped(state).ToArray();
            var equippedTags=new HashSet<string>(equipment.SelectMany(x=>x.tags));
            var cardTags=new HashSet<string>(action.tags);
            double multiplier=1;
            foreach(var modifier in equipment.SelectMany(x=>x.modifiers).Where(x=>x.value==value))
            {
                var tags=modifier.source==TagSource.Card?cardTags:equippedTags;
                bool match;
                switch(modifier.match)
                {
                    case MatchMode.Any:match=modifier.requiredTags.Any(tags.Contains);break;
                    case MatchMode.All:match=modifier.requiredTags.All(tags.Contains);break;
                    case MatchMode.None:match=!modifier.requiredTags.Any(tags.Contains);break;
                    default:throw new InvalidOperationException("Unsupported tag match rule.");
                }
                if(match)multiplier+=modifier.bonus;
            }
            return Math.Max(0,multiplier);
        }
        private static IEnumerable<EquipmentDefinition> Equipped(RunState state)
            =>state.equipmentIds.Where(id=>!string.IsNullOrEmpty(id)).Select(state.config.Equipment);
        private static int Limit(long value)=>(int)Math.Max(0,Math.Min(int.MaxValue,value));
    }
}
