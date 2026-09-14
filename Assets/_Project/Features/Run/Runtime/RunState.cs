using System;
namespace FateDice
{
    // Runtime checkpoint/display adapter. Existing schema1 field names remain unchanged.
    [Serializable] public sealed class RunState : RunStateData
    {
        public GameConfigData config;
        public string recoveryGeneration = "";
        public override RunRulesCatalog Rules => config;
        public CoreRunState ToCore() => RunStateCopy.ToCore(this);
        public RunState DeepCopy()
        {
            var copy=new RunState {config=config?.DeepCopy(), recoveryGeneration=recoveryGeneration ?? ""};
            RunStateCopy.CopyFields(this,copy);
            return copy;
        }
        public static RunState FromCore(CoreRunState source,PresentationSettings presentation, string recoveryGeneration = "")
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var copy=new RunState {config=new GameConfigData {presentation=presentation?.DeepCopy()}, recoveryGeneration=recoveryGeneration};
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
