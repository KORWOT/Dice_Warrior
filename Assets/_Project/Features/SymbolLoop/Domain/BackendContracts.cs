using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiceWarrior.SymbolLoop
{
    public static class BackendProtocol
    {
        public const int Version = 1;
        public const string RulesVersion = "symbol-loop-mvp-1";
        public const string VerificationVersion = "symbol-loop-replay-1";
        public const string MockUser = "mock-player-01";
        public const int InitialMockCurrency = 20;
        public const int MaxCommands = 2048;
    }

    public enum BackendMode { Mock, Firebase }
    public enum BackendStatus { Approved, Rejected, Conflict, PendingVerification, UnknownAfterTimeout, TemporaryFailure, Unsupported }

    [Serializable] public sealed class BackendProfile
    {
        public string userId = BackendProtocol.MockUser;
        public string environment = "mock";
        public long revision;
        public Profile values = new Profile();
    }

    [Serializable] public sealed class RunTicket
    {
        public string serverRunId, rulesVersion = BackendProtocol.RulesVersion;
        public uint seed;
        public Rules rules;
        public BackendProfile startingProfile;
    }

    [Serializable] public sealed class RunCommandEntry
    {
        public int sequence;
        public string kind, arg, targetId, clientCommandId;
    }

    [Serializable] public sealed class BackendRequest
    {
        public int protocolVersion = BackendProtocol.Version;
        public string rulesVersion = BackendProtocol.RulesVersion;
        public string requestId, kind, serverRunId;
        public long expectedRevision;
        public string startingDie = "balanced", startingWeapon = "blade", upgrade = "hp";
        public List<RunCommandEntry> commands = new List<RunCommandEntry>();
    }

    [Serializable] public sealed class SubmissionReceipt
    {
        public string requestId, serverRunId, receiptId;
        public BackendStatus status = BackendStatus.PendingVerification;
        public int appliedAmount;
        public string verificationVersion;
        public int completedRegions;
        public bool won, abandoned;
    }

    public sealed class RunVerificationResult
    {
        public bool accepted;
        public string code, message;
        public int failedSequence, payout;
        public Run finalRun;
    }

    [Serializable] public sealed class BackendReply
    {
        public BackendStatus status;
        public string code, message;
        public BackendProfile profile;
        public RunTicket run;
        public SubmissionReceipt submission;
        public List<RunCommandEntry> commands;
        public bool IsConfirmed => status == BackendStatus.Approved || status == BackendStatus.PendingVerification;
    }

    public interface IGameBackend
    {
        Task<BackendReply> GetProfileAsync(CancellationToken ct);
        Task<BackendReply> StartRunAsync(BackendRequest request, CancellationToken ct);
        Task<BackendReply> SubmitRunAsync(BackendRequest request, CancellationToken ct);
        Task<BackendReply> UpgradeAsync(BackendRequest request, CancellationToken ct);
        Task<BackendReply> GetOperationStatusAsync(string requestId, CancellationToken ct);
        Task<BackendReply> GetRunStatusAsync(string serverRunId, CancellationToken ct);
    }
}
