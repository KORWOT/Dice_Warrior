using System;
using System.Collections;
using DiceWarrior.SymbolLoop;
using UnityEngine;

namespace DiceWarrior.SharedWorld
{
    // Presentation copies never execute rules or consume gameplay random streams.
    public sealed class CompetitionPlayback : MonoBehaviour
    {
        public CompetitionScreen screen;
        public SymbolLoopPlayback human;
        public bool MovementRolling { get; private set; }
        public bool ReducedMotion { get; set; }
        public bool DiceRolling=>MovementRolling || human.DiceRolling || (screen.v5Area && screen.v5Area.DiceRolling);
        public int VisiblePosition { get; private set; }
        static Run View(CompetitionState state,ParticipantState participant)
        {
            Phase phase=participant.phase==CompetitionPhase.Combat?Phase.Combat:participant.phase==CompetitionPhase.Complete?Phase.Result:Phase.Map;
            return new Run {rules=state.definition.combat,position=participant.position,lastTravel=participant.lastTravel,
                hp=participant.hp,maxHp=participant.maxHp,power=participant.power,guard=participant.guard,phase=phase,
                dice=participant.dice,equipment=participant.equipment,enemy=participant.enemy,lastTurn=participant.lastTurn,
                region=Math.Min(3,participant.completedLaps+1),won=participant.trialSucceeded};
        }
        public void Snap(CompetitionState state)
        {
            MovementRolling=false;
            bool v5=state!=null && CompetitionV5Content.IsEnabled(state.definition);
            human.Snap(state==null || (v5 && state.participants[0].phase==CompetitionPhase.Combat)?null:View(state,state.participants[0]),state!=null && screen.board.activeSelf);
            if(v5 && screen.v5Area)screen.v5Area.Snap(state);
            for(int i=0;i<screen.pawns.Length;i++)
            {
                screen.pawns[i].gameObject.SetActive(state!=null);
                screen.pawns[i].localScale=Vector3.one;
                if(state!=null) screen.pawns[i].anchoredPosition=screen.PawnPosition(i,state.participants[i].position);
            }
            if(state==null) { human.cue.text="공동 세계 시험 · 영구 재화 지급 없음";return; }
            if(state.finished) human.tray.SetActive(false);
            var p=state.participants[0];VisiblePosition=p.position;
            human.cue.text=state.finished?"경기 종료 · 순위는 시험 결과입니다":p.phase==CompetitionPhase.Combat?"기호를 굴려 스킬을 발동하세요":p.phase==CompetitionPhase.Encounter?"장소의 사건을 선택하세요":p.phase==CompetitionPhase.LapReward?"완주 보상을 선택하세요":p.phase==CompetitionPhase.Complete?"다른 모험가들의 라운드를 진행하세요":"숫자 주사위로 다음 장소를 향하세요";
        }
        public IEnumerator Play(string kind,CompetitionState before,CompetitionState after)
        {
            var p=before.participants[0];var next=after.participants[0];
            if(CompetitionV5Content.IsEnabled(before.definition) && kind.StartsWith("combat_",StringComparison.Ordinal))
            {
                if(!screen.v5Area)throw new InvalidOperationException("V5 전투 영역의 authored 참조가 없습니다.");
                yield return screen.v5Area.Play(kind,before,after);
            }
            else if(kind=="travel")
            {
                bool v5=CompetitionV5Content.IsEnabled(before.definition);
                MovementRolling=true;human.cue.text="숫자 주사위를 굴리는 중…";
                if(v5)
                {
                    screen.v5Shell.SetBoardPresentation("굴리는 중…","이동 주사위 · 멈추면 눈을 공개합니다.",true);
                    human.rollLabel.text="이동 주사위 · 굴리는 중";
                    // The tumble is independent of the saved result. Reveal only on landing.
                    human.numericDie.Configure(1);
                    if(!ReducedMotion)yield return Animate(.75f,t=>human.numericDie.Pose(t*.8f,0));
                    human.numericDie.Configure(next.lastTravel);
                    if(!ReducedMotion)yield return Animate(.15f,t=>human.numericDie.Pose(.8f+.2f*t,0));
                    human.rollLabel.text=$"이동 주사위 · {next.lastTravel}눈";
                }
                else
                {
                    human.numericDie.Configure(next.lastTravel);
                    yield return Animate(.85f,t=>human.numericDie.Pose(t,0));
                }
                MovementRolling=false;
                int steps=Math.Min(next.lastTravel,screen.cells.Length-p.position);
                for(int step=1;step<=steps;step++)
                {
                    int position=(p.position+step)%screen.cells.Length;Vector2 start=screen.pawns[0].anchoredPosition,end=screen.PawnPosition(0,position);
                    human.cue.text=after.definition.world.places[position].name;
                    if(v5)
                    {
                        screen.tileFrames[position].color=new Color(.10f,.34f,.36f);
                        screen.v5Shell.SetBoardPresentation("이동 중…",$"{next.lastTravel}눈 · {step}/{steps}칸 · {human.cue.text}",true);
                    }
                    yield return Animate(v5?(ReducedMotion ? .025f : .12f):.18f,t=>screen.pawns[0].anchoredPosition=Vector2.Lerp(start,end,Mathf.SmoothStep(0,1,t))+Vector2.up*(v5&&ReducedMotion?0:Mathf.Sin(t*Mathf.PI)*12));
                    VisiblePosition=position;
                }
                if(v5)
                {
                    string arrival=$"{next.lastTravel}눈 · 실제 {steps}칸 → {after.definition.world.places[next.position].name}";
                    if(steps<next.lastTravel)arrival+="\n완주 지점 도착 · 초과 이동 종료";
                    human.cue.text=arrival;
                    screen.v5Shell.SetBoardPresentation(next.phase==CompetitionPhase.LapReward?"완주 보상으로 이동 중…":"사건으로 이동 중…",arrival,true);
                    yield return Animate(ReducedMotion ? .05f : .18f,_=>{});
                }
            }
            else if(kind=="combat")
            {
                var first=View(before,p);var last=View(after,next);
                if(last.enemy==null && first.enemy!=null) { last.enemy=LoopJson.Copy(first.enemy);last.enemy.hp=last.lastTurn.enemyHpAfter;last.enemy.shield=0; }
                yield return human.Play("combat",first,last);
            }
            if(kind=="lap" && CompetitionV5Content.IsEnabled(before.definition) && next.dice.Count>p.dice.Count && screen.v5Shell)
                yield return Animate(ReducedMotion ? .1f : .35f,t=>screen.v5Shell.ShowDiceGrowth(after,p.dice.Count,t,ReducedMotion));
            if(before.worldRound!=after.worldRound || after.finished)
            {
                human.cue.text="AI 모험가들의 행동 결과";
                if(CompetitionV5Content.IsEnabled(before.definition) && screen.v5Shell && screen.v5Shell.gameObject.activeInHierarchy)
                {
                    var view=screen.v5Shell;
                    if(view.Data?.showBoard==true)view.SetBoardPresentation("AI 차례","AI 행동 결과를 표시하고 있습니다.",true);
                    else if(view.rewardGrowth && view.rewardGrowth.activeSelf)view.summary.text="수령 완료 · AI 행동 결과를 표시하고 있습니다.";
                    else if(view.Data!=null){view.Data.notice="AI 행동 결과를 표시하고 있습니다. 잠시 기다려 주세요.";view.Bind(view.Data);}
                }
                for(int i=1;i<screen.pawns.Length;i++)
                {
                    int index=i;Vector2 start=screen.pawns[i].anchoredPosition,end=screen.PawnPosition(i,after.participants[i].position);
                    if(start!=end) yield return Animate(ReducedMotion ? .025f : .18f,t=>screen.pawns[index].anchoredPosition=Vector2.Lerp(start,end,t));
                }
            }
        }
        static IEnumerator Animate(float duration,Action<float> frame)
        {
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime) { frame(Mathf.Clamp01(elapsed/duration));yield return null; }
            frame(1);
        }
    }
}
