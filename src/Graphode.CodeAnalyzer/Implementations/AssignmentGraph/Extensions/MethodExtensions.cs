using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations.Common;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions
{
    public static class MethodExtensions
    {
        public static string GetKey(this MethodDefinition methodDefinition)
        {
            return GetIdValue(methodDefinition);
        }

        public static string GetKey(this MethodReference methodReference)
        {
            return GetIdValue(methodReference);
        }

        public static string GetIdValue(MethodReference methodDefinition)
        {
            var spaceIndex = methodDefinition.FullName.IndexOf(" ", StringComparison.Ordinal);
            var signature = methodDefinition.FullName.Substring(spaceIndex + 1);
            var genericTypeMarkerIndex = signature.IndexOf("`", StringComparison.Ordinal);

            if (genericTypeMarkerIndex > -1) // is a method of a generic type and needs special logic
            {
                var twoDotsIndex = signature.IndexOf("::", StringComparison.Ordinal);
                if (twoDotsIndex < genericTypeMarkerIndex)
                    return signature;

                // extra generic types of method name
                if (signature.IndexOf("!") > -1)
                {
                    try
                    {
                        var genericTypes = TypeService.GetGenericTypeParametersFromMethodName(methodDefinition.FullName);
                        signature = TypeService.ReplaceGenericPlaceholders(signature, genericTypes);

                        var fullNameWithGenTypes = signature.Substring(0, genericTypeMarkerIndex + 2) + signature.Substring(twoDotsIndex);
                        return fullNameWithGenTypes;
                    }
                    catch(Exception ex)
                    {

                    }
                }

                var fullName = signature.Substring(0, genericTypeMarkerIndex + 2) + signature.Substring(twoDotsIndex);
                return fullName;
            }
            else if(methodDefinition.CallingConvention == MethodCallingConvention.Generic) // generic method
            {
                var genericTypes = TypeService.GetGenericMethodParameters(methodDefinition.FullName);
                var elementMethod = methodDefinition.GetElementMethod().FullName;
                elementMethod = TypeService.ReplaceGenericPlaceholders(elementMethod, genericTypes);
                spaceIndex = elementMethod.IndexOf(" ", StringComparison.Ordinal);
                signature = elementMethod.Substring(spaceIndex + 1);
            }

            return signature;
        }

        public static string GetGenericKey(this MethodReference methodReference)
        {
            return GetGenericIdValue(methodReference);
        }

        public static string GetGenericIdValue(MethodReference methodDefinition)
        {
            var spaceIndex = methodDefinition.FullName.IndexOf(" ", StringComparison.Ordinal);
            var signature = methodDefinition.FullName.Substring(spaceIndex + 1);
            var genericTypeMarkerIndex = signature.IndexOf("`", StringComparison.Ordinal);
            
            if (genericTypeMarkerIndex > -1) // is a method of a generic type and needs special logic
            {
                // has generic param but not a generic method
                var twoDotsIndex = signature.IndexOf("::", StringComparison.Ordinal);
                if (twoDotsIndex < genericTypeMarkerIndex)
                {
                    if (methodDefinition.CallingConvention == MethodCallingConvention.Generic) // generic method
                    {
                        var elementMethod = methodDefinition.GetElementMethod().FullName;
                        elementMethod = TypeService.ReplaceGenericPlaceholders(elementMethod);
                        spaceIndex = elementMethod.IndexOf(" ", StringComparison.Ordinal);
                        signature = elementMethod.Substring(spaceIndex + 1);

                        return signature;
                    }

                    return string.Empty;
                }

                // extra generic types of method name
                if (signature.IndexOf("!") > -1)
                {
                    try
                    {
                        signature = TypeService.ReplaceGenericPlaceholders(signature);

                        var fullNameWithGenTypes = signature.Substring(0, genericTypeMarkerIndex + 2) + signature.Substring(twoDotsIndex);
                        return fullNameWithGenTypes;
                    }
                    catch (Exception ex)
                    {

                    }
                }

                return string.Empty;
            }
            else if (methodDefinition.CallingConvention == MethodCallingConvention.Generic) // generic method
            {
                var elementMethod = methodDefinition.GetElementMethod().FullName;
                elementMethod = TypeService.ReplaceGenericPlaceholders(elementMethod);
                spaceIndex = elementMethod.IndexOf(" ", StringComparison.Ordinal);
                signature = elementMethod.Substring(spaceIndex + 1);

                return signature;
            }

            return string.Empty;
        }

        private static string GetGenericMethodKey(string signature, int genericMarkerIndex)
        {
            var twoDotsIndex = signature.IndexOf("::", StringComparison.Ordinal);
            if (twoDotsIndex < genericMarkerIndex)
                return signature;

            // extra generic types of method name
            if (signature.IndexOf("!") > -1)
            {
                try
                {
                    var genericTypes = TypeService.GetGenericTypeParametersFromMethodName(signature);

                    for (int i = 0; i < 4; i++)
                    {
                        var genParamPlaceholder = "!" + i;
                        if (signature.IndexOf(genParamPlaceholder) > -1)
                            signature = signature.Replace(genParamPlaceholder, genericTypes[i]);
                        else
                            break;
                    }

                    var fullNameWithGenTypes = signature.Substring(0, genericMarkerIndex + 2) + signature.Substring(twoDotsIndex);
                    return fullNameWithGenTypes;
                }
                catch (Exception ex)
                {

                }
            }

            var fullName = signature.Substring(0, genericMarkerIndex + 2) + signature.Substring(twoDotsIndex);
            return fullName;
        }

        public static string GetOwnerKey(MethodDefinition methodDefinition)
        {
            return methodDefinition.DeclaringType.GetKey();
        }

        public static string GetOwnerKey(this MethodReference methodDefinition)
        {
            return methodDefinition.DeclaringType.GetKey();
        }

        public static string GetKey(this TypeReference typeReference)
        {
            var genericMarkerIndex = typeReference.FullName.IndexOf("`", StringComparison.Ordinal);

            if (genericMarkerIndex > -1)
            {
                var fullName = typeReference.FullName.Substring(0, genericMarkerIndex + 2);
                return fullName;
            }

            return typeReference.FullName;
        }

        public static string GetKey(this TypeDefinition typeReference)
        {
            var genericMarkerIndex = typeReference.FullName.IndexOf("`", StringComparison.Ordinal);

            if (genericMarkerIndex > -1)
            {
                var fullName = typeReference.FullName.Substring(0, genericMarkerIndex + 2);
                return fullName;
            }

            return typeReference.FullName;
        }
    }
}
