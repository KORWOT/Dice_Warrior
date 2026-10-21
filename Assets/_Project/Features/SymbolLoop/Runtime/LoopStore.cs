using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    public static class LoopJson
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            TypeNameHandling=TypeNameHandling.None, MissingMemberHandling=MissingMemberHandling.Error,
            MaxDepth=64
        };
        public static string Encode(object value) => JsonConvert.SerializeObject(value, Settings);
        public static T Decode<T>(string value) => JsonConvert.DeserializeObject<T>(value, Settings);
        public static T Copy<T>(T value) => Decode<T>(Encode(value));
        public static string Hash(string text)
        {
            using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");
        }
    }
    public interface ILoopStore
    {
        PlayerDocument Read();
        void Commit(PlayerDocument candidate, long expectedRevision, int expectedGeneration);
    }
    public sealed class LoopStore : ILoopStore
    {
        [Serializable] sealed class Envelope { public int format=1; public string payload, checksum; }
        readonly string path;
        public LoopStore(string path) { this.path=Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))); }
        public PlayerDocument Read()
        {
            if (!File.Exists(path))
            {
                if (File.Exists(path+".backup") || File.Exists(path+".generation"))
                    throw new IOException("주 저장 파일이 없습니다. 백업을 확인해 주세요.");
                return new PlayerDocument();
            }
            var doc=ReadFile(path);
            if(doc.generation < ReadGeneration()) throw new IOException("복구가 완료되지 않았습니다. 백업 복구를 다시 진행해 주세요.");
            return doc;
        }
        public PlayerDocument InspectBackup() => LoopJson.Copy(ReadFile(path+".backup"));
        public Command ReadPending()
        {
            if(!File.Exists(path+".pending")) return null;
            var command=LoopJson.Decode<Command>(File.ReadAllText(path+".pending"));
            if(command==null || string.IsNullOrWhiteSpace(command.id)) throw new IOException("미결 입력을 확인할 수 없습니다.");
            return command;
        }
        public void WritePending(Command command)
        {
            var existing=ReadPending();
            if(existing!=null)
            {
                if(LoopJson.Encode(existing)!=LoopJson.Encode(command)) throw new IOException("이전 입력의 처리를 먼저 확인해 주세요.");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+".pending.tmp";
            File.WriteAllText(temp,LoopJson.Encode(command));
            File.Move(temp,path+".pending");
        }
        public void ClearPending(string id)
        {
            var existing=ReadPending();
            if(existing!=null && existing.id!=id) throw new IOException("미결 입력이 바뀌었습니다.");
            if(existing!=null) File.Delete(path+".pending");
        }
        public void DiscardPending()
        {
            if(File.Exists(path+".pending"))
                File.Move(path+".pending",path+".pending-discarded-"+Guid.NewGuid().ToString("N"));
        }
        public void Commit(PlayerDocument candidate, long expectedRevision, int expectedGeneration)
        {
            LoopRules.Validate(candidate);
            var current=Read();
            if(current.revision!=expectedRevision || current.generation!=expectedGeneration)
                throw new InvalidOperationException("저장 상태가 변경되었습니다. 다시 불러와 주세요.");
            if(candidate.revision!=expectedRevision+1 || candidate.generation!=expectedGeneration)
                throw new InvalidOperationException("Invalid commit version");
            Write(candidate, true);
        }
        public PlayerDocument RestoreBackup()
        {
            var restored=ReadFile(path+".backup");
            int currentGeneration=ReadGeneration();
            try { currentGeneration=Math.Max(currentGeneration, ReadFile(path).generation); }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (JsonException) { }
            restored.generation=checked(Math.Max(currentGeneration, restored.generation)+1);
            restored.revision++;
            LoopRules.Validate(restored);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(File.Exists(path)) File.Copy(path,path+".damaged-"+Guid.NewGuid().ToString("N"));
            string marker=path+".generation", tmp=marker+".tmp";
            File.WriteAllText(tmp,restored.generation.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if(File.Exists(marker)) File.Replace(tmp,marker,null); else File.Move(tmp,marker);
            Write(restored, false);
            return LoopJson.Copy(restored);
        }
        int ReadGeneration()
        {
            if(!File.Exists(path+".generation")) return 0;
            if(!int.TryParse(File.ReadAllText(path+".generation"),out int value)||value<0) throw new IOException("복구 세대 파일이 손상되었습니다.");
            return value;
        }
        static PlayerDocument ReadFile(string file)
        {
            if(!File.Exists(file)) throw new IOException("사용 가능한 백업/저장 파일이 없습니다.");
            var info=new FileInfo(file);
            if(info.Length>16*1024*1024) throw new IOException("저장 파일이 허용 크기를 초과했습니다.");
            try
            {
                var envelope=LoopJson.Decode<Envelope>(File.ReadAllText(file));
                if(envelope==null || envelope.format!=1 || envelope.payload==null || envelope.checksum!=LoopJson.Hash(envelope.payload))
                    throw new IOException("저장 파일 체크섬이 일치하지 않습니다.");
                var document=LoopJson.Decode<PlayerDocument>(envelope.payload);
                LoopRules.Validate(document);
                return document;
            }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is NullReferenceException)
            { throw new IOException("저장 데이터 형식이 올바르지 않습니다. 원본을 보존했습니다.", e); }
        }
        void Write(PlayerDocument document, bool backup)
        {
            string payload=LoopJson.Encode(document);
            string text=LoopJson.Encode(new Envelope {payload=payload,checksum=LoopJson.Hash(payload)});
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    byte[] bytes=Encoding.UTF8.GetBytes(text); stream.Write(bytes,0,bytes.Length); stream.Flush(true);
                }
                if(File.Exists(path)) File.Replace(temp,path,backup ? path+".backup" : null);
                else File.Move(temp,path);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
}
