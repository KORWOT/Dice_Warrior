using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    public static class ExplorationRules
    {
        public static void Initialize(RunStateData state)
        {
            state.nodes.Clear();state.availableNodeIds.Clear();
            state.nodeHistory = new List<NodeState>();
            for(var i=0;i<state.Rules.world.branchCount;i++)state.availableNodeIds.Add(CreateNode(state,false).id);
            FillPreview(state);state.phase=RunPhase.Map;
        }
        public static void PruneTo(RunStateData state,IEnumerable<string> roots)
        {
            var keep=new HashSet<string>();var pending=new Stack<string>(roots);
            while(pending.Count>0)
            {
                var id=pending.Pop();if(!keep.Add(id))continue;
                var node=state.nodes.Single(x=>x.id==id);
                foreach(var child in node.childIds)pending.Push(child);
            }
            if(state.nodeHistory==null)state.nodeHistory=new List<NodeState>();
            state.nodeHistory.AddRange(state.nodes.Where(x=>!keep.Contains(x.id)));
            state.nodes.RemoveAll(x=>!keep.Contains(x.id));
        }
        public static void Advance(RunStateData state)
        {
            state.availableNodeIds=new List<string>(state.selectedNode.childIds);
            var boss=state.eventsResolved>=state.Rules.world.eventsToBoss;
            if(state.availableNodeIds.Count==0)
                for(var i=0;i<state.Rules.world.branchCount;i++)state.availableNodeIds.Add(CreateNode(state,boss).id);
            PruneTo(state,state.availableNodeIds);
            state.selectedNode=null;
            if(boss)
            {
                foreach(var node in state.nodes.Where(x=>state.availableNodeIds.Contains(x.id))){node.type=NodeType.Boss;node.childIds.Clear();}
                PruneTo(state,state.availableNodeIds);
            }
            else FillPreview(state);
            state.phase=RunPhase.Map;
        }
        private static NodeState CreateNode(RunStateData state,bool boss)
        {
            var node=new NodeState{id=state.runId+":node:"+(state.nextNodeId++),
                type=boss?NodeType.Boss:(NodeType)DiceRules.WeightedIndex(state.Rules.fate.nodeWeights,ref state.rngState)};
            state.nodes.Add(node);return node;
        }
        private static void FillPreview(RunStateData state)
        {
            foreach(var root in state.availableNodeIds.ToArray())Fill(state,root,state.Rules.world.previewDepth);
        }
        private static void Fill(RunStateData state,string id,int depth)
        {
            if(depth<=1)return;
            var node=state.nodes.Single(x=>x.id==id);
            if(node.childIds.Count==0)
                for(var i=0;i<state.Rules.world.branchCount;i++)node.childIds.Add(CreateNode(state,false).id);
            foreach(var child in node.childIds.ToArray())Fill(state,child,depth-1);
        }
    }
}
