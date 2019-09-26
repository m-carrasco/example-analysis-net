using Backend.Model;
using Model;
using Model.ThreeAddressCode.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewAnalyses.Interprocedueral
{
    class SuperControlFlowGraph : ControlFlowGraph
    {
        private ControlFlowGraph cfg;
        private int nextAvailableCFGNodeId;

        //private ISet<ControlFlowGraph> added;
        public SuperControlFlowGraph(ControlFlowGraph cfg)
        {
            this.cfg = cfg;
            //this.added = new HashSet<ControlFlowGraph>();
        }

        public void Add(MethodCallInstruction call, ISet<ControlFlowGraph> targets)
        {

        }

        private void Split(CFGNode node, IInstruction ins)
        {

            if (node.Instructions.First() == ins)
            {
                CFGNode newNode = new CFGNode(nextAvailableCFGNodeId, CFGNodeKind.BasicBlock);
                newNode.Instructions.Add(ins);
                node.Instructions.RemoveAt(0);
                


            }


        }
    }
}
