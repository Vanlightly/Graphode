using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class InstructionService
    {
        #region .: Get Instructions :.

        public static List<Instruction> GetInstructions(MethodDefinition method)
        {
            if (method.HasBody)
                return method.Body.Instructions.ToList();

            return new List<Instruction>();

            //var instructions = new List<Instruction>();

            //if (method.HasBody)
            //{
            //    foreach (var instruction in method.Body.Instructions)
            //    {
            //        instructions.Add(instruction);

            //        if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            //        {
            //            MethodDefinition methodDef = null;
            //            var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
            //            if (!resolved)
            //                continue;

            //            if (instruction.OpCode.Name.Equals("newobj"))
            //            {
            //                var typeDefinition = methodDef.DeclaringType;

            //                if (typeDefinition != null && typeDefinition.IsNestedPrivate
            //                    && (typeDefinition.Name.IndexOf("__") > -1))
            //                {
            //                    // lazily evaluated state machine. For example, uses yield return
            //                    instructions.AddRange(ExtractNestedInstructions(typeDefinition));
            //                }
            //            }
            //            else if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldftn)
            //            {
            //                instructions.AddRange(GetInstructionsWithoutNesting(methodDef));
            //            }
            //        }
            //    }
            //}

            //return instructions;
        }

        private static List<Instruction> GetInstructionsWithoutNesting(MethodDefinition method)
        {
            if (method.HasBody)
                return method.Body.Instructions.ToList();

            return new List<Instruction>();
        }

        private static List<Instruction> ExtractNestedInstructions(TypeDefinition typeDefinition)
        {
            var instructions = new List<Instruction>();

            foreach (var nestedClassMethod in typeDefinition.Methods.Where(x => x.HasBody))
            {
                instructions.AddRange(nestedClassMethod.Body.Instructions);
            }

            return instructions;
        }

        #endregion .: Get Instructions :.
    }
}
