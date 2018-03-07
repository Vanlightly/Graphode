using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.Common
{
    public enum PropertyBackingType
    {
        NonSupportedBackingType,
        AutoProperty,
        FieldBacked,
        MethodBacked,
        InlineString
    }
}
