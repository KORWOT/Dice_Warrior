using System;
using System.IO;
using System.Net;

namespace DiceWarrior.SymbolLoop
{
    [Serializable] public sealed class HttpBackendSettings
    {
        public string endpoint = "http://127.0.0.1:5087";
        public string userId = BackendProtocol.MockUser;
        public int timeoutSeconds = 15;

        public static HttpBackendSettings Load(string path)
        {
            if(!File.Exists(path)) return new HttpBackendSettings();
            if(new FileInfo(path).Length>16384) throw new InvalidOperationException("HTTP Mock 설정 파일이 너무 큽니다.");
            return LoopJson.Decode<HttpBackendSettings>(File.ReadAllText(path))
                ?? throw new InvalidOperationException("HTTP Mock 설정 파일이 비어 있습니다.");
        }
        public Uri Validate(bool developmentAllowed)
        {
            if(!developmentAllowed) throw new InvalidOperationException("HTTP Mock은 Editor 또는 개발 빌드에서만 사용할 수 있습니다.");
            if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) ||
                (uri.Scheme!=Uri.UriSchemeHttp && uri.Scheme!=Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath!="/" ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("HTTP Mock 주소는 경로·계정 정보 없는 로컬 서버 주소여야 합니다.");
            string host=uri.Host.Trim('[',']');
            if(!string.Equals(host,"localhost",StringComparison.OrdinalIgnoreCase) &&
                (!IPAddress.TryParse(host,out var ip) || !IPAddress.IsLoopback(ip)))
                throw new InvalidOperationException("개발 신원은 루프백 서버에만 전송할 수 있습니다.");
            ValidateUserId(userId);
            if(timeoutSeconds<1 || timeoutSeconds>120) throw new InvalidOperationException("HTTP Mock 제한 시간은 1~120초여야 합니다.");
            return new Uri(uri.GetLeftPart(UriPartial.Authority)+"/");
        }
        public string CheckpointPath(string persistentRoot)
        {
            var uri=Validate(true);
            string key=LoopJson.Hash(uri.AbsoluteUri+"\n"+userId).ToLowerInvariant();
            return Path.Combine(persistentRoot,"symbol-loop-backend-v1","http-mock",key,"checkpoint.json");
        }
        internal static void ValidateId(string value,string name)
        {
            if(string.IsNullOrEmpty(value) || value.Length>128) throw new ArgumentException("개발 계정 또는 요청 ID 길이가 올바르지 않습니다.",name);
            foreach(char c in value)
                if(!((c>='a' && c<='z') || (c>='A' && c<='Z') || (c>='0' && c<='9') || c=='-' || c=='_' || c=='.' || c==':'))
                    throw new ArgumentException("개발 계정 또는 요청 ID에 허용되지 않는 문자가 있습니다.",name);
        }
        static void ValidateUserId(string value)
        {
            if(string.IsNullOrEmpty(value) || value.Length>64) throw new ArgumentException("개발 계정 ID는 1~64자여야 합니다.",nameof(userId));
            foreach(char c in value)
                if(!((c>='a' && c<='z') || (c>='A' && c<='Z') || (c>='0' && c<='9') || c=='-' || c=='_'))
                    throw new ArgumentException("개발 계정 ID에는 영문·숫자·밑줄·하이픈만 사용할 수 있습니다.",nameof(userId));
        }
    }
}
