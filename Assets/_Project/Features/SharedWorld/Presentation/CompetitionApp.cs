using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;

namespace DiceWarrior.SharedWorld
{
    public sealed class CompetitionApp : MonoBehaviour
    {
        public SymbolLoopApp legacy;
        public UIManager ui;
        public TextAsset content;
        public SymbolLoopConfig combatConfig;
        public CompetitionSession Session { get; private set; }
        public bool IsPresenting=>busy;
        public bool IsOpen=>active;
        CompetitionStore store;
        CompetitionDefinition definition;
        Profile profile;
        Func<uint> seedSource;
        bool active,busy,confirmAbandon,confirmRestore;
        string notice="";
        Coroutine presentation;
        int width,height;
        Rect safeArea;

        void OnEnable() { if(legacy) legacy.CompetitionRequested+=OpenFromLobby;if(active) Render(); }
        void OnDisable() { if(legacy) legacy.CompetitionRequested-=OpenFromLobby;CancelPresentation(); }
        void OnApplicationPause(bool value) { if(value) CancelPresentation();if(active) Render(); }
        void Update()
        {
            if(!active || !ui) return;
            if(width==Screen.width && height==Screen.height && safeArea==Screen.safeArea) return;
            width=Screen.width;height=Screen.height;safeArea=Screen.safeArea;ui.Root.ApplySafeArea();
        }
        CompetitionDefinition LoadDefinition()
        {
            if(!content || !combatConfig) throw new InvalidOperationException("공동 세계 콘텐츠 연결을 확인해 주세요.");
            var result=CompetitionContent.Load(content.text);result.combat=combatConfig.CreateRules();return result;
        }
        async void OpenFromLobby()
        {
            if(busy || !legacy || legacy.IsPresenting) return;
            active=true;legacy.SuspendView();busy=true;notice="시작 성장 전력을 확인하는 중…";Render();
            try
            {
                await legacy.Backend.InitializeAsync();
                if(!this || !active) return;
                if(!legacy.Backend.ProfileReady) throw new InvalidOperationException("권한 프로필을 확인하지 못했습니다. 기존 진행은 보존합니다.");
                string path=Path.Combine(Path.GetDirectoryName(legacy.CheckpointPath),"shared-world-preview-v1","checkpoint.json");
                Open(path,LoadDefinition(),legacy.Session.Snapshot.profile);
            }
            catch(Exception error) { notice=error.Message; }
            finally { busy=false;if(this && active && isActiveAndEnabled) Render(); }
        }
        // Explicit dependency injection keeps UI verification away from real saves and authority data.
        public void Open(string path,CompetitionDefinition rules,Profile startingProfile,Func<uint> seeds=null)
        {
            active=true;definition=LoopJson.Copy(rules);profile=LoopJson.Copy(startingProfile);seedSource=seeds;
            store=new CompetitionStore(path);Session=null;Session=new CompetitionSession(store,definition);notice="";
            Render();
        }
        async void StartMatch()
        {
            if(busy) return;busy=true;Render();
            try
            {
                if(legacy)
                {
                    await legacy.Backend.InitializeAsync();
                    if(!this || !active) return;
                    if(!legacy.Backend.ProfileReady) throw new InvalidOperationException("새 경기 시작 전 권한 프로필 확인이 필요합니다.");
                    profile=legacy.Session.Snapshot.profile;
                }
                uint seed=seedSource!=null?seedSource():BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(),0)|1u;
                Session.Start(LoopJson.Copy(profile),seed,Guid.NewGuid().ToString("N"));notice="";
            }
            catch(Exception error) { notice=error.Message; }
            finally { busy=false;if(this && active && isActiveAndEnabled) Render(); }
        }
        void Dispatch(string kind,string arg="")
        {
            if(busy || Session==null) return;busy=true;
            var before=Session.Snapshot.state;
            try { Session.Execute(Session.Prepare(kind,arg));notice="";confirmAbandon=false; }
            catch(Exception error) { notice=error.Message;busy=false;Render();return; }
            var after=Session.Snapshot.state;
            if(!isActiveAndEnabled || !active) { busy=false;return; }
            if(before!=null && after!=null && ui.ActiveScreen is CompetitionScreen screen)
            {
                ui.SetInputLocked(true);presentation=StartCoroutine(Present(screen.playback.Play(kind,before,after)));return;
            }
            busy=false;Render();
        }
        IEnumerator Present(IEnumerator sequence)
        {
            var stack=new Stack<IEnumerator>();stack.Push(sequence);
            while(stack.Count>0)
            {
                bool moved;object current=null;
                try { moved=stack.Peek().MoveNext();if(moved) current=stack.Peek().Current; }
                catch(Exception error) { notice="저장된 결과를 표시합니다: "+error.Message;break; }
                if(!moved) { stack.Pop();continue; }
                if(current is IEnumerator nested) stack.Push(nested);else yield return current;
            }
            presentation=null;busy=false;ui.SetInputLocked(false);if(active) Render();
        }
        void CancelPresentation()
        {
            if(presentation!=null) { StopCoroutine(presentation);presentation=null;busy=false;if(ui) ui.SetInputLocked(false); }
        }
        public void Close()
        {
            if(busy && presentation==null) return;
            CancelPresentation();active=false;
            if(legacy) legacy.ResumeView();else if(ui) ui.CloseAll();
        }
        void Reload()
        {
            try { if(Session==null) Session=new CompetitionSession(store,definition);else Session.Reload();notice=""; }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        void InspectBackup()
        {
            try
            {
                var backup=store.InspectBackup();var state=backup.state;
                notice=state==null?"백업: 경기 시작 전":$"백업: 라운드 {state.worldRound}, 명령 {backup.commands.Count}개. 복원하면 이후 진행을 되돌립니다.";
                confirmRestore=true;
            }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        void Restore()
        {
            try { if(Session==null) { store.RestoreBackup();Session=new CompetitionSession(store,definition); } else Session.RestoreBackup();notice="백업을 복원했습니다.";confirmRestore=false; }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        public void Render()
        {
            if(!active || !ui || !isActiveAndEnabled) return;
            var state=Session?.Snapshot.state;
            var data=new CompetitionScreenData {title="공동 세계 · 모험가 경쟁",subtitle="개발 시험 · 영구 재화 지급 없음",state=state,
                status="인간 1 + AI 3 · 같은 시작 전력",detail="",history="",onClose=CancelPresentation};
            var labels=new List<string>();var actions=new List<Action>();var enabled=new List<bool>();
            void Add(string label,Action action,bool available=true) { labels.Add(label);actions.Add(()=>{if(!busy) action();});enabled.Add(available && !busy); }
            void Cmd(string label,string kind,string arg="",bool available=true)=>Add(label,()=>Dispatch(kind,arg),available);
            if(Session==null)
            {
                data.detail=notice;
                if(!busy)
                {
                    if(store!=null)
                    {
                        Add("저장 다시 불러오기",Reload);
                        if(confirmRestore) { Add("백업 복원 확정",Restore);Add("복원 취소",()=>{confirmRestore=false;Render();}); }
                        else Add("백업 내용 확인",InspectBackup);
                    }
                    else if(legacy) Add("프로필 다시 확인",OpenFromLobby);
                    Add("기존 모험으로",Close);
                }
            }
            else if(state==null)
            {
                data.detail=$"고정된 12개 장소에서 각자 사건을 해결합니다.\n영구 성장 {profile.growth}단계의 시작 전력을 네 명에게 동일 적용합니다.\n3바퀴와 최종 시련 · 최대 30라운드";
                data.history="순위: 시련 성공 → 완주 → 명예 → HP / 모두 같으면 공동 순위\n이 모드는 서버 정산 전 시험이며 성장 재화를 지급하지 않습니다.";
                Add("시험 경기 시작",StartMatch);Add("기존 모험으로",Close);
            }
            else
            {
                var p=state.participants[0];
                data.status=$"라운드 {Math.Min(state.worldRound,state.definition.maxRounds)}/{state.definition.maxRounds} · 당신 {p.rank}위 · 골드 {p.gold} · LV {p.level} · XP {p.xp}";
                data.detail=state.definition.world.places[p.position].name+" · "+state.definition.world.places[p.position].description;
                data.history=state.lastRound.Count==0?"빨강: 전쟁 · 파랑: 지혜 · 보라: 행운\n같은 칸에서도 사건과 보상은 각자 독립적입니다.":string.Join("\n",state.lastRound.Select(s=>$"{s.name}: {s.message}"));
                if(confirmAbandon && !state.finished)
                {
                    data.detail="당신의 모험을 포기할까요? 나머지 참가자는 계속 진행합니다.";
                    Cmd("포기 확정","abandon");Add("계속 모험",()=>{confirmAbandon=false;Render();});
                }
                else if(state.finished)
                {
                    data.detail="경기 종료 · "+state.endReason+"\n영구 재화 지급 0 · 서버 검증/정산은 후속 단계입니다.";
                    Add("새 시험 경기",StartMatch);Add("기존 모험으로",Close);
                }
                else
                {
                    switch(p.phase)
                    {
                        case CompetitionPhase.Map:Cmd("이동 주사위 굴리기","travel");break;
                        case CompetitionPhase.Combat:
                            data.detail=$"{p.enemy.name} · HP {p.enemy.hp}/{p.enemy.maxHp}\n{p.message}";Cmd("기호 주사위 굴리기","combat");break;
                        case CompetitionPhase.Encounter:
                            data.detail=state.definition.world.places[p.position].name+" · 사건 선택\n"+string.Join(" / ",p.offer.choices.Select(c=>c.description));
                            foreach(var choice in p.offer.choices)
                            {
                                string cost=choice.goldCost>0?$" · {choice.goldCost}골드":choice.hpCost>0?$" · HP {choice.hpCost}":"";
                                Cmd(choice.label+cost+(choice.available?"":" · "+choice.unavailableReason),"choose",choice.id,choice.available);
                            }
                            break;
                        case CompetitionPhase.LapReward:
                            data.detail=$"{p.completedLaps+1}바퀴 완주 · 개인 보상\n골드 +{state.definition.combat.lapGold} / XP +{state.definition.combat.lapXp} / HP +{state.definition.combat.lapHeal}";
                            if(p.completedLaps<state.definition.lapsToFinish-1)
                                foreach(var die in state.definition.combat.dice) Cmd(die.name+" 주사위 추가","lap",die.id);
                            else foreach(var gear in state.definition.combat.gear) Cmd(gear.name,"lap",gear.id);
                            break;
                        case CompetitionPhase.Complete:Cmd("다음 AI 라운드","advance");break;
                    }
                    Add("기존 모험으로",Close);
                    if(!p.finished && !p.eliminated) Add("모험 포기",()=>{confirmAbandon=true;Render();});
                }
            }
            if(!string.IsNullOrEmpty(notice)) data.history=notice;
            data.labels=labels.ToArray();data.actions=actions.ToArray();data.enabled=enabled.ToArray();
            if(data.labels.Length>8) throw new InvalidOperationException("화면 선택지는 8개를 초과할 수 없습니다.");
            ui.Show<CompetitionScreen>(data);
        }
    }
}
