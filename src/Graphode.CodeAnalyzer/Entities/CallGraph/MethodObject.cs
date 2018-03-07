using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.CallGraph
{
    public class MethodObject
    {
        public MethodObject()
        {
            MethodsCalled = new List<MethodCall>();
            FieldsRead = new List<FieldReference>();
        }

        public string AppDomain { get; set; }

        public ImplementsType ImplementsType { get; set; }
        public MethodDefinition InterfaceMethod { get; set; }
        public MethodDefinition AbstractMethod { get; set; }
        public MethodDefinition VirtualMethod { get; set; }
        public MethodDefinition ConcreteMethod { get; set; }

        public List<MethodCall> MethodsCalled { get; set; }
        public List<FieldReference> FieldsRead { get; set; }

        public MethodDefinition GetMethodDefinition()
        {
            if (ConcreteMethod != null)
                return ConcreteMethod;

            if (InterfaceMethod != null)
                return InterfaceMethod;

            if (AbstractMethod != null)
                return AbstractMethod;

            throw new Exception("No method definition");
        }

        public MethodType GetMethodType()
        {
            if (HasImplementation() && HasInterface())
                return MethodType.ImplAndInterface;

            if (HasInterface())
                return MethodType.InterfaceOnly;

            if (HasImplementation())
                return MethodType.ImplementationOnly;

            return MethodType.NotDefined;
        }

        public bool HasImplementation()
        {
            return ConcreteMethod != null;
        }

        public bool HasInterface()
        {
            return InterfaceMethod != null;
        }

        public bool HasAbstract()
        {
            return AbstractMethod != null;
        }

        public bool OverridesBaseClass()
        {
            return VirtualMethod != null;
        }
    }
}
