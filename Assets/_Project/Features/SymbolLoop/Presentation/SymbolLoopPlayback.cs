using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SymbolLoop
{
    public sealed class SymbolLoopPlayback : MonoBehaviour
    {
        public GameObject combatStage, tray;
        public RectTransform pawn, hero, foe, effect;
        public RectTransform[] cells;
        public LoopDieGraphic numericDie;
        public LoopDieGraphic[] symbolDice;
        public LoopGlyphGraphic effectGlyph;
        public Text cue, rollLabel, playerHp, enemyHp, playerShield, enemyShield, skillName, impact;
        public Image playerFill, enemyFill;
        public Text[] dieLabels;
        public int VisiblePosition { get; private set; }
        public string VisibleSkill { get; private set; }
        public int DisplayedEnemyHp { get; private set; }
        public int DisplayedPlayerHp { get; private set; }
        public bool DiceRolling { get; private set; }
        public bool EnemyActing { get; private set; }
        Vector2 heroHome,foeHome,numericHome;
        bool homesSet;

        void RememberHomes()
        {
            if(homesSet) return; homesSet=true;
            heroHome=hero.anchoredPosition; foeHome=foe.anchoredPosition; numericHome=numericDie.rectTransform.anchoredPosition;
        }
        public void Snap(Run run,bool showBoard)
        {
            RememberHomes(); DiceRolling=false; EnemyActing=false; VisibleSkill=null;
            hero.anchoredPosition=heroHome; foe.anchoredPosition=foeHome;
            hero.localScale=foe.localScale=Vector3.one; effect.gameObject.SetActive(false); impact.text=""; skillName.text="";
            pawn.localScale=Vector3.one; numericDie.rectTransform.anchoredPosition=numericHome;
            bool combat=run!=null && run.phase==Phase.Combat;
            combatStage.SetActive(combat); tray.SetActive(run!=null && run.phase!=Phase.Result);
            pawn.gameObject.SetActive(showBoard && run!=null);
            cue.text=run==null ? "시작 준비를 마치고 여정을 떠나세요" : Cue(run);
            numericDie.gameObject.SetActive(!combat);
            numericDie.Configure(run==null || run.lastTravel==0 ? 1 : run.lastTravel);
            rollLabel.text=combat ? "SYMBOL DICE  /  전체 조합" : "TRAVEL D6  /  이동 주사위";
            for(int i=0;i<symbolDice.Length;i++)
            {
                bool active=combat && i<run.dice.Count;
                symbolDice[i].gameObject.SetActive(active); dieLabels[i].gameObject.SetActive(active);
                symbolDice[i].rectTransform.localScale=Vector3.one;
                if(!active) continue;
                var def=run.rules.dice.Single(d=>d.id==run.dice[i].definitionId);
                int face=run.lastTurn!=null && i<run.lastTurn.faces.Length ? (int)run.lastTurn.faces[i] : (int)def.faces[0];
                symbolDice[i].Configure(face,def.faces);
                float x=330+(i-(run.dice.Count-1)*.5f)*172;
                symbolDice[i].rectTransform.anchoredPosition=new Vector2(x-58,-30);
                dieLabels[i].rectTransform.anchoredPosition=new Vector2(x-78,-141);
                dieLabels[i].text=def.name+" · "+SymbolName((Symbol)face);
            }
            if(run==null) return;
            VisiblePosition=run.position; pawn.anchoredPosition=CellPosition(run.position);
            if(combat) SetHealth(run.hp,run.maxHp,run.enemy.hp,run.enemy.maxHp,0,run.enemy.shield);
        }
        public static string SymbolName(Symbol symbol)=>new[]{"검","지팡이","불","방패"}[(int)symbol];
        static string Cue(Run run)
        {
            switch(run.phase)
            {
                case Phase.Combat:return "기호를 굴려 스킬을 발동하세요";
                case Phase.Event:return "이벤트 발견 · 행동을 선택하세요";
                case Phase.Treasure:return "보물 발견 · 보상을 선택하세요";
                case Phase.Shop:return "상점 도착 · 필요한 물품을 고르세요";
                case Phase.Rest:return "야영지 도착 · 회복 또는 훈련";
                case Phase.LapReward:return "지역 완주 · 보상을 선택하세요";
                case Phase.Result:return run.won ? "세 지역 모험을 완주했습니다" : "이번 여정이 끝났습니다";
                default:return "주사위를 굴려 모험을 시작하세요";
            }
        }
        Vector2 CellPosition(int index)=>cells[index].anchoredPosition+new Vector2(cells[index].sizeDelta.x-34,-34);
        void SetHealth(int hp,int maxHp,int ehp,int emax,int block,int shield)
        {
            DisplayedPlayerHp=hp; DisplayedEnemyHp=ehp;
            playerHp.text=$"전사  {hp} / {maxHp}"; enemyHp.text=$"수호자  {ehp} / {emax}";
            playerFill.fillAmount=(float)hp/maxHp; enemyFill.fillAmount=(float)ehp/emax;
            playerShield.text="보호 "+block; enemyShield.text="보호 "+shield;
        }
        static IEnumerator Animate(float duration,Action<float> frame)
        {
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            { frame(Mathf.Clamp01(elapsed/duration)); yield return null; }
            frame(1);
        }
        public IEnumerator Play(string kind,Run before,Run after)
        {
            if(kind=="travel") yield return Travel(before,after);
            else if(kind=="combat") yield return Combat(before,after);
        }
        IEnumerator Travel(Run before,Run after)
        {
            cue.text="숫자 주사위를 굴리는 중…"; numericDie.Configure(after.lastTravel);
            Vector2 home=numericDie.rectTransform.anchoredPosition; DiceRolling=true;
            yield return Animate(.95f,t=>{
                numericDie.Pose(t,0);
                numericDie.rectTransform.anchoredPosition=home+new Vector2(Mathf.Sin(t*21)*22,Mathf.Abs(Mathf.Sin(t*13))*26)*(1-t);
            });
            DiceRolling=false; numericDie.rectTransform.anchoredPosition=home;
            cue.text=after.lastTravel+"칸 이동"; yield return new WaitForSecondsRealtime(.18f);
            int steps=Mathf.Min(after.lastTravel,12-before.position);
            for(int i=1;i<=steps;i++)
            {
                int next=(before.position+i)%12; Vector2 from=pawn.anchoredPosition,to=CellPosition(next);
                yield return Animate(.22f,t=>pawn.anchoredPosition=Vector2.Lerp(from,to,Mathf.SmoothStep(0,1,t))+Vector2.up*(Mathf.Sin(t*Mathf.PI)*22));
                VisiblePosition=next;
                cue.text=next==0 ? "출발점 도착 · 지역 완주!" : $"{i} / {steps}칸 이동";
            }
            yield return Animate(.24f,t=>pawn.localScale=Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*.22f));
            pawn.localScale=Vector3.one;
        }
        IEnumerator Combat(Run before,Run after)
        {
            TurnLog log=after.lastTurn; DiceRolling=true; cue.text="기호 주사위를 굴리는 중…";
            for(int i=0;i<before.dice.Count;i++)
            {
                var def=before.rules.dice.Single(d=>d.id==before.dice[i].definitionId);
                symbolDice[i].Configure((int)log.faces[i],def.faces); dieLabels[i].text=def.name;
            }
            yield return Animate(1.05f,t=>{
                for(int i=0;i<before.dice.Count;i++)
                {
                    float p=Mathf.Clamp01(t*1.15f-i*.07f); symbolDice[i].Pose(p,i);
                    symbolDice[i].rectTransform.localScale=Vector3.one*(1+Mathf.Sin(p*Mathf.PI)*.16f);
                }
            });
            for(int i=0;i<before.dice.Count;i++) { symbolDice[i].Pose(1,i); symbolDice[i].rectTransform.localScale=Vector3.one; dieLabels[i].text=SymbolName(log.faces[i]); }
            DiceRolling=false;
            bool combo=before.rules.combos.Any(s=>s.id==log.skills[0].skillId);
            cue.text=combo ? "전체 기호 일치 · 조합 스킬!" : "눈마다 기본기 발동";
            yield return new WaitForSecondsRealtime(.28f);
            int block=0;
            foreach(var skill in log.skills)
            {
                if(skill.status!="applied") continue;
                VisibleSkill=skill.skillId; skillName.text=(combo ? "COMBO  ·  " : "SKILL  ·  ")+skill.name;
                var definition=before.rules.combos.Concat(before.rules.basics).Single(s=>s.id==skill.skillId);
                LoopGlyph glyph=definition.tag=="guard" ? LoopGlyph.Shield : definition.tag=="fire" ? LoopGlyph.Fire : definition.tag=="arcane" ? LoopGlyph.Staff : LoopGlyph.Sword;
                effectGlyph.glyph=glyph; effectGlyph.color=LoopGlyphGraphic.Tint(glyph); effectGlyph.SetVerticesDirty();
                effect.gameObject.SetActive(true); impact.text="";
                yield return Animate(.45f,t=>{
                    effect.anchoredPosition=skill.damage==0 && skill.block>0 ? heroHome : Vector2.Lerp(heroHome+new Vector2(72,0),foeHome,t);
                    effect.localScale=Vector3.one*(.55f+Mathf.Sin(t*Mathf.PI)*.55f);
                    effect.localRotation=Quaternion.Euler(0,0,glyph==LoopGlyph.Sword ? -90+150*t : 20*Mathf.Sin(t*6));
                    hero.anchoredPosition=heroHome+Vector2.right*Mathf.Sin(t*Mathf.PI)*16;
                });
                block+=skill.block;
                SetHealth(log.playerHpBefore,before.maxHp,skill.enemyHpAfter,before.enemy.maxHp,block,skill.enemyShieldAfter);
                int absorbed=skill.enemyShieldBefore-skill.enemyShieldAfter;
                impact.text=(skill.damage>0 ? "−"+skill.damage : absorbed>0 ? "방어됨" : "")+(skill.block>0 ? "   보호 +"+skill.block : "");
                impact.color=effectGlyph.color;
                yield return Animate(.42f,t=>{
                    effect.localScale=Vector3.one*(1+t*1.25f);
                    Color c=effectGlyph.color;c.a=1-t;effectGlyph.color=c;
                    foe.anchoredPosition=foeHome+Vector2.right*(Mathf.Sin(t*40)*10*(1-t));
                });
                effect.gameObject.SetActive(false); foe.anchoredPosition=foeHome; hero.anchoredPosition=heroHome;
            }
            VisibleSkill=null;
            if(log.retaliation)
            {
                EnemyActing=true; skillName.text=log.intent==Intent.Defend ? "적의 방어" : "적의 반격"; cue.text="적 행동";
                yield return Animate(.4f,t=>foe.anchoredPosition=foeHome+Vector2.left*Mathf.Sin(t*Mathf.PI)*42);
                SetHealth(log.playerHpAfter,before.maxHp,log.enemyHpAfter,before.enemy.maxHp,0,after.enemy.shield);
                impact.text=log.intent==Intent.Defend ? "적 보호 +"+after.enemy.shield : log.retaliationDamage==0 ? "공격 방어!" : "HP −"+log.retaliationDamage;
                impact.color=new Color(1,.58f,.51f);
                yield return Animate(.3f,t=>hero.anchoredPosition=heroHome+Vector2.right*Mathf.Sin(t*36)*8*(1-t));
                EnemyActing=false;
            }
            else { cue.text="수호자 격파 · 반격 없음"; yield return Animate(.4f,t=>foe.localScale=Vector3.one*(1-t*.35f)); }
            yield return new WaitForSecondsRealtime(.25f);
        }
    }
}
