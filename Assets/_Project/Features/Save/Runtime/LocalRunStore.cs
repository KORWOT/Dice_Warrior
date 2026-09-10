using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FateDice
{
    // The store only validates and persists already decided state. It never runs RNG or loads a live asset.
    public sealed class LocalRunStore : IRunStore
    {
        public string Path { get; }
        public bool Exists => File.Exists(Path);
        public LocalRunStore(string path)
        {
            if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("A save file path is required.",nameof(path));
            Path=System.IO.Path.GetFullPath(path);
        }
        public void Save(RunState state)
        {
            ValidateState(state);
            var payload=JsonUtility.ToJson(state);
            var envelope=new LocalSaveEnvelope{schema=RunState.CurrentSchema,payload=payload,checksum=Checksum(payload)};
            var contents=JsonUtility.ToJson(envelope);
            // A damaged/unsupported slot must be explicitly archived; an autosave may not erase its evidence.
            if(Exists)Load();
            var parent=System.IO.Path.GetDirectoryName(Path);
            Directory.CreateDirectory(parent);
            var temporary=Path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                var bytes=new UTF8Encoding(false).GetBytes(contents);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    stream.Write(bytes,0,bytes.Length);
                    stream.Flush(true);
                }
                if(Exists)File.Replace(temporary,Path,null);
                else File.Move(temporary,Path);
            }
            finally
            {
                // Never delete or truncate the destination as a replacement fallback.
                if(File.Exists(temporary))
                {
                    try{File.Delete(temporary);}
                    catch(IOException){}
                    catch(UnauthorizedAccessException){}
                }
            }
        }
        public RunState Load()
        {
            if(!Exists)throw new FileNotFoundException("No saved run exists at '"+Path+"'.",Path);
            var envelope=Parse<LocalSaveEnvelope>(File.ReadAllText(Path),"save envelope");
            if(envelope==null)throw Invalid("The save envelope is missing.");
            if(envelope.schema!=RunState.CurrentSchema)throw Invalid("Unsupported envelope schema "+envelope.schema+". Expected "+RunState.CurrentSchema+".");
            if(string.IsNullOrEmpty(envelope.payload)||string.IsNullOrWhiteSpace(envelope.checksum))throw Invalid("The save payload or checksum is missing.");
            if(!string.Equals(Checksum(envelope.payload),envelope.checksum,StringComparison.OrdinalIgnoreCase))throw Invalid("The save checksum does not match; the file may be damaged.");
            var state=Parse<RunState>(envelope.payload,"run payload");
            RestoreLegacyPresentation(state,envelope.payload);
            ValidateState(state);
            ShopRules.RestoreLegacy(state);
            return state;
        }
        public string Archive()
        {
            if(!Exists)throw new FileNotFoundException("No saved run exists to archive at '"+Path+"'.",Path);
            var archive=Path+"."+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+"."+Guid.NewGuid().ToString("N")+".bak";
            File.Move(Path,archive);
            return archive;
        }
        // Unity's inline-object deserialization creates absent groups too. Check the actual JSON
        // path, not default-filled DTOs or a substring that could match unrelated data.
        void RestoreLegacyPresentation(RunState state,string payload)
        {
            if(state==null)return; // The normal validator owns a missing payload.
            JObject payloadFields;
            try { payloadFields=JObject.Parse(payload); }
            catch(JsonException error) { throw Invalid("Invalid JSON in checkpoint compatibility: "+error.Message,error); }
            // The inline parser can round a double by one ULP. Restore these two numeric
            // fields from their exact saved JSON values, without changing the payload/file.
            if(payloadFields["playedSeconds"]!=null)
                state.playedSeconds=ReadDuration(payloadFields["playedSeconds"],"playedSeconds");
            var resultFields=payloadFields["lastResult"] as JObject;
            if(state.lastResult!=null&&resultFields?["playedSeconds"]!=null)
                state.lastResult.playedSeconds=ReadDuration(resultFields["playedSeconds"],"lastResult.playedSeconds");
            var presentation=state.config?.presentation;
            if(presentation==null)return;
            var fields=payloadFields["config"]?["presentation"] as JObject;
            if(fields==null)return;
            if(fields["explorationDice"]==null||fields["explorationDice"].Type==JTokenType.Null)
            {
                presentation.explorationDice=null;
                presentation.explorationDice=presentation.DiceTiming(false);
            }
            if(fields["combatDice"]==null||fields["combatDice"].Type==JTokenType.Null)
            {
                presentation.combatDice=null;
                presentation.combatDice=presentation.DiceTiming(true);
            }
        }
        double ReadDuration(JToken value,string name)
        {
            if(value.Type!=JTokenType.Float&&value.Type!=JTokenType.Integer)
                throw Invalid(name+" must be a numeric duration.");
            return value.Value<double>();
        }
        T Parse<T>(string json,string label)
        {
            if(string.IsNullOrWhiteSpace(json)||!json.TrimStart().StartsWith("{",StringComparison.Ordinal)||!json.TrimEnd().EndsWith("}",StringComparison.Ordinal))
                throw Invalid("Invalid JSON in "+label+".");
            try{return JsonUtility.FromJson<T>(json);}
            catch(ArgumentException error){throw Invalid("Invalid JSON in "+label+": "+error.Message,error);}
        }
        static string Checksum(string payload)
        {
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
        }
        InvalidDataException Invalid(string reason,Exception inner=null)=>new InvalidDataException("Saved run '"+Path+"': "+reason,inner);
        void ValidateState(RunState state)
        {
            try { RunStateValidator.Validate(state); }
            catch (RunStateValidationException error) { throw Invalid(error.Message, error); }
        }
    }
}
