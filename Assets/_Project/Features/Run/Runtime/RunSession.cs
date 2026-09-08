using System;
using System.Linq;
using UnityEngine;

namespace FateDice
{
    // Commands work on a private copy. Checkpoint must succeed before the visible state is committed.
    public sealed class RunSession
    {
        public RunState State { get; private set; }
        public Action<RunState> Checkpoint { get; set; }
        public RunSession(RunState state)
        {
            State=state??throw new ArgumentNullException(nameof(state));
        }
        public static RunSession New(GameConfigData data,uint seed,string trial,Grade cap)
        {
            if(seed==0)throw new ArgumentOutOfRangeException(nameof(seed));
            if(data==null)throw new ArgumentNullException(nameof(data));
            var errors=data.Validate();
            if(errors.Length>0)throw new InvalidOperationException(FateDiceConfig.DefaultAssetPath+": "+string.Join("; ",errors));
            if(!data.combat.trialActionIds.Contains(trial))throw new ArgumentException("Select a trial wildcard.",nameof(trial));
            if(!Enum.IsDefined(typeof(Grade),cap))throw new ArgumentOutOfRangeException(nameof(cap));
            var config=data.DeepCopy();
            var state=new RunState {runId=Guid.NewGuid().ToString("N"),config=config,initialSeed=seed,rngState=seed,explorationCap=cap,
                hp=config.growth.startingMaxHp,baseMaxHp=config.growth.startingMaxHp,basePower=config.growth.startingPower,
                baseGuard=config.growth.startingGuard,gold=config.growth.startingGold,level=1,
                actionIds=config.combat.startingActionIds.Concat(new[]{trial}).Distinct().ToList(),
                dieIds=Enumerable.Repeat(config.dice.basicDieId,6).ToArray()};
            ExplorationRules.Initialize(state);
            state.message="Choose a connected path. Every resolved normal event advances Great Fate once.";
            return new RunSession(state);
        }
        public bool Roll()
        {
            if(State.phase!=RunPhase.CombatRoll&&State.phase!=RunPhase.ExplorationRoll)return false;
            Apply(next=>{
                var combat=next.phase==RunPhase.CombatRoll;
                next.dice=DiceRules.Roll(next.config,next.dieIds,ref next.rngState);
                RefreshOffers(next,combat);
                next.message=combat?"Choose one action. Every offered action can be played.":"Choose one fate. Only type and grade are revealed until selection.";
            });return true;
        }
        public bool Reroll(int index)
        {
            if((State.phase!=RunPhase.CombatCards&&State.phase!=RunPhase.ExplorationCards)||index<0||index>=6||
                !State.rerollUnlocked||State.rerollCharges<State.config.growth.rerollCost)return false;
            Apply(next=>{
                var combat=next.phase==RunPhase.CombatCards;
                var die=next.config.Die(next.dieIds[index]);
                next.dice[index]=die.values[DiceRules.WeightedIndex(die.weights,ref next.rngState)];
                next.rerollCharges-=next.config.growth.rerollCost;
                RefreshOffers(next,combat);
                next.message="Die "+(index+1)+" rerolled. New cards are fixed; "+next.rerollCharges+" charges remain.";
            });return true;
        }
        private static void RefreshOffers(RunState state,bool combat)
        {
            var hand=DiceRules.BestHand(state.dice,state.config.dice.hands);state.hand=hand.kind;state.fatePower=hand.fatePower;
            state.cards=combat?FateCardRules.GenerateActions(state):FateCardRules.GenerateExploration(state);
            state.phase=combat?RunPhase.CombatCards:RunPhase.ExplorationCards;
        }

        public bool ChooseAction(string cardId)
        {
            if(State.phase!=RunPhase.CombatCards||!State.cards.Any(x=>x.id==cardId))return false;
            Apply(next=>CombatRules.Resolve(next,next.cards.Single(x=>x.id==cardId)));
            return true;
        }
        public bool ChooseNode(string id)
        {
            if(State.phase!=RunPhase.Map||!State.availableNodeIds.Contains(id))return false;
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
        public bool ChooseFate(string id)
        {
            if(State.phase!=RunPhase.ExplorationCards||!State.cards.Any(x=>x.id==id))return false;
            Apply(next=>{
                var card=next.cards.Single(x=>x.id==id);var encounter=next.config.Event(card.contentId);
                next.activeEventId=encounter.id;next.activeGrade=card.grade;next.selectedGrades[(int)card.grade]++;
                next.pendingReward=CloneReward(encounter.reward);
                var capBonus=next.config.fate.capRewardMultipliers[(int)next.explorationCap];
                next.pendingReward.gold=Round(next.pendingReward.gold*capBonus);
                next.pendingReward.xp=Round(next.pendingReward.xp*capBonus);
                next.pendingRewardId=next.selectedNode.id+":reward";next.rewardReturnPhase=RunPhase.Map;
                next.cards.Clear();next.purchasedIds.Clear();next.message=encounter.description;
                if(encounter.type==NodeType.Combat)CombatRules.Begin(next,encounter.enemyId);
                else next.phase=encounter.type==NodeType.Shop?RunPhase.Shop:RunPhase.Encounter;
            });return true;
        }
        public RewardDefinition RestTrainingReward()=>TrainingReward(State);
        private static RewardDefinition TrainingReward(RunState state)
        {
            var reward=CloneReward(state.config.world.restTraining);
            reward.xp=Round(reward.xp*state.config.fate.capRewardMultipliers[(int)state.explorationCap]);
            return reward;
        }

        public bool ResolveEncounter(bool train)
        {
            if(State.phase!=RunPhase.Encounter)return false;
            var type=State.config.Event(State.activeEventId).type;
            if(train&&type!=NodeType.Rest)return false;
            Apply(next=>{
                if(train)
                {
                    next.pendingReward=TrainingReward(next);
                }
                next.phase=RunPhase.Reward;next.message="The outcome is fixed. Claim it to continue.";
            });return true;
        }
        public bool ClaimReward()
        {
            if(State.phase!=RunPhase.Reward||State.pendingReward==null||State.processedRewardIds.Contains(State.pendingRewardId))return false;
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
        public bool Equip(bool accept)
        {
            if(State.phase!=RunPhase.EquipmentChoice||string.IsNullOrEmpty(State.pendingEquipmentId))return false;
            Apply(next=>{
                var item=next.config.Equipment(next.pendingEquipmentId);
                if(accept)GrowthRules.Equip(next,item.id);
                next.message=(accept?"Equipped ":"Left behind ")+item.label+".";
                next.pendingEquipmentId=null;FinishReward(next);
            });return true;
        }
        public bool ReplaceDie(int index)
        {
            if(State.phase!=RunPhase.EquipmentChoice||!string.IsNullOrEmpty(State.pendingEquipmentId)||string.IsNullOrEmpty(State.pendingDieId)||index< -1||index>=6)return false;
            Apply(next=>{
                if(index>=0)next.dieIds[index]=next.pendingDieId;
                next.message=index>=0?"Die "+(index+1)+" replaced with "+next.config.Die(next.pendingDieId).label+".":"Die replacement declined.";
                next.pendingDieId=null;FinishReward(next);
            });return true;
        }
        public bool Buy(string id)
        {
            if(State.phase!=RunPhase.Shop||State.purchasedIds.Contains(id))return false;
            var product=State.config.world.shop.FirstOrDefault(x=>x.id==id);
            if(product==null||State.gold<product.price)return false;
            Apply(next=>{
                var chosen=next.config.world.shop.Single(x=>x.id==id);
                next.gold-=chosen.price;next.purchasedIds.Add(id);
                next.pendingReward=CloneReward(chosen.reward);next.pendingRewardId=next.selectedNode.id+":shop:"+id;
                next.rewardReturnPhase=RunPhase.Shop;next.phase=RunPhase.Reward;next.message="Purchased "+chosen.label+". Claim the item.";
            });return true;
        }
        public bool LeaveShop()
        {
            if(State.phase!=RunPhase.Shop)return false;
            Apply(next=>{next.pendingReward=null;next.rewardReturnPhase=RunPhase.Map;FinishEvent(next);});return true;
        }
        public bool SetExplorationCap(Grade cap)
        {
            if(State.phase!=RunPhase.Map||!Enum.IsDefined(typeof(Grade),cap))return false;
            Apply(next=>{next.explorationCap=cap;next.message="Exploration cap: "+cap+". Combat grades are unchanged.";});return true;
        }
        private static void FinishReward(RunState state)
        {
            if(!string.IsNullOrEmpty(state.pendingEquipmentId)||!string.IsNullOrEmpty(state.pendingDieId)){state.phase=RunPhase.EquipmentChoice;return;}
            if(state.rewardReturnPhase==RunPhase.Shop){state.phase=RunPhase.Shop;return;}
            FinishEvent(state);
        }
        private static void FinishEvent(RunState state)
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

        private void Apply(Action<RunState> command)
        {
            var next=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            next.sequence++;command(next);
            if(next.phase==RunPhase.Result)next.lastResult=Record(next);
            Checkpoint?.Invoke(next);
            State=next;
        }
        private static RunRecord Record(RunState state)=>new RunRecord{
            runId=state.runId,seed=state.initialSeed,won=state.won,events=state.eventsResolved,combatTurns=state.combatTurns,
            selectedGrades=(int[])state.selectedGrades.Clone(),playedSeconds=state.playedSeconds};

        private static RewardDefinition CloneReward(RewardDefinition reward)=>JsonUtility.FromJson<RewardDefinition>(JsonUtility.ToJson(reward));
    }
}
