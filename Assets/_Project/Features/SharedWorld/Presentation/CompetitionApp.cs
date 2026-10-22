using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiceWarrior.SymbolLoop;
using FateDice;
using UnityEngine;

namespace DiceWarrior.SharedWorld
{
    public sealed class CompetitionApp : MonoBehaviour
    {
        public UIManager ui;
        public TextAsset content;
        public SymbolLoopConfig combatConfig;
        public CompetitionSession Session { get; private set; }
        public CompetitionAccount Account { get; private set; }
        public bool IsPresenting=>busy;
        public bool IsOpen=>active;
        public bool InLobby=>lobby;
        CompetitionStore store;
        CompetitionDefinition definition;
        Profile profile;
        Func<uint> seedSource;
        IGameBackend transport;
        readonly CancellationTokenSource lifetime=new CancellationTokenSource();
        bool active,busy,opened,ownsTransport,paused,lobby=true,confirmAbandon,confirmRestore,confirmAccountRestore,restorePreferences;
        string notice="",menu="",competitionError="";
        Coroutine presentation;
        int width,height;
        Rect safeArea;

        void Start()
        {
            if(Application.platform==RuntimePlatform.Android) UnityEngine.Screen.orientation=ScreenOrientation.Portrait;
            if(!opened) OpenDefault();
        }
        void OpenDefault()
        {
            try
            {
                var settings=HttpBackendSettings.Load(Path.Combine(Application.persistentDataPath,"symbol-loop-backend-v1","http-settings.json"));
                OpenHttp(settings,Application.persistentDataPath);
            }
            catch(Exception error) { active=opened=true;notice=error.Message;Render(); }
        }
        void OnEnable() { if(active) Render(); }
        void OnDisable()=>CancelPresentation();
        void OnApplicationPause(bool value) { paused=value;if(value) CancelPresentation();else if(active) Render(); }
        void Update()
        {
            if(!active || !ui) return;
            if(width==UnityEngine.Screen.width && height==UnityEngine.Screen.height && safeArea==UnityEngine.Screen.safeArea) return;
            width=UnityEngine.Screen.width;height=UnityEngine.Screen.height;safeArea=UnityEngine.Screen.safeArea;ui.Root.ApplySafeArea();
        }
        CompetitionDefinition LoadDefinition()
        {
            if(!content || !combatConfig) throw new InvalidOperationException("공동 세계 콘텐츠 연결을 확인해 주세요.");
            var result=CompetitionContent.Load(content.text);result.combat=combatConfig.CreateRules();return result;
        }
        public void OpenHttp(HttpBackendSettings settings,string persistentRoot)
        {
            if(Account!=null || transport!=null) throw new InvalidOperationException("이미 연결된 계정입니다.");
            var rules=LoadDefinition();string accountPath=settings.CheckpointPath(persistentRoot);
            var backend=new HttpGameBackend(settings,Application.isEditor || Debug.isDebugBuild);
            try
            {
                OpenAccount(accountPath,Path.Combine(Path.GetDirectoryName(accountPath),"shared-world-preview-v1","checkpoint.json"),rules,backend);
                ownsTransport=true;
            }
            catch { if(Account==null) transport=null;backend.Dispose();throw; }
        }
        public void OpenAccount(string accountPath,string competitionPath,CompetitionDefinition rules,IGameBackend backend,Func<uint> seeds=null)
        {
            if(busy || Account!=null) throw new InvalidOperationException("이미 열려 있거나 처리 중인 계정입니다.");
            definition=LoopJson.Copy(rules);seedSource=seeds;transport=backend;
            Account=new CompetitionAccount(accountPath,Path.Combine(Path.GetDirectoryName(competitionPath),"lobby-loadout.json"),definition.combat,backend);
            active=opened=true;lobby=true;profile=Account.Profile;store=new CompetitionStore(competitionPath);
            LoadCompetition();AccountAction(ct=>Account.RefreshAsync(ct));
        }
        // Tests inject isolated state. The authored Title uses HTTP and server-confirmed profiles.
        public void Open(string path,CompetitionDefinition rules,Profile startingProfile,Func<uint> seeds=null)
        {
            if(Account!=null) throw new InvalidOperationException("연결된 계정을 로컬 프로필로 대체할 수 없습니다.");
            active=opened=true;lobby=true;menu="";definition=LoopJson.Copy(rules);profile=LoopJson.Copy(startingProfile);seedSource=seeds;
            store=new CompetitionStore(path);LoadCompetition();Render();
        }
        void LoadCompetition()
        {
            Session=null;
            try { Session=new CompetitionSession(store,definition);notice="";competitionError=""; }
            catch(Exception error) { competitionError=notice=error.Message; }
        }
        string AccountMessage()
        {
            if(Account==null) return "";
            if(Account.StorageError!=null) return "계정 저장 오류 · "+Account.StorageError;
            if(Account.LoadoutError!=null) return "출전 설정 오류 · "+Account.LoadoutError;
            if(Account.PendingError!=null) return "미결 요청 손상 · 원문 보존 · "+Account.PendingError;
            if(Account.Pending!=null) return "이전 요청의 결과가 미확정입니다. 계정 관리에서 같은 요청을 재확인하세요.";
            if(!Account.Ready) return Account.LastReply?.message ?? "서버 프로필을 확인해야 새 경기를 시작할 수 있습니다.";
            if(Account.HasLegacyRun) return "이전 1인 모험 기록 보존 중 · 이 계정의 영구 성장 구매는 보류합니다.";
            return Account.LastReply?.message ?? "";
        }
        async void AccountAction(Func<CancellationToken,Task> action)
        {
            if(busy || Account==null) return;busy=true;Render();
            try { await action(lifetime.Token);if(this) { profile=Account.Profile;notice=AccountMessage(); } }
            catch(Exception error) { if(this) { profile=Account.Profile;notice=error.Message; } }
            finally { busy=false;if(this) Render(); }
        }
        async void StartMatch()
        {
            if(busy || Session==null) return;busy=true;Render();
            try
            {
                if(Account!=null)
                {
                    await Account.RefreshAsync(lifetime.Token);
                    if(!this) return;
                    if(!Account.Ready) throw new InvalidOperationException(AccountMessage());
                    profile=Account.Profile;
                }
                uint seed=seedSource!=null?seedSource():BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(),0)|1u;
                Session.Start(LoopJson.Copy(profile),seed,Guid.NewGuid().ToString("N"));lobby=false;menu="";notice="";
            }
            catch(Exception error) { if(this) notice=error.Message; }
            finally { busy=false;if(this) Render(); }
        }
        void SelectLoadout(string kind,string id)
        {
            try
            {
                if(Account!=null) { Account.SelectLoadout(kind,id);profile=Account.Profile; }
                else if(kind=="die") profile.startingDie=id;else profile.startingWeapon=id;
                menu="";notice="";
            }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        void Resume()
        {
            try { Session.Reload();lobby=false;menu="";notice=""; }
            catch(Exception error) { Session=null;notice=error.Message; }
            Render();
        }
        void Dispatch(string kind,string arg="")
        {
            if(busy || Session==null) return;busy=true;var before=Session.Snapshot.state;
            try { Session.Execute(Session.Prepare(kind,arg));notice="";confirmAbandon=false; }
            catch(Exception error) { notice=error.Message;busy=false;Render();return; }
            var after=Session.Snapshot.state;
            if(!isActiveAndEnabled || !active || paused) { busy=false;return; }
            if(before!=null && after!=null && ui.ActiveScreen is CompetitionScreen screen)
            { ui.SetInputLocked(true);presentation=StartCoroutine(Present(screen.playback.Play(kind,before,after)));return; }
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
            presentation=null;busy=false;ui.SetInputLocked(false);Render();
        }
        void CancelPresentation()
        {
            if(presentation!=null) { StopCoroutine(presentation);presentation=null;busy=false;if(ui) ui.SetInputLocked(false); }
        }
        public void Close()
        {
            if(busy && presentation==null) return;
            CancelPresentation();lobby=true;menu="";confirmAbandon=false;Render();
        }
        void Reload() { LoadCompetition();lobby=true;Render(); }
        void InspectBackup()
        {
            try
            {
                var backup=store.InspectBackup();var state=backup.state;
                notice=state==null?"경기 백업: 시작 전":$"경기 백업: 라운드 {state.worldRound}, 명령 {backup.commands.Count}개. 이후 진행을 되돌립니다.";confirmRestore=true;
            }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        void Restore()
        {
            try { if(Session==null) { store.RestoreBackup();Session=new CompetitionSession(store,definition); } else Session.RestoreBackup();notice="경기 백업을 복원했습니다.";confirmRestore=false;lobby=true; }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        void InspectAccountBackup(bool preferences)
        {
            try
            {
                var backup=Account.InspectBackup(preferences);restorePreferences=preferences;confirmAccountRestore=true;
                notice=preferences?$"출전 백업: {backup.profile.startingDie} / {backup.profile.startingWeapon}":$"계정 백업: 성장 {backup.profile.growth} · 재화 {backup.profile.currency}. 미결 서버 요청은 유지합니다.";
            }
            catch(Exception error) { notice=error.Message; }
            Render();
        }
        public void Render()
        {
            if(!active || !ui || !isActiveAndEnabled || paused) return;
            var state=Session?.Snapshot.state;
            var data=new CompetitionScreenData {title="DICE WARRIOR · 공동 모험",subtitle="인간 1 + AI 3 · 경기 성장 보상은 검증 전까지 0",state=lobby?null:state,
                status="같은 세계 · 각자의 모험",detail="",history="",onClose=CancelPresentation};
            var labels=new List<string>();var actions=new List<Action>();var enabled=new List<bool>();
            void Add(string label,Action action,bool available=true) { labels.Add(label);actions.Add(()=>{if(!busy) action();});enabled.Add(available && !busy); }
            void Cmd(string label,string kind,string arg="",bool available=true)=>Add(label,()=>Dispatch(kind,arg),available);
            if(confirmAccountRestore)
            {
                data.detail="백업을 복원하면 이후의 로컬 상태가 되돌아갑니다. 계정 권한은 서버에서 다시 확인합니다.";
                Add("계정 백업 복원 확정",()=>{confirmAccountRestore=false;AccountAction(ct=>Account.RestoreBackupAsync(restorePreferences,ct));});
                Add("복원 취소",()=>{confirmAccountRestore=false;Render();});
            }
            else if(confirmRestore)
            {
                data.detail="경기 백업을 복원할까요? 원본은 보존합니다.";Add("경기 백업 복원 확정",Restore);Add("복원 취소",()=>{confirmRestore=false;Render();});
            }
            else if(menu=="account" && Account!=null)
            {
                data.detail=AccountMessage();
                Add("계정 저장 다시 읽기",()=>AccountAction(async ct=>{Account.Reload();await Account.RefreshAsync(ct);}));
                if(Account.Pending!=null)
                {
                    if(LoopSession.IsBackend(Account.Pending.kind)) Add("같은 요청 다시 확인",()=>AccountAction(ct=>Account.RetryPendingAsync(ct)));
                    else if(Account.Pending.backend==null) Add("이전 입력 보존 취소",()=>{menu="discard";Render();});
                }
                Add("계정 백업 확인",()=>InspectAccountBackup(false));Add("출전 백업 확인",()=>InspectAccountBackup(true));
                Add("로비로",()=>{menu="";Render();});
            }
            else if(menu=="discard")
            {
                data.detail="이전 모험의 미결 로컬 입력을 보존 사본으로 옮기고 취소할까요? 서버 요청은 취소하지 않습니다.";
                Add("입력 보존 취소 확정",()=>{menu="account";AccountAction(ct=>Account.DiscardLocalPendingAsync(ct));});Add("돌아가기",()=>{menu="account";Render();});
            }
            else if(Session==null)
            {
                data.detail="경기 저장 또는 연결을 열 수 없습니다. 기존 기록은 보존했습니다.\n"+competitionError;
                if(store!=null) { Add("경기 저장 다시 읽기",Reload);Add("경기 백업 확인",InspectBackup); }
                else Add("HTTP 설정 다시 불러오기",OpenDefault);
                if(Account!=null) Add("계정 관리",()=>{menu="account";Render();});
            }
            else if(lobby || state==null)
            {
                var selection=profile ?? new Profile();
                string die=definition.combat.dice.FirstOrDefault(d=>d.id==selection.startingDie)?.name ?? "주사위 재선택 필요";
                string weapon=definition.combat.gear.FirstOrDefault(g=>g.id==selection.startingWeapon)?.name ?? "무기 재선택 필요";
                data.status=Account!=null && !Account.HasProfile?"서버 프로필 미확인":$"성장 재화 {selection.currency} · 영구 성장 {selection.growth}단계"+(Account!=null && !Account.Ready?" · 캐시":"");
                data.detail=$"출전: {die} 주사위 / {weapon}\n고정된 12곳 · 3바퀴와 최종 시련 · 최대 30라운드\n네 모험가는 동일한 시작 성장 전력을 적용받습니다.";
                data.history=AccountMessage();
                if(menu=="loadout")
                {
                    foreach(var item in definition.combat.dice) { string id=item.id;Add(item.name+" 주사위",()=>SelectLoadout("die",id)); }
                    foreach(var item in definition.combat.gear.Where(g=>g.slot==0)) { string id=item.id;Add(item.name,()=>SelectLoadout("weapon",id)); }
                    Add("로비로",()=>{menu="";Render();});
                }
                else
                {
                    if(state!=null && !state.finished) Add("경기 이어하기",Resume);
                    else Add("경기 시작",StartMatch,Account==null || Account.Ready);
                    if(state?.finished==true) Add("지난 경기 결과",Resume);
                    Add("출전 선택",()=>{menu="loadout";Render();},Account==null || Account.LoadoutError==null);
                    if(Account!=null)
                    {
                        long price=20L*(selection.growth+1L);
                        Add(Account.HasLegacyRun?"영구 성장 · 이전 기록 보존으로 보류":$"영구 HP +5 · {price} 재화",()=>AccountAction(ct=>Account.GrowAsync(ct)),Account.Ready && !Account.HasLegacyRun && selection.currency>=price);
                        Add("프로필 다시 확인",()=>AccountAction(ct=>Account.RefreshAsync(ct)));Add("계정 관리",()=>{menu="account";Render();});
                    }
                    Add("경기 백업 확인",InspectBackup);
                }
            }
            else
            {
                var p=state.participants[0];data.state=state;
                data.status=$"라운드 {Math.Min(state.worldRound,state.definition.maxRounds)}/{state.definition.maxRounds} · 당신 {p.rank}위 · 골드 {p.gold} · LV {p.level} · XP {p.xp}";
                data.detail=state.definition.world.places[p.position].name+" · "+state.definition.world.places[p.position].description;
                data.history=state.lastRound.Count==0?"빨강: 전쟁 · 파랑: 지혜 · 보라: 행운\n같은 칸에서도 사건과 보상은 각자 독립적입니다.":string.Join("\n",state.lastRound.Select(s=>$"{s.name}: {s.message}"));
                if(confirmAbandon && !state.finished)
                {
                    data.detail="당신의 모험을 포기할까요? 나머지 참가자는 계속 진행합니다.";Cmd("포기 확정","abandon");Add("계속 모험",()=>{confirmAbandon=false;Render();});
                }
                else if(state.finished)
                { data.detail="경기 종료 · "+state.endReason+"\n영구 재화 지급 0 · 서버 검증/정산은 후속 단계입니다.";Add("로비로",Close); }
                else
                {
                    switch(p.phase)
                    {
                        case CompetitionPhase.Map:Cmd("이동 주사위 굴리기","travel");break;
                        case CompetitionPhase.Combat:data.detail=$"{p.enemy.name} · HP {p.enemy.hp}/{p.enemy.maxHp}\n{p.message}";Cmd("기호 주사위 굴리기","combat");break;
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
                            if(p.completedLaps<state.definition.lapsToFinish-1) foreach(var die in state.definition.combat.dice) Cmd(die.name+" 주사위 추가","lap",die.id);
                            else foreach(var gear in state.definition.combat.gear) Cmd(gear.name,"lap",gear.id);
                            break;
                        case CompetitionPhase.Complete:Cmd("다음 AI 라운드","advance");break;
                    }
                    Add("로비로",Close);if(!p.finished && !p.eliminated) Add("모험 포기",()=>{confirmAbandon=true;Render();});
                }
            }
            if(!string.IsNullOrEmpty(notice)) data.history=notice;
            data.labels=labels.ToArray();data.actions=actions.ToArray();data.enabled=enabled.ToArray();
            if(data.labels.Length>8) throw new InvalidOperationException("화면 선택지는 8개를 초과할 수 없습니다.");
            ui.Show<CompetitionScreen>(data);
        }
        void OnDestroy()
        {
            active=false;lifetime.Cancel();if(ownsTransport) (transport as IDisposable)?.Dispose();lifetime.Dispose();if(ui) ui.CloseAll();
        }
    }
}
