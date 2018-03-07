using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class SignatureKeyService
    {
        private static Dictionary<string, string> _fullSignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _lightSignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _genericSignatures = new Dictionary<string, string>();

        public static void CleanIndexes()
        {
            _fullSignatures.Clear();
            _lightSignatures.Clear();
            _genericSignatures.Clear();
        }

        public static string GetFullMethodSignature(MethodReference method)
        {
            if (_fullSignatures.ContainsKey(method.FullName))
                return _fullSignatures[method.FullName];

            var signature = method.GetKey();
            _fullSignatures.Add(method.FullName, signature);
            return signature;
        }

        public static string GetGenericMethodSignature(MethodReference method)
        {
            if (_genericSignatures.ContainsKey(method.FullName))
                return _genericSignatures[method.FullName];

            var signature = method.GetGenericKey();
            if(!string.IsNullOrEmpty(signature))
                _genericSignatures.Add(method.FullName, signature);

            return signature;
        }

        public static string ConvertFullNameToSignature(string fullName)
        {
            var spaceIndex = fullName.IndexOf(" ", StringComparison.Ordinal);
            var signature = fullName.Substring(spaceIndex + 1);

            return signature;
        }

        public static string GetMethodSignature(MethodReference method)
        {
            if (_lightSignatures.ContainsKey(method.FullName))
                return _lightSignatures[method.FullName];

            var signature = method.FullName.Substring(method.FullName.IndexOf("::") + 2);
            _lightSignatures.Add(method.FullName, signature);

            return signature;
        }
    }
}
