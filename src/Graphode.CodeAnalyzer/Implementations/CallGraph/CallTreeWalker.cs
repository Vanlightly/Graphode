using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using Graphode.CodeAnalyzer.Contracts.CallGraph;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Implementations.Resolvers;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Entities.Common;
using System.Text.RegularExpressions;
using System.Configuration;
using Graphode.CodeAnalyzer.Graph;
using Graphode.CodeAnalyzer.Logging;

namespace Graphode.CodeAnalyzer.Implementations.CallGraph
{
    public class CallTreeWalker : ICallTreeWalker
    {
        private readonly IMethodIndexer _methodIndexer;
        private readonly IDatabaseResolver _databaseResolver;
        private readonly ILogOutput _logOutput;
        
        private MethodGraph _methodGraph;
        private Dictionary<string, PublicMethodNode> _methodNodeLookup;
        private string _companyAssembliesPattern;
        
        public CallTreeWalker(IMethodIndexer methodIndexer,
            IDatabaseResolver databaseResolver,
            ILogOutput logOutput)
        {
            _methodIndexer = methodIndexer;
            _databaseResolver = databaseResolver;
            _logOutput = logOutput;
            _methodNodeLookup = new Dictionary<string, PublicMethodNode>();
        }

        public MethodGraph WalkMethods(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules)
        {
            _companyAssembliesPattern = companyAssembliesPattern;
            _methodGraph = new MethodGraph(applicationName);

            int moduleCounter = 1;
            foreach (var module in modules)
            {
                string moduleMessagee = "Module " + moduleCounter + " of " + modules.Count + "  " + module.Name;
                WalkMethods(companyAssembliesPattern, module, moduleMessagee);

                moduleCounter++;
            }

            return _methodGraph;
        }

        public void WalkMethods(string companyAssembliesPattern, ModuleDefinition module, string moduleMessagee)
        {
            var publicMethods = DecompilerService.GetPublicMethods(companyAssembliesPattern, module)
                .Where(x => !IsBackListed(x))
                .OrderBy(x => x.DeclaringType.Name)
                .ThenBy(x => x.Name)
                .ToList();

            int methodCount = publicMethods.Count;
            var publicMethodsAnalyzed = new HashSet<string>();
            _methodNodeLookup.Clear();

            int methodCounter = 0;
            foreach (var publicMethod in publicMethods)
            {
                methodCounter++;
                _logOutput.LogAnalysis("Method " + methodCounter + " of " + methodCount + " : " + moduleMessagee + " -> " + publicMethod.Name);
                if ((publicMethod.IsGetter || publicMethod.IsSetter) && !IsNoteworthyProperty(publicMethod))
                    continue;

                var signature = SignatureKeyService.GetFullMethodSignature(publicMethod);
                if (_methodIndexer.HasMethod(signature))
                {
                    var unfilteredRootNodes = _methodIndexer.GetMethods(signature);
                    var rootNodes = unfilteredRootNodes.Where(x => x.HasImplementation() 
                                        && (
                                            // if it is a public implementation of a different assembly, then'll we'll filter it out here (and analyze it that assembly)
                                            (x.ConcreteMethod.IsPublic && x.ConcreteMethod.Module.Name.Equals(module.Name)) 
                                            // if it is a private implementation then analyze it now as we'll miss it when we analyze the public methods of the other assembly
                                            || !x.ConcreteMethod.DeclaringType.IsPublic
                                           )
                                        )
                                        .ToList();

                    foreach (var rootMethod in rootNodes)
                    {
                        if (!AlreadyProcessed(rootMethod.GetMethodDefinition()))
                        {
                            var publicMethodNode = GetPublicMethodNode(rootMethod);
                            var callTreeNode = new ExploreTreeNode() { FullSignature = signature };
                            Walk(publicMethodNode, rootMethod, 1, callTreeNode);
                            CacheNode(rootMethod.GetMethodDefinition(), publicMethodNode);
                            _methodGraph.AddMethodNode(publicMethodNode);
                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        private bool AlreadyProcessed(MethodDefinition methodDefinition)
        {
            return _methodNodeLookup.ContainsKey(methodDefinition.FullName);
        }

        private void CacheNode(MethodDefinition methodDefinition, PublicMethodNode publicMethodNode)
        {
            if(!_methodNodeLookup.ContainsKey(methodDefinition.FullName))
                _methodNodeLookup.Add(methodDefinition.FullName, publicMethodNode);
        }

        private PublicMethodNode GetCachedRootNode(MethodDefinition methodDefinition)
        {
            PublicMethodNode cachedNode = null;
            if (_methodNodeLookup.TryGetValue(methodDefinition.FullName, out cachedNode))
                return cachedNode;

            return null;
        }

        private bool IsBackListed(MethodReference methodReference)
        {
            if (methodReference.DeclaringType.Name.Equals("DynamicExpression"))
                return true;

            if (methodReference.DeclaringType.Name.Equals("DynamicQueryable"))
                return true;

            return false;
        }

        private PublicMethodNode GetPublicMethodNode(MethodObject method)
        {
            var publicMethodNode = new PublicMethodNode();
            publicMethodNode.AppDomain = method.AppDomain;
            publicMethodNode.MethodName = SignatureKeyService.GetMethodSignature(method.GetMethodDefinition());

            if (method.HasImplementation())
            {
                publicMethodNode.ConcreteType = new TypeInfo();
                publicMethodNode.ConcreteType.AssemblyName = method.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name;
                publicMethodNode.ConcreteType.AssemblyVersion = GetAssemblyVersion(method.ConcreteMethod);
                publicMethodNode.ConcreteType.TypeName = method.ConcreteMethod.DeclaringType.FullName;
            }
            else
            {

            }

            if (method.HasInterface())
            {
                publicMethodNode.InterfaceType = new TypeInfo();
                publicMethodNode.InterfaceType.AssemblyName = method.InterfaceMethod.DeclaringType.Module.Assembly.Name.Name;
                publicMethodNode.InterfaceType.AssemblyVersion = GetAssemblyVersion(method.InterfaceMethod);
                publicMethodNode.InterfaceType.TypeName = method.InterfaceMethod.DeclaringType.FullName;
            }

            if (method.HasAbstract())
            {
                publicMethodNode.AbstractType = new TypeInfo();
                publicMethodNode.AbstractType.AssemblyName = method.AbstractMethod.DeclaringType.Module.Assembly.Name.Name;
                publicMethodNode.AbstractType.AssemblyVersion = GetAssemblyVersion(method.AbstractMethod);
                publicMethodNode.AbstractType.TypeName = method.AbstractMethod.DeclaringType.FullName;
            }

            if (method.OverridesBaseClass())
            {
                publicMethodNode.BaseClassType = new TypeInfo();
                publicMethodNode.BaseClassType.AssemblyName = method.VirtualMethod.DeclaringType.Module.Assembly.Name.Name;
                publicMethodNode.BaseClassType.AssemblyVersion = GetAssemblyVersion(method.VirtualMethod);
                publicMethodNode.BaseClassType.TypeName = method.VirtualMethod.DeclaringType.FullName;
            }

            return publicMethodNode;
        }

        private string GetAssemblyVersion(MethodDefinition method)
        {
            var fullAssemblyName = method.Module.Assembly.FullName;

            var versionIndex = fullAssemblyName.IndexOf("Version=");
            var commaIndex = fullAssemblyName.IndexOf(',', versionIndex);
            var version = fullAssemblyName.Substring(versionIndex + 8, commaIndex - (versionIndex + 8));
            return version;
        }

        private void Walk(PublicMethodNode rootMethod, MethodObject currentMethod, int depth, ExploreTreeNode callTreeNode)
        {
            if (IsRecursiveLoop(callTreeNode))
                return;

            if (!currentMethod.HasImplementation())
            {
                // Perhaps log it somewhere in debug mode
                //File.AppendAllText("No_Implementation.txt", currentMethod.GetMethodDefinition().FullName);
                return;
            }

            var isCrossAssemblyCall = IsCrossAssemblyCall(rootMethod, currentMethod, depth);
            if (isCrossAssemblyCall)
            {
                // we stop walking this branch here and if it is worthy of
                // logging the call we will do so
                if (IsNoteworthyCrossAssemblyCall(currentMethod))
                {
                    var publicMethod = GetPublicMethodNode(currentMethod);
                    rootMethod.CrossAssemblyCalls.Add(publicMethod);
                }
            }
            else
            {
                ContinueDownCallTree(rootMethod, currentMethod, depth, callTreeNode);
            }
        }

        private bool IsRecursiveLoop(ExploreTreeNode callTreeNode)
        {
            ExploreTreeNode parent = callTreeNode.Parent;
            while (parent != null)
            {
                if (parent.FullSignature.Equals(callTreeNode.FullSignature))
                    return true;

                parent = parent.Parent;
            }

            return false;
        }

        private void ContinueDownCallTree(PublicMethodNode rootMethod, MethodObject currentMethod, int depth, ExploreTreeNode callTreeNode)
        {
            foreach (var calledMethod in currentMethod.MethodsCalled)
            {
                CheckForResourceCall(calledMethod, currentMethod, rootMethod);
                var calledMethodSignature = SignatureKeyService.GetFullMethodSignature(calledMethod.MethodCalled);
                var treeNode = new ExploreTreeNode() { FullSignature = calledMethodSignature };
                callTreeNode.AddChild(treeNode);

                bool isGenericAndIndexed = false;
                string genericSignature = null;
                var methodIsIndexed = _methodIndexer.HasMethod(calledMethodSignature);
                if (!methodIsIndexed)
                {
                    genericSignature = SignatureKeyService.GetGenericMethodSignature(calledMethod.MethodCalled);
                    if (!string.IsNullOrEmpty(genericSignature))
                        isGenericAndIndexed = _methodIndexer.HasMethod(genericSignature);
                }

                if (methodIsIndexed || isGenericAndIndexed)
                {
                    List<MethodObject> matchingMethodNodes = null;
                    if (methodIsIndexed)
                        matchingMethodNodes = _methodIndexer.GetMethods(calledMethodSignature);
                    else if (isGenericAndIndexed)
                        matchingMethodNodes = _methodIndexer.GetMethods(genericSignature);

                    foreach (var calledMethodNode in matchingMethodNodes)
                    {
                        var cachedRootNode = GetCachedRootNode(calledMethodNode.GetMethodDefinition());
                        
                        if (cachedRootNode != null) // this is a call to an already analyzed public method, we copy over the cross assembly calls and resource accesses already calculated for this node
                            cachedRootNode.CopyCallsToNode(rootMethod);
                        else // this is not a call to a previously analyzed puyblic method, so we continue down the call tree
                            Walk(rootMethod, calledMethodNode, depth + 1, treeNode);
                    }
                }
            }
        }



        private bool IsCrossAssemblyCall(PublicMethodNode rootMethod, MethodObject calledMethod, int depth)
        {
            if (depth == 1)
                return false;

            var calledMethodDefinition = calledMethod.GetMethodDefinition();
            if (!calledMethodDefinition.IsPublic && !calledMethodDefinition.DeclaringType.IsPublic)
                return false;

            if (calledMethod.GetMethodType() == MethodType.ImplAndInterface)
            {
                if (calledMethod.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }
            else if (calledMethod.GetMethodType() == MethodType.InterfaceOnly)
            {
                if (calledMethod.InterfaceMethod.DeclaringType.Module.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }
            else
            {
                if (calledMethod.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }

            if (!Regex.IsMatch(calledMethodDefinition.DeclaringType.Namespace, _companyAssembliesPattern))
                return false;

            return true;
        }

        private bool IsNoteworthyCrossAssemblyCall(MethodObject calledMethod)
        {
            var calledMethodDefinition = calledMethod.GetMethodDefinition();
            if (calledMethodDefinition.IsGetter)
            {
                return IsNoteworthyProperty(calledMethodDefinition);
            }
            else if (calledMethodDefinition.IsSetter)
            {
                return false;
            }
            else if (calledMethodDefinition.IsConstructor)
            {
                var isNoteworthyConstructor = calledMethod.MethodsCalled.Any(x => !x.MethodCalled.DeclaringType.Module.Name.Equals(calledMethod.GetMethodDefinition().DeclaringType.Module.Name)
                    && !Regex.IsMatch(x.MethodCalled.DeclaringType.Module.Name, _companyAssembliesPattern));

                return isNoteworthyConstructor;
            }


            return true;
        }

        private bool IsNoteworthyProperty(MethodDefinition calledMethodDefinition)
        {
            if (calledMethodDefinition.IsGetter || calledMethodDefinition.IsSetter)
            {
                var backingType = PropertyService.GetBackingField(calledMethodDefinition);
                if (backingType != PropertyBackingType.MethodBacked)
                    return false;
            }

            return true;
        }

        private void CheckForResourceCall(MethodCall calledMethod, MethodObject currentMethod, PublicMethodNode rootMethod)
        {
            var dbMatch = _databaseResolver.IsTargetMethodMatch(calledMethod, currentMethod);
            if (dbMatch.IsMatch)
            {
                var databaseKey = _databaseResolver.GetDatabaseKey(dbMatch, calledMethod, currentMethod);
                if (databaseKey != null)
                {
                    var resourceAccessNode = new ResourceAccessNode();
                    resourceAccessNode.AppDomain = currentMethod.AppDomain;
                    resourceAccessNode.ConfigurationResource = ConfigurationResource.Database;
                    resourceAccessNode.ResourceKey = databaseKey;

                    rootMethod.AddResourceAccess(resourceAccessNode);
                    _methodGraph.AddResourceAccessNode(resourceAccessNode);
                }

                return;
            }
        }
    }
}
