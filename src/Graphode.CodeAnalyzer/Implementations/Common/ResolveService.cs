using Graphode.CodeAnalyzer.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class ResolveService
    {
        private static HashSet<string> _unresolvableAssemblies = new HashSet<string>();
        private static Dictionary<string, int> _assemblyResolutionErrorCounts = new Dictionary<string, int>();

        private static HashSet<string> _unresolvableTypes = new HashSet<string>();
        private static HashSet<string> _unresolvableMembers = new HashSet<string>();
        private static Dictionary<string, MethodDefinition> _resolvedMethods = new Dictionary<string, MethodDefinition>();
        private static Dictionary<string, FieldDefinition> _resolvedFields = new Dictionary<string, FieldDefinition>();
        private static Dictionary<string, TypeDefinition> _resolvedTypes= new Dictionary<string,TypeDefinition>();

        public static void CleanIndexes()
        {
            _unresolvableAssemblies.Clear();
            _assemblyResolutionErrorCounts.Clear();
            _unresolvableTypes.Clear();
            _unresolvableMembers.Clear();
            _resolvedMethods.Clear();
            _resolvedTypes.Clear();
        }

        public static bool TryResolve(MethodReference methodReference, out MethodDefinition methodDefinition)
        {
            try
            {
                if (!_unresolvableAssemblies.Contains(methodReference.DeclaringType.Module.Assembly.Name.Name) && !_unresolvableMembers.Contains(methodReference.FullName))
                {
                    if (_resolvedMethods.ContainsKey(methodReference.FullName))
                    {
                        methodDefinition = _resolvedMethods[methodReference.FullName];
                        return true;
                    }

                    methodDefinition = methodReference.Resolve();
                    if (methodDefinition != null)
                    {
                        _resolvedMethods.Add(methodReference.FullName, methodDefinition);
                        return true;
                    }
                    else
                    {
                        IncrementAssemblyResolutionErrorCounter(methodReference);
                        _unresolvableMembers.Add(methodReference.FullName);
                        LogOutput.LogAnalysisMessage("ERROR Failed to resolve method " + methodReference.FullName);
                        return false;
                    }
                }
                else
                {
                    methodDefinition = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogOutput.LogAnalysisMessage("ERROR Failed to resolve method " + methodReference.FullName);
                methodDefinition = null;
                _unresolvableMembers.Add(methodReference.FullName);
                IncrementAssemblyResolutionErrorCounter(methodReference);
                return false;
            }
        }

        public static bool TryResolve(FieldReference fieldReference, out FieldDefinition fieldDefinition)
        {
            try
            {
                if (!_unresolvableAssemblies.Contains(fieldReference.DeclaringType.Module.Assembly.Name.Name) && !_unresolvableMembers.Contains(fieldReference.FullName))
                {
                    if (_resolvedFields.ContainsKey(fieldReference.FullName))
                    {
                        fieldDefinition = _resolvedFields[fieldReference.FullName];
                        return true;
                    }

                    fieldDefinition = fieldReference.Resolve();
                    if (fieldDefinition != null)
                    {
                        _resolvedFields.Add(fieldReference.FullName, fieldDefinition);
                        return true;
                    }
                    else
                    {
                        IncrementAssemblyResolutionErrorCounter(fieldReference);
                        _unresolvableMembers.Add(fieldReference.FullName);
                        LogOutput.LogAnalysisMessage("ERROR Failed to resolve field " + fieldReference.FullName);
                        return false;
                    }
                }
                else
                {
                    fieldDefinition = null;
                    return false;
                }
            }
            catch (Exception)
            {
                LogOutput.LogAnalysisMessage("ERROR Failed to resolve method " + fieldReference.FullName);
                fieldDefinition = null;
                _unresolvableMembers.Add(fieldReference.FullName);
                IncrementAssemblyResolutionErrorCounter(fieldReference);
                return false;
            }
        }

        private static void IncrementAssemblyResolutionErrorCounter(MemberReference memberReference)
        {
            if (_assemblyResolutionErrorCounts.ContainsKey(memberReference.DeclaringType.Module.Assembly.Name.Name))
            {
                int currentValue = _assemblyResolutionErrorCounts[memberReference.DeclaringType.Module.Assembly.Name.Name];
                if (currentValue > 100)
                    _unresolvableAssemblies.Add(memberReference.DeclaringType.Module.Assembly.Name.Name);
                else
                    _assemblyResolutionErrorCounts[memberReference.DeclaringType.Module.Assembly.Name.Name] = currentValue + 1;
            }
            else
                _assemblyResolutionErrorCounts.Add(memberReference.DeclaringType.Module.Assembly.Name.Name, 1);
        }

        public static bool TryResolve(TypeReference typeReference, out TypeDefinition typeDefinition)
        {
            try
            {
                if (_resolvedTypes.ContainsKey(typeReference.FullName))
                {
                    typeDefinition = _resolvedTypes[typeReference.FullName];
                    return true;
                }

                if (!_unresolvableTypes.Contains(typeReference.FullName))
                {
                    typeDefinition = typeReference.Resolve();
                    _resolvedTypes.Add(typeReference.FullName, typeDefinition);
                    return true;
                }
                else
                {
                    typeDefinition = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogOutput.LogAnalysisMessage("ERROR Failed to resolve type " + typeReference.FullName);
                typeDefinition = null;
                _unresolvableTypes.Add(typeReference.FullName);
                return false;
            }
        }

        public static bool TryResolveMethodDeclaringType(MethodReference methodRef, out TypeDefinition typeDefinition)
        {
            if (methodRef.DeclaringType.IsGenericInstance)
            {
                var genType = (GenericInstanceType)methodRef.DeclaringType;
                if (genType.DeclaringType != null)
                    return TryResolve(genType.DeclaringType, out typeDefinition);

                typeDefinition = null;
                return false;
            }
            else
            {
                return TryResolve(methodRef.DeclaringType, out typeDefinition);
            }
        }
    }
}
