using System;

namespace FateDice
{
    // The application owns this concrete state; its rules contain no display settings.
    [Serializable] public sealed class CoreRunState : RunStateData
    {
        public RunRulesCatalog config;
        public override RunRulesCatalog Rules => config;
        public CoreRunState DeepCopy() => RunStateCopy.ToCore(this);
    }
}
