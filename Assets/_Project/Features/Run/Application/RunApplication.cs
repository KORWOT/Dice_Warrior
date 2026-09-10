using System;
using System.Linq;

namespace FateDice
{
    // Commands work on a private copy. Checkpoint must succeed before the visible state is committed.
    public sealed class RunApplication
    {
        readonly object gate = new object();
        CoreRunState state;
        bool executing;
        double pendingElapsed;
        bool checkpointCurrent;
        Action<CoreRunState> checkpoint;
        public Action<CoreRunState> Checkpoint
        {
            get { lock (gate) return checkpoint; }
            set
            {
                lock (gate)
                {
                    if (executing) throw new InvalidOperationException("Cannot change stores during a checkpoint.");
                    checkpoint = value;
                    checkpointCurrent = false;
                }
            }
        }
        // Compatibility export. Mutating any returned tree never changes this session.
        public CoreRunState State => ReadSnapshot();
        public RunPhase Phase { get { lock (gate) return state.phase; } }

        public RunApplication(CoreRunState state, Action<CoreRunState> checkpoint = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            RunStateValidator.Validate(state);
            this.state = Copy(state);
            ShopRules.RestoreLegacy(this.state);
            if (checkpoint != null) Checkpoint = checkpoint;
        }
        public CoreRunState ReadSnapshot()
        {
            lock (gate)
            {
                var copy = Copy(state);
                copy.playedSeconds += pendingElapsed;
                return copy;
            }
        }
        public RunCommandToken CaptureCommandToken()
        { lock (gate) return new RunCommandToken(state.runId, state.sequence); }
        public void RecordElapsed(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            lock (gate)
            {
                if (executing) throw new InvalidOperationException("Cannot record time during a checkpoint.");
                if (state.phase == RunPhase.Result) return;
                double next = pendingElapsed + seconds;
                if (double.IsInfinity(state.playedSeconds + next)) throw new ArgumentOutOfRangeException(nameof(seconds));
                pendingElapsed = next;
            }
        }
        public bool SaveCheckpoint()
        {
            lock (gate)
            {
                if (executing) return false;
                if (checkpointCurrent && pendingElapsed == 0) return true;
                Commit(_ => { }, false);
                return true;
            }
        }
        bool AcceptToken(RunCommandToken token) => !executing &&
            (token == null || (token.RunId == state.runId && token.Sequence == state.sequence));
        public static RunApplication New(RunRulesCatalog data,uint seed,string trial,Grade cap, Action<CoreRunState> checkpoint = null, RunRecord previousResult = null)
        {
            if(seed==0)throw new ArgumentOutOfRangeException(nameof(seed));
            if(data==null)throw new ArgumentNullException(nameof(data));
            var errors=data.Validate();
            if(errors.Length>0)throw new InvalidOperationException("Run rules"+": "+string.Join("; ",errors));
            if(!data.combat.trialActionIds.Contains(trial))throw new ArgumentException("Select a trial wildcard.",nameof(trial));
            if(!Enum.IsDefined(typeof(Grade),cap))throw new ArgumentOutOfRangeException(nameof(cap));
            var config=data.DeepCopy();
            var state=new CoreRunState {runId=Guid.NewGuid().ToString("N"),config=config,initialSeed=seed,rngState=seed,explorationCap=cap,
                hp=config.growth.startingMaxHp,baseMaxHp=config.growth.startingMaxHp,basePower=config.growth.startingPower,
                baseGuard=config.growth.startingGuard,gold=config.growth.startingGold,level=1,
                actionIds=config.combat.startingActionIds.Concat(new[]{trial}).Distinct().ToList(),
                dieIds=Enumerable.Repeat(config.dice.basicDieId,6).ToArray()};
            ExplorationRules.Initialize(state);
            state.message="Choose a connected path. Every resolved normal event advances Great Fate once.";
            if (previousResult != null)
                state.lastResult = RunStateCopy.Record(previousResult);
            var session = new RunApplication(state, checkpoint);
            if (checkpoint != null) session.SaveCheckpoint();
            return session;
        }
        public bool Roll(RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.CombatRoll&&state.phase!=RunPhase.ExplorationRoll)return false;
                Apply(next=>{
                    var combat=next.phase==RunPhase.CombatRoll;
                    next.dice=DiceRules.Roll(next.config,next.dieIds,ref next.rngState);
                    RefreshOffers(next,combat);
                    next.message=combat?"Choose one action. Every offered action can be played.":"Choose one fate. Only type and grade are revealed until selection.";
                });return true;

            }
        }
        public bool Reroll(int index, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if((state.phase!=RunPhase.CombatCards&&state.phase!=RunPhase.ExplorationCards)||index<0||index>=6||
                    !state.rerollUnlocked||state.rerollCharges<state.config.growth.rerollCost)return false;
                Apply(next=>{
                    var combat=next.phase==RunPhase.CombatCards;
                    var die=next.config.Die(next.dieIds[index]);
                    next.dice[index]=die.values[DiceRules.WeightedIndex(die.weights,ref next.rngState)];
                    next.rerollCharges-=next.config.growth.rerollCost;
                    RefreshOffers(next,combat);
                    next.message="Die "+(index+1)+" rerolled. New cards are fixed; "+next.rerollCharges+" charges remain.";
                });return true;

            }
        }
        private static void RefreshOffers(CoreRunState state,bool combat)
        {
            var hand=DiceRules.BestHand(state.dice,state.config.dice.hands);state.hand=hand.kind;state.fatePower=hand.fatePower;
            state.cards=combat?FateCardRules.GenerateActions(state):FateCardRules.GenerateExploration(state);
            state.phase=combat?RunPhase.CombatCards:RunPhase.ExplorationCards;
        }

        public bool ChooseAction(string cardId, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.CombatCards||!state.cards.Any(x=>x.id==cardId))return false;
                Apply(next=>CombatRules.Resolve(next,next.cards.Single(x=>x.id==cardId)));
                return true;

            }
        }
        public bool ChooseNode(string id, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Map||!state.availableNodeIds.Contains(id))return false;
                Apply(next=>{
                    next.selectedNode=next.nodes.Single(x=>x.id==id);
                    ExplorationRules.PruneTo(next,new[]{id});
                    next.availableNodeIds.Clear();next.dice=null;next.cards.Clear();
                    next.boss=next.selectedNode.type==NodeType.Boss;
                    next.rewardReturnPhase=RunPhase.Map;next.activeGrade=Grade.Common;
                    if(next.boss)
                    {
                        next.activeEventId="great-fate";next.pendingReward=CloneReward(next.config.world.bossReward);
                        next.pendingRewardId=next.selectedNode.id+":reward";
                        CombatRules.Begin(next,next.config.combat.bossEnemyId);
                        next.message="The Fate keeper awaits. The next intent is already visible.";
                    }
                    else{next.phase=RunPhase.ExplorationRoll;next.message="Chosen path: "+next.selectedNode.type+". Roll for fate cards.";}
                });return true;

            }
        }
        public bool ChooseFate(string id, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.ExplorationCards||!state.cards.Any(x=>x.id==id))return false;
                Apply(next=>{
                    var card=next.cards.Single(x=>x.id==id);var encounter=next.config.Event(card.contentId);
                    next.activeEventId=encounter.id;next.activeGrade=card.grade;next.selectedGrades[(int)card.grade]++;
                    next.pendingReward=CloneReward(encounter.reward);
                    var capBonus=next.config.fate.capRewardMultipliers[(int)next.explorationCap];
                    next.pendingReward.gold=Round(next.pendingReward.gold*capBonus);
                    next.pendingReward.xp=Round(next.pendingReward.xp*capBonus);
                    next.pendingRewardId=next.selectedNode.id+":reward";next.rewardReturnPhase=RunPhase.Map;
                    next.cards.Clear();next.purchasedIds.Clear();next.shopOffers=new System.Collections.Generic.List<ShopOffer>();next.message=encounter.description;
                    if(encounter.type==NodeType.Combat)CombatRules.Begin(next,encounter.enemyId);
                    else next.phase=encounter.type==NodeType.Shop?RunPhase.Shop:RunPhase.Encounter;
                    if(next.phase==RunPhase.Shop)ShopRules.Enter(next);
                });return true;

            }
        }
        public RewardDefinition RestTrainingReward()
        { lock (gate) return TrainingReward(state); }
        private static RewardDefinition TrainingReward(CoreRunState state)
        {
            var reward=CloneReward(state.config.world.restTraining);
            reward.xp=Round(reward.xp*state.config.fate.capRewardMultipliers[(int)state.explorationCap]);
            return reward;
        }

        public bool ResolveEncounter(bool train, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Encounter)return false;
                var type=state.config.Event(state.activeEventId).type;
                if(train&&type!=NodeType.Rest)return false;
                Apply(next=>{
                    if(train)
                    {
                        next.pendingReward=TrainingReward(next);
                    }
                    next.phase=RunPhase.Reward;next.message="The outcome is fixed. Claim it to continue.";
                });return true;

            }
        }
        public bool ClaimReward(RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Reward||state.pendingReward==null||state.processedRewardIds.Contains(state.pendingRewardId))return false;
                Apply(next=>{
                    var reward=next.pendingReward;
                    GrowthRules.Grant(next,reward);
                    next.pendingEquipmentId=reward.equipmentId;next.pendingDieId=reward.dieId;
                    next.processedRewardIds.Add(next.pendingRewardId);next.pendingReward=null;
                    next.message="Claimed: "+reward.gold+" coins, "+reward.xp+" XP, HP "+Signed(reward.health)+".";
                    if(next.hp==0)
                    {
                        if(!next.boss&&!next.resolvedEventIds.Contains(next.selectedNode.id)){next.resolvedEventIds.Add(next.selectedNode.id);next.eventsResolved++;}
                        next.phase=RunPhase.Result;next.won=false;next.message+=" The price was fatal.";return;
                    }
                    FinishReward(next);
                });return true;

            }
        }
        public bool Equip(bool accept, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.EquipmentChoice||string.IsNullOrEmpty(state.pendingEquipmentId))return false;
                Apply(next=>{
                    var item=next.config.Equipment(next.pendingEquipmentId);
                    if(accept)GrowthRules.Equip(next,item.id);
                    next.message=(accept?"Equipped ":"Left behind ")+item.label+".";
                    next.pendingEquipmentId=null;FinishReward(next);
                });return true;

            }
        }
        public bool ReplaceDie(int index, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.EquipmentChoice||!string.IsNullOrEmpty(state.pendingEquipmentId)||string.IsNullOrEmpty(state.pendingDieId)||index< -1||index>=6)return false;
                Apply(next=>{
                    if(index>=0)next.dieIds[index]=next.pendingDieId;
                    next.message=index>=0?"Die "+(index+1)+" replaced with "+next.config.Die(next.pendingDieId).label+".":"Die replacement declined.";
                    next.pendingDieId=null;FinishReward(next);
                });return true;

            }
        }
        public bool Buy(string id, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Shop||state.purchasedIds.Contains(id))return false;
                var product=state.config.world.shop.FirstOrDefault(x=>x.id==id);
                var offer=ShopRules.Offer(state,id);
                if(product==null||offer==null||state.gold<offer.price)return false;
                Apply(next=>{
                    var chosen=next.config.world.shop.Single(x=>x.id==id);
                    next.gold-=ShopRules.Offer(next,id).price;next.purchasedIds.Add(id);
                    next.pendingReward=CloneReward(chosen.reward);next.pendingRewardId=next.selectedNode.id+":shop:"+id;
                    next.rewardReturnPhase=RunPhase.Shop;next.phase=RunPhase.Reward;next.message="Purchased "+chosen.label+". Claim the item.";
                });return true;

            }
        }
        public bool LeaveShop(RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Shop)return false;
                Apply(next=>{next.pendingReward=null;next.rewardReturnPhase=RunPhase.Map;next.shopOffers.Clear();FinishEvent(next);});return true;

            }
        }
        public bool SetExplorationCap(Grade cap, RunCommandToken expected = null)
        {
            lock (gate)
            {
                if (!AcceptToken(expected)) return false;
                if(state.phase!=RunPhase.Map||!Enum.IsDefined(typeof(Grade),cap))return false;
                Apply(next=>{next.explorationCap=cap;next.message="Exploration cap: "+cap+". Combat grades are unchanged.";});return true;

            }
        }
        private static void FinishReward(CoreRunState state)
        {
            if(!string.IsNullOrEmpty(state.pendingEquipmentId)||!string.IsNullOrEmpty(state.pendingDieId)){state.phase=RunPhase.EquipmentChoice;return;}
            if(state.rewardReturnPhase==RunPhase.Shop){state.phase=RunPhase.Shop;return;}
            FinishEvent(state);
        }
        private static void FinishEvent(CoreRunState state)
        {
            if(state.boss)
            {
                state.won=true;state.phase=RunPhase.Result;state.message="Victory! The Fate keeper falls. One section cleared.";return;
            }
            if(!state.resolvedEventIds.Contains(state.selectedNode.id))
            {
                state.resolvedEventIds.Add(state.selectedNode.id);state.eventsResolved++;
            }
            state.activeEventId=null;state.activeEnemyId=null;state.dice=null;state.cards.Clear();
            ExplorationRules.Advance(state);
            state.message+=" Event resolved: "+state.eventsResolved+"/"+state.config.world.eventsToBoss+".";
        }
        private static int Round(float value)=>(int)Math.Round(value,MidpointRounding.AwayFromZero);
        private static string Signed(int value)=>(value>=0?"+":"")+value;

        private void Apply(Action<CoreRunState> command) => Commit(command, true);
        private void Commit(Action<CoreRunState> command, bool advanceSequence)
        {
            executing = true;
            try
            {
                var next = Copy(state);
                next.playedSeconds += pendingElapsed;
                if (advanceSequence) next.sequence++;
                command(next);
                if (next.phase == RunPhase.Result) next.lastResult = Record(next);
                RunStateValidator.Validate(next);
                // Stores/callbacks may retain their input; never lend them the committed tree.
                Checkpoint?.Invoke(Copy(next));
                state = next;
                pendingElapsed = 0;
                checkpointCurrent = true;
            }
            finally { executing = false; }
        }
        private static CoreRunState Copy(CoreRunState source) => source.DeepCopy();
        private static RunRecord Record(CoreRunState state)=>new RunRecord{
            runId=state.runId,seed=state.initialSeed,won=state.won,events=state.eventsResolved,combatTurns=state.combatTurns,
            selectedGrades=(int[])state.selectedGrades.Clone(),playedSeconds=state.playedSeconds};

        private static RewardDefinition CloneReward(RewardDefinition reward)=>RulesCopy.Reward(reward);
    }
}
