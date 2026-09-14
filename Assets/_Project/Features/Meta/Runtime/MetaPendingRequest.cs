using System;
using System.IO;
using System.Threading.Tasks;

namespace FateDice
{
    // Serialized once at submission. All consumers receive copies, including the service adapter.
    [Serializable] public sealed class MetaPendingRequest
    {
        public string profileId, kind, payload, fingerprint;
        public static MetaPendingRequest Create(string profileId, MetaRequest request)
        {
            string kind = request is StartRunRequest ? "start" : request is LoadoutRequest ? "loadout" :
                request is GrowthRequest ? "grow" : request is SettlementRequest ? "settle" : throw new ArgumentException("Unknown meta request.");
            var value = new MetaPendingRequest { profileId = profileId, kind = kind, payload = MetaJson.Encode(request) };
            value.fingerprint = value.CalculateFingerprint(); value.Validate(profileId); return value;
        }
        public MetaPendingRequest Copy() => new MetaPendingRequest { profileId = profileId, kind = kind, payload = payload, fingerprint = fingerprint };
        string CalculateFingerprint() => MetaJson.Hash(profileId + "\n" + kind + "\n" + payload);
        public MetaRequest Request()
        {
            switch (kind)
            {
                case "start": return MetaJson.Decode<StartRunRequest>(payload);
                case "loadout": return MetaJson.Decode<LoadoutRequest>(payload);
                case "grow": return MetaJson.Decode<GrowthRequest>(payload);
                case "settle": return MetaJson.Decode<SettlementRequest>(payload);
                default: throw new InvalidDataException("알 수 없는 메타 요청 종류입니다.");
            }
        }
        public void Validate(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner) || profileId != owner || string.IsNullOrEmpty(payload) || fingerprint != CalculateFingerprint())
                throw new InvalidDataException("보관된 요청의 프로필 또는 내용이 올바르지 않습니다.");
            var r = Request();
            if (r == null || string.IsNullOrWhiteSpace(r.requestId) || r.requestId.Length > 128 || r.expectedRevision < 0 || r.expectedRecoveryGeneration == null)
                throw new InvalidDataException("보관된 요청 식별자 또는 버전이 올바르지 않습니다.");
        }
        public async Task DispatchAsync(IMetaProgressionService service)
        {
            var r = Request();
            switch (kind)
            {
                case "start": await service.StartRunAsync((StartRunRequest)r); break;
                case "loadout": await service.SaveLoadoutAsync((LoadoutRequest)r); break;
                case "grow": await service.GrowAsync((GrowthRequest)r); break;
                case "settle": await service.SettleAsync((SettlementRequest)r); break;
            }
        }
    }
    public enum MetaRequestOutcome { Unknown, Committed, Rejected, SupersededByRecovery }
    public sealed class MetaRequestResolution
    {
        public MetaRequestOutcome outcome;
        public PlayerSaveDocument snapshot;
        public string runId, message;
    }
    public interface IMetaRequestJournal
    {
        string ProfileId { get; }
        MetaPendingRequest Read();
        void Begin(MetaPendingRequest request);
        void Clear(MetaPendingRequest request);
    }
}
