using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph.Extensions
{
    public static class FieldExtensions
    {
        public static string GetKey(this FieldDefinition fieldDefinition)
        {
            return "field_type:" + fieldDefinition.FieldType.FullName +
                    " owner_type:" + fieldDefinition.DeclaringType.FullName +
                    " field_name:" + fieldDefinition.Name;
        }

        public static string GetKey(this FieldReference fieldReference)
        {
            return "field_type:" + fieldReference.FieldType.FullName +
                    " owner_type:" + fieldReference.DeclaringType.FullName +
                    " field_name:" + fieldReference.Name;
        }

        public static string GetOwnerKey(this FieldDefinition fieldDefinition)
        {
            return fieldDefinition.DeclaringType.FullName;
        }

        public static string GetOwnerKey(this FieldReference fieldReference)
        {
            return fieldReference.DeclaringType.FullName;
        }
    }
}
