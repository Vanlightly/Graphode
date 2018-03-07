using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph.InstructionScopes;
using Graphode.CodeAnalyzer.Exceptions;
using Graphode.CodeAnalyzer.Logging;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class InstructionParser : IInstructionParser
    {
        private readonly ILogOutput _logOutput;

        public InstructionParser(ILogOutput logOutput)
        {
            _logOutput = logOutput;
        }

        public List<Triple> ParseInstructions(MethodDefinition methodDefinition)
        {
            var triples = new List<Triple>();

            if (methodDefinition.HasBody)
            {
                var mainScope = InstructionScopeParser.BuildMainScope(methodDefinition);
                ParseMainScope(mainScope, methodDefinition, triples);

                foreach (var scope in mainScope.ObjectInitializerScopes)
                    ParseObjectInitializerScope(scope, methodDefinition, triples);
            }

            return triples;
        }

        private void ParseMainScope(MainScope mainScope, MethodDefinition methodDefinition, List<Triple> triples)
        {
            foreach (var instruction in mainScope.Instructions)
            {
                ValidateInstruction(instruction);
                
                if (IsToTrigger(instruction))
                {
                    try
                    {
                        ParseInstruction(instruction, methodDefinition, triples);
                    }
                    catch (ILParseException ex)
                    {
                        _logOutput.LogAnalysis("ERROR " + ex.Message);
                    }
                }
            }
        }

        private void ParseObjectInitializerScope(ObjectInitializerScope objectInitializerScope, MethodDefinition methodDefinition, List<Triple> triples)
        {
            foreach (var memberScope in objectInitializerScope.MemberScopes)
                ParseMemberScope(memberScope, methodDefinition, triples);
        }

        private void ParseMemberScope(MemberScope memberScope, MethodDefinition methodDefinition, List<Triple> triples)
        {
            if (memberScope.HasNestedScope)
                ParseObjectInitializerScope(memberScope.NestedScope, methodDefinition, triples);

            foreach (var instruction in memberScope.Instructions)
            {
                ValidateInstruction(instruction);

                if (IsToTrigger(instruction))
                {
                    try
                    {
                        if (instruction.Next != null && instruction.Next.OpCode.Code == Mono.Cecil.Cil.Code.Nop
                            && instruction.Next.Next == null)
                        {
                            ParseInstruction(instruction, methodDefinition, triples, memberScope.Parent.ConstructorInstructionKey);
                        }
                        else
                        {
                            ParseInstruction(instruction, methodDefinition, triples);
                        }
                    }
                    catch (ILParseException ex)
                    {
                        _logOutput.LogAnalysis("ERROR " + ex.Message);
                    }
                }
            }
        }

        private void ValidateInstruction(Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var methodRef = (MethodReference)instruction.Operand;
                MethodDefinition methodDef = null;
                var resolved = ResolveService.TryResolve(methodRef, out methodDef);
                if (!resolved)
                    throw new ILParseException("Could not resolve " + methodRef.FullName);
            }
        }

        private bool IsValidateInstruction(Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var methodRef = (MethodReference)instruction.Operand;
                MethodDefinition methodDef = null;
                var resolved = ResolveService.TryResolve(methodRef, out methodDef);
                return resolved;
            }

            return true;
        }

        private bool IsToTrigger(Instruction instruction)
        {
            if (instruction.OpCode.Name.StartsWith("stfld"))
                return true;

            if (instruction.OpCode.Name.StartsWith("stsfld"))
                return true;

            if (instruction.OpCode.Name.StartsWith("stloc"))
                return true;

            if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ret)
                return true;

            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                // if it is a delegate or lambda that calls a method then it loads the method onto the stack for later execution
                // we ignore the loading onto the stack as a trigger
                if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldftn)
                    return false;

                var methodReference = (MethodReference)instruction.Operand;
                if (methodReference.FullName.IndexOf("System.Reflection") > -1)
                    return false;

                return true;
            }

            return false;
        }

        private void ParseInstruction(Instruction instruction, MethodDefinition parentMethod, List<Triple> triples, string constructorInstructionKey = null)
        {
            var toObjectType = InstructionKeyService.GetToObjectType(instruction);
            if (!InstructionKeyService.IsSupportedObject(toObjectType))
                return;

            if (toObjectType == ObjectType.Argument)
            {
                var argumentTriples = GetMethodCallArgumentTriples(parentMethod, instruction, constructorInstructionKey);
                foreach (var argumentTriple in argumentTriples)
                    triples.Add(argumentTriple);
            }
            else if (toObjectType == ObjectType.NilArgument)
            {
                var triple = GetNilArgumentTriple(instruction, parentMethod);
                if (triple != null)
                    triples.Add(triple);
            }
            else if (toObjectType == ObjectType.ReturnValue && parentMethod.FullName.StartsWith("System.Void"))
            { }
            else
            {
                var triple = GetGenericTriple(instruction, parentMethod, toObjectType);
                if (triple != null)
                    triples.Add(triple);
            }
        }

        private List<Triple> GetMethodCallArgumentTriples(MethodDefinition parentMethod, Instruction instruction, string constructorInstructionKey = null)
        {
            var triples = new List<Triple>();
            var calledMethod = (MethodReference)instruction.Operand;
            var rootNode = GetInstructionTree(parentMethod, instruction, calledMethod);

            if (rootNode == null)
                return triples;

            string instanceOwnerKey = string.Empty;
            if (constructorInstructionKey != null)
            {
                instanceOwnerKey = null;
                triples = TraverseTree(parentMethod, rootNode, instanceOwnerKey, true, constructorInstructionKey);
            }
            else
            {
                instanceOwnerKey = InstructionKeyService.GetToInstanceOwnerKey(instruction, ObjectType.Method, parentMethod);
                triples = TraverseTree(parentMethod, rootNode, instanceOwnerKey, false);
            }

            return triples;
        }

        private InstructionTreeNode GetInstructionTree(MethodDefinition parentMethod, Instruction instruction, MethodReference calledMethod)
        {
            try
            {
                var rootNode = new InstructionTreeNode();
                MethodArgumentInstructionParser.GetParameterInstructionTree(parentMethod, calledMethod, instruction, rootNode);
                return rootNode;
            }
            catch (Exception ex)
            {
                _logOutput.LogAnalysis("ERROR " + parentMethod.FullName + " instruction" + instruction.ToString() + " Error-> " + ex);
                return null;
            }
        }

        private List<Triple> TraverseTree(MethodDefinition parentMethod,
            InstructionTreeNode parentNode,
            string instanceOwnerKey,
            bool isObjectInitializerMember,
            string constructorInstructionKey = null)
        {
            var triples = new List<Triple>();

            var toArgumentCounter = parentNode.ChildInstructions.Count;
            foreach (var argumentInstruction in parentNode.ChildInstructions)
            {
                var fromObjectType = InstructionKeyService.GetFromObjectType(argumentInstruction.Instruction);
                var fromInstanceOwnerKey = InstructionKeyService.GetFromInstanceOwnerKey(argumentInstruction.Instruction, fromObjectType, parentMethod);

                var triple = BuildTriple(parentNode.Instruction, instanceOwnerKey, argumentInstruction.Instruction, fromInstanceOwnerKey, parentMethod, toArgumentCounter);
                if (triple == null)
                    continue;

                if (isObjectInitializerMember)
                {
                    triple.SetsObjectInitializerMember = true;
                    triple.ConstructorInstructionKey = constructorInstructionKey;
                }

                triples.Add(triple);
                toArgumentCounter--;

                if (argumentInstruction.ChildInstructions.Any())
                {
                    var subTriples = TraverseTree(parentMethod, argumentInstruction, triple.From.InstanceOwnerKey, false);
                    triples.AddRange(subTriples);
                }
            }

            return triples;
        }

        private Triple GetNilArgumentTriple(Instruction instruction, MethodDefinition parentMethod, string objectInitializerInstanceKey = null)
        {
            var parseResult = new ParseResult();
            var nilArgumentTriple = new Triple();
            nilArgumentTriple.ParentAssembly = parentMethod.Module.Assembly.Name.Name;
            nilArgumentTriple.ParentMethod = parentMethod;

            string toInstanceOwnerKey = string.Empty;
            if (objectInitializerInstanceKey != null)
                toInstanceOwnerKey = objectInitializerInstanceKey;
            //else if (MethodArgumentInstructionParser.IsChainedToPreviousInstruction(instruction))
            //    toInstanceOwnerKey = null;
            else
                toInstanceOwnerKey = InstructionKeyService.GetToInstanceOwnerKey(instruction, ObjectType.NilArgument, parentMethod);

            nilArgumentTriple.To = BuildToTriple(instruction, toInstanceOwnerKey, parentMethod);
            nilArgumentTriple.To.ObjectType = ObjectType.NilArgument;
            nilArgumentTriple.From = new TripleObject();
            nilArgumentTriple.From.ObjectType = ObjectType.None;
            return nilArgumentTriple;
        }

        private Triple GetGenericTriple(Instruction instruction,
            MethodDefinition parentMethod,
            ObjectType toObjectType,
            string constructorInstructionKey = null)
        {
            var fromInstruction = instruction.Previous;
            var fromObjectType = InstructionKeyService.GetFromObjectType(fromInstruction);
            var fromInstanceOwnerKey = InstructionKeyService.GetFromInstanceOwnerKey(fromInstruction, fromObjectType, parentMethod);

            string toInstanceOwnerKey = string.Empty;
            if (constructorInstructionKey != null)
                toInstanceOwnerKey = null;
            else
                toInstanceOwnerKey = InstructionKeyService.GetToInstanceOwnerKey(instruction, toObjectType, parentMethod);
            
            var triple = BuildTriple(instruction, toInstanceOwnerKey, fromInstruction, fromInstanceOwnerKey, parentMethod);
            if (constructorInstructionKey != null)
            {
                triple.SetsObjectInitializerMember = true;
                triple.ConstructorInstructionKey = constructorInstructionKey;
            }

            return triple;
        }

        private Triple BuildTriple(Instruction toInstruction,
            string toInstanceOwnerKey,
            Instruction fromInstruction,
            string fromInstanceOwnerKey,
            MethodDefinition parentMethod,
            int? toArgumentCounter = null)
        {
            if (!IsSupportedToObject(toInstruction))
                return null;

            if (!IsSupportedFromObject(fromInstruction))
                return null;

            var triple = new Triple();
            triple.ParentAssembly = parentMethod.Module.Assembly.Name.Name;
            triple.ParentMethod = parentMethod;
            triple.To = BuildToTriple(toInstruction, toInstanceOwnerKey, parentMethod, toArgumentCounter);
            triple.From = BuildFromTriple(fromInstruction, fromInstanceOwnerKey, parentMethod);

            return triple;
        }

        private bool IsSupportedToObject(Instruction instruction)
        {
            var toObjectType = InstructionKeyService.GetToObjectType(instruction);
            if (!InstructionKeyService.IsSupportedObject(toObjectType))
                return false;

            return true;
        }

        private bool IsSupportedFromObject(Instruction instruction)
        {
            var fromObjectType = InstructionKeyService.GetFromObjectType(instruction);
            if (!InstructionKeyService.IsSupportedObject(fromObjectType))
                return false;

            return true;
        }

        private TripleObject BuildToTriple(Instruction toInstruction,
            string toInstanceOwnerKey,
            MethodDefinition parentMethod,
            int? toArgumentCounter = null)
        {
            var tripleObj = new TripleObject();
            tripleObj.Instruction = toInstruction;

            var toObjectType = InstructionKeyService.GetToObjectType(toInstruction);
            tripleObj.ObjectType = toObjectType;
            tripleObj.InstanceOwnerKey = toInstanceOwnerKey;

            if (toObjectType == ObjectType.Argument)
            {
                var methodCallReference = (MethodReference)toInstruction.Operand;
                tripleObj.ObjectKey = InstructionKeyService.GetObjectKey(toInstruction, toObjectType, methodCallReference, toArgumentCounter.Value);
            }
            else
            {
                tripleObj.ObjectKey = InstructionKeyService.GetObjectKey(toInstruction, toObjectType, parentMethod);
            }

            tripleObj.InstructionKey = InstructionKeyService.GetInstructionKey(toInstruction, parentMethod);
            tripleObj.OwnerTypeCategory = InstructionKeyService.GetTypeCategory(toInstruction);
            tripleObj.OwnerTypeKey = InstructionKeyService.GetTypeKey(toInstruction);
            tripleObj.InheritsFromConcreteClass = InstructionKeyService.GetConcreteInheritance(toInstruction);

            return tripleObj;
        }

        private TripleObject BuildFromTriple(Instruction fromInstruction,
            string fromInstanceOwnerKey,
            MethodDefinition parentMethod)
        {
            var tripleObj = new TripleObject();
            tripleObj.Instruction = fromInstruction;

            var fromObjectType = InstructionKeyService.GetFromObjectType(fromInstruction);
            tripleObj.ObjectType = fromObjectType;

            if (fromObjectType == ObjectType.Argument)
            {
                int fromArgumentIndex = (int)DecompilerService.GetPosition(fromInstruction);
                tripleObj.ObjectKey = InstructionKeyService.GetObjectKey(fromInstruction, fromObjectType, parentMethod, fromArgumentIndex);
            }
            else
            {
                tripleObj.ObjectKey = InstructionKeyService.GetObjectKey(fromInstruction, fromObjectType, parentMethod);
            }

            tripleObj.InstructionKey = InstructionKeyService.GetInstructionKey(fromInstruction, parentMethod);
            tripleObj.OwnerTypeCategory = InstructionKeyService.GetTypeCategory(fromInstruction);
            tripleObj.OwnerTypeKey = InstructionKeyService.GetTypeKey(fromInstruction);
            tripleObj.InheritsFromConcreteClass = InstructionKeyService.GetConcreteInheritance(fromInstruction);
            tripleObj.InstanceOwnerKey = fromInstanceOwnerKey;

            return tripleObj;
        }

        //private Instruction JumpPastAnyChainedInstructions(Instruction instruction)
        //{
        //    var instructionCursor = instruction;
        //    bool isChained = false;
        //    while (instructionCursor != null && MethodArgumentInstructionParser.IsChainedToPreviousInstruction(instructionCursor))
        //    {
        //        isChained = true;
        //        instructionCursor = instructionCursor.Previous;
        //    }

        //    if (isChained)
        //        instructionCursor = instructionCursor.Next;

        //    return instructionCursor;
        //}

        //private Triple GetChainedTripleFromRealToVirtual(Instruction fromInstruction, MethodDefinition parentMethod, string chainObjectKey)
        //{
        //    var fromObjectType = InstructionKeyService.GetFromObjectType(fromInstruction);
        //    if (!InstructionKeyService.IsSupportedObject(fromObjectType))
        //        return null;

        //    var triple = new Triple();
        //    triple.ParentAssembly = parentMethod.Module.Assembly.Name.Name;
        //    triple.ParentMethod = parentMethod;

        //    triple.To = new TripleObject();
        //    triple.To.Instruction = null;
        //    triple.To.ObjectType = ObjectType.Chain;
        //    triple.To.ObjectKey = chainObjectKey;

        //    triple.From = new TripleObject();
        //    triple.From.Instruction = fromInstruction;
        //    triple.From.ObjectType = fromObjectType;
        //    triple.From.ObjectKey = InstructionKeyService.GetObjectKey(fromInstruction, fromObjectType, parentMethod);
        //    triple.From.OwnerTypeKey = InstructionKeyService.GetTypeKey(fromInstruction);
        //    triple.From.InstanceOwnerKey = InstructionKeyService.GetFromInstanceOwnerKey(fromInstruction, fromObjectType, parentMethod);
        //    triple.From.InstructionKey = InstructionKeyService.GetInstructionKey(fromInstruction, parentMethod);
        //    triple.From.OwnerTypeCategory = InstructionKeyService.GetTypeCategory(fromInstruction);
        //    triple.From.InheritsFromConcreteClass = InstructionKeyService.GetConcreteInheritance(fromInstruction);

        //    return triple;
        //}

        //private Triple GetChainedTripleFromVirtualToReal(Instruction toInstruction, MethodDefinition parentMethod, string chainObjectKey)
        //{
        //    var toObjectType = InstructionKeyService.GetToObjectType(toInstruction);
        //    if (!InstructionKeyService.IsSupportedObject(toObjectType))
        //        return null;

        //    var triple = new Triple();
        //    triple.ParentAssembly = parentMethod.Module.Assembly.Name.Name;
        //    triple.ParentMethod = parentMethod;

        //    triple.To = new TripleObject();
        //    triple.To.Instruction = toInstruction;
        //    triple.To.ObjectType = toObjectType;
        //    triple.To.ObjectKey = InstructionKeyService.GetObjectKey(toInstruction, toObjectType, parentMethod);
        //    triple.To.InstructionKey = InstructionKeyService.GetInstructionKey(toInstruction, parentMethod);
        //    triple.To.OwnerTypeCategory = InstructionKeyService.GetTypeCategory(toInstruction);
        //    triple.To.OwnerTypeKey = InstructionKeyService.GetTypeKey(toInstruction);
        //    triple.To.InheritsFromConcreteClass = InstructionKeyService.GetConcreteInheritance(toInstruction);

        //    triple.From.Instruction = null;
        //    triple.From.ObjectType = ObjectType.Chain;
        //    triple.From.ObjectKey = Guid.NewGuid().ToString();
        //    triple.From.InstanceOwnerKey = chainObjectKey;

        //    return triple;
        //}

        //private Instruction GetFromInstruction(Instruction instruction)
        //{
        //    var fromInstruction = instruction.Previous;
        //    if (fromInstruction.OpCode.Code == Mono.Cecil.Cil.Code.Nop)
        //    {
        //        var dupInstruction = GetCorrespondingDup(fromInstruction);
        //        fromInstruction = dupInstruction.Previous;
        //    }

        //    return fromInstruction;
        //}

        //private Instruction GetCorrespondingDup(Instruction nop)
        //{
        //    var instructionCursor = nop;
        //    int counter = 0;
        //    while (instructionCursor.Previous != null && instructionCursor.OpCode.Code != Mono.Cecil.Cil.Code.Dup)
        //    {
        //        // if it is not the starting nop then we have a nested nop-dup
        //        counter++;
        //        if (counter > 1 && instructionCursor.OpCode.Code == Mono.Cecil.Cil.Code.Nop)
        //            instructionCursor = GetCorrespondingDup(instructionCursor);

        //        instructionCursor = instructionCursor.Previous;
        //    }

        //    if (instructionCursor.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Nop)
        //    {
        //        instructionCursor = instructionCursor.Previous;
        //        return GetCorrespondingDup(instructionCursor);
        //    }

        //    return instructionCursor;
        //}
    }
}
