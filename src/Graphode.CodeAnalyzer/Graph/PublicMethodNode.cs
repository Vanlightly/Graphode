using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations.Common;

namespace Graphode.CodeAnalyzer.Graph
{
    public class TypeInfo
    {
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string TypeName { get; set; }
    }

    public class PublicMethodNode
    {
        public PublicMethodNode()
        {
            CrossAssemblyCalls = new List<PublicMethodNode>();
            ResourceAccesses = new List<ResourceAccessNode>();
        }

        private List<ResourceAccessNode> ResourceAccesses { get; set; }

        public string MethodName { get; set; }
        public string AppDomain { get; set; }
        public TypeInfo InterfaceType { get; set; }
        public TypeInfo AbstractType { get; set; }
        public TypeInfo BaseClassType { get; set; }
        public TypeInfo ConcreteType { get; set; }
        
        public List<PublicMethodNode> CrossAssemblyCalls { get; set; }

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

            var idString = string.Empty;
            if (InterfaceType != null)
                idString = AppDomain + ":" + InterfaceType.AssemblyName + ":" + InterfaceType.AssemblyVersion + ":" + InterfaceType.TypeName + ":" + MethodName;
            else if (AbstractType != null)
                idString = AppDomain + ":" + AbstractType.AssemblyName + ":" + AbstractType.AssemblyVersion + ":" + AbstractType.TypeName + ":" + MethodName;
            else if (ConcreteType != null)
                idString = AppDomain + ":" + ConcreteType.AssemblyName + ":" + ConcreteType.AssemblyVersion + ":" + ConcreteType.TypeName + ":" + MethodName;
            else
                throw new Exception("Id cannot be generated as no types exist. Method is " + MethodName);

            _toNodeId = HashService.GetMd5Hash(idString);

            return _toNodeId;
        }

        private string _fromNodeId;
        public string GetFromNodeId()
        {
            if (!string.IsNullOrEmpty(_toNodeId))
                return _fromNodeId;

            var idString = string.Empty;
            if (ConcreteType != null)
                idString = AppDomain + ":" + ConcreteType.AssemblyName + ":" + ConcreteType.AssemblyVersion + ":" + ConcreteType.TypeName + ":" + MethodName;
            else if (AbstractType != null)
                idString = AppDomain + ":" + AbstractType.AssemblyName + ":" + AbstractType.AssemblyVersion + ":" + AbstractType.TypeName + ":" + MethodName;
            else if (InterfaceType != null)
                idString = AppDomain + ":" + InterfaceType.AssemblyName + ":" + InterfaceType.AssemblyVersion + ":" + InterfaceType.TypeName + ":" + MethodName;
            else
                throw new Exception("Id cannot be generated as no types exist. Method is " + MethodName);

            _fromNodeId = HashService.GetMd5Hash(idString);

            return _fromNodeId;
        }

        public void CopyCallsToNode(PublicMethodNode node)
        {
            foreach (var crossAssemblyCall in CrossAssemblyCalls)
            {
                if (!node.CrossAssemblyCalls.Any(x => x.GetFromNodeId().Equals(crossAssemblyCall.GetFromNodeId())))
                {
                    node.CrossAssemblyCalls.Add(crossAssemblyCall);
                }
                else
                {

                }
            }

            foreach (var resourceAccess in ResourceAccesses)
            {
                node.AddResourceAccess(resourceAccess);
            }
        }
    }
}
