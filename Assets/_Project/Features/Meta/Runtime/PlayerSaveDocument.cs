using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FateDice
{
    [Serializable] public sealed class SettlementReceipt
    {
        public string runId, outcome;
        public long currency, profileRevision;
        public SettlementReceipt Copy() => (SettlementReceipt)MemberwiseClone();
    }
    [Serializable] public sealed class MetaOperationReceipt
    {
        public string requestId, fingerprint, runId;
        public long profileRevision;
        public MetaOperationReceipt Copy() => (MetaOperationReceipt)MemberwiseClone();
    }
    [Serializable] public sealed class PlayerSaveDocument
    {
        public int schema = 1;
        public long revision;
        public PlayerProfileData profile;
        public RunState run;
        public RunStartSnapshot start;
        public bool legacyRun;
        public List<SettlementReceipt> settlements = new List<SettlementReceipt>();
        public List<MetaOperationReceipt> operations = new List<MetaOperationReceipt>();
        public PlayerSaveDocument Copy() => new PlayerSaveDocument { schema = schema, revision = revision,
            profile = profile.Copy(), run = run?.DeepCopy(), start = start?.Copy(), legacyRun = legacyRun,
            settlements = settlements.Select(x => x.Copy()).ToList(), operations = operations.Select(x => x.Copy()).ToList() };

        public void Validate(string profileId)
        {
            ProfileRules.Validate(profile);
            ProfileRules.Require(schema == 1 && revision >= 0 && profile.profileId == profileId, "저장 버전 또는 플레이어가 일치하지 않습니다.");
            ProfileRules.Require(settlements != null && operations != null &&
                settlements.All(x => x != null && !string.IsNullOrWhiteSpace(x.runId) &&
                    (x.outcome == "won" || x.outcome == "lost" || x.outcome == "abandoned" || x.outcome == "legacy") && x.currency >= 0 && x.profileRevision > 0 && x.profileRevision <= profile.revision) &&
                settlements.Select(x => x.runId).Distinct().Count() == settlements.Count &&
                operations.All(x => x != null && !string.IsNullOrWhiteSpace(x.requestId) && !string.IsNullOrWhiteSpace(x.fingerprint) && x.profileRevision > 0 && x.profileRevision <= profile.revision) &&
                operations.Select(x => x.requestId).Distinct().Count() == operations.Count, "정산 또는 명령 기록이 올바르지 않습니다.");
            if (run == null)
            { ProfileRules.Require(start == null && !legacyRun, "런 시작 기록에 진행 상태가 없습니다."); return; }
            RunStateValidator.Validate(run);
            ProfileRules.Require(run.recoveryGeneration == profile.recoveryGeneration, "프로필과 여정의 복구 세대가 일치하지 않습니다.");
            ProfileRules.Require(run.profileId == profileId, "다른 플레이어의 여정입니다.");
            if (legacyRun) ProfileRules.Require(start == null, "구형 런의 시작 기록이 올바르지 않습니다.");
            else
            {
                ProfileRules.Require(start != null && start.runId == run.runId && start.profileId == profileId &&
                    start.profileRevision >= 0 && start.profileRevision <= profile.revision && start.characterRank >= 0 &&
                    start.rulesVersion == run.config.version && start.policy != null, "런 시작 기록이 일치하지 않습니다.");
                start.policy.Validate();
                ProfileRules.ValidateOwnership(profile, start.loadout);
            }
        }
    }

    // Field-only DTO serialization avoids computed Rules properties and preserves null/empty values.
    // TypeNameHandling.None is intentional: persisted JSON must never select CLR types.
    public static class MetaJson
    {
        sealed class Fields : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization) =>
                type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(field =>
                { var p = base.CreateProperty(field, MemberSerialization.Fields); p.Readable = p.Writable = true; return p; }).ToList();
        }
        static JsonSerializerSettings Settings() => new JsonSerializerSettings { ContractResolver = new Fields(),
            TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 128 };
        public static string Encode(object value) => JsonConvert.SerializeObject(value, Settings());
        public static T Decode<T>(string value) => JsonConvert.DeserializeObject<T>(value, Settings());
        public static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }
    public interface IPlayerDataStore
    {
        IMetaRequestJournal RequestJournal { get; }
        string ProfileId { get; }
        bool Exists { get; }
        PlayerSaveDocument Read();
        void Write(long expectedRevision, PlayerSaveDocument candidate);
    }
    public sealed partial class LocalPlayerDataStore : IPlayerDataStore
    {
        public IMetaRequestJournal RequestJournal => new LocalMetaRequestJournal(Path, ProfileId);
        public string Path { get; }
        public string ProfileId { get; }
        public bool Exists => File.Exists(Path);
        public LocalPlayerDataStore(string path, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException(nameof(profileId));
            Path = System.IO.Path.GetFullPath(path); ProfileId = profileId;
        }
        public static string PlayerPath(string root, string profileId) => System.IO.Path.Combine(root, MetaJson.Hash(profileId), "player.json");
        public PlayerSaveDocument Read() => ReadBytes(File.ReadAllBytes(Path));
        PlayerSaveDocument ReadBytes(byte[] bytes)
        {
            try
            {
                string text;
                using (var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true)) text = reader.ReadToEnd();
                var envelope = MetaJson.Decode<LocalSaveEnvelope>(text);
                if (envelope == null) throw new InvalidDataException("프로필 파일 내용이 없습니다.");
                if (envelope.schema != 1) throw MetaFailure.InvalidFile(MetaFailureKind.UnsupportedSchema, "지원하지 않는 프로필 파일 버전입니다.");
                if (string.IsNullOrEmpty(envelope.payload) || MetaJson.Hash(envelope.payload) != envelope.checksum)
                    throw new InvalidDataException("프로필 파일의 체크섬이 올바르지 않습니다.");
                var doc = MetaJson.Decode<PlayerSaveDocument>(envelope.payload);
                if (doc == null) throw new InvalidDataException("프로필 내용이 없습니다.");
                if (doc.schema != 1 || (doc.profile != null && doc.profile.schema != PlayerProfileData.CurrentSchema))
                    throw MetaFailure.InvalidFile(MetaFailureKind.UnsupportedSchema, "지원하지 않는 프로필 문서 버전입니다.");
                if (doc.profile != null && doc.profile.profileId != ProfileId || doc.run != null && doc.run.profileId != ProfileId)
                    throw MetaFailure.InvalidFile(MetaFailureKind.ForeignProfile, "다른 플레이어의 프로필 또는 여정입니다.");
                doc.Validate(ProfileId); return doc;
            }
            catch (JsonException e) { throw new InvalidDataException("프로필 JSON을 읽지 못했습니다.", e); }
            catch (MetaCommandException e) { throw new InvalidDataException(e.Message, e); }
            catch (RunStateValidationException e) { throw new InvalidDataException(e.Message, e); }
        }
        public void Write(long expectedRevision, PlayerSaveDocument candidate)
        {
            candidate.Validate(ProfileId);
            ProfileRules.Require(candidate.revision == expectedRevision, "오래된 저장 문서입니다. 다시 불러와 주세요.", MetaFailureKind.Stale);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            // OS-owned exclusive file handle serializes writers, including other service instances.
            // This is our profile write lock; it never touches Unity's project lock.
            using (var writeLock = new FileStream(Path + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var previousBytes = Exists ? File.ReadAllBytes(Path) : null;
                var previous = previousBytes == null ? null : ReadBytes(previousBytes);
                long currentRevision = previous?.revision ?? 0;
                ProfileRules.Require(previous == null || previous.profile.recoveryGeneration == candidate.profile.recoveryGeneration,
                    "복구 이전의 저장 후보입니다. 다시 불러와 주세요.", MetaFailureKind.Stale);
                ProfileRules.Require(currentRevision == expectedRevision, "다른 작업이 먼저 저장했습니다. 다시 불러와 주세요.", MetaFailureKind.Stale);
                var next = candidate.Copy(); next.revision = checked(expectedRevision + 1); next.Validate(ProfileId);
                var bytes = EncodeDocument(next);
                // Preserve exactly the validated document. A bad current file never replaces a good backup.
                // Backup failure aborts before touching the primary document.
                if (previousBytes != null) WriteAtomicBytes(BackupPath, previousBytes);
                WriteAtomicBytes(Path, bytes);
            }
        }
    }
}
