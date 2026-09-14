using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace FateDice
{
    public sealed class ProfileBackupPreview
    {
        public string Fingerprint { get; }
        public DateTime SavedAtUtc { get; }
        public long ProfileRevision { get; }
        public int EventsResolved { get; }
        internal ProfileBackupPreview(string fingerprint, DateTime savedAtUtc, PlayerSaveDocument document)
        { Fingerprint = fingerprint; SavedAtUtc = savedAtUtc; ProfileRevision = document.profile.revision; EventsResolved = document.run?.eventsResolved ?? 0; }
    }
    public interface IPlayerDataRecoveryStore
    {
        ProfileBackupPreview InspectBackup();
        PlayerSaveDocument RestoreBackup(ProfileBackupPreview preview);
    }
    public sealed partial class LocalPlayerDataStore : IPlayerDataRecoveryStore
    {
        public string BackupPath => Path + ".backup";
        public string RecoveryArchiveDirectory => Path + ".recovery";

        public ProfileBackupPreview InspectBackup()
        {
            if (!File.Exists(BackupPath)) return null;
            var bytes = File.ReadAllBytes(BackupPath);
            var document = ReadBytes(bytes);
            return new ProfileBackupPreview(Fingerprint(bytes), File.GetLastWriteTimeUtc(BackupPath), document);
        }
        public PlayerSaveDocument RestoreBackup(ProfileBackupPreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            using (var writeLock = new FileStream(Path + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var backup = File.ReadAllBytes(BackupPath);
                var restored = ReadBytes(backup);
                ProfileRules.Require(Fingerprint(backup) == preview.Fingerprint, "확인한 뒤 백업이 변경되었습니다. 백업 정보를 다시 확인해 주세요.", MetaFailureKind.Stale);
                var damaged = File.ReadAllBytes(Path);
                bool invalid = false;
                try { ReadBytes(damaged); }
                catch (InvalidDataException) { invalid = true; }
                ProfileRules.Require(invalid, "현재 저장이 정상입니다. 최신 저장을 다시 불러와 주세요.");

                // Receipts, progression and rules come from one document. Only the ownership fence changes.
                string generation = Guid.NewGuid().ToString("N");
                restored.profile.recoveryGeneration = generation;
                if (restored.run != null) restored.run.recoveryGeneration = generation;
                restored.Validate(ProfileId);
                var restoredBytes = EncodeDocument(restored);
                Directory.CreateDirectory(RecoveryArchiveDirectory);
                string archive = System.IO.Path.Combine(RecoveryArchiveDirectory, "damaged-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json");
                // CreateNew + flush must succeed before replacing the only damaged original.
                using (var file = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(damaged, 0, damaged.Length); file.Flush(true); }
                WriteAtomicBytes(Path, restoredBytes);
                return restored.Copy();
            }
        }
        static string Fingerprint(byte[] bytes)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", ""); }

        static byte[] EncodeDocument(PlayerSaveDocument document)
        {
            string payload = MetaJson.Encode(document);
            return new UTF8Encoding(false).GetBytes(MetaJson.Encode(new LocalSaveEnvelope {
                schema = 1, payload = payload, checksum = MetaJson.Hash(payload) }));
        }
        static void WriteAtomicBytes(string destination, byte[] bytes)
        {
            string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(destination)) File.Replace(temp, destination, null); else File.Move(temp, destination);
            }
            finally
            {
                if (File.Exists(temp))
                    try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }
}
