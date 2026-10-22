using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;

namespace DiceWarrior.SharedWorld
{
    public interface ICompetitionStore
    {
        CompetitionDocument Read();
        void Commit(CompetitionDocument candidate, long expectedRevision, int expectedGeneration);
        CompetitionDocument InspectBackup();
        CompetitionDocument RestoreBackup();
    }
    public sealed class CompetitionStore : ICompetitionStore
    {
        [Serializable] sealed class Envelope { public int format = 1; public string payload, checksum; }
        const long MaxBytes = 4 * 1024 * 1024;
        static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        readonly string path;
        object Gate => Gates.GetOrAdd(path, _ => new object());
        public CompetitionStore(string path) { this.path = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))); }
        public CompetitionDocument Read()
        {
            lock (Gate)
            {
                if (!File.Exists(path))
                {
                    if (File.Exists(path + ".backup") || File.Exists(path + ".generation"))
                        throw new IOException("주 경기 파일이 없습니다. 백업을 확인해 주세요.");
                    return new CompetitionDocument();
                }
                var result = ReadFile(path);
                if (result.generation < ReadGeneration()) throw new IOException("백업 복구가 완료되지 않았습니다. 다시 복구해 주세요.");
                return result;
            }
        }
        public void Commit(CompetitionDocument candidate, long expectedRevision, int expectedGeneration)
        {
            lock (Gate)
            {
                ValidateDocument(candidate);
                var disk = Read();
                if (disk.revision != expectedRevision || disk.generation != expectedGeneration ||
                    candidate.revision != checked(expectedRevision + 1) || candidate.generation != expectedGeneration)
                    throw new InvalidOperationException("경기 저장 상태가 변경되었습니다.");
                if (disk.state != null && candidate.state?.matchId != disk.state.matchId)
                {
                    if (!disk.state.finished) throw new InvalidOperationException("진행 중인 경기는 교체할 수 없습니다.");
                    PreserveRaw(".completed-");
                }
                Write(candidate, true);
            }
        }
        public CompetitionDocument InspectBackup()
        {
            lock (Gate) return ReadFile(path + ".backup");
        }
        public CompetitionDocument RestoreBackup()
        {
            lock (Gate)
            {
                var backup = ReadFile(path + ".backup");
                int generation = Math.Max(backup.generation, ReadGeneration());
                if (File.Exists(path))
                {
                    try { generation = Math.Max(generation, ReadFile(path).generation); }
                    catch (IOException) { }
                    PreserveRaw(".damaged-");
                }
                backup.generation = checked(generation + 1);
                // Marker comes first: a crash after it prevents publishing the older generation.
                AtomicText(path + ".generation", backup.generation.ToString(CultureInfo.InvariantCulture), null);
                Write(backup, false);
                return LoopJson.Copy(backup);
            }
        }
        int ReadGeneration()
        {
            if (!File.Exists(path + ".generation")) return 0;
            string raw = File.ReadAllText(path + ".generation");
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 0)
                throw new IOException("경기 복구 세대 기록을 읽을 수 없습니다.");
            return value;
        }
        void PreserveRaw(string suffix)
        {
            if (!File.Exists(path)) return;
            string destination = path + suffix + Guid.NewGuid().ToString("N");
            using (var source = File.OpenRead(path))
            using (var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { source.CopyTo(target); target.Flush(true); }
        }
        CompetitionDocument ReadFile(string file)
        {
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length > MaxBytes) throw new IOException("경기 파일이 없거나 크기 한도를 초과했습니다.");
                var envelope = LoopJson.Decode<Envelope>(File.ReadAllText(file));
                if (envelope == null || envelope.format != 1 || envelope.payload == null || envelope.checksum != LoopJson.Hash(envelope.payload))
                    throw new IOException("경기 파일의 체크섬을 확인할 수 없습니다.");
                var document = LoopJson.Decode<CompetitionDocument>(envelope.payload);
                ValidateDocument(document); return document;
            }
            catch (JsonException error) { throw new IOException("경기 파일의 형식이 손상되었습니다. 원본을 보존했습니다.", error); }
            catch (ArgumentException error) { throw new IOException("경기 파일의 상태가 유효하지 않습니다.", error); }
            catch (InvalidOperationException error) { throw new IOException("경기 파일의 상태가 유효하지 않습니다.", error); }
        }
        void Write(CompetitionDocument document, bool backup)
        {
            ValidateDocument(document);
            string payload = LoopJson.Encode(document);
            string text = LoopJson.Encode(new Envelope { payload = payload, checksum = LoopJson.Hash(payload) });
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new IOException("경기 파일 크기 한도에 도달했습니다. 이전 저장을 보존했습니다.");
            AtomicText(path, text, backup && File.Exists(path) ? path + ".backup" : null);
        }
        static void AtomicText(string destination, string text, string backup)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string temp = destination + ".writing-" + Guid.NewGuid().ToString("N");
            try
            {
                byte[] data = new UTF8Encoding(false).GetBytes(text);
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(data, 0, data.Length); stream.Flush(true); }
                if (File.Exists(destination)) File.Replace(temp, destination, backup);
                else File.Move(temp, destination);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static void ValidateDocument(CompetitionDocument document)
        {
            if (document == null || document.schema != 1 || document.revision < 0 || document.generation < 0 ||
                document.commands == null || document.receipts == null || document.commands.Count > BackendProtocol.MaxCommands ||
                document.commands.Count != document.receipts.Count)
                throw new InvalidOperationException("지원하지 않는 경기 저장 구조입니다.");
            if (document.state == null)
            {
                if (document.revision != 0 || document.commands.Count != 0) throw new InvalidOperationException("경기 시작 정보가 없습니다.");
                return;
            }
            CompetitionRules.Validate(document.state);
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            long previousRevision = 0;
            for (int i = 0; i < document.commands.Count; i++)
            {
                var command = document.commands[i]; var receipt = document.receipts[i];
                if (command == null || command.sequence != i + 1 || string.IsNullOrWhiteSpace(command.clientCommandId) ||
                    command.clientCommandId.Length > 64 || !ids.Add(command.clientCommandId) ||
                    string.IsNullOrWhiteSpace(command.kind) || command.kind.Length > 32 || command.arg == null || command.arg.Length > 1024 ||
                    string.IsNullOrWhiteSpace(command.targetId) || command.targetId.Length > 512 ||
                    receipt == null || receipt.id != command.clientCommandId || receipt.fingerprint == null || receipt.fingerprint.Length != 64 ||
                    receipt.revision <= previousRevision || receipt.revision > document.revision)
                    throw new InvalidOperationException("경기 명령 기록이 유효하지 않습니다.");
                previousRevision = receipt.revision;
            }
        }
    }
}
