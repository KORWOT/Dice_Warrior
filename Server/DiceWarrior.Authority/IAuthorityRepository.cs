using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiceWarrior.SymbolLoop
{
    // The callback owns a detached snapshot. Repositories may retry it on CAS conflicts.
    // Callbacks must keep external side effects out; replies become visible only after commit.
    public interface IAuthorityRepository
    {
        BackendReply Execute(Func<MockAuthorityDocument, MockMutation> operation, CancellationToken ct);
    }

    [Serializable] public sealed class MockRunRecord
    {
        public string owner;
        public RunTicket ticket;
        public List<RunCommandEntry> commands;
        public SubmissionReceipt submission;
    }

    [Serializable] public sealed class MockOperationRecord
    {
        public string id, fingerprint;
        public BackendReply reply;
    }

    [Serializable] public sealed class MockAuthorityDocument
    {
        public int schema = 1;
        public BackendProfile profile = new BackendProfile {
            values = new Profile { currency = BackendProtocol.InitialMockCurrency }
        };
        public string activeRunId;
        public List<MockRunRecord> runs = new List<MockRunRecord>();
        public List<MockOperationRecord> operations = new List<MockOperationRecord>();
        public bool rateWindowOpen;
        public long rateWindowStartMs;
        public int startsInWindow, submitsInWindow, abandonsInWindow;
    }

    public sealed class MockMutation
    {
        public BackendReply reply;
        public bool changed;
    }

    public static class AuthorityValidation
    {
        public const int MaxRuns = 128, MaxOperations = 1024, MaxSubmissionBytes = 512 * 1024;
        public const int MaxGrowth = 10000;
        public static void Validate(MockAuthorityDocument document, string userId = BackendProtocol.MockUser)
        {
            if (document == null || document.schema != 1 || !ValidProfile(document.profile, userId) ||
                document.runs == null || document.runs.Count > MaxRuns ||
                document.operations == null || document.operations.Count > MaxOperations || document.rateWindowStartMs<0 ||
                document.startsInWindow<0 || document.submitsInWindow<0 || document.abandonsInWindow<0)
                throw new ArgumentException("Invalid Mock authority document");
            var runIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var run in document.runs)
            {
                if (run == null || run.owner != document.profile.userId || run.ticket == null ||
                    !ValidId(run.ticket.serverRunId) || !runIds.Add(run.ticket.serverRunId) ||
                    run.ticket.rulesVersion != BackendProtocol.RulesVersion || run.ticket.seed == 0 ||
                    !ValidProfile(run.ticket.startingProfile, userId) ||
                    run.ticket.startingProfile.revision > document.profile.revision ||
                    run.ticket.startingProfile.userId != run.owner)
                    throw new ArgumentException("Invalid Mock run ticket");
                ValidateStoredRules(run.ticket.rules);
                if (run.submission == null)
                {
                    if (run.commands != null || run.ticket.serverRunId != document.activeRunId)
                        throw new ArgumentException("Invalid Mock active run");
                }
                else if (run.commands == null || !MockGameBackend.ValidCommands(run.commands) ||
                    Encoding.UTF8.GetByteCount(LoopJson.Encode(run.commands)) > MaxSubmissionBytes ||
                    run.submission.serverRunId != run.ticket.serverRunId || !ValidId(run.submission.requestId) ||
                    !ValidId(run.submission.receiptId) || !LoopRules.ValidSubmission(run.submission) ||
                    (run.submission.status==BackendStatus.Approved && run.submission.appliedAmount!=
                        checked(run.submission.completedRegions*run.ticket.rules.regionCurrency+(run.submission.won?run.ticket.rules.winCurrency:0))) ||
                    run.ticket.serverRunId == document.activeRunId)
                    throw new ArgumentException("Invalid Mock submission");
            }
            if (document.activeRunId != null && !runIds.Contains(document.activeRunId))
                throw new ArgumentException("Missing Mock active run");
            var operationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var operation in document.operations)
            {
                if (operation == null || !ValidId(operation.id) || !operationIds.Add(operation.id) ||
                    operation.fingerprint == null || operation.fingerprint.Length != 64 ||
                    operation.fingerprint.Any(c => !Uri.IsHexDigit(c)) || operation.reply == null ||
                    !Enum.IsDefined(typeof(BackendStatus), operation.reply.status) ||
                    !ValidProfile(operation.reply.profile, userId) || operation.reply.profile.revision > document.profile.revision)
                    throw new ArgumentException("Invalid Mock operation receipt");
            }
        }

        static void ValidateStoredRules(Rules rules)
        {
            if (rules == null || rules.dice == null || rules.dice.Any(d => d == null) ||
                rules.gear == null || rules.gear.Any(g => g == null) ||
                rules.basics == null || rules.basics.Any(s => s == null) ||
                rules.combos == null || rules.combos.Any(s => s == null))
                throw new ArgumentException("Invalid Mock rules");
            LoopRules.ValidateRules(rules);
        }

        public static bool ValidId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
        static bool ValidProfile(BackendProfile profile, string userId) => profile != null && profile.userId == userId && ValidId(userId) &&
            profile.environment == "mock" && profile.revision >= 0 && profile.values != null &&
            profile.values.currency >= 0 && profile.values.growth >= 0 && profile.values.growth <= MaxGrowth &&
            ValidId(profile.values.startingDie) && ValidId(profile.values.startingWeapon);

    }
}
