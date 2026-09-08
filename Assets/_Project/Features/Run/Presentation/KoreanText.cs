using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FateDice
{
    // Presentation only. Saved labels, rule messages, enum values and stable IDs stay unchanged.
    public static class KoreanText
    {
        static readonly Dictionary<string, string> DefaultContent = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Plain D6", "기본 주사위" },
            { "Ember D6", "불씨 주사위" },
            { "Pair", "원 페어" },
            { "Two pairs", "투 페어" },
            { "Triple", "트리플" },
            { "Straight", "스트레이트" },
            { "Full house", "풀하우스" },
            { "Three pairs", "트리플 페어" },
            { "Four of a kind", "포카드" },
            { "Full straight", "풀 스트레이트" },
            { "Five of a kind", "파이브 카드" },
            { "Six of a kind", "식스 카드" },
            { "Strike", "공격" },
            { "Guard", "방어" },
            { "Heavy strike", "강공격" },
            { "Fireball", "화염구" },
            { "Bastion", "철벽" },
            { "Road bandit", "길목 도적" },
            { "Stone sentry", "돌 파수꾼" },
            { "Fate keeper", "운명의 수호자" },
            { "Attack", "공격" },
            { "Defend", "방어" },
            { "Heavy attack", "강공격" },
            { "Wanderer", "방랑자" },
            { "attack", "공격" },
            { "physical", "물리" },
            { "defense", "방어" },
            { "fire", "불" },
            { "magic", "마법" },
            { "projectile", "투사체" },
            { "steel", "강철" },
            { "luck", "행운" },
            { "Ember blade", "불씨 검" },
            { "Guard plate", "수호 판금" },
            { "Traveler charm", "여행자 부적" },
            { "Road encounter", "길목의 적" },
            { "Defeat the enemy to claim the spoils.", "적을 쓰러뜨리고 전리품을 획득하세요." },
            { "The wishing stone", "소원의 돌" },
            { "A sharp toll buys insight and the power to reroll.", "체력을 대가로 깨달음과 재굴림 능력을 얻습니다." },
            { "Abandoned cache", "버려진 보물" },
            { "Take the coins and inspect a piece of equipment.", "골드를 챙기고 장비를 확인하세요." },
            { "Wayfarer's stall", "여행자의 가판대" },
            { "Spend run coins, or leave when ready.", "골드로 물건을 구매하거나 길을 떠나세요." },
            { "Quiet camp", "고요한 야영지" },
            { "Recover health or train for experience and a reroll charge.", "체력을 회복하거나 훈련하여 경험치와 재굴림 횟수를 얻으세요." },
            { "Healing draught", "회복 물약" },
            { "Reroll training + 3 charges", "재굴림 훈련 + 3회" },
            { "Ember die (replace one)", "불씨 주사위 (1개 교체)" }
        };

        static readonly Dictionary<string, string> FixedNotices = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Choose a connected path. Every resolved normal event advances Great Fate once.", "연결된 경로를 선택하세요. 일반 사건을 해결할 때마다 대운명이 한 단계 진행됩니다." },
            { "Choose one action. Every offered action can be played.", "행동 카드 1장을 선택하세요. 제시된 행동은 모두 사용할 수 있습니다." },
            { "Choose one fate. Only type and grade are revealed until selection.", "운명 카드 1장을 선택하세요. 선택 전에는 유형과 등급만 공개됩니다." },
            { "The Fate keeper awaits. The next intent is already visible.", "운명의 수호자가 기다립니다. 적의 다음 행동이 공개되어 있습니다." },
            { "The outcome is fixed. Claim it to continue.", "결과가 확정되었습니다. 보상을 받아 계속 진행하세요." },
            { "Die replacement declined.", "주사위를 교체하지 않았습니다." },
            { "Victory! The Fate keeper falls. One section cleared.", "승리! 운명의 수호자를 쓰러뜨리고 구간을 완료했습니다." }
        };

        public static string Content(string text)
        {
            if (text == null) return null;
            return DefaultContent.TryGetValue(text, out var translated) ? translated : text;
        }

        public static string HandStage(RunState state)
        {
            if (state == null || state.dice == null) return "주사위를 굴려 확인";
            var hand = state.config.dice.hands.Single(definition => definition.kind == state.hand);
            int stage = state.config.dice.hands.Count(definition => definition.priority < hand.priority) + 1;
            return Content(hand.label) + " · 조합 단계 " + stage + "/" + state.config.dice.hands.Length;
        }

        public static string HandSummary(RunState state, bool multiline = false)
        {
            if (state == null || state.dice == null) return "주사위를 굴려 확인";
            var hand = state.config.dice.hands.Single(definition => definition.kind == state.hand);
            int stage = state.config.dice.hands.Count(definition => definition.priority < hand.priority) + 1;
            return Content(hand.label) + (multiline ? "\n" : " · ") + "조합 단계 " + stage + "/" +
                state.config.dice.hands.Length + " · 운명력 " + state.fatePower;
        }

        public static string Grade(global::FateDice.Grade grade)
        {
            switch (grade)
            {
                case global::FateDice.Grade.Common: return "일반";
                case global::FateDice.Grade.Uncommon: return "고급";
                case global::FateDice.Grade.Rare: return "희귀";
                case global::FateDice.Grade.Epic: return "영웅";
                case global::FateDice.Grade.Legendary: return "전설";
                default: return grade.ToString();
            }
        }

        public static string Node(NodeType type)
        {
            switch (type)
            {
                case NodeType.Combat: return "전투";
                case NodeType.Event: return "사건";
                case NodeType.Treasure: return "보물";
                case NodeType.Shop: return "상점";
                case NodeType.Rest: return "휴식";
                case NodeType.Boss: return "대운명";
                default: return type.ToString();
            }
        }

        public static string Slot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "무기";
                case EquipmentSlot.Armor: return "방어구";
                case EquipmentSlot.Accessory: return "장신구";
                default: return slot.ToString();
            }
        }

        public static string Tag(string id)
        {
            switch (id)
            {
                case "attack": return "공격";
                case "physical": return "물리";
                case "defense": return "방어";
                case "fire": return "불";
                case "magic": return "마법";
                case "projectile": return "투사체";
                case "steel": return "강철";
                case "luck": return "행운";
                default: return id;
            }
        }

        public static string Match(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.Any: return "하나 이상 포함";
                case MatchMode.All: return "모두 포함";
                case MatchMode.None: return "모두 미포함";
                default: return mode.ToString();
            }
        }

        public static string Source(TagSource source)
        {
            switch (source)
            {
                case TagSource.Card: return "행동 카드 태그";
                case TagSource.Equipment: return "장착 장비 태그";
                default: return source.ToString();
            }
        }

        public static string Value(ModifiedValue value)
        {
            switch (value)
            {
                case ModifiedValue.Damage: return "피해";
                case ModifiedValue.Block: return "수호 획득량";
                default: return value.ToString();
            }
        }

        public static string Notice(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // FinishEvent appends this suffix to a known reward, equipment or shop message.
            var resolved = LegacyMatch(text, @"\A(?<body>.+) Event resolved: (?<done>[0-9]+)/(?<total>[0-9]+)\.\z");
            if (resolved.Success && TryNotice(resolved.Groups["body"].Value, out var body))
                return body + " 사건 해결: " + resolved.Groups["done"].Value + "/" + resolved.Groups["total"].Value + ".";

            return TryNotice(text, out var translated) ? translated : text;
        }

        static bool TryNotice(string text, out string translated)
        {
            if (FixedNotices.TryGetValue(text, out translated) || DefaultContent.TryGetValue(text, out translated))
                return true;

            var match = LegacyMatch(text, @"\ADie (?<index>[1-6]) rerolled\. New cards are fixed; (?<charges>[0-9]+) charges remain\.\z");
            if (match.Success)
            {
                translated = match.Groups["index"].Value + "번 주사위를 다시 굴렸습니다. 새 카드가 확정되었으며 재굴림 " + match.Groups["charges"].Value + "회가 남았습니다.";
                return true;
            }

            match = LegacyMatch(text, @"\AChosen path: (?<type>Combat|Event|Treasure|Shop|Rest|Boss)\. Roll for fate cards\.\z");
            if (match.Success)
            {
                var type = (NodeType)Enum.Parse(typeof(NodeType), match.Groups["type"].Value);
                translated = "선택한 경로: " + Node(type) + ". 주사위를 굴려 운명 카드를 확인하세요.";
                return true;
            }

            match = LegacyMatch(text, @"\AClaimed: (?<gold>-?[0-9]+) coins, (?<xp>-?[0-9]+) XP, HP (?<hp>[+-][0-9]+)\.(?<fatal> The price was fatal\.)?\z");
            if (match.Success)
            {
                translated = "보상 획득: 골드 " + match.Groups["gold"].Value + ", 경험치 " + match.Groups["xp"].Value + ", 체력 " + match.Groups["hp"].Value + ".";
                if (match.Groups["fatal"].Success) translated += " 대가를 치르다 쓰러졌습니다.";
                return true;
            }

            match = LegacyMatch(text, @"\A(?<equipped>Equipped |Left behind )(?<name>.+)\.\z");
            if (match.Success)
            {
                translated = (match.Groups["equipped"].Value == "Equipped " ? "장착: " : "두고 온 장비: ") + Content(match.Groups["name"].Value) + ".";
                return true;
            }

            match = LegacyMatch(text, @"\ADie (?<index>[1-6]) replaced with (?<name>.+)\.\z");
            if (match.Success)
            {
                translated = match.Groups["index"].Value + "번 주사위 교체: " + Content(match.Groups["name"].Value) + ".";
                return true;
            }

            match = LegacyMatch(text, @"\APurchased (?<name>.+)\. Claim the item\.\z");
            if (match.Success)
            {
                translated = "구매: " + Content(match.Groups["name"].Value) + ". 물품을 받으세요.";
                return true;
            }

            match = LegacyMatch(text, @"\AExploration cap: (?<grade>Common|Uncommon|Rare|Epic|Legendary)\. Combat grades are unchanged\.\z");
            if (match.Success)
            {
                var grade = (global::FateDice.Grade)Enum.Parse(typeof(global::FateDice.Grade), match.Groups["grade"].Value);
                translated = "탐험 등급 상한: " + Grade(grade) + ". 전투 등급에는 영향을 주지 않습니다.";
                return true;
            }

            match = LegacyMatch(text, @"\A(?<name>.+): (?<damage>[0-9]+) damage, (?<block>[0-9]+) shield\.(?:(?<victory> Victory! Claim the earned reward\.)| Enemy gains (?<enemyBlock>[0-9]+) shield\.| Enemy deals (?<enemyDamage>[0-9]+) damage\.(?<ended> The journey ends\.)?)?\z");
            if (match.Success)
            {
                translated = Content(match.Groups["name"].Value) + ": 피해 " + match.Groups["damage"].Value + ", 수호 " + match.Groups["block"].Value + ".";
                if (match.Groups["victory"].Success) translated += " 승리! 보상을 받으세요.";
                if (match.Groups["enemyBlock"].Success) translated += " 적이 수호 " + match.Groups["enemyBlock"].Value + " 획득.";
                if (match.Groups["enemyDamage"].Success) translated += " 적이 " + match.Groups["enemyDamage"].Value + "의 피해를 주었습니다.";
                if (match.Groups["ended"].Success) translated += " 여정이 끝났습니다.";
                return true;
            }

            translated = null;
            return false;
        }

        static System.Text.RegularExpressions.Match LegacyMatch(string text, string pattern) =>
            Regex.Match(text, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}
