using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiceWarrior.SymbolLoop
{
    public static class GameBackendFactory
    {
        public static IGameBackend Create(BackendMode mode, string settingsPath, Func<uint> seedSource = null, bool developmentAllowed = false)
        {
            switch (mode)
            {
                case BackendMode.HttpMock:
                    if(seedSource!=null) throw new InvalidOperationException("HTTP 서버의 시드는 클라이언트에서 지정할 수 없습니다.");
                    return new HttpGameBackend(HttpBackendSettings.Load(settingsPath),developmentAllowed);
                case BackendMode.Mock: throw new InvalidOperationException("인프로세스 Mock은 테스트의 명시적 백엔드 주입으로만 사용할 수 있습니다.");
                case BackendMode.Firebase: return new UnsupportedFirebaseBackend();
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown backend mode.");
            }
        }

        sealed class UnsupportedFirebaseBackend : IGameBackend
        {
            public Task<BackendReply> GetProfileAsync(CancellationToken ct) => Unsupported(ct);
            public Task<BackendReply> StartRunAsync(BackendRequest request, CancellationToken ct) => Unsupported(ct);
            public Task<BackendReply> SubmitRunAsync(BackendRequest request, CancellationToken ct) => Unsupported(ct);
            public Task<BackendReply> UpgradeAsync(BackendRequest request, CancellationToken ct) => Unsupported(ct);
            public Task<BackendReply> GetOperationStatusAsync(string requestId, CancellationToken ct) => Unsupported(ct);
            public Task<BackendReply> GetRunStatusAsync(string serverRunId, CancellationToken ct) => Unsupported(ct);
            static Task<BackendReply> Unsupported(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new BackendReply { status = BackendStatus.Unsupported, code = "FIREBASE_NOT_CONFIGURED",
                    message = "Firebase 백엔드는 아직 연결되지 않았습니다." });
            }
        }
    }
}
