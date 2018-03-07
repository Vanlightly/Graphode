using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class DecompilerService
    {
        public static ModuleDefinition GetModuleDefinition(string folder, string dllName)
        {
            var parameters = GetReaderParameters(folder);
            return ModuleDefinition.ReadModule(Path.Combine(folder, dllName), parameters);
        }

        private static ReaderParameters GetReaderParameters(string folder)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(folder);

            var parameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
            };

            return parameters;
        }

        public static long GetPosition(Instruction instruction)
        {
            if (instruction.OpCode.Name.Equals("stloc.s"))
            {
                return ((VariableReference)instruction.Operand).Index;
            }
            else if (instruction.OpCode.Name.Equals("ldloc.s"))
            {
                return ((VariableReference)instruction.Operand).Index;
            }
            else if (instruction.OpCode.Name.Equals("ldc.i4.s"))
            {
                return int.Parse(instruction.Operand.ToString());
            }
            else if (instruction.OpCode.Name.Equals("ldc.i4.m1"))
            {
                return -1;
            }
            else if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_I8
                || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_R4
                || instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldc_R8)
            {
                var stringVal = instruction.Operand.ToString();
                if (stringVal.IndexOf(".") > -1)
                    return long.Parse(instruction.Operand.ToString().Substring(0, stringVal.IndexOf(".")));

                return long.Parse(stringVal);
            }

            int index = -1;
            var parts = instruction.OpCode.Name.Split('.');
            int.TryParse(parts[parts.Length-1], out index);

            return index;
        }

        public static List<MethodDefinition> GetConcreteMethods(ModuleDefinition module)
        {
            return module.Types.Where(x => !x.IsInterface)
                               .SelectMany(x => x.Methods)
                               .Where(x => x.HasBody)
                               .ToList();
        }

        public static List<MethodDefinition> GetInterfaceMethods(ModuleDefinition module)
        {
            return module.Types.Where(x => x.IsInterface)
                               .SelectMany(x => x.Methods)
                               .ToList();
        }

        public static List<MethodDefinition> GetAbstractMethods(ModuleDefinition module)
        {
            return module.Types.Where(x => x.IsAbstract
                                        && !x.IsInterface)
                               .SelectMany(x => x.Methods)
                               .Where(x => x.IsAbstract)
                               .ToList();
        }

        public static List<MethodDefinition> GetPublicMethods(string companyAssembliesPattern, ModuleDefinition module)
        {
            return module.Types.Where(x => x.IsPublic
                    || (x.BaseType != null && x.BaseType.FullName.Equals("System.ServiceProcess.ServiceBase")) // is a windows service
                    || (Regex.IsMatch(x.Namespace, companyAssembliesPattern) && x.Interfaces.Any(i => i.FullName.IndexOf("Rhino.ServiceBus") > -1))) // is a windows service
                .SelectMany(x => x.Methods)
                .Where(x => x.IsPublic)
                .ToList();
        }
    }
}
