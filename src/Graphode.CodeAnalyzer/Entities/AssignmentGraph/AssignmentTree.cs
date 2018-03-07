using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class AssignmentTree
    {
        public AssignmentTree()
        {
            FoundNodes = new List<AssignmentTreeNode>();
        }

        public AssignmentTreeNode RootNode { get; set; }

        public List<AssignmentTreeNode> FoundNodes { get; set; }
    }
}
