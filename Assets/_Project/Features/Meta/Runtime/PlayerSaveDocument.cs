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
        string ProfileId { get; }
        bool Exists { get; }
        PlayerSaveDocument Read();
        void Write(long expectedRevision, PlayerSaveDocument candidate);
    }
    public sealed class LocalPlayerDataStore : IPlayerDataStore
    {
        public string Path { get; }
        public string ProfileId { get; }
        public bool Exists => File.Exists(Path);
        public LocalPlayerDataStore(string path, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException(nameof(profileId));
            Path = System.IO.Path.GetFullPath(path); ProfileId = profileId;
        }
        public static string PlayerPath(string root, string profileId) => System.IO.Path.Combine(root, MetaJson.Hash(profileId), "player.json");
        public PlayerSaveDocument Read()
        {
            try
            {
                var envelope = MetaJson.Decode<LocalSaveEnvelope>(File.ReadAllText(Path));
                if (envelope == null || envelope.schema != 1 || string.IsNullOrEmpty(envelope.payload) || MetaJson.Hash(envelope.payload) != envelope.checksum)
                    throw new InvalidDataException("프로필 파일의 버전 또는 체크섬이 올바르지 않습니다.");
                var doc = MetaJson.Decode<PlayerSaveDocument>(envelope.payload);
                if (doc == null) throw new InvalidDataException("프로필 내용이 없습니다.");
                doc.Validate(ProfileId); return doc;
            }
            catch (JsonException e) { throw new InvalidDataException("프로필 JSON을 읽지 못했습니다.", e); }
            catch (MetaCommandException e) { throw new InvalidDataException(e.Message, e); }
            catch (RunStateValidationException e) { throw new InvalidDataException(e.Message, e); }
        }
        public void Write(long expectedRevision, PlayerSaveDocument candidate)
        {
            candidate.Validate(ProfileId);
            ProfileRules.Require(candidate.revision == expectedRevision, "오래된 저장 문서입니다. 다시 불러와 주세요.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            // OS-owned exclusive file handle serializes writers, including other service instances.
            // This is our profile write lock; it never touches Unity's project lock.
            using (var writeLock = new FileStream(Path + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                long currentRevision = Exists ? Read().revision : 0;
                ProfileRules.Require(currentRevision == expectedRevision, "다른 작업이 먼저 저장했습니다. 다시 불러와 주세요.");
                var next = candidate.Copy(); next.revision = checked(expectedRevision + 1); next.Validate(ProfileId);
                string payload = MetaJson.Encode(next);
                var bytes = new UTF8Encoding(false).GetBytes(MetaJson.Encode(new LocalSaveEnvelope { schema = 1, payload = payload, checksum = MetaJson.Hash(payload) }));
                string temp = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                    if (Exists) File.Replace(temp, Path, null); else File.Move(temp, Path);
                }
                finally
                {
                    if (File.Exists(temp))
                        try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }
    }
}
