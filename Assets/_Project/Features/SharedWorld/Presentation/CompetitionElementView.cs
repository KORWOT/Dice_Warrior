using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    // Read-only v4 views. Preview participants are detached display inputs, never saved or rolled.
    public static class CompetitionElementView
    {
        static readonly AttributeStat[] Stats=(AttributeStat[])Enum.GetValues(typeof(AttributeStat));
        static readonly string[] Abbreviations={"STR","DEX","CON","INT","WIL","LUK"};
        public static void Roster(CompetitionScreenData data,CompetitionDefinition definition,Profile profile,string selected)
        {
            data.growthMode="adventurers";data.growthTitle="후원할 모험가 선택";
            data.growthSummary="기본 제공 3종 · 선택은 다음 모험에 적용됩니다.";
            data.adventurerCards=definition.adventurers.Select(a=>
            {
                var value=CompetitionAttributes.Preview(definition,profile,a.id);var v=value.attributes;
                return (a.id==selected?"[선택] ":"")+a.name+$" · 시작 HP {value.maxHp}\n"+
                    $"힘 {v.strength} · 민첩 {v.dexterity} · 체질 {v.constitution}\n지능 {v.intelligence} · 정신 {v.will} · 행운 {v.luck}\n"+value.passiveDescription;
            }).ToArray();
        }
        public static int Details(CompetitionScreenData data,CompetitionDefinition definition,Profile profile,ParticipantState participant,string selected,string section,int page)
        {
            var value=participant==null?CompetitionAttributes.Preview(definition,profile,selected):CompetitionAttributes.Describe(definition,participant);
            var shown=participant??PreviewParticipant(profile,selected,value);
            data.growthSummary=$"{value.name} · 모험가 Lv.{value.level} · HP {value.hp}/{value.maxHp}\n{value.passiveDescription}";
            if(section=="stats")
            {
                data.growthMode="attributes";data.growthTitle=participant==null?"다음 모험 · 6가지 능력치":"현재 모험 · 6가지 능력치";
                data.statValues=Stats.Select((s,i)=>$"{CompetitionAttributes.Label(s)}  {Abbreviations[i]}  {value.attributes.Get(s)}").ToArray();
                data.statSources=Stats.Select(s=>
                {
                    int effect=value.attributes.Get(s)-value.baseStats.Get(s)-value.levelBonus;
                    return $"기본 {value.baseStats.Get(s)} + 레벨 {value.levelBonus} + 효과 {effect}\n= {value.attributes.Get(s)}";
                }).ToArray();
                data.growthBody=$"최대 HP: 기본 {value.baseHp} + 체질 {value.constitutionHp}\n+ 기존 계정 {value.accountHp} = {value.maxHp}\n"+
                    "레벨마다 6스탯 +1 · HP는 체질 증가분만 반영\n"+
                    $"행운: 보물 골드 +{value.attributes.luck/5} (이번 경기)\n치명타 0% · 회피 0%\n장비·고유 능력의 조건별 효과는 ‘효과 출처’에서 확인";
                return 1;
            }
            data.growthMode="element-text";
            var pages=new List<string>();string title;
            if(section=="skills") { title="주사위 · 기술의 타입과 계수";SkillPages(pages,definition,shown); }
            else if(section=="elements") { title="속성 · 약점과 저항";pages.Add(ElementText(definition,shown)); }
            else if(section=="tags") { title="태그 · 소유 대상과 출처";TagPages(pages,definition,shown); }
            else { title="효과 출처 · 적용 조건";ModifierPages(pages,definition,shown,value); }
            if(pages.Count==0) pages.Add("현재 표시할 항목이 없습니다.");
            page=Math.Max(0,Math.Min(page,pages.Count-1));
            data.growthTitle=title+$"  {page+1}/{pages.Count}";data.growthBody=pages[page];return pages.Count;
        }
        static ParticipantState PreviewParticipant(Profile profile,string selected,V4StatBreakdown value)
            =>new ParticipantState {id="preview",adventurerId=selected,level=1,hp=value.hp,maxHp=value.maxHp,
                v4=new V4ParticipantState {accountHp=value.accountHp},equipment=new[]{profile.startingWeapon,null,null},
                dice=new List<DieSlot>{new DieSlot {instanceId="preview-die",definitionId=profile.startingDie}}};
        static Skill[] Possible(CompetitionDefinition definition,ParticipantState p)
        {
            var dice=p.dice.Select(d=>definition.combat.dice.Single(x=>x.id==d.definitionId)).ToArray();
            var ids=new HashSet<string>(StringComparer.Ordinal);var faces=new Symbol[dice.Length];
            void Visit(int index)
            {
                if(index==faces.Length) {foreach(var skill in SymbolCombat.Plan(definition.combat,faces,true)) ids.Add(skill.id);return;}
                foreach(var face in dice[index].faces.Distinct()) {faces[index]=face;Visit(index+1);}
            }
            Visit(0);return definition.combat.basics.Concat(definition.combat.combos).Where(s=>ids.Contains(s.id)).ToArray();
        }
        static void SkillPages(List<string> pages,CompetitionDefinition definition,ParticipantState p)
        {
            string dice="보유 주사위\n"+string.Join("\n",p.dice.Select((d,i)=>
            {
                var die=definition.combat.dice.Single(x=>x.id==d.definitionId);
                return $"{i+1}. {die.name}: "+string.Join(" / ",die.faces.Select(SymbolName));
            }));
            var skills=Possible(definition,p);var enemy=CompetitionElements.Enemy(definition,p);
            for(int offset=0;offset<skills.Length;offset+=3)
            {
                var lines=new List<string>{dice,"",enemy==null?"가능한 기술 · 중립 대상 예상":"가능한 기술 · 현재 적 상성 반영"};
                foreach(var skill in skills.Skip(offset).Take(3))
                {
                    var entry=definition.v4.skills.Single(s=>s.skillId==skill.id);
                    var result=CompetitionElements.SkillPreview(definition,p,skill.id,enemy);
                    lines.Add(skill.name+" · "+CompetitionElements.TypeName(result.damageType));
                    if(skill.damage>0) lines.Add($"피해 {skill.damage} + {CompetitionElements.ScalingText(entry.damageScaling)} → {result.damageBeforeShield}");
                    if(skill.block>0) lines.Add($"보호 {skill.block} + {CompetitionElements.ScalingText(entry.blockScaling)} → {result.block}");
                    lines.Add(ConversionText(result)+AffinityText(result.affinityBasisPoints));
                }
                pages.Add(string.Join("\n",lines));
            }
        }
        static string ElementText(CompetitionDefinition definition,ParticipantState p)
        {
            var enemy=CompetitionElements.Enemy(definition,p);
            string target=enemy==null?"현재 전투 상대 없음 · 기술 예상은 중립 기준":EnemyText(definition,enemy);
            var lines=new List<string>{target,"","내 피해 저항"};
            foreach(DamageType type in Enum.GetValues(typeof(DamageType)))
                lines.Add(CompetitionElements.TypeName(type)+"  "+Percent(CompetitionElements.Resistance(definition,p,type)));
            lines.Add("");lines.Add("속성별 피해 보정은 기술·장비 조건에 따라 적용됩니다.");
            lines.Add("기호는 조합을, 피해 타입은 상성을 결정합니다.");
            lines.Add("치명타 0% · 회피 0% · 태그만으로 효과가 생기지 않습니다.");
            if(p.v4.lastTurn!=null)
            {
                lines.Add("");lines.Add("마지막 전투 · 저장된 결과");
                foreach(var result in p.v4.lastTurn.skills.Take(3))
                {
                    string name=definition.combat.basics.Concat(definition.combat.combos).Single(s=>s.id==result.skillId).name;
                    lines.Add(name+" · "+CompetitionElements.TypeName(result.damageType)+(result.status=="applied"?$" · HP 피해 {result.damageApplied}":" · 처치 후 생략"));
                }
            }
            return string.Join("\n",lines);
        }
        static void TagPages(List<string> pages,CompetitionDefinition definition,ParticipantState p)
        {
            foreach(var skill in Possible(definition,p))
            {
                var result=CompetitionElements.SkillPreview(definition,p,skill.id,CompetitionElements.Enemy(definition,p));
                var snapshot=CompetitionElements.ActionTags(definition,p,skill.id,result.damageType);
                var raw=snapshot.Raw;var effective=snapshot.Effective;
                for(int offset=0;offset<Math.Max(1,raw.Count);offset+=4)
                {
                    var lines=new List<string>{skill.name+" · 현재 상태 미리보기","태그는 분류이며 효과 수치와 별개입니다.",""};
                    foreach(var grant in raw.Skip(offset).Take(4))
                    {
                        bool active=effective.Any(g=>g.ownerKind==grant.ownerKind && g.ownerId==grant.ownerId && g.tagId==grant.tagId && g.sourceKind==grant.sourceKind && g.sourceId==grant.sourceId && g.ordinal==grant.ordinal);
                        string name=definition.v4.tags.Single(t=>t.id==grant.tagId).name;
                        lines.Add((active?"[유효] ":"[비활성] ")+name+" · "+Owner(grant.ownerKind));
                        lines.Add(grant.tagId);lines.Add("출처: "+Source(definition,grant.sourceKind,grant.sourceId));lines.Add("");
                    }
                    pages.Add(string.Join("\n",lines));
                }
            }
        }
        static void ModifierPages(List<string> pages,CompetitionDefinition definition,ParticipantState p,V4StatBreakdown value)
        {
            pages.Add("장착 장비\n"+string.Join("\n",p.equipment.Select((id,i)=>new[]{"무기","방어구","장신구"}[i]+": "+
                (id==null?"없음":definition.combat.gear.Single(g=>g.id==id).name.Split(new[]{" · "},StringSplitOptions.None)[0])))+
                "\n\n고유 능력\n"+value.passiveName+"\n"+value.passiveDescription+
                "\n\n다음 페이지: 기술별 조건을 만족하는 효과\n같은 태그가 여러 번 있어도 보정은 중복 지급되지 않습니다.");
            foreach(var skill in Possible(definition,p))
            {
                var result=CompetitionElements.SkillPreview(definition,p,skill.id,CompetitionElements.Enemy(definition,p));
                var modifiers=value.modifiers.Concat(result.modifiers).GroupBy(m=>m.sourceKind+"/"+m.sourceId+"/"+m.id+"/"+m.target).Select(g=>g.First()).ToArray();
                for(int offset=0;offset<Math.Max(1,modifiers.Length);offset+=4)
                {
                    var lines=new List<string>{skill.name+" · "+CompetitionElements.TypeName(result.damageType)+" · 현재 조건","예상 피해 "+result.damageBeforeShield+" / 보호 "+result.block,""};
                    foreach(var modifier in modifiers.Skip(offset).Take(4))
                    {
                        lines.Add(Source(definition,modifier.sourceKind,modifier.sourceId));
                        lines.Add(Target(modifier.target)+" "+Amount(modifier));lines.Add("규칙: "+modifier.id);lines.Add("");
                    }
                    if(modifiers.Length==0) lines.Add("현재 조건에 적용되는 추가 보정 없음");
                    pages.Add(string.Join("\n",lines));
                }
            }
        }
        public static string CombatSummary(CompetitionDefinition definition,ParticipantState p)
        {
            var enemy=CompetitionElements.Enemy(definition,p);
            string text=enemy==null?"":EnemyText(definition,enemy);
            var skills=p.v4?.lastTurn?.skills;
            if(skills!=null && skills.Count>0) text+="\n최근 피해: "+string.Join(" / ",skills.Where(s=>s.status=="applied").Select(s=>CompetitionElements.TypeName(s.damageType)+" "+s.damageApplied));
            return text;
        }
        public static string EncounterSummary(CompetitionDefinition definition,EncounterChoice choice)
        {
            if(choice.kind!=EncounterKind.Combat) return choice.description;
            var enemy=definition.v4.enemies.Single(e=>e.encounterId==choice.id);
            return choice.label+" · 약점 "+(enemy.weakness.HasValue?CompetitionElements.TypeName(enemy.weakness.Value)+" +"+Percent(definition.v4.weaknessBasisPoints):"없음")+
                " · 저항 "+(enemy.resistance.HasValue?CompetitionElements.TypeName(enemy.resistance.Value)+" −"+Percent(definition.v4.resistanceBasisPoints):"없음");
        }
        static string EnemyText(CompetitionDefinition d,V4EnemyDefinition enemy)
            =>"적 공격 "+CompetitionElements.TypeName(enemy.attackType)+" · 약점 "+(enemy.weakness.HasValue?CompetitionElements.TypeName(enemy.weakness.Value)+" +"+Percent(d.v4.weaknessBasisPoints):"없음")+
                " · 저항 "+(enemy.resistance.HasValue?CompetitionElements.TypeName(enemy.resistance.Value)+" −"+Percent(d.v4.resistanceBasisPoints):"없음");
        static string ConversionText(V4SkillResult result)=>result.originalType==result.damageType?"":CompetitionElements.TypeName(result.originalType)+" → "+CompetitionElements.TypeName(result.damageType)+" · ";
        static string AffinityText(int value)=>value==10000?"상성: 중립":value>10000?"약점 +"+Percent(value-10000):"저항 −"+Percent(10000-value);
        static string Percent(int basisPoints)=>(basisPoints/100m).ToString("0.##",CultureInfo.InvariantCulture)+"%";
        static string Amount(V4Modifier value)=>value.operation==ModifierOperation.FinalMultiply?"×"+(value.amount/10000m).ToString("0.####",CultureInfo.InvariantCulture):
            (value.amount>=0?"+":"")+(value.operation==ModifierOperation.PercentAdd || value.target=="resistance"?Percent(value.amount):value.amount.ToString(CultureInfo.InvariantCulture));
        static string Target(string target)
        {
            foreach(var stat in Stats) if(CompetitionAttributes.Target(stat)==target) return CompetitionAttributes.Label(stat);
            return target=="damage"?"피해":target=="block"?"보호":target=="resistance"?"피해 저항":target;
        }
        static string Source(CompetitionDefinition d,string kind,string id)
        {
            var adventurer=d.adventurers.FirstOrDefault(a=>a.id==id);
            if(kind=="adventurer" && adventurer!=null) return "고유 능력 · "+adventurer.name;
            if(kind=="gear")
            {
                string gearId=id.Contains("/")?id.Substring(id.LastIndexOf('/')+1):id;
                var gear=d.combat.gear.FirstOrDefault(g=>g.id==gearId);
                if(gear!=null) return "장비 · "+gear.name.Split(new[]{" · "},StringSplitOptions.None)[0];
            }
            return kind+" / "+id;
        }
        static string Owner(TagOwnerKind kind)=>new[]{"모험가","행동","기술","대상","장비","상태","장소","결과","피해"}[(int)kind];
        static string SymbolName(Symbol symbol)=>new[]{"검","지팡이","불","방패"}[(int)symbol];
    }
}
