using Graphode.CodeAnalyzer.Entities.CallGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public class MethodGraph
    {
        public MethodGraph(string applicationName, GraphType graphType)
        {
            ApplicationName = applicationName;
            GraphType = graphType;
            MethodNodes = new List<MethodNode>();
            ResourceAccessNodes = new List<ResourceAccessNode>();
        }

        public string ApplicationName { get; set; }
        public GraphType GraphType { get; set; }

        private List<MethodNode> MethodNodes { get; set; }
        private List<ResourceAccessNode> ResourceAccessNodes { get; set; }

        public void AddMethodNode(MethodNode methodNode)
        {
            MethodNodes.Add(methodNode);
        }

        public void AddResourceAccessNode(ResourceAccessNode resourceAccessNode)
        {
            ResourceAccessNodes.Add(resourceAccessNode);
        }

        public List<MethodNode> GetMethodNodes()
        {
            var grouped = MethodNodes.GroupBy(x => x.GetFromNodeId()).ToList();

            var concrete = MethodNodes.GroupBy(x => x.GetFromNodeId())
                .Select(x => x.First())
                .ToList();

            return concrete;
        }

        public List<ResourceAccessNode> GetResourceAccessNodes()
        {
            return ResourceAccessNodes.GroupBy(x => x.GetNodeId())
                .Select(x => x.First())
                .ToList();
        }

        public List<MethodToMethodRelationship> GenerateMethodRelationships()
        {
            var relationships = new List<MethodToMethodRelationship>();

            foreach (var method in GetMethodNodes())
            {
                foreach (var calledMethod in method.CrossAssemblyCalls)
                {
                    var relationship = new MethodToMethodRelationship();
                    relationship.Caller = method;
                    relationship.Callee = calledMethod;

                    relationships.Add(relationship);
                }

                foreach (var calledMethod in method.PublicInnerAssemblyCalls)
                {
                    var relationship = new MethodToMethodRelationship();
                    relationship.Caller = method;
                    relationship.Callee = calledMethod;

                    relationships.Add(relationship);
                }

                foreach (var calledMethod in method.NonPublicInnerAssemblyCalls)
                {
                    var relationship = new MethodToMethodRelationship();
                    relationship.Caller = method;
                    relationship.Callee = calledMethod;

                    relationships.Add(relationship);
                }
            }

            var grouped = relationships.GroupBy(x => new { CallerId = x.Caller.GetFromNodeId(), CalleeId = x.Callee.GetToNodeId() }).ToList();

            var rels = relationships.GroupBy(x => new { CallerId = x.Caller.GetFromNodeId(), CalleeId = x.Callee.GetToNodeId() })
                .Select(x => x.First())
                .ToList();

            return rels;
        }

        public List<MethodToResourceRelationship> GenerateResourceRelationships()
        {
            var relationships = new List<MethodToResourceRelationship>();

            foreach (var publicMethod in GetMethodNodes())
            {
                foreach (var resourceAccess in publicMethod.GetResourceAccessNodes())
                {
                    var relationship = new MethodToResourceRelationship();
                    relationship.Caller = publicMethod;
                    relationship.Resource = resourceAccess;

                    relationships.Add(relationship);
                }
            }

            return relationships.GroupBy(x => new { CallerId = x.Caller.GetFromNodeId(), CalleeId = x.Resource.GetNodeId() })
                .Select(x => x.First())
                .ToList();
        }
    }
}
