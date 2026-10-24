using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionElements
    {
        public static string TypeName(DamageType type) => new[] { "물리", "화염", "냉기", "번개" }[(int)type];
        public static string ScalingText(AttributeValues coefficients) => string.Join(" + ", CompetitionAttributes.All.Where(s => coefficients.Get(s) > 0)
            .Select(s => CompetitionAttributes.Label(s) + " " + (coefficients.Get(s) / 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%"));

        public static V4EnemyDefinition Enemy(CompetitionDefinition d, ParticipantState p) => p?.enemy == null || p.v4?.enemyId == null ? null :
            d.v4.enemies.Single(e => e.encounterId == p.v4.enemyId);
        public static int Affinity(CompetitionDefinition d, V4EnemyDefinition enemy, DamageType type) => enemy == null ? 10000 :
            enemy.weakness == type ? 10000 + d.v4.weaknessBasisPoints : enemy.resistance == type ? 10000 - d.v4.resistanceBasisPoints : 10000;
        public static TagSnapshot ActionTags(CompetitionDefinition d, ParticipantState p, string skillId, DamageType type) =>
            CompetitionV4Content.Tags(d, p, skillId, type, Enemy(d, p));

        public static int Resistance(CompetitionDefinition d, ParticipantState p, DamageType type)
        {
            var modifiers = CompetitionAttributes.Bindings(d, p, CompetitionV4Content.Tags(d, p, null, type));
            return Math.Min(8500, CompetitionModifiers.Evaluate(0, modifiers.Where(m => m.target == "resistance")));
        }

        public static V4SkillResult SkillPreview(CompetitionDefinition d, ParticipantState p, string skillId, V4EnemyDefinition enemy = null)
        {
            var skill = d.combat.basics.Concat(d.combat.combos).Single(s => s.id == skillId);
            var definition = d.v4.skills.Single(s => s.skillId == skillId);
            return SkillPreview(d, p, skill, definition, enemy);
        }

        public static V4SkillResult SkillPreview(CompetitionDefinition d, ParticipantState p, V5SkillDefinition skill, V4EnemyDefinition enemy = null)
        {
            if (!CompetitionV5Content.IsEnabled(d)) throw new ArgumentException("Expected v5 skill definition");
            return SkillPreview(d, p, ToSkill(skill), skill.effect, enemy);
        }

        static Skill ToSkill(V5SkillDefinition skill) => new Skill { id = skill.id, name = skill.name, damage = skill.damage, block = skill.block };

        static V4SkillResult SkillPreview(CompetitionDefinition d, ParticipantState p, Skill skill, V4SkillDefinition definition, V4EnemyDefinition enemy)
        {
            string skillId = skill.id;
            DamageType type = definition.damageType;
            var conversions = p.equipment.Where(id => id != null).Select(id => d.v4.gear.Single(g => g.gearId == id)).Where(g => g.conversion.HasValue).ToArray();
            if (conversions.Length > 1) throw new ArgumentException("Conflicting primary damage conversions");
            if (skill.damage > 0 && conversions.Length == 1) type = conversions[0].conversion.Value;
            var tags = CompetitionV4Content.Tags(d, p, skillId, type, enemy, definition);
            var modifiers = CompetitionAttributes.Bindings(d, p, tags).Where(m => m.target == "damage" || m.target == "block").ToArray();
            var stats = CompetitionAttributes.Describe(d, p).attributes;
            int affinity = Affinity(d, enemy, type);
            return new V4SkillResult { skillId = skillId, originalType = definition.damageType, damageType = type, affinityBasisPoints = affinity,
                damageBeforeShield = skill.damage > 0 ? CompetitionModifiers.EvaluateScaled(skill.damage, stats, definition.damageScaling,
                    modifiers.Where(m => m.target == "damage"), affinity) : 0,
                block = skill.block > 0 ? CompetitionModifiers.EvaluateScaled(skill.block, stats, definition.blockScaling,
                    modifiers.Where(m => m.target == "block")) : 0,
                modifiers = modifiers, tags = tags.Effective.Select(t => t.ownerKind + "/" + t.ownerId + ":" + t.tagId).ToArray() };
        }

        public static TurnLog Resolve(CompetitionDefinition d, ParticipantState participant, Symbol[] faces)
        {
            var plan = SymbolCombat.Plan(d.combat, faces, true);
            return ResolvePlanned(d, participant, plan, plan.Select(s => d.v4.skills.Single(v => v.skillId == s.id)).ToArray(), faces);
        }

        public static TurnLog ResolvePlanned(CompetitionDefinition d, ParticipantState participant, V5SkillDefinition[] plan)
        {
            if (!CompetitionV5Content.IsEnabled(d) || plan == null || plan.Length > 3) throw new ArgumentException("Invalid v5 skill plan");
            return ResolvePlanned(d, participant, plan.Select(ToSkill).ToArray(), plan.Select(s => s.effect).ToArray(), Array.Empty<Symbol>());
        }

        static TurnLog ResolvePlanned(CompetitionDefinition d, ParticipantState participant, Skill[] plan, V4SkillDefinition[] definitions, Symbol[] faces)
        {
            if (participant.enemy == null || participant.enemy.hp <= 0 || participant.hp <= 0 || participant.v4 == null)
                throw new InvalidOperationException("Combat ended or missing v4 state");
            var p = LoopJson.Copy(participant);
            var enemy = p.enemy;
            var profile = Enemy(d, p) ?? throw new ArgumentException("Missing frozen enemy profile");
            var typed = new V4CombatLog { retaliationType = profile.attackType };
            var log = new TurnLog { number = ++p.turn, faces = (Symbol[])faces.Clone(), intent = enemy.intent,
                playerHpBefore = p.hp, enemyHpBefore = enemy.hp };
            int block = 0;
            for (int index = 0; index < plan.Length; ++index)
            {
                var skill = plan[index];
                var step = new SkillLog { skillId = skill.id, name = skill.name, status = "applied", enemyHpBefore = enemy.hp, enemyShieldBefore = enemy.shield };
                log.skills.Add(step);
                V4SkillResult result;
                if (enemy.hp <= 0)
                {
                    step.status = "enemy-defeated";
                    var def = definitions[index];
                    result = new V4SkillResult { skillId = skill.id, originalType = def.damageType, damageType = def.damageType, status = step.status };
                }
                else
                {
                    result = SkillPreview(d, p, skill, definitions[index], profile);
                    int absorbed = Math.Min(enemy.shield, result.damageBeforeShield);
                    enemy.shield -= absorbed;
                    step.damage = Math.Min(enemy.hp, result.damageBeforeShield - absorbed); enemy.hp -= step.damage;
                    step.block = result.block; block = checked(block + step.block); result.damageApplied = step.damage;
                }
                typed.skills.Add(result); step.enemyHpAfter = enemy.hp; step.enemyShieldAfter = enemy.shield;
            }
            enemy.shield = 0; log.playerBlock = block;
            if (enemy.hp > 0)
            {
                log.retaliation = true;
                if (enemy.intent == Intent.Defend) enemy.shield = enemy.attack;
                else
                {
                    int attack = checked(enemy.attack + (enemy.intent == Intent.Heavy ? enemy.attack / 2 : 0));
                    typed.retaliationBeforeBlock = CompetitionModifiers.EvaluateScaled(attack, new AttributeValues(), new AttributeValues(),
                        Array.Empty<V4Modifier>(), 10000 - Resistance(d, p, profile.attackType));
                    log.retaliationDamage = Math.Min(p.hp, Math.Max(0, typed.retaliationBeforeBlock - block)); p.hp -= log.retaliationDamage;
                }
            }
            log.playerHpAfter = p.hp; log.enemyHpAfter = enemy.hp;
            participant.hp = p.hp; participant.turn = p.turn; participant.enemy = enemy;
            participant.lastTurn = log; participant.v4.lastTurn = typed;
            return log;
        }

        // Read-only heuristic using the faces actually owned. No simulated roll or RNG consumption.
        public static int MatchupScore(CompetitionDefinition d, ParticipantState p, string encounterId)
        {
            var enemy = d.v4.enemies.Single(e => e.encounterId == encounterId);
            var faces = p.dice.SelectMany(die => d.combat.dice.Single(x => x.id == die.definitionId).faces);
            int total = 0, count = 0;
            foreach (var face in faces)
            {
                var basic = d.combat.basics.Single(s => s.symbols[0] == face);
                if (basic.damage <= 0) continue;
                total += SkillPreview(d, p, basic.id, enemy).affinityBasisPoints - 10000; ++count;
            }
            return count == 0 ? 0 : total / (count * 1000);
        }
    }
}
