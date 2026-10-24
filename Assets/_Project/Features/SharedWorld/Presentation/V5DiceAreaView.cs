using System;
using System.Collections;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    public sealed class V5DiceAreaView : MonoBehaviour
    {
        public RectTransform layout,footer,rollZone,holdZone;
        public Text heading,enemy,player,intent,rollHint,holdHint,preview,notice;
        public ScrollRect feedbackScroll;
        public Image enemyFill,playerFill;
        public CommonButtonView[] dice;
        public V5FaceGraphic[] faces;
        public V5DieGraphic[] cubes;
        public RectTransform battleStage;
        public LoopGlyphGraphic heroGlyph,enemyGlyph,effectGlyph;
        public Text stageHeroName,stageEnemyName,stageAction,impact;
        public CommonButtonView roll,reroll,execute,diceInfo,combatInfo;
        public bool IsAnimating { get; private set; }
        public bool DiceRolling { get; private set; }
        public bool EnemyActing {get;private set;}
        public string VisibleSkill {get;private set;}
        public bool ReducedMotion {get;set;}
        Action<string,string> command;
        Action openDice,openCombat;
        public static ButtonAppearance Appearance=>new ButtonAppearance {normal=new Color(.11f,.17f,.26f),selected=new Color(.22f,.43f,.47f),pressed=new Color(.17f,.55f,.52f),disabled=new Color(.1f,.13f,.18f)};
        static ButtonAppearance Primary=>new ButtonAppearance{normal=new Color(.95f,.74f,.36f),selected=new Color(1,.83f,.5f),pressed=new Color(.85f,.6f,.24f),disabled=new Color(.28f,.25f,.2f)};
        static ButtonAppearance DieAppearance=>new ButtonAppearance{normal=new Color(.08f,.14f,.22f,.2f),selected=new Color(.1f,.3f,.33f,.5f),pressed=new Color(.15f,.4f,.42f,.7f),disabled=new Color(.08f,.14f,.22f,.2f)};
        public void Bind(CompetitionState state,Action<string,string> dispatch,Action showDice,Action showCombat,string message)
        { command=dispatch;openDice=showDice;openCombat=showCombat;Snap(state);if(!string.IsNullOrEmpty(message)) SetNotice(message); }
        void OnRectTransformDimensionsChange()
        {
            if(!layout || !footer)return;var size=((RectTransform)transform).rect.size;
            float scale=Mathf.Max(.01f,Mathf.Min(size.x/720,size.y/1280));layout.localScale=footer.localScale=Vector3.one*scale;
        }
        public void Snap(CompetitionState state)
        {
            IsAnimating=DiceRolling=EnemyActing=false;VisibleSkill=null;OnRectTransformDimensionsChange();if(preview)preview.transform.localScale=Vector3.one;
            if(state==null || !CompetitionV5Content.IsEnabled(state.definition))return;
            if(!layout || !footer || !rollZone || !holdZone || dice==null || dice.Length!=3 || cubes==null || cubes.Length!=3 || !battleStage || !heroGlyph || !enemyGlyph || !effectGlyph)
                throw new InvalidOperationException("V5 주사위 영역의 authored 연결이 없습니다.");
            var p=state.participants[0];var selection=p.v5?.selection;
            bool selecting=selection?.phase==V5SelectionPhase.Selection && p.phase==CompetitionPhase.Combat && !state.finished;
            heading.text=$"세계 라운드 {state.worldRound} · 전투 행동턴 {selection?.combatTurnSequence ?? 0}/{state.definition.maxCombatTurns}";
            SetHealth(p.hp,p.maxHp,0,p.enemy?.hp ?? 0,p.enemy?.maxHp ?? 1,p.enemy?.shield ?? 0,p.enemy?.name ?? "전투 결과",true);
            intent.text=p.enemy==null?p.message:"적 의도: "+(p.enemy.intent==Intent.Defend?"방어":p.enemy.intent==Intent.Heavy?"강공격":"공격")+$" · 기준 공격 {p.enemy.attack}";
            var results=selection?.results ?? Array.Empty<V5FaceResult>();
            var plan=selecting?CompetitionV5Skills.PlanSteps(state.definition,p,results):Array.Empty<V5PlannedSkill>();
            holdZone.gameObject.SetActive(false);ResetStage(state.definition,p);
            for(int i=0;i<3;i++)
            {
                var button=dice[i];button.gameObject.SetActive(i<p.dice.Count);
                if(i>=p.dice.Count){button.Unbind();continue;}
                var result=results.FirstOrDefault(r=>r.slotIndex==i);bool held=result?.held==true;
                var rect=(RectTransform)button.transform;rect.SetParent(rollZone,false);
                rect.anchoredPosition=SlotPosition(i,p.dice.Count);rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
                string id=p.dice[i].instanceId;var face=result==null?null:CompetitionV5Content.Face(state.definition,result.faceId);
                if(faces!=null && i<faces.Length && faces[i]){faces[i].gameObject.SetActive(false);if(face!=null)faces[i].Configure(face);}
                cubes[i].gameObject.SetActive(true);cubes[i].Configure(CompetitionV5Content.Faces(state.definition,p.adventurerId,p.dice[i].definitionId),result?.faceIndex ?? 0);
                cubes[i].RevealResult=result!=null;cubes[i].Held=held;
                int group=Array.FindIndex(plan,s=>s.dieSlotIndices.Contains(i));
                button.Bind(id,ResultLabel(state.definition,p.dice[i],i,face,held,group),null,"",DieAppearance,selecting && CompetitionV5Dice.CanHold(state,p),held,_=>command?.Invoke(held?"combat_release":"combat_hold",id));
            }
            rollHint.text=$"전투 주사위 {p.dice.Count}개 · "+(selecting?$"보관 {results.Count(r=>r.held)}개":"첫 굴림 준비");
            holdHint.text=$"보관 변경 {selection?.holdChanges ?? 0}/{state.definition.v5.maxHoldChangesPerTurn} · 같은 자리에서 보관/해제";
            bool awaitRoll=selection?.phase==V5SelectionPhase.AwaitRoll && p.phase==CompetitionPhase.Combat;
            roll.Bind("roll","주사위 굴리기",null,"",Primary,awaitRoll,false,_=>command?.Invoke("combat_roll",""));roll.gameObject.SetActive(awaitRoll);roll.label.color=new Color(.1f,.14f,.2f);
            reroll.Bind("reroll",$"다시 굴리기\n남은 {selection?.rerollsLeft ?? 0}회",null,"",Appearance,selecting && selection.rerollsLeft>0 && results.Any(r=>!r.held),false,_=>command?.Invoke("combat_reroll",""));
            execute.Bind("execute","기술 실행",null,"",Primary,selecting,false,_=>command?.Invoke("combat_execute",""));execute.label.color=new Color(.1f,.14f,.2f);
            reroll.gameObject.SetActive(!awaitRoll);execute.gameObject.SetActive(!awaitRoll);
            diceInfo.Bind("dice-info","주사위 6면",null,"",Appearance,true,false,_=>openDice?.Invoke());
            combatInfo.Bind("combat-info","전투 정보\n모험 메뉴",null,"",Appearance,true,false,_=>openCombat?.Invoke());
            preview.text=selecting?PreviewText(CompetitionV5Skills.Preview(state.definition,p)):"먼저 주사위를 굴려 이번 결과를 확인하세요.";
            string limit=CompetitionV5Dice.HoldUnavailableReason(state,p);
            string detail=selecting?"보관/해제는 턴을 쓰지 않습니다. 기술 실행 후 살아 있는 적이 행동합니다.":"최초 굴림 대기 · 주사위/전투 상세는 조회할 수 있습니다.";
            if(selecting && results.All(r=>r.held))detail+="\n모두 보관 중 · 다시 굴릴 주사위가 없습니다.";
            else if(selecting && selection.rerollsLeft==0)detail+="\n다시 굴리기 소진 · 보관 변경 또는 기술 실행 가능";
            if(selecting && limit!=null)detail="보관 변경 제한 · "+limit+"\n"+
                (selection.rerollsLeft>0 && results.Any(r=>!r.held)?"남은 다시 굴리기 또는 기술 실행이 가능합니다.":"기술 실행이 가능합니다.");
            if(selection?.combatTurnSequence>=state.definition.maxCombatTurns)detail+="\n마지막 행동턴 · 이 실행 후 적을 처치하지 못하면 탈락합니다.";
            SetNotice(detail);
        }
        public static Vector2 SlotPosition(int slot,int count)=>new Vector2(330+(slot-(count-1)*.5f)*214,-212);
        public static Vector2 CardPosition(int slot)=>SlotPosition(slot,3);
        static string ResultLabel(CompetitionDefinition definition,DieSlot die,int slot,V5FaceDefinition face,bool held,int group)
        {return DieLabel(definition,die,slot)+"\n"+(face==null?"굴림 대기":"결과: "+face.name)+"\n"+
            (face==null?"첫 굴림 후 보관 가능":(held?"보관 중 · ":"탭하여 보관 · ")+(group>=0?"기술 "+(group+1):"꽝"));}
        public static string Marker(int slot)=>slot>=0 && slot<3?"①②③"[slot].ToString():(slot+1).ToString();
        public static string DieLabel(CompetitionDefinition definition,DieSlot die,int slot)
        {string name=definition.combat.dice.Single(d=>d.id==die.definitionId).name;return Marker(slot)+" "+name+(name.EndsWith("주사위",StringComparison.Ordinal)?"":" 주사위");}
        public static string KindLabel(V5FaceKind kind)=>kind==V5FaceKind.Class?"전용":kind==V5FaceKind.Blank?"꽝":"공용";
        public static string PreviewText(V5CombatPreview value,int limit=int.MaxValue)
        {
            if(value.noOp)return "발동 기술 없음 · 모든 눈이 꽝\n기술 실행은 가능하며 적이 행동합니다.";
            if(value.status=="await-roll")return "최초 굴림 대기 · 아직 발동 예상이 없습니다.";
            string text="발동 예상 "+value.skills.Length+"개\n"+string.Join("\n",value.skills.Take(limit).Select((s,i)=>
                $"기술 {i+1} · {s.name} · {s.dieSlotIndices.Length}눈 ["+string.Join("+",s.dieSlotIndices.Select(Marker))+"]\n"+
                (s.status=="enemy-defeated"?"처치 후 생략":EffectText(s.damageType,s.damageApplied,s.shieldAbsorbed,s.block))));
            return text+(value.skills.Length>limit?$"\n추가 {value.skills.Length-limit}개 · 전투 상세에서 전체 보기":"");
        }
        static string EffectText(DamageType type,int damage,int absorbed,int block)
        {
            string effect=damage>0?CompetitionElements.TypeName(type)+" HP 피해 "+damage:"";
            if(absorbed>0)effect+=(effect.Length>0?" · ":"")+"보호막 흡수 "+absorbed;
            if(block>0)effect+=(effect.Length>0?" · ":"")+"보호 +"+block;
            return effect.Length>0?effect:"HP 피해 0 · 보호 생성 없음";
        }
        public IEnumerator Play(string kind,CompetitionState before,CompetitionState after)
        {
            var previous=before.participants[0].v5.selection;Snap(kind=="combat_execute"?before:after);IsAnimating=true;
            LockInput();
            if(kind=="combat_roll" || kind=="combat_reroll")
            {
                DiceRolling=true;var moved=after.participants[0].v5.selection.results.Where(r=>kind=="combat_roll" || previous.results.Single(x=>x.dieInstanceId==r.dieInstanceId).held==false).Select(r=>r.slotIndex).ToArray();
                string action=kind=="combat_roll"?"굴리는 중":"다시 굴리는 중";
                roll.gameObject.SetActive(false);reroll.gameObject.SetActive(true);execute.gameObject.SetActive(true);reroll.label.text=action+"…\n남은 "+after.participants[0].v5.selection.rerollsLeft+"회";
                if(!ReducedMotion)
                {
                    foreach(int i in moved){cubes[i].RevealResult=false;cubes[i].Pose(0,i);dice[i].label.text=DieLabel(after.definition,after.participants[0].dice[i],i)+"\n"+action+"\n결과 대기";}
                    // Held labels retain their old face, but no old/new combination group is presented before reveal.
                    foreach(var result in previous.results.Where(r=>r.held))
                        dice[result.slotIndex].label.text=DieLabel(before.definition,before.participants[0].dice[result.slotIndex],result.slotIndex)+"\n결과: "+CompetitionV5Content.Face(before.definition,result.faceId).name+"\n보관 중 · 리롤 제외";
                    preview.text="주사위가 멈추면 표시됩니다.\n새 결과와 기술 구성을 확인하는 중";rollHint.text=kind=="combat_roll"?"첫 굴림 · 전투 주사위 "+moved.Length+"개":"보관 유지 · "+string.Join("·",moved.Select(Marker))+"만 리롤";
                    SetNotice("보관한 주사위는 고정됩니다. 결과 공개 후 조작할 수 있습니다.");
                    yield return Animate(kind=="combat_roll"?.75f:.55f,t=>{foreach(int i in moved)cubes[i].Pose(t,i);});
                }
                Snap(after);IsAnimating=DiceRolling=true;LockInput();rollHint.text="결과 공개 · 큐브의 정면과 아래 결과를 확인하세요";
                yield return Animate(.15f,_=>{});
                DiceRolling=false;
            }
            else if(kind=="combat_hold" || kind=="combat_release")
            {
                var moved=after.participants[0].v5.selection.results.Where(r=>previous.results.Single(x=>x.dieInstanceId==r.dieInstanceId).held!=r.held).Select(r=>r.slotIndex).ToArray();
                yield return Animate(ReducedMotion?.04f:.12f,t=>{foreach(int i in moved)cubes[i].Pulse=ReducedMotion?0:Mathf.Sin(t*Mathf.PI)*.5f;});
            }
            else if(kind=="combat_execute")
            {
                var p=after.participants[0];var initial=before.participants[0];var log=p.lastTurn;int block=0;
                roll.gameObject.SetActive(false);
                foreach(var result in p.v5.lastTurn.results)
                {
                    var card=dice[result.slotIndex];card.gameObject.SetActive(true);card.transform.SetParent(rollZone,false);
                    ((RectTransform)card.transform).anchoredPosition=SlotPosition(result.slotIndex,initial.dice.Count);
                    cubes[result.slotIndex].Configure(CompetitionV5Content.Faces(before.definition,initial.adventurerId,initial.dice[result.slotIndex].definitionId),result.faceIndex);cubes[result.slotIndex].Held=result.held;
                    card.label.text=DieLabel(before.definition,initial.dice[result.slotIndex],result.slotIndex)+"\n"+CompetitionV5Content.Face(after.definition,result.faceId).name+"\n실행한 눈";
                }
                preview.text="확정한 눈으로 기술을 실행합니다.";execute.label.text="기술 실행 중…";stageAction.text="기술 실행";SetNotice("내 기술 실행 · 현재 보호는 0부터 누적됩니다.");
                SetHealth(log.playerHpBefore,initial.maxHp,0,log.enemyHpBefore,initial.enemy.maxHp,initial.enemy.shield,initial.enemy.name,true);
                yield return Animate(.12f,_=>{});
                if(p.v5.lastTurn?.noOp==true) {preview.text="발동 기술 없음 · 모든 눈이 꽝";yield return Animate(.25f,_=>{});}
                var plan=CompetitionV5Skills.PlanSteps(before.definition,initial,p.v5.lastTurn.results);
                for(int index=0;index<log.skills.Count;index++)
                {
                    var skill=log.skills[index];var step=plan[index];
                    preview.text=$"기술 {index+1} · {skill.name} · {step.dieSlotIndices.Length}눈 ["+string.Join("+",step.dieSlotIndices.Select(Marker))+"]\n"+
                        (skill.status=="enemy-defeated"?"처치 후 생략":EffectText(p.v4.lastTurn.skills[index].damageType,skill.enemyHpBefore-skill.enemyHpAfter,skill.enemyShieldBefore-skill.enemyShieldAfter,skill.block));
                    foreach(int slot in step.dieSlotIndices)dice[slot].label.text=DieLabel(before.definition,initial.dice[slot],slot)+"\n"+CompetitionV5Content.Face(before.definition,p.v5.lastTurn.results.Single(r=>r.slotIndex==slot).faceId).name+"\n기술 "+(index+1)+(skill.status=="enemy-defeated"?" 생략":" 실행");
                    VisibleSkill=skill.skillId;stageAction.text="기술 "+(index+1)+" · "+skill.name;
                    foreach(var cube in cubes)cube.Pulse=0;foreach(int slot in step.dieSlotIndices)cubes[slot].Pulse=1;
                    if(skill.status=="enemy-defeated"){impact.text="처치 후 생략";yield return Animate(.12f,_=>{});continue;}
                    var type=p.v4.lastTurn.skills[index].damageType;bool defending=skill.damage==0 && skill.block>0;
                    ConfigureEffect(type,defending);impact.text="";
                    yield return Animate(ReducedMotion?.06f:.28f,t=>EffectFrame(t,false,defending));
                    block+=skill.block;
                    SetHealth(log.playerHpBefore,initial.maxHp,block,skill.enemyHpAfter,initial.enemy.maxHp,skill.enemyShieldAfter,initial.enemy.name,false);
                    impact.text=EffectText(type,skill.enemyHpBefore-skill.enemyHpAfter,skill.enemyShieldBefore-skill.enemyShieldAfter,skill.block);
                    yield return Animate(ReducedMotion?.08f:.14f,_=>{});effectGlyph.gameObject.SetActive(false);
                }
                int enemyShield=log.retaliation && log.intent==Intent.Defend?initial.enemy.attack:0;
                foreach(var cube in cubes)cube.Pulse=0;VisibleSkill=null;
                if(log.retaliation)
                {
                    EnemyActing=true;stageAction.text=log.intent==Intent.Defend?"적의 방어":"적의 반격";impact.text="";
                    ConfigureEffect(DamageType.Physical,log.intent==Intent.Defend);
                    yield return Animate(ReducedMotion?.06f:.25f,t=>EffectFrame(t,true,log.intent==Intent.Defend));
                    effectGlyph.gameObject.SetActive(false);
                }
                else stageAction.text="적 처치 · 반격 없음";
                SetHealth(log.playerHpAfter,initial.maxHp,0,log.enemyHpAfter,initial.enemy.maxHp,enemyShield,initial.enemy.name,true);
                impact.text=log.retaliation?(log.intent==Intent.Defend?"적 보호 +"+enemyShield:"내 HP 피해 "+log.retaliationDamage):"적 처치";
                SetNotice(log.retaliation?$"적 행동 종료 · 받은 HP 피해 {log.retaliationDamage}\n이번 실행 보호 {log.playerBlock} · 현재 보호 0 (턴 초기화)":"적 처치 · 반격 없음\n현재 보호 0 (턴 초기화)");
                yield return Animate(ReducedMotion?.08f:.3f,_=>{});EnemyActing=false;
            }
            preview.transform.localScale=Vector3.one;Snap(after);
        }
        void LockInput()
        {foreach(var button in dice.Concat(new[]{roll,reroll,execute,diceInfo,combatInfo})){button.button.interactable=false;button.group.interactable=false;}}
        void ResetStage(CompetitionDefinition definition,ParticipantState p)
        {
            heroGlyph.rectTransform.anchoredPosition=new Vector2(130,-84);enemyGlyph.rectTransform.anchoredPosition=new Vector2(530,-84);
            heroGlyph.transform.localScale=enemyGlyph.transform.localScale=Vector3.one;effectGlyph.gameObject.SetActive(false);
            stageHeroName.text=definition.adventurers.Single(h=>h.id==p.adventurerId).name;stageEnemyName.text=p.enemy?.name ?? "전투 종료";
            stageAction.text="주사위 → 기술 → 효과";impact.text="";
        }
        void ConfigureEffect(DamageType type,bool guard)
        {
            effectGlyph.glyph=guard?LoopGlyph.Shield:type==DamageType.Physical?LoopGlyph.Sword:type==DamageType.Fire?LoopGlyph.Fire:LoopGlyph.Staff;
            effectGlyph.color=guard?new Color(.32f,.84f,.8f):type==DamageType.Fire?new Color(1,.48f,.25f):type==DamageType.Ice?new Color(.5f,.82f,1):type==DamageType.Lightning?new Color(.95f,.82f,.45f):new Color(.95f,.91f,.81f);
            effectGlyph.SetVerticesDirty();effectGlyph.gameObject.SetActive(true);
        }
        void EffectFrame(float progress,bool enemyAction,bool guard)
        {
            var from=enemyAction?new Vector2(530,-84):new Vector2(130,-84);var to=enemyAction?new Vector2(130,-84):new Vector2(530,-84);
            effectGlyph.rectTransform.anchoredPosition=ReducedMotion?(guard?from:to):guard?from:Vector2.Lerp(from,to,progress)+Vector2.up*(8*Mathf.Sin(progress*Mathf.PI));
            effectGlyph.rectTransform.localScale=Vector3.one*(ReducedMotion?.85f:.72f+.18f*Mathf.Sin(progress*Mathf.PI));
            effectGlyph.rectTransform.localRotation=Quaternion.identity;
        }
        void SetHealth(int hp,int maxHp,int block,int enemyHp,int enemyMaxHp,int enemyShield,string enemyName,bool reset)
        {
            player.text=$"내 HP {hp}/{maxHp} · 현재 보호 {block}"+(reset?" (턴 초기화)":"");
            enemy.text=$"{enemyName}  HP {enemyHp}/{enemyMaxHp} · 보호 {enemyShield}";
            playerFill.fillAmount=(float)hp/maxHp;enemyFill.fillAmount=(float)enemyHp/enemyMaxHp;
        }
        void SetNotice(string message)
        {
            notice.text=message;
            if(feedbackScroll)
            {
                var rect=notice.rectTransform;rect.sizeDelta=new Vector2(feedbackScroll.viewport.rect.width,Mathf.Max(feedbackScroll.viewport.rect.height,notice.preferredHeight));
                feedbackScroll.verticalNormalizedPosition=1;
            }
        }
        static IEnumerator Animate(float duration,Action<float> frame)
        {for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime){frame(Mathf.Clamp01(elapsed/duration));yield return null;}frame(1);}
        public void Unbind()
        {foreach(var button in dice.Concat(new[]{roll,reroll,execute,diceInfo,combatInfo}))if(button)button.Unbind();command=null;openDice=openCombat=null;}
    }
}
