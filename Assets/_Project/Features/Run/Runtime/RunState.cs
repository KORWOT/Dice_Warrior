using System;
namespace FateDice
{
    // Runtime checkpoint/display adapter. Existing schema1 field names remain unchanged.
    [Serializable] public sealed class RunState : RunStateData
    {
        public GameConfigData config;
        public override RunRulesCatalog Rules => config;
        public CoreRunState ToCore() => RunStateCopy.ToCore(this);
        public RunState DeepCopy()
        {
            var copy=new RunState {config=config?.DeepCopy()};
            RunStateCopy.CopyFields(this,copy);
            return copy;
        }
        public static RunState FromCore(CoreRunState source,PresentationSettings presentation)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var copy=new RunState {config=new GameConfigData {presentation=presentation?.DeepCopy()}};
            source.config.CopyRulesTo(copy.config);
            RunStateCopy.CopyFields(source,copy);
            return copy;
        }
    }
    [Serializable] public sealed class LocalSaveEnvelope
    {
        public int schema = RunState.CurrentSchema;
        public string checksum;
        public string payload;
    }
}
