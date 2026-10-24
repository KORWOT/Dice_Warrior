using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiceWarrior.SharedWorld
{
    // Read-only descriptions of the saved definition and shared rule projections.
    public static class CompetitionV5UiText
    {
        public static string Die(CompetitionDefinition d, string hero, string dieId)
        {
            var die = d.combat.dice.Single(value => value.id == dieId);
            var faces = CompetitionV5Content.Faces(d, hero, dieId);
            return die.name + " · 실제 6면\n" + string.Join(" / ", faces.Select((face, i) => (i + 1) + ": " + face.name));
        }

        public static string Gear(CompetitionDefinition d, ParticipantState p, string gearId)
        {
            var gear = d.combat.gear.Single(value => value.id == gearId);
            string current = p?.equipment[gear.slot];
            var lines = p == null ? new List<string> { "다음 모험 시작 무기", "후보: " + gear.name }
                : new List<string> { new[] { "무기", "방어구", "장신구" }[gear.slot] + " 교체",
                    "현재: " + (current == null ? "없음" : d.combat.gear.Single(value => value.id == current).name), "후보: " + gear.name };
            var effect = d.v4.gear.Single(value => value.gearId == gearId);
            foreach (var binding in effect.bindings)
            {
                string condition = binding.anyOf.Length == 0 ? "항상" : string.Join(" 또는 ", binding.anyOf.Select(query => Query(d, query)));
                lines.Add(condition + " → " + Target(binding.target) + " " + Amount(binding));
            }
            if (effect.conversion.HasValue) lines.Add("피해 타입 전환: " + CompetitionElements.TypeName(effect.conversion.Value));
            if (current == gearId) lines.Add("현재 장착 중인 장비입니다.");
            return Friendly(d, string.Join("\n", lines));
        }

        public static string Encounter(CompetitionDefinition d, ParticipantState p, EncounterChoice choice)
        {
            var definition = d.encounters.Single(value => value.id == choice.id);
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(choice.description)) lines.Add(choice.description);
            var costs = new List<string>();
            if (choice.hpCost > 0) costs.Add("HP " + choice.hpCost + " 소모");
            if (choice.goldCost > 0) costs.Add("골드 " + choice.goldCost + " 소모");
            lines.Add(costs.Count == 0 ? "소모 없음" : string.Join(" · ", costs));
            if (choice.hpCost > 0 || definition.minHp > 0)
                lines.Add("선택 조건: 최소 HP " + Math.Max(definition.minHp, choice.hpCost + 1) + " · 현재 " + p.hp);
            if (definition.requiredGearId != null)
                lines.Add("필요 장비: " + d.combat.gear.Single(value => value.id == definition.requiredGearId).name);
            int gold = CompetitionEncounterRules.RewardGold(d, p, choice);
            string reward = "골드 +" + gold;
            if (d.v4.treasureEncounterIds.Contains(choice.id)) reward += " (기본 " + choice.gold + " + 행운 +" + (gold - choice.gold) + ")";
            lines.Add((choice.kind == EncounterKind.Combat ? "승리 보상: " : "획득: ") + reward + " · 경험치 +" + choice.xp + " · 명예 +" + choice.honor);
            if (choice.heal > 0) lines.Add("HP 회복 +" + choice.heal + " (최대 HP까지)");
            if (choice.kind == EncounterKind.Gear) lines.Add(Gear(d, p, choice.rewardId));
            if (choice.kind == EncounterKind.Die) lines.Add("첫 번째 주사위 교체\n" + Die(d, p.adventurerId, choice.rewardId));
            if (choice.kind == EncounterKind.Combat)
            {
                var enemy = d.v4.enemies.Single(value => value.encounterId == choice.id);
                lines.Add("적 HP " + choice.enemyHp + " · 공격 " + choice.enemyAttack + " · " + CompetitionElements.TypeName(enemy.attackType));
                lines.Add("약점 " + (enemy.weakness.HasValue ? CompetitionElements.TypeName(enemy.weakness.Value) + " +" + Percent(d.v4.weaknessBasisPoints) : "없음") +
                    " · 저항 " + (enemy.resistance.HasValue ? CompetitionElements.TypeName(enemy.resistance.Value) + " −" + Percent(d.v4.resistanceBasisPoints) : "없음"));
            }
            if (!choice.available) lines.Add("선택 불가: " + choice.unavailableReason);
            return Friendly(d, string.Join("\n", lines));
        }

        public static string Lap(CompetitionState state, string choiceId)
        {
            var p = CompetitionRules.Human(state); var d = state.definition;
            var after = CompetitionCommandRules.PreviewLapReward(state, choiceId);
            var lines = new List<string> { "선택 즉시 보상을 받고 다음 단계로 진행합니다.",
                $"골드 {p.gold} → {after.gold} (+{after.gold - p.gold}) · 경험치 +{d.combat.lapXp}",
                $"레벨 {p.level} → {after.level} · 남은 경험치 {p.xp} → {after.xp}",
                $"HP {p.hp}/{p.maxHp} → {after.hp}/{after.maxHp} (현재 HP +{after.hp - p.hp})" };
            if (after.completedLaps < d.lapsToFinish)
            {
                lines.Add($"주사위 {p.dice.Count} → {after.dice.Count}개\n" + Die(d, p.adventurerId, after.dice.Last().definitionId));
                lines.Add("보상 후 AI 차례가 진행됩니다.");
            }
            else { lines.Add(Gear(d, p, choiceId)); lines.Add("보상 후 최종 시련에 바로 진입합니다. AI 차례는 시련 종료 후 진행됩니다."); }
            return string.Join("\n", lines);
        }

        public static string Results(CompetitionState state)
        {
            var ranked = CompetitionRules.Rankings(state);
            var lines = new List<string> { state.finished ? "최종 순위 · " + Friendly(state.definition, state.endReason) : "현재 순위 · AI 모험 진행 중",
                "시련 성공 → 완주 → 명예 → 남은 HP 순서로 순위를 정합니다.", "네 기준이 모두 같으면 공동 순위입니다." };
            foreach (var p in ranked)
            {
                bool tied = ranked.Count(other => other.rank == p.rank) > 1;
                lines.Add((tied ? "공동 " : "") + p.rank + "위 · " + p.name + (p.isHuman ? " (나)" : ""));
                lines.Add((p.trialSucceeded ? "시련 성공" : "시련 미성공") + $" · 완주 {p.completedLaps} · 명예 {p.honor} · HP {p.hp}/{p.maxHp}");
                lines.Add(p.eliminated ? "탈락 · " + p.message : p.finished ? "모험 완료 · " + p.message : state.finished ? "경기 종료 · 라운드 제한" : "진행 중 · " + p.message);
            }
            if (!state.finished) lines.Add("내 모험 종료 후에도 ‘다음 AI 라운드’로 전체 경기를 진행할 수 있습니다.");
            return Friendly(state.definition, string.Join("\n", lines));
        }

        public static string Friendly(CompetitionDefinition d, string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            var names = new Dictionary<string, string>(StringComparer.Ordinal) {
                { "all-complete", "전원 모험 완료·탈락" }, { "round-limit", "라운드 제한 도달" },
                { "enemy-defeated", "앞선 기술로 적 처치 · 생략" }, { "await-roll", "굴림 대기" }, { "no-op", "발동 기술 없음" } };
            foreach (var tag in d.v4.tags) names[tag.id] = tag.name;
            foreach (var face in d.v5.faces) names[face.id] = face.name;
            foreach (var hero in d.adventurers) names[hero.id] = hero.name;
            foreach (var skill in d.v5.skills) names[skill.id] = skill.name;
            foreach (var die in d.combat.dice) names[die.id] = die.name;
            foreach (var gear in d.combat.gear) names[gear.id] = gear.name.Split(new[] { " · " }, StringSplitOptions.None)[0];
            foreach (var pair in names.OrderByDescending(pair => pair.Key.Length))
                text = Regex.Replace(text, @"(?<![A-Za-z0-9_.-])" + Regex.Escape(pair.Key) + @"(?![A-Za-z0-9_.-])", match => pair.Value);
            return text;
        }

        static string Query(CompetitionDefinition d, TagQuery query)
        {
            var parts = new List<string>();
            if (query.all.Length > 0) parts.Add(string.Join(" + ", query.all));
            if (query.any.Length > 0) parts.Add("(" + string.Join(" 또는 ", query.any) + ")");
            if (query.none.Length > 0) parts.Add(string.Join("·", query.none) + " 제외");
            return Friendly(d, parts.Count == 0 ? "모든 대상" : string.Join(" · ", parts)) + (query.includeChildren ? " (하위 분류 포함)" : "");
        }
        static string Target(string target)
        {
            foreach (AttributeStat stat in Enum.GetValues(typeof(AttributeStat)))
                if (CompetitionAttributes.Target(stat) == target) return CompetitionAttributes.Label(stat);
            return target == "damage" ? "피해" : target == "block" ? "보호" : target == "resistance" ? "피해 저항" : target;
        }
        static string Percent(int basisPoints) => (basisPoints / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        static string Amount(V4Binding binding) => binding.operation == ModifierOperation.FinalMultiply
            ? "×" + (binding.amount / 10000m).ToString("0.####", CultureInfo.InvariantCulture)
            : (binding.amount >= 0 ? "+" : "") + (binding.operation == ModifierOperation.PercentAdd || binding.target == "resistance"
                ? Percent(binding.amount) : binding.amount.ToString(CultureInfo.InvariantCulture));
    }
}
