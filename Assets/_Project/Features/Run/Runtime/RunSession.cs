using System;

namespace FateDice
{
    // Preserves the public runtime API. All state changes are owned by the pure application.
    public sealed class RunSession
    {
        readonly RunApplication application;
        readonly PresentationSettings presentation;
        sealed class CheckpointAdapter
        {
            public readonly Action<RunState> Callback;
            readonly PresentationSettings presentation;
            public CheckpointAdapter(Action<RunState> callback,PresentationSettings style)
            { Callback=callback; presentation=style; }
            public void Save(CoreRunState state) => Callback(RunState.FromCore(state,presentation));
        }
        public Action<RunState> Checkpoint
        {
            get => (application.Checkpoint?.Target as CheckpointAdapter)?.Callback;
            set => application.Checkpoint=value==null?null:new Action<CoreRunState>(new CheckpointAdapter(value,presentation).Save);
        }
        public RunState State => ReadSnapshot();
        public RunPhase Phase => application.Phase;
        public RunSession(RunState state,IRunStore store=null)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            // Runtime validation includes the saved style; the core catalog excludes it.
            RunStateValidator.Validate(state);
            presentation=state.config.presentation.DeepCopy();
            application=new RunApplication(state.ToCore());
            if(store!=null)Checkpoint=store.Save;
        }
        private RunSession(RunApplication value,PresentationSettings style)
        { application=value; presentation=style.DeepCopy(); }
        public static RunSession New(GameConfigData data,uint seed,string trial,Grade cap,IRunStore store=null,RunRecord previousResult=null)
        {
            if(seed==0)throw new ArgumentOutOfRangeException(nameof(seed));
            if(data==null)throw new ArgumentNullException(nameof(data));
            var errors=data.Validate();
            if(errors.Length>0)throw new InvalidOperationException(FateDiceConfig.DefaultAssetPath+": "+string.Join("; ",errors));
            var session=new RunSession(RunApplication.New(data,seed,trial,cap,previousResult:previousResult),data.presentation);
            if(store!=null){session.Checkpoint=store.Save;session.SaveCheckpoint();}
            return session;
        }
        public RunState ReadSnapshot() => RunState.FromCore(application.ReadSnapshot(),presentation);
        public RunCommandToken CaptureCommandToken() => application.CaptureCommandToken();
        public void RecordElapsed(double seconds) => application.RecordElapsed(seconds);
        public bool SaveCheckpoint() => application.SaveCheckpoint();
        public RewardDefinition RestTrainingReward() => application.RestTrainingReward();
        public bool Roll(RunCommandToken expected=null) => application.Roll(expected);
        public bool Reroll(int index, RunCommandToken expected=null) => application.Reroll(index, expected);
        public bool ChooseAction(string cardId, RunCommandToken expected=null) => application.ChooseAction(cardId, expected);
        public bool ChooseNode(string id, RunCommandToken expected=null) => application.ChooseNode(id, expected);
        public bool ChooseFate(string id, RunCommandToken expected=null) => application.ChooseFate(id, expected);
        public bool ResolveEncounter(bool train, RunCommandToken expected=null) => application.ResolveEncounter(train, expected);
        public bool ClaimReward(RunCommandToken expected=null) => application.ClaimReward(expected);
        public bool Equip(bool accept, RunCommandToken expected=null) => application.Equip(accept, expected);
        public bool ReplaceDie(int index, RunCommandToken expected=null) => application.ReplaceDie(index, expected);
        public bool Buy(string id, RunCommandToken expected=null) => application.Buy(id, expected);
        public bool LeaveShop(RunCommandToken expected=null) => application.LeaveShop(expected);
        public bool SetExplorationCap(Grade cap, RunCommandToken expected=null) => application.SetExplorationCap(cap, expected);
    }
}
