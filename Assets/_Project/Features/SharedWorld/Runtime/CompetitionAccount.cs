using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    // Reuses the durable account protocol without exposing a new solo start or settlement action.
    public sealed class CompetitionAccount
    {
        readonly LoopStore store,loadoutStore;
        readonly Rules rules;
        readonly IGameBackend backend;
        readonly string loadoutPath;
        LoopSession loadout;
        public LoopSession Session { get; private set; }
        public GameBackendCoordinator Coordinator { get; private set; }
        public Command Pending { get; private set; }
        public string StorageError { get; private set; }
        public string LoadoutError { get; private set; }
        public string PendingError { get; private set; }
        public bool Busy=>Coordinator?.Busy ?? false;
        public bool HasLegacyRun=>Session?.Snapshot.run!=null && !(Session.Snapshot.run.phase==Phase.Result && Session.Snapshot.run.settled);
        public bool Ready=>StorageError==null && LoadoutError==null && PendingError==null && Pending==null && (Coordinator?.ProfileReady ?? false);
        public bool HasProfile=>Session?.Snapshot.authority!=null;
        public BackendReply LastReply=>Coordinator?.LastReply;
        public Profile Profile
        {
            get
            {
                var result=Session?.Snapshot.profile ?? new Profile();
                if(loadout!=null && File.Exists(loadoutPath))
                {
                    var selection=loadout.Snapshot.profile;result.startingDie=selection.startingDie;result.startingWeapon=selection.startingWeapon;
                }
                return result;
            }
        }
        public CompetitionAccount(string checkpointPath,string preferencePath,Rules rules,IGameBackend backend)
        {
            this.rules=LoopJson.Copy(rules ?? throw new ArgumentNullException(nameof(rules)));this.backend=backend ?? throw new ArgumentNullException(nameof(backend));
            store=new LoopStore(checkpointPath);loadoutPath=Path.GetFullPath(preferencePath);loadoutStore=new LoopStore(loadoutPath);Reload();
        }
        public void Reload()
        {
            if(Busy) throw new InvalidOperationException("계정 요청 처리 중입니다.");
            StorageError=null;LoadoutError=null;Session=null;Coordinator=null;loadout=null;
            try { Session=new LoopSession(store,rules);Coordinator=new GameBackendCoordinator(Session,store,backend); }
            catch(Exception error) { StorageError=error.Message; }
            try
            {
                loadout=new LoopSession(loadoutStore,rules);
                var document=loadout.Snapshot;
                if(document.run!=null || document.authority!=null || document.ticket!=null)
                    throw new InvalidOperationException("출전 설정 파일에 계정/여정 데이터가 있습니다. 원본을 보존했습니다.");
            }
            catch(Exception error) { loadout=null;LoadoutError=error.Message; }
            ReadPending();
        }
        void ReadPending()
        {
            Pending=null;PendingError=null;
            try { Pending=store.ReadPending(); }
            catch(Exception error) { PendingError=error.Message; }
        }
        public async Task RefreshAsync(CancellationToken ct)
        {
            if(Busy) throw new InvalidOperationException("계정 요청 처리 중입니다.");
            if(Session==null || loadout==null) Reload();else ReadPending();
            if(StorageError!=null || LoadoutError!=null || PendingError!=null || Pending!=null) return;
            try { await Coordinator.InitializeAsync(ct); }
            finally { ReadPending(); }
        }
        public void SelectLoadout(string kind,string id)
        {
            if(Busy || loadout==null) throw new InvalidOperationException("출전 설정을 확인한 뒤 다시 선택하세요.");
            if(kind!="die" && kind!="weapon") throw new ArgumentException("Unknown loadout selection");
            var previous=loadout.Snapshot;var candidate=LoopJson.Copy(previous);
            if(!File.Exists(loadoutPath))
            {
                var initial=Profile;candidate.profile.startingDie=initial.startingDie;candidate.profile.startingWeapon=initial.startingWeapon;
            }
            if(kind=="die" && rules.dice.Any(d=>d.id==id)) candidate.profile.startingDie=id;
            else if(kind=="weapon" && rules.gear.Any(g=>g.id==id && g.slot==0)) candidate.profile.startingWeapon=id;
            else throw new ArgumentException("선택할 수 없는 출전 장비입니다.");
            candidate.revision=checked(previous.revision+1);loadoutStore.Commit(candidate,previous.revision,previous.generation);loadout.Reload();
        }
        public async Task GrowAsync(CancellationToken ct)
        {
            if(!Ready) throw new InvalidOperationException("최신 프로필과 미결 요청을 먼저 확인하세요.");
            if(HasLegacyRun) throw new InvalidOperationException("이전 1인 모험 기록을 보존 중이므로 이 계정의 영구 성장 구매는 보류합니다.");
            try { await Coordinator.ExecuteAsync(Session.Prepare("grow"),ct); }
            finally { ReadPending(); }
        }
        public async Task RetryPendingAsync(CancellationToken ct)
        {
            if(Busy || Session==null) throw new InvalidOperationException("계정 저장을 먼저 복구하세요.");
            ReadPending();
            if(PendingError!=null || Pending==null) throw new InvalidOperationException("재확인할 요청을 읽을 수 없습니다. 원문을 보존했습니다.");
            if(!LoopSession.IsBackend(Pending.kind)) throw new InvalidOperationException("이전 로컬 입력은 새 모험에 적용하지 않습니다. 보존 취소를 선택하세요.");
            try { await Coordinator.ExecuteAsync(LoopJson.Copy(Pending),ct); }
            finally { ReadPending(); }
        }
        public async Task DiscardLocalPendingAsync(CancellationToken ct)
        {
            if(Busy) throw new InvalidOperationException("계정 요청 처리 중입니다.");ReadPending();
            if(Pending==null || LoopSession.IsBackend(Pending.kind) || Pending.backend!=null)
                throw new InvalidOperationException("서버 요청은 취소하지 않고 같은 ID로 확인해야 합니다.");
            store.DiscardPending(Pending.id);ReadPending();await RefreshAsync(ct);
        }
        public PlayerDocument InspectBackup(bool preferences=false)=>(preferences?loadoutStore:store).InspectBackup();
        public async Task RestoreBackupAsync(bool preferences,CancellationToken ct)
        {
            if(Busy) throw new InvalidOperationException("계정 요청 처리 중입니다.");
            (preferences?loadoutStore:store).RestoreBackup();Reload();await RefreshAsync(ct);
        }
    }
}
