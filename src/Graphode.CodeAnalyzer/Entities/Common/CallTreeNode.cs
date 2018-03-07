using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.Common
{
    public class ExploreTreeNode
    {
        public ExploreTreeNode()
        {
            Children = new List<ExploreTreeNode>();
        }

        public ExploreTreeNode Parent { get; set; }
        public string FullSignature { get; set; }
        public List<ExploreTreeNode> Children { get; set; }
        public int GetDepth()
        {
            var node = this;
            int counter = 0;
            while(node != null)
            {
                counter++;
                node = node.Parent;
            }

            return counter;
        }

        public void AddChild(ExploreTreeNode treeNode)
        {
            Children.Add(treeNode);
            treeNode.Parent = this;
        }

        public override string ToString()
        {
            return FullSignature;
        }
    }
}
