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
using System;

namespace Graphode.CodeAnalyzer.Implementations.CallGraph
{
    public class CallTreeWalker : ICallTreeWalker
    {
        private readonly IMethodIndexer _methodIndexer;
        private readonly IDatabaseResolver _databaseResolver;
        private readonly ILogOutput _logOutput;

        private Dictionary<string, MethodNode> _methodNodeLookup;
        private string _companyAssembliesPattern;

        public CallTreeWalker(IMethodIndexer methodIndexer,
            IDatabaseResolver databaseResolver,
            ILogOutput logOutput)
        {
            _methodIndexer = methodIndexer;
            _databaseResolver = databaseResolver;
            _logOutput = logOutput;
            _methodNodeLookup = new Dictionary<string, MethodNode>();
        }

        #region Public methods

        public MethodGraph BuildCrossAssemblyGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules)
        {
            _companyAssembliesPattern = companyAssembliesPattern;
            var methodGraph = new MethodGraph(applicationName, GraphType.CrossAssembly);

            int moduleCounter = 1;
            foreach (var module in modules)
            {
                string moduleMessagee = "Cross Assembly Graph - Module " + moduleCounter + " of " + modules.Count + "  " + module.Name;
                DoCrossAssemblyWalk(methodGraph, companyAssembliesPattern, module, moduleMessagee);

                moduleCounter++;
            }

            return methodGraph;
        }

        public MethodGraph BuildPublicInnerAssemblyGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules)
        {
            _companyAssembliesPattern = companyAssembliesPattern;
            var methodGraph = new MethodGraph(applicationName, GraphType.PublicMethods);

            int moduleCounter = 1;
            foreach (var module in modules)
            {
                string moduleMessagee = "Public Method Assembly Graph - Module " + moduleCounter + " of " + modules.Count + "  " + module.Name;
                DoPublicInnerAssemblyWalk(methodGraph, companyAssembliesPattern, module, moduleMessagee);

                moduleCounter++;
            }

            return methodGraph;
        }

        public MethodGraph BuildFullGraph(string applicationName, string companyAssembliesPattern, List<ModuleDefinition> modules)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region Cross Assembly Graph

        private void DoCrossAssemblyWalk(MethodGraph methodGraph, string companyAssembliesPattern, ModuleDefinition module, string moduleMessagee)
        {
            var publicMethods = DecompilerService.GetPublicMethods(companyAssembliesPattern, module)
                .Where(x => !IsBlackListed(x))
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
                            var publicMethodNode = GetMethodNode(methodGraph.GraphType, methodGraph.ApplicationName, rootMethod);
                            var callTreeNode = new ExploreTreeNode() { FullSignature = signature };
                            CrossAssemblyWalk(methodGraph, publicMethodNode, rootMethod, 1, callTreeNode);
                            CacheNode(rootMethod.GetMethodDefinition(), publicMethodNode);
                            methodGraph.AddMethodNode(publicMethodNode);
                        }
                    }
                }
            }
        }

        private void CrossAssemblyWalk(MethodGraph methodGraph, MethodNode rootMethod, MethodObject currentMethod, int depth, ExploreTreeNode callTreeNode)
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
                // if it is a simple property access then we don't care. Only add it if the access is interesting
                if (IsNoteworthyMethodCall(currentMethod))
                {
                    var publicMethod = GetMethodNode(methodGraph.GraphType, methodGraph.ApplicationName, currentMethod);
                    rootMethod.CrossAssemblyCalls.Add(publicMethod);
                }
            }
            else
            {
                // continue down the call tree unless the called method is of another assembly
                // the call tree originating at that method will be generated when that assembly is analyzed
                ContinueDownCrossAssemblyTree(methodGraph, rootMethod, currentMethod, depth, callTreeNode);
            }
        }

        private void ContinueDownCrossAssemblyTree(MethodGraph methodGraph, MethodNode rootMethod, MethodObject currentMethod, int depth, ExploreTreeNode callTreeNode)
        {
            foreach (var calledMethod in currentMethod.MethodsCalled)
            {
                CheckForResourceCall(methodGraph, calledMethod, currentMethod, rootMethod);
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
                            CrossAssemblyWalk(methodGraph, rootMethod, calledMethodNode, depth + 1, treeNode);
                    }
                }
            }
        }

        #endregion Cross Assembly Graph


        #region Public Inner Assembly Graph

        private void DoPublicInnerAssemblyWalk(MethodGraph methodGraph, string companyAssembliesPattern, ModuleDefinition module, string moduleMessagee)
        {
            var publicMethods = DecompilerService.GetPublicMethods(companyAssembliesPattern, module)
                .Where(x => !IsBlackListed(x))
                .OrderBy(x => x.DeclaringType.Name)
                .ThenBy(x => x.Name)
                .ToList();

            int methodCount = publicMethods.Count;
            var publicMethodsAnalyzed = new HashSet<string>();
            _methodNodeLookup.Clear();

            int methodCounter = 0;
            foreach (var method in publicMethods)
            {
                methodCounter++;
                _logOutput.LogAnalysis("Method " + methodCounter + " of " + methodCount + " : " + moduleMessagee + " -> " + method.Name);
                if ((method.IsGetter || method.IsSetter) && !IsNoteworthyProperty(method))
                    continue;

                var signature = SignatureKeyService.GetFullMethodSignature(method);
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
                            var publicMethodNode = GetMethodNode(methodGraph.GraphType, methodGraph.ApplicationName, rootMethod);
                            var callTreeNode = new ExploreTreeNode() { FullSignature = signature };
                            PublicInnerAssemblyWalk(methodGraph, publicMethodNode, rootMethod, 1, callTreeNode);
                            CacheNode(rootMethod.GetMethodDefinition(), publicMethodNode);
                            methodGraph.AddMethodNode(publicMethodNode);
                        }
                    }
                }
            }
        }

        private void PublicInnerAssemblyWalk(MethodGraph methodGraph, MethodNode rootMethod, MethodObject currentMethod, int depth, ExploreTreeNode callTreeNode)
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
            var isPublicInnerAssemblyCall = IsPublicInnerAssemblyCall(rootMethod, currentMethod, depth);
            var currentMethodNode = GetMethodNode(methodGraph.GraphType, methodGraph.ApplicationName, currentMethod);

            if (isCrossAssemblyCall)
            {
                // if it is a simple property access then we don't care. Only add it if the access is interesting
                if (IsNoteworthyMethodCall(currentMethod))
                    rootMethod.CrossAssemblyCalls.Add(currentMethodNode);
            }
            else if (isPublicInnerAssemblyCall)
            {
                // if it is a simple property access then we don't care. Only add it if the access is interesting
                if (IsNoteworthyMethodCall(currentMethod))
                    rootMethod.PublicInnerAssemblyCalls.Add(currentMethodNode);
            }

            // continue down the call tree unless the called method is of another assembly or is public
            // the call tree originating at a method of another assembly will be generated when that assembly is analyzed
            // the call tree originating at a public method of this assembly will be generated when that public method is walked
            if (!isCrossAssemblyCall && !isPublicInnerAssemblyCall)
                ContinueDownPublicInnerAssemblyTree(methodGraph, rootMethod, currentMethod, depth, callTreeNode);
        }

        private void ContinueDownPublicInnerAssemblyTree(MethodGraph methodGraph, MethodNode parentMethodNode, MethodObject parentMethod, int depth, ExploreTreeNode callTreeNode)
        {
            foreach (var calledMethod in parentMethod.MethodsCalled)
            {
                CheckForResourceCall(methodGraph, calledMethod, parentMethod, parentMethodNode);
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

                        if (cachedRootNode != null) // this is a call to an already analyzed method, we copy over the calls and resource accesses already calculated for this node
                            cachedRootNode.CopyCallsToNode(parentMethodNode);
                        else // this is not a call to a previously analyzed method, so we continue down the call tree
                            PublicInnerAssemblyWalk(methodGraph, parentMethodNode, calledMethodNode, depth + 1, treeNode);
                    }
                }
            }
        }

        #endregion Public Inner Assembly Graph


        private bool AlreadyProcessed(MethodDefinition methodDefinition)
        {
            return _methodNodeLookup.ContainsKey(methodDefinition.FullName);
        }

        private void CacheNode(MethodDefinition methodDefinition, MethodNode publicMethodNode)
        {
            if (!_methodNodeLookup.ContainsKey(methodDefinition.FullName))
                _methodNodeLookup.Add(methodDefinition.FullName, publicMethodNode);
        }

        private MethodNode GetCachedRootNode(MethodDefinition methodDefinition)
        {
            MethodNode cachedNode = null;
            if (_methodNodeLookup.TryGetValue(methodDefinition.FullName, out cachedNode))
                return cachedNode;

            return null;
        }

        private bool IsBlackListed(MethodReference methodReference)
        {
            if (methodReference.DeclaringType.Name.Equals("DynamicExpression"))
                return true;

            if (methodReference.DeclaringType.Name.Equals("DynamicQueryable"))
                return true;

            return false;
        }

        private MethodNode GetMethodNode(GraphType graphType, string appDomain, MethodObject method)
        {
            var methodDef = method.GetMethodDefinition();

            var methodNode = new MethodNode(graphType, appDomain);
            methodNode.MethodName = SignatureKeyService.GetMethodSignature(methodDef);
            methodNode.IsPublic = methodDef.IsPublic && methodDef.DeclaringType.IsPublic;

            if (method.HasImplementation())
            {
                methodNode.ConcreteType = new TypeInfo();
                methodNode.ConcreteType.AssemblyName = method.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name;
                methodNode.ConcreteType.AssemblyVersion = GetAssemblyVersion(method.ConcreteMethod);
                methodNode.ConcreteType.TypeName = method.ConcreteMethod.DeclaringType.FullName;
            }
            else
            {

            }

            if (method.HasInterface())
            {
                methodNode.InterfaceType = new TypeInfo();
                methodNode.InterfaceType.AssemblyName = method.InterfaceMethod.DeclaringType.Module.Assembly.Name.Name;
                methodNode.InterfaceType.AssemblyVersion = GetAssemblyVersion(method.InterfaceMethod);
                methodNode.InterfaceType.TypeName = method.InterfaceMethod.DeclaringType.FullName;
            }

            if (method.HasAbstract())
            {
                methodNode.AbstractType = new TypeInfo();
                methodNode.AbstractType.AssemblyName = method.AbstractMethod.DeclaringType.Module.Assembly.Name.Name;
                methodNode.AbstractType.AssemblyVersion = GetAssemblyVersion(method.AbstractMethod);
                methodNode.AbstractType.TypeName = method.AbstractMethod.DeclaringType.FullName;
            }

            if (method.OverridesBaseClass())
            {
                methodNode.BaseClassType = new TypeInfo();
                methodNode.BaseClassType.AssemblyName = method.VirtualMethod.DeclaringType.Module.Assembly.Name.Name;
                methodNode.BaseClassType.AssemblyVersion = GetAssemblyVersion(method.VirtualMethod);
                methodNode.BaseClassType.TypeName = method.VirtualMethod.DeclaringType.FullName;
            }

            return methodNode;
        }

        private string GetAssemblyVersion(MethodDefinition method)
        {
            var fullAssemblyName = method.Module.Assembly.FullName;

            var versionIndex = fullAssemblyName.IndexOf("Version=");
            var commaIndex = fullAssemblyName.IndexOf(',', versionIndex);
            var version = fullAssemblyName.Substring(versionIndex + 8, commaIndex - (versionIndex + 8));
            return version;
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

        private bool IsCrossAssemblyCall(MethodNode rootMethod, MethodObject calledMethod, int depth)
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

        private bool IsPublicInnerAssemblyCall(MethodNode rootMethod, MethodObject calledMethod, int depth)
        {
            if (depth == 1)
                return false;

            var calledMethodDefinition = calledMethod.GetMethodDefinition();

            // if the method called is not public then return false
            if (!calledMethodDefinition.IsPublic && !calledMethodDefinition.DeclaringType.IsPublic)
                return false;

            return IsInnerAssemblyCall(rootMethod, calledMethod, calledMethodDefinition);
        }

        private bool IsNonPublicInnerAssemblyCall(MethodNode rootMethod, MethodObject calledMethod, int depth)
        {
            if (depth == 1)
                return false;

            var calledMethodDefinition = calledMethod.GetMethodDefinition();

            // if the method called is not public then return false
            if (calledMethodDefinition.IsPublic && calledMethodDefinition.DeclaringType.IsPublic)
                return false;

            return IsInnerAssemblyCall(rootMethod, calledMethod, calledMethodDefinition);
        }

        private bool IsInnerAssemblyCall(MethodNode rootMethod, MethodObject calledMethod, MethodDefinition calledMethodDefinition)
        {
            if (calledMethod.GetMethodType() == MethodType.ImplAndInterface) // method has concrete implementation and interface
            {
                // if the concrete method called is not of the same assembly as the root method then return false
                if (!calledMethod.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }
            else if (calledMethod.GetMethodType() == MethodType.InterfaceOnly) // method has only an interface (concrete impl module may not be loaded)
            {
                // if the interface method called is not of the same assembly as the root method then return false
                if (!calledMethod.InterfaceMethod.DeclaringType.Module.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }
            else // just a concrete impl with no interface
            {
                // if the concrete method called is not of the same assembly as the root method then return false
                if (!calledMethod.ConcreteMethod.DeclaringType.Module.Assembly.Name.Name.Equals(rootMethod.ConcreteType.AssemblyName))
                    return false;
            }

            // if the called method is not of the company's namespace then return flase
            if (!Regex.IsMatch(calledMethodDefinition.DeclaringType.Namespace, _companyAssembliesPattern))
                return false;

            return true;
        }

        private bool IsNoteworthyMethodCall(MethodObject calledMethod)
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

        private void CheckForResourceCall(MethodGraph methodGraph, MethodCall calledMethod, MethodObject currentMethod, MethodNode rootMethod)
        {
            var dbMatch = _databaseResolver.IsTargetMethodMatch(calledMethod, currentMethod);
            if (dbMatch.IsMatch)
            {
                var databaseKey = _databaseResolver.GetDatabaseKey(dbMatch, calledMethod, currentMethod);
                if (databaseKey != null)
                {
                    var resourceAccessNode = new ResourceAccessNode(methodGraph.GraphType, methodGraph.ApplicationName);
                    resourceAccessNode.ConfigurationResource = ConfigurationResource.Database;
                    resourceAccessNode.ResourceKey = databaseKey;

                    rootMethod.AddResourceAccess(resourceAccessNode);
                    methodGraph.AddResourceAccessNode(resourceAccessNode);
                }

                return;
            }
        }
    }
}
