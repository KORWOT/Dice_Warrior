using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;

namespace DiceWarrior.SharedWorld
{
    [Serializable] public sealed class AdventurerSelection
    {
        public int schema=1;
        public long revision;
        public string adventurerId="wanderer";
    }
    // Local next-match preference only. It grants no ownership or permanent rewards.
    public sealed class AdventurerSelectionStore
    {
        [Serializable] sealed class Envelope { public int format=1;public string payload,checksum; }
        static readonly ConcurrentDictionary<string,object> Gates=new ConcurrentDictionary<string,object>(StringComparer.OrdinalIgnoreCase);
        readonly string path;
        readonly string[] ids;
        object Gate=>Gates.GetOrAdd(path,_=>new object());
        public AdventurerSelectionStore(string path,string[] allowedIds)
        {
            this.path=Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
            ids=(string[])(allowedIds ?? throw new ArgumentNullException(nameof(allowedIds))).Clone();
            if(!ids.Contains("wanderer") || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count()!=ids.Length) throw new ArgumentException("Invalid adventurer choices");
        }
        public AdventurerSelection Read()
        {
            lock(Gate)
            {
                if(!File.Exists(path))
                {
                    if(File.Exists(path+".backup")) throw new IOException("모험가 선택 원본이 없습니다. 백업을 보존했습니다.");
                    return new AdventurerSelection();
                }
                if(new FileInfo(path).Length>16384) throw new IOException("모험가 선택 파일의 크기 한도를 초과했습니다.");
                try
                {
                    var envelope=LoopJson.Decode<Envelope>(File.ReadAllText(path));
                    if(envelope==null || envelope.format!=1 || envelope.payload==null || envelope.checksum!=LoopJson.Hash(envelope.payload)) throw new IOException("모험가 선택 체크섬 오류입니다. 원본을 보존했습니다.");
                    var value=LoopJson.Decode<AdventurerSelection>(envelope.payload);
                    if(value==null || value.schema!=1 || value.revision<1 || !ids.Contains(value.adventurerId)) throw new IOException("지원하지 않는 모험가 선택 기록입니다. 원본을 보존했습니다.");
                    return value;
                }
                catch(JsonException error) { throw new IOException("모험가 선택 파일이 손상되었습니다. 원본을 보존했습니다.",error); }
            }
        }
        public AdventurerSelection Select(string id,long expectedRevision)
        {
            lock(Gate)
            {
                if(!ids.Contains(id)) throw new ArgumentException("선택할 수 없는 모험가입니다.");
                var previous=Read();
                if(previous.revision!=expectedRevision) throw new InvalidOperationException("모험가 선택이 다른 화면에서 변경되었습니다. 다시 읽어 주세요.");
                var next=new AdventurerSelection {adventurerId=id,revision=checked(previous.revision+1)};
                string payload=LoopJson.Encode(next),temp=path+".writing-"+Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                try
                {
                    byte[] bytes=new UTF8Encoding(false).GetBytes(LoopJson.Encode(new Envelope {payload=payload,checksum=LoopJson.Hash(payload)}));
                    using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                    if(File.Exists(path)) File.Replace(temp,path,path+".backup");else File.Move(temp,path);
                }
                finally {if(File.Exists(temp)) File.Delete(temp);}
                return next;
            }
        }
    }
}
