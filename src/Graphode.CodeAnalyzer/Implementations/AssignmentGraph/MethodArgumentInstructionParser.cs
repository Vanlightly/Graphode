using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Exceptions;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class MethodArgumentInstructionParser
    {
        public static Instruction GetParameterInstructionTree(MethodReference callingMethod, MethodReference method, Instruction instruction, InstructionTreeNode parentNode)
        {
            if(method.Name.Equals(".ctor") && 
                (method.DeclaringType.Name.IndexOf("Func`") > -1
                    || method.FullName.IndexOf("System.Object,System.IntPtr") > -1))
                return GetParameterInstructionTreeForFuncConstructor(callingMethod, method, instruction, parentNode);
            else
                return GetParameterInstructionTreeForNormalMethod(callingMethod, method, instruction, parentNode);
        }

        #region .: Normal Method :.

        private static Instruction GetParameterInstructionTreeForNormalMethod(MethodReference parentMethodRef, MethodReference method, Instruction instruction, InstructionTreeNode parentNode)
        {
            MethodDefinition parentMethod = parentMethodRef.Resolve();
            Instruction targetMethodInstruction = instruction;
            MethodDefinition targetMethodDef = null;
            var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out targetMethodDef);
            if (!resolved)
                throw new ILParseException("Could not resolve " + ((MethodReference)instruction.Operand).FullName);

            var parameterCount = method.Parameters.Count;

            if (parentNode.Instruction == null)
                parentNode.Instruction = instruction;

            for (int i = 0; i < parameterCount; i++)
            {
                if (instruction.Previous == null)
                    throw new ILParseException("Unexpectedly reached the end of the instructions while analysing " + parentMethodRef.FullName);

                instruction = GetPreviousInstruction(instruction);

                if (ShouldSkip(instruction))
                    instruction = GetPreviousInstruction(instruction);

                var childNode = new InstructionTreeNode();
                childNode.Instruction = instruction;
                parentNode.ChildInstructions.Add(childNode);

                if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var instructionMethodDef = ResolveMethod(instruction);
                    if (instructionMethodDef.Parameters.Any())
                    {
                        var lastInstructionOfParameters = GetParameterInstructionTree(parentMethodRef, instructionMethodDef, instruction, childNode);
                        instruction = lastInstructionOfParameters;
                        //if (AreInstructionsChained(instruction, lastInstructionOfParameters.Previous))
                        //    instruction = lastInstructionOfParameters.Previous; // move to chained instruction so later call jumps chained instructions
                    }
                }

                instruction = JumpPastAnyOwnerInstructions(instruction, parentMethod);
            }

            instruction = JumpAnyJumpableInstructionsOfTargetMethod(targetMethodInstruction, instruction, targetMethodDef, parentMethod);

            //if(TargetMethodHasJumpablePreviousInstruction(targetMethodInstruction, instruction, targetMethodDef, parentMethod))
            //    instruction = GetPreviousInstruction(instruction);
            //if (HasThis(callingMethod, methodDef) && instruction.OpCode.Code != Mono.Cecil.Cil.Code.Ldarg_0)
            //    instruction = GetPreviousInstruction(instruction);

            return instruction;
        }

        private static Instruction JumpPastAnyOwnerInstructions(Instruction instruction, MethodDefinition parentMethod)
        {
            if (IsChainedToPreviousInstruction(instruction, parentMethod))
            {
                instruction = JumpPastAnyChainedInstructions(instruction, parentMethod);
            }
            
            return instruction;
        }

        private static MethodDefinition ResolveMethod(Instruction instruction)
        {
            MethodDefinition instructionMethodDef = null;
            var subMethodResolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out instructionMethodDef);
            if (!subMethodResolved)
                throw new ILParseException("Could not resolve " + ((MethodReference)instruction.Operand).FullName);

            return instructionMethodDef;
        }

        public static bool IsChainedToPreviousInstruction(Instruction instruction, MethodDefinition parentMethod)
        {
            return AreInstructionsChained(instruction, instruction.Previous, parentMethod);
        }

        public static ChainType IsChainedToPreviousInstructionAsChainType(Instruction instruction, MethodDefinition parentMethod)
        {
            return AreInstructionsChainedAsChainType(instruction, instruction.Previous, parentMethod);
        }

        public static bool AreInstructionsChained(Instruction instruction, Instruction priorInstruction, MethodDefinition parentMethod)
        {
            var chainType = AreInstructionsChainedAsChainType(instruction, priorInstruction, parentMethod);
            return chainType == ChainType.Chained || chainType == ChainType.EndOfChain;
        }

        public static ChainType AreInstructionsChainedAsChainType(Instruction instruction, Instruction priorInstruction, MethodDefinition parentMethod)
        {
            if (IsStaticMember(instruction))
                return ChainType.NotChained;

            if (priorInstruction != null)
            {
                if (priorInstruction.Operand == null)
                {
                    if (priorInstruction.OpCode.Name.StartsWith("ldloc")
                        || priorInstruction.OpCode.Name.StartsWith("ldarg"))
                    {
                        // if it is ldarg.0 and a local 
                        if(priorInstruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                        {
                            if (GetInstructionType(instruction).Equals(parentMethod.DeclaringType.FullName))
                                return ChainType.EndOfChain;
                        }
                        else // is local variable or method argument
                        {
                            if (!GetInstructionType(instruction).Equals(GetInstructionType(priorInstruction)))
                                return ChainType.EndOfChain;
                        }
                    }
                }
                else
                {
                    if (priorInstruction.OpCode.Name.StartsWith("call"))
                    {
                        var methodRef = (MethodReference)priorInstruction.Operand;
                        if (methodRef.ReturnType != null && methodRef.ReturnType.FullName.Equals(GetInstructionType(instruction)))
                        {
                            return ChainType.Chained;
                        }
                    }
                    else if (priorInstruction.OpCode.Name.StartsWith("ldfld"))
                    {
                        var fieldRef = (FieldReference)priorInstruction.Operand;
                        if (fieldRef.FieldType.FullName.Equals(GetInstructionType(instruction)))
                        {
                            return ChainType.Chained;
                        }
                    }
                    else if (priorInstruction.OpCode.Name.StartsWith("ldloc"))
                    {
                        var variableRef = (VariableReference)priorInstruction.Operand;
                        if (variableRef.VariableType.FullName.Equals(GetInstructionType(instruction)))
                        {
                            return ChainType.EndOfChain;
                        }
                    }
                    else if (priorInstruction.OpCode.Name.StartsWith("ldarg")
                        && priorInstruction.OpCode.Code != Mono.Cecil.Cil.Code.Ldarg_0) // is method argument
                    {
                        // if both are method arguments then they are not chained
                        if (instruction.OpCode.Name.StartsWith("ldarg"))
                            return ChainType.NotChained;

                        var parameterRef = (ParameterReference)priorInstruction.Operand;
                        if (parameterRef.ParameterType.FullName.Equals(GetInstructionType(instruction)))
                        {
                            return ChainType.EndOfChain;
                        }
                    }
                }
            }

            return ChainType.NotChained;
        }

        public static Instruction JumpPastAnyChainedInstructions(Instruction instruction, MethodDefinition parentMethod)
        {
            var instructionCursor = instruction;
            
            while (instructionCursor != null && IsChainedToPreviousInstruction(instructionCursor, parentMethod))
            {
                bool endOfChain = IsChainedToPreviousInstructionAsChainType(instructionCursor, parentMethod) == ChainType.EndOfChain;
                instructionCursor = instructionCursor.Previous;

                if (endOfChain)
                    break;
            }

            if (instructionCursor.OpCode.OperandType == OperandType.InlineMethod)
            {
                var instructionMethodDef = ResolveMethod(instructionCursor);
                if (HasJumpablePreviousInstruction(instructionMethodDef, instructionCursor, parentMethod))
                    instructionCursor = instructionCursor.Previous;
            }
            else if (instructionCursor.OpCode.OperandType == OperandType.InlineField)
            {
                var fieldRef = ((FieldReference)instructionCursor.Operand).Resolve();
                if (HasJumpablePreviousInstruction(fieldRef, instructionCursor, parentMethod))
                    instructionCursor = instructionCursor.Previous;
            }

            return instructionCursor;
        }

        private static Instruction JumpAnyJumpableInstructionsOfTargetMethod(Instruction targetMethodInstruction, Instruction lastInstruction, MethodDefinition targetMethod, MethodDefinition parentMethod)
        {
            if (AreInstructionsChained(targetMethodInstruction, lastInstruction.Previous, parentMethod))
            {
                //return JumpPastAnyChainedInstructions(lastInstruction.Previous, parentMethod);
                return GetPreviousInstruction(lastInstruction);
            }
            // if the method belongs to a base class then we'll need to skip the ldarg.0
            else if (targetMethod.DeclaringType.FullName.Equals(parentMethod.DeclaringType.FullName)
                        || (parentMethod.DeclaringType.BaseType != null
                            && targetMethod.DeclaringType.FullName.Equals(parentMethod.DeclaringType.BaseType.FullName)))
            {
                if (lastInstruction.Previous != null && lastInstruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                {
                    return GetPreviousInstruction(lastInstruction);
                }
            }
            // if the method belongs to a different class then we'll need to skip the ldarg
            //else if (!instructionMethodDef.DeclaringType.FullName.Equals(callingMethodDef.DeclaringType.FullName))
            //{
            //    if (instruction.Previous != null
            //        && instruction.Previous.OpCode.Code != Mono.Cecil.Cil.Code.Ldarg_0
            //        && instruction.Previous.OpCode.Name.StartsWith("ldarg"))
            //    {
            //        return true;
            //    }
            //}
            // is a static method then skip the ldarg.0
            else if (!targetMethod.IsStatic && lastInstruction.Previous != null
                    && lastInstruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
            {
                return GetPreviousInstruction(lastInstruction);
            }

            return lastInstruction;
        }

        private static bool HasJumpablePreviousInstruction(MethodDefinition instructionMethodDef, Instruction instruction, MethodDefinition parentMethod)
        {
            // if the method belongs to a base class then we'll need to skip the ldarg.0
            if (instructionMethodDef.DeclaringType.FullName.Equals(parentMethod.DeclaringType.FullName)
                        || (parentMethod.DeclaringType.BaseType != null
                            && instructionMethodDef.DeclaringType.FullName.Equals(parentMethod.DeclaringType.BaseType.FullName)))
            {
                if (instruction.Previous != null && instruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                {
                    return true;
                }
            }
            else if(AreInstructionsChained(instruction, instruction.Previous, parentMethod))
            {
                return true;
            }
            // if the method belongs to a different class then we'll need to skip the ldarg
            //else if (!instructionMethodDef.DeclaringType.FullName.Equals(callingMethodDef.DeclaringType.FullName))
            //{
            //    if (instruction.Previous != null
            //        && instruction.Previous.OpCode.Code != Mono.Cecil.Cil.Code.Ldarg_0
            //        && instruction.Previous.OpCode.Name.StartsWith("ldarg"))
            //    {
            //        return true;
            //    }
            //}
            // WRONG!!! is a static method then skip the ldarg.0
            //else if (instructionMethodDef.IsStatic && instruction.Previous != null
            //        && instruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
            //{
            //    return true;
            //}

            return false;
        }

        private static bool HasJumpablePreviousInstruction(FieldDefinition fieldRef, Instruction instruction, MethodDefinition parentMethod)
        {
            // is a local field and previous is ldarg.0, then jump it
            if (HasThis(parentMethod, fieldRef) && instruction.Previous != null
                    && instruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
            {
                return true;
            }
            // if the method belongs to a base class then we'll need to skip the ldarg.0
            else if (fieldRef.DeclaringType.FullName.Equals(parentMethod.DeclaringType.FullName)
                        || (parentMethod.DeclaringType.BaseType != null
                            && fieldRef.DeclaringType.FullName.Equals(parentMethod.DeclaringType.BaseType.FullName)))
            {
                if (instruction.Previous != null && instruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                {
                    return true;
                }
            }
            else if (AreInstructionsChained(instruction, instruction.Previous, parentMethod))
            {
                return true;
            }
            // WRONG!!!! is a static method then skip the ldarg.0
            //else if (!fieldRef.IsStatic && instruction.Previous != null
            //        && instruction.Previous.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
            //{
            //    return true;
            //}

            return false;
        }

        public static string GetInstructionType(Instruction instruction)
        {
            if (instruction.Operand == null)
                return string.Empty;

            var memberReference = instruction.Operand as MemberReference;
            if (memberReference != null && memberReference.DeclaringType != null)
            {
                return memberReference.DeclaringType.FullName;
            }

            var variableReference = instruction.Operand as VariableReference;
            if (variableReference != null && variableReference.VariableType != null)
            {
                return variableReference.VariableType.FullName;
            }

            var parameterReference = instruction.Operand as ParameterReference;
            if (parameterReference != null && parameterReference.ParameterType != null)
            {
                return parameterReference.ParameterType.FullName;
            }

            return string.Empty;
        }

        public static bool IsStaticMember(Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                MethodDefinition methodDef = null;
                var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
                if (resolved)
                {
                    return methodDef.IsStatic;
                }
            }
            else if (instruction.OpCode.OperandType == OperandType.InlineField)
            {
                FieldDefinition fieldDef = null;
                var resolved = ResolveService.TryResolve((FieldReference)instruction.Operand, out fieldDef);
                if (resolved)
                {
                    return fieldDef.IsStatic;
                }
            }

            return false;
        }

        private static bool ShouldSkip(Instruction instruction)
        {
            return instruction.OpCode.Code == Mono.Cecil.Cil.Code.Box
                || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Unbox
                || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Dup
                || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Nop;
                //|| instruction.OpCode.Name.StartsWith("ldc");
        }

        public static Instruction GetPriorInstruction(MethodReference callingMethod, MethodReference method, Instruction instruction)
        {
            try
            {
                var rootNode = new InstructionTreeNode();
                var lastInstruction = GetParameterInstructionTree(callingMethod, method, instruction, rootNode);

                if(lastInstruction == null)
                {

                }

                // if the last instruction is ldarg.0 then return the instruction after that which will be the owner field or method
                if(lastInstruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                {
                    return lastInstruction.Next;
                }

                // if the last instruction is the target instruction, then return the previous one
                // this can happen with compiled generated classes
                if(lastInstruction.Offset == instruction.Offset)
                {
                    return lastInstruction.Previous;
                }

                return lastInstruction;
                //return GetPreviousInstruction(lastInstruction);
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private static Instruction GetPreviousInstruction(Instruction instruction)
        {
            if (instruction.Previous == null)
                return instruction;

            return instruction.Previous;
        }

        //public static Instruction GetPriorInstruction(MethodReference callingMethod, MethodReference method, Instruction instruction)
        //{
        //    var parameterCount = method.Parameters.Count;

        //    for (int i = 0; i < parameterCount; i++)
        //    {
        //        instruction = instruction.Previous;

        //        if (ShouldSkip(instruction))
        //            instruction = instruction.Previous;

        //        if (instruction.OpCode.OperandType == OperandType.InlineMethod)
        //        {
        //            var subMethodDef = ((MethodReference)instruction.Operand).Resolve();
        //            if (subMethodDef.Parameters.Any())
        //            {
        //                instruction = GoToEndOfScope(callingMethod, subMethodDef, instruction);
        //            }
        //        }
        //        else if (instruction.OpCode.OperandType == OperandType.InlineField)
        //        {
        //            if (HasThis(callingMethod, ((FieldReference)instruction.Operand).Resolve()))
        //                instruction = instruction.Previous;
        //        }
        //    }

        //    instruction = instruction.Previous;

        //    return instruction;
        //}

        //private static Instruction GoToEndOfScope(MethodReference callingMethod, MethodReference method, Instruction instruction)
        //{
        //    var methodDef = method.Resolve();
        //    var parameterCount = method.Parameters.Count;

        //    for (int i = 0; i < parameterCount; i++)
        //    {
        //        instruction = instruction.Previous;

        //        if (ShouldSkip(instruction))
        //            instruction = instruction.Previous;

        //        if (instruction.OpCode.OperandType == OperandType.InlineMethod)
        //        {
        //            var subMethodDef = ((MethodReference)instruction.Operand).Resolve();
        //            if (subMethodDef.Parameters.Any())
        //            {
        //                instruction = GetPriorInstruction(callingMethod, subMethodDef, instruction);
        //            }
        //        }
        //        else if (instruction.OpCode.OperandType == OperandType.InlineField)
        //        {
        //            if (HasThis(callingMethod, ((FieldReference)instruction.Operand).Resolve()))
        //                instruction = instruction.Previous;
        //        }
        //    }

        //    if (HasThis(callingMethod, methodDef))
        //        instruction = instruction.Previous;

        //    return instruction;
        //}

        private static bool HasThis(MethodReference callingMethod, MethodDefinition methodDef)
        {
            if (callingMethod.DeclaringType.FullName.Equals(methodDef.DeclaringType.FullName))
            {
                return !methodDef.IsStatic;
            }

            return false;
        }

        private static bool HasThis(MethodReference callingMethod, FieldDefinition methodDef)
        {
            if (callingMethod.DeclaringType.FullName.Equals(methodDef.DeclaringType.FullName))
            {
                return !methodDef.IsStatic;
            }

            return false;
        }

        #endregion .: Normal Method :.


        #region .: Func Constructor :.

        public static Instruction GetParameterInstructionTreeForFuncConstructor(MethodReference callingMethod, MethodReference funcMethod, Instruction instruction, InstructionTreeNode parentNode)
        {
            if (parentNode.Instruction == null)
                parentNode.Instruction = instruction;

            int parameterCount = GetParameterCount(funcMethod);

            for (int i = 0; i < parameterCount; i++)
            {
                instruction = GetPreviousInstruction(instruction);
                var childNode = new InstructionTreeNode();
                childNode.Instruction = instruction;
                parentNode.ChildInstructions.Add(childNode);
            }

            return instruction;
        }

        private static int GetParameterCount(MethodReference funcMethod)
        {
            int aposIndex = funcMethod.DeclaringType.Name.IndexOf("`");
            var value = funcMethod.DeclaringType.Name.Substring(aposIndex + 1);

            int count = 0;
            var success = int.TryParse(value, out count);
            if (success)
                return count;
            else
                return 1; // TODO: this needs further analysis
        }

        #endregion .: Func Constructor :.
    }
}
