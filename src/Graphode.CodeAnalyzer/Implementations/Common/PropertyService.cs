using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.Common;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class PropertyService
    {
        private static Dictionary<string, PropertyBackingType> _analyzedProperties = new Dictionary<string, PropertyBackingType>();

        public static void CleanIndexes()
        {
            _analyzedProperties.Clear();
        }

        public static PropertyBackingType GetBackingField(MethodDefinition method)
        {
            var signature = SignatureKeyService.GetFullMethodSignature(method);

            if (_analyzedProperties.ContainsKey(signature))
                return _analyzedProperties[signature];

            PropertyBackingType backingType;

            if (InstructionService.GetInstructions(method).Any(x => x.ToString().IndexOf("BackingField") > -1))
            {
                backingType = PropertyBackingType.AutoProperty;
            }
            else
            {
                var backingMethods = InstructionService.GetInstructions(method).Where(x => x.OpCode.OperandType == OperandType.InlineMethod)
                        .Select(x => x.Operand as MethodReference).ToList();

                var backingFields = InstructionService.GetInstructions(method).Where(x => x.OpCode.OperandType == OperandType.InlineField)
                    .Select(x => x.Operand as FieldReference).ToList();

                var backingStrings = InstructionService.GetInstructions(method).Where(x => x.OpCode.OperandType == OperandType.InlineString)
                    .Select(x => x.Operand.ToString()).ToList();

                if (backingMethods.Any())
                {
                    backingType = PropertyBackingType.MethodBacked;
                }
                else if (backingFields.Any())
                {
                    backingType = PropertyBackingType.FieldBacked;
                }
                else if (backingStrings.Any())
                {
                    backingType = PropertyBackingType.InlineString;
                }
            }

            backingType = PropertyBackingType.NonSupportedBackingType;
            _analyzedProperties.Add(signature, backingType);

            return backingType;
        }
    }
}
