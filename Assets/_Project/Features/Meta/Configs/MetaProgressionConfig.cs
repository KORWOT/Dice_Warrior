using System;
using System.Linq;
using UnityEngine;

namespace FateDice
{
    [CreateAssetMenu(menuName = "Fate Dice/메타 성장 설정")]
    public sealed class MetaProgressionConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/_Project/Features/Meta/Configs/DefaultMetaProgression.asset";
        [Tooltip("개발용 초기 재화. 기존 프로필에는 소급 적용하지 않습니다.")]
        public int startingCurrency;
        public string[] startingEquipmentIds = Array.Empty<string>();
        public string[] extraDieIds = Array.Empty<string>();
        public MetaPolicy policy = new MetaPolicy();

        public PlayerProfileData CreateProfile(string profileId, GameConfigData rules)
        {
            policy.Validate();
            ProfileRules.Require(startingCurrency >= 0 && startingEquipmentIds != null && extraDieIds != null,
                "초기 보유품과 성장 재화를 확인해 주세요.");
            ProfileRules.Require(startingEquipmentIds.All(id => rules.growth.equipment.Any(x => x.id == id)) &&
                extraDieIds.All(id => rules.dice.dice.Any(x => x.id == id)), "초기 보유품의 정의를 찾지 못했습니다.");
            var dice = Enumerable.Repeat(rules.dice.basicDieId, 6).Concat(extraDieIds)
                .Select((id, i) => new OwnedItem { instanceId = "starter-die-" + i, definitionId = id }).ToArray();
            var p = new PlayerProfileData { profileId = profileId, growthCurrency = startingCurrency,
                characters = new[] { new OwnedCharacter { characterId = rules.growth.characterId } }, dice = dice,
                equipment = startingEquipmentIds.Select((id, i) => new OwnedItem { instanceId = "starter-gear-" + i, definitionId = id }).ToArray(),
                wildcards = (string[])rules.combat.trialActionIds.Clone(),
                loadout = new RunLoadout { characterId = rules.growth.characterId, wildcardId = rules.combat.trialActionIds[0],
                    dice = dice.Take(6).Select(x => x.instanceId).ToArray() } };
            ProfileRules.Validate(p); ProfileRules.ValidateLoadout(p, p.loadout, rules); return p;
        }
    }
}
