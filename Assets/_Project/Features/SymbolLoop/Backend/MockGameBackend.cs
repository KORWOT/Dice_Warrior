using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiceWarrior.SymbolLoop
{
    public sealed class MockGameBackend : IGameBackend
    {
        readonly MockAuthorityStore store;
        readonly Func<uint> seedSource;
        readonly Rules rules;
        readonly Func<long> nowMilliseconds;

        public MockGameBackend(MockAuthorityStore store, Func<uint> seedSource = null, Rules rules = null, Func<long> nowMilliseconds = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.seedSource = seedSource ?? CreateSeed;
            this.rules = LoopJson.Copy(rules ?? new Rules());
            LoopRules.ValidateRules(this.rules);
            this.nowMilliseconds=nowMilliseconds ?? (()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public Task<BackendReply> GetProfileAsync(CancellationToken ct) => Execute(document => new MockMutation {
            reply = Reply(document, BackendStatus.Approved, "PROFILE", "개발용 Mock 프로필입니다.", ActiveTicket(document))
        }, ct);

        public Task<BackendReply> GetOperationStatusAsync(string requestId, CancellationToken ct) => Execute(document => {
            var operation = document.operations.Find(item => item.id == requestId);
            return new MockMutation { reply = operation == null
                ? Reply(document, BackendStatus.Rejected, "OPERATION_NOT_FOUND", "확인된 작업이 없습니다. 같은 요청 ID로 재시도하세요.")
                : operation.reply };
        }, ct);

        public Task<BackendReply> GetRunStatusAsync(string serverRunId, CancellationToken ct) => Execute(document => {
            var run=document.runs.Find(item=>item.ticket.serverRunId==serverRunId && item.owner==document.profile.userId);
            return new MockMutation {reply=run==null
                ? Reply(document,BackendStatus.Rejected,"RUN_NOT_FOUND","이 계정의 런을 찾을 수 없습니다.")
                : Reply(document,run.submission?.status ?? BackendStatus.Approved,
                    run.submission==null?"RUN_ACTIVE":"RUN_RECEIPT","권한 저장소에서 여정 상태를 확인했습니다.",
                    run.ticket,run.submission,run.commands)};
        },ct);

        public Task<BackendReply> StartRunAsync(BackendRequest request, CancellationToken ct) =>
            Mutate(request, "start", Start, ct);
        public Task<BackendReply> SubmitRunAsync(BackendRequest request, CancellationToken ct) =>
            Mutate(request, "settle", (document,input)=>Submit(document,input,ct), ct);
        public Task<BackendReply> UpgradeAsync(BackendRequest request, CancellationToken ct) =>
            Mutate(request, "grow", Upgrade, ct);

        Task<BackendReply> Mutate(BackendRequest request, string kind,
            Func<MockAuthorityDocument, BackendRequest, BackendReply> action, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Snapshot every field before fingerprinting; caller-owned objects never become authority state.
            if (request == null || !MockAuthorityStore.ValidId(request.requestId))
                return Task.FromResult(new BackendReply { status = BackendStatus.Rejected, code = "INVALID_REQUEST_ID",
                    message = "유효한 요청 ID가 필요합니다." });
            if (request.commands != null && request.commands.Count > BackendProtocol.MaxCommands)
                return Task.FromResult(new BackendReply { status = BackendStatus.Rejected, code = "COMMAND_LIMIT",
                    message = "런 명령 수가 개발용 Mock 한도를 초과했습니다." });
            string encoded = LoopJson.Encode(request);
            if (Encoding.UTF8.GetByteCount(encoded) > MockAuthorityStore.MaxSubmissionBytes)
                return Task.FromResult(new BackendReply { status = BackendStatus.Rejected, code = "REQUEST_TOO_LARGE",
                    message = "요청 용량이 512KiB를 초과했습니다." });
            var snapshot = LoopJson.Decode<BackendRequest>(encoded);
            string fingerprint = LoopJson.Hash(kind + ":" + encoded);
            return Execute(document => {
                var previous = document.operations.Find(item => item.id == snapshot.requestId);
                if (previous != null) return new MockMutation { reply = previous.fingerprint == fingerprint
                    ? previous.reply : Reply(document, BackendStatus.Rejected, "REQUEST_ID_REUSED", "같은 요청 ID에 다른 내용이 사용되었습니다.") };
                if (document.operations.Count >= MockAuthorityStore.MaxOperations)
                    return new MockMutation { reply = Reply(document, BackendStatus.Rejected, "OPERATION_LIMIT", "Mock 작업 보관 한도에 도달했습니다.") };

                BackendReply reply;
                if (snapshot.protocolVersion != BackendProtocol.Version || snapshot.rulesVersion != BackendProtocol.RulesVersion)
                    reply = Reply(document, BackendStatus.Rejected, "VERSION_MISMATCH", "지원하지 않는 프로토콜 또는 규칙 버전입니다.");
                else if (snapshot.kind != kind)
                    reply = Reply(document, BackendStatus.Rejected, "WRONG_OPERATION", "요청 종류가 호출한 작업과 다릅니다.");
                else reply = CheckRate(document,snapshot) ?? action(document, snapshot);
                document.operations.Add(new MockOperationRecord { id = snapshot.requestId, fingerprint = fingerprint,
                    reply = LoopJson.Copy(reply) });
                return new MockMutation { reply = reply, changed = true };
            }, ct);
        }

        BackendReply Start(MockAuthorityDocument document, BackendRequest request)
        {
            var conflict = CheckRevision(document, request);
            if (conflict != null) return conflict;
            if (document.activeRunId != null) return Reply(document, BackendStatus.Conflict, "RUN_ACTIVE", "진행 중인 런을 먼저 제출하세요.");
            if (document.runs.Count >= MockAuthorityStore.MaxRuns)
                return Reply(document, BackendStatus.Rejected, "RUN_LIMIT", "Mock 런 보관 한도에 도달했습니다.");
            if (!rules.dice.Any(die => die.id == request.startingDie) ||
                !rules.gear.Any(gear => gear.id == request.startingWeapon && gear.slot == 0))
                return Reply(document, BackendStatus.Rejected, "INVALID_LOADOUT", "출전 주사위 또는 무기를 사용할 수 없습니다.");
            uint seed = seedSource();
            if (seed == 0) throw new InvalidOperationException("Backend seed source returned zero.");
            document.profile.values.startingDie = request.startingDie;
            document.profile.values.startingWeapon = request.startingWeapon;
            document.profile.revision++;
            var ticket = new RunTicket { serverRunId = Guid.NewGuid().ToString("N"), seed = seed,
                rulesVersion = BackendProtocol.RulesVersion, rules = LoopJson.Copy(rules),
                startingProfile = LoopJson.Copy(document.profile) };
            document.activeRunId = ticket.serverRunId;
            document.runs.Add(new MockRunRecord { owner = document.profile.userId, ticket = ticket });
            return Reply(document, BackendStatus.Approved, "RUN_STARTED", "Mock 권한에서 런을 시작했습니다.", ticket);
        }

        BackendReply Upgrade(MockAuthorityDocument document, BackendRequest request)
        {
            var conflict = CheckRevision(document, request);
            if (conflict != null) return conflict;
            if (document.activeRunId != null) return Reply(document, BackendStatus.Conflict, "RUN_ACTIVE", "런 진행 중에는 영구 성장을 변경할 수 없습니다.");
            if (request.upgrade != "hp") return Reply(document, BackendStatus.Rejected, "INVALID_UPGRADE", "지원하지 않는 영구 성장입니다.");
            if (document.profile.values.growth >= MockAuthorityStore.MaxGrowth)
                return Reply(document, BackendStatus.Rejected, "GROWTH_LIMIT", "성장 보관 한도에 도달했습니다.");
            long cost = 20L * (document.profile.values.growth + 1L);
            if (document.profile.values.currency < cost)
                return Reply(document, BackendStatus.Rejected, "INSUFFICIENT_CURRENCY", "영구 성장 재화가 부족합니다.");
            document.profile.values.currency -= (int)cost;
            document.profile.values.growth++;
            document.profile.revision++;
            return Reply(document, BackendStatus.Approved, "UPGRADED", "영구 HP 성장을 적용했습니다.");
        }

        BackendReply Submit(MockAuthorityDocument document, BackendRequest request,CancellationToken ct)
        {
            var run = document.runs.Find(item => item.ticket.serverRunId == request.serverRunId);
            if (run == null || run.owner != document.profile.userId)
                return Reply(document, BackendStatus.Rejected, "RUN_NOT_FOUND", "이 계정의 런을 찾을 수 없습니다.");
            if (run.submission != null)
                return Reply(document, run.submission.status,
                    run.submission.status==BackendStatus.Approved?"RUN_VERIFIED":"PENDING_VERIFICATION",
                    run.submission.status==BackendStatus.Approved?"이미 승인된 정산 영수증입니다.":"기존 P1 검증 대기 기록을 그대로 보존합니다.", submission:run.submission);
            var conflict = CheckRevision(document, request);
            if (conflict != null) return conflict;
            if (document.activeRunId != request.serverRunId)
                return Reply(document, BackendStatus.Conflict, "RUN_NOT_ACTIVE", "활성 런이 변경되었습니다.");
            if (!ValidCommands(request.commands))
                return Reply(document, BackendStatus.Rejected, "INVALID_COMMANDS", "명령 로그의 순번, ID 또는 형식을 확인하세요.");
            var verified=MockRunVerifier.Verify(run.ticket,request.commands,ct);
            if(!verified.accepted)
                return Reply(document,BackendStatus.Rejected,verified.code,
                    $"기록 검증 실패 · 명령 {verified.failedSequence}: {verified.message}");
            bool abandoned=request.commands.Last().kind=="abandon";
            if(abandoned && document.abandonsInWindow>=10)
                return Reply(document,BackendStatus.Rejected,"ABANDON_RATE_LIMIT","60초 동안 여정 포기는 최대 10회입니다.");
            if(document.profile.values.currency>int.MaxValue-verified.payout)
                return Reply(document,BackendStatus.Rejected,"CURRENCY_LIMIT","성장 재화 보관 한도에 도달했습니다.");
            run.commands = LoopJson.Copy(request.commands);
            run.submission = new SubmissionReceipt { requestId = request.requestId, serverRunId = request.serverRunId,
                receiptId = Guid.NewGuid().ToString("N"), status = BackendStatus.Approved, appliedAmount = verified.payout,
                verificationVersion=BackendProtocol.VerificationVersion,completedRegions=verified.finalRun.completedRegions,
                won=verified.finalRun.won,abandoned=abandoned };
            document.profile.values.currency=checked(document.profile.values.currency+verified.payout);
            if(abandoned) document.abandonsInWindow++;
            document.activeRunId = null;
            document.profile.revision++;
            return Reply(document, BackendStatus.Approved, "RUN_VERIFIED",
                $"기록 재현 검증 완료 · 성장 재화 {verified.payout} 지급",submission:run.submission);
        }

        BackendReply CheckRate(MockAuthorityDocument document,BackendRequest request)
        {
            if(request.kind!="start" && request.kind!="settle") return null;
            // Receipt lookup is not a new start/submission. It never consumes a rate slot.
            if(request.kind=="settle" && document.runs.Any(r=>r.ticket.serverRunId==request.serverRunId && r.submission!=null)) return null;
            long now=nowMilliseconds();
            if(now<0) throw new InvalidOperationException("Backend clock returned a negative time.");
            if(!document.rateWindowOpen || now-document.rateWindowStartMs>=60000)
            {
                document.rateWindowOpen=true; document.rateWindowStartMs=now;
                document.startsInWindow=document.submitsInWindow=document.abandonsInWindow=0;
            }
            // A clock rollback cannot open a new window.
            if(request.kind=="start")
            {
                if(document.startsInWindow>=10) return Reply(document,BackendStatus.Rejected,"START_RATE_LIMIT","60초 동안 런 시작 요청은 최대 10회입니다.");
                document.startsInWindow++;
            }
            else
            {
                if(document.submitsInWindow>=20) return Reply(document,BackendStatus.Rejected,"SUBMIT_RATE_LIMIT","60초 동안 새로운 제출 요청은 최대 20회입니다.");
                document.submitsInWindow++;
            }
            return null;
        }

        internal static bool ValidCommands(List<RunCommandEntry> commands)
        {
            if (commands == null || commands.Count > BackendProtocol.MaxCommands) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < commands.Count; index++)
            {
                var command = commands[index];
                if (command == null || command.sequence != index + 1 ||
                    (command.kind != "travel" && command.kind != "combat" && command.kind != "choose" && command.kind != "lap" && command.kind != "abandon") ||
                    !MockAuthorityStore.ValidId(command.clientCommandId) || !ids.Add(command.clientCommandId) ||
                    (command.arg != null && command.arg.Length > 1024) ||
                    (command.targetId != null && command.targetId.Length > 128)) return false;
            }
            return true;
        }

        static BackendReply CheckRevision(MockAuthorityDocument document, BackendRequest request)
        {
            if (request.expectedRevision != document.profile.revision)
                return Reply(document, BackendStatus.Conflict, "REVISION_CONFLICT", "권한 프로필이 변경되었습니다. 최신 프로필을 확인하세요.");
            return document.profile.revision == long.MaxValue
                ? Reply(document, BackendStatus.Rejected, "REVISION_LIMIT", "권한 프로필 revision 한도에 도달했습니다.") : null;
        }

        static RunTicket ActiveTicket(MockAuthorityDocument document) => document.activeRunId == null ? null :
            document.runs.Single(run => run.ticket.serverRunId == document.activeRunId).ticket;
        static BackendReply Reply(MockAuthorityDocument document, BackendStatus status, string code, string message,
            RunTicket run = null, SubmissionReceipt submission = null,List<RunCommandEntry> commands=null) => new BackendReply {
                status = status, code = code, message = message, profile = LoopJson.Copy(document.profile),
                run = run == null ? null : LoopJson.Copy(run), submission = submission == null ? null : LoopJson.Copy(submission),
                commands=commands==null?null:LoopJson.Copy(commands)
            };

        Task<BackendReply> Execute(Func<MockAuthorityDocument, MockMutation> action, CancellationToken ct)
        {
            try { return Task.FromResult(store.Execute(action, ct)); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is SecurityException)
            {
                return Task.FromResult(new BackendReply { status = BackendStatus.TemporaryFailure, code = "AUTHORITY_STORAGE_UNAVAILABLE",
                    message = "Mock 권한 저장소를 사용할 수 없습니다. 원본을 보존하고 같은 요청으로 다시 확인하세요." });
            }
        }

        static uint CreateSeed()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                uint value;
                do { random.GetBytes(bytes); value = BitConverter.ToUInt32(bytes, 0); } while (value == 0);
                return value;
            }
        }
    }
}
