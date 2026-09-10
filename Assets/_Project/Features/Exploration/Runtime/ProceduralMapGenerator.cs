using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    /// <summary>Version1 complete campaign map. A seed-local stream never consumes gameplay RNG.</summary>
    public static class ProceduralMapGenerator
    {
        public static void Initialize(RunStateData state)
        {
            if(state==null||state.Rules?.world==null||state.Rules.fate==null)
                throw new ArgumentException("A procedural map requires a configured run.",nameof(state));
            var world=state.Rules.world;
            if(world.mapGenerationVersion!=1||world.eventsToBoss<1||world.eventsToBoss>100||
                world.mapColumns<2||world.mapColumns>7||world.mapPathCount<2||world.mapPathCount>12||state.initialSeed==0)
                throw new ArgumentException("Unsupported procedural map configuration or seed.",nameof(state));

            // Keep this derivation and iteration order stable within generationVersion1.
            uint mapRng=state.initialSeed^0xD1B54A35u;
            mapRng^=mapRng>>16;mapRng*=0x7FEB352Du;mapRng^=mapRng>>15;mapRng*=0x846CA68Bu;mapRng^=mapRng>>16;
            if(mapRng==0)mapRng=0xA341316Cu;
            var columns=world.mapColumns;
            var floors=world.eventsToBoss;
            var roots=new List<int>();
            var availableLanes=Enumerable.Range(0,columns).ToList();
            var rootCount=2+Choose(Math.Min(columns,world.mapPathCount)-1,ref mapRng);
            for(var i=0;i<rootCount;i++)
            {
                var index=Choose(availableLanes.Count,ref mapRng);
                roots.Add(availableLanes[index]);availableLanes.RemoveAt(index);
            }
            roots.Sort();
            var cells=new SortedDictionary<int,SortedSet<int>>();
            var links=new Dictionary<int,SortedDictionary<int,SortedSet<int>>>();
            for(var floor=1;floor<=floors;floor++)
            {
                cells.Add(floor,new SortedSet<int>());
                links.Add(floor,new SortedDictionary<int,SortedSet<int>>());
            }
            for(var path=0;path<world.mapPathCount;path++)
            {
                // Every distinct start has a full path before repeated starts enrich branches/merges.
                var lane=path<roots.Count?roots[path]:roots[Choose(roots.Count,ref mapRng)];
                for(var floor=1;floor<=floors;floor++)
                {
                    cells[floor].Add(lane);
                    if(floor==floors)break;
                    var candidates=new List<int>();
                    for(var target=Math.Max(0,lane-1);target<=Math.Min(columns-1,lane+1);target++)
                        if(!Crosses(links[floor],lane,target))candidates.Add(target);
                    // Staying in the same lane is always legal against adjacent-lane edges.
                    if(candidates.Count==0)throw new InvalidOperationException("No noncrossing next-floor route exists.");
                    var next=candidates[Choose(candidates.Count,ref mapRng)];
                    if(!links[floor].TryGetValue(lane,out var children))links[floor].Add(lane,children=new SortedSet<int>());
                    children.Add(next);lane=next;
                }
            }

            var nodes=new List<NodeState>();
            var byPosition=new Dictionary<int,NodeState>();
            var nextId=state.nextNodeId;
            foreach(var layer in cells)
                foreach(var lane in layer.Value)
                {
                    var node=new NodeState {id=state.runId+":node:"+(nextId++),floor=layer.Key,lane=lane,
                        type=(NodeType)DiceRules.WeightedIndex(state.Rules.fate.nodeWeights,ref mapRng)};
                    nodes.Add(node);byPosition.Add(layer.Key*columns+lane,node);
                }
            var boss=new NodeState {id=state.runId+":node:"+(nextId++),type=NodeType.Boss,floor=floors+1,lane=columns/2};
            nodes.Add(boss);
            foreach(var node in nodes)
            {
                if(node.type==NodeType.Boss)continue;
                if(node.floor==floors)node.childIds.Add(boss.id);
                else foreach(var lane in links[node.floor][node.lane])node.childIds.Add(byPosition[(node.floor+1)*columns+lane].id);
            }
            // Publish only after all topology and weighted types have been produced successfully.
            state.nodes=nodes;state.nodeHistory=new List<NodeState>();
            state.availableNodeIds=nodes.Where(n=>n.floor==1).Select(n=>n.id).ToList();
            state.nextNodeId=nextId;state.selectedNode=null;state.phase=RunPhase.Map;
        }

        static bool Crosses(SortedDictionary<int,SortedSet<int>> existing,int source,int target)
        {
            foreach(var edge in existing)
                if((edge.Key<source&&edge.Value.Max>target)||(edge.Key>source&&edge.Value.Min<target))return true;
            return false;
        }

        static int Choose(int count,ref uint rng)
        {
            var weights=new float[count];
            for(var i=0;i<count;i++)weights[i]=1;
            return DiceRules.WeightedIndex(weights,ref rng);
        }
    }
}
