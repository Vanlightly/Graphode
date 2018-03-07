using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.AssignmentGraph
{
    public enum ObjectType
    {
        NotDefined,
        Field,
        Method,
        Argument,
        LocalVariable,
        ReturnValue,
        InlineString,
        InlineNumber,
        NilArgument,
        None,
        Chain
    }
}
