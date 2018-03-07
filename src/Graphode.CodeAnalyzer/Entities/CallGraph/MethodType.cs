using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.CallGraph
{
    public enum MethodType
    {
        NotDefined,
        InterfaceOnly,
        ImplementationOnly,
        ImplAndInterface
    }
}
