using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Graph
{
    public class MethodGraph
    {
        public MethodGraph(string applicationName)
        {
            ApplicationName = applicationName;
            PublicMethodNodes = new List<PublicMethodNode>();
            ResourceAccessNodes = new List<ResourceAccessNode>();
        }

        public string ApplicationName { get; set; }

        private List<PublicMethodNode> PublicMethodNodes { get; set; }
        private List<ResourceAccessNode> ResourceAccessNodes { get; set; }

        public void AddMethodNode(PublicMethodNode publicMethodNode)
        {
            PublicMethodNodes.Add(publicMethodNode);
        }

        public void AddResourceAccessNode(ResourceAccessNode resourceAccessNode)
        {
            ResourceAccessNodes.Add(resourceAccessNode);
        }

        public List<PublicMethodNode> GetPublicMethodNodes()
        {
            var grouped = PublicMethodNodes.GroupBy(x => x.GetFromNodeId()).ToList();

            var concrete = PublicMethodNodes.GroupBy(x => x.GetFromNodeId())
                .Select(x => x.First())
                .ToList();

            return concrete;

            //return PublicMethodNodes;
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

            foreach (var publicMethod in GetPublicMethodNodes())
            {
                foreach (var calledMethod in publicMethod.CrossAssemblyCalls)
                {
                    var relationship = new MethodToMethodRelationship();
                    relationship.Caller = publicMethod;
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

            foreach (var publicMethod in GetPublicMethodNodes())
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
