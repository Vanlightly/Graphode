using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Exceptions
{
    [Serializable]
    public class ILParseException : Exception
    {
        public ILParseException(string message)
            : base(message)
        { }

    }
}
