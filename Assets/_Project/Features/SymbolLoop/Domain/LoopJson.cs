using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    // Shared by Unity and the standalone server. Existing protocol-1 JSON stays byte-compatible.
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
}
