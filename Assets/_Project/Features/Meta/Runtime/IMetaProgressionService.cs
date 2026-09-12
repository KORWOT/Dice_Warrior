using System;
using System.Threading.Tasks;

namespace FateDice
{
    [Serializable] public class MetaRequest { public string requestId; public long expectedRevision; }
    [Serializable] public sealed class LoadoutRequest : MetaRequest { public RunLoadout loadout; }
    [Serializable] public sealed class StartRunRequest : MetaRequest { public bool abandonActiveRun; }
    [Serializable] public sealed class SettlementRequest : MetaRequest { public string runId; }
    [Serializable] public sealed class GrowthRequest : MetaRequest { public string characterId; }

    // A future authenticated Firebase adapter supplies an already loaded profile cache and async
    // authoritative commands. UID, costs, grants and accepted run evidence belong to that adapter/server.
    // Local run checkpoints remain a separate device operation, not Firestore writes every frame.
    public interface IMetaProgressionService
    {
        PlayerProfileData Profile { get; }
        MetaPolicy Policy { get; }
        IRunStore RunStore { get; }
        SettlementReceipt Settlement(string runId);
        bool NeedsSettlement { get; }
        Task<PlayerProfileData> SaveLoadoutAsync(LoadoutRequest request);
        Task<RunState> StartRunAsync(StartRunRequest request);
        Task<SettlementReceipt> SettleAsync(SettlementRequest request);
        Task<PlayerProfileData> GrowAsync(GrowthRequest request);
    }
}
