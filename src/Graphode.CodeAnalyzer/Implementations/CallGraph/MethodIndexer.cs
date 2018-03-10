using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.CallGraph;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.CodeAnalyzer.Implementations.CallGraph
{
    // singleinstance!!
    public class MethodIndexer : IMethodIndexer
    {
        private readonly IDelegateIndexer _delegateIndexer;


        public MethodIndexer(IDelegateIndexer delegateIndexer)
        {
            _delegateIndexer = delegateIndexer;
            CleanIndexes();
        }

        public Dictionary<string, MethodDefinition> InterfaceMethodsIndexedByName { get; set; }
        public Dictionary<string, MethodDefinition> InterfaceMethodsIndexedByGenericSignature { get; set; }
        public Dictionary<string, MethodDefinition> AbstractMethodsIndexedByName { get; set; }
        public Dictionary<string, MethodDefinition> ImplementationMethodsIndexedByName { get; set; }
        public Dictionary<string, MethodDefinition> ImplementationMethodsIndexedByGenericSignature { get; set; }
        public List<MethodDefinition> ImplementationMethodsList { get; set; }
        public List<MethodDefinition> InterfaceMethodsList { get; set; }
        public List<MethodDefinition> AbstractMethodsList { get; set; }

        public IndexedList<string, MethodDefinition> InterfaceMethodsIndexedByTypeName { get; set; }
        public IndexedList<string, MethodDefinition> AbstractMethodsIndexedByTypeName { get; set; }
        public IndexedList<string, MethodDefinition> ImplementationMethodsIndexedByTypeName { get; set; }

        public Dictionary<string, TypeDefinition> InterfaceTypes { get; set; }
        public Dictionary<string, TypeDefinition> AbstractTypes { get; set; }
        public Dictionary<string, TypeDefinition> ImplementationTypes { get; set; }

        public List<MethodObject> MethodObjectsList { get; set; }
        public IndexedList<string, MethodObject> MethodObjectsIndexedByFullName { get; set; }

        public void CleanIndexes()
        {
            InterfaceMethodsIndexedByGenericSignature = new Dictionary<string, MethodDefinition>();
            InterfaceMethodsIndexedByName = new Dictionary<string, MethodDefinition>();
            AbstractMethodsIndexedByName = new Dictionary<string, MethodDefinition>();
            ImplementationMethodsIndexedByName = new Dictionary<string, MethodDefinition>();
            ImplementationMethodsIndexedByGenericSignature = new Dictionary<string, MethodDefinition>();

            ImplementationMethodsList = new List<MethodDefinition>();
            InterfaceMethodsList = new List<MethodDefinition>();
            AbstractMethodsList = new List<MethodDefinition>();

            InterfaceMethodsIndexedByTypeName = new IndexedList<string, MethodDefinition>();
            AbstractMethodsIndexedByTypeName = new IndexedList<string, MethodDefinition>();
            ImplementationMethodsIndexedByTypeName = new IndexedList<string, MethodDefinition>();

            MethodObjectsIndexedByFullName = new IndexedList<string, MethodObject>();

            InterfaceTypes = new Dictionary<string, TypeDefinition>();
            AbstractTypes = new Dictionary<string, TypeDefinition>();
            ImplementationTypes = new Dictionary<string, TypeDefinition>();

            MethodObjectsList = new List<MethodObject>();
        }

        public void Load(ModuleDefinition module)
        {
            IndexInterfaceMethods(module);
            IndexAbstractMethods(module);
            IndexConcreteMethods(module);
        }

        private void IndexInterfaceMethods(ModuleDefinition module)
        {
            var interfaceMethods = DecompilerService.GetInterfaceMethods(module);

            foreach (var method in interfaceMethods)
            {
                // index by a simplified generic signature if it is a generic interface method
                var genericSignature = SignatureKeyService.GetGenericMethodSignature(method);
                if (!string.IsNullOrEmpty(genericSignature))
                {
                    if (!InterfaceMethodsIndexedByGenericSignature.ContainsKey(genericSignature))
                        InterfaceMethodsIndexedByGenericSignature.Add(genericSignature, method);
                }

                // index by the standard method signature
                var methodSignature = SignatureKeyService.GetFullMethodSignature(method);
                if (!InterfaceMethodsIndexedByName.ContainsKey(methodSignature))
                    InterfaceMethodsIndexedByName.Add(methodSignature, method);

                InterfaceMethodsIndexedByTypeName.Add(method.DeclaringType.FullName, method);
                InterfaceMethodsList.Add(method);
            }
        }

        private void IndexAbstractMethods(ModuleDefinition module)
        {
            var abstractMethods = DecompilerService.GetAbstractMethods(module);

            foreach (var method in abstractMethods)
            {
                AbstractMethodsIndexedByName.Add(SignatureKeyService.GetFullMethodSignature(method), method);
                AbstractMethodsIndexedByTypeName.Add(method.DeclaringType.FullName, method);
                AbstractMethodsList.Add(method);
            }
        }

        private void IndexConcreteMethods(ModuleDefinition module)
        {
            var implementationMethods = DecompilerService.GetConcreteMethods(module);

            foreach (var method in implementationMethods)
            {
                if (ShouldSkip(method))
                    continue;

                var methodNameKey = SignatureKeyService.GetFullMethodSignature(method);
                if (!ImplementationMethodsIndexedByName.ContainsKey(methodNameKey))
                {
                    ImplementationMethodsIndexedByName.Add(methodNameKey, method);
                    ImplementationMethodsList.Add(method);
                    ImplementationMethodsIndexedByTypeName.Add(method.DeclaringType.FullName, method);
                }

                var genericSignature = SignatureKeyService.GetGenericMethodSignature(method);
                if (!string.IsNullOrEmpty(genericSignature))
                {
                    if (!ImplementationMethodsIndexedByGenericSignature.ContainsKey(genericSignature))
                        ImplementationMethodsIndexedByGenericSignature.Add(genericSignature, method);
                }
            }
        }

        private bool ShouldSkip(MethodDefinition method)
        {
            return method.FullName.IndexOf("AnonymousType") > -1
                || method.FullName.IndexOf("PrivateImplementationDetails") > -1
                || (method.DeclaringType.BaseType != null && method.DeclaringType.BaseType.FullName.Equals("System.Web.Services.Protocols.SoapHttpClientProtocol"));
        }


        public void BuildMethodObjects(string appDomain)
        {
            foreach (var method in ImplementationMethodsList)
            {
                if (ShouldSkip(method))
                    continue;

                var node = new MethodObject();
                node.AppDomain = appDomain;
                node.ConcreteMethod = method;

                var implementsInterface = FindInterfaceMethod(method, node);
                if (implementsInterface)
                {
                    MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(node.InterfaceMethod), node);
                    var genericSignature = SignatureKeyService.GetGenericMethodSignature(node.InterfaceMethod);
                    if (!string.IsNullOrEmpty(genericSignature))
                        MethodObjectsIndexedByFullName.Add(genericSignature, node);
                }

                var implementsAbstractClass = FindAbstractMethod(method, node);
                if (implementsAbstractClass)
                    MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(node.AbstractMethod), node);

                var inheritsFromBaseClass = FindBaseClassMethod(method, node);
                if (inheritsFromBaseClass)
                    MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(node.VirtualMethod), node);

                FindFieldsAndMethods(method, node, true);
                MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(method), node);
                MethodObjectsList.Add(node);
            }

            foreach (var method in InterfaceMethodsList)
            {
                if (!MethodObjectsIndexedByFullName.HasIndex(SignatureKeyService.GetFullMethodSignature(method)))
                {
                    var node = new MethodObject();
                    node.AppDomain = appDomain;
                    node.InterfaceMethod = method;
                    node.ImplementsType = ImplementsType.None;

                    MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(method), node);
                }
            }

            foreach (var method in AbstractMethodsList)
            {
                if (!MethodObjectsIndexedByFullName.HasIndex(SignatureKeyService.GetFullMethodSignature(method)))
                {
                    var node = new MethodObject();
                    node.AppDomain = appDomain;
                    node.AbstractMethod = method;
                    node.ImplementsType = ImplementsType.None;

                    MethodObjectsIndexedByFullName.Add(SignatureKeyService.GetFullMethodSignature(method), node);
                }
            }

        }

        private bool FindInterfaceMethod(MethodDefinition method, MethodObject methodNode)
        {
            // multi level inheritance is flattened here
            foreach (var interfaceParent in method.DeclaringType.Interfaces)
            {
                var matchingMethod = InterfaceMethodsIndexedByTypeName.Get(interfaceParent.InterfaceType.FullName)
                                        .FirstOrDefault(x => SignatureKeyService.GetMethodSignature(x).Equals(SignatureKeyService.GetMethodSignature(method)));
                if (matchingMethod != null)
                {
                    methodNode.InterfaceMethod = matchingMethod;
                    methodNode.ImplementsType = ImplementsType.Interface;

                    return true;
                }
                else // might be an interface with generics. We have indexed the generic form but now search using a generic instance (instead of T, a real type)
                {
                    TypeDefinition interfaceDefinition = null;
                    var resolved = ResolveService.TryResolve(interfaceParent.InterfaceType, out interfaceDefinition);
                    if (resolved && interfaceDefinition.GenericParameters.Any())
                        return ResolveGenericsInterface(method, methodNode, interfaceParent.InterfaceType, interfaceDefinition);
                }
            }

            return false;
        }

        private bool ResolveGenericsInterface(MethodDefinition method, MethodObject methodNode, TypeReference interfaceParent, TypeDefinition interfaceDefinition)
        {
            var genericTypes = TypeService.GetGenericTypeParameters(interfaceParent.FullName);

            if (interfaceDefinition.GenericParameters.Count != genericTypes.Count)
                return false;

            var genericTypesDict = new Dictionary<string, string>();
            for (int i = 0; i < interfaceDefinition.GenericParameters.Count; i++)
            {
                var genericPlaceholder = interfaceDefinition.GenericParameters[i].FullName;
                var typedGeneric = genericTypes[i];
                genericTypesDict.Add(genericPlaceholder, typedGeneric);
            }

            foreach (var m in interfaceDefinition.Methods)
            {
                foreach (var k in genericTypesDict.Keys)
                {
                    var typedMethodName = m.FullName.Replace(k, genericTypesDict[k]);
                    var interfaceMethodNamePart = typedMethodName.Substring(typedMethodName.IndexOf("::") + 2);
                    var concreteMethodNamePart = method.FullName.Substring(method.FullName.IndexOf("::") + 2);
                    if (interfaceMethodNamePart.Equals(concreteMethodNamePart))
                    {
                        // we replace a generic T type with a generic instance type and index that
                        var sig = SignatureKeyService.ConvertFullNameToSignature(typedMethodName);
                        MethodObjectsIndexedByFullName.Add(sig, methodNode);

                        methodNode.InterfaceMethod = m;
                        methodNode.ImplementsType = ImplementsType.Interface;

                        return true;
                    }
                }
            }

            return false;
        }

        private bool FindAbstractMethod(MethodDefinition method, MethodObject methodNode)
        {
            // TODO currently only looks at direct base type
            if (AbstractMethodsIndexedByTypeName.HasIndex(method.DeclaringType.BaseType.FullName))
            {
                var methodsOfAbstractParent = AbstractMethodsIndexedByTypeName.Get(method.DeclaringType.BaseType.FullName);
                var matchingMethod = methodsOfAbstractParent.FirstOrDefault(x => SignatureKeyService.GetMethodSignature(x).Equals(SignatureKeyService.GetMethodSignature(method)));
                if (matchingMethod != null)
                {
                    methodNode.AbstractMethod = matchingMethod;
                    methodNode.ImplementsType = ImplementsType.Abstract;

                    return true;
                }
            }

            return false;
        }

        private bool FindBaseClassMethod(MethodDefinition method, MethodObject methodNode)
        {
            // TODO currently only looks at direct base type
            if (ImplementationMethodsIndexedByTypeName.HasIndex(method.DeclaringType.BaseType.FullName))
            {
                var methodsOfBaseParent = ImplementationMethodsIndexedByTypeName.Get(method.DeclaringType.BaseType.FullName);
                var matchingMethod = methodsOfBaseParent.FirstOrDefault(x => SignatureKeyService.GetMethodSignature(x).Equals(SignatureKeyService.GetMethodSignature(method)));
                if (matchingMethod != null)
                {
                    methodNode.VirtualMethod = matchingMethod;
                    methodNode.ImplementsType = ImplementsType.Overrides;

                    return true;
                }
            }

            return false;
        }

        private void FindFieldsAndMethods(MethodDefinition method, MethodObject methodNode, bool allowRecursion)
        {
            if (!method.HasBody)
                return;

            List<Instruction> instructions = null;
            var isAsync = IsAsync(method);
            if (isAsync.Item1)
                instructions = GetInstructionsOfAsyncMethod(isAsync.Item2);
            else
                instructions = method.Body.Instructions.ToList();

            foreach (var instruction in instructions)
            {
                try
                {
                    if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Call
                    || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Calli
                    || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt
                    || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Newobj)
                    {

                        var methodRef = (MethodReference)instruction.Operand;
                        MethodDefinition methodDef = null;
                        var resolved = ResolveService.TryResolve(methodRef, out methodDef);
                        if (!resolved)
                        {
                            // log error
                            continue;
                        }

                        if (DelegateIndexer.IsDelegateInvocation(methodRef))
                        {
                            var concreteMethods = _delegateIndexer.GetAssignedMethods(methodRef);
                            foreach (var concreteMethod in concreteMethods)
                            {
                                var delegateCall = new MethodCall();
                                delegateCall.Instruction = concreteMethod.MethodAssignmentInstruction;
                                delegateCall.MethodCalled = concreteMethod.AssignedMethod;
                                delegateCall.OwnerMethod = method;
                                methodNode.MethodsCalled.Add(delegateCall);
                            }
                        }
                        else if (IsLazilyEvaluated(instruction, methodDef))
                        {
                            var nestedMethods = ExtractNestedMethods(methodDef.DeclaringType);
                            foreach (var nestedMethod in nestedMethods)
                            {
                                if (allowRecursion)
                                    FindFieldsAndMethods(nestedMethod, methodNode, false);
                            }
                        }
                        else if (!methodRef.FullName.Equals("System.Void System.Object::.ctor()"))
                        {
                            bool funcMethodCallsFound = false;

                            // try to detect funcs
                            if (methodRef.Parameters.Any())
                            {
                                var rootNode = new InstructionTreeNode();
                                MethodArgumentInstructionParser.GetParameterInstructionTree(method, methodRef, instruction, rootNode);

                                foreach (var loadFunctionNode in rootNode.GetDescendants().Where(x => x.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldftn))
                                {
                                    // except for delegates!
                                    var funcCall = new MethodCall();
                                    funcCall.Instruction = loadFunctionNode.Instruction;
                                    funcCall.MethodCalled = loadFunctionNode.Instruction.Operand as MethodReference;
                                    funcCall.OwnerMethod = method;
                                    methodNode.MethodsCalled.Add(funcCall);

                                    funcMethodCallsFound = true;
                                }
                            }

                            bool isCompilerServicesRelated = false;
                            if (isAsync.Item1 && (methodRef.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices") || methodRef.DeclaringType.FullName.StartsWith("System.Threading.Tasks")))
                                isCompilerServicesRelated = true;
                            
                            // if not a func and not an async related CompilerServices call then go ahead and register the call
                            if (!funcMethodCallsFound && !isCompilerServicesRelated)
                            {
                                var methodCall = new MethodCall();
                                methodCall.Instruction = instruction;
                                methodCall.MethodCalled = methodRef;
                                methodCall.OwnerMethod = method;
                                methodNode.MethodsCalled.Add(methodCall);
                            }
                        }
                    }
                    else if (instruction.OpCode.OperandType == OperandType.InlineField)
                    {
                        var field = (FieldReference)instruction.Operand;
                        if ((instruction.OpCode.Name.StartsWith("ldfld") || instruction.OpCode.Name.StartsWith("ldsfld")) && field.Name.IndexOf("BackingField") == -1)
                        {
                            methodNode.FieldsRead.Add(field);
                        }
                    }
                    else if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldftn)
                    {
                        var methodRef = (MethodReference)instruction.Operand;
                        MethodDefinition functionDefinition = null;
                        var resolved = ResolveService.TryResolve(methodRef, out functionDefinition);
                        if (!resolved)
                        {
                            // log error
                            continue;
                        }

                        if (allowRecursion)
                            FindFieldsAndMethods(functionDefinition, methodNode, false);
                    }
                }
                catch (Exception ex)
                {
                    // log it
                }
            }
        }

        private Tuple<bool, TypeDefinition> IsAsync(MethodDefinition method)
        {
            if (method.HasBody)
            {
                var firstInstr = method.Body.Instructions.First();
                if (firstInstr.OpCode.Code == Mono.Cecil.Cil.Code.Newobj && firstInstr.Operand != null)
                {
                    var methodRef = (MethodReference)firstInstr.Operand;
                    MethodDefinition methodDefinition = null;
                    var resolved = ResolveService.TryResolve(methodRef, out methodDefinition);
                    if (resolved)
                    {
                        if (methodDefinition.DeclaringType.Interfaces.Any(x => x.InterfaceType.FullName.Equals("System.Runtime.CompilerServices.IAsyncStateMachine")))
                        {
                            return Tuple.Create(true, methodDefinition.DeclaringType);
                        }
                    }
                }
            }

            return Tuple.Create<bool, TypeDefinition>(false, null);
        }

        private List<Instruction> GetInstructionsOfAsyncMethod(TypeDefinition asyncStateMachineType)
        {
            return asyncStateMachineType.Methods[1].Body.Instructions.ToList();
        }

        

        private bool IsLazilyEvaluated(Instruction instruction, MethodDefinition methodDef)
        {
            if (instruction.OpCode.Name.Equals("newobj"))
            {
                var typeDefinition = methodDef.DeclaringType;

                if (typeDefinition != null && typeDefinition.IsNestedPrivate
                    && (typeDefinition.Name.IndexOf("__") > -1))
                {
                    return true;
                }
            }

            return false;
        }

        private List<MethodDefinition> ExtractNestedMethods(TypeDefinition typeDefinition)
        {
            return typeDefinition.Methods.Where(x => x.HasBody).ToList();
        }

        //private IndexedList<MethodDefinition, Instruction> ExtractNestedInstructions(TypeDefinition typeDefinition)
        //{
        //    var instructions = new IndexedList<MethodDefinition, Instruction>();

        //    foreach (var nestedClassMethod in typeDefinition.Methods.Where(x => x.HasBody))
        //    {
        //        instructions.SetList(nestedClassMethod, nestedClassMethod.Body.Instructions.ToList());
        //    }

        //    return instructions;
        //}

        public bool HasMethod(string signatureKey)
        {
            return MethodObjectsIndexedByFullName.HasIndex(signatureKey);
        }

        public List<MethodObject> GetMethods(string signatureKey)
        {
            return MethodObjectsIndexedByFullName.Get(signatureKey);
        }

        public List<MethodObject> GetAllMethods()
        {
            return MethodObjectsList;
        }
    }
}
