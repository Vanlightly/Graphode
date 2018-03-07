using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph.InstructionScopes;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class InstructionScopeParser
    {
        public static MainScope BuildMainScope(MethodDefinition methodDefinition)
        {
            var mainScope = new MainScope();
            var instructionCursor = methodDefinition.Body.Instructions.First();
            while (instructionCursor != null)
            {
                if (IsNewObjectInitializerScope(instructionCursor))
                {
                    mainScope.Instructions.Add(instructionCursor);
                    var objectInitializerScope = BuildObjectInitializer(instructionCursor, methodDefinition);
                    mainScope.ObjectInitializerScopes.Add(objectInitializerScope);
                    var endOffset = objectInitializerScope.GetEndOffset();
                    instructionCursor = JumpToInstructionAfterOffset(methodDefinition, endOffset);
                }
                else
                {
                    mainScope.Instructions.Add(instructionCursor);
                    instructionCursor = instructionCursor.Next;
                }
            }

            ReorderInstructions(mainScope.Instructions);

            return mainScope;
        }

        public static ObjectInitializerScope BuildObjectInitializer(Instruction contructorInstruction, MethodDefinition parentMethod)
        {
            var objectInitializerScope = new ObjectInitializerScope();
            objectInitializerScope.ParentMethod = parentMethod;
            objectInitializerScope.ConstructorInstruction = contructorInstruction;
            objectInitializerScope.ConstructorInstructionKey = InstructionKeyService.GetInstructionKey(contructorInstruction, parentMethod);
            objectInitializerScope.ConstructorOwnerKey = Guid.NewGuid().ToString();

            var currentMemberScope = new MemberScope();
            currentMemberScope.Parent = objectInitializerScope;
            objectInitializerScope.MemberScopes.Add(currentMemberScope);

            try
            {
                var instructionCursor = contructorInstruction.Next;

                while (instructionCursor != null && instructionCursor.Next != null)
                {
                    if (instructionCursor.OpCode.Code == Mono.Cecil.Cil.Code.Nop
                        && instructionCursor.Next.OpCode.Code == Mono.Cecil.Cil.Code.Dup)
                    {
                        currentMemberScope.Instructions.Add(instructionCursor);
                        currentMemberScope = new MemberScope();
                        currentMemberScope.Parent = objectInitializerScope;
                        objectInitializerScope.MemberScopes.Add(currentMemberScope);
                    }
                    else if (instructionCursor.OpCode.Code == Mono.Cecil.Cil.Code.Nop
                        && instructionCursor.Next.OpCode.Code != Mono.Cecil.Cil.Code.Dup)
                    {
                        currentMemberScope.Instructions.Add(instructionCursor);
                        break;
                    }
                    else if (IsNewObjectInitializerScope(instructionCursor))
                    {
                        currentMemberScope.Instructions.Add(instructionCursor);
                        currentMemberScope.NestedScope = BuildObjectInitializer(instructionCursor, parentMethod);

                        var nestedScopeEndOffset = currentMemberScope.NestedScope.GetEndOffset();
                        instructionCursor = JumpToInstructionAfterOffset(parentMethod, nestedScopeEndOffset);
                        if (instructionCursor == null)
                            break;
                        currentMemberScope.Instructions.Add(instructionCursor);
                    }
                    else
                    {
                        currentMemberScope.Instructions.Add(instructionCursor);
                    }

                    instructionCursor = instructionCursor.Next;
                }

            }
            catch(Exception ex)
            {

            }

            foreach (var memberScope in objectInitializerScope.MemberScopes)
                ReorderInstructions(memberScope.Instructions);

            return objectInitializerScope;
        }

        public static bool IsNewObjectInitializerScope(Instruction instruction)
        {
            if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Newobj)
            {
                var methodRef = (MethodReference)instruction.Operand;
                if (methodRef.DeclaringType.FullName.IndexOf("System.Func") > -1)
                    return false;

                // compiler generated
                if (methodRef.FullName.IndexOf("__") > -1)
                    return false;

                if (instruction.Next != null && instruction.Next.OpCode.Code == Mono.Cecil.Cil.Code.Dup)
                    return true;
            }

            return false;
        }

        private static void ReorderInstructions(List<Instruction> instructions)
        {
            if (instructions.Count < 2)
                return;

            int instructionCount = instructions.Count;
            for (int i = 0; i < instructionCount; i++)
            {
                if (i == 0 && instructionCount > 1)
                {
                    instructions[i].Previous = null;
                    instructions[i].Next = instructions[i + 1];
                }
                else if (i == instructionCount - 1 && instructionCount > 1)
                {
                    instructions[i].Previous = instructions[i - 1];
                    instructions[i].Next = null;
                }
                else
                {
                    instructions[i].Previous = instructions[i - 1];
                    instructions[i].Next = instructions[i + 1];
                }
            }
        }

        private static Instruction JumpToInstructionAfterOffset(MethodDefinition methodDefinition, int offset)
        {
            foreach(var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.Offset > offset)
                    return instruction;
            }

            return null;
        }

        //private Instruction GetLastInstructionOfChildScope(Instruction instruction)
        //{
        //    var lastInstruction = instruction.Next;
        //    if (instruction.Next.OpCode.Code == Mono.Cecil.Cil.Code.Dup)
        //    {
        //        return GetCorrespondingNop(instruction.Next);
        //    }
        //    else
        //    {
        //        throw new ILParseException("This is not a child scope");
        //    }
        //}

        //private Instruction GetCorrespondingNop(Instruction dup)
        //{
        //    var instructionCursor = dup;
        //    int counter = 0;
        //    while (instructionCursor.Next != null && instructionCursor.OpCode.Code != Mono.Cecil.Cil.Code.Nop)
        //    {
        //        // if it is not the starting dup then we have a nested dup-nop
        //        counter++;
        //        if (counter > 1 && instructionCursor.OpCode.Code == Mono.Cecil.Cil.Code.Dup)
        //            instructionCursor = GetCorrespondingNop(instructionCursor);

        //        instructionCursor = instructionCursor.Next;
        //    }

        //    if (instructionCursor.Next.OpCode.Code == Mono.Cecil.Cil.Code.Dup)
        //    {
        //        instructionCursor = instructionCursor.Next;
        //        return GetCorrespondingNop(instructionCursor);
        //    }

        //    return instructionCursor;
        //}
    }
}
