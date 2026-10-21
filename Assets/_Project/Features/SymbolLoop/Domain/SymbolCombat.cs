using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceWarrior.SymbolLoop
{
    public static class SymbolCombat
    {
        public static string Signature(IEnumerable<Symbol> symbols)
        {
            var counts = new int[4];
            foreach (var symbol in symbols)
            {
                if ((int)symbol < 0 || (int)symbol >= counts.Length) throw new ArgumentException("Unknown symbol");
                counts[(int)symbol]++;
            }
            return string.Join(":", counts);
        }

        public static Skill[] Plan(Rules rules, Symbol[] faces)
        {
            if (faces == null || faces.Length < 1 || faces.Length > 3) throw new ArgumentException("Dice count");
            string key = Signature(faces);
            var combo = rules.combos.SingleOrDefault(s => Signature(s.symbols) == key);
            return combo != null ? new[] { combo } : faces.Select(f => rules.basics.Single(s => s.symbols[0] == f)).ToArray();
        }

        public static TurnLog Resolve(Run run, Symbol[] faces)
        {
            if (run.enemy == null || run.enemy.hp <= 0 || run.hp <= 0) throw new InvalidOperationException("Combat ended");
            var enemy = run.enemy;
            var log = new TurnLog {number=++run.turn, faces=(Symbol[])faces.Clone(), intent=enemy.intent,
                playerHpBefore=run.hp, enemyHpBefore=enemy.hp};
            int playerBlock = 0;
            foreach (var skill in Plan(run.rules, faces))
            {
                var step = new SkillLog {skillId=skill.id, name=skill.name, status="applied",
                    enemyHpBefore=enemy.hp, enemyShieldBefore=enemy.shield};
                log.skills.Add(step);
                if (enemy.hp <= 0) step.status = "enemy-defeated";
                else
                {
                    var gear = run.equipment.Where(id => id != null).Select(id => run.rules.gear.Single(g => g.id == id))
                        .Where(g => g.tag == skill.tag).ToArray();
                    int damage = skill.damage > 0 ? skill.damage + run.power + gear.Sum(g => g.damage) : 0;
                    int absorbed = Math.Min(enemy.shield, damage);
                    enemy.shield -= absorbed;
                    step.damage = Math.Min(enemy.hp, damage - absorbed);
                    enemy.hp -= step.damage;
                    step.block = skill.block > 0 ? skill.block + run.guard + gear.Sum(g => g.block) : 0;
                    playerBlock += step.block;
                }
                step.enemyHpAfter = enemy.hp;
                step.enemyShieldAfter = enemy.shield;
            }
            enemy.shield = 0;
            log.playerBlock = playerBlock;
            if (enemy.hp > 0)
            {
                log.retaliation = true;
                if (enemy.intent == Intent.Defend) enemy.shield = enemy.attack;
                else
                {
                    int attack = enemy.attack + (enemy.intent == Intent.Heavy ? enemy.attack / 2 : 0);
                    log.retaliationDamage = Math.Min(run.hp, Math.Max(0, attack - playerBlock));
                    run.hp -= log.retaliationDamage;
                }
            }
            log.playerHpAfter = run.hp;
            log.enemyHpAfter = enemy.hp;
            run.lastTurn = log;
            return log;
        }
    }
}
