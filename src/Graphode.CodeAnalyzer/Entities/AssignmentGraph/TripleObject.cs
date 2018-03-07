using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.Common;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public class TripleObject
    {
        public Instruction Instruction { get; set; }

        #region Keys
        public ObjectType ObjectType { get; set; }
        public string ObjectKey { get; set; }
        public string OwnerTypeKey { get; set; }
        public TypeCategory OwnerTypeCategory { get; set; }
        public string InstanceOwnerKey { get; set; }
        public string InheritsFromConcreteClass { get; set; }

        public string InstructionKey { get; set; }
        public string MemberName
        {
            get
            {
                if (ObjectKey == null)
                    return Guid.NewGuid().ToString();

                if (ObjectKey.IndexOf(")") > -1)
                    return ObjectKey.Substring(0, ObjectKey.LastIndexOf(")") + 1);

                return ObjectKey;
            }
        }

        private string _objectKeyType;
        public string ObjectKeyType
        {
            get
            {
                if (_objectKeyType != null)
                {
                    if(!_objectKeyType.Equals(OwnerTypeKey))
                    {

                    }

                    return _objectKeyType;
                }

                if (ObjectKey == null)
                    return null;

                int twoDotsIndex = ObjectKey.IndexOf("::");
                if (twoDotsIndex == -1)
                {
                    if (!ObjectKey.Equals(OwnerTypeKey))
                    {

                    }

                    return ObjectKey;
                }

                _objectKeyType = ObjectKey.Substring(0, twoDotsIndex);

                if (!_objectKeyType.Equals(OwnerTypeKey))
                {

                }
                return _objectKeyType;
            }
        }

        private string _objectKeyMember;
        public string ObjectKeyMember
        {
            get
            {
                if (_objectKeyMember != null)
                    return _objectKeyMember;

                if (ObjectKey == null)
                    return null;

                int twoDotsIndex = ObjectKey.IndexOf("::");
                if (twoDotsIndex == -1)
                    return ObjectKey;

                _objectKeyMember = ObjectKey.Substring(twoDotsIndex+2);
                return _objectKeyMember;
            }
        }

        #endregion Keys

        public override string ToString()
        {
            if (ObjectType == ObjectType.None)
                return "None";

            return ObjectKey;
        }

        public string ToInstructionKeyString()
        {
            if (ObjectType == ObjectType.None)
                return "None";

            return InstructionKey;
        }
    }
}
