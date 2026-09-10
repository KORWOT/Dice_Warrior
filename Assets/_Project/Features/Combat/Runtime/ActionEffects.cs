using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    public enum ActionEffectKind { Damage, Block }
    [Serializable] public sealed class ActionEffectDefinition
    {
        public ActionEffectKind kind;
        public float coefficient;
    }
    public interface IActionEffectHandler
    {
        ActionEffectKind Kind { get; }
        ActionEffect Evaluate(RunStateData state,ActionDefinition action,float coefficient,double gradeRatio);
    }
    public sealed class DamageEffectHandler : IActionEffectHandler
    {
        public ActionEffectKind Kind => ActionEffectKind.Damage;
        public ActionEffect Evaluate(RunStateData state,ActionDefinition action,float coefficient,double gradeRatio) => new ActionEffect
        {
            damage=CombatRules.Amount(GrowthRules.Stats(state).power,
                coefficient*gradeRatio*GrowthRules.Multiplier(state,action,ModifiedValue.Damage),state.Rules.combat.minimumEffect)
        };
    }
    public sealed class BlockEffectHandler : IActionEffectHandler
    {
        public ActionEffectKind Kind => ActionEffectKind.Block;
        public ActionEffect Evaluate(RunStateData state,ActionDefinition action,float coefficient,double gradeRatio) => new ActionEffect
        {
            block=CombatRules.Amount(GrowthRules.Stats(state).guard,
                coefficient*gradeRatio*GrowthRules.Multiplier(state,action,ModifiedValue.Block),state.Rules.combat.minimumEffect)
        };
    }
    public static class EffectResolver
    {
        // Explicit, fixed composition for the two supported mechanics. No discovery or mutable registry.
        static readonly IActionEffectHandler[] Handlers={new DamageEffectHandler(),new BlockEffectHandler()};
        public static IEnumerable<ActionEffectDefinition> Definitions(ActionDefinition action)
        {
            if(action.effects!=null&&action.effects.Length>0)return action.effects;
            // Schema1 definitions retain their original numeric source and calculation order.
            return new[]{
                new ActionEffectDefinition{kind=ActionEffectKind.Damage,coefficient=action.damageCoefficient},
                new ActionEffectDefinition{kind=ActionEffectKind.Block,coefficient=action.blockCoefficient}};
        }
        public static bool IsValid(ActionDefinition action)
        {
            if(action==null)return false;
            var effects=Definitions(action).ToArray();
            return effects.Length>0&&effects.All(effect=>effect!=null&&Handlers.Any(h=>h.Kind==effect.kind)&&
                !float.IsNaN(effect.coefficient)&&!float.IsInfinity(effect.coefficient)&&effect.coefficient>=0)&&
                effects.Any(effect=>effect.coefficient>0);
        }
        public static ActionEffect Evaluate(RunStateData state,OfferedCard card)
        {
            var action=state.Rules.Action(card.contentId);
            var ratio=(double)state.Rules.fate.gradeMultipliers[(int)card.grade]/state.Rules.fate.gradeMultipliers[(int)action.grade];
            long damage=0,block=0;
            foreach(var effect in Definitions(action))
            {
                var handler=effect==null?null:Handlers.FirstOrDefault(h=>h.Kind==effect.kind);
                if(handler==null)throw new InvalidOperationException("Unsupported action effect.");
                var amount=handler.Evaluate(state,action,effect.coefficient,ratio);
                damage=Math.Min(int.MaxValue,damage+amount.damage);
                block=Math.Min(int.MaxValue,block+amount.block);
            }
            return new ActionEffect{damage=(int)damage,block=(int)block};
        }
    }
}
