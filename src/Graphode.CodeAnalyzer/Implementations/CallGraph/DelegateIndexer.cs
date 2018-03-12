using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.CallGraph;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Contracts.Common;

namespace Graphode.CodeAnalyzer.Implementations.CallGraph
{
    // singleinstance!!
    public class DelegateIndexer : IDelegateIndexer
    {
        private readonly IAssignmentGraphWalker _assignmentGraphWalker;
        
        private IndexedList<string, IndexedDelegate> _delegateMethods;

        public DelegateIndexer(IAssignmentGraphWalker assignmentGraphWalker)
        {
            _assignmentGraphWalker = assignmentGraphWalker;

            CleanIndexes();
        }

        public void CleanIndexes()
        {
            _delegateMethods = new IndexedList<string, IndexedDelegate>();
        }

        public void Load(List<ModuleDefinition> modules)
        {
            foreach (var module in modules)
                Load(module);
        }

        private void Load(ModuleDefinition module)
        {
            var concreteMethods = DecompilerService.GetConcreteMethods(module);

            foreach (var method in concreteMethods)
            {
                if (TypeService.ShouldSkipDelegateIndexing(method.DeclaringType))
                    continue;

                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineMethod)
                    {
                        var calledMethod = (MethodReference)instruction.Operand;
                        if (IsDelegateInvocation(calledMethod))
                        {
                            IndexDelegateInvocation(instruction, calledMethod, method);
                        }
                    }
                }
            }
        }

        public List<IndexedDelegate> GetAssignedMethods(MethodDefinition parentMethod, MethodReference delegateInvocation)
        {
            string key = GetDelegateKey(parentMethod, delegateInvocation);
            return _delegateMethods.Get(key);
        }

        private void IndexDelegateInvocation(Instruction instruction, MethodReference invokedDelegate, MethodDefinition parentMethod)
        {
            //var instructionKey = InstructionKeyService.GetInstructionKey(instruction, parentMethod);
            var targetKey = invokedDelegate.DeclaringType.GetKey() + "::.ctor(System.Object,System.IntPtr)>>1";

            var assignmentTrees = _assignmentGraphWalker.PerformBacktrackingSearch(instruction,
                parentMethod,
                new DefaultSourceDetector(new HashSet<string>() { targetKey }),
                new List<GoToInstancePattern>() {
                    new GoToInstancePattern()
                    {
                        TypeMustHavePattern = invokedDelegate.DeclaringType.FullName,
                        MemberMustHavePattern = "Invoke",
                        TryInstance = TryInstance.Only
                    }
                },
                new List<string>());

            foreach (var result in assignmentTrees)
            {
                foreach (var node in result.FoundNodes)
                {
                    if (node.Triple.From.Instruction.OpCode.Name == "ldvirtftn")
                    {
                        var methodRef = (MethodReference)node.Triple.From.Instruction.Operand;
                        MethodDefinition assignedMethod = null;
                        var resolved = ResolveService.TryResolve(methodRef, out assignedMethod);
                        if (resolved)
                        {
                            var indexedDelegate = new IndexedDelegate();
                            indexedDelegate.AssignedMethod = assignedMethod;
                            indexedDelegate.MethodAssignmentInstruction = node.Triple.From.Instruction;
                            string key = GetDelegateKey(parentMethod, invokedDelegate);
                            _delegateMethods.Add(key, indexedDelegate);
                        }
                    }
                }
            }
        }

        public static string GetDelegateKey(MethodDefinition parentMethod, MethodReference invokedDelegate)
        {
            return parentMethod.FullName + ":" + parentMethod.DeclaringType.FullName + ":" + invokedDelegate.FullName;
        }


        public static bool IsDelegateInvocation(MethodReference calledMethod)
        {
            if (calledMethod.FullName.IndexOf("__") > -1) // is a compiler generated class
                return false;

            TypeDefinition declaringType = null;
            var resolved = ResolveService.TryResolveMethodDeclaringType(calledMethod, out declaringType);
            if (resolved)
            {
                return declaringType != null
                    && declaringType.BaseType != null
                    && declaringType.BaseType.FullName.Equals("System.MulticastDelegate")
                    && calledMethod.Name.Equals("Invoke");
            }

            return false;
        }
    }
}
