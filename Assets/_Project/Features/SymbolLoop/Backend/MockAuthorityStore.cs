using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    [Serializable] internal sealed class MockRunRecord
    {
        public string owner;
        public RunTicket ticket;
        public List<RunCommandEntry> commands;
        public SubmissionReceipt submission;
    }

    [Serializable] internal sealed class MockOperationRecord
    {
        public string id, fingerprint;
        public BackendReply reply;
    }

    [Serializable] internal sealed class MockAuthorityDocument
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

    internal sealed class MockMutation
    {
        public BackendReply reply;
        public bool changed;
    }

    // This is a development authority, not a secure server or a cross-process database.
    public sealed class MockAuthorityStore
    {
        internal const int MaxRuns = 128, MaxOperations = 1024, MaxSubmissionBytes = 512 * 1024;
        internal const int MaxGrowth = 10000;
        const long MaxAuthorityBytes = 128L * 1024 * 1024;
        const string Marker = "symbol-loop-mock-authority:1";
        static readonly ConcurrentDictionary<string, object> Gates =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        [Serializable] sealed class Envelope { public int format = 1; public string payload, checksum; }
        readonly string path;
        readonly object gate;

        public MockAuthorityStore(string path)
        {
            this.path = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
            gate = Gates.GetOrAdd(this.path, _ => new object());
        }

        internal BackendReply Execute(Func<MockAuthorityDocument, MockMutation> operation, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            lock (gate)
            {
                ct.ThrowIfCancellationRequested();
                var document = ReadOrInitialize(ct);
                var mutation = operation(document);
                if (mutation.changed) Write(document, ct);
                return LoopJson.Copy(mutation.reply);
            }
        }

        MockAuthorityDocument ReadOrInitialize(CancellationToken ct)
        {
            if (File.Exists(path))
            {
                if (!File.Exists(path + ".marker") || new FileInfo(path + ".marker").Length != Encoding.UTF8.GetByteCount(Marker) ||
                    File.ReadAllText(path + ".marker") != Marker)
                    throw new IOException("Mock 권한 저장 표식이 없거나 손상되었습니다. 원본을 보존했습니다.");
                return ReadFile();
            }
            if (File.Exists(path + ".backup") || File.Exists(path + ".marker"))
                throw new IOException("Mock 권한 주 저장 파일이 없습니다. 자동 초기화 또는 백업 복원하지 않습니다.");
            var fresh = new MockAuthorityDocument();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ct.ThrowIfCancellationRequested();
            // A crash after this durable marker must not grant a second initial balance.
            using (var marker = new FileStream(path + ".marker", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(Marker);
                marker.Write(bytes, 0, bytes.Length);
                marker.Flush(true);
            }
            Write(fresh, ct);
            return fresh;
        }

        MockAuthorityDocument ReadFile()
        {
            if (new FileInfo(path).Length > MaxAuthorityBytes) throw new IOException("Mock 권한 저장 용량을 초과했습니다.");
            try
            {
                var envelope = LoopJson.Decode<Envelope>(File.ReadAllText(path));
                if (envelope == null || envelope.format != 1 || envelope.payload == null ||
                    envelope.checksum != LoopJson.Hash(envelope.payload))
                    throw new IOException("Mock 권한 저장 체크섬이 일치하지 않습니다.");
                var document = LoopJson.Decode<MockAuthorityDocument>(envelope.payload);
                Validate(document);
                return document;
            }
            catch (Exception error) when (error is JsonException || error is ArgumentException)
            { throw new IOException("Mock 권한 저장 형식이 올바르지 않습니다. 원본을 보존했습니다.", error); }
        }

        static void Validate(MockAuthorityDocument document)
        {
            if (document == null || document.schema != 1 || !ValidProfile(document.profile) ||
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
                    !ValidProfile(run.ticket.startingProfile) ||
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
                    !ValidProfile(operation.reply.profile) || operation.reply.profile.revision > document.profile.revision)
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

        internal static bool ValidId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
        static bool ValidProfile(BackendProfile profile) => profile != null && profile.userId == BackendProtocol.MockUser &&
            profile.environment == "mock" && profile.revision >= 0 && profile.values != null &&
            profile.values.currency >= 0 && profile.values.growth >= 0 && profile.values.growth <= MaxGrowth &&
            ValidId(profile.values.startingDie) && ValidId(profile.values.startingWeapon);

        void Write(MockAuthorityDocument document, CancellationToken ct)
        {
            Validate(document);
            string payload = LoopJson.Encode(document);
            byte[] bytes = Encoding.UTF8.GetBytes(LoopJson.Encode(new Envelope { payload = payload, checksum = LoopJson.Hash(payload) }));
            if (bytes.LongLength > MaxAuthorityBytes) throw new IOException("Mock 권한 저장 용량을 초과했습니다.");
            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                ct.ThrowIfCancellationRequested();
                if (File.Exists(path)) File.Replace(temporary, path, path + ".backup");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
