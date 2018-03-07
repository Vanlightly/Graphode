using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class AssignmentTreeNode
    {
        public AssignmentTreeNode()
        {
            Children = new List<AssignmentTreeNode>();
        }

        public AssignmentTreeNode(Triple triple)
        {
            Triple = triple;
            Children = new List<AssignmentTreeNode>();
        }

        public AssignmentTreeNode(Triple triple, bool isTarget)
        {
            Triple = triple;
            IsTarget = isTarget;
            Children = new List<AssignmentTreeNode>();
        }

        public Triple Triple { get; set; }
        public bool IsTarget { get; set; }
        public bool IsRegressionToInstance { get; set; }
        public AssignmentTreeNode Parent { get; set; }
        public List<AssignmentTreeNode> Children { get; set; }
    }
}
