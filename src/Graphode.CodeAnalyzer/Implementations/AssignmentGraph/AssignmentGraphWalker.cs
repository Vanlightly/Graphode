using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.Common;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class AssignmentGraphWalker : IAssignmentGraphWalker
    {
        private Stack<string> _instructionKeyStack;
        private readonly ITripleStore _tripleStore;
        
        private List<GoToInstancePattern> _goToInstancePatterns;
        private List<string> _searchBaseConstructorPatterns;

        public AssignmentGraphWalker(ITripleStore tripleStore)
        {
            _tripleStore = tripleStore;
            CreatePrintFile();
        }

        public IList<Triple> GetDirectAssignmentsFrom(HashSet<string> targetObjectKeys)
        {
            var triples = new List<Triple>();
            foreach(var key in targetObjectKeys)
                triples.AddRange(_tripleStore.GetFrom(key));

            return triples;
        }

        public IList<Triple> GetDirectAssignmentsTo(HashSet<string> targetObjectKeys)
        {
            var triples = new List<Triple>();
            foreach (var key in targetObjectKeys)
                triples.AddRange(_tripleStore.GetTo(key));

            return triples;
        }

        public IList<AssignmentTree> PerformBacktrackingSearch(string startingObjectKey,
            ISourceDetector nameSourceDetector,
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns)
        {
            _goToInstancePatterns = permittedToGoInstancePatterns;
            _searchBaseConstructorPatterns = searchBaseConstructorPatterns;

            var assignmentTrees = new List<AssignmentTree>();
            CreateStartTraceSection(startingObjectKey);

            var startingKeys = _tripleStore.GetTo(startingObjectKey);

            foreach (var start in startingKeys)
            {
                Print(startingObjectKey, 0);

                var exploreTreeNode = new ExploreTreeNode() { FullSignature = start.ToString() };
                var rootNode = new AssignmentTreeNode(start);
                var assignmentTree = new AssignmentTree();
                assignmentTree.RootNode = rootNode;
                var counter = new BacktrackCounter();
                
                _instructionKeyStack = new Stack<string>();
                BacktrackingSearch(start, start, nameSourceDetector, counter, assignmentTree, rootNode, false, exploreTreeNode, 0);

                assignmentTrees.Add(assignmentTree);
            }

            return assignmentTrees;
        }

        public IList<AssignmentTree> PerformBacktrackingSearch(Instruction instruction, MethodDefinition methodOfInstruction,
            ISourceDetector nameSourceDetector,
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns)
        {
            _goToInstancePatterns = permittedToGoInstancePatterns;
            _searchBaseConstructorPatterns = searchBaseConstructorPatterns;

            var assignmentTrees = new List<AssignmentTree>();
            CreateStartTraceSection(methodOfInstruction.FullName + " : " + instruction);

            var instructionKey = InstructionKeyService.GetInstructionKey(instruction, methodOfInstruction);
            var startingKeys = _tripleStore.GetToViaInstructionKey(instructionKey);
            
            foreach (var start in startingKeys)
            {
                Print(instructionKey, 0);

                var exploreTreeNode = new ExploreTreeNode() { FullSignature = start.ToString() };
                var rootNode = new AssignmentTreeNode(start);
                var assignmentTree = new AssignmentTree();
                assignmentTree.RootNode = rootNode;
                var counter = new BacktrackCounter();

                _instructionKeyStack = new Stack<string>();
                BacktrackingSearch(start, start, nameSourceDetector, counter, assignmentTree, rootNode, false, exploreTreeNode, 0);

                assignmentTrees.Add(assignmentTree);
            }



            return assignmentTrees;
        }

        private BacktrackResult BacktrackingSearch(Triple startTriple, 
            Triple triple,
            ISourceDetector nameSourceDetector,
            BacktrackCounter counter, 
            AssignmentTree assignmentTree, 
            AssignmentTreeNode lastNode, 
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            depth++;
            counter.Count++;
            Print("BACKTRACKED TO " + triple, depth);
            
            if (IsRecursiveLoop(exploreTreeNode))
            {
                Print("RECURSIVE LOOP", depth);
                return BacktrackResult.BranchFailure;
            }

            if(counter.Count > 150)
            {
                Print("Limit Reached", depth);
                return BacktrackResult.Stop;
            }

            if (IsAssignedByMethod(triple))
            {
                var canStepInto = _tripleStore.Back(triple).Any();
                var methodRef = (MethodReference)triple.From.Instruction.Operand;
                if (methodRef.HasParameters && canStepInto)
                {
                    PushInstructionKeyToStack(triple, depth);
                    inReturnScope = true;
                }
            }

            if (nameSourceDetector.IsNameSource(triple))
            {
                AddTargetToAssignmentTree(triple, assignmentTree, lastNode, depth);
                return BacktrackResult.Success;
            }

            if (IsReturnScopeArgument(triple, ref inReturnScope))
            {
                var branchResult = BacktrackingSearchFromInstructionKeyStack(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                if (branchResult != BacktrackResult.BranchFailure)
                    return branchResult;
            }
            else if (ShouldGoToBaseConstructor(triple))
            {
                var branchResult = GoToBaseConstructor(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                if (branchResult != BacktrackResult.BranchFailure)
                    return branchResult;
            }
            else if(ShouldOnlyGoToInstance(triple))
            {
                var branchResult = GoToInstance(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                if (branchResult != BacktrackResult.BranchFailure)
                    return branchResult;
            }
            else
            {
                if(FromIsRelevantObjectInitializerConstructor(triple))
                {
                    Print("Is Relevant Object Initializer Constructor" + triple, depth);
                    var objectInitializerMemberTriples = _tripleStore.GetToViaContructorInstructionKey(triple.From.InstructionKey);
                    if(objectInitializerMemberTriples.Any())
                    {
                        Print("Found " + objectInitializerMemberTriples.Count + " Object Initializer Members " + triple, depth);
                        var branchResult = BacktrackingSearchMatches(startTriple, triple, objectInitializerMemberTriples, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, true, exploreTreeNode, depth);
                        if (branchResult != BacktrackResult.BranchFailure)
                            return branchResult;
                    }
                }

                var goneToInstance = false;
                if (ShouldGoToInstanceFirst(triple))
                {
                    goneToInstance = true;
                    var branchResult = GoToInstance(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                    if (branchResult != BacktrackResult.BranchFailure)
                        return branchResult;
                }

                Print("Check for direct triple to -> from match" + triple, depth);
                var backTriples = _tripleStore.Back(triple);
                if (backTriples.Any())
                {
                    var branchResult = BacktrackingSearchMatches(startTriple, triple, backTriples, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, true, exploreTreeNode, depth);
                    if (branchResult != BacktrackResult.BranchFailure)
                        return branchResult;
                }

                if (triple.From.ObjectType == ObjectType.Method)
                {
                    Print("Step over method to argument " + triple, depth);
                    PushInstructionKeyToStack(triple, depth);
                    var branchResult = BacktrackingSearchFromInstructionKeyStack(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                    if (branchResult != BacktrackResult.BranchFailure)
                        return branchResult;
                }

                if (CanGoToInstance(triple) && !goneToInstance)
                {
                    var branchResult = GoToInstance(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
                    if (branchResult != BacktrackResult.BranchFailure)
                        return branchResult;
                }
            }

            Print("DEADEND", depth);

            return BacktrackResult.BranchFailure;
        }

        private bool IsRecursiveLoop(ExploreTreeNode exploreTreeNode)
        {
            ExploreTreeNode parent = exploreTreeNode.Parent;
            while (parent != null)
            {
                if (parent.FullSignature.Equals(exploreTreeNode.FullSignature))
                    return true;

                parent = parent.Parent;
            }

            return false;
        }

        private ExploreTreeNode CreateExploreTreeNode(Triple triple, string searchType, ExploreTreeNode exploreTreeNode)
        {
            var childExploreTreeNode = new ExploreTreeNode()
            {
                FullSignature = string.Format("{0}:{1}", searchType, triple.From.InstructionKey)
            };

            exploreTreeNode.AddChild(childExploreTreeNode);

            return childExploreTreeNode;
        }

        private bool IsAssignedByMethod(Triple triple)
        {
            return triple.From.ObjectType == ObjectType.Method;
        }

        private void PushInstructionKeyToStack(Triple triple, int depth)
        {
            Print("Push " + triple.From.InstructionKey + " to instruction stack " + triple, depth);
            _instructionKeyStack.Push(triple.From.InstructionKey);
        }

        private void AddTargetToAssignmentTree(Triple triple, AssignmentTree assignmentTree, AssignmentTreeNode lastNode, int depth)
        {
            Print("FOUND " + triple.From.ObjectKey, depth);
            var foundAssignmentTreeNode = new AssignmentTreeNode(triple, true);
            lastNode.Children.Add(foundAssignmentTreeNode);
            assignmentTree.FoundNodes.Add(foundAssignmentTreeNode);
        }
        
        private bool IsReturnScopeArgument(Triple triple, ref bool inReturnScope)
        {
            var provisionalInReturnScope = !IsFromProperty(triple) && triple.From.ObjectType == ObjectType.Argument && inReturnScope;
            if(provisionalInReturnScope)
            {
                if(!_instructionKeyStack.Any())
                {
                    inReturnScope = false;
                    return false;
                }
            }

            return provisionalInReturnScope;
        }

        private bool IsFromProperty(Triple triple)
        {
            return triple.From.MemberName.IndexOf("get_") > -1
                || triple.From.MemberName.IndexOf("set_") > -1;
        }

        private bool ShouldGoToBaseConstructor(Triple triple)
        {
            foreach (var pattern in _searchBaseConstructorPatterns)
            {
                if (triple.To.InheritsFromConcreteClass != null && triple.To.InheritsFromConcreteClass.IndexOf(pattern) > -1)
                {
                    return true;
                }
            }

            return false;
        }

        private BacktrackResult GoToBaseConstructor(Triple startTriple,
            Triple triple,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree,
            AssignmentTreeNode lastNode,
            BacktrackCounter counter,
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            var baseClass = triple.To.InheritsFromConcreteClass;
            var baseClassCtors = _tripleStore.GetAllTriples().Where(x => x.To.ObjectKey.IndexOf(baseClass) > -1
                        && x.To.ObjectKey.IndexOf(".ctor") > -1);

            foreach (var baseClassCtorTriple in baseClassCtors)
            {
                if (baseClassCtorTriple.To.InstanceOwnerKey.Equals(triple.To.MemberName))
                {
                    var childExploreTreeNode = CreateExploreTreeNode(baseClassCtorTriple, "ViaBaseConstructor", exploreTreeNode);
                    var branchResult = BacktrackingSearchMatches(startTriple, triple, new List<Triple>() { baseClassCtorTriple }, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, true, childExploreTreeNode, depth);
                    if (branchResult != BacktrackResult.BranchFailure)
                        return branchResult;
                }
            }

            return BacktrackResult.BranchFailure;
        }

        private BacktrackResult BacktrackingSearchFromInstructionKeyStack(Triple startTriple,
            Triple triple,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree,
            AssignmentTreeNode lastNode,
            BacktrackCounter counter,
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            var result = BacktrackResult.BranchFailure;

            if(_instructionKeyStack.Count == 0)
            {
                // TODO log error
                Print("Tried to pop empty stack " + triple, depth);
                return BacktrackResult.Stop;
            }

            var instructionKey = _instructionKeyStack.Pop();
            Print("Pop from instruction stack " + instructionKey, depth);
            var matchingTriples = _tripleStore.GetToViaInstructionKey(instructionKey);

            List<Triple> targetTriples = null;
            if (triple.From.ObjectKey.EndsWith("return"))
            {
                targetTriples = matchingTriples;
            }
            else
            {
                var targetPosition = triple.From.ObjectKey.Substring(triple.From.ObjectKey.IndexOf(">>") + 2);
                targetTriples = matchingTriples.Where(x => x.To.ObjectKey.EndsWith(targetPosition)).ToList();
            }

            if (targetTriples.Any())
            {
                var branchResult = BacktrackingSearchMatches(startTriple, triple, targetTriples, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, true, exploreTreeNode, depth);
                if (branchResult == BacktrackResult.Success)
                    result = BacktrackResult.Success;
                else if (branchResult == BacktrackResult.Stop)
                    return branchResult;
            }

            return result;
        }

        private BacktrackResult BacktrackingSearchMatches(Triple startTriple,
            Triple triple, 
            List<Triple> targetMatchingTriples,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree, 
            AssignmentTreeNode lastNode, 
            BacktrackCounter counter, 
            bool inReturnScope,
            bool printObjectKey,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            var result = BacktrackResult.BranchFailure;

            int matchCounter = 0;
            foreach (var backTriple in targetMatchingTriples)
            {
                matchCounter++;
                ExploreTreeNode childExploreTreeNode = null;

                if (printObjectKey)
                {
                    childExploreTreeNode = CreateExploreTreeNode(backTriple, "ObjectKey", exploreTreeNode);
                    Print(matchCounter + " matched object key " + backTriple, depth);
                }
                else
                {
                    childExploreTreeNode = CreateExploreTreeNode(backTriple, "InstructionKey", exploreTreeNode);
                    Print(matchCounter + " MATCH INSTRUCTION KEY " + triple.From.InstructionKey, depth);
                }

                var assignmentNode = new AssignmentTreeNode(backTriple);
                var branchResult = BacktrackingSearch(startTriple, backTriple, nameSourceDetector, counter, assignmentTree, assignmentNode, inReturnScope, childExploreTreeNode, depth);
                if (branchResult == BacktrackResult.Success)
                {
                    lastNode.Children.Add(assignmentNode);
                    result = BacktrackResult.Success;
                }
                else if(branchResult == BacktrackResult.Stop)
                {
                    return branchResult;
                }
            }

            return result;
        }

        private bool CanGoToInstance(Triple triple)
        {
            bool patternMatched = false;

            foreach(var pattern in _goToInstancePatterns)
            {
                if (pattern.IsMatch(triple.To.ObjectKeyType, triple.To.ObjectKeyMember))
                    patternMatched = true;
            }

            return patternMatched && !string.IsNullOrEmpty(triple.To.InstanceOwnerKey);

            //return (triple.To.ObjectKey.StartsWith("System.Data")
            //    || triple.To.ObjectKey.StartsWith("TestAssembly.Set1.SendingTransport"))
            //    && !string.IsNullOrEmpty(triple.To.InstanceOwnerKey);
        }

        private bool ShouldGoToInstanceFirst(Triple triple)
        {
            bool patternMatched = false;

            foreach (var pattern in _goToInstancePatterns)
            {
                if (pattern.IsMatch(triple.To.ObjectKeyType, triple.To.ObjectKeyMember) && pattern.TryInstance == TryInstance.First)
                    patternMatched = true;
            }

            return patternMatched && !string.IsNullOrEmpty(triple.To.InstanceOwnerKey);
        }

        private bool ShouldOnlyGoToInstance(Triple triple)
        {
            bool patternMatched = false;

            foreach (var pattern in _goToInstancePatterns)
            {
                if (pattern.IsMatch(triple.To.ObjectKeyType, triple.To.ObjectKeyMember) && pattern.TryInstance == TryInstance.Only)
                    patternMatched = true;
            }

            return patternMatched && !string.IsNullOrEmpty(triple.To.InstanceOwnerKey);
        }

        private bool IsObjectInitializerContructor(Triple triple)
        {
            return triple.SetsObjectInitializerMember;
        }

        private bool FromIsRelevantObjectInitializerConstructor(Triple triple)
        {
            if (triple.From.ObjectType == ObjectType.None)
                return false;

            //var isObjectInitializerCtor = IsObjectInitializerContructor(triple);
            //if (!isObjectInitializerCtor)
            //    return false;

            bool patternMatched = false;

            foreach (var pattern in _goToInstancePatterns)
            {
                if (pattern.IsObjectInitializerMatch(triple.From.ObjectKeyType, triple.From.ObjectKeyMember))
                    patternMatched = true;
            }

            if (!patternMatched)
                return false;

            var objectInitializerTriples = _tripleStore.GetToViaContructorInstructionKey(triple.From.InstructionKey);
            return objectInitializerTriples.Any();
        }

        private BacktrackResult GoToInstance(Triple startTriple,
            Triple triple,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree,
            AssignmentTreeNode lastNode,
            BacktrackCounter counter,
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            var branchResult = BacktrackingSearchFromInstance(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
            if (branchResult != BacktrackResult.BranchFailure)
                return branchResult;

            branchResult = BacktrackingSearchFromInstanceProperty(startTriple, triple, nameSourceDetector, assignmentTree, lastNode, counter, inReturnScope, exploreTreeNode, depth);
            if (branchResult != BacktrackResult.BranchFailure)
                return branchResult;

            return BacktrackResult.BranchFailure;
        }

        private BacktrackResult BacktrackingSearchFromInstance(Triple startTriple,
            Triple triple,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree,
            AssignmentTreeNode lastNode,
            BacktrackCounter counter,
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            Print("Check for instance assignments", depth);
            var result = BacktrackResult.BranchFailure;
            if (triple.To.InstanceOwnerKey == null)
                return result;

            List<Triple> instanceAssignments = _tripleStore.GetTo(triple.To.InstanceOwnerKey);
            Print(instanceAssignments.Count + " Instance Assignments", depth);

            if (instanceAssignments.Any())
            {
                int instanceCounter = 0;
                foreach (var instanceAssignment in instanceAssignments)
                {
                    instanceCounter++;
                    var childExploreTreeNode = CreateExploreTreeNode(instanceAssignment, "InstanceAssignment", exploreTreeNode);
                    var assignmentNode = new AssignmentTreeNode(instanceAssignment);
                    assignmentNode.IsRegressionToInstance = true;
                    Print("Gone to instance assignment " + instanceCounter + ": " + instanceAssignment, depth);
                    var branchResult = BacktrackingSearch(startTriple, instanceAssignment, nameSourceDetector, counter, assignmentTree, assignmentNode, inReturnScope, childExploreTreeNode, depth);
                    if (branchResult == BacktrackResult.Success)
                    {
                        lastNode.Children.Add(assignmentNode);
                        result = BacktrackResult.Success;
                    }
                    else if (branchResult == BacktrackResult.Stop)
                    {
                        return branchResult;
                    }
                }
            }

            return result;
        }

        private BacktrackResult BacktrackingSearchFromInstanceProperty(Triple startTriple,
            Triple triple,
            ISourceDetector nameSourceDetector,
            AssignmentTree assignmentTree,
            AssignmentTreeNode lastNode,
            BacktrackCounter counter,
            bool inReturnScope,
            ExploreTreeNode exploreTreeNode,
            int depth)
        {
            Print("Check for instance property assignments", depth);
            var result = BacktrackResult.BranchFailure;

            var instanceMemberAssignments = _tripleStore.GetToViaInstanceOwnerKey(triple.To.InstanceOwnerKey);
            if (instanceMemberAssignments.Any())
            {
                if (instanceMemberAssignments.Count() > 1 && instanceMemberAssignments.Any(x => x.ParentAssembly.Equals(triple.ParentAssembly)))
                    instanceMemberAssignments = instanceMemberAssignments.Where(x => x.ParentAssembly.Equals(triple.ParentAssembly)).ToList();

                foreach (var instanceAssignment in instanceMemberAssignments)
                {
                    if (instanceAssignment.To.ObjectKey.Equals(startTriple.To.ObjectKey))
                        continue;

                    var childExploreTreeNode = CreateExploreTreeNode(instanceAssignment, "InstanceMemberAssignment", exploreTreeNode);
                    var assignmentNode = new AssignmentTreeNode(instanceAssignment);
                    assignmentNode.IsRegressionToInstance = true;
                    Print("Gone to instance property assignment " + instanceAssignment, depth);
                    var branchResult = BacktrackingSearch(startTriple, instanceAssignment, nameSourceDetector, counter, assignmentTree, assignmentNode, inReturnScope, childExploreTreeNode, depth);
                    if (branchResult == BacktrackResult.Success)
                    {
                        lastNode.Children.Add(assignmentNode);
                        result = BacktrackResult.Success;
                    }
                    else if (branchResult == BacktrackResult.Stop)
                    {
                        return branchResult;
                    }
                }
            }

            return result;
        }

        private void CreatePrintFile()
        {
            //File.WriteAllText("C:\\ApplicationGraph\\tree.txt", "");
        }

        private void CreateStartTraceSection(string message)
        {
            //File.AppendAllText("C:\\ApplicationGraph\\tree.txt", Environment.NewLine);
            //File.AppendAllText("C:\\ApplicationGraph\\tree.txt", "@@@@@@@@@@@ Starting " + message + Environment.NewLine);
        }

        private void Print(string message, int depth)
        {
            //string spaces = string.Empty;

            //for (int i = 0; i < depth; i++)
            //    spaces += "-";

            //var line = string.Format("{0}{1}", spaces, message);

            //File.AppendAllLines("C:\\ApplicationGraph\\tree.txt", new List<string>() { line });
        } 

    }
}
