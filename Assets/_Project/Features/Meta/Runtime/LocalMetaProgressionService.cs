using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FateDice
{
    // Development authority only. Checksums and local receipts protect consistency, not cheating.
    // Never promote these balances/results into an authenticated Firebase account as trusted data.
    public sealed class LocalMetaProgressionService : IMetaProgressionService
    {
        readonly IPlayerDataStore store;
        readonly Func<GameConfigData> content;
        readonly MetaProgressionConfig config;
        readonly ISeedSource seed;
        readonly IRunStore legacy;
        readonly PlayerProfileData initial;
        public IRunStore RunStore { get; }
        public PlayerProfileData Profile => Read().profile.Copy();
        public MetaPolicy Policy { get { var p = config.policy.Copy(); p.Validate(); return p; } }
        public bool NeedsSettlement
        { get { var d = Read(); return d.run != null && d.run.phase == RunPhase.Result && !d.settlements.Any(x => x.runId == d.run.runId); } }
        public SettlementReceipt Settlement(string runId) => Read().settlements.FirstOrDefault(x => x.runId == runId)?.Copy();

        public LocalMetaProgressionService(IPlayerDataStore store, Func<GameConfigData> content,
            MetaProgressionConfig config, ISeedSource seed = null, IRunStore legacy = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.config = config ? config : throw new ArgumentNullException(nameof(config));
            this.seed = seed ?? new SystemSeedSource(); this.legacy = legacy;
            initial = config.CreateProfile(store.ProfileId, content());
            RunStore = new Checkpoints(this);
        }
        PlayerSaveDocument Read()
        {
            if (store.Exists) return store.Read();
            var d = new PlayerSaveDocument { profile = initial.Copy() };
            if (legacy != null && legacy.Exists)
            {
                d.run = legacy.Load().DeepCopy(); d.run.profileId = store.ProfileId; d.legacyRun = true;
            }
            d.Validate(store.ProfileId); return d;
        }
        void Commit(PlayerSaveDocument d) { d.Validate(store.ProfileId); store.Write(d.revision, d); }
        static void Request(MetaRequest r)
        { ProfileRules.Require(r != null && !string.IsNullOrWhiteSpace(r.requestId) && r.requestId.Length <= 128 && r.expectedRevision >= 0, "요청 식별자와 버전을 확인해 주세요."); }
        static string Fingerprint(string kind, object input) => MetaJson.Hash(kind + ":" + MetaJson.Encode(input));
        static MetaOperationReceipt Replay(PlayerSaveDocument d, MetaRequest r, string fingerprint)
        {
            Request(r);
            var previous = d.operations.FirstOrDefault(x => x.requestId == r.requestId);
            ProfileRules.Require(previous == null || previous.fingerprint == fingerprint, "같은 요청 식별자에 다른 내용을 사용할 수 없습니다.");
            return previous;
        }
        static void Revision(PlayerSaveDocument d, MetaRequest r)
        { ProfileRules.Require(d.profile.revision == r.expectedRevision, "프로필이 변경되었습니다. 최신 상태에서 다시 시도해 주세요."); }
        static void Complete(PlayerSaveDocument d, MetaRequest r, string fingerprint, string runId = null)
        {
            d.profile.revision = checked(d.profile.revision + 1);
            d.operations.Add(new MetaOperationReceipt { requestId = r.requestId, fingerprint = fingerprint,
                runId = runId, profileRevision = d.profile.revision });
        }
        static Task<T> Execute<T>(Func<T> operation)
        { try { return Task.FromResult(operation()); } catch (Exception e) { return Task.FromException<T>(e); } }

        public Task<PlayerProfileData> SaveLoadoutAsync(LoadoutRequest request) => Execute(() =>
        {
            Request(request); var d = Read(); var fingerprint = Fingerprint("loadout", request.loadout);
            if (Replay(d, request, fingerprint) != null) return d.profile.Copy();
            Revision(d, request); ProfileRules.ValidateLoadout(d.profile, request.loadout, content());
            d.profile.loadout = request.loadout.Copy(); Complete(d, request, fingerprint); Commit(d); return d.profile.Copy();
        });

        public Task<RunState> StartRunAsync(StartRunRequest request) => Execute(() =>
        {
            Request(request); var d = Read(); var fingerprint = Fingerprint("start", request.abandonActiveRun);
            var replay = Replay(d, request, fingerprint);
            if (replay != null)
            {
                ProfileRules.Require(d.run != null && d.run.runId == replay.runId, "이미 처리한 출발 요청입니다. 현재 여정을 확인해 주세요.");
                return d.run.DeepCopy();
            }
            Revision(d, request);
            if (d.run != null && !d.settlements.Any(x => x.runId == d.run.runId))
            {
                ProfileRules.Require(d.run.phase != RunPhase.Result, "이전 여정의 성장 재화를 먼저 정산해 주세요.");
                ProfileRules.Require(request.abandonActiveRun, "진행 중인 여정을 포기한 뒤 새 여정을 시작해 주세요.");
                d.settlements.Add(new SettlementReceipt { runId = d.run.runId, outcome = "abandoned", profileRevision = d.profile.revision + 1 });
            }
            var rules = content().DeepCopy(); var policy = Policy; var l = d.profile.loadout;
            ProfileRules.ValidateLoadout(d.profile, l, rules);
            int rank = d.profile.characters.Single(x => x.characterId == l.characterId).rank;
            rules.growth.startingMaxHp = checked(rules.growth.startingMaxHp + rank * policy.hpPerRank);
            rules.growth.startingPower = checked(rules.growth.startingPower + rank * policy.powerPerRank);
            rules.growth.startingGuard = checked(rules.growth.startingGuard + rank * policy.guardPerRank);
            // Factory candidate only; no checkpoint is published until the whole player document commits.
            var candidate = RunSession.New(rules, seed.NextSeed(), l.wildcardId, l.cap, previousResult: d.run?.lastResult).ReadSnapshot();
            candidate.profileId = store.ProfileId;
            candidate.equipmentIds = l.equipment.Select(id => string.IsNullOrEmpty(id) ? null : d.profile.equipment.Single(x => x.instanceId == id).definitionId).ToArray();
            candidate.dieIds = l.dice.Select(id => d.profile.dice.Single(x => x.instanceId == id).definitionId).ToArray();
            candidate.hp = GrowthRules.Stats(candidate).maxHp;
            RunStateValidator.Validate(candidate);
            d.run = candidate; d.legacyRun = false;
            d.start = new RunStartSnapshot { runId = candidate.runId, profileId = store.ProfileId, profileRevision = d.profile.revision,
                rulesVersion = rules.version, characterRank = rank, loadout = l.Copy(), policy = policy.Copy() };
            Complete(d, request, fingerprint, candidate.runId); Commit(d); return candidate.DeepCopy();
        });

        public Task<SettlementReceipt> SettleAsync(SettlementRequest request) => Execute(() =>
        {
            Request(request); var d = Read(); var fingerprint = Fingerprint("settle", request.runId);
            var replay = Replay(d, request, fingerprint);
            var paid = d.settlements.FirstOrDefault(x => x.runId == request.runId);
            if (paid != null)
            {
                if (replay == null)
                {
                    d.operations.Add(new MetaOperationReceipt { requestId = request.requestId, fingerprint = fingerprint,
                        runId = request.runId, profileRevision = d.profile.revision });
                    Commit(d);
                }
                return paid.Copy();
            }
            Revision(d, request);
            ProfileRules.Require(d.run != null && d.run.runId == request.runId && d.run.phase == RunPhase.Result,
                "정산할 완료 여정을 찾지 못했습니다.");
            var policy = d.start?.policy;
            long amount = d.legacyRun ? 0 : checked((long)d.run.eventsResolved * policy.currencyPerEvent + (d.run.won ? policy.victoryBonus : 0));
            d.profile.growthCurrency = checked(d.profile.growthCurrency + amount);
            Complete(d, request, fingerprint, request.runId);
            var receipt = new SettlementReceipt { runId = request.runId, currency = amount, profileRevision = d.profile.revision,
                outcome = d.legacyRun ? "legacy" : d.run.won ? "won" : "lost" };
            d.settlements.Add(receipt); Commit(d); return receipt.Copy();
        });

        public Task<PlayerProfileData> GrowAsync(GrowthRequest request) => Execute(() =>
        {
            Request(request); var d = Read(); var fingerprint = Fingerprint("grow", request.characterId);
            if (Replay(d, request, fingerprint) != null) return d.profile.Copy();
            Revision(d, request); var c = d.profile.characters.SingleOrDefault(x => x.characterId == request.characterId); var policy = Policy;
            ProfileRules.Require(c != null && c.rank < policy.maxRank, "성장 가능한 보유 캐릭터를 선택해 주세요.");
            long cost = policy.Cost(c.rank);
            ProfileRules.Require(d.profile.growthCurrency >= cost, "성장 재화가 부족합니다.");
            d.profile.growthCurrency -= cost; c.rank++;
            Complete(d, request, fingerprint); Commit(d); return d.profile.Copy();
        });

        sealed class Checkpoints : IRunStore
        {
            readonly LocalMetaProgressionService owner;
            public Checkpoints(LocalMetaProgressionService owner) { this.owner = owner; }
            public bool Exists => owner.Read().run != null;
            public RunState Load() => owner.Read().run?.DeepCopy() ?? throw new FileNotFoundException("저장된 여정이 없습니다.");
            public void Save(RunState state)
            {
                var d = owner.Read(); RunStateValidator.Validate(state);
                ProfileRules.Require(d.run != null && state.profileId == owner.store.ProfileId && state.runId == d.run.runId &&
                    state.initialSeed == d.run.initialSeed && state.playedSeconds >= d.run.playedSeconds &&
                    (state.sequence == d.run.sequence || (long)state.sequence == (long)d.run.sequence + 1) &&
                    MetaJson.Hash(MetaJson.Encode(state.config)) == MetaJson.Hash(MetaJson.Encode(d.run.config)),
                    "시작 기록과 다른 여정 또는 오래된 체크포인트입니다.");
                if (state.sequence == d.run.sequence)
                {
                    // Autosave keeps the command sequence. It may advance elapsed time only;
                    // conflicting n+1 candidates from two sessions must not overwrite each other.
                    var candidate = state.DeepCopy(); var saved = d.run.DeepCopy();
                    candidate.playedSeconds = saved.playedSeconds = 0;
                    if (candidate.lastResult != null) candidate.lastResult.playedSeconds = 0;
                    if (saved.lastResult != null) saved.lastResult.playedSeconds = 0;
                    ProfileRules.Require(MetaJson.Encode(candidate) == MetaJson.Encode(saved), "다른 세션이 먼저 여정을 변경했습니다. 저장된 여정을 다시 불러와 주세요.");
                }
                ProfileRules.Require(!d.settlements.Any(x => x.runId == state.runId) || MetaJson.Encode(state) == MetaJson.Encode(d.run),
                    "정산한 여정은 변경할 수 없습니다.");
                d.run = state.DeepCopy(); owner.Commit(d);
            }
            public string Archive() => throw new MetaCommandException("영구 프로필은 여정 슬롯 비우기로 삭제할 수 없습니다. 원본을 보존하고 복구해 주세요.");
        }
    }
}
