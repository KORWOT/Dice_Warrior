using System;
using System.Linq;

namespace DiceWarrior.SymbolLoop
{
    // Deterministic gameplay commands; callers own the candidate Run and persistence.
    public static class RunCommandRules
    {
        public static string Target(Run run)
        {
            if(run==null) throw new ArgumentNullException(nameof(run));
            return FormattableString.Invariant($"{run.region}:{run.position}:{run.phase}:{run.turn}");
        }

        public static void Apply(Run run,string kind,string arg)
        {
            if(run==null || run.phase==Phase.Result) throw new InvalidOperationException("이미 끝났거나 존재하지 않는 여정입니다.");
            arg=arg ?? "";
            switch(kind)
            {
                case "travel":
                    RequireEmpty(arg);
                    if(run.phase!=Phase.Map) throw new InvalidOperationException("Not on board");
                    LoopRules.Travel(run,1+(int)(LoopRules.Next(ref run.travelRng)%6)); break;
                case "combat": RequireEmpty(arg); LoopRules.RollCombat(run); break;
                case "lap":
                    if(arg.Length>0 && !run.rules.gear.Any(g=>g.id==arg)) throw new ArgumentException("Unknown lap choice");
                    LoopRules.ClaimLap(run,arg); break;
                case "choose": LoopRules.Choose(run,arg); break;
                case "abandon":
                    RequireEmpty(arg); run.phase=Phase.Result; run.won=false; run.message="여정을 마쳤습니다."; break;
                default: throw new ArgumentException("Unknown run command");
            }
        }

        static void RequireEmpty(string arg)
        {
            if(arg.Length!=0) throw new ArgumentException("이 명령에는 결과/인수를 지정할 수 없습니다.");
        }
    }
}
