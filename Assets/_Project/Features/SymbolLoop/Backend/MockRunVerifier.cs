using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    // Reconstructs a new Run from authority-owned inputs. It never accepts a client final state.
    public static class MockRunVerifier
    {
        public const string VerificationVersion = BackendProtocol.VerificationVersion;

        public static RunVerificationResult Verify(RunTicket trustedTicket,
            IReadOnlyList<RunCommandEntry> commands, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (trustedTicket == null) return Reject("REPLAY_TICKET_INVALID", "시작 티켓이 없습니다.");
            if (trustedTicket.rulesVersion != BackendProtocol.RulesVersion)
                return Reject("REPLAY_RULES_UNSUPPORTED", "이 시작 티켓의 규칙 버전은 지원하지 않습니다.");
            if (commands == null || commands.Count == 0)
                return Reject("REPLAY_LOG_EMPTY", "재현할 명령 로그가 없습니다.");
            if (commands.Count > BackendProtocol.MaxCommands)
                return Reject("REPLAY_LIMIT_EXCEEDED", "명령 개수 제한을 초과했습니다.");

            // Bound every string before serializing or executing untrusted input. Own the copies.
            var replay = new List<RunCommandEntry>(commands.Count);
            for (int i = 0; i < commands.Count; ++i)
            {
                ct.ThrowIfCancellationRequested();
                var command = commands[i];
                if (command == null || !ValidId(command.clientCommandId) || !ValidId(command.targetId) ||
                    command.arg?.Length > 1024 || !SupportedKind(command.kind))
                    return Reject("REPLAY_COMMAND_INVALID", "명령 종류·식별자·인수 형식이 올바르지 않습니다.", i + 1);
                replay.Add(new RunCommandEntry { sequence = command.sequence, kind = command.kind, arg = command.arg,
                    targetId = command.targetId, clientCommandId = command.clientCommandId });
            }
            if (Encoding.UTF8.GetByteCount(LoopJson.Encode(replay)) > MockAuthorityStore.MaxSubmissionBytes)
                return Reject("REPLAY_LIMIT_EXCEEDED", "명령 로그 용량 제한을 초과했습니다.");

            Run run;
            Profile profile;
            try
            {
                if (!ValidTicket(trustedTicket)) return Reject("REPLAY_TICKET_INVALID", "시작 티켓의 값이 올바르지 않습니다.");
                var rules = LoopJson.Copy(trustedTicket.rules);
                profile = LoopJson.Copy(trustedTicket.startingProfile.values);
                run = LoopRules.Start(rules, profile, trustedTicket.seed, trustedTicket.serverRunId);
                ValidateRun(run, profile);
            }
            catch (Exception error) when (IsInvalidData(error))
            { return Reject("REPLAY_TICKET_INVALID", "시작 상태를 재현할 수 없습니다."); }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < replay.Count; ++i)
            {
                ct.ThrowIfCancellationRequested();
                var command = replay[i];
                int sequence = i + 1;
                if (command.sequence != sequence)
                    return Reject("REPLAY_SEQUENCE_INVALID", "명령 순번이 연속되지 않습니다.", sequence);
                if (!ids.Add(command.clientCommandId))
                    return Reject("REPLAY_COMMAND_ID_DUPLICATE", "같은 명령 식별자가 중복되었습니다.", sequence);
                if (run.phase == Phase.Result)
                    return Reject("REPLAY_ALREADY_FINISHED", "종료된 런 뒤에 추가 명령이 있습니다.", sequence);
                if (!string.Equals(command.targetId, RunCommandRules.Target(run), StringComparison.Ordinal))
                    return Reject("REPLAY_TARGET_MISMATCH", "명령 대상이 재현된 상태와 일치하지 않습니다.", sequence);
                try { RunCommandRules.Apply(run, command.kind, command.arg); }
                catch (Exception error) when (IsInvalidData(error))
                { return Reject("REPLAY_COMMAND_INVALID", "현재 단계에서 실행할 수 없는 선택·비용·명령입니다.", sequence); }
                try { ValidateRun(run, profile); }
                catch (Exception error) when (IsInvalidData(error))
                { return Reject("REPLAY_STATE_INVALID", "명령 실행 결과가 허용된 상태를 벗어났습니다.", sequence); }
            }
            ct.ThrowIfCancellationRequested();
            if (run.phase != Phase.Result)
                return Reject("REPLAY_INCOMPLETE", "종료되지 않은 런은 정산할 수 없습니다.", replay.Count);
            try
            {
                int payout = checked(run.completedRegions * run.rules.regionCurrency + (run.won ? run.rules.winCurrency : 0));
                return new RunVerificationResult { accepted = true, code = "REPLAY_VERIFIED", message = "명령 로그 재현을 확인했습니다.",
                    payout = payout, finalRun = run };
            }
            catch (OverflowException)
            { return Reject("REPLAY_STATE_INVALID", "정산 금액이 허용 범위를 초과했습니다.", replay.Count); }
        }

        static bool ValidTicket(RunTicket ticket)
        {
            var profile = ticket.startingProfile;
            var rules = ticket.rules;
            return ValidId(ticket.serverRunId) && ticket.seed != 0 && profile != null &&
                profile.userId == BackendProtocol.MockUser && profile.environment == "mock" && profile.revision >= 0 &&
                profile.values != null && profile.values.currency >= 0 && profile.values.growth >= 0 &&
                profile.values.growth <= MockAuthorityStore.MaxGrowth && ValidId(profile.values.startingDie) &&
                ValidId(profile.values.startingWeapon) && rules != null && rules.dice != null && !rules.dice.Any(d => d == null) &&
                rules.gear != null && !rules.gear.Any(g => g == null) && rules.basics != null && !rules.basics.Any(s => s == null) &&
                rules.combos != null && !rules.combos.Any(s => s == null);
        }
        static bool ValidId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
        static bool SupportedKind(string value) => value == "travel" || value == "combat" || value == "choose" ||
            value == "lap" || value == "abandon";
        static void ValidateRun(Run run, Profile profile) => LoopRules.Validate(new PlayerDocument { profile = profile, run = run });
        static bool IsInvalidData(Exception error) => error is ArgumentException || error is InvalidOperationException ||
            error is NullReferenceException || error is IndexOutOfRangeException || error is OverflowException || error is JsonException;
        static RunVerificationResult Reject(string code, string message, int failedSequence = 0) =>
            new RunVerificationResult { code = code, message = message, failedSequence = failedSequence };
    }
}
