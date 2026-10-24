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
        public Image enemyFill,playerFill;
        public CommonButtonView[] dice;
        public V5FaceGraphic[] faces;
        public CommonButtonView roll,reroll,execute,diceInfo,combatInfo;
        public bool IsAnimating { get; private set; }
        public bool DiceRolling { get; private set; }
        Action<string,string> command;
        Action openDice,openCombat;
        public static ButtonAppearance Appearance=>new ButtonAppearance {normal=new Color(.12f,.25f,.32f),selected=new Color(.23f,.46f,.5f),pressed=new Color(.13f,.6f,.53f),disabled=new Color(.12f,.15f,.19f)};
        public void Bind(CompetitionState state,Action<string,string> dispatch,Action showDice,Action showCombat,string message)
        { command=dispatch;openDice=showDice;openCombat=showCombat;Snap(state);if(!string.IsNullOrEmpty(message)) notice.text=message; }
        void OnRectTransformDimensionsChange()
        {
            if(!layout || !footer)return;var size=((RectTransform)transform).rect.size;
            float scale=Mathf.Max(.01f,Mathf.Min(size.x/720,size.y/1280));layout.localScale=footer.localScale=Vector3.one*scale;
        }
        public void Snap(CompetitionState state)
        {
            IsAnimating=DiceRolling=false;OnRectTransformDimensionsChange();if(preview)preview.transform.localScale=Vector3.one;
            if(state==null || !CompetitionV5Content.IsEnabled(state.definition))return;
            if(!layout || !footer || !rollZone || !holdZone || dice==null || dice.Length!=3 || faces==null || faces.Length!=3)
                throw new InvalidOperationException("V5 주사위 영역의 authored 연결이 없습니다.");
            var p=state.participants[0];var selection=p.v5?.selection;
            bool selecting=selection?.phase==V5SelectionPhase.Selection && p.phase==CompetitionPhase.Combat && !state.finished;
            heading.text=$"{p.name} · 전투 {selection?.combatTurnSequence ?? 0}/20 · 라운드 {state.worldRound}";
            SetHealth(p.hp,p.maxHp,0,p.enemy?.hp ?? 0,p.enemy?.maxHp ?? 1,p.enemy?.shield ?? 0,p.enemy?.name ?? "전투 결과",true);
            intent.text=p.enemy==null?p.message:"적 의도: "+(p.enemy.intent==Intent.Defend?"방어":p.enemy.intent==Intent.Heavy?"강공격":"공격")+$" · 기준 공격 {p.enemy.attack}";
            var results=selection?.results ?? Array.Empty<V5FaceResult>();
            for(int i=0;i<3;i++)
            {
                var button=dice[i];button.gameObject.SetActive(i<p.dice.Count && selecting);
                if(i>=p.dice.Count){button.Unbind();continue;}
                var result=results.FirstOrDefault(r=>r.slotIndex==i);bool held=result?.held==true;
                var rect=(RectTransform)button.transform;rect.SetParent(held?holdZone:rollZone,false);
                rect.anchoredPosition=new Vector2(20+i*210,-58);rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
                string id=p.dice[i].instanceId;var face=result==null?null:CompetitionV5Content.Face(state.definition,result.faceId);
                faces[i].gameObject.SetActive(face!=null);if(face!=null)faces[i].Configure(face);
                string label=face==null?$"주사위 {i+1}\n굴림 대기":$"{i+1} · {face.name}\n{KindLabel(face.kind)} · {(held?"탭하여 해제":"탭하여 보관")}";
                button.Bind(id,label,null,"",Appearance,selecting && CompetitionV5Dice.CanHold(state,p),held,_=>command?.Invoke(held?"combat_release":"combat_hold",id));
                button.gameObject.SetActive(selecting);
            }
            rollHint.text=selecting && results.All(r=>r.held)?"모두 보관 중 · 해제하면 여기로 돌아옵니다":"굴림 영역 · 탭하여 보관";
            holdHint.text=results.Any(r=>r.held)?"보관 영역 · 리롤에서 제외 · 탭하여 해제":"보관 영역 · 유지할 눈을 위에서 탭하세요";
            bool awaitRoll=selection?.phase==V5SelectionPhase.AwaitRoll && p.phase==CompetitionPhase.Combat;
            roll.Bind("roll","굴리기",null,"",Appearance,awaitRoll,false,_=>command?.Invoke("combat_roll",""));roll.gameObject.SetActive(awaitRoll);
            reroll.Bind("reroll",$"리롤  {selection?.rerollsLeft ?? 0}/1",null,"",Appearance,selecting && selection.rerollsLeft>0 && results.Any(r=>!r.held),false,_=>command?.Invoke("combat_reroll",""));
            execute.Bind("execute","실행",null,"",Appearance,selecting,false,_=>command?.Invoke("combat_execute",""));
            diceInfo.Bind("dice-info","주사위 정보",null,"",Appearance,true,false,_=>openDice?.Invoke());
            combatInfo.Bind("combat-info","전투 정보",null,"",Appearance,true,false,_=>openCombat?.Invoke());
            preview.text=selecting?PreviewText(CompetitionV5Skills.Preview(state.definition,p),2):"먼저 굴리기를 눌러 눈을 확인하세요.";
            notice.text=selecting?(CompetitionV5Dice.HoldUnavailableReason(state,p) ?? "보관/해제는 턴을 쓰지 않습니다. 실행하면 적이 행동합니다."):"이번 행동 턴 · 최초 굴림 대기";
        }
        public static string KindLabel(V5FaceKind kind)=>kind==V5FaceKind.Class?"전용":kind==V5FaceKind.Blank?"꽝":"공용";
        public static string PreviewText(V5CombatPreview value,int limit=int.MaxValue)
        {
            if(value.noOp)return "NO_OP · 발동 기술 없음\n이번 실행 시 적이 행동합니다.";
            string text=string.Join("\n",value.skills.Take(limit).Select(s=>s.name+" · "+(s.status=="enemy-defeated"?"처치 후 생략":$"{CompetitionElements.TypeName(s.damageType)} HP 피해 {s.damageApplied} / 흡수 {s.shieldAbsorbed} / 보호 {s.block}")));
            return text+(value.skills.Length>limit?$"\n외 {value.skills.Length-limit}개 · 전투 정보에서 전체 보기":"");
        }
        public IEnumerator Play(string kind,CompetitionState before,CompetitionState after)
        {
            var previous=before.participants[0].v5.selection;var starts=dice.Select(d=>d.transform.position).ToArray();Snap(kind=="combat_execute"?before:after);IsAnimating=true;
            foreach(var button in dice.Concat(new[]{roll,reroll,execute,diceInfo,combatInfo}))button.button.interactable=false;
            if(kind=="combat_roll" || kind=="combat_reroll")
            {
                DiceRolling=true;var moved=after.participants[0].v5.selection.results.Where(r=>kind=="combat_roll" || previous.results.Single(x=>x.dieInstanceId==r.dieInstanceId).held==false).Select(r=>r.slotIndex).ToArray();
                foreach(int i in moved)faces[i].gameObject.SetActive(false);
                yield return Animate(.65f,t=>{foreach(int i in moved){dice[i].transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*6)*(1-t)*25);dice[i].transform.localScale=Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*.1f);}});
                DiceRolling=false;
            }
            else if(kind=="combat_hold" || kind=="combat_release")
            {
                var destinations=dice.Select(d=>((RectTransform)d.transform).anchoredPosition).ToArray();
                var localStarts=dice.Select((d,i)=>(Vector2)d.transform.parent.InverseTransformPoint(starts[i])).ToArray();
                yield return Animate(.18f,t=>{for(int i=0;i<dice.Length;i++){var rect=(RectTransform)dice[i].transform;rect.localPosition=Vector2.Lerp(localStarts[i],new Vector2(destinations[i].x, destinations[i].y),Mathf.SmoothStep(0,1,t));}});
            }
            else if(kind=="combat_execute")
            {
                var p=after.participants[0];var initial=before.participants[0];var log=p.lastTurn;int block=0;
                roll.gameObject.SetActive(false);
                foreach(var result in p.v5.lastTurn.results)
                {
                    var card=dice[result.slotIndex];card.gameObject.SetActive(true);card.transform.SetParent(result.held?holdZone:rollZone,false);
                    ((RectTransform)card.transform).anchoredPosition=new Vector2(20+result.slotIndex*210,-58);
                    faces[result.slotIndex].gameObject.SetActive(true);faces[result.slotIndex].Configure(CompetitionV5Content.Face(after.definition,result.faceId));
                    card.label.text=$"{result.slotIndex+1} · "+CompetitionV5Content.Face(after.definition,result.faceId).name+"\n실행한 눈";
                }
                preview.text="저장된 실행 결과를 표시합니다.";notice.text="내 기술 실행 · 현재 보호는 0부터 누적됩니다.";
                SetHealth(log.playerHpBefore,initial.maxHp,0,log.enemyHpBefore,initial.enemy.maxHp,initial.enemy.shield,initial.enemy.name,true);
                yield return Animate(.12f,_=>{});
                if(p.v5.lastTurn?.noOp==true) {preview.text="NO_OP · 발동 기술 없음";yield return Animate(.25f,_=>{});}
                foreach(var skill in log.skills)
                {
                    preview.text=skill.name+"\n"+(skill.status=="enemy-defeated"?"처치 후 생략":$"HP 피해 {skill.enemyHpBefore-skill.enemyHpAfter} · 흡수 {skill.enemyShieldBefore-skill.enemyShieldAfter} · 보호 {skill.block}");
                    block+=skill.block;
                    SetHealth(log.playerHpBefore,initial.maxHp,block,skill.enemyHpAfter,initial.enemy.maxHp,skill.enemyShieldAfter,initial.enemy.name,false);
                    yield return Animate(.24f,t=>preview.transform.localScale=Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*.06f));
                }
                int enemyShield=log.retaliation && log.intent==Intent.Defend?initial.enemy.attack:0;
                SetHealth(log.playerHpAfter,initial.maxHp,0,log.enemyHpAfter,initial.enemy.maxHp,enemyShield,initial.enemy.name,true);
                notice.text=log.retaliation?$"적 행동 종료 · 받은 HP 피해 {log.retaliationDamage}\n이번 실행 보호 {log.playerBlock} · 현재 보호 0 (턴 초기화)":"적 처치 · 반격 없음\n현재 보호 0 (턴 초기화)";
                yield return Animate(.3f,_=>{});
            }
            preview.transform.localScale=Vector3.one;Snap(after);
        }
        void SetHealth(int hp,int maxHp,int block,int enemyHp,int enemyMaxHp,int enemyShield,string enemyName,bool reset)
        {
            player.text=$"내 HP {hp}/{maxHp} · 현재 보호 {block}"+(reset?" (턴 초기화)":"");
            enemy.text=$"{enemyName}  HP {enemyHp}/{enemyMaxHp} · 보호 {enemyShield}";
            playerFill.fillAmount=(float)hp/maxHp;enemyFill.fillAmount=(float)enemyHp/enemyMaxHp;
        }
        static IEnumerator Animate(float duration,Action<float> frame)
        {for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime){frame(Mathf.Clamp01(elapsed/duration));yield return null;}frame(1);}
        public void Unbind()
        {foreach(var button in dice.Concat(new[]{roll,reroll,execute,diceInfo,combatInfo}))if(button)button.Unbind();command=null;openDice=openCombat=null;}
    }
}
