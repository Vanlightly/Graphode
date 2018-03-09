using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions;
using Graphode.CodeAnalyzer.Contracts.Common;
using System.Text.RegularExpressions;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    // singleinstance!
    public class TypeService : ITypeService
    {
        private IndexedList<string, TypeReference> _typesByInterfaceImplemented;

        public TypeService()
        {
            CleanIndexes();
        }

        public void CleanIndexes()
        {
            _typesByInterfaceImplemented = new IndexedList<string, TypeReference>();
        }

        public void Load(List<ModuleDefinition> modules)
        {
            var types = modules.SelectMany(x => x.Types).Where(x => x.Interfaces.Any()).ToList();

            foreach (var typeDefinition in types)
            {
                foreach (var interfce in typeDefinition.Interfaces)
                {
                    _typesByInterfaceImplemented.Add(interfce.InterfaceType.GetKey(), typeDefinition);
                }
            }
        }

        public List<TypeReference> GetTypesImplementing(string interfaceKey)
        {
            return _typesByInterfaceImplemented.Get(interfaceKey);
        }

        public static bool ShouldSkipResolve(TypeReference typeReference)
        {
            //if (typeReference.FullName.StartsWith("System.")
            //    || typeReference.FullName.Equals("<PrivateImplementationDetails>")
            //    || typeReference.FullName.IndexOf("anonymous") > -1
            //    || typeReference.FullName.IndexOf("__") > -1)
            //    return true;

            return false;
        }

        public static bool ShouldSkipDelegateIndexing(TypeDefinition typeReference)
        {
            if (ShouldSkipResolve(typeReference)
                || typeReference.BaseType != null && typeReference.BaseType.FullName.Equals("System.Data.Objects.DataClasses.EntityObject"))
                return true;

            return false;
        }

        public static List<string> GetGenericTypeParameters(string typeFullName)
        {
            int typesStart = typeFullName.IndexOf("<");
            int typesEnd = typeFullName.LastIndexOf(">");
            var genericTypesText = typeFullName.Substring(typesStart + 1, typesEnd - typesStart - 1);

            return ParseGenericParameters(genericTypesText);
        }

        public static List<string> GetGenericTypeParametersFromMethodName(string methodFullName)
        {
            var twoDotsIndex = methodFullName.IndexOf("::", StringComparison.Ordinal);
            var methodNameAndTypes = methodFullName.Substring(0, twoDotsIndex);
            int typesStart = methodNameAndTypes.IndexOf("<");
            int typesEnd = methodNameAndTypes.LastIndexOf(">");
            var genericTypesText = methodNameAndTypes.Substring(typesStart + 1, typesEnd - typesStart - 1);

            return ParseGenericParameters(genericTypesText);
        }

        public static List<string> GetGenericMethodParameters(string methodFullName)
        {
            var twoDotsIndex = methodFullName.IndexOf("::", StringComparison.Ordinal);
            if (twoDotsIndex == -1)
                return new List<string>();

            var methodNameAndTypes = methodFullName.Substring(twoDotsIndex+2);
            int typesStart = methodNameAndTypes.IndexOf("<");
            int typesEnd = methodNameAndTypes.LastIndexOf(">");

            if(typesStart == -1 || typesEnd == -1)
                return new List<string>();

            var genericTypesText = methodNameAndTypes.Substring(typesStart + 1, typesEnd - typesStart - 1);

            return ParseGenericParameters(genericTypesText);
        }

        private static List<string> ParseGenericParameters(string genericParamsText)
        {
            try
            {
                // type,type
                // type<x,type<y,z>>,type
                if (genericParamsText.IndexOf(">") == -1)
                    return genericParamsText.Split(',').ToList();

                var genericTypes = new List<string>();
                int nestCount = 0;
                int start = 0;
                for (int i = 0; i < genericParamsText.Length; i++)
                {
                    if (genericParamsText[i] == '<')
                        nestCount++;
                    else if (genericParamsText[i] == '>')
                        nestCount--;
                    else if (genericParamsText[i] == ',' && nestCount == 0)
                    {
                        var genType = genericParamsText.Substring(start, i - start);
                        genericTypes.Add(genType);
                        start = i + 1;
                    }
                }

                var lastGenType = genericParamsText.Substring(start);
                genericTypes.Add(lastGenType);

                return genericTypes;
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }

        public static string ReplaceGenericPlaceholders(string signature, List<string> genericTypes)
        {
            // works for up to 4 generic params
            // this is a bit of a hack really
            for (int i = 0; i < 4; i++)
            {
                if (i == genericTypes.Count)
                    return signature;

                var genParamPlaceholder1 = "!" + i;
                var genParamPlaceholder2 = "!!" + i;
                if (signature.IndexOf(genParamPlaceholder2) > -1)
                    signature = signature.Replace(genParamPlaceholder2, genericTypes[i]);
                else if (signature.IndexOf(genParamPlaceholder1) > -1)
                    signature = signature.Replace(genParamPlaceholder1, genericTypes[i]);
                else
                    break;
            }

            return signature;
        }

        public static string ReplaceGenericPlaceholders(string signature)
        {
            //---------------------------------------------------------
            // STEP 1 - replace thes !0 pattern with a generic param T
            // works for up to 4 generic params
            // this is a bit of a hack really
            for (int i = 0; i < 4; i++)
            {
                var genParamPlaceholder1 = "!" + i;
                var genParamPlaceholder2 = "!!" + i;
                if (signature.IndexOf(genParamPlaceholder2) > -1)
                    signature = signature.Replace(genParamPlaceholder2, "T");
                else if (signature.IndexOf(genParamPlaceholder1) > -1)
                    signature = signature.Replace(genParamPlaceholder1, "T");
                else
                    break;
            }

            //---------------------------------------------------------
            // STEP 2 - find all generic params, like T and log the index ranges in the string in the genTypeIndexes variable list
            var openPar = signature.IndexOf("(");
            var closePar = signature.IndexOf(")");
            var parametersText = signature.Substring(openPar+1, closePar - openPar - 1);
            var genTypeIndexes = new List<Tuple<int, int>>();

            // if only has a single parameter
            if (parametersText.IndexOf(",") == -1)
            {
                if (parametersText.IndexOf(".") == -1) // if it is a generic parameter
                {
                    var temp = signature.Substring(0, openPar) + "(GENERIC_TYPE)";
                    return temp;
                }
                else if(parametersText.IndexOf("`") > -1) // could have a generic param
                {
                    // need to parse it to extract the position of the generic type here :(
                    var genericParamMatches = Regex.Matches(parametersText, @"\<\w+\>");
                    foreach (Match match in genericParamMatches)
                    {
                        genTypeIndexes.Add(Tuple.Create(match.Index + 1, match.Index + 1 + match.Length - 3));
                    }
                }
            }
            else
            {
                int start = 0;
                int nestCount = 0;
                for (int i = 0; i < parametersText.Length; i++)
                {
                    if (parametersText[i] == '<')
                        nestCount++;
                    else if (parametersText[i] == '>')
                        nestCount--;

                    if ((parametersText[i] == ',' || i == parametersText.Length-1) && nestCount == 0)
                    {
                        string paramType = null;
                        if(i == parametersText.Length - 1)
                            paramType = parametersText.Substring(start);
                        else
                            paramType = parametersText.Substring(start, i - start);

                        if (paramType.IndexOf(".") == -1)
                        {
                            genTypeIndexes.Add(Tuple.Create(start, start+ paramType.Length - 1));
                        }
                        else if (paramType.IndexOf("`") > -1) // could have a generic param
                        {
                            // need to parse it to extract the position of the generic type here :(
                            var genericParamMatches = Regex.Matches(paramType, @"\<\w+\>");
                            foreach (Match match in genericParamMatches)
                                genTypeIndexes.Add(Tuple.Create(start + match.Index + 1, start + match.Index + 1 + match.Length - 3));

                            genericParamMatches = Regex.Matches(paramType, @"\<\w+,");
                            foreach (Match match in genericParamMatches)
                                genTypeIndexes.Add(Tuple.Create(start + match.Index + 1, start + match.Index + 1 + match.Length - 3));

                            genericParamMatches = Regex.Matches(paramType, @",\w+\>");
                            foreach (Match match in genericParamMatches)
                                genTypeIndexes.Add(Tuple.Create(start + match.Index + 1, start + match.Index + 1 + match.Length - 3));

                            genericParamMatches = Regex.Matches(paramType, @",\w+,");
                            foreach (Match match in genericParamMatches)
                                genTypeIndexes.Add(Tuple.Create(start + match.Index + 1, start + match.Index + 1 + match.Length - 3));
                        }

                        start = i + 1;
                    }
                }
            }

            //---------------------------------------------------------
            // STEP 3 - If any generic params were found then replace them with the placeholder GENERIC_TYPE and return the generic signature
            if (genTypeIndexes.Any())
            {
                genTypeIndexes = genTypeIndexes.OrderBy(x => x.Item1).ToList();

                try
                {
                    string genericSig = parametersText.Substring(0, genTypeIndexes.First().Item1) + "GENERIC_TYPE";

                    for (int i = 1; i < genTypeIndexes.Count; i++)
                    {
                        var s = genTypeIndexes[i - 1].Item2+1;
                        var end = genTypeIndexes[i].Item1 - 1;
                        var p = parametersText.Substring(s, end - s + 1);
                        genericSig += p + "GENERIC_TYPE";
                    }

                    if(genTypeIndexes.Last().Item2 < parametersText.Length-1)
                        genericSig += parametersText.Substring(genTypeIndexes.Last().Item2+1);

                    return signature.Substring(0, openPar) + "(" + genericSig + ")";
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }
            }

            return signature;
        }
    }
}
