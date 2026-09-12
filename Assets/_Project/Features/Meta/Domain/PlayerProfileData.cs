using System;
using System.Linq;

namespace FateDice
{
    [Serializable] public sealed class OwnedItem
    {
        public string instanceId, definitionId;
        public OwnedItem Copy() => new OwnedItem { instanceId = instanceId, definitionId = definitionId };
    }
    [Serializable] public sealed class OwnedCharacter
    {
        public string characterId;
        public int rank;
        public OwnedCharacter Copy() => new OwnedCharacter { characterId = characterId, rank = rank };
    }
    [Serializable] public sealed class RunLoadout
    {
        public string characterId, wildcardId;
        public string[] equipment = new string[3], dice = new string[6];
        public Grade cap = Grade.Legendary;
        public RunLoadout Copy() => new RunLoadout { characterId = characterId, wildcardId = wildcardId,
            equipment = (string[])equipment.Clone(), dice = (string[])dice.Clone(), cap = cap };
    }
    [Serializable] public sealed class PlayerProfileData
    {
        public const int CurrentSchema = 1;
        public int schema = CurrentSchema;
        public string profileId, authority = "LocalDevelopment";
        public long revision, growthCurrency;
        public OwnedCharacter[] characters = Array.Empty<OwnedCharacter>();
        public OwnedItem[] equipment = Array.Empty<OwnedItem>(), dice = Array.Empty<OwnedItem>();
        public string[] wildcards = Array.Empty<string>();
        public RunLoadout loadout;
        public PlayerProfileData Copy() => new PlayerProfileData { schema = schema, profileId = profileId,
            authority = authority, revision = revision, growthCurrency = growthCurrency,
            characters = characters.Select(x => x.Copy()).ToArray(), equipment = equipment.Select(x => x.Copy()).ToArray(),
            dice = dice.Select(x => x.Copy()).ToArray(), wildcards = (string[])wildcards.Clone(), loadout = loadout.Copy() };
    }
    [Serializable] public sealed class MetaPolicy
    {
        public string version = "local-meta-1";
        public int currencyPerEvent = 1, victoryBonus = 10, maxRank = 20, rankCost = 10;
        public int hpPerRank = 2, powerPerRank = 1, guardPerRank;
        public MetaPolicy Copy() => (MetaPolicy)MemberwiseClone();
        public long Cost(int currentRank) => checked((long)rankCost * (currentRank + 1));
        public void Validate()
        {
            ProfileRules.Require(!string.IsNullOrWhiteSpace(version) && currencyPerEvent >= 0 && victoryBonus >= 0 &&
                maxRank > 0 && maxRank <= 1000 && rankCost > 0 && hpPerRank >= 0 && powerPerRank >= 0 && guardPerRank >= 0,
                "성장 정책의 버전·비용·효과를 확인해 주세요.");
        }
    }
    [Serializable] public sealed class RunStartSnapshot
    {
        public string runId, profileId, rulesVersion;
        public long profileRevision;
        public int characterRank;
        public RunLoadout loadout;
        public MetaPolicy policy;
        public RunStartSnapshot Copy() => new RunStartSnapshot { runId = runId, profileId = profileId,
            rulesVersion = rulesVersion, profileRevision = profileRevision, characterRank = characterRank,
            loadout = loadout.Copy(), policy = policy.Copy() };
    }
    public sealed class MetaCommandException : InvalidOperationException
    {
        public MetaCommandException(string message) : base(message) { }
    }
    public static class ProfileRules
    {
        public static void Require(bool condition, string message)
        { if (!condition) throw new MetaCommandException(message); }

        public static void Validate(PlayerProfileData p)
        {
            Require(p != null && p.schema == PlayerProfileData.CurrentSchema, "지원하지 않는 프로필 버전입니다.");
            Require(!string.IsNullOrWhiteSpace(p.profileId) && p.authority == "LocalDevelopment" && p.revision >= 0 && p.growthCurrency >= 0,
                "프로필 식별자·권한·재화가 올바르지 않습니다.");
            Require(p.characters != null && p.characters.Length > 0 && p.characters.All(x => x != null && x.rank >= 0), "캐릭터 데이터가 올바르지 않습니다.");
            Unique(p.characters.Select(x => x.characterId).ToArray());
            Items(p.equipment); Items(p.dice); Unique(p.wildcards);
            Unique(p.equipment.Concat(p.dice).Select(x => x.instanceId).ToArray());
            ValidateOwnership(p, p.loadout);
        }
        public static void ValidateOwnership(PlayerProfileData p, RunLoadout l)
        {
            Require(l != null && p.characters.Any(x => x.characterId == l.characterId) && p.wildcards.Contains(l.wildcardId),
                "보유한 캐릭터와 와일드 카드를 선택해 주세요.");
            Require(Enum.IsDefined(typeof(Grade), l.cap) && l.equipment != null && l.equipment.Length == 3 && l.dice != null && l.dice.Length == 6,
                "장비 3칸과 주사위 6개, 탐험 등급을 확인해 주세요.");
            Unique(l.dice); Unique(l.equipment.Where(x => !string.IsNullOrEmpty(x)).ToArray());
            Require(l.dice.All(id => p.dice.Any(x => x.instanceId == id)) &&
                l.equipment.All(id => string.IsNullOrEmpty(id) || p.equipment.Any(x => x.instanceId == id)), "보유하지 않은 장비 또는 주사위입니다.");
        }
        public static void ValidateLoadout(PlayerProfileData p, RunLoadout l, RunRulesCatalog rules)
        {
            ValidateOwnership(p, l);
            Require(l.characterId == rules.growth.characterId && rules.combat.trialActionIds.Contains(l.wildcardId), "현재 콘텐츠에서 지원하지 않는 출전 구성입니다.");
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrEmpty(l.equipment[i])) continue;
                var item = p.equipment.Single(x => x.instanceId == l.equipment[i]);
                Require(rules.growth.equipment.Any(x => x.id == item.definitionId && (int)x.slot == i), "장비 종류와 장착 슬롯이 맞지 않습니다.");
            }
            Require(l.dice.All(id => rules.dice.dice.Any(d => d.id == p.dice.Single(x => x.instanceId == id).definitionId)), "사용할 주사위 정의를 찾지 못했습니다.");
        }
        static void Items(OwnedItem[] items)
        {
            Require(items != null && items.All(x => x != null && !string.IsNullOrWhiteSpace(x.definitionId)), "보유품 데이터가 올바르지 않습니다.");
            Unique(items.Select(x => x.instanceId).ToArray());
        }
        static void Unique(string[] values)
        {
            Require(values != null && values.All(x => !string.IsNullOrWhiteSpace(x)) && values.Distinct(StringComparer.Ordinal).Count() == values.Length,
                "식별자가 없거나 같은 개체가 중복되었습니다.");
        }
    }
}
