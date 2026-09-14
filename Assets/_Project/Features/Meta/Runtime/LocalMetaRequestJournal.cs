using System;
using System.IO;
using System.Text;

namespace FateDice
{
    // One pending intent per profile, separate from rollback-able progression/receipts.
    public sealed class LocalMetaRequestJournal : IMetaRequestJournal
    {
        readonly string profilePath;
        public string Path => profilePath + ".pending";
        public string ProfileId { get; }
        public LocalMetaRequestJournal(string profilePath, string profileId)
        { this.profilePath = System.IO.Path.GetFullPath(profilePath); ProfileId = profileId; }
        public MetaPendingRequest Read()
        {
            if (!File.Exists(Path)) return null;
            try
            {
                var e = MetaJson.Decode<LocalSaveEnvelope>(File.ReadAllText(Path));
                if (e == null || e.schema != 1 || string.IsNullOrEmpty(e.payload) || e.checksum != MetaJson.Hash(e.payload))
                    throw new InvalidDataException("보관된 요청 기록을 확인하지 못했습니다. 원본을 유지합니다.");
                var request = MetaJson.Decode<MetaPendingRequest>(e.payload);
                if (request == null) throw new InvalidDataException("보관된 요청 내용이 없습니다.");
                request.Validate(ProfileId); return request;
            }
            catch (Newtonsoft.Json.JsonException e) { throw new InvalidDataException("보관된 요청을 읽지 못했습니다.", e); }
        }
        public void Begin(MetaPendingRequest request)
        {
            request.Validate(ProfileId);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(profilePath));
            using (var gate = new FileStream(profilePath + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var current = Read();
                if (current != null)
                {
                    ProfileRules.Require(current.fingerprint == request.fingerprint, "먼저 보관된 요청의 결과를 확인해 주세요.");
                    return;
                }
                string payload = MetaJson.Encode(request);
                var bytes = new UTF8Encoding(false).GetBytes(MetaJson.Encode(new LocalSaveEnvelope { schema = 1,
                    payload = payload, checksum = MetaJson.Hash(payload) }));
                string temp = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var f = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { f.Write(bytes, 0, bytes.Length); f.Flush(true); }
                    File.Move(temp, Path);
                }
                finally { if (File.Exists(temp)) try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
            }
        }
        public void Clear(MetaPendingRequest request)
        {
            request.Validate(ProfileId);
            using (var gate = new FileStream(profilePath + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var current = Read();
                ProfileRules.Require(current != null && current.fingerprint == request.fingerprint,
                    "보관된 요청이 바뀌었습니다. 새로운 요청은 지우지 않습니다.", MetaFailureKind.Stale);
                File.Delete(Path);
            }
        }
    }
}
