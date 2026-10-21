using System;
using System.Linq;

namespace DiceWarrior.SymbolLoop
{
    public sealed class LoopSession
    {
        readonly ILoopStore store;
        readonly Rules rules;
        PlayerDocument state;
        public PlayerDocument Snapshot => LoopJson.Copy(state);
        public LoopSession(ILoopStore store, Rules rules)
        {
            this.store=store ?? throw new ArgumentNullException(nameof(store));
            LoopRules.ValidateRules(rules);
            this.rules=LoopJson.Copy(rules);
            Reload();
        }
        public void Reload() { var loaded=store.Read(); LoopRules.Validate(loaded); state=loaded; }
        public Command Prepare(string kind, string arg="", uint seed=0) => new Command {
            id=Guid.NewGuid().ToString("N"),kind=kind,arg=arg,seed=seed,
            revision=state.revision,generation=state.generation,runId=state.run?.id
        };
        public void Execute(Command command)
        {
            if(command==null || string.IsNullOrWhiteSpace(command.id)) throw new ArgumentException("Request ID");
            string fingerprint=LoopJson.Hash(LoopJson.Encode(new {command.kind,command.arg,command.seed,command.runId,
                command.revision,command.generation}));
            var disk=store.Read();
            var receipt=disk.receipts.SingleOrDefault(r=>r.id==command.id);
            if(receipt!=null)
            {
                if(receipt.fingerprint!=fingerprint) throw new InvalidOperationException("동일 요청 ID의 내용이 바뀌었습니다.");
                state=disk; return;
            }
            if(disk.revision!=command.revision || disk.generation!=command.generation || disk.run?.id!=command.runId)
                throw new InvalidOperationException("이전 화면의 입력입니다. 다시 불러와 주세요.");
            var candidate=LoopJson.Copy(disk);
            var run=candidate.run;
            switch(command.kind)
            {
                case "start":
                    if(run!=null && !(run.phase==Phase.Result && run.settled))
                        throw new InvalidOperationException("현재 여정의 결과를 먼저 정산해 주세요.");
                    candidate.run=LoopRules.Start(LoopJson.Copy(rules),candidate.profile,command.seed,command.id);
                    break;
                case "loadout":
                    RequireLobby(run);
                    var parts=command.arg.Split(':');
                    if(parts.Length!=2) throw new ArgumentException("Loadout");
                    if(parts[0]=="die" && rules.dice.Any(d=>d.id==parts[1])) candidate.profile.startingDie=parts[1];
                    else if(parts[0]=="weapon" && rules.gear.Any(g=>g.id==parts[1] && g.slot==0)) candidate.profile.startingWeapon=parts[1];
                    else throw new ArgumentException("Loadout");
                    break;
                case "grow":
                    RequireLobby(run);
                    int cost=checked(20*(candidate.profile.growth+1));
                    if(candidate.profile.currency<cost) throw new InvalidOperationException("성장 재화가 부족합니다.");
                    candidate.profile.currency-=cost; candidate.profile.growth++; break;
                case "travel":
                    if(run==null || run.phase!=Phase.Map) throw new InvalidOperationException("Not on board");
                    LoopRules.Travel(run,1+(int)(LoopRules.Next(ref run.travelRng)%6)); break;
                case "combat": LoopRules.RollCombat(run); break;
                case "lap": LoopRules.ClaimLap(run,command.arg); break;
                case "choose": LoopRules.Choose(run,command.arg); break;
                case "abandon":
                    if(run==null || run.phase==Phase.Result) throw new InvalidOperationException("No active run");
                    run.phase=Phase.Result; run.won=false; run.message="여정을 마쳤습니다."; break;
                case "settle":
                    if(run==null || run.phase!=Phase.Result) throw new InvalidOperationException("No result");
                    if(!run.settled)
                    {
                        run.payout=checked(run.completedRegions*run.rules.regionCurrency+(run.won?run.rules.winCurrency:0));
                        candidate.profile.currency=checked(candidate.profile.currency+run.payout); run.settled=true;
                    }
                    break;
                default: throw new ArgumentException("Unknown command");
            }
            candidate.revision++;
            candidate.receipts.Add(new Receipt {id=command.id,fingerprint=fingerprint,revision=candidate.revision});
            LoopRules.Validate(candidate);
            store.Commit(candidate,disk.revision,disk.generation);
            state=candidate;
        }
        static void RequireLobby(Run run)
        {
            if(run!=null && !(run.phase==Phase.Result && run.settled)) throw new InvalidOperationException("여정 중 준비를 바꿀 수 없습니다.");
        }
    }
}
