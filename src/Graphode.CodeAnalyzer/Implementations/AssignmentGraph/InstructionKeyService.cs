using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.Common;
using Graphode.CodeAnalyzer.Exceptions;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class InstructionKeyService
    {
        public static ObjectType GetToObjectType(Instruction instruction)
        {
            if (instruction.OpCode.Name.StartsWith("stfld"))
                return ObjectType.Field;

            if (instruction.OpCode.Name.StartsWith("stsfld"))
                return ObjectType.Field;

            if (instruction.OpCode.Name.StartsWith("stloc"))
                return ObjectType.LocalVariable;

            if (instruction.OpCode.Name.StartsWith("ret"))
                return ObjectType.ReturnValue;

            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var methodReference = (MethodReference)instruction.Operand;
                if (methodReference.HasParameters)
                    return ObjectType.Argument;
                else
                    return ObjectType.NilArgument;
            }

            return ObjectType.NotDefined;
        }

        public static ObjectType GetFromObjectType(Instruction instruction)
        {
            if (instruction.OpCode.Name.StartsWith("ldfld"))
                return ObjectType.Field;

            if (instruction.OpCode.Name.StartsWith("ldsfld"))
                return ObjectType.Field;

            if (instruction.OpCode.Name.StartsWith("ldloc"))
                return ObjectType.LocalVariable;

            if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0)
                return ObjectType.None;

            if (instruction.OpCode.Name.StartsWith("ldarg"))
                return ObjectType.Argument;

            if (instruction.OpCode.Name.StartsWith("ldstr"))
                return ObjectType.InlineString;

            if (instruction.OpCode.Name.StartsWith("ldc"))
                return ObjectType.InlineNumber;

            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                return ObjectType.Method;

            return ObjectType.NotDefined;
        }

        public static string GetObjectKey(Instruction instruction, ObjectType objectType, MethodReference methodOfArgument, int? argumentIndex = null)
        {
            if (objectType == ObjectType.Argument && !argumentIndex.HasValue)
                argumentIndex = (int)DecompilerService.GetPosition(instruction);

            switch (objectType)
            {
                case ObjectType.Argument:
                    return methodOfArgument.GetKey() + ">>" + argumentIndex;
                case ObjectType.NilArgument:
                    var nilArgMethodReference = (MethodReference)instruction.Operand;
                    return nilArgMethodReference.GetKey() + ">>0";
                case ObjectType.Field:
                    var fieldReference = (FieldReference)instruction.Operand;
                    return fieldReference.GetKey();
                case ObjectType.LocalVariable:
                    var stackPosition = DecompilerService.GetPosition(instruction);
                    return methodOfArgument.GetKey() + " StackPosition:" + stackPosition;
                case ObjectType.ReturnValue:
                    return methodOfArgument.GetKey() + ">>return";
                case ObjectType.Method:
                    var methodReference = (MethodReference)instruction.Operand;
                    return methodReference.GetKey() + ">>return";
                case ObjectType.InlineString:
                    return instruction.Operand.ToString();
                case ObjectType.InlineNumber:
                    var number = DecompilerService.GetPosition(instruction);
                    return number.ToString();
                case ObjectType.None:
                    return null;
                default:
                    throw new ILParseException("Unsupported ObjectType: " + objectType);
            }
        }

        //public static string GetLinkKey(Instruction instruction, string objectKey, MethodReference methodOfInstruction)
        //{
        //    int arrowIndex = objectKey.IndexOf(">>");
        //    if (arrowIndex > -1)
        //    {
        //        return objectKey.Substring(0, arrowIndex) + GetInstructionKey(instruction, methodOfInstruction);
        //    }
        //    //else if(objectKey.IndexOf("get_") > -1 || objectKey.IndexOf("set_") > -1)
        //    //{
        //    //    return objectKey.Substring(0, objectKey.LastIndexOf("(")).Replace("set_", "").Replace("get_", "") + "()";
        //    //}

        //    return objectKey + instruction.Offset + GetInstructionKey(instruction, methodOfInstruction);
        //}

        public static string GetInstructionKey(Instruction instruction, MethodReference methodOfInstruction)
        {
            return "|" + methodOfInstruction.GetKey().GetHashCode() + "@" + instruction.Offset;
        }

        public static string GetTypeOwnerKey(Instruction instruction, ObjectType objectType, MethodDefinition parentMethod)
        {
            switch (objectType)
            {
                case ObjectType.Field:
                    var fieldReference = (FieldReference)instruction.Operand;
                    return fieldReference.GetOwnerKey();
                case ObjectType.Method:
                    var methodReference = (MethodReference)instruction.Operand;
                    return methodReference.GetOwnerKey();
                default:
                    return parentMethod.GetOwnerKey();
            }
        }

        /// <summary>
        /// This is a mixed bag that needs to be cleaned up as it means different things to different parts of the logic
        /// Returns the key of the instance object that owns this object. 
        /// For 
        ///  - This.Methods is the type key
        ///  - This.Fields is the Field object key
        ///  - Methods and fields on other classes is the instantiated object owner
        ///  - arguments it is the key of the method
        /// This field is used when
        /// - Navigating to the instance when backtracking
        /// - Identifying ??
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="objectType"></param>
        /// <param name="parentMethod"></param>
        /// <returns></returns>
        public static string GetToInstanceOwnerKey(Instruction instruction, ObjectType objectType, MethodDefinition parentMethod)
        {
            if (objectType == ObjectType.NotDefined || objectType == ObjectType.ReturnValue || objectType == ObjectType.InlineString || objectType == ObjectType.None || objectType == ObjectType.LocalVariable)
                return null;

            Instruction priorInstruction = null;
            if (objectType == ObjectType.Method || objectType == ObjectType.Argument || objectType == ObjectType.NilArgument)
            {
                MethodDefinition methodDef = null;
                var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
                if (methodDef.IsStatic || methodDef.Name.Equals(".ctor"))
                    return parentMethod.GetKey();
                else if (methodDef.GetOwnerKey().Equals(parentMethod.GetOwnerKey()))
                    return methodDef.GetOwnerKey();

                priorInstruction = MethodArgumentInstructionParser.GetPriorInstruction(parentMethod, methodDef, instruction);
            }
            else if (objectType == ObjectType.Field)
            {
                var fieldReference = (FieldReference)instruction.Operand;
                if(fieldReference.DeclaringType.FullName.Equals(parentMethod.DeclaringType.FullName))
                    return null;

                priorInstruction = instruction.Previous;
            }
            else
            {
                return null;

                //MethodDefinition methodDef = null;
                //var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
                //if (methodDef.IsStatic || methodDef.Name.Equals(".ctor"))
                //    return parentMethod.GetKey();

                //if (methodDef.GetOwnerKey().Equals(parentMethod.GetOwnerKey()))
                //    return methodDef.GetOwnerKey();

                //priorInstruction = instruction.Previous;
            }

            if (priorInstruction == null)
                return null;

            //if (MethodArgumentInstructionParser.AreInstructionsChained(instruction, priorInstruction))
            //    return null;

            var ownerObjectType = GetFromObjectType(priorInstruction);
            if (!IsSupportedObject(ownerObjectType))
                return null;

            var ownerInstanceKey = GetObjectKey(priorInstruction, ownerObjectType, parentMethod);

            return ownerInstanceKey;
        }

        public static string GetFromInstanceOwnerKey(Instruction instruction, ObjectType objectType, MethodDefinition parentMethod)
        {
            if (objectType == ObjectType.NotDefined || objectType == ObjectType.ReturnValue || objectType == ObjectType.InlineString 
                || objectType == ObjectType.None || objectType == ObjectType.LocalVariable || objectType == ObjectType.InlineNumber)
                return null;

            // for argument we use the key of the method
            if (objectType == ObjectType.Argument)
                return parentMethod.GetKey();

            Instruction priorInstruction = null;
            if (objectType == ObjectType.Method)
            {
                MethodDefinition methodDef = null;
                var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
                if (!resolved)
                    return null;
                else if (methodDef.IsStatic || methodDef.Name.Equals(".ctor"))
                    return parentMethod.GetKey();
                else if (methodDef.GetOwnerKey().Equals(parentMethod.GetOwnerKey()))
                    return methodDef.GetOwnerKey();

                priorInstruction = MethodArgumentInstructionParser.GetPriorInstruction(parentMethod, methodDef, instruction);
            }
            else if (objectType == ObjectType.Field)
            {
                var fieldReference = (FieldReference)instruction.Operand;
                if (fieldReference.DeclaringType.FullName.Equals(parentMethod.DeclaringType.FullName))
                    return null;

                priorInstruction = instruction.Previous;
            }
            else
            {
                return null;
                //MethodDefinition methodDef = null;
                //var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out methodDef);
                //if (methodDef.IsStatic || methodDef.Name.Equals(".ctor"))
                //    return parentMethod.GetKey();

                //if (methodDef.GetOwnerKey().Equals(parentMethod.GetOwnerKey()))
                //    return methodDef.GetOwnerKey();

                //priorInstruction = instruction.Previous;
            }

            if (priorInstruction == null)
                return null;

            var ownerObjectType = GetFromObjectType(priorInstruction);
            if (!IsSupportedObject(ownerObjectType))
                return null;

            var ownerInstanceKey = GetObjectKey(priorInstruction, ownerObjectType, parentMethod);

            return ownerInstanceKey;
        }

        public static string GetTypeKey(Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var method = (MethodReference)instruction.Operand;
                return method.DeclaringType.GetKey();
            }
            else if (instruction.OpCode.OperandType == OperandType.InlineField)
            {
                var field = (FieldReference)instruction.Operand;
                return field.DeclaringType.GetKey();
            }
            else if (instruction.OpCode.OperandType == OperandType.InlineArg)
            {
                var field = (ParameterReference)instruction.Operand;
                return field.ParameterType.GetKey();
            }

            return null;
        }

        public static string GetConcreteInheritance(Instruction instruction)
        {
            TypeReference typeRef = null;

            var memberRef = instruction.Operand as MemberReference;
            if (memberRef != null)
            {
                if(TypeService.ShouldSkipResolve(memberRef.DeclaringType))
                    return null;

                TypeDefinition typeDef = null;
                bool resolved = ResolveService.TryResolve(memberRef.DeclaringType, out typeDef);
                if (resolved)
                {
                    if (typeDef.BaseType != null)
                    {
                        return typeDef.BaseType.FullName;
                    }
                }
            }
            else if (instruction.OpCode.OperandType == OperandType.InlineArg)
            {
                var field = (ParameterReference)instruction.Operand;
                TypeDefinition typeDef = null;
                bool resolved = ResolveService.TryResolve(field.ParameterType, out typeDef);
                if (resolved)
                {
                    return typeDef.BaseType.FullName;
                }
            }

            return null;
        }

        public static TypeCategory GetTypeCategory(Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                MethodDefinition method = null;
                var resolved = ResolveService.TryResolve((MethodReference)instruction.Operand, out method);

                if (method.DeclaringType.IsInterface)
                    return TypeCategory.Interface;
            }

            return TypeCategory.Concrete;
        }

        public static bool IsSupportedObject(ObjectType objectType)
        {
            return objectType != ObjectType.NotDefined;
        }
    }
}
