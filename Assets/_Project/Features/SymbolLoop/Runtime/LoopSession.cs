using System;
using System.Linq;

namespace DiceWarrior.SymbolLoop
{
    // Owns local commands/checkpoints. Permanent assets only enter through backend responses.
    public sealed class LoopSession
    {
        readonly ILoopStore store;
        readonly Rules rules;
        PlayerDocument state;
        public PlayerDocument Snapshot => LoopJson.Copy(state);
        public LoopSession(ILoopStore store, Rules rules)
        {
            this.store=store ?? throw new ArgumentNullException(nameof(store));
            LoopRules.ValidateRules(rules);
            this.rules=LoopJson.Copy(rules);
            Reload();
        }
        public void Reload() { var loaded=store.Read(); LoopRules.Validate(loaded); state=loaded; }
        public Command Prepare(string kind, string arg="", uint seed=0)
        {
            var command=new Command {id=Guid.NewGuid().ToString("N"),kind=kind,arg=arg,seed=seed,
                revision=state.revision,generation=state.generation,runId=state.run?.id};
            if(IsBackend(kind)) command.backend=new BackendRequest {
                requestId=command.id,kind=kind,expectedRevision=state.authority?.revision ?? 0,
                serverRunId=kind=="settle"?state.ticket?.serverRunId:null,
                startingDie=state.profile.startingDie,startingWeapon=state.profile.startingWeapon,
                commands=kind=="settle"?LoopJson.Copy(state.commands):new System.Collections.Generic.List<RunCommandEntry>()
            };
            return command;
        }
        public static bool IsBackend(string kind) => kind=="start" || kind=="grow" || kind=="settle";
        static string Fingerprint(Command command) => LoopJson.Hash(LoopJson.Encode(command));
        PlayerDocument ReadCommand(Command command, bool requireFresh, out bool applied)
        {
            if(command==null || string.IsNullOrWhiteSpace(command.id)) throw new ArgumentException("Request ID");
            var disk=store.Read();
            var receipt=disk.receipts.SingleOrDefault(r=>r.id==command.id);
            applied=receipt!=null;
            if(applied)
            {
                if(receipt.fingerprint!=Fingerprint(command)) throw new InvalidOperationException("동일 요청 ID의 내용이 바뀌었습니다.");
                state=disk; return disk;
            }
            if(requireFresh && (disk.revision!=command.revision || disk.generation!=command.generation || disk.run?.id!=command.runId))
                throw new InvalidOperationException("이전 화면의 입력입니다. 다시 불러와 주세요.");
            return disk;
        }
        public void ValidateBackendCommand(Command command)
        {
            var disk=ReadCommand(command,true,out bool applied); if(applied) return;
            if(!IsBackend(command.kind) || command.backend==null || command.backend.requestId!=command.id || command.backend.kind!=command.kind)
                throw new ArgumentException("Invalid backend command");
            if(command.kind=="settle")
            {
                if(disk.run==null || disk.run.phase!=Phase.Result || disk.ticket==null || command.backend.serverRunId!=disk.ticket.serverRunId)
                    throw new InvalidOperationException("제출할 여정 결과가 없습니다.");
            }
            else RequireLobby(disk.run);
        }
        public void RefreshProfile(BackendProfile profile)
        {
            ValidateProfile(profile);
            var disk=store.Read();
            if(disk.authority!=null && disk.authority.revision>profile.revision) throw new InvalidOperationException("권한 프로필이 과거 버전입니다.");
            var candidate=LoopJson.Copy(disk); SetProfile(candidate,profile);
            if(LoopJson.Encode(candidate)==LoopJson.Encode(disk)) { state=disk; return; }
            Save(candidate,disk,null);
        }
        public bool RestoreActiveRun(RunTicket ticket,BackendProfile profile)
        {
            if(ticket==null) { RefreshProfile(profile); return false; }
            var disk=store.Read();
            ValidateProfile(profile);
            if(disk.authority!=null && disk.authority.revision>profile.revision)
                throw new InvalidOperationException("권한 프로필이 과거 버전입니다. 최신 여정을 보존했습니다.");
            if(disk.run?.id==ticket.serverRunId) { RefreshProfile(profile); return false; }
            RequireLobby(disk.run);
            if(ticket.rulesVersion!=BackendProtocol.RulesVersion || ticket.seed==0 || ticket.startingProfile==null)
                throw new InvalidOperationException("복원할 수 없는 규칙의 승인 런입니다.");
            var candidate=LoopJson.Copy(disk);
            candidate.run=LoopRules.Start(LoopJson.Copy(ticket.rules),LoopJson.Copy(ticket.startingProfile.values),ticket.seed,ticket.serverRunId);
            candidate.ticket=LoopJson.Copy(ticket); candidate.commands.Clear(); candidate.submission=null;
            SetProfile(candidate,profile); Save(candidate,disk,null); return true;
        }
        static void ValidateProfile(BackendProfile profile)
        {
            if(profile==null || profile.values==null || profile.revision<0 || profile.values.currency<0 || profile.values.growth<0)
                throw new ArgumentException("Invalid backend profile");
        }
        static void SetProfile(PlayerDocument candidate,BackendProfile profile)
        {
            ValidateProfile(profile); candidate.authority=LoopJson.Copy(profile);
            candidate.profile.currency=profile.values.currency; candidate.profile.growth=profile.values.growth;
        }
        public void ReconcileSubmission(BackendReply reply)
        {
            var disk=store.Read();
            var candidate=LoopJson.Copy(disk); ApplySubmission(candidate,reply);
            if(candidate.authority==null || candidate.authority.revision<=reply.profile.revision) SetProfile(candidate,reply.profile);
            if(LoopJson.Encode(candidate)==LoopJson.Encode(disk)) { state=disk; return; }
            Save(candidate,disk,null);
        }
        static void ApplySubmission(PlayerDocument candidate,BackendReply reply)
        {
            if(reply==null || !reply.IsConfirmed || !LoopRules.ValidSubmission(reply.submission) || candidate.run==null ||
                candidate.run.id!=reply.submission.serverRunId)
                throw new InvalidOperationException("정산 기록과 로컬 여정이 다릅니다. 진행을 보존했습니다.");
            if(reply.submission.status==BackendStatus.Approved && reply.commands!=null)
            {
                if(reply.run==null || reply.run.serverRunId!=candidate.run.id || reply.run.rulesVersion!=BackendProtocol.RulesVersion)
                    throw new InvalidOperationException("승인된 여정의 복구 기록이 없습니다.");
                var run=LoopRules.Start(LoopJson.Copy(reply.run.rules),LoopJson.Copy(reply.run.startingProfile.values),reply.run.seed,reply.run.serverRunId);
                foreach(var entry in reply.commands) RunCommandRules.Apply(run,entry.kind,entry.arg);
                if(run.phase!=Phase.Result || run.completedRegions!=reply.submission.completedRegions || run.won!=reply.submission.won)
                    throw new InvalidOperationException("권한 영수증과 복구 결과가 일치하지 않습니다.");
                candidate.run=run; candidate.ticket=LoopJson.Copy(reply.run); candidate.commands=LoopJson.Copy(reply.commands);
            }
            else if(reply.submission.status==BackendStatus.PendingVerification)
            {
                // P1 records stay unverified: do not replay or award them during recovery.
                candidate.run.phase=Phase.Result;
                if(reply.commands!=null) candidate.commands=LoopJson.Copy(reply.commands);
            }
            else if(candidate.run.phase!=Phase.Result)
                throw new InvalidOperationException("과거 진행을 복구하려면 승인된 명령 기록이 필요합니다.");
            candidate.run.settled=true; candidate.run.payout=reply.submission.appliedAmount;
            candidate.submission=LoopJson.Copy(reply.submission);
        }
        public void ApplyBackend(Command command,BackendReply reply)
        {
            if(reply==null || !reply.IsConfirmed) throw new InvalidOperationException("승인되지 않은 응답입니다.");
            // A confirmed operation may outlive a local save or backup restore. Never issue a new operation for it.
            var disk=ReadCommand(command,false,out bool applied); if(applied) return;
            var candidate=LoopJson.Copy(disk);
            if(command.kind=="start")
            {
                var ticket=reply.run;
                if(ticket==null || ticket.rulesVersion!=BackendProtocol.RulesVersion || ticket.seed==0 || ticket.startingProfile==null)
                    throw new InvalidOperationException("지원되지 않는 시작 승인입니다.");
                if(candidate.run?.id!=ticket.serverRunId)
                {
                    RequireLobby(candidate.run);
                    candidate.run=LoopRules.Start(LoopJson.Copy(ticket.rules),LoopJson.Copy(ticket.startingProfile.values),ticket.seed,ticket.serverRunId);
                    candidate.ticket=LoopJson.Copy(ticket); candidate.commands.Clear(); candidate.submission=null;
                }
            }
            else if(command.kind=="settle")
            {
                if(candidate.run==null || candidate.run.id!=command.backend.serverRunId)
                    throw new InvalidOperationException("제출 응답과 로컬 여정이 다릅니다. 진행을 보존했습니다.");
                ApplySubmission(candidate,reply);
            }
            else if(command.kind!="grow") throw new ArgumentException("Unknown backend operation");
            // Old idempotent responses must not roll back a cache already refreshed to a newer server revision.
            if(candidate.authority==null || candidate.authority.revision<=reply.profile.revision) SetProfile(candidate,reply.profile);
            Save(candidate,disk,command);
        }
        public void Execute(Command command)
        {
            var disk=ReadCommand(command,true,out bool applied); if(applied) return;
            if(IsBackend(command.kind)) throw new InvalidOperationException("런 시작·정산·영구 성장은 백엔드 승인이 필요합니다.");
            var candidate=LoopJson.Copy(disk); var run=candidate.run;
            string target=run==null?"lobby":RunCommandRules.Target(run);
            switch(command.kind)
            {
                case "loadout":
                    RequireLobby(run); var parts=command.arg.Split(':');
                    if(parts.Length!=2) throw new ArgumentException("Loadout");
                    if(parts[0]=="die" && rules.dice.Any(d=>d.id==parts[1])) candidate.profile.startingDie=parts[1];
                    else if(parts[0]=="weapon" && rules.gear.Any(g=>g.id==parts[1] && g.slot==0)) candidate.profile.startingWeapon=parts[1];
                    else throw new ArgumentException("Loadout");
                    break;
                default: RunCommandRules.Apply(run,command.kind,command.arg); break;
            }
            if(command.kind!="loadout") candidate.commands.Add(new RunCommandEntry {
                sequence=candidate.commands.Count+1,kind=command.kind,arg=command.arg,targetId=target,clientCommandId=command.id});
            Save(candidate,disk,command);
        }
        void Save(PlayerDocument candidate,PlayerDocument disk,Command command)
        {
            candidate.revision=checked(disk.revision+1);
            if(command!=null) candidate.receipts.Add(new Receipt {id=command.id,fingerprint=Fingerprint(command),revision=candidate.revision});
            LoopRules.Validate(candidate); store.Commit(candidate,disk.revision,disk.generation); state=candidate;
        }
        static void RequireLobby(Run run)
        {
            if(run!=null && !(run.phase==Phase.Result && run.settled)) throw new InvalidOperationException("현재 여정의 결과를 먼저 제출해 주세요.");
        }
    }
}
