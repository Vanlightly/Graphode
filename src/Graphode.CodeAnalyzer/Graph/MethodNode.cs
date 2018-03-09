using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Graph
{
    public class TypeInfo
    {
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string TypeName { get; set; }
    }

    public class MethodNode
    {
        public MethodNode(GraphType graphType, string appDomain)
        {
            GraphType = graphType;
            AppDomain = appDomain;

            CrossAssemblyCalls = new List<MethodNode>();
            PublicInnerAssemblyCalls = new List<MethodNode>();
            NonPublicInnerAssemblyCalls = new List<MethodNode>();
            ResourceAccesses = new List<ResourceAccessNode>();
        }

        private List<ResourceAccessNode> ResourceAccesses { get; set; }

        public string MethodName { get; set; }
        public bool IsPublic { get; set; }
        public string AppDomain { get; set; }
        public GraphType GraphType { get; set; }
        public TypeInfo InterfaceType { get; set; }
        public TypeInfo AbstractType { get; set; }
        public TypeInfo BaseClassType { get; set; }
        public TypeInfo ConcreteType { get; set; }
        
        public List<MethodNode> CrossAssemblyCalls { get; set; }
        public List<MethodNode> PublicInnerAssemblyCalls { get; set; }
        public List<MethodNode> NonPublicInnerAssemblyCalls { get; set; }

        public void AddResourceAccess(ResourceAccessNode resourceAccessNode)
        {
            if (!ResourceAccesses.Any(x => x.ConfigurationResource == resourceAccessNode.ConfigurationResource
                 && x.ResourceKey.Value.Equals(resourceAccessNode.ResourceKey.Value)))
                ResourceAccesses.Add(resourceAccessNode);
        }

        public List<ResourceAccessNode> GetResourceAccessNodes()
        {
            return ResourceAccesses;
        }

        private string _toNodeId;
        public string GetToNodeId()
        {
            if (!string.IsNullOrEmpty(_toNodeId))
                return _toNodeId;

            var idString = GraphType.ToString();
            if (InterfaceType != null)
                idString += ":" + AppDomain + ":" + InterfaceType.AssemblyName + ":" + InterfaceType.AssemblyVersion + ":" + InterfaceType.TypeName + ":" + MethodName;
            else if (AbstractType != null)
                idString += ":" + AppDomain + ":" + AbstractType.AssemblyName + ":" + AbstractType.AssemblyVersion + ":" + AbstractType.TypeName + ":" + MethodName;
            else if (ConcreteType != null)
                idString += ":" + AppDomain + ":" + ConcreteType.AssemblyName + ":" + ConcreteType.AssemblyVersion + ":" + ConcreteType.TypeName + ":" + MethodName;
            else
                throw new Exception("Id cannot be generated as no types exist. Method is " + MethodName);

            _toNodeId = HashService.GetMd5Hash(idString);

            return _toNodeId;
        }

        private string _fromNodeId;
        public string GetFromNodeId()
        {
            // if there is no toNodeId then use the fromNodeId
            if (!string.IsNullOrEmpty(_toNodeId))
                return _fromNodeId;

            var idString = GraphType.ToString();
            if (ConcreteType != null)
                idString += ":" + AppDomain + ":" + ConcreteType.AssemblyName + ":" + ConcreteType.AssemblyVersion + ":" + ConcreteType.TypeName + ":" + MethodName;
            else if (AbstractType != null)
                idString += ":" + AppDomain + ":" + AbstractType.AssemblyName + ":" + AbstractType.AssemblyVersion + ":" + AbstractType.TypeName + ":" + MethodName;
            else if (InterfaceType != null)
                idString += ":" + AppDomain + ":" + InterfaceType.AssemblyName + ":" + InterfaceType.AssemblyVersion + ":" + InterfaceType.TypeName + ":" + MethodName;
            else
                throw new Exception("Id cannot be generated as no types exist. Method is " + MethodName);

            _fromNodeId = HashService.GetMd5Hash(idString);

            return _fromNodeId;
        }

        public void CopyCallsToNode(MethodNode node)
        {
            foreach (var call in CrossAssemblyCalls)
            {
                if (!node.CrossAssemblyCalls.Any(x => x.GetFromNodeId().Equals(call.GetFromNodeId())))
                    node.CrossAssemblyCalls.Add(call);
            }

            foreach (var call in PublicInnerAssemblyCalls)
            {
                if (!node.PublicInnerAssemblyCalls.Any(x => x.GetFromNodeId().Equals(call.GetFromNodeId())))
                    node.PublicInnerAssemblyCalls.Add(call);
            }

            foreach (var call in NonPublicInnerAssemblyCalls)
            {
                if (!node.NonPublicInnerAssemblyCalls.Any(x => x.GetFromNodeId().Equals(call.GetFromNodeId())))
                    node.NonPublicInnerAssemblyCalls.Add(call);
            }

            foreach (var resourceAccess in ResourceAccesses)
            {
                node.AddResourceAccess(resourceAccess);
            }
        }
    }
}
