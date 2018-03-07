using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class InstructionTreeNode
    {
        public InstructionTreeNode()
        {
            ChildInstructions = new List<InstructionTreeNode>();
            ChainsInstructions = new List<InstructionTreeNode>();
        }

        public InstructionTreeNode Parent { get; set; }
        public Instruction Instruction { get; set; }
        public List<InstructionTreeNode> ChainsInstructions { get; set; }
        public List<InstructionTreeNode> ChildInstructions { get; set; }

        public List<InstructionTreeNode> GetDescendants()
        {
            var allNodes = new List<InstructionTreeNode>();
            allNodes.Add(this);

            AddDescendants(this, allNodes);

            return allNodes;
        }

        private void AddDescendants(InstructionTreeNode node, List<InstructionTreeNode> allNodes)
        {
            foreach(var child in node.ChildInstructions)
            {
                allNodes.Add(child);
                AddDescendants(child, allNodes);
            }
        }
    }
}
