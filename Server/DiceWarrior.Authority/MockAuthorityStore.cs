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
    // This is a development authority, not a secure server or a cross-process database.
    public sealed class MockAuthorityStore : IAuthorityRepository
    {
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

        public BackendReply Execute(Func<MockAuthorityDocument, MockMutation> operation, CancellationToken ct)
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
                AuthorityValidation.Validate(document);
                return document;
            }
            catch (Exception error) when (error is JsonException || error is ArgumentException)
            { throw new IOException("Mock 권한 저장 형식이 올바르지 않습니다. 원본을 보존했습니다.", error); }
        }

        void Write(MockAuthorityDocument document, CancellationToken ct)
        {
            AuthorityValidation.Validate(document);
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
