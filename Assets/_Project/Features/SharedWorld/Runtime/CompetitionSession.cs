using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    [Serializable] public sealed class CompetitionDocument
    {
        public int schema = 1, generation;
        public long revision;
        public CompetitionState state;
        public List<CompetitionCommandEntry> commands = new List<CompetitionCommandEntry>();
        public List<Receipt> receipts = new List<Receipt>();
    }
    [Serializable] public sealed class CompetitionCommand
    {
        public string id, kind, arg, matchId, targetId;
        public long revision;
        public int generation;
    }
    public sealed class CompetitionSession
    {
        readonly ICompetitionStore store;
        readonly CompetitionDefinition definition;
        CompetitionDocument document;
        public CompetitionDocument Snapshot => LoopJson.Copy(document);
        public CompetitionSession(ICompetitionStore store, CompetitionDefinition definition)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.definition = LoopJson.Copy(definition ?? throw new ArgumentNullException(nameof(definition)));
            Reload();
        }
        public void Start(Profile profile, uint seed, string matchId, string adventurerId = "wanderer")
            => StartCore(profile, seed, matchId, adventurerId, false);
        public void Restart(Profile profile, uint seed, string matchId, string adventurerId = "wanderer")
            => StartCore(profile, seed, matchId, adventurerId, true);
        void StartCore(Profile profile, uint seed, string matchId, string adventurerId, bool replaceActive)
        {
            var disk = store.Read();
            RequireFresh(disk, document.revision, document.generation);
            if (disk.state != null && !disk.state.finished && !replaceActive) throw new InvalidOperationException("진행 중인 경기를 먼저 마쳐 주세요.");
            if (disk.state?.matchId == matchId) throw new InvalidOperationException("새 경기는 다른 ID가 필요합니다.");
            var candidate = LoopJson.Copy(disk);
            candidate.state = CompetitionRules.Start(LoopJson.Copy(definition), LoopJson.Copy(profile), seed, matchId, adventurerId);
            candidate.commands.Clear(); candidate.receipts.Clear();
            Save(candidate, disk, replaceActive);
        }
        public CompetitionCommand Prepare(string kind, string arg = "")
        {
            if (document.state == null) throw new InvalidOperationException("먼저 경기를 시작해 주세요.");
            return new CompetitionCommand {
                id = Guid.NewGuid().ToString("N"), kind = kind, arg = arg ?? "",
                revision = document.revision, generation = document.generation,
                matchId = document.state.matchId, targetId = CompetitionCommandRules.Target(document.state)
            };
        }
        public void Execute(CompetitionCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.id) || command.id.Length > 64 ||
                string.IsNullOrWhiteSpace(command.kind) || command.kind.Length > 32 || command.arg == null || command.arg.Length > 1024)
                throw new ArgumentException("경기 명령 형식이 잘못되었습니다.");
            var disk = store.Read();
            if (disk.generation != command.generation || disk.state == null || disk.state.matchId != command.matchId)
                throw new InvalidOperationException("다른 경기 또는 복구 이전 화면의 입력입니다.");
            string fingerprint = LoopJson.Hash(LoopJson.Encode(command));
            var receipt = disk.receipts.SingleOrDefault(r => r.id == command.id);
            if (receipt != null)
            {
                if (receipt.fingerprint != fingerprint) throw new InvalidOperationException("동일 명령 ID의 내용이 바뀌었습니다.");
                document = disk; return;
            }
            RequireFresh(disk, command.revision, command.generation);
            if (command.targetId != CompetitionCommandRules.Target(disk.state))
                throw new InvalidOperationException("이전 사건 또는 다른 참가자의 입력입니다.");
            if (disk.commands.Count >= BackendProtocol.MaxCommands) throw new InvalidOperationException("경기 명령 보관 한도에 도달했습니다. 기록을 보존했습니다.");
            var candidate = LoopJson.Copy(disk);
            CompetitionCommandRules.Apply(candidate.state, command.kind, command.arg);
            candidate.commands.Add(new CompetitionCommandEntry {
                sequence = candidate.commands.Count + 1, kind = command.kind, arg = command.arg,
                targetId = command.targetId, clientCommandId = command.id
            });
            candidate.receipts.Add(new Receipt { id = command.id, fingerprint = fingerprint, revision = disk.revision + 1 });
            Save(candidate, disk);
        }
        static void RequireFresh(CompetitionDocument disk, long revision, int generation)
        {
            if (disk.revision != revision || disk.generation != generation)
                throw new InvalidOperationException("저장 상태가 변경되었습니다. 다시 불러와 주세요.");
        }
        void Save(CompetitionDocument candidate, CompetitionDocument previous, bool replaceActive = false)
        {
            candidate.revision = checked(previous.revision + 1);
            CompetitionStore.ValidateDocument(candidate);
            store.Commit(candidate, previous.revision, previous.generation, replaceActive);
            document = candidate;
        }
        public void Reload()
        {
            var loaded = store.Read(); CompetitionStore.ValidateDocument(loaded); document = loaded;
        }
        public CompetitionDocument InspectBackup() => store.InspectBackup();
        public void RestoreBackup()
        {
            var restored = store.RestoreBackup(); CompetitionStore.ValidateDocument(restored); document = restored;
        }
    }
}
