using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    public sealed class RunStateValidationException : InvalidOperationException
    {
        public RunStateValidationException(string message, Exception inner = null) : base(message, inner) { }
    }

    // The same invariant checks are used before command commit and at the disk boundary.
    public static class RunStateValidator
    {
        static RunStateValidationException Invalid(string reason, Exception inner = null) =>
            new RunStateValidationException(reason, inner);
        static void Require(bool condition,string reason)
        {
            if(!condition)throw Invalid(reason);
        }
        static HashSet<string> Ids(IEnumerable<string> values,string name)
        {
            Require(values!=null,name+" is missing.");
            var result=new HashSet<string>(StringComparer.Ordinal);
            foreach(var value in values)Require(!string.IsNullOrWhiteSpace(value)&&result.Add(value),name+" contains a missing or duplicate ID.");
            return result;
        }
        public static void Validate(RunStateData state)
        {
            Require(state!=null,"The run payload is missing.");
            Require(state.schema==RunStateData.CurrentSchema,"Unsupported run schema "+state.schema+". Expected "+RunStateData.CurrentSchema+".");
            Require(!string.IsNullOrWhiteSpace(state.runId)&&!string.IsNullOrWhiteSpace(state.profileId),"The run/profile ID is missing.");
            Require(state.Rules!=null,"The actual configuration snapshot is missing.");
            string[] errors;
            try{errors=state.Rules.Validate();}
            catch(ArgumentException error){throw Invalid("Invalid configuration snapshot: "+error.Message,error);}
            Require(errors.Length==0,"Invalid configuration snapshot: "+string.Join("; ",errors));
            var config=state.Rules;
            Require(state.rngState!=0&&state.initialSeed!=0,"The saved RNG state and initial seed must be nonzero.");
            Require(Enum.IsDefined(typeof(RunPhase),state.phase),"The saved phase is unsupported.");
            Require(Enum.IsDefined(typeof(Grade),state.explorationCap)&&Enum.IsDefined(typeof(Grade),state.activeGrade),"A saved grade is unsupported.");
            Require(state.rewardReturnPhase==RunPhase.Map||state.rewardReturnPhase==RunPhase.Shop,"The reward return phase must be Map or Shop.");
            Require(state.baseMaxHp>0&&state.basePower>=0&&state.baseGuard>=0,"The saved base stats are invalid.");
            Require(state.hp>=0&&state.shield>=0&&state.gold>=0&&state.xp>=0,"HP, shield, coins, and XP cannot be negative.");
            Require(state.level>=1&&state.level<=config.growth.levels.Length+1,"The saved level is outside the configured growth table.");
            Require(state.eventsResolved>=0&&state.eventsResolved<=config.world.eventsToBoss&&state.combatTurns>=0&&state.sequence>=0&&state.nextNodeId>0,"The saved progress counters are invalid.");
            Require(state.rerollCharges>=0&&(state.rerollUnlocked||state.rerollCharges==0),"Reroll charges require earned permission and cannot be negative.");
            Require(!double.IsNaN(state.playedSeconds)&&!double.IsInfinity(state.playedSeconds)&&state.playedSeconds>=0,"The saved play duration is invalid.");
            Require(state.selectedGrades!=null&&state.selectedGrades.Length==5&&state.selectedGrades.All(x=>x>=0),"The grade history must contain five nonnegative counts.");
            var actions=Ids(state.actionIds,"Owned actions");
            Require(actions.Count>0&&actions.All(id=>config.combat.actions.Any(x=>x.id==id)),"An owned action ID is missing from the saved snapshot.");
            Require(state.equipmentIds!=null&&state.equipmentIds.Length==3,"Exactly three equipment slots are required.");
            long maxHp=state.baseMaxHp;
            for(var slot=0;slot<3;slot++)
            {
                var id=state.equipmentIds[slot];if(string.IsNullOrEmpty(id))continue;
                var item=config.growth.equipment.FirstOrDefault(x=>x.id==id);
                Require(item!=null&&(int)item.slot==slot,"Equipment slot "+slot+" has a missing ID or mismatched slot.");
                maxHp+=item.maxHp;
            }
            Require(state.hp<=maxHp,"Saved HP exceeds the current maximum.");
            Require(state.dieIds!=null&&state.dieIds.Length==6&&state.dieIds.All(id=>config.dice.dice.Any(x=>x.id==id)),"Exactly six existing die IDs are required.");
            var purchases=Ids(state.purchasedIds,"Purchased products");
            Require(purchases.All(id=>config.world.shop.Any(x=>x.id==id)),"A purchased product ID is missing from the snapshot.");
            Require(ShopRules.Validate(state).Length==0,"The fixed shop offers are invalid.");
            var rewards=Ids(state.processedRewardIds,"Processed rewards");
            var resolved=Ids(state.resolvedEventIds,"Resolved events");
            Require(resolved.Count==state.eventsResolved,"The normal event count and resolved IDs disagree.");
            ValidateGraph(state);
            Require(string.IsNullOrEmpty(state.pendingEquipmentId)||config.growth.equipment.Any(x=>x.id==state.pendingEquipmentId),"The pending equipment ID is missing from the snapshot.");
            Require(string.IsNullOrEmpty(state.pendingDieId)||config.dice.dice.Any(x=>x.id==state.pendingDieId),"The pending die ID is missing from the snapshot.");
            // Unity inline serialization restores null DTOs as empty objects. Phase and stable IDs define presence.
            var hasPendingReward=state.phase==RunPhase.CombatRoll||state.phase==RunPhase.CombatCards||
                state.phase==RunPhase.Encounter||state.phase==RunPhase.Reward||
                (state.phase==RunPhase.Shop&&!rewards.Contains(state.pendingRewardId))||
                (state.phase==RunPhase.Result&&!state.won&&state.enemyHp>0&&!string.IsNullOrEmpty(state.activeEnemyId));
            if(hasPendingReward)
            {
                Require(state.pendingReward!=null,"This phase requires its fixed pending reward.");
                var reward=state.pendingReward;
                Require(!string.IsNullOrWhiteSpace(state.pendingRewardId)&&!rewards.Contains(state.pendingRewardId),"A pending reward ID is missing or already processed.");
                Require(reward.gold>=0&&reward.xp>=0&&reward.rerollCharges>=0,"A pending reward has invalid quantities.");
                Require(string.IsNullOrEmpty(reward.equipmentId)||config.growth.equipment.Any(x=>x.id==reward.equipmentId),"A pending reward equipment ID is missing.");
                Require(string.IsNullOrEmpty(reward.dieId)||config.dice.dice.Any(x=>x.id==reward.dieId),"A pending reward die ID is missing.");
                Require(string.IsNullOrEmpty(reward.addActionId)||config.combat.actions.Any(x=>x.id==reward.addActionId),"A pending reward action ID is missing.");
            }
            else Require(EmptyReward(state.pendingReward),"This phase contains unexpected values in an absent reward.");
            var encounter=string.IsNullOrEmpty(state.activeEventId)?null:config.world.events.FirstOrDefault(x=>x.id==state.activeEventId);
            if(!string.IsNullOrEmpty(state.activeEventId))
                Require(encounter!=null||(state.boss&&state.activeEventId=="great-fate"),"The active event ID is missing from the snapshot.");
            var enemy=string.IsNullOrEmpty(state.activeEnemyId)?null:config.combat.enemies.FirstOrDefault(x=>x.id==state.activeEnemyId);
            if(!string.IsNullOrEmpty(state.activeEnemyId))
                Require(enemy!=null&&enemy.intents.Any(x=>x.id==state.activeIntentId),"The active enemy or intent ID is missing from the snapshot.");
            Require(state.enemyHp>=0&&state.enemyShield>=0&&(enemy==null||state.enemyHp<=enemy.maxHp),"The enemy HP or shield is invalid.");
            if(state.dice!=null&&state.dice.Length>0)
            {
                Require(state.dice.Length==6&&state.dice.All(x=>x>=1&&x<=6),"A roll must contain six values in 1..6.");
                var hand=DiceRules.BestHand(state.dice,config.dice.hands);
                Require(state.hand==hand.kind&&state.fatePower==hand.fatePower,"The saved hand or fate power disagrees with the fixed dice.");
            }
            Require(state.cards!=null,"The fixed card list is missing.");
            Ids(state.cards.Select(x=>x==null?null:x.id),"Offered cards");
            var cardPhase=state.phase==RunPhase.CombatCards||state.phase==RunPhase.ExplorationCards;
            Require(cardPhase?state.cards.Count==config.world.offeredCards:state.cards.Count==0,"The phase and number of fixed cards disagree.");
            if(cardPhase)
            {
                Require(state.dice!=null&&state.dice.Length==6,"Fixed cards require the saved six-die result.");
                foreach(var card in state.cards)
                {
                    Require(Enum.IsDefined(typeof(Grade),card.grade),"An offered grade is unsupported.");
                    if(state.phase==RunPhase.CombatCards)
                        Require(card.type==NodeType.Combat&&actions.Contains(card.contentId),"An offered action is not owned or has the wrong type.");
                    else
                    {
                        var offered=config.world.events.FirstOrDefault(x=>x.id==card.contentId);
                        Require(offered!=null&&offered.type==card.type&&offered.grade==card.grade&&card.grade<=state.explorationCap,"An offered fate does not match its fixed event type, grade, or cap.");
                    }
                }
                if(state.phase==RunPhase.ExplorationCards)Require(state.cards[0].type==state.selectedNode.type,"The guaranteed fate does not match the selected path.");
            }
            var pendingChoice=!string.IsNullOrEmpty(state.pendingEquipmentId)||!string.IsNullOrEmpty(state.pendingDieId);
            Require(!pendingChoice||state.phase==RunPhase.EquipmentChoice||state.phase==RunPhase.Result,"A pending equipment/die choice is in the wrong phase.");
            if(state.phase!=RunPhase.Result)
            {
                Require(state.hp>0&&!state.won,"A live phase requires a living player and unfinished result.");
                if(state.selectedNode!=null)Require(!resolved.Contains(state.selectedNode.id),"The active normal event has already been resolved.");
            }
            switch(state.phase)
            {
                case RunPhase.Map:
                    Require(string.IsNullOrEmpty(state.activeEventId)&&string.IsNullOrEmpty(state.activeEnemyId)&&!hasPendingReward,"A map cannot contain an active encounter or unclaimed reward.");
                    break;
                case RunPhase.ExplorationRoll:
                case RunPhase.ExplorationCards:
                    Require(!state.boss&&string.IsNullOrEmpty(state.activeEventId)&&!hasPendingReward,"Exploration requires a normal path before an event is selected.");
                    break;
                case RunPhase.CombatRoll:
                case RunPhase.CombatCards:
                    Require(enemy!=null&&state.enemyHp>0&&hasPendingReward,"Combat requires a living enemy, fixed intent, and deferred reward.");
                    Require(state.boss?state.activeEnemyId==config.combat.bossEnemyId:encounter!=null&&encounter.type==NodeType.Combat&&encounter.enemyId==state.activeEnemyId,"The enemy does not belong to the active combat event.");
                    break;
                case RunPhase.Encounter:
                    Require(encounter!=null&&(encounter.type==NodeType.Event||encounter.type==NodeType.Treasure||encounter.type==NodeType.Rest)&&hasPendingReward,"An encounter requires a revealed event and fixed reward.");
                    break;
                case RunPhase.Shop:
                    Require(encounter!=null&&encounter.type==NodeType.Shop,"The shop phase requires a revealed shop event.");
                    break;
                case RunPhase.Reward:
                    Require(hasPendingReward&&!string.IsNullOrEmpty(state.pendingRewardId),"The reward phase has no unclaimed reward.");
                    Require(state.boss||encounter!=null,"A reward must belong to an active encounter.");
                    if(state.boss||(encounter!=null&&encounter.type==NodeType.Combat))Require(state.enemyHp==0,"Combat rewards require enemy defeat.");
                    break;
                case RunPhase.EquipmentChoice:
                    Require(pendingChoice&&!hasPendingReward&&rewards.Contains(state.pendingRewardId),"An equipment/die choice requires an already claimed reward.");
                    break;
                case RunPhase.Result:
                    Require(state.won?state.boss&&state.hp>0&&state.enemyHp==0&&!hasPendingReward&&state.eventsResolved==config.world.eventsToBoss:state.hp==0,"The result does not match the saved victory or defeat.");
                    break;
            }
        }
        static bool EmptyNode(NodeState node)=>node==null||(string.IsNullOrEmpty(node.id)&&node.type==default&&
            (node.childIds==null||node.childIds.Count==0)&&node.floor==0&&node.lane==0);
        static bool EmptyReward(RewardDefinition reward)=>reward==null||(reward.gold==0&&reward.xp==0&&reward.health==0&&
            reward.rerollCharges==0&&string.IsNullOrEmpty(reward.equipmentId)&&string.IsNullOrEmpty(reward.dieId)&&string.IsNullOrEmpty(reward.addActionId));
        static void ValidateGraph(RunStateData state)
        {
            Require(state.nodes!=null&&state.nodes.Count>0&&state.nodes.All(x=>x!=null),"The saved path graph is missing.");
            var ids=Ids(state.nodes.Select(x=>x.id),"Path nodes");
            var nodes=state.nodes.ToDictionary(x=>x.id,StringComparer.Ordinal);
            foreach(var node in state.nodes)
            {
                Require(Enum.IsDefined(typeof(NodeType),node.type),"A path node has an unsupported type.");
                var children=Ids(node.childIds,"Children of "+node.id);
                Require(children.All(ids.Contains),"A path child ID is missing from the graph.");
            }
            // Schema 1 saves written before node history remain valid. Never erase or repair saved data here.
            var history=state.nodeHistory??new List<NodeState>();
            Require(history.All(x=>x!=null),"Node history contains a missing node.");
            var historyIds=Ids(history.Select(x=>x.id),"Node history");
            Require(!historyIds.Overlaps(ids),"An archived node ID is still active.");
            var all=state.nodes.Concat(history).ToDictionary(x=>x.id,StringComparer.Ordinal);
            foreach(var node in history)
            {
                Require(Enum.IsDefined(typeof(NodeType),node.type),"An archived node has an unsupported type.");
                var children=Ids(node.childIds,"Archived children of "+node.id);
                Require(children.All(all.ContainsKey),"An archived child ID is missing from the graph and history.");
            }
            // White/gray/black DFS permits shared children but rejects back edges, including history.
            var colors=new Dictionary<string,byte>();
            foreach(var root in all.Keys)
            {
                var traversal=new Stack<KeyValuePair<string,bool>>();
                traversal.Push(new KeyValuePair<string,bool>(root,false));
                while(traversal.Count>0)
                {
                    var step=traversal.Pop();
                    if(step.Value){colors[step.Key]=2;continue;}
                    colors.TryGetValue(step.Key,out var color);
                    Require(color!=1,"The saved path graph contains a cycle.");
                    if(color==2)continue;
                    colors[step.Key]=1;traversal.Push(new KeyValuePair<string,bool>(step.Key,true));
                    foreach(var child in all[step.Key].childIds)traversal.Push(new KeyValuePair<string,bool>(child,false));
                }
            }
            var procedural=state.Rules.world.mapGenerationVersion==1;
            var available=Ids(state.availableNodeIds,"Available paths");
            Require(available.All(ids.Contains),"An available path ID is missing from the graph.");
            if(procedural)ValidateProceduralGraph(state,all);
            IEnumerable<string> roots;
            if(state.phase==RunPhase.Map)
            {
                Require(EmptyNode(state.selectedNode)&&(procedural?available.Count>0:available.Count==state.Rules.world.branchCount),"A map requires available branches and no selected path.");
                var boss=state.eventsResolved>=state.Rules.world.eventsToBoss;
                Require(available.All(id=>(nodes[id].type==NodeType.Boss)==boss),"The next paths disagree with the Great Fate threshold.");
                if(procedural)Require(available.All(id=>nodes[id].floor==state.eventsResolved+1),"The next paths disagree with the completed campaign floor.");
                roots=state.availableNodeIds;
            }
            else
            {
                Require(state.selectedNode!=null&&ids.Contains(state.selectedNode.id)&&available.Count==0,"The active phase requires exactly one selected path.");
                var selected=nodes[state.selectedNode.id];
                Require(state.selectedNode.type==selected.type&&state.selectedNode.floor==selected.floor&&state.selectedNode.lane==selected.lane&&
                    state.selectedNode.childIds!=null&&state.selectedNode.childIds.SequenceEqual(selected.childIds),"The selected path copy disagrees with the graph.");
                Require(state.boss==(selected.type==NodeType.Boss),"The boss flag disagrees with the selected path.");
                if(procedural)
                {
                    var expectedFloor=state.eventsResolved+(state.resolvedEventIds.Contains(selected.id)?0:1);
                    Require(selected.floor==expectedFloor,"The selected path disagrees with campaign progress.");
                }
                roots=new[]{selected.id};
            }
            var seen=new HashSet<string>();var pending=new Stack<string>(roots);
            while(pending.Count>0)
            {
                var id=pending.Pop();
                if(!seen.Add(id))continue;
                foreach(var child in nodes[id].childIds)pending.Push(child);
            }
            Require(seen.Count==nodes.Count,"The path graph contains unreachable branches.");
        }

        static void ValidateProceduralGraph(RunStateData state,Dictionary<string,NodeState> all)
        {
            var world=state.Rules.world;
            var positions=new HashSet<int>();
            var parents=new HashSet<string>();
            var bosses=0;
            foreach(var node in all.Values)
            {
                Require(node.floor>=1&&node.floor<=world.eventsToBoss+1&&node.lane>=0&&node.lane<world.mapColumns,"A procedural node has an invalid floor or lane.");
                Require(positions.Add(node.floor*world.mapColumns+node.lane),"Procedural nodes overlap the same floor and lane.");
                if(node.type==NodeType.Boss)
                {
                    bosses++;
                    Require(node.floor==world.eventsToBoss+1&&node.childIds.Count==0,"The sole boss must terminate the last campaign floor.");
                }
                else
                {
                    Require(node.floor<=world.eventsToBoss&&node.childIds.Count>=1&&node.childIds.Count<=3,"A normal procedural node must lead to the next floor.");
                    foreach(var childId in node.childIds)
                    {
                        var child=all[childId];parents.Add(childId);
                        Require(child.floor==node.floor+1,"A procedural edge must advance exactly one floor.");
                        Require(child.type==NodeType.Boss||Math.Abs(child.lane-node.lane)<=1,"A normal procedural edge must use the same or adjacent lane.");
                    }
                }
            }
            Require(bosses==1,"A procedural campaign requires exactly one boss.");
            var roots=all.Values.Count(n=>n.floor==1);
            Require(roots>=2&&roots<=Math.Min(world.mapColumns,world.mapPathCount),"The procedural starting paths are invalid.");
            Require(all.Values.All(n=>n.floor==1||parents.Contains(n.id)),"A procedural node is disconnected from the starting paths.");
            foreach(var layer in all.Values.GroupBy(n=>n.floor))
            {
                var ordered=layer.OrderBy(n=>n.lane).ToArray();
                for(var left=0;left<ordered.Length;left++)
                    for(var right=left+1;right<ordered.Length;right++)
                        foreach(var leftId in ordered[left].childIds)
                            foreach(var rightId in ordered[right].childIds)
                                Require(all[leftId].lane<=all[rightId].lane,"Procedural path edges cross between lanes.");
            }
            for(var index=0;index<state.resolvedEventIds.Count;index++)
            {
                Require(all.TryGetValue(state.resolvedEventIds[index],out var resolved)&&resolved.type!=NodeType.Boss&&resolved.floor==index+1,
                    "Resolved events must record one normal node per completed campaign floor.");
                if(index>0)Require(all[state.resolvedEventIds[index-1]].childIds.Contains(resolved.id),"The completed campaign path is not connected.");
            }
            if(state.resolvedEventIds.Count>0)
            {
                var last=all[state.resolvedEventIds[state.resolvedEventIds.Count-1]];
                if(state.phase==RunPhase.Map)
                    Require(last.childIds.SequenceEqual(state.availableNodeIds),"Available campaign paths disagree with the last completed node.");
                else if(state.selectedNode!=null&&!string.IsNullOrEmpty(state.selectedNode.id)&&state.selectedNode.id!=last.id)
                    Require(last.childIds.Contains(state.selectedNode.id),"The selected campaign node is not connected to the completed path.");
            }
        }
    }
}
