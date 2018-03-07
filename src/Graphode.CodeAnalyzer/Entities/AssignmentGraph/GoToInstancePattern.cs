using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public enum TryInstance
    {
        First,
        Only
    }

    public class GoToInstancePattern
    {
        public string TypeMustHavePattern { get; set; }
        public string TypeMustNotHavePattern { get; set; }
        public string MemberMustHavePattern { get; set; }
        public string MemberMustNotHavePattern { get; set; }
        public TryInstance TryInstance { get; set; }

        public bool IsMatch(string typeName, string memberName)
        {
            var isTypeMatch = IsTypeMatch(typeName);
            if (!isTypeMatch)
                return false;

            return IsMemberMatch(memberName);
        }

        private bool IsTypeMatch(string typeName)
        {
            if (!string.IsNullOrEmpty(TypeMustHavePattern))
            {
                if (typeName.IndexOf(TypeMustHavePattern) == -1)
                    return false;
            }

            if (!string.IsNullOrEmpty(TypeMustNotHavePattern))
            {
                if (typeName.IndexOf(TypeMustNotHavePattern) > -1)
                    return false;
            }

            return true;
        }

        private bool IsMemberMatch(string memberName)
        {
            if (!string.IsNullOrEmpty(MemberMustHavePattern))
            {
                if (memberName.IndexOf(MemberMustHavePattern) == -1)
                    return false;
            }

            if (!string.IsNullOrEmpty(MemberMustNotHavePattern))
            {
                if (memberName.IndexOf(MemberMustNotHavePattern) > -1)
                    return false;
            }

            return true;
        }

        public bool IsObjectInitializerMatch(string typeName, string memberName)
        {
            var isTypeMatch = IsTypeMatch(typeName);
            if (!isTypeMatch)
                return false;

            return memberName.StartsWith(".ctor");
        }
        
    }
}
