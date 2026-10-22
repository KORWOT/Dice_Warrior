using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    // Read-only presentation of frozen match values; no RNG, commands or persistence.
    public static class CompetitionGrowthView
    {
        public static void Roster(CompetitionScreenData data,CompetitionDefinition definition,Profile profile,string selected)
        {
            data.growthMode="adventurers";data.growthTitle="후원할 모험가 선택";
            data.growthSummary="기본 제공 3종 · 선택은 다음 모험에 적용됩니다.";
            data.adventurerCards=definition.adventurers.Select(a=>
            {
                var value=CompetitionStats.Preview(definition,profile,a.id);
                return (a.id==selected?"[선택] ":"")+a.name+"\n"+$"시작 HP {value.maxHp} · 무력 {value.strength} / 수호 {value.guard} / 비전 {value.arcane}\n"+value.passiveDescription;
            }).ToArray();
        }
        public static void Details(CompetitionScreenData data,CompetitionDefinition definition,Profile profile,ParticipantState participant,string selected,bool skills)
        {
            bool modern=CompetitionStats.IsEnabled(definition);
            data.growthMode=skills?"skills":modern?"stats":"legacy";
            var value=modern?(participant==null?CompetitionStats.Preview(definition,profile,selected):CompetitionStats.Describe(definition,participant)):null;
            data.growthTitle=participant==null?"다음 모험 시작 능력치":"현재 모험가 능력치";
            if(value!=null)
            {
                data.growthSummary=$"{value.name} · 모험가 Lv.{participant?.level ?? 1} · HP {value.hp}/{value.maxHp}\n{value.passiveDescription}";
                data.statValues=new[]{$"무력  STR   {value.strength}",$"수호  GRD   {value.guard}",$"비전  ARC   {value.arcane}"};
                data.statSources=new[]{$"기본 {value.baseStrength} + 런 레벨 {value.levelBonus} = {value.strength}",
                    $"기본 {value.baseGuard} + 런 레벨 {value.levelBonus} = {value.guard}",$"기본 {value.baseArcane} + 런 레벨 {value.levelBonus} = {value.arcane}"};
                data.growthBody=$"최대 HP: 기본 {value.baseHp} + 기존 계정 +{value.accountHp}\n+ 런 레벨 {value.levelHp} = {value.maxHp}\n\n장비는 기술별 피해·보호막에 적용됩니다.\n"+GearText(definition,profile,participant)+"\n\n신 권능·임시 스탯 보정: 없음";
            }
            else
            {
                data.growthSummary="기존 규칙 경기 · 시작 시 동결된 능력치를 유지합니다.";
                data.growthBody=$"{participant.name} · 모험가 Lv.{participant.level}\nHP {participant.hp}/{participant.maxHp}\n위력 {participant.power} · 수호 {participant.guard}\n\n새 RGB 규칙은 새 게임부터 적용됩니다.\n\n"+GearText(definition,profile,participant);
            }
            if(skills)
            {
                var dice=participant?.dice.Select(d=>d.definitionId).ToArray() ?? new[]{profile.startingDie};
                var definitions=dice.Select(id=>definition.combat.dice.Single(d=>d.id==id)).ToArray();
                data.growthBody="기호 주사위\n"+string.Join("\n",definitions.Select((d,i)=>$"{i+1}. {d.name}: "+string.Join(" / ",d.faces.Select(SymbolName))))+"\n\n현재 가능한 기술\n";
                var possible=new HashSet<string>(StringComparer.Ordinal);var faces=new Symbol[definitions.Length];
                void Visit(int index)
                {
                    if(index==faces.Length) {foreach(var skill in SymbolCombat.Plan(definition.combat,faces,definition.rulesVersion!=CompetitionContent.LegacyVersion)) possible.Add(skill.id);return;}
                    foreach(var face in definitions[index].faces.Distinct()) {faces[index]=face;Visit(index+1);}
                }
                Visit(0);
                data.growthBody+=string.Join("\n",definition.combat.basics.Concat(definition.combat.combos).Where(s=>possible.Contains(s.id)).Select(s=>s.name+" · "+(modern?CompetitionStats.ScalingText(definition,s.id):"기존 위력/수호 규칙")));
            }
        }
        static string GearText(CompetitionDefinition definition,Profile profile,ParticipantState participant)
        {
            var ids=participant?.equipment ?? new[]{profile.startingWeapon,null,null};
            return string.Join("\n",ids.Select((id,i)=>
            {
                string slot=(new[]{"무기","방어구","장신구"})[i]+": ";
                if(id==null) return slot+"없음";
                var gear=definition.combat.gear.Single(g=>g.id==id);
                string tag=gear.tag=="steel"?"강철":gear.tag=="arcane"?"비전":gear.tag=="fire"?"불":"수호";
                string name=gear.name.Split(new[]{" · "},StringSplitOptions.None)[0];
                return slot+name+" · "+tag+(gear.damage>0?$" 피해 +{gear.damage}":"")+(gear.block>0?$" 보호 +{gear.block}":"");
            }));
        }
        static string SymbolName(Symbol symbol)=>new[]{"검","지팡이","불","방패"}[(int)symbol];
    }
}
