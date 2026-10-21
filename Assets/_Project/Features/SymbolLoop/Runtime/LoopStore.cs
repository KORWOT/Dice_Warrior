using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    public interface ILoopStore
    {
        PlayerDocument Read();
        void Commit(PlayerDocument candidate, long expectedRevision, int expectedGeneration);
    }
    public sealed class LoopStore : ILoopStore
    {
        [Serializable] sealed class Envelope { public int format=1; public string payload, checksum; }
        readonly string path;
        static readonly ConcurrentDictionary<string,object> Gates = new ConcurrentDictionary<string,object>(StringComparer.OrdinalIgnoreCase);
        object Gate => Gates.GetOrAdd(path,_=>new object());
        public LoopStore(string path) { this.path=Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))); }
        public PlayerDocument Read()
        {
            lock(Gate)
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
        }
        public PlayerDocument InspectBackup()
        {
            lock(Gate) return LoopJson.Copy(ReadFile(path+".backup"));
        }
        public Command ReadPending()
        {
            lock(Gate)
            {
                if(!File.Exists(path+".pending")) return null;
                var command=LoopJson.Decode<Command>(File.ReadAllText(path+".pending"));
                if(command==null || string.IsNullOrWhiteSpace(command.id)) throw new IOException("미결 입력을 확인할 수 없습니다.");
                return command;
            }
        }
        public void WritePending(Command command)
        {
            lock(Gate)
            {
                if(command==null || string.IsNullOrWhiteSpace(command.id)) throw new ArgumentException("Request ID");
                string text=LoopJson.Encode(command);
                var existing=ReadPending();
                if(existing!=null)
                {
                    if(LoopJson.Encode(existing)!=text) throw new IOException("이전 입력의 처리를 먼저 확인해 주세요.");
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp=path+".pending-"+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    WriteText(temp,text);
                    File.Move(temp,path+".pending");
                }
                finally { if(File.Exists(temp)) File.Delete(temp); }
            }
        }
        public void ClearPending(string id)
        {
            lock(Gate)
            {
                var existing=ReadPending();
                if(existing!=null && existing.id!=id) throw new IOException("미결 입력이 바뀌었습니다.");
                if(existing!=null) File.Delete(path+".pending");
            }
        }
        public void DiscardPending(string expectedId=null)
        {
            lock(Gate)
            {
                if(expectedId!=null)
                {
                    var existing=ReadPending();
                    if(existing!=null && existing.id!=expectedId) throw new IOException("미결 입력이 바뀌었습니다.");
                }
                if(File.Exists(path+".pending"))
                    File.Move(path+".pending",path+".pending-discarded-"+Guid.NewGuid().ToString("N"));
            }
        }
        public string PreserveCheckpoint()
        {
            lock(Gate)
            {
                if(!File.Exists(path)) return null;
                string preserved=path+".reconciled-"+Guid.NewGuid().ToString("N")+".json";
                CopyRaw(path,preserved);
                return preserved;
            }
        }
        public void Commit(PlayerDocument candidate, long expectedRevision, int expectedGeneration)
        {
            lock(Gate)
            {
                LoopRules.Validate(candidate);
                var current=Read();
                if(current.revision!=expectedRevision || current.generation!=expectedGeneration)
                    throw new InvalidOperationException("저장 상태가 변경되었습니다. 다시 불러와 주세요.");
                if(candidate.revision!=expectedRevision+1 || candidate.generation!=expectedGeneration)
                    throw new InvalidOperationException("Invalid commit version");
                Write(candidate, true);
            }
        }
        public void RecordBackendFailure(Command command,BackendReply reply,string diagnostic=null)
        {
            lock(Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string file=path+".backend-error-"+Guid.NewGuid().ToString("N")+".json";
                WriteText(file,LoopJson.Encode(new {command,reply,diagnostic}));
            }
        }
        public PlayerDocument RestoreBackup()
        {
            lock(Gate)
            {
                var restored=ReadFile(path+".backup");
                int currentGeneration=ReadGeneration();
                try { currentGeneration=Math.Max(currentGeneration, ReadFile(path).generation); }
                catch (IOException) { }
                catch (ArgumentException) { }
                catch (JsonException) { }
                restored.generation=checked(Math.Max(currentGeneration, restored.generation)+1);
                restored.revision=checked(restored.revision+1);
                LoopRules.Validate(restored);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(File.Exists(path)) CopyRaw(path,path+".damaged-"+Guid.NewGuid().ToString("N"));
                string marker=path+".generation", tmp=marker+"-"+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    WriteText(tmp,restored.generation.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if(File.Exists(marker)) File.Replace(tmp,marker,null); else File.Move(tmp,marker);
                }
                finally { if(File.Exists(tmp)) File.Delete(tmp); }
                Write(restored, false);
                return LoopJson.Copy(restored);
            }
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
                WriteText(temp,text);
                if(File.Exists(path)) File.Replace(temp,path,backup ? path+".backup" : null);
                else File.Move(temp,path);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
        static void WriteText(string file,string text)
        {
            using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(text);
                stream.Write(bytes,0,bytes.Length); stream.Flush(true);
            }
        }
        static void CopyRaw(string source,string destination)
        {
            string temp=destination+".tmp";
            try
            {
                using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                { input.CopyTo(output); output.Flush(true); }
                File.Move(temp,destination);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
}
