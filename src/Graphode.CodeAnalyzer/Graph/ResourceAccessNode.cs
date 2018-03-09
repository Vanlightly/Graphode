using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Graph
{
    public class ResourceAccessNode
    {
        public ResourceAccessNode(GraphType graphType, string appDomain)
        {
            GraphType = graphType;
            AppDomain = appDomain;
            CorrespondingMethodNodes = new List<MethodNode>();
        }

        public ConfigurationResource ConfigurationResource { get; set; }
        public ResourceKeyResult ResourceKey { get; set; }
        public string AppDomain { get; set; }
        public GraphType GraphType { get; set; }
        // deprecated property
        public List<MethodNode> CorrespondingMethodNodes { get; set; }

        public string GetNodeId()
        {
            return GraphType + ":" + ResourceKey.Value + ":" + ConfigurationResource + ":" + AppDomain;
        }
    }
}
