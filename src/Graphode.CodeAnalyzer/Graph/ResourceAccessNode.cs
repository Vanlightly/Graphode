using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Graph
{
    public class ResourceAccessNode
    {
        public ResourceAccessNode()
        {
            CorrespondingMethodNodes = new List<PublicMethodNode>();
        }

        public ConfigurationResource ConfigurationResource { get; set; }
        public ResourceKeyResult ResourceKey { get; set; }
        public string AppDomain { get; set; }
        public List<PublicMethodNode> CorrespondingMethodNodes { get; set; }

        public string GetNodeId()
        {
            return ResourceKey.Value + ":" + ConfigurationResource + ":" + AppDomain;
        }
    }
}
